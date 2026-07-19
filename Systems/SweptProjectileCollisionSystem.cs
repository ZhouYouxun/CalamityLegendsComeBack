using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Systems
{
    /// <summary>
    /// 让某个 ModProjectile 显式退出扫掠判定，恢复原版"只看当前帧矩形"的行为。
    /// </summary>
    internal interface INoSweptCollision
    {
    }

    /// <summary>
    /// 高速弹幕的连续碰撞检测（扫掠判定）。
    ///
    /// 原版只在弹幕移动完成后，用它当前所在的矩形去和敌人矩形求交。
    /// 一个子步位移 40 像素、判定框只有 8 像素的弹幕，完全可能在一步之内
    /// 从敌人身前跳到敌人身后，两次采样都不重叠，于是穿身而过零伤害。
    ///
    /// 这里把"这一子步的实际位移"还原成一条线段，用闵可夫斯基和把敌人矩形
    /// 按弹幕自身尺寸外扩，再做线段 vs 矩形相交。命中返回 true，没命中返回 null
    /// 交还给原版判定——只会补命中，绝不会吃掉本来能打中的情况。
    /// </summary>
    internal static class SweptProjectileCollision
    {
        // 子步位移小于这个距离时，原版末位置矩形不可能漏判整只敌人，直接放行省性能。
        private const float MinimumSweepDistance = 4f;

        // 按弹幕类型索引：本模组的、且没有自己写 Colliding 的弹幕才参与扫掠。
        private static bool[] sweepEligible = Array.Empty<bool>();

        internal static void BuildEligibilityTable(Mod mod)
        {
            List<ModProjectile> ownProjectiles = new(mod.GetContent<ModProjectile>());
            int highestType = 0;
            foreach (ModProjectile modProjectile in ownProjectiles)
                highestType = Math.Max(highestType, modProjectile.Type);

            bool[] table = new bool[highestType + 1];
            foreach (ModProjectile modProjectile in ownProjectiles)
            {
                if (modProjectile is INoSweptCollision)
                    continue;

                // 自己写了 Colliding 的弹幕（激光、挥砍、鞭子等）已经有手写判定，
                // 全局插一脚会抢在它前面短路掉真正的逻辑，一律不碰。
                if (DeclaresOwnColliding(modProjectile.GetType()))
                    continue;

                table[modProjectile.Type] = true;
            }

            sweepEligible = table;
        }

        internal static void ClearEligibilityTable()
        {
            sweepEligible = Array.Empty<bool>();
        }

        private static bool DeclaresOwnColliding(Type projectileType)
        {
            MethodInfo colliding = projectileType.GetMethod(
                nameof(ModProjectile.Colliding),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(Rectangle), typeof(Rectangle) },
                modifiers: null);

            return colliding is not null && colliding.DeclaringType != typeof(ModProjectile);
        }

        internal static bool IsEligible(Projectile projectile)
        {
            return (uint)projectile.type < (uint)sweepEligible.Length && sweepEligible[projectile.type];
        }

        /// <summary>
        /// 取这一子步里真正发生的位移。
        ///
        /// 原版 Projectile.Update 的顺序是：AI() → oldPosition = position → HandleMovement() → Damage()。
        /// 也就是说判定发生时，oldPosition 恰好是本子步"移动之前"的坐标，且每个 extraUpdate
        /// 子步都会刷新一次。AI 里的瞬移发生在快照之前，天然被排除；撞墙被截短、
        /// 入水减速、ShouldUpdatePosition 返回 false 导致原地不动，也都会如实反映。
        /// </summary>
        internal static Vector2 GetStepDisplacement(Projectile projectile)
        {
            Vector2 displacement = projectile.position - projectile.oldPosition;
            float distance = displacement.Length();

            // 位移比速度还长，说明是 AI 里手动调用 Damage() 之类的非常规路径。
            // 按速度长度截断，绝不在弹幕从未经过的地方凭空拉出一条判定线。
            float limit = projectile.velocity.Length() + 1f;
            if (distance > limit)
                return distance > 0f ? displacement / distance * limit : Vector2.Zero;

            return displacement;
        }

        /// <summary>
        /// 扫掠判定本体。已经自己写了 Colliding 的弹幕想补高速漏判时，
        /// 可以在自己的 Colliding 里直接调用它做或运算。
        /// </summary>
        internal static bool SweepHits(Projectile projectile, Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 displacement = GetStepDisplacement(projectile);
            if (displacement.LengthSquared() <= MinimumSweepDistance * MinimumSweepDistance)
                return false;

            Vector2 projSize = new(projHitbox.Width, projHitbox.Height);
            Vector2 travelEnd = new(projHitbox.X + projSize.X * 0.5f, projHitbox.Y + projSize.Y * 0.5f);
            Vector2 travelStart = travelEnd - displacement;

            // 把敌人矩形按弹幕尺寸外扩（闵可夫斯基和），这样"矩形扫过矩形"
            // 就等价于"弹幕中心的轨迹线段穿过外扩矩形"，判定范围与原版重叠完全一致。
            Vector2 expandedTopLeft = targetHitbox.TopLeft() - projSize * 0.5f;
            Vector2 expandedSize = targetHitbox.Size() + projSize;

            return Collision.CheckAABBvLineCollision(expandedTopLeft, expandedSize, travelStart, travelEnd);
        }
    }

    internal sealed class SweptProjectileCollisionSystem : ModSystem
    {
        public override void PostSetupContent()
        {
            SweptProjectileCollision.BuildEligibilityTable(Mod);
        }

        public override void Unload()
        {
            SweptProjectileCollision.ClearEligibilityTable();
        }
    }

    internal sealed class SweptProjectileCollisionGlobalProjectile : GlobalProjectile
    {
        public override bool? Colliding(Projectile projectile, Rectangle projHitbox, Rectangle targetHitbox)
        {
            // 只处理打敌人的玩家弹幕。敌对弹幕（BossAI）保持原版判定，
            // 免得把"擦身而过"变成"必中"，无声改掉 Boss 战难度。
            if (!projectile.friendly || projectile.hostile)
                return null;

            if (!SweptProjectileCollision.IsEligible(projectile))
                return null;

            if (SweptProjectileCollision.SweepHits(projectile, projHitbox, targetHitbox))
                return true;

            // 没扫到就交还原版判定，保证这套机制永远只加命中、不减命中。
            return null;
        }
    }
}
