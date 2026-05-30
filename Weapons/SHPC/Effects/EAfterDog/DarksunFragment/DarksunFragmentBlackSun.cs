using CalamityMod;
using CalamityMod.Particles;
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

            float radius = GetRadiusForLevel(Level);
            Projectile.Resize((int)(radius * 2f), (int)(radius * 2f));
            Projectile.Center = Projectile.Center;
            Projectile.rotation -= 0.035f + Level * 0.008f;

            ShotTimer++;
            float lifeProgress = Utils.GetLerpValue(Lifetime, 0f, Projectile.timeLeft, true);
            int interval = Math.Max(5, (int)MathHelper.Lerp(15f, 9f, lifeProgress) - (Level - 1));
            if (Projectile.owner == Main.myPlayer && ShotTimer >= interval)
            {
                ShotTimer = 0f;
                SpawnEclipseBolt(radius);
            }

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
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 spawn = Projectile.Center + angle.ToRotationVector2() * Main.rand.NextFloat(radius + 110f, radius + 180f);
            Vector2 tangent = (Projectile.Center - spawn).SafeNormalize(Vector2.UnitY).RotatedBy(-MathHelper.PiOver2);
            Vector2 control = Projectile.Center + tangent * Main.rand.NextFloat(80f, 155f) + Main.rand.NextVector2Circular(26f, 26f);

            Projectile.NewProjectile(
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
            Texture2D vortex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleVortex").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D face = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/ScreamyFace").Value;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float radiusScale = GetRadiusForLevel(Level) / 96f;
            float fade = Projectile.timeLeft < 24 ? Projectile.timeLeft / 24f : Utils.GetLerpValue(0f, 18f, Timer, true);
            float pulse = 1f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.2f) * 0.05f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < 7; i++)
            {
                Color color = Color.Lerp(new Color(255, 198, 42), Color.Black, i / 6f) * fade * (0.42f - i * 0.035f);
                color.A = 0;
                Main.EntitySpriteDraw(vortex, drawPos, null, color, Projectile.rotation * (i % 2 == 0 ? 1f : -1.35f) + i, vortex.Size() * 0.5f, radiusScale * pulse * (1f + i * 0.035f), SpriteEffects.None);
            }

            Main.EntitySpriteDraw(ring, drawPos, null, new Color(255, 205, 64, 0) * 0.55f * fade, -Projectile.rotation * 1.8f, ring.Size() * 0.5f, radiusScale * 0.7f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPos, null, Color.Black * 0.68f * fade, Projectile.rotation, bloom.Size() * 0.5f, radiusScale * 0.5f, SpriteEffects.None);
            Main.EntitySpriteDraw(face, drawPos, null, new Color(25, 18, 4, 0) * 0.36f * fade, -Projectile.rotation * 0.5f, face.Size() * 0.5f, radiusScale * 0.33f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            return false;
        }
    }
}
