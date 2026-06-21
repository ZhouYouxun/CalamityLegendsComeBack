using System;
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
    // 终结技：神手从天而降，蓄力后握住玩家，赋予10秒无敌+70%减速。
    // 贴图待补充；目前以粒子光效占位。
    public class AegisUltimateHand : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Player Owner => Main.player[Projectile.owner];

        private const int StateApproach = 0;
        private const int StateChargeUp = 1;
        private const int StateGripping = 2;

        private const int ApproachTime = 55;
        private const int ChargeUpTime = 28;

        private ref float State  => ref Projectile.ai[0];
        private ref float Timer  => ref Projectile.ai[1];

        // 神手初始位置（OnSpawn时确定，不再移动初始坐标）
        private Vector2 spawnPos;
        private bool initialized = false;

        private static readonly Color HandGold  = new(255, 220, 80);
        private static readonly Color HandWhite = new(255, 250, 220);

        public override void SetDefaults()
        {
            Projectile.width  = Projectile.height = 80;
            Projectile.friendly    = false;
            Projectile.tileCollide = false;
            Projectile.penetrate   = -1;
            Projectile.timeLeft    = 80 + BalanceAegisBlade.UltimateDuration + ApproachTime + ChargeUpTime;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!initialized)
            {
                spawnPos    = Projectile.Center;
                initialized = true;
            }

            Timer++;

            switch ((int)State)
            {
                case StateApproach: DoApproach(); break;
                case StateChargeUp: DoChargeUp(); break;
                case StateGripping: DoGripping(); break;
            }
        }

        // ── 接近阶段：从高空快速飞向玩家 ────────────────────────────────

        private void DoApproach()
        {
            float progress = Timer / ApproachTime;
            float lerpFactor = CalamityUtils.EaseInOutExp(progress, 3f, 3f) * 0.16f;
            Projectile.Center = Vector2.Lerp(Projectile.Center, Owner.Center, lerpFactor);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 vel = Main.rand.NextVector2Circular(2f, 2f);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), vel, false,
                    Main.rand.Next(8, 16), 0.055f, HandGold,
                    new Vector2(1.3f, 0.3f), true, false, 0.85f));
            }

            if (Timer >= ApproachTime)
            {
                State = StateChargeUp;
                Timer = 0;
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.3f }, Owner.Center);
            }
        }

        // ── 蓄力阶段：震颤 + 吸入粒子 ───────────────────────────────────

        private void DoChargeUp()
        {
            float shake = 5f * (1f - Timer / ChargeUpTime);
            Projectile.Center = Owner.Center + Main.rand.NextVector2Circular(shake, shake);

            if (!Main.dedServ && Timer % 2 == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    float orbit = Main.rand.NextFloat(40f, 100f);
                    Vector2 orbitVec = Main.rand.NextVector2CircularEdge(1f, 1f) * orbit;
                    Vector2 vel = (Projectile.Center - (Projectile.Center + orbitVec))
                                   .SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(5f, 11f);
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(
                        Projectile.Center + orbitVec, vel,
                        "CalamityMod/Particles/Sparkle", false,
                        Main.rand.Next(10, 20), Main.rand.NextFloat(0.8f, 1.5f),
                        HandGold, new Vector2(0.3f, 1.5f), true, true, shrinkSpeed: 0.14f));
                }
            }

            if (Timer >= ChargeUpTime)
            {
                State = StateGripping;
                Timer = 0;
                Owner.GetModPlayer<AegisBladePlayer>().ActivateUltimate();
                SoundEngine.PlaySound(SoundID.Item67 with { Volume = 1f, Pitch = 0.2f }, Owner.Center);

                if (!Main.dedServ)
                {
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                        Owner.Center, Vector2.Zero, HandGold, Vector2.One, 0f, 0.06f, 2.2f, 26));
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                        Owner.Center, Vector2.Zero, HandWhite, Vector2.One, 0f, 0.08f, 1.2f, 18));

                    for (int i = 0; i < 24; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(6f, 16f);
                        Dust d = Dust.NewDustPerfect(Owner.Center, DustID.GoldFlame, vel, 0, HandGold, 1.6f);
                        d.noGravity = true;
                    }
                }
            }
        }

        // ── 握持阶段：罩住玩家 ───────────────────────────────────────────

        private void DoGripping()
        {
            Projectile.Center = Owner.Center;

            // 持续罩住特效
            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Owner.Center + angle.ToRotationVector2() * Main.rand.NextFloat(30f, 60f);
                Vector2 vel = (Owner.Center - pos).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1f, 3f);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(pos, vel, false,
                    Main.rand.Next(8, 16), 0.04f, HandGold,
                    new Vector2(1.1f, 0.3f), true, false, 0.8f));
            }

            if (!Owner.GetModPlayer<AegisBladePlayer>().UltimateActive)
                Projectile.Kill();
        }

        // ── 绘制：光效占位（贴图待替换） ────────────────────────────────

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            float pulse = 0.8f + 0.2f * MathF.Sin(Main.GlobalTimeWrappedHourly * 6f);
            float size  = (int)State == StateGripping ? 2.8f : 1.3f;
            float alpha = (int)State == StateApproach
                ? (Timer / ApproachTime)
                : 1f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(bloom, drawPos, null,
                HandWhite with { A = 0 } * 0.5f * pulse * alpha,
                0f, bloom.Size() * 0.5f, size * 0.6f, SpriteEffects.None, 0);

            Main.EntitySpriteDraw(bloom, drawPos, null,
                HandGold with { A = 0 } * 0.75f * pulse * alpha,
                0f, bloom.Size() * 0.5f, size, SpriteEffects.None, 0);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
