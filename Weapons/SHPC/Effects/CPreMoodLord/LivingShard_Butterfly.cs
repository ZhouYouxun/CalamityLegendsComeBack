using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    // 萤火魂蝶：生命碎片光球引爆后炸出的追踪弹幕。命中直接给玩家回血一次后消失，不再生成额外的追踪弹幕。
    public class LivingShard_Butterfly : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/Summon/PinkButterfly";

        private const int DecelPhaseLength = 18;
        private const int DamageActivationDelay = 15;
        private const int HealAmount = 10;

        // 与生命碎片光球炸出时的初速一致（旧版"生命气息"的继承速度：SHPLB shootSpeed 20f * 1.3）
        private const float TopSpeed = 20f * 1.3f;
        // 追踪阶段持续加速，最终速度封顶在TopSpeed的2.5倍，防止追不上快速敌人
        private const float MaxTrackSpeed = TopSpeed * 2.5f;
        private const float TrackAccelDuration = 240f;

        private static readonly Color OutlineColor = new(110, 255, 140);

        private int timer;
        private int trackTimer;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1; // 只造成一次伤害，命中后自动消失
            Projectile.timeLeft = 360;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => timer >= DamageActivationDelay ? null : false;

        public override void AI()
        {
            timer++;

            if (timer <= DecelPhaseLength)
            {
                // 逐渐减速：先像被甩出去的萤火虫一样飘一下再收速度
                Projectile.velocity *= 0.93f;
            }
            else
            {
                trackTimer++;

                NPC target = Projectile.Center.ClosestNPCAt(1000f);
                if (target != null)
                {
                    Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
                    // 持续加速的追踪：速度和转向强度随trackTimer慢慢爬升，最终封顶在MaxTrackSpeed，避免越追越丢
                    float accelInterpolant = Utils.GetLerpValue(0f, TrackAccelDuration, trackTimer, true);
                    float desiredSpeed = MathHelper.Lerp(2.2f, MaxTrackSpeed, accelInterpolant);
                    float blendRate = MathHelper.Lerp(1f / 34f, 1f / 12f, accelInterpolant);

                    Vector2 desiredVelocity = desiredDirection * desiredSpeed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, blendRate);
                }
            }

            // ===== 动画帧 =====
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            if (Math.Abs(Projectile.velocity.X) > 0.2f)
                Projectile.spriteDirection = Projectile.velocity.X > 0f ? 1 : -1;

            Projectile.rotation = Projectile.velocity.X * 0.045f;

            Lighting.AddLight(Projectile.Center, OutlineColor.ToVector3() * 0.62f);
            SpawnFireflyTrail();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player owner = Main.player[Projectile.owner];
            owner.statLife = Math.Min(owner.statLifeMax2, owner.statLife + HealAmount);
            owner.HealEffect(HealAmount);
            SoundEngine.PlaySound(SoundID.NPCDeath58 with { Volume = 0.5f, Pitch = 0.2f }, owner.Center);
        }

        public override void OnKill(int timeLeft)
        {
            SpawnButterflyDeathBurst(Projectile.Center, Projectile.velocity);
        }

        private void SpawnFireflyTrail()
        {
            if (Main.rand.NextBool(3))
            {
                SquishyLightParticle particle = new(
                    Projectile.Center,
                    -Projectile.velocity * 0.15f,
                    Main.rand.NextFloat(0.22f, 0.36f),
                    Color.Lerp(OutlineColor, Color.White, Main.rand.NextFloat(0.1f, 0.4f)),
                    Main.rand.Next(14, 22));

                GeneralParticleHandler.SpawnParticle(particle);
            }

            if (Main.rand.NextBool(5))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GreenTorch,
                    -Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    100,
                    new Color(120, 255, 150),
                    0.7f);
                dust.noGravity = true;
                dust.fadeIn = 0.4f;
            }
        }

        // 爆炸的蝴蝶形扩散特效：纯用Dust沿数学上的“蝴蝶曲线”(Temple Fay's Butterfly Curve)取点，
        // r(θ) = e^sinθ − 2cos4θ + sin^5((2θ−π)/24)，θ∈[0,12π)，每个点再沿自身方向继续往外飞，
        // 形成一次性绽放开的蝴蝶形尘爆。
        private static void SpawnButterflyDeathBurst(Vector2 center, Vector2 velocity)
        {
            const int steps = 96;
            const float thetaMax = 12f * MathHelper.Pi;
            const float scale = 15f;

            float baseRotation = velocity.LengthSquared() > 0.01f
                ? velocity.ToRotation() - MathHelper.PiOver2
                : Main.rand.NextFloat(MathHelper.TwoPi);

            for (int i = 0; i < steps; i++)
            {
                float theta = thetaMax * i / steps;
                float r = MathF.Exp(MathF.Sin(theta)) - 2f * MathF.Cos(4f * theta)
                    + MathF.Pow(MathF.Sin((2f * theta - MathHelper.Pi) / 24f), 5f);

                float angle = theta + baseRotation;
                Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * r * scale;

                Vector2 outward = offset.SafeNormalize(Vector2.UnitX);
                Vector2 dustVelocity = outward * Main.rand.NextFloat(1.4f, 2.4f);

                Color color = Color.Lerp(new Color(255, 170, 210), new Color(140, 255, 170), i / (float)steps);

                Dust dust = Dust.NewDustPerfect(
                    center + offset,
                    Main.rand.NextBool() ? DustID.GreenTorch : DustID.TintableDustLighted,
                    dustVelocity,
                    100,
                    color,
                    Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
                dust.fadeIn = 0.3f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            Rectangle frame = new(0, frameHeight * Projectile.frame, texture.Width, frameHeight);
            Vector2 origin = new(texture.Width * 0.5f, frameHeight * 0.5f);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);

            // 不染色，只给贴图加一层绿色包边——原贴图保持它本身的粉色
            float pulse = 0.7f + 0.3f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f + Projectile.identity);
            Color outline = OutlineColor with { A = 0 } * (0.55f * pulse);
            float outlineDistance = 1.6f + pulse * 1.2f;

            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * outlineDistance;
                Main.EntitySpriteDraw(texture, drawPosition + offset, frame, outline, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0f);
            }

            Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, spriteEffects, 0f);

            return false;
        }
    }
}
