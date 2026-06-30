using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Healing;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules
{
    /// <summary>
    /// 默认规则：格子槽为空时的行为，完全照搬 DesertEagleLifeRound 的效果。
    /// 银色螺旋尾迹 + 命中吸血 8% + 银色冲击特效。
    /// </summary>
    public class DEDefaultBulletRule : DEBulletRule
    {
        private const string SparkTexturePath = "CalamityMod/Particles/ThinEndedLine";
        private static readonly Color SilverMain = new(214, 224, 236);
        private static readonly Color SilverAccent = new(255, 255, 255);
        private static readonly Color SilverDark = new(140, 152, 170);

        public override int GunItemType => 0;  // 无枪
        public override int ExtraUpdates => 4;
        public override int ArmorPenetration => 0;

        public override void AI(Projectile projectile, Player owner)
        {
            // 旋转与方向
            projectile.spriteDirection = projectile.direction = (projectile.velocity.X > 0).ToDirectionInt();
            projectile.rotation = projectile.velocity.ToRotation()
                + (projectile.spriteDirection == 1 ? 0f : MathHelper.Pi)
                + MathHelper.ToRadians(90f) * projectile.direction;

            projectile.localAI[0] += 1f;
            SpawnBulletTrail(projectile.Center, projectile.velocity, 0.95f);

            if (projectile.localAI[0] > 4f)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 dustSpeed = -projectile.velocity * Main.rand.NextFloat(0.45f, 0.7f);
                    Dust dust = Dust.NewDustPerfect(
                        projectile.Center - projectile.velocity * 0.1f * i,
                        DustID.SilverCoin,
                        dustSpeed,
                        120,
                        Color.Lerp(SilverMain, SilverAccent, Main.rand.NextFloat()),
                        Main.rand.NextFloat(0.75f, 1.05f));
                    dust.noGravity = true;
                }
            }
        }

        public override bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity)
        {
            SpawnSilverImpact(projectile.Center, oldVelocity.SafeNormalize(Vector2.UnitX), 1.15f);
            return true;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            owner.SpawnLifeStealProjectile(
                target, projectile,
                ModContent.ProjectileType<TransfusionTrail>(),
                (int)Math.Round(hit.Damage * 0.08));
            SpawnSilverImpact(projectile.Center, projectile.velocity.SafeNormalize(Vector2.UnitX), 1.25f);
        }

        public override bool PreDraw(Projectile projectile, Player owner, ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], lightColor, 1);
            return false;
        }

        public override string TooltipEffectEN => "Silver homing bullet; heals 8% of damage dealt";
        public override string TooltipEffectZH => "银色弹幕，命中回血8%";

        // ── 银色特效工具方法（与 DesertEagleLifeRound 完全一致）──────────

        private static void SpawnSilverImpact(Vector2 position, Vector2 direction, float scale = 1f)
        {
            Vector2 impactDirection = direction.SafeNormalize(Vector2.UnitY);
            const float pulseScale = 1.15f;
            const int ringLifetime = 24;
            const int sparkCount = 18;
            const int dustCount = 28;

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(position, Vector2.Zero, SilverAccent, 1.15f * scale, 28));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(position, Vector2.Zero, SilverAccent,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, 0f, 0.01f,
                    0.08f * pulseScale * scale, ringLifetime, true, 0.95f));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(position, Vector2.Zero, SilverMain * 0.82f,
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, 0f, 0.01f,
                    0.12f * pulseScale * scale, ringLifetime + 3, true, 0.7f));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(position, Vector2.Zero,
                    SilverAccent * 0.75f, new Vector2(1f, 4.8f) * scale, impactDirection.ToRotation(), 0.16f, 0.034f, ringLifetime));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(position, Vector2.Zero,
                    SilverMain * 0.55f, new Vector2(1f, 4.2f) * scale, impactDirection.ToRotation() + MathHelper.PiOver2, 0.14f, 0.03f, ringLifetime - 2));

                for (int i = 0; i < sparkCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / sparkCount;
                    Vector2 sparkDir = angle.ToRotationVector2();
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        position + sparkDir * 10f * scale, sparkDir * 9f * scale, false, 16,
                        0.045f * scale, Color.Lerp(SilverMain, SilverAccent, i % 2 == 0 ? 0.75f : 0.35f),
                        new Vector2(1.2f, 0.58f), true));
                }
                for (int i = 0; i < sparkCount; i++)
                {
                    float angle = MathHelper.TwoPi * (i + 0.5f) / sparkCount;
                    Vector2 sparkDir = angle.ToRotationVector2();
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(
                        position + sparkDir * 7f * scale, sparkDir * 7f * scale, SparkTexturePath, false, 15,
                        0.04f * scale, i % 2 == 0 ? SilverAccent : SilverMain, new Vector2(0.75f, 1.8f), shrinkSpeed: 0.78f));
                }
            }

            for (int i = 0; i < dustCount; i++)
            {
                float angle = MathHelper.TwoPi * i / dustCount;
                Vector2 dustDir = angle.ToRotationVector2();
                Dust dust = Dust.NewDustPerfect(position, i % 2 == 0 ? DustID.SilverCoin : DustID.SilverFlame,
                    dustDir * 7.5f * scale, 105, Color.Lerp(SilverDark, SilverAccent, i % 3 / 2f), 1.25f * scale);
                dust.noGravity = true;
            }
        }

        private static void SpawnBulletTrail(Vector2 position, Vector2 velocity, float scale = 1f)
        {
            Vector2 forward = velocity.SafeNormalize(Vector2.UnitY);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float phase = Main.GlobalTimeWrappedHourly * 18f;
            float helixRadius = 5.5f * scale;

            for (int i = 0; i < 3; i++)
            {
                float helixPhase = phase + MathHelper.TwoPi * i / 3f;
                float offset = (float)System.Math.Sin(helixPhase) * helixRadius;
                float depth = 0.55f + 0.45f * (float)System.Math.Cos(helixPhase);
                Dust dust = Dust.NewDustPerfect(position + side * offset,
                    i == 0 ? DustID.SilverCoin : i == 1 ? DustID.SilverFlame : DustID.TintableDustLighted,
                    -forward * 0.45f + side * (float)System.Math.Cos(helixPhase) * 0.18f,
                    105, Color.Lerp(SilverDark, SilverAccent, depth),
                    MathHelper.Lerp(0.62f, 0.92f, depth) * scale);
                dust.noGravity = true;
            }

            if (Main.dedServ) return;

            GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                position + forward * 5f * scale, -velocity * 0.045f, false, 3, 0.014f * scale,
                SilverAccent, new Vector2(0.75f, 2.8f), false, true));

            if (!Main.rand.NextBool(4)) return;
            GeneralParticleHandler.SpawnParticle(new CustomSpark(position, -forward * 1.1f,
                SparkTexturePath, false, 9, 0.023f * scale, SilverMain, new Vector2(0.46f, 2.4f), shrinkSpeed: 0.8f));
        }
    }
}
