using System;
using CalamityLegendsComeBack.Weapons.AegisBlade.Visuals;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    /// <summary>
    /// 追踪圣火。炉心炸开后四散、减速，随后锁定最近目标俯冲。
    /// 视觉参考 Providence 的 HolyFlare：火焰本体随速度拉伸旋转，
    /// 尾部同时挂 GlowOrb 亮光与 MediumMist 深灰圣灰。
    /// </summary>
    public class AegisFireball : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const float ScatterVelocityRetention = 0.86f;
        private const int ScatterPhaseUpdates = 28;
        private const float HomingSpeed = 25f;
        private const float HomingStrength = 0.18f;
        private const float HomingRange = 95f * 16f;
        private const int   MaxLifetime      = 240;
        private const float BodyRadius       = 13f;

        private ref float Timer => ref Projectile.ai[0];

        /// <summary>0 = 刚散开还在减速，1 = 已进入追踪俯冲。用于把火焰拉长。</summary>
        private float DiveFactor => Utils.GetLerpValue(ScatterPhaseUpdates, ScatterPhaseUpdates + 26f, Timer, true);

        private float LifeFade => Utils.GetLerpValue(0f, 26f, Projectile.timeLeft, true) *
                                  Utils.GetLerpValue(0f, 10f, Timer, true);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type]     = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width  = Projectile.height = 22;
            Projectile.friendly    = true;
            Projectile.DamageType  = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate   = 3;
            Projectile.timeLeft    = MaxLifetime;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity  = true;
            Projectile.localNPCHitCooldown   = 20;
        }

        public override void AI()
        {
            Timer++;
            if (Timer < ScatterPhaseUpdates)
                Projectile.velocity *= ScatterVelocityRetention;
            else
                HomeInOnClosestTarget();

            // HolyFlare 的做法：速度越快，本体越对齐飞行方向；几乎静止时保持竖直
            float speedRatio = MathHelper.Clamp(Projectile.velocity.Length() / 12f, 0f, 1f);
            float targetRotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.rotation = MathHelper.Lerp(Projectile.rotation,
                MathHelper.WrapAngle(targetRotation), 0.2f + 0.5f * speedRatio);

            AegisVisuals.Light(Projectile.Center, 0.85f);
            EmitFlameTrail();
        }

        private void HomeInOnClosestTarget()
        {
            NPC target = FindClosestTarget();
            if (target is null)
            {
                Projectile.velocity *= 0.94f;
                return;
            }

            Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX)) * HomingSpeed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, HomingStrength);
        }

        private NPC FindClosestTarget()
        {
            NPC bestTarget = null;
            float bestDistanceSquared = HomingRange * HomingRange;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distanceSquared = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                if (distanceSquared >= bestDistanceSquared)
                    continue;

                bestDistanceSquared = distanceSquared;
                bestTarget = npc;
            }

            return bestTarget;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.48f, Pitch = 0.18f }, Projectile.Center);
            EmitFlameBurst(Projectile.Center, oldVelocity.SafeNormalize(Vector2.UnitY), 0.75f);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = 0.08f }, target.Center);
            EmitFlameBurst(target.Center, Projectile.velocity.SafeNormalize(Vector2.UnitY), 0.9f);

            // 正义旗式灼烧标记：火光沿飞来方向被压进敌人身体
            AegisVisuals.WarbannerConverge(target.Center,
                Projectile.velocity.SafeNormalize(Vector2.UnitY), 1.5f, 3,
                1f + target.Hitbox.Width / 420f);
        }

        public override void OnKill(int timeLeft)
        {
            // 撞墙与打穿次数用尽都已经各自放过爆闪了，只有自然烧完才补这一下轻量消散，
            // 否则一次命中会连放两次爆炸。
            if (Main.dedServ || timeLeft > 0)
                return;

            AegisVisuals.HolyDetonation(Projectile.Center, 0.55f, false);
        }

        private void EmitFlameTrail()
        {
            if (Main.dedServ)
                return;

            Vector2 backwards = -Projectile.velocity.SafeNormalize(Vector2.UnitX);

            if ((int)Timer % 4 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    backwards * Main.rand.NextFloat(0.25f, 1.1f),
                    false, Main.rand.Next(10, 17),
                    Main.rand.NextFloat(0.15f, 0.28f) * (0.85f + DiveFactor * 0.4f),
                    AegisVisuals.RandomFlameColor(), true, false, true));
            }

            // HolyFlare 的灰烬轨迹
            if (Main.rand.NextBool(4))
            {
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    Projectile.Center + backwards * Main.rand.NextFloat(4f, 14f),
                    backwards * Main.rand.NextFloat(0.3f, 1.2f),
                    Color.Lerp(AegisVisuals.Charred, Color.DarkSlateGray, Main.rand.NextFloat(0.25f, 0.8f)),
                    Color.Transparent, Main.rand.NextFloat(0.22f, 0.44f), Main.rand.Next(18, 32),
                    Main.rand.NextFloat(-0.05f, 0.05f)));
            }

            if (Main.rand.NextBool(5))
            {
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    AegisVisuals.ProfanedFireDust, backwards * Main.rand.NextFloat(0.6f, 2.4f),
                    0, Color.White, Main.rand.NextFloat(0.75f, 1.3f));
                ember.noGravity = true;
            }

            // 俯冲阶段追加切向火星，表现"加速扑过去"
            if (DiveFactor > 0.4f && (int)Timer % 3 == 0)
            {
                Vector2 side = backwards.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloatDirection();
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + backwards * 6f, backwards * 2.4f + side * 1.6f, false,
                    Main.rand.Next(6, 11), Main.rand.NextFloat(0.055f, 0.09f),
                    AegisVisuals.Add(AegisVisuals.Gold, 0.8f), new Vector2(2.1f, 0.45f), true, false, 1f));
            }
        }

        private void EmitFlameBurst(Vector2 position, Vector2 direction, float strength)
        {
            if (Main.dedServ)
                return;

            AegisVisuals.HolyDetonation(position, 0.95f * strength, true, direction.ToRotation());
            AegisVisuals.DirectionalImpact(position, direction, 0.72f * strength);
            AegisVisuals.EmberJet(position, direction, 5, 0.8f * strength, 0.8f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D fire = AegisVisuals.Tex(AegisVisuals.TexFireBody);
            Texture2D wisp = AegisVisuals.Tex(AegisVisuals.TexFlameWisp);
            Texture2D orb = AegisVisuals.Tex(AegisVisuals.TexOrbSoft);
            Texture2D star = AegisVisuals.Tex(AegisVisuals.TexStarThin);
            Texture2D bloom = AegisVisuals.Tex(AegisVisuals.TexBloom);

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float fade = LifeFade;
            if (fade <= 0.01f)
                return false;

            // 俯冲越快，火焰越被拉长（HolySpear 的 squish 思路）
            float stretch = MathHelper.Clamp(Projectile.velocity.Length() / 26f, 0f, 0.35f);
            Vector2 squish = new(1f - stretch, 1f + stretch * 1.5f);
            float flicker = 0.88f + 0.12f * MathF.Sin(Main.GlobalTimeWrappedHourly * 22f + Projectile.identity);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            // ① 残影拖尾：外层余烬火团
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                float completion = i / (float)Projectile.oldPos.Length;
                Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float trailRadius = BodyRadius * MathHelper.Lerp(0.95f, 0.22f, completion);

                Main.EntitySpriteDraw(fire, trailPosition, null,
                    AegisVisuals.Add(Color.Lerp(AegisVisuals.Flame, AegisVisuals.Ember, completion),
                        0.42f * (1f - completion) * fade),
                    Projectile.rotation + completion * 1.6f, fire.Size() * 0.5f,
                    new Vector2(AegisVisuals.RadiusScale(fire, trailRadius)) * squish, SpriteEffects.None, 0);
            }

            // ② 亵渎背光：本体正下方压一层暗红
            AegisVisuals.ProfanedBackglow(fire, drawPosition, null, Projectile.rotation, fire.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(fire, BodyRadius)) * squish, fade, 3.2f, 5);

            // ③ 火焰丝缕：反向缓转，让火在自己翻卷
            Main.EntitySpriteDraw(wisp, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Flame, 0.55f * fade * flicker),
                -Projectile.rotation * 0.8f + Main.GlobalTimeWrappedHourly * 2.4f, wisp.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(wisp, BodyRadius * 1.35f)) * squish, SpriteEffects.None, 0);

            // ④ 火焰主体
            Main.EntitySpriteDraw(fire, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Gold, 0.85f * fade * flicker),
                Projectile.rotation, fire.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(fire, BodyRadius)) * squish, SpriteEffects.None, 0);

            // ⑤ 白金内芯 + 星芒 + 外晕
            Main.EntitySpriteDraw(bloom, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Ember, 0.5f * fade),
                0f, bloom.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(bloom, BodyRadius * 1.75f)), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(orb, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Core, 0.72f * fade * flicker),
                0f, orb.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(orb, BodyRadius * 0.42f)), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(star, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Core, 0.34f * fade),
                Projectile.rotation * 0.5f + Main.GlobalTimeWrappedHourly * 1.8f, star.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(star, BodyRadius * 0.95f)), SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
