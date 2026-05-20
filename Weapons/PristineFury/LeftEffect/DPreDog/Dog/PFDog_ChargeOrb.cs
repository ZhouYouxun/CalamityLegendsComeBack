using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFDog_ChargeOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float HoldoutIndex => ref Projectile.ai[0];
        private ref float Charge => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(160, 100, 255));

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.penetrate = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Timer++;
            int index = (int)HoldoutIndex;
            if (!Main.projectile.IndexInRange(index) || !Main.projectile[index].active || Main.projectile[index].ModProjectile is not NewLegendPristineFuryHoldOut holdout)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = holdout.GunTipPosition + holdout.AimDirection * 11f;
            Projectile.velocity = holdout.AimDirection;
            Projectile.rotation = holdout.AimDirection.ToRotation();
            Projectile.timeLeft = 2;
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * (0.3f + Charge * 1.25f));

            if (!Main.dedServ && Main.rand.NextFloat() < 0.24f + Charge * 0.34f)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(32f + Charge * 76f, 32f + Charge * 76f);
                Particle mote = new PointParticle(
                    Projectile.Center + offset,
                    -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.6f, 4.8f),
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.75f, 1.18f),
                    Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.12f, 0.36f)));

                GeneralParticleHandler.SpawnParticle(mote);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Charge <= 0.02f || Main.dedServ)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmear").Value;
            Texture2D glowBlade = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowBlade").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = (Color.Lerp(ThemeColor, Color.White, Charge * 0.42f) with { A = 0 }) * Charge;
            float pulse = 0.86f + 0.14f * (float)System.Math.Sin(Timer * 0.18f);

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(bloom, drawPosition, null, color * 0.52f, Projectile.rotation, bloom.Size() * 0.5f, new Vector2(0.36f + Charge * 0.72f, 0.36f + Charge * 0.72f) * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(smear, drawPosition, null, color * 0.34f, -Projectile.rotation + Timer * 0.035f, smear.Size() * 0.5f, 0.18f + Charge * 0.42f, SpriteEffects.None, 0);

            for (int i = 0; i < 6; i++)
            {
                float rotation = Projectile.rotation + MathHelper.TwoPi * i / 6f + Timer * (0.015f + i * 0.002f);
                Main.EntitySpriteDraw(star, drawPosition, null, color * 0.42f, rotation, star.Size() * 0.5f, new Vector2(0.14f + Charge * 0.1f, 1.05f + Charge * 2.4f), SpriteEffects.None, 0);
                Main.EntitySpriteDraw(glowBlade, drawPosition, null, color * 0.16f, rotation, new Vector2(glowBlade.Width * 0.5f, glowBlade.Height), new Vector2(0.18f + Charge * 0.18f, 0.64f + Charge * 0.8f), SpriteEffects.None, 0);
            }

            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
