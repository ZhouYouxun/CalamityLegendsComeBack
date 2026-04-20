using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    internal static class CosmicDischargeCommon
    {
        public const string ChainTexturePath = "CalamityLegendsComeBack/Weapons/CosmicDischarge/CosmicDischargeFlail";
        public const string RingTexturePath = "CalamityMod/Particles/BloomRing";
        public static readonly Color FrostCoreColor = new(150, 255, 255);
        public static readonly Color FrostGlowColor = new(110, 175, 255);
        public static readonly Color FrostDarkColor = new(58, 84, 150);

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

        public static void DrawChain(SpriteBatch spriteBatch, Vector2 startWorld, Vector2 endWorld, Color drawColor, float scale, bool rigid, float gfxOffY = 0f)
        {
            Texture2D texture = ModContent.Request<Texture2D>(ChainTexturePath).Value;
            Rectangle handleFrame = new(0, 0, texture.Width, 62);
            Rectangle body1Frame = new(0, 64, texture.Width, 28);
            Rectangle body2Frame = new(0, 94, texture.Width, 18);
            Rectangle tailFrame = new(0, 114, texture.Width, 84);

            Vector2 chain = endWorld - startWorld;
            if (chain.LengthSquared() < 4f)
                return;

            Vector2 direction = chain.SafeNormalize(Vector2.UnitY);
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

            float startOffset = 30f * scale;
            float endOffset = 66f * scale;
            float remaining = System.Math.Max(0f, chain.Length() - startOffset - endOffset);
            Vector2 drawPosition = startWorld + direction * startOffset;
            bool useBody1 = rigid;

            while (remaining > 2f)
            {
                Rectangle bodyFrame = useBody1 ? body1Frame : body2Frame;
                float segmentHeight = bodyFrame.Height * scale;
                if (remaining < segmentHeight)
                {
                    int croppedHeight = (int)MathHelper.Clamp(remaining / scale, 2f, bodyFrame.Height);
                    bodyFrame.Height = croppedHeight;
                    segmentHeight = croppedHeight * scale;
                }

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition - Main.screenPosition + drawOffset,
                    bodyFrame,
                    drawColor,
                    rotation,
                    new Vector2(bodyFrame.Width * 0.5f, 0f),
                    scale,
                    SpriteEffects.None);

                drawPosition += direction * segmentHeight;
                remaining -= segmentHeight;
                useBody1 = rigid ? !useBody1 : false;
            }

            Main.EntitySpriteDraw(
                texture,
                endWorld - Main.screenPosition + drawOffset,
                tailFrame,
                drawColor,
                rotation,
                new Vector2(tailFrame.Width * 0.5f, 0f),
                scale,
                SpriteEffects.None);
        }

        public static void DrawRightHoldIndicator(SpriteBatch spriteBatch, Player player, float intensity)
        {
            Texture2D ring = ModContent.Request<Texture2D>(RingTexturePath).Value;
            Vector2 drawPosition = player.Bottom - Main.screenPosition + new Vector2(0f, -6f + player.gfxOffY);
            Color ringColor = Color.Lerp(FrostGlowColor, FrostCoreColor, 0.45f) * (0.35f * intensity);

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
        }
    }
}
