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
    // 蓄力光环：每个实例是一个绕X轴斜过一个角度、再丢掉Z轴投影回屏幕的圆环弧段（3D→2D的“斜圆投影”），
    // 只活很短一段时间，自己转、自己缓慢扩张、自己淡入淡出，靠不断新生成来维持持续的观感。
    // 转速不是固定值：蓄力过程中读取武器当前的蓄力进度让转速持续加快，
    // 一旦蓄满（ChargeReady），转速改为非线性地衰减回0，同时整体透明度慢慢淡出直至消失；
    // 锚点每帧都重新读取武器当前位置和朝向，历史轨迹只存“相对武器锚点的偏移量”（相对运动而非绝对运动）。
    //
    // 相对旧的粒子版本：运动、生命周期、配色全部一字未改，只把“载体”从 DrawLineBetter 折线
    // 换成了传统着色拖尾（CalamityMod:TrailStreak + SylvestaffStreak，参考 EndlessDevourJavOrbSmall），
    // 配色仍用我们自己的 haloColor，宽度也保持本项目纤细口径。
    internal sealed class BFRightChargeHaloProj : ModProjectile, IPixelatedPrimitiveRenderer
    {
        private const int TrailPointCount = 10;
        private const float MinRotateSpeed = 0.08f;
        private const float MaxRotateSpeed = 0.5f;
        private const float ChargingFollowRate = 0.15f; // 蓄力中：转速比较灵敏地跟随蓄力进度
        private const float ReadyDecelFollowRate = 0.05f; // 蓄满后：转速缓慢、非线性地衰减到0
        private const float ReadyFadeDecay = 0.94f; // 蓄满后：透明度按指数衰减，慢慢消失
        private const float AnchorForwardOffset = 16f;

        // 图元的宽度函数返回的是“半宽”，这里的取值对应旧版折线 1.4~5.5px 的观感（着色器拖尾边缘更软，略放宽一点）。
        private const float TrailHalfWidth = 3.6f;

        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Projectile weapon;
        private NewLegendBlossomFluxHoldOut holdout;
        private float spinRotation;
        private float zRot;
        private float exRot;
        private float radius;
        private float lifeSpanTicks;
        private float trailAlpha;
        private float currentSpeed;
        private float postReadyFade = 1f;
        private bool enteredReadyPhase;
        private int time;
        private Color haloColor;
        private Vector2[] relativeOffsets;

        private bool WeaponValid =>
            weapon is not null &&
            weapon.active &&
            weapon.type == ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>();

        private Vector2 Anchor => weapon.Center + weapon.rotation.ToRotationVector2() * AnchorForwardOffset;

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

        public static void Spawn(Projectile weaponProjectile, float radius, float startRot, float zRot, float exRot, Color color, float chargeCompletionAtSpawn)
        {
            if (Main.dedServ || weaponProjectile is null)
                return;

            // 纯客户端视觉：owner 传 Main.maxPlayers(255)，NewProjectile 就不会联网同步，
            // 每个客户端各自生成自己的一份——和原来的粒子表现完全一致，也不会在多人里出现重影。
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
            halo.radius = radius;
            halo.spinRotation = startRot;
            halo.zRot = zRot;
            halo.exRot = exRot;
            halo.haloColor = color;
            halo.currentSpeed = MathHelper.Lerp(MinRotateSpeed, MaxRotateSpeed, MathHelper.Clamp(chargeCompletionAtSpawn, 0f, 1f));
            halo.lifeSpanTicks = (Main.rand.NextFloat(3.6f, 5.2f) - startRot) / halo.currentSpeed;
            halo.BuildInitialTrail();
            halo.SnapToAnchor();
        }

        private void BuildInitialTrail()
        {
            relativeOffsets = new Vector2[TrailPointCount];
            float r = spinRotation - TrailPointCount * currentSpeed;

            for (int i = 0; i < TrailPointCount; i++)
            {
                float length = TiltedCircleProjection(r, zRot, out float overrideAngle) * radius;
                relativeOffsets[i] = (overrideAngle + exRot).ToRotationVector2() * length;
                r += currentSpeed;
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

            // 一旦观测到蓄满或右键停止蓄力，就永久进入“减速+淡出”阶段，不会再因为读数波动而反复横跳。
            if (!enteredReadyPhase && (holdout is null || !holdout.GetHaloRightChargeActive() || holdout.GetHaloChargeReady()))
                enteredReadyPhase = true;

            float chargeCompletion = holdout?.GetChargeCompletion() ?? 0f;
            float targetSpeed = enteredReadyPhase
                ? 0f
                : MathHelper.Lerp(MinRotateSpeed, MaxRotateSpeed, MathHelper.Clamp(chargeCompletion, 0f, 1f));
            float followRate = enteredReadyPhase ? ReadyDecelFollowRate : ChargingFollowRate;

            currentSpeed = MathHelper.Lerp(currentSpeed, targetSpeed, followRate);
            spinRotation += currentSpeed;

            float length = TiltedCircleProjection(spinRotation, zRot, out float overrideAngle) * radius;

            for (int i = 0; i < relativeOffsets.Length - 1; i++)
                relativeOffsets[i] = relativeOffsets[i + 1];
            relativeOffsets[^1] = (overrideAngle + exRot).ToRotationVector2() * length;

            if (enteredReadyPhase)
            {
                postReadyFade *= ReadyFadeDecay;
                trailAlpha = postReadyFade;
                if (postReadyFade < 0.03f)
                {
                    Projectile.Kill();
                    return;
                }
            }
            else
            {
                if (time < (int)(lifeSpanTicks * 0.4f))
                    trailAlpha = time / (lifeSpanTicks * 0.4f);
                else
                    trailAlpha = 1f;

                if (time > (int)(lifeSpanTicks * 0.9f))
                {
                    trailAlpha *= 0.9f;
                    if (trailAlpha < 0.02f)
                    {
                        Projectile.Kill();
                        return;
                    }
                }
            }

            SnapToAnchor();
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

        private float HaloWidthFunction(float completion, Vector2 _) => StreakWidth(completion, TrailHalfWidth);

        private static float StreakWidth(float completion, float maxBodyWidth)
        {
            const float curveRatio = 0.15f;

            if (completion < curveRatio)
                return MathF.Sin(completion / curveRatio * MathHelper.PiOver2) * maxBodyWidth + curveRatio;

            return Utils.Remap(completion, curveRatio, 1f, maxBodyWidth, 0f);
        }

        private Color HaloColorFunction(float completion, Vector2 _) => StreakColor(completion, haloColor);

        private Color StreakColor(float completion, Color baseColor)
        {
            Color tipColor = Color.Lerp(baseColor, Color.Transparent, Utils.GetLerpValue(0.8f, 1f, completion, true));
            Color color = Color.Lerp(baseColor, tipColor, completion);

            // 复刻旧版折线「越靠尾部越暗」的线性衰减，保持原本的明暗层次。
            return color * (trailAlpha * MathHelper.Lerp(1f, 0.1f, completion));
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            if (!WeaponValid || relativeOffsets is null || trailAlpha <= 0.01f)
                return;

            Vector2[] trailPoints = BuildRenderPoints();
            if (trailPoints.Length < 4)
                return;

            // 传统着色拖尾：参考 EndlessDevourJavOrbSmall 的 TrailStreak + SylvestaffStreak 单层描边流，
            // 配色仍用我们自己的 haloColor，宽度也保持本项目的纤细口径（不照搬参考里明显偏粗的宽度）。
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
        // 再直接丢掉Z轴做正交投影，圆就被压扁成椭圆弧——这就是整个效果的投影核心。
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
