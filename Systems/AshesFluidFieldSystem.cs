using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using CalamityMod.FluidSimulation;

namespace CalamityLegendsComeBack.Systems
{
    public class AshesFluidFieldSystem : ModSystem
    {
        public static FluidField SharedField;

        private static readonly List<FluidSourceInfo> pendingSources = new();

        // Tracks how long it has been since a curse shot last fed the field. Used to keep the
        // (expensive, screen-covering) fluid simulation dormant while the weapon isn't firing,
        // and to let the fire dissipate smoothly instead of snapping off.
        private static int framesSinceLastSource = int.MaxValue;
        private const int IdleUpdateFrames = 180;
        private const int IdleDrawFrames = 220;

        private struct FluidSourceInfo
        {
            public Vector2 WorldPos;
            public Vector2 Velocity;
            public Color Color;
            public float Power;
        }

        public static void RegisterSource(Vector2 worldPos, Vector2 velocity, Color color, float power)
        {
            if (Main.dedServ)
                return;

            lock (pendingSources)
            {
                pendingSources.Add(new FluidSourceInfo
                {
                    WorldPos = worldPos,
                    Velocity = velocity,
                    Color = color,
                    Power = power
                });
            }
        }

        public override void Load()
        {
            if (!Main.dedServ)
            {
                On_Main.SortDrawCacheWorms += DrawFluidFieldHook;
            }
        }

        public override void Unload()
        {
            if (!Main.dedServ)
            {
                On_Main.SortDrawCacheWorms -= DrawFluidFieldHook;
            }

            // SharedField will be disposed by FluidFieldManager.OnModUnload automatically,
            // but we null it here for safety.
            SharedField = null;
            pendingSources.Clear();
        }

        private static void DrawFluidFieldHook(On_Main.orig_SortDrawCacheWorms orig, Main self)
        {
            orig(self);

            // Only blit while the fire is still alive/fading; stays dormant otherwise.
            if (SharedField is not null && !Main.gameMenu && framesSinceLastSource <= IdleDrawFrames)
            {
                Vector2 screenCenter = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
                // Draw in World space using GameViewMatrix transformation matrix.
                SharedField.Draw(screenCenter, false, Main.GameViewMatrix.TransformationMatrix, Matrix.Identity);
            }
        }

        public override void PostUpdateEverything()
        {
            if (Main.dedServ)
                return;

            // Advance the idle timer. RegisterSource pushes into pendingSources during projectile
            // AI (earlier this tick), so a non-empty queue means fire is active this frame.
            bool activeThisFrame;
            lock (pendingSources)
                activeThisFrame = pendingSources.Count > 0;

            if (activeThisFrame)
                framesSinceLastSource = 0;
            else if (framesSinceLastSource != int.MaxValue)
                framesSinceLastSource++;

            // Nothing has fed the field for a while: let it go dormant. The field stays allocated
            // (a single screen-sized field, like Calamity's own cursor field) but stops simulating
            // and drawing so it costs nothing while idle.
            if (framesSinceLastSource > IdleUpdateFrames)
            {
                if (SharedField is not null)
                    SharedField.ShouldUpdate = false;

                lock (pendingSources)
                    pendingSources.Clear();
                return;
            }

            int size = 370;
            FluidFieldManager.AdjustSizeRelativeToGraphicsQuality(ref size);
            float scale = MathHelper.Max(Main.screenWidth, Main.screenHeight) / size;

            if (SharedField is null || SharedField.Size != size)
            {
                SharedField = FluidFieldManager.CreateField(size, scale, 0.1f, 50f, 0.992f);
            }

            SharedField.ShouldUpdate = Main.instance.IsActive && (Main.LocalPlayer.miscCounter % 2 == 0);

            if (SharedField.ShouldUpdate)
            {
                List<FluidSourceInfo> sourcesToInject;
                lock (pendingSources)
                {
                    sourcesToInject = new List<FluidSourceInfo>(pendingSources);
                    pendingSources.Clear();
                }

                SharedField.UpdateAction = () =>
                {
                    int origin = SharedField.Size / 2;
                    Vector2 screenCenter = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;

                    foreach (var src in sourcesToInject)
                    {
                        Vector2 screenPos = src.WorldPos - Main.screenPosition;
                        Vector2 offset = screenPos - screenCenter;

                        int x = (int)Math.Round(offset.X / SharedField.Scale) + origin;
                        int y = (int)Math.Round(offset.Y / SharedField.Scale) + origin;

                        int area = (int)Math.Ceiling(5f / SharedField.Scale);
                        for (int i = -area; i <= area; i++)
                        {
                            for (int j = -area; j <= area; j++)
                            {
                                SharedField.CreateSource(x + i, y + j, src.Power, src.Color, i == 0 && j == 0 ? src.Velocity : Vector2.Zero);
                            }
                        }
                    }
                };
            }
            else
            {
                lock (pendingSources)
                {
                    if (pendingSources.Count > 300)
                    {
                        pendingSources.RemoveRange(0, pendingSources.Count - 300);
                    }
                }
            }
        }
    }
}
