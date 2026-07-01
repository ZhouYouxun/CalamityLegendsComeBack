using System;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.LeftGeneral
{
    internal sealed class YC_BurningShard : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 120;
        private const int MaxFollowFireballs = 5;
        private const float FollowOrbitRadius = 148f;
        private static readonly Color CoreGold = new(255, 215, 86);
        private static readonly Color CoreRed = new(255, 70, 34);

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/Boss/HolyBomb";

        private ref float Timer => ref Projectile.localAI[0];
        private bool AttachedToHoldout => Projectile.ai[0] == 1f;
        private int HoldoutIndex => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 62;
            Projectile.height = 62;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.scale = 0.82f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Crystal);
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.58f, Pitch = -0.22f }, Projectile.Center);
        }

        public override bool? CanDamage() => Timer % 24f < 9f ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, 96f * Projectile.scale, targetHitbox);

        internal static bool CanSpawnFollowFireballFor(Player player, int queued = 0)
        {
            return CountFollowFireballs(player, queued) < MaxFollowFireballs;
        }

        internal static int NextFollowOrbitSlotFor(Player player)
        {
            int shardType = ModContent.ProjectileType<YC_BurningShard>();
            bool[] usedSlots = new bool[MaxFollowFireballs];

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active &&
                    projectile.owner == player.whoAmI &&
                    projectile.type == shardType &&
                    projectile.ai[0] == 2f)
                {
                    usedSlots[PositiveModulo((int)projectile.ai[1], MaxFollowFireballs)] = true;
                }
            }

            for (int i = 0; i < MaxFollowFireballs; i++)
            {
                if (!usedSlots[i])
                    return i;
            }

            return 0;
        }

        public override void AI()
        {
            Timer++;

            if (AttachedToHoldout)
            {
                if (!TryFollowHoldout())
                {
                    Projectile.Kill();
                    return;
                }

                Projectile.timeLeft = 2;
            }
            else if (Projectile.ai[0] == 2f)
            {
                OrbitOwner();
            }
            else
                Projectile.velocity *= 0.88f;

            Projectile.rotation = 0f;
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 3)
                Projectile.frame = 0;

            Projectile.scale = MathHelper.Lerp(Projectile.scale, AttachedToHoldout ? 0.7f : 0.95f, 0.04f);
            Lighting.AddLight(Projectile.Center, Color.Lerp(CoreRed, CoreGold, 0.45f).ToVector3() * 0.85f);

            if (Projectile.owner == Main.myPlayer && (int)Timer % 16 == 0)
                FireSplinter();

            EmitIdleFlames();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(new BalanceYharimsCrystal().GetFireDebuffType(), 300);
        }

        public override void OnKill(int timeLeft)
        {
            ExplodeDamage();
            SpawnDeathEffects();

            if (Projectile.owner == Main.myPlayer)
            {
                int count = 6;
                for (int i = 0; i < count; i++)
                {
                    float angle = MathHelper.TwoPi * i / count;
                    Vector2 velocity = angle.ToRotationVector2() * 8f;
                    int splinter = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        velocity,
                        ModContent.ProjectileType<YC_BurningShardSplinter>(),
                        Math.Max(1, (int)(Projectile.damage * 0.5f)),
                        Projectile.knockBack * 0.3f,
                        Projectile.owner);
                    if (Main.projectile.IndexInRange(splinter))
                    {
                        YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[splinter], YCWeaponForm.Crystal);
                        Main.projectile[splinter].CritChance = Projectile.CritChance;
                    }
                }
            }

            BalanceYharimsCrystal growth = new();
            if (Projectile.owner == Main.myPlayer && growth.ShouldShardReleaseBrimstoneMissiles())
                ReleaseBrimstoneMissiles();

            if (Projectile.owner == Main.myPlayer && growth.ShouldShardReleaseRainbowBolts())
                ReleaseRainbowBolts();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color main = Color.Lerp(CoreRed, CoreGold, 0.52f) with { A = 0 };
            float pulse = 1f + (float)Math.Sin(Timer * 0.18f) * 0.08f;

            int frameHeight = texture.Height / Main.projFrames[Type];
            Rectangle frame = new(0, frameHeight * Projectile.frame, texture.Width, frameHeight);
            Vector2 origin = new(texture.Width * 0.5f, frameHeight * 0.5f + 22f);

            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f + Timer * 0.035f).ToRotationVector2() * 4f;
                Main.EntitySpriteDraw(bloom, drawPosition + offset, null, main * 0.28f, -Projectile.rotation, bloom.Size() * 0.5f, Projectile.scale * 0.36f * pulse, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, drawPosition, frame, Color.Lerp(lightColor, Color.White, 0.35f), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPosition, null, Color.White with { A = 0 } * 0.22f, Projectile.rotation, bloom.Size() * 0.5f, Projectile.scale * 0.18f, SpriteEffects.None);
            return false;
        }

        private bool TryFollowHoldout()
        {
            if (HoldoutIndex < 0 || HoldoutIndex >= Main.maxProjectiles)
                return false;

            Projectile holdout = Main.projectile[HoldoutIndex];
            if (!holdout.active || holdout.owner != Projectile.owner)
                return false;

            Vector2 direction = holdout.velocity.SafeNormalize(Vector2.UnitX * Main.player[Projectile.owner].direction);
            if (direction == Vector2.Zero)
                direction = holdout.rotation.ToRotationVector2();

            Projectile.Center = holdout.Center + direction * 34f + direction.RotatedBy(MathHelper.PiOver2) * (float)Math.Sin(Timer * 0.08f) * 10f;
            return true;
        }

        private void OrbitOwner()
        {
            Player player = Main.player[Projectile.owner];
            int slot = PositiveModulo((int)Projectile.ai[1], MaxFollowFireballs);
            float spinDirection = player.direction >= 0 ? 1f : -1f;
            float angle = MathHelper.TwoPi * slot / MaxFollowFireballs + Timer * 0.045f * spinDirection;
            Vector2 orbitDirection = angle.ToRotationVector2();
            Vector2 tangent = orbitDirection.RotatedBy(MathHelper.PiOver2 * spinDirection);
            Vector2 desiredCenter = player.MountedCenter + orbitDirection * FollowOrbitRadius;
            Vector2 toDesired = desiredCenter - Projectile.Center;

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, toDesired * 0.12f + tangent * 2.8f, 0.2f);

            if (toDesired.Length() > 520f)
                Projectile.Center = Vector2.Lerp(Projectile.Center, desiredCenter, 0.35f);
        }

        private static int CountFollowFireballs(Player player, int queued = 0)
        {
            int shardType = ModContent.ProjectileType<YC_BurningShard>();
            int activeCount = queued;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active &&
                    projectile.owner == player.whoAmI &&
                    projectile.type == shardType &&
                    projectile.ai[0] == 2f)
                {
                    activeCount++;
                }
            }

            return activeCount;
        }

        private static int PositiveModulo(int value, int divisor)
        {
            int result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        private void FireSplinter()
        {
            NPC target = Projectile.Center.ClosestNPCAt(880f);
            Vector2 direction = target is null
                ? Main.rand.NextVector2CircularEdge(1f, 1f)
                : (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);

            int splinter = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + direction * 18f,
                direction.RotatedByRandom(0.18f) * Main.rand.NextFloat(7f, 10.5f),
                ModContent.ProjectileType<YC_BurningShardSplinter>(),
                Math.Max(1, (int)(Projectile.damage * 0.42f)),
                Projectile.knockBack * 0.3f,
                Projectile.owner);

            if (Main.projectile.IndexInRange(splinter))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[splinter], YCWeaponForm.Crystal);
                Main.projectile[splinter].CritChance = Projectile.CritChance;
            }
        }

        private void ExplodeDamage()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            Vector2 center = Projectile.Center;
            int oldWidth = Projectile.width;
            int oldHeight = Projectile.height;
            Projectile.Resize(190, 190);
            Projectile.Center = center;
            Projectile.Damage();
            Projectile.Resize(oldWidth, oldHeight);
            Projectile.Center = center;
        }

        private void ReleaseBrimstoneMissiles()
        {
            int count = Main.rand.Next(7, 14);
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(6.4f, 10.6f);
                int missile = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + Main.rand.NextVector2Circular(18f, 18f),
                    velocity,
                    ModContent.ProjectileType<YC_BrimstoneWheelMissile>(),
                    Math.Max(1, (int)(Projectile.damage * 0.44f)),
                    Projectile.knockBack * 0.2f,
                    Projectile.owner);

                if (Main.projectile.IndexInRange(missile))
                {
                    YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[missile], YCWeaponForm.Crystal);
                    Main.projectile[missile].CritChance = Projectile.CritChance;
                }
            }
        }

        private void ReleaseRainbowBolts()
        {
            int count = 7;
            for (int i = 0; i < count; i++)
            {
                float angle = -MathHelper.PiOver2 + MathHelper.Lerp(-0.72f, 0.72f, i / (float)(count - 1));
                Vector2 velocity = angle.ToRotationVector2() * 10.8f;
                int bolt = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    velocity,
                    ModContent.ProjectileType<YC_RainbowUltimaBolt>(),
                    Math.Max(1, (int)(Projectile.damage * 0.52f)),
                    Projectile.knockBack * 0.35f,
                    Projectile.owner);

                if (Main.projectile.IndexInRange(bolt))
                {
                    YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[bolt], YCWeaponForm.Crystal);
                    Main.projectile[bolt].CritChance = Projectile.CritChance;
                }
            }
        }

        private void EmitIdleFlames()
        {
            if (Main.dedServ)
                return;

            if (Main.rand.NextBool(2))
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 3.6f);
                Particle flame = new CustomSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(24f, 24f),
                    velocity,
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.18f, 0.36f),
                    Main.rand.NextBool(4) ? Color.White : Color.Lerp(CoreRed, CoreGold, Main.rand.NextFloat()),
                    new Vector2(0.58f, 1.05f),
                    shrinkSpeed: 0.72f);
                GeneralParticleHandler.SpawnParticle(flame);
            }
        }

        private void SpawnDeathEffects()
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DeadSunExplosion") { Volume = 0.46f, Pitch = -0.1f }, Projectile.Center);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, CoreGold * 0.8f, "CalamityMod/Particles/FlameExplosion", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.08f, 0.95f, 22, true));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, Color.Lerp(CoreGold, Color.White, 0.24f), Vector2.One, Projectile.rotation, 0.16f, 2.1f, 22));

            for (int i = 0; i < 46; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 9.5f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(4) ? DustID.YellowTorch : DustID.GoldFlame, velocity, 0, Main.rand.NextBool(4) ? Color.White : CoreGold, Main.rand.NextFloat(0.8f, 1.55f));
                dust.noGravity = true;
            }
        }
    }

    internal sealed class YC_BurningShardSplinter : ModProjectile, ILocalizedModType
    {
        private static readonly Color TrailGold = new(255, 218, 92);
        private static readonly Color TrailRed = new(255, 72, 38);
        private const float HomingRange = 1350f;
        private const float MaxSpeed = 18.5f;
        private const int HomingDelay = 8;
        private const float FreeFlightDamping = 0.998f;
        private const float NoTargetDamping = 0.994f;

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 22;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 105;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Crystal);
        }

        public override void AI()
        {
            Timer++;
            HomeTowardTarget();
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, Color.Lerp(TrailRed, TrailGold, 0.45f).ToVector3() * 0.38f);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.6f, DustID.GoldFlame, -Projectile.velocity * Main.rand.NextFloat(0.08f, 0.2f), 0, Main.rand.NextBool() ? TrailGold : TrailRed, Main.rand.NextFloat(0.65f, 1f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(new BalanceYharimsCrystal().GetFireDebuffType(), 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            PrimitiveRenderer.RenderTrail(
                Projectile.oldPos,
                new PrimitiveSettings(PrimitiveWidthFunction, PrimitiveColorFunction, PrimitiveOffsetFunction, shader: GameShaders.Misc["CalamityMod:TrailStreak"]),
                36);

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, Color.White with { A = 0 } * 0.65f, Projectile.rotation, bloom.Size() * 0.5f, 0.055f, SpriteEffects.None);
            return false;
        }

        private void HomeTowardTarget()
        {
            if (Timer <= HomingDelay)
            {
                FreeDrift(FreeFlightDamping);
                return;
            }

            NPC target = Projectile.Center.ClosestNPCAt(HomingRange);
            if (target is null)
            {
                FreeDrift(NoTargetDamping);
                return;
            }

            Vector2 currentVelocity = Projectile.velocity;
            float currentSpeed = currentVelocity.Length();
            if (currentSpeed < 0.1f)
                currentVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 5f;

            Vector2 currentDirection = currentVelocity.SafeNormalize(Vector2.UnitX);
            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(currentDirection);
            float warmup = Utils.GetLerpValue(HomingDelay, HomingDelay + 28f, Timer, true);
            float closePressure = Utils.GetLerpValue(360f, 70f, Projectile.Distance(target.Center), true);
            float pull = MathHelper.Max(warmup, closePressure * 0.85f);
            float inertia = MathHelper.Lerp(18f, 7.5f, pull);
            float targetSpeed = MathHelper.Lerp(11f, MaxSpeed, pull);

            Projectile.velocity = (currentVelocity * inertia + desiredDirection * targetSpeed) / (inertia + 1f);
            float sideSway = (float)Math.Sin((Timer + Projectile.identity * 5f) * 0.1f) * MathHelper.Lerp(0.012f, 0.003f, pull);
            Projectile.velocity = Projectile.velocity.RotatedBy(sideSway);

            if (Projectile.velocity.Length() > MaxSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(desiredDirection) * MaxSpeed;
        }

        private void FreeDrift(float damping)
        {
            float wander = (float)Math.Sin((Timer + Projectile.identity * 7f) * 0.09f) * 0.005f;
            Projectile.velocity = Projectile.velocity.RotatedBy(wander) * damping;
        }

        private float PrimitiveWidthFunction(float completionRatio, Vector2 vertexPos)
        {
            float head = Utils.GetLerpValue(0f, 0.26f, completionRatio, true);
            return MathHelper.Lerp(3f, 18f, head) * Utils.GetLerpValue(1f, 0.55f, completionRatio, true);
        }

        private Color PrimitiveColorFunction(float completionRatio, Vector2 vertexPos)
        {
            float wave = (float)Math.Cos(completionRatio * 2.7f - Main.GlobalTimeWrappedHourly * 5.3f) * 0.5f + 0.5f;
            Color start = Color.Lerp(TrailGold, Color.White, wave * 0.55f);
            Color end = Color.Lerp(TrailRed, TrailGold, 0.35f);
            return Color.Lerp(start, end, Utils.GetLerpValue(0f, 0.72f, completionRatio, true)) * Projectile.Opacity;
        }

        private Vector2 PrimitiveOffsetFunction(float completionRatio, Vector2 vertexPosition) => Projectile.Size * 0.5f + Projectile.velocity * 1.25f;
    }

    internal sealed class YC_BrimstoneWheelMissile : ModProjectile, ILocalizedModType
    {
        private static readonly Color BrimColor = new(222, 42, 38);

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/Boss/BrimstoneBarrage";

        private ref float Timer => ref Projectile.localAI[0];
        private bool exploding;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 44;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Crystal);
        }

        public override void AI()
        {
            Timer++;
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, BrimColor.ToVector3() * 0.42f);
            EmitBrimstoneDust(direction);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<CalamityMod.Buffs.DamageOverTime.BrimstoneFlames>(), 300);
            if (!exploding)
                Projectile.Kill();
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => true;

        public override void OnKill(int timeLeft)
        {
            if (timeLeft <= 0)
                return;

            Vector2 center = Projectile.Center;
            exploding = true;
            if (Projectile.owner == Main.myPlayer)
            {
                int oldWidth = Projectile.width;
                int oldHeight = Projectile.height;
                Projectile.Resize(86, 86);
                Projectile.Center = center;
                Projectile.Damage();
                Projectile.Resize(oldWidth, oldHeight);
                Projectile.Center = center;
            }

            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.34f, Pitch = 0.1f }, center);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, BrimColor * 0.65f, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.08f, 0.58f, 14, true));

            for (int i = 0; i < 16; i++)
            {
                Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(8f, 8f), ModContent.DustType<SquashDust>(), Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.4f, 7.6f), 0, BrimColor * Main.rand.NextFloat(0.45f, 0.85f), Main.rand.NextFloat(0.9f, 1.35f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], BrimColor * 0.36f, 1);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.Lerp(BrimColor, Color.White, 0.18f), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            return false;
        }

        private void EmitBrimstoneDust(Vector2 direction)
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
                return;

            Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), ModContent.DustType<SquashDust>(), -direction * Main.rand.NextFloat(2.2f, 4.8f), 0, BrimColor * 0.6f, Main.rand.NextFloat(1f, 1.3f));
            dust.noGravity = true;
        }
    }

    internal sealed class YC_RainbowUltimaBolt : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityLegendsComeBack/Texture/Calamity/RangePROJ/UltimaBolt";

        private ref float Timer => ref Projectile.localAI[0];
        private const int LeftTurnFrames = 42;
        private const int RightTurnFrames = 42;
        private const float HomingRange = 1650f;
        private const float MaxSpeed = 21f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 210;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Crystal);
        }

        public override void AI()
        {
            Timer++;
            RunCurvedHoming();
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Lighting.AddLight(Projectile.Center, Main.DiscoColor.ToVector3() * 0.44f);
            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(9f, 9f),
                    -Projectile.velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.15f, 0.45f),
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.6f, 1.05f),
                    Main.DiscoColor));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(new BalanceYharimsCrystal().GetFireDebuffType(), 240);
        }

        private void RunCurvedHoming()
        {
            if (Timer <= LeftTurnFrames)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(-0.035f);
                return;
            }

            if (Timer <= LeftTurnFrames + RightTurnFrames)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(0.041f);
                return;
            }

            NPC target = Projectile.Center.ClosestNPCAt(HomingRange);
            if (target == null)
            {
                Projectile.velocity *= 0.998f;
                return;
            }

            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(currentDirection);
            float warmup = Utils.GetLerpValue(LeftTurnFrames + RightTurnFrames, LeftTurnFrames + RightTurnFrames + 36f, Timer, true);
            float closePressure = Utils.GetLerpValue(420f, 80f, Projectile.Distance(target.Center), true);
            float turn = MathHelper.Lerp(MathHelper.ToRadians(10f), MathHelper.ToRadians(24f), MathHelper.Max(warmup, closePressure));
            float speed = MathHelper.Clamp(Projectile.velocity.Length() + 0.16f, 10f, MaxSpeed);

            Projectile.velocity = currentDirection.ToRotation().AngleTowards(desired.ToRotation(), turn).ToRotationVector2() * speed;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = texture.Size() * 0.5f;

            Color main = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.9f + Projectile.identity * 0.01f) % 1f, 1f, 0.64f);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, main with { A = 0 } * 0.58f, Projectile.rotation, bloom.Size() * 0.5f, Projectile.scale * 0.28f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.Lerp(main, Color.White, 0.35f), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
