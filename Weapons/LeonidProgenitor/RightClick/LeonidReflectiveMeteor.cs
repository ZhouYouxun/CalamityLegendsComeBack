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

            Color themeColor = Color.Lerp(LeonidVisualUtils.StratusBlue, LeonidVisualUtils.MoonViolet, 0.45f);
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
                        Color.Lerp(LeonidVisualUtils.StratusBlue, LeonidVisualUtils.MoonWhite, 0.35f),
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
            CLCBLightingBoltsSystem.Spawn_LeonidStarfieldMatrixBurst(Projectile.Center, 1f);

            // Spawn some nice dusts
            for (int i = 0; i < 12; i++)
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.TintableDustLighted,
                    (i * MathHelper.TwoPi / 12f).ToRotationVector2() * Main.rand.NextFloat(1.5f, 3f),
                    100,
                    Color.Lerp(LeonidVisualUtils.StratusBlue, LeonidVisualUtils.MoonViolet, 0.35f),
                    Main.rand.NextFloat(1.2f, 1.8f));
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Rogue/LeonidProgenitorGlow").Value;
            
            float opacity = 1f - Projectile.alpha / 255f;
            Color drawColor = Color.Lerp(LeonidVisualUtils.StratusBlue, LeonidVisualUtils.MoonViolet, 0.38f) * opacity;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            // The halo and glow sheet are emission textures, so never alpha-blend their black base.
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D magic1 = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_01").Value;
            Texture2D magic2 = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_02").Value;

            // 星空色：随时间在层云蓝/月光紫/月白/星金间流动
            Color starryColor = LeonidVisualUtils.GetMeteorColor(Projectile.whoAmI * 0.19f);
            Color trailColor = Color.Lerp(starryColor, LeonidVisualUtils.StratusBlue, 0.32f);

            LeonidVisualUtils.BeginAdditiveSpriteBatch();

            // 双层魔法阵：0.5 缩放，缓慢互相反向旋转，亮度随呼吸脉冲起伏
            float magicSpin = Main.GlobalTimeWrappedHourly * 0.55f + Projectile.whoAmI * 0.7f;
            float magicPulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.4f + Projectile.whoAmI);
            Color magicColorA = starryColor with { A = 0 };
            Color magicColorB = Color.Lerp(starryColor, LeonidVisualUtils.MoonViolet, 0.5f) with { A = 0 };
            Main.EntitySpriteDraw(magic1, drawPosition, null, magicColorA * ((0.3f + 0.14f * magicPulse) * opacity),
                magicSpin, magic1.Size() * 0.5f, 0.5f * (1f + 0.05f * magicPulse), SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(magic2, drawPosition, null, magicColorB * ((0.26f + 0.12f * (1f - magicPulse)) * opacity),
                -magicSpin * 0.8f, magic2.Size() * 0.5f, 0.5f * (1f - 0.04f * magicPulse), SpriteEffects.None, 0f);

            // 左键彗星同款拖尾：柔光晕点 + 间隔星闪
            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float t = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 trailWorld = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                Vector2 trailPos = trailWorld - Main.screenPosition;

                float trailScale = 0.026f + t * 0.04f;
                Main.EntitySpriteDraw(bloom, trailPos, null, trailColor with { A = 0 } * (0.24f * t * opacity),
                    0f, bloom.Size() * 0.5f, trailScale, SpriteEffects.None, 0);

                if (i % 3 == 0)
                    LeonidVisualUtils.DrawSparkle(trailWorld, LeonidVisualUtils.MoonWhite, 0.18f * t * opacity, 0.18f + t * 0.12f, Projectile.rotation + i);
            }

            LeonidVisualUtils.DrawGlowBlade(Projectile.Center - direction * 5f, direction, drawColor, 0.24f * opacity, 0.075f * Projectile.scale, 0.02f * Projectile.scale);
            Main.EntitySpriteDraw(bloom, drawPosition, null, drawColor * 0.25f, 0f, bloom.Size() * 0.5f, Projectile.scale * 0.35f, SpriteEffects.None, 0f);
            LeonidVisualUtils.DrawCelestialHead(Projectile.Center, starryColor, opacity, Projectile.scale * 0.9f, Projectile.rotation);
            LeonidVisualUtils.DrawSparkle(Projectile.Center, LeonidVisualUtils.MoonWhite, 0.28f * opacity, 0.28f * Projectile.scale, Projectile.rotation);
            Main.EntitySpriteDraw(glow, drawPosition, null, Color.White * opacity, Projectile.rotation, glow.Size() * 0.5f, Projectile.scale * 0.8f, SpriteEffects.None, 0f);
            // 星空色包边：流动渐变色描边，随呼吸脉冲微微涨缩
            float outlineRadius = 3.2f + 0.6f * magicPulse;
            for (int i = 0; i < 8; i++)
            {
                Vector2 off = (i * MathHelper.TwoPi / 8f + magicSpin * 0.5f).ToRotationVector2() * outlineRadius;
                Main.EntitySpriteDraw(texture, drawPosition + off, null,
                    starryColor with { A = 0 } * opacity * 0.55f,
                    Projectile.rotation, origin, Projectile.scale * 0.8f, SpriteEffects.None, 0f);
            }
            // Night-sky blue additive glow outline
            for (int i = 0; i < 8; i++)
            {
                Vector2 off = (i * MathHelper.TwoPi / 8f).ToRotationVector2() * 2.2f;
                Main.EntitySpriteDraw(texture, drawPosition + off, null,
                    LeonidVisualUtils.NightSkyBlue with { A = 0 } * opacity * 0.5f,
                    Projectile.rotation, origin, Projectile.scale * 0.8f, SpriteEffects.None, 0f);
            }
            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();

            // Draw afterimages
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 oldDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(texture, oldDrawPosition, null, drawColor * completion * 0.35f, Projectile.rotation, origin, Projectile.scale * 0.8f, SpriteEffects.None, 0f);
            }

            // Draw normal sprite
            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, Projectile.rotation, origin, Projectile.scale * 0.8f, SpriteEffects.None, 0f);

            return false;
        }
    }
}
