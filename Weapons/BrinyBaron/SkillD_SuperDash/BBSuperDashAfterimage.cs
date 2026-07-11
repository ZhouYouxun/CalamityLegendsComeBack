using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal sealed class BBSuperDashAfterimage : ModProjectile
    {
        private const int Lifetime = 25;
        private const float VelocityRetention = 0.84f;

        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private float WeaponRotation => Projectile.ai[0];
        private float WeaponScale => Projectile.ai[1] <= 0f ? 1f : Projectile.ai[1];
        private int SnapshotDirection => Projectile.ai[2] < 0f ? -1 : 1;

        public override void SetDefaults()
        {
            Projectile.width = 42;
            Projectile.height = 64;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.Center += Projectile.velocity;
            Projectile.velocity *= VelocityRetention;

            float progress = 1f - Projectile.timeLeft / (float)Lifetime;
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.36f, 0.55f) * (1f - progress * 0.55f));

            if (Main.dedServ)
                return;

            if (Projectile.timeLeft % 4 == 0)
            {
                Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.7f) * Main.rand.NextFloat(0.6f, 2.1f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(20f, 28f),
                    velocity,
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.18f, 0.32f),
                    Color.Lerp(Color.DeepSkyBlue, Color.White, Main.rand.NextFloat(0.2f, 0.55f)),
                    true,
                    false,
                    true));
            }

            if (Projectile.timeLeft == 1)
                SpawnDissolveBurst();
        }

        private void SpawnDissolveBurst()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX * SnapshotDirection);
            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = forward.RotatedByRandom(MathHelper.Pi) * Main.rand.NextFloat(1.2f, 4.8f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(22f, 30f),
                    velocity,
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.22f, 0.44f),
                    Color.Lerp(new Color(95, 206, 255), Color.White, Main.rand.NextFloat(0.2f, 0.65f))));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers)
                return false;

            Player owner = Main.player[Projectile.owner];
            if (!owner.active)
                return false;

            float progress = 1f - Projectile.timeLeft / (float)Lifetime;
            float opacity = Utils.GetLerpValue(0f, 4f, Projectile.timeLeft, true) * MathHelper.Lerp(0.72f, 0.03f, progress);
            float outlineOpacity = Utils.GetLerpValue(0f, 0.72f, progress, true) * Utils.GetLerpValue(0f, 4f, Projectile.timeLeft, true);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, owner.gfxOffY);

            DrawPlayerAura(drawPosition, outlineOpacity);
            DrawFrozenPlayer(owner, opacity, progress);
            DrawWeaponAfterimage(drawPosition, opacity, outlineOpacity);
            return false;
        }

        private void DrawFrozenPlayer(Player owner, float opacity, float progress)
        {
            int oldImmuneAlpha = owner.immuneAlpha;
            int oldDirection = owner.direction;

            owner.immuneAlpha = Math.Max(owner.immuneAlpha, (int)MathHelper.Lerp(90f, 245f, progress));
            owner.direction = SnapshotDirection;
            Main.PlayerRenderer.DrawPlayer(Main.Camera, owner, Projectile.position, owner.fullRotation, owner.fullRotationOrigin);
            owner.direction = oldDirection;
            owner.immuneAlpha = oldImmuneAlpha;
        }

        private static void DrawPlayerAura(Vector2 drawPosition, float opacity)
        {
            if (opacity <= 0f)
                return;

            Texture2D bloom = TextureAssets.MagicPixel.Value;
            Color auraColor = new Color(60, 210, 255, 0) * (0.18f + opacity * 0.32f);
            Rectangle vertical = new((int)drawPosition.X - 18, (int)drawPosition.Y - 46, 36, 92);
            Rectangle horizontal = new((int)drawPosition.X - 31, (int)drawPosition.Y - 22, 62, 44);
            Main.spriteBatch.Draw(bloom, vertical, auraColor);
            Main.spriteBatch.Draw(bloom, horizontal, auraColor * 0.75f);

            Texture2D circle = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Main.EntitySpriteDraw(
                circle,
                drawPosition,
                null,
                new Color(90, 225, 255, 0) * (opacity * 0.36f),
                0f,
                circle.Size() * 0.5f,
                0.32f + opacity * 0.08f,
                SpriteEffects.None,
                0f);
        }

        private void DrawWeaponAfterimage(Vector2 drawPosition, float opacity, float outlineOpacity)
        {
            Texture2D weaponTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron").Value;
            Rectangle frame = weaponTexture.Frame();
            Vector2 origin = frame.Size() * 0.5f;
            bool facingLeft = SnapshotDirection < 0;
            SpriteEffects effects = facingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float drawRotation = WeaponRotation + (facingLeft ? MathHelper.PiOver2 : 0f);
            Color outlineColor = new Color(90, 225, 255, 0) * outlineOpacity;

            int outlineCount = 6;
            for (int i = 0; i < outlineCount; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / outlineCount).ToRotationVector2() * MathHelper.Lerp(2f, 8f, outlineOpacity);
                Main.EntitySpriteDraw(
                    weaponTexture,
                    drawPosition + offset,
                    frame,
                    outlineColor,
                    drawRotation,
                    origin,
                    WeaponScale * (1.04f + outlineOpacity * 0.08f),
                    effects,
                    0f);
            }

            Main.EntitySpriteDraw(
                weaponTexture,
                drawPosition,
                frame,
                Color.Lerp(new Color(120, 225, 255), Color.White, 0.28f) * opacity,
                drawRotation,
                origin,
                WeaponScale,
                effects,
                0f);
        }
    }
}
