using CalamityLegendsComeBack.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive
{
    // A compact 3x3 charge readout for the nine follow-up Judgement waves.
    internal sealed class YCAuricJudgementMatrix : ModProjectile, IScreenOverlayProjectile
    {
        private const int MatrixWidth = 3;
        private const int MatrixCapacity = MatrixWidth * MatrixWidth;
        private const float SlotSpacing = 8f;
        private const float SlotSize = 3.4f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hide = true;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || owner.GetModPlayer<YharimsCrystalStatePlayer>().AuricJudgementCharges <= 0)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center;
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            int charges = System.Math.Clamp(owner.GetModPlayer<YharimsCrystalStatePlayer>().AuricJudgementCharges, 0, MatrixCapacity);
            Vector2 center = owner.Top - Main.screenPosition + new Vector2(0f, owner.gfxOffY - 24f);
            float pulse = 0.92f + 0.08f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 5f);

            for (int row = 0; row < MatrixWidth; row++)
            {
                for (int column = 0; column < MatrixWidth; column++)
                {
                    int index = row * MatrixWidth + column;
                    Vector2 position = center + new Vector2((column - 1) * SlotSpacing, (row - 1) * SlotSpacing);
                    bool active = index < charges;

                    DrawDiamond(position, SlotSize * 1.5f, active ? new Color(255, 190, 62, 0) * 0.33f : new Color(95, 68, 32, 0) * 0.22f);
                    DrawDiamond(position, SlotSize * (active ? pulse : 1f), active ? new Color(255, 221, 118, 0) * 0.9f : new Color(48, 35, 18, 0) * 0.7f);
                }
            }

            return false;
        }

        private static void DrawDiamond(Vector2 center, float size, Color color)
        {
            Main.EntitySpriteDraw(
                TextureAssets.MagicPixel.Value,
                center,
                new Rectangle(0, 0, 1, 1),
                color,
                MathHelper.PiOver4,
                new Vector2(0.5f),
                new Vector2(size),
                SpriteEffects.None,
                0f);
        }
    }
}
