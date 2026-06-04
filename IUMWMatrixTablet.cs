using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack
{
    public class IUMWMatrixTablet : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Assets/UI/IUMWIcon";

        public override void SetDefaults()
        {
            Item.width = 64;
            Item.height = 64;
            Item.rare = ItemRarityID.Cyan;
            Item.value = 0;
            Item.useTime = 12;
            Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<IUMWMatrixPanel>();
            Item.shootSpeed = 0f;
            Item.UseSound = null;
        }

        public override bool CanUseItem(Player player)
        {
            return Main.myPlayer == player.whoAmI &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !player.mouseInterface &&
                !(Main.playerInventory && Main.HoverItem.type == Type);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return IUMWMatrixPanel.OpenOrClose(player, source);
        }
    }
}
