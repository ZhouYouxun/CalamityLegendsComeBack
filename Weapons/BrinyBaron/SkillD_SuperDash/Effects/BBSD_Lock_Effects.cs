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
            Vector2 tipDirection = (projectile.rotation - MathHelper.PiOver4).ToRotationVector2();
            Vector2 weaponTip = projectile.Center + tipDirection * (42f * projectile.scale);

            if (timer % 3 == 0)
            {
                Particle beamSpark = new CustomSpark(
                    weaponTip + Main.rand.NextVector2Circular(4f, 4f),
                    beamDirection * Main.rand.NextFloat(0.45f, 1.15f),
                    "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade",
                    false,
                    8,
                    0.14f,
                    new Color(150, 235, 255) * 0.88f,
                    new Vector2(0.52f, 1.2f),
                    glowCenter: true,
                    shrinkSpeed: 0.9f,
                    glowCenterScale: 0.92f,
                    glowOpacity: 0.62f);
                GeneralParticleHandler.SpawnParticle(beamSpark);

                DirectionalPulseRing focusRing = new DirectionalPulseRing(
                    weaponTip - beamDirection * (8f + (timer % 4) * 14f),
                    -beamDirection * 1.15f,
                    targetLocked ? new Color(120, 220, 255) : new Color(98, 208, 255),
                    new Vector2(1.75f, 4.8f),
                    beamDirection.ToRotation() - MathHelper.PiOver2,
                    0.32f,
                    0.045f,
                    14);
                GeneralParticleHandler.SpawnParticle(focusRing);

                Vector2 smokeBase = weaponTip - beamDirection * (14f + (timer % 5) * 12f);
                Vector2 smokeVelocity = -beamDirection.RotatedBy(Math.Sin(timer * 0.42f) * 0.12f) * 2.1f;
                GeneralParticleHandler.SpawnParticle(
                    new HeavySmokeParticle(
                        smokeBase,
                        smokeVelocity,
                        targetLocked ? new Color(70, 146, 196) : new Color(74, 156, 188),
                        23,
                        0.88f,
                        0.65f
                    )
                );

                GeneralParticleHandler.SpawnParticle(
                    new HeavySmokeParticle(
                        smokeBase + beamDirection.RotatedBy(MathHelper.PiOver2) * 10f,
                        smokeVelocity.RotatedBy(0.18f) * 0.9f,
                        targetLocked ? new Color(108, 214, 255) : new Color(115, 220, 255),
                        23,
                        0.66f,
                        0.65f
                    )
                );
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
            Texture2D glowTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D magic03 = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_03").Value;
            Texture2D magic04 = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_04").Value;
            Vector2 drawPosition = (target?.Center ?? focusPoint) - Main.screenPosition;
            float pulse = 0.75f + 0.25f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
            Color outerColor = targetLocked ? new Color(255, 226, 126, 0) : new Color(125, 225, 255, 0);
            Color innerColor = targetLocked ? new Color(255, 255, 255, 0) : new Color(220, 250, 255, 0);
            Color magicColor = new Color(255, 232, 120, 0) * 0.92f;

            Main.EntitySpriteDraw(glowTex, drawPosition, null, outerColor * 0.55f, 0f, glowTex.Size() * 0.5f, 0.34f * pulse, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glowTex, drawPosition, null, innerColor * 0.42f, 0f, glowTex.Size() * 0.5f, 0.18f * pulse, SpriteEffects.None, 0f);

            float ringRadius = targetLocked ? 42f : 28f;
            for (int i = 0; i < 4; i++)
            {
                float angle = Main.GlobalTimeWrappedHourly * 2f + MathHelper.PiOver2 * i;
                Vector2 offset = angle.ToRotationVector2() * ringRadius;
                Main.EntitySpriteDraw(glowTex, drawPosition + offset, null, outerColor * 0.75f, angle, glowTex.Size() * 0.5f, 0.08f, SpriteEffects.None, 0f);
            }

            Main.EntitySpriteDraw(
                magic03,
                drawPosition,
                null,
                magicColor,
                Main.GlobalTimeWrappedHourly * 0.8f,
                magic03.Size() * 0.5f,
                0.5f,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                magic04,
                drawPosition,
                null,
                magicColor * 0.95f,
                -Main.GlobalTimeWrappedHourly * 0.65f,
                magic04.Size() * 0.5f,
                0.5f,
                SpriteEffects.FlipHorizontally,
                0f);
        }
    }
}
