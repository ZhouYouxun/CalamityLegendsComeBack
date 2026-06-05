using CalamityMod;
using CalamityMod.Projectiles.Turret;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClickTurret
{
    internal enum MilitaryTurretKind
    {
        Lab,
        Water,
        Onyx,
        Fire,
        Ice,
        Laser,
        Plague
    }

    internal readonly struct MilitaryTurretStats
    {
        public MilitaryTurretStats(
            string displayName,
            string bodyTexture,
            string headTexture,
            int projectileType,
            int baseDamage,
            float knockback,
            float shootSpeed,
            int startupDelay,
            int useTime,
            float maxRange,
            float shootForwardOffset,
            float maxAngleDeviance,
            float maxDeltaAngle,
            float closeAimThreshold,
            float closeAimLerp,
            Color themeColor)
        {
            DisplayName = displayName;
            BodyTexture = bodyTexture;
            HeadTexture = headTexture;
            ProjectileType = projectileType;
            BaseDamage = baseDamage;
            Knockback = knockback;
            ShootSpeed = shootSpeed;
            StartupDelay = startupDelay;
            UseTime = useTime;
            MaxRange = maxRange;
            ShootForwardOffset = shootForwardOffset;
            MaxAngleDeviance = maxAngleDeviance;
            MaxDeltaAngle = maxDeltaAngle;
            CloseAimThreshold = closeAimThreshold;
            CloseAimLerp = closeAimLerp;
            ThemeColor = themeColor;
        }

        public string DisplayName { get; }
        public string BodyTexture { get; }
        public string HeadTexture { get; }
        public int ProjectileType { get; }
        public int BaseDamage { get; }
        public float Knockback { get; }
        public float ShootSpeed { get; }
        public int StartupDelay { get; }
        public int UseTime { get; }
        public float MaxRange { get; }
        public float ShootForwardOffset { get; }
        public float MaxAngleDeviance { get; }
        public float MaxDeltaAngle { get; }
        public float CloseAimThreshold { get; }
        public float CloseAimLerp { get; }
        public Color ThemeColor { get; }
    }

    internal static class MilitaryTurretUtility
    {
        public const int MaxTurrets = 8;
        public const int MinSpacingTiles = 18;
        public const float MinSpacing = MinSpacingTiles * 16f;
        public const int TurretLifetime = 5 * 60 * 60;
        public const int ManaPerCall = 40;

        public static MilitaryTurretKind SelectBiomeTurret(Player player)
        {
            var calamityPlayer = player.Calamity();

            if (player.ZoneJungle)
                return MilitaryTurretKind.Plague;

            if (player.ZoneSnow)
                return MilitaryTurretKind.Ice;

            if (player.ZoneUnderworldHeight || calamityPlayer.ZoneCalamity)
                return MilitaryTurretKind.Fire;

            if (calamityPlayer.ZoneSunkenSea || calamityPlayer.ZoneSulphur || calamityPlayer.ZoneAbyss || player.ZoneBeach)
                return MilitaryTurretKind.Water;

            if (calamityPlayer.ZoneAstral || player.ZoneSkyHeight || player.ZoneHallow)
                return MilitaryTurretKind.Laser;

            if (player.ZoneDesert || player.ZoneCorrupt || player.ZoneCrimson)
                return MilitaryTurretKind.Onyx;

            return MilitaryTurretKind.Lab;
        }

        public static MilitaryTurretStats GetStats(MilitaryTurretKind kind)
        {
            return kind switch
            {
                MilitaryTurretKind.Water => new MilitaryTurretStats(
                    "Water Turret",
                    "CalamityMod/Tiles/PlayerTurrets/PlayerWaterTurret",
                    "CalamityMod/Tiles/PlayerTurrets/WaterTurretHead",
                    ModContent.ProjectileType<WaterShot>(),
                    14,
                    6.5f,
                    6.5f,
                    25,
                    25,
                    300f,
                    24f,
                    MathHelper.ToRadians(12f),
                    MathHelper.ToRadians(5f),
                    MathHelper.ToRadians(8f),
                    0.08f,
                    new Color(70, 185, 255)),

                MilitaryTurretKind.Onyx => new MilitaryTurretStats(
                    "Onyx Turret",
                    "CalamityMod/Tiles/PlayerTurrets/PlayerOnyxTurret",
                    "CalamityMod/Tiles/PlayerTurrets/OnyxTurretHead",
                    ModContent.ProjectileType<OnyxShot>(),
                    20,
                    2f,
                    6.5f,
                    55,
                    55,
                    300f,
                    30f,
                    MathHelper.ToRadians(36f),
                    MathHelper.ToRadians(5f),
                    MathHelper.ToRadians(2f),
                    0.2f,
                    new Color(160, 105, 255)),

                MilitaryTurretKind.Fire => new MilitaryTurretStats(
                    "Fire Turret",
                    "CalamityMod/Tiles/PlayerTurrets/PlayerFireTurret",
                    "CalamityMod/Tiles/PlayerTurrets/FireTurretHead",
                    ModContent.ProjectileType<FireShot>(),
                    21,
                    1f,
                    8f,
                    10,
                    6,
                    300f,
                    24f,
                    MathHelper.ToRadians(36f),
                    MathHelper.ToRadians(5f),
                    MathHelper.ToRadians(12f),
                    0.08f,
                    new Color(255, 105, 45)),

                MilitaryTurretKind.Ice => new MilitaryTurretStats(
                    "Ice Turret",
                    "CalamityMod/Tiles/PlayerTurrets/PlayerIceTurret",
                    "CalamityMod/Tiles/PlayerTurrets/IceTurretHead",
                    ModContent.ProjectileType<IceShot>(),
                    80,
                    4f,
                    8f,
                    45,
                    45,
                    450f,
                    24f,
                    MathHelper.ToRadians(36f),
                    MathHelper.ToRadians(3f),
                    MathHelper.ToRadians(12f),
                    0.08f,
                    new Color(135, 235, 255)),

                MilitaryTurretKind.Laser => new MilitaryTurretStats(
                    "Laser Turret",
                    "CalamityMod/Tiles/PlayerTurrets/PlayerLaserTurret",
                    "CalamityMod/Tiles/PlayerTurrets/LaserTurretHead",
                    ModContent.ProjectileType<LaserShot>(),
                    60,
                    2.5f,
                    11f * 0.64f,
                    60,
                    60,
                    1000f,
                    36f,
                    MathHelper.ToRadians(5f),
                    MathHelper.ToRadians(6.5f),
                    MathHelper.ToRadians(1f),
                    1f,
                    new Color(255, 120, 255)),

                MilitaryTurretKind.Plague => new MilitaryTurretStats(
                    "Plague Turret",
                    "CalamityMod/Tiles/PlayerTurrets/PlayerPlagueTurret",
                    "CalamityMod/Tiles/PlayerTurrets/PlagueTurretHead",
                    ModContent.ProjectileType<PlagueShot>(),
                    100,
                    6f,
                    16f,
                    50,
                    50,
                    900f,
                    24f,
                    MathHelper.ToRadians(50f),
                    MathHelper.ToRadians(6f),
                    MathHelper.ToRadians(12f),
                    0.08f,
                    new Color(95, 235, 55)),

                _ => new MilitaryTurretStats(
                    "Lab Turret",
                    "CalamityMod/Tiles/PlayerTurrets/PlayerLabTurret",
                    "CalamityMod/Tiles/PlayerTurrets/LabTurretHead",
                    ModContent.ProjectileType<DraedonLaser>(),
                    40,
                    3.5f,
                    5f,
                    10,
                    55,
                    600f,
                    6f,
                    MathHelper.ToRadians(12f),
                    MathHelper.ToRadians(4f),
                    MathHelper.ToRadians(12f),
                    0.08f,
                    new Color(105, 210, 255))
            };
        }

        public static int CountOwnedCalls(Player player)
        {
            int beaconType = ModContent.ProjectileType<MilitaryCallerBeacon>();
            int dropPodType = ModContent.ProjectileType<MilitaryTurretDropPod>();
            int turretType = ModContent.ProjectileType<MilitaryFriendlyTurret>();
            int count = 0;

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != player.whoAmI)
                    continue;

                if (projectile.type == beaconType || projectile.type == dropPodType || projectile.type == turretType)
                    count++;
            }

            return count;
        }

        public static bool CanIssueCall(Player player, Vector2 targetWorld, out string reason)
        {
            Vector2 restingPoint = FindRestingPoint(targetWorld);
            if (IsTooCloseToActiveTurret(player, restingPoint))
            {
                reason = "部署位点过近";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public static bool CanDeployTurret(Player player, Vector2 restingPoint, out string reason)
        {
            if (IsTooCloseToActiveTurret(player, restingPoint))
            {
                reason = "部署位点过近";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public static void ReplaceOldestTurretIfAtCapacity(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            while (CountActiveTurrets(player) >= MaxTurrets)
            {
                Projectile oldestTurret = FindOldestTurret(player);
                if (oldestTurret == null)
                    return;

                oldestTurret.ai[2] = 1f;
                oldestTurret.netUpdate = true;
                oldestTurret.Kill();
            }
        }

        public static Vector2 FindRestingPoint(Vector2 targetWorld)
        {
            int tileX = Utils.Clamp((int)(targetWorld.X / 16f), 10, Main.maxTilesX - 10);
            int tileY = Utils.Clamp((int)(targetWorld.Y / 16f), 10, Main.maxTilesY - 10);
            int lowerBound = Utils.Clamp(tileY + 90, 10, Main.maxTilesY - 10);
            int upperBound = Utils.Clamp(tileY - 45, 10, Main.maxTilesY - 10);

            for (int y = tileY; y <= lowerBound; y++)
            {
                if (IsSolidGround(tileX, y))
                    return new Vector2(tileX * 16f + 8f, y * 16f);
            }

            for (int y = tileY; y >= upperBound; y--)
            {
                if (IsSolidGround(tileX, y))
                    return new Vector2(tileX * 16f + 8f, y * 16f);
            }

            return targetWorld;
        }

        public static void NotifyFailure(Player player, string reason, Vector2 worldPosition)
        {
            SpawnRejectedBurst(worldPosition);

            if (player.whoAmI == Main.myPlayer && !string.IsNullOrEmpty(reason))
                CombatText.NewText(player.Hitbox, Color.OrangeRed, reason);
        }

        public static void SpawnRejectedBurst(Vector2 worldPosition)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(worldPosition + Main.rand.NextVector2Circular(18f, 18f), DustID.RedTorch);
                dust.velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 6f);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.9f, 1.35f);
            }
        }

        private static int CountActiveTurrets(Player player)
        {
            int turretType = ModContent.ProjectileType<MilitaryFriendlyTurret>();
            int count = 0;

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner == player.whoAmI && projectile.type == turretType)
                    count++;
            }

            return count;
        }

        private static Projectile FindOldestTurret(Player player)
        {
            int turretType = ModContent.ProjectileType<MilitaryFriendlyTurret>();
            Projectile oldest = null;

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != player.whoAmI || projectile.type != turretType)
                    continue;

                if (oldest == null ||
                    projectile.timeLeft < oldest.timeLeft ||
                    projectile.timeLeft == oldest.timeLeft && projectile.identity < oldest.identity)
                {
                    oldest = projectile;
                }
            }

            return oldest;
        }

        private static bool IsTooCloseToActiveTurret(Player player, Vector2 restingPoint)
        {
            int turretType = ModContent.ProjectileType<MilitaryFriendlyTurret>();

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != player.whoAmI || projectile.type != turretType)
                    continue;

                if (Vector2.Distance(projectile.Bottom, restingPoint) < MinSpacing)
                    return true;
            }

            return false;
        }

        private static bool IsSolidGround(int tileX, int tileY)
        {
            Tile tile = Framing.GetTileSafely(tileX, tileY);
            return tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
        }
    }
}
