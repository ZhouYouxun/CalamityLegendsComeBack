using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.Miao
{
    internal sealed class MiaoTraceSystem : ModSystem
    {
        private const int SummaryIntervalTicks = 30;
        private const int ProjectileTypeWarningThreshold = 50;
        private const int TotalProjectileWarningThreshold = 200;

        private static readonly Dictionary<int, Dictionary<int, int>> SpawnCountsByTrace = new();
        private static readonly HashSet<string> EmittedWarnings = new();

        private static int nextTraceId = 1;
        private static int summaryTimer;

        public static string LogPath => Path.Combine(Main.SavePath, "MiaoGunTrace.log");

        public static int AllocateTraceId() => nextTraceId++;

        public static void Log(string text)
        {
            try
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {text}{Environment.NewLine}");
            }
            catch
            {
            }
        }

        public static void RecordSpawn(int traceId, int projectileType)
        {
            if (traceId < 0)
                return;

            if (!SpawnCountsByTrace.TryGetValue(traceId, out Dictionary<int, int> counts))
            {
                counts = new Dictionary<int, int>();
                SpawnCountsByTrace[traceId] = counts;
            }

            counts.TryGetValue(projectileType, out int count);
            counts[projectileType] = count + 1;
        }

        public static string GetProjectileName(int projectileType)
        {
            ModProjectile modProjectile = ProjectileLoader.GetProjectile(projectileType);
            if (modProjectile != null)
                return $"{modProjectile.Mod.Name}/{modProjectile.Name} ({projectileType})";

            string vanillaName = ProjectileID.Search.GetName(projectileType);
            if (!string.IsNullOrEmpty(vanillaName))
                return $"{vanillaName} ({projectileType})";

            return Language.GetTextValue("LegacyProjectile.0") + $" ({projectileType})";
        }

        public override void OnWorldLoad()
        {
            SpawnCountsByTrace.Clear();
            EmittedWarnings.Clear();
            nextTraceId = 1;
            summaryTimer = 0;
            Log("========== MiaoGun trace session started ==========");
        }

        public override void PostUpdateEverything()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            summaryTimer++;
            if (summaryTimer < SummaryIntervalTicks)
                return;

            summaryTimer = 0;
            WriteAliveSummaries();
        }

        private static void WriteAliveSummaries()
        {
            Dictionary<int, Dictionary<int, int>> aliveByTrace = new();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active)
                    continue;

                MiaoTraceProjectile tracker = projectile.GetGlobalProjectile<MiaoTraceProjectile>();
                if (tracker.TraceId < 0)
                    continue;

                if (!aliveByTrace.TryGetValue(tracker.TraceId, out Dictionary<int, int> counts))
                {
                    counts = new Dictionary<int, int>();
                    aliveByTrace[tracker.TraceId] = counts;
                }

                counts.TryGetValue(projectile.type, out int count);
                counts[projectile.type] = count + 1;
            }

            foreach ((int traceId, Dictionary<int, int> counts) in aliveByTrace)
            {
                int total = counts.Values.Sum();
                string detail = string.Join(", ", counts
                    .OrderByDescending(pair => pair.Value)
                    .Take(12)
                    .Select(pair => $"{GetProjectileName(pair.Key)} x{pair.Value}"));

                Log($"[Shot {traceId}] ALIVE total={total}: {detail}");
                EmitWarnings(traceId, total, counts);
            }
        }

        private static void EmitWarnings(int traceId, int total, Dictionary<int, int> counts)
        {
            if (total >= TotalProjectileWarningThreshold && EmittedWarnings.Add($"{traceId}:total:{total / TotalProjectileWarningThreshold}"))
                Log($"[Shot {traceId}] WARNING runaway total active projectiles={total}");

            foreach ((int projectileType, int count) in counts)
            {
                if (count < ProjectileTypeWarningThreshold)
                    continue;

                string warningKey = $"{traceId}:type:{projectileType}:{count / ProjectileTypeWarningThreshold}";
                if (EmittedWarnings.Add(warningKey))
                    Log($"[Shot {traceId}] WARNING runaway projectile: {GetProjectileName(projectileType)} activeCount={count}");
            }
        }
    }
}
