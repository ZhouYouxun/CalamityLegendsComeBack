using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Rarities;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.SacredThunder
{
    public class SacredThunder : RogueWeapon, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        public override float StealthDamageMultiplier => 0.85f;
        public override float StealthVelocityMultiplier => 0.65f;

        public override void SetDefaults()
        {
            Item.width = 74;
            Item.height = 74;
            Item.damage = 50000;
            Item.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Item.knockBack = 8f;
            Item.useAnimation = 32;
            Item.useTime = 32;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<SacredThunderPROJ>();
            Item.shootSpeed = 19f;
            Item.value = CalamityGlobalItem.RarityHotPinkBuyPrice;
            Item.rare = ModContent.RarityType<HotPink>();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            bool stealthStrike = player.Calamity().StealthStrikeAvailable();
            int projectileID = Projectile.NewProjectile(
                source,
                position,
                velocity,
                type,
                damage,
                knockback,
                player.whoAmI,
                stealthStrike ? 1f : 0f);

            if (projectileID.WithinBounds(Main.maxProjectiles))
                Main.projectile[projectileID].Calamity().stealthStrike = stealthStrike;

            return false;
        }

        public override void ModifyWeaponCrit(Player player, ref float crit)
        {
            crit += 16f;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.FindAndReplace("[GFB]", this.GetLocalizedValue("LegendaryText"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<StormfrontRazor>()
                .AddIngredient<Exorcism>()
                .AddIngredient<DynamicPursuer>()
                .AddIngredient<Seraphim>()
                .AddIngredient(ItemID.HallowedBar, 114)
                .AddIngredient(ItemID.Wire, 514)
                .AddIngredient<ShadowspecBar>(5)
                .AddTile<DraedonsForge>()
                .Register();
        }
    }
}
