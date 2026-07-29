using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.LeftClick;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.RightClick;
using CalamityMod;
using CalamityMod.Items;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces
{
    /// <summary>
    /// 双鱼座 / Pisces —— Dragoon Drizzlefish × Polaris Parrotfish 的双态联动重铸。
    /// 左键：会落地、会扩散、会留下硫火战场的暴躁喷吐（3 小 1 大火球，重力下坠 + 地火锚点）。
    /// 右键：按住后逐级提纯、快速追击单体的冷静光学射击（I/II/III 级光弹 + 满蓄双束神圣激光）。
    /// 两者在场上留下的“锚点”被互相串联，形成一次明确、可读、可控的联动爆发。
    /// 物品本身只做 SetDefaults、左右键分流与进度成长；不承载长生命周期状态（那些在 <see cref="PiscesPlayer"/> / 各弹幕里）。
    /// </summary>
    public sealed class Pisces : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityMod/Items/Fishing/BrimstoneCragCatches/DragoonDrizzlefish";

        private static int HoldoutType => ModContent.ProjectileType<PiscesOpticHoldout>();
        private static int LeftHoldoutType => ModContent.ProjectileType<PiscesBrimstoneHoldout>();

        public static readonly SoundStyle SpewSound = SoundID.Item20;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Type] = true;
            Item.staff[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 38;
            Item.damage = PiscesBalance.BaseDamage;
            Item.knockBack = PiscesBalance.KnockBack;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = Item.useAnimation = PiscesBalance.LeftBaseUseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.UseSound = null; // 由发射逻辑自行播放
            Item.shoot = ModContent.ProjectileType<PiscesBrimstoneFireball>();
            Item.shootSpeed = PiscesBalance.SmallFireballSpeed;
            Item.value = CalamityGlobalItem.RarityPurpleBuyPrice;
            Item.rare = ItemRarityID.Purple;
            Item.noUseGraphic = true;
        }

        public override Vector2? HoldoutOrigin() => new Vector2(7f, 7f);

        public override bool AltFunctionUse(Player player) => true;

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                // 右键：快速触发以生成/维持持械（真正的蓄力节奏由持械弹幕读鼠标自持）。
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.useTime = Item.useAnimation = 6;
                Item.UseSound = null;
                Item.noUseGraphic = true;
                return true;
            }

            // 左键：硫火喷吐（成长后更快）。
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = Item.useAnimation = PiscesBalance.LeftUseTime();
            Item.UseSound = null;
            Item.noUseGraphic = false;
            // 左键也使用独立手持鱼弹幕，物品原图不参与默认手持绘制。
            Item.noUseGraphic = true;
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                // 右键优先：一旦开始光学持械，立刻清理硫火手持，避免双鱼抢同一只手。
                if (Main.myPlayer == player.whoAmI)
                    PiscesBrimstoneHoldout.KillOwnedBy(player.whoAmI);
                if (Main.myPlayer == player.whoAmI && player.ownedProjectileCounts[HoldoutType] <= 0)
                {
                    Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
                    Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter, aim,
                        HoldoutType, player.GetWeaponDamage(Item), Item.knockBack, player.whoAmI);
                }
                return false;
            }

            // 即使操作系统同时上报两键，也由右键持械压过左键喷吐。
            if (player.ownedProjectileCounts[HoldoutType] > 0)
                return false;

            FireBrimstoneSpew(player, source, position, velocity, damage, knockback);
            return false;
        }

        /// <summary>左键“3 小 1 大”：前三发小火球，第四发大火球；计数保存在 PiscesPlayer。</summary>
        private void FireBrimstoneSpew(Player player, IEntitySource source, Vector2 position, Vector2 velocity, int damage, float knockback)
        {
            PiscesPlayer mp = player.GetModPlayer<PiscesPlayer>();
            Vector2 shotVelocity = velocity.RotatedByRandom(PiscesBalance.LeftSpread);

            int shotType;
            if (mp.LeftShotCounter < PiscesBalance.BigShotInterval - 1)
            {
                shotType = ModContent.ProjectileType<PiscesBrimstoneFireball>();
                mp.LeftShotCounter++;
                SoundEngine.PlaySound(SpewSound with { Pitch = 0.05f }, position);
            }
            else
            {
                shotType = ModContent.ProjectileType<PiscesBrimstoneFireballBig>();
                mp.LeftShotCounter = 0;
                SoundEngine.PlaySound(SpewSound with { Pitch = -0.25f, Volume = 1.1f }, position);
            }

            if (Main.myPlayer == player.whoAmI)
            {
                int proj = Projectile.NewProjectile(source, position, shotVelocity, shotType,
                    Math.Max(1, damage), knockback, player.whoAmI);
                if (Main.projectile.IndexInRange(proj))
                    Main.projectile[proj].CritChance = player.GetWeaponCrit(Item);

                Projectile.NewProjectile(source, player.MountedCenter, shotVelocity.SafeNormalize(Vector2.UnitX * player.direction),
                    LeftHoldoutType, 0, 0f, player.whoAmI);
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<CalamityMod.Items.Fishing.BrimstoneCragCatches.DragoonDrizzlefish>()
                .AddIngredient<CalamityMod.Items.Weapons.Ranged.PolarisParrotfish>()
                .AddTile(TileID.CookingPots)
                .Register();
        }
    }
}
