using CalamityMod;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
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
            if (ashTimer < 14)
                return;

            ashTimer = 0;
            int damage = 1;
            if (Player.HeldItem?.ModItem is NewVesuvius)
                damage = Math.Max(1, (int)(Player.GetWeaponDamage(Player.HeldItem) * 0.12f));

            Vector2 spawnPosition = Player.Center + new Vector2(Main.rand.NextFloat(-210f, 210f), Main.rand.NextFloat(-220f, -120f));
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
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.012f, 0.35f, 2.2f);
            Projectile.rotation += Projectile.velocity.X * 0.02f + 0.025f;
            Projectile.alpha = (int)MathHelper.Lerp(0f, 220f, Utils.GetLerpValue(42f, 0f, Projectile.timeLeft, true));

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Particle ash = new SquareAshParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(0.25f, 0.25f),
                    Main.rand.Next(32, 54),
                    Main.rand.NextFloat(0.36f, 0.74f),
                    Color.Lerp(Color.DimGray, Color.OrangeRed, 0.15f));
                GeneralParticleHandler.SpawnParticle(ash);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 120);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Color color = Color.Lerp(Color.DarkGray, Color.OrangeRed, 0.18f) * Projectile.Opacity;
            Main.EntitySpriteDraw(
                pixel,
                Projectile.Center - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                Projectile.rotation,
                new Vector2(0.5f),
                new Vector2(8f, 8f) * Projectile.scale,
                SpriteEffects.None);
            return false;
        }
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

            int count = (int)MathHelper.Clamp(Radius / 9f, 14f, 42f);
            for (int i = 0; i < count; i++)
            {
                float offset = MathHelper.Lerp(-Radius, Radius, i / (float)Math.Max(1, count - 1));
                Vector2 position = Projectile.Center + Vector2.UnitX * offset + new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-6f, 6f));
                Dust dust = Dust.NewDustPerfect(position, Main.rand.NextBool(3) ? DustID.Torch : DustID.Smoke, new Vector2(Math.Sign(offset) * Main.rand.NextFloat(1f, 4f), -Main.rand.NextFloat(2f, 7f)), 120, Color.Lerp(Color.Gray, Color.OrangeRed, 0.28f), Main.rand.NextFloat(0.9f, 1.8f));
                dust.noGravity = Main.rand.NextBool(2);

                if (i % 4 == 0)
                    RancorLavaMetaball.SpawnParticle(position, Main.rand.NextFloat(24f, 48f));
            }

            GeneralParticleHandler.SpawnParticle(new PulseRing(
                Projectile.Center,
                Vector2.Zero,
                new Color(255, 110, 35) * 0.75f,
                0.12f,
                Radius / 70f,
                24));
        }

        private void ApplyScreenShake(float fallSpeed)
        {
            if (Main.dedServ)
                return;

            float shake = MathHelper.Clamp(3f + fallSpeed * 0.35f, 4f, 16f);
            float distanceFactor = Utils.GetLerpValue(1800f, 180f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, shake * distanceFactor);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float progress = 1f - Projectile.timeLeft / 28f;
            float fade = Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);
            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                new Color(255, 100, 35, 0) * 0.18f * fade,
                0f,
                bloom.Size() * 0.5f,
                new Vector2(Radius / bloom.Width, Radius * 0.22f / bloom.Height) * (0.8f + progress * 0.6f),
                SpriteEffects.None);
            return false;
        }
    }
}
