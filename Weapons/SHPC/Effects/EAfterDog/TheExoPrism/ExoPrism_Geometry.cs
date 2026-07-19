using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.TheExoPrism
{
    internal class ExoPrism_Geometry : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "Terraria/Images/Projectile_1";

        private const int HomingFrames = 54;
        private const int DriftFrames = 42;
        private const float HomingRange = 1400f;
        private const float BaseHomingSpeed = 8.5f;
        private const float MaxHomingSpeed = 13.5f;

        private static readonly Color PrismCyan = new(96, 235, 255);
        private static readonly Color MiracleGold = new(255, 205, 95);

        private int geometryType;
        private float sizeMultiplier;
        private float localTimeOffset;
        private float rotationSeed;

        private ref float Timer => ref Projectile.localAI[0];

        private class GeometryData
        {
            public Vector3[] Points;
            public int[,] Edges;
        }

        private static List<GeometryData> geometries;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;

            EnsureGeometries();
        }

        private static int[,] BuildEdgesFromFaces(int[][] faces)
        {
            HashSet<(int, int)> edgeSet = new();

            for (int i = 0; i < faces.Length; i++)
            {
                int[] face = faces[i];
                for (int j = 0; j < face.Length; j++)
                {
                    int a = face[j];
                    int b = face[(j + 1) % face.Length];
                    if (a > b)
                        (a, b) = (b, a);

                    edgeSet.Add((a, b));
                }
            }

            int[,] edges = new int[edgeSet.Count, 2];
            int index = 0;
            foreach ((int a, int b) in edgeSet)
            {
                edges[index, 0] = a;
                edges[index, 1] = b;
                index++;
            }

            return edges;
        }

        private static GeometryData GenerateRandomGeometry()
        {
            int pointCount = Main.rand.Next(6, 10);
            Vector3[] points = new Vector3[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                Vector3 point = new(
                    Main.rand.NextFloat(-1f, 1f),
                    Main.rand.NextFloat(-1f, 1f),
                    Main.rand.NextFloat(-1f, 1f));

                if (point.Length() < 0.001f)
                    point = Vector3.UnitX;

                point.Normalize();
                points[i] = point;
            }

            HashSet<(int, int)> edges = new();
            for (int i = 0; i < pointCount; i++)
            {
                List<(float distance, int index)> nearbyPoints = new();
                for (int j = 0; j < pointCount; j++)
                {
                    if (i != j)
                        nearbyPoints.Add((Vector3.Distance(points[i], points[j]), j));
                }

                nearbyPoints.Sort((a, b) => a.distance.CompareTo(b.distance));
                int connectCount = Main.rand.Next(2, 4);
                for (int k = 0; k < connectCount; k++)
                {
                    int a = Math.Min(i, nearbyPoints[k].index);
                    int b = Math.Max(i, nearbyPoints[k].index);
                    edges.Add((a, b));
                }
            }

            int[,] edgeArray = new int[edges.Count, 2];
            int edgeIndex = 0;
            foreach ((int a, int b) in edges)
            {
                edgeArray[edgeIndex, 0] = a;
                edgeArray[edgeIndex, 1] = b;
                edgeIndex++;
            }

            return new GeometryData { Points = points, Edges = edgeArray };
        }

        private static void EnsureGeometries()
        {
            if (geometries != null)
                return;

            geometries = new List<GeometryData>
            {
                new GeometryData
                {
                    Points = new[]
                    {
                        new Vector3(-1, -1, -1), new Vector3(1, -1, -1), new Vector3(1, 1, -1), new Vector3(-1, 1, -1),
                        new Vector3(-1, -1, 1), new Vector3(1, -1, 1), new Vector3(1, 1, 1), new Vector3(-1, 1, 1)
                    },
                    Edges = new[,]
                    {
                        { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 }, { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
                        { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
                    }
                },
                new GeometryData
                {
                    Points = new[]
                    {
                        new Vector3(0, 0, -1), new Vector3(0, 0, 1), new Vector3(-1, 0, 0),
                        new Vector3(1, 0, 0), new Vector3(0, -1, 0), new Vector3(0, 1, 0)
                    },
                    Edges = new[,]
                    {
                        { 0, 2 }, { 0, 3 }, { 0, 4 }, { 0, 5 }, { 1, 2 }, { 1, 3 }, { 1, 4 }, { 1, 5 }
                    }
                },
                new GeometryData
                {
                    Points = new[]
                    {
                        new Vector3(1, 1, 1), new Vector3(-1, -1, 1), new Vector3(-1, 1, -1), new Vector3(1, -1, -1)
                    },
                    Edges = new[,]
                    {
                        { 0, 1 }, { 0, 2 }, { 0, 3 }, { 1, 2 }, { 1, 3 }, { 2, 3 }
                    }
                }
            };
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            EnsureGeometries();
            geometryType = Main.rand.Next(3);
            if (geometryType == 2)
                _ = GenerateRandomGeometry(); // Preserve the original RNG sequence; draw the stable tetrahedron instead.

            sizeMultiplier = Main.rand.NextFloat(0.5f, 2f);
            localTimeOffset = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            rotationSeed = Projectile.identity * 0.61803398875f;
        }

        public override void AI()
        {
            Timer++;
            RunPulsingHoming();
        }

        private void RunPulsingHoming()
        {
            int cycleLength = HomingFrames + DriftFrames;
            int cycleFrame = (int)Timer % cycleLength;
            bool homingActive = cycleFrame < HomingFrames;

            if (homingActive)
            {
                NPC target = FindTarget();
                if (target != null)
                    HomeTowardTarget(target, cycleFrame / (float)Math.Max(1, HomingFrames - 1));
                else
                    Drift();
            }
            else
                Drift();

            if (Projectile.velocity.Length() > 0.1f)
                Projectile.rotation = Projectile.velocity.ToRotation();
        }

        private NPC FindTarget()
        {
            int preferredTarget = (int)Projectile.ai[0];
            if (Main.npc.IndexInRange(preferredTarget))
            {
                NPC preferred = Main.npc[preferredTarget];
                if (preferred.CanBeChasedBy(Projectile, false) && Projectile.Distance(preferred.Center) <= HomingRange)
                    return preferred;
            }

            NPC target = null;
            float bestDistance = HomingRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                target = npc;
                bestDistance = distance;
            }

            return target;
        }

        private void HomeTowardTarget(NPC target, float windowProgress)
        {
            float distance = Projectile.Distance(target.Center);
            float currentSpeed = Projectile.velocity.Length();
            Vector2 fallbackDirection = currentSpeed > 0.1f ? Projectile.velocity.SafeNormalize(Vector2.UnitX) : Vector2.UnitY;
            float speed = MathHelper.Lerp(BaseHomingSpeed, MaxHomingSpeed, MathHelper.Clamp(windowProgress, 0f, 1f));
            float predictionFrames = MathHelper.Clamp(distance / Math.Max(speed, 1f), 8f, 22f);
            Vector2 aimPoint = target.Center + target.velocity * predictionFrames;
            Vector2 desiredDirection = (aimPoint - Projectile.Center).SafeNormalize(fallbackDirection);
            float closePressure = Utils.GetLerpValue(520f, 120f, distance, true);
            float inertia = MathHelper.Lerp(18f, 5.5f, MathHelper.Max(windowProgress, closePressure));

            Projectile.velocity = (Projectile.velocity * inertia + desiredDirection * speed) / (inertia + 1f);

            float cappedSpeed = MathHelper.Clamp(Projectile.velocity.Length(), BaseHomingSpeed * 0.45f, MaxHomingSpeed);
            Projectile.velocity = Projectile.velocity.SafeNormalize(desiredDirection) * cappedSpeed;
        }

        private void Drift()
        {
            float sway = (float)Math.Sin((Timer + Projectile.identity * 11f) * 0.055f) * 0.01f;
            Projectile.velocity = Projectile.velocity.RotatedBy(sway) * 0.986f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.16f, Pitch = 0.28f, PitchVariance = 0.1f, MaxInstances = 6 }, target.Center);
            target.AddBuff(ModContent.BuffType<MiracleBlight>(), 300);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            EnsureGeometries();
            if (geometries == null || geometries.Count == 0)
                return false;

            GeometryData geometry = geometries[Math.Clamp(geometryType, 0, geometries.Count - 1)];
            float t = Timer * 0.065f + localTimeOffset + rotationSeed;
            float pulse = 0.78f + 0.22f * (float)Math.Sin(Timer * 0.14f + rotationSeed);
            float size = 32f * sizeMultiplier;

            // Counter-rotating nested copies turn every polyhedron into a readable two-layer matrix.
            Matrix outerRotation = Matrix.CreateFromYawPitchRoll(t * 1.15f, t * 0.92f, t * 0.76f);
            Matrix innerRotation = Matrix.CreateFromYawPitchRoll(-t * 2.45f + 0.7f, t * 1.83f, -t * 1.52f - 0.45f);

            DrawGeometryLayer(geometry, outerRotation, size, Color.Lerp(PrismCyan, Color.White, 0.35f) * pulse, 2.35f);
            DrawGeometryLayer(geometry, innerRotation, size * 0.64f, Color.Lerp(MiracleGold, Color.White, 0.48f) * (0.88f * pulse), 1.55f);
            DrawRefractionWake(pulse);
            return false;
        }

        private Vector2 ProjectPoint(Vector3 point, Matrix rotation, float size)
        {
            Vector3 rotated = Vector3.Transform(point * size, rotation);
            const float focalLength = 900f;
            const float depthBias = 960f;
            float perspective = focalLength / (focalLength + rotated.Z + depthBias);
            return Projectile.Center + new Vector2(rotated.X, rotated.Y) * perspective;
        }

        private void DrawGeometryLayer(GeometryData geometry, Matrix rotation, float size, Color color, float width)
        {
            Vector2[] points = new Vector2[geometry.Points.Length];
            for (int i = 0; i < geometry.Points.Length; i++)
                points[i] = ProjectPoint(geometry.Points[i], rotation, size);

            int edgeCount = geometry.Edges.GetLength(0);
            for (int i = 0; i < edgeCount; i++)
            {
                Vector2 start = points[geometry.Edges[i, 0]];
                Vector2 end = points[geometry.Edges[i, 1]];
                Main.spriteBatch.DrawLineBetter(start, end, color * 0.28f, width + 2.1f);
                Main.spriteBatch.DrawLineBetter(start, end, color, width);
            }
        }

        // This wake is an independent refractive field, not a series of old geometry positions.
        private void DrawRefractionWake(float pulse)
        {
            if (Projectile.velocity.LengthSquared() < 0.01f)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            float phase = Timer * 0.18f + rotationSeed;

            for (int side = -1; side <= 1; side += 2)
            {
                float lateralOffset = side * (10f + 3f * (float)Math.Sin(phase));
                Vector2 railStart = Projectile.Center - forward * 10f + normal * lateralOffset;
                Vector2 railEnd = Projectile.Center - forward * (74f + 12f * (float)Math.Sin(phase + side)) + normal * lateralOffset * 1.55f;
                Color railColor = Color.Lerp(PrismCyan, MiracleGold, side > 0 ? 0.32f : 0.08f) * (0.42f * pulse);

                Main.spriteBatch.DrawLineBetter(railStart, railEnd, railColor * 0.3f, 3.4f);
                Main.spriteBatch.DrawLineBetter(railStart, railEnd, railColor, 1.05f);

                for (int i = 1; i <= 2; i++)
                {
                    float completion = i / 3f;
                    Vector2 sliceCenter = Vector2.Lerp(railStart, railEnd, completion);
                    float sliceWidth = MathHelper.Lerp(5f, 13f, completion);
                    Main.spriteBatch.DrawLineBetter(sliceCenter - normal * sliceWidth, sliceCenter + normal * sliceWidth, railColor * 0.58f, 0.9f);
                }
            }
        }
    }
}
