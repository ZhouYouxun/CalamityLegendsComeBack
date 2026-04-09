using Microsoft.Xna.Framework;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft
{
    public interface IYCLeftBeamSource
    {
        Vector2 DesiredAimDirection { get; }
        Vector2 ForwardDirection { get; }
        float GetBeamLength(float defaultLength, float forwardOffset);
        float GetBeamTurnRateRadians(float defaultTurnRateRadians);

        void OnLeftBeamHit(NPC target, NPC.HitInfo hit, int damageDone, Projectile beamProjectile);
    }
}
