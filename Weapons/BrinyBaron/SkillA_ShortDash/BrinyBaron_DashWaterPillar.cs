using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash
{
    internal sealed class BrinyBaron_DashWaterPillar : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int timer;
        private float ScaleFactor => Projectile.ai[0] <= 0f ? 1f : Projectile.ai[0];
        private bool CenterLane => Projectile.ai[1] == 1f;

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 150;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 32;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override bool ShouldUpdatePosition() => true;

        public override void AI()
        {
            timer++;

            Vector2 center = Projectile.Center;
            Projectile.width = (int)(28f * ScaleFactor);
            Projectile.height = (int)(150f * ScaleFactor);
            Projectile.Center = center;
            Projectile.velocity.Y *= 0.94f;

            Lighting.AddLight(Projectile.Center, new Vector3(0.05f, 0.22f, 0.32f) * Projectile.Opacity);
            SpawnWaterColumnParticles();
        }

        private void SpawnWaterColumnParticles()
        {
            if (Main.dedServ)
                return;

            float fade = Utils.GetLerpValue(0f, 8f, timer, true) * Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);
            int count = CenterLane ? 5 : 3;

            for (int i = 0; i < count; i++)
            {
                Vector2 offset = new(Main.rand.NextFloat(-Projectile.width * 0.45f, Projectile.width * 0.45f), Main.rand.NextFloat(-Projectile.height * 0.42f, Projectile.height * 0.42f));
                Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(1.6f, 4.2f) + Vector2.UnitX * Main.rand.NextFloat(-0.45f, 0.45f);
                Color color = Color.Lerp(new Color(65, 175, 255), Color.White, Main.rand.NextFloat(0.1f, 0.44f)) * fade;

                Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, Main.rand.NextBool(3) ? DustID.Frost : DustID.Water, velocity, 100, color, Main.rand.NextFloat(0.74f, 1.15f) * ScaleFactor);
                dust.noGravity = true;

                if (i == 0 || Main.rand.NextBool(3))
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + offset,
                        velocity * 0.35f,
                        false,
                        Main.rand.Next(8, 13),
                        Main.rand.NextFloat(0.18f, 0.34f) * ScaleFactor,
                        color,
                        true,
                        false,
                        true));
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/ThinEndedLine").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float fade = Utils.GetLerpValue(0f, 8f, timer, true) * Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);
            Color columnColor = new Color(70, 190, 255, 0) * (CenterLane ? 0.58f : 0.42f) * fade;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

            Main.EntitySpriteDraw(
                line,
                drawPosition,
                null,
                columnColor,
                -MathHelper.PiOver2,
                line.Size() * 0.5f,
                new Vector2(Projectile.height / (float)line.Width, Projectile.width / (float)line.Height) * 0.95f,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition + Vector2.UnitY * Projectile.height * 0.38f,
                null,
                columnColor * 0.42f,
                0f,
                bloom.Size() * 0.5f,
                new Vector2(0.34f, 0.12f) * ScaleFactor,
                SpriteEffects.None);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            return false;
        }
    }
}

