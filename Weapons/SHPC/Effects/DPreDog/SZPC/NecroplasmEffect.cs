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

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC
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

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            sinTimer = 0f;
            timer = 0;

            projectile.penetrate = 5;
            projectile.velocity *= 1.8f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            timer++;
            projectile.velocity *= 1.04f;

            NPC target = projectile.Center.ClosestNPCAt(3000f);
            if (target != null)
            {
                Vector2 desired = projectile.SafeDirectionTo(target.Center);
                projectile.velocity = (projectile.velocity * 5f + desired * 16f) / 6f;
            }

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
            owner.SetScreenshake(8.5f);
            float power = 1.35f;

            SpawnNecroplasmCollapseDust(projectile, owner, power);
            SpawnNecroplasmCollapsePulses(projectile, owner, power);
            SpawnNecroplasmCollapseSmears(projectile, owner, power);
            PlayNecroplasmCollapseSounds(projectile);
            SpawnNecroplasmDamage(projectile);
        }

        private void SpawnNecroplasmCollapseDust(Projectile projectile, Player owner, float power)
        {
            for (int i = 0; i < 55; i++)
            {
                Color useColor = GetRandomNecroBurstColor(owner);
                int dustType = Main.rand.NextBool(6)
                    ? ModContent.DustType<VoidDustInverted>()
                    : ModContent.DustType<VoidDust>();
                Vector2 dustVelocity = (projectile.velocity * 6f * power).RotatedByRandom(100f) * Main.rand.NextFloat(0.2f, 1f);

                Dust dust = Dust.NewDustPerfect(projectile.Center, dustType, dustVelocity);
                dust.noGravity = true;
                dust.noLightEmittence = true;
                dust.scale = Main.rand.NextFloat(1.55f, 2.05f) * power;
                dust.color = useColor;

                if (i % 2 != 0)
                    continue;

                Vector2 sparkVelocity = new Vector2(0f, -34f * power).RotatedByRandom(100f) * Main.rand.NextFloat(0.1f, 1f);
                Particle spark = new CustomSpark(
                    projectile.Center,
                    sparkVelocity,
                    "CalamityMod/Particles/Sparkle",
                    false,
                    40,
                    Main.rand.NextFloat(1.15f, 2f) * power,
                    useColor,
                    new Vector2(0.4f, 1.1f));
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        private void SpawnNecroplasmCollapsePulses(Projectile projectile, Player owner, float power)
        {
            for (int i = 0; i < 3; i++)
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
                    (0.4f - i * 0.03f) * power,
                    13);
                GeneralParticleHandler.SpawnParticle(softBurst);
            }

            Particle bloomRing = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                ThemeColor,
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0.15f * power,
                2.15f * power,
                38);
            GeneralParticleHandler.SpawnParticle(bloomRing);

            Particle blackCore = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                Color.Black,
                "CalamityMod/Particles/SmallBloom",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0f,
                1.05f * power,
                39,
                false);
            GeneralParticleHandler.SpawnParticle(blackCore);
        }

        private void SpawnNecroplasmCollapseSmears(Projectile projectile, Player owner, float power)
        {
            int parts = 8;
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
                    19,
                    3f * power,
                    useColor,
                    new Vector2(0.2f, 1f));
                GeneralParticleHandler.SpawnParticle(smear);
            }
        }

        private static void PlayNecroplasmCollapseSounds(Projectile projectile)
        {
            for (int i = 0; i < 3; i++)
            {
                SoundStyle meteorSound = new("CalamityMod/Sounds/Item/EarthMeteor");
                SoundEngine.PlaySound(meteorSound with { Volume = 0.44f, Pitch = -0.12f * (i + 1), MaxInstances = 3 }, projectile.Center);
            }

            SoundStyle reflectSound = new("CalamityMod/Sounds/Item/ShadowboltReflect");
            SoundEngine.PlaySound(reflectSound with { Volume = 0.76f, Pitch = -0.34f }, projectile.Center);
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
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 8f);

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
            if (owner.shirtColor == Color.White && Main.rand.NextBool(4))
                return new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB);

            return Main.rand.Next(5) switch
            {
                0 => ThemeColor,
                1 => StartColor,
                2 => EndColor,
                3 => new Color(120, 16, 95),
                _ => Color.Lerp(Color.Black, ThemeColor, Main.rand.NextFloat(0.25f, 0.75f))
            };
        }
    }
}
