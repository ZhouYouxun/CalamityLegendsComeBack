using System;
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
            // 允许长按右键持续触发（三连投掷靠这个自动续期控制器）。
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
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
            bool isLocal = Main.myPlayer == player.whoAmI;

            // ===== 右键：三连投掷（优先级始终高于左键）=====
            if (player.altFunctionUse == 2)
            {
                // 右键始终可用，不受左键阻挡；触发时会在 Shoot 里接管并中断左键。
                // 仅避免重复生成控制器。
                if (throwControllerActive)
                    return false;

                Item.useStyle = ItemUseStyleID.Shoot;
                Item.useTime = Item.useAnimation = 8;
                Item.channel = false;
                Item.autoReuse = true;
                Item.shoot = ModContent.ProjectileType<UmbralNadirThrowController>();
                Item.UseSound = null;
                return true;
            }

            // ===== 左键：近战连招（右键优先，故左键让位于右键）=====
            // 右键投掷进行中、或此刻正摁着右键，左键都不生效。
            if (throwControllerActive)
                return false;
            if (isLocal && Main.mouseRight)
                return false;
            if (leftHoldoutActive)
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
                // 右键接管：中断正在进行的左键连招（右键优先）
                CancelLeftHoldout(player);

                if (!HasActiveProjectile(player, ModContent.ProjectileType<UmbralNadirThrowController>()))
                {
                    // 右键使用独立的右键基础伤害（含玩家近战加成），与左键解耦
                    int rightDamage = Math.Max(1, (int)player.GetTotalDamage(Item.DamageType).ApplyTo(UmbralNadirBalance.GetRightBaseDamage()));
                    Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero,
                        ModContent.ProjectileType<UmbralNadirThrowController>(), rightDamage, knockback, player.whoAmI);
                }
                return false;
            }

            if (!HasActiveProjectile(player, ModContent.ProjectileType<UmbralNadirHoldout>()))
            {
                Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero,
                    ModContent.ProjectileType<UmbralNadirHoldout>(), damage, knockback, player.whoAmI);
            }
            return false;
        }

        private static void CancelLeftHoldout(Player player)
        {
            int holdoutType = ModContent.ProjectileType<UmbralNadirHoldout>();
            if (player.ownedProjectileCounts[holdoutType] <= 0)
                return;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == player.whoAmI && proj.type == holdoutType)
                    proj.Kill();
            }
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
