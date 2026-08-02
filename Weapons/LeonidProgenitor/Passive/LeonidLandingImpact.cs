using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    // This landing passive belongs to Meteorfall: a high fall compresses the starlight into the ground.
    public sealed class LeonidLandingImpactPlayer : ModPlayer
    {
        private const float MinimumFallTiles = 7f;
        private const float MajorFallTiles = 21f;

        private bool wasAirborne;
        private float fallPeakY;

        public override void PostUpdate()
        {
            bool holdingLeonid = Player.HeldItem?.ModItem is LeonidProgenitor;
            TrackLanding(holdingLeonid);
        }

        public override void UpdateDead()
        {
            wasAirborne = false;
            fallPeakY = 0f;
        }

        private void TrackLanding(bool active)
        {
            bool airborne = Math.Abs(Player.velocity.Y) > 0.05f || Player.jump > 0 || Player.fallStart < (int)(Player.position.Y / 16f);
            if (active && airborne)
            {
                if (!wasAirborne)
                    fallPeakY = Player.Bottom.Y;

                wasAirborne = true;
                fallPeakY = Player.gravDir > 0f
                    ? Math.Min(fallPeakY, Player.Bottom.Y)
                    : Math.Max(fallPeakY, Player.Top.Y);
                return;
            }

            if (active && wasAirborne && Player.velocity.Y == 0f && Player.whoAmI == Main.myPlayer)
            {
                float landingY = Player.gravDir > 0f ? Player.Bottom.Y : Player.Top.Y;
                float fallTiles = Math.Abs(landingY - fallPeakY) / 16f;
                if (fallTiles >= MinimumFallTiles)
                    SpawnLandingImpact(fallTiles, fallTiles >= MajorFallTiles);
            }

            if (!airborne || !active)
            {
                wasAirborne = false;
                fallPeakY = Player.Bottom.Y;
            }
        }

        private void SpawnLandingImpact(float fallTiles, bool majorImpact)
        {
            int damage = Math.Max(1, (int)(Player.GetWeaponDamage(Player.HeldItem) * (majorImpact ? 1.4f : 0.7f)));
            float radius = majorImpact ? 300f : 150f;
            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Bottom,
                Vector2.Zero,
                ModContent.ProjectileType<LeonidLandingImpact>(),
                damage,
                majorImpact ? 9f : 5f,
                Player.whoAmI,
                radius,
                fallTiles,
                majorImpact ? 1f : 0f);
        }
    }

    public sealed class LeonidLandingImpact : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private float Radius => Projectile.ai[0] <= 0f ? 150f : Projectile.ai[0];
        private float FallTiles => Math.Max(7f, Projectile.ai[1]);
        private bool MajorImpact => Projectile.ai[2] > 0f;

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
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Projectile.timeLeft >= 20;

        public override void AI()
        {
            if (Projectile.localAI[0] != 0f)
                return;

            Projectile.localAI[0] = 1f;
            Projectile.Resize((int)(Radius * 2f), (int)(Radius * 0.8f));
            Projectile.Damage();
            SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.85f, Pitch = -0.32f }, Projectile.Center);
            ApplyScreenShake();
            SpawnImpactParticles();
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

            Color deepSpace = new(43, 21, 92);
            Color stellarViolet = new(176, 83, 255);
            Color starCore = new(126, 226, 255);
            float power = MajorImpact ? 2f : 1f;

            int squareCount = MajorImpact ? 42 : 22;
            for (int i = 0; i < squareCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 0.5f) * Main.rand.NextFloat(2.5f, 8f) * power;
                GeneralParticleHandler.SpawnParticle(new SquareParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(Radius * 0.24f, 10f),
                    velocity - Vector2.UnitY * Main.rand.NextFloat(0.5f, 3.4f),
                    false,
                    Main.rand.Next(34, 58),
                    Main.rand.NextFloat(1.5f, 3.4f),
                    Color.Lerp(stellarViolet, starCore, Main.rand.NextFloat(0.18f, 0.72f))));
            }

            int sparkCount = MajorImpact ? 34 : 18;
            for (int i = 0; i < sparkCount; i++)
            {
                float side = Main.rand.NextBool() ? -1f : 1f;
                Vector2 velocity = new Vector2(side * Main.rand.NextFloat(3f, 14f) * power, -Main.rand.NextFloat(1.5f, 8f) * power);
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(18f, 5f),
                    velocity,
                    Main.rand.NextBool(3) ? starCore : stellarViolet,
                    deepSpace,
                    Main.rand.NextFloat(0.75f, 1.35f),
                    Main.rand.Next(16, 28)));
            }

            int smokeCount = MajorImpact ? 16 : 8;
            for (int i = 0; i < smokeCount; i++)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(28f, 7f),
                    new Vector2(Main.rand.NextFloatDirection() * 2.4f, -Main.rand.NextFloat(0.8f, 3.4f)) * power,
                    Color.Lerp(deepSpace, new Color(68, 45, 92), Main.rand.NextFloat()),
                    Main.rand.Next(24, 42),
                    Main.rand.NextFloat(0.35f, 0.75f),
                    0.62f,
                    Main.rand.NextFloat(-0.05f, 0.05f),
                    true,
                    required: false));
            }
        }

        private void ApplyScreenShake()
        {
            if (Main.dedServ)
                return;

            float shake = MajorImpact ? 14f : MathHelper.Clamp(4f + (FallTiles - 7f) * 0.32f, 4f, 8f);
            float distanceFactor = Utils.GetLerpValue(1800f, 180f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, shake * distanceFactor);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj").Value;
            float opacity = Utils.GetLerpValue(0f, 22f, Projectile.timeLeft, true) * 0.78f;
            float scale = Radius / 78f;

            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityMod:CircularGradientWithEdge"]
                .UseOpacity(opacity)
                .UseColor(Color.Lerp(new Color(73, 31, 138), new Color(112, 211, 255), MajorImpact ? 0.48f : 0.3f))
                .UseSecondaryColor(new Color(224, 181, 255))
                .UseSaturation(scale)
                .Apply();
            Main.EntitySpriteDraw(pixel, Projectile.Center - Main.screenPosition, null, Color.White,
                0f, pixel.Size() * 0.5f, scale * 156f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
