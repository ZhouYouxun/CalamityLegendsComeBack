using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.A_Dev.SHPBow
{
    internal sealed class SHPBowPlayer : ModPlayer
    {
        public const int TapThresholdFrames = 9;

        private const int BarFadeInFrames = 10;
        private const int BarFadeOutFrames = 16;
        private const int BarDropFrames = 20;

        private readonly SHPBowMode[] coreSequence =
        {
            SHPBowMode.Pierce,
            SHPBowMode.Pierce,
            SHPBowMode.Pierce,
            SHPBowMode.Pierce
        };

        private int sequenceLength = 1;
        private int rightPressFrames;
        private bool trackingRightPress;
        private ulong lastProcessedFrame;
        private float chargeBarProgress;
        private float chargeBarOpacity;

        public bool HoldingSHPBow { get; private set; }
        public bool ShortTapReleasedThisFrame { get; private set; }
        public bool LongHoldReachedThisFrame { get; private set; }
        public bool LongHoldReleasedThisFrame { get; private set; }

        public int SequenceLength => sequenceLength;
        public int PackedSequence => SHPBowModeHelpers.PackSequence(coreSequence, sequenceLength);
        public SHPBowMode SequenceAccentMode => coreSequence[sequenceLength - 1];
        public int RightPressFrames => rightPressFrames;
        public bool TrackingRightPress => trackingRightPress;
        public bool RightMouseHeld => Player.Calamity().mouseRight || Main.mouseRight;
        public float RightHoldProgress => MathHelper.Clamp(rightPressFrames / (float)TapThresholdFrames, 0f, 1f);
        public float ChargeBarProgress => chargeBarProgress;
        public float ChargeBarOpacity => chargeBarOpacity;
        public bool ShowRightHoldBar => HoldingSHPBow && (trackingRightPress || chargeBarOpacity > 0f);

        public override void ResetEffects()
        {
            HoldingSHPBow = false;
        }

        public override void UpdateDead()
        {
            ResetRightClickState();
            chargeBarProgress = 0f;
            chargeBarOpacity = 0f;
        }

        public override void PostUpdate()
        {
            if (!HoldingSHPBow && chargeBarOpacity > 0f)
                UpdateChargeBar(false, 0f);

            if (Player.whoAmI != Main.myPlayer)
                return;

            if (Player.HeldItem.type != ModContent.ItemType<SHPBow>() &&
                Player.ownedProjectileCounts[ModContent.ProjectileType<SHPBowHoldout>()] <= 0 &&
                Player.ownedProjectileCounts[ModContent.ProjectileType<SHPBowSelectionPanel>()] <= 0)
            {
                ResetRightClickState();
            }
        }

        public void SetHoldingSHPBow()
        {
            HoldingSHPBow = true;
        }

        public SHPBowMode GetSequenceMode(int index)
        {
            int safeIndex = Utils.Clamp(index, 0, sequenceLength - 1);
            return coreSequence[safeIndex];
        }

        public int CountMode(SHPBowMode mode)
        {
            int count = 0;
            for (int i = 0; i < sequenceLength; i++)
            {
                if (coreSequence[i] == mode)
                    count++;
            }

            return count;
        }

        public void AppendMode(SHPBowMode mode)
        {
            mode = SHPBowModeHelpers.ClampMode((int)mode);
            if (sequenceLength < SHPBowModeHelpers.MaxSequenceLength)
            {
                coreSequence[sequenceLength] = mode;
                sequenceLength++;
                return;
            }

            for (int i = 1; i < SHPBowModeHelpers.MaxSequenceLength; i++)
                coreSequence[i - 1] = coreSequence[i];

            coreSequence[SHPBowModeHelpers.MaxSequenceLength - 1] = mode;
        }

        public void ResetSequence(SHPBowMode mode)
        {
            coreSequence[0] = SHPBowModeHelpers.ClampMode((int)mode);
            for (int i = 1; i < SHPBowModeHelpers.MaxSequenceLength; i++)
                coreSequence[i] = coreSequence[0];

            sequenceLength = 1;
        }

        public void UpdateChargeBar(bool active, float progress)
        {
            if (active)
            {
                chargeBarProgress = MathHelper.Clamp(progress, 0f, 1f);
                chargeBarOpacity = MathHelper.Clamp(chargeBarOpacity + 1f / BarFadeInFrames, 0f, 1f);
            }
            else
            {
                chargeBarProgress = MathHelper.Clamp(chargeBarProgress - 1f / BarDropFrames, 0f, 1f);
                chargeBarOpacity = MathHelper.Clamp(chargeBarOpacity - 1f / BarFadeOutFrames, 0f, 1f);
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
            LongHoldReachedThisFrame = false;
            LongHoldReleasedThisFrame = false;

            bool validRightInput =
                Player.HeldItem.type == ModContent.ItemType<SHPBow>() &&
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

        private void ResetRightClickState()
        {
            trackingRightPress = false;
            rightPressFrames = 0;
            ShortTapReleasedThisFrame = false;
            LongHoldReachedThisFrame = false;
            LongHoldReleasedThisFrame = false;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["SHPBowSequenceLength"] = sequenceLength;
            tag["SHPBowSequence"] = PackedSequence;
        }

        public override void LoadData(TagCompound tag)
        {
            int packed = tag.ContainsKey("SHPBowSequence")
                ? tag.GetInt("SHPBowSequence")
                : (tag.ContainsKey("SHPBowMode") ? ((1 << 8) | (tag.GetInt("SHPBowMode") & 3)) : (1 << 8));

            sequenceLength = SHPBowModeHelpers.SequenceLength(packed);
            for (int i = 0; i < SHPBowModeHelpers.MaxSequenceLength; i++)
                coreSequence[i] = SHPBowModeHelpers.SequenceMode(packed, i);
        }
    }
}
