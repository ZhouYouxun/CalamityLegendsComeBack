using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    // 五种战术各自的环绕轨迹风格。载体统一是着色拖尾，运动按风格分派，
    // 具体的数量/半径/寿命/速度档/加减速全部由生成器(NewLegendBlossomFluxHoldOut.RightChargeLattice.cs)
    // 通过 HaloSpawnParams 一次性喂进来，这里只负责“把参数跑成轨迹”。
    internal enum BFHaloStyle
    {
        SlashStar,   // A 破甲：玫瑰线尖瓣星，快转 + 锐利半径脉冲，像刀光
        RisingHelix, // B 恢复：慢转小圆 + 持续上浮 + 呼吸，柔和
        GyroRing,    // C 侦测：斜圆投影，整环同倾角相位均分，匀速陀螺仪
        EmberBurst,  // D 爆破：绝对坐标弹道余烬，高速外抛 + 强阻力减速
        WobbleCloud, // E 瘟疫：慢转 + 垂直正弦抖动 + 半径缓扩，粘稠扩散
    }

    // 蓄力光环：无伤、纯客户端视觉、着色器驱动。历史轨迹只存“相对武器锚点的偏移量”，
    // 锚点每帧重读武器当前位置与朝向，所以整团环绕会跟着枪走（相对运动而非绝对运动）。
    internal sealed class BFRightChargeHaloProj : ModProjectile, IPixelatedPrimitiveRenderer
    {
        // 生成器一次性传入的整套配置（每战术自己一份）。运动过程中会变的量另存为下面的可变字段。
        public struct HaloSpawnParams
        {
            public BFHaloStyle Style;
            public Color Color;
            public float Charge;

            // 锚点（武器局部：Forward 沿瞄准方向，Side 垂直方向）
            public float AnchorForward;
            public float AnchorSide;

            // 生命周期与淡入淡出（帧、比例）
            public float LifeSpan;
            public float FadeInFraction;
            public float FadeOutFraction;

            // 环绕自转：转速随蓄力进度在 Min~Max 间插值；蓄满/松开后按 ReadyDecel 衰减到 0
            public float StartRotation;
            public float MinSpeed;
            public float MaxSpeed;
            public float ChargingFollow;
            public float ReadyDecel;
            public float FadeDecay;

            // 半径与拖尾
            public float Radius;
            public float HalfWidth;
            public int TrailPoints;

            // —— 风格专属 ——
            // SlashStar
            public float Lobes;
            public float Sharpness;
            public float SpinPhase;
            // GyroRing
            public float TiltZ;
            public float TiltEx;
            // RisingHelix
            public float RiseSpeed;
            public float Squash;
            // EmberBurst
            public Vector2 EmberVelocity;
            public float EmberDrag;
            public float EmberGravity;
            // WobbleCloud
            public float ExpandSpeed;
            public float WobbleAmp;
            public float WobbleFreq;
        }

        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Projectile weapon;
        private NewLegendBlossomFluxHoldOut holdout;
        private HaloSpawnParams config;

        // 运动过程中的可变状态
        private float spinRotation;
        private float currentSpeed;
        private float runtimeRadius;
        private Vector2 emberOffset;
        private Vector2 emberVel;
        private float trailAlpha;
        private float postReadyFade = 1f;
        private bool enteredReadyPhase;
        private int time;
        private Vector2[] relativeOffsets;

        private bool WeaponValid =>
            weapon is not null &&
            weapon.active &&
            weapon.type == ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>();

        private Vector2 Anchor
        {
            get
            {
                Vector2 forward = weapon.rotation.ToRotationVector2();
                Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
                return weapon.Center + forward * config.AnchorForward + side * config.AnchorSide;
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 0; // 必须保持每帧只更新一次，否则转速会翻倍
            Projectile.timeLeft = 1800; // 纯兜底，正常寿命由下面的淡入淡出逻辑接管
        }

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor) => false;

        public static void Spawn(Projectile weaponProjectile, in HaloSpawnParams config)
        {
            if (Main.dedServ || weaponProjectile is null)
                return;

            // 纯客户端视觉：owner 传 Main.maxPlayers(255)，NewProjectile 就不会联网同步，
            // 每个客户端各自生成自己的一份，多人里也不会出现重影。
            Projectile spawned = Projectile.NewProjectileDirect(
                weaponProjectile.GetSource_FromThis(),
                weaponProjectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<BFRightChargeHaloProj>(),
                0,
                0f,
                Main.maxPlayers);

            if (spawned is null || !spawned.active || spawned.ModProjectile is not BFRightChargeHaloProj halo)
                return;

            halo.weapon = weaponProjectile;
            halo.holdout = weaponProjectile.ModProjectile as NewLegendBlossomFluxHoldOut;
            halo.config = config;
            halo.spinRotation = config.StartRotation;
            halo.runtimeRadius = config.Radius;
            halo.currentSpeed = MathHelper.Lerp(config.MinSpeed, config.MaxSpeed, MathHelper.Clamp(config.Charge, 0f, 1f));
            halo.emberVel = config.EmberVelocity;
            halo.SeedTrail();
            halo.SnapToAnchor();
        }

        // 初始化拖尾历史：环绕风格倒推几步自转，让一出生就有一段弧；余烬风格沿反速度方向铺一条短尾。
        private void SeedTrail()
        {
            int n = Math.Max(4, config.TrailPoints);
            relativeOffsets = new Vector2[n];

            if (config.Style == BFHaloStyle.EmberBurst)
            {
                Vector2 tailDir = config.EmberVelocity.SafeNormalize(Vector2.UnitX);
                for (int i = 0; i < n; i++)
                    relativeOffsets[i] = -tailDir * ((n - 1 - i) * 1.5f);
                emberOffset = Vector2.Zero;
                return;
            }

            float step = MathHelper.Max(currentSpeed, 0.02f);
            float spin = spinRotation - (n - 1) * step;
            for (int i = 0; i < n; i++)
            {
                relativeOffsets[i] = ComputeStyleOffset(spin, 0f);
                spin += step;
            }
        }

        public override void AI()
        {
            if (!WeaponValid || relativeOffsets is null)
            {
                Projectile.Kill();
                return;
            }

            time++;

            // 一旦观测到蓄满或右键停止蓄力，就永久进入“减速 + 淡出”阶段，不会因读数波动而反复横跳。
            if (!enteredReadyPhase && (holdout is null || !holdout.GetHaloRightChargeActive() || holdout.GetHaloChargeReady()))
                enteredReadyPhase = true;

            Vector2 head = config.Style == BFHaloStyle.EmberBurst ? UpdateEmber() : UpdateOrbit();

            for (int i = 0; i < relativeOffsets.Length - 1; i++)
                relativeOffsets[i] = relativeOffsets[i + 1];
            relativeOffsets[^1] = head;

            if (!UpdateLifetime())
                return;

            SnapToAnchor();
        }

        // 环绕类：转速随蓄力进度加速（蓄满后非线性减速回 0），再按风格算出当前头部偏移。
        private Vector2 UpdateOrbit()
        {
            float chargeCompletion = holdout?.GetChargeCompletion() ?? 0f;
            float targetSpeed = enteredReadyPhase
                ? 0f
                : MathHelper.Lerp(config.MinSpeed, config.MaxSpeed, MathHelper.Clamp(chargeCompletion, 0f, 1f));
            float followRate = enteredReadyPhase ? config.ReadyDecel : config.ChargingFollow;

            currentSpeed = MathHelper.Lerp(currentSpeed, targetSpeed, followRate);
            spinRotation += currentSpeed;

            return ComputeStyleOffset(spinRotation, time);
        }

        // 余烬类：绝对坐标里的弹道积分——初速外抛、强阻力减速、微重力下坠，头部偏移就是积分位置。
        private Vector2 UpdateEmber()
        {
            emberVel *= config.EmberDrag;
            emberVel.Y += config.EmberGravity;
            emberOffset += emberVel;
            return emberOffset;
        }

        // 返回 false 表示本帧已 Kill。
        private bool UpdateLifetime()
        {
            if (enteredReadyPhase)
            {
                postReadyFade *= config.FadeDecay;
                trailAlpha = postReadyFade;
                if (postReadyFade < 0.03f)
                {
                    Projectile.Kill();
                    return false;
                }
                return true;
            }

            float life = MathHelper.Max(config.LifeSpan, 1f);
            float fadeIn = life * config.FadeInFraction;
            float fadeOutStart = life * (1f - config.FadeOutFraction);

            trailAlpha = time < fadeIn ? time / MathHelper.Max(fadeIn, 1f) : 1f;

            if (time > fadeOutStart)
            {
                float k = 1f - (time - fadeOutStart) / MathHelper.Max(life - fadeOutStart, 1f);
                trailAlpha *= MathHelper.Clamp(k, 0f, 1f);
                if (time >= life)
                {
                    Projectile.Kill();
                    return false;
                }
            }

            return true;
        }

        // 各风格的核心：给定当前自转角与年龄，算出相对锚点的头部偏移。余烬风格不走这里。
        private Vector2 ComputeStyleOffset(float spin, float age)
        {
            switch (config.Style)
            {
                case BFHaloStyle.SlashStar:
                {
                    // 玫瑰线：半径随角度做尖锐脉冲，形成旋转的尖瓣星（刀光感），Sharpness 越大越尖。
                    float lobe = MathF.Pow(MathF.Abs(MathF.Sin(config.Lobes * spin + config.SpinPhase)), config.Sharpness);
                    float r = config.Radius * (0.38f + 0.62f * lobe);
                    return spin.ToRotationVector2() * r;
                }

                case BFHaloStyle.RisingHelix:
                {
                    // 慢转小圆 + 轻微竖椭圆 + 呼吸，整体随年龄向上漂，像治愈光点上浮。
                    float breathe = 1f + 0.12f * MathF.Sin(age * 0.05f + config.SpinPhase);
                    Vector2 c = spin.ToRotationVector2() * (config.Radius * breathe);
                    c.Y *= config.Squash;
                    return c - new Vector2(0f, config.RiseSpeed * age);
                }

                case BFHaloStyle.GyroRing:
                {
                    // 斜圆正交投影（保留旧版投影核心）：同一环的多枚共享 TiltZ/TiltEx，相位均分即成陀螺仪环。
                    float len = TiltedCircleProjection(spin, config.TiltZ, out float overrideAngle) * config.Radius;
                    return (overrideAngle + config.TiltEx).ToRotationVector2() * len;
                }

                case BFHaloStyle.WobbleCloud:
                {
                    // 慢转 + 半径随年龄缓扩 + 垂直方向叠层正弦抖动（伪噪声），粘稠地向外弥散。
                    float rr = config.Radius + config.ExpandSpeed * age;
                    Vector2 c = spin.ToRotationVector2() * rr;
                    Vector2 normal = (spin + MathHelper.PiOver2).ToRotationVector2();
                    float wob = MathF.Sin(age * config.WobbleFreq + config.SpinPhase) * config.WobbleAmp
                              + MathF.Sin(age * config.WobbleFreq * 0.5f + config.SpinPhase * 1.7f) * config.WobbleAmp * 0.5f;
                    return c + normal * wob;
                }

                default:
                    return spin.ToRotationVector2() * config.Radius;
            }
        }

        // 弹幕本体不参与绘制，但把位置贴到拖尾头部，光照/调试才对得上。
        private void SnapToAnchor()
        {
            Projectile.Center = Anchor + relativeOffsets[^1];
            Projectile.velocity = Vector2.Zero;
        }

        // relativeOffsets 存的是「旧→新」，而图元拖尾约定 completion=0 在头部，所以倒着取。
        private Vector2[] BuildRenderPoints()
        {
            Vector2 anchor = Anchor;
            int count = relativeOffsets.Length;
            Vector2[] points = new Vector2[count];

            for (int i = 0; i < count; i++)
                points[i] = anchor + relativeOffsets[count - 1 - i];

            return points;
        }

        private float HaloWidthFunction(float completion, Vector2 _) => StreakWidth(completion, config.HalfWidth);

        private static float StreakWidth(float completion, float maxBodyWidth)
        {
            const float curveRatio = 0.15f;

            if (completion < curveRatio)
                return MathF.Sin(completion / curveRatio * MathHelper.PiOver2) * maxBodyWidth + curveRatio;

            return Utils.Remap(completion, curveRatio, 1f, maxBodyWidth, 0f);
        }

        private Color HaloColorFunction(float completion, Vector2 _) => StreakColor(completion, config.Color);

        private Color StreakColor(float completion, Color baseColor)
        {
            Color tipColor = Color.Lerp(baseColor, Color.Transparent, Utils.GetLerpValue(0.8f, 1f, completion, true));
            Color color = Color.Lerp(baseColor, tipColor, completion);

            // 越靠尾部越暗的线性衰减，保持明暗层次。
            return color * (trailAlpha * MathHelper.Lerp(1f, 0.1f, completion));
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            if (!WeaponValid || relativeOffsets is null || trailAlpha <= 0.01f)
                return;

            Vector2[] trailPoints = BuildRenderPoints();
            if (trailPoints.Length < 4)
                return;

            // 传统着色拖尾：TrailStreak + SylvestaffStreak 单层描边流，配色用我们自己的 haloColor，
            // 宽度保持本项目纤细口径。
            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    HaloWidthFunction,
                    HaloColorFunction,
                    (_, _) => Vector2.Zero,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:TrailStreak"]),
                trailPoints.Length * 2);
        }

        // 先在XY平面取一个正圆上的点，绕X轴转 zRot 把这个圆“斜过来”一个角度，
        // 再直接丢掉Z轴做正交投影，圆就被压扁成椭圆弧——GyroRing 的投影核心。
        private static float TiltedCircleProjection(float rotation, float zRot, out float overrideAngle)
        {
            Vector3 circlePoint = new(MathF.Cos(rotation), MathF.Sin(rotation), 0f);
            Vector3 tilted = Vector3.Transform(circlePoint, Matrix.CreateRotationX(zRot - MathHelper.PiOver2));
            Vector2 targetDir = new(tilted.X, tilted.Y);
            overrideAngle = targetDir.ToRotation();
            return targetDir.Length();
        }
    }
}
