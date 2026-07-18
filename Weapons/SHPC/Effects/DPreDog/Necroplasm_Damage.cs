using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    // The additional damage orb is intentionally slim. It uses the same three-frame Phantom
    // Spirit silhouette as the main orb, but has its own compact hitbox and much sharper chase.
    internal sealed class SHPCNecroplasmDamage : ModProjectile, ILocalizedModType
    {
        private static readonly Color OuterColor = new(78, 65, 202);
        private static readonly Color CoreColor = new(149, 249, 255);

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (Projectile.velocity.LengthSquared() < 0.01f)
                Projectile.velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * 8f;
        }

        public override void AI()
        {
            if (Projectile.numUpdates == 0)
                Timer++;

            NPC target = Projectile.Center.ClosestNPCAt(2000f);
            Vector2 fallback = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (target is not null && Timer >= 5f)
            {
                float pressure = Utils.GetLerpValue(5f, 28f, Timer, true);
                float targetSpeed = MathHelper.Lerp(14f, 28f, pressure);
                Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center, fallback) * targetSpeed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, MathHelper.Lerp(0.16f, 0.34f, pressure));
            }
            else
                Projectile.velocity *= 0.992f;

            float speed = MathHelper.Clamp(Projectile.velocity.Length(), 5f, 30f);
            Projectile.velocity = Projectile.velocity.SafeNormalize(fallback) * speed;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, CoreColor.ToVector3() * 0.38f);

            if (!Main.dedServ && (int)Timer % 3 == 0)
                SpawnTrail();
        }

        private void SpawnTrail()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                Projectile.Center - direction * Main.rand.NextFloat(3f, 10f) + Main.rand.NextVector2Circular(2f, 2f),
                -direction * Main.rand.NextFloat(0.4f, 1.5f),
                false,
                Main.rand.Next(8, 13),
                Main.rand.NextFloat(0.10f, 0.17f),
                Main.rand.NextBool() ? OuterColor : CoreColor,
                true,
                false));

            Dust dust = Dust.NewDustPerfect(Projectile.Center, (int)CalamityDusts.Necroplasm,
                -direction * Main.rand.NextFloat(0.6f, 1.8f), 100, CoreColor, 0.72f);
            dust.noGravity = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Projectile.velocity.SafeNormalize(Vector2.UnitX) * 0.2f,
                CoreColor,
                new Vector2(0.28f, 0.66f),
                Projectile.rotation,
                0.06f,
                0.56f,
                12));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D ghost = ModContent.Request<Texture2D>("CalamityMod/NPCs/NormalNPCs/PhantomSpirit").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            int frameHeight = ghost.Height / 3;
            int frameIndex = ((int)(Timer / 4f) + Projectile.identity) % 3;
            Rectangle frame = new(0, frameHeight * frameIndex, ghost.Width, frameHeight);
            Vector2 origin = frame.Size() * 0.5f;
            float opacity = Utils.GetLerpValue(0f, 8f, Timer, true) * Utils.GetLerpValue(0f, 14f, Projectile.timeLeft, true);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < Projectile.oldPos.Length; i += 3)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float trailOpacity = (1f - i / (float)Projectile.oldPos.Length) * 0.22f * opacity;
                Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(ghost, trailPosition, frame, OuterColor * trailOpacity, Projectile.rotation * 0.08f, origin, 0.48f, SpriteEffects.None);
            }

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bloom, drawPosition, null, OuterColor * (0.32f * opacity), 0f, bloom.Size() * 0.5f, 0.18f, SpriteEffects.None);
            Main.EntitySpriteDraw(ghost, drawPosition, frame, CoreColor * (0.72f * opacity), Projectile.rotation * 0.06f, origin, 0.56f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
