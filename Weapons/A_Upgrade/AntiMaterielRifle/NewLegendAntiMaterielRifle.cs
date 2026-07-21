using System.Collections.Generic;
using System.IO;
using CalamityMod;
using CalamityMod.Items;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle
{
    public sealed class NewLegendAntiMaterielRifle : ModItem, ILocalizedModType
    {
        private static int HoldoutType => ModContent.ProjectileType<AMRHoldout>();

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityMod/Items/Weapons/Ranged/AntiMaterielRifle";

        public bool ScopeZoomEnabled = true;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Type] = true;
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 154;
            Item.height = 40;
            Item.damage = AMRBalance.InitialDamage;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 2;
            Item.useAnimation = 2;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.knockBack = 8f;
            Item.UseSound = null;
            Item.shoot = HoldoutType;
            Item.shootSpeed = 32f;
            Item.useAmmo = AmmoID.Bullet;
            Item.value = CalamityGlobalItem.RarityOrangeBuyPrice;
            Item.rare = ItemRarityID.Orange;
            Item.crit = 20;
        }

        public override ModItem Clone(Item item)
        {
            NewLegendAntiMaterielRifle clone = (NewLegendAntiMaterielRifle)base.Clone(item);
            clone.ScopeZoomEnabled = ScopeZoomEnabled;
            return clone;
        }

        public override void SaveData(TagCompound tag) => tag["scopeZoomEnabled"] = ScopeZoomEnabled;

        public override void LoadData(TagCompound tag)
        {
            ScopeZoomEnabled = !tag.ContainsKey("scopeZoomEnabled") || tag.GetBool("scopeZoomEnabled");
        }

        public override void NetSend(BinaryWriter writer) => writer.Write(ScopeZoomEnabled);

        public override void NetReceive(BinaryReader reader) => ScopeZoomEnabled = reader.ReadBoolean();

        public override bool AltFunctionUse(Player player) => true;
        public override bool CanUseItem(Player player) => false;
        public override bool CanShoot(Player player) => false;
        public override bool CanRightClick() => true;
        public override bool ConsumeItem(Player player) => false;

        public override void RightClick(Player player)
        {
            ScopeZoomEnabled = !ScopeZoomEnabled;
            Item.NetStateChanged();

            SoundStyle sound = (ScopeZoomEnabled ? SoundID.MenuOpen : SoundID.MenuClose) with
            {
                Volume = 0.72f,
                Pitch = ScopeZoomEnabled ? 0.08f : -0.1f
            };
            SoundEngine.PlaySound(sound, player.Center);
        }

        public override void HoldItem(Player player)
        {
            AMRPlayer amrPlayer = player.GetModPlayer<AMRPlayer>();
            amrPlayer.SetHoldingWeapon();
            player.Calamity().mouseWorldListener = true;

            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            if (Main.myPlayer != player.whoAmI || player.ownedProjectileCounts[HoldoutType] > 0)
                return;

            Vector2 aimDirection = (GetMouseWorld(player) - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            int holdout = Projectile.NewProjectile(
                player.GetSource_ItemUse(Item),
                player.MountedCenter,
                aimDirection,
                HoldoutType,
                player.GetWeaponDamage(Item),
                Item.knockBack,
                player.whoAmI);

            if (Main.projectile.IndexInRange(holdout))
                Main.projectile[holdout].CritChance = player.GetWeaponCrit(Item);
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage *= AMRBalance.CurrentDamage / (float)AMRBalance.InitialDamage;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string text =
                this.GetLocalizedValue("LeftClick") + "\n" +
                this.GetLocalizedValue("RightClick") + "\n" +
                this.GetLocalizedValue("InventoryToggle");

            tooltips.FindAndReplace("[GFB]", text);

            bool shiftPressed = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) ||
                               Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift);

            AMRProgressionStage currentStage = AMRBalance.Stage;
            int maxStageIndex = (int)currentStage;

            if (shiftPressed)
            {
                for (int i = 0; i <= maxStageIndex; i++)
                {
                    string stageText = this.GetLocalizedValue($"Stage{i}");
                    if (!string.IsNullOrEmpty(stageText))
                    {
                        tooltips.Add(new TooltipLine(Mod, $"AMRStageText_{i}", $"• {stageText}")
                        {
                            OverrideColor = i == maxStageIndex ? new Color(255, 215, 100) : new Color(180, 220, 255)
                        });
                    }
                }

                tooltips.Add(new TooltipLine(Mod, "AMRLegendaryText", this.GetLocalizedValue("LegendaryText"))
                {
                    OverrideColor = new Color(102, 230, 255)
                });
            }
            else
            {
                string latestStageText = this.GetLocalizedValue($"Stage{maxStageIndex}");
                if (!string.IsNullOrEmpty(latestStageText))
                {
                    string latestPrefix = this.GetLocalizedValue("LatestUnlockPrefix");
                    string displayLatest = string.Format(latestPrefix, latestStageText);
                    tooltips.Add(new TooltipLine(Mod, "AMRLatestStageText", displayLatest)
                    {
                        OverrideColor = new Color(255, 215, 100)
                    });
                }

                tooltips.Add(new TooltipLine(Mod, "AMRLegendaryHint", this.GetLocalizedValue("LegendaryHint"))
                {
                    OverrideColor = Color.Gray
                });
            }
        }

        internal static bool CanUseWorldInput(Player player)
        {
            if (player.noItems || player.CCed || Main.mapFullscreen || Main.blockMouse || player.mouseInterface)
                return false;

            if (Main.playerInventory && !Main.HoverItem.IsAir)
                return false;

            return true;
        }

        internal static bool IsWorldRightClickInteractionActive(Player player)
        {
            return Main.playerInventory || player.chest != -1 || player.talkNPC != -1 || player.sleeping.isSleeping;
        }

        internal static Vector2 GetMouseWorld(Player player)
        {
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }
    }

    internal sealed class NewLegendAntiMaterielRifleShop : GlobalNPC
    {
        public override void ModifyShop(NPCShop shop)
        {
            if (shop.NpcType == NPCID.ArmsDealer)
                shop.AddWithCustomValue<NewLegendAntiMaterielRifle>(Item.buyPrice(gold: 20));
        }
    }
}
