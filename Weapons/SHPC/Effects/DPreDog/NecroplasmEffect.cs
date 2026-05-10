using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    public class NecroplasmEffect : DefaultEffect
    {
        public override int EffectID => 31;

        public override int AmmoType => ModContent.ItemType<Necroplasm>();

        public override Color ThemeColor => new Color(255, 80, 180);
        public override Color StartColor => new Color(255, 120, 200);
        public override Color EndColor => new Color(200, 40, 140);

        public override float SquishyLightParticleFactor => 1.85f;
        public override float ExplosionPulseFactor => 1.85f;

        private float sinTimer;
        private int timer;
        private int homingTimer;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            sinTimer = 0f;
            timer = 0;
            homingTimer = 0;

            projectile.penetrate = 5;
            if (projectile.timeLeft < 240)
                projectile.timeLeft = 240;
            projectile.velocity *= 1.8f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            timer++;
            projectile.velocity *= 1.04f;

            NPC target = projectile.Center.ClosestNPCAt(3000f);
            if (target != null)
            {
                homingTimer++;
                Vector2 desired = projectile.SafeDirectionTo(target.Center);
                float trackingPower = Utils.GetLerpValue(0f, 120f, homingTimer, true);
                float targetSpeed = MathHelper.Lerp(20f, 34f, trackingPower);
                float inertia = MathHelper.Lerp(5f, 2.5f, trackingPower);
                projectile.velocity = (projectile.velocity * inertia + desired * targetSpeed) / (inertia + 1f);
            }
            else
                homingTimer = 0;

            if (timer % 6 == 0)
            {
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<Necroplasm_Damage>(),
                    (int)(projectile.damage * 0.5f),
                    0f,
                    projectile.owner
                );
            }

            sinTimer += 0.22f;

            float pulse = (float)System.Math.Sin(sinTimer);
            float angle = projectile.velocity.ToRotation() + MathHelper.Pi / 2f;
            Vector2 normal = angle.ToRotationVector2();
            float radius = 6f;
            Vector2 offset = normal * pulse * radius;

            CreateVoidDust(projectile.Center + offset);
            CreateVoidDust(projectile.Center - offset);

            if (Main.rand.NextBool(3))
            {
                SquishyLightParticle particle = new(
                    projectile.Center,
                    -projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.05f, 0.2f),
                    Main.rand.NextFloat(0.3f, 0.6f),
                    Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat()),
                    Main.rand.Next(10, 16)
                );
                GeneralParticleHandler.SpawnParticle(particle);
            }

            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.35f);
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            owner.SetScreenshake(3.5f);
            float power = 0.58f;

            SpawnNecroplasmCollapseDust(projectile, owner, power);
            SpawnNecroplasmCollapsePulses(projectile, owner, power);
            SpawnNecroplasmCollapseSmears(projectile, owner, power);
            PlayNecroplasmCollapseSounds(projectile);
            SpawnNecroplasmDamage(projectile);
        }

        private void SpawnNecroplasmCollapseDust(Projectile projectile, Player owner, float power)
        {
            for (int i = 0; i < 24; i++)
            {
                Color useColor = GetRandomNecroBurstColor(owner);
                Vector2 dustVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 7.5f) * power;

                Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.FireworkFountain_Pink, dustVelocity);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.85f, 1.35f) * power;
                dust.color = useColor;

                if (i % 3 != 0)
                    continue;

                Vector2 sparkVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.8f, 6.8f) * power;
                Particle spark = new CustomSpark(
                    projectile.Center,
                    sparkVelocity,
                    "CalamityMod/Particles/Sparkle",
                    false,
                    18,
                    Main.rand.NextFloat(0.45f, 0.82f) * power,
                    useColor,
                    new Vector2(0.4f, 1.1f));
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        private void SpawnNecroplasmCollapsePulses(Projectile projectile, Player owner, float power)
        {
            for (int i = 0; i < 2; i++)
            {
                Color useColor = GetRandomNecroBurstColor(owner);
                Particle softBurst = new CustomPulse(
                    projectile.Center,
                    Vector2.Zero,
                    useColor,
                    "CalamityMod/Particles/SoftRoundExplosion",
                    Vector2.One,
                    Main.rand.NextFloat(-10f, 10f),
                    0f,
                    (0.24f - i * 0.03f) * power,
                    10);
                GeneralParticleHandler.SpawnParticle(softBurst);
            }

            Particle bloomRing = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                ThemeColor,
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0.06f * power,
                0.86f * power,
                22);
            GeneralParticleHandler.SpawnParticle(bloomRing);
        }

        private void SpawnNecroplasmCollapseSmears(Projectile projectile, Player owner, float power)
        {
            int parts = 5;
            float rot = Main.rand.NextFloat(-9f, 9f);

            for (int i = 0; i < parts; i++)
            {
                Color useColor = GetRandomNecroBurstColor(owner);
                Vector2 smearVelocity = new Vector2(0f, -15f * (i % 2 == 0 ? 1.8f : 1f) * power)
                    .RotatedBy(i * (MathHelper.TwoPi / parts))
                    .RotatedBy(rot);

                Particle smear = new CustomSpark(
                    projectile.Center,
                    smearVelocity,
                    "CalamityMod/Particles/VerticalSmear",
                    false,
                    13,
                    1.35f * power,
                    useColor,
                    new Vector2(0.2f, 1f));
                GeneralParticleHandler.SpawnParticle(smear);
            }
        }

        private static void PlayNecroplasmCollapseSounds(Projectile projectile)
        {
            SoundStyle reflectSound = new("CalamityMod/Sounds/Item/ShadowboltReflect");
            SoundEngine.PlaySound(reflectSound with { Volume = 0.48f, Pitch = -0.18f }, projectile.Center);
        }

        private static void SpawnNecroplasmDamage(Projectile projectile)
        {
            int projIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner
            );

            if (projIndex >= 0 && projIndex < Main.maxProjectiles)
            {
                Projectile proj = Main.projectile[projIndex];
                proj.width = 250;
                proj.height = 250;
            }

            for (int i = 0; i < 6; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 12f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    velocity,
                    ModContent.ProjectileType<Necroplasm_Damage>(),
                    (int)(projectile.damage * Main.rand.NextFloat(0.8f, 1.2f)),
                    0f,
                    projectile.owner
                );
            }
        }

        private void CreateVoidDust(Vector2 pos)
        {
            Dust dust = Dust.NewDustPerfect(
                pos,
                DustID.FireworkFountain_Pink,
                Vector2.Zero,
                0,
                Color.Lerp(StartColor, EndColor, Main.rand.NextFloat()),
                Main.rand.NextFloat(1.1f, 1.6f)
            );
            dust.noGravity = true;
        }

        private Color GetRandomNecroBurstColor(Player owner)
        {
            if (owner.shirtColor == Color.White && Main.rand.NextBool(8))
                return new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB);

            return Main.rand.Next(5) switch
            {
                0 => ThemeColor,
                1 => StartColor,
                2 => EndColor,
                3 => new Color(120, 16, 95),
                _ => new Color(235, 70, 170)
            };
        }
    }
}
