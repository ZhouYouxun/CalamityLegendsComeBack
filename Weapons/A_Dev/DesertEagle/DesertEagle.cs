using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot;
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
        public virtual int SilverVolleyDamage => 200;
        public virtual int LifeRoundDamage => 1981;
        public virtual float HoldoutSpinContactDamageMultiplier => 0.31f;
        public virtual float HoldoutFullChargeRoundDamageMultiplier => 22.0f;
        // Variant hooks: old Eagles keep their own rounds while sharing the input and holdout framework.
        // A non-positive primary type deliberately preserves the ammo-selected projectile for the base weapon.
        public virtual int PrimaryVolleyProjectileType => -1;
        public virtual int LifeRoundProjectileType => ModContent.ProjectileType<DesertEagleLifeRound>();
        public virtual int ChargedRoundProjectileType => ModContent.ProjectileType<DesertEagleHeavyRound>();
        public virtual bool UsesSilverVolleyVisuals => true;
        public virtual string DesertEagleTextureAssetPath => TextureAssetPath;
        public virtual bool HasDesertEaglePrimaryFire => true;
        public virtual bool HasDesertEagleSpin => true;

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => DesertEagleTextureAssetPath;

        internal static int SharedHoldoutType => ModContent.ProjectileType<DesertEagleHoldout>();
        protected virtual int HoldoutType => SharedHoldoutType;
        internal static readonly SoundStyle DeltaForceDesertEagleUnsuppressedSound = new("CalamityLegendsComeBack/Sound/Other/DeltaForce/沙漠之鹰无消音");
        internal static readonly SoundStyle DeltaForceDesertEagleSuppressedSound = new("CalamityLegendsComeBack/Sound/Other/DeltaForce/沙漠之鹰有消音");
        internal static readonly SoundStyle DeltaForceSvdMarksmanRifleSound = new("CalamityLegendsComeBack/Sound/Other/DeltaForce/Svd射手步枪");

        public override void SetStaticDefaults()
        {
            if (HasDesertEagleSpin)
                ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 82;
            Item.height = 46;
            Item.damage = LifeRoundDamage;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 12;
            Item.useAnimation = 12;
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

        public override bool AltFunctionUse(Player player) => HasDesertEagleSpin;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
                return false;

            if (!HasDesertEaglePrimaryFire)
                return false;

            if (player.ownedProjectileCounts[HoldoutType] > 0)
                return false;

            // 300 RPM 恒定射速
            Item.damage = LifeRoundDamage;
            Item.useTime = 12;
            Item.useAnimation = 12;
            Item.UseSound = null;
            Item.shootSpeed = 18f;

            return base.CanUseItem(player);
        }

        public override void HoldItem(Player player)
        {
            DesertEaglePlayer eaglePlayer = player.GetModPlayer<DesertEaglePlayer>();
            Item.damage = LifeRoundDamage;

            if (!HasDesertEagleSpin)
            {
                Item.noUseGraphic = false;
                return;
            }

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
            if (!ItemHasDesertEagleSpin(player.HeldItem))
                return false;

            DesertEaglePlayer eaglePlayer = player.GetModPlayer<DesertEaglePlayer>();

            return player.ownedProjectileCounts[SharedHoldoutType] > 0 ||
                (Main.myPlayer == player.whoAmI && eaglePlayer.TrackingRightPress);
        }

        public override void UpdateInventory(Player player)
        {
            DesertEaglePlayer eaglePlayer = player.GetModPlayer<DesertEaglePlayer>();
            bool trackingRightClick = Main.myPlayer == player.whoAmI && eaglePlayer.TrackingRightPress;

            if (!HasDesertEagleSpin || player.ownedProjectileCounts[HoldoutType] <= 0 && !trackingRightClick)
                Item.noUseGraphic = false;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!HasDesertEaglePrimaryFire)
                return false;

            DesertEagleSlotPlayer slotPlayer = player.GetModPlayer<DesertEagleSlotPlayer>();
            DEBulletRule rule = DEBulletRegistry.GetRule(slotPlayer.SlottedGunType);

            Vector2 muzzleDirection = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 muzzlePosition = player.MountedCenter + muzzleDirection * 24f;

            SoundEngine.PlaySound(DeltaForceDesertEagleUnsuppressedSound with { Volume = 1f, PitchVariance = 0.03f }, player.Center);
            DesertEagleSilverGlobalProjectile.SpawnSilverMuzzleFlash(muzzlePosition, muzzleDirection, 1f);

            float shotExtra = rule.GetShotExtra(slotPlayer);
            int bulletDamage = (int)(GetConfiguredWeaponDamage(player, LifeRoundDamage) * rule.DamageMultiplier);

            int projectileIndex = Projectile.NewProjectile(
                source,
                muzzlePosition,
                muzzleDirection * 18f,
                ModContent.ProjectileType<DELeftBullet>(),
                bulletDamage,
                knockback,
                player.whoAmI,
                slotPlayer.SlottedGunType,
                shotExtra);
            if (Main.projectile.IndexInRange(projectileIndex))
                Main.projectile[projectileIndex].GetGlobalProjectile<DesertEagleSilverGlobalProjectile>().SilverMarked = true;

            player.velocity -= muzzleDirection * 0.3f;
            return false;
        }

        public virtual int GetConfiguredWeaponDamage(Player player, int baseDamage)
        {
            int originalDamage = Item.damage;
            Item.damage = baseDamage;
            int adjustedDamage = player.GetWeaponDamage(Item);
            Item.damage = originalDamage;

            return Math.Max(1, adjustedDamage);
        }

        internal static bool ItemHasDesertEagleSpin(Item item) =>
            item?.ModItem is DesertEagle weapon && weapon.HasDesertEagleSpin;

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
