using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.Passive
{
    public class VesuviusPassivePlayer : ModPlayer
    {
        public int LeftClickCooldown;

        private bool holdingVesuvius;
        private bool wasAirborne;
        private float strongestFallSpeed;
        private int ashTimer;

        public override void ResetEffects()
        {
            holdingVesuvius = false;
        }

        public override void PostUpdate()
        {
            if (LeftClickCooldown > 0)
                LeftClickCooldown--;

            if (!holdingVesuvius)
            {
                ashTimer = 0;
                TrackLanding(false);
                return;
            }

            Player.noFallDmg = true;
            ImproveFallSpeed();
            SpawnAmbientAsh();
            TrackLanding(true);
        }

        public void SetHoldingVesuvius()
        {
            holdingVesuvius = true;
        }

        private void ImproveFallSpeed()
        {
            float fallingVelocity = Player.velocity.Y * Player.gravDir;
            if (fallingVelocity <= 0.2f)
                return;

            Player.maxFallSpeed = Math.Max(Player.maxFallSpeed, Player.controlDown ? 40f : 24f);
            Player.gravity = Math.Max(Player.gravity, Player.controlDown ? 1.16f : 0.66f);

            if (Player.controlDown && !Player.controlJump)
                Player.velocity.Y += Player.gravDir * 0.42f;
        }

        private void SpawnAmbientAsh()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            ashTimer++;
            if (ashTimer < 6)
                return;

            ashTimer = 0;
            int damage = 1;
            if (Player.HeldItem?.ModItem is NewVesuvius)
                damage = Math.Max(1, (int)(Player.GetWeaponDamage(Player.HeldItem) * 0.12f));

            for (int i = 0; i < 3; i++)
            {
                Vector2 spawnPosition = Player.Center + new Vector2(Main.rand.NextFloat(-520f, 520f), Main.rand.NextFloat(-260f, -100f));
                Vector2 velocity = new Vector2(-Player.direction * Main.rand.NextFloat(0.55f, 1.35f), Main.rand.NextFloat(0.75f, 1.55f));
                Projectile.NewProjectile(
                    Player.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<VesuviusAshFall>(),
                    damage,
                    0f,
                    Player.whoAmI,
                    -Player.direction);
            }
        }

        private void TrackLanding(bool active)
        {
            bool airborne = Math.Abs(Player.velocity.Y) > 0.05f || Player.jump > 0 || Player.fallStart < (int)(Player.position.Y / 16f);
            float fallSpeed = Player.velocity.Y * Player.gravDir;

            if (active && airborne && fallSpeed > 0.5f)
            {
                wasAirborne = true;
                strongestFallSpeed = Math.Max(strongestFallSpeed, fallSpeed);
                return;
            }

            if (active && wasAirborne && Player.velocity.Y == 0f && strongestFallSpeed >= 4f && Player.whoAmI == Main.myPlayer)
                SpawnLandingQuake(strongestFallSpeed);

            if (!airborne || !active)
            {
                wasAirborne = false;
                strongestFallSpeed = 0f;
            }
        }

        private void SpawnLandingQuake(float fallSpeed)
        {
            int damage = 1;
            if (Player.HeldItem?.ModItem is NewVesuvius)
                damage = Math.Max(1, (int)(Player.GetWeaponDamage(Player.HeldItem) * MathHelper.Clamp(0.35f + fallSpeed / 32f, 0.5f, 1.65f)));

            float radius = MathHelper.Clamp(90f + fallSpeed * 13f, 110f, 330f);
            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Bottom,
                Vector2.Zero,
                ModContent.ProjectileType<VesuviusLandingQuake>(),
                damage,
                5f + fallSpeed * 0.12f,
                Player.whoAmI,
                radius,
                fallSpeed);
        }
    }

    public class VesuviusAshFall : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.velocity.X += Projectile.ai[0] * 0.012f;
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.04f, 0.8f, 5.5f);
            Projectile.rotation += Projectile.velocity.X * 0.02f + 0.025f;
            Projectile.alpha = (int)MathHelper.Lerp(0f, 220f, Utils.GetLerpValue(42f, 0f, Projectile.timeLeft, true));

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Particle smoke = new HeavySmokeParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    Projectile.velocity * Main.rand.NextFloat(0.04f, 0.18f) + Main.rand.NextVector2Circular(0.12f, 0.12f),
                    Color.Lerp(new Color(58, 50, 44), new Color(92, 76, 60), Main.rand.NextFloat()),
                    Main.rand.Next(12, 24),
                    Main.rand.NextFloat(0.06f, 0.14f),
                    0.52f,
                    Main.rand.NextFloat(-0.04f, 0.04f),
                    false,
                    required: false);
                GeneralParticleHandler.SpawnParticle(smoke);

                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                        DustID.Smoke,
                        Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.18f, 0.18f),
                        175,
                        Color.Lerp(new Color(54, 46, 38), new Color(78, 64, 50), Main.rand.NextFloat()),
                        Main.rand.NextFloat(0.14f, 0.32f));
                    d.noGravity = true;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 120);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    public class VesuviusLandingQuake : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private float Radius => Projectile.ai[0] <= 0f ? 150f : Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 160;
            Projectile.height = 160;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 28;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Projectile.timeLeft >= 20;

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.Resize((int)(Radius * 2f), (int)(Radius * 0.8f));
                Projectile.Damage();
                SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.85f, Pitch = -0.32f }, Projectile.Center);
                ApplyScreenShake(Projectile.ai[1]);
                SpawnImpactParticles();
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(targetHitbox.Center.ToVector2(), Projectile.Center - new Vector2(Radius, Radius * 0.38f), Projectile.Center + new Vector2(Radius, Radius * 0.38f));
            return Vector2.Distance(closest, targetHitbox.Center.ToVector2()) < 12f ||
                   Math.Abs(targetHitbox.Center.X - Projectile.Center.X) <= Radius && Math.Abs(targetHitbox.Center.Y - Projectile.Center.Y) <= Radius * 0.5f;
        }

        private void SpawnImpactParticles()
        {
            if (Main.dedServ)
                return;

            float fallSpeed = Projectile.ai[1];

            // 1. Spawn flat transparent shockwaves as CustomPulse particles
            // Foggy circle center shockwave
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                new Color(255, 90, 20) * 0.7f,
                "CalamityMod/Particles/HighResFoggyCircleHardEdge",
                new Vector2(1f, 0.15f),
                0f,
                0.02f,
                Radius * 2.2f / 2048f,
                22,
                true
            ));

            // Hollow circle ring shockwave
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                new Color(255, 140, 30) * 0.8f,
                "CalamityMod/Particles/HighResHollowCircleHardEdge",
                new Vector2(1f, 0.12f),
                0f,
                0.01f,
                Radius * 2.4f / 2048f,
                26,
                true
            ));

            // Central bloom flash
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                new Color(255, 60, 10) * 0.5f,
                "CalamityMod/Particles/BloomCircle",
                new Vector2(1f, 0.25f),
                0f,
                0.05f,
                Radius * 1.5f / 128f,
                18,
                true
            ));

            // 2. Spawn upward radiating burst particles (semi-circular)
            int sparkCount = (int)MathHelper.Clamp(Radius * 0.16f, 16f, 50f);
            for (int i = 0; i < sparkCount; i++)
            {
                float angle = Main.rand.NextFloat(-MathHelper.Pi, 0f);
                Vector2 direction = angle.ToRotationVector2();
                float speed = Main.rand.NextFloat(3f, 15f) * (0.6f + fallSpeed * 0.04f);
                Vector2 velocity = direction * speed;

                if (Main.rand.NextBool())
                {
                    Color sparkColor = Color.Lerp(new Color(255, 110, 24), new Color(255, 192, 72), Main.rand.NextFloat());
                    Color bloomColor = new Color(255, 50, 10) * 0.5f;
                    float scale = Main.rand.NextFloat(0.9f, 1.5f);
                    int lifetime = Main.rand.Next(15, 26);
                    GeneralParticleHandler.SpawnParticle(new CritSpark(
                        Projectile.Center + Main.rand.NextVector2Circular(16f, 4f),
                        velocity,
                        sparkColor,
                        bloomColor,
                        scale,
                        lifetime));
                }
                else
                {
                    Color sparkColor = Color.Lerp(new Color(255, 150, 30), new Color(255, 220, 60), Main.rand.NextFloat());
                    float scale = Main.rand.NextFloat(0.8f, 1.4f);
                    int lifetime = Main.rand.Next(12, 22);
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(16f, 4f),
                        velocity,
                        false,
                        lifetime,
                        scale,
                        sparkColor));
                }
            }

            // 3. Spawn upward radiating ash and scoria smoke (semi-circular)
            int smokeCount = (int)MathHelper.Clamp(Radius * 0.08f, 8f, 22f);
            for (int i = 0; i < smokeCount; i++)
            {
                float angle = Main.rand.NextFloat(-MathHelper.Pi * 0.85f, -MathHelper.Pi * 0.15f);
                Vector2 direction = angle.ToRotationVector2();
                float speed = Main.rand.NextFloat(2f, 7f) * (0.6f + fallSpeed * 0.04f);
                Vector2 velocity = direction * speed;

                Color smokeColor = Color.Lerp(new Color(74, 48, 40), new Color(88, 84, 80), Main.rand.NextFloat());
                float scale = Main.rand.NextFloat(0.6f, 1.2f);
                int lifetime = Main.rand.Next(20, 42);
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(24f, 6f),
                    velocity,
                    smokeColor,
                    lifetime,
                    scale,
                    0.65f,
                    Main.rand.NextFloat(-0.05f, 0.05f),
                    true,
                    required: false));
            }

            // 4. Spawn upward radiating glow orbs
            int orbCount = (int)MathHelper.Clamp(Radius * 0.04f, 4f, 12f);
            for (int i = 0; i < orbCount; i++)
            {
                float angle = Main.rand.NextFloat(-MathHelper.Pi, 0f);
                Vector2 direction = angle.ToRotationVector2();
                float speed = Main.rand.NextFloat(2.5f, 9f) * (0.6f + fallSpeed * 0.04f);
                Vector2 velocity = direction * speed;

                Color orbColor = Color.Lerp(new Color(255, 88, 24), new Color(255, 242, 190), Main.rand.NextFloat(0f, 0.7f));
                float scale = Main.rand.NextFloat(0.35f, 0.75f);
                int lifetime = Main.rand.Next(14, 28);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 4f),
                    velocity,
                    false,
                    lifetime,
                    scale,
                    orbColor));
            }

            // 5. Spawn some localized ground-based debris/dust (no metaballs)
            int dustCount = (int)MathHelper.Clamp(Radius * 0.12f, 12f, 36f);
            for (int i = 0; i < dustCount; i++)
            {
                float offset = MathHelper.Lerp(-Radius, Radius, i / (float)Math.Max(1, dustCount - 1));
                Vector2 position = Projectile.Center + Vector2.UnitX * offset + new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-4f, 4f));
                
                // Ground smoke/rock dust
                Dust dust = Dust.NewDustPerfect(
                    position, 
                    Main.rand.NextBool(3) ? DustID.Torch : DustID.Smoke, 
                    new Vector2(Math.Sign(offset) * Main.rand.NextFloat(0.5f, 3f), -Main.rand.NextFloat(1.5f, 5f)), 
                    110, 
                    Color.Lerp(Color.DarkGray, Color.OrangeRed, 0.2f), 
                    Main.rand.NextFloat(0.8f, 1.6f));
                dust.noGravity = Main.rand.NextBool();
            }
        }

        private void ApplyScreenShake(float fallSpeed)
        {
            if (Main.dedServ)
                return;

            float shake = MathHelper.Clamp(3f + fallSpeed * 0.35f, 4f, 16f);
            float distanceFactor = Utils.GetLerpValue(1800f, 180f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, shake * distanceFactor);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
