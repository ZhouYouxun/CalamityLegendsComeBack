using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal class BBSwing_Slash : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.NewWeapons.DPreDog";
        public override string Texture => "Terraria/Images/Extra_98";

        private const int Lifetime = 24;
        private const int BaseSize = 54;

        private float SlashScale => Projectile.ai[0] <= 0f ? 1f : Projectile.ai[0];
        private float SlashRotationOffset => Projectile.ai[1];

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
            Projectile.rotation = Projectile.velocity.ToRotation() + SlashRotationOffset;
            Projectile.Opacity = 1f;
        }

        private void ResizeToScale(float scale)
        {
            Vector2 center = Projectile.Center;
            int size = (int)(BaseSize * scale);
            Projectile.width = Projectile.height = size;
            Projectile.scale = scale;
            Projectile.Center = center;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + SlashRotationOffset;

            float lifeProgress = 1f - Projectile.timeLeft / (float)Lifetime;
            float fadeCurve = (float)System.Math.Sin(Utils.GetLerpValue(0f, 1f, lifeProgress, true) * MathHelper.Pi);
            Projectile.Opacity = fadeCurve;

            if (Projectile.timeLeft >= Lifetime - 1)
            {
                SpawnInitialBurst();
            }
        }

        private void SpawnInitialBurst()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 6; i++)
            {
                Vector2 dustVelocity =
                    forward.RotatedBy(Main.rand.NextFloat(-0.55f, 0.55f)) * Main.rand.NextFloat(2f, 5.5f) +
                    right * Main.rand.NextFloat(-1.2f, 1.2f);

                Dust water = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Water,
                    dustVelocity,
                    100,
                    new Color(75, 175, 255),
                    Main.rand.NextFloat(0.85f, 1.2f) * SlashScale);
                water.noGravity = true;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(
                        Projectile.Center,
                        DustID.Frost,
                        dustVelocity * 0.65f,
                        100,
                        new Color(210, 248, 255),
                        Main.rand.NextFloat(0.7f, 1f) * SlashScale);
                    frost.noGravity = true;
                }
            }

            GlowSparkParticle spark = new GlowSparkParticle(
                Projectile.Center,
                forward * 0.25f,
                false,
                10,
                0.12f * SlashScale,
                Color.White,
                new Vector2(3f, 0.6f),
                true);
            GeneralParticleHandler.SpawnParticle(spark);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = Projectile.rotation.ToRotationVector2();
            float halfLength = BaseSize * SlashScale * 0.95f;
            float lineWidth = BaseSize * SlashScale * 0.18f;
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

        public override Color? GetAlpha(Color lightColor) =>
            Color.Lerp(new Color(60, 170, 255, 0), Color.White, 0.28f) * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Extra[98].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color slashColor = Color.Lerp(new Color(60, 170, 255, 0), new Color(220, 250, 255, 0), 0.35f) * Projectile.Opacity;
            Color coreColor = new Color(220, 250, 255, 0) * Projectile.Opacity * 0.65f;

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                slashColor,
                Projectile.rotation,
                origin,
                new Vector2(1.9f, 0.34f) * Projectile.scale,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                coreColor,
                Projectile.rotation + MathHelper.PiOver2,
                origin,
                new Vector2(0.85f, 0.12f) * Projectile.scale,
                SpriteEffects.None,
                0);

            return false;
        }
    }
}
