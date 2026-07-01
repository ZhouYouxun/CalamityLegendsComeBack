using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Projectiles.Typeless;
using CalamityMod.Skies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{


    internal static class CosmicDischargeCommon
    {
        public const string ChainTexturePath = "CalamityLegendsComeBack/Weapons/CosmicDischarge/LeftClick/CosmicDischargeFlail";
        public const string RingTexturePath = "CalamityMod/Particles/BloomRing";
        private const int ChainHandleHeight = 62;
        private const int ChainBodyStartY = 64;
        private const int ChainBodyHeight = 28;
        private const int ChainTailStartY = 114;
        private const int ChainTailHeight = 84;
        private const float ChainBodyStartOffset = 30f;
        public const float ShockwaveFinalScaleMultiplier = 0.3f;
        public static readonly Color DoGCyanColor = Color.Cyan;
        public static readonly Color DoGFuchsiaColor = Color.Fuchsia;
        public static readonly Color DoGPurpleColor = new(136, 26, 186);
        public static readonly Color DoGSkyBlueColor = Color.SkyBlue;
        public static readonly Color DoGAliceColor = Color.AliceBlue;
        public static readonly Color DoGWhiteColor = Color.White;
        public static readonly Color DoGBlackColor = new(0, 0, 0, 0);

        public static Color DoGSpecialColor =>
            Color.Lerp(
                DoGFuchsiaColor,
                DoGCyanColor,
                MathHelper.SmoothStep(0f, 1f, (MathF.Sin(Main.GlobalTimeWrappedHourly * 2f) + 1f) * 0.5f));

        public static Color ThreeColorSpark =>
            Color.Lerp(
                Color.Lerp(DoGFuchsiaColor, DoGAliceColor, Main.rand.NextFloat(0.5f)),
                DoGCyanColor,
                Main.rand.NextFloat());

        public static Color GetModeColor(CosmicDischargeAttackMode mode) => mode switch
        {
            CosmicDischargeAttackMode.Whip => DoGCyanColor,
            CosmicDischargeAttackMode.Sword => DoGFuchsiaColor,
            CosmicDischargeAttackMode.ChainKnife => DoGPurpleColor,
            _ => DoGSpecialColor
        };

        public static Color Transparent(Color color) => new(color.R, color.G, color.B, 0);

        public static Color RandomDoGColor(bool includePurple = true)
        {
            int max = includePurple ? 4 : 3;
            return Main.rand.Next(max) switch
            {
                0 => DoGCyanColor,
                1 => DoGFuchsiaColor,
                2 => DoGSpecialColor,
                _ => DoGPurpleColor
            };
        }

        public static Vector2 GetAimDirection(Player player, Vector2 fallback)
        {
            Vector2 mouse = player.Calamity().mouseWorld;
            Vector2 direction = mouse - player.MountedCenter;
            if (direction.LengthSquared() < 0.001f)
                direction = fallback;

            if (direction.LengthSquared() < 0.001f)
                direction = Vector2.UnitX * player.direction;

            return direction.SafeNormalize(Vector2.UnitX * player.direction);
        }

        public static void HoldPlayer(Player player, Projectile projectile, Vector2 aimDirection, float armRotationOffset = 0f)
        {
            player.ChangeDir(aimDirection.X >= 0f ? 1 : -1);
            player.heldProj = projectile.whoAmI;
            player.itemTime = 2;
            player.itemAnimation = 2;
            player.itemRotation = aimDirection.ToRotation();

            float armRotation = aimDirection.ToRotation() - MathHelper.PiOver2 + armRotationOffset;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, armRotation);
        }

        public static List<Vector2> BuildCurvedBlade(Player player, Vector2 direction, float reach, float sideBend, float curl, int pointCount = 18)
        {
            List<Vector2> points = new(pointCount);
            Vector2 start = player.MountedCenter;
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                float forward = reach * t;
                float bend = sideBend * MathF.Sin(MathHelper.Pi * t);
                float wave = curl * MathF.Sin(MathHelper.TwoPi * t) * (1f - t * 0.35f);
                points.Add(start + direction * forward + normal * (bend + wave));
            }

            return points;
        }

        public static bool CheckCurveCollision(IReadOnlyList<Vector2> points, Rectangle targetHitbox, float width)
        {
            if (points == null || points.Count < 2)
                return false;

            for (int i = 0; i < points.Count - 1; i++)
            {
                float collisionPoint = 0f;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), points[i], points[i + 1], width, ref collisionPoint))
                    return true;
            }

            return false;
        }

        public static bool TargetIntersectsTip(IReadOnlyList<Vector2> points, Rectangle targetHitbox, float radius)
        {
            if (points == null || points.Count == 0)
                return false;

            Vector2 tip = points[^1];
            Vector2 closest = Vector2.Clamp(targetHitbox.Center.ToVector2(), targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.DistanceSquared(closest, tip) <= radius * radius;
        }

        public static void ApplyDoGDebuffs(NPC target, int duration)
        {
            if (target == null || !target.active)
                return;

            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), duration);
        }

        public static void SpawnWhipOverTrail(Player player, Vector2 handle, Vector2 tip, float whipAngle, float whipLength)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new ElectricSpark(
                tip + Main.rand.NextVector2Circular(15f, 15f),
                Main.rand.NextVector2Circular(1.5f, 1.5f),
                DoGSpecialColor,
                DoGFuchsiaColor,
                0.3f * 0.3f,
                6,
                MathHelper.PiOver4 * 0.5f,
                6f,
                0.75f));
            GeneralParticleHandler.SpawnParticle(new NanoParticle(
                handle + Main.rand.NextVector2Circular(8f, 8f),
                Main.rand.NextVector2Circular(1.5f, 1.5f),
                DoGCyanColor,
                0.5f * 0.3f,
                18,
                emitsLight: true));
            GeneralParticleHandler.SpawnParticle(new SemiCircularSmearVFX(
                Vector2.Lerp(handle, tip, 0.58f),
                DoGSpecialColor * 0.7f,
                whipAngle + MathHelper.PiOver2,
                MathHelper.Clamp(whipLength / 80f, 0.8f, 7.2f),
                new Vector2(1f, 1.3f)));

            if (Main.rand.NextBool(2))
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    tip + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextVector2Circular(2f, 2f) - Vector2.UnitY * 1.5f,
                    DoGFuchsiaColor * 0.6f,
                    12,
                    0.8f * 0.3f,
                    0.5f,
                    0.02f,
                    true));
        }

        public static void SpawnWhipUnderTrail(Vector2 handle, Vector2 tip, float whipAngle, Vector2 swingDirection, float whipLength)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new CircularSmearVFX(
                Vector2.Lerp(handle, tip, 0.58f),
                DoGCyanColor * 0.6f,
                whipAngle + MathHelper.Pi + MathHelper.PiOver2,
                MathHelper.Clamp(whipLength / 70f, 0.8f, 7.6f)));

            if ((int)Main.GameUpdateCount % 3 == 0)
            {
                Vector2 velocity = swingDirection.SafeNormalize(Vector2.UnitX) * 4f;
                GeneralParticleHandler.SpawnParticle(new BoltParticle(
                    tip,
                    velocity,
                    false,
                    10,
                    0.7f * 0.3f,
                    DoGCyanColor,
                    new Vector2(0.12f, 3.5f),
                    true,
                    true));
            }
        }

        public static void SpawnWhipThrustTrail(Player player, Vector2 tip, Vector2 thrustDirection, bool striking)
        {
            if (Main.dedServ)
                return;

            Vector2 handle = player.MountedCenter;
            if (!striking)
            {
                GeneralParticleHandler.SpawnParticle(new LineVFX(
                    handle,
                    tip - handle,
                    0.03f * 0.3f,
                    DoGSpecialColor * 0.4f));

                if ((int)Main.GameUpdateCount % 2 == 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float t = (i + 0.5f) / 3f;
                        GeneralParticleHandler.SpawnParticle(new NanoParticle(
                            Vector2.Lerp(handle, tip, t) + Main.rand.NextVector2Circular(6f, 6f),
                            Main.rand.NextVector2Circular(2f, 2f),
                            DoGSpecialColor,
                            0.4f * 0.3f,
                            12,
                            emitsLight: true));
                    }
                }
                return;
            }

            GeneralParticleHandler.SpawnParticle(new StaticGlowLine(
                handle,
                tip,
                Vector2.Zero,
                3,
                0.08f * 0.3f,
                1f,
                DoGSpecialColor));

            if ((int)Main.GameUpdateCount % 3 == 0)
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    tip + Main.rand.NextVector2Circular(8f, 8f),
                    thrustDirection.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(2f, 6f),
                    DoGSpecialColor,
                    DoGWhiteColor,
                    0.5f * 0.3f,
                    15,
                    1.2f,
                    1.5f,
                    0.004f));
        }

        public static void SpawnSwordSwingTrail(Vector2 bladeTip, Vector2 bladeMidpoint, float swingAngle, bool secondSwing)
        {
            if (Main.dedServ)
                return;

            Color mainColor = secondSwing ? DoGFuchsiaColor : DoGCyanColor;
            Vector2 bladeCenter = Vector2.Lerp(bladeMidpoint, bladeTip, 0.45f);
            float bladeDiskAngle = swingAngle + MathHelper.PiOver2 + (secondSwing ? MathHelper.Pi : 0f);
            GeneralParticleHandler.SpawnParticle(new CircularSmearVFX(
                bladeCenter,
                mainColor * 0.62f,
                bladeDiskAngle,
                4.6f));

            GeneralParticleHandler.SpawnParticle(new SemiCircularSmearVFX(
                bladeCenter,
                DoGSpecialColor * 0.45f,
                bladeDiskAngle,
                3.6f,
                new Vector2(1f, 1.2f)));
        }

        public static void SpawnSwordSwingFlare(Vector2 bladeTip)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new FlareShine(
                    bladeTip,
                    Main.rand.NextVector2Circular(2f, 2f),
                    DoGWhiteColor,
                    DoGCyanColor,
                    MathHelper.PiOver2 * i,
                    new Vector2(0.2f, 2.5f) * 0.3f,
                    new Vector2(0.05f, 0.5f) * 0.3f,
                    12,
                    spawnDelay: i));
            }
        }

        public static void SpawnChainTrail(IReadOnlyList<Vector2> chainPoints, Vector2 knifePosition, Vector2 knifeVelocity, bool secondArc, bool finisher)
        {
            if (Main.dedServ || chainPoints == null || chainPoints.Count <= 1)
                return;

            if (finisher)
            {
                StarsmokeMetaball.SpawnParticle(
                    knifePosition + Main.rand.NextVector2Circular(15f, 15f),
                    knifeVelocity * 0.3f,
                    Main.rand.NextFloat(15f, 35f) * 0.3f,
                    30,
                    new Vector2(0.8f, 1.2f),
                    0.15f,
                    0.5f);

                if ((int)Main.GameUpdateCount % 10 == 0)
                    GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(
                        knifePosition,
                        DoGSpecialColor,
                        Main.rand.NextFloat(MathHelper.TwoPi),
                        0.6f * 0.3f,
                        new Vector2(1f, 1f),
                        starAmount: 8,
                        spinSpeed: 0.06f));
                return;
            }

            if (secondArc)
            {
                if (Main.rand.NextBool(3))
                {
                    Vector2 point = chainPoints[Main.rand.Next(chainPoints.Count)];
                    GeneralParticleHandler.SpawnParticle(new FancyStars(
                        point + Main.rand.NextVector2Circular(8f, 8f),
                        Main.rand.NextFloat(MathHelper.TwoPi),
                        Main.rand.NextFloat(0.2f, 0.45f) * 0.3f,
                        Main.rand.NextVector2Circular(2f, 2f),
                        Main.rand.NextFloat(0.03f, 0.08f),
                        14,
                        DoGFuchsiaColor));
                }

                if ((int)Main.GameUpdateCount % 4 == 0)
                {
                    Vector2 point = chainPoints[Main.rand.Next(chainPoints.Count)];
                    GeneralParticleHandler.SpawnParticle(new GlowSquareParticle(
                        point + Main.rand.NextVector2Circular(5f, 5f),
                        Main.rand.NextVector2Circular(2f, 2f),
                        false,
                        10,
                        Main.rand.NextFloat(0.04f, 0.09f) * 0.3f,
                        DoGFuchsiaColor,
                        rotation: Main.rand.NextFloat(0.05f, 0.12f)));
                }
                return;
            }

            for (int i = 0; i < chainPoints.Count; i += 3)
            {
                GeneralParticleHandler.SpawnParticle(new NanoParticle(
                    chainPoints[i] + Main.rand.NextVector2Circular(4f, 4f),
                    Main.rand.NextVector2Circular(1.5f, 1.5f),
                    i % 6 < 3 ? DoGCyanColor : DoGFuchsiaColor,
                    0.35f * 0.3f,
                    10,
                    emitsLight: true));
            }

            if ((int)Main.GameUpdateCount % 3 == 0)
            {
                Vector2 point = chainPoints[Main.rand.Next(chainPoints.Count)];
                GeneralParticleHandler.SpawnParticle(new ElectricSpark(
                    point + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2Circular(1.5f, 1.5f),
                    DoGCyanColor,
                    DoGFuchsiaColor,
                    0.275f * 0.3f,
                    5,
                    MathHelper.PiOver4 * 0.5f,
                    5f,
                    0.5f));
            }

            if (Main.rand.NextBool(4))
                GeneralParticleHandler.SpawnParticle(new TechyHoloysquareParticle(
                    knifePosition + Main.rand.NextVector2Circular(12f, 12f),
                    Main.rand.NextVector2Circular(4f, 4f),
                    Main.rand.NextFloat(0.4f, 0.8f) * 0.3f,
                    DoGSpecialColor,
                    14,
                    0.8f));
        }

        public static void SpawnWhipOverHit(NPC target)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 10; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    target.Center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextVector2Circular(8f, 8f),
                    true,
                    14,
                    Main.rand.NextFloat(0.6f, 1f) * 0.3f,
                    Main.rand.NextBool() ? DoGCyanColor : DoGFuchsiaColor,
                    new Vector2(0.1f, 0.5f),
                    true));

            GeneralParticleHandler.SpawnParticle(new FlameExplosion(target.Center, Vector2.Zero, DoGCyanColor, new Vector2(1.4f, 0.7f), Main.rand.NextFloat(MathHelper.TwoPi), 0.1f * 0.3f, 0.7f * 0.3f, 14, 0.8f));
            GeneralParticleHandler.SpawnParticle(new FlameExplosion(target.Center, Vector2.Zero, DoGFuchsiaColor * 0.8f, new Vector2(0.7f, 1.3f), Main.rand.NextFloat(MathHelper.TwoPi), 0.08f * 0.3f, 0.55f * 0.3f, 12, 0.7f));
            GeneralParticleHandler.SpawnParticle(new StaticPulseRing(target.Center, Vector2.Zero, DoGSpecialColor, Vector2.One, 0f, 0.04f, 0.45f * 0.3f * ShockwaveFinalScaleMultiplier, 14));
        }

        public static void SpawnWhipUnderHit(NPC target, float whipAngle)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 6; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 8f);
                GeneralParticleHandler.SpawnParticle(new BoltParticle(target.Center, velocity, true, 12, 0.6f * 0.3f, DoGCyanColor, new Vector2(0.1f, 4f), true, true));
            }

            for (int i = 0; i < 3; i++)
                GeneralParticleHandler.SpawnParticle(new ImpactParticle(target.Center, 0.08f + i * 0.06f, 15 - i * 2, (0.6f + i * 0.2f) * 0.3f, DoGCyanColor));

            for (int i = 0; i < 6; i++)
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(target.Center + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextVector2Circular(6f, 6f), true, 12, 0.12f * 0.3f, DoGCyanColor));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(target.Center, Vector2.Zero, DoGCyanColor, new Vector2(1.5f, 0.6f), whipAngle, 0.04f, 0.5f * 0.3f * ShockwaveFinalScaleMultiplier, 15));
        }

        public static void SpawnWhipThrustHit(Player player, NPC target, Vector2 thrustDirection)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 8; i++)
            {
                Vector2 dir = (MathHelper.TwoPi * i / 8f).ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new StaticGlowLine(target.Center, target.Center + dir * Main.rand.NextFloat(80f, 180f), Vector2.Zero, 18, 0.08f * 0.3f, 0.88f, i % 2 == 0 ? DoGCyanColor : DoGFuchsiaColor));
            }

            GeneralParticleHandler.SpawnParticle(new FlatGlow(target.Center, Vector2.Zero, DoGSpecialColor, thrustDirection.ToRotation(), new Vector2(0.1f, 3f) * 0.3f, new Vector2(3f, 0.1f) * 0.3f, 12));
            GeneralParticleHandler.SpawnParticle(new BloomLineVFX(player.Center, (target.Center - player.Center) * 1.3f, 0.06f * 0.3f, DoGSpecialColor, 8, true));

            for (int i = 0; i < 8; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSquareParticle(
                    target.Center + Main.rand.NextVector2Circular(12f, 12f),
                    Main.rand.NextVector2Circular(7f, 7f),
                    true,
                    14,
                    Main.rand.NextFloat(0.05f, 0.12f) * 0.3f,
                    ThreeColorSpark,
                    rotation: Main.rand.NextFloat(0.05f)));
        }

        public static void SpawnSwordOneHit(NPC target)
        {
            if (Main.dedServ)
                return;

            SpawnDoGMediumExplosion(target.Center, DoGCyanColor);
        }

        public static void SpawnSwordTwoHit(NPC target, Vector2 swingDirection)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new SlashThrough(DoGFuchsiaColor, target.Center, swingDirection.ToRotation(), 15, target));
            for (int i = 0; i < 12; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    target.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextVector2Circular(9f, 9f) - Vector2.UnitY * 2f,
                    true,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.7f, 1.1f) * 0.3f,
                    ThreeColorSpark,
                    new Vector2(0.1f, 0.5f),
                    true));

            for (int i = 0; i < 6; i++)
                GeneralParticleHandler.SpawnParticle(new CritSpark(target.Center + Main.rand.NextVector2Circular(15f, 15f), Main.rand.NextVector2Circular(5f, 5f), DoGFuchsiaColor, DoGWhiteColor, 0.5f * 0.3f, 20, 1.5f, 1.2f, 0.006f));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(target.Center, Vector2.Zero, DoGFuchsiaColor, new Vector2(1.8f, 0.5f), swingDirection.ToRotation(), 0.04f, 0.5f * 0.3f * ShockwaveFinalScaleMultiplier, 14));
        }

        public static void SpawnSwordFinisherCharge(Player player, float time)
        {
            if (Main.dedServ)
                return;

            if (time % 5f == 0f)
                GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(player.Center, DoGSpecialColor, Main.rand.NextFloat(MathHelper.TwoPi), 0.7f * 0.3f, Vector2.One, starAmount: 6, spinSpeed: 0.04f));

            if (time % 4f == 0f)
            {
                for (int i = 0; i < 4; i++)
                {
                    float dir = i * MathHelper.PiOver2 + time * 0.05f;
                    GeneralParticleHandler.SpawnParticle(new ChargeUpLineVFX(player.Center + dir.ToRotationVector2() * 120f, dir + MathHelper.Pi, 0.035f * 0.3f, DoGSpecialColor, 20));
                }
            }

            Vector2 particlePos = player.Center + Main.rand.NextVector2Circular(100f, 100f);
            GeneralParticleHandler.SpawnParticle(new NanoParticle(particlePos, (player.Center - particlePos).SafeNormalize(Vector2.Zero) * 3f, DoGSpecialColor, 0.5f * 0.3f, 20, emitsLight: true));
        }

        public static void SpawnSwordFinisherRelease(Player player)
        {
            if (Main.dedServ)
                return;

            SpawnCustomPulse(player.Center, DoGWhiteColor * 0.8f, 0.3f, 2.5f, "CalamityMod/Particles/ShineExplosion1", 20);
            SpawnCustomPulse(player.Center, DoGCyanColor * 0.7f, 0.5f, 3f, "CalamityMod/Particles/PlasmaExplosion", 22);
            SpawnCustomPulse(player.Center, DoGFuchsiaColor * 0.6f, 0.7f, 3.5f, "CalamityMod/Particles/ShineExplosion2", 24);
            GeneralParticleHandler.SpawnParticle(new PlayerCenteredPulseRing(player, Vector2.Zero, DoGCyanColor, Vector2.One, 0f, 0.04f, 0.6f * 0.3f * ShockwaveFinalScaleMultiplier, 16));
            GeneralParticleHandler.SpawnParticle(new PlayerCenteredPulseRing(player, Vector2.Zero, DoGFuchsiaColor, Vector2.One, 0f, 0.06f, 0.9f * 0.3f * ShockwaveFinalScaleMultiplier, 20));
            GeneralParticleHandler.SpawnParticle(new StrongBloom(player.Center, Vector2.Zero, DoGWhiteColor, 1.5f * 0.3f, 18));
        }

        public static void SpawnSwordFinisherHit(IEntitySource source, Player player, NPC target, Vector2 swingDirection)
        {
            if (Main.dedServ)
                return;

            Vector2 center = target.Center;
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, DoGCyanColor, new Vector2(1.3f, 0.75f), 0f, 0.25f * 0.3f, 1.8f * 0.3f, 22));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, DoGWhiteColor, new Vector2(0.9f), MathHelper.PiOver4, 0.35f * 0.3f, 1.5f * 0.3f, 20));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, DoGFuchsiaColor * 0.9f, new Vector2(0.7f, 1.3f), MathHelper.Pi / 3f, 0.3f * 0.3f, 1.2f * 0.3f, 18));

            SpawnDistortionBurst(center, 12, 6, 50f, 30f);
            SpawnRiftCrackProjectiles(source, center, player.whoAmI, 8, 4f, 10f, 18f, 26f);
            GeneralParticleHandler.SpawnParticle(new SlashThrough(DoGSpecialColor, center, swingDirection.ToRotation(), 18, target));

            for (int i = 0; i < 15; i++)
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(center + Main.rand.NextVector2Circular(20f, 20f), Main.rand.NextVector2Circular(7f, 7f), ThreeColorSpark, DoGWhiteColor, Main.rand.NextFloat(0.3f, 0.8f) * 0.3f, Main.rand.Next(20, 35), 2f, 1.5f));

            for (int i = 0; i < 4; i++)
                GeneralParticleHandler.SpawnParticle(new ImpactParticle(center + Main.rand.NextVector2Circular(5f, 5f), 0.1f + i * 0.07f, 18 - i * 2, (0.8f + i * 0.15f) * 0.3f, i % 2 == 0 ? DoGCyanColor : DoGFuchsiaColor));

            SpawnRadiatingGlowLines(center, 12, 100f, 220f, 0.09f * 0.3f, 0.87f, 18);
            SpawnCustomPulse(center, DoGCyanColor * 0.8f, 0.2f, 2f, "CalamityMod/Particles/ShineExplosion1", 18);
            SpawnCustomPulse(center, DoGWhiteColor * 0.9f, 0.3f, 2.5f, "CalamityMod/Particles/PlasmaExplosion", 20);
            SpawnCustomPulse(center, DoGFuchsiaColor * 0.7f, 0.45f, 3f, "CalamityMod/Particles/ShineExplosion2", 22);
            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, DoGWhiteColor, 2f * 0.3f, 20));
        }

        public static void SpawnChainOneHit(NPC target)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 6; i++)
                GalaxyMetaball.SpawnParticle(target.Center + Main.rand.NextVector2Circular(30f, 30f), Main.rand.NextVector2Circular(4f, 4f), Main.rand.NextFloat(20f, 45f) * 0.3f);

            for (int i = 0; i < 8; i++)
                GeneralParticleHandler.SpawnParticle(new BoltParticle(target.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 9f), true, 12, 0.7f * 0.3f, DoGCyanColor, new Vector2(0.12f, 3.8f), true, true));

            for (int i = 0; i < 5; i++)
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(target.Center, Main.rand.NextVector2Circular(7f, 7f), false, 14, 0.15f * 0.3f, DoGCyanColor));
        }

        public static void SpawnChainTwoHit(Player player, NPC target)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 8; i++)
                GeneralParticleHandler.SpawnParticle(new RoundedStarParticle(target.Center + Main.rand.NextVector2Circular(50f, 50f), Main.rand.NextVector2Circular(3f, 3f), i % 2 == 0 ? DoGFuchsiaColor : DoGSpecialColor, Main.rand.NextFloat(0.4f, 0.8f) * 0.3f, 25, 0.05f, 0.95f, false, target.Center, player.whoAmI));

            for (int i = 0; i < 10; i++)
                GeneralParticleHandler.SpawnParticle(new TechyHoloysquareParticle(target.Center + Main.rand.NextVector2Circular(20f, 20f), Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 8f), Main.rand.NextFloat(0.5f, 1f) * 0.3f, ThreeColorSpark, 16, 0.85f));

            GeneralParticleHandler.SpawnParticle(new ImpactParticle(target.Center, 0.15f, 18, 0.9f * 0.3f, DoGFuchsiaColor));
        }

        public static void SpawnChainFinisherBurst(IEntitySource source, Player player, Vector2 center, Vector2 aimDirection, int damage, float knockBack)
        {
            NPC target = FindNearestTarget(center, 1200f);
            if (target != null)
                SpawnFriendlyLaser(source, center, center.DirectionTo(target.Center), damage, knockBack, player.whoAmI, -1.5f, 0.45f, 2f);

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, DoGCyanColor, new Vector2(1.2f, 0.8f), 0f, 0.3f * 0.3f, 2f * 0.3f, 24));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, DoGFuchsiaColor, new Vector2(0.8f, 1.2f), MathHelper.Pi / 3f, 0.4f * 0.3f, 1.6f * 0.3f, 22));

            for (int i = 0; i < 8; i++)
                GalaxyMetaball.SpawnParticle(center + Main.rand.NextVector2Circular(60f, 60f), Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextFloat(40f, 80f) * 0.3f);
            SpawnDistortionBurst(center, 10, 5, 55f, 35f);
            SpawnRiftCrackProjectiles(source, center, player.whoAmI, 6, 4f, 10f, 16f, 24f);

            for (int i = 0; i < 15; i++)
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(center + Main.rand.NextVector2Circular(20f, 20f), Main.rand.NextVector2Circular(8f, 8f), ThreeColorSpark, DoGWhiteColor, Main.rand.NextFloat(0.3f, 0.8f) * 0.3f, Main.rand.Next(20, 35), 2f, 1.5f));
            for (int i = 0; i < 4; i++)
                GeneralParticleHandler.SpawnParticle(new ImpactParticle(center + Main.rand.NextVector2Circular(8f, 8f), 0.1f + i * 0.06f, 18 - i * 2, (0.8f + i * 0.15f) * 0.3f, i % 2 == 0 ? DoGCyanColor : DoGFuchsiaColor));
            SpawnRadiatingGlowLines(center, 10, 100f, 220f, 0.09f * 0.3f, 0.87f, 18);
            SpawnCustomPulse(center, DoGWhiteColor, 0.4f, 3.5f, "CalamityMod/Particles/PlasmaExplosion", 26);
            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, DoGWhiteColor, 2.5f * 0.3f, 22));
        }

        public static void SpawnQuickDrawCharge(Player player, float time)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 8; i++)
            {
                Vector2 spawn = player.Center + Main.rand.NextVector2Circular(60f, 60f);
                GeneralParticleHandler.SpawnParticle(new NanoParticle(spawn, (player.Center - spawn).SafeNormalize(Vector2.Zero) * 5f, DoGSpecialColor, 0.6f * 0.3f, 15, true, true));
            }

            for (int i = 0; i < 6; i++)
            {
                float dir = i * MathHelper.TwoPi / 6f;
                GeneralParticleHandler.SpawnParticle(new ChargeUpLineVFX(player.Center + dir.ToRotationVector2() * 80f, dir + MathHelper.Pi, 0.04f * 0.3f, DoGSpecialColor, 18, fullFadeInPoint: 0.3f));
            }

            for (int i = 0; i < 5; i++)
            {
                Vector2 spawn = player.Center + Main.rand.NextVector2Circular(100f, 100f);
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(spawn, (player.Center - spawn).SafeNormalize(Vector2.Zero) * 12f, 0.18f * 0.3f, DoGSpecialColor, 10, hueShift: 0.003f));
            }

            if (time == 1f)
            {
                GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(player.Center, DoGCyanColor, 0f, 0.9f * 0.3f, Vector2.One, starAmount: 10, spinSpeed: 0.04f));
                GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(player.Center, DoGFuchsiaColor, MathHelper.PiOver4, 0.8f * 0.3f, Vector2.One, starAmount: 8, spinSpeed: -0.035f));
                for (int i = 0; i < 10; i++)
                    GeneralParticleHandler.SpawnParticle(new RoundedStarParticle(player.Center + Main.rand.NextVector2Circular(80f, 80f), Vector2.Zero, DoGSpecialColor, 0.5f * 0.3f, 22, 0.05f, 1f, true, player.Center, player.whoAmI));
            }
        }

        public static void SpawnQuickDrawFullBurst(IEntitySource source, Player player, Vector2 center, int damage, float knockBack)
        {
            NPC target = FindNearestTarget(center, 1200f);
            if (target != null)
                SpawnFriendlyLaser(source, center, center.DirectionTo(target.Center), (int)(damage * 0.7f), knockBack, player.whoAmI, -1.5f, 0.5f, 2f);

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, DoGCyanColor, new Vector2(1.4f, 0.7f), 0f, 0.3f * 0.3f, 2.5f * 0.3f, 25));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, DoGWhiteColor, Vector2.One, MathHelper.PiOver4, 0.5f * 0.3f, 2f * 0.3f, 23));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, DoGFuchsiaColor, new Vector2(0.7f, 1.3f), MathHelper.PiOver2, 0.4f * 0.3f, 1.8f * 0.3f, 21));

            for (int i = 0; i < 4; i++)
                GeneralParticleHandler.SpawnParticle(new FlameExplosion(center + Main.rand.NextVector2Circular(20f, 20f), Vector2.Zero, i % 2 == 0 ? DoGCyanColor : DoGFuchsiaColor, new Vector2(0.8f + Main.rand.NextFloat(0.6f), 0.8f + Main.rand.NextFloat(0.4f)), Main.rand.NextFloat(MathHelper.TwoPi), 0.15f * 0.3f, 1f * 0.3f, 16, 0.75f));

            SpawnDistortionBurst(center, 20, 10, 60f, 40f);
            SpawnRiftCrackProjectiles(source, center, player.whoAmI, 12, 3f, 10f, 18f, 30f);
            SpawnAllDoGParticles(center, player, 1f);

            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, DoGWhiteColor, 3f * 0.3f, 25));
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DoGLaserWallBigAttack") { Volume = 0.62f, MaxInstances = 2 }, center);
        }

        public static void SpawnSwitchPortalAI(Player player, Vector2 center, float time, CosmicDischargeAttackMode targetMode)
        {
            if (Main.dedServ)
                return;

            Color modeColor = GetModeColor(targetMode);
            if (time <= 8f)
            {
                for (int i = 0; i < 2; i++)
                    GeneralParticleHandler.SpawnParticle(new TechyHoloysquareParticle(center + Main.rand.NextVector2Circular(24f, 24f), Main.rand.NextVector2Circular(3f, 3f), Main.rand.NextFloat(0.45f, 0.85f) * 0.3f, Main.rand.NextBool() ? modeColor : ThreeColorSpark, 14, 0.8f));

                for (int i = 0; i < 3; i++)
                {
                    Vector2 spawn = center + Main.rand.NextVector2Circular(46f, 46f);
                    GeneralParticleHandler.SpawnParticle(new NanoParticle(spawn, (center - spawn).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.5f, 4f), DoGSpecialColor, 0.45f * 0.3f, 14, emitsLight: true));
                }
            }

            if (time == 4f)
            {
                GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(center, DoGSpecialColor, 0f, 0.5f * 0.3f, Vector2.One, starAmount: 6, spinSpeed: 0.04f));
                for (int i = 0; i < 5; i++)
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(center + Main.rand.NextVector2Circular(26f, 26f), Main.rand.NextVector2Circular(4f, 4f), false, 12, 0.14f * 0.3f, i % 2 == 0 ? DoGCyanColor : DoGFuchsiaColor));
            }

            if (time == 8f)
            {
                SpawnCustomPulse(center, DoGWhiteColor, 0.2f, 2f, "CalamityMod/Particles/ShineExplosion2", 16);
                SpawnCustomPulse(center, DoGSpecialColor, 0.3f, 2.5f, "CalamityMod/Particles/PlasmaExplosion", 18);
                for (int i = 0; i < 10; i++)
                    GeneralParticleHandler.SpawnParticle(new TechyHoloysquareParticle(center + Main.rand.NextVector2Circular(18f, 18f), Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 8f), Main.rand.NextFloat(0.5f, 1f) * 0.3f, ThreeColorSpark, 16, 0.9f));
                GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, DoGWhiteColor, 1.5f * 0.3f, 16));
            }

            if (time >= 8f)
            {
                for (int i = 0; i < 2; i++)
                    GeneralParticleHandler.SpawnParticle(new ElectricSpark(center + Main.rand.NextVector2Circular(36f, 36f), Main.rand.NextVector2Circular(1.5f, 1.5f), DoGCyanColor, DoGFuchsiaColor, 0.25f * 0.3f, 5, MathHelper.PiOver4 * 0.5f, 5f, 0.5f));
                GeneralParticleHandler.SpawnParticle(new FlatGlow(center, Vector2.Zero, modeColor, Main.rand.NextFloat(MathHelper.TwoPi), new Vector2(0.08f, 1.4f) * 0.3f, new Vector2(1.4f, 0.08f) * 0.3f, 10));
            }
        }

        public static void SpawnUltimateCharge(Player player, float time)
        {
            if (Main.dedServ)
                return;

            if (time % 3f == 0f)
            {
                for (int i = 0; i < 10; i++)
                {
                    float dir = i * MathHelper.TwoPi / 10f + time * 0.02f;
                    float radius = 150f + MathF.Sin(time * 0.1f) * 30f;
                    GeneralParticleHandler.SpawnParticle(new ChargeUpLineVFX(player.Center + dir.ToRotationVector2() * radius, dir + MathHelper.Pi, 0.04f * 0.3f, DoGSpecialColor, 20));
                }
            }

            if (time == 1f)
            {
                for (int i = 0; i < 12; i++)
                    GeneralParticleHandler.SpawnParticle(new RoundedStarParticle(player.Center + Main.rand.NextVector2Circular(100f, 100f), Vector2.Zero, ThreeColorSpark, Main.rand.NextFloat(0.4f, 0.8f) * 0.3f, 65, 0.04f + i * 0.005f, 1f, true, player.Center, player.whoAmI));
            }

            if (time % 10f == 0f)
            {
                GeneralParticleHandler.SpawnParticle(new PlayerCenteredPulseRing(player, Vector2.Zero, DoGCyanColor, Vector2.One, 0f, 0.05f, 0.8f * 0.3f * ShockwaveFinalScaleMultiplier, 30));
                GeneralParticleHandler.SpawnParticle(new PlayerCenteredPulseRing(player, Vector2.Zero, DoGFuchsiaColor, Vector2.One, 0f, 0.07f, 1.1f * 0.3f * ShockwaveFinalScaleMultiplier, 35));
            }

            if (time % 15f == 0f)
            {
                GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(player.Center, DoGCyanColor, 0f, 1.2f * 0.3f, Vector2.One, starAmount: 12, spinSpeed: 0.03f));
                GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(player.Center, DoGFuchsiaColor, MathHelper.PiOver4, 1f * 0.3f, Vector2.One, starAmount: 8, spinSpeed: -0.025f));
            }

            for (int i = 0; i < 6; i++)
            {
                Vector2 spawn = player.Center + Main.rand.NextVector2Circular(120f, 120f);
                GeneralParticleHandler.SpawnParticle(new NanoParticle(spawn, (player.Center - spawn).SafeNormalize(Vector2.Zero) * 4f, DoGSpecialColor, Main.rand.NextFloat(0.3f, 0.7f) * 0.3f, 20, emitsLight: true));
            }
        }

        public static void SpawnUltimateBurst(IEntitySource source, Player player, Vector2 center, int damage, float knockBack)
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy() || Vector2.DistanceSquared(npc.Center, center) > 1800f * 1800f)
                    continue;

                Vector2 direction = center.DirectionTo(npc.Center);
                SpawnFriendlyLaser(source, center, direction, damage, knockBack, player.whoAmI, 0f, 0.5f, 6f);
                SpawnFriendlyLaser(source, center, direction.RotatedBy(0.15f), (int)(damage * 0.6f), knockBack, player.whoAmI, 1.5f, 0.42f, 5f);
                SpawnFriendlyLaser(source, center, direction.RotatedBy(-0.15f), (int)(damage * 0.6f), knockBack, player.whoAmI, -1.5f, 0.42f, 5f);
            }

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, DoGCyanColor, new Vector2(1.5f, 0.7f), 0f, 0.4f * 0.3f, 3f * 0.3f, 28));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, DoGWhiteColor, Vector2.One, MathHelper.Pi / 5f, 0.6f * 0.3f, 2.5f * 0.3f, 26));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, DoGFuchsiaColor, new Vector2(0.7f, 1.4f), MathHelper.Pi / 3f, 0.5f * 0.3f, 2.8f * 0.3f, 24));

            SpawnDistortionBurst(center, 35, 18, 80f, 50f);
            SpawnRiftCrackProjectiles(source, center, player.whoAmI, 25, 4f, 12f, 22f, 32f);
            SpawnAllDoGParticles(center, player, 1.35f);
            for (int i = 0; i < 15; i++)
                GalaxyMetaball.SpawnParticle(center + Main.rand.NextVector2Circular(90f, 90f), Main.rand.NextVector2Circular(6f, 6f), Main.rand.NextFloat(40f, 90f) * 0.3f);
            for (int i = 0; i < 8; i++)
                StarsmokeMetaball.SpawnParticle(center + Main.rand.NextVector2Circular(80f, 80f), Main.rand.NextVector2Circular(5f, 5f), 40f * 0.3f, 40, new Vector2(0.8f, 1.2f), 0.15f, 0.5f);

            for (int i = 0; i < 3; i++)
                GeneralParticleHandler.SpawnParticle(new PlayerCenteredPulseRing(player, Vector2.Zero, ThreeColorSpark, Vector2.One, 0f, 0.05f + i * 0.03f, (1.5f + i * 0.5f) * 0.3f * ShockwaveFinalScaleMultiplier, 30 + i * 5));

            GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(center, DoGSpecialColor, 0f, 1.5f * 0.3f, Vector2.One, starAmount: 16, spinSpeed: 0.05f));
            GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(center, DoGCyanColor, MathHelper.PiOver4, 1.2f * 0.3f, Vector2.One, starAmount: 12, spinSpeed: -0.04f));
            GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(center, DoGFuchsiaColor, MathHelper.PiOver2, 1f * 0.3f, Vector2.One, starAmount: 10, spinSpeed: 0.03f));
            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, DoGWhiteColor, 4f * 0.3f, 32));
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DoGLaserWallBigAttack") { Volume = 0.78f, MaxInstances = 2 }, center);
        }

        public static void SpawnUltimateFieldIdle(Vector2 center, float radius, float time)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 4; i++)
                GeneralParticleHandler.SpawnParticle(new NanoParticle(center + Main.rand.NextVector2Circular(radius, radius), Main.rand.NextVector2Circular(2f, 2f), DoGSpecialColor, 0.4f * 0.3f, 20, emitsLight: true));

            if (time % 10f == 0f)
                GeneralParticleHandler.SpawnParticle(new StaticPulseRing(center, Vector2.Zero, DoGSpecialColor, Vector2.One, 0f, 0.03f, (radius / 50f) * 0.3f * ShockwaveFinalScaleMultiplier, 20));

            if (time % 15f == 0f)
                GeneralParticleHandler.SpawnParticle(new ConstellationRingVFX(center, DoGSpecialColor, 0f, 0.8f * 0.3f, Vector2.One, starAmount: 8, spinSpeed: 0.02f));

            if (time % 5f == 0f)
                GeneralParticleHandler.SpawnParticle(new GenericBloom(center + Main.rand.NextVector2Circular(radius, radius), Vector2.Zero, DoGSpecialColor * 0.4f, 3f * 0.3f, 15));
        }

        public static void SpawnCustomPulse(Vector2 center, Color color, float originalScale, float finalScale, string texture, int lifeTime)
        {
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                color,
                texture,
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                originalScale * 0.3f,
                finalScale * 0.3f * ShockwaveFinalScaleMultiplier,
                lifeTime));
        }

        public static void SpawnDistortionBurst(Vector2 center, int squares, int circles, float squareSpread, float circleSpread)
        {
            for (int i = 0; i < squares; i++)
                DoGDistortionMetaball.SpawnParticle(center + Main.rand.NextVector2Circular(squareSpread, squareSpread), Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextFloat(30f, 70f) * 0.3f, true, Main.rand.NextFloat(MathHelper.TwoPi));

            for (int i = 0; i < circles; i++)
                DoGDistortionMetaball.SpawnParticle(center + Main.rand.NextVector2Circular(circleSpread, circleSpread), Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextFloat(20f, 50f) * 0.3f);
        }

        public static void SpawnRiftCrackProjectiles(IEntitySource source, Vector2 center, int owner, int count, float minSpeed, float maxSpeed, float minWidth, float maxWidth)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / count).ToRotationVector2().RotatedByRandom(0.32f) * Main.rand.NextFloat(minSpeed, maxSpeed);
                Projectile.NewProjectile(source, center, velocity, ModContent.ProjectileType<DoGRiftCrack>(), 0, 0f, owner, 0f, Main.rand.NextFloat(minWidth, maxWidth));
            }
        }

        public static void SpawnFriendlyLaser(IEntitySource source, Vector2 center, Vector2 direction, int damage, float knockBack, int owner, float attackSpeed, float scale, float shake)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            direction = direction.SafeNormalize(Vector2.UnitX);
            int laser = Projectile.NewProjectile(source, center, direction, ModContent.ProjectileType<FriendlyLaserWallBeam>(), damage, knockBack, owner, attackSpeed, 0f, shake);
            if (Main.projectile.IndexInRange(laser))
                Main.projectile[laser].scale *= scale;
        }

        public static NPC FindNearestTarget(Vector2 center, float maxDistance)
        {
            NPC best = null;
            float bestDistance = maxDistance;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.Distance(center, npc.Center);
                if (distance < bestDistance)
                {
                    best = npc;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static void SpawnDoGMediumExplosion(Vector2 center, Color mainColor)
        {
            GeneralParticleHandler.SpawnParticle(new ImpactParticle(center, 0.12f, 18, 0.8f * 0.3f, DoGCyanColor));
            GeneralParticleHandler.SpawnParticle(new ImpactParticle(center, 0.09f, 15, 0.6f * 0.3f, DoGFuchsiaColor));
            for (int i = 0; i < 8; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(center + Main.rand.NextVector2Circular(8f, 8f), Main.rand.NextVector2Circular(8f, 8f), true, 14, 0.8f * 0.3f, mainColor, new Vector2(0.1f, 0.5f), true));
            for (int i = 0; i < 4; i++)
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(center, Main.rand.NextVector2Circular(5f, 5f), true, 12, 0.15f * 0.3f, DoGWhiteColor));
            GeneralParticleHandler.SpawnParticle(new StaticPulseRing(center, Vector2.Zero, DoGCyanColor, Vector2.One, 0f, 0.05f, 0.5f * 0.3f * ShockwaveFinalScaleMultiplier, 12));
            GeneralParticleHandler.SpawnParticle(new StaticPulseRing(center, Vector2.Zero, DoGFuchsiaColor, Vector2.One, 0f, 0.08f, 0.8f * 0.3f * ShockwaveFinalScaleMultiplier, 15));
            GeneralParticleHandler.SpawnParticle(new FlameExplosion(center, Vector2.Zero, DoGCyanColor, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.1f * 0.3f, 0.7f * 0.3f, 12, 0.7f));
            GeneralParticleHandler.SpawnParticle(new FlameExplosion(center, Vector2.Zero, DoGFuchsiaColor, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.08f * 0.3f, 0.6f * 0.3f, 10, 0.6f));
        }

        private static void SpawnRadiatingGlowLines(Vector2 center, int count, float minLength, float maxLength, float xScale, float xShrink, int lifetime)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Main.rand.NextVector2Unit();
                GeneralParticleHandler.SpawnParticle(new StaticGlowLine(center, center + dir * Main.rand.NextFloat(minLength, maxLength), dir * 0.5f, lifetime, xScale, xShrink, i % 2 == 0 ? DoGCyanColor : DoGFuchsiaColor));
            }
        }

        private static void SpawnAllDoGParticles(Vector2 center, Player player, float scale)
        {
            for (int i = 0; i < 25 * scale; i++)
                GeneralParticleHandler.SpawnParticle(new SparkParticle(center + Main.rand.NextVector2Circular(20f, 20f), Main.rand.NextVector2Circular(12f, 12f), false, Main.rand.Next(18, 35), Main.rand.NextFloat(0.5f, 1.2f) * 0.3f, ThreeColorSpark));
            for (int i = 0; i < 20 * scale; i++)
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(center + Main.rand.NextVector2Circular(15f, 15f), Main.rand.NextVector2Circular(10f, 10f), ThreeColorSpark, DoGWhiteColor, Main.rand.NextFloat(0.3f, 0.9f) * 0.3f, Main.rand.Next(18, 35), 2f, 1.5f));
            for (int i = 0; i < 6 * scale; i++)
                GeneralParticleHandler.SpawnParticle(new ImpactParticle(center, 0.07f + i * 0.08f, 20 - i * 2, (0.6f + i * 0.2f) * 0.3f, i % 2 == 0 ? DoGCyanColor : DoGFuchsiaColor));
            for (int i = 0; i < 12 * scale; i++)
                GeneralParticleHandler.SpawnParticle(new FlareShine(center + Main.rand.NextVector2Circular(20f, 20f), Main.rand.NextVector2Circular(2f, 2f), DoGWhiteColor, DoGCyanColor, i * MathHelper.PiOver4, new Vector2(0.15f, 2.5f) * 0.3f, new Vector2(0.02f, 0.4f) * 0.3f, 15, spawnDelay: i / 2));
            for (int i = 0; i < 20 * scale; i++)
                GeneralParticleHandler.SpawnParticle(new FancyStars(center + Main.rand.NextVector2Circular(30f, 30f), Main.rand.NextFloat(MathHelper.TwoPi), Main.rand.NextFloat(0.3f, 0.7f) * 0.3f, Main.rand.NextVector2Circular(8f, 8f), Main.rand.NextFloat(0.04f, 0.1f), 20, ThreeColorSpark));
            SpawnCustomPulse(center, DoGCyanColor * 0.9f, 0.2f, 3f * scale, "CalamityMod/Particles/ShineExplosion1", 24);
            SpawnCustomPulse(center, DoGWhiteColor * 0.85f, 0.4f, 3.5f * scale, "CalamityMod/Particles/PlasmaExplosion", 26);
            SpawnCustomPulse(center, DoGFuchsiaColor * 0.8f, 0.6f, 4f * scale, "CalamityMod/Particles/ShineExplosion2", 28);
            SpawnRadiatingGlowLines(center, (int)(16 * scale), 120f, 300f, 0.08f * 0.3f, 0.87f, 20);
            for (int i = 0; i < 12 * scale; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSquareParticle(center + Main.rand.NextVector2Circular(20f, 20f), Main.rand.NextVector2Circular(8f, 8f), true, 16, Main.rand.NextFloat(0.06f, 0.13f) * 0.3f, ThreeColorSpark, rotation: Main.rand.NextFloat(0.08f, 0.15f)));
            for (int i = 0; i < 25 * scale; i++)
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f, 20f), 0.2f * 0.3f, ThreeColorSpark, 12, hueShift: 0.005f));
            for (int i = 0; i < 10 * scale; i++)
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(center, Main.rand.NextVector2Circular(8f, 8f), false, 14, 0.15f * 0.3f, i % 2 == 0 ? DoGCyanColor : DoGFuchsiaColor));
            for (int i = 0; i < 15 * scale; i++)
                GeneralParticleHandler.SpawnParticle(new TechyHoloysquareParticle(center + Main.rand.NextVector2Circular(24f, 24f), Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 8f), Main.rand.NextFloat(0.5f, 1f) * 0.3f, ThreeColorSpark, 16, 0.85f));
            for (int i = 0; i < 10 * scale; i++)
                GeneralParticleHandler.SpawnParticle(new ElectricSpark(center + Main.rand.NextVector2Circular(36f, 36f), Main.rand.NextVector2Circular(2f, 2f), DoGCyanColor, DoGFuchsiaColor, 0.275f * 0.3f, 6, MathHelper.PiOver4 * 0.5f, 5f, 0.5f));
            for (int i = 0; i < 12 * scale; i++)
                GeneralParticleHandler.SpawnParticle(new BoltParticle(center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 10f), true, 12, 0.65f * 0.3f, DoGCyanColor, new Vector2(0.12f, 4f), true, true));
            for (int i = 0; i < 50 * scale; i++)
                GeneralParticleHandler.SpawnParticle(new NanoParticle(center + Main.rand.NextVector2Circular(90f, 90f), Main.rand.NextVector2Circular(5f, 5f), ThreeColorSpark, Main.rand.NextFloat(0.3f, 0.7f) * 0.3f, 20, emitsLight: true));
            for (int i = 0; i < 6 * scale; i++)
                GeneralParticleHandler.SpawnParticle(new FlatGlow(center, Vector2.Zero, DoGSpecialColor, i * MathHelper.PiOver2, new Vector2(0.1f, 2.5f) * 0.3f, new Vector2(2.5f, 0.1f) * 0.3f, 14));
        }

        public static void SpawnDoGSparkBurst(Vector2 center, int count, float minSpeed, float maxSpeed, float scale = 0.65f, Vector2? bias = null)
        {
            if (Main.dedServ)
                return;

            scale *= 0.3f;
            Vector2 biasDirection = bias.GetValueOrDefault(Vector2.Zero);
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = count <= 1
                    ? Main.rand.NextVector2CircularEdge(1f, 1f)
                    : (MathHelper.TwoPi * i / count).ToRotationVector2().RotatedByRandom(0.45f);
                Vector2 velocity = direction * Main.rand.NextFloat(minSpeed, maxSpeed) + biasDirection;
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    center + Main.rand.NextVector2Circular(18f, 18f),
                    velocity,
                    false,
                    Main.rand.Next(14, 28),
                    Main.rand.NextFloat(scale * 0.72f, scale * 1.25f),
                    RandomDoGColor()));
            }
        }

        public static void SpawnDoGRiftCracks(Vector2 center, int count, float minLength, float maxLength, float scale = 0.55f)
        {
            if (Main.dedServ)
                return;

            scale *= 0.3f;
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.18f, 0.18f)).ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    center + direction * Main.rand.NextFloat(2f, 18f),
                    direction * Main.rand.NextFloat(minLength, maxLength),
                    false,
                    Main.rand.Next(8, 15),
                    Main.rand.NextFloat(scale * 0.7f, scale * 1.15f),
                    Transparent(RandomDoGColor()) * 0.75f));
            }
        }

        public static void SpawnDoGImpact(Vector2 center, Vector2 direction, bool heavy, bool tip = false)
        {
            if (Main.dedServ)
                return;

            direction = direction.SafeNormalize(Vector2.UnitX);
            Color main = heavy ? DoGFuchsiaColor : DoGSpecialColor;
            Color secondary = tip ? DoGWhiteColor : DoGCyanColor;
            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                center,
                Vector2.Zero,
                Transparent(main) * (heavy ? 0.55f : 0.35f),
                (heavy ? 0.68f : 0.44f) * 0.3f,
                heavy ? 24 : 16));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                direction * 0.7f,
                Transparent(secondary) * (heavy ? 0.45f : 0.3f),
                Vector2.One,
                direction.ToRotation(),
                0.04f,
                (heavy ? 0.32f : 0.19f) * 0.3f * ShockwaveFinalScaleMultiplier,
                heavy ? 18 : 12));
            SpawnDoGSparkBurst(center, heavy ? 22 : 12, 3f, heavy ? 13f : 8f, heavy ? 0.72f : 0.5f, direction * 1.2f);
            if (heavy || tip)
                SpawnDoGRiftCracks(center, tip ? 5 : 3, 4f, tip ? 10f : 7f, tip ? 0.72f : 0.48f);
        }

        public static bool HasOwnedProjectile(Player player, params int[] projectileTypes)
        {
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != player.whoAmI)
                    continue;

                for (int i = 0; i < projectileTypes.Length; i++)
                {
                    if (projectile.type == projectileTypes[i])
                        return true;
                }
            }

            return false;
        }

        public static void DrawChain(SpriteBatch spriteBatch, Vector2 startWorld, Vector2 endWorld, Color drawColor, float scale, bool rigid, float gfxOffY = 0f)
        {
            Texture2D texture = ModContent.Request<Texture2D>(ChainTexturePath).Value;
            Rectangle handleFrame = new(0, 0, texture.Width, ChainHandleHeight);
            Rectangle bodyFrame = new(0, ChainBodyStartY, texture.Width, ChainBodyHeight);
            Rectangle tailFrame = new(0, ChainTailStartY, texture.Width, ChainTailHeight);

            Vector2 chain = endWorld - startWorld;
            float chainLength = chain.Length();
            if (chainLength < 2f)
                return;

            Vector2 direction = chain / chainLength;
            float rotation = direction.ToRotation() + MathHelper.PiOver2;
            Vector2 drawOffset = Vector2.UnitY * gfxOffY;

            Main.EntitySpriteDraw(
                texture,
                startWorld - Main.screenPosition + drawOffset,
                handleFrame,
                drawColor,
                rotation,
                handleFrame.Size() * 0.5f,
                scale,
                SpriteEffects.FlipVertically);

            float startOffset = Math.Min(ChainBodyStartOffset * scale, chainLength);
            float tailLength = ChainTailHeight * scale;
            float bodyEndDistance = MathHelper.Clamp(chainLength - tailLength, startOffset, chainLength);
            float remaining = System.Math.Max(0f, bodyEndDistance - startOffset);
            Vector2 drawPosition = startWorld + direction * startOffset;

            while (remaining > 2f)
            {
                Rectangle drawFrame = bodyFrame;
                float segmentHeight = drawFrame.Height * scale;
                if (remaining < segmentHeight)
                {
                    int croppedHeight = (int)MathHelper.Clamp(remaining / scale, 2f, bodyFrame.Height);
                    drawFrame.Height = croppedHeight;
                    segmentHeight = croppedHeight * scale;
                }

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition - Main.screenPosition + drawOffset,
                    drawFrame,
                    drawColor,
                    rotation,
                    new Vector2(drawFrame.Width * 0.5f, 0f),
                    scale,
                    SpriteEffects.None);

                drawPosition += direction * segmentHeight;
                remaining -= segmentHeight;
            }

            Main.EntitySpriteDraw(
                texture,
                startWorld + direction * bodyEndDistance - Main.screenPosition + drawOffset,
                tailFrame,
                drawColor,
                rotation,
                new Vector2(tailFrame.Width * 0.5f, 0f),
                scale,
                SpriteEffects.FlipVertically);
        }

        public static void DrawCurvedChain(SpriteBatch spriteBatch, IReadOnlyList<Vector2> points, Color drawColor, float scale, float gfxOffY = 0f)
        {
            if (points == null || points.Count < 2)
                return;

            Texture2D texture = ModContent.Request<Texture2D>(ChainTexturePath).Value;
            Rectangle handleFrame = new(0, 0, texture.Width, ChainHandleHeight);
            Rectangle bodyFrame = new(0, ChainBodyStartY, texture.Width, ChainBodyHeight);
            Rectangle tailFrame = new(0, ChainTailStartY, texture.Width, ChainTailHeight);
            Vector2 drawOffset = Vector2.UnitY * gfxOffY;

            Vector2 firstDirection = (points[1] - points[0]).SafeNormalize(Vector2.UnitY);
            Main.EntitySpriteDraw(
                texture,
                points[0] - Main.screenPosition + drawOffset,
                handleFrame,
                drawColor,
                firstDirection.ToRotation() + MathHelper.PiOver2,
                handleFrame.Size() * 0.5f,
                scale,
                SpriteEffects.FlipVertically);

            float pathLength = 0f;
            for (int i = 0; i < points.Count - 1; i++)
                pathLength += Vector2.Distance(points[i], points[i + 1]);

            float bodyStartDistance = Math.Min(ChainBodyStartOffset * scale, pathLength);
            float bodyEndDistance = MathHelper.Clamp(pathLength - ChainTailHeight * scale, bodyStartDistance, pathLength);
            Vector2 tailPosition = points[^1];
            Vector2 lastDirection = (points[^1] - points[^2]).SafeNormalize(Vector2.UnitY);

            float traveled = 0f;
            bool foundTailPosition = false;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 segment = points[i + 1] - points[i];
                float segmentLength = segment.Length();
                if (segmentLength < 2f)
                    continue;

                Vector2 segmentDirection = segment / segmentLength;
                float segmentStartDistance = traveled;
                float segmentEndDistance = traveled + segmentLength;

                if (segmentEndDistance > bodyStartDistance && segmentStartDistance < bodyEndDistance)
                {
                    float localStart = Math.Max(bodyStartDistance, segmentStartDistance) - segmentStartDistance;
                    float localEnd = Math.Min(bodyEndDistance, segmentEndDistance) - segmentStartDistance;
                    if (localEnd - localStart > 2f)
                        DrawBodySegment(texture, bodyFrame, points[i] + segmentDirection * localStart, points[i] + segmentDirection * localEnd, drawColor, scale, drawOffset);
                }

                if (!foundTailPosition && bodyEndDistance <= segmentEndDistance)
                {
                    tailPosition = points[i] + segmentDirection * (bodyEndDistance - segmentStartDistance);
                    lastDirection = segmentDirection;
                    foundTailPosition = true;
                }

                traveled = segmentEndDistance;
            }

            Main.EntitySpriteDraw(
                texture,
                tailPosition - Main.screenPosition + drawOffset,
                tailFrame,
                Color.Lerp(drawColor, DoGWhiteColor, 0.22f),
                lastDirection.ToRotation() + MathHelper.PiOver2,
                new Vector2(tailFrame.Width * 0.5f, 0f),
                scale,
                SpriteEffects.FlipVertically);
        }

        public static void DrawRightHoldIndicator(SpriteBatch spriteBatch, Player player, float intensity)
        {
            Texture2D ring = ModContent.Request<Texture2D>(RingTexturePath).Value;
            Vector2 drawPosition = player.Bottom - Main.screenPosition + new Vector2(0f, -6f + player.gfxOffY);
            Color ringColor = Color.Lerp(DoGPurpleColor, DoGSpecialColor, 0.45f) * (0.35f * intensity);

            spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                ring,
                drawPosition,
                null,
                ringColor,
                0f,
                ring.Size() * 0.5f,
                new Vector2(0.85f, 0.28f) * (1f + 0.2f * intensity),
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                ring,
                drawPosition,
                null,
                DoGSpecialColor * (0.18f * intensity),
                Main.GlobalTimeWrappedHourly * 0.8f,
                ring.Size() * 0.5f,
                new Vector2(0.45f, 0.14f) * (1f + 0.15f * intensity),
                SpriteEffects.None);

            spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private static void DrawBodySegment(Texture2D texture, Rectangle frame, Vector2 start, Vector2 end, Color drawColor, float scale, Vector2 drawOffset)
        {
            Vector2 segment = end - start;
            float length = segment.Length();
            if (length < 2f)
                return;

            Vector2 direction = segment / length;
            float rotation = direction.ToRotation() + MathHelper.PiOver2;
            float step = frame.Height * scale;
            Vector2 position = start;

            for (float traveled = 0f; traveled < length; traveled += step)
            {
                float remaining = length - traveled;
                Rectangle drawFrame = frame;
                if (remaining < step)
                    drawFrame.Height = (int)MathHelper.Clamp(remaining / scale, 2f, frame.Height);

                Main.EntitySpriteDraw(
                    texture,
                    position - Main.screenPosition + drawOffset,
                    drawFrame,
                    drawColor,
                    rotation,
                    new Vector2(drawFrame.Width * 0.5f, 0f),
                    scale,
                    SpriteEffects.None);

                position += direction * step;
            }
        }
    }
}
