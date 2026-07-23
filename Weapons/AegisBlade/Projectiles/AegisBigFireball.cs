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
    /// 圣火炉心。左键轮盘向两侧甩出，减速悬停后炸开为四枚追踪圣火。
    /// 视觉参考 Providence 的 HolyBomb：果冻状挤压呼吸 + 周期性"打嗝" + 三重爆闪消亡。
    /// </summary>
    public class AegisBigFireball : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int MaxHoverTimer = 65;
        private const int HiccupInterval = 22;   // 每隔多少帧抽搐一次（HolyBomb 是 120，这里寿命短所以更密）
        private const float CoreRadius = 21f;

        private ref float Timer => ref Projectile.ai[0];
        private ref float HasErupted => ref Projectile.ai[1];

        /// <summary>0 → 1 的挤压动画进度，每次"打嗝"重置为 0。</summary>
        private float squishAnimation = 1f;

        /// <summary>爆裂前的收紧预警：最后 18 帧核心急剧收缩变亮。</summary>
        private float ImminentEruption => Utils.GetLerpValue(18f, 2f, Projectile.timeLeft, true);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type]     = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width  = Projectile.height = 36;
            Projectile.friendly    = true;
            Projectile.DamageType  = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate   = -1;
            Projectile.timeLeft    = MaxHoverTimer;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 15;
        }

        public override void AI()
        {
            Timer++;

            // 逐渐减速至空中悬停
            if (Projectile.velocity.Length() > 0.15f)
            {
                Projectile.velocity *= 0.90f;
            }
            else
            {
                Projectile.velocity = Vector2.Zero;
            }

            Projectile.rotation += 0.08f;
            AegisVisuals.Light(Projectile.Center, 1.15f);

            // ── HolyBomb 式挤压呼吸：平时缓慢回弹，每隔 HiccupInterval 帧猛地抽搐一次 ──
            squishAnimation = MathHelper.Clamp(squishAnimation + 0.06f, 0f, 1f);
            if (Timer > 6f && Timer % HiccupInterval == 0f)
            {
                squishAnimation = 0f;
                SpawnHiccup();
            }

            EmitCoreFlames();
        }

        /// <summary>周期性抽搐：炉心一颤，火屑从顶部炸出，配合挤压动画重置。</summary>
        private void SpawnHiccup()
        {
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.4f, Pitch = 0.45f }, Projectile.Center);
            if (Main.dedServ)
                return;

            for (int i = 0; i < 8; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + new Vector2(0f, -10f),
                    new Vector2(Main.rand.NextFloat(-4.5f, 4.5f), Main.rand.NextFloat(-3.2f, 0.8f)),
                    false, Main.rand.Next(18, 28), Main.rand.NextFloat(0.22f, 0.4f),
                    AegisVisuals.RandomFlameColor(), true, false, true));
            }

            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero,
                AegisVisuals.Add(AegisVisuals.Gold, 0.55f), AegisVisuals.TexBloom, Vector2.One,
                0f, 0.12f, 0.55f, 10));
        }

        private void EmitCoreFlames()
        {
            if (Main.dedServ)
                return;

            // 环绕炉心公转的火屑：不是随机撒，而是沿轨道被甩出来
            if ((int)Timer % 2 == 0)
            {
                float orbit = Timer * 0.35f + Projectile.identity;
                Vector2 orbitDirection = orbit.ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + orbitDirection * CoreRadius * 0.9f,
                    orbitDirection.RotatedBy(MathHelper.PiOver2) * 1.4f + orbitDirection * 0.5f,
                    false, Main.rand.Next(12, 20), Main.rand.NextFloat(0.16f, 0.3f),
                    AegisVisuals.RandomFlameColor(), true, false, true));
            }

            // 深灰圣灰：Providence 圣火的固定搭配，让炉心不是纯发光球
            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(16f, 16f),
                    Main.rand.NextVector2Circular(2.4f, 2.4f) - Vector2.UnitY * 0.6f,
                    Color.Lerp(AegisVisuals.Charred, Color.DarkSlateGray, Main.rand.NextFloat(0.3f, 0.9f)),
                    Color.Transparent, Main.rand.NextFloat(0.4f, 0.75f), Main.rand.Next(20, 34),
                    Main.rand.NextFloat(-0.04f, 0.04f)));
            }

            if (Main.rand.NextBool(3))
            {
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(18f, 18f),
                    AegisVisuals.ProfanedFireDust, Main.rand.NextVector2Circular(2f, 2f),
                    0, Color.White, Main.rand.NextFloat(1.1f, 1.9f));
                ember.noGravity = true;
            }

            // 爆裂预警：最后 18 帧火星被反向"吸"回炉心
            if (ImminentEruption > 0.05f && Main.rand.NextBool(2))
            {
                Vector2 inward = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center + inward * Main.rand.NextFloat(46f, 82f),
                    -inward * Main.rand.NextFloat(3f, 7f) * ImminentEruption, false,
                    Main.rand.Next(10, 18), Main.rand.NextFloat(0.5f, 1.1f),
                    AegisVisuals.Gradient(Main.rand.NextFloat(0f, 0.5f))));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
            AegisVisuals.DirectionalImpact(target.Center, direction, 0.9f);
            AegisVisuals.EmberJet(target.Center, direction, 5, 0.85f, 0.7f);
        }

        public override void OnKill(int timeLeft)
        {
            if (HasErupted > 0f) return;
            HasErupted = 1f;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.82f, Pitch = 0.1f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact with { Volume = 0.7f, Pitch = 0.35f }, Projectile.Center);

            if (!Main.dedServ)
            {
                // Providence 三重爆闪 + 火焰体积 + 圣灰 + 重火烟尘粒子
                AegisVisuals.HolyDetonation(Projectile.Center, 2.4f);
                AegisVisuals.CoronaRing(Projectile.Center, 16, 1.5f);

                for (int i = 0; i < 20; i++)
                {
                    Vector2 particleVel = Main.rand.NextVector2Circular(12f, 12f);
                    GeneralParticleHandler.SpawnParticle(new FlameParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(30f, 30f), 28,
                        Main.rand.NextFloat(0.45f, 0.7f), Main.rand.NextFloat(1.2f, 2.2f),
                        AegisVisuals.Core, AegisVisuals.Ember));

                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                        Projectile.Center, particleVel * 1.5f,
                        AegisVisuals.Charred, 35, Main.rand.NextFloat(0.6f, 1.2f), 0.8f,
                        Main.rand.NextFloat(-0.04f, 0.04f), true));

                    GeneralParticleHandler.SpawnParticle(new SparkParticle(
                        Projectile.Center, particleVel * 1.8f, false, 16,
                        Main.rand.NextFloat(1.2f, 2.4f), AegisVisuals.Gold));
                }

                // 四向冲击光锥（BlastCone），指向即将飞出的四枚圣火
                for (int i = 0; i < 4; i++)
                {
                    float angle = MathHelper.TwoPi * i / 4f;
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero,
                        AegisVisuals.Add(AegisVisuals.Gold, 0.85f), AegisVisuals.TexBlastCone,
                        new Vector2(Main.rand.NextFloat(3.2f, 4.6f), 1.3f), angle, 0.85f, 0f, 24));
                }

                AegisVisuals.Screenshake(Projectile.Center, 3.2f, 1100f);
            }

            if (Projectile.owner == Main.myPlayer)
            {
                int fireballType = ModContent.ProjectileType<AegisFireball>();
                int fireballDamage = (int)(Projectile.damage * 0.75f);
                int count = 4;

                for (int i = 0; i < count; i++)
                {
                    float angle = MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.25f, 0.25f);
                    Vector2 shootVelocity = angle.ToRotationVector2() * Main.rand.NextFloat(10f, 14f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, shootVelocity,
                        fireballType, fireballDamage, Projectile.knockBack * 0.5f, Projectile.owner);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float growth = MathHelper.Clamp(Timer / 12f, 0.25f, 1f);

            // HolyBomb 的挤压：横向鼓、纵向瘪，然后回弹
            float squish = CalamityUtils.SineBumpEasing(squishAnimation, 1) * 0.26f;
            Vector2 squishVector = new(1f + squish, 1f - squish);

            // 爆裂前收紧：核心变小变亮，外圈符文急速旋转
            float eruption = ImminentEruption;
            float radius = CoreRadius * growth * MathHelper.Lerp(1f, 0.74f, eruption);
            float brightness = MathHelper.Lerp(1f, 1.5f, eruption);
            float spin = Main.GlobalTimeWrappedHourly * (2.4f + eruption * 9f) + Projectile.identity;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            // ① 残影：余烬色，越旧越暗
            Texture2D fire = AegisVisuals.Tex(AegisVisuals.TexFireBody);
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                float completion = i / (float)Projectile.oldPos.Length;
                Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(fire, trailPosition, null,
                    AegisVisuals.Add(Color.Lerp(AegisVisuals.Flame, AegisVisuals.Ember, completion), 0.36f * (1f - completion)),
                    -Projectile.rotation * 0.6f + completion * 2.2f, fire.Size() * 0.5f,
                    new Vector2(AegisVisuals.RadiusScale(fire, radius * MathHelper.Lerp(0.85f, 0.3f, completion))),
                    SpriteEffects.None, 0);
            }

            // ② 符文封印环：炉心不是一团火，而是一颗被封住的火
            AegisVisuals.DrawRuneSigil(drawPosition, radius * 1.75f, spin, 0.5f * growth * brightness,
                squishVector, 0.9f + eruption * 0.6f);

            // ③ 日核本体
            AegisVisuals.DrawSolarCore(drawPosition, radius, brightness * growth,
                Projectile.rotation, squishVector);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
