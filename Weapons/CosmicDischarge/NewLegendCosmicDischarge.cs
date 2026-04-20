using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class NewLegendCosmicDischarge : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityLegendsComeBack/Weapons/CosmicDischarge/CosmicDischarge";

        private static int ComboHoldoutType => ModContent.ProjectileType<CosmicDischargeComboHoldout>();
        private static int HookType => ModContent.ProjectileType<CosmicDischargeHookHead>();
        private static int KillReadyType => ModContent.ProjectileType<CosmicDischargeKillModeReady>();
        private static int KillSlashType => ModContent.ProjectileType<CosmicDischargeKillModeSlash>();

        public override void SetDefaults()
        {
            Item.width = 50;
            Item.height = 52;
            Item.damage = 450;
            Item.knockBack = 6f;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = null;
            Item.shootSpeed = 18f;
            Item.shoot = ComboHoldoutType;
            Item.value = CalamityGlobalItem.RarityDarkBlueBuyPrice;
            Item.rare = ModContent.RarityType<CosmicPurple>();
        }

        public override bool AltFunctionUse(Player player) => true;
        public override bool MeleePrefix() => true;

        public override bool CanUseItem(Player player)
        {
            bool hasCombo = HasOwnedProjectile(player, ComboHoldoutType, HookType);
            bool hasKillReady = HasOwnedProjectile(player, KillReadyType);
            bool hasKillSlash = HasOwnedProjectile(player, KillSlashType);

            if (player.altFunctionUse == 2)
            {
                if (hasCombo || hasKillSlash || hasKillReady)
                    return false;

                Item.channel = false;
                Item.useTime = Item.useAnimation = 18;
                Item.shoot = KillReadyType;
                Item.shootSpeed = 0f;
                Item.UseSound = null;
            }
            else
            {
                Item.channel = true;
                Item.useTime = Item.useAnimation = 20;
                Item.UseSound = null;
                Item.shootSpeed = 18f;
                Item.shoot = hasKillReady ? KillSlashType : ComboHoldoutType;

                if (hasKillSlash)
                    return false;

                if (!hasKillReady && hasCombo)
                    return false;
            }

            return base.CanUseItem(player);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 aimDirection = CosmicDischargeCommon.GetAimDirection(player, Vector2.UnitX * player.direction);

            if (player.altFunctionUse == 2)
            {
                SoundStyle activateSound = new("CalamityMod/Sounds/Item/DemonSwordKillMode");
                SoundEngine.PlaySound(activateSound with { Volume = 0.75f, Pitch = 0.45f }, player.Center);
                Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, KillReadyType, damage * 2, knockback, player.whoAmI, aimDirection.ToRotation());
                return false;
            }

            if (type == KillSlashType)
            {
                int readyIndex = FindOwnedProjectile(player, KillReadyType);
                if (readyIndex != -1)
                    Main.projectile[readyIndex].Kill();

                Projectile.NewProjectile(source, player.MountedCenter, aimDirection * 20f, type, damage * 3, knockback, player.whoAmI, aimDirection.ToRotation());
                return false;
            }

            Projectile.NewProjectile(source, player.MountedCenter, aimDirection * Item.shootSpeed, type, damage, knockback, player.whoAmI, aimDirection.ToRotation());
            return false;
        }

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;
        }

        public override void AddRecipes()
        {
        }

        private static bool HasOwnedProjectile(Player player, params int[] projectileTypes)
        {
            foreach (Projectile projectile in Main.projectile)
            {
                if (!projectile.active || projectile.owner != player.whoAmI)
                    continue;

                for (int i = 0; i < projectileTypes.Length; i++)
                {
                    if (projectile.type == projectileTypes[i])
                        return true;
                }
            }

            return false;
        }

        private static int FindOwnedProjectile(Player player, int projectileType)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == projectileType)
                    return i;
            }

            return -1;
        }
    }
}
