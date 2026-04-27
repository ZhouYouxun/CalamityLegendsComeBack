using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.Miao
{
    public static class MiaoGunProjectileCatalog
    {
        private const string ProjectileListPath = "Weapons/A_Dev/Miao/所有弹幕的内部名字【全是模组弹幕】.txt";

        private static int[] cachedProjectileTypes = Array.Empty<int>();
        private static bool initialized;

        public static bool TryGetRandomProjectileType(out int projectileType)
        {
            EnsureLoaded();

            if (cachedProjectileTypes.Length == 0)
            {
                projectileType = ProjectileID.Bullet;
                return true;
            }

            projectileType = cachedProjectileTypes[Main.rand.Next(cachedProjectileTypes.Length)];
            return true;
        }

        private static void EnsureLoaded()
        {
            if (initialized)
                return;

            initialized = true;

            Mod thisMod = ModContent.GetInstance<CalamityLegendsComeBack>();
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamityMod))
                return;

            byte[] rawProjectileList;
            try
            {
                rawProjectileList = thisMod.GetFileBytes(ProjectileListPath);
            }
            catch
            {
                return;
            }

            List<int> projectileTypes = new();
            HashSet<int> seenTypes = new();
            string projectileText = Encoding.UTF8.GetString(rawProjectileList);
            string[] projectileNames = projectileText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string rawName in projectileNames)
            {
                string projectileName = rawName.Trim();
                if (projectileName.Length == 0 || projectileName.StartsWith("//", StringComparison.Ordinal))
                    continue;

                if (calamityMod.TryFind(projectileName, out ModProjectile modProjectile) && seenTypes.Add(modProjectile.Type))
                    projectileTypes.Add(modProjectile.Type);
            }

            cachedProjectileTypes = projectileTypes.ToArray();
        }
    }
}
