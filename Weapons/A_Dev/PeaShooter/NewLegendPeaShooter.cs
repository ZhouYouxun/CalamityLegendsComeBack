using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.PeaShooter
{
    public class NewLegendPeaShooter : ModItem, ILocalizedModType
    {
        public const string TextureAssetPath = "CalamityLegendsComeBack/Weapons/A_Dev/PeaShooter/豌豆炮";

        private static int HoldoutType => ModContent.ProjectileType<PeaShooterHoldout>();
        private static int AccessoryHoldoutType => ModContent.ProjectileType<PeaShooterASSHold>();
        private readonly BalancePeaShooter balance = new();

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => TextureAssetPath;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 28;
            Item.damage = balance.GetBaseDamage();
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 2;
            Item.useAnimation = 2;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.knockBack = 3.2f;
            Item.UseSound = null;
            Item.shoot = HoldoutType;
            Item.shootSpeed = balance.GetShootSpeed();
            Item.value = CalamityGlobalItem.RarityTurquoiseBuyPrice;
            Item.rare = ModContent.RarityType<Turquoise>();
            Item.accessory = true;
            Item.Calamity().devItem = true;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player) => false;

        public override bool CanShoot(Player player) => false;

        public override bool ConsumeItem(Player player) => false;

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.JungleSpores, 3)
                .AddIngredient(ItemID.Vine)
                .AddIngredient(ItemID.RichMahogany, 15)
                .AddTile(TileID.WorkBenches)
                .Register();
        }

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            player.GetModPlayer<PeaShooterPlayer>().SetHoldingPeaShooter();

            if (Main.myPlayer != player.whoAmI || player.ownedProjectileCounts[HoldoutType] > 0)
                return;

            Vector2 direction = (GetMouseWorld(player) - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            int holdoutIndex = Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                player.MountedCenter,
                direction,
                HoldoutType,
                player.GetWeaponDamage(Item),
                Item.knockBack,
                player.whoAmI);

            if (Main.projectile.IndexInRange(holdoutIndex))
                Main.projectile[holdoutIndex].CritChance = player.GetWeaponCrit(Item);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            PeaShooterPlayer peaPlayer = player.GetModPlayer<PeaShooterPlayer>();
            peaPlayer.SetPeaShooterAccessory();

            if (player.dead || Main.myPlayer != player.whoAmI || player.ownedProjectileCounts[AccessoryHoldoutType] > 0)
                return;

            Projectile.NewProjectile(
                player.GetSource_FromThis(),
                player.Center,
                Vector2.Zero,
                AccessoryHoldoutType,
                0,
                Item.knockBack,
                player.whoAmI);
        }

        public override void UpdateInventory(Player player)
        {
            Item.noUseGraphic = true;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base += balance.GetBaseDamage() - Item.damage;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            PeaShooterPlayer peaPlayer = Main.LocalPlayer.GetModPlayer<PeaShooterPlayer>();
            int stageIndex = balance.GetCompletedStageIndex();
            string stageName = BalancePeaShooter.GetStageName(stageIndex);
            PeaShooterFireStyle style = BalancePeaShooter.GetFireStyle(stageIndex);
            string styleName = Language.GetTextValue(BalancePeaShooter.GetFireStyleKey(style));
            string peaName = Language.GetTextValue(BalancePeaShooter.GetPeaNameKey(peaPlayer.SelectedPea));
            string peaEffect = Language.GetTextValue(BalancePeaShooter.GetPeaEffectKey(peaPlayer.SelectedPea));

            string merged =
                string.Format(this.GetLocalizedValue("PeaShooter_Left"), styleName, stageName) + "\n" +
                string.Format(this.GetLocalizedValue("PeaShooter_Right"), peaName) + "\n" +
                string.Format(this.GetLocalizedValue("PeaShooter_Effect"), peaEffect) + "\n" +
                this.GetLocalizedValue("PeaShooter_Accessory") + "\n" +
                this.GetLocalizedValue("PeaShooter_Zombie") + "\n" +
                this.GetLocalizedValue("PeaShooter_Flavor");

            tooltips.FindAndReplace("[GFB]", merged);
        }

        internal static bool CanUseWorldInput(Player player)
        {
            if (player.noItems ||
                player.CCed ||
                Main.mapFullscreen ||
                Main.blockMouse ||
                player.mouseInterface)
            {
                return false;
            }

            if (Main.playerInventory && !Main.HoverItem.IsAir)
                return false;

            return true;
        }

        internal static Vector2 GetMouseWorld(Player player)
        {
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }
    }
}
