using CalamityMod;
using CalamityMod.Particles;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.Skill
{
    internal sealed class BBDrinkingFountainOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private float Timer => Projectile.localAI[0];
        private Player Owner => Main.player[Projectile.owner];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 1;
        }

        public override bool? CanDamage() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            SpawnRecoveryFlash(Projectile.Center, 1.05f);
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Vector2 destination = Owner.Center;

            Vector2 desiredVelocity = Projectile.SafeDirectionTo(destination) * 13f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.12f);
            Projectile.rotation += 0.22f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.1f, 0.42f, 0.62f));

            if (Projectile.Hitbox.Intersects(Owner.Hitbox))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Owner.Heal(9);
                Projectile.Kill();
                return;
            }

            if (!Main.dedServ && (int)Timer % 7 == 0)
            {
                Color glow = Color.Lerp(new Color(105, 227, 255), Color.White, Main.rand.NextFloat(0.15f, 0.45f));
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center, -Projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.25f, 0.9f),
                    false, Main.rand.Next(10, 16), Main.rand.NextFloat(0.16f, 0.28f), glow, true, false, true));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D core = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/TinyGreyscaleCircle").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/ThinEndedLine").Value;
            Texture2D magic = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_03").Value;
            Texture2D circle = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_04").Value;
            Color paleBlue = new(88, 202, 255, 0);
            Color whiteBlue = new(220, 247, 255, 0);
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            Vector2 position = Projectile.Center - Main.screenPosition;
            float pulse = 0.88f + 0.12f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8.4f + Projectile.identity * 0.47f);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float completion = i / (float)Projectile.oldPos.Length;
                float opacity = 1f - completion;
                Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(core, trailPosition, null, paleBlue * opacity * 0.42f, Projectile.rotation,
                    core.Size() * 0.5f, MathHelper.Lerp(0.22f, 0.56f, opacity), SpriteEffects.None);
                Main.EntitySpriteDraw(bloom, trailPosition, null, paleBlue * opacity * 0.16f, Projectile.rotation,
                    bloomOrigin, 0.13f + opacity * 0.12f, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(line, position, null, paleBlue * 0.38f, Projectile.rotation + MathHelper.PiOver2,
                line.Size() * 0.5f, new Vector2(0.035f, 0.13f), SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, position, null, paleBlue * 0.84f, Projectile.rotation,
                bloomOrigin, 0.16f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, position, null, whiteBlue * 0.52f, Projectile.rotation,
                bloomOrigin, 0.075f, SpriteEffects.None);
            Main.EntitySpriteDraw(magic, position, null, paleBlue * 0.30f, -Projectile.rotation * 0.72f,
                magic.Size() * 0.5f, 0.017f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(circle, position, null, whiteBlue * 0.22f, Projectile.rotation * 0.86f + MathHelper.PiOver4,
                circle.Size() * 0.5f, 0.012f * (1.08f - 0.08f * pulse), SpriteEffects.None);
            Main.EntitySpriteDraw(core, position, null, Color.White with { A = 0 } * 0.86f, Projectile.rotation,
                core.Size() * 0.5f, 0.68f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SpawnRecoveryFlash(Projectile.Center, 0.9f);
        }

        // This is the BRecov transfer flash copied into the fountain's blue palette:
        // a pulse ring, vertical bloom line and compact core at both release and heal.
        private static void SpawnRecoveryFlash(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = new(88, 202, 255);
            Color accentColor = new(220, 247, 255);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center, -Vector2.UnitY * 0.25f, Color.Lerp(mainColor, Color.White, 0.18f),
                new Vector2(0.8f, 1.5f), -MathHelper.PiOver2, 0.15f * intensity, 0.038f, 14));
            GeneralParticleHandler.SpawnParticle(new BloomLineVFX(
                center + Vector2.UnitY * 18f, -Vector2.UnitY * 36f, 0.92f * intensity,
                Color.Lerp(mainColor, accentColor, 0.52f), 12));
            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                center, Vector2.Zero, Color.Lerp(mainColor, Color.White, 0.18f), 0.76f * intensity, 14));

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 4.2f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    center + Main.rand.NextVector2Circular(7f, 7f), velocity, false, Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.18f, 0.34f) * intensity,
                    Color.Lerp(mainColor, accentColor, Main.rand.NextFloat(0.2f, 0.65f)), true, false, true));
            }
        }
    }
}
