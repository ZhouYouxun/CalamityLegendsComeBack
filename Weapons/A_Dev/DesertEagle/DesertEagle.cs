using System;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle;
using CalamityMod;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle
{
    public class DesertEagle : ModItem, ILocalizedModType
    {
        public const string TextureAssetPath = "CalamityLegendsComeBack/Weapons/A_Dev/DesertEagle/沙漠之鹰的贴图，用这个改名";

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => TextureAssetPath;

        private static int HoldoutType => ModContent.ProjectileType<DesertEagleHoldout>();

        public override void SetDefaults()
        {
            Item.width = 82;
            Item.height = 46;
            Item.damage = 186;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 9;
            Item.useAnimation = 9;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 3f;
            Item.autoReuse = true;
            Item.shootSpeed = 16f;
            Item.shoot = ProjectileID.Bullet;
            Item.useAmmo = AmmoID.Bullet;
            Item.UseSound = SoundID.Item41 with { Volume = 0.8f, Pitch = -0.12f };
            Item.value = Item.sellPrice(0, 14);
            Item.rare = ItemRarityID.Lime;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            DesertEaglePlayer eaglePlayer = player.GetModPlayer<DesertEaglePlayer>();

            if (player.altFunctionUse == 2)
                return false;

            if (player.ownedProjectileCounts[HoldoutType] > 0)
                return false;

            if (!eaglePlayer.CanUsePrimaryFire())
                return false;

            if (eaglePlayer.PendingLifeRound)
            {
                Item.useTime = 16;
                Item.useAnimation = 16;
                Item.UseSound = null;
                Item.shootSpeed = 18f;
            }
            else
            {
                Item.useTime = 9;
                Item.useAnimation = 9;
                Item.UseSound = SoundID.Item41 with { Volume = 0.8f, Pitch = -0.12f };
                Item.shootSpeed = 16f;
            }

            return base.CanUseItem(player);
        }

        public override void HoldItem(Player player)
        {
            DesertEaglePlayer eaglePlayer = player.GetModPlayer<DesertEaglePlayer>();
            eaglePlayer.SetHoldingDesertEagle();

            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            eaglePlayer.ProcessRightClickState();
            bool validRightInput =
                Main.myPlayer == player.whoAmI &&
                (player.Calamity().mouseRight || Main.mouseRight) &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !player.mouseInterface &&
                !(Main.playerInventory && Main.HoverItem.type == Item.type);

            bool hideHeldItemForRightClick =
                ShouldHideHeldItem(player) ||
                validRightInput;

            Item.noUseGraphic = hideHeldItemForRightClick;

            if (hideHeldItemForRightClick)
            {
                player.itemTime = 0;
                player.itemAnimation = 0;
                player.itemRotation = 0f;
            }

            player.heldProj = hideHeldItemForRightClick ? -1 : player.heldProj;
            
 
            if (Main.myPlayer == player.whoAmI &&
                validRightInput &&
                player.ownedProjectileCounts[HoldoutType] <= 0)
            {
                Vector2 shootDirection = (player.Calamity().mouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
                Projectile.NewProjectile(
                    Item.GetSource_FromThis(),
                    player.Center,
                    shootDirection,
                    HoldoutType,
                    player.GetWeaponDamage(Item),
                    Item.knockBack,
                    player.whoAmI);
            }
        }
        private static bool ShouldHideHeldItem(Player player)
        {
            DesertEaglePlayer eaglePlayer = player.GetModPlayer<DesertEaglePlayer>();

            return player.ownedProjectileCounts[HoldoutType] > 0 ||
                (Main.myPlayer == player.whoAmI && eaglePlayer.TrackingRightPress);
        }

        public override void UpdateInventory(Player player)
        {
            DesertEaglePlayer eaglePlayer = player.GetModPlayer<DesertEaglePlayer>();
            bool trackingRightClick = Main.myPlayer == player.whoAmI && eaglePlayer.TrackingRightPress;

            if (player.ownedProjectileCounts[HoldoutType] <= 0 && !trackingRightClick)
                Item.noUseGraphic = false;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            DesertEaglePlayer eaglePlayer = player.GetModPlayer<DesertEaglePlayer>();
            Vector2 muzzleDirection = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 muzzlePosition = player.MountedCenter + muzzleDirection * 24f;

            if (eaglePlayer.PendingLifeRound)
            {
                SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.95f, Pitch = -0.1f }, player.Center);
                DesertEagleSilverGlobalProjectile.SpawnSilverMuzzleFlash(muzzlePosition, muzzleDirection, 1.15f);

                Projectile.NewProjectile(
                    source,
                    muzzlePosition,
                    muzzleDirection * 18f,
                    ModContent.ProjectileType<DesertEagleLifeRound>(),
                    (int)(damage * 1.8f),
                    knockback * 1.35f,
                    player.whoAmI,
                    0f,
                    1.4f);

                eaglePlayer.ConsumeLifeRound();
                return false;
            }

            SoundEngine.PlaySound(SoundID.Item41 with { Volume = 0.9f, Pitch = -0.18f + Main.rand.NextFloat(-0.04f, 0.04f) }, player.Center);
            DesertEagleSilverGlobalProjectile.SpawnSilverMuzzleFlash(muzzlePosition, muzzleDirection, 0.85f);

            for (int shot = 0; shot < 4; shot++)
            {
                float spread = shot switch
                {
                    0 => -0.08f,
                    1 => -0.025f,
                    2 => 0.025f,
                    _ => 0.08f
                };

                Vector2 shotVelocity = velocity.RotatedBy(spread + Main.rand.NextFloat(-0.025f, 0.025f)) * Main.rand.NextFloat(0.92f, 1.08f);
                int projectileIndex = Projectile.NewProjectile(source, muzzlePosition + Main.rand.NextVector2Circular(2f, 2f), shotVelocity, type, (int)(damage * 0.42f), knockback, player.whoAmI);

                if (Main.projectile.IndexInRange(projectileIndex))
                    Main.projectile[projectileIndex].GetGlobalProjectile<DesertEagleSilverGlobalProjectile>().SilverMarked = true;
            }

            player.velocity -= muzzleDirection * 0.55f;
            eaglePlayer.RegisterSilverVolley();
            return false;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (ShouldHideHeldItem(player))
            {
                player.itemRotation = 0f;
                return;
            }

            player.ChangeDir(Math.Sign((player.Calamity().mouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 7f;
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = new Vector2(-22f, 5f);

            CalamityUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player)
        {
            if (player.ownedProjectileCounts[HoldoutType] > 0)
                return;

            player.ChangeDir(Math.Sign((player.Calamity().mouseWorld - player.Center).X));

            float animProgress = 0.5f - player.itemTime / (float)Math.Max(1, player.itemTimeMax);
            float rotation = (player.Center - player.Calamity().mouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            if (animProgress < 0.4f)
                rotation += -0.045f * (float)Math.Pow((0.6f - animProgress) / 0.6f, 2f) * player.direction;

            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.PearlGod>())
                .AddIngredient(ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.Hellborn>())
                .AddIngredient<DarksunFragment>(6)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
