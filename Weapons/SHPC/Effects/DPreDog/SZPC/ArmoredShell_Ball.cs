using CalamityMod.Particles;
using CalamityMod.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC
{
    internal class ArmoredShell_Ball : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        private const float ExplosionLifetime = 10f;
        private const float ExplosionMaxScale = 2.35f;
        private const float VolterionShotSpeed = 18f;
        private const int VolterionShotLifetime = 12 * 15;

        public ref float OrbType => ref Projectile.ai[0];
        public ref float ExplosionTimer => ref Projectile.ai[1];
        public ref float InwardDirectionX => ref Projectile.localAI[0];
        public ref float InwardDirectionY => ref Projectile.localAI[1];

        public static Asset<Texture2D> Bloom;
        public static Asset<Texture2D> Explosion;

        public override string Texture => "CalamityMod/Projectiles/Magic/VolterionOrb";

        private Vector2 InwardDirection =>
            new Vector2(InwardDirectionX, InwardDirectionY).SafeNormalize(-Projectile.velocity.SafeNormalize(Vector2.UnitX));

        public override void Load()
        {
            Bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Explosion = ModContent.Request<Texture2D>("CalamityMod/Particles/PlasmaExplosion");
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 38;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 40;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(InwardDirectionX);
            writer.Write(InwardDirectionY);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            InwardDirectionX = reader.ReadSingle();
            InwardDirectionY = reader.ReadSingle();
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 4 % Main.projFrames[Type];

            Lighting.AddLight(Projectile.Center, GetColor(OrbType).ToVector3());

            if (ExplosionTimer > 0f)
            {
                if (ExplosionTimer == 1f)
                    SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.55f, Pitch = 0.15f }, Projectile.Center);

                float progress = ExplosionTimer / ExplosionLifetime;
                float scaleLevel = PiecewiseAnimation(progress, new CurveSegment[]
                {
                    new CurveSegment(EasingType.PolyOut, 0f, 0f, 1f, 3)
                });

                Projectile.scale = MathHelper.Lerp(1f, ExplosionMaxScale, scaleLevel);
                Projectile.Opacity = 1f - progress;
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation += 0.08f;
                Projectile.friendly = false;

                if (ExplosionTimer++ >= ExplosionLifetime)
                    Projectile.Kill();

                return;
            }

            Projectile.rotation += 0.01f;
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * Projectile.velocity.Length();
            Projectile.Opacity = 1f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            TriggerSmallExplosion();
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            TriggerSmallExplosion();
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 inwardDirection = InwardDirection;
            SpawnDeathFanFX(inwardDirection);

            if (Projectile.owner != Main.myPlayer)
                return;

            Projectile lightning = Projectile.NewProjectileDirect(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                inwardDirection * VolterionShotSpeed,
                ModContent.ProjectileType<VolterionShot>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                1f
            );
            lightning.timeLeft = VolterionShotLifetime;
        }

        private void TriggerSmallExplosion()
        {
            if (ExplosionTimer > 0f)
                return;

            ExplosionTimer = 1f;
            Projectile.penetrate = -1;
            Projectile.netUpdate = true;
        }

        private void SpawnDeathFanFX(Vector2 inwardDirection)
        {
            Color color = GetColor(OrbType);
            Vector2 normal = inwardDirection.RotatedBy(MathHelper.PiOver2);

            for (int i = -3; i <= 3; i++)
            {
                float spread = i / 3f;
                Vector2 fanDirection = inwardDirection.RotatedBy(spread * 0.5f).SafeNormalize(inwardDirection);
                Vector2 fanVelocity = fanDirection * (4.4f + (1f - MathF.Abs(spread)) * 2.6f) + normal * (spread * 1.3f);

                BoltParticle bolt = new BoltParticle(
                    Projectile.Center + fanDirection * 4f,
                    fanVelocity,
                    false,
                    12 + (3 - Math.Abs(i)),
                    0.2f + (1f - MathF.Abs(spread)) * 0.08f,
                    Color.Lerp(color, Color.White, 0.18f),
                    new Vector2(0.34f, 0.92f + (1f - MathF.Abs(spread)) * 0.18f),
                    true
                );
                GeneralParticleHandler.SpawnParticle(bolt);

                Dust chemicalDust = Dust.NewDustPerfect(
                    Projectile.Center + fanDirection * 6f,
                    DustID.FireworksRGB,
                    fanVelocity * 0.7f
                );
                chemicalDust.noGravity = true;
                chemicalDust.noLight = true;
                chemicalDust.scale = 1f + (1f - MathF.Abs(spread)) * 0.18f;
                chemicalDust.fadeIn = 1.12f;
                chemicalDust.color = Color.Lerp(color, new Color(185, 255, 240), 0.35f);
            }

            for (int i = 0; i < 5; i++)
            {
                float angleOffset = MathHelper.Lerp(-0.32f, 0.32f, i / 4f);
                Vector2 puffDirection = inwardDirection.RotatedBy(angleOffset).SafeNormalize(inwardDirection);
                Vector2 puffVelocity = puffDirection * (2.1f + i * 0.45f);

                Dust mistDust = Dust.NewDustPerfect(
                    Projectile.Center + puffDirection * (4f + i),
                    DustID.Smoke,
                    puffVelocity
                );
                mistDust.noGravity = true;
                mistDust.scale = 1.1f + i * 0.08f;
                mistDust.fadeIn = 1.18f;
                mistDust.color = Color.Lerp(color, new Color(124, 255, 211), 0.42f);
            }
        }

        public static Color GetColor(float type) =>
            Color.Lerp(new Color(51, 197, 255), new Color(143, 51, 255), 0.2f + 0.15f * type + 0.2f * MathF.Sin(Main.GlobalTimeWrappedHourly * 10f));

        public override Color? GetAlpha(Color lightColor) => GetColor(OrbType);

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Color color = Projectile.GetAlpha(lightColor);

            if (ExplosionTimer > 0f)
            {
                Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
                Texture2D explosionTex = Explosion.Value;
                Main.EntitySpriteDraw(explosionTex, drawPos, null, color * Projectile.Opacity, Projectile.rotation, explosionTex.Size() * 0.5f, 0.02f * Projectile.scale, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
                return false;
            }

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            Texture2D bloomTex = Bloom.Value;
            Main.EntitySpriteDraw(bloomTex, drawPos, null, color * 0.5f, 0f, bloomTex.Size() * 0.5f, 0.42f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return true;
        }

        public override bool? CanDamage() => ExplosionTimer > 0f ? false : base.CanDamage();

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CircularHitboxCollision(Projectile.Center, 20 * Projectile.scale, targetHitbox);
    }
}
