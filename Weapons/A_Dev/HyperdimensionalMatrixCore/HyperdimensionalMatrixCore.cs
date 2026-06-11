using System;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Summon;
using CalamityMod.Rarities;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore
{
    public sealed class HyperdimensionalMatrixCore : ModItem, ILocalizedModType
    {
        public const int BaseDamage = 45;

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 64;
            Item.height = 64;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Summon;
            Item.mana = 10;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = false;
            Item.knockBack = 2f;
            Item.UseSound = SoundID.Item60;
            Item.buffType = ModContent.BuffType<HyperdimensionalMatrixCoreBuff>();
            Item.shoot = ModContent.ProjectileType<HyperdimensionalMatrixCoreProjectile>();
            Item.shootSpeed = 0f;
            Item.value = CalamityGlobalItem.RarityVioletBuyPrice;
            Item.rare = ModContent.RarityType<BurnishedAuric>();
            Item.Calamity().devItem = true;
        }

        public override void Unload() => HyperdimensionalMatrixVisuals.UnloadInventoryIconTextures();

        public override bool PreDrawInInventory(
            SpriteBatch spriteBatch,
            Vector2 position,
            Rectangle frame,
            Color drawColor,
            Color itemColor,
            Vector2 origin,
            float scale)
        {
            HyperdimensionalMatrixVisuals.DrawInventoryIcon(position, scale);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            HyperdimensionalMatrixVisuals.DrawWorldIcon(Item.Center - Main.screenPosition, scale);
            return false;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            bool coreExists = player.ownedProjectileCounts[Item.shoot] > 0;
            Item.mana = player.altFunctionUse == 2 ? 0 : 10;
            return player.altFunctionUse == 2 ? false : !coreExists;
        }

        public override void HoldItem(Player player)
        {
            if (Main.myPlayer != player.whoAmI)
                return;

            player.Calamity().rightClickListener = true;
            player.GetModPlayer<HyperdimensionalMatrixCorePlayer>().ProcessRightClickSwitch();
        }

        public override bool Shoot(
            Player player,
            EntitySource_ItemUse_WithAmmo source,
            Vector2 position,
            Vector2 velocity,
            int type,
            int damage,
            float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                TrySwitchCoreToNextForm(player);
                return false;
            }

            RemoveOtherSlotConsumingMinions(player, type);
            player.AddBuff(Item.buffType, 2);

            Projectile core = Projectile.NewProjectileDirect(
                source,
                player.Center + new Vector2(0f, -80f),
                Vector2.Zero,
                type,
                damage,
                knockback,
                player.whoAmI);

            core.originalDamage = Item.damage;
            core.minionSlots = Math.Max(1f, player.maxMinions);
            return false;
        }

        internal static bool TrySwitchCoreToNextForm(Player player)
        {
            int coreType = ModContent.ProjectileType<HyperdimensionalMatrixCoreProjectile>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner == player.whoAmI &&
                    projectile.type == coreType &&
                    projectile.ModProjectile is HyperdimensionalMatrixCoreProjectile core)
                {
                    core.AdvanceToNextForm();
                    return true;
                }
            }

            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<EyeOfNight>()
                .AddIngredient<DubiousPlating>(8)
                .AddIngredient(ItemID.SoulofLight)
                .AddIngredient(ItemID.SoulofNight)
                .AddTile(TileID.Anvils)
                .Register();
        }

        internal static void RemoveOtherSlotConsumingMinions(Player player, int coreType)
        {
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != player.whoAmI ||
                    projectile.type == coreType ||
                    !projectile.minion ||
                    projectile.minionSlots <= 0f)
                {
                    continue;
                }

                projectile.Kill();
            }
        }
    }

    public sealed class HyperdimensionalMatrixCoreBuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/HyperdimensionalMatrixCore/矩阵BUFF";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<HyperdimensionalMatrixCoreProjectile>()] <= 0)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
                return;
            }

            player.buffTime[buffIndex] = 18000;
            player.statDefense += 15;
            player.endurance += 0.15f;
            Lighting.AddLight(player.Center, new Vector3(0.08f, 0.3f, 0.4f));
        }
    }

    public sealed class HyperdimensionalMatrixCorePlayer : ModPlayer
    {
        private int rightClickSwitchCooldown;
        private bool wasHoldingRightClick;

        public override void PostUpdate()
        {
            if (rightClickSwitchCooldown > 0)
                rightClickSwitchCooldown--;
        }

        internal void ProcessRightClickSwitch()
        {
            bool holdingRightClick = Player.Calamity().mouseRight || Main.mouseRight;
            bool validRightClick =
                holdingRightClick &&
                !wasHoldingRightClick &&
                rightClickSwitchCooldown <= 0 &&
                Player.ownedProjectileCounts[ModContent.ProjectileType<HyperdimensionalMatrixCoreProjectile>()] > 0 &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !Player.mouseInterface &&
                !(Main.playerInventory && Main.HoverItem.type == ModContent.ItemType<HyperdimensionalMatrixCore>());

            if (validRightClick && HyperdimensionalMatrixCore.TrySwitchCoreToNextForm(Player))
            {
                rightClickSwitchCooldown = 12;
                Player.itemTime = 0;
                Player.itemAnimation = 0;
                Player.itemRotation = 0f;
            }

            wasHoldingRightClick = holdingRightClick;
        }
    }
}
