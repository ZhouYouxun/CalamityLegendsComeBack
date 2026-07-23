using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects
{
    // 狮子座星光特效的统一入口。
    //
    // 星光走的是 Calamity 的粒子系统而不是弹幕：
    //   · 纯客户端，不占弹幕槽位、不走网络同步（这把武器本身已经会同时刷很多弹幕）；
    //   · 从 AI/OnKill 里调用即可，这些回调在每个客户端都会跑，所有人都看得到。
    // 星光本身不造成任何伤害，只做点缀，不参与数值平衡。
    //
    // 绘制约定（本项目反复踩过的坑）：
    //   · 粒子由处理器用 BlendState.Additive 开批次 → 保留 alpha，整体乘不透明度；
    //   · 本文件给弹幕 PreDraw 用的 Draw* 助手跑在默认 AlphaBlend 批次里
    //     → 贴图是预乘 alpha 的，把 A 设成 0 就等价于加法混合，黑底自然消失。
    public static class LeonidStarlight
    {
        private static Vector2 gravityWellCenter;
        private static float gravityWellStrength;
        private static uint gravityWellFrame;
        private static uint trackerReservationFrame;
        private static readonly List<Vector2> TrackerReservationCenters = new();

        private const float TrackerGroupRadius = 96f;
        private const float SpawnKeepChance = 0.8f;

        // 换世界时把星光的静态状态归零。
        internal static void ResetStaticState()
        {
            LeonidStarlightMote.ClearRegistry();
            gravityWellFrame = 0u;
            gravityWellStrength = 0f;
            trackerReservationFrame = 0u;
            TrackerReservationCenters.Clear();
        }

        private static Color DefaultColor(float phaseOffset) => LeonidVisualUtils.GetMeteorColor(phaseOffset);

        private static bool CanSpawn(int wanted) =>
            !Main.dedServ && GeneralParticleHandler.FreeSpacesAvailable() > wanted;

        // Preserve an exact 80% average even for tiny groups. The occasional one-star
        // difference also keeps repeated releases from looking mechanically identical.
        private static int ReducedCount(int originalCount)
        {
            if (originalCount <= 0)
                return 0;

            float desiredCount = originalCount * SpawnKeepChance;
            int reducedCount = (int)MathF.Floor(desiredCount);
            if (Main.rand.NextFloat() < desiredCount - reducedCount)
                reducedCount++;
            return reducedCount;
        }

        // Calls made on the same frame and near the same origin belong to one visual
        // release. Only the first batch in that release may assign a homing mote.
        private static bool TryReserveTracker(Vector2 center)
        {
            if (trackerReservationFrame != Main.GameUpdateCount)
            {
                trackerReservationFrame = Main.GameUpdateCount;
                TrackerReservationCenters.Clear();
            }

            float groupRadiusSquared = TrackerGroupRadius * TrackerGroupRadius;
            foreach (Vector2 reservedCenter in TrackerReservationCenters)
            {
                if (Vector2.DistanceSquared(center, reservedCenter) <= groupRadiusSquared)
                    return false;
            }

            TrackerReservationCenters.Add(center);
            return true;
        }

        private static void SpawnCore(
            Vector2 position,
            Vector2 velocity,
            Color color,
            LeonidStarlightShape shape,
            float scale,
            int hoverTime,
            int lifetime,
            float lanceSpeed,
            float homingRange,
            bool linksToSiblings,
            LeonidStarlightMotion motion,
            float motionPhase)
        {
            if (!CanSpawn(1))
                return;

            GeneralParticleHandler.SpawnParticle(new LeonidStarlightMote(
                position, velocity, color, shape, scale, hoverTime, lifetime, lanceSpeed, homingRange,
                linksToSiblings, motion, motionPhase));
        }

        // ── 生成 ───────────────────────────────────────────────────

        // 单颗星光：独立调用只做自由漂流；批量接口会在内部单独分配唯一的追踪星。
        public static void Spawn(
            Vector2 position,
            Vector2 velocity,
            Color color,
            LeonidStarlightShape shape = LeonidStarlightShape.Mote,
            float scale = 1f,
            int hoverTime = 26,
            int lifetime = 150,
            float lanceSpeed = 17f,
            float homingRange = 760f,
            bool linksToSiblings = true)
        {
            if (Main.dedServ || Main.rand.NextFloat() >= SpawnKeepChance)
                return;

            SpawnCore(position, velocity, color, shape, scale, hoverTime, lifetime, lanceSpeed, homingRange,
                linksToSiblings, LeonidStarlightMotion.FreeDrift, Main.rand.NextFloat(MathHelper.TwoPi));
        }

        // 四散爆开：流星炸开、狮首咆哮这类"从一点向外炸"的场合。
        // 每颗的悬停时长有随机偏移，锁定不会整齐划一地同时触发。
        public static void Burst(
            Vector2 center,
            int count,
            Color? color = null,
            LeonidStarlightShape shape = LeonidStarlightShape.Mote,
            float speed = 6f,
            float scale = 1f,
            int hoverTime = 26,
            int lifetime = 150,
            float lanceSpeed = 17f,
            float spawnRadius = 6f)
        {
            count = ReducedCount(count);
            if (count <= 0 || !CanSpawn(count))
                return;

            int trackerIndex = TryReserveTracker(center) ? Main.rand.Next(count) : -1;

            for (int i = 0; i < count; i++)
            {
                Color tint = color ?? DefaultColor(i * 0.21f);
                SpawnCore(
                    center + Main.rand.NextVector2Circular(spawnRadius, spawnRadius),
                    Main.rand.NextVector2Circular(speed, speed) * Main.rand.NextFloat(0.55f, 1f),
                    tint,
                    shape,
                    scale * Main.rand.NextFloat(0.75f, 1.25f),
                    hoverTime + Main.rand.Next(-6, 13),
                    lifetime + Main.rand.Next(-20, 21),
                    lanceSpeed * Main.rand.NextFloat(0.85f, 1.2f),
                    760f,
                    true,
                    i == trackerIndex ? LeonidStarlightMotion.Homing : LeonidStarlightMotion.BurstArc,
                    i * MathHelper.TwoPi / count + Main.rand.NextFloat(-0.18f, 0.18f));
            }
        }

        // 均匀星环：需要读得出"阵型"的场合（蓄力完成、终结技展开）。
        public static void Ring(
            Vector2 center,
            int count,
            float radius,
            Color? color = null,
            LeonidStarlightShape shape = LeonidStarlightShape.Mote,
            float speed = 4f,
            float scale = 1f,
            int hoverTime = 30,
            int lifetime = 160,
            float lanceSpeed = 17f,
            float angleOffset = 0f)
        {
            count = ReducedCount(count);
            if (count <= 0 || !CanSpawn(count))
                return;

            int trackerIndex = TryReserveTracker(center) ? Main.rand.Next(count) : -1;

            for (int i = 0; i < count; i++)
            {
                float angle = angleOffset + i * MathHelper.TwoPi / count;
                Vector2 direction = angle.ToRotationVector2();
                Color tint = color ?? DefaultColor(i * 0.29f);

                SpawnCore(
                    center + direction * radius,
                    direction * speed,
                    tint,
                    shape,
                    scale,
                    hoverTime + Main.rand.Next(-4, 9),
                    lifetime,
                    lanceSpeed,
                    760f,
                    true,
                    i == trackerIndex ? LeonidStarlightMotion.Homing : LeonidStarlightMotion.RadialSpiral,
                    angle);
            }
        }

        // 锥形喷射：沿着某个方向甩出去，用在发射/挥击的瞬间。
        public static void Spray(
            Vector2 center,
            Vector2 direction,
            int count,
            Color? color = null,
            LeonidStarlightShape shape = LeonidStarlightShape.Mote,
            float speed = 7f,
            float spread = 0.75f,
            float scale = 1f,
            int hoverTime = 22,
            int lifetime = 140,
            float lanceSpeed = 17f)
        {
            count = ReducedCount(count);
            if (count <= 0 || !CanSpawn(count))
                return;

            direction = direction.SafeNormalize(Vector2.UnitY);
            int trackerIndex = TryReserveTracker(center) ? Main.rand.Next(count) : -1;
            for (int i = 0; i < count; i++)
            {
                Color tint = color ?? DefaultColor(i * 0.17f);
                Vector2 velocity = direction.RotatedByRandom(spread) * speed * Main.rand.NextFloat(0.6f, 1.15f);

                SpawnCore(
                    center + Main.rand.NextVector2Circular(5f, 5f),
                    velocity,
                    tint,
                    shape,
                    scale * Main.rand.NextFloat(0.8f, 1.2f),
                    hoverTime + Main.rand.Next(-5, 10),
                    lifetime,
                    lanceSpeed,
                    760f,
                    true,
                    i == trackerIndex ? LeonidStarlightMotion.Homing : LeonidStarlightMotion.SprayFan,
                    i * MathHelper.TwoPi / count + Main.rand.NextFloat(-0.16f, 0.16f));
            }
        }

        // 飞行途中随手掉落的一粒星屑。调用方自己控制频率。
        public static void Shed(
            Vector2 position,
            Vector2 travelVelocity,
            Color? color = null,
            LeonidStarlightShape shape = LeonidStarlightShape.Mote,
            float scale = 0.7f,
            int hoverTime = 20,
            int lifetime = 110)
        {
            if (Main.dedServ || Main.rand.NextFloat() >= SpawnKeepChance)
                return;

            Vector2 velocity = -travelVelocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.5f, 2f)
                + Main.rand.NextVector2Circular(1.1f, 1.1f);

            SpawnCore(
                position + Main.rand.NextVector2Circular(6f, 6f),
                velocity,
                color ?? DefaultColor(Main.rand.NextFloat(6f)),
                shape,
                scale,
                hoverTime + Main.rand.Next(-4, 9),
                lifetime,
                14f,
                760f,
                true,
                LeonidStarlightMotion.TrailScatter,
                Main.rand.NextFloat(MathHelper.TwoPi));
        }

        // 从高空一条横带上洒下来的星雨，用于终结技。
        public static void Rain(
            Vector2 center,
            float halfWidth,
            float height,
            int count,
            Color? color = null,
            LeonidStarlightShape shape = LeonidStarlightShape.Mote,
            float scale = 1f,
            int hoverTime = 34,
            int lifetime = 190)
        {
            count = ReducedCount(count);
            if (count <= 0 || !CanSpawn(count))
                return;

            int trackerIndex = TryReserveTracker(center) ? Main.rand.Next(count) : -1;

            for (int i = 0; i < count; i++)
            {
                Vector2 position = center + new Vector2(
                    Main.rand.NextFloat(-halfWidth, halfWidth),
                    -height + Main.rand.NextFloat(-70f, 70f));

                SpawnCore(
                    position,
                    new Vector2(Main.rand.NextFloat(-1.4f, 1.4f), Main.rand.NextFloat(3f, 7f)),
                    color ?? DefaultColor(i * 0.23f),
                    shape,
                    scale * Main.rand.NextFloat(0.7f, 1.3f),
                    hoverTime + Main.rand.Next(-8, 17),
                    lifetime,
                    19f,
                    760f,
                    true,
                    i == trackerIndex ? LeonidStarlightMotion.Homing : LeonidStarlightMotion.RainFall,
                    i * MathHelper.TwoPi / count + Main.rand.NextFloat(-0.22f, 0.22f));
            }
        }

        // ── 引力场联动 ─────────────────────────────────────────────
        // 终结技每帧报点，星光会跟着一起被往下拽。同一时间只可能有一个场，单槽足够。

        public static void ReportGravityWell(Vector2 center, float strength)
        {
            gravityWellCenter = center;
            gravityWellStrength = strength;
            gravityWellFrame = Main.GameUpdateCount;
        }

        public static bool TryGetGravityWell(out Vector2 center, out float strength)
        {
            center = gravityWellCenter;
            strength = gravityWellStrength;
            return Main.GameUpdateCount - gravityWellFrame <= 2;
        }

        // ── 弹幕 PreDraw 用的绘制助手 ──────────────────────────────
        // 以下全部跑在默认 AlphaBlend 批次里，靠 A = 0 达成加法效果，
        // 调用方不需要切换混合模式。

        // 一枚十字星芒。给弹幕头部加一点"星"的锐度。
        public static void DrawFlare(Vector2 worldPosition, Color color, float opacity, float scale, float rotation)
        {
            Texture2D flare = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/ShineFlare").Value;
            color.A = 0;

            Main.EntitySpriteDraw(
                flare,
                worldPosition - Main.screenPosition,
                null,
                color * opacity,
                rotation,
                flare.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0f);
        }

        // 放射状大耀斑。972² 原图，scale 建议 0.02~0.06。
        public static void DrawSunburst(Vector2 worldPosition, Color color, float opacity, float scale, float rotation)
        {
            Texture2D burst = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/BloomFlare").Value;
            color.A = 0;

            Main.EntitySpriteDraw(
                burst,
                worldPosition - Main.screenPosition,
                null,
                color * opacity,
                rotation,
                burst.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0f);
        }

        // 准星环。锁定预告、蓄力刻度这类"机械感"提示。
        public static void DrawReticle(Vector2 worldPosition, Color color, float opacity, float scale, float rotation)
        {
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/FadedStarRing").Value;
            color.A = 0;

            Main.EntitySpriteDraw(
                ring,
                worldPosition - Main.screenPosition,
                null,
                color * opacity,
                rotation,
                ring.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0f);
        }

        // 沿弹幕的历史轨迹铺一串逐渐收缩、逐渐扭转的星芒——
        // 这就是「盲目正义」拖尾的核心手法，换成本武器的配色。
        public static void DrawStarTrail(
            Vector2[] oldPositions,
            Vector2 hitboxSize,
            Color innerColor,
            Color outerColor,
            float opacity,
            float scale,
            float rotation,
            int step = 2,
            float twist = 0.18f)
        {
            if (oldPositions == null || opacity <= 0.002f)
                return;

            Texture2D flare = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/ShineFlare").Value;
            Vector2 origin = flare.Size() * 0.5f;
            Vector2 halfSize = hitboxSize * 0.5f;

            for (int i = oldPositions.Length - 1; i >= 0; i -= step)
            {
                if (oldPositions[i] == Vector2.Zero)
                    continue;

                float t = 1f - i / (float)oldPositions.Length;
                Color color = Color.Lerp(outerColor, innerColor, t);
                color.A = 0;

                Main.EntitySpriteDraw(
                    flare,
                    oldPositions[i] + halfSize - Main.screenPosition,
                    null,
                    color * (opacity * t * t),
                    rotation + i * twist,
                    origin,
                    scale * (0.3f + 0.7f * t),
                    SpriteEffects.None,
                    0f);
            }
        }

        // 绕着一点公转的几枚小星，用来给蓄力/充能这类"聚拢"状态加环绕感。
        public static void DrawOrbitingStars(
            Vector2 worldPosition,
            Color color,
            float opacity,
            float scale,
            float radius,
            int count = 4,
            float speed = 1.6f)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = Main.GlobalTimeWrappedHourly * speed + i * MathHelper.TwoPi / count;
                float breathe = 0.8f + 0.2f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3.1f + i);
                Vector2 offset = angle.ToRotationVector2() * radius * breathe;

                DrawFlare(
                    worldPosition + offset,
                    Color.Lerp(color, LeonidVisualUtils.MoonWhite, 0.35f),
                    opacity * breathe,
                    scale,
                    -angle);
            }
        }
    }

    internal sealed class LeonidStarlightResetSystem : ModSystem
    {
        public override void OnWorldUnload() => LeonidStarlight.ResetStaticState();
    }
}
