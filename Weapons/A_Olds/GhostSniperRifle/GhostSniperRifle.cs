using CalamityMod;
using CalamityMod.Items;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.GhostSniperRifle
{
    public class GhostSniperRifle : ModItem, ILocalizedModType
    {
        public static readonly SoundStyle FireSound = new("CalamityMod/Sounds/Item/NitroExpressRifleFire")
        {
            Volume = 0.6f
        };

        public new string LocalizationCategory => "Items.Weapons";

        public override void SetDefaults()
        {
            Item.width = 100;
            Item.height = 22;
            Item.damage = 210;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 70;
            Item.useAnimation = 70;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 7.5f;
            Item.value = CalamityGlobalItem.RarityLightRedBuyPrice;
            Item.UseSound = FireSound;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.Bullet;
            Item.rare = ItemRarityID.LightRed;
        }

        public override Vector2? HoldoutOffset() => new(-10f, 8f);

        public override void ModifyWeaponCrit(Player player, ref float crit) => crit += 10f;

        public override void HoldItem(Player player) => player.Calamity().mouseWorldListener = true;

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            type = ModContent.ProjectileType<GhostSniperRound>();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 muzzle = player.MountedCenter + direction * 36f;

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/EtherealCoreUse") with { Volume = 0.55f, Pitch = -0.2f }, muzzle);
            SoundEngine.PlaySound(SoundID.Item38 with { Volume = 0.45f, Pitch = -0.35f }, muzzle);
            SpawnMuzzleEffects(muzzle, direction);

            Projectile.NewProjectile(source, muzzle, direction * velocity.Length(), type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            player.ChangeDir(Math.Sign((player.Calamity().mouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 desiredPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 35f;
            Vector2 spriteSize = new(Item.width, Item.height);
            Vector2 itemOrigin = new(-5f, 6f);

            CalamityUtils.CleanHoldStyle(player, itemRotation, desiredPosition, spriteSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player)
        {
            player.ChangeDir(Math.Sign((player.Calamity().mouseWorld - player.Center).X));
            float animationCompletion = 1f - player.itemTime / (float)player.itemTimeMax;
            float armRotation = (player.Center - player.Calamity().mouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;

            if (animationCompletion < 0.5f)
                armRotation += -0.45f * (float)Math.Pow((0.5f - animationCompletion) / 0.5f, 2.0) * player.direction;

            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.None, armRotation);

            if (animationCompletion > 0.5f)
            {
                float backArmRotation = armRotation + 0.52f * player.direction;
                Player.CompositeArmStretchAmount backArmStretch = ((float)Math.Sin(MathHelper.Pi * (animationCompletion - 0.5f) / 0.36f)).ToStretchAmount();
                player.SetCompositeArmBack(true, backArmStretch, backArmRotation);
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.SpectreBar, 7)
                .AddIngredient(ItemID.Ectoplasm, 3)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

        private static void SpawnMuzzleEffects(Vector2 muzzle, Vector2 direction)
        {
            Color ghostBlue = new(170, 245, 255);

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.42f) * Main.rand.NextFloat(2.2f, 7.4f);
                Dust dust = Dust.NewDustPerfect(
                    muzzle + direction * Main.rand.NextFloat(2f, 18f),
                    Main.rand.NextBool(3) ? DustID.SpectreStaff : DustID.GemDiamond,
                    velocity,
                    80,
                    Main.rand.NextBool(4) ? ghostBlue : Color.White,
                    Main.rand.NextFloat(0.7f, 1.25f));
                dust.noGravity = true;
            }
        }
    }
}
