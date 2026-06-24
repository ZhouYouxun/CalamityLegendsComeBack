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
        private int GetChainCount()
        {
            return Kind switch
            {
                CosmicDischargeAttackKind.ChainKnifeBiteAll => 5,
                CosmicDischargeAttackKind.ChainKnifeScatter => 4,
                _ => 3
            };
        }
        private System.Collections.Generic.List<Vector2> GenerateChainConvergencePoints(Vector2 direction, float reach, float lane)
        {
            System.Collections.Generic.List<Vector2> points = new();
            Vector2 startPos = Owner.MountedCenter;
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            float chainCount = Math.Max(1, GetChainCount() - 1);
            float laneOffset = lane * 18f;
            float curveSign = lane == 0f ? 0f : Math.Sign(lane);
            float curveStrength = Math.Abs(lane) / chainCount;
            int segments = 20;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float convergence = 1f - t;
                float side = laneOffset * convergence + curveSign * MathF.Sin(MathHelper.Pi * t) * MathHelper.Lerp(24f, 92f, curveStrength);
                float wave = MathF.Sin(Time * 0.42f + t * MathHelper.TwoPi) * 6f * curveStrength * convergence;
                points.Add(startPos + direction * (reach * t) + normal * (side + wave));
            }

            return points;
        }
        private void UpdateChainArcSwing()
        {
            // Spine-of-Thanatos-style convergence: chains meet at cursor and detonate.
            // Timing mirrors SpineOfThanatos: slow buildup → explosive mid-extension → fast retract.
            const int arcWindup = 6;
            int arcExtendFrames = Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll ? 52 : 26;
            int arcHoldFrames = 8;
            int arcSnapEnd = arcWindup + arcExtendFrames;
            int arcHoldEnd = arcSnapEnd + arcHoldFrames;
            // BiteAll retract = 92 - 66 = 26 ticks = ~13 real frames, matching SpineOfThanatos FlyBackTime.
            int arcTotalDuration = Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll ? 92 : arcHoldEnd + 18;

            float maxReach = Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll ? 560f : 500f;
            bool ultActive = Owner.GetModPlayer<CosmicDischargePlayer>().UltimateFieldActive;
            bool empActive = Owner.GetModPlayer<CosmicDischargePlayer>().DevourerAscensionActive;
            if (ultActive || empActive) maxReach *= 1.2f;

            if (Time <= arcWindup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            Vector2 desired = Owner.Calamity().mouseWorld - Owner.MountedCenter;
            if (desired.LengthSquared() < 0.001f)
                desired = AimDirection;
            Vector2 direction = desired.SafeNormalize(AimDirection);
            float targetReach = MathHelper.Clamp(desired.Length(), 160f, maxReach);

            if (Time <= arcWindup)
            {
                float prep = EaseOutCubic(Time / arcWindup);
                SetBlade(direction, MathHelper.Lerp(78f, targetReach * 0.36f, prep), 0f, 30f);
            }
            else if (Time <= arcSnapEnd)
            {
                float t = (Time - arcWindup) / (float)arcExtendFrames;
                // SpineOfThanatos-style: near-zero movement for first ~50% of time,
                // then explosive acceleration peaking at ~77%, slight decel at end.
                float extend = MathHelper.SmoothStep(0f, 1f, t * t);
                SetBlade(direction, MathHelper.Lerp(targetReach * 0.36f, targetReach, extend), 0f, 52f);

                PlayReleaseOnce(SoundID.Item71, 0.9f, 0.2f, 6.2f);

                // Trigger impact when chains reach ~92% of their destination.
                if (!impactEffectsPlayed && t >= 0.90f)
                {
                    EmitAirCrack(TipPosition, direction, 1.35f);
                    if (Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll)
                    {
                        CosmicDischargeCommon.SpawnChainFinisherBurst(Projectile.GetSource_FromThis(), Owner, TipPosition, direction, (int)(Projectile.damage * 0.6f), Projectile.knockBack);
                        SpawnRiftExplosion(TipPosition, empActive ? 220f : 184f, 0.82f);
                    }
                    else
                    {
                        float radius = Kind == CosmicDischargeAttackKind.ChainKnifeScatter ? 146f : 126f;
                        SpawnRiftExplosion(TipPosition, empActive ? radius * 1.2f : radius, 0.62f);
                    }
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

            if (Time >= arcTotalDuration)
                Projectile.Kill();

            var chainPoints = GetActivePoints();
            CosmicDischargeCommon.SpawnChainTrail(
                chainPoints,
                TipPosition,
                Projectile.velocity.SafeNormalize(AimDirection) * Math.Max(2f, Projectile.velocity.Length() / 18f),
                Kind == CosmicDischargeAttackKind.ChainKnifeScatter,
                Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll && Time <= arcSnapEnd);
        }
    }
}
