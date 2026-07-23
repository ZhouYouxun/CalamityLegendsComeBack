using System;
using CalamityLegendsComeBack.Weapons.AegisBlade.Visuals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    // 能量条UI，仅在持有 AegisBlade 时存活，显示在玩家头顶。
    // 注：当前版本的终结技能量实际显示在左上角的 Calamity CooldownHandler（见 AegisBlade.HoldItem），
    // 这个头顶条没有被生成。这里仍按统一的圣火配色处理，保证它一旦启用就和整把武器同一套语言。
    public class AegisEnergyUI : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color BarColor       = AegisVisuals.Ember;
        private static readonly Color BarColorReady  = AegisVisuals.Core;
        private static readonly Color BarColorCharge = AegisVisuals.Gold;

        public override void SetDefaults()
        {
            Projectile.width  = 2;
            Projectile.height = 2;
            Projectile.tileCollide  = false;
            Projectile.ignoreWater  = true;
            Projectile.penetrate    = -1;
            Projectile.timeLeft     = 2;
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
            Projectile.Center   = owner.Center + new Vector2(0f, -80f);
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner) return false;

            Player owner = Main.player[Projectile.owner];
            AegisBladePlayer bp = owner.GetModPlayer<AegisBladePlayer>();

            if (owner.HeldItem.type != ModContent.ItemType<AegisBlade>()) return false;

            Texture2D barBG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barFG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;

            float progress = bp.AegisEnergy / BalanceAegisBlade.EnergyMax;
            bool ready = progress >= 1f;

            // 颜色沿统一调色盘走：未满 = 余烬红 → 圣金，蓄力中 = 圣金，充满 = 白金
            Color col;
            if (ready)            col = BarColorReady;
            else if (bp.ShieldCharging || bp.ShieldFullyCharged)
                                  col = BarColorCharge;
            else                  col = Color.Lerp(BarColor, BarColorCharge, progress);

            // 充满时轻微脉动
            float pulseAlpha = ready ? (0.75f + 0.15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 6f)) : 0.75f;

            Vector2 barPos = owner.Center - Main.screenPosition + new Vector2(-barBG.Width * 0.75f, -80f);
            Rectangle frame = new Rectangle(0, 0, (int)(progress * barFG.Width), barFG.Height);
            Vector2 barCenter = barPos + new Vector2(barBG.Width * 0.5f, barBG.Height * 0.5f);

            // 充满时在条后压一层圣火余晖 + 两端符文星芒，让"可以放大招了"在余光里也读得到
            if (ready)
            {
                Texture2D bloom = AegisVisuals.Tex(AegisVisuals.TexBloom);
                Texture2D star = AegisVisuals.Tex(AegisVisuals.TexStarThin);

                Main.spriteBatch.Draw(bloom, barCenter, null,
                    AegisVisuals.Add(AegisVisuals.Gold, 0.35f * pulseAlpha), 0f, bloom.Size() * 0.5f,
                    new Vector2(barBG.Width / 120f, barBG.Height / 34f), SpriteEffects.None, 0f);

                for (int i = -1; i <= 1; i += 2)
                {
                    Main.spriteBatch.Draw(star, barCenter + new Vector2(i * barBG.Width * 0.5f, 0f), null,
                        AegisVisuals.Add(AegisVisuals.Core, 0.55f * pulseAlpha),
                        Main.GlobalTimeWrappedHourly * 2.2f * i, star.Size() * 0.5f,
                        AegisVisuals.RadiusScale(star, 9f), SpriteEffects.None, 0f);
                }
            }

            Main.spriteBatch.Draw(barBG, barPos, Color.Lerp(AegisVisuals.Charred, col, 0.35f) * 0.9f);
            Main.spriteBatch.Draw(barFG, barPos, frame, col * pulseAlpha);

            return false;
        }
    }
}
