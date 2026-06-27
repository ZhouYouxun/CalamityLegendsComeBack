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
    // 终结技：神手沿玩家相对坐标下降，蓄力后握住玩家，赋予10秒无敌+70%减速。
    // 贴图待补充；目前以粒子光效占位。
    public class AegisUltimateHand : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Player Owner => Main.player[Projectile.owner];

        private const int StateApproach = 0;
        private const int StateChargeUp = 1;
        private const int StateGripping = 2;

        private const int ApproachTime = 76;
        private const int ChargeUpTime = 28;

        private ref float State  => ref Projectile.ai[0];
        private ref float Timer  => ref Projectile.ai[1];

        private float relativeYOffset = -520f;
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
                relativeYOffset = MathHelper.Clamp(Projectile.Center.Y - Owner.Center.Y, -680f, -260f);
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
            float progress = MathHelper.Clamp(Timer / ApproachTime, 0f, 1f);
            float eased = MathHelper.SmoothStep(0f, 1f, progress);
            relativeYOffset = MathHelper.Lerp(relativeYOffset, MathHelper.Lerp(-520f, 0f, eased), 0.18f);
            Projectile.Center = Owner.Center + new Vector2(0f, relativeYOffset);

            if (!Main.dedServ)
                EmitConvergingSpiral(Owner.Center, progress, false);

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
            Projectile.Center = Owner.Center;

            if (!Main.dedServ)
                EmitConvergingSpiral(Owner.Center, MathHelper.Clamp(Timer / ChargeUpTime, 0f, 1f), true);

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
                EmitGripFilaments();

            if (!Owner.GetModPlayer<AegisBladePlayer>().UltimateActive)
                Projectile.Kill();
        }

        private void EmitConvergingSpiral(Vector2 center, float progress, bool tight)
        {
            int count = tight ? 5 : 3;
            float baseRadius = tight ? MathHelper.Lerp(96f, 18f, progress) : MathHelper.Lerp(170f, 38f, progress);
            float spin = Main.GlobalTimeWrappedHourly * (tight ? 5.8f : 3.6f);
            for (int i = 0; i < count; i++)
            {
                float t = (Timer * 0.11f + i / (float)count) % 1f;
                float angle = spin + MathHelper.TwoPi * i / count + t * MathHelper.TwoPi * 1.618f;
                float radius = baseRadius * (1f - t * 0.55f);
                Vector2 offset = angle.ToRotationVector2() * radius;
                Vector2 tangent = offset.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);
                Vector2 velocity = -offset.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(1.4f, tight ? 7.5f : 5.2f, progress) + tangent * (tight ? 1.2f : 0.7f);
                Dust dust = Dust.NewDustPerfect(center + offset, DustID.GoldFlame, velocity, 0, HandGold, tight ? 1.25f : 1f);
                dust.noGravity = true;
                dust.fadeIn = 0.8f + progress * 0.5f;
            }
        }

        private void EmitGripFilaments()
        {
            float angle = Main.GlobalTimeWrappedHourly * 4.2f + Main.rand.NextFloat(MathHelper.TwoPi);
            float radius = Main.rand.NextFloat(28f, 62f);
            Vector2 offset = angle.ToRotationVector2() * radius;
            Vector2 velocity = -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.4f, 3.6f);
            Dust dust = Dust.NewDustPerfect(Owner.Center + offset, DustID.GoldFlame, velocity, 0, Main.rand.NextBool(3) ? HandWhite : HandGold, Main.rand.NextFloat(0.8f, 1.35f));
            dust.noGravity = true;
            dust.fadeIn = 0.9f;
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
