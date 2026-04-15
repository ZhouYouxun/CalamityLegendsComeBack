using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.TheExoPrism
{
    internal class ExoPrism_LazerMark : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 10;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 0;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
            {
                Projectile.Kill();
                return;
            }

            NPC target = Main.npc[targetIndex];
            if (!target.active || target.dontTakeDamage)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = target.Center;
            Projectile.velocity = Vector2.Zero;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float time = Lifetime - Projectile.timeLeft;
            float lifeProgress = time / Lifetime;
            float fade = Utils.GetLerpValue(0f, 3f, time, true) * Utils.GetLerpValue(0f, 3f, Projectile.timeLeft, true);
            float shrink = MathHelper.Lerp(1.3f, 0.55f, lifeProgress);
            float expand = MathHelper.Lerp(0.55f, 1.55f, lifeProgress);
            float pulse = 0.5f + 0.5f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 18f + Projectile.identity * 0.37f);
            float pulse2 = 0.5f + 0.5f * (float)System.Math.Cos(Main.GlobalTimeWrappedHourly * 14f + Projectile.identity * 0.19f);

            Texture2D thinSquare = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSquareParticleBig").Value;
            Texture2D thickSquare = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSquareParticleThick").Value;
            Texture2D triangle = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowTriangle").Value;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Color cyan = new Color(110, 245, 255, 0);
            Color gold = new Color(255, 210, 110, 0);
            Color lime = new Color(170, 255, 135, 0);
            Color orange = new Color(255, 150, 80, 0);
            Color white = new Color(255, 255, 255, 0);

            Color thinColor = Color.Lerp(cyan, white, 0.35f + pulse * 0.4f) * (0.42f * fade);
            Color thickColor = Color.Lerp(gold, orange, 0.35f + pulse2 * 0.45f) * (0.30f * fade);
            Color triColor = Color.Lerp(lime, cyan, 0.3f + pulse * 0.5f) * (0.22f * fade);

            float baseRot = time * 0.26f;

            Main.EntitySpriteDraw(thinSquare, drawPos, null, thinColor, MathHelper.PiOver4 + baseRot, thinSquare.Size() / 2f, (expand + pulse * 0.35f) * 0.82f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(thinSquare, drawPos, null, thinColor * 0.82f, -MathHelper.PiOver4 - baseRot * 1.25f, thinSquare.Size() / 2f, (shrink + pulse2 * 0.28f) * 0.95f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(thinSquare, drawPos, null, thinColor * 0.7f, baseRot * -1.8f, thinSquare.Size() / 2f, (1.05f + pulse * 0.22f) * shrink, SpriteEffects.None, 0);

            Main.EntitySpriteDraw(thickSquare, drawPos, null, thickColor, baseRot * 0.8f, thickSquare.Size() / 2f, (0.68f + pulse2 * 0.18f) * expand, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(thickSquare, drawPos, null, thickColor * 0.8f, MathHelper.PiOver2 - baseRot * 1.1f, thickSquare.Size() / 2f, (0.92f + pulse * 0.16f) * shrink, SpriteEffects.None, 0);

            for (int i = 0; i < 4; i++)
            {
                float angle = MathHelper.PiOver2 * i + baseRot * (i % 2 == 0 ? 1.3f : -1.1f);
                Vector2 offset = angle.ToRotationVector2() * (10f + pulse2 * 6f);
                Main.EntitySpriteDraw(
                    triangle,
                    drawPos + offset,
                    null,
                    triColor,
                    angle + MathHelper.PiOver4,
                    triangle.Size() / 2f,
                    (0.55f + pulse * 0.16f) * MathHelper.Lerp(1.15f, 0.72f, lifeProgress),
                    SpriteEffects.None,
                    0);
            }

            return false;
        }
    }
}
