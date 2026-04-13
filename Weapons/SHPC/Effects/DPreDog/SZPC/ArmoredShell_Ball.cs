using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
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

        public ref float OrbType => ref Projectile.ai[0];
        public ref float ExplosionTimer => ref Projectile.ai[1];

        public static Asset<Texture2D> Bloom;
        public static Asset<Texture2D> Explosion;

        public override string Texture => "CalamityMod/Projectiles/Magic/VolterionOrb";

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
            Projectile.timeLeft = 240;
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

        private void TriggerSmallExplosion()
        {
            if (ExplosionTimer > 0f)
                return;

            ExplosionTimer = 1f;
            Projectile.penetrate = -1;
            Projectile.netUpdate = true;
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
