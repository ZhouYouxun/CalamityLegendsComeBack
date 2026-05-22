using System;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.SlowDown
{
    internal sealed class DesertEagleSlowDownSystem : ModSystem
    {
        private const float SlowMotionTimeScale = 0.5f;

        public static float TimeScale => SlowMotionActive ? SlowMotionTimeScale : 1f;
        public static bool SlowMotionActive { get; private set; }

        public override void Load()
        {
            On_Main.DoUpdate += SlowDownGameTime;
            On_SoundEngine.PlaySound_refSoundStyle_Nullable1_SoundUpdateCallback += SlowDownSoundPitch;
        }

        public override void Unload()
        {
            On_Main.DoUpdate -= SlowDownGameTime;
            On_SoundEngine.PlaySound_refSoundStyle_Nullable1_SoundUpdateCallback -= SlowDownSoundPitch;
            SlowMotionActive = false;
        }

        private static SlotId SlowDownSoundPitch(
            On_SoundEngine.orig_PlaySound_refSoundStyle_Nullable1_SoundUpdateCallback orig,
            ref SoundStyle style,
            Vector2? position,
            SoundUpdateCallback updateCallback)
        {
            if (SlowMotionActive)
                style = style with { Pitch = MathHelper.Clamp(style.Pitch + MathF.Log2(TimeScale), -1f, 1f) };

            return orig(ref style, position, updateCallback);
        }

        private static void SlowDownGameTime(On_Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
        {
            SlowMotionActive = ShouldSlowDown();

            if (!SlowMotionActive)
            {
                orig(self, ref gameTime);
                return;
            }

            GameTime scaledGameTime = new(
                gameTime.TotalGameTime,
                TimeSpan.FromTicks((long)(gameTime.ElapsedGameTime.Ticks * SlowMotionTimeScale)),
                gameTime.IsRunningSlowly);

            orig(self, ref scaledGameTime);
        }

        private static bool ShouldSlowDown()
        {
            if (Main.netMode != NetmodeID.SinglePlayer || Main.gameMenu || Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers)
                return false;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead || player.HeldItem.type != ModContent.ItemType<DesertEagle>())
                return false;

            return player.ownedProjectileCounts[ModContent.ProjectileType<DesertEagleHoldout>()] > 0;
        }
    }
}
