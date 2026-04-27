using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI
{
    internal class BFRightUIPlayer : ModPlayer
    {
        public const int TapThresholdFrames = 9;

        private int rightPressFrames;
        private bool trackingRightPress;
        private ulong lastProcessedFrame;

        public BlossomFluxChloroplastPresetType CurrentPreset { get; private set; } = BlossomFluxChloroplastPresetType.Chlo_ABreak;
        public int ReconPriorityTargetIndex { get; private set; } = -1;
        public int ReconPriorityTimeLeft { get; private set; }
        public bool PassiveRainEnabled { get; private set; } = true;
        public bool ShortTapReleasedThisFrame { get; private set; }
        public bool LongHoldReleasedThisFrame { get; private set; }
        public bool LongHoldReachedThisFrame { get; private set; }

        public int RightPressFrames => rightPressFrames;
        public bool TrackingRightPress => trackingRightPress;
        public bool RightMouseHeld => Player.Calamity().mouseRight || Main.mouseRight;
        public float RightHoldProgress => MathHelper.Clamp(rightPressFrames / (float)TapThresholdFrames, 0f, 1f);
        public bool ShowRightHoldBar => trackingRightPress && Player.HeldItem.type == ModContent.ItemType<NewLegendBlossomFlux>();
        public bool LongHoldActive => trackingRightPress && rightPressFrames > TapThresholdFrames;
        public bool PassiveRainUnlocked => Main.hardMode;
        public bool UltimateUnlocked => NPC.downedQueenBee;
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
            CurrentPreset = BlossomFluxChloroplastPresetType.Chlo_ABreak;
            ClearReconPriorityTarget();
            ResetRightClickState();
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
            ShortTapReleasedThisFrame = false;
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
                if (rightPressFrames == TapThresholdFrames + 1)
                    LongHoldReachedThisFrame = true;

                return;
            }

            if (!trackingRightPress)
                return;

            if (rightPressFrames <= TapThresholdFrames)
                ShortTapReleasedThisFrame = true;
            else
                LongHoldReleasedThisFrame = true;

            trackingRightPress = false;
            rightPressFrames = 0;
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
                BlossomFluxChloroplastPresetType.Chlo_BRecov => NPC.downedQueenBee,
                BlossomFluxChloroplastPresetType.Chlo_CDetec => Main.hardMode,
                BlossomFluxChloroplastPresetType.Chlo_DBomb => NPC.downedMechBoss3,
                BlossomFluxChloroplastPresetType.Chlo_EPlague => DownedBossSystem.downedPlaguebringer,
                _ => false
            };
        }

        public void TogglePassiveRain()
        {
            if (!PassiveRainUnlocked)
                return;

            PassiveRainEnabled = !PassiveRainEnabled;
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
            ShortTapReleasedThisFrame = false;
            LongHoldReleasedThisFrame = false;
            LongHoldReachedThisFrame = false;
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
    }
}
