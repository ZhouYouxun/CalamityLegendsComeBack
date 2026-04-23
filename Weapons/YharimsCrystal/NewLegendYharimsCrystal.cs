using CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft;
using YCRight = CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRight;
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

        private static int LeftHoldoutType => ModContent.ProjectileType<YC_LeftHoldOut>();
        private static int RightHoldoutType => ModContent.ProjectileType<YCRight.YC_RightHoldOut>();
        private static int VipType => ModContent.ProjectileType<YC_EX_VIP>();

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
            Item.shoot = LeftHoldoutType;
            Item.shootSpeed = 30f;
            Item.UseSound = SoundID.Item13;
            Item.value = Item.sellPrice(0, 20);
            Item.rare = ItemRarityID.Red;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (HasActiveVIP(player))
                return false;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.shootSpeed = 30f;

            if (player.altFunctionUse == 2)
            {
                Item.shoot = RightHoldoutType;
                Item.UseSound = null;
            }
            else
            {
                Item.shoot = LeftHoldoutType;
                Item.UseSound = SoundID.Item13;
            }

            return base.CanUseItem(player);
        }

        public override bool CanShoot(Player player)
        {
            if (player.altFunctionUse == 2 || HasActiveVIP(player))
                return false;

            return player.ownedProjectileCounts[LeftHoldoutType] <= 0 &&
                   player.ownedProjectileCounts[RightHoldoutType] <= 0;
        }

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;

            YCEXPlayer exPlayer = player.GetModPlayer<YCEXPlayer>();
            SyncCooldownDisplay(player, exPlayer);

            if (Main.myPlayer == player.whoAmI &&
                KeybindSystem.LegendarySkill.JustPressed &&
                player.GetModPlayer<global::CalamityLegendsComeBack.Accssory.EXPlayer>().EXAccessoryEquipped &&
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

            if (Main.myPlayer != player.whoAmI ||
                !player.Calamity().mouseRight ||
                Main.mapFullscreen ||
                Main.blockMouse ||
                (Main.playerInventory && Main.HoverItem.type == Item.type) ||
                player.ownedProjectileCounts[LeftHoldoutType] > 0 ||
                player.ownedProjectileCounts[RightHoldoutType] > 0)
            {
                return;
            }

            Vector2 rightAimDirection = (player.Calamity().mouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);

            Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                player.MountedCenter,
                rightAimDirection,
                RightHoldoutType,
                player.GetWeaponDamage(Item),
                Item.knockBack,
                player.whoAmI);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2 || HasActiveVIP(player))
                return false;

            if (player.ownedProjectileCounts[LeftHoldoutType] > 0 || player.ownedProjectileCounts[RightHoldoutType] > 0)
                return false;

            return true;
        }

        private static bool HasActiveVIP(Player player) => player.ownedProjectileCounts[VipType] > 0;

        private static void KillOwnedCrystalHoldouts(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI)
                    continue;

                if (projectile.type == LeftHoldoutType || projectile.type == RightHoldoutType)
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
