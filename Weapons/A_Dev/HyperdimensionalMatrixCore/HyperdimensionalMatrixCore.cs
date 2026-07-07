using System;
using System.Collections.Generic;
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
        public const int BaseDamage = 33;

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
            Item.damage = 17;
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

        public override void Unload()
        {
            if (!Main.dedServ)
                Main.QueueMainThreadAction(HyperdimensionalMatrixVisuals.UnloadInventoryIconTextures);
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            HyperdimensionalMatrixVisuals.DrawInventoryIcon(position, scale);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
            ref float rotation, ref float scale, int whoAmI)
        {
            HyperdimensionalMatrixVisuals.DrawWorldIcon(Item.Center - Main.screenPosition, scale);
            return false;
        }

        public override bool CanUseItem(Player player)
            => player.ownedProjectileCounts[Item.shoot] <= 0;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (!Main.keyState.PressingShift())
                return;

            tooltips.RemoveAll(line => line.Mod == "Terraria" &&
                line.Name.StartsWith("Tooltip", StringComparison.Ordinal));
            tooltips.Add(new TooltipLine(Mod, "AttackDetails", this.GetLocalizedValue("AttackDetails")));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
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
                    continue;

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
            player.endurance   += 0.15f;
            player.lifeRegen   += 16;      // ~8 HP/s quantum resonance
            player.moveSpeed   += 0.08f;   // spacetime phase drift
            player.luck        += 0.05f;   // probability interference
            player.aggro       -= 400;     // adversarial stealth matrix
            Lighting.AddLight(player.Center, new Vector3(0.08f, 0.3f, 0.4f));
        }
    }
}
