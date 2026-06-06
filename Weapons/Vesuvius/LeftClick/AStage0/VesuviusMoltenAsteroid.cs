using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.AStage0
{
    public class VesuviusMoltenAsteroid : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/Magic/AsteroidMolten";

        private int Variant => (int)MathHelper.Clamp(Projectile.ai[0], 0f, 5f);
        private bool NoLargeExplosion => Projectile.ai[2] == 1f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale = Projectile.ai[1] <= 0f ? 0.7f : Projectile.ai[1];
                Projectile.localAI[0] = 1f;
            }

            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
            {
                Player owner = Main.player[Projectile.owner];
                if (Projectile.position.Y > owner.position.Y - 300f || Projectile.position.Y < Main.worldSurface * 16.0)
                    Projectile.tileCollide = true;
            }

            Projectile.rotation += Projectile.velocity.X * 2f;
            if (NoLargeExplosion)
                Projectile.velocity *= 0.985f;

            Color glow = Color.Lerp(new Color(255, 80, 20), new Color(255, 220, 80), Main.rand.NextFloat(0.25f, 0.65f));
            Lighting.AddLight(Projectile.Center, glow.ToVector3() * 0.28f * Projectile.scale * VesuviusProjectileVisuals.VisualIntensity);

            SpawnAsteroidMoltenDust();
            VesuviusProjectileVisuals.SpawnMoltenMeteorTrail(Projectile, NoLargeExplosion ? 0.82f : 1.08f, !NoLargeExplosion);
        }

        private void SpawnAsteroidMoltenDust()
        {
            if (Main.dedServ || Projectile.velocity.LengthSquared() <= 0.01f)
                return;

            Vector2 position = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 10f;
            for (int side = -1; side <= 1; side += 2)
            {
                Dust flaming = Main.dust[Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.CopperCoin, 0f, 0f, 0, new Color(255, Main.DiscoG, 0), 1f)];
                flaming.position = position;
                flaming.velocity = Projectile.velocity.RotatedBy(side * MathHelper.PiOver2) * 0.33f + Projectile.velocity / 4f;
                flaming.position += Projectile.velocity.RotatedBy(side * MathHelper.PiOver2);
                flaming.fadeIn = 0.5f;
                flaming.noGravity = true;
            }

            int fiery = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.CopperCoin, 0f, 0f, 0, new Color(255, Main.DiscoG, 0), 1f);
            Main.dust[fiery].velocity *= 0.5f;
            Main.dust[fiery].scale *= 1.3f;
            Main.dust[fiery].fadeIn = 1f;
            Main.dust[fiery].noGravity = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
        }

        public override void OnKill(int timeLeft)
        {
            SpawnImpactEffects(NoLargeExplosion ? 0.55f : 1f);

            if (NoLargeExplosion || Projectile.owner != Main.myPlayer)
                return;

            Vector2 oldCenter = Projectile.Center;
            Projectile.position = oldCenter;
            Projectile.width = Projectile.height = (int)(92f * Projectile.scale);
            Projectile.Center = oldCenter;
            Projectile.penetrate = -1;
            Projectile.maxPenetrate = -1;
            Projectile.Damage();
        }

        private void SpawnImpactEffects(float strength)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.45f * strength, Pitch = -0.05f }, Projectile.Center);

            Color color = new Color(255, 96, 32);
            VesuviusProjectileVisuals.SpawnMoltenImpact(Projectile.Center, strength * Projectile.scale, !NoLargeExplosion);
            int dustCount = Math.Max(1, (int)Math.Ceiling(12f * strength * VesuviusProjectileVisuals.VisualIntensity));
            for (int i = 0; i < dustCount; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? DustID.InfernoFork : DustID.Smoke, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f) * strength * VesuviusProjectileVisuals.VisualScale, 90, color, Main.rand.NextFloat(0.8f, 1.5f) * VesuviusProjectileVisuals.VisualScale);
                dust.noGravity = Main.rand.NextBool();
            }

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                color * 0.72f * VesuviusProjectileVisuals.VisualIntensity,
                "CalamityMod/Particles/ShatteredExplosion",
                Vector2.One,
                Main.rand.NextFloat(-0.3f, 0.3f),
                0.08f,
                0.34f * strength * Projectile.scale * VesuviusProjectileVisuals.VisualScale,
                15));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = GetAsteroidTexture(false);
            Color trailColor = Color.Lerp(lightColor, VesuviusProjectileVisuals.LavaOrange, 0.42f) * VesuviusProjectileVisuals.VisualIntensity;
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], trailColor, 1, texture);
            return false;
        }

        public override void PostDraw(Color lightColor)
        {
            Texture2D glow = GetAsteroidTexture(true);
            if (glow == null)
                return;

            Main.EntitySpriteDraw(
                glow,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White * VesuviusProjectileVisuals.VisualIntensity,
                Projectile.rotation,
                glow.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None);
        }

        private Texture2D GetAsteroidTexture(bool glow)
        {
            if (glow && Variant == 4)
                return null;

            string suffix = Variant == 0 ? string.Empty : (Variant + 1).ToString();
            string path = glow
                ? $"CalamityMod/Projectiles/Magic/AsteroidMoltenGlow{suffix}"
                : $"CalamityMod/Projectiles/Magic/AsteroidMolten{suffix}";

            return ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;
        }
    }
}
