using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.TheEnforcer
{
    internal sealed class TheNewEnforcerSlash : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 18;
        private const int BaseSize = 56;

        private float SlashScale => Projectile.ai[0] <= 0f ? 1f : Projectile.ai[0];
        private float SlashRotation => Projectile.ai[1];

        public new string LocalizationCategory => "Projectiles.TheEnforcer";
        public override string Texture => "Terraria/Images/Extra_98";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = BaseSize;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = Lifetime;
            Projectile.MaxUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10 * Projectile.MaxUpdates;
            Projectile.alpha = 255;
        }

        public override void OnSpawn(IEntitySource source)
        {
            ResizeToScale(SlashScale);
            Projectile.rotation = SlashRotation;
            Projectile.Opacity = 1f;
            SpawnInitialBurst();
        }

        public override void AI()
        {
            Projectile.rotation = SlashRotation;
            Projectile.velocity *= 0.86f;

            float lifeProgress = 1f - Projectile.timeLeft / (float)Lifetime;
            Projectile.Opacity = MathF.Sin(Utils.GetLerpValue(0f, 1f, lifeProgress, true) * MathHelper.Pi);

            if (Main.rand.NextBool(3))
            {
                Vector2 direction = Projectile.rotation.ToRotationVector2();
                Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + direction * Main.rand.NextFloat(-BaseSize, BaseSize) * Projectile.scale + normal * Main.rand.NextFloat(-8f, 8f) * Projectile.scale,
                    Main.rand.NextBool() ? DustID.Shadowflame : DustID.BlueTorch,
                    normal * Main.rand.NextFloat(-1.5f, 1.5f) - direction * Main.rand.NextFloat(0.4f, 1.3f),
                    100,
                    Main.rand.NextBool() ? new Color(126, 58, 255) : new Color(70, 220, 255),
                    Main.rand.NextFloat(0.75f, 1.15f));
                dust.noGravity = true;
            }
        }

        private void ResizeToScale(float scale)
        {
            Vector2 center = Projectile.Center;
            int size = (int)(BaseSize * scale);
            Projectile.width = Projectile.height = size;
            Projectile.scale = scale;
            Projectile.Center = center;
        }

        private void SpawnInitialBurst()
        {
            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.rotation.ToRotationVector2();
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 5; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(1.4f, 4.6f) + normal * Main.rand.NextFloat(-1.5f, 1.5f);
                Color color = Color.Lerp(new Color(126, 58, 255), new Color(70, 220, 255), Main.rand.NextFloat(0.12f, 0.55f));
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    velocity,
                    false,
                    Main.rand.Next(8, 13),
                    0.065f * Projectile.scale,
                    color,
                    new Vector2(2.4f, 0.55f),
                    true));
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = Projectile.rotation.ToRotationVector2();
            float halfLength = BaseSize * SlashScale * 1.05f;
            float lineWidth = BaseSize * SlashScale * 0.2f;
            Vector2 start = Projectile.Center - direction * halfLength;
            Vector2 end = Projectile.Center + direction * halfLength;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, lineWidth, ref collisionPoint);
        }

        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            int size = (int)(BaseSize * SlashScale);
            hitbox = new Rectangle((int)Projectile.Center.X - size / 2, (int)Projectile.Center.Y - size / 2, size, size);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Extra[ExtrasID.SharpTears].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color mainColor = Color.Lerp(new Color(112, 48, 255), new Color(76, 230, 255), 0.28f);
            mainColor.A = 0;
            Color coreColor = Color.White;
            coreColor.A = 0;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                mainColor * 0.18f * Projectile.Opacity,
                0f,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.5f,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                mainColor * Projectile.Opacity,
                Projectile.rotation,
                origin,
                new Vector2(2.05f, 0.34f) * Projectile.scale,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                coreColor * 0.58f * Projectile.Opacity,
                Projectile.rotation,
                origin,
                new Vector2(1.08f, 0.1f) * Projectile.scale,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
