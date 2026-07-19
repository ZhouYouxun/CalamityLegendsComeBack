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
        private int soulsFired;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            sinTimer = 0f;
            timer = 0;
            homingTimer = 0;
            soulsFired = 0;

            projectile.penetrate = 5;
            if (projectile.timeLeft < 240)
                projectile.timeLeft = 240;
            projectile.velocity *= 1.8f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            timer++;
            projectile.velocity *= 1.03f;

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

            float soulSpeedFactor = Utils.GetLerpValue(0f, 26f, soulsFired, true);
            float minSpeed = MathHelper.Lerp(16f, 24f, soulSpeedFactor);
            float maxSpeed = MathHelper.Lerp(34f, 54f, soulSpeedFactor);
            float currentSpeed = projectile.velocity.Length();
            if (currentSpeed > maxSpeed)
                projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitX) * maxSpeed;
            else if (currentSpeed < minSpeed)
                projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitX) * minSpeed;

            if (timer % 7 == 0)
            {
                soulsFired++;
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<SHPCNecroplasmDamage>(),
                    (int)(projectile.damage * 0.30f),
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
                (int)(projectile.damage * 1),
                projectile.knockBack,
                projectile.owner
            );

            if (projIndex >= 0 && projIndex < Main.maxProjectiles)
            {
                Projectile proj = Main.projectile[projIndex];
                proj.width = 250;
                proj.height = 250;
                proj.Center = projectile.Center;
            }

            const int shardCount = 6;
            float baseRotation = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < shardCount; i++)
            {
                float progress = i / (float)System.Math.Max(1, shardCount - 1);
                float angle = baseRotation + i * MathHelper.Pi * 0.72f + Main.rand.NextFloat(-0.18f, 0.18f);
                Vector2 direction = angle.ToRotationVector2();
                Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2);
                float radius = MathHelper.Lerp(46f, 174f, progress) + Main.rand.NextFloat(-14f, 18f);
                Vector2 spawnPosition = projectile.Center + direction * radius + tangent * Main.rand.NextFloat(-18f, 18f);
                Vector2 velocity = direction * Main.rand.NextFloat(4.5f, 10.5f) + tangent * Main.rand.NextFloat(-2.8f, 2.8f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<SHPCNecroplasmDamage>(),
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

        private static void SpawnShortFlightSouls(Projectile projectile, NPC target, int flightTime)
        {
            float travelFactor = Utils.GetLerpValue(18f, 150f, flightTime, true);
            int extraCount = System.Math.Max(1, (int)System.MathF.Round(MathHelper.Lerp(4f, 1f, travelFactor)));
            int damage = (int)(projectile.damage * 0.65f);
            if (damage < 1)
                damage = 1;

            Vector2 aimDirection = (target.Center - projectile.Center).SafeNormalize(projectile.velocity.SafeNormalize(Vector2.UnitX));
            Vector2 normal = aimDirection.RotatedBy(MathHelper.PiOver2);
            float speed = MathHelper.Lerp(9f, 15f, 1f - travelFactor);
            for (int i = 0; i < extraCount; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                float lane = (i + 1) / (float)extraCount;
                Vector2 spawnPosition =
                    target.Center -
                    aimDirection * MathHelper.Lerp(24f, 62f, lane) +
                    normal * side * MathHelper.Lerp(10f, 36f, lane);
                Vector2 velocity = (aimDirection * speed + normal * side * Main.rand.NextFloat(1.2f, 4.2f)).RotatedByRandom(0.12f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<SHPCNecroplasmDamage>(),
                    damage,
                    0f,
                    projectile.owner);
            }
        }
    }
}
