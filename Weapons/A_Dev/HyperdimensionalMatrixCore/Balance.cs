namespace CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore
{
    // Intervals, lifetimes, and hit cooldowns are measured in frames.
    // Damage multipliers use the core's current damage, except the explosion multiplier,
    // which is applied to its parent orbital projection's damage.
    public static class Balance
    {
        public const int FormDuration = 420;
        public const float TargetingRange = 2400f;

        public const int PiercingAttackInterval = 18;
        public const int PiercingProjectileCount = 8;
        public const float PiercingProjectileDamageMultiplier = 0.24f;
        public const int PiercingProjectilePenetration = 8;
        public const int PiercingProjectileLocalHitCooldown = 8;
        public const float PiercingProjectileSpawnRadius = 24f;
        public const float PiercingProjectileInitialSpeed = 24f;
        public const float PiercingProjectileHomingSpeed = 33f;
        public const float PiercingProjectileMaxTurn = 0.15f;
        public const float PiercingProjectileAcceleration = 0.08f;

        public const int OrbitalBurstCount = 5;
        public const int OrbitalBurstShotInterval = 5;
        public const int OrbitalBurstCooldown = 48;
        public const int OrbitalProjectileCountMin = 6;
        public const int OrbitalProjectileCountMax = 9;
        public const float OrbitalProjectileDamageMultiplier = 0.5f;
        public const int OrbitalProjectilePenetration = 1;
        public const int OrbitalProjectileLocalHitCooldown = 10;
        public const int OrbitalProjectileLifetime = 270;
        public const int OrbitalMaxSimultaneousTargets = 6;
        public const float OrbitalProjectileSpeedMultiplier = 1.3f;
        public const float OrbitalProjectileGravity = 0.55f;
        public const float OrbitalProjectileMaxFallSpeed = 39f;
        public const float OrbitalProjectileSpawnSpreadX = 260f;
        public const float OrbitalProjectileAimSpreadX = 110f;
        public const float OrbitalProjectileAimSpreadY = 70f;
        public const float OrbitalProjectileHorizontalCorrectionStrength = 0.006f;
        public const float OrbitalProjectileMaxHorizontalCorrection = 1.6f;
        public const float OrbitalProjectileHorizontalCorrectionLerp = 0.04f;
        public const float OrbitalExplosionDamageMultiplier = 1.15f;
        public const int OrbitalExplosionPenetration = -1;
        public const int OrbitalExplosionLocalHitCooldown = -1;
        public const int OrbitalExplosionSize = 75;

        public const int FractureProjectileLifetime = 132;
        public const int FractureAttackInterval = FractureProjectileLifetime;
        public const int FractureProjectileCount = 1;
        public const float FractureProjectileDamageMultiplier = 1f;
        public const int FractureProjectilePenetration = -1;
        public const int FractureProjectileLocalHitCooldown = 5;

        public const int HyperdimensionalAttackInterval = 1;
        public const int HyperdimensionalProjectileCount = 1;
        public const float HyperdimensionalProjectileDamageMultiplier = 0.48f;
        public const int HyperdimensionalProjectilePenetration = -1;
        public const int HyperdimensionalProjectileLocalHitCooldown = 5;
        public const float HyperdimensionalBeamLength = 3600f;

        public static int OrbitalBurstCycleLength =>
            (OrbitalBurstCount - 1) * OrbitalBurstShotInterval + OrbitalBurstCooldown;
    }
}
