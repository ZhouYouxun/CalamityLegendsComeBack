using CalamityLegendsComeBack.Weapons.Malachite.passive;
using CalamityMod;
using CalamityMod.CalPlayer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
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

        private readonly HashSet<int> grazedProjectileIds = new();
        private bool holdingMalachite;
        private bool wasHoldingMalachite;
        private int grazeVisualCooldown;
        private int depletionBurstTimer;
        private int rightFeatherGenerationTimer;

        public bool DepletionBurstActive => depletionBurstTimer > 0;

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
            grazedProjectileIds.Clear();
            grazeVisualCooldown = 0;
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

            if (grazeVisualCooldown > 0)
                grazeVisualCooldown--;

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

            if (Player.whoAmI == Main.myPlayer)
                UpdateGrazeDetection();
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
            calamity.rogueStealth = MathHelper.Clamp(calamity.rogueStealth + previousStealth * 0.5f, 0f, calamity.rogueStealthMax);
            AddStealthPoints(15f);
        }

        public void StartDepletionBurst()
        {
            depletionBurstTimer = DepletionBurstFrames;
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

        private void TryGenerateRightFeather()
        {
            if (Player.whoAmI != Main.myPlayer || Player.dead)
                return;

            Item malachiteItem = null;
            bool isHeld = false;

            if (Player.HeldItem != null && Player.HeldItem.type == ModContent.ItemType<Malachite>())
            {
                malachiteItem = Player.HeldItem;
                isHeld = true;
            }
            else
            {
                for (int i = 0; i < 10; i++)
                {
                    Item item = Player.inventory[i];
                    if (item != null && item.type == ModContent.ItemType<Malachite>())
                    {
                        malachiteItem = item;
                        break;
                    }
                }
            }

            if (malachiteItem == null)
            {
                rightFeatherGenerationTimer = 0;
                return;
            }

            int currentFeathers = MalachiteRightFeather.CountStoredRightFeathers(Player);
            if (currentFeathers >= MalachiteBalance.RightFeatherMaxCount)
            {
                rightFeatherGenerationTimer = 0;
                return;
            }

            int targetDelay = isHeld ? 45 : 180;

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
        }

        private void UpdateGrazeDetection()
        {
            if (grazeVisualCooldown > 0)
                return;

            grazedProjectileIds.RemoveWhere(id => id < 0 || id >= Main.maxProjectiles || !Main.projectile[id].active);

            Rectangle grazeBox = Player.Hitbox;
            grazeBox.Inflate(42, 42);

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!CanGrazeProjectile(projectile, grazeBox))
                    continue;

                grazedProjectileIds.Add(projectile.whoAmI);
                AddStealthPoints(5f);
                SpawnGrazeFeedback(projectile.Center);
                return;
            }
        }

        private bool CanGrazeProjectile(Projectile projectile, Rectangle grazeBox)
        {
            if (!projectile.active ||
                !projectile.hostile ||
                projectile.friendly ||
                projectile.damage <= 0 ||
                projectile.owner == Player.whoAmI ||
                grazedProjectileIds.Contains(projectile.whoAmI))
            {
                return false;
            }

            if (projectile.Hitbox.Intersects(Player.Hitbox))
                return false;

            return projectile.Hitbox.Intersects(grazeBox);
        }

        private void AddStealthPoints(float points)
        {
            CalamityPlayer calamity = Player.Calamity();
            if (calamity.rogueStealthMax <= 0f)
                calamity.rogueStealthMax = 1f;

            float amount = calamity.rogueStealthMax * points / 100f;
            calamity.rogueStealth = MathHelper.Clamp(calamity.rogueStealth + amount, 0f, calamity.rogueStealthMax);
        }

        private void SpawnGrazeFeedback(Vector2 center)
        {
            if (grazeVisualCooldown <= 0)
            {
                grazeVisualCooldown = 10;
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.28f, Pitch = 0.55f }, Player.Center);
                if (Player.whoAmI == Main.myPlayer)
                {
                    Projectile.NewProjectile(
                        Player.GetSource_FromThis(),
                        Player.Center,
                        Main.rand.NextVector2CircularEdge(1f, 1f),
                        ModContent.ProjectileType<MalachiteGrazeSlashVisual>(),
                        0,
                        0f,
                        Player.whoAmI,
                        Main.rand.NextFloat(MathHelper.TwoPi));
                }
            }

            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.Terra,
                    Main.rand.NextVector2Circular(2.4f, 2.4f),
                    80,
                    new Color(120, 255, 150),
                    Main.rand.NextFloat(0.75f, 1.15f));
                dust.noGravity = true;
            }
        }

        private void OnSwitchToMalachite()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            int rightFeathersCount = MalachiteRightFeather.CountStoredRightFeathers(Player);
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
                    if (stealthStrike)
                    {
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
                MalachiteKunai.FireStoredPeacockKunaiAsLeftThrows(Player, mouseWorld);
            }
        }
    }

    
}
