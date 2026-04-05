//using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
//using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken;
//using CalamityMod.Items;
//using CalamityMod.Items.Materials;
//using CalamityMod.Rarities;
//using Microsoft.Xna.Framework;
//using System;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace CalamityLegendsComeBack.Weapons.BrinyBaron
//{
//    internal class BBTest : ModItem
//    {
//        // ⭐ 测试用：这里直接改 Tier（1~3）
//        private const int TestTier = 1;

//        public override void SetDefaults()
//        {
//            Item.width = 44;
//            Item.height = 50;
//            Item.damage = 1200;
//            Item.DamageType = DamageClass.Melee;
//            Item.noMelee = true;
//            Item.useTurn = true;
//            Item.noUseGraphic = true;
//            Item.useStyle = ItemUseStyleID.Swing;
//            Item.useTime = Item.useAnimation = 20;
//            Item.knockBack = 8.5f;
//            Item.UseSound = SoundID.Item1;
//            Item.autoReuse = true;
//            Item.value = CalamityGlobalItem.RarityVioletBuyPrice;
//            Item.rare = ModContent.RarityType<BurnishedAuric>();

//            // ❌ 不再直接用 shoot
//            Item.shoot = ModContent.ProjectileType<BrinyBaron_RightClick_Shuriken>();
//            Item.shootSpeed = 33f;
//        }

//        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
//        {
//            // ⭐ 手动发射，并传入 Tier
//            Projectile proj = Projectile.NewProjectileDirect(
//                source,
//                player.Center,
//                velocity,
//                ModContent.ProjectileType<BrinyBaron_RightClick_Shuriken>(),
//                damage,
//                knockback,
//                player.whoAmI
//            );

//            // ⭐ 关键：写入 Tier
//            proj.ai[0] = 2;
//            // ⭐ 强制修改尺寸
//            proj.width = 50;
//            proj.height = 50;

//            return false;
//        }






//    }
//}