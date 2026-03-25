using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    public class RuinousSoulEffect : DefaultEffect
    {
        public override int EffectID => 30;

        public override int AmmoType => ModContent.ItemType<RuinousSoul>();

        public override Color ThemeColor => new Color(190, 120, 255);
        public override Color StartColor => new Color(255, 210, 255);
        public override Color EndColor => new Color(90, 30, 140);

        public override float SquishyLightParticleFactor => 1.85f;
        public override float ExplosionPulseFactor => 1.85f;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            // 初始速度三倍
            projectile.velocity *= 3f;

            // 穿透次数设置为三十
            projectile.penetrate = 30;
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            // 每帧乘以 1.02，抵消减速
            projectile.velocity *= 1.02f;
        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 命中音效
            SoundEngine.PlaySound(SoundID.Item103 with
            {
                Volume = 0.75f,
                Pitch = -0.15f,
                PitchVariance = 0.18f
            }, target.Center);

            // 命中灵魂爆闪粒子
            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.5f, 6f);

                SquishyLightParticle particle = new(
                    target.Center,
                    velocity,
                    Main.rand.NextFloat(0.45f, 1f),
                    Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat()),
                    Main.rand.Next(18, 28)
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            // 命中外圈线性粒子
            int points = 14;
            float radians = MathHelper.TwoPi / points;
            Vector2 spinningPoint = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));

            for (int i = 0; i < points; i++)
            {
                Vector2 dir = spinningPoint.RotatedBy(radians * i);
                LineParticle line = new(
                    target.Center + dir * 10f,
                    dir * Main.rand.NextFloat(3f, 9f),
                    false,
                    Main.rand.Next(16, 24),
                    Main.rand.NextFloat(0.4f, 0.8f),
                    Color.Lerp(ThemeColor, EndColor, Main.rand.NextFloat())
                );
                GeneralParticleHandler.SpawnParticle(line);
            }

            // 少量 Dust 补充层次
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    target.Center,
                    DustID.PurpleTorch,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 4.8f),
                    0,
                    Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1f, 1.6f)
                );
                dust.noGravity = true;
            }
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
        }
    }
}