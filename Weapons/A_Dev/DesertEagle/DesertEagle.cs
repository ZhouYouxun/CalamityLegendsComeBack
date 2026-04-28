using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle;
using CalamityMod;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle
{
    public class DesertEagle : ModItem, ILocalizedModType
    {
        public const string TextureAssetPath = "CalamityLegendsComeBack/Weapons/A_Dev/DesertEagle/沙漠之鹰";

        // Balance knobs kept out of SetDefaults so they are easy to tune while testing.
        public int SilverVolleyDamage => 200;
        public int LifeRoundDamage => 1981;
        public float HoldoutSpinContactDamageMultiplier => 0.31f;
        public float HoldoutFullChargeRoundDamageMultiplier => 22.0f;

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => TextureAssetPath;

        private static int HoldoutType => ModContent.ProjectileType<DesertEagleHoldout>();
        internal static readonly SoundStyle DeltaForceDesertEagleUnsuppressedSound = new("CalamityLegendsComeBack/Sound/Other/DeltaForce/沙漠之鹰无消音");
        internal static readonly SoundStyle DeltaForceDesertEagleSuppressedSound = new("CalamityLegendsComeBack/Sound/Other/DeltaForce/沙漠之鹰有消音");
        internal static readonly SoundStyle DeltaForceSvdMarksmanRifleSound = new("CalamityLegendsComeBack/Sound/Other/DeltaForce/Svd射手步枪");

        public override void SetDefaults()
        {
            Item.width = 82;
            Item.height = 46;
            Item.damage = LifeRoundDamage;
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
            Item.UseSound = null;
            Item.value = Item.sellPrice(0, 14);
            Item.rare = ItemRarityID.Lime;
            Item.Calamity().devItem = true;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            DesertEaglePlayer eaglePlayer = player.GetModPlayer<DesertEaglePlayer>();

            if (player.altFunctionUse == 2)
                return false;

            Item.damage = eaglePlayer.PendingLifeRound ? LifeRoundDamage : SilverVolleyDamage;

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
                Item.UseSound = null;
                Item.shootSpeed = 16f;
            }

            return base.CanUseItem(player);
        }

        public override void HoldItem(Player player)
        {
            DesertEaglePlayer eaglePlayer = player.GetModPlayer<DesertEaglePlayer>();
            eaglePlayer.SetHoldingDesertEagle();
            Item.damage = eaglePlayer.PendingLifeRound ? LifeRoundDamage : SilverVolleyDamage;

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
                int holdoutDamage = GetConfiguredWeaponDamage(player, LifeRoundDamage);

                Projectile.NewProjectile(
                    Item.GetSource_FromThis(),
                    player.Center,
                    shootDirection,
                    HoldoutType,
                    holdoutDamage,
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
                SoundEngine.PlaySound(DeltaForceDesertEagleUnsuppressedSound with { Volume = 1f, Pitch = -0.05f }, player.Center);
                DesertEagleSilverGlobalProjectile.SpawnSilverMuzzleFlash(muzzlePosition, muzzleDirection, 1.15f);
                int lifeRoundDamage = GetConfiguredWeaponDamage(player, LifeRoundDamage);

                Projectile.NewProjectile(
                    source,
                    muzzlePosition,
                    muzzleDirection * 18f,
                    ModContent.ProjectileType<DesertEagleLifeRound>(),
                    lifeRoundDamage,
                    knockback * 1.35f,
                    player.whoAmI,
                    0f,
                    1.4f);

                eaglePlayer.ConsumeLifeRound();
                return false;
            }

            SoundEngine.PlaySound(DeltaForceDesertEagleUnsuppressedSound with { Volume = 0.95f, PitchVariance = 0.04f }, player.Center);
            DesertEagleSilverGlobalProjectile.SpawnSilverMuzzleFlash(muzzlePosition, muzzleDirection, 0.85f);
            int volleyDamage = GetConfiguredWeaponDamage(player, SilverVolleyDamage);

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
                int projectileIndex = Projectile.NewProjectile(source, muzzlePosition + Main.rand.NextVector2Circular(2f, 2f), shotVelocity, type, volleyDamage, knockback, player.whoAmI);

                if (Main.projectile.IndexInRange(projectileIndex))
                    Main.projectile[projectileIndex].GetGlobalProjectile<DesertEagleSilverGlobalProjectile>().SilverMarked = true;
            }

            player.velocity -= muzzleDirection * 0.55f;
            eaglePlayer.RegisterSilverVolley();
            return false;
        }

        public int GetConfiguredWeaponDamage(Player player, int baseDamage)
        {
            int originalDamage = Item.damage;
            Item.damage = baseDamage;
            int adjustedDamage = player.GetWeaponDamage(Item);
            Item.damage = originalDamage;

            return Math.Max(1, adjustedDamage);
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
            Vector2 itemOrigin = new Vector2(-22f, -5f);

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
            CreateRecipe().
                AddIngredient<PearlGod>(1).
                AddIngredient<Hellborn>(1).
                AddIngredient<CosmiliteBar>(8).
                AddIngredient<DarksunFragment>(5).
                AddTile<CosmicAnvil>().
                Register();
        }
    }
}
