using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Core.Systems
{
    // Minimal "运镜" primitive: briefly pulls the camera toward a world point (a death, a ritual altar,
    // a telegraph epicenter) instead of relying on screen shake alone to sell a big moment. It blends with
    // the vanilla player-centered camera rather than hard-locking it away, so the player never loses track
    // of their own character. Three phases per request: rise (ease toward the focus), hold (sit on it),
    // fall (ease back to normal) — not an instant snap-then-cut.
    public sealed class IUMWCameraFocusPlayer : ModPlayer
    {
        private Vector2 focusPosition;
        private float pullStrength;
        private float interpolant;
        private int holdTimer;
        private int riseFrames = 10;
        private int fallFrames = 26;

        public override void Initialize()
        {
            interpolant = 0f;
            holdTimer = 0;
            pullStrength = 0f;
        }

        public override void ModifyScreenPosition()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (holdTimer <= 0 && interpolant <= 0.001f)
                return;

            if (holdTimer > 0)
            {
                holdTimer--;
                float riseStep = riseFrames > 0 ? 1f / riseFrames : 1f;
                interpolant = System.Math.Min(1f, interpolant + riseStep);
            }
            else
            {
                float fallStep = fallFrames > 0 ? 1f / fallFrames : 1f;
                interpolant = System.Math.Max(0f, interpolant - fallStep);
            }

            if (interpolant <= 0.001f)
            {
                interpolant = 0f;
                return;
            }

            Vector2 playerCenteredPos = Main.screenPosition;
            Vector2 focusCenteredPos = focusPosition - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;

            // Smoothstep rather than a linear blend — a linear pull reads as mechanical, this reads as a camera operator easing in.
            float eased = interpolant * interpolant * (3f - 2f * interpolant);
            Main.screenPosition = Vector2.Lerp(playerCenteredPos, focusCenteredPos, eased * pullStrength);

            Main.screenPosition.X = MathHelper.Clamp(Main.screenPosition.X, 0f, Main.maxTilesX * 16f - Main.screenWidth);
            Main.screenPosition.Y = MathHelper.Clamp(Main.screenPosition.Y, 0f, Main.maxTilesY * 16f - Main.screenHeight);
        }

        /// <summary>
        /// Ask the camera to lean toward <paramref name="worldPos"/> for a beat.
        /// strength: 0-1 blend toward the focus point (1 = fully centered on it at peak).
        /// holdFrames: how long to sit at full pull before releasing.
        /// A stronger request in progress is never interrupted by a weaker one, so overlapping cues from
        /// different attacks can't fight each other for the camera.
        /// </summary>
        public void RequestFocus(Vector2 worldPos, float strength, int holdFrames, int riseFrames = 10, int fallFrames = 26)
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (holdTimer > 0 && strength < pullStrength)
                return;

            focusPosition = worldPos;
            pullStrength = MathHelper.Clamp(strength, 0f, 1f);
            holdTimer = holdFrames;
            this.riseFrames = System.Math.Max(1, riseFrames);
            this.fallFrames = System.Math.Max(1, fallFrames);
        }

        public void ReleaseFocus()
        {
            holdTimer = 0;
        }
    }

    public static class IUMWCameraExtensions
    {
        public static IUMWCameraFocusPlayer IUMWCamera(this Player player) => player.GetModPlayer<IUMWCameraFocusPlayer>();
    }
}
