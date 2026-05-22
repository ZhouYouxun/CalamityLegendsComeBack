using System;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    internal sealed class AzureThunderPlayer : ModPlayer
    {
        public const int AttackManaCost = 8;
        public const int ThunderChargeMax = 10;
        public const int UltimateEnergyMax = 120;
        public const int HarmonyDuration = 25 * 60;
        public const int RightClickCooldownMax = 3 * 60;
        public const int AutoGroundSwordInterval = 10 * 60;
        public const int UltimateAutoGainInterval = 2 * 60;

        public int ThunderCharge;
        public int UltimateEnergy;
        public int RightClickCooldown;

        private bool holdingAzureThunder;
        private int autoGroundSwordTimer = AutoGroundSwordInterval;
        private int ultimateAutoGainTimer;
        private int lastThunderChargeGrantFrame = -9999;

        public bool HoldingAzureThunder =>
            holdingAzureThunder &&
            Player.HeldItem != null &&
            !Player.HeldItem.IsAir &&
            Player.HeldItem.type == ModContent.ItemType<AzureThunder>();

        public bool HarmonyActive => Player.HasBuff(ModContent.BuffType<AzureThunderHarmonyBuff>());

        public override void ResetEffects()
        {
            holdingAzureThunder = false;
        }

        public override void UpdateDead()
        {
            ThunderCharge = 0;
            UltimateEnergy = 0;
            RightClickCooldown = 0;
            autoGroundSwordTimer = AutoGroundSwordInterval;
            ultimateAutoGainTimer = 0;
        }

        public override void PostUpdate()
        {
            ThunderCharge = Utils.Clamp(ThunderCharge, 0, ThunderChargeMax);
            UltimateEnergy = Utils.Clamp(UltimateEnergy, 0, UltimateEnergyMax);

            if (RightClickCooldown > 0)
                RightClickCooldown--;

            if (HarmonyActive && Player.whoAmI == Main.myPlayer)
                EnsureHarmonyBar();

            if (HoldingAzureThunder)
            {
                HandleUltimateAutoGain();
                SyncCooldownDisplays();
                HandleUltimateInput();
                HandleAutomaticGroundSword();
            }
            else if (ThunderCharge > 0 || UltimateEnergy > 0)
            {
                autoGroundSwordTimer = AutoGroundSwordInterval;
            }
            else
            {
                autoGroundSwordTimer = AutoGroundSwordInterval;
            }

            if (HoldingAzureThunder)
                ApplyPassiveStatGrowth();
        }

        public void SetHoldingAzureThunder()
        {
            holdingAzureThunder = true;
        }

        public bool TrySpendMana()
        {
            return Player.CheckMana(Player.HeldItem, AttackManaCost, true, false);
        }

        public void AddThunderCharge(int amount)
        {
            if (amount <= 0)
                return;

            ThunderCharge = Utils.Clamp(ThunderCharge + amount, 0, ThunderChargeMax);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.35f, Pitch = 0.35f }, Player.Center);
        }

        public int ConsumeThunderCharge()
        {
            int consumed = ThunderCharge;
            ThunderCharge = 0;
            return consumed;
        }

        public void AddUltimateEnergy(int amount)
        {
            if (amount <= 0)
                return;

            int oldValue = UltimateEnergy;
            UltimateEnergy = Utils.Clamp(UltimateEnergy + amount, 0, UltimateEnergyMax);

            if (oldValue < UltimateEnergyMax && UltimateEnergy >= UltimateEnergyMax && Player.whoAmI == Main.myPlayer)
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.65f, Pitch = 0.1f }, Player.Center);
        }

        public void TryGainThunderChargeFromTarget(NPC target)
        {
            if (target == null || !target.active || target.friendly || target.dontTakeDamage)
                return;

            if (Main.GameUpdateCount - lastThunderChargeGrantFrame < 24)
                return;

            int stacks = CountElectroDebuffs(target);
            if (stacks <= 0)
                return;

            lastThunderChargeGrantFrame = (int)Main.GameUpdateCount;
            AddThunderCharge(Math.Min(3, stacks));
        }

        public void RestoreManaForOwnedSwords(bool includeLeftClickGrowth = false)
        {
            int amount = CountOwnedAzureThunderSwords(Player);
            if (includeLeftClickGrowth && AzureThunderProgression.DownedAnyMech)
                amount += CountOwnedAzureThunderSwords(Player) / 3 * 5;

            if (amount <= 0)
                return;

            int oldMana = Player.statMana;
            Player.statMana = Math.Min(Player.statManaMax2, Player.statMana + amount);
            int restored = Player.statMana - oldMana;
            if (restored > 0)
                Player.ManaEffect(restored);
        }

        public void RestoreManaFromConsumedCharge(int consumedCharge)
        {
            if (!AzureThunderProgression.DownedAnyMech || consumedCharge <= 0)
                return;

            int amount = consumedCharge * 5;
            int oldMana = Player.statMana;
            Player.statMana = Math.Min(Player.statManaMax2, Player.statMana + amount);
            int restored = Player.statMana - oldMana;
            if (restored > 0)
                Player.ManaEffect(restored);
        }

        public void RestoreLifeFromFourSymbols()
        {
            int amount = AzureThunderProgression.FourSymbolsLifeRestore;
            if (amount <= 0)
                return;

            int healAmount = Math.Min(amount, Player.statLifeMax2 - Player.statLife);
            if (healAmount <= 0)
                return;

            Player.statLife += healAmount;
            Player.HealEffect(healAmount, true);
        }

        private void HandleUltimateInput()
        {
            if (Main.myPlayer != Player.whoAmI || !KeybindSystem.LegendarySkill.JustPressed)
                return;

            if (UltimateEnergy < UltimateEnergyMax)
                return;

            UltimateEnergy = 0;
            ultimateAutoGainTimer = 0;
            Player.AddBuff(ModContent.BuffType<AzureThunderHarmonyBuff>(), HarmonyDuration);
            EnsureHarmonyBar();

            for (int i = 0; i < 36; i++)
            {
                Vector2 velocity = Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(2.5f, 8.5f);
                Dust dust = Dust.NewDustPerfect(Player.Center, DustID.FireworksRGB, velocity, 0, AzureThunderColors.Yellow, Main.rand.NextFloat(1.15f, 1.8f));
                dust.noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.85f, Pitch = 0.15f }, Player.Center);
        }

        private void HandleAutomaticGroundSword()
        {
            if (Main.myPlayer != Player.whoAmI)
                return;

            if (!AzureThunderProgression.FourSymbolsUnlocked)
                return;

            int groundSwordCount = CountOwnedGroundSwords(Player);
            if (groundSwordCount >= AzureThunderProgression.AutomaticSwordLimit)
            {
                autoGroundSwordTimer = AutoGroundSwordInterval;
                return;
            }

            if (autoGroundSwordTimer > 0)
            {
                autoGroundSwordTimer--;
                return;
            }

            SpawnGroundSword(Player, Player.Center + Main.rand.NextVector2CircularEdge(160f, 80f), Player.GetWeaponDamage(Player.HeldItem), Player.HeldItem.knockBack);
            RestoreLifeFromFourSymbols();
            autoGroundSwordTimer = AutoGroundSwordInterval;
        }

        private void ApplyPassiveStatGrowth()
        {
            if (AzureThunderProgression.DownedDesertScourge)
                Player.manaRegenBonus += CountOwnedAzureThunderSwords(Player);

            if (AzureThunderProgression.DownedFishron)
                Player.lifeRegen += 2;

            if (AzureThunderProgression.DownedYharon)
                Player.GetDamage(DamageClass.Magic) += 0.02f;
        }

        private void HandleUltimateAutoGain()
        {
            if (UltimateEnergy >= UltimateEnergyMax)
            {
                ultimateAutoGainTimer = 0;
                return;
            }

            ultimateAutoGainTimer++;
            if (ultimateAutoGainTimer < UltimateAutoGainInterval)
                return;

            ultimateAutoGainTimer = 0;
            AddUltimateEnergy(1);
        }

        private void SyncCooldownDisplays()
        {
            if (Player.Calamity().cooldowns.TryGetValue(AzureThunderChargeCooldown.ID, out var chargeCooldown))
                chargeCooldown.timeLeft = ThunderCharge;
            else
                Player.AddCooldown(AzureThunderChargeCooldown.ID, ThunderCharge);

            if (Player.Calamity().cooldowns.TryGetValue(AzureThunderUltimateCooldown.ID, out var ultimateCooldown))
                ultimateCooldown.timeLeft = UltimateEnergy;
            else
                Player.AddCooldown(AzureThunderUltimateCooldown.ID, UltimateEnergy);
        }

        private void EnsureHarmonyBar()
        {
            int barType = ModContent.ProjectileType<AzureThunderHarmonyBar>();
            if (Player.ownedProjectileCounts[barType] > 0)
                return;

            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Top,
                Vector2.Zero,
                barType,
                0,
                0f,
                Player.whoAmI);
        }

        public static int CountElectroDebuffs(NPC target)
        {
            int stacks = 0;
            if (target.HasBuff(BuffID.Electrified))
                stacks++;
            if (target.HasBuff(ModContent.BuffType<AuricRebuke>()))
                stacks++;
            if (target.HasBuff(ModContent.BuffType<GalvanicCorrosion>()))
                stacks++;
            if (target.HasBuff(ModContent.BuffType<StaticDischarge>()))
                stacks++;
            if (target.HasBuff(ModContent.BuffType<VermillionFlux>()))
                stacks++;
            if (target.HasBuff(ModContent.BuffType<ElementalMix>()))
                stacks++;
            if (target.HasBuff(ModContent.BuffType<MiracleBlight>()))
                stacks++;

            return stacks;
        }

        public static int CountOwnedAzureThunderSwords(Player player)
        {
            int count = 0;
            int groundType = ModContent.ProjectileType<AzureThunderGroundSword>();
            int flyingType = ModContent.ProjectileType<AzureThunderFlyingSword>();

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != player.whoAmI)
                    continue;

                if (projectile.type == groundType || projectile.type == flyingType)
                    count++;
            }

            return count;
        }

        public static int CountOwnedGroundSwords(Player player)
        {
            int count = 0;
            int groundType = ModContent.ProjectileType<AzureThunderGroundSword>();

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == groundType)
                    count++;
            }

            return count;
        }

        public static int CountGroundSwordsNear(Player player, Vector2 center, float radius)
        {
            int count = 0;
            int groundType = ModContent.ProjectileType<AzureThunderGroundSword>();
            float radiusSquared = radius * radius;

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != groundType)
                    continue;

                if (projectile.DistanceSQ(center) <= radiusSquared)
                    count++;
            }

            return count;
        }

        public static NPC FindMouseNearestTarget(Player player, float maxDistance = 1600f)
        {
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            if (mouseWorld == Vector2.Zero)
                mouseWorld = Main.MouseWorld;

            return FindNearestTarget(mouseWorld, maxDistance);
        }

        public static NPC FindNearestTarget(Vector2 point, float maxDistance = 1600f)
        {
            NPC bestTarget = null;
            float bestDistance = maxDistance;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.Distance(point, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        public static Vector2 GetMouseWorld(Player player)
        {
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }

        public static void SpawnGroundSword(Player player, Vector2 position, int damage, float knockback)
        {
            int groundType = ModContent.ProjectileType<AzureThunderGroundSword>();
            if (CountOwnedGroundSwords(player) >= AzureThunderGroundSword.MaxGroundSwords)
                return;

            int sword = Projectile.NewProjectile(
                player.GetSource_FromThis(),
                position,
                Vector2.Zero,
                groundType,
                Math.Max(1, damage),
                knockback,
                player.whoAmI);

            if (Main.projectile.IndexInRange(sword))
                ApplyProjectileGrowth(Main.projectile[sword]);
        }

        public static void ApplyProjectileGrowth(Projectile projectile)
        {
            if (AzureThunderProgression.DownedEvilTier2)
                projectile.ArmorPenetration += 2;
        }

        public static void ApplyUltimateDot(NPC target, int duration)
        {
            if (target == null || !target.active)
                return;

            if (AzureThunderProgression.DownedYharon)
                target.AddBuff(ModContent.BuffType<AuricRebuke>(), duration);
            else if (AzureThunderProgression.DownedDragonfolly)
                target.AddBuff(ModContent.BuffType<VermillionFlux>(), duration);
            else if (AzureThunderProgression.DownedWallOfFlesh)
                target.AddBuff(BuffID.Electrified, duration);
            else
                target.AddBuff(ModContent.BuffType<StaticDischarge>(), duration);

            if (AzureThunderProgression.DownedMoonLord)
                target.AddBuff(ModContent.BuffType<ElementalMix>(), duration);
        }

        public static void SpawnVerticalLightning(
            IEntitySource source,
            Vector2 impactPosition,
            NPC target,
            int damage,
            float knockback,
            int owner,
            bool gainCharge = false,
            bool applyStaticDischarge = false,
            bool big = false,
            int ultimateEnergyGain = 0,
            bool applyCrumbling = false,
            float spawnHeightMultiplier = 1f)
        {
            Vector2 targetPosition = target?.Center ?? impactPosition;
            Vector2 targetVelocity = target?.velocity ?? Vector2.Zero;
            float spawnDistance = 1000f * Math.Max(0.1f, spawnHeightMultiplier);
            Vector2 spawnPosition = targetPosition - Vector2.UnitY.RotatedByRandom(0.2f) * spawnDistance;
            Vector2 velocity = (targetPosition - spawnPosition + targetVelocity * 7.5f).SafeNormalize(Vector2.UnitY) * 30f;
            int flags = 0;
            if (gainCharge)
                flags |= AzureThunderStormLightning.GainChargeFlag;
            if (applyStaticDischarge)
                flags |= AzureThunderStormLightning.StaticDischargeFlag;
            if (big)
                flags |= AzureThunderStormLightning.BigLightningFlag;
            if (applyCrumbling)
                flags |= AzureThunderStormLightning.CrumblingFlag;

            SpawnDirectionalLightning(
                source,
                spawnPosition,
                velocity,
                Math.Max(1, damage),
                knockback,
                owner,
                flags,
                ultimateEnergyGain,
                big);
        }

        public static void SpawnDirectionalLightning(
            IEntitySource source,
            Vector2 spawnPosition,
            Vector2 velocity,
            int damage,
            float knockback,
            int owner,
            int flags = 0,
            int ultimateEnergyGain = 0,
            bool big = false)
        {
            int lightning = Projectile.NewProjectile(
                source,
                spawnPosition,
                velocity,
                ModContent.ProjectileType<AzureThunderStormLightning>(),
                Math.Max(1, damage),
                knockback,
                owner,
                velocity.ToRotation(),
                Main.rand.Next(100),
                flags);

            if (Main.projectile.IndexInRange(lightning))
            {
                Projectile projectile = Main.projectile[lightning];
                projectile.CritChance = Main.player[owner].GetWeaponCrit(Main.player[owner].HeldItem);
                ApplyProjectileGrowth(projectile);
                projectile.localAI[1] = ultimateEnergyGain;
                if (big)
                {
                    projectile.width = 70;
                    projectile.height = 70;
                }
            }
        }

        public static void SpawnFlatLightning(IEntitySource source, Vector2 position, Vector2 velocity, int damage, float knockback, int owner, float size = 1f)
        {
            int lightning = Projectile.NewProjectile(
                source,
                position,
                velocity.SafeNormalize(Vector2.UnitX) * Math.Max(velocity.Length(), 14f),
                ModContent.ProjectileType<AzureThunderFlatLightning>(),
                Math.Max(1, damage),
                knockback,
                owner,
                0f,
                size);

            if (Main.projectile.IndexInRange(lightning))
                ApplyProjectileGrowth(Main.projectile[lightning]);
        }
    }

    internal static class AzureThunderColors
    {
        public static readonly Color Yellow = new(255, 232, 66);
        public static readonly Color PaleYellow = new(255, 255, 194);
        public static readonly Color Azure = new(52, 192, 255);
        public static readonly Color DeepAzure = new(20, 88, 190);
    }
}
