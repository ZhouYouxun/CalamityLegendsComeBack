using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFHardMode_TotalityFire : ModProjectile, ILocalizedModType
    {
        private bool initialized;
        private bool spawnedGroundFire;
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Rogue/TotalityFire";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 3;
            ProjectileID.Sets.TrailCacheLength[Type] = 2;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (!initialized)
            {
                initialized = true;
                Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
                Projectile.frameCounter = 0;
            }

            Projectile.rotation = Projectile.ai[1] > 0f
                ? -Projectile.velocity.X * 0.05f + MathHelper.PiOver2
                : Projectile.velocity.ToRotation();
            Projectile.ai[1]--;

            Projectile.localAI[0] = Math.Min(5f, Projectile.localAI[0] + 1f);
            if (Projectile.localAI[0] >= 5f)
            {
                if (Projectile.velocity.Y == 0f)
                    Projectile.velocity.X *= 0.97f;
                Projectile.velocity.Y = Math.Min(16f, Projectile.velocity.Y + 0.2f);
            }

            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 0.42f);
            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.08f, 80, ThemeColor, Main.rand.NextFloat(0.7f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.ai[1] = 10f;
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X * 0.35f;
            if (Projectile.velocity.Y != oldVelocity.Y && oldVelocity.Y > 1f)
                Projectile.velocity.Y = -oldVelocity.Y * 0.32f;

            if (!spawnedGroundFire && Projectile.owner == Main.myPlayer)
            {
                spawnedGroundFire = true;
                int field = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Bottom,
                    Vector2.Zero,
                    ModContent.ProjectileType<PFHardMode_GroundFire>(),
                    Math.Max(1, (int)(Projectile.damage * 0.72f)),
                    0f,
                    Projectile.owner);
                PFLeftEffectRules.ApplyTheme(field, (PristineFuryMark)(int)Projectile.ai[2]);
            }

            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) =>
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 300);

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityMod.CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.Lerp(lightColor, ThemeColor, 0.72f));
            return false;
        }

        internal static void SpawnBurstEffects(Vector2 center, Color theme, float scale)
        {
            if (Main.dedServ)
                return;

            Color warm = Color.Lerp(theme, Color.White, 0.24f);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, warm * 0.82f, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.08f, 1.08f * scale, 22));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, theme * 0.55f, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.18f, 1.85f * scale, 28));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, warm, "CalamityMod/Particles/SoftRoundExplosion", Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.03f, 0.42f * scale, 18, true));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, Color.White * 0.55f, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.05f, 0.24f * scale, 12, false));

            for (int i = 0; i < 5; i++)
            {
                float angle = MathHelper.TwoPi * i / 5f + Main.rand.NextFloat(-0.18f, 0.18f);
                Vector2 offset = angle.ToRotationVector2() * Main.rand.NextFloat(16f, 44f) * scale;
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center + offset, Vector2.Zero, theme * 0.42f, Vector2.One, angle, 0.06f, Main.rand.NextFloat(0.22f, 0.42f) * scale, 14));
            }

            for (int i = 0; i < 34; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / 34f).ToRotationVector2().RotatedByRandom(0.13f) * Main.rand.NextFloat(3.2f, 11.8f) * scale;
                GeneralParticleHandler.SpawnParticle(new SparkParticle(center, velocity, false, Main.rand.Next(12, 24), Main.rand.NextFloat(0.62f, 1.35f) * scale, Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.1f, 0.45f))));
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.1f, 4.8f) * scale;
                GeneralParticleHandler.SpawnParticle(new PointParticle(center + Main.rand.NextVector2Circular(30f, 30f) * scale, velocity, false, Main.rand.Next(12, 22), Main.rand.NextFloat(0.72f, 1.18f) * scale, Color.Lerp(warm, Color.White, Main.rand.NextFloat(0.08f, 0.36f))));
            }

            for (int i = 0; i < 42; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(40f, 40f) * scale,
                    Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.6f, 8f) * scale,
                    70,
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.04f, 0.28f)),
                    Main.rand.NextFloat(0.8f, 1.75f) * scale);
                dust.noGravity = true;
            }

            for (int i = 0; i < 9; i++)
            {
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    center + Main.rand.NextVector2Circular(34f, 34f) * scale,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.6f, 2.4f) * scale,
                    Color.Lerp(theme, Color.Goldenrod, Main.rand.NextFloat(0.12f, 0.36f)),
                    Color.Transparent,
                    Main.rand.NextFloat(0.45f, 0.92f) * scale,
                    Main.rand.Next(24, 42),
                    Main.rand.NextFloat(-0.06f, 0.06f)));
            }
        }
    }

    internal sealed class PFHardMode_GroundFire : ModProjectile, ILocalizedModType
    {
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 82;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 210;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 24;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 0.34f);
            if (Main.dedServ || !Main.rand.NextBool(2))
                return;

            Vector2 position = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-34f, 34f), Main.rand.NextFloat(-5f, 4f));
            GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                position,
                new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), Main.rand.NextFloat(-2.8f, -0.7f)),
                ThemeColor,
                Color.DarkGoldenrod,
                Main.rand.NextFloat(0.5f, 1f),
                Main.rand.Next(22, 40),
                Main.rand.NextFloat(-0.07f, 0.07f)));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) =>
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 300);

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
