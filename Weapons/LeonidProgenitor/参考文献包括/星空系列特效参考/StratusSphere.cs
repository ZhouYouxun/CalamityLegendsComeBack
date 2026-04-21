using System;
using System.Collections.Generic;
using System.Linq;
using CalamityMod.CalPlayer;
using CalamityMod.DataStructures;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables.Ores;
using CalamityMod.Projectiles.Typeless;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Weapons.Typeless
{
    public class StratusSphere : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Typeless";
        public static readonly SoundStyle CastSound = new SoundStyle("CalamityMod/Sounds/Item/StratusSphereCast") with { Volume = 0.75f };

        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 30;
            Item.damage = 400;
            Item.DamageType = AverageDamageClass.Instance;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useLimitPerAnimation = 1;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 5.5f;
            Item.value = CalamityGlobalItem.RarityTurquoiseBuyPrice;
            Item.rare = ModContent.RarityType<Turquoise>();
            Item.UseSound = CastSound;
            Item.autoReuse = true;
            Item.shootSpeed = 20f;
            Item.shoot = ModContent.ProjectileType<StratusBlackHole>();
            Item.noUseGraphic = true;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.NebulaArcanum).
                AddIngredient<Lumenyl>(6).
                AddIngredient<RuinousSoul>(4).
                AddIngredient<ExodiumCluster>(12).
                AddTile(TileID.MythrilAnvil).
                Register();
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            List<Projectile> holes = new();
                foreach (var item in Main.ActiveProjectiles)
                {
                    if (item.type == type && item.owner == player.whoAmI && item.timeLeft > 30)
                    {
                        holes.Add(item);
                    }
                }
            var orderedholes = holes.OrderBy(x => x.timeLeft).ToList();
            while (orderedholes.Count >= 1)
            {
                orderedholes.First().timeLeft = 30;
                orderedholes.Remove(orderedholes.First());
            }
            if (player.altFunctionUse == 2)
            {
                if (player.Calamity().Starshield > 45)
                {
                    player.Calamity().Starshield = 30;
                }
                else
                    player.Calamity().Starshield = CalamityUtils.MinutesToFrames(10);
            }
            else
            {

                Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
                player.Calamity().Starshield = (int)MathHelper.Min(30,player.Calamity().Starshield);
            }
            return false;
        }

    }
}
