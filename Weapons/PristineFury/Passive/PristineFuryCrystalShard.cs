using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.Passive
{
    // 行为分层：全方向弹出 → 各自随机减速漂移 → 减速结束后惰性追踪预热 → 高惯性弧线收束 + 接近加压
    internal sealed class PristineFuryCrystalShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Boss/ProvidenceCrystalShard";

        private const float MaxHomeSpeed  = 18f;
        private const float HomingInertia = 22f;
        private const float WarmupFrames  = 36f;

        private ref float Hue         => ref Projectile.ai[0];
        private ref float TargetIdxAI => ref Projectile.ai[1];
        private ref float Timer       => ref Projectile.localAI[0];
        private ref float DecelEndAI  => ref Projectile.localAI[1];

        // 从 identity 导出，无需额外存储；每枚碎片值不同且稳定
        private float DampingFactor => 0.978f + (Projectile.identity % 15) * 0.001f; // 0.978~0.992
        private float DecelEndFrame => DecelEndAI > 0f ? DecelEndAI : 30f;

        private int  TargetIdx    => (int)TargetIdxAI - 1;
        private bool InDecelPhase => Timer < DecelEndFrame;
        private Color ShardColor  => Main.hslToRgb(Hue, 1f, 0.58f);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type]     = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width                  = 22;
            Projectile.height                 = 22;
            Projectile.friendly               = true;
            Projectile.DamageType             = DamageClass.Ranged;
            Projectile.penetrate              = 1;
            Projectile.tileCollide            = false;
            Projectile.ignoreWater            = true;
            Projectile.timeLeft               = 380;
            Projectile.alpha                  = 255;
            Projectile.usesLocalNPCImmunity   = true;
            Projectile.localNPCHitCooldown    = 8;
        }

        public override void AI()
        {
            // 首帧初始化随机减速时长（22~42 帧）
            if (Timer == 0f)
                DecelEndAI = Main.rand.Next(22, 43);

            Timer++;

            // 淡入：减速阶段慢淡入，体现"刚弹出还在飘"
            Projectile.alpha = Math.Max(0, Projectile.alpha - (InDecelPhase ? 16 : 38));

            if (InDecelPhase)
                DoDecelPhase();
            else
                DoHomingPhase();

            if (Projectile.velocity.LengthSquared() > 0.01f)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (!Main.dedServ && Timer % 2 == 0)
                SpawnTrailParticle();

            Lighting.AddLight(Projectile.Center, ShardColor.ToVector3() * 0.32f);
        }

        // ─── 减速阶段：随机阻尼 + 轻微漂移游走 ──────────────────────────────
        private void DoDecelPhase()
        {
            Projectile.velocity *= DampingFactor;

            float wander = (float)Math.Sin((Timer + Projectile.identity * 5f) * 0.1f) * 0.007f;
            Projectile.velocity = Projectile.velocity.RotatedBy(wander);
        }

        // ─── 追踪阶段：预热 → 高惯性弧线 → 接近加压 → 侧向游移递减 ────────
        private void DoHomingPhase()
        {
            NPC target = FindTarget();
            if (target == null)
            {
                Projectile.velocity *= 0.994f;
                return;
            }

            float phase        = Timer - DecelEndFrame;
            float warmup       = Utils.GetLerpValue(0f, WarmupFrames, phase, true);
            float closePressure = Utils.GetLerpValue(420f, 70f, Projectile.Distance(target.Center), true);
            float pullStrength = MathHelper.Lerp(0.28f, 1f, MathHelper.Max(warmup, closePressure * 0.72f));

            float targetSpeed = MathHelper.Lerp(5f, MaxHomeSpeed, pullStrength);
            Vector2 toTarget  = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity);

            // 速度极低时给一个小初推，防止方向漂移后追不上去
            if (Projectile.velocity.LengthSquared() < 0.04f)
                Projectile.velocity = toTarget * 2.5f;

            Projectile.velocity = (Projectile.velocity * HomingInertia + toTarget * targetSpeed)
                                  / (HomingInertia + 1f);

            // 侧向游移：随 pullStrength 增大而减小，全力追时几乎消失
            float sway = (float)Math.Sin((Timer + Projectile.identity * 7f) * 0.07f)
                * MathHelper.Lerp(0.007f, 0.0015f, pullStrength);
            Projectile.velocity = Projectile.velocity.RotatedBy(sway);

            if (Projectile.velocity.Length() > MaxHomeSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(toTarget) * MaxHomeSpeed;
        }

        private NPC FindTarget()
        {
            int idx = TargetIdx;
            if (idx >= 0 && idx < Main.maxNPCs)
            {
                NPC c = Main.npc[idx];
                if (c.active && c.CanBeChasedBy())
                    return c;
            }
            float bestDistSq = 1400f * 1400f;
            NPC best = null;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                float d = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                if (d < bestDistSq) { bestDistSq = d; best = npc; }
            }
            return best;
        }

        private void SpawnTrailParticle()
        {
            Color c = ShardColor;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right   = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 pos     = Projectile.Center
                              - forward * Main.rand.NextFloat(2f, 6f)
                              + right   * Main.rand.NextFloat(-3f, 3f);
            Vector2 vel     = -forward * Main.rand.NextFloat(0.3f, 0.9f)
                              + right   * Main.rand.NextFloat(-0.15f, 0.15f);

            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                pos, vel, false, Main.rand.Next(8, 14),
                Main.rand.NextFloat(0.18f, 0.32f) * Projectile.scale,
                c with { A = 0 }, false, false, false));

            if (Timer % 3 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    pos, vel * 1.4f, false, Main.rand.Next(6, 10),
                    Main.rand.NextFloat(0.1f, 0.18f),
                    Color.Lerp(c, Color.White, Main.rand.NextFloat(0.1f, 0.4f)) with { A = 0 }));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Burst();
        public override void OnKill(int timeLeft) => Burst();

        private void Burst()
        {
            if (Main.dedServ) return;
            Color c = ShardColor;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, Vector2.Zero, c with { A = 0 },
                Vector2.One, 0f, 0.06f, 0.22f, 14));

            for (int i = 0; i < 5; i++)
            {
                Vector2 bv = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.9f, 2.6f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(3f, 3f), bv, false,
                    Main.rand.Next(10, 17), Main.rand.NextFloat(0.28f, 0.5f),
                    Color.Lerp(c, Color.White, Main.rand.NextFloat(0.15f, 0.45f)) with { A = 0 },
                    false, false, false));
            }

            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.4f, 3.2f),
                    false, Main.rand.Next(7, 11), Main.rand.NextFloat(0.12f, 0.2f),
                    c with { A = 0 }));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex   = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star  = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;

            float opacity    = 1f - Projectile.alpha / 255f;
            Color shardColor = ShardColor;
            Vector2 drawPos  = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None,
                Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // 残影拖尾：从自身色渐变到深紫，每帧 bloom 圆 + 星芒条
            for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                float t = (float)i / Projectile.oldPos.Length;
                Color tc = Color.Lerp(shardColor with { A = 0 }, new Color(80, 10, 140) with { A = 0 }, t)
                    * ((1f - t) * 0.55f * opacity);

                Vector2 tp         = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float   bloomScale = Projectile.scale * MathHelper.Lerp(0.08f, 0.22f, 1f - t);
                float   starScale  = Projectile.scale * MathHelper.Lerp(0.04f, 0.14f, 1f - t);

                Main.EntitySpriteDraw(bloom, tp, null, tc * 0.42f,
                    Projectile.oldRot[i], bloom.Size() * 0.5f, bloomScale, SpriteEffects.None);
                Main.EntitySpriteDraw(star, tp, null, tc * 0.28f,
                    Projectile.oldRot[i] + t * 0.6f, star.Size() * 0.5f,
                    new Vector2(starScale * 0.9f, starScale * 0.32f), SpriteEffects.None);
            }

            // 主体外层光晕
            Main.EntitySpriteDraw(bloom, drawPos, null,
                shardColor with { A = 0 } * (0.4f * opacity),
                0f, bloom.Size() * 0.5f, Projectile.scale * 0.22f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPos, null,
                Color.White with { A = 0 } * (0.18f * opacity),
                0f, bloom.Size() * 0.5f, Projectile.scale * 0.1f, SpriteEffects.None);

            // 星芒十字
            Main.EntitySpriteDraw(star, drawPos, null,
                shardColor with { A = 0 } * (0.52f * opacity),
                Projectile.rotation, star.Size() * 0.5f,
                new Vector2(Projectile.scale * 0.28f, Projectile.scale * 0.1f), SpriteEffects.None);
            Main.EntitySpriteDraw(star, drawPos, null,
                Color.White with { A = 0 } * (0.28f * opacity),
                Projectile.rotation + MathHelper.PiOver2, star.Size() * 0.5f,
                new Vector2(Projectile.scale * 0.16f, Projectile.scale * 0.055f), SpriteEffects.None);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None,
                Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // 本体水晶碎片，用色相染色
            Color bodyColor = new Color(
                (int)(shardColor.R * opacity),
                (int)(shardColor.G * opacity),
                (int)(shardColor.B * opacity),
                (int)(200 * opacity));
            Main.EntitySpriteDraw(tex, drawPos, tex.Frame(), bodyColor, Projectile.rotation,
                tex.Frame().Center(), Projectile.scale, SpriteEffects.None);

            return false;
        }
    }
}
