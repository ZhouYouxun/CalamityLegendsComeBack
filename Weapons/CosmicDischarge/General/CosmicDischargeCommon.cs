using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    internal enum CosmicDischargeAttackMode
    {
        Whip,
        Sword,
        ChainKnife
    }

    internal enum CosmicDischargeAttackKind
    {
        WhipOver,
        WhipUnder,
        WhipThrust,
        SwordSwingOne,
        SwordSwingTwo,
        SwordFinisher,
        ChainKnifeSingle,
        ChainKnifeScatter,
        ChainKnifeBiteAll,
        QuickDraw
    }

    internal static class CosmicDischargeCommon
    {
        public const string ChainTexturePath = "CalamityLegendsComeBack/Weapons/CosmicDischarge/LeftClick/CosmicDischargeFlail";
        public const string RingTexturePath = "CalamityMod/Particles/BloomRing";
        private const int ChainHandleHeight = 62;
        private const int ChainBodyStartY = 64;
        private const int ChainBodyHeight = 28;
        private const int ChainTailStartY = 114;
        private const int ChainTailHeight = 84;
        private const float ChainBodyStartOffset = 30f;
        public static readonly Color DoGCyanColor = Color.Cyan;
        public static readonly Color DoGFuchsiaColor = Color.Fuchsia;
        public static readonly Color DoGPurpleColor = new(145, 0, 255);
        public static readonly Color DoGWhiteColor = Color.White;
        public static readonly Color DoGBlackColor = new(0, 0, 0, 0);

        // Compatibility aliases: old callers now inherit the DoG palette instead of the removed frost palette.
        public static Color FrostCoreColor => DoGSpecialColor;
        public static Color FrostGlowColor => DoGPurpleColor;
        public static Color FrostDarkColor => Color.Lerp(DoGPurpleColor, Color.Black, 0.35f);
        public static Color FrostWhiteColor => DoGWhiteColor;

        public static Color DoGSpecialColor =>
            Color.Lerp(
                DoGFuchsiaColor,
                DoGCyanColor,
                MathHelper.SmoothStep(0f, 1f, (MathF.Sin(Main.GlobalTimeWrappedHourly * 2f) + 1f) * 0.5f));

        public static Color GetModeColor(CosmicDischargeAttackMode mode) => mode switch
        {
            CosmicDischargeAttackMode.Whip => DoGCyanColor,
            CosmicDischargeAttackMode.Sword => DoGFuchsiaColor,
            CosmicDischargeAttackMode.ChainKnife => DoGPurpleColor,
            _ => DoGSpecialColor
        };

        public static Color Transparent(Color color) => new(color.R, color.G, color.B, 0);

        public static Color RandomDoGColor(bool includePurple = true)
        {
            int max = includePurple ? 4 : 3;
            return Main.rand.Next(max) switch
            {
                0 => DoGCyanColor,
                1 => DoGFuchsiaColor,
                2 => DoGSpecialColor,
                _ => DoGPurpleColor
            };
        }

        public static Vector2 GetAimDirection(Player player, Vector2 fallback)
        {
            Vector2 mouse = player.Calamity().mouseWorld;
            Vector2 direction = mouse - player.MountedCenter;
            if (direction.LengthSquared() < 0.001f)
                direction = fallback;

            if (direction.LengthSquared() < 0.001f)
                direction = Vector2.UnitX * player.direction;

            return direction.SafeNormalize(Vector2.UnitX * player.direction);
        }

        public static void HoldPlayer(Player player, Projectile projectile, Vector2 aimDirection, float armRotationOffset = 0f)
        {
            player.ChangeDir(aimDirection.X >= 0f ? 1 : -1);
            player.heldProj = projectile.whoAmI;
            player.itemTime = 2;
            player.itemAnimation = 2;
            player.itemRotation = aimDirection.ToRotation();

            float armRotation = aimDirection.ToRotation() - MathHelper.PiOver2 + armRotationOffset;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, armRotation);
        }

        public static List<Vector2> BuildCurvedBlade(Player player, Vector2 direction, float reach, float sideBend, float curl, int pointCount = 18)
        {
            List<Vector2> points = new(pointCount);
            Vector2 start = player.MountedCenter;
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                float forward = reach * t;
                float bend = sideBend * MathF.Sin(MathHelper.Pi * t);
                float wave = curl * MathF.Sin(MathHelper.TwoPi * t) * (1f - t * 0.35f);
                points.Add(start + direction * forward + normal * (bend + wave));
            }

            return points;
        }

        public static bool CheckCurveCollision(IReadOnlyList<Vector2> points, Rectangle targetHitbox, float width)
        {
            if (points == null || points.Count < 2)
                return false;

            for (int i = 0; i < points.Count - 1; i++)
            {
                float collisionPoint = 0f;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), points[i], points[i + 1], width, ref collisionPoint))
                    return true;
            }

            return false;
        }

        public static bool TargetIntersectsTip(IReadOnlyList<Vector2> points, Rectangle targetHitbox, float radius)
        {
            if (points == null || points.Count == 0)
                return false;

            Vector2 tip = points[^1];
            Vector2 closest = Vector2.Clamp(targetHitbox.Center.ToVector2(), targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.DistanceSquared(closest, tip) <= radius * radius;
        }

        public static void ApplyColdDebuffs(NPC target, int duration) => ApplyDoGDebuffs(target, duration);

        public static void ApplyDoGDebuffs(NPC target, int duration)
        {
            if (target == null || !target.active)
                return;

            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), duration);
        }

        public static void SpawnDoGSparkBurst(Vector2 center, int count, float minSpeed, float maxSpeed, float scale = 0.65f, Vector2? bias = null)
        {
            if (Main.dedServ)
                return;

            Vector2 biasDirection = bias.GetValueOrDefault(Vector2.Zero);
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = count <= 1
                    ? Main.rand.NextVector2CircularEdge(1f, 1f)
                    : (MathHelper.TwoPi * i / count).ToRotationVector2().RotatedByRandom(0.45f);
                Vector2 velocity = direction * Main.rand.NextFloat(minSpeed, maxSpeed) + biasDirection;
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    center + Main.rand.NextVector2Circular(18f, 18f),
                    velocity,
                    false,
                    Main.rand.Next(14, 28),
                    Main.rand.NextFloat(scale * 0.72f, scale * 1.25f),
                    RandomDoGColor()));
            }
        }

        public static void SpawnDoGRiftCracks(Vector2 center, int count, float minLength, float maxLength, float scale = 0.55f)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < count; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.18f, 0.18f)).ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    center + direction * Main.rand.NextFloat(2f, 18f),
                    direction * Main.rand.NextFloat(minLength, maxLength),
                    false,
                    Main.rand.Next(8, 15),
                    Main.rand.NextFloat(scale * 0.7f, scale * 1.15f),
                    Transparent(RandomDoGColor()) * 0.75f));
            }
        }

        public static void SpawnDoGImpact(Vector2 center, Vector2 direction, bool heavy, bool tip = false)
        {
            if (Main.dedServ)
                return;

            direction = direction.SafeNormalize(Vector2.UnitX);
            Color main = heavy ? DoGFuchsiaColor : DoGSpecialColor;
            Color secondary = tip ? DoGWhiteColor : DoGCyanColor;
            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                center,
                Vector2.Zero,
                Transparent(main) * (heavy ? 0.55f : 0.35f),
                heavy ? 0.68f : 0.44f,
                heavy ? 24 : 16));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                direction * 0.7f,
                Transparent(secondary) * (heavy ? 0.45f : 0.3f),
                Vector2.One,
                direction.ToRotation(),
                0.04f,
                heavy ? 0.32f : 0.19f,
                heavy ? 18 : 12));
            SpawnDoGSparkBurst(center, heavy ? 22 : 12, 3f, heavy ? 13f : 8f, heavy ? 0.72f : 0.5f, direction * 1.2f);
            if (heavy || tip)
                SpawnDoGRiftCracks(center, tip ? 5 : 3, 4f, tip ? 10f : 7f, tip ? 0.72f : 0.48f);
        }

        public static bool HasOwnedProjectile(Player player, params int[] projectileTypes)
        {
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != player.whoAmI)
                    continue;

                for (int i = 0; i < projectileTypes.Length; i++)
                {
                    if (projectile.type == projectileTypes[i])
                        return true;
                }
            }

            return false;
        }

        public static void DrawChain(SpriteBatch spriteBatch, Vector2 startWorld, Vector2 endWorld, Color drawColor, float scale, bool rigid, float gfxOffY = 0f)
        {
            Texture2D texture = ModContent.Request<Texture2D>(ChainTexturePath).Value;
            Rectangle handleFrame = new(0, 0, texture.Width, ChainHandleHeight);
            Rectangle bodyFrame = new(0, ChainBodyStartY, texture.Width, ChainBodyHeight);
            Rectangle tailFrame = new(0, ChainTailStartY, texture.Width, ChainTailHeight);

            Vector2 chain = endWorld - startWorld;
            float chainLength = chain.Length();
            if (chainLength < 2f)
                return;

            Vector2 direction = chain / chainLength;
            float rotation = direction.ToRotation() + MathHelper.PiOver2;
            Vector2 drawOffset = Vector2.UnitY * gfxOffY;

            Main.EntitySpriteDraw(
                texture,
                startWorld - Main.screenPosition + drawOffset,
                handleFrame,
                drawColor,
                rotation,
                handleFrame.Size() * 0.5f,
                scale,
                SpriteEffects.FlipVertically);

            float startOffset = Math.Min(ChainBodyStartOffset * scale, chainLength);
            float tailLength = ChainTailHeight * scale;
            float bodyEndDistance = MathHelper.Clamp(chainLength - tailLength, startOffset, chainLength);
            float remaining = System.Math.Max(0f, bodyEndDistance - startOffset);
            Vector2 drawPosition = startWorld + direction * startOffset;

            while (remaining > 2f)
            {
                Rectangle drawFrame = bodyFrame;
                float segmentHeight = drawFrame.Height * scale;
                if (remaining < segmentHeight)
                {
                    int croppedHeight = (int)MathHelper.Clamp(remaining / scale, 2f, bodyFrame.Height);
                    drawFrame.Height = croppedHeight;
                    segmentHeight = croppedHeight * scale;
                }

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition - Main.screenPosition + drawOffset,
                    drawFrame,
                    drawColor,
                    rotation,
                    new Vector2(drawFrame.Width * 0.5f, 0f),
                    scale,
                    SpriteEffects.None);

                drawPosition += direction * segmentHeight;
                remaining -= segmentHeight;
            }

            Main.EntitySpriteDraw(
                texture,
                startWorld + direction * bodyEndDistance - Main.screenPosition + drawOffset,
                tailFrame,
                drawColor,
                rotation,
                new Vector2(tailFrame.Width * 0.5f, 0f),
                scale,
                SpriteEffects.FlipVertically);
        }

        public static void DrawCurvedChain(SpriteBatch spriteBatch, IReadOnlyList<Vector2> points, Color drawColor, float scale, float gfxOffY = 0f)
        {
            if (points == null || points.Count < 2)
                return;

            Texture2D texture = ModContent.Request<Texture2D>(ChainTexturePath).Value;
            Rectangle handleFrame = new(0, 0, texture.Width, ChainHandleHeight);
            Rectangle bodyFrame = new(0, ChainBodyStartY, texture.Width, ChainBodyHeight);
            Rectangle tailFrame = new(0, ChainTailStartY, texture.Width, ChainTailHeight);
            Vector2 drawOffset = Vector2.UnitY * gfxOffY;

            Vector2 firstDirection = (points[1] - points[0]).SafeNormalize(Vector2.UnitY);
            Main.EntitySpriteDraw(
                texture,
                points[0] - Main.screenPosition + drawOffset,
                handleFrame,
                drawColor,
                firstDirection.ToRotation() + MathHelper.PiOver2,
                handleFrame.Size() * 0.5f,
                scale,
                SpriteEffects.FlipVertically);

            float pathLength = 0f;
            for (int i = 0; i < points.Count - 1; i++)
                pathLength += Vector2.Distance(points[i], points[i + 1]);

            float bodyStartDistance = Math.Min(ChainBodyStartOffset * scale, pathLength);
            float bodyEndDistance = MathHelper.Clamp(pathLength - ChainTailHeight * scale, bodyStartDistance, pathLength);
            Vector2 tailPosition = points[^1];
            Vector2 lastDirection = (points[^1] - points[^2]).SafeNormalize(Vector2.UnitY);

            float traveled = 0f;
            bool foundTailPosition = false;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 segment = points[i + 1] - points[i];
                float segmentLength = segment.Length();
                if (segmentLength < 2f)
                    continue;

                Vector2 segmentDirection = segment / segmentLength;
                float segmentStartDistance = traveled;
                float segmentEndDistance = traveled + segmentLength;

                if (segmentEndDistance > bodyStartDistance && segmentStartDistance < bodyEndDistance)
                {
                    float localStart = Math.Max(bodyStartDistance, segmentStartDistance) - segmentStartDistance;
                    float localEnd = Math.Min(bodyEndDistance, segmentEndDistance) - segmentStartDistance;
                    if (localEnd - localStart > 2f)
                        DrawBodySegment(texture, bodyFrame, points[i] + segmentDirection * localStart, points[i] + segmentDirection * localEnd, drawColor, scale, drawOffset);
                }

                if (!foundTailPosition && bodyEndDistance <= segmentEndDistance)
                {
                    tailPosition = points[i] + segmentDirection * (bodyEndDistance - segmentStartDistance);
                    lastDirection = segmentDirection;
                    foundTailPosition = true;
                }

                traveled = segmentEndDistance;
            }

            Main.EntitySpriteDraw(
                texture,
                tailPosition - Main.screenPosition + drawOffset,
                tailFrame,
                Color.Lerp(drawColor, DoGWhiteColor, 0.22f),
                lastDirection.ToRotation() + MathHelper.PiOver2,
                new Vector2(tailFrame.Width * 0.5f, 0f),
                scale,
                SpriteEffects.FlipVertically);
        }

        public static void DrawRightHoldIndicator(SpriteBatch spriteBatch, Player player, float intensity)
        {
            Texture2D ring = ModContent.Request<Texture2D>(RingTexturePath).Value;
            Vector2 drawPosition = player.Bottom - Main.screenPosition + new Vector2(0f, -6f + player.gfxOffY);
            Color ringColor = Color.Lerp(DoGPurpleColor, DoGSpecialColor, 0.45f) * (0.35f * intensity);

            spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                ring,
                drawPosition,
                null,
                ringColor,
                0f,
                ring.Size() * 0.5f,
                new Vector2(0.85f, 0.28f) * (1f + 0.2f * intensity),
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                ring,
                drawPosition,
                null,
                DoGSpecialColor * (0.18f * intensity),
                Main.GlobalTimeWrappedHourly * 0.8f,
                ring.Size() * 0.5f,
                new Vector2(0.45f, 0.14f) * (1f + 0.15f * intensity),
                SpriteEffects.None);

            spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private static void DrawBodySegment(Texture2D texture, Rectangle frame, Vector2 start, Vector2 end, Color drawColor, float scale, Vector2 drawOffset)
        {
            Vector2 segment = end - start;
            float length = segment.Length();
            if (length < 2f)
                return;

            Vector2 direction = segment / length;
            float rotation = direction.ToRotation() + MathHelper.PiOver2;
            float step = frame.Height * scale;
            Vector2 position = start;

            for (float traveled = 0f; traveled < length; traveled += step)
            {
                float remaining = length - traveled;
                Rectangle drawFrame = frame;
                if (remaining < step)
                    drawFrame.Height = (int)MathHelper.Clamp(remaining / scale, 2f, frame.Height);

                Main.EntitySpriteDraw(
                    texture,
                    position - Main.screenPosition + drawOffset,
                    drawFrame,
                    drawColor,
                    rotation,
                    new Vector2(drawFrame.Width * 0.5f, 0f),
                    scale,
                    SpriteEffects.None);

                position += direction * step;
            }
        }
    }
}
