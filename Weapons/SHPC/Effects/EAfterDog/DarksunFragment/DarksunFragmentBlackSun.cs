using CalamityMod;
using CalamityMod.Particles;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.DarksunFragment
{
    internal class DarksunFragmentBlackSun : ModProjectile, ILocalizedModType
    {
        public const int Lifetime = 360;
        public const int MaxLevel = 5;
        public const float BaseRadius = 32f;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float ShotTimer => ref Projectile.localAI[1];
        private int Level => Utils.Clamp((int)Projectile.ai[0], 1, MaxLevel);

        public static float GetRadiusForLevel(int level) => BaseRadius + (Utils.Clamp(level, 1, MaxLevel) - 1) * 5f;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = (int)(BaseRadius * 2f);
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Timer++;
            Projectile.velocity = Vector2.Zero;
            Projectile.ai[0] = MathHelper.Clamp(Projectile.ai[0], 1f, MaxLevel);
            if (Level >= MaxLevel && Projectile.owner == Main.myPlayer)
            {
                Projectile.Kill();
                return;
            }

            float radius = GetRadiusForLevel(Level);
            Projectile.Resize((int)(radius * 2f), (int)(radius * 2f));
            Projectile.Center = Projectile.Center;
            Projectile.rotation -= 0.035f + Level * 0.008f;

            ShotTimer++;
            float lifeProgress = Utils.GetLerpValue(Lifetime, 0f, Projectile.timeLeft, true);
            int interval = Math.Max(3, (int)MathHelper.Lerp(16f, 7f, lifeProgress) - Level * 2);
            if (Projectile.owner == Main.myPlayer && ShotTimer >= interval)
            {
                ShotTimer = 0f;
                SpawnEclipseBolt(radius);
            }

            MaintainOrbitingShps();

            if (Timer % 6f == 0f && !Main.dedServ)
            {
                Vector2 spawnOffset = Main.rand.NextVector2CircularEdge(radius, radius);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + spawnOffset,
                    DustID.GoldFlame,
                    -spawnOffset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.4f, 1.2f),
                    0,
                    Main.rand.NextBool(3) ? Color.Black : new Color(255, 190, 45),
                    Main.rand.NextFloat(0.85f, 1.45f));
                dust.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.78f, 0.46f, 0.08f) * (0.3f + Level * 0.08f));
        }

        private void SpawnEclipseBolt(float radius)
        {
            float levelProgress = Utils.GetLerpValue(1f, MaxLevel, Level, true);
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 spawn = Projectile.Center + angle.ToRotationVector2() * Main.rand.NextFloat(
                radius + MathHelper.Lerp(105f, 255f, levelProgress),
                radius + MathHelper.Lerp(175f, 440f, levelProgress));
            Vector2 tangent = (Projectile.Center - spawn).SafeNormalize(Vector2.UnitY).RotatedBy(-MathHelper.PiOver2);
            Vector2 control = Projectile.Center + tangent * Main.rand.NextFloat(
                MathHelper.Lerp(82f, 170f, levelProgress),
                MathHelper.Lerp(160f, 285f, levelProgress)) + Main.rand.NextVector2Circular(26f + Level * 5f, 26f + Level * 5f);

            int bolt = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawn,
                Vector2.Zero,
                ModContent.ProjectileType<DarksunFragmentEclipseBolt>(),
                Math.Max(1, (int)(Projectile.damage * (0.34f + Level * 0.04f))),
                Projectile.knockBack * 0.3f,
                Projectile.owner,
                Projectile.whoAmI,
                control.X,
                control.Y);

            if (Main.projectile.IndexInRange(bolt))
                Main.projectile[bolt].localAI[0] = MathHelper.Lerp(78f, 44f, levelProgress);
        }

        private void MaintainOrbitingShps()
        {
            if (Level < 3 || Projectile.owner != Main.myPlayer)
                return;

            int soulType = ModContent.ProjectileType<NewSHPS>();
            int desiredCount = Utils.Clamp(Level + 3, 6, 9);
            int activeCount = 0;
            foreach (Projectile soul in Main.ActiveProjectiles)
            {
                if (soul.type == soulType && soul.owner == Projectile.owner && (int)soul.ai[0] == 0 && (int)soul.ai[1] == Projectile.whoAmI && soul.ai[2] == 3f)
                    activeCount++;
            }

            if (activeCount >= desiredCount || Timer % 5f != 0f)
                return;

            int spawnCount = Math.Min(2, desiredCount - activeCount);
            for (int i = 0; i < spawnCount; i++)
            {
                Vector2 spawnOffset = Main.rand.NextVector2CircularEdge(128f + Level * 18f, 82f + Level * 10f).RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
                Vector2 spawn = Projectile.Center + spawnOffset;
                Vector2 velocity = spawnOffset.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2 * (Main.rand.NextBool() ? 1f : -1f)) * Main.rand.NextFloat(14f, 23f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawn,
                    velocity,
                    soulType,
                    Math.Max(1, Projectile.damage / 5),
                    Projectile.knockBack * 0.15f,
                    Projectile.owner,
                    0f,
                    Projectile.whoAmI,
                    3f);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Level < 3 || Projectile.owner != Main.myPlayer)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<DarksunFragmentBlackSunExplosion>(),
                Math.Max(1, (int)(Projectile.damage * (3.8f + Level * 0.42f))),
                Projectile.knockBack,
                Projectile.owner,
                Level);
        }

        public static void UpgradeOrExplode(Projectile sun, int amount = 1)
        {
            sun.ai[0] = MathHelper.Clamp(sun.ai[0] + amount, 1f, MaxLevel);
            sun.timeLeft = Lifetime;
            sun.netUpdate = true;

            int level = Utils.Clamp((int)sun.ai[0], 1, MaxLevel);
            SpawnUpgradeBurst(sun.Center, level);

            if (level >= MaxLevel && sun.owner == Main.myPlayer)
                sun.Kill();
        }

        public static void SpawnUpgradeBurst(Vector2 center, int level)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.4f, Pitch = 0.15f + level * 0.03f, MaxInstances = 4 }, center);
            for (int i = 0; i < 18 + level * 4; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 7.5f);
                Particle spark = new CustomSpark(
                    center,
                    velocity,
                    "CalamityMod/Particles/Sparkle",
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.05f, 0.12f),
                    Main.rand.NextBool() ? new Color(255, 205, 70) : Color.Black,
                    Vector2.One);
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D[] sunsetVortexTextures =
            {
                ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/Sun/fbmnoise2_003").Value,
                ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/Sun/fbmnoise2_004").Value,
                ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/Sun/fbmnoise2_005").Value,
                ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/Sun/fbmnoise2_006").Value,
                ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/Sun/gradationline_004").Value
            };

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float radiusScale = GetRadiusForLevel(Level) / 96f;
            float fade = Projectile.timeLeft < 24 ? Projectile.timeLeft / 24f : Utils.GetLerpValue(0f, 18f, Timer, true);
            float pulse = 1f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.2f) * 0.05f;

            // 这里直接参考 SunsetASunsetRightEXPMagic2：
            // 10 组沿圆周偏移的旋转纹理，每组叠加 5 张噪声/线纹理。
            // 之前这部分错误地放在一次性飞行光球上，命中后立即消失，所以黑日本体看不到漩涡。
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            float sunsetVortexScale = radiusScale * pulse * (1.38f + Level * 0.035f);
            for (int i = 0; i < 10; i++)
            {
                float angle = MathHelper.TwoPi * i / 3f + Main.GlobalTimeWrappedHourly * MathHelper.TwoPi;
                Color baseColor = i % 2 == 0 ? new Color(255, 205, 56) : new Color(70, 42, 8);
                Color drawColor = Color.Lerp(baseColor, Color.Black, MathHelper.Clamp(i * 0.09f, 0f, 0.82f)) * fade * 0.56f;
                Vector2 offset = (angle + Main.GlobalTimeWrappedHourly * i / 16f).ToRotationVector2() * (5f + Level * 0.7f);

                foreach (Texture2D texture in sunsetVortexTextures)
                {
                    Main.EntitySpriteDraw(
                        texture,
                        drawPos + offset,
                        null,
                        drawColor,
                        -angle + MathHelper.PiOver2,
                        texture.Size() * 0.5f,
                        sunsetVortexScale,
                        SpriteEffects.None);
                }
            }

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < 3; i++)
            {
                Color edge = new Color(255, 205, 64, 0) * fade * (0.64f - i * 0.13f);
                Main.EntitySpriteDraw(ring, drawPos, null, edge, -Projectile.rotation * (1.35f + i * 0.28f), ring.Size() * 0.5f, radiusScale * (0.62f + i * 0.12f), SpriteEffects.None);
            }
            Main.EntitySpriteDraw(ring, drawPos, null, new Color(255, 205, 64, 0) * 0.55f * fade, -Projectile.rotation * 1.8f, ring.Size() * 0.5f, radiusScale * 0.7f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPos, null, new Color(255, 180, 36, 0) * 0.26f * fade, Projectile.rotation, bloom.Size() * 0.5f, radiusScale * 0.74f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            return false;
        }
    }
}
