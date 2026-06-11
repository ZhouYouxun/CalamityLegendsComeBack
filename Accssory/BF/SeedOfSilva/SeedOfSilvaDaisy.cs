using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.SeedOfSilva
{
    internal sealed class SeedOfSilvaDaisy : SeedOfSilvaFlowerProjectile
    {
        private const float DaisyMaxLife = 15f;
        private const float DaisyRegenPerFrame = 1.2f / 60f;

        private float daisyLife = DaisyMaxLife;
        private float daisyStoredHeal;
        private int daisyHitCooldown;

        protected override int FlowerSlot => 1;
        protected override BlossomFluxChloroplastPresetType FlowerPreset => BlossomFluxChloroplastPresetType.Chlo_BRecov;
        protected override string FlowerTexturePath => "CalamityLegendsComeBack/Accssory/BF/SeedOfSilva/SeedPack/Daisy";

        protected override void UpdateBlooming(Player owner, BFAccessoryPlayer accessoryPlayer)
        {
            daisyLife = System.Math.Min(DaisyMaxLife, daisyLife + DaisyRegenPerFrame);
            daisyStoredHeal += DaisyRegenPerFrame;

            if (daisyHitCooldown > 0)
            {
                daisyHitCooldown--;
                return;
            }

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy() || !npc.Hitbox.Intersects(Projectile.Hitbox))
                    continue;

                int incomingDamage = System.Math.Max(1, npc.damage);
                if (incomingDamage < 20)
                    continue;

                daisyHitCooldown = 30;
                daisyLife = System.Math.Max(0f, daisyLife - System.Math.Max(1f, incomingDamage * (1f - owner.endurance) - owner.statDefense * 0.5f));
                int healAmount = (int)System.Math.Floor(daisyStoredHeal);
                if (healAmount > 0 && owner.statLife < owner.statLifeMax2)
                {
                    owner.statLife = System.Math.Min(owner.statLifeMax2, owner.statLife + healAmount);
                    owner.HealEffect(healAmount, true);
                    daisyStoredHeal -= healAmount;
                }

                EmitFlowerBurst(new Color(150, 255, 174), 5);
                break;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);
            if (IsBlooming)
                DrawDaisyHealthBar();

            return false;
        }

        private void DrawDaisyHealthBar()
        {
            Texture2D barBackground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barForeground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;
            float progress = MathHelper.Clamp(daisyLife / DaisyMaxLife, 0f, 1f);
            float scale = 0.78f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(-barBackground.Width * scale * 0.5f, -44f);
            Rectangle frameCrop = new(0, 0, (int)(barForeground.Width * progress), barForeground.Height);
            Color backColor = new Color(20, 72, 32) * 0.82f;
            Color frontColor = Color.Lerp(new Color(80, 210, 96), new Color(205, 255, 188), progress);

            Main.EntitySpriteDraw(barBackground, drawPosition, null, backColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0);
            if (frameCrop.Width > 0)
                Main.EntitySpriteDraw(barForeground, drawPosition, frameCrop, frontColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
    }
}
