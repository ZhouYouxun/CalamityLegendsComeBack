using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    public class NewLegendBrinyBaron : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 120;
            Item.height = 120;

            Item.damage = 120;
            Item.DamageType = DamageClass.Melee;

            Item.useTime = 60;
            Item.useAnimation = 60;

            Item.knockBack = 6f;
            Item.autoReuse = true;
            Item.useTurn = true;

            Item.channel = true;

            Item.noUseGraphic = true;
            Item.noMelee = true;

            Item.useStyle = ItemUseStyleID.Shoot;

            Item.shoot = ModContent.ProjectileType<CommonAttack.BrinyBaron_LeftClick_Swing>();
        }













        // ===============================
        // ❗射击函数（默认行为）
        // ===============================
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 默认：允许发射
            return true;
        }

        // ===============================
        // ❗制作配方（默认空配方）
        // ===============================
        public override void AddRecipes()
        {
            //CreateRecipe()
            //    .AddIngredient(ItemID.Wood, 10) // 随便一个默认材料
            //    .AddTile(TileID.WorkBenches)
            //    .Register();







        }

        // ===============================
        // ❗右键功能开关（默认关闭）
        // ===============================
        public override bool AltFunctionUse(Player player)
        {
            return false; // 默认不启用右键
        }

        // ===============================
        // ❗左右键分流（默认全部走左键）
        // ===============================
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                // 右键逻辑（默认啥也不改）
            }
            else
            {
                // 左键逻辑（默认）
            }

            return base.CanUseItem(player);
        }

        // ===============================
        // ❗手持动画（默认）
        // ===============================
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            // 默认行为（不改）
        }












    }
}