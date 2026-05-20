 
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using CalamityMod;
using Microsoft.Xna.Framework;

namespace CalamityRangerExpansion.Content.BOWChange.BPrePlantera.BrimstoneFuryC
{
    internal class BrimstoneFuryRe : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Weapons.BPrePlantera";
        public override void SetDefaults()
        {
            // 设置武器基本属性
            Item.width = 40; // 弓的宽度
            Item.height = 80; // 弓的高度
            Item.damage = 55; // 武器伤害
            Item.DamageType = DamageClass.Ranged; // 伤害类型：远程
            Item.useTime = 5; // 使用时间（5帧）
            Item.useAnimation = 5; // 动画时间（5帧）
            Item.useStyle = ItemUseStyleID.Shoot; // 使用风格：射击
            Item.knockBack = 4; // 击退力
            // Item.UseSound = SoundID.Item5; // 一般情况下他没有使用音效
            Item.shoot = ModContent.ProjectileType<BrimstoneFuryReHold>(); // 手持弹幕
            Item.shootSpeed = 0f; // 手持弹幕的初始速度为0
            Item.noMelee = true; // 不进行近战攻击
            Item.noUseGraphic = true; // 使用时隐藏物品模型
            Item.channel = true; // 支持长按
            Item.autoReuse = true; // 自动连点
            Item.useAmmo = AmmoID.Arrow;

            Item.value = CalamityGlobalItem.RarityPinkBuyPrice;
            Item.rare = ItemRarityID.Pink;
            // Item.Calamity().canFirePointBlankShots = true;
        }
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
        public override bool CanConsumeAmmo(Item ammo, Player player) => player.ownedProjectileCounts[Item.shoot] > 0;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 shootVelocity = velocity;
            Vector2 shootDirection = shootVelocity.SafeNormalize(Vector2.UnitX * player.direction);
            Projectile.NewProjectile(source, position, shootDirection, ModContent.ProjectileType<BrimstoneFuryReHold>(), damage, knockback, player.whoAmI);
            return false;
        }

        //public override Vector2? HoldoutOffset() => new Vector2(0, 10);
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            //recipe.AddIngredient(ItemID.Spear, 1);
            recipe.AddIngredient<BrimstoneFury>();
            //recipe.AddCondition(Condition.BirthdayParty);
            //recipe.AddTile(TileType<DraedonsForge>());
            //recipe.AddTile(TileID.MythrilAnvil);
            recipe.Register();
        }
    }
}
