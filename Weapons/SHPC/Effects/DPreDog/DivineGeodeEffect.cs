using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Dusts;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    public class DivineGeodeEffect : DefaultEffect
    {
        public override int EffectID => 28;

        public override int AmmoType => ModContent.ItemType<DivineGeode>();

        public override Color ThemeColor => new Color(255, 230, 120);
        public override Color StartColor => new Color(255, 255, 210);
        public override Color EndColor => new Color(255, 150, 60);

        public override float SquishyLightParticleFactor => 1.85f;
        public override float ExplosionPulseFactor => 1.85f;

        private int timer;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            timer = 0;

            // 初始速度翻倍
            projectile.velocity *= 4f;

            // 出生金色爆闪
            for (int i = 0; i < 12; i++)
            {
                Vector2 vel = projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.3f, 1.2f);

                SquishyLightParticle p = new(
                    projectile.Center,
                    vel,
                    Main.rand.NextFloat(0.6f, 1.2f),
                    Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat()),
                    16
                );

                GeneralParticleHandler.SpawnParticle(p);
            }
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            timer++;

            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.7f);

            // 飞行拖尾（更亮、更密）
            if (Main.rand.NextBool(2))
            {
                Vector2 offset = Main.rand.NextVector2Circular(6f, 6f);

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + offset,
                    ModContent.DustType<SquashDust>(),
                    -projectile.velocity * Main.rand.NextFloat(0.3f, 0.8f)
                );
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.8f, 2.2f);
                dust.color = Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat());
                dust.fadeIn = 1.8f;
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 vel = -projectile.velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.3f, 1.2f);

                SquishyLightParticle p = new(
                    projectile.Center,
                    vel,
                    Main.rand.NextFloat(0.5f, 1.0f),
                    Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat()),
                    14
                );

                GeneralParticleHandler.SpawnParticle(p);
            }
        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // 主爆
            for (int i = 0; i < 16; i++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 6f);

                SquishyLightParticle p = new(
                    projectile.Center,
                    dir,
                    Main.rand.NextFloat(0.8f, 1.6f),
                    Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat()),
                    20
                );

                GeneralParticleHandler.SpawnParticle(p);
            }

            // 八方向几何射线
            for (int i = 0; i < 8; i++)
            {
                float rot = MathHelper.TwoPi / 8f * i;

                Vector2 velocity = new Vector2(1f, 0f).RotatedBy(rot) * 12f;

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    velocity,
                    ModContent.ProjectileType<DivineGeode_Lazer>(),
                    projectile.damage,
                    projectile.knockBack,
                    projectile.owner
                );
            }
        }
    }
}