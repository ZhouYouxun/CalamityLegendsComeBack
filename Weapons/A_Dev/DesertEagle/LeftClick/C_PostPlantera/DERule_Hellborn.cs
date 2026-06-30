using CalamityMod.Particles;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.C_PostPlantera
{
    /// <summary>
    /// 地狱降临：橙红色火焰球，每帧加速 ×1.035，命中时小型区域爆炸，螺旋火焰拖影。
    /// </summary>
    public class DERule_Hellborn : DEBulletRule
    {
        private static readonly Color HellOrange = new(255, 100, 15);
        private static readonly Color HellRed = new(255, 30, 5);
        private static readonly Color HellYellow = new(255, 220, 40);

        private const float AccelMultiplier = 1.035f;
        private const float MaxSpeed = 40f;

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.Hellborn>();

        public override int ExtraUpdates => 2;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.width = 18;
            projectile.height = 18;
            projectile.light = 0.65f;
        }

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            // 生成时释放一帧烟雾
            if (!Main.dedServ)
            {
                for (int i = 0; i < 3; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                        projectile.Center, Main.rand.NextVector2Circular(1.5f, 1.5f),
                        HellOrange * 0.5f, 14, Main.rand.NextFloat(0.4f, 0.6f), 0.55f, Main.rand.NextFloat(-0.03f, 0.03f)));
                }
            }
        }

        public override void AI(Projectile projectile, Player owner)
        {
            // 加速
            float speed = projectile.velocity.Length();
            if (speed < MaxSpeed)
                projectile.velocity *= AccelMultiplier;

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            projectile.localAI[0] += 1f;

            float t = Main.GlobalTimeWrappedHourly * 20f + projectile.whoAmI;
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);

            // 螺旋火焰 dust
            for (int i = 0; i < 2; i++)
            {
                float helixPhase = t + MathHelper.Pi * i;
                float offset = (float)System.Math.Sin(helixPhase) * 7f;
                Dust dust = Dust.NewDustPerfect(projectile.Center + side * offset,
                    i == 0 ? DustID.Torch : DustID.OrangeTorch,
                    -forward * 0.5f, 120, i == 0 ? HellOrange : HellRed, 1.2f);
                dust.noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(6))
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    projectile.Center, -forward * 0.06f,
                    HellOrange * 0.35f, 12, Main.rand.NextFloat(0.35f, 0.5f), 0.5f, 0f));
                GeneralParticleHandler.SpawnParticle(new CircularSmearVFX(
                    projectile.Center,
                    Color.Lerp(HellOrange, HellRed, 0.6f) * 0.4f,
                    projectile.rotation,
                    0.38f));
            }

            Lighting.AddLight(projectile.Center, HellOrange.R / 255f * 0.7f, HellOrange.G / 255f * 0.4f, 0f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 420);
            SpawnHellbornExplosion(projectile.Center, projectile.damage);
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            SpawnHellbornExplosion(projectile.Center, projectile.damage);
        }

        private static void SpawnHellbornExplosion(Vector2 pos, int damage)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, HellYellow, 1.3f, 28));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, HellOrange * 0.85f,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, 0f, 0f, 0.1f, 24, true, 0.88f));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, HellRed * 0.6f,
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, 0f, 0f, 0.13f, 28, true, 0.72f));
                for (int i = 0; i < 5; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(pos,
                        Main.rand.NextVector2Circular(3f, 3f),
                        Color.Lerp(HellOrange, HellRed, Main.rand.NextFloat()) * 0.55f,
                        20, Main.rand.NextFloat(0.5f, 0.8f), 0.55f, Main.rand.NextFloat(-0.03f, 0.03f)));
                }
                for (int i = 0; i < 20; i++)
                {
                    float angle = MathHelper.TwoPi * i / 20f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        pos + angle.ToRotationVector2() * 10f, angle.ToRotationVector2() * 12f,
                        false, 14, 0.03f, i % 2 == 0 ? HellOrange : HellRed, new Vector2(1.0f, 0.5f)));
                }
            }
            for (int i = 0; i < 24; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.Torch,
                    Main.rand.NextVector2Circular(12f, 12f), 110, HellOrange, 1.3f);
                dust.noGravity = true;
            }
        }

        public override string TooltipEffectEN => "Accelerating fireball (×1.035 per tick); erupts in a blazing AoE on impact";
        public override string TooltipEffectZH => "持续加速的火焰球（每帧×1.035），命中时触发烈焰区域爆炸";
    }
}
