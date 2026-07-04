using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    public class NewLegendM14 : ModItem, ILocalizedModType
    {
        private static int HoldoutType => ModContent.ProjectileType<MK14EBRHoldout>();
        private readonly BalanceMK14EBR balance = new();

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/MK14EBR/M14/m14";

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 48;
            Item.damage = BalanceMK14EBR.BaseDamage[0];
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 2;
            Item.useAnimation = 2;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.knockBack = 4f;
            Item.UseSound = null;
            Item.shoot = HoldoutType;
            Item.shootSpeed = 18f;
            Item.useAmmo = AmmoID.Bullet;
            Item.value = CalamityGlobalItem.RarityPinkBuyPrice;
            Item.rare = ModContent.RarityType<BurnishedAuric>();
        }

        public override bool CanUseItem(Player player) => false;

        public override bool CanShoot(Player player) => false;

        public override bool ConsumeItem(Player player) => false;

        public override void HoldItem(Player player)
        {
            player.GetModPlayer<MK14EBRPlayer>().SetHoldingMK14EBR();
            player.Calamity().mouseWorldListener = true;

            if (Main.myPlayer != player.whoAmI)
                return;

            if (player.ownedProjectileCounts[HoldoutType] <= 0)
            {
                Vector2 aimDirection = (NewLegendMK14EBR.GetMouseWorld(player) - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
                int holdoutIndex = Projectile.NewProjectile(
                    Item.GetSource_FromThis(),
                    player.MountedCenter,
                    aimDirection,
                    HoldoutType,
                    player.GetWeaponDamage(Item),
                    Item.knockBack,
                    player.whoAmI);

                if (Main.projectile.IndexInRange(holdoutIndex))
                    Main.projectile[holdoutIndex].CritChance = player.GetWeaponCrit(Item);
            }
        }

        public override void UpdateInventory(Player player)
        {
            Item.noUseGraphic = true;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base += balance.GetBaseDamage() - Item.damage;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<M1Garand>()
                .AddIngredient(ItemID.SoulofSight)
                .AddIngredient(ItemID.SoulofFright)
                .AddIngredient(ItemID.SoulofMight)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            spriteBatch.Draw(texture, position, frame, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            spriteBatch.Draw(texture, Item.Center - Main.screenPosition, null, lightColor, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
