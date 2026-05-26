using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    internal class BFArrow_CDetecReticle : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float TargetNpcIndex => ref Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 45;
            Projectile.alpha = 255;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!BFArrowCommon.InBounds(TargetNpcIndex, Main.maxNPCs))
            {
                Projectile.Kill();
                return;
            }

            NPC npc = Main.npc[(int)TargetNpcIndex];
            if (!npc.active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = npc.Center;
            Projectile.Opacity = Utils.GetLerpValue(0f, 2f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, new Vector3(0.12f, 0.34f, 0.4f) * Projectile.Opacity);

            if (Main.dedServ || Main.rand.NextBool(2))
                return;

            Vector2 offset = Main.rand.NextVector2CircularEdge(npc.width * 0.58f + 10f, npc.height * 0.58f + 10f);
            Dust dust = Dust.NewDustPerfect(
                npc.Center + offset,
                DustID.Electric,
                -offset.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.32f) * Main.rand.NextFloat(1.2f, 2.6f),
                80,
                Color.Lerp(new Color(80, 232, 255), Color.White, Main.rand.NextFloat(0.15f, 0.42f)),
                Main.rand.NextFloat(0.88f, 1.22f));
            dust.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D glowTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D magic03 = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_03").Value;
            Texture2D magic04 = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_04").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color outerColor = new Color(116, 236, 255, 0) * Projectile.Opacity;
            Color innerColor = new Color(240, 255, 255, 0) * Projectile.Opacity;
            float rotation = Main.GlobalTimeWrappedHourly * 4f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(glowTex, drawPosition, null, outerColor * 0.42f, 0f, glowTex.Size() * 0.5f, 0.14f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glowTex, drawPosition, null, innerColor * 0.36f, 0f, glowTex.Size() * 0.5f, 0.07f, SpriteEffects.None, 0f);

            for (int i = 0; i < 4; i++)
            {
                float angle = rotation + MathHelper.PiOver2 * i;
                Vector2 offset = angle.ToRotationVector2() * 10f;
                Main.EntitySpriteDraw(glowTex, drawPosition + offset, null, outerColor * 0.7f, angle, glowTex.Size() * 0.5f, 0.04f, SpriteEffects.None, 0f);
            }

            Main.EntitySpriteDraw(magic03, drawPosition, null, outerColor * 0.7f, rotation * 0.35f, magic03.Size() * 0.5f, 0.12f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(magic04, drawPosition, null, innerColor * 0.65f, -rotation * 0.28f, magic04.Size() * 0.5f, 0.12f, SpriteEffects.FlipHorizontally, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
