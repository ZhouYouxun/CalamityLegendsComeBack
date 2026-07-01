using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Healing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick
{
    internal enum DEBurstStyle
    {
        Gold,
        Fire,
        Slag,
        Fungal,
        Needle,
        Pearl,
        Hellborn,
        Plague,
        Astral
    }

    internal static class DEBulletUtils
    {
        private const string SparkTexturePath = "CalamityMod/Particles/ThinEndedLine";

        public static void OrientToVelocity(Projectile projectile, float extraRotation = MathHelper.PiOver2)
        {
            projectile.spriteDirection = projectile.direction = (projectile.velocity.X > 0f).ToDirectionInt();
            projectile.rotation = projectile.velocity.ToRotation() + extraRotation;
        }

        public static void TrailDust(Projectile projectile, int dustType, Color color, float scale = 1f, float backSpeed = 0.35f)
        {
            if (!Main.rand.NextBool(2))
                return;

            Vector2 backward = -projectile.velocity.SafeNormalize(Vector2.UnitX) * projectile.velocity.Length() * backSpeed;
            Dust dust = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(2f, 2f), dustType, backward.RotatedByRandom(0.18f), 120, color, scale);
            dust.noGravity = true;
        }

        public static void GlowTrail(Projectile projectile, Color color, float scale = 1f)
        {
            if (Main.dedServ || Main.rand.NextBool(3))
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                projectile.Center - forward * 3f,
                -forward * Main.rand.NextFloat(0.5f, 1.6f),
                false,
                Main.rand.Next(5, 8),
                Main.rand.NextFloat(0.018f, 0.03f) * scale,
                color,
                new Vector2(0.55f, 2.2f),
                true));
        }

        public static void BurstDust(Vector2 position, Color color, int dustType, int count, float speed, float scale)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(speed, speed) * Main.rand.NextFloat(0.55f, 1.1f);
                Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(8f, 8f), dustType, velocity, 110, color, scale * Main.rand.NextFloat(0.75f, 1.25f));
                dust.noGravity = true;
            }
        }

        public static void ParticleBurst(Vector2 position, Color color, float scale = 1f)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new StrongBloom(position, Vector2.Zero, color, 0.75f * scale, 16));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                position,
                Vector2.Zero,
                color * 0.75f,
                "CalamityMod/Particles/HighResHollowCircleHardEdge",
                Vector2.One,
                0f,
                0.01f,
                0.07f * scale,
                18,
                true,
                0.82f));

            for (int i = 0; i < 10; i++)
            {
                Vector2 sparkDirection = (MathHelper.TwoPi * i / 10f).ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    position + sparkDirection * 7f * scale,
                    sparkDirection * Main.rand.NextFloat(4f, 8f) * scale,
                    SparkTexturePath,
                    false,
                    Main.rand.Next(10, 15),
                    Main.rand.NextFloat(0.025f, 0.045f) * scale,
                    color,
                    new Vector2(0.7f, 1.9f),
                    shrinkSpeed: 0.78f));
            }
        }

        public static void SpawnAreaBurst(IEntitySource source, Vector2 position, int damage, float knockback, int owner, DEBurstStyle style, float radius)
        {
            Projectile.NewProjectile(source, position, Vector2.Zero, ModContent.ProjectileType<SubProjectiles.DEBullet_AreaBurst>(), damage, knockback, owner, (float)style, radius);
        }

        public static NPC FindTarget(Vector2 position, float range, Projectile projectile, NPC excluded = null, bool requireLineOfSight = true)
        {
            NPC best = null;
            float bestDistance = range;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc == excluded || !npc.CanBeChasedBy(projectile))
                    continue;

                float distance = Vector2.Distance(position, npc.Center);
                if (distance >= bestDistance)
                    continue;

                if (requireLineOfSight && !Collision.CanHitLine(position, 4, 4, npc.Center, 4, 4))
                    continue;

                best = npc;
                bestDistance = distance;
            }

            return best;
        }

        public static void SimpleHoming(Projectile projectile, float range, float turnStrength, float maxSpeed)
        {
            NPC target = FindTarget(projectile.Center, range, projectile, null, false);
            if (target == null)
                return;

            Vector2 desiredVelocity = (target.Center - projectile.Center).SafeNormalize(projectile.velocity.SafeNormalize(Vector2.UnitX)) * maxSpeed;
            projectile.velocity = Vector2.Lerp(projectile.velocity, desiredVelocity, turnStrength);
        }

        public static void SpawnLifeSteal(Player owner, NPC target, Projectile projectile, int heal, float cooldownMultiplier = 0.8f)
        {
            owner.SpawnLifeStealProjectile(target, projectile, ModContent.ProjectileType<TransfusionTrail>(), Math.Max(1, heal), cooldownMultiplier);
        }

        public static Color BurstColor(DEBurstStyle style) => style switch
        {
            DEBurstStyle.Gold => new Color(255, 210, 75),
            DEBurstStyle.Fire => new Color(255, 104, 36),
            DEBurstStyle.Slag => new Color(224, 139, 70),
            DEBurstStyle.Fungal => new Color(72, 210, 255),
            DEBurstStyle.Needle => new Color(96, 255, 96),
            DEBurstStyle.Pearl => new Color(255, 223, 235),
            DEBurstStyle.Hellborn => new Color(255, 58, 24),
            DEBurstStyle.Plague => new Color(117, 255, 69),
            DEBurstStyle.Astral => new Color(126, 166, 255),
            _ => Color.White
        };

        public static int BurstDustType(DEBurstStyle style) => style switch
        {
            DEBurstStyle.Gold => DustID.GoldCoin,
            DEBurstStyle.Fire => DustID.Torch,
            DEBurstStyle.Slag => DustID.Sand,
            DEBurstStyle.Fungal => DustID.BlueTorch,
            DEBurstStyle.Needle => DustID.Poisoned,
            DEBurstStyle.Pearl => DustID.GemDiamond,
            DEBurstStyle.Hellborn => DustID.FireworksRGB,
            DEBurstStyle.Plague => DustID.GreenTorch,
            DEBurstStyle.Astral => DustID.Firework_Blue,
            _ => DustID.TintableDust
        };
    }

    public sealed class DesertEagleFungicideGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public int FungicideStacks;
        public int FungicideTimer;

        public override void ResetEffects(NPC npc)
        {
            if (FungicideTimer > 0)
            {
                FungicideTimer--;
                return;
            }

            FungicideStacks = 0;
        }

        public void AddFungicideStack(NPC npc, Projectile source, int hitDamage)
        {
            FungicideStacks = Math.Min(4, FungicideStacks + 1);
            FungicideTimer = 360;

            float ringScale = 0.65f + FungicideStacks * 0.16f;
            DEBulletUtils.BurstDust(npc.Center, Color.Lerp(Color.DeepSkyBlue, Color.White, FungicideStacks / 4f), DustID.BlueTorch, 8 + FungicideStacks * 2, 2.4f + FungicideStacks, ringScale);

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                npc.Center,
                Vector2.Zero,
                Color.DeepSkyBlue * 0.7f,
                "CalamityMod/Particles/HighResHollowCircleHardEdge",
                Vector2.One,
                0f,
                0.01f,
                0.035f + FungicideStacks * 0.012f,
                15,
                true,
                0.7f));

            if (FungicideStacks < 4)
                return;

            FungicideStacks = 0;
            FungicideTimer = 0;
            if (Main.myPlayer == source.owner)
            {
                DEBulletUtils.SpawnAreaBurst(
                    source.GetSource_FromAI(),
                    npc.Center,
                    Math.Max(1, (int)(hitDamage * 0.72f)),
                    source.knockBack,
                    source.owner,
                    DEBurstStyle.Fungal,
                    92f);
            }

            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.72f, Pitch = 0.18f }, npc.Center);
        }
    }
}

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles
{
    public class DEBullet_AreaBurst : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Particles/BloomCircle";

        private DEBurstStyle Style => (DEBurstStyle)(int)Projectile.ai[0];
        private float Radius => Math.Max(24f, Projectile.ai[1]);

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 6;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] != 0f)
                return;

            Projectile.localAI[0] = 1f;
            int size = (int)(Radius * 2f);
            Projectile.Resize(size, size);

            Color color = DEBulletUtils.BurstColor(Style);
            int dustType = DEBulletUtils.BurstDustType(Style);
            DEBulletUtils.BurstDust(Projectile.Center, color, dustType, 30, Radius * 0.075f, MathHelper.Clamp(Radius / 82f, 0.8f, 1.8f));
            DEBulletUtils.ParticleBurst(Projectile.Center, color, MathHelper.Clamp(Radius / 78f, 0.85f, 2.2f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            switch (Style)
            {
                case DEBurstStyle.Gold:
                    target.AddBuff(BuffID.Midas, 240);
                    break;

                case DEBurstStyle.Fire:
                    target.AddBuff(BuffID.OnFire3, 240);
                    break;

                case DEBurstStyle.Slag:
                    target.AddBuff(ModContent.BuffType<ArmorCrunch>(), 180);
                    break;

                case DEBurstStyle.Fungal:
                    target.AddBuff(BuffID.Poisoned, 240);
                    break;

                case DEBurstStyle.Needle:
                    target.AddBuff(BuffID.Venom, 180);
                    break;

                case DEBurstStyle.Hellborn:
                    target.AddBuff(BuffID.OnFire3, 360);
                    target.AddBuff(ModContent.BuffType<Dragonfire>(), 210);
                    break;

                case DEBurstStyle.Plague:
                    target.AddBuff(ModContent.BuffType<Plague>(), 240);
                    break;

                case DEBurstStyle.Astral:
                    target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 240);
                    break;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, Radius, targetHitbox);

        public override bool PreDraw(ref Color lightColor) => false;
    }

    public class DEBullet_ThermalZone : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Particles/BloomCircle";

        private bool Ice => Projectile.ai[0] == 0f;
        private float Radius => Math.Max(56f, Projectile.ai[1]);

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.Resize((int)(Radius * 2f), (int)(Radius * 2f));
                DEBulletUtils.ParticleBurst(Projectile.Center, Ice ? Color.LightSkyBlue : Color.OrangeRed, Radius / 74f);
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(Radius, Radius) * Main.rand.NextFloat(0.2f, 1f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + offset,
                    Ice ? DustID.IceTorch : DustID.Torch,
                    -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.2f, 1.4f),
                    100,
                    Ice ? Color.LightSkyBlue : Color.OrangeRed,
                    Main.rand.NextFloat(0.75f, 1.3f));
                dust.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, (Ice ? Color.LightSkyBlue : Color.OrangeRed).ToVector3() * 0.35f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Ice)
            {
                target.AddBuff(BuffID.Frostburn2, 180);
                if (!target.boss && target.lifeMax <= 12000)
                {
                    target.AddBuff(BuffID.Frozen, 35);
                    target.velocity *= 0.18f;
                }
            }
            else
            {
                target.AddBuff(BuffID.OnFire3, 240);
                target.AddBuff(ModContent.BuffType<Dragonfire>(), 120);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, Radius, targetHitbox);

        public override bool PreDraw(ref Color lightColor) => false;
    }

    public class DEBullet_PestilentCloud : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Particles/BloomCircle";

        private float Radius => Math.Max(86f, Projectile.ai[0]);

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.Resize((int)(Radius * 2f), (int)(Radius * 2f));
                DEBulletUtils.ParticleBurst(Projectile.Center, Color.LawnGreen, Radius / 82f);
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.75f, Pitch = -0.15f }, Projectile.Center);
            }

            float fade = MathHelper.Clamp(Projectile.timeLeft / 150f, 0f, 1f);
            if (Main.rand.NextBool(2))
            {
                Vector2 offset = Main.rand.NextVector2Circular(Radius, Radius);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + offset,
                    Main.rand.NextBool() ? DustID.GreenTorch : DustID.TerraBlade,
                    Main.rand.NextVector2Circular(0.7f, 0.7f) - Vector2.UnitY * Main.rand.NextFloat(0.15f, 0.7f),
                    120,
                    Color.Lerp(Color.LawnGreen, Color.YellowGreen, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.8f, 1.45f) * fade);
                dust.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, Color.LawnGreen.ToVector3() * 0.25f * fade);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Plague>(), 300);
            target.AddBuff(BuffID.Poisoned, 180);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, Radius, targetHitbox);

        public override bool PreDraw(ref Color lightColor) => false;
    }

    public class DEBullet_HydraSnake : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/Ranged/HydrasBlood";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 100;
            Projectile.extraUpdates = 2;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            float side = Projectile.ai[0] == 0f ? 1f : Math.Sign(Projectile.ai[0]);
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 sideVelocity = forward.RotatedBy(MathHelper.PiOver2) * (float)Math.Sin(Projectile.localAI[0] * 0.42f) * side * 0.42f;
            Projectile.velocity += sideVelocity;
            Projectile.velocity = Projectile.velocity.SafeNormalize(forward) * MathHelper.Clamp(Projectile.velocity.Length(), 9f, 17f);

            if (Projectile.localAI[0] > 10f)
                DEBulletUtils.SimpleHoming(Projectile, 520f, 0.045f, 15f);

            DEBulletUtils.OrientToVelocity(Projectile);
            DEBulletUtils.TrailDust(Projectile, DustID.Venom, Color.LimeGreen, 0.85f, 0.16f);
            Lighting.AddLight(Projectile.Center, Color.LimeGreen.ToVector3() * 0.32f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.LimeGreen * 0.75f, 1);
            return true;
        }
    }

    public class DEBullet_NeedleSpike : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/Ranged/NeedlerProj";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.extraUpdates = 2;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.alpha = Math.Max(0, Projectile.alpha - 18);
            DEBulletUtils.OrientToVelocity(Projectile);
            DEBulletUtils.TrailDust(Projectile, DustID.Poisoned, Color.GreenYellow, 0.7f, 0.2f);
            Lighting.AddLight(Projectile.Center, Color.GreenYellow.ToVector3() * 0.24f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 240);
        }
    }

    public class DEBullet_StellarStar : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/Boss/AstralFlame";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 80;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.13f * Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);

            if (Projectile.localAI[0] < 36f)
            {
                Projectile.velocity *= 0.94f;
            }
            else
            {
                Projectile.tileCollide = true;
                float speed = MathHelper.Clamp(Projectile.velocity.Length() + 0.18f, 5f, 18f);
                DEBulletUtils.SimpleHoming(Projectile, 720f, 0.06f, speed);
            }

            if (Projectile.alpha > 0)
                Projectile.alpha -= 6;

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextBool() ? DustID.Firework_Blue : DustID.OrangeTorch,
                    Main.rand.NextVector2Circular(1.4f, 1.4f),
                    80,
                    Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Orange,
                    Main.rand.NextFloat(0.75f, 1.3f));
                dust.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, Color.Lerp(Color.DeepSkyBlue, Color.Orange, 0.45f).ToVector3() * 0.42f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 240);
            if (Main.myPlayer == Projectile.owner)
                DEBulletUtils.SpawnAreaBurst(Projectile.GetSource_FromAI(), Projectile.Center, Math.Max(1, (int)(hit.Damage * 0.45f)), Projectile.knockBack, Projectile.owner, DEBurstStyle.Astral, 86f);
        }

        public override void OnKill(int timeLeft)
        {
            DEBulletUtils.BurstDust(Projectile.Center, Color.DeepSkyBlue, DustID.Firework_Blue, 12, 4.5f, 1.1f);
            DEBulletUtils.BurstDust(Projectile.Center, Color.Orange, DustID.OrangeTorch, 12, 4.5f, 1.1f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.White * 0.8f, 1);
            return true;
        }
    }
}
