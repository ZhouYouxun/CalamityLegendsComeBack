using CalamityLegendsComeBack.Weapons.Vesuvius.Core;
using CalamityLegendsComeBack.Weapons.Vesuvius.EXSkill;
using CalamityLegendsComeBack.Weapons.Vesuvius.Passive;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.RightClick
{
    public sealed class VesuviusSuperFlameHoldout : ModProjectile, ILocalizedModType
    {
        private const int ChargeTime = 48;
        private const int AfterfireTime = 24;

        private int timer;
        private bool fired;

        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityLegendsComeBack/Weapons/Vesuvius/Vesuvius2Flame";

        private Player Owner => Main.player[Projectile.owner];
        private int PowerStage => Math.Max(1, (int)Projectile.ai[0]);
        private Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        private float ChargeProgress => MathHelper.Clamp(timer / (float)ChargeTime, 0f, 1f);

        private Vector2 PoseDirection
        {
            get
            {
                if (!fired)
                {
                    // 前半段只是压低杖身，后半段再明显拖到身后，蓄力不会一按下就瞬移到终点。
                    float eased = ChargeProgress * ChargeProgress * (3f - 2f * ChargeProgress);
                    return AimDirection.RotatedBy(-Owner.direction * MathHelper.ToRadians(118f) * eased);
                }

                int afterfireTimer = timer - ChargeTime;
                float swing = Utils.GetLerpValue(0f, 5f, afterfireTimer, true);
                float recover = Utils.GetLerpValue(AfterfireTime, 9f, afterfireTimer, true);
                float angle = MathHelper.Lerp(-118f, 26f, swing) * recover;
                return AimDirection.RotatedBy(Owner.direction * MathHelper.ToRadians(angle));
            }
        }

        private Vector2 LogicalMuzzle => Owner.RotatedRelativePoint(Owner.MountedCenter, true) + AimDirection * 54f;

        public override void SetDefaults()
        {
            Projectile.width = 62;
            Projectile.height = 62;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.ModItem is not NewVesuvius || Owner.CantUseHoldout())
            {
                Projectile.Kill();
                return;
            }

            if (!fired && Main.myPlayer == Projectile.owner)
            {
                if (!Owner.Calamity().mouseRight || Main.mapFullscreen || Main.blockMouse || Owner.mouseInterface)
                {
                    Projectile.Kill();
                    return;
                }

                Vector2 targetDirection = (Owner.Calamity().mouseWorld - Owner.MountedCenter)
                    .SafeNormalize(Vector2.UnitX * Owner.direction);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, targetDirection, 0.2f)
                    .SafeNormalize(Vector2.UnitX * Owner.direction);
                Projectile.netUpdate = true;
            }

            Projectile.timeLeft = 2;
            timer++;
            UpdateHeldPose();

            if (!fired)
            {
                SpawnChargeEffects();
                if (timer >= ChargeTime)
                    FireSuperFlame();
                return;
            }

            SpawnAfterfireEffects();
            if (timer >= ChargeTime + AfterfireTime)
                Projectile.Kill();
        }

        private void UpdateHeldPose()
        {
            Vector2 poseDirection = PoseDirection;
            Projectile.direction = AimDirection.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = Projectile.direction;
            Projectile.rotation = poseDirection.ToRotation();
            Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, true) + poseDirection * 34f;

            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (poseDirection * Projectile.direction).ToRotation();

            // 右键从蓄压开始就是双手持握：前手控制杖口，后手拖住杖柄。
            float armRotation = poseDirection.ToRotation() - MathHelper.PiOver2;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRotation - Owner.direction * 0.16f);
        }

        private void FireSuperFlame()
        {
            fired = true;
            Projectile.netUpdate = true;

            if (Main.myPlayer == Projectile.owner)
            {
                int manaCost = Math.Max(1, Owner.HeldItem.mana * 4);
                if (!Owner.CheckMana(Owner.HeldItem, manaCost, true, false))
                {
                    Projectile.Kill();
                    return;
                }

                float range = 440f + PowerStage * 65f;
                int flameIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    LogicalMuzzle,
                    AimDirection,
                    ModContent.ProjectileType<VesuviusSuperFlame>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    range,
                    PowerStage);

                if (Main.projectile.IndexInRange(flameIndex))
                    Main.projectile[flameIndex].CritChance = Projectile.CritChance;

                Owner.GetModPlayer<VesuviusPassivePlayer>().GrantAfterflameWindow();
                Owner.GetModPlayer<VesuviusEXPlayer>().GainEX(1);
            }

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1.1f, Pitch = -0.42f }, LogicalMuzzle);
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.9f, Pitch = -0.5f }, LogicalMuzzle);
            ApplyScreenShake(13f);
            SpawnMuzzleBurst();
        }

        private void SpawnChargeEffects()
        {
            if (Main.dedServ)
                return;

            Color stageColor = VesuviusProgression.GetStageColor(Math.Min(PowerStage, VesuviusProgression.GetMaxStage()));
            Vector2 muzzle = LogicalMuzzle;
            if (timer % 2 == 0)
            {
                int lane = timer / 2 % 5;
                float phase = timer * 0.13f + lane * MathHelper.TwoPi / 5f;
                Vector2 axis = AimDirection;
                Vector2 normal = axis.RotatedBy(MathHelper.PiOver2);
                float radius = 36f + ChargeProgress * 66f;
                Vector2 offset = axis * (float)Math.Cos(phase) * radius + normal * (float)Math.Sin(phase) * radius * 0.52f;
                Vector2 velocity = -offset * (0.05f + ChargeProgress * 0.035f) + normal * (float)Math.Cos(phase) * 0.7f;
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    muzzle + offset,
                    velocity,
                    false,
                    Main.rand.Next(11, 18),
                    Main.rand.NextFloat(0.34f, 0.66f),
                    Color.Lerp(VesuviusProjectileVisuals.LavaOrange, VesuviusProjectileVisuals.HotWhite, ChargeProgress * 0.72f),
                    true));
            }

            if (timer % 5 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new SquareAshParticle(
                    muzzle + Main.rand.NextVector2Circular(54f + ChargeProgress * 28f, 34f + ChargeProgress * 18f),
                    Main.rand.NextVector2Circular(0.8f, 0.8f) - Vector2.UnitY * 0.45f,
                    Main.rand.Next(22, 34),
                    Main.rand.NextFloat(0.45f, 0.9f),
                    Color.Lerp(VesuviusProjectileVisuals.AshGray, stageColor, 0.16f)));
            }

            Lighting.AddLight(muzzle, stageColor.ToVector3() * (0.35f + ChargeProgress * 0.75f));
            if (ChargeProgress > 0.75f)
                ApplyScreenShake((ChargeProgress - 0.75f) * 7f);
        }

        private void SpawnMuzzleBurst()
        {
            if (Main.dedServ)
                return;

            Color stageColor = VesuviusProgression.GetStageColor(Math.Min(PowerStage, VesuviusProgression.GetMaxStage()));
            GeneralParticleHandler.SpawnParticle(new StrongBloom(LogicalMuzzle, Vector2.Zero, VesuviusProjectileVisuals.HotWhite, 1.25f, 18));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(LogicalMuzzle, AimDirection * 0.5f, stageColor,
                new Vector2(1f, 0.48f), AimDirection.ToRotation(), 0.14f, 2.4f, 20));

            for (int i = 0; i < 26; i++)
            {
                Vector2 velocity = AimDirection.RotatedByRandom(0.42f) * Main.rand.NextFloat(7f, 19f);
                Dust ember = Dust.NewDustPerfect(LogicalMuzzle, i % 4 == 0 ? DustID.Obsidian : DustID.InfernoFork,
                    velocity, 55, Color.Lerp(stageColor, Color.White, Main.rand.NextFloat(0.05f, 0.4f)), Main.rand.NextFloat(0.9f, 1.75f));
                ember.noGravity = i % 3 != 0;
            }
        }

        private void SpawnAfterfireEffects()
        {
            if (Main.dedServ || timer % 3 != 0)
                return;

            Vector2 side = PoseDirection.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloatDirection();
            GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                Projectile.Center + side * Main.rand.NextFloat(10f, 28f),
                side * Main.rand.NextFloat(1f, 3f) - Vector2.UnitY * Main.rand.NextFloat(0.4f, 1.6f),
                VesuviusProjectileVisuals.ScoriaSmoke,
                Main.rand.Next(24, 38),
                Main.rand.NextFloat(0.45f, 0.8f),
                0.66f,
                Main.rand.NextFloat(-0.05f, 0.05f),
                false));
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1800f, 220f, Vector2.Distance(Main.LocalPlayer.Center, Owner.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                Main.LocalPlayer.Calamity().GeneralScreenShakePower,
                power * distanceFactor);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/Vesuvius/NewVesuviusGlow").Value;
            int mouthFrame = fired || ChargeProgress >= 1f ? 1 : 0;
            Rectangle frame = texture.Frame(1, 2, 0, mouthFrame);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            SpriteEffects effects = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float rotation = Projectile.rotation + (Projectile.spriteDirection < 0 ? MathHelper.Pi : 0f) + MathHelper.ToRadians(45f * Projectile.spriteDirection);

            Main.EntitySpriteDraw(texture, drawPosition, frame, lightColor, rotation, frame.Size() * 0.5f, Projectile.scale, effects);
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(glow, drawPosition, null, Color.White * (0.45f + ChargeProgress * 0.4f), rotation,
                glow.Size() * 0.5f, Projectile.scale, effects);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }

    public sealed class VesuviusSuperFlame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private float Range => Math.Max(360f, Projectile.ai[0]);
        private int PowerStage => Math.Max(1, (int)Projectile.ai[1]);
        private Vector2 Direction => Projectile.velocity.SafeNormalize(Vector2.UnitX);
        private float effectiveRange;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 27;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => Projectile.localAI[0] >= 3f && Projectile.localAI[0] <= 17f;

        public override void AI()
        {
            if (effectiveRange <= 0f)
            {
                float[] samples = new float[5];
                Collision.LaserScan(Projectile.Center, Direction, 28f, Range, samples);
                foreach (float sample in samples)
                    effectiveRange += sample / samples.Length;
                effectiveRange = MathHelper.Clamp(effectiveRange, 48f, Range);
            }

            Projectile.localAI[0]++;
            SpawnFlameCone();
            Lighting.AddLight(Projectile.Center + Direction * Math.Min(Range * 0.42f, 320f), 1.25f, 0.48f, 0.08f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            float width = 52f + PowerStage * 5f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + Direction * effectiveRange,
                width,
                ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            VesuviusCombatSystem.ApplyVolcanicCalamity(target);
        }

        private void SpawnFlameCone()
        {
            if (Main.dedServ)
                return;

            float age = Projectile.localAI[0];
            float extend = 1f - (float)Math.Pow(1f - Utils.GetLerpValue(0f, 10f, age, true), 3f);
            float fade = Utils.GetLerpValue(27f, 17f, age, true);
            float activeRange = effectiveRange * extend;
            Vector2 normal = Direction.RotatedBy(MathHelper.PiOver2);

            // 火焰主体不是一条发光直线，而是“白热内芯—金橙火舌—黑灰外壳”三层喷流。
            // 每层沿距离逐渐放宽，视线仍能从稀疏的烟灰间看见真正的判定方向。
            int streamCount = PowerStage >= 4 ? 7 : 5;
            for (int i = 0; i < streamCount; i++)
            {
                float distance = Main.rand.NextFloat(18f, Math.Max(22f, activeRange));
                float along = distance / Math.Max(1f, effectiveRange);
                float halfWidth = 9f + along * (48f + PowerStage * 4f);
                Vector2 position = Projectile.Center + Direction * distance + normal * Main.rand.NextFloat(-halfWidth, halfWidth);
                Vector2 velocity = Direction * Main.rand.NextFloat(4f, 10f) + normal * Main.rand.NextFloat(-1.7f, 1.7f);
                Color flameColor = i == 0
                    ? VesuviusProjectileVisuals.HotWhite
                    : Color.Lerp(VesuviusProjectileVisuals.LavaGold, VesuviusProjectileVisuals.LavaOrange, along * 0.8f);

                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    position,
                    velocity,
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.5f, 1.05f) * fade,
                    flameColor,
                    true));

                if (i >= streamCount - 2 && Main.rand.NextBool(2))
                {
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                        position,
                        velocity * 0.22f - Vector2.UnitY * Main.rand.NextFloat(0.4f, 1.8f),
                        Color.Lerp(VesuviusProjectileVisuals.ScoriaSmoke, VesuviusProjectileVisuals.AshGray, 0.35f),
                        Main.rand.Next(24, 42),
                        Main.rand.NextFloat(0.48f, 0.95f) * fade,
                        0.72f,
                        Main.rand.NextFloat(-0.06f, 0.06f),
                        false));
                }
            }

            if ((int)age % 4 == 0)
            {
                float ringDistance = Math.Min(activeRange, effectiveRange * Main.rand.NextFloat(0.28f, 0.82f));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center + Direction * ringDistance,
                    Direction * 0.5f,
                    Color.Lerp(VesuviusProjectileVisuals.LavaOrange, VesuviusProjectileVisuals.LavaGold, 0.4f) * fade,
                    new Vector2(1f, 0.46f),
                    Direction.ToRotation(),
                    0.18f,
                    1.1f + ringDistance / Math.Max(1f, effectiveRange) * 1.8f,
                    15));
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    public sealed class VesuviusFollowupMeteor : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/Magic/AsteroidMolten3";

        private Vector2 Target => new(Projectile.ai[0], Projectile.ai[1]);
        private int PowerStage => Math.Max(1, (int)Projectile.ai[2]);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 54;
            Projectile.height = 54;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 100;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.scale = 1.15f;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Vector2 desiredDirection = (Target - Projectile.Center).SafeNormalize(Vector2.UnitY);
            float speed = MathHelper.Clamp(19f + PowerStage * 0.8f + Projectile.localAI[0] * 0.05f, 19f, 28f);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredDirection * speed, 0.075f);
            Projectile.rotation += 0.13f + Projectile.velocity.X * 0.006f;

            if (Vector2.Distance(Projectile.Center, Target) < 110f)
                Projectile.tileCollide = true;

            VesuviusProjectileVisuals.SpawnMoltenMeteorTrail(Projectile, 1.15f + PowerStage * 0.06f, true);
            Lighting.AddLight(Projectile.Center, 1.05f, 0.35f, 0.06f);

            if (Projectile.localAI[0] > 18f && Vector2.Dot(Target - Projectile.Center, Projectile.velocity) < 0f)
                Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1.05f, Pitch = -0.38f }, Projectile.Center);
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<VesuviusFollowupMeteorBlast>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    PowerStage);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMoltenGlow3").Value;
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type],
                VesuviusProjectileVisuals.LavaOrange * 0.72f, 1);
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, Color.White,
                Projectile.rotation, glow.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }

    public sealed class VesuviusFollowupMeteorBlast : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int PowerStage => Math.Max(1, (int)Projectile.ai[0]);
        private float Radius => 104f + PowerStage * 13f;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => Projectile.localAI[0] <= 1f;

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.Resize((int)(Radius * 2f), (int)(Radius * 2f));
                SpawnDetonation();
                ApplyScreenShake();
            }

            Projectile.localAI[0]++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.Distance(closest, Projectile.Center) <= Radius;
        }

        private void SpawnDetonation()
        {
            if (Main.dedServ)
                return;

            VesuviusProjectileVisuals.SpawnMoltenImpact(Projectile.Center, 1.45f + PowerStage * 0.08f, true);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                VesuviusProjectileVisuals.LavaGold,
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.2f,
                3.4f + PowerStage * 0.25f,
                24));

            // 爆心喷出厚重熔岩块，外圈只留少量灰烟；陨石因此更像真实落体砸开的火山口，
            // 而不是把左键火球的圆形光晕放大后再次使用。
            for (int i = 0; i < 24 + PowerStage * 3; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 0.72f) * Main.rand.NextFloat(4f, 13f);
                Dust debris = Dust.NewDustPerfect(
                    Projectile.Center,
                    i % 4 == 0 ? DustID.Obsidian : DustID.InfernoFork,
                    velocity - Vector2.UnitY * Main.rand.NextFloat(0f, 3f),
                    65,
                    Color.Lerp(VesuviusProjectileVisuals.LavaOrange, Color.White, Main.rand.NextFloat(0.04f, 0.3f)),
                    Main.rand.NextFloat(1f, 2f));
                debris.noGravity = i % 3 == 0;
            }
        }

        private void ApplyScreenShake()
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1900f, 220f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                Main.LocalPlayer.Calamity().GeneralScreenShakePower,
                (10f + PowerStage) * distanceFactor);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
