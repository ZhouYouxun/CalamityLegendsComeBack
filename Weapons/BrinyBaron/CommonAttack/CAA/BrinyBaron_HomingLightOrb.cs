using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal class BrinyBaron_HomingLightOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const float HomingRange = 920f;
        private const float MaxSpeed = 16.5f;

        // 出生后的短暂自由飞行时间。因为 extraUpdates = 1，这里约等于 9 个正常帧。
        private const int HomingDelay = 18;

        // 惯性越大，转向越慢，越不会变成“贴脸导弹”。
        private const float HomingInertia = 27f;

        // 没开始追踪 / 找不到目标时的轻微减速，让它有一点漂浮感。
        private const float FreeFlightDamping = 0.996f;
        private const float NoTargetDamping = 0.992f;

        // 很小的游移角度，负责制造“好像在追，但又不是特别主动”的神秘弧线。
        private const float WanderingTurnStrength = 0.006f;

        private int timer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 95;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = BB_Balance.GetLeftProjectileHitCooldown(BBLeftProjectile.HomingLightOrb);
        }

        public override void AI()
        {
            timer++;
            Projectile.rotation += Projectile.velocity.X * 0.012f + 0.04f;

            // 依旧保留海蓝色照明
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.34f, 0.55f));

            HomeTowardTarget();
            SpawnLightTrail();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Burst();
        }

        public override void OnKill(int timeLeft)
        {
            Burst();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color theme = Color.Lerp(new Color(90, 205, 255), Color.White, 0.2f);
            float pulse = 1f + (float)Math.Sin(timer * 0.24f + Projectile.identity * 0.4f) * 0.08f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            // 残影拖尾：发光球 + 细星芒
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = i / (float)Projectile.oldPos.Length;
                Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;

                Color trailColor = Color.Lerp(theme, Color.Transparent, completion) * (0.46f * (1f - completion));
                float bloomScale = Projectile.scale * MathHelper.Lerp(0.08f, 0.22f, 1f - completion);
                float starScale = Projectile.scale * MathHelper.Lerp(0.12f, 0.26f, 1f - completion);

                Main.EntitySpriteDraw(
                    bloom,
                    trailPosition,
                    null,
                    trailColor * 0.34f,
                    Projectile.rotation,
                    bloom.Size() * 0.5f,
                    bloomScale,
                    SpriteEffects.None);

                Main.EntitySpriteDraw(
                    star,
                    trailPosition,
                    null,
                    trailColor * 0.22f,
                    Projectile.rotation + completion * 0.7f,
                    star.Size() * 0.5f,
                    new Vector2(starScale * 0.9f, starScale * 0.35f),
                    SpriteEffects.None);
            }

            // 主体：外层蓝光
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                new Color(60, 185, 255, 0) * 0.42f,
                0f,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.24f * pulse,
                SpriteEffects.None);

            // 主体：内层白芯
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                Color.White with { A = 0 } * 0.24f,
                0f,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.11f,
                SpriteEffects.None);

            // 主体：星芒核心，让它更像“流光星屑”而不是泡泡
            Main.EntitySpriteDraw(
                star,
                drawPosition,
                null,
                new Color(145, 235, 255, 0) * 0.55f,
                Projectile.rotation,
                star.Size() * 0.5f,
                new Vector2(Projectile.scale * 0.32f, Projectile.scale * 0.12f) * pulse,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                star,
                drawPosition,
                null,
                Color.White with { A = 0 } * 0.28f,
                Projectile.rotation + MathHelper.PiOver2,
                star.Size() * 0.5f,
                new Vector2(Projectile.scale * 0.18f, Projectile.scale * 0.06f),
                SpriteEffects.None);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private void HomeTowardTarget()
        {
            // 前几帧完全不追踪，让它先按照出生速度飘出去。
            // 这一步非常关键：否则它会像导弹一样立刻贴向敌人，神秘感就没了。
            if (timer <= HomingDelay)
            {
                FreeDrift();
                return;
            }

            NPC target = FindNearestTarget(HomingRange);
            if (target == null)
            {
                FreeDrift(NoTargetDamping);
                return;
            }

            Vector2 currentVelocity = Projectile.velocity;
            float currentSpeed = currentVelocity.Length();

            // 防止极端情况下速度太小，导致方向计算不稳定。
            if (currentSpeed < 0.1f)
                currentVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 4f;

            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(currentVelocity.SafeNormalize(Vector2.UnitX));

            // 刚过启动延迟时只轻轻拉一下，随后才逐渐变得更愿意追踪。
            float warmup = Utils.GetLerpValue(HomingDelay, HomingDelay + 36f, timer, true);

            // 离敌人很近时稍微积极一点，避免它在敌人旁边绕半天不命中。
            float closePressure = Utils.GetLerpValue(360f, 70f, Projectile.Distance(target.Center), true);
            float pullStrength = MathHelper.Lerp(0.35f, 1f, MathHelper.Max(warmup, closePressure * 0.75f));

            float targetSpeed = MathHelper.Lerp(10.5f, MaxSpeed, pullStrength);
            Vector2 desiredVelocity = desiredDirection * targetSpeed;

            // “半追踪感”的核心：不直接锁到 desiredVelocity，而是用高惯性慢慢拽过去。
            Projectile.velocity = (currentVelocity * HomingInertia + desiredVelocity) / (HomingInertia + 1f);

            // 保留一点点游移，不让轨迹变得过于机械。
            float sideSway = (float)Math.Sin((timer + Projectile.identity * 7f) * 0.075f) *
                MathHelper.Lerp(0.012f, 0.004f, pullStrength);
            Projectile.velocity = Projectile.velocity.RotatedBy(sideSway);

            // 限速，避免多次惯性叠加后速度过高。
            if (Projectile.velocity.Length() > MaxSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(desiredDirection) * MaxSpeed;
        }

        private void FreeDrift(float damping = FreeFlightDamping)
        {
            // 自由飞行期也不是绝对直线，而是有一点非常轻的偏航。
            float wander = (float)Math.Sin((timer + Projectile.identity * 5f) * 0.08f) * WanderingTurnStrength;
            Projectile.velocity = Projectile.velocity.RotatedBy(wander) * damping;
        }

        private void SpawnLightTrail()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            Color themeA = new Color(80, 185, 255);
            Color themeB = new Color(210, 248, 255);

            // 主拖尾：改成小型流光球，而不是泡泡
            if (timer % 2 == 0)
            {
                Vector2 orbPos =
                    Projectile.Center
                    - forward * Main.rand.NextFloat(3f, 8f)
                    + right * Main.rand.NextFloat(-4f, 4f);

                Vector2 orbVelocity =
                    -forward * Main.rand.NextFloat(0.35f, 1.2f)
                    + right * Main.rand.NextFloat(-0.18f, 0.18f)
                    + Main.rand.NextVector2Circular(0.08f, 0.08f);

                GlowOrbParticle orb = new GlowOrbParticle(
                    orbPos,
                    orbVelocity,
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.34f, 0.52f) * Projectile.scale,
                    Color.Lerp(themeA, themeB, Main.rand.NextFloat(0.15f, 0.7f)),
                    true,
                    false,
                    true);

                GeneralParticleHandler.SpawnParticle(orb);
            }

            // 辅助尾迹：少量线性流光，强调“追踪流星”的味道
            if (timer % 3 == 0)
            {
                Vector2 linePos =
                    Projectile.Center
                    - forward * Main.rand.NextFloat(2f, 7f)
                    + right * Main.rand.NextFloat(-3f, 3f);

                Vector2 lineVelocity =
                    -forward * Main.rand.NextFloat(0.8f, 1.8f)
                    + right * Main.rand.NextFloat(-0.25f, 0.25f);

                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    linePos,
                    lineVelocity,
                    false,
                    Main.rand.Next(7, 12),
                    Main.rand.NextFloat(0.12f, 0.2f),
                    Color.Lerp(themeA, themeB, Main.rand.NextFloat(0.1f, 0.5f))));
            }

            // 保留水/霜 Dust，当作海洋味道的底层填充
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextBool(3) ? DustID.Frost : DustID.Water,
                    -Projectile.velocity * Main.rand.NextFloat(0.03f, 0.08f),
                    0,
                    Color.Lerp(themeA, themeB, Main.rand.NextFloat(0.06f, 0.32f)),
                    Main.rand.NextFloat(0.55f, 0.86f));
                dust.noGravity = true;
            }
        }

        private void Burst()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (forward == Vector2.Zero)
                forward = Vector2.UnitX;

            Color theme = new Color(90, 205, 255);

            // 先来一个方向脉冲环
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                theme * 0.72f,
                Vector2.One,
                Projectile.rotation,
                0.02f,
                0.26f,
                18));

            // 爆开时改成发光流星碎屑
            for (int i = 0; i < 5; i++)
            {
                Vector2 burstVelocity =
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.8f, 2.4f);

                GlowOrbParticle orb = new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    burstVelocity,
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.42f, 0.66f),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.15f, 0.55f)),
                    true,
                    false,
                    true);

                GeneralParticleHandler.SpawnParticle(orb);
            }

            // 再补几条线状碎光，视觉上更锐利
            for (int i = 0; i < 6; i++)
            {
                Vector2 lineVelocity =
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.3f, 3.6f);

                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                    lineVelocity,
                    false,
                    Main.rand.Next(8, 12),
                    Main.rand.NextFloat(0.14f, 0.22f),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.08f, 0.4f))));
            }

            // Dust 依旧保留，维持水蓝冲击感
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(3) ? DustID.Frost : DustID.Water,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 4.6f),
                    100,
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.1f, 0.6f)),
                    Main.rand.NextFloat(0.62f, 1.04f));
                dust.noGravity = true;
            }
        }

        private NPC FindNearestTarget(float maxDistance)
        {
            NPC closestTarget = null;
            float closestDistance = maxDistance;

            foreach (NPC npc in Main.npc)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestTarget = npc;
            }

            return closestTarget;
        }
    }
}
