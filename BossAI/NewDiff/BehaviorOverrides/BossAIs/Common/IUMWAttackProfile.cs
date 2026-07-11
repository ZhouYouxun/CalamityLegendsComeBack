using Microsoft.Xna.Framework;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common
{
    internal enum IUMWPatternKind
    {
        LateralDashBarrage,
        OrbitingCrossfire,
        FallingCurtain,
        ConvergingFan,
        SpiralBloom,
        MinefieldPulse,
        VortexPressure,
        SniperGrid
    }

    internal sealed record IUMWAttackProfile(
        string Name,
        IUMWPatternKind Kind,
        string PrimaryProjectile,
        Color Color,
        int DustType,
        string SecondaryProjectile = null,
        int Duration = 150,
        int FireRate = 24,
        float Speed = 10f,
        int Count = 4,
        float Spread = 0.58f);
}
