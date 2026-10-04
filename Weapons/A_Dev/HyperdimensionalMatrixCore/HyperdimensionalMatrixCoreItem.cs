using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Summon;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore
{
    // Weapon form of the Matrix Core. The separate HDMCUncompiledCore boss summon stays hidden.
    public sealed class HyperdimensionalMatrixCore : ModItem, ILocalizedModType
    {
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
            HyperdimensionalMatrixCoreRuntime.RemoveOtherSlotConsumingMinions(player, type);
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
    }
}
