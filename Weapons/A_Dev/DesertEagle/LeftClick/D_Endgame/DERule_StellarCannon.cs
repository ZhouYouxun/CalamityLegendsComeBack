using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.D_Endgame
{
    /// <summary>
    /// 新星炮：巨型星质炮弹，初速慢但持续加速，命中/消亡时 192 像素范围巨型爆炸，施加星质感染。
    /// </summary>
    public class DERule_StellarCannon : DEBulletRule
    {
        private static readonly Color AstralOrange = new(255, 160, 50);
        private static readonly Color AstralBlue = new(100, 180, 255);
        private static readonly Color AstralGreen = new(60, 230, 100);
        private static readonly Color AstralWhite = new(220, 240, 255);

        private const float AccelMultiplier = 1.065f;
        private const float MaxSpeed = 60f;

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.StellarCannon>();

        public override int ExtraUpdates => 1;   // 初始较慢
        public override float SpeedMultiplier => 0.5f;  // 起步速度减半

        public override void SetDefaults(Projectile projectile)
        {
            projectile.width = 30;
            projectile.height = 30;
            projectile.light = 0.8f;
            projectile.alpha = 255;  // 淡入
        }

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            // 生成时：橙蓝光晕
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(projectile.Center, Vector2.Zero, AstralOrange, 1.0f, 24));
                for (int i = 0; i < 3; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                        projectile.Center, Main.rand.NextVector2Circular(1.5f, 1.5f),
                        AstralOrange * 0.45f, 16, Main.rand.NextFloat(0.45f, 0.65f), 0.55f, 0f));
                }
            }
        }

        public override void AI(Projectile projectile, Player owner)
        {
            // 淡入
            if (projectile.alpha > 0)
                projectile.alpha = System.Math.Max(0, projectile.alpha - 30);

            // 持续加速
            float speed = projectile.velocity.Length();
            if (speed < MaxSpeed)
                projectile.velocity *= AccelMultiplier;

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            float t = Main.GlobalTimeWrappedHourly * 10f + projectile.whoAmI * 0.55f;

            // 双色螺旋
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 3; i++)
            {
                float helixPhase = t + MathHelper.TwoPi * i / 3f;
                float offset = (float)System.Math.Sin(helixPhase) * 10f;
                Color astralColor = i == 0 ? AstralOrange : i == 1 ? AstralBlue : AstralGreen;
                Dust dust = Dust.NewDustPerfect(projectile.Center + side * offset,
                    DustID.TintableDustLighted,
                    -forward * 0.4f, 120, astralColor, 1.0f);
                dust.noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    projectile.Center, Main.rand.NextVector2Circular(2f, 2f),
                    false, 8, 0.022f,
                    Main.rand.Next(3) == 0 ? AstralOrange : Main.rand.NextBool() ? AstralBlue : AstralGreen,
                    new Vector2(0.7f, 0.7f)));
            }

            Lighting.AddLight(projectile.Center, AstralOrange.R / 255f * 0.7f, AstralBlue.G / 255f * 0.5f, AstralBlue.B / 255f * 0.6f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 360);
            SpawnStellarDetonation(projectile.Center);
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            SpawnStellarDetonation(projectile.Center);
        }

        private static void SpawnStellarDetonation(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                // 主爆炸 Bloom
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, AstralWhite, 2.0f, 36));
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, AstralOrange, 1.5f, 28));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, AstralOrange * 0.9f,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, 0f, 0f, 0.16f, 30, true, 0.92f));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, AstralBlue * 0.7f,
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, 0f, 0f, 0.2f, 36, true, 0.78f));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, AstralGreen * 0.5f,
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, 0f, 0f, 0.24f, 40, true, 0.65f));

                for (int i = 0; i < 8; i++)
                {
                    float angle = MathHelper.TwoPi * i / 8f;
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(pos, Vector2.Zero,
                        (i % 3 == 0 ? AstralOrange : i % 3 == 1 ? AstralBlue : AstralGreen) * 0.8f,
                        new Vector2(0.8f, 4.8f), angle, 0.2f, 0.045f, 22));
                }

                for (int i = 0; i < 8; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(pos,
                        Main.rand.NextVector2Circular(4f, 4f),
                        Color.Lerp(AstralOrange, AstralBlue, Main.rand.NextFloat()) * 0.5f,
                        24, Main.rand.NextFloat(0.55f, 0.9f), 0.6f, Main.rand.NextFloat(-0.02f, 0.02f)));
                }

                for (int i = 0; i < 28; i++)
                {
                    float angle = MathHelper.TwoPi * i / 28f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        pos + angle.ToRotationVector2() * 15f, angle.ToRotationVector2() * 16f,
                        false, 18, 0.04f,
                        i % 3 == 0 ? AstralOrange : i % 3 == 1 ? AstralBlue : AstralGreen,
                        new Vector2(1.1f, 0.5f)));
                }
            }

            for (int i = 0; i < 32; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(14f, 14f), 120,
                    i % 3 == 0 ? AstralOrange : i % 3 == 1 ? AstralBlue : AstralGreen, 1.35f);
                dust.noGravity = true;
            }
        }

        public override string TooltipEffectEN => "Huge stellar orb — slow start, accelerates to warp speed; detonates in a massive 192px tri-color AoE";
        public override string TooltipEffectZH => "巨型星质炮弹，初速慢但持续加速；命中时三色192px巨型爆炸，施加星质感染";
    }
}
