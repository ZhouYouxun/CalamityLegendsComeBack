using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    public class NewLegendPristineFury : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(5, 4));
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 100;
            Item.height = 46;
            Item.damage = 77;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 2;
            Item.useAnimation = 2;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.knockBack = 5f;
            Item.UseSound = null;
            Item.shoot = ModContent.ProjectileType<NewLegendPristineFuryHoldOut>();
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.Gel;
            Item.value = CalamityGlobalItem.RarityTurquoiseBuyPrice;
            Item.rare = ModContent.RarityType<Turquoise>();
        }

        public override Vector2? HoldoutOffset() => new Vector2(-25f, -10f);

        public override bool CanUseItem(Player player) => false;

        public override bool CanShoot(Player player) => false;

        public override void HoldItem(Player player)
        {
            player.GetModPlayer<PristineFuryPlayer>().HoldingPristineFury = true;
            player.Calamity().mouseWorldListener = true;

            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            if (Main.myPlayer == player.whoAmI &&
                player.ownedProjectileCounts[ModContent.ProjectileType<NewLegendPristineFuryHoldOut>()] <= 0)
            {
                Vector2 direction = (GetMouseWorld(player) - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
                Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    player.MountedCenter,
                    direction,
                    ModContent.ProjectileType<NewLegendPristineFuryHoldOut>(),
                    player.GetWeaponDamage(Item),
                    Item.knockBack,
                    player.whoAmI);
            }
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            PristineFuryMark mark = player.GetModPlayer<PristineFuryPlayer>().CurrentMark;
            damage.Base += PristineFuryMarkHelper.GetDisplayedBaseDamage(mark) - Item.damage;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            PristineFuryMark mark = Main.LocalPlayer.GetModPlayer<PristineFuryPlayer>().CurrentMark;
            string markName = PristineFuryMarkHelper.GetName(mark);
            string intro = string.Format(this.GetLocalizedValue("PristineFuryIntro"), markName);
            string left = this.GetLocalizedValue($"Left_{mark}");
            string right = this.GetLocalizedValue("RightClick");
            string hook = this.GetLocalizedValue("Hook");
            string legendarySection = Main.keyState.PressingShift()
                ? this.GetLocalizedValue("LegendaryText")
                : this.GetLocalizedValue("LegendaryHint");

            string finalText =
                intro + "\n\n" +
                left + "\n\n" +
                right + "\n" +
                hook + "\n\n" +
                legendarySection + "\n";

            tooltips.FindAndReplace("[GFB]", finalText);
        }

        public override bool CanRightClick() => false;

        public override bool ConsumeItem(Player player) => false;

        private static Vector2 GetMouseWorld(Player player)
        {
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frameI, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int frameHeight = texture.Height / 4;
            int frame = (int)(Main.GameUpdateCount / 5 % 4);
            Rectangle source = new(0, frameHeight * frame, texture.Width, frameHeight);
            spriteBatch.Draw(texture, position, source, Color.White, 0f, new Vector2(texture.Width * 0.5f, frameHeight * 0.5f), scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
