using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.Combo;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.LeftClick;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.RightClick;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Rarities;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir
{
    /// <summary>
    /// 冥蚀天底 / Umbral Nadir —— 由原版天底重铸的黑绿虚空长枪。
    /// 左键：上劈 → 下劈 → 冲刺贯穿 的三段优雅近战连招，命中释放冥融虚空核。
    /// 右键：三连虚空投矛，扎入敌人后持续引发连锁虚空爆裂。
    /// 无被动、无大招；左右键互斥，摁住其一时另一键无效。
    /// </summary>
    public class UmbralNadir : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Melee";

        // 左键单段挥砍的基础用时（会被攻速修正）。
        private const int LeftUseTime = 26;

        public override void SetStaticDefaults()
        {
            // 禁用全局攻速加成，让连招节奏由武器自身掌控。
            ItemID.Sets.BonusAttackSpeedMultiplier[Item.type] = 0f;
            // 不使用长按右键连发：一次右键点击生成一个投掷控制器，控制器自身读取 mouseRight 续期成轮。
        }

        public override void SetDefaults()
        {
            Item.width = 144;
            Item.height = 144;
            Item.damage = UmbralNadirBalance.GetInitialLeftDamage(); // 由 ModifyWeaponDamage 动态重定基到当前阶段
            Item.knockBack = 8.5f;
            Item.DamageType = DamageClass.Melee;

            Item.useAnimation = Item.useTime = LeftUseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.channel = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useTurn = false;
            Item.UseSound = null;

            Item.shoot = ModContent.ProjectileType<UmbralNadirHoldout>();
            Item.shootSpeed = 1f;

            Item.value = CalamityGlobalItem.RarityVioletBuyPrice;
            Item.rare = ModContent.RarityType<BurnishedAuric>();
            Item.Calamity().donorItem = true;
        }

        public override bool AltFunctionUse(Player player) => true;

        // 左键伤害随 Boss 进程动态重定基到当前阶段（小传奇成长曲线）。
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base += UmbralNadirBalance.GetLeftBaseDamage() - Item.damage;
        }

        public override bool CanUseItem(Player player)
        {
            bool leftHoldoutActive = HasActiveProjectile(player, ModContent.ProjectileType<UmbralNadirHoldout>());
            bool throwControllerActive = HasActiveProjectile(player, ModContent.ProjectileType<UmbralNadirThrowController>());
            bool spinActive = HasActiveProjectile(player, ModContent.ProjectileType<UmbralNadirSpinHoldout>());
            bool isLocal = Main.myPlayer == player.whoAmI;

            // ===== 右键 =====
            // 单独右键 = 投矛；但"按住左键期间再按右键"属于回旋（由左键 holdout 检测 mouseRight 自行切换），
            // 所以此处：正按住左键时右键不生成投掷控制器，把这一输入让给回旋逻辑。
            if (player.altFunctionUse == 2)
            {
                if (spinActive || throwControllerActive)
                    return false;
                if (isLocal && Main.mouseLeft)
                    return false;

                Item.useStyle = ItemUseStyleID.Shoot;
                Item.useTime = Item.useAnimation = 8;
                Item.channel = false;
                Item.autoReuse = false; // 一次点击一个控制器，长按由控制器自身维持成轮
                Item.shoot = ModContent.ProjectileType<UmbralNadirThrowController>();
                Item.UseSound = null;
                return true;
            }

            // ===== 左键：近战连招 =====
            if (spinActive || throwControllerActive || leftHoldoutActive)
                return false;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = Item.useAnimation = LeftUseTime;
            Item.channel = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<UmbralNadirHoldout>();
            Item.UseSound = null;
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                // 单独右键：生成三连投掷控制器（独立的右键基础伤害，含玩家近战加成，与左键解耦）
                if (!HasActiveProjectile(player, ModContent.ProjectileType<UmbralNadirThrowController>()))
                {
                    int rightDamage = Math.Max(1, (int)player.GetTotalDamage(Item.DamageType).ApplyTo(UmbralNadirBalance.GetRightBaseDamage()));
                    Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero,
                        ModContent.ProjectileType<UmbralNadirThrowController>(), rightDamage, knockback, player.whoAmI);
                }
                return false;
            }

            // 左键：生成近战连招 holdout（回旋切换由 holdout 内部检测右键完成）
            if (!HasActiveProjectile(player, ModContent.ProjectileType<UmbralNadirHoldout>()))
            {
                Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero,
                    ModContent.ProjectileType<UmbralNadirHoldout>(), damage, knockback, player.whoAmI);
            }
            return false;
        }

        private static bool HasActiveProjectile(Player player, int projectileType)
        {
            if (player.ownedProjectileCounts[projectileType] <= 0)
                return false;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == player.whoAmI && proj.type == projectileType)
                    return true;
            }
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<CalamityMod.Items.Weapons.Melee.Nadir>().
                AddIngredient<AuricBar>(5).
                AddIngredient<DarksunFragment>(8).
                AddIngredient<TwistingNether>(5).
                AddTile<CosmicAnvil>().
                Register();
        }
    }
}
