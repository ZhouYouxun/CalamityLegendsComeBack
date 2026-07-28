using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.Passive_QuickDash
{
    // Stores the other end of Vortex Eye's current teleport pair. This is deliberately
    // player state instead of projectile state: the pair remains usable after the
    // one-frame teleport controller has finished.
    internal sealed class BrinyBaronVortexEyeTeleportPlayer : ModPlayer
    {
        private Vector2 returnAnchor;

        public bool HasReturnAnchor { get; private set; }

        public bool CanReturn => HasReturnAnchor && Player.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>().IsCoolingDown;

        public void BeginCycle(Vector2 origin)
        {
            returnAnchor = origin;
            HasReturnAnchor = true;
        }

        public Vector2 UseReturnAnchor(Vector2 currentPosition)
        {
            Vector2 destination = returnAnchor;
            returnAnchor = currentPosition;
            HasReturnAnchor = true;
            return destination;
        }

        public void ClearReturnAnchor()
        {
            HasReturnAnchor = false;
            returnAnchor = Vector2.Zero;
        }

        public override void UpdateDead() => ClearReturnAnchor();

        public override void PostUpdate()
        {
            if (!Player.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>().IsCoolingDown)
                ClearReturnAnchor();
        }
    }
}
