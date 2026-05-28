using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.TheEnforcer
{
    public class TheNewEnforcer : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        public override void SetDefaults()
        {
            Item.width = 100;
            Item.height = 100;
            Item.damage = 2600;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = 57;
            Item.useAnimation = 57;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.channel = true;
            Item.knockBack = 7f;
            Item.shoot = ModContent.ProjectileType<TheNewEnforcerHoldOut>();
            Item.shootSpeed = 0f;
            Item.UseSound = null;
            Item.rare = ModContent.RarityType<CosmicPurple>();
            Item.value = Item.sellPrice(platinum: 1);
        }

        public override bool CanUseItem(Player player) => base.CanUseItem(player);

        public override bool CanShoot(Player player) =>
            player.ownedProjectileCounts[ModContent.ProjectileType<TheNewEnforcerHoldOut>()] <= 0;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(
                source,
                player.MountedCenter,
                Vector2.Zero,
                ModContent.ProjectileType<TheNewEnforcerHoldOut>(),
                damage,
                knockback,
                player.whoAmI);

            return false;
        }
    }
}
