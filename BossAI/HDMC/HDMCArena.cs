using System;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.HDMC
{
    /// <summary>
    /// 超维矩阵·数据牢笼——Boss 登场瞬间建立的矩形边界。
    /// 技术沿用灾厄克隆体竞技场（软墙约束本地玩家 + 越界惩罚），配色换成矩阵数据流风格。
    ///
    /// 设计分寸：
    ///  · 「软墙」——距边缘 PushZone 内开始施加递增的向心推力，越靠边推得越狠；
    ///  · 越过硬边界会被钳回并触发一次「数据反噬」（掉血 + 爆散 + 震屏），带冷却，不会连锁秒杀；
    ///  · 边界四条边会「感知」玩家：玩家靠近哪条边，那条边就高亮加粗——既是警示也是演出。
    ///
    /// ai[0] = 宿主 Boss 的 whoAmI。中心 = 生成位置（登场时的玩家位置）。
    /// </summary>
    public sealed class HDMCArena : ModProjectile
    {
        // 半宽 / 半高（全尺寸 2800 × 1900，与灾厄克隆体竞技场同量级）
        public const float HalfWidth = 1400f;
        public const float HalfHeight = 950f;

        private const float PushZone = 280f;     // 距边缘多近开始向心推
        private const float HurtCooldown = 28f;  // 越界反噬冷却帧

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Host => (int)Projectile.ai[0];
        private ref float Age => ref Projectile.localAI[0];
        private ref float Fade => ref Projectile.localAI[1];
        private ref float HurtTimer => ref Projectile.localAI[2];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2; // 自管生命周期，逐帧顶回安全值
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        private bool HostAlive()
        {
            int h = Host;
            if (h < 0 || h >= Main.maxNPCs)
                return false;
            NPC n = Main.npc[h];
            return n.active && n.type == ModContent.NPCType<HDMCSovereign>();
        }

        public override void AI()
        {
            Projectile.timeLeft = 2;
            Age++;

            bool alive = HostAlive();

            // 淡入；宿主消失后淡出并自尽
            if (alive)
            {
                Fade = MathHelper.Clamp(Fade + 1f / 40f, 0f, 1f);
            }
            else
            {
                Fade = MathHelper.Clamp(Fade - 1f / 45f, 0f, 1f);
                if (Fade <= 0f)
                {
                    Projectile.Kill();
                    return;
                }
            }

            if (HurtTimer > 0f)
                HurtTimer--;

            if (Age == 1f && !Main.dedServ)
                SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndSpaceWarp) { Volume = 0.6f, Pitch = -0.2f }, Projectile.Center);

            Lighting.AddLight(Projectile.Center, HDMCUtil.DataColor(Age * 0.01f).ToVector3() * 0.15f);

            // 完全成形后才开始约束（给玩家看清边界成型的余地）
            if (alive && Fade >= 0.5f)
                ConfineLocalPlayer();
        }

        /// <summary>软墙约束本地玩家：近边向心推，越界钳回 + 数据反噬。</summary>
        private void ConfineLocalPlayer()
        {
            Player p = Main.LocalPlayer;
            if (p is null || !p.active || p.dead)
                return;

            Vector2 c = Projectile.Center;
            bool breached = false;
            Vector2 breachPoint = p.Center;

            float left = c.X - HalfWidth, right = c.X + HalfWidth;
            float top = c.Y - HalfHeight, bottom = c.Y + HalfHeight;

            // ── X 轴 ──
            if (p.Center.X < left + PushZone)
            {
                p.velocity.X += ((left + PushZone) - p.Center.X) * 0.01f;
                if (p.Center.X < left)
                {
                    p.position.X += left - p.Center.X;
                    if (p.velocity.X < 0f) p.velocity.X = 0f;
                    breached = true; breachPoint.X = left;
                }
            }
            else if (p.Center.X > right - PushZone)
            {
                p.velocity.X -= (p.Center.X - (right - PushZone)) * 0.01f;
                if (p.Center.X > right)
                {
                    p.position.X += right - p.Center.X;
                    if (p.velocity.X > 0f) p.velocity.X = 0f;
                    breached = true; breachPoint.X = right;
                }
            }

            // ── Y 轴 ──
            if (p.Center.Y < top + PushZone)
            {
                p.velocity.Y += ((top + PushZone) - p.Center.Y) * 0.01f;
                if (p.Center.Y < top)
                {
                    p.position.Y += top - p.Center.Y;
                    if (p.velocity.Y < 0f) p.velocity.Y = 0f;
                    breached = true; breachPoint.Y = top;
                }
            }
            else if (p.Center.Y > bottom - PushZone)
            {
                p.velocity.Y -= (p.Center.Y - (bottom - PushZone)) * 0.01f;
                if (p.Center.Y > bottom)
                {
                    p.position.Y += bottom - p.Center.Y;
                    if (p.velocity.Y > 0f) p.velocity.Y = 0f;
                    breached = true; breachPoint.Y = bottom;
                }
            }

            if (breached && HurtTimer <= 0f && !p.HasBuff(BuffID.ChaosState))
            {
                HurtTimer = HurtCooldown;
                p.Hurt(
                    PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral($"{p.name} 试图越过数据边界，被反噬解构了。")),
                    120, 0);

                if (!Main.dedServ)
                {
                    HDMCUtil.DataBurstParticles(breachPoint, 10, 6, 8f);
                    HDMCUtil.ScreenShake(breachPoint, 3f, 700f);
                    SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndGeoBurst) { Volume = 0.5f, Pitch = 0.3f }, breachPoint);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = Main.GlobalTimeWrappedHourly;
            float op = MathHelper.Clamp(Fade, 0f, 1f);
            if (op <= 0.001f)
                return false;

            Vector2 c = Projectile.Center;
            Vector2 tl = c + new Vector2(-HalfWidth, -HalfHeight);
            Vector2 tr = c + new Vector2(HalfWidth, -HalfHeight);
            Vector2 bl = c + new Vector2(-HalfWidth, HalfHeight);
            Vector2 br = c + new Vector2(HalfWidth, HalfHeight);

            Player p = Main.LocalPlayer;

            // 四条边——玩家靠近哪条边，那条边就高亮加粗
            DrawEdge(tl, tr, p, t, op, 0.02f); // 上
            DrawEdge(br, bl, p, t, op, 0.30f); // 下
            DrawEdge(bl, tl, p, t, op, 0.55f); // 左
            DrawEdge(tr, br, p, t, op, 0.78f); // 右

            // 角括号
            DrawCorner(tl, new Vector2(1f, 1f), op);
            DrawCorner(tr, new Vector2(-1f, 1f), op);
            DrawCorner(bl, new Vector2(1f, -1f), op);
            DrawCorner(br, new Vector2(-1f, -1f), op);

            return false;
        }

        private void DrawEdge(Vector2 a, Vector2 b, Player p, float t, float op, float hue)
        {
            float prox = MathHelper.Clamp(1f - DistancePointToSegment(p.Center, a, b) / PushZone, 0f, 1f);
            float baseAlpha = 0.32f + prox * 0.6f;

            Color glow = HDMCUtil.DataColor(hue + t * 0.05f, op * baseAlpha * 0.35f);
            Color main = HDMCUtil.DataColor(hue + t * 0.05f, op * baseAlpha);
            Main.spriteBatch.DrawLineBetter(a, b, glow, 10f + prox * 10f);
            Main.spriteBatch.DrawLineBetter(a, b, main, 2.4f + prox * 1.6f);

            // 沿边流动的数据刻度节点
            int ticks = Math.Max(1, (int)(Vector2.Distance(a, b) / 90f));
            for (int i = 0; i <= ticks; i++)
            {
                float flow = (i / (float)ticks + t * 0.15f) % 1f;
                Vector2 pos = Vector2.Lerp(a, b, flow);
                HyperdimensionalMatrixVisuals.DrawNode(pos, HDMCUtil.DataColor(hue + flow, op * baseAlpha), 3f + prox * 3f);
            }
        }

        private void DrawCorner(Vector2 corner, Vector2 sign, float op)
        {
            const float arm = 60f;
            Color col = HDMCUtil.DataColor(0.12f, op * 0.9f);
            Vector2 hx = corner + new Vector2(arm * sign.X, 0f);
            Vector2 hy = corner + new Vector2(0f, arm * sign.Y);
            Main.spriteBatch.DrawLineBetter(corner, hx, col * 0.4f, 8f);
            Main.spriteBatch.DrawLineBetter(corner, hx, col, 3f);
            Main.spriteBatch.DrawLineBetter(corner, hy, col * 0.4f, 8f);
            Main.spriteBatch.DrawLineBetter(corner, hy, col, 3f);
            HyperdimensionalMatrixVisuals.DrawNode(corner, Color.White with { A = 0 } * op, 7f);
        }

        private static float DistancePointToSegment(Vector2 pt, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lenSq = ab.LengthSquared();
            if (lenSq < 0.0001f)
                return Vector2.Distance(pt, a);
            float tt = MathHelper.Clamp(Vector2.Dot(pt - a, ab) / lenSq, 0f, 1f);
            return Vector2.Distance(pt, a + ab * tt);
        }
    }
}
