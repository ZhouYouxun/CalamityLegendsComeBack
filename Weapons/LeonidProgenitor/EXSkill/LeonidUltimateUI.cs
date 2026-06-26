using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityMod;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    public class LeonidUltimateUI : ModProjectile
    {
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

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || owner.HeldItem.type != ModContent.ItemType<LeonidProgenitor>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center + new Vector2(0f, -76f);
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            if (owner.HeldItem.type != ModContent.ItemType<LeonidProgenitor>())
                return false;

            var modPlayer = owner.GetModPlayer<LeonidProgenitorPlayer>();
            float progress = modPlayer.UltimateEnergy / 100f;

            Texture2D barBG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barFG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;

            bool stealthReady = owner.Calamity().rogueStealth >= owner.Calamity().rogueStealthMax * 0.999f;
            bool ultimateReady = progress >= 1f && stealthReady;

            Color col;
            if (ultimateReady)
            {
                // Pulsing gold-white stratus color when fully ready.
                col = LeonidVisualUtils.GetReadyGold();
            }
            else if (progress >= 1f)
            {
                // Moon-violet when energy is full but stealth is not.
                col = Color.Lerp(LeonidVisualUtils.MoonViolet, LeonidVisualUtils.MoonWhite, 0.18f);
            }
            else
            {
                // Dimmer deep-blue during charging.
                col = LeonidVisualUtils.GetCelestialColor(progress) * 0.82f;
            }

            Vector2 barPos = owner.Center - Main.screenPosition + new Vector2(-barBG.Width * 0.5f, -76f);
            Rectangle frame = new Rectangle(0, 0, (int)(progress * barFG.Width), barFG.Height);

            // Draw bar
            Main.spriteBatch.Draw(barBG, barPos, col * 0.9f);
            Main.spriteBatch.Draw(barFG, barPos, frame, col * 0.75f);

            // Draw a spinning star icon next to the bar when stealth is ready
            if (stealthReady)
            {
                Texture2D star = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/star_01").Value;
                Vector2 starPos = barPos + new Vector2(barBG.Width + 12f, barBG.Height * 0.5f);
                Color starColor = Color.Lerp(LeonidVisualUtils.StratusBlue, LeonidVisualUtils.MoonWhite, 0.3f + 0.3f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f));
                
                Main.spriteBatch.Draw(star, starPos, null, starColor * 0.9f, Main.GlobalTimeWrappedHourly * 3f, star.Size() * 0.5f, 0.22f, SpriteEffects.None, 0f);
            }

            return false;
        }
    }
}
