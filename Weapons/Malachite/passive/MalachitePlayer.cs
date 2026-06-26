using CalamityLegendsComeBack.Accssory.MC.General;
using CalamityLegendsComeBack.Accssory.MC.PeacockBox;
using CalamityLegendsComeBack.Accssory.MC.PeacockScroll;
using CalamityLegendsComeBack.Weapons.Malachite.EXSkill;
using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Malachite
{
    internal sealed class MalachitePlayer : ModPlayer
    {
        public const int DepletionBurstFrames = 90;

        private bool holdingMalachite;
        private bool wasHoldingMalachite;
        private int depletionBurstTimer;
        private int rightFeatherGenerationTimer;
        private int rightClickBoostTimer;
        private int leftClickHasteTimer;
        private int leftClickScatterCounter;

        public bool DepletionBurstActive => depletionBurstTimer > 0;
        public bool LeftClickHasteActive => leftClickHasteTimer > 0;

        public override void ResetEffects()
        {
            holdingMalachite = false;
        }

        public override void UpdateDead()
        {
            holdingMalachite = false;
            wasHoldingMalachite = false;
            depletionBurstTimer = 0;
            rightFeatherGenerationTimer = 0;
            rightClickBoostTimer = 0;
            leftClickHasteTimer = 0;
            leftClickScatterCounter = 0;
        }

        public override void PostUpdateEquips()
        {
            if (Player.HeldItem.type != ModContent.ItemType<Malachite>())
                return;

            SetHoldingMalachite();
        }

        public override void PostUpdate()
        {
            if (depletionBurstTimer > 0)
                depletionBurstTimer--;
            if (leftClickHasteTimer > 0)
                leftClickHasteTimer--;
            if (rightClickBoostTimer > 0)
            {
                rightClickBoostTimer--;
                ApplyRightClickBoost();
            }

            TryGenerateRightFeather();

            bool currentlyHolding = Player.HeldItem != null && Player.HeldItem.type == ModContent.ItemType<Malachite>();

            if (currentlyHolding && !wasHoldingMalachite)
            {
                OnSwitchToMalachite();
            }
            else if (!currentlyHolding && wasHoldingMalachite)
            {
                OnSwitchAwayFromMalachite();
            }

            wasHoldingMalachite = currentlyHolding;

            if (!holdingMalachite || Player.HeldItem.type != ModContent.ItemType<Malachite>())
                return;

            ApplyShadowStepBonuses();
        }

        public void SetHoldingMalachite()
        {
            holdingMalachite = true;

            CalamityPlayer calamity = Player.Calamity();
            if (calamity.rogueStealthMax < 1f)
                calamity.rogueStealthMax = 1f;

            calamity.wearingRogueArmor = true;
        }

        public void RestoreStealthPoints(float points)
        {
            AddStealthPoints(points);
        }

        public void ConsumeHalfStealthAndRestore(CalamityPlayer calamity)
        {
            float previousStealth = calamity.rogueStealth;
            calamity.ConsumeStealthByAttacking();
            float bonusRestore = previousStealth * Player.GetModPlayer<MCGeneralPlayer>().StealthRestoreOnStealthStrike;
            calamity.rogueStealth = MathHelper.Clamp(calamity.rogueStealth + previousStealth * 0.5f + bonusRestore, 0f, calamity.rogueStealthMax);
            AddStealthPoints(15f);
            AdvanceFinaleCooldownFromStealthAttack();
        }

        public void StartDepletionBurst()
        {
            depletionBurstTimer = DepletionBurstFrames;
        }

        public void TriggerRightClickBoost()
        {
            rightClickBoostTimer = Math.Max(rightClickBoostTimer, MalachiteBalance.RightClickBoostFrames);
        }

        public void TriggerLeftClickHaste()
        {
            if (MalachiteBalance.RightClickLeftHasteUnlocked)
                leftClickHasteTimer = Math.Max(leftClickHasteTimer, MalachiteBalance.LeftClickHasteFrames);
        }

        public bool ConsumeLeftClickForScatterBurst()
        {
            if (!MalachiteBalance.LeftClickScatterUnlocked)
            {
                leftClickScatterCounter = 0;
                return false;
            }

            leftClickScatterCounter++;
            if (leftClickScatterCounter < MalachiteBalance.LeftClickScatterInterval)
                return false;

            leftClickScatterCounter = 0;
            return true;
        }

        public void AdvanceFinaleCooldownFromStealthAttack()
        {
            if (Player.Calamity().cooldowns.TryGetValue(MalachiteEXCooldown.ID, out var cooldown))
                cooldown.timeLeft = Math.Max(0, cooldown.timeLeft - MalachiteBalance.FinaleStealthAttackAdvanceFrames);
        }

        private void ApplyShadowStepBonuses()
        {
            CalamityPlayer calamity = Player.Calamity();
            if (calamity.rogueStealthMax <= 0f || calamity.rogueStealth < calamity.rogueStealthMax * 0.5f)
                return;

            Player.moveSpeed += 0.15f;
            Player.maxRunSpeed += 0.35f;
            Player.runAcceleration *= 1.08f;
        }

        private void ApplyRightClickBoost()
        {
            Player.moveSpeed += 0.15f;
            Player.maxRunSpeed += 0.9f;
            Player.jumpSpeedBoost += 1.15f;
            Player.runAcceleration *= 1.12f;
            SpawnRightClickBoostLines();
        }

        private void SpawnRightClickBoostLines()
        {
            if (Main.dedServ || !Main.rand.NextBool(4))
                return;

            Vector2 forward = Player.velocity.SafeNormalize(Vector2.UnitX * Player.direction);
            Vector2 backward = -forward;
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 position =
                Player.Center +
                backward * Main.rand.NextFloat(12f, 34f) +
                side * Main.rand.NextFloat(-12f, 12f) +
                Main.rand.NextVector2Circular(2f, 5f);
            Vector2 velocity = backward * Main.rand.NextFloat(2.2f, 4.4f) + Player.velocity * 0.12f;
            Color color = Color.Lerp(new Color(70, 255, 135), Color.White, Main.rand.NextFloat(0.08f, 0.22f)) * 0.76f;

            Particle line = Main.rand.NextBool()
                ? new LineParticle(position, velocity * 0.42f, false, Main.rand.Next(7, 11), Main.rand.NextFloat(0.34f, 0.48f), color)
                : new AltSparkParticle(position, velocity, false, Main.rand.Next(7, 10), Main.rand.NextFloat(0.28f, 0.42f), color);
            GeneralParticleHandler.SpawnParticle(line);
        }

        private void TryGenerateRightFeather()
        {
            if (Player.whoAmI != Main.myPlayer || Player.dead)
                return;

            if (!MalachiteBalance.RightClickUnlocked)
            {
                rightFeatherGenerationTimer = 0;
                return;
            }

            bool peacockBoxEquipped = Player.GetModPlayer<PeacockBoxPlayer>().PeacockBoxEquipped;
            bool peacockScrollEquipped = Player.GetModPlayer<PeacockScrollPlayer>().PeacockScrollEquipped;
            Item malachiteItem = FindMalachiteItem(out bool isHeld);
            if (malachiteItem == null || (!isHeld && !peacockBoxEquipped && !peacockScrollEquipped))
            {
                rightFeatherGenerationTimer = 0;
                return;
            }

            int currentFeathers = MalachiteRightFeather.CountStoredRightFeathers(Player);
            if (currentFeathers >= MalachiteBalance.RightFeatherMaxCount)
            {
                rightFeatherGenerationTimer = 0;
                if (peacockBoxEquipped)
                    TryAutoReleaseStoredRightFeathers(malachiteItem);

                return;
            }

            int baseDelay = (isHeld || peacockScrollEquipped)
                ? MalachiteBalance.RightFeatherGenerationFrames
                : MalachiteBalance.PeacockBoxFeatherGenerationFrames;
            int targetDelay = peacockScrollEquipped ? baseDelay / 2 : baseDelay;

            rightFeatherGenerationTimer++;
            if (rightFeatherGenerationTimer < targetDelay)
                return;

            rightFeatherGenerationTimer = 0;

            int damage = Player.GetWeaponDamage(malachiteItem);
            MalachiteRightFeather.TrySpawnStoredRightFeather(
                Player,
                Player.GetSource_FromThis(),
                damage,
                malachiteItem.knockBack);

            if (peacockBoxEquipped &&
                MalachiteRightFeather.CountStoredRightFeathers(Player) >= MalachiteBalance.RightFeatherMaxCount)
            {
                TryAutoReleaseStoredRightFeathers(malachiteItem);
            }
        }

        private Item FindMalachiteItem(out bool isHeld)
        {
            isHeld = Player.HeldItem != null && Player.HeldItem.type == ModContent.ItemType<Malachite>();
            if (isHeld)
                return Player.HeldItem;

            for (int i = 0; i < Player.inventory.Length; i++)
            {
                Item item = Player.inventory[i];
                if (item != null && item.type == ModContent.ItemType<Malachite>())
                    return item;
            }

            return null;
        }

        private void TryAutoReleaseStoredRightFeathers(Item malachiteItem)
        {
            if (MalachiteRightFeather.CountStoredRightFeathers(Player) < MalachiteBalance.RightFeatherMaxCount)
                return;

            Vector2 target = FindNearestAutoReleaseTarget(1600f) ?? GetMouseWorld();
            int damage = Player.GetWeaponDamage(malachiteItem);
            if (!MalachiteRightFeather.ReleaseStoredRightFeathers(
                Player,
                Player.GetSource_FromThis(),
                target,
                damage,
                malachiteItem.knockBack,
                stealthEnhanced: false))
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.74f, Pitch = 0.24f, MaxInstances = 3 }, Player.Center);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/PlagueSounds/PBGAttackSwitchShort") { Volume = 0.34f, Pitch = 0.35f, MaxInstances = 3 }, Player.Center);
        }

        private Vector2? FindNearestAutoReleaseTarget(float range)
        {
            NPC bestTarget = null;
            float bestDistance = range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.Distance(Player.Center, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget?.Center;
        }

        private Vector2 GetMouseWorld()
        {
            Vector2 mouseWorld = Player.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero
                ? Main.MouseWorld
                : mouseWorld;
        }

        private void AddStealthPoints(float points)
        {
            CalamityPlayer calamity = Player.Calamity();
            if (calamity.rogueStealthMax <= 0f)
                calamity.rogueStealthMax = 1f;

            float amount = calamity.rogueStealthMax * points / 100f;
            calamity.rogueStealth = MathHelper.Clamp(calamity.rogueStealth + amount, 0f, calamity.rogueStealthMax);
        }

        private void OnSwitchToMalachite()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            int rightFeathersCount = MalachiteRightFeather.CountStoredRightFeathers(Player);
            if (!MalachiteBalance.RightClickUnlocked)
                return;

            if (rightFeathersCount >= 1)
            {
                CalamityPlayer calamity = Player.Calamity();
                bool stealthStrike = calamity.StealthStrikeAvailable();
                var source = Player.GetSource_FromThis();
                int damage = Player.GetWeaponDamage(Player.HeldItem);
                float knockback = Player.HeldItem.knockBack;
                Vector2 mouseWorld = calamity.mouseWorld == Vector2.Zero ? Main.MouseWorld : calamity.mouseWorld;

                if (MalachiteRightFeather.ReleaseStoredRightFeathers(Player, source, mouseWorld, damage, knockback, stealthStrike))
                {
                    if (MalachiteBalance.RightClickMoveBoostUnlocked)
                        TriggerRightClickBoost();
                    if (MalachiteBalance.RightClickLeftHasteUnlocked)
                        TriggerLeftClickHaste();

                    if (stealthStrike)
                    {
                        if (MalachiteBalance.RightClickConsumesNoStealth)
                            AdvanceFinaleCooldownFromStealthAttack();
                        else
                            ConsumeHalfStealthAndRestore(calamity);
                    }
                    SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.86f, Pitch = stealthStrike ? -0.08f : 0.18f }, Player.Center);
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.56f, Pitch = 0.35f }, Player.Center);
                }
            }
        }

        private void OnSwitchAwayFromMalachite()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (MalachiteKunai.CountStoredPeacockKunai(Player) > 0)
            {
                CalamityPlayer calamity = Player.Calamity();
                Vector2 mouseWorld = calamity.mouseWorld == Vector2.Zero ? Main.MouseWorld : calamity.mouseWorld;
                Item malachiteItem = FindMalachiteItem(out _);
                int damage = malachiteItem != null ? Player.GetWeaponDamage(malachiteItem) : -1;
                float knockback = malachiteItem?.knockBack ?? 0f;

                MalachiteKunai.FireStoredPeacockKunaiAsLeftThrows(Player, mouseWorld, damage, knockback);
            }
        }
    }

    
}
