using CalamityLegendsComeBack.Weapons;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    internal sealed class CosmicDischargePlayer : ModPlayer
    {
        public const int UltimateEnergyMax = 120;
        public const int RightThrustEnergyGain = 10;
        public const int UltimateFieldDuration = 10 * 60;
        public const int PassiveCooldownFrames = 60 * 60;
        public const int RiftGuardDuration = 3 * 60;
        public const int RiftRevivalDuration = 3 * 60;

        public int UltimateEnergy;
        public int PassiveCooldownTimer;
        public int QuickDrawCooldownTimer;
        public CosmicDischargeAttackMode AttackMode;
        public int DevourerSliverCount;

        public bool DevourerAscensionActive => Player.HasBuff(ModContent.BuffType<CosmicDischargeDevourerAscensionBuff>());

        private bool holdingCosmicDischarge;
        private bool wasUltimateReady;
        private int comboIndex;
        private int comboResetTimer;
        private CosmicDischargeAttackMode comboMode;

        public bool HoldingCosmicDischarge =>
            holdingCosmicDischarge &&
            Player.HeldItem != null &&
            !Player.HeldItem.IsAir &&
            Player.HeldItem.type == ModContent.ItemType<NewLegendCosmicDischarge>();

        public bool UltimateFieldActive => Player.ownedProjectileCounts[ModContent.ProjectileType<CosmicDischargeUltimateField>()] > 0;

        public bool ShouldShowPassiveCooldown =>
            HoldingCosmicDischarge ||
            PassiveCooldownTimer > 0 ||
            Player.HasBuff(ModContent.BuffType<CosmicDischargeRiftRevivalBuff>());

        public override void ResetEffects()
        {
            holdingCosmicDischarge = false;
        }

        public override void UpdateDead()
        {
            holdingCosmicDischarge = false;
            UltimateEnergy = 0;
            comboIndex = 0;
            comboResetTimer = 0;
            QuickDrawCooldownTimer = 0;
            DevourerSliverCount = 0;
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasUltimateReady, false);
        }

        public override void PostUpdate()
        {
            if (PassiveCooldownTimer > 0)
                PassiveCooldownTimer--;

            if (QuickDrawCooldownTimer > 0)
                QuickDrawCooldownTimer--;

            if (comboResetTimer > 0)
                comboResetTimer--;
            else
                comboIndex = 0;

            if (!Player.HasBuff(ModContent.BuffType<CosmicDischargeDevourerSliverBuff>()))
            {
                DevourerSliverCount = 0;
            }

            if (DevourerSliverCount >= 10 && !DevourerAscensionActive)
            {
                Player.ClearBuff(ModContent.BuffType<CosmicDischargeDevourerSliverBuff>());
                DevourerSliverCount = 0;
                Player.AddBuff(ModContent.BuffType<CosmicDischargeDevourerAscensionBuff>(), 720); // 12 seconds

                if (Player.whoAmI == Main.myPlayer)
                {
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftOpen") { Volume = 0.8f, Pitch = -0.08f }, Player.Center);
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerSpawn") { Volume = 0.58f, Pitch = 0.18f }, Player.Center);
                    Player.Calamity().GeneralScreenShakePower = System.Math.Max(Player.Calamity().GeneralScreenShakePower, 20f);
                    CombatText.NewText(Player.getRect(), CosmicDischargeCommon.DoGWhiteColor, "Devourer Ascension!", true, true);
                }
            }

            if (DevourerAscensionActive && Player.active && !Player.dead)
            {
                // 升华状态是持续 12 秒的常驻光环，频率必须压到最低，
                // 否则会盖过攻击本身的特效 —— 常驻效果永远不该比瞬时事件抢眼。
                if (Main.rand.NextBool(12))
                {
                    Vector2 dustPos = Player.Center + Main.rand.NextVector2Circular(24f, 32f);
                    Dust d = Dust.NewDustPerfect(
                        dustPos,
                        DustID.TintableDustLighted,
                        new Vector2(Player.velocity.X * 0.4f, Main.rand.NextFloat(-2.5f, -0.6f)),
                        0,
                        CosmicDischargeCommon.RiftColor(),
                        Main.rand.NextFloat(0.6f, 0.85f)
                    );
                    d.noGravity = true;
                }

                if (Main.rand.NextBool(35))
                {
                    if (!Main.dedServ)
                    {
                        GeneralParticleHandler.SpawnParticle(new PulseRing(
                            Player.MountedCenter,
                            Vector2.Zero,
                            CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * 0.42f,
                            0.035f,
                            0.55f,
                            20
                        ));
                    }
                }
            }

            if (HoldingCosmicDischarge)
            {
                HandleUltimateInput();
                SyncCooldownDisplays();
            }
            else if (UltimateEnergy > 0 || PassiveCooldownTimer > 0 || QuickDrawCooldownTimer > 0)
            {
                SyncCooldownDisplays();
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (UltimateFieldActive || Player.HasBuff(ModContent.BuffType<CosmicDischargeUltimateGuardBuff>()))
                modifiers.FinalDamage *= 0.65f;

            if (HoldingCosmicDischarge && PassiveCooldownTimer <= 0)
                modifiers.ModifyHurtInfo += CapIncomingDamage;
        }

        private void CapIncomingDamage(ref Player.HurtInfo info)
        {
            if (info.Cancelled)
                return;
            int cap = (int)(Player.statLifeMax2 * 0.20f);
            if (info.Damage > cap)
            {
                info.Damage = cap;
                if (Player.whoAmI == Main.myPlayer)
                {
                    PassiveCooldownTimer = PassiveCooldownFrames;
                    Player.AddBuff(ModContent.BuffType<CosmicDischargeRiftRevivalBuff>(), 300);
                    SyncCooldownDisplays();
                }
            }
        }

        public void SetHoldingCosmicDischarge()
        {
            holdingCosmicDischarge = true;
        }

        public void ToggleAttackMode()
        {
            SetAttackMode(AttackMode switch
            {
                CosmicDischargeAttackMode.Whip => CosmicDischargeAttackMode.Sword,
                CosmicDischargeAttackMode.Sword => CosmicDischargeAttackMode.ChainKnife,
                _ => CosmicDischargeAttackMode.Whip
            });
        }

        public void SetAttackMode(CosmicDischargeAttackMode mode)
        {
            AttackMode = mode;

            if (Player.whoAmI == Main.myPlayer)
            {
                string modeName = AttackMode switch
                {
                    CosmicDischargeAttackMode.Sword => Terraria.Localization.Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.NewLegendCosmicDischarge.SwordName"),
                    CosmicDischargeAttackMode.ChainKnife => Terraria.Localization.Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.NewLegendCosmicDischarge.ChainKnifeName"),
                    _ => Terraria.Localization.Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.NewLegendCosmicDischarge.WhipName")
                };
                Color textColor = CosmicDischargeCommon.GetModeColor(AttackMode);
                CombatText.NewText(Player.getRect(), textColor, modeName, true, false);
            }

            comboIndex = 0;
            comboResetTimer = 0;

            // Clear old mode buffs
            Player.ClearBuff(ModContent.BuffType<CosmicDischargeWhipBuff>());
            Player.ClearBuff(ModContent.BuffType<CosmicDischargeSwordBuff>());
            Player.ClearBuff(ModContent.BuffType<CosmicDischargeChainKnifeBuff>());

            // Apply new mode buff
            int nextBuffType = AttackMode switch
            {
                CosmicDischargeAttackMode.Sword => ModContent.BuffType<CosmicDischargeSwordBuff>(),
                CosmicDischargeAttackMode.ChainKnife => ModContent.BuffType<CosmicDischargeChainKnifeBuff>(),
                _ => ModContent.BuffType<CosmicDischargeWhipBuff>()
            };
            Player.AddBuff(nextBuffType, 600); // 10 seconds

            float pitch = AttackMode switch
            {
                CosmicDischargeAttackMode.Sword => -0.15f,
                CosmicDischargeAttackMode.ChainKnife => -0.35f,
                _ => 0.35f
            };

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DemonSwordKillMode") { Volume = 0.68f, Pitch = pitch, MaxInstances = 2 }, Player.Center);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftOpen") { Volume = 0.34f, Pitch = pitch + 0.2f, MaxInstances = 2 }, Player.Center);

            // 切换形态是"仪式"而非"攻击"，给 Medium 档：能看出发生了什么，但不抢戏。
            CosmicDischargeCommon.SpawnRiftBurst(
                Player.MountedCenter,
                RiftTier.Medium,
                default,
                CosmicDischargeCommon.GetModeColor(AttackMode));
        }

        public int ConsumeComboIndex(CosmicDischargeAttackMode mode)
        {
            if (comboMode != mode || comboResetTimer <= 0)
                comboIndex = 0;

            comboMode = mode;
            int current = comboIndex;
            comboIndex = (comboIndex + 1) % 3;
            comboResetTimer = 72;
            return current;
        }

        public void AddUltimateEnergy(int amount)
        {
            if (amount <= 0 || UltimateFieldActive)
                return;

            UltimateEnergy = Utils.Clamp(UltimateEnergy + amount, 0, UltimateEnergyMax);
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasUltimateReady, UltimateEnergy >= UltimateEnergyMax);
            SyncCooldownDisplays();
        }

        public void AddDevourerSliver(int amount)
        {
            if (DevourerAscensionActive || !Player.active || Player.dead)
                return;

            DevourerSliverCount = System.Math.Clamp(DevourerSliverCount + amount, 0, 10);
            if (DevourerSliverCount > 0)
            {
                Player.AddBuff(ModContent.BuffType<CosmicDischargeDevourerSliverBuff>(), 1800);
                if (Player.whoAmI == Main.myPlayer && amount > 0)
                {
                    CombatText.NewText(Player.getRect(), CosmicDischargeCommon.DoGSpecialColor, $"+{amount} Rift Sliver ({DevourerSliverCount}/10)", false, false);
                }
            }
        }

        private void HandleUltimateInput()
        {
            if (Main.myPlayer != Player.whoAmI || !KeybindSystem.LegendarySkill.JustPressed)
                return;

            if (UltimateEnergy < UltimateEnergyMax || UltimateFieldActive)
                return;

            UltimateEnergy = 0;
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasUltimateReady, false);
            SyncCooldownDisplays();

            int fieldType = ModContent.ProjectileType<CosmicDischargeUltimateField>();
            Projectile.NewProjectile(
                Player.GetSource_ItemUse(Player.HeldItem),
                Player.Center,
                Vector2.Zero,
                fieldType,
                Player.GetWeaponDamage(Player.HeldItem),
                0f,
                Player.whoAmI);

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftOpen") { Volume = 0.78f, Pitch = -0.12f }, Player.Center);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerSpawn") { Volume = 0.46f, Pitch = 0.12f }, Player.Center);

            if (Player.whoAmI == Main.myPlayer)
            {
                Player.Calamity().GeneralScreenShakePower = System.Math.Max(Player.Calamity().GeneralScreenShakePower, 15f);
                string activeText = Terraria.Localization.Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.NewLegendCosmicDischarge.UltimateActiveText");
                CombatText.NewText(Player.getRect(), CosmicDischargeCommon.DoGSpecialColor, activeText, true, true);
            }
        }

        private void SyncCooldownDisplays()
        {
            SyncCooldown(CosmicDischargeUltimateCooldown.ID, UltimateEnergyMax, UltimateEnergy);
            SyncCooldown(CosmicDischargePassiveCooldown.ID, PassiveCooldownFrames, PassiveCooldownTimer);
            SyncCooldown(CosmicDischargeQuickDrawCooldown.ID, 1800, QuickDrawCooldownTimer);
        }

        private void SyncCooldown(string id, int duration, int timeLeft)
        {
            if (Player.Calamity().cooldowns.TryGetValue(id, out var cooldown))
            {
                cooldown.duration = duration;
                cooldown.timeLeft = timeLeft;
                return;
            }

            Player.AddCooldown(id, duration).timeLeft = timeLeft;
        }
    }
}
