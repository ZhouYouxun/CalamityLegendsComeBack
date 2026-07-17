using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Particles;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.GodsKey
{
    // 原版“钥匙剑”蜂王之剑(BeeKeeper)贴图 + 描边层，左键切换本模组全部传奇武器面板永久×1.2
    public sealed class GodsKey : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "Terraria/Images/Item_" + ItemID.BeeKeeper;

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item29;
            Item.noMelee = true;
            Item.autoReuse = false;
            Item.maxStack = 1;
            Item.value = CalamityGlobalItem.RarityVioletBuyPrice;
            Item.rare = ModContent.RarityType<BurnishedAuric>();
            Item.Calamity().devItem = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.IronBar, 10)
                .AddIngredient(ItemID.Chain, 15)
                .Register();
        }

        public override bool? UseItem(Player player)
        {
            if (player.itemAnimation > 0 && player.itemTime == 0)
            {
                player.itemTime = Item.useTime;

                GodsKeyPlayer godsKey = player.GetModPlayer<GodsKeyPlayer>();
                godsKey.PanelBoostEnabled = !godsKey.PanelBoostEnabled;

                if (player.whoAmI == Main.myPlayer)
                    SpawnToggleBurst(player, godsKey.PanelBoostEnabled);
            }

            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            bool enabled = Main.LocalPlayer.GetModPlayer<GodsKeyPlayer>().PanelBoostEnabled;
            string modeText = this.GetLocalizedValue(enabled ? "ModeOn" : "ModeOff");

            tooltips.Add(new TooltipLine(Mod, "GodsKeyMode", modeText)
            {
                OverrideColor = enabled ? new Color(255, 215, 110) : new Color(160, 170, 185)
            });
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            DrawDivineOutline(spriteBatch, TextureAssets.Item[Type].Value, position, frame, origin, scale);
            return true;
        }

        private static void DrawDivineOutline(SpriteBatch spriteBatch, Texture2D texture, Vector2 position,
            Rectangle frame, Vector2 origin, float scale)
        {
            float pulse = 0.7f + 0.3f * MathF.Sin(Main.GlobalTimeWrappedHourly * 4.4f);
            Color outlineColor = Color.Lerp(new Color(255, 221, 130), Color.White, 0.3f) with { A = 0 };
            float distance = 2f + pulse * 1.6f;

            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * distance;
                spriteBatch.Draw(texture, position + offset, frame, outlineColor * (0.5f * pulse), 0f, origin, scale, SpriteEffects.None, 0f);
            }
        }

        private static void SpawnToggleBurst(Player player, bool enabled)
        {
            SoundEngine.PlaySound(enabled ? SoundID.Item29 with { Pitch = 0.3f } : SoundID.Item29 with { Pitch = -0.3f }, player.Center);

            Color burstColor = enabled ? new Color(255, 221, 130) : new Color(120, 130, 150);
            for (int i = 0; i < 28; i++)
            {
                float angle = MathHelper.TwoPi * i / 28f;
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(5f, 11f);
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    player.Center,
                    velocity,
                    Main.rand.NextFloat(0.9f, 1.4f),
                    Color.Lerp(burstColor, Color.White, Main.rand.NextFloat(0.2f, 0.6f)),
                    Main.rand.Next(24, 34)));
            }

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                player.Center,
                Vector2.Zero,
                burstColor,
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                0f,
                0.2f,
                2.6f,
                28));
        }
    }
}
