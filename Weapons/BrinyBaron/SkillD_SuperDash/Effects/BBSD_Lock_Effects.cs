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
    internal static class BBSD_Lock_Effects
    {
        internal static void SpawnTargetAcquireEffects(Vector2 targetCenter, Vector2 ownerCenter)
        {
            if (Main.dedServ)
                return;

            // Targeting system lock-on lines contracting on the target
            for (int i = 0; i < 12; i++)
            {
                float angle = MathHelper.TwoPi * i / 12f;
                Vector2 spawnPos = targetCenter + angle.ToRotationVector2() * 60f;
                Vector2 velocity = (targetCenter - spawnPos).SafeNormalize(Vector2.UnitX) * 4.2f;

                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    spawnPos,
                    velocity,
                    false,
                    12,
                    0.34f,
                    Color.Cyan));
            }

            // Expanding aqua ring indicating target lock-on success
            DirectionalPulseRing ring = new DirectionalPulseRing(
                targetCenter,
                Vector2.Zero,
                new Color(95, 210, 255, 0),
                new Vector2(0.7f, 0.7f),
                0f,
                0.14f,
                0.02f,
                14);
            GeneralParticleHandler.SpawnParticle(ring);
        }

        internal static void SpawnLockingEffects(Projectile projectile, Player owner, Vector2 focusPoint, NPC target, int timer, bool targetLocked)
        {
            if (Main.dedServ)
                return;

            // If a target is locked, spawn flowing water/energy streams traveling along the targeting beam
            if (targetLocked && target != null)
            {
                Vector2 tip = projectile.Center + (projectile.rotation - MathHelper.PiOver4).ToRotationVector2() * (50f * projectile.scale);
                if (timer % 4 == 0)
                {
                    Vector2 flowDir = (target.Center - tip).SafeNormalize(Vector2.UnitX);
                    float flowSpeed = Main.rand.NextFloat(9f, 16f);

                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        tip,
                        flowDir * flowSpeed,
                        false,
                        24,
                        0.25f,
                        Color.Lerp(Color.DeepSkyBlue, Color.Cyan, Main.rand.NextFloat())));
                }
            }
        }

        internal static void DrawLockBeam(Vector2 startWorld, Vector2 endWorld, float opacity)
        {
            Texture2D lineTex = ModContent.Request<Texture2D>("CalamityMod/Particles/ThinEndedLine").Value;
            Texture2D glowTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float distance = Vector2.Distance(startWorld, endWorld);
            if (distance <= 8f)
                return;

            Vector2 direction = (endWorld - startWorld).SafeNormalize(Vector2.UnitX);
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 10f);

            for (int i = 10; i < distance - 10f; i += 10)
            {
                float completion = MathHelper.Lerp(0.9f, 2.6f, 1f - i / distance);
                Vector2 drawPosition = startWorld - Main.screenPosition + direction * i;

                for (int layer = 0; layer < 2; layer++)
                {
                    Color color = layer == 0 ? new Color(95, 210, 255, 0) : new Color(255, 245, 205, 0);
                    float width = layer == 0 ? 0.92f : 0.3f;
                    Main.EntitySpriteDraw(
                        lineTex,
                        drawPosition,
                        null,
                        color * opacity,
                        direction.ToRotation() + MathHelper.PiOver2,
                        lineTex.Size() * 0.5f,
                        new Vector2(width * completion * MathHelper.Max(pulse, 0.28f), 1.08f) * 0.01f,
                        SpriteEffects.None,
                        0f);
                }
            }

            Main.EntitySpriteDraw(glowTex, startWorld - Main.screenPosition, null, new Color(145, 230, 255, 0) * opacity, 0f, glowTex.Size() * 0.5f, 0.24f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glowTex, endWorld - Main.screenPosition, null, new Color(255, 234, 140, 0) * opacity, 0f, glowTex.Size() * 0.5f, 0.32f, SpriteEffects.None, 0f);
        }

        internal static void DrawTargetingReticle(Vector2 focusPoint, NPC target, bool targetLocked)
        {
            Vector2 drawPosition = (target?.Center ?? focusPoint) - Main.screenPosition;
            bool locked = targetLocked;
            float time = Main.GlobalTimeWrappedHourly;
            float pulse = 0.78f + 0.22f * (float)System.Math.Sin(time * (locked ? 10f : 7f));

            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ringA = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_03").Value;
            Texture2D ringB = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_04").Value;
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/ThinEndedLine").Value;

            Color techBlue = new(70, 190, 255, 0);
            Color cyan = new(150, 245, 255, 0);
            Color white = new(235, 255, 255, 0);
            Color outerColor = Color.Lerp(techBlue, cyan, locked ? 0.7f : 0.35f);
            Color innerColor = Color.Lerp(cyan, white, locked ? 0.65f : 0.35f);
            float lockInterpolant = locked ? 1f : 0f;
            float ringScale = MathHelper.Lerp(0.34f, 0.48f, lockInterpolant) * pulse * 0.3333f;
            float tickRadius = MathHelper.Lerp(28f, 42f, lockInterpolant) * 0.3333f;

            Main.EntitySpriteDraw(glow, drawPosition, null, outerColor * 0.52f, 0f, glow.Size() * 0.5f, ringScale, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glow, drawPosition, null, innerColor * 0.34f, 0f, glow.Size() * 0.5f, ringScale * 0.48f, SpriteEffects.None, 0f);

            Main.EntitySpriteDraw(ringA, drawPosition, null, outerColor * 0.88f, time * 0.95f, ringA.Size() * 0.5f, MathHelper.Lerp(0.46f, 0.58f, lockInterpolant) * 0.3333f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(ringB, drawPosition, null, cyan * 0.76f, -time * 0.72f, ringB.Size() * 0.5f, MathHelper.Lerp(0.42f, 0.54f, lockInterpolant) * 0.3333f, SpriteEffects.FlipHorizontally, 0f);

            for (int i = 0; i < 4; i++)
            {
                float angle = time * (locked ? 2.4f : 1.5f) + MathHelper.PiOver2 * i;
                Vector2 direction = angle.ToRotationVector2();
                Vector2 tickPosition = drawPosition + direction * tickRadius;
                Main.EntitySpriteDraw(
                    line,
                    tickPosition,
                    null,
                    outerColor * 0.85f,
                    angle + MathHelper.PiOver2,
                    line.Size() * 0.5f,
                    new Vector2(0.036f, MathHelper.Lerp(0.14f, 0.22f, lockInterpolant)) * 0.3333f,
                    SpriteEffects.None,
                    0f);
            }

            if (locked)
                Main.EntitySpriteDraw(glow, drawPosition, null, white * 0.32f, 0f, glow.Size() * 0.5f, 0.14f * pulse * 0.3333f, SpriteEffects.None, 0f);
        }
    }
}
