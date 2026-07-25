using System;
using System.Collections.Generic;
using System.Linq;
using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using CalamityMod.Projectiles.Melee;
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
        // 缚囚之悼同款抽打需要的矢量随机种子
        private Vector2 whipSeed1;
        private Vector2 whipSeed2;
        private Vector2 whipSeed3;

        private int GetChainCount()
        {
            return Kind switch
            {
                CosmicDischargeAttackKind.ChainKnifeBiteAll => 3, // 3 链抽打
                CosmicDischargeAttackKind.ChainKnifeScatter => 4,
                _ => 3
            };
        }

        private List<Vector2> GenerateChainConvergencePoints(Vector2 direction, float reach, float lane)
        {
            List<Vector2> points = new();
            Vector2 startPos = Owner.MountedCenter;

            // 如果是第3次重击 (ChainKnifeBiteAll)，使用缚囚之悼同款的双阶贝塞尔曲线抽打轨迹，但缩小至 0.85 倍
            if (Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll)
            {
                float seed = (lane + 2f) * 13.37f;
                float progress = MathHelper.Clamp(Time / 20f, 0f, 1f); // 持续 10 帧 (20 ticks)
                float orientation = lane >= 0 ? 1f : -1f;

                GenerateFlailWhipCurve(seed, direction, reach * 0.85f, out Vector2 c0, out Vector2 c1, out Vector2 c2, out Vector2 c3, progress, orientation);

                BezierCurve curve = new(new Vector2[] { c0, c1, c2, c3 });
                int segments = 14;
                for (int i = 0; i <= segments; i++)
                {
                    points.Add(curve.Evaluate(i / (float)segments));
                }
                return points;
            }

            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            int extendFrames = 26;
            float flightProgress = Utils.GetLerpValue(6f, 6f + extendFrames, Time, true);

            float fanEnvelope = MathF.Sin(flightProgress * MathHelper.Pi);
            float laneSpacing = 30f + Math.Abs(lane) * 10f;
            Vector2 endpoint = startPos + direction * reach + normal * lane * laneSpacing * fanEnvelope;
            int normalSegments = 12;

            for (int i = 0; i <= normalSegments; i++)
            {
                float t = i / (float)normalSegments;
                points.Add(Vector2.Lerp(startPos, endpoint, t));
            }

            return points;
        }

        /// <summary>
        /// 缚囚之悼 (Lamentations of the Chained) 同款的动态 Bezier 抽打曲线生成算法（按 0.85 比例缩放）
        /// </summary>
        private void GenerateFlailWhipCurve(float seed, Vector2 direction, float baseReach, out Vector2 control0, out Vector2 control1, out Vector2 control2, out Vector2 control3, float progress, float orientation)
        {
            float rand = 0.5f + ((float)Math.Sin(seed * 17.07947) + (float)Math.Sin(seed * 0.2f * 25.13274)) * 0.25f;
            float seed1 = 0.5f + ((float)Math.Sin(rand * 100f * 17.07947) + (float)Math.Sin(rand * 100f * 0.2f * 25.13274)) * 0.25f;
            float seed2 = 0.5f + ((float)Math.Sin(seed1 * 50f * 17.07947) + (float)Math.Sin(seed1 * 50f * 0.2f * 25.13274)) * 0.25f;
            float seed3 = 0.5f + ((float)Math.Sin(seed2 * 17.07947) + (float)Math.Sin(seed2 * 0.2f * 25.13274)) * 0.25f;

            if ((orientation == -1 && rand >= 0.5f) || (orientation == 1 && rand < 0.5f))
                rand = 1f - rand;

            rand += progress * 0.1f * orientation;
            float flip = rand >= 0.5f ? 1f : -1f;

            control0 = Owner.MountedCenter + direction.RotatedBy(MathHelper.ToRadians(MathHelper.Lerp(MathHelper.PiOver4 * 0.3f * flip, MathHelper.PiOver4 * flip, rand))) * MathHelper.Lerp(40f, 110f, (float)Math.Sin(rand * MathHelper.Pi)) * 0.85f;

            float easedShift = progress == 1f ? 1f : 1f - (float)Math.Pow(2, -10 * progress);
            float reach = baseReach * (0.75f + 0.75f * seed2 - 0.05f * easedShift);
            control3 = Owner.MountedCenter + (direction * reach).RotatedBy(MathHelper.Lerp(-0.01f * flip, 0.01f * flip, easedShift));

            Vector2 point1 = control3 + direction.RotatedBy(MathHelper.PiOver2) * 40f * 0.85f;
            Vector2 point2 = control3 + direction.RotatedBy(MathHelper.PiOver2) * 40f * 0.85f + direction * 210f * 0.85f;
            Vector2 point3 = control3 + direction.RotatedBy(-MathHelper.PiOver2) * 40f * 0.85f + direction * 210f * 0.85f;
            Vector2 point4 = control3 + direction.RotatedBy(-MathHelper.PiOver2) * 40f * 0.85f;
            BezierCurve curve = new(new Vector2[] { point1, point2, point3, point4 });
            control3 = curve.Evaluate(rand);

            Vector2 directionFromHead = direction.RotatedBy(MathHelper.ToRadians(MathHelper.Lerp(0, 160f * flip, (float)Math.Sin(rand * MathHelper.Pi)))) * MathHelper.Lerp(110f, 170f, MathHelper.Clamp((float)Math.Sin(rand * MathHelper.Pi) - 0.5f, 0f, 1f) * 2f) * 0.85f;
            control2 = control3 + directionFromHead;

            Vector2 directionFromSecondToLastPoint = Utils.SafeNormalize(directionFromHead.RotatedBy(MathHelper.Pi - MathHelper.ToRadians(MathHelper.Lerp(80f * flip, 110f * flip, (float)Math.Sin(rand * MathHelper.Pi)))), Vector2.Zero) * MathHelper.Lerp(100f, 240f, (float)Math.Sin(rand * MathHelper.Pi)) * 0.85f;
            control1 = control2 + directionFromSecondToLastPoint;

            control3 += Vector2.UnitX.RotatedBy(MathHelper.TwoPi * seed1) * seed3 * 25f;
            control2 += Vector2.UnitX.RotatedBy(MathHelper.TwoPi * seed2) * seed1 * 25f;
            control1 += Vector2.UnitX.RotatedBy(MathHelper.TwoPi * seed3) * seed2 * 25f;
        }

        private void UpdateChainArcSwing()
        {
            // 第3下重击改成连续的缚囚之悼同款抽打：持续 10 帧（对应 20 个逻辑 ticks，因为 extraUpdates = 1）
            int arcTotalDuration = Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll ? 20 : 52;
            const int arcWindup = 3;

            float maxReach = Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll ? 440f : 500f;
            bool ultActive = Owner.GetModPlayer<CosmicDischargePlayer>().UltimateFieldActive;
            bool empActive = Owner.GetModPlayer<CosmicDischargePlayer>().DevourerAscensionActive;
            if (ultActive || empActive) maxReach *= 1.18f;

            if (Time <= arcWindup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            Vector2 desired = Owner.Calamity().mouseWorld - Owner.MountedCenter;
            if (desired.LengthSquared() < 0.001f)
                desired = AimDirection;
            Vector2 direction = desired.SafeNormalize(AimDirection);
            float targetReach = MathHelper.Clamp(desired.Length(), 160f, maxReach);

            if (Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll)
            {
                // 缚囚之悼同款 3 链迅捷抽打
                SetBlade(direction, targetReach, 0f, 42f);

                if ((int)Time % 4 == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.85f, Pitch = 0.15f }, Owner.Center);
                    ApplyScreenShake(3.5f);
                }

                // 抽打期间持续释放斩切破空痕
                if (Main.rand.NextBool(2))
                {
                    EmitAirCrack(TipPosition, direction, 1.15f);
                    Vector2 sliceDir = Main.rand.NextVector2CircularEdge(40f, 80f) * 0.85f;
                    Particle slice = new LineVFX(Owner.MountedCenter + direction * targetReach * 0.6f - sliceDir, sliceDir * 2f, 0.2f, CosmicDischargeCommon.RiftTwilight * 0.7f)
                    {
                        Lifetime = 6
                    };
                    GeneralParticleHandler.SpawnParticle(slice);
                }
            }
            else
            {
                // 第 1 下和第 2 下重击维持原有逻辑
                int arcExtendFrames = 26;
                int arcHoldFrames = 8;
                int arcSnapEnd = arcWindup + arcExtendFrames;
                int arcHoldEnd = arcSnapEnd + arcHoldFrames;

                if (Time <= arcWindup)
                {
                    float prep = EaseOutCubic(Time / arcWindup);
                    SetBlade(direction, MathHelper.Lerp(78f, targetReach * 0.36f, prep), 0f, 30f);
                }
                else if (Time <= arcSnapEnd)
                {
                    float t = (Time - arcWindup) / (float)arcExtendFrames;
                    float extend = MathHelper.SmoothStep(0f, 1f, t * t);
                    SetBlade(direction, MathHelper.Lerp(targetReach * 0.36f, targetReach, extend), 0f, 52f);

                    PlayReleaseOnce(SoundID.Item71, 0.9f, 0.2f, 6.2f);

                    if (!impactEffectsPlayed && t >= 0.90f)
                    {
                        EmitAirCrack(TipPosition, direction, 1.35f);
                        float radius = Kind == CosmicDischargeAttackKind.ChainKnifeScatter ? 146f : 126f;
                        SpawnRiftExplosion(TipPosition, empActive ? radius * 1.2f : radius, 0.62f);
                    }
                }
                else if (Time <= arcHoldEnd)
                {
                    SetBlade(direction, targetReach, 0f, 38f);
                }
                else
                {
                    float t = Utils.GetLerpValue(arcHoldEnd, arcTotalDuration, Time, true);
                    currentlyRetracting = true;
                    Projectile.localNPCHitCooldown = 8;
                    float retract = MathF.Sin(t * MathHelper.PiOver2);
                    SetBlade(direction, MathHelper.Lerp(targetReach, 55f, retract), 0f, MathHelper.Lerp(34f, 16f, retract));
                }
            }

            if (Time >= arcTotalDuration)
                Projectile.Kill();
        }
    }
}
