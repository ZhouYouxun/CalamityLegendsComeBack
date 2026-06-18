using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI
{
    internal class BFRightUIPlayer : ModPlayer
    {
        private int rightPressFrames;
        private bool trackingRightPress;
        private bool showRightChargeBar;
        private float rightChargeProgress;
        private ulong lastProcessedFrame;

        private const string SavedPresetKey = "BlossomFluxCurrentPreset";

        public BlossomFluxChloroplastPresetType CurrentPreset { get; private set; } = BlossomFluxChloroplastPresetType.Chlo_ABreak;
        public int ReconPriorityTargetIndex { get; private set; } = -1;
        public int ReconPriorityTimeLeft { get; private set; }
        public bool PassiveRainEnabled { get; private set; } = true;
        public bool LongHoldReleasedThisFrame { get; private set; }
        public bool LongHoldReachedThisFrame { get; private set; }

        public int RightPressFrames => rightPressFrames;
        public bool TrackingRightPress => trackingRightPress;
        public bool RightMouseHeld => Player.Calamity().mouseRight || Main.mouseRight;
        public bool FormSwitchKeyHeld => KeybindSystem.LegendaryWeaponFormSwitch?.Current == true;
        public float RightHoldProgress => rightChargeProgress;
        public bool ShowRightHoldBar => showRightChargeBar && Player.HeldItem.type == ModContent.ItemType<NewLegendBlossomFlux>();
        public bool LongHoldActive => trackingRightPress && rightPressFrames > 0;
        public bool PassiveRainUnlocked => true;
        public bool UltimateUnlocked => true;
        public int UnlockedPresetCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i <= (int)BlossomFluxChloroplastPresetType.Chlo_EPlague; i++)
                {
                    if (IsPresetUnlocked((BlossomFluxChloroplastPresetType)i))
                        count++;
                }

                return count;
            }
        }

        public override void UpdateDead()
        {
            ClearReconPriorityTarget();
            ResetRightClickState();
        }

        public override void SaveData(TagCompound tag)
        {
            tag[SavedPresetKey] = (int)CurrentPreset;
        }

        public override void LoadData(TagCompound tag)
        {
            CurrentPreset = tag.ContainsKey(SavedPresetKey) && IsValidPresetValue(tag.GetInt(SavedPresetKey))
                ? (BlossomFluxChloroplastPresetType)tag.GetInt(SavedPresetKey)
                : BlossomFluxChloroplastPresetType.Chlo_ABreak;
        }

        public override void ResetEffects()
        {
            showRightChargeBar = false;
            rightChargeProgress = 0f;
        }

        public override void PostUpdate()
        {
            EnsureUnlockedPresetSelected();

            if (ReconPriorityTimeLeft > 0)
                ReconPriorityTimeLeft--;

            if (ReconPriorityTimeLeft <= 0 ||
                !ReconPriorityTargetIndex.WithinBounds(Main.maxNPCs) ||
                !Main.npc[ReconPriorityTargetIndex].active)
            {
                ClearReconPriorityTarget();
            }

            if (Player.whoAmI != Main.myPlayer)
                return;

            if (Player.HeldItem.type != ModContent.ItemType<NewLegendBlossomFlux>() &&
                Player.ownedProjectileCounts[ModContent.ProjectileType<BFSelectionPanel>()] <= 0)
            {
                ResetRightClickState();
            }
        }

        public void ProcessRightClickState(bool selectionPanelOpen)
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (lastProcessedFrame == Main.GameUpdateCount)
                return;

            lastProcessedFrame = Main.GameUpdateCount;
            LongHoldReleasedThisFrame = false;
            LongHoldReachedThisFrame = false;

            bool validRightInput =
                Player.HeldItem.type == ModContent.ItemType<NewLegendBlossomFlux>() &&
                !Player.noItems &&
                !Player.CCed &&
                !selectionPanelOpen &&
                RightMouseHeld &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !Player.mouseInterface &&
                !(Main.playerInventory && Main.HoverItem.type == Player.HeldItem.type);

            if (validRightInput)
            {
                if (!trackingRightPress)
                {
                    trackingRightPress = true;
                    rightPressFrames = 0;
                }

                rightPressFrames++;
                if (rightPressFrames == 1)
                    LongHoldReachedThisFrame = true;

                return;
            }

            if (!trackingRightPress)
                return;

            LongHoldReleasedThisFrame = true;
            trackingRightPress = false;
            rightPressFrames = 0;
        }

        public void SetRightChargeBar(float progress)
        {
            showRightChargeBar = true;
            rightChargeProgress = MathHelper.Clamp(progress, 0f, 1f);
        }

        public bool TrySetPreset(BlossomFluxChloroplastPresetType preset)
        {
            if (!IsPresetUnlocked(preset))
                return false;

            CurrentPreset = preset;
            return true;
        }

        public bool IsPresetUnlocked(BlossomFluxChloroplastPresetType preset)
        {
            return preset switch
            {
                BlossomFluxChloroplastPresetType.Chlo_ABreak => true,
                BlossomFluxChloroplastPresetType.Chlo_BRecov => BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.EyeOfCthulhu),
                BlossomFluxChloroplastPresetType.Chlo_CDetec => BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.QueenBee),
                BlossomFluxChloroplastPresetType.Chlo_DBomb => BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.WallOfFlesh),
                BlossomFluxChloroplastPresetType.Chlo_EPlague => BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MechBoss),
                _ => false
            };
        }

        public void SetReconPriorityTarget(int npcIndex, int timeLeft)
        {
            ReconPriorityTargetIndex = npcIndex;
            ReconPriorityTimeLeft = timeLeft;
        }

        public void ClearReconPriorityTarget()
        {
            ReconPriorityTargetIndex = -1;
            ReconPriorityTimeLeft = 0;
        }

        private void ResetRightClickState()
        {
            trackingRightPress = false;
            rightPressFrames = 0;
            LongHoldReleasedThisFrame = false;
            LongHoldReachedThisFrame = false;
            showRightChargeBar = false;
            rightChargeProgress = 0f;
        }

        private void EnsureUnlockedPresetSelected()
        {
            if (IsPresetUnlocked(CurrentPreset))
                return;

            CurrentPreset = GetHighestUnlockedPreset();
        }

        private BlossomFluxChloroplastPresetType GetHighestUnlockedPreset()
        {
            if (IsPresetUnlocked(BlossomFluxChloroplastPresetType.Chlo_EPlague))
                return BlossomFluxChloroplastPresetType.Chlo_EPlague;

            if (IsPresetUnlocked(BlossomFluxChloroplastPresetType.Chlo_DBomb))
                return BlossomFluxChloroplastPresetType.Chlo_DBomb;

            if (IsPresetUnlocked(BlossomFluxChloroplastPresetType.Chlo_CDetec))
                return BlossomFluxChloroplastPresetType.Chlo_CDetec;

            if (IsPresetUnlocked(BlossomFluxChloroplastPresetType.Chlo_BRecov))
                return BlossomFluxChloroplastPresetType.Chlo_BRecov;

            return BlossomFluxChloroplastPresetType.Chlo_ABreak;
        }

        private static bool IsValidPresetValue(int value)
        {
            return value >= (int)BlossomFluxChloroplastPresetType.Chlo_ABreak &&
                value <= (int)BlossomFluxChloroplastPresetType.Chlo_EPlague;
        }
    }
}
