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
                SetBlade(baseDirection.RotatedBy(-sign * MathHelper.Lerp(0.82f, 0.24f, prep)), MathHelper.Lerp(76f, 178f, prep), -0.18f * sign, 24f);
            }
            else if (Time <= snapEnd)
            {
                float t = (Time - WhipArcWindup) / WhipArcSnap;
                float snap = MathF.Sin(t * MathHelper.PiOver2);
                float over = MathF.Sin(MathHelper.Pi * t);
                SetBlade(baseDirection.RotatedBy(MathHelper.Lerp(-0.2f, 0.05f, snap) * sign), MathHelper.Lerp(178f, maxReach + 30f, snap), -0.08f * sign, 38f + over * 8f);
                PlayReleaseOnce(SoundID.Item71, 0.86f, side < 0f ? -0.22f : 0.05f, 4.1f);

                if (!impactEffectsPlayed && t >= 0.7f)
                {
                    // 整次挥鞭只画一道弧。这是鞭形唯一的"挥砍残像"。
                    CosmicDischargeCommon.SpawnSwingSmear(
                        Vector2.Lerp(Owner.MountedCenter, TipPosition, 0.58f),
                        Projectile.velocity.ToRotation() + MathHelper.PiOver2,
                        MathHelper.Clamp(Projectile.velocity.Length() / 90f, 1.2f, 5.5f),
                        CosmicDischargeCommon.RiftLightBlue);

                    EmitAirCrack(TipPosition, baseDirection, 0.8f);
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
                float retract = MathF.Sin(t * MathHelper.PiOver2);
                SetBlade(baseDirection.RotatedBy(sign * MathHelper.Lerp(0.08f, 0.28f, retract)), MathHelper.Lerp(maxReach, 64f, retract), 0.12f * sign, MathHelper.Lerp(30f, 18f, retract));
            }

            if (Time >= WhipArcDuration)
                Projectile.Kill();

            // 逐帧拖尾统一由 AI() 里的 SpawnBladeWakeDust 处理，这里不再另开一套。
        }
        private void UpdateThrust(bool quickDraw)
        {
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
            points.Add(startPos);

            int directionSign = Owner.direction * (int)side;

            float t = Time / (float)WhipArcDuration;
            float progress = 0f;
            float retreatProgress = 0f;

            float p = t * 1.5f;
            if (p > 1.0f)
            {
                retreatProgress = (p - 1.0f) / 0.5f;
                progress = MathHelper.Lerp(1f, 0f, retreatProgress);
            }
            else
            {
                progress = p;
            }

            var modPlayer = Owner.GetModPlayer<CosmicDischargePlayer>();
            if (modPlayer.UltimateFieldActive || modPlayer.DevourerAscensionActive)
            {
                reach *= 1.25f;
            }
            int segments = 18;
            float totalLength = reach;
            float segLen = totalLength / segments;

            float angleStep = (float)Math.PI * 8f * (1f - progress) * (float)(-directionSign) / segments;

            Vector2 currentPos = startPos;
            float baseAngle = direction.ToRotation();
            float leftAngle = baseAngle - (float)Math.PI / 2f;
            float rightAngle = baseAngle + (float)Math.PI / 2f;

            for (int i = 0; i < segments; i++)
            {
                float ratio = (float)i / segments;
                float currentStep = angleStep * ratio;

                Vector2 extendVec = currentPos + baseAngle.ToRotationVector2() * segLen;
                Vector2 leftVec = currentPos + leftAngle.ToRotationVector2() * (segLen * 1.5f);
                Vector2 rightVec = currentPos + rightAngle.ToRotationVector2() * (segLen * 1.5f);

                float invProgress = 1f - progress;
                float lerpWeight = 1f - invProgress * invProgress;

                Vector2 temp = Vector2.Lerp(leftVec, extendVec, lerpWeight * 0.9f + 0.1f);
                Vector2 targetVec = Vector2.Lerp(rightVec, temp, lerpWeight * 0.7f + 0.3f);

                Vector2 rawPoint = startPos + (targetVec - startPos);
                
                float rotFactor = retreatProgress * retreatProgress;
                Vector2 finalPoint = rawPoint.RotatedBy(4.712389f * rotFactor * (float)directionSign, startPos);

                points.Add(finalPoint);

                baseAngle += currentStep;
                leftAngle += currentStep;
                rightAngle += currentStep;
                currentPos = extendVec;
            }

            return points;
        }
        private System.Collections.Generic.List<Vector2> GenerateThrustPoints(Vector2 direction, float reach)
        {
            System.Collections.Generic.List<Vector2> points = new System.Collections.Generic.List<Vector2>();
            Vector2 startPos = Owner.MountedCenter;
            points.Add(startPos);
            int segments = 18;
            
            float shake = MathF.Sin(Time * 0.8f) * 12f * (1f - Time / WhipThrustDuration);
            Vector2 perp = direction.RotatedBy(MathHelper.PiOver2);

            for (int i = 1; i <= segments; i++)
            {
                float ratio = (float)i / segments;
                Vector2 linePos = startPos + direction * (reach * ratio);
                float wave = MathF.Sin(ratio * MathHelper.TwoPi + Time * 0.6f) * shake;
                points.Add(linePos + perp * wave);
            }

            return points;
        }
    }
}
