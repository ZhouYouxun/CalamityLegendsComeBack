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
    public class LeonidReflectiveMeteor : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/LeonidProgenitor";
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";

        public Player Owner => Main.player[Projectile.owner];
        
        // ai[0] == 0: Hovering/Decelerating
        // ai[0] == 1: Returning to player
        public bool IsReturning
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        public int TimesBounced;
        public int decelerateTimer;
        private bool orbitInitialized;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1200; // 20 seconds
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 45;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            Projectile.alpha -= 16;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            Color themeColor = new Color(150, 100, 255);
            Lighting.AddLight(Projectile.Center, themeColor.ToVector3() * 0.45f);

            // Rotate based on velocity or spin slowly when hovering
            if (Projectile.velocity.LengthSquared() > 0.05f)
            {
                Projectile.rotation += Projectile.velocity.X * 0.04f + 0.15f * Math.Sign(Projectile.velocity.X);
            }
            else
            {
                Projectile.rotation += 0.02f;
            }

            if (IsReturning)
            {
                // Returning state: attracted to player
                Vector2 toPlayer = Owner.Center - Projectile.Center;
                float distance = toPlayer.Length();

                if (distance > 120f && !orbitInitialized)
                {
                    // Move directly towards player
                    Vector2 dir = toPlayer.SafeNormalize(Vector2.UnitY);
                    Projectile.velocity = dir * 16f;
                }
                else
                {
                    // Start orbiting and contracting
                    if (!orbitInitialized)
                    {
                        orbitInitialized = true;
                        // Set current radius
                        Projectile.ai[1] = distance;
                        // Determine initial angle
                        Projectile.localAI[0] = toPlayer.ToRotation() + MathHelper.Pi;
                    }

                    float currentRadius = Projectile.ai[1];
                    currentRadius -= 4.5f; // spiral inward
                    Projectile.ai[1] = currentRadius;

                    float currentAngle = Projectile.localAI[0];
                    currentAngle += 0.16f; // orbital rotation speed
                    Projectile.localAI[0] = currentAngle;

                    Projectile.Center = Owner.Center + currentAngle.ToRotationVector2() * currentRadius;
                    Projectile.velocity = Vector2.Zero;
                }

                // Visual trails during recycle
                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center,
                        DustID.TintableDustLighted,
                        Main.rand.NextVector2Circular(1f, 1f),
                        100,
                        new Color(180, 150, 255),
                        Main.rand.NextFloat(0.6f, 0.9f));
                    d.noGravity = true;
                }
            }
            else
            {
                // Hovering / Decelerating state
                if (decelerateTimer > 0)
                {
                    decelerateTimer--;
                }
                else
                {
                    // Decelerate
                    Projectile.velocity *= 0.93f;
                    if (Projectile.velocity.Length() < 0.25f)
                    {
                        Projectile.velocity = Vector2.Zero;
                    }
                }

                // Add a gentle floating bobbing effect
                if (Projectile.velocity == Vector2.Zero)
                {
                    Projectile.Center += new Vector2(0f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f + Projectile.whoAmI) * 0.08f);
                }

                // Disappear if max bounce reached
                if (TimesBounced >= 7)
                {
                    Projectile.Kill();
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            // Spawn some nice dusts
            for (int i = 0; i < 12; i++)
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.TintableDustLighted,
                    (i * MathHelper.TwoPi / 12f).ToRotationVector2() * Main.rand.NextFloat(1.5f, 3f),
                    100,
                    new Color(160, 120, 255),
                    Main.rand.NextFloat(1.2f, 1.8f));
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Rogue/LeonidProgenitorGlow").Value;
            
            Color drawColor = new Color(160, 120, 255) * (1f - Projectile.alpha / 255f);
            Vector2 origin = texture.Size() * 0.5f;

            // Draw afterimages
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 oldDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(texture, oldDrawPosition, null, drawColor * completion * 0.35f, Projectile.rotation, origin, Projectile.scale * 0.8f, SpriteEffects.None, 0f);
            }

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            
            // Draw a subtle halo behind the hovering comet
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Main.spriteBatch.Draw(bloom, drawPosition, null, drawColor * 0.25f, 0f, bloom.Size() * 0.5f, Projectile.scale * 0.35f, SpriteEffects.None, 0f);

            // Draw normal sprite
            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, Projectile.rotation, origin, Projectile.scale * 0.8f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glow, drawPosition, null, Color.White * (1f - Projectile.alpha / 255f), Projectile.rotation, glow.Size() * 0.5f, Projectile.scale * 0.8f, SpriteEffects.None, 0f);

            return false;
        }
    }
}
