using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC.TwistingNether的参考文献
{
    public class DeathsAscension : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Melee";

        public const int RiftLifeTime = 600;

        public const float OrbitalScytheDamageMult = 0.4f;

        public const float RiftScytheDamageMult = 0.125f;

        public const int RiftOrbitalAmount = 4;

        public const int ScytheShotAmount = 4;


        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 70;
            Item.height = 70;
            Item.damage = 700;
            Item.knockBack = 9f;
            Item.useAnimation = Item.useTime = 24;
            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.channel = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.shootSpeed = 12f;
            Item.shoot = ModContent.ProjectileType<DeathsAscensionSwing>();
            Item.value = CalamityGlobalItem.RarityPureGreenBuyPrice;
            Item.rare = ModContent.RarityType<PureGreen>();
            Item.Calamity().donorItem = true;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool? UseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.shoot = ModContent.ProjectileType<DeathsAscensionProjectile>();
            }
            else
            {
                Item.shoot = ModContent.ProjectileType<DeathsAscensionSwing>();
            }
            return base.UseItem(player);
        }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2f)
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.UseSound = SoundID.Item71;
                Item.useTurn = true;
                Item.autoReuse = true;
                Item.noMelee = false;
                Item.noUseGraphic = false;
                Item.channel = false;
            }
            else
            {
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.UseSound = null;
                Item.useTurn = false;
                Item.autoReuse = false;
                Item.noMelee = true;
                Item.noUseGraphic = true;
                Item.channel = true;
            }
            return base.CanUseItem(player);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int spreadfactor = 9;
            if (player.altFunctionUse == 2f)
            {
                for (int index = 0; index < ScytheShotAmount; ++index)
                {
                    float SpeedX = velocity.X + Main.rand.NextFloat(-spreadfactor, spreadfactor + 1);
                    float SpeedY = velocity.Y + Main.rand.NextFloat(-spreadfactor, spreadfactor + 1);
                    Projectile.NewProjectile(source, position.X, position.Y, SpeedX, SpeedY, type, (int)(damage * 0.125f), knockback, player.whoAmI);
                }

                // Tell the rift(s) to shoot
                foreach (Projectile p in Main.ActiveProjectiles)
                {
                    if (p.type == ModContent.ProjectileType<DeathsAscensionRift>() && p.owner == player.whoAmI && p.ai[0] <= 0)
                        p.ai[0] = 10f; // Cooldown before scythes can be shot again because right click code is cool and shoots twice without this
                }
            }
            else
            {
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<DeathsAscensionSwing>(), damage, knockback, player.whoAmI);
            }
            return false;
        }
        
        public override void UseItemFrame(Player player)
        {
            player.itemLocation = (Vector2)player.HandPosition;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.DeathSickle).
                AddIngredient<RuinousSoul>(4).
                AddIngredient(ItemID.SoulofNight, 15).
                AddIngredient<TwistingNether>(3).
                AddTile(TileID.MythrilAnvil).
                Register();
        }
    }
}
