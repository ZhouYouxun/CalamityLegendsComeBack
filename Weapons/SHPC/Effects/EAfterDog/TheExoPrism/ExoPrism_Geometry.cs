using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.TheExoPrism
{
    internal class ExoPrism_Geometry : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_1";

        private int geometryType;

        private float sizeMultiplier;
        private float localTimeOffset;

        // 这里写死当前上限，后面你要加新几何体，直接改这个数字最快
        private const int MaxGeometryType = 2;

        // ================= 几何结构定义 =================
        private class GeometryData
        {
            public Vector3[] Points;
            public int[,] Edges;
        }

        private static List<GeometryData> geometries;

        public override void SetStaticDefaults()
        {
            // 几何体绘制范围较大，给更宽松的屏幕检查范围
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 160;
            Projectile.height = 160;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;

            EnsureGeometries();
        }

        // ================= 初始化几何体 =================
        private static void EnsureGeometries()
        {
            if (geometries != null)
                return;

            geometries = new List<GeometryData>();

            // ===== 0：立方体 =====
            geometries.Add(new GeometryData
            {
                Points = new Vector3[]
                {
                    new(-1,-1,-1), new( 1,-1,-1), new( 1, 1,-1), new(-1, 1,-1),
                    new(-1,-1, 1), new( 1,-1, 1), new( 1, 1, 1), new(-1, 1, 1)
                },
                Edges = new int[,]
                {
                    {0,1},{1,2},{2,3},{3,0},
                    {4,5},{5,6},{6,7},{7,4},
                    {0,4},{1,5},{2,6},{3,7}
                }
            });

            // ===== 1：八面体 =====
            geometries.Add(new GeometryData
            {
                Points = new Vector3[]
                {
                    new( 0, 0,-1), new( 0, 0, 1),
                    new(-1, 0, 0), new( 1, 0, 0),
                    new( 0,-1, 0), new( 0, 1, 0)
                },
                Edges = new int[,]
                {
                    {0,2},{0,3},{0,4},{0,5},
                    {1,2},{1,3},{1,4},{1,5}
                }
            });

            // ===== 2：四面体 =====
            geometries.Add(new GeometryData
            {
                Points = new Vector3[]
                {
                    new( 1, 1, 1),
                    new(-1,-1, 1),
                    new(-1, 1,-1),
                    new( 1,-1,-1)
                },
                Edges = new int[,]
                {
                    {0,1},{0,2},{0,3},
                    {1,2},{1,3},{2,3}
                }
            });
        }

        // ================= OnSpawn 随机选择 =================
        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            EnsureGeometries();

            int safeMax = Math.Min(MaxGeometryType, geometries.Count - 1);
            geometryType = Main.rand.Next(safeMax + 1);

            // 尺寸：0.5x ~ 2.0x
            sizeMultiplier = Main.rand.NextFloat(0.5f, 2f);

            // 时间偏移：让每个实例不同步
            localTimeOffset = Main.rand.NextFloat(0f, MathHelper.TwoPi);
        }

        // ================= AI =================
        public override void AI()
        {
            NPC target = null;
            float maxDist = 1200f;

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.CanBeChasedBy())
                {
                    float d = Vector2.Distance(npc.Center, Projectile.Center);
                    if (d < maxDist)
                    {
                        maxDist = d;
                        target = npc;
                    }
                }
            }

            if (target == null)
                return;

            // ================= 围绕目标旋转 =================

            // 时间推进
            float t = Projectile.timeLeft * 0.03f;

            // 半径（比Phantasmal更大）
            float baseRadius = 80f + (float)Math.Sin(Projectile.identity * 1.37f) * 20f;

            // 随机扰动半径（动态变化）
            float radius = baseRadius + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f + Projectile.identity) * 16f;

            // 旋转角度（慢速）
            float angle = t + Projectile.identity * 0.6f;

            Vector2 circlePos = target.Center + angle.ToRotationVector2() * radius;

            // ================= 向圆轨道移动 =================
            Vector2 moveDir = (circlePos - Projectile.Center).SafeNormalize(Vector2.UnitX);

            float accel = Main.rand.NextFloat(0.12f, 0.28f); // 比原版更慢
            float maxSpeed = 6f; // 限速更低

            if (Projectile.velocity.Length() < maxSpeed)
                Projectile.velocity += moveDir * accel;
            else
                Projectile.velocity *= 0.88f;

            // ================= 朝向 =================
            if (Projectile.velocity.Length() > 0.1f)
                Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override void OnKill(int timeLeft)
        {
        }

        // ================= 绘制 =================
        public override bool PreDraw(ref Color lightColor)
        {
            EnsureGeometries();

            if (geometries == null || geometries.Count == 0)
                return false;

            if (geometryType < 0 || geometryType >= geometries.Count)
                geometryType = 0;

            Vector2 center = Projectile.Center;
            float t = Main.GlobalTimeWrappedHourly + localTimeOffset;

            Color GetExoColor()
            {
                List<Color> eColors = new List<Color>()
            {
                Color.Lerp(Color.OrangeRed, Color.White, 0.28f),
                Color.Lerp(Color.MediumTurquoise, Color.White, 0.28f),
                Color.Lerp(Color.Orange, Color.White, 0.28f),
                Color.Lerp(Color.LawnGreen, Color.White, 0.28f)
            };

                float rate = Main.GlobalTimeWrappedHourly * 8f;
                int colorIndex = (int)(rate / 2f % eColors.Count);
                Color currentColor = eColors[colorIndex];
                Color nextColor = eColors[(colorIndex + 1) % eColors.Count];
                float lerpValue = rate % 2f > 1f ? 1f : rate % 1f;

                return Color.Lerp(currentColor, nextColor, lerpValue);
            }

            // 三轴旋转
            float yaw = t * (0.6f + Projectile.identity * 0.0007f);
            float pitch = t * (0.45f + Projectile.identity * 0.0009f);
            float roll = t * (0.3f + Projectile.identity * 0.0011f);
            Matrix rot = Matrix.CreateFromYawPitchRoll(yaw, pitch, roll);

            float size = 2f * 16f * sizeMultiplier;
            float focal = 1000f;
            float zBias = 1000f;

            Vector2 Project(Vector3 p)
            {
                Vector3 r = Vector3.Transform(p, rot);
                float persp = focal / (focal + r.Z + zBias);
                return center + new Vector2(r.X, r.Y) * persp;
            }

            GeometryData geo = geometries[geometryType];

            Vector2[] projected = new Vector2[geo.Points.Length];
            for (int i = 0; i < geo.Points.Length; i++)
                projected[i] = Project(geo.Points[i] * size);

            int edgeCount = geo.Edges.GetLength(0);

            Color lineColor = GetExoColor();

            for (int i = 0; i < edgeCount; i++)
            {
                Main.spriteBatch.DrawLineBetter(
                    projected[geo.Edges[i, 0]],
                    projected[geo.Edges[i, 1]],
                    lineColor,
                    3f
                );
            }

            return false;
        }










    }
}