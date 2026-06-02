using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.A_Drill;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.B_Flamethrower;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.C_Warships;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.D_Laser;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.E_TyrantPrism;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal
{
    public class NewLegendYharimsCrystal : ModItem, ILocalizedModType
    {
        //public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YharimsCrystal";
        public new string LocalizationCategory => "Items.Weapons";
        private BalanceYharimsCrystal damageBalance = new();

        private static int VipType => ModContent.ProjectileType<YC_EX_VIP>();
        private static int MainHoldoutType => ModContent.ProjectileType<YC_TyrantPrismHoldout>();

        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 44;
            Item.damage = 30;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 6;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 2f;
            Item.channel = true;
            Item.autoReuse = true;
            Item.shoot = MainHoldoutType;
            Item.shootSpeed = 30f;
            Item.UseSound = null;
            Item.value = Item.sellPrice(0, 20);
            Item.rare = ItemRarityID.Red;
        }

        public override bool CanUseItem(Player player)
        {
            if (HasActiveVIP(player))
                return false;

            if (player.altFunctionUse == 2)
                return false;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.shootSpeed = 30f;
            Item.shoot = MainHoldoutType;
            Item.UseSound = null;

            return !HasAnyActiveMainHoldout(player) && base.CanUseItem(player);
        }

        public override bool CanShoot(Player player)
        {
            if (player.altFunctionUse == 2 || HasActiveVIP(player))
                return false;

            return !HasAnyActiveMainHoldout(player);
        }

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;

            YCEXPlayer exPlayer = player.GetModPlayer<YCEXPlayer>();
            SyncCooldownDisplay(player, exPlayer);

            if (Main.myPlayer == player.whoAmI &&
                KeybindSystem.LegendarySkill.JustPressed &&
                player.GetModPlayer<global::CalamityLegendsComeBack.Accssory.LegendaryEmblemPlayer>().EXAccessoryEquipped &&
                exPlayer.CanActivateUltimate &&
                !HasActiveVIP(player))
            {
                KillOwnedCrystalHoldouts(player);

                Vector2 aimDirection = (player.Calamity().mouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
                Projectile.NewProjectile(
                    Item.GetSource_FromThis(),
                    player.Center,
                    aimDirection,
                    VipType,
                    player.GetWeaponDamage(Item),
                    Item.knockBack,
                    player.whoAmI);

                SoundEngine.PlaySound(SoundID.Item119 with { Volume = 0.85f, Pitch = -0.1f }, player.Center);
            }

            if (HasActiveVIP(player))
                return;

            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2 || HasActiveVIP(player) || HasAnyActiveMainHoldout(player))
                return false;

            Vector2 aimDirection = (player.Calamity().mouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);

            Projectile.NewProjectile(
                source,
                player.MountedCenter,
                aimDirection,
                MainHoldoutType,
                damage,
                knockback,
                player.whoAmI);

            return false;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = damageBalance.GetLeftClickBaseDamage();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string modeInfo = this.GetLocalizedValue("PrismReworkInfo");
            string commandInfo = this.GetLocalizedValue("RightCommandInfo");
            string resetInfo = this.GetLocalizedValue("ResetInfo");
            string exInfo = this.GetLocalizedValue("EXInfo");

            tooltips.FindAndReplace("[GFB]", modeInfo + "\n" + commandInfo + "\n" + resetInfo + "\n\n" + exInfo);
        }

        private static bool HasAnyActiveMainHoldout(Player player)
        {
            return player.ownedProjectileCounts[MainHoldoutType] > 0 ||
                   player.ownedProjectileCounts[ModContent.ProjectileType<YC_DrillHoldout>()] > 0 ||
                   player.ownedProjectileCounts[ModContent.ProjectileType<YC_FlamethrowerHoldout>()] > 0 ||
                   player.ownedProjectileCounts[ModContent.ProjectileType<YC_WarshipHoldout>()] > 0 ||
                   player.ownedProjectileCounts[ModContent.ProjectileType<YC_HelixLaserHoldout>()] > 0;
        }

        private static bool IsMainHoldoutType(int projectileType)
        {
            return projectileType == MainHoldoutType ||
                   projectileType == ModContent.ProjectileType<YC_DrillHoldout>() ||
                   projectileType == ModContent.ProjectileType<YC_FlamethrowerHoldout>() ||
                   projectileType == ModContent.ProjectileType<YC_WarshipHoldout>() ||
                   projectileType == ModContent.ProjectileType<YC_HelixLaserHoldout>();
        }

        private static bool HasActiveVIP(Player player) => player.ownedProjectileCounts[VipType] > 0;

        private static void KillOwnedCrystalHoldouts(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI)
                    continue;

                if (IsMainHoldoutType(projectile.type))
                    projectile.Kill();
            }
        }

        private static void SyncCooldownDisplay(Player player, YCEXPlayer exPlayer)
        {
            if (player.Calamity().cooldowns.TryGetValue(YCEXCoolDown.ID, out var cooldown))
            {
                cooldown.timeLeft = exPlayer.DisplayRawValue;
            }
            else
            {
                player.AddCooldown(YCEXCoolDown.ID, 0);
            }
        }
    }
}
