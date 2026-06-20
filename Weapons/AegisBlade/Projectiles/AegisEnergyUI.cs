using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    // 能量条UI（每帧跟随玩家显示在头顶，仅在持有 AegisBlade 时存活）。
    public class AegisEnergyUI : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color BarColor = new(200, 240, 255);
        private static readonly Color BarColorReady = new(255, 230, 80);

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || owner.HeldItem.type != ModContent.ItemType<AegisBlade>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center + new Vector2(0f, -72f);
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner) return false;

            Player owner = Main.player[Projectile.owner];
            AegisBladePlayer bladePlayer = owner.GetModPlayer<AegisBladePlayer>();

            if (owner.HeldItem.type != ModContent.ItemType<AegisBlade>())
                return false;

            Texture2D barBG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barFG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;

            float progress = bladePlayer.AegisEnergy / BalanceAegisBlade.EnergyMax;
            bool ready = progress >= 1f;
            Color col = ready ? BarColorReady : BarColor;

            Vector2 barPos = owner.Center - Main.screenPosition + new Vector2(-barBG.Width * 0.75f, -72f);
            Rectangle frame = new Rectangle(0, 0, (int)(progress * barFG.Width), barFG.Height);

            Main.spriteBatch.Draw(barBG, barPos, col * 0.9f);
            Main.spriteBatch.Draw(barFG, barPos, frame, col * 0.75f);

            return false;
        }
    }
}
