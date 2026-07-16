using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    public class LivingShardEffect : DefaultEffect
    {
        public override int EffectID => 15;

        // 生命碎片（你自己替换真实ID）
        public override int AmmoType => ModContent.ItemType<LivingShard>();


        // ===== 三主题色（纯绿色系）=====
        public override Color ThemeColor => new Color(120, 255, 120);
        public override Color StartColor => new Color(180, 255, 180);
        public override Color EndColor => new Color(60, 200, 120);

        // 交给下方自定义的引爆闪光处理，屏蔽共享默认爆炸（仿照AshesofCalamityEffect等"直接引爆"效果的写法）
        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override float GlowIntensityFactor => 0f;

        private const int MinButterflies = 3;
        private const int MaxButterflies = 5; // Next(3,6) 上限不含，实际取3~5
        private const float SpreadHalfAngleDegrees = 10f; // 正前方20度范围 = ±10度

        // 继承SHPLB(NewLegendSHPB)的默认shootSpeed(20f)再快一些——与旧版"生命气息"一致的速度
        private const float ButterflyLaunchSpeed = 20f * 1.3f;

        // ================= OnSpawn：直接引爆（仿照 PurifiedGelEffect 的 firstFrame 套路）=================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.GetGlobalProjectile<LivingShard_GP>().firstFrame = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 2;
            projectile.tileCollide = false;
            // 本体只是一次性的引爆触发器，不需要可见、也不需要自己造成伤害
            projectile.friendly = false;
            projectile.hide = true;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            LivingShard_GP gp = projectile.GetGlobalProjectile<LivingShard_GP>();
            if (!gp.firstFrame)
                return;

            gp.firstFrame = false;
            projectile.Kill();
        }

        // ================= OnKill：引爆炸出萤火魂蝶群 =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityLegendsComeBack/Sound/Other/DeltaForce/沙漠之鹰有消音"), projectile.Center);

            if (projectile.owner == Main.myPlayer)
                SpawnFireflyButterflies(projectile, owner);

            Vector2 center = projectile.Center;

            // ================= 引爆闪光：自己画的冲击波，不再依赖共享默认爆炸 =================
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                Color.Lerp(Color.LimeGreen, Color.White, 0.25f),
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                0f,
                0.14f,
                1.05f,
                20));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                Color.White,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One * 0.6f,
                0f,
                0.5f,
                0.06f,
                14));

            int count = 12;

            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count;

                Vector2 dir = angle.ToRotationVector2();
                Vector2 vel = dir * Main.rand.NextFloat(2f, 5f);

                SquishyLightParticle particle = new(
                    center,
                    vel,
                    1.2f,
                    Color.Lerp(Color.LimeGreen, Color.White, 0.3f),
                    18
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }
        }

        private static void SpawnFireflyButterflies(Projectile projectile, Player owner)
        {
            int count = Main.rand.Next(MinButterflies, MaxButterflies + 1);
            int damagePerButterfly = Math.Max(1, (int)Math.Round(projectile.damage / (float)count));

            Vector2 forward = projectile.velocity.SafeNormalize(
                owner.direction == 0 ? Vector2.UnitX : new Vector2(owner.direction, 0f));
            float baseRotation = forward.ToRotation();

            for (int i = 0; i < count; i++)
            {
                float angle = baseRotation + MathHelper.ToRadians(Main.rand.NextFloat(-SpreadHalfAngleDegrees, SpreadHalfAngleDegrees));
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(ButterflyLaunchSpeed * 0.92f, ButterflyLaunchSpeed * 1.08f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    velocity,
                    ModContent.ProjectileType<LivingShard_Butterfly>(),
                    damagePerButterfly,
                    0f,
                    projectile.owner
                );
            }
        }
    }

    // 让生命碎片的光球在生成后立刻引爆（同 PurifiedGelEffect 的 firstFrame 套路）
    internal class LivingShard_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool firstFrame;
    }
}
