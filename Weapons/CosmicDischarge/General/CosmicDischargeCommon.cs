using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
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
        ChainKnife,
        Greatsword
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
        GreatswordSwingOne,
        GreatswordSwingTwo,
        GreatswordFinisher,
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
        public static readonly Color FrostCoreColor = new(150, 255, 255);
        public static readonly Color FrostGlowColor = new(110, 175, 255);
        public static readonly Color FrostDarkColor = new(58, 84, 150);
        public static readonly Color FrostWhiteColor = new(225, 250, 255);

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

        public static void ApplyColdDebuffs(NPC target, int duration)
        {
            if (target == null || !target.active)
                return;

            target.AddBuff(ModContent.BuffType<Nightwither>(), duration);
            target.AddBuff(BuffID.Frozen, duration);
            target.AddBuff(BuffID.Frostburn2, duration);
            target.AddBuff(BuffID.Chilled, duration);
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
                SpriteEffects.None);

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
                SpriteEffects.None);
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
                SpriteEffects.None);

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
                Color.Lerp(drawColor, FrostWhiteColor, 0.22f),
                lastDirection.ToRotation() + MathHelper.PiOver2,
                new Vector2(tailFrame.Width * 0.5f, 0f),
                scale,
                SpriteEffects.None);
        }

        public static void DrawRightHoldIndicator(SpriteBatch spriteBatch, Player player, float intensity)
        {
            Texture2D ring = ModContent.Request<Texture2D>(RingTexturePath).Value;
            Vector2 drawPosition = player.Bottom - Main.screenPosition + new Vector2(0f, -6f + player.gfxOffY);
            Color ringColor = Color.Lerp(FrostGlowColor, FrostCoreColor, 0.45f) * (0.35f * intensity);

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
                FrostCoreColor * (0.18f * intensity),
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
