using System.Collections.Generic;
using CalamityLegendsComeBack.Systems;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.Armor.Wulfrum;
using CalamityMod.Items.Tools;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Items.Weapons.Melee;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.CallofDuty
{
    public sealed class CallofDuty : ModItem, ILocalizedModType
    {
        internal const int BaseDamage = 280;

        private const string SourceFile = "Weapons/A_Upgrade/CallofDuty/CallofDuty.cs";

        // 大招（责任军团）伤害倍率：肉后/月后/神后三档。
        // 军团快照伤害 = 当前左键伤害 × 本档倍率；各单位自带的攻击系数在这个基准之上作为相对权重继续生效。
        private static readonly float[] UltimateDamageMultipliers =
        {
            2.50f, // Tier 0: 肉后（大招解锁起点，机械 Boss 之后）
            3.20f, // Tier 1: 月后（月亮领主之后）
            4.00f  // Tier 2: 神后（亵渎天神 Providence 之后）
        };

        internal static float GetUltimateDamageMultiplier() =>
            UltimateDamageTier.Resolve(SourceFile, nameof(UltimateDamageMultipliers), UltimateDamageMultipliers);

        internal const int BaseSequenceInterval = 18;
        internal const int MinimumSequenceInterval = 12;

        private int inventoryFrame;
        private int inventoryFrameCounter;

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityMod/Items/SummonItems/Invasion/MartianDistressRemote";

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 52;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Summon;
            Item.crit = 4;
            Item.knockBack = 2f;
            Item.useTime = BaseSequenceInterval;
            Item.useAnimation = BaseSequenceInterval;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.UseSound = null;
            Item.shoot = ModContent.ProjectileType<CallofDutyHoldout>();
            Item.shootSpeed = 15f;
            Item.value = CalamityGlobalItem.RarityRedBuyPrice;
            Item.rare = ItemRarityID.Red;
        }

        public override bool AltFunctionUse(Player player) => true;
        public override bool CanUseItem(Player player) => false;
        public override bool CanShoot(Player player) => false;
        public override bool ConsumeItem(Player player) => false;

        public override void HoldItem(Player player)
        {
            CallofDutyPlayer phonePlayer = player.GetModPlayer<CallofDutyPlayer>();
            phonePlayer.HoldingPhone = true;
            player.Calamity().mouseWorldListener = true;

            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            int holdoutType = ModContent.ProjectileType<CallofDutyHoldout>();
            if (Main.myPlayer != player.whoAmI || player.ownedProjectileCounts[holdoutType] > 0)
                return;

            Vector2 aim = (GetMouseWorld(player) - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            Projectile.NewProjectile(
                player.GetSource_ItemUse(Item),
                player.MountedCenter,
                aim,
                holdoutType,
                0,
                0f,
                player.whoAmI);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string key = KeybindSystem.LegendarySkill?.GetAssignedKeys().Count > 0
                ? KeybindSystem.LegendarySkill.GetAssignedKeys()[0]
                : "P";

            string compact = string.Format(this.GetLocalizedValue("Compact"), key);
            tooltips.FindAndReplace("[GFB]", compact);

            if (Main.keyState.PressingShift())
            {
                tooltips.Add(new TooltipLine(Mod, "CallofDutyDetails", this.GetLocalizedValue("Details"))
                {
                    OverrideColor = new Color(132, 226, 255)
                });
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "CallofDutyHint", this.GetLocalizedValue("DetailsHint"))
                {
                    OverrideColor = new Color(151, 173, 184)
                });
            }

            tooltips.Add(new TooltipLine(Mod, "CallofDutyLegendary", this.GetLocalizedValue("LegendaryText"))
            {
                OverrideColor = new Color(194, 255, 67)
            });
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<RoverDrive>()
                .AddIngredient<WulfrumDrill>()
                .AddIngredient<WulfrumScrewdriver>()
                .AddIngredient<WulfrumProsthesis>()
                .AddIngredient<WulfrumHat>()
                .AddIngredient<WulfrumJacket>()
                .AddIngredient<WulfrumOveralls>()
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Items/SummonItems/Invasion/MartianDistressRemote_Animated").Value;
            Rectangle source = Item.GetCurrentFrame(ref inventoryFrame, ref inventoryFrameCounter, 5, 12);
            spriteBatch.Draw(texture, position, source, Color.White, 0f, source.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Items/SummonItems/Invasion/MartianDistressRemote_Animated").Value;
            Rectangle source = Item.GetCurrentFrame(ref inventoryFrame, ref inventoryFrameCounter, 5, 12);
            spriteBatch.Draw(texture, Item.Center - Main.screenPosition, source, lightColor, rotation, source.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }

        internal static bool HasPhoneInMainInventory(Player player)
        {
            int inventorySlots = System.Math.Min(58, player.inventory.Length);
            for (int i = 0; i < inventorySlots; i++)
            {
                if (player.inventory[i].type == ModContent.ItemType<CallofDuty>())
                    return true;
            }
            return false;
        }

        internal static Item FindPhone(Player player)
        {
            if (player.HeldItem?.type == ModContent.ItemType<CallofDuty>())
                return player.HeldItem;

            int inventorySlots = System.Math.Min(58, player.inventory.Length);
            for (int i = 0; i < inventorySlots; i++)
            {
                if (player.inventory[i].type == ModContent.ItemType<CallofDuty>())
                    return player.inventory[i];
            }
            return null;
        }

        internal static bool HasEquippedRoverDrive(Player player)
        {
            int end = System.Math.Min(10, player.armor.Length);
            for (int i = 3; i < end; i++)
            {
                if (player.armor[i].type == ModContent.ItemType<RoverDrive>())
                    return true;
            }
            return false;
        }

        internal static bool CanUseWorldInput(Player player)
        {
            if (player.noItems || player.CCed || Main.mapFullscreen || Main.blockMouse || player.mouseInterface)
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
    }
}
