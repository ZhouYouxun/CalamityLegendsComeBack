using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    // 蓄力光环：每个实例是一个绕X轴斜过一个角度、再丢掉Z轴投影回屏幕的圆环弧段（3D→2D的“斜圆投影”），
    // 只活很短一段时间，自己转、自己缓慢扩张、自己淡入淡出，靠不断新生成来维持持续的观感。
    // 转速不是固定值：蓄力过程中读取武器当前的蓄力进度让转速持续加快，
    // 一旦蓄满（ChargeReady），转速改为非线性地衰减回0，同时整体透明度慢慢淡出直至消失；
    // 锚点每帧都重新读取武器当前位置和朝向，历史轨迹只存“相对武器锚点的偏移量”（相对运动而非绝对运动）。
    internal sealed class BFRightChargeHaloParticle : Particle
    {
        public override string Texture => "CalamityMod/Particles/ThinEndedLine";
        public override bool UseAdditiveBlend => true;
        public override bool UseCustomDraw => true;

        private const int TrailPointCount = 10;
        private const float MinRotateSpeed = 0.08f;
        private const float MaxRotateSpeed = 0.5f;
        private const float ChargingFollowRate = 0.15f; // 蓄力中：转速比较灵敏地跟随蓄力进度
        private const float ReadyDecelFollowRate = 0.05f; // 蓄满后：转速缓慢、非线性地衰减到0
        private const float ReadyFadeDecay = 0.94f; // 蓄满后：透明度按指数衰减，慢慢消失
        private const float AnchorForwardOffset = 16f;

        private Projectile weapon;
        private NewLegendBlossomFluxHoldOut holdout;
        private float zRot;
        private float exRot;
        private float radius;
        private float lifeSpanTicks;
        private float trailAlpha;
        private float currentSpeed;
        private float postReadyFade = 1f;
        private bool enteredReadyPhase;
        private Vector2[] relativeOffsets;

        public static void Spawn(Projectile weaponProjectile, float radius, float startRot, float zRot, float exRot, Color color, float chargeCompletionAtSpawn)
        {
            if (Main.dedServ || weaponProjectile is null)
                return;

            BFRightChargeHaloParticle particle = new()
            {
                weapon = weaponProjectile,
                holdout = weaponProjectile.ModProjectile as NewLegendBlossomFluxHoldOut,
                radius = radius,
                Rotation = startRot,
                zRot = zRot,
                exRot = exRot,
                Color = color,
                Velocity = Vector2.Zero,
            };

            particle.currentSpeed = MathHelper.Lerp(MinRotateSpeed, MaxRotateSpeed, MathHelper.Clamp(chargeCompletionAtSpawn, 0f, 1f));
            particle.lifeSpanTicks = (Main.rand.NextFloat(3.6f, 5.2f) - startRot) / particle.currentSpeed;
            particle.BuildInitialTrail();

            GeneralParticleHandler.SpawnParticle(particle);
        }

        private void BuildInitialTrail()
        {
            relativeOffsets = new Vector2[TrailPointCount];
            float r = Rotation - TrailPointCount * currentSpeed;

            for (int i = 0; i < TrailPointCount; i++)
            {
                float length = TiltedCircleProjection(r, zRot, out float overrideAngle) * radius;
                relativeOffsets[i] = (overrideAngle + exRot).ToRotationVector2() * length;
                r += currentSpeed;
            }
        }

        public override void Update()
        {
            if (weapon is null || !weapon.active)
            {
                Kill();
                return;
            }

            // 一旦观测到蓄满，就永久进入“减速+淡出”阶段，不会再因为读数波动而反复横跳。
            if (!enteredReadyPhase && (holdout?.GetHaloChargeReady() ?? false))
                enteredReadyPhase = true;

            float chargeCompletion = holdout?.GetChargeCompletion() ?? 0f;
            float targetSpeed = enteredReadyPhase
                ? 0f
                : MathHelper.Lerp(MinRotateSpeed, MaxRotateSpeed, MathHelper.Clamp(chargeCompletion, 0f, 1f));
            float followRate = enteredReadyPhase ? ReadyDecelFollowRate : ChargingFollowRate;

            currentSpeed = MathHelper.Lerp(currentSpeed, targetSpeed, followRate);
            Rotation += currentSpeed;

            float length = TiltedCircleProjection(Rotation, zRot, out float overrideAngle) * radius;

            for (int i = 0; i < relativeOffsets.Length - 1; i++)
                relativeOffsets[i] = relativeOffsets[i + 1];
            relativeOffsets[^1] = (overrideAngle + exRot).ToRotationVector2() * length;

            if (enteredReadyPhase)
            {
                postReadyFade *= ReadyFadeDecay;
                trailAlpha = postReadyFade;
                if (postReadyFade < 0.03f)
                    Kill();
            }
            else
            {
                if (Time < (int)(lifeSpanTicks * 0.4f))
                    trailAlpha = Time / (lifeSpanTicks * 0.4f);
                else
                    trailAlpha = 1f;

                if (Time > (int)(lifeSpanTicks * 0.9f))
                {
                    trailAlpha *= 0.9f;
                    if (trailAlpha < 0.02f)
                        Kill();
                }
            }
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            if (weapon is null || relativeOffsets is null)
                return;

            Vector2 anchor = weapon.Center + weapon.rotation.ToRotationVector2() * AnchorForwardOffset;
            int count = relativeOffsets.Length;

            for (int i = 0; i < count - 1; i++)
            {
                float segmentFactor = (i + 1f) / count;
                Color segmentColor = Color * (trailAlpha * segmentFactor);
                float width = MathHelper.Lerp(1.4f, 5.5f, segmentFactor);

                spriteBatch.DrawLineBetter(anchor + relativeOffsets[i], anchor + relativeOffsets[i + 1], segmentColor, width);
            }
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
