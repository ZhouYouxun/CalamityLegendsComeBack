using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    public class NewLegendMK14EBR : ModItem, ILocalizedModType
    {
        private static int HoldoutType => ModContent.ProjectileType<MK14EBRHoldout>();
        private static int PanelType => ModContent.ProjectileType<MK14ModificationPanel>();
        private readonly BalanceMK14EBR balance = new();

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => MK14TextureComposer.BodyPath;

        public MK14Barrel Barrel { get; private set; } = MK14Barrel.Long;
        public MK14Muzzle Muzzle { get; private set; } = MK14Muzzle.None;
        public MK14Underbarrel Underbarrel { get; private set; } = MK14Underbarrel.None;
        public MK14Stock Stock { get; private set; } = MK14Stock.EBR;
        public MK14Sight Sight { get; private set; } = MK14Sight.RedDot;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 186;
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
            Item.value = CalamityGlobalItem.RarityTurquoiseBuyPrice;
            Item.rare = ModContent.RarityType<Turquoise>();
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player) => false;

        public override bool CanShoot(Player player) => false;

        public override bool ConsumeItem(Player player) => false;

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<NewLegendM14>()
                .AddIngredient(ItemID.ChlorophyteBar, 10)
                .AddIngredient<PerennialBar>(10)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

        public override void HoldItem(Player player)
        {
            ClampLockedAttachments();
            MK14EBRPlayer mk14Player = player.GetModPlayer<MK14EBRPlayer>();
            mk14Player.SetHoldingMK14EBR();
            ApplyHoldPassives(player);

            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer != player.whoAmI)
                return;

            player.Calamity().rightClickListener = true;
            if (mk14Player.ConsumeRightClickPress(player) && CanUseWorldInput(player, allowMouseBlockedByPanel: true))
                ToggleModificationPanel(player);

            if (player.ownedProjectileCounts[HoldoutType] <= 0)
            {
                Vector2 aimDirection = (GetMouseWorld(player) - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
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
            ClampLockedAttachments();
            MK14RuntimeStats stats = MK14AttachmentDatabase.BuildStats(this, player);
            damage.Base += balance.GetBaseDamage() - Item.damage;
            damage *= stats.DamageMultiplier;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string intro = this.GetLocalizedValue("MK14_Intro");
            string stageName = BalanceMK14EBR.GetLocalizedStageName(balance.GetCompletedStageIndex());
            string stage = string.Format(this.GetLocalizedValue("MK14_Stage"), stageName);
            string right = this.GetLocalizedValue("MK14_RightClick");
            string ammo = this.GetLocalizedValue("MK14_Ammo");
            string loadout = BuildLoadoutTooltip();
            string legendarySection = Main.keyState.PressingShift()
                ? this.GetLocalizedValue("LegendaryText")
                : this.GetLocalizedValue("LegendaryHint");

            string finalText =
                intro + "\n" +
                stage + "\n\n" +
                ammo + "\n" +
                right + "\n\n" +
                loadout + "\n\n" +
                legendarySection + "\n";

            tooltips.FindAndReplace("[GFB]", finalText);
        }

        internal bool TrySetAttachment(MK14AttachmentSlot slot, int value)
        {
            MK14AttachmentDefinition definition = MK14AttachmentDatabase.Get(slot, value);
            if (!MK14AttachmentDatabase.IsUnlocked(definition))
                return false;

            switch (slot)
            {
                case MK14AttachmentSlot.Barrel:
                    Barrel = (MK14Barrel)value;
                    break;
                case MK14AttachmentSlot.Muzzle:
                    Muzzle = (MK14Muzzle)value;
                    break;
                case MK14AttachmentSlot.Underbarrel:
                    Underbarrel = (MK14Underbarrel)value;
                    break;
                case MK14AttachmentSlot.Stock:
                    Stock = (MK14Stock)value;
                    break;
                case MK14AttachmentSlot.Sight:
                    Sight = (MK14Sight)value;
                    break;
            }

            return true;
        }

        internal int GetSelectedValue(MK14AttachmentSlot slot) => slot switch
        {
            MK14AttachmentSlot.Barrel => (int)Barrel,
            MK14AttachmentSlot.Muzzle => (int)Muzzle,
            MK14AttachmentSlot.Underbarrel => (int)Underbarrel,
            MK14AttachmentSlot.Stock => (int)Stock,
            MK14AttachmentSlot.Sight => (int)Sight,
            _ => 0
        };

        internal IEnumerable<MK14AttachmentDefinition> GetSelectedDefinitions()
        {
            yield return MK14AttachmentDatabase.Get(MK14AttachmentSlot.Barrel, (int)Barrel);
            yield return MK14AttachmentDatabase.Get(MK14AttachmentSlot.Muzzle, (int)Muzzle);
            yield return MK14AttachmentDatabase.Get(MK14AttachmentSlot.Underbarrel, (int)Underbarrel);
            yield return MK14AttachmentDatabase.Get(MK14AttachmentSlot.Stock, (int)Stock);
            yield return MK14AttachmentDatabase.Get(MK14AttachmentSlot.Sight, (int)Sight);
        }

        public void ClampLockedAttachments()
        {
            if (!MK14AttachmentDatabase.IsUnlocked(MK14AttachmentDatabase.Get(MK14AttachmentSlot.Barrel, (int)Barrel)))
                Barrel = MK14Barrel.Long;

            if (!MK14AttachmentDatabase.IsUnlocked(MK14AttachmentDatabase.Get(MK14AttachmentSlot.Muzzle, (int)Muzzle)))
                Muzzle = MK14Muzzle.None;

            if (!MK14AttachmentDatabase.IsUnlocked(MK14AttachmentDatabase.Get(MK14AttachmentSlot.Underbarrel, (int)Underbarrel)))
                Underbarrel = MK14Underbarrel.None;

            if (!MK14AttachmentDatabase.IsUnlocked(MK14AttachmentDatabase.Get(MK14AttachmentSlot.Stock, (int)Stock)))
                Stock = MK14Stock.EBR;

            if (!MK14AttachmentDatabase.IsUnlocked(MK14AttachmentDatabase.Get(MK14AttachmentSlot.Sight, (int)Sight)))
                Sight = MK14Sight.RedDot;
        }

        internal static bool CanUseWorldInput(Player player, bool allowMouseBlockedByPanel = false)
        {
            if (player.noItems ||
                player.CCed ||
                Main.mapFullscreen ||
                player.mouseInterface)
            {
                return false;
            }

            if (!allowMouseBlockedByPanel && Main.blockMouse)
                return false;

            if (Main.playerInventory && !Main.HoverItem.IsAir)
                return false;

            return true;
        }

        internal static Vector2 GetMouseWorld(Player player)
        {
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }

        private void ToggleModificationPanel(Player player)
        {
            Projectile panel = FindOwnedPanel(player);
            if (panel != null)
            {
                MK14ModificationPanel.RequestClose(panel);
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.55f, Pitch = 0.05f }, player.Center);
                return;
            }

            Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                player.Center,
                Vector2.Zero,
                PanelType,
                0,
                0f,
                player.whoAmI);

            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.65f, Pitch = 0.07f }, player.Center);
        }

        private static Projectile FindOwnedPanel(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == PanelType)
                    return projectile;
            }

            return null;
        }

        private void ApplyHoldPassives(Player player)
        {
            switch (Stock)
            {
                case MK14Stock.EBR:
                    player.moveSpeed += 0.08f;
                    player.maxRunSpeed *= 1.08f;
                    player.runAcceleration *= 1.12f;
                    break;
                case MK14Stock.Heavy:
                    player.maxRunSpeed *= 0.9f;
                    player.runAcceleration *= 0.9f;
                    player.statDefense += 10 + (int)MathF.Ceiling(player.statDefense * 0.1f);
                    break;
            }

            MK14RuntimeStats stats = MK14AttachmentDatabase.BuildStats(this);
            if (stats.AggroReduction > 0)
                player.aggro -= stats.AggroReduction;
        }

        private string BuildLoadoutTooltip()
        {
            List<string> lines = new();
            foreach (MK14AttachmentDefinition definition in GetSelectedDefinitions().Where(d => d != null))
            {
                string name = Language.GetTextValue(definition.NameKey);
                string effect = Language.GetTextValue(definition.EffectKey);
                lines.Add($"[c/85D7FF:{name}] - {effect}");
            }

            return string.Join("\n", lines);
        }

        public override ModItem Clone(Item item)
        {
            ModItem clone = base.Clone(item);
            if (clone is NewLegendMK14EBR newItem && item.ModItem is NewLegendMK14EBR oldItem)
            {
                newItem.Barrel = oldItem.Barrel;
                newItem.Muzzle = oldItem.Muzzle;
                newItem.Underbarrel = oldItem.Underbarrel;
                newItem.Stock = oldItem.Stock;
                newItem.Sight = oldItem.Sight;
            }

            return clone;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["MK14Barrel"] = (int)Barrel;
            tag["MK14Muzzle"] = (int)Muzzle;
            tag["MK14Underbarrel"] = (int)Underbarrel;
            tag["MK14Stock"] = (int)Stock;
            tag["MK14Sight"] = (int)Sight;
        }

        public override void LoadData(TagCompound tag)
        {
            Barrel = (MK14Barrel)Utils.Clamp(tag.GetInt("MK14Barrel"), 0, MK14AttachmentDatabase.Barrels.Length - 1);
            Muzzle = (MK14Muzzle)Utils.Clamp(tag.GetInt("MK14Muzzle"), 0, MK14AttachmentDatabase.Muzzles.Length - 1);
            Underbarrel = (MK14Underbarrel)Utils.Clamp(tag.GetInt("MK14Underbarrel"), 0, MK14AttachmentDatabase.Underbarrels.Length - 1);
            Stock = (MK14Stock)Utils.Clamp(tag.GetInt("MK14Stock"), 0, MK14AttachmentDatabase.Stocks.Length - 1);
            Sight = (MK14Sight)Utils.Clamp(tag.GetInt("MK14Sight"), 0, MK14AttachmentDatabase.Sights.Length - 1);
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write((byte)Barrel);
            writer.Write((byte)Muzzle);
            writer.Write((byte)Underbarrel);
            writer.Write((byte)Stock);
            writer.Write((byte)Sight);
        }

        public override void NetReceive(BinaryReader reader)
        {
            Barrel = (MK14Barrel)reader.ReadByte();
            Muzzle = (MK14Muzzle)reader.ReadByte();
            Underbarrel = (MK14Underbarrel)reader.ReadByte();
            Stock = (MK14Stock)reader.ReadByte();
            Sight = (MK14Sight)reader.ReadByte();
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            MK14TextureComposer.DrawComposite(spriteBatch, this, position, Color.White, 0f, scale, SpriteEffects.None);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            MK14TextureComposer.DrawComposite(spriteBatch, this, Item.Center - Main.screenPosition, lightColor, rotation, scale, SpriteEffects.None);
            return false;
        }
    }
}
