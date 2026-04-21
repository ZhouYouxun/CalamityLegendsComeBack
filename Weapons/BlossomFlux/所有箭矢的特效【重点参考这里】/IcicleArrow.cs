using CalamityMod.Projectiles.Ranged;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Ammo
{
    public class IcicleArrow : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Ammo";
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 99;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 50;
            Item.damage = 6;
            Item.DamageType = DamageClass.Ranged;
            Item.consumable = true;
            Item.knockBack = 2.5f;
            Item.value = Item.buyPrice(copper: 80); // Sold by Archmage
            Item.rare = ItemRarityID.Yellow;
            Item.shoot = ModContent.ProjectileType<IcicleArrowProj>();
            Item.shootSpeed = 0.2f;
            Item.ammo = AmmoID.Arrow;
            Item.maxStack = Item.CommonMaxStack;
        }
    }
}
