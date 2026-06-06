using CalamityMod;
using CalamityMod.CalPlayer;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Malachite
{
    internal sealed class MalachitePlayer : ModPlayer
    {
        public const int RightClickCooldownFrames = 3 * 60;
        public const int DepletionBurstFrames = 90;

        private readonly HashSet<int> grazedProjectileIds = new();
        private bool holdingMalachite;
        private int grazeVisualCooldown;
        private int depletionBurstTimer;

        public int RightClickCooldown { get; private set; }

        public bool CanUseRightClick => RightClickCooldown <= 0;

        public float RightClickCooldownCompletion => 1f - RightClickCooldown / (float)RightClickCooldownFrames;

        public bool DepletionBurstActive => depletionBurstTimer > 0;

        public override void ResetEffects()
        {
            holdingMalachite = false;
        }

        public override void UpdateDead()
        {
            holdingMalachite = false;
            RightClickCooldown = 0;
            depletionBurstTimer = 0;
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
            if (RightClickCooldown > 0)
                RightClickCooldown--;

            if (depletionBurstTimer > 0)
                depletionBurstTimer--;

            if (grazeVisualCooldown > 0)
                grazeVisualCooldown--;

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

        public void StartRightClickCooldown()
        {
            RightClickCooldown = RightClickCooldownFrames;
        }

        public void RestoreStealthPoints(float points)
        {
            AddStealthPoints(points);
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

            Player.endurance += 0.15f;
            Player.moveSpeed += 0.15f;
            Player.maxRunSpeed += 0.35f;
            Player.runAcceleration *= 1.08f;
        }

        private void UpdateGrazeDetection()
        {
            grazedProjectileIds.RemoveWhere(id => id < 0 || id >= Main.maxProjectiles || !Main.projectile[id].active);

            Rectangle grazeBox = Player.Hitbox;
            grazeBox.Inflate(42, 42);

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!CanGrazeProjectile(projectile, grazeBox))
                    continue;

                grazedProjectileIds.Add(projectile.whoAmI);
                AddStealthPoints(1f);
                SpawnGrazeFeedback(projectile.Center);
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
                grazeVisualCooldown = 8;
                SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.25f, Pitch = 0.45f }, Player.Center);
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
    }
}
