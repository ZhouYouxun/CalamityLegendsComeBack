using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common
{
    /// <summary>
    /// 「到点才出现的危险」的公共预告弹幕。
    ///
    /// 这套模板原本只存在于 Cryogen 的 CryogenFrostColumnTelegraph 里，是全项目唯一一个独立预告弹幕类；
    /// 其余 17 个 boss 想加预警都得从零手写，于是大多数干脆就没加（各家 PreDraw 里的预警线只有 1~3 处，
    /// Cryogen 有 17 处）。抽出来之后，加一处"这里 N 帧后会落下危险"的成本降到写一个 SpawnPayload。
    ///
    /// 分工是这套模板的关键，别打破它：
    ///   · <b>占位</b>由 Boss 完成 —— Boss 决定在哪里生成本预告，这是它的空间编排。
    ///   · <b>预告</b>由弹幕本体承担 —— 它自己画自己、自己数帧，CanDamage 恒为 false。
    ///   · <b>危险</b>到点才由 SpawnPayload 生成 —— 真正会打人的东西是另一个弹幕。
    ///
    /// 为什么 CanDamage 和 AI 是 sealed：预警绝不能伤人、也绝不能提前或延后开火。这两条是
    /// 玩家判断"这个 boss 讲不讲道理"的底线，不留给子类改写的余地。子类要做的事都有对应的钩子。
    ///
    /// ai[0] = 预警时长（帧）；&lt;= 0 时用 <see cref="DefaultDuration"/>
    /// ai[1] = 到点后交给危险物的伤害
    /// </summary>
    public abstract class LegendsTelegraph : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        /// <summary>ai[0] 没给时的默认预警时长。太短玩家来不及读，建议不低于 30 帧。</summary>
        protected virtual int DefaultDuration => 45;

        /// <summary>开火瞬间的音效。默认无声 —— 有音效的话请用清脆短促的，别盖过 Boss 本体的招式音。</summary>
        protected virtual SoundStyle? FireSound => null;

        protected int Duration => Projectile.ai[0] <= 0f ? DefaultDuration : (int)Projectile.ai[0];
        protected int PayloadDamage => (int)Projectile.ai[1];
        protected float Elapsed => Projectile.localAI[0];

        /// <summary>0 → 1 的预警进度。绘制时用它把"越来越危险"表达出来（变亮、收窄、加速闪）。</summary>
        protected float Progress => MathHelper.Clamp(Elapsed / Math.Max(Duration, 1), 0f, 1f);

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        // 铁律：预警本身永远不伤人。玩家必须能确信"亮着的线还打不到我"，否则预警就失去意义了。
        public sealed override bool? CanDamage() => false;

        public sealed override void AI()
        {
            Projectile.localAI[0]++;
            WarningTick(Progress);

            if (Elapsed < Duration)
                return;

            // 只有服务端/单机生成真正的危险物，客户端只负责把预警画出来
            if (Main.netMode != NetmodeID.MultiplayerClient)
                SpawnPayload(PayloadDamage);

            if (FireSound.HasValue)
                SoundEngine.PlaySound(FireSound.Value, Projectile.Center);

            Projectile.Kill();
        }

        /// <summary>到点了：在这里生成真正会打人的东西。只在服务端/单机被调用。</summary>
        protected abstract void SpawnPayload(int damage);

        /// <summary>预警期间每帧调用（各端都会跑）。用来喷粒子、或者让预警跟着目标移动。</summary>
        protected virtual void WarningTick(float progress) { }

        // --- 绘制辅助 --------------------------------------------------------------------------------------
        // 加法混合作用域 + 直线绘制。全项目的预警线以前各写各的 End/Begin，MagicPixel 采样也各写各的
        // （那张图是 1x1000，不裁一个像素出来的话"线"会变成一整块）。收敛到这里，两个坑都只有一处能踩错。

        protected static void BeginAdditive(SpriteBatch sb)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }

        protected static void EndAdditive(SpriteBatch sb)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>世界坐标画一条线。MagicPixel 是 1x1000，必须只采样一个像素。</summary>
        protected static void DrawWarnLine(SpriteBatch sb, Vector2 worldStart, Vector2 worldEnd, Color color, float width)
        {
            Vector2 delta = worldEnd - worldStart;
            sb.Draw(
                TextureAssets.MagicPixel.Value,
                worldStart - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                delta.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(delta.Length(), width),
                SpriteEffects.None,
                0f);
        }

        /// <summary>
        /// 标准"预警线"观感：一条宽而暗的辉光随进度收窄，中间一条越来越亮的芯。
        /// 直接用这个能让全项目的预警长得像同一个模式的东西，玩家换 Boss 不用重新学怎么读。
        /// </summary>
        protected static void DrawStandardWarnBeam(SpriteBatch sb, Vector2 worldStart, Vector2 worldEnd, Color tint, float progress)
        {
            float flash = 0.35f + 0.65f * progress;
            float wide = MathHelper.Lerp(10f, 4f, progress);
            float core = MathHelper.Lerp(1.5f, 3.5f, progress);

            BeginAdditive(sb);
            DrawWarnLine(sb, worldStart, worldEnd, Color.Lerp(tint, Color.White, progress) * flash * 0.7f, wide);
            DrawWarnLine(sb, worldStart, worldEnd, Color.White * flash, core);
            EndAdditive(sb);
        }
    }
}
