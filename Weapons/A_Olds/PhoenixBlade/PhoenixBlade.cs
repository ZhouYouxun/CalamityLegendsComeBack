using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.PhoenixBlade
{
    public class PhoenixBlade : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        public override void SetDefaults()
        {
            Item.width = 106;
            Item.height = 106;
            Item.damage = 280;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.channel = true;
            Item.knockBack = 6.5f;
            Item.shoot = ModContent.ProjectileType<PhoenixBladeHoldout>();
            Item.shootSpeed = 0f;
            Item.UseSound = null;
            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.sellPrice(gold: 15);
        }

        public override bool CanUseItem(Player player) => base.CanUseItem(player);

        public override bool CanShoot(Player player) =>
            player.ownedProjectileCounts[ModContent.ProjectileType<PhoenixBladeHoldout>()] <= 0;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(
                source,
                player.MountedCenter,
                Vector2.Zero,
                ModContent.ProjectileType<PhoenixBladeHoldout>(),
                damage,
                knockback,
                player.whoAmI);

            return false;
        }
    }
}
