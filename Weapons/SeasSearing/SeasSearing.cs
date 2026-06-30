using CalamityLegendsComeBack.Accssory;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    public class SeasSearing : ModItem, ILocalizedModType
    {
        private static int HoldoutType => ModContent.ProjectileType<SeasSearingHoldout>();

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SeasSearing/NewLegendSeasSearing";

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width        = 74;
            Item.height       = 34;
            Item.damage       = SS_Balance.GetInitialBaseDamage();
            Item.DamageType   = DamageClass.Ranged;
            Item.useTime      = 2;
            Item.useAnimation = 2;
            Item.useStyle     = ItemUseStyleID.Shoot;
            Item.noUseGraphic = true;
            Item.noMelee      = true;
            Item.channel      = true;
            Item.autoReuse    = true;
            Item.knockBack    = 5f;
            Item.UseSound     = null;
            Item.shoot        = HoldoutType;
            Item.shootSpeed   = 34f;
            Item.useAmmo      = AmmoID.Bullet;   // 允许读取子弹信息（CanShoot=false 阻止自动消耗）
            Item.value        = CalamityGlobalItem.RarityTurquoiseBuyPrice;
            Item.rare         = ModContent.RarityType<Turquoise>();
        }

        public override bool AltFunctionUse(Player player) => true;

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base += SS_Balance.GetBaseDamage() - Item.damage;
        }

        public override bool CanUseItem(Player player)  => false;
        public override bool CanShoot(Player player)    => false;
        public override bool ConsumeItem(Player player) => false;

        public override void HoldItem(Player player)
        {
            player.GetModPlayer<SeasSearingPlayer>().SetHoldingSeasSearing();

            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            if (Main.myPlayer != player.whoAmI || HasActiveHoldout(player))
                return;

            Vector2 aimDirection = (GetMouseWorld(player) - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            int holdoutIndex = Projectile.NewProjectile(
                player.GetSource_ItemUse(Item),
                player.MountedCenter, aimDirection,
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
            int stage = SS_Balance.GetLeftClickStage();
            string keyText  = KeybindSystem.LegendarySkill.GetAssignedKeys().FirstOrDefault() ?? "Unbound";
            string intro    = this.GetLocalizedValue("Intro");
            string left     = this.GetLocalizedValue($"LeftClick_Stage{stage}");
            string right    = this.GetLocalizedValue("RightClick");
            string passive  = this.GetLocalizedValue("Passive");
            string ultimate = string.Format(this.GetLocalizedValue("Ultimate"), keyText);
            bool   shifted  = Main.keyState.PressingShift();
            string legendary = shifted ? this.GetLocalizedValue("LegendaryText") : this.GetLocalizedValue("LegendaryHint");

            string finalText = intro + "\n\n" + left + "\n" + right + "\n" + passive + "\n" + ultimate + "\n";

            if (shifted)
                tooltips.RemoveAll(t => t.Text == "[GFB]");
            else
                tooltips.FindAndReplace("[GFB]", finalText);
            tooltips.Add(new TooltipLine(Mod, "SeasSearingAbyssalPollutionLegendaryText", legendary));
        }

        public override void AddRecipes()
        {
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity) ||
                !calamity.TryFind("SeasSearing", out ModItem originalSeaSearing))
                return;

            CreateRecipe()
                .AddIngredient(originalSeaSearing.Type)
                .AddIngredient<DepthCells>(25)
                .AddIngredient<Lumenyl>(18)
                .AddIngredient<InfectedArmorPlating>(12)
                .AddIngredient(ItemID.IllegalGunParts)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

        internal static bool CanUseWorldInput(Player player)
        {
            if (player.noItems || player.CCed || Main.mapFullscreen || player.mouseInterface) return false;
            if (Main.blockMouse) return false;
            if (Main.playerInventory && !Main.HoverItem.IsAir) return false;
            return true;
        }

        internal static Vector2 GetMouseWorld(Player player)
        {
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }

        private static bool HasActiveHoldout(Player player)
        {
            int holdoutType = HoldoutType;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == holdoutType)
                    return true;
            }
            return false;
        }
    }
}
