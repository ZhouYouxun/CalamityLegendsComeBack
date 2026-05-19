using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Graphics.Metaballs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    internal class AshesofCalamity_Soul : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const float MainSoulMarker = 1f;
        private bool IsMainSoul => Projectile.ai[0] == MainSoulMarker;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 52;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (IsMainSoul)
                Projectile.timeLeft = 260;
            else
                Projectile.timeLeft = 46;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 9)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }

            if (Projectile.frame >= 6)
                Projectile.frame = 0;

            if (Projectile.alpha > 5)
                Projectile.alpha -= 15;
            if (Projectile.alpha < 5)
                Projectile.alpha = 5;

            if (IsMainSoul)
                MainSoulAI();
            else
                SubSoulAI();

            Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() +
                                  (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi) -
                                  MathHelper.ToRadians(90f) * Projectile.direction;

            Lighting.AddLight(Projectile.Center, 0.65f, 0f, 0f);
            SpawnFireShape();
        }

        private void MainSoulAI()
        {
            NPC target = Projectile.Center.ClosestNPCAt(1050f);
            Vector2 fallbackDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float desiredSpeed = MathHelper.Clamp(Projectile.velocity.Length() * 1.012f + 0.12f, 16f, 27f);

            if (target != null)
            {
                Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(fallbackDirection) * desiredSpeed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.095f);
            }
            else
                Projectile.velocity = fallbackDirection * desiredSpeed;
        }

        private void SubSoulAI()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float speed = MathHelper.Min(Projectile.velocity.Length() * 1.01f, 18f);
            Projectile.velocity = direction * speed;
        }

        private void SpawnFireShape()
        {
            float soulScale = IsMainSoul ? 1.2f : 0.82f;

            Dust brimDust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(26f, 26f) * soulScale,
                (int)CalamityDusts.Brimstone,
                null,
                170,
                default,
                0.95f * soulScale
            );
            brimDust.noGravity = true;
            brimDust.velocity *= 0.45f;
            brimDust.velocity += Projectile.velocity * 0.08f;

            CalamitasMetaball.SpawnParticle(
                Projectile.Center + Projectile.velocity * 0.75f,
                Main.rand.NextVector2Circular(2f, 2f),
                52f * soulScale
            );
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < 2; i++)
            {
                CalamitasMetaball.SpawnParticle(
                    target.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2Circular(2f, 2f),
                    58f
                );
            }

            if (!IsMainSoul || Projectile.owner != Main.myPlayer)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<AshesofCalamity_Portal>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner
            );
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(250, 50, 50, Projectile.alpha);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/LargeBloom").Value;
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = 1f - Projectile.alpha / 255f;
            float scale = IsMainSoul ? 0.36f : 0.25f;
            Color brimstone = new Color(255, 45, 32) * opacity;
            Color ember = new Color(80, 0, 0) * opacity;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float trailCompletion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 oldDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(
                    bloom,
                    oldDrawPosition,
                    null,
                    ember * trailCompletion * 0.45f,
                    Projectile.rotation,
                    bloom.Size() * 0.5f,
                    scale * trailCompletion,
                    SpriteEffects.None
                );
            }

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                ember * 0.92f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                scale * 1.3f,
                SpriteEffects.None
            );

            Main.EntitySpriteDraw(
                spark,
                drawPosition,
                null,
                brimstone,
                Projectile.rotation,
                spark.Size() * 0.5f,
                new Vector2(scale * 0.95f, scale * 1.55f),
                SpriteEffects.None
            );

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                Color.White * opacity * 0.52f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                scale * 0.42f,
                SpriteEffects.None
            );

            return false;
        }
    }
}
