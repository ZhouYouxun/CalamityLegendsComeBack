using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.P90
{
    public sealed class NewLegendP90 : ModItem, ILocalizedModType
    {
        internal const int MagazineCapacity = 50;
        internal const int ReloadConsumeCount = 10;
        internal const int ReloadFrames = 20;
        internal const int RollFrames = 30;
        internal const int RollCooldownFrames = 10 * 60;
        internal const int ShockGrenadeCooldownFrames = 90;

        private static int HoldoutType => ModContent.ProjectileType<NewLegendP90Holdout>();

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Upgrade/P90/NewLegendP90";

        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Type] = true;
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 28;
            Item.damage = 18;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 2;
            Item.useAnimation = 2;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.knockBack = 1.5f;
            Item.UseSound = null;
            Item.shoot = HoldoutType;
            Item.shootSpeed = 9f;
            Item.useAmmo = AmmoID.Bullet;
            Item.value = CalamityGlobalItem.RarityOrangeBuyPrice;
            Item.rare = ItemRarityID.Orange;
        }

        public override bool AltFunctionUse(Player player) => true;
        public override bool CanUseItem(Player player) => false;
        public override bool CanShoot(Player player) => false;
        public override bool ConsumeItem(Player player) => false;

        public override void HoldItem(Player player)
        {
            player.GetModPlayer<NewLegendP90Player>().SetHoldingP90();
            player.Calamity().mouseWorldListener = true;

            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            if (Main.myPlayer != player.whoAmI || player.ownedProjectileCounts[HoldoutType] > 0)
                return;

            Vector2 aimDirection = (GetMouseWorld(player) - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            int holdoutIndex = Projectile.NewProjectile(
                player.GetSource_ItemUse(Item),
                player.MountedCenter,
                aimDirection,
                HoldoutType,
                player.GetWeaponDamage(Item),
                Item.knockBack,
                player.whoAmI);

            if (Main.projectile.IndexInRange(holdoutIndex))
                Main.projectile[holdoutIndex].CritChance = player.GetWeaponCrit(Item);
        }

        public override void UpdateInventory(Player player)
        {
            Item.noUseGraphic = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            NewLegendP90Player p90Player = Main.LocalPlayer.GetModPlayer<NewLegendP90Player>();
            string status = string.Format(this.GetLocalizedValue("MagazineStatus"),
                p90Player.Magazine,
                MagazineCapacity);

            string text =
                this.GetLocalizedValue("LeftClick") + "\n" +
                this.GetLocalizedValue("Conversion") + "\n" +
                this.GetLocalizedValue("Reload") + "\n" +
                this.GetLocalizedValue("Roll") + "\n" +
                this.GetLocalizedValue("Shock") + "\n" +
                this.GetLocalizedValue("Mark") + "\n" +
                this.GetLocalizedValue("Modes") + "\n" +
                status;

            tooltips.FindAndReplace("[GFB]", text);
            tooltips.Add(new TooltipLine(Mod, "P90CompactLegendaryText", this.GetLocalizedValue("LegendaryText"))
            {
                OverrideColor = new Color(255, 229, 132)
            });
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<CalamityMod.Items.Weapons.Ranged.P90>()
                .AddIngredient(ItemID.IllegalGunParts)
                .AddIngredient(ItemID.HallowedBar, 8)
                .AddIngredient<EssenceofSunlight>(3)
                .AddTile(TileID.MythrilAnvil)
                .Register();
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

    internal sealed class NewLegendP90Shop : GlobalNPC
    {
        public override void ModifyShop(NPCShop shop)
        {
            if (shop.NpcType == NPCID.ArmsDealer)
                shop.AddWithCustomValue<NewLegendP90>(Item.buyPrice(gold: 20));
        }
    }
}
