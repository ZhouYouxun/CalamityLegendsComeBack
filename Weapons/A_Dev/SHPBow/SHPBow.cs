using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.NPCs.SunkenSea;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Rarities;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.SHPBow
{
    public class SHPBow : ModItem, ILocalizedModType
    {
        public const string TextureAssetPath = "CalamityLegendsComeBack/Weapons/A_Dev/SHPBow/SHPB";

        public override string Texture => TextureAssetPath;
        public new string LocalizationCategory => "Items.Weapons";

        private static int HoldoutType => ModContent.ProjectileType<SHPBowHoldout>();

        public override void SetDefaults()
        {
            Item.width = 68;
            Item.height = 126;
            Item.damage = 58;
            Item.DamageType = DamageClass.Ranged;
            Item.useAnimation = 10;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.knockBack = 3.25f;
            Item.UseSound = null;
            Item.shoot = HoldoutType;
            Item.shootSpeed = 18f;
            Item.useAmmo = AmmoID.Arrow;
            Item.value = CalamityGlobalItem.RarityVioletBuyPrice;
            Item.rare = ModContent.RarityType<BurnishedAuric>();
            Item.Calamity().devItem = true;
        }

        public override bool CanUseItem(Player player) => false;

        public override bool CanShoot(Player player) => false;

        public override bool ConsumeItem(Player player) => false;

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            player.GetModPlayer<SHPBowPlayer>().SetHoldingSHPBow();

            if (Main.myPlayer == player.whoAmI && player.ownedProjectileCounts[HoldoutType] <= 0)
            {
                Projectile.NewProjectile(
                    Item.GetSource_FromThis(),
                    player.Center,
                    Vector2.UnitX * player.direction,
                    HoldoutType,
                    player.GetWeaponDamage(Item),
                    Item.knockBack,
                    player.whoAmI);
            }
        }

        public override void UpdateInventory(Player player)
        {
            if (player.HeldItem.type != Type && player.ownedProjectileCounts[HoldoutType] <= 0)
                Item.noUseGraphic = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            SHPBowPlayer bowPlayer = Main.LocalPlayer.GetModPlayer<SHPBowPlayer>();
            string sequenceText = BuildSequenceText(bowPlayer);

            string merged =
                string.Format(this.GetLocalizedValue("SHPB_CurrentSequence"), sequenceText) + "\n" +
                this.GetLocalizedValue("SHPB_Left") + "\n" +
                this.GetLocalizedValue("SHPB_StackHint") + "\n\n" +
                this.GetLocalizedValue("SHPB_Right") + "\n\n" +
                this.GetLocalizedValue("SHPB_Mode0") + "\n" +
                this.GetLocalizedValue("SHPB_Mode1") + "\n" +
                this.GetLocalizedValue("SHPB_Mode2") + "\n" +
                this.GetLocalizedValue("SHPB_Mode3") + "\n\n" +
                this.GetLocalizedValue("SHPB_EX") + "\n\n" +
                this.GetLocalizedValue("SHPB_Final") + "\n";

            tooltips.FindAndReplace("[GFB]", merged);
        }

        private string BuildSequenceText(SHPBowPlayer bowPlayer)
        {
            StringBuilder builder = new();

            for (int i = 0; i < bowPlayer.SequenceLength; i++)
            {
                if (i > 0)
                    builder.Append(" > ");

                builder.Append(this.GetLocalizedValue($"ModeName{(int)bowPlayer.GetSequenceMode(i)}"));
            }

            return builder.ToString();
        }


        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<PlasmaDriveCore>().
                AddIngredient<SuspiciousScrap>(4).
                AddRecipeGroup("AnyMythrilBar", 10).
                AddIngredient<TitaniumRailgun> (1). // 钛金电磁炮，对应穿透
                AddIngredient<Buzzkill>(1). // 嗡鸣绞轮，对应反弹
                AddIngredient<HolofibreImmolator> (1). // R-PMA纤化焚毁器，对应散射
                AddIngredient<ClamorRifle> (1). // 音波步枪,对应追踪
                AddIngredient(ItemID.SoulofFright, 1).
                AddIngredient(ItemID.SoulofMight, 1).
                AddIngredient(ItemID.SoulofSight, 1).
                AddTile(TileID.MythrilAnvil).
                Register();
        }
    }
}
