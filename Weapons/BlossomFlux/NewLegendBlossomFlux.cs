using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Passive;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow;
using CalamityLegendsComeBack.Weapons.BlossomFlux.TurretMode;
using CalamityMod;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    public class NewLegendBlossomFlux : ModItem, ILocalizedModType
    {
        private const int ParallelArrowCount = 3;
        private const int BurstGroupCount = 4;
        private const int NormalBurstGroupInterval = 15;
        private const int TurretBurstGroupInterval = 9;
        private const int NormalEchoDelay = 4;
        private const int TurretEchoDelay = 2;
        private const int NormalLeafDelayFromBurstStart = 13;
        private const int TurretLeafDelayFromBurstStart = 7;
        private const float ParallelSpacing = 18f;
        private const float LeafSpeed = 14f;

        private int burstGroupsStarted;
        private int pendingEchoTimer = -1;
        private int pendingLeafTimer = -1;

        public new string LocalizationCategory => "Items.Weapons";

        public override void SetDefaults()
        {
            Item.width = 78;
            Item.height = 78;
            Item.damage = 12;
            Item.DamageType = DamageClass.Ranged;
            Item.useAnimation = BurstGroupCount * NormalBurstGroupInterval;
            Item.useTime = NormalBurstGroupInterval;
            Item.useLimitPerAnimation = BurstGroupCount;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.knockBack = 3.5f;
            Item.UseSound = SoundID.Item5;
            Item.shoot = ProjectileID.WoodenArrowFriendly;
            Item.shootSpeed = 15f;
            Item.useAmmo = AmmoID.Arrow;
            Item.value = Item.sellPrice(0, 9);
            Item.rare = ItemRarityID.Pink;

        }

        public override Vector2? HoldoutOffset() => new Vector2(-10f, 0f);

        public override bool CanUseItem(Player player)
        {
            if (HasActiveSelectionPanel(player) || HasActiveRightHoldout(player))
                return false;

            int burstInterval = GetBurstGroupInterval(player);
            Item.useTime = burstInterval;
            Item.useAnimation = BurstGroupCount * burstInterval;
            Item.useLimitPerAnimation = BurstGroupCount;
            ResetBurstState();
            return base.CanUseItem(player);
        }

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            BFEXPlayer exPlayer = player.GetModPlayer<BFEXPlayer>();
            BFRightUIPlayer rightUIPlayer = player.GetModPlayer<BFRightUIPlayer>();
            BFPassivePlayer passivePlayer = player.GetModPlayer<BFPassivePlayer>();
            BFTurretModePlayer turretPlayer = player.GetModPlayer<BFTurretModePlayer>();
            turretPlayer.SetHoldingBlossomFlux();
            passivePlayer.SetHoldingBlossomFlux();
            SyncUltimateDisplay(player, exPlayer);
            rightUIPlayer.ProcessRightClickState(HasActiveSelectionPanel(player));

            if (Main.myPlayer == player.whoAmI && rightUIPlayer.ShortTapReleasedThisFrame)
                ToggleSelectionPanel(player);
            if (Main.myPlayer == player.whoAmI && rightUIPlayer.LongHoldReachedThisFrame)
                SpawnRightHoldout(player);

            if (Main.myPlayer == player.whoAmI && KeybindSystem.LegendarySkill.JustPressed)
            {
                if (turretPlayer.TurretModeActive)
                {
                    if (turretPlayer.CanExitTurretMode)
                        turretPlayer.ExitTurretMode();
                    else
                        SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.45f, Pitch = 0.12f }, player.Center);
                }
                else if (rightUIPlayer.UltimateUnlocked && exPlayer.CanTriggerUltimate)
                {
                    exPlayer.ConsumeUltimateCharge();
                    CloseInteractiveProjectiles(player);
                    ResetBurstState();
                    turretPlayer.EnterTurretMode();
                }
                else if (!rightUIPlayer.UltimateUnlocked)
                {
                    SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.48f, Pitch = -0.08f }, player.Center);
                }
            }

            if (player.whoAmI != Main.myPlayer)
                return;

            if (pendingEchoTimer > 0 && --pendingEchoTimer == 0)
                FireEchoVolley(player);

            if (pendingLeafTimer > 0 && --pendingLeafTimer == 0)
                FireLeafBody(player);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            burstGroupsStarted++;
            FireParallelVolley(player, source, velocity, type, damage, knockback);

            pendingEchoTimer = GetEchoDelay(player);
            if (burstGroupsStarted >= BurstGroupCount)
                pendingLeafTimer = GetLeafDelayFromBurstStart(player);

            return false;
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
            if (player.GetModPlayer<BFTurretModePlayer>().TurretModeActive)
                damage *= BFTurretModePlayer.TurretDamageMultiplier;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // Tooltip 结构刻意向 SHPC 靠拢：左键段、当前战术、右键段、大招段、传奇记录。
            BlossomFluxChloroplastPresetType currentPreset = GetDisplayedPreset();
            BFRightUIPlayer rightUIPlayer = Main.LocalPlayer.GetModPlayer<BFRightUIPlayer>();
            string leftText = this.GetLocalizedValue("BF_Left");
            string rightText = this.GetLocalizedValue("BF_Right");
            string presetName = this.GetLocalizedValue($"PresetName{(int)currentPreset}");
            string presetText = string.Format(this.GetLocalizedValue("BF_Preset"), presetName);
            string leftPresetText = this.GetLocalizedValue($"PresetLeft{(int)currentPreset}");
            string rightPresetText = this.GetLocalizedValue($"PresetRight{(int)currentPreset}");
            string passiveStatus = this.GetLocalizedValue(
                rightUIPlayer.PassiveRainUnlocked
                    ? (rightUIPlayer.PassiveRainEnabled ? "PassiveStateOn" : "PassiveStateOff")
                    : "PassiveStateLocked");
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

        private void ResetBurstState()
        {
            burstGroupsStarted = 0;
            pendingEchoTimer = -1;
            pendingLeafTimer = -1;
        }

        private void FireEchoVolley(Player player)
        {
            if (!player.PickAmmo(Item, out int projectileType, out float speed, out int damage, out float knockback, out _, false))
                return;

            Vector2 velocity = GetAimVelocity(player, speed);
            FireParallelVolley(player, Item.GetSource_FromThis(), velocity, projectileType, damage, knockback);
        }

        private void FireLeafBody(Player player)
        {
            Vector2 velocity = GetAimVelocity(player, LeafSpeed);
            int damage = (int)(player.GetWeaponDamage(Item) * 1.4f);
            int currentPreset = (int)player.GetModPlayer<BFRightUIPlayer>().CurrentPreset;

            Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                GetShootOrigin(player, velocity),
                velocity,
                ModContent.ProjectileType<BlossomFluxChloroplast>(),
                damage,
                Item.knockBack,
                player.whoAmI,
                currentPreset);

            SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.2f, Volume = 0.8f }, player.Center);
        }

        private void FireParallelVolley(Player player, IEntitySource source, Vector2 velocity, int projectileType, int damage, float knockback)
        {
            Vector2 shootVelocity = velocity.SafeNormalize(Vector2.UnitX * player.direction) * velocity.Length();
            if (shootVelocity == Vector2.Zero)
                shootVelocity = Vector2.UnitX * player.direction * Item.shootSpeed;

            Vector2 origin = GetShootOrigin(player, shootVelocity);
            Vector2 normal = shootVelocity.SafeNormalize(Vector2.UnitX * player.direction).RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < ParallelArrowCount; i++)
            {
                float offsetAmount = (i - (ParallelArrowCount - 1f) * 0.5f) * ParallelSpacing;
                Vector2 spawnPosition = origin + normal * offsetAmount;

                int projectileIndex = Projectile.NewProjectile(source, spawnPosition, shootVelocity, projectileType, damage, knockback, player.whoAmI);
                if (!projectileIndex.WithinBounds(Main.maxProjectiles))
                    continue;

                Projectile projectile = Main.projectile[projectileIndex];
                projectile.arrow = true;
                projectile.noDropItem = true;
                projectile.extraUpdates++;
                BFArrowCommon.TagBlossomFluxLeftArrow(projectile);
            }

            SoundEngine.PlaySound(SoundID.Item5 with { Pitch = Main.rand.NextFloat(-0.12f, 0.05f), Volume = 0.9f }, player.Center);
        }

        private Vector2 GetAimVelocity(Player player, float speed)
        {
            Vector2 aimTarget = player.Calamity().mouseWorld;
            if (aimTarget == Vector2.Zero)
                aimTarget = Main.MouseWorld;

            Vector2 aimDirection = aimTarget - player.RotatedRelativePoint(player.MountedCenter);
            if (aimDirection == Vector2.Zero)
                aimDirection = Vector2.UnitX * player.direction;

            return aimDirection.SafeNormalize(Vector2.UnitX * player.direction) * speed;
        }

        private static Vector2 GetShootOrigin(Player player, Vector2 velocity)
        {
            Vector2 origin = player.RotatedRelativePoint(player.MountedCenter);
            Vector2 muzzleOffset = velocity.SafeNormalize(Vector2.UnitX * player.direction) * 24f;

            if (Collision.CanHit(origin, 0, 0, origin + muzzleOffset, 0, 0))
                origin += muzzleOffset;

            return origin;
        }

        private static void SyncUltimateDisplay(Player player, BFEXPlayer exPlayer)
        {
            if (!player.GetModPlayer<BFRightUIPlayer>().UltimateUnlocked)
            {
                if (player.Calamity().cooldowns.TryGetValue(BFEXCoolDown.ID, out var hiddenCooldown))
                    hiddenCooldown.timeLeft = 0;

                return;
            }

            if (player.Calamity().cooldowns.TryGetValue(BFEXCoolDown.ID, out var cooldown))
            {
                cooldown.timeLeft = exPlayer.UltimateChargeFrames;
            }
            else
            {
                player.AddCooldown(BFEXCoolDown.ID, exPlayer.UltimateChargeFrames);
            }
        }

        private static bool HasActiveSelectionPanel(Player player) =>
            player.ownedProjectileCounts[ModContent.ProjectileType<BFSelectionPanel>()] > 0;

        private static bool HasActiveRightHoldout(Player player) =>
            player.ownedProjectileCounts[ModContent.ProjectileType<BFRight_HoldOut>()] > 0;

        private static int GetBurstGroupInterval(Player player) =>
            player.GetModPlayer<BFTurretModePlayer>().TurretModeActive ? TurretBurstGroupInterval : NormalBurstGroupInterval;

        private static int GetEchoDelay(Player player) =>
            player.GetModPlayer<BFTurretModePlayer>().TurretModeActive ? TurretEchoDelay : NormalEchoDelay;

        private static int GetLeafDelayFromBurstStart(Player player) =>
            player.GetModPlayer<BFTurretModePlayer>().TurretModeActive ? TurretLeafDelayFromBurstStart : NormalLeafDelayFromBurstStart;

        private static BlossomFluxChloroplastPresetType GetDisplayedPreset()
        {
            if (Main.LocalPlayer?.active != true)
                return BlossomFluxChloroplastPresetType.Chlo_ABreak;

            return Main.LocalPlayer.GetModPlayer<BFRightUIPlayer>().CurrentPreset;
        }

        private void ToggleSelectionPanel(Player player)
        {
            int selectionPanelType = ModContent.ProjectileType<BFSelectionPanel>();
            bool panelFound = false;

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != selectionPanelType)
                    continue;

                projectile.ai[0] = 1f;
                projectile.netUpdate = true;
                panelFound = true;
            }

            if (panelFound)
                return;

            Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                player.Center,
                Vector2.Zero,
                selectionPanelType,
                0,
                0f,
                player.whoAmI);

            SoundEngine.PlaySound(SoundID.MenuOpen with { Pitch = 0.1f, Volume = 0.55f }, player.Center);
        }

        private void SpawnRightHoldout(Player player)
        {
            if (HasActiveSelectionPanel(player) || HasActiveRightHoldout(player))
                return;

            Vector2 shootDirection = GetAimVelocity(player, 1f).SafeNormalize(Vector2.UnitX * player.direction);

            Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                player.Center,
                shootDirection,
                ModContent.ProjectileType<BFRight_HoldOut>(),
                player.GetWeaponDamage(Item),
                Item.knockBack,
                player.whoAmI);
        }

        private static void CloseInteractiveProjectiles(Player player)
        {
            int selectionPanelType = ModContent.ProjectileType<BFSelectionPanel>();
            int holdoutType = ModContent.ProjectileType<BFRight_HoldOut>();

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != player.whoAmI)
                    continue;

                if (projectile.type != selectionPanelType && projectile.type != holdoutType)
                    continue;

                projectile.Kill();
                projectile.netUpdate = true;
            }
        }

        private static string GetLegendarySkillKeyText()
        {
            string assignedKeys = string.Join("/", KeybindSystem.LegendarySkill.GetAssignedKeys());
            return string.IsNullOrWhiteSpace(assignedKeys) ? "Unbound" : assignedKeys;
        }

        public override bool CanRightClick()
        {
            return true;
        }

        public override void RightClick(Player player)
        {
            BFRightUIPlayer rightUIPlayer = player.GetModPlayer<BFRightUIPlayer>();
            if (!rightUIPlayer.PassiveRainUnlocked)
            {
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.42f, Pitch = 0.16f }, player.Center);
                return;
            }

            rightUIPlayer.TogglePassiveRain();
            SoundEngine.PlaySound(
                rightUIPlayer.PassiveRainEnabled
                    ? SoundID.MenuTick with { Volume = 0.55f, Pitch = 0.08f }
                    : SoundID.MenuClose with { Volume = 0.5f, Pitch = -0.1f },
                player.Center);
        }

        public override bool ConsumeItem(Player player)
        {
            return false;
        }
    }
}
