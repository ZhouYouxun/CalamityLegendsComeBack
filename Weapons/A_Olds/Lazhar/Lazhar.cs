using CalamityMod;
using CalamityMod.Items.Materials;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.Lazhar
{
    /// <summary>
    /// 拉扎尔射线 (Lazhar)
    /// 定位：单体超高伤害魔法武器。
    /// 攻击模式：
    /// - 左键：高速三连发（M16 style）追踪金色激光，对单体目标造成致命的持续伤害。
    /// - 右键：发射一枚雷达锁定信标，雷达锁定击中的敌怪。
    /// 协同效应：
    /// 当攻击被雷达锁定的目标时，左键的拉扎尔射线伤害提升50%，以百分之百的绝对转向力追踪被锁定的敌怪，
    /// 并在每次击中时从太空中降下毁灭性的高能轨道卫星激光轰炸（Orbital Laser Strike），完美实现极速单体融化。
    /// 枪械外观：使用手持弹幕 (LazharHoldout) 呈现极其精致的持枪与开火后坐力、发光外圈以及枪口星芒特效，细节堪比SHPC。
    /// </summary>
    public class Lazhar : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Magic";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Olds/Lazhar/拉扎尔射线";

        public override void SetStaticDefaults()
        {
            // 允许右键连续开火
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            // 基本大小与外观
            Item.width = 72;
            Item.height = 32;

            // 战斗属性 (定位：单体极高输出)
            Item.damage = 560; // 极高单体伤害
            Item.DamageType = DamageClass.Magic;
            Item.mana = 14;
            Item.knockBack = 4f;

            // 施法速度控制：36帧。
            // 使用手持弹幕进行动画控制，真正的射击速度和连发逻辑由 LazharHoldout 协调。
            Item.useTime = 36;
            Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true; // 纯法术射击
            Item.noUseGraphic = true; // 隐藏默认手持贴图，完全由手持弹幕接管
            Item.autoReuse = true;

            // 弹药与弹幕发射
            Item.shoot = ModContent.ProjectileType<LazharHoldout>();
            Item.shootSpeed = 1f;

            // 稀有度与经济平衡 (Calamity Turquoise 稀有度，对应月后级别强度)
            Item.rare = ModContent.RarityType<Turquoise>();
            Item.value = Item.sellPrice(gold: 25);
            Item.Calamity().devItem = false;
        }

        /// <summary>
        /// 允许右键释放锁定信标
        /// </summary>
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        /// <summary>
        /// 限制发射条件：当存在活动的 Lazhar 手持弹幕时，不能再次开火，以确保弹幕生命周期和三连发节奏的连贯性。
        /// </summary>
        public override bool CanShoot(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<LazharHoldout>()] == 0;
        }

        /// <summary>
        /// 接管弹幕发射逻辑，判定是左键的爆发现线，还是右键的雷达锁定信标，并生成对应的 LazharHoldout 实例。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 判定开火模式：
            // ai[0] = 0 代表左键（三连发射线）
            // ai[0] = 1 代表右键（雷达锁定信标）
            int attackMode = player.altFunctionUse == 2 ? 1 : 0;

            // 创建手持弹幕。此弹幕会黏附于玩家，控制朝向，渲染枪体，并在生命周期内按节奏射出真正的攻击性弹幕。
            Projectile.NewProjectile(
                source,
                player.MountedCenter,
                velocity,
                ModContent.ProjectileType<LazharHoldout>(),
                damage,
                knockback,
                player.whoAmI,
                attackMode
            );

            return false; // 返回 false 阻止 Terraria 默认发射机制，完全由我们手写控制
        }

        /// <summary>
        /// 物品合成表设计：融入灾厄科技材料 Dubious Plating (可疑板材)、Mysterious Circuitry (神秘电路)、Lumenyl (流光水晶) 以及神圣锭。
        /// </summary>
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<DubiousPlating>(25)
                .AddIngredient<MysteriousCircuitry>(25)
                .AddIngredient<Lumenyl>(15)
                .AddIngredient(ItemID.HallowedBar, 12)
                .AddIngredient(ItemID.SoulofLight, 15)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
