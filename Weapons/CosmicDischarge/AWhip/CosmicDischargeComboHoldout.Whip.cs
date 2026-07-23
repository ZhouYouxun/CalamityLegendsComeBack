using System;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public partial class CosmicDischargeComboHoldout
    {
        private void UpdateWhipArc(float side)
        {
            int snapEnd = WhipArcWindup + WhipArcSnap;
            int holdEnd = snapEnd + WhipArcHold;
            float sign = side * Math.Sign(Owner.direction == 0 ? 1 : Owner.direction);

            var modPlayer = Owner.GetModPlayer<CosmicDischargePlayer>();
            bool ultActive = modPlayer.UltimateFieldActive;
            bool empActive = modPlayer.DevourerAscensionActive;
            float maxReach = WhipReach;
            if (ultActive || empActive)
                maxReach *= 1.25f;

            if (Time <= WhipArcWindup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            Vector2 baseDirection = AimDirection;
            if (Time <= WhipArcWindup)
            {
                float t = Time / WhipArcWindup;
                float prep = EaseOutCubic(t);
                SetBlade(baseDirection.RotatedBy(-sign * MathHelper.Lerp(0.98f, 0.32f, prep)), MathHelper.Lerp(64f, 160f, prep), -0.2f * sign, 24f);
            }
            else if (Time <= snapEnd)
            {
                float t = (Time - WhipArcWindup) / WhipArcSnap;
                float snap = SmootherStep(t);
                float over = MathF.Sin(MathHelper.Pi * t);
                SetBlade(baseDirection.RotatedBy(-sign * MathHelper.Lerp(0.32f, 0f, snap)), MathHelper.Lerp(160f, maxReach, snap), -0.1f * sign * (1f - snap), 38f + over * 10f);
                PlayReleaseOnce(new SoundStyle("CalamityMod/Sounds/Custom/LoudSwingWoosh") { MaxInstances = 3 }, 0.82f, side < 0f ? -0.18f : -0.04f, 4.6f);

                if (!apexSoundPlayed && t >= 0.999f)
                {
                    apexSoundPlayed = true;
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/WulfrumHookGrapple") { Volume = 0.7f, Pitch = -0.18f, MaxInstances = 3 }, TipPosition);
                    SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.42f, Pitch = -0.28f, MaxInstances = 3 }, TipPosition);
                    ApplyScreenShake(4.8f);
                }

                if (!impactEffectsPlayed && t >= 0.999f)
                {
                    // 整次挥鞭只画一道弧。这是鞭形唯一的"挥砍残像"。
                    CosmicDischargeCommon.SpawnSwingSmear(
                        Vector2.Lerp(Owner.MountedCenter, TipPosition, 0.72f),
                        baseDirection.ToRotation() + MathHelper.PiOver2,
                        MathHelper.Clamp(Projectile.velocity.Length() / 105f, 1.2f, 5.2f),
                        CosmicDischargeCommon.RiftLightBlue);

                    EmitAirCrack(TipPosition, baseDirection, 0.94f);
                    SpawnWhipRiftBombFan(baseDirection, ultActive || empActive ? 4 : 3, ultActive || empActive ? 0.48f : 0.36f);
                }
            }
            else if (Time <= holdEnd)
            {
                SetBlade(baseDirection, maxReach, 0f, 40f);
            }
            else
            {
                float t = Utils.GetLerpValue(holdEnd, WhipArcDuration, Time, true);
                currentlyRetracting = true;
                Projectile.localNPCHitCooldown = (ultActive || empActive) ? 4 : 120;
                float retract = SmootherStep(t);
                SetBlade(baseDirection, MathHelper.Lerp(maxReach, 18f, retract), 0f, MathHelper.Lerp(34f, 16f, retract));

                if (!retractSoundPlayed)
                {
                    retractSoundPlayed = true;
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/WulfrumHookDisengage") { Volume = 0.68f, Pitch = -0.12f, MaxInstances = 3 }, Owner.Center);
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.38f, Pitch = -0.32f, MaxInstances = 3 }, Owner.Center);
                    ApplyScreenShake(2.2f);
                }
            }

            if (Time >= WhipArcDuration)
                Projectile.Kill();

            // 逐帧拖尾统一由 AI() 里的 SpawnBladeWakeDust 处理，这里不再另开一套。
        }
        private void UpdateThrust(bool quickDraw)
        {
            if (!quickDraw)
            {
                UpdateHeavyThrust();
                return;
            }

            int duration = quickDraw ? QuickDrawDuration : 64;
            int windup = quickDraw ? QuickDrawWindup : WhipThrustWindup;
            int snapFrames = quickDraw ? 7 : 11;
            int holdFrames = quickDraw ? 9 : 8;
            int snapEnd = windup + snapFrames;
            int holdEnd = snapEnd + holdFrames;
            float maxReach = quickDraw ? QuickDrawReach : ThrustReach;

            if (Time <= windup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            Vector2 direction = AimDirection;
            if (Time <= windup)
            {
                float t = quickDraw ? 1f : EaseOutCubic(Time / windup);
                SetBlade(direction.RotatedBy(-0.1f * Owner.direction), MathHelper.Lerp(58f, quickDraw ? 126f : 156f, t), -0.04f * Owner.direction, quickDraw ? 26f : 28f);
                // 蓄力照 DoG 的节奏 —— DoGTeleportRift 整个蓄力期只脉冲 3 次，不是逐帧喷。
                if (quickDraw && (Time == 1f || Time == 8f || Time == 15f))
                    CosmicDischargeCommon.SpawnChargePulse(Owner.MountedCenter, Time / QuickDrawWindup, 0.7f);
            }
            else if (Time <= snapEnd)
            {
                float t = (Time - windup) / snapFrames;
                float snap = MathF.Sin(t * MathHelper.PiOver2);
                float over = MathF.Sin(MathHelper.Pi * t);
                SetBlade(direction, MathHelper.Lerp(126f, maxReach + (quickDraw ? 70f : 34f), snap), 0f, 42f + over * 10f);
                PlayReleaseOnce(SoundID.Item122, quickDraw ? 0.9f : 0.72f, quickDraw ? 0.28f : 0.06f, quickDraw ? 6.2f : 4.4f);

                if (!impactEffectsPlayed && t >= 0.72f)
                    EmitAirCrack(TipPosition, direction, quickDraw ? 1.35f : 0.92f);
            }
            else if (Time <= holdEnd)
            {
                SetBlade(direction, maxReach + (quickDraw ? 38f : 8f), 0f, quickDraw ? 48f : 40f);
            }
            else
            {
                float t = Utils.GetLerpValue(holdEnd, duration, Time, true);
                currentlyRetracting = true;
                Projectile.localNPCHitCooldown = quickDraw ? 3 : 16;
                float retract = MathF.Sin(t * MathHelper.PiOver2);
                SetBlade(direction.RotatedBy(Owner.direction * MathHelper.Lerp(0f, 0.18f, retract)), MathHelper.Lerp(maxReach, 52f, retract), 0.08f * Owner.direction, MathHelper.Lerp(36f, 18f, retract));
            }

            if (quickDraw && !quickDrawBurstPlayed && Time >= QuickDrawWindup)
            {
                quickDrawBurstPlayed = true;
                Vector2 burstCenter = Owner.Calamity().mouseWorld;
                if (Vector2.DistanceSquared(burstCenter, Owner.Center) < 32f * 32f)
                    burstCenter = TipPosition;

                CosmicDischargeCommon.SpawnQuickDrawFullBurst(Projectile.GetSource_FromThis(), Owner, burstCenter, Projectile.damage, Projectile.knockBack);
                SpawnRiftExplosion(burstCenter, 180f, 0.72f);
            }

            if (Time >= duration)
                Projectile.Kill();
        }

        private void UpdateHeavyThrust()
        {
            const int snapFrames = 14;
            const int holdFrames = 5;
            int snapEnd = WhipThrustWindup + snapFrames;
            int holdEnd = snapEnd + holdFrames;
            float maxReach = ThrustReach + 58f;

            if (Time <= WhipThrustWindup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            Vector2 direction = AimDirection;
            if (Time <= WhipThrustWindup)
            {
                float t = EaseOutCubic(Time / WhipThrustWindup);
                SetBlade(direction.RotatedBy(-0.08f * Owner.direction * (1f - t)), MathHelper.Lerp(52f, 118f, t), -0.05f * Owner.direction, 28f);
            }
            else if (Time <= snapEnd)
            {
                float t = (Time - WhipThrustWindup) / snapFrames;
                float extension = SmootherStep(t);
                float over = MathF.Sin(MathHelper.Pi * t);
                SetBlade(direction, MathHelper.Lerp(118f, maxReach, extension), 0f, 40f + over * 14f);

                if (!releaseSoundPlayed)
                    SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.46f, Pitch = -0.22f, MaxInstances = 3 }, Owner.Center);
                PlayReleaseOnce(new SoundStyle("CalamityMod/Sounds/Custom/WulfrumHookShoot") { MaxInstances = 3 }, 0.82f, -0.24f, 5.8f);

                if (!apexSoundPlayed && t >= 0.999f)
                {
                    apexSoundPlayed = true;
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/WulfrumHookGrapple") { Volume = 0.82f, Pitch = -0.24f, MaxInstances = 3 }, TipPosition);
                    SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.48f, Pitch = -0.32f, MaxInstances = 3 }, TipPosition);
                    ApplyScreenShake(6.4f);
                }

                if (!impactEffectsPlayed && t >= 0.999f)
                    EmitAirCrack(TipPosition, direction, 1.18f);
            }
            else if (Time <= holdEnd)
            {
                SetBlade(direction, maxReach, 0f, 46f);
            }
            else
            {
                float t = Utils.GetLerpValue(holdEnd, WhipThrustDuration, Time, true);
                currentlyRetracting = true;
                Projectile.localNPCHitCooldown = 16;
                float retract = SmootherStep(t);
                SetBlade(direction, MathHelper.Lerp(maxReach, 18f, retract), 0f, MathHelper.Lerp(42f, 16f, retract));

                if (!retractSoundPlayed)
                {
                    retractSoundPlayed = true;
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/WulfrumHookDisengage") { Volume = 0.8f, Pitch = -0.22f, MaxInstances = 3 }, Owner.Center);
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.46f, Pitch = -0.38f, MaxInstances = 3 }, Owner.Center);
                    ApplyScreenShake(3.2f);
                }
            }

            if (Time >= WhipThrustDuration)
                Projectile.Kill();
        }

        private void SpawnWhipRiftBombFan(Vector2 direction, int count, float damageFactor)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            count = Math.Max(1, count);
            float spread = MathHelper.ToRadians(24f);
            for (int i = 0; i < count; i++)
            {
                float offset = count == 1 ? 0f : MathHelper.Lerp(-spread, spread, i / (float)(count - 1));
                Vector2 velocity = direction.RotatedBy(offset) * Main.rand.NextFloat(8.5f, 12.5f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    TipPosition + direction * 8f,
                    velocity,
                    ModContent.ProjectileType<CosmicDischargeDoGRiftBomb>(),
                    (int)(Projectile.damage * damageFactor),
                    Projectile.knockBack * 0.35f,
                    Projectile.owner,
                    Main.rand.NextFloat(20f, 34f));
            }
        }
        private void SpawnQuickDrawBombs()
        {
            int bombCount = CosmicDischargeProgression.QuickDrawRiftBombCount;
            if (bombCount <= 0 || Main.myPlayer != Projectile.owner)
                return;

            if (spawnedBombBursts >= CosmicDischargeProgression.QuickDrawRiftBombBursts)
                return;

            bool shouldBurst = Time == 10f || Time == 16f || Time == 23f;
            if (!shouldBurst)
                return;

            spawnedBombBursts++;
            Vector2 direction = Projectile.velocity.SafeNormalize(AimDirection);
            for (int i = 0; i < bombCount; i++)
            {
                float t = (i + 0.5f) / bombCount;
                Vector2 spawnPosition = Owner.MountedCenter + direction * Projectile.velocity.Length() * t + Main.rand.NextVector2Circular(24f, 24f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    Main.rand.NextVector2Circular(1.8f, 1.8f),
                    ModContent.ProjectileType<CosmicDischargeDoGRiftBomb>(),
                    (int)(Projectile.damage * CosmicDischargeProgression.QuickDrawRiftBombDamageFactor),
                    0f,
                    Projectile.owner,
                    Main.rand.NextFloat(12f, 24f));
            }
        }
        private System.Collections.Generic.List<Vector2> GenerateWhipPoints(Vector2 direction, float reach)
        {
            float side = Kind == CosmicDischargeAttackKind.WhipOver ? -1f : 1f;
            return GenerateWhipPointsForSide(direction, reach, side);
        }
        private System.Collections.Generic.List<Vector2> GenerateWhipPointsForSide(Vector2 direction, float reach, float side)
        {
            System.Collections.Generic.List<Vector2> points = new System.Collections.Generic.List<Vector2>();
            Vector2 startPos = Owner.MountedCenter;
            Vector2 endPos = startPos + direction * reach;
            float facingSide = Math.Sign(Owner.direction == 0 ? 1 : Owner.direction) * side;

            float straightness;
            if (Time <= WhipArcWindup)
                straightness = MathHelper.Lerp(0.04f, 0.32f, SmootherStep(Time / WhipArcWindup));
            else if (Time <= WhipArcWindup + WhipArcSnap)
                straightness = MathHelper.Lerp(0.32f, 1f, SmootherStep((Time - WhipArcWindup) / WhipArcSnap));
            else
                straightness = 1f;

            float curveAmount = 1f - straightness;
            float sizeFactor = MathHelper.Clamp(reach / WhipReach, 0.32f, 1.2f);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2) * facingSide;

            Vector2 straightControlOne = startPos + direction * (reach / 3f);
            Vector2 straightControlTwo = startPos + direction * (reach * 2f / 3f);
            Vector2 curledControlOne = startPos - direction * (92f * sizeFactor) + normal * (166f * sizeFactor);
            Vector2 curledControlTwo = endPos - direction * (reach * 0.42f) + normal * (118f * sizeFactor);
            Vector2 controlOne = Vector2.Lerp(straightControlOne, curledControlOne, curveAmount);
            Vector2 controlTwo = Vector2.Lerp(straightControlTwo, curledControlTwo, curveAmount);

            const int segments = 18;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float inverse = 1f - t;
                Vector2 point = inverse * inverse * inverse * startPos +
                                3f * inverse * inverse * t * controlOne +
                                3f * inverse * t * t * controlTwo +
                                t * t * t * endPos;
                points.Add(point);
            }

            return points;
        }
        private System.Collections.Generic.List<Vector2> GenerateThrustPoints(Vector2 direction, float reach)
        {
            System.Collections.Generic.List<Vector2> points = new System.Collections.Generic.List<Vector2>();
            Vector2 startPos = Owner.MountedCenter;
            const int segments = 18;
            for (int i = 0; i <= segments; i++)
                points.Add(startPos + direction * (reach * i / segments));

            return points;
        }
    }
}
