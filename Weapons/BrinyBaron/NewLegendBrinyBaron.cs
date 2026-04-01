using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using CalamityLegendsComeBack.Weapons.BrinyBaron.LeftClick;
using CalamityLegendsComeBack.Weapons.BrinyBaron.POWER;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillB_SpinDash;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
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

            // =========================
            // 左键基础项：对齐 GrandDad 的思路
            // =========================
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.useTurn = true;
            Item.knockBack = 6f;
            Item.autoReuse = true;

            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<BrinyBaron_LeftClick_Swing>();
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.Shoot;

            Item.shootSpeed = 16f;
            Item.rare = ItemRarityID.Pink;
            Item.UseSound = null;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                BBEXPlayer tidePlayer = player.GetModPlayer<BBEXPlayer>();
                Item.useTime = 24;
                Item.useAnimation = 24;
                Item.channel = false;
                Item.noUseGraphic = false;
                Item.noMelee = true;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.shoot = tidePlayer.TideFull
                    ? ModContent.ProjectileType<BrinyBaron_SkillSpinRush_SpinBlade>()
                    : ModContent.ProjectileType<BrinyBaron_SkillDashTornado_BladeDash>();
                Item.shootSpeed = 16f;
                Item.UseSound = SoundID.Item39;
            }
            else
            {
                // =========================
                // 左键：保持和 GrandDad 一样的 Holdout 武器配置
                // =========================
                Item.useTime = 60;
                Item.useAnimation = 60;
                Item.channel = true;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.shoot = ModContent.ProjectileType<BrinyBaron_LeftClick_Swing>();
                Item.shootSpeed = 16f;
                Item.UseSound = null;
            }

            return base.CanUseItem(player);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // =========================
            // 右键：手里剑逻辑保持原样
            // =========================
            if (player.altFunctionUse == 2)
                return true;

            // =========================
            // 左键：永远只允许存在一个 Holdout
            // =========================
            int holdoutType = ModContent.ProjectileType<BrinyBaron_LeftClick_Swing>();
            if (player.ownedProjectileCounts[holdoutType] > 0)
                return false;

            return true;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
        }

        public override void HoldItem(Player player)
        {
            BBEXPlayer tidePlayer = player.GetModPlayer<BBEXPlayer>();

            if (player.Calamity().cooldowns.TryGetValue(BBEXCoolDown.ID, out var cooldown))
                cooldown.timeLeft = tidePlayer.TideValue;
            else
                player.AddCooldown(BBEXCoolDown.ID, tidePlayer.TideValue);
        }

        public override void AddRecipes()
        {
        }
    }
}
