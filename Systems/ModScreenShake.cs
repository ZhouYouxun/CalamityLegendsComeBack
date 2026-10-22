using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace CalamityLegendsComeBack.Systems
{
    internal static class ModScreenShake
    {
        private const float FullStrengthDistance = 180f;
        private const float MaximumDistance = 1800f;

        public static float Factor(Player viewer, Vector2 source)
        {
            float distance = Vector2.Distance(viewer.Center, source);
            return Utils.GetLerpValue(MaximumDistance, FullStrengthDistance, distance, true);
        }

        private static float Attenuate(Player viewer, Vector2 source, float power) => power * Factor(viewer, source);

        public static void Max(Player viewer, Vector2 source, float power)
        {
            if (Main.dedServ || viewer == null || !viewer.active)
                return;

            var calamityPlayer = viewer.Calamity();
            calamityPlayer.GeneralScreenShakePower = Math.Max(calamityPlayer.GeneralScreenShakePower,
                Attenuate(viewer, source, power));
        }

        public static void Set(Player viewer, Vector2 source, float power)
        {
            if (Main.dedServ || viewer == null || !viewer.active)
                return;

            float attenuated = Attenuate(viewer, source, power);
            if (attenuated > 0f)
                viewer.Calamity().GeneralScreenShakePower = attenuated;
        }

        public static void Add(Player viewer, Vector2 source, float power)
        {
            if (Main.dedServ || viewer == null || !viewer.active)
                return;

            viewer.Calamity().GeneralScreenShakePower += Attenuate(viewer, source, power);
        }
    }
}
