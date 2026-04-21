using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using CalamityMod.Projectiles.Melee;
using CalamityMod;
using System.Runtime.InteropServices;
using Terraria.DataStructures;
using CalamityMod.Rarities;
using CalamityMod.CalPlayer;
using Terraria.Localization;

namespace CalamityThrowingSpear.Weapons.NewWeapons.DPreDog.ElementalArkJav
{
    public class ElementalArkJav : ModItem, ILocalizedModType
    {
        public override string Texture => "CalamityThrowingSpear/Weapons/NewWeapons/DPreDog/ElementalArkJav/EAJBlade";
        public new string LocalizationCategory => "NewWeapons.DPreDog";
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        }

        private int attackCounter = 0; // 攻击计数器
        private int slowMotionFrames = 10; // 减速的帧数

        public override bool AltFunctionUse(Player player) => true; // 启用右键功能

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2) // 右键按下
            {
                CalamityPlayer calamityPlayer = player.Calamity();

                // 检查是否启用了 zenithWorld 模式
                if (Main.zenithWorld)
                {
                    // 获取 CalamityPlayer 实例并检查是否吃过任意一种水果[按顺序分别为：沐血柑橘，奇迹之果，污浊云莓，圣佑草莓]
                    if (calamityPlayer.sTangerine || calamityPlayer.mFruit || calamityPlayer.tCloudberry || calamityPlayer.sStrawberry)
                    {
                        // 如果玩家吃过任意一种水果，拒绝右键攻击并显示提示
                        CombatText.NewText(player.getRect(), Microsoft.Xna.Framework.Color.Pink, Language.GetTextValue("Mods.CalamityThrowingSpear.TheSpecialText.ElementalArkJav1"));
                        return false; // 禁止右键攻击
                    }
                }


                // 检查是否启用了 getGoodWorld 模式
                if (Main.getGoodWorld)
                {
                    // 在该模式下不扣除血量，也不检查水果
                    player.AddBuff(BuffID.PotionSickness, 1800); // 1800帧 = 30秒

                    // 设置10帧内移动速度减90%
                    player.GetModPlayer<ElementalArkPlayer>().slowMotionTimer = slowMotionFrames;

                    // 右键攻击设定
                    Item.useTime = Item.useAnimation = 120;
                    Item.useStyle = ItemUseStyleID.Shoot;
                    Item.shoot = ModContent.ProjectileType<ElementalArkJavSUPERPROJ>();
                    Item.shootSpeed = 50f;
                    Item.damage = 168; // 右键攻击的伤害倍率为1倍

                    return base.CanUseItem(player); // 正常执行攻击
                }

                // 否则按照普通模式的逻辑扣除血量和检测水果
                //CalamityPlayer calamityPlayer = player.Calamity();
                int fruitCount = 0;
                if (calamityPlayer.sTangerine) fruitCount++;
                if (calamityPlayer.mFruit) fruitCount++;
                if (calamityPlayer.tCloudberry) fruitCount++;
                if (calamityPlayer.sStrawberry) fruitCount++;
                
                if (fruitCount == 4)
                {
                    player.KillMe(PlayerDeathReason.ByCustomReason(player.name + " " + Language.GetTextValue("Mods.CalamityThrowingSpear.TheSpecialText.ElementalArkJav2")), player.statLifeMax2, 0);
                    return false;
                }

                int baseHealthLoss = 359;
                int additionalLoss = fruitCount * 50;
                int totalHealthLoss = baseHealthLoss + additionalLoss;

                player.statLife -= totalHealthLoss;
                if (player.statLife <= 0)
                {
                    player.KillMe(PlayerDeathReason.ByCustomReason(player.name + " " + Language.GetTextValue("Mods.CalamityThrowingSpear.TheSpecialText.ElementalArkJav3")), totalHealthLoss, 0);
                    return false;
                }

                player.AddBuff(BuffID.PotionSickness, 1800);

                player.GetModPlayer<ElementalArkPlayer>().slowMotionTimer = slowMotionFrames;

                Item.useTime = Item.useAnimation = 120;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.shoot = ModContent.ProjectileType<ElementalArkJavSUPERPROJ>();
                Item.shootSpeed = 50f;
                Item.damage = 168;
            }
            else // 左键五连击
            {
                CalamityPlayer calamityPlayer = player.Calamity();

                int fruitCount = 0;
                if (calamityPlayer.sTangerine) fruitCount++;
                if (calamityPlayer.mFruit) fruitCount++;
                if (calamityPlayer.tCloudberry) fruitCount++;
                if (calamityPlayer.sStrawberry) fruitCount++;

                int healthLoss = fruitCount; // 每种水果扣除1点血量
                player.statLife -= healthLoss;

                Item.useTime = Item.useAnimation = 30;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.damage = 183; // 左键的基础伤害
                Item.shootSpeed = 25f;
                Item.shoot = ModContent.ProjectileType<ElementalArkJavFragments>();
            }
            return base.CanUseItem(player);
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2) // 右键攻击
            {
                Vector2 shootPosition = player.MountedCenter; // 使用玩家的中心位置作为发射位置
                Projectile.NewProjectile(source, shootPosition, velocity, ModContent.ProjectileType<ElementalArkJavSUPERPROJ>(), damage, knockback, player.whoAmI);
                return false;
            }
            //else 
            {
                // 左键五连击攻击逻辑
                attackCounter++;
                switch (attackCounter % 5)
                {
                    case 1:
                        type = ModContent.ProjectileType<ElementalArkJavFragments>();
                        Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
                        break;
                    case 2:
                        type = ModContent.ProjectileType<ElementalArkJavBlade>();
                        int bladeProjUp = Projectile.NewProjectile(source, position, velocity * 3, type, damage, knockback, player.whoAmI);
                        Main.projectile[bladeProjUp].localAI[0] = 1f; // 第1次会直接顺时针偏移前进，逆时针返回
                        break;
                    case 3:
                        type = ModContent.ProjectileType<ElementalArkJavFragments>();
                        Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
                        break;
                    case 4:
                        type = ModContent.ProjectileType<ElementalArkJavBlade>();
                        int bladeProjDown = Projectile.NewProjectile(source, position, velocity * 3, type, damage, knockback, player.whoAmI);
                        Main.projectile[bladeProjDown].localAI[0] = 0f; // 第2次会逆时针偏移前进，顺时针返回
                        break;
                    case 0:
                        // 上回旋的 ElementalArkJavFragments05
                        int projUp = Projectile.NewProjectile(source, position, velocity * 6, ModContent.ProjectileType<ElementalArkJavFragments05>(), damage, knockback, player.whoAmI);
                        Main.projectile[projUp].ai[1] = 1f;
                        Main.projectile[projUp].ModProjectile<ElementalArkJavFragments05>().isCurveUpwards = true;
                        // 下回旋的 ElementalArkJavFragments05
                        int projDown = Projectile.NewProjectile(source, position, velocity * 6, ModContent.ProjectileType<ElementalArkJavFragments05>(), damage, knockback, player.whoAmI);
                        Main.projectile[projDown].ai[1] = 0f;
                        Main.projectile[projDown].ModProjectile<ElementalArkJavFragments05>().isCurveUpwards = false;
                        break;
                }
            }
            return false; // 禁止默认的射击行为
        }


        //public override void AddRecipes()
        //{
        //    Recipe recipe = CreateRecipe();
        //    recipe.AddIngredient<ArkoftheElements>();
        //    recipe.Register();
        //}

        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 50;
            Item.damage = 168;
            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.useTurn = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = Item.useAnimation = 15;
            Item.knockBack = 8.5f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = CalamityGlobalItem.RarityHotPinkBuyPrice;
            Item.rare = ModContent.RarityType<HotPink>();
            Item.Calamity().devItem = true;
            Item.shoot = ModContent.ProjectileType<ElementalArkJavFragments>();
            Item.shootSpeed = 25f;
        }


        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (Main.zenithWorld)
            // 当天顶世界模式被启用时，超大幅度提升伤害
            {
                damage *= 15f;
            }
        }


        //public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        //{
        //    attackCounter++;
        //    switch (attackCounter % 5)
        //    {
        //        case 1:
        //            type = ModContent.ProjectileType<ElementalArkJavFragments>();
        //            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
        //            break;

        //        case 2:
        //            type = ModContent.ProjectileType<ElementalArkJavBlade>();
        //            int bladeProjUp = Projectile.NewProjectile(source, position, (velocity) * 2, type, damage, knockback, player.whoAmI);
        //            Main.projectile[bladeProjUp].localAI[0] = 1f;
        //            break;

        //        case 3:
        //            type = ModContent.ProjectileType<ElementalArkJavFragments>();
        //            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
        //            break;

        //        case 4:
        //            type = ModContent.ProjectileType<ElementalArkJavBlade>();
        //            int bladeProjDown = Projectile.NewProjectile(source, position, (velocity) * 2, type, damage, knockback, player.whoAmI);
        //            Main.projectile[bladeProjDown].localAI[0] = 0f;
        //            break;

        //        case 0:
        //            // 上回旋的 ElementalArkJavFragments05
        //            int projUp = Projectile.NewProjectile(source, position, (velocity) * 3, ModContent.ProjectileType<ElementalArkJavFragments05>(), damage, knockback, player.whoAmI);
        //            Main.projectile[projUp].ai[1] = 1f; // 上回旋标志
        //            Main.projectile[projUp].ModProjectile<ElementalArkJavFragments05>().isCurveUpwards = true;

        //            // 下回旋的 ElementalArkJavBlade05
        //            int projDown = Projectile.NewProjectile(source, position, (velocity) * 3, ModContent.ProjectileType<ElementalArkJavFragments05>(), damage, knockback, player.whoAmI);
        //            Main.projectile[projDown].ai[1] = 0f; // 下回旋标志
        //            Main.projectile[projDown].ModProjectile<ElementalArkJavFragments05>().isCurveUpwards = false;

        //            break;
        //    }

        //    return false;
        //}


    }
}

