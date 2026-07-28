using CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Holdout;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Rarities;
using CalamityMod.Systems.Collections;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper
{
    /// <summary>
    /// 以太之低语 / Aether's Whisper —— 由原版以太之低语重铸的微光魔法重炮（传奇重做）。
    /// 左键：长按压缩微光，松开发射一发有蓄势与后坐的坍缩炮。
    /// 右键：四连折射伪激光扫射，每束借墙反射一次、终点分解为回收晶片。
    /// 采用「嘉登军械库」持械范式：物品本身不攻击，只在手上时生成唯一的常驻持械弹幕
    /// （<see cref="AethersWhisperHoldout"/>），由它读鼠标做左右键——因此切武器 / 滚轮不会卡手。
    /// </summary>
    public class AethersWhisper : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Magic";
        public override string Texture => "CalamityMod/Items/Weapons/Magic/AethersWhisper";

        private static int HoldoutType => ModContent.ProjectileType<AethersWhisperHoldout>();

        public override void SetStaticDefaults()
        {
            CalamityItemSets.ExtraDebuffTooltip_Enemy[Type] = [ModContent.BuffType<Shadowflame>()];
        }

        public override void SetDefaults()
        {
            Item.width = 134;
            Item.height = 44;
            Item.damage = AethersWhisperBalance.BaseDamage;
            Item.knockBack = AethersWhisperBalance.KnockBack;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 0; // 魔力由持械弹幕在真正放出攻击时手动扣除
            Item.useAnimation = Item.useTime = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.UseSound = null;
            Item.shoot = HoldoutType;
            Item.shootSpeed = 1f;
            Item.value = CalamityGlobalItem.RarityTurquoiseBuyPrice;
            Item.rare = ModContent.RarityType<Turquoise>();
        }

        public override Vector2? HoldoutOffset() => new Vector2(-10, 0);

        // 攻击完全交给持械弹幕；物品自身不进入 use 状态，避免占用 itemAnimation 卡住滚轮/切换。
        public override bool CanUseItem(Player player) => false;
        public override bool CanShoot(Player player) => false;

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            if (Main.myPlayer == player.whoAmI && player.ownedProjectileCounts[HoldoutType] <= 0)
            {
                Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
                Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter, aim,
                    HoldoutType, player.GetWeaponDamage(Item), Item.knockBack, player.whoAmI);
            }
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
