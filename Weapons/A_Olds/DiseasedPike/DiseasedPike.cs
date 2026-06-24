using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;

namespace CalamityLegendsComeBack.Weapons.A_Olds.DiseasedPike
{
    public class DiseasedPike : BaseSwordHoldoutItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Melee";

        public override int ProjectileType => ModContent.ProjectileType<DiseasedPikeProj>();

        public override bool SizeModifiers => false;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 66;
            Item.height = 66;
            Item.damage = 130;
            Item.DamageType = ModContent.GetInstance<TrueMeleeNoSpeedDamageClass>();
            Item.useAnimation = Item.useTime = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 7f;
            Item.autoReuse = true;
            Item.value = Item.sellPrice(gold: 15);
            Item.rare = ItemRarityID.Yellow;
            Item.shootSpeed = 1f;
            Item.channel = true;
            base.SetDefaults();
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool CanShoot(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<DiseasedPikeProj>()] == 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var pikePlayer = player.GetModPlayer<DiseasedPikePlayer>();
            int attackMode = 0;
            bool isRightClick = player.altFunctionUse == 2;

            if (isRightClick)
            {
                attackMode = 3;
            }
            else
            {
                attackMode = pikePlayer.ComboIndex;
                pikePlayer.ComboIndex = (pikePlayer.ComboIndex + 1) % 3;
                pikePlayer.ComboResetTimer = 180;
            }

            Projectile.NewProjectile(source, player.MountedCenter, velocity, type, damage, knockback, player.whoAmI, attackMode);
            return false;
        }
    }
}
