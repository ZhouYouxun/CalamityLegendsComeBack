using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.CtrlChip
{
    internal sealed class CtrlChipReticle : ModProjectile
    {
        private const float VisualScale = 1f / 3f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            CtrlChipPlayer ctrlPlayer = owner.GetModPlayer<CtrlChipPlayer>();
            if (!ctrlPlayer.CtrlChipEquipped || ctrlPlayer.ReticleWorld == Vector2.Zero)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = ctrlPlayer.ReticleWorld;
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            if (owner.whoAmI != Main.myPlayer)
                return false;

            CtrlChipPlayer ctrlPlayer = owner.GetModPlayer<CtrlChipPlayer>();
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            bool locked = ctrlPlayer.ReticleHasTarget;
            float time = Main.GlobalTimeWrappedHourly;
            float pulse = 0.78f + 0.22f * (float)System.Math.Sin(time * (locked ? 10f : 7f));

            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ringA = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_03").Value;
            Texture2D ringB = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_04").Value;
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;

            Color techBlue = new(70, 190, 255, 0);
            Color cyan = new(150, 245, 255, 0);
            Color white = new(235, 255, 255, 0);
            Color outerColor = Color.Lerp(techBlue, cyan, locked ? 0.7f : 0.35f);
            Color innerColor = Color.Lerp(cyan, white, locked ? 0.65f : 0.35f);
            float lockInterpolant = locked ? 1f : 0f;
            float ringScale = MathHelper.Lerp(0.34f, 0.48f, lockInterpolant) * pulse * VisualScale;
            float tickRadius = MathHelper.Lerp(28f, 42f, lockInterpolant) * VisualScale;

            Main.EntitySpriteDraw(glow, drawPosition, null, outerColor * 0.52f, 0f, glow.Size() * 0.5f, ringScale, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glow, drawPosition, null, innerColor * 0.34f, 0f, glow.Size() * 0.5f, ringScale * 0.48f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(ringA, drawPosition, null, outerColor * 0.88f, time * 0.95f, ringA.Size() * 0.5f, MathHelper.Lerp(0.46f, 0.58f, lockInterpolant) * VisualScale, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(ringB, drawPosition, null, cyan * 0.76f, -time * 0.72f, ringB.Size() * 0.5f, MathHelper.Lerp(0.42f, 0.54f, lockInterpolant) * VisualScale, SpriteEffects.FlipHorizontally, 0f);

            for (int i = 0; i < 4; i++)
            {
                float angle = time * (locked ? 2.4f : 1.5f) + MathHelper.PiOver2 * i;
                Vector2 direction = angle.ToRotationVector2();
                Vector2 tickPosition = drawPosition + direction * tickRadius;
                Main.EntitySpriteDraw(line, tickPosition, null, outerColor * 0.85f, angle + MathHelper.PiOver2, line.Size() * 0.5f,
                    new Vector2(0.036f, MathHelper.Lerp(0.14f, 0.22f, lockInterpolant)) * VisualScale, SpriteEffects.None, 0f);
            }

            if (locked)
                Main.EntitySpriteDraw(glow, drawPosition, null, white * 0.32f, 0f, glow.Size() * 0.5f, 0.14f * pulse * VisualScale, SpriteEffects.None, 0f);

            return false;
        }
    }
}
