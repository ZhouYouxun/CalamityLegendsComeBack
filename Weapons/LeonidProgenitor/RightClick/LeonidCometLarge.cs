using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    public class LeonidCometLarge : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/LeonidProgenitor";
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";

        public Player Owner => Main.player[Projectile.owner];
        public float Progress => Projectile.ai[2];

        private bool initialized;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = 2;
        }

        public override void AI()
        {
            if (!initialized)
            {
                Projectile.scale = 0.8f + 0.6f * Progress;
                Projectile.DamageType = Owner.HeldItem.DamageType;
                initialized = true;
            }

            Color trailColor = LeonidVisualUtils.GetCelestialColor(Progress, Projectile.whoAmI * 0.13f);
            Lighting.AddLight(Projectile.Center, trailColor.ToVector3() * 0.8f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(16f * Projectile.scale, 16f * Projectile.scale),
                    Main.rand.NextBool(3) ? DustID.Electric : DustID.TintableDustLighted,
                    -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.15f),
                    100,
                    Color.Lerp(trailColor, LeonidVisualUtils.MoonWhite, Main.rand.NextFloat(0.28f)),
                    Main.rand.NextFloat(0.9f, 1.3f));
                dust.noGravity = true;
            }

            Projectile.rotation += 0.16f * Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.85f, Pitch = -0.2f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f, Pitch = 0.1f }, Projectile.Center);

            Color themeColor = LeonidVisualUtils.GetCelestialColor(Progress, Projectile.whoAmI * 0.17f);
            LeonidVisualUtils.SpawnDustBurst(Projectile.Center, themeColor, 78, 12f, 1.8f);
            LeonidVisualUtils.SpawnBloomBurst(Projectile.Center, themeColor * 0.45f, 0.7f, 16);
            LeonidVisualUtils.SpawnCelestialPulse(Projectile.Center, Projectile.oldVelocity, themeColor, 1.65f, 28);

            // Release 7 reflective meteors
            if (Main.myPlayer == Projectile.owner)
            {
                int reflectiveType = ModContent.ProjectileType<LeonidReflectiveMeteor>();
                int splitDamage = (int)(Projectile.damage * 0.5f);

                for (int i = 0; i < 7; i++)
                {
                    // Generate initial velocity in random directions
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float speed = Main.rand.NextFloat(5.5f, 13.5f);
                    Vector2 splitVel = angle.ToRotationVector2() * speed;

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        splitVel,
                        reflectiveType,
                        splitDamage,
                        Projectile.knockBack * 0.4f,
                        Projectile.owner);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Rogue/LeonidProgenitorGlow").Value;
            Color drawColor = LeonidVisualUtils.GetCelestialColor(Progress, Projectile.whoAmI * 0.17f) * (1f - Projectile.alpha / 255f);
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            // These textures contain black source pixels; keep all emitted light additive.
            LeonidVisualUtils.BeginAdditiveSpriteBatch();
            LeonidVisualUtils.DrawGlowBlade(Projectile.Center - direction * 12f, direction, drawColor, 0.46f, 0.18f * Projectile.scale, 0.034f * Projectile.scale);
            LeonidVisualUtils.DrawCelestialHead(Projectile.Center, drawColor, 1f - Projectile.alpha / 255f, Projectile.scale * 1.28f, Projectile.rotation);
            Main.EntitySpriteDraw(glow, drawPosition, null, Color.White * (1f - Projectile.alpha / 255f), Projectile.rotation, glow.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            LeonidVisualUtils.DrawBloom(Projectile.Center, drawColor * 0.3f, Projectile.scale * 0.45f);
            // Night-sky blue outline glow
            float nsOp = (1f - Projectile.alpha / 255f) * 0.48f;
            for (int i = 0; i < 8; i++)
            {
                Vector2 off = (i * MathHelper.TwoPi / 8f).ToRotationVector2() * 2.8f * Projectile.scale;
                Main.EntitySpriteDraw(texture, drawPosition + off, null,
                    LeonidVisualUtils.NightSkyBlue with { A = 0 } * nsOp,
                    Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 oldDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(texture, oldDrawPosition, null, drawColor * completion * 0.3f, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Color.Lerp(drawColor, Color.White, 0.08f), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);

            return false;
        }
    }
}
