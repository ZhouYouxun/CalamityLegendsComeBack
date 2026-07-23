using System;
using System.IO;
using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.Cooldowns;
using CalamityMod.Items.Accessories;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.CallofDuty
{
    internal enum ResponsibilityCommandMode : byte
    {
        Return,
        Move,
        Attack
    }

    public sealed class CallofDutyPlayer : ModPlayer
    {
        public const int UltimateDurationFrames = 60 * 60;
        public const int UltimateCooldownFrames = 60 * 60;

        public bool HoldingPhone { get; set; }
        public bool PhonePassiveActive { get; private set; }
        public bool RoverDriveBoosted { get; private set; }

        public int RedialTarget { get; private set; } = -1;
        public int RedialTargetTimer { get; private set; }
        public int RedialCooldownTimer { get; internal set; }

        public int FastDialPriorityTarget { get; private set; } = -1;
        public int FastDialPriorityTimer { get; private set; }

        public int BothHoldTimer { get; internal set; }

        public int UltimateCharge { get; private set; }
        public bool ArmyActive { get; private set; }
        public int ArmyGeneration { get; private set; }
        internal ResponsibilityCommandMode CommandMode { get; private set; }
        public Vector2 CommandPosition { get; private set; }
        public int CommandTarget { get; private set; } = -1;

        public int PriorityTarget { get; private set; } = -1;
        public int PriorityTargetTimer { get; private set; }

        private int nextSequenceId;
        private float boostedShieldDurability;
        private float boostedShieldRechargeProgress;
        private bool wasRoverDriveBoosted;
        private bool capturedBoostedShieldHit;

        public bool UltimateReady => UltimateCharge >= UltimateCooldownFrames;
        public float UltimateCompletion => MathHelper.Clamp(UltimateCharge / (float)UltimateCooldownFrames, 0f, 1f);

        public override void Initialize()
        {
            UltimateCharge = UltimateCooldownFrames;
            CommandMode = ResponsibilityCommandMode.Return;
            RedialTarget = -1;
            FastDialPriorityTarget = -1;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["ultimateCharge"] = UltimateCharge;
        }

        public override void LoadData(TagCompound tag)
        {
            UltimateCharge = tag.ContainsKey("ultimateCharge")
                ? Math.Clamp(tag.GetInt("ultimateCharge"), 0, UltimateCooldownFrames)
                : UltimateCooldownFrames;
        }

        public override void ResetEffects()
        {
            HoldingPhone = false;
            PhonePassiveActive = CallofDuty.HasPhoneInMainInventory(Player);
            RoverDriveBoosted = PhonePassiveActive && CallofDuty.HasEquippedRoverDrive(Player);
            ApplyRoverDriveFlags();
        }

        public override void PostUpdateEquips()
        {
            ApplyRoverDriveFlags();

            if (Ultimate.ResponsibilityArmyUnitBase.IsInsideAnyAmplifierField(Player.Center))
                Player.statDefense += 20;
        }

        private void ApplyRoverDriveFlags()
        {
            if (!PhonePassiveActive)
                return;

            CalamityPlayer calamityPlayer = Player.Calamity();
            calamityPlayer.roverDrive = true;
            calamityPlayer.roverDriveShieldVisible = true;
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (Main.myPlayer != Player.whoAmI || KeybindSystem.LegendarySkill?.JustPressed != true)
                return;
            if (Player.HeldItem?.type != ModContent.ItemType<CallofDuty>())
                return;

            if (!ArmyActive && !UltimateReady)
                return;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                CallofDutyPackets.SendUltimateRequest();
            else
                CallofDutyPackets.ToggleUltimate(Player);
        }

        public override void PostUpdate()
        {
            // Redial cooldown & target timers
            if (RedialCooldownTimer > 0)
                RedialCooldownTimer--;

            if (RedialTargetTimer > 0)
            {
                RedialTargetTimer--;
                if (!Main.npc.IndexInRange(RedialTarget) || !Main.npc[RedialTarget].CanBeChasedBy() || Vector2.DistanceSquared(Player.Center, Main.npc[RedialTarget].Center) > 1400f * 1400f)
                    ClearRedialTarget();
            }
            else if (RedialTarget >= 0)
            {
                ClearRedialTarget();
            }

            if (FastDialPriorityTimer > 0)
            {
                FastDialPriorityTimer--;
                if (!Main.npc.IndexInRange(FastDialPriorityTarget) || !Main.npc[FastDialPriorityTarget].CanBeChasedBy())
                    ClearFastDialTarget();
            }
            else if (FastDialPriorityTarget >= 0)
            {
                ClearFastDialTarget();
            }

            // The sixty-second cooldown starts only after every recalled unit has finished
            // smoking out and disappeared. ArmyActive deliberately remains true during that
            // fade-out, so dismissing the army cannot overlap its cooldown with its duration.
            if (!ArmyActive && UltimateCharge < UltimateCooldownFrames)
                UltimateCharge++;

            if (PriorityTargetTimer > 0)
            {
                PriorityTargetTimer--;
                if (Main.npc.IndexInRange(PriorityTarget) && Main.npc[PriorityTarget].CanBeChasedBy())
                    Player.MinionAttackTargetNPC = PriorityTarget;
                else
                    ClearPriorityTarget();
            }

            if (ArmyActive && Main.netMode != NetmodeID.MultiplayerClient && !Ultimate.ResponsibilityArmyUnitBase.AnyActiveUnitFor(Player.whoAmI, ArmyGeneration))
            {
                ArmyActive = false;
                CallofDutyPackets.SendState(Player);
            }

            UpdateBoostedRoverDriveShield();
            SyncUltimateCooldownDisplay();
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (PhonePassiveActive && RoverDriveBoosted)
                modifiers.ModifyHurtInfo += CaptureBoostedShieldAfterCalamity;
        }

        private void CaptureBoostedShieldAfterCalamity(ref Player.HurtInfo info)
        {
            boostedShieldDurability = Math.Max(0f, Player.Calamity().RoverDriveShieldDurability);
            boostedShieldRechargeProgress = 0f;
            capturedBoostedShieldHit = true;
        }

        private void UpdateBoostedRoverDriveShield()
        {
            CalamityPlayer calamityPlayer = Player.Calamity();
            int nativeMax = Math.Max(1, RoverDrive.ShieldDurabilityMax);
            int boostedMax = nativeMax * 2;

            if (!PhonePassiveActive || !RoverDriveBoosted)
            {
                if (wasRoverDriveBoosted)
                {
                    float ratio = boostedMax <= 0 ? 0f : boostedShieldDurability / boostedMax;
                    calamityPlayer.RoverDriveShieldDurability = (int)MathF.Ceiling(MathHelper.Clamp(ratio, 0f, 1f) * nativeMax);
                    SyncRoverDurabilityCooldown(nativeMax, calamityPlayer.RoverDriveShieldDurability);
                }

                wasRoverDriveBoosted = false;
                boostedShieldDurability = 0f;
                boostedShieldRechargeProgress = 0f;
                capturedBoostedShieldHit = false;
                return;
            }

            if (!wasRoverDriveBoosted)
            {
                float ratio = calamityPlayer.RoverDriveShieldDurability / (float)nativeMax;
                boostedShieldDurability = MathHelper.Clamp(ratio, 0f, 1f) * boostedMax;
                boostedShieldRechargeProgress = 0f;
                wasRoverDriveBoosted = true;
            }

            // Calamity's native recharge code clamps to its static 20-point maximum. The boosted
            // layer therefore keeps a per-player durability value and injects it back after the
            // native update. Damage itself is still absorbed by Calamity's original shield path.
            if (!capturedBoostedShieldHit && boostedShieldDurability <= 0f && calamityPlayer.RoverDriveShieldDurability > 0)
                boostedShieldDurability = calamityPlayer.RoverDriveShieldDurability / (float)nativeMax * boostedMax;

            bool waitingForRecharge = calamityPlayer.cooldowns.ContainsKey(WulfrumRoverDriveRecharge.ID);
            if (!waitingForRecharge && boostedShieldDurability > 0f && boostedShieldDurability < boostedMax)
            {
                boostedShieldRechargeProgress += boostedMax / (float)Math.Max(1, RoverDrive.TotalShieldRechargeTime);
                int wholePoints = (int)MathF.Floor(boostedShieldRechargeProgress);
                if (wholePoints > 0)
                {
                    boostedShieldDurability = Math.Min(boostedMax, boostedShieldDurability + wholePoints);
                    boostedShieldRechargeProgress -= wholePoints;
                }
            }

            calamityPlayer.RoverDriveShieldDurability = Math.Clamp((int)MathF.Ceiling(boostedShieldDurability), 0, boostedMax);
            SyncRoverDurabilityCooldown(boostedMax, calamityPlayer.RoverDriveShieldDurability);
            capturedBoostedShieldHit = false;
        }

        private void SyncRoverDurabilityCooldown(int maximum, int current)
        {
            if (current <= 0)
                return;

            CalamityPlayer calamityPlayer = Player.Calamity();
            if (calamityPlayer.cooldowns.TryGetValue(WulfrumRoverDriveDurability.ID, out var cooldown))
            {
                cooldown.duration = maximum;
                cooldown.timeLeft = current;
                return;
            }

            Player.AddCooldown(WulfrumRoverDriveDurability.ID, maximum).timeLeft = current;
        }

        private void SyncUltimateCooldownDisplay()
        {
            if (!HoldingPhone && UltimateReady && !ArmyActive)
                return;

            int timeLeft = UltimateCooldownFrames - UltimateCharge;
            if (Player.Calamity().cooldowns.TryGetValue(CallofDutyUltimateCooldown.ID, out var cooldown))
            {
                cooldown.duration = UltimateCooldownFrames;
                cooldown.timeLeft = timeLeft;
                return;
            }
            Player.AddCooldown(CallofDutyUltimateCooldown.ID, UltimateCooldownFrames).timeLeft = timeLeft;
        }

        public override void UpdateDead()
        {
            ClearRedialTarget();
            ClearFastDialTarget();
            ClearPriorityTarget();
            boostedShieldDurability = 0f;
            boostedShieldRechargeProgress = 0f;
            wasRoverDriveBoosted = false;

            if (ArmyActive && Main.netMode != NetmodeID.MultiplayerClient)
                Ultimate.ResponsibilityArmyUnitBase.DismissAllFor(Player.whoAmI, ArmyGeneration);
        }

        public override void OnEnterWorld()
        {
            ArmyActive = false;
            ClearRedialTarget();
            ClearFastDialTarget();
            CommandMode = ResponsibilityCommandMode.Return;
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            if (Main.netMode == NetmodeID.Server)
                CallofDutyPackets.SendState(Player, toWho, fromWho);
        }

        internal int AllocateSequenceId()
        {
            nextSequenceId++;
            if (nextSequenceId >= 100000)
                nextSequenceId = 1;
            return nextSequenceId;
        }

        internal void SetRedialTarget(int target, int duration = 360)
        {
            RedialTarget = target;
            RedialTargetTimer = duration;
        }

        internal void ClearRedialTarget()
        {
            RedialTarget = -1;
            RedialTargetTimer = 0;
        }

        internal void SetFastDialTarget(int target, int duration = 720)
        {
            FastDialPriorityTarget = target;
            FastDialPriorityTimer = duration;
        }

        internal void ClearFastDialTarget()
        {
            FastDialPriorityTarget = -1;
            FastDialPriorityTimer = 0;
        }

        internal void SetPriorityTarget(int target, int duration)
        {
            PriorityTarget = target;
            PriorityTargetTimer = Math.Max(PriorityTargetTimer, duration);
        }

        private void ClearPriorityTarget()
        {
            PriorityTarget = -1;
            PriorityTargetTimer = 0;
        }

        internal void StartArmy(int generation)
        {
            ArmyGeneration = generation;
            ArmyActive = true;
            UltimateCharge = 0;
            CommandMode = ResponsibilityCommandMode.Return;
            CommandTarget = -1;
            CommandPosition = Player.Center;
        }

        internal void FinishArmy()
        {
            ArmyActive = false;
            CommandMode = ResponsibilityCommandMode.Return;
            CommandTarget = -1;
        }

        internal void BeginArmyRecall()
        {
            // Keep ArmyActive true until ResponsibilityArmyUnitBase confirms that the last
            // physical unit is gone. This is intentionally different from finishing the skill.
            CommandMode = ResponsibilityCommandMode.Return;
            CommandTarget = -1;
        }

        internal void FillUltimate()
        {
            UltimateCharge = UltimateCooldownFrames;
        }

        internal void ReceiveState(int charge, bool active, int generation)
        {
            UltimateCharge = Math.Clamp(charge, 0, UltimateCooldownFrames);
            ArmyActive = active;
            ArmyGeneration = generation;
        }

        internal void SetCommand(ResponsibilityCommandMode mode, Vector2 position, int target)
        {
            CommandMode = mode;
            CommandPosition = position;
            CommandTarget = target;
        }
    }
}
