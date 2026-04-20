using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Passive;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityMod;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    public class NewLegendBlossomFlux : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        public override void SetDefaults()
        {
            Item.width = 78;
            Item.height = 78;
            Item.damage = 12;
            Item.DamageType = DamageClass.Ranged;
            Item.useAnimation = 2;
            Item.useTime = 2;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.knockBack = 3.5f;
            Item.UseSound = SoundID.Item5;
            Item.shoot = ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>();
            Item.shootSpeed = 15f;
            Item.useAmmo = AmmoID.Arrow;
            Item.value = Item.sellPrice(0, 9);
            Item.rare = ItemRarityID.Pink;
        }

        public override Vector2? HoldoutOffset() => new Vector2(-10f, 0f);
        public override bool CanUseItem(Player player) => false;
        public override bool CanShoot(Player player) => false;

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            BFEXPlayer exPlayer = player.GetModPlayer<BFEXPlayer>();
            BFPassivePlayer passivePlayer = player.GetModPlayer<BFPassivePlayer>();
            passivePlayer.SetHoldingBlossomFlux();
            SyncUltimateDisplay(player, exPlayer);
            passivePlayer.SyncPassiveDisplay();

            if (Main.myPlayer == player.whoAmI &&
                player.ownedProjectileCounts[ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>()] <= 0)
            {
                Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    player.Center,
                    Vector2.UnitX * player.direction,
                    ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>(),
                    player.GetWeaponDamage(Item),
                    Item.knockBack,
                    player.whoAmI);
            }
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            bool[] downStages =
            {
                NPC.downedBoss1,
                NPC.downedBoss2,
                DownedBossSystem.downedHiveMind || DownedBossSystem.downedPerforator,
                NPC.downedBoss3,
                DownedBossSystem.downedSlimeGod,
                Main.hardMode,
                NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3,
                DownedBossSystem.downedCalamitasClone,
                NPC.downedPlantBoss,
                NPC.downedGolemBoss,
                NPC.downedAncientCultist,
                NPC.downedMoonlord,
                DownedBossSystem.downedProvidence,
                DownedBossSystem.downedSignus && DownedBossSystem.downedStormWeaver && DownedBossSystem.downedCeaselessVoid,
                DownedBossSystem.downedPolterghast,
                DownedBossSystem.downedDoG,
                DownedBossSystem.downedYharon,
                DownedBossSystem.downedExoMechs && DownedBossSystem.downedCalamitas,
                DownedBossSystem.downedPrimordialWyrm
            };

            int[] stageDamage =
            {
                12,
                17,
                22,
                27,
                32,
                40,
                54,
                66,
                82,
                96,
                116,
                155,
                185,
                220,
                255,
                320,
                395,
                560,
                720
            };

            int finalDamage = 10;
            for (int i = 0; i < downStages.Length; i++)
            {
                if (downStages[i])
                    finalDamage = stageDamage[i];
                else
                    break;
            }

            damage.Base = finalDamage;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            BlossomFluxChloroplastPresetType currentPreset = GetDisplayedPreset();
            BFRightUIPlayer rightUIPlayer = Main.LocalPlayer.GetModPlayer<BFRightUIPlayer>();
            BFPassivePlayer passivePlayer = Main.LocalPlayer.GetModPlayer<BFPassivePlayer>();
            string leftText = this.GetLocalizedValue("BF_Left");
            string rightText = this.GetLocalizedValue("BF_Right");
            string presetName = this.GetLocalizedValue($"PresetName{(int)currentPreset}");
            string presetText = string.Format(this.GetLocalizedValue("BF_Preset"), presetName);
            string leftPresetText = this.GetLocalizedValue($"PresetLeft{(int)currentPreset}");
            string rightPresetText = this.GetLocalizedValue($"PresetRight{(int)currentPreset}");
            string passiveStatus = !passivePlayer.PassiveUnlocked
                ? this.GetLocalizedValue("PassiveStateLocked")
                : passivePlayer.FinalStandActive
                    ? this.GetLocalizedValue("PassiveStateActive")
                    : passivePlayer.PassiveReady
                        ? this.GetLocalizedValue("PassiveStateReady")
                        : string.Format(this.GetLocalizedValue("PassiveStateCooldown"), passivePlayer.RemainingSeconds);
            string passiveText = string.Format(this.GetLocalizedValue("BF_Passive"), passiveStatus);
            string unlockRecovery = this.GetLocalizedValue(rightUIPlayer.IsPresetUnlocked(BlossomFluxChloroplastPresetType.Chlo_BRecov) ? "PresetUnlock1" : "PresetLock1");
            string unlockRecon = this.GetLocalizedValue(rightUIPlayer.IsPresetUnlocked(BlossomFluxChloroplastPresetType.Chlo_CDetec) ? "PresetUnlock2" : "PresetLock2");
            string unlockBombard = this.GetLocalizedValue(rightUIPlayer.IsPresetUnlocked(BlossomFluxChloroplastPresetType.Chlo_DBomb) ? "PresetUnlock3" : "PresetLock3");
            string unlockPlague = this.GetLocalizedValue(rightUIPlayer.IsPresetUnlocked(BlossomFluxChloroplastPresetType.Chlo_EPlague) ? "PresetUnlock4" : "PresetLock4");
            string ultimateText = rightUIPlayer.UltimateUnlocked
                ? string.Format(this.GetLocalizedValue("BF_Ultimate"), GetLegendarySkillKeyText())
                : this.GetLocalizedValue("BF_UltimateLocked");
            string finalText = this.GetLocalizedValue("BF_Final");
            string legendaryText = this.GetLocalizedValue("LegendaryText");
            string shiftHint = this.GetLocalizedValue("LegendaryHint");
            string legendarySection = Main.keyState.PressingShift() ? legendaryText : shiftHint;

            string merged =
                leftText + "\n" +
                presetText + "\n" +
                leftPresetText + "\n\n" +
                rightText + "\n" +
                rightPresetText + "\n\n" +
                passiveText + "\n" +
                unlockRecovery + "\n" +
                unlockRecon + "\n" +
                unlockBombard + "\n" +
                unlockPlague + "\n\n" +
                ultimateText + "\n\n" +
                finalText + "\n\n" +
                legendarySection + "\n";

            tooltips.FindAndReplace("[GFB]", merged);
        }

        internal static void SyncUltimateDisplay(Player player, BFEXPlayer exPlayer)
        {
            if (!player.GetModPlayer<BFRightUIPlayer>().UltimateUnlocked)
            {
                if (player.Calamity().cooldowns.TryGetValue(BFEXCoolDown.ID, out var hiddenCooldown))
                    hiddenCooldown.timeLeft = 0;

                return;
            }

            if (player.Calamity().cooldowns.TryGetValue(BFEXCoolDown.ID, out var cooldown))
            {
                cooldown.timeLeft = exPlayer.DisplayFrames;
            }
            else
            {
                player.AddCooldown(BFEXCoolDown.ID, exPlayer.DisplayFrames);
            }
        }

        private static BlossomFluxChloroplastPresetType GetDisplayedPreset()
        {
            if (Main.LocalPlayer?.active != true)
                return BlossomFluxChloroplastPresetType.Chlo_ABreak;

            return Main.LocalPlayer.GetModPlayer<BFRightUIPlayer>().CurrentPreset;
        }

        private static string GetLegendarySkillKeyText()
        {
            string assignedKeys = string.Join("/", KeybindSystem.LegendarySkill.GetAssignedKeys());
            return string.IsNullOrWhiteSpace(assignedKeys) ? "Unbound" : assignedKeys;
        }

        public override bool CanRightClick() => false;

        public override bool ConsumeItem(Player player) => false;
    }
}
