using CalamityMod.Buffs.Summon;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables.Ores;
using CalamityMod.Projectiles.Summon;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Summon
{
    public class Sirius : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Summon";

        public override void SetStaticDefaults() => ItemID.Sets.StaffMinionSlotsRequired[Type] = 1f;

        public override void SetDefaults()
        {
            Item.width = Item.height = 62;
            Item.damage = 90;
            Item.useAnimation = Item.useTime = 24;
            Item.mana = 10;
            Item.knockBack = 10f;
            Item.buffType = ModContent.BuffType<SiriusBuff>();
            Item.shoot = ModContent.ProjectileType<SiriusMinion>();
            Item.DamageType = DamageClass.Summon;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item44;
            Item.rare = ModContent.RarityType<PureGreen>();
            Item.value = CalamityGlobalItem.RarityPureGreenBuyPrice;
            Item.noMelee = true;
            Item.autoReuse = true;
        }

        public override bool CanUseItem(Player player) => true;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.ownedProjectileCounts[type] > 0)
            {
                Projectile sirius = null;
                foreach (var item in Main.ActiveProjectiles)
                {
                    if (item.type == type && item.owner == player.whoAmI)
                    {
                        if (item.type == type)
                            sirius = item;
                    }
                }
                if (sirius != null)
                {
                    sirius.ai[1]++;

                }
                return false;
            }
            else
                return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<VengefulSunStaff>().
                AddIngredient<Lumenyl>(5).
                AddIngredient<RuinousSoul>(2).
                AddIngredient<ExodiumCluster>(12).
                AddTile(TileID.MythrilAnvil).
                Register();
        }
    }
}
