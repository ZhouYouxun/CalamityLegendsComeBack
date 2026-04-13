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

            for (int i = 0; i < 3; i++)
            {
                DirectionalPulseRing ring = new DirectionalPulseRing(
                    targetCenter,
                    Vector2.Zero,
                    Color.Lerp(new Color(118, 230, 255), Color.White, 0.35f),
                    new Vector2(0.62f + i * 0.08f, 1.18f + i * 0.18f),
                    MathHelper.TwoPi * i / 3f,
                    0.12f + i * 0.02f,
                    0.02f,
                    12 + i * 2);
                GeneralParticleHandler.SpawnParticle(ring);
            }

            Vector2 beamDirection = (targetCenter - ownerCenter).SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 10; i++)
            {
                Dust burst = Dust.NewDustPerfect(
                    targetCenter + Main.rand.NextVector2Circular(30f, 30f),
                    DustID.GemSapphire,
                    beamDirection.RotatedByRandom(0.75f) * Main.rand.NextFloat(1.6f, 5.5f),
                    100,
                    new Color(178, 245, 255),
                    Main.rand.NextFloat(1f, 1.4f));
                burst.noGravity = true;
            }
        }

        internal static void SpawnLockingEffects(Projectile projectile, Player owner, Vector2 focusPoint, NPC target, int timer, bool targetLocked)
        {
            if (Main.dedServ)
                return;

            Vector2 beamTarget = target?.Center ?? focusPoint;
            Vector2 ownerCenter = owner.MountedCenter;
            Vector2 beamDirection = (beamTarget - ownerCenter).SafeNormalize(Vector2.UnitX);
            Vector2 midpoint = Vector2.Lerp(ownerCenter, beamTarget, 0.5f);

            if (timer % 3 == 0)
            {
                Particle beamSpark = new CustomSpark(
                    midpoint + Main.rand.NextVector2Circular(12f, 12f),
                    beamDirection * Main.rand.NextFloat(0.6f, 1.8f),
                    "CalamityMod/Particles/BloomLineSoftEdge",
                    false,
                    10,
                    0.12f,
                    new Color(150, 235, 255) * 0.88f,
                    new Vector2(0.8f, 2.8f),
                    glowCenter: true,
                    shrinkSpeed: 0.72f,
                    glowCenterScale: 0.92f,
                    glowOpacity: 0.62f);
                GeneralParticleHandler.SpawnParticle(beamSpark);
            }

            if (timer % 5 == 0)
            {
                DirectionalPulseRing focusRing = new DirectionalPulseRing(
                    beamTarget,
                    Vector2.Zero,
                    targetLocked ? new Color(255, 228, 140) : new Color(98, 208, 255),
                    new Vector2(0.55f, 1.1f),
                    timer * 0.05f,
                    0.09f,
                    0.014f,
                    11);
                GeneralParticleHandler.SpawnParticle(focusRing);
            }
        }

        internal static void DrawLockBeam(Vector2 startWorld, Vector2 endWorld, float opacity)
        {
            Texture2D lineTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;
            Texture2D glowTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float distance = Vector2.Distance(startWorld, endWorld);
            if (distance <= 8f)
                return;

            Vector2 direction = (endWorld - startWorld).SafeNormalize(Vector2.UnitX);
            for (float step = 12f; step < distance - 12f; step += 12f)
            {
                float completion = step / distance;
                float pulse = 0.75f + 0.25f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 22f - completion * 8f);
                Vector2 drawPosition = startWorld - Main.screenPosition + direction * step;

                Main.EntitySpriteDraw(
                    lineTex,
                    drawPosition,
                    null,
                    new Color(125, 220, 255, 0) * opacity * 0.75f,
                    direction.ToRotation() + MathHelper.PiOver2,
                    lineTex.Size() * 0.5f,
                    new Vector2(0.011f * pulse, 0.96f),
                    SpriteEffects.None,
                    0f);

                Main.EntitySpriteDraw(
                    lineTex,
                    drawPosition,
                    null,
                    new Color(255, 245, 205, 0) * opacity * 0.42f,
                    direction.ToRotation() + MathHelper.PiOver2,
                    lineTex.Size() * 0.5f,
                    new Vector2(0.006f * pulse, 0.72f),
                    SpriteEffects.None,
                    0f);
            }

            Main.EntitySpriteDraw(glowTex, startWorld - Main.screenPosition, null, new Color(145, 230, 255, 0) * opacity, 0f, glowTex.Size() * 0.5f, 0.24f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glowTex, endWorld - Main.screenPosition, null, new Color(255, 234, 140, 0) * opacity, 0f, glowTex.Size() * 0.5f, 0.32f, SpriteEffects.None, 0f);
        }

        internal static void DrawTargetingReticle(Vector2 focusPoint, NPC target, bool targetLocked)
        {
            Texture2D glowTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 drawPosition = (target?.Center ?? focusPoint) - Main.screenPosition;
            float pulse = 0.75f + 0.25f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
            Color outerColor = targetLocked ? new Color(255, 226, 126, 0) : new Color(125, 225, 255, 0);
            Color innerColor = targetLocked ? new Color(255, 255, 255, 0) : new Color(220, 250, 255, 0);

            Main.EntitySpriteDraw(glowTex, drawPosition, null, outerColor * 0.55f, 0f, glowTex.Size() * 0.5f, 0.34f * pulse, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glowTex, drawPosition, null, innerColor * 0.42f, 0f, glowTex.Size() * 0.5f, 0.18f * pulse, SpriteEffects.None, 0f);

            float ringRadius = targetLocked ? 42f : 28f;
            for (int i = 0; i < 4; i++)
            {
                float angle = Main.GlobalTimeWrappedHourly * 2f + MathHelper.PiOver2 * i;
                Vector2 offset = angle.ToRotationVector2() * ringRadius;
                Main.EntitySpriteDraw(glowTex, drawPosition + offset, null, outerColor * 0.75f, angle, glowTex.Size() * 0.5f, 0.08f, SpriteEffects.None, 0f);

                Vector2 lineScale = i % 2 == 0 ? new Vector2(ringRadius * 2f, 2f) : new Vector2(2f, ringRadius * 2f);
                Main.EntitySpriteDraw(pixel, drawPosition, null, innerColor * 0.2f, 0f, new Vector2(0.5f, 0.5f), lineScale, SpriteEffects.None, 0f);
            }
        }
    }
}
