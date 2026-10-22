using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;


namespace CalamityLegendsComeBack.Weapons.GaelsGreatsword
{
    internal static class GaelGreatswordProgression
    {
        public static int GetStage()
        {
            int stage = 0;
            if (NPC.downedBoss1)
                stage++;
            if (NPC.downedBoss2 || NPC.downedQueenBee)
                stage++;
            if (NPC.downedBoss3)
                stage++;
            if (Main.hardMode)
                stage += 2;
            return Math.Min(stage, 5);
        }

        public static int GetBaseDamage()
        {
            return GetStage() switch
            {
                0 => 28,
                1 => 35,
                2 => 43,
                3 => 52,
                4 => 66,
                _ => 78,
            };
        }

        public static int GetLeftUseTime(Player player)
        {
            float speed = Math.Max(0.5f, player.GetAttackSpeed(DamageClass.Melee));
            int raw = (int)MathF.Round(23f / speed);
            return Math.Max(12, raw);
        }

        public static int GetSwingDuration(Player player, bool followupSlash)
        {
            float baseDuration = followupSlash ? 19f : 23f;
            float speed = Math.Max(0.5f, player.GetAttackSpeed(DamageClass.Melee));
            return Math.Max(followupSlash ? 12 : 14, (int)MathF.Round(baseDuration / speed));
        }

        public static int GetRepeatHitCooldown()
        {
            return GetStage() switch
            {
                0 => 22,
                1 => 18,
                2 => 15,
                3 => 12,
                _ => 9,
            };
        }

        public static float GetPassiveRagePerFrame()
        {
            return GetStage() switch
            {
                0 => 0.035f,
                1 => 0.045f,
                2 => 0.055f,
                3 => 0.065f,
                _ => 0.075f,
            };
        }

        public static int GetFlightFrames()
        {
            if (Main.hardMode)
                return 120;
            if (NPC.downedBoss3)
                return 82;
            if (NPC.downedBoss2 || NPC.downedQueenBee)
                return 56;
            if (NPC.downedBoss1)
                return 32;
            return 0;
        }

        public static float GetFlightAcceleration()
        {
            return Main.hardMode ? 0.42f : NPC.downedBoss3 ? 0.34f : 0.25f;
        }

        public static float GetFlightTopSpeed()
        {
            return Main.hardMode ? 6.4f : NPC.downedBoss3 ? 5.2f : 4.1f;
        }
    }
}
