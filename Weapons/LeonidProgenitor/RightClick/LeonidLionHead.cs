using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    public class LeonidLionHead : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/LeonidProgenitor";
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";

        public Player Owner => Main.player[Projectile.owner];

        public Vector2 TargetPosition;
        public bool IsBiting;
        private int biteTimer;
        private bool spawnedSubProjectiles;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = false; // only damage when biting
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
        }

        public override bool? CanDamage() => IsBiting;

        public override void AI()
        {
            // Set TargetPosition to mouse if uninitialized (failsafe)
            if (TargetPosition == Vector2.Zero)
            {
                TargetPosition = Main.MouseWorld;
            }

            Color themeColor = Color.Lerp(LeonidVisualUtils.GetReadyGold(Projectile.whoAmI * 0.2f), LeonidVisualUtils.MoonViolet, IsBiting ? 0.22f : 0.38f);
            Lighting.AddLight(Projectile.Center, themeColor.ToVector3() * (IsBiting ? 1.4f : 0.7f));

            if (!IsBiting)
            {
                // Move to target
                Vector2 toTarget = TargetPosition - Projectile.Center;
                float distance = toTarget.Length();

                if (distance < 16f || Projectile.timeLeft < 10)
                {
                    // Start Biting!
                    IsBiting = true;
                    biteTimer = 16;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.friendly = true;
                    Projectile.netUpdate = true;
                }
                else
                {
                    // Fly towards target
                    Vector2 dir = toTarget.SafeNormalize(Vector2.UnitX * Owner.direction);
                    Projectile.velocity = dir * 14f;
                }

                // Travel dusts
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(16f, 16f),
                        DustID.TintableDustLighted,
                        -Projectile.velocity * 0.2f,
                        100,
                        Color.Lerp(LeonidVisualUtils.StratusBlue, LeonidVisualUtils.MoonViolet, 0.45f),
                        Main.rand.NextFloat(0.8f, 1.2f));
                    d.noGravity = true;
                }

                Projectile.rotation = Projectile.velocity.ToRotation() + (Owner.direction == -1 ? MathHelper.Pi : 0f);
            }
            else
            {
                // Biting state: trigger roar/damage/splits on first frame
                if (biteTimer == 16)
                {
                    // Sound effects
                    SoundEngine.PlaySound(SoundID.Zombie88 with { Volume = 0.95f, Pitch = -0.3f }, Projectile.Center);
                    SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.9f, Pitch = -0.2f }, Projectile.Center);
                    Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 6f);

                    // Spawn 14 reflective meteors radially
                    if (Main.myPlayer == Projectile.owner && !spawnedSubProjectiles)
                    {
                        spawnedSubProjectiles = true;
                        int reflectiveType = ModContent.ProjectileType<LeonidReflectiveMeteor>();
                        int splitDamage = (int)(Projectile.damage * 0.45f);

                        int meteorCount = Owner.GetModPlayer<LeonidConstellationPlayer>().IsUnlocked(LeonidStar.Alterf) ? 18 : 14;
                        for (int i = 0; i < meteorCount; i++)
                        {
                            float angle = i * MathHelper.TwoPi / meteorCount;
                            float speed = Main.rand.NextFloat(11.5f, 17f);
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

                // Shake and scale up
                biteTimer--;
                Projectile.scale = 1.0f + 1.6f * (float)Math.Sin((16 - biteTimer) / 16f * MathHelper.PiOver2);

                // Bite visual shockwave particles
                if (Main.rand.NextBool())
                {
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(48f * Projectile.scale, 48f * Projectile.scale),
                        DustID.TintableDustLighted,
                        Main.rand.NextVector2Circular(2f, 2f),
                        100,
                        LeonidVisualUtils.GetReadyGold(Projectile.whoAmI * 0.3f),
                        Main.rand.NextFloat(0.9f, 1.3f));
                    d.noGravity = true;
                }

                if (biteTimer <= 0)
                {
                    Projectile.Kill();
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            Color themeColor = LeonidVisualUtils.GetReadyGold(Projectile.whoAmI * 0.3f);
            LeonidVisualUtils.SpawnDustBurst(Projectile.Center, themeColor, 28, 8f, 1.3f);
            LeonidVisualUtils.SpawnBloomBurst(Projectile.Center, themeColor * 0.5f, 0.9f, 18);
            LeonidVisualUtils.SpawnCelestialPulse(Projectile.Center, Projectile.oldVelocity, themeColor, 1.45f, 24);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Rogue/LeonidProgenitorGlow").Value;
            float opacity = 1f - Projectile.alpha / 255f;
            Color drawColor = Color.Lerp(LeonidVisualUtils.GetReadyGold(Projectile.whoAmI * 0.3f), LeonidVisualUtils.MoonViolet, IsBiting ? 0.16f : 0.34f) * opacity;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            LeonidVisualUtils.BeginAdditiveSpriteBatch();
            LeonidVisualUtils.DrawGlowBlade(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction), drawColor, IsBiting ? 0.42f : 0.22f, 0.14f * Projectile.scale, 0.032f * Projectile.scale);
            Main.EntitySpriteDraw(glow, drawPosition, null, Color.White * opacity, Projectile.rotation, glow.Size() * 0.5f, Projectile.scale, Owner.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

            if (IsBiting)
            {
                Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
                Main.EntitySpriteDraw(bloom, drawPosition, null, drawColor * 0.4f, 0f, bloom.Size() * 0.5f, Projectile.scale * 0.75f, SpriteEffects.None, 0f);
                Main.EntitySpriteDraw(bloom, drawPosition, null, Color.White * 0.25f, 0f, bloom.Size() * 0.5f, Projectile.scale * 0.4f, SpriteEffects.None, 0f);
                LeonidVisualUtils.DrawSparkle(Projectile.Center, LeonidVisualUtils.MoonWhite, 0.46f * opacity, 0.54f * Projectile.scale, -Projectile.rotation);
            }

            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();

            SpriteEffects effects = Owner.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float rotation = Projectile.rotation;

            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, rotation, origin, Projectile.scale, effects, 0f);

            return false;
        }
    }
}
