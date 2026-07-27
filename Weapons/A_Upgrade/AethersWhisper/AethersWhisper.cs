using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Rarities;
using CalamityMod.Systems.Collections;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper
{
    /// <summary>
    /// 以太之低语 / Aether's Whisper —— 由原版以太之低语重铸的微光魔法重炮（传奇重做）。
    /// 左键：长按压缩微光，松开发射一发有蓄势与后坐的坍缩炮（蓄得越满越粗越重）。
    /// 右键：四连折射伪激光扫射，每束可借墙反射一次、终点分解为两枚晶片并沿镜像弧回收至枪口。
    /// 不做组合键；左右键互斥（右键优先，会中断左键蓄力）。无被动、无充能条、无大招。
    /// 数值以 600 基础伤害为锚点，全部集中于 <see cref="AethersWhisperBalance"/>。
    /// </summary>
    public class AethersWhisper : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Magic";
        public override string Texture => "CalamityMod/Items/Weapons/Magic/AethersWhisper";

        private static int LeftHoldoutType => ModContent.ProjectileType<AethersWhisperHoldout>();
        private static int SweepHoldoutType => ModContent.ProjectileType<AethersWhisperSweepHoldout>();

        public override void SetStaticDefaults()
        {
            // 沿用原武器：敌人受击的暗影焰提示。
            CalamityItemSets.ExtraDebuffTooltip_Enemy[Type] = [ModContent.BuffType<Shadowflame>()];
            // 允许长按右键持续触发（四连扫射靠自动续期的扫射控制器）。
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 134;
            Item.height = 44;
            Item.damage = AethersWhisperBalance.BaseDamage;
            Item.knockBack = AethersWhisperBalance.KnockBack;
            Item.DamageType = DamageClass.Magic;

            // 魔力由左右键的持握弹幕在“真正放出攻击”时手动扣除（见文档 2.2），故此处不设自动扣魔。
            Item.mana = 0;

            Item.useAnimation = Item.useTime = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.channel = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useTurn = false;
            Item.UseSound = null;

            Item.shoot = LeftHoldoutType;
            Item.shootSpeed = 1f;

            Item.value = CalamityGlobalItem.RarityTurquoiseBuyPrice;
            Item.rare = ModContent.RarityType<Turquoise>();
        }

        public override Vector2? HoldoutOffset() => new Vector2(-10, 0);

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            bool leftActive = HasActiveProjectile(player, LeftHoldoutType);
            bool sweepActive = HasActiveProjectile(player, SweepHoldoutType);
            bool isLocal = Main.myPlayer == player.whoAmI;

            // ===== 右键：四连折射扫射（优先级始终高于左键）=====
            if (player.altFunctionUse == 2)
            {
                if (sweepActive)
                    return false; // 已有扫射控制器，避免重复生成

                Item.useStyle = ItemUseStyleID.Shoot;
                Item.useTime = Item.useAnimation = 8;
                Item.channel = false;
                Item.autoReuse = true;
                Item.shoot = SweepHoldoutType;
                return true;
            }

            // ===== 左键：微光坍缩炮（右键优先，故让位于右键）=====
            if (sweepActive)
                return false;
            if (isLocal && Main.mouseRight)
                return false;
            if (leftActive)
                return false;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = Item.useAnimation = 24;
            Item.channel = true;
            Item.autoReuse = true;
            Item.shoot = LeftHoldoutType;
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                // 右键接管：中断正在进行的左键蓄力（右键优先）。
                CancelProjectiles(player, LeftHoldoutType);

                if (!HasActiveProjectile(player, SweepHoldoutType))
                {
                    Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
                    Projectile.NewProjectile(source, player.MountedCenter, aim,
                        SweepHoldoutType, damage, knockback, player.whoAmI);
                }
                return false;
            }

            if (!HasActiveProjectile(player, LeftHoldoutType))
            {
                Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
                Projectile.NewProjectile(source, player.MountedCenter, aim,
                    LeftHoldoutType, damage, knockback, player.whoAmI);
            }
            return false;
        }

        private static void CancelProjectiles(Player player, int projectileType)
        {
            if (player.ownedProjectileCounts[projectileType] <= 0)
                return;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == player.whoAmI && proj.type == projectileType)
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
                AddIngredient<CalamityMod.Items.Weapons.Magic.AethersWhisper>().
                AddIngredient<AuricBar>(5).
                AddIngredient<TwistingNether>(5).
                AddTile<CosmicAnvil>().
                Register();
        }
    }
}
