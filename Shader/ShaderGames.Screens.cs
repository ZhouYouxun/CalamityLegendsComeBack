using System;
using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.DarksunFragment;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Shader
{
    public sealed partial class ShaderGames
    {
        private const string DarkPlasmaKamuiVortexRegistrationName = "DarkPlasmaKamuiVortex";
        private const string DarksunFragmentGravitationalLensingRegistrationName = "DarksunFragmentGravitationalLensing";
        private const int DarkPlasmaEffectID = 32;
        private const float DarkPlasmaVortexRadius = 16f * 16f;
        private const float DarksunOuterVortexTextureRadius = 256f;
        private const float DarksunOuterVortexScaleDivisor = 96f;
        private const float DarksunLensingRadiusPadding = 5f * 16f;

        public static Effect BlackHoleDistortionShader => GetEffect("BlackHoleDistortion");
        public static Effect ScreenSimplyDistortedShader => GetEffect("ScreenSimplyDistorted");
        public static Effect DarkPlasmaKamuiVortexShader => GetEffect("DarkPlasmaKamuiVortex");
        public static Effect DarksunFragmentGravitationalLensingShader => GetEffect("DarksunFragmentGravitationalLensing");

        private static readonly ShaderDefinition[] ScreenShaders =
        [
            // Screen shaders are activated through Filters.Scene.
            new("BlackHoleDistortion", ShaderCategory.Screen, "Pass1", "BlackHoleDistortion"),
            new("DarkPlasmaKamuiVortex", ShaderCategory.Screen, "Pass1", DarkPlasmaKamuiVortexRegistrationName),
            new("DarksunFragmentGravitationalLensing", ShaderCategory.Screen, "Pass1", DarksunFragmentGravitationalLensingRegistrationName),
            new("ScreenSimplyDistorted", ShaderCategory.Screen, "Pass1", "ScreenSimplyDistorted")
        ];

        private static void RegisterScreenShaders()
        {
            foreach (ShaderDefinition shader in ScreenShaders)
                RegisterSceneFilter(shader.Name, shader.PassName, shader.RegistrationName, EffectPriority.Medium);
        }

        private static void UpdateScreenShaderParameters()
        {
            foreach (ShaderDefinition shader in ScreenShaders)
            {
                string key = SceneFilterKey(shader.RegistrationName);
                Filter filter = Filters.Scene[key];
                if (filter is null || !filter.IsActive())
                    continue;

                Effect effect = filter.GetShader().Shader;
                switch (shader.Name)
                {
                    case "BlackHoleDistortion":
                        effect.Parameters["uCenter"]?.SetValue(new Vector2(0.5f, 0.5f));
                        effect.Parameters["uRadius"]?.SetValue(0.42f);
                        effect.Parameters["uStrength"]?.SetValue(0.16f + 0.04f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.4f));
                        effect.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
                        break;

                    case "ScreenSimplyDistorted":
                        effect.Parameters["uScreenResolution"]?.SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
                        break;
                }
            }

            UpdateDarkPlasmaKamuiVortex();
            UpdateDarksunFragmentGravitationalLensing();
        }

        private static void UpdateDarkPlasmaKamuiVortex()
        {
            string key = SceneFilterKey(DarkPlasmaKamuiVortexRegistrationName);
            Filter filter = Filters.Scene[key];
            if (filter is null)
                return;

            if (!TryFindDarkPlasmaTarget(out Projectile target, out float opacity))
            {
                if (filter.IsActive())
                    Filters.Scene.Deactivate(key);

                return;
            }

            if (!filter.IsActive())
            {
                Filters.Scene.Activate(key, target.Center);
                filter = Filters.Scene[key];
            }

            ScreenShaderData shaderData = filter.GetShader();
            shaderData.UseTargetPosition(target.Center);
            Effect effect = shaderData.Shader;
            float lifeProgress = Utils.GetLerpValue(420f, 0f, target.timeLeft, true);
            float pulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3.2f + target.identity * 0.17f);

            effect.Parameters["uRadius"]?.SetValue(DarkPlasmaVortexRadius);
            effect.Parameters["uStrength"]?.SetValue((MathHelper.Lerp(1.65f, 1.95f, lifeProgress) + 0.05f * pulse) * opacity);
            effect.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
        }

        private static void UpdateDarksunFragmentGravitationalLensing()
        {
            string key = SceneFilterKey(DarksunFragmentGravitationalLensingRegistrationName);
            Filter filter = Filters.Scene[key];
            if (filter is null)
                return;

            if (!TryFindDarksunBlackSunTarget(out Projectile target, out float opacity))
            {
                if (filter.IsActive())
                    Filters.Scene.Deactivate(key);

                return;
            }

            if (!filter.IsActive())
            {
                Filters.Scene.Activate(key, target.Center);
                filter = Filters.Scene[key];
            }

            int level = Utils.Clamp((int)target.ai[0], 1, DarksunFragmentBlackSun.MaxLevel);
            float blackSunRadius = DarksunFragmentBlackSun.GetRadiusForLevel(level);
            float levelProgress = Utils.GetLerpValue(1f, DarksunFragmentBlackSun.MaxLevel, level, true);

            ScreenShaderData shaderData = filter.GetShader();
            shaderData.UseTargetPosition(target.Center);
            Effect effect = shaderData.Shader;
            effect.Parameters["uRadius"]?.SetValue(GetDarksunOuterVortexRadius(level));
            effect.Parameters["uHorizonRadius"]?.SetValue(blackSunRadius * MathHelper.Lerp(0.5f, 0.94f, opacity));
            effect.Parameters["uStrength"]?.SetValue(MathHelper.Lerp(0.42f, 0.76f, levelProgress) * opacity);
        }

        private static bool TryFindDarkPlasmaTarget(out Projectile target, out float opacity)
        {
            target = null;
            opacity = 0f;
            int shpbType = ModContent.ProjectileType<NewLegendSHPB>();
            float bestScore = float.MaxValue;

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.type != shpbType || (int)projectile.ai[0] != DarkPlasmaEffectID)
                    continue;

                if (projectile.GetGlobalProjectile<DarkPlasma_GP>().releaseOnly)
                    continue;

                float projectileOpacity = GetDarkPlasmaOpacity(projectile);
                if (projectileOpacity <= 0.02f)
                    continue;

                Vector2 screenCenter = GetZoomedScreenPosition(projectile.Center);
                if (!IsWithinScreenInfluence(screenCenter, DarkPlasmaVortexRadius * GetMaximumZoom()))
                    continue;

                float score = Vector2.DistanceSquared(screenCenter, new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f);
                if (score >= bestScore)
                    continue;

                bestScore = score;
                target = projectile;
                opacity = projectileOpacity;
            }

            return target is not null;
        }

        private static bool TryFindDarksunBlackSunTarget(out Projectile target, out float opacity)
        {
            target = null;
            opacity = 0f;
            int blackSunType = ModContent.ProjectileType<DarksunFragmentBlackSun>();
            float bestScore = float.MaxValue;

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.type != blackSunType)
                    continue;

                float projectileOpacity = GetDarksunBlackSunOpacity(projectile);
                if (projectileOpacity <= 0.02f)
                    continue;

                int level = Utils.Clamp((int)projectile.ai[0], 1, DarksunFragmentBlackSun.MaxLevel);
                float radius = GetDarksunOuterVortexRadius(level);
                Vector2 screenCenter = GetZoomedScreenPosition(projectile.Center);
                if (!IsWithinScreenInfluence(screenCenter, (radius + 24f) * GetMaximumZoom()))
                    continue;

                float score = Vector2.DistanceSquared(screenCenter, new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f);
                if (score >= bestScore)
                    continue;

                bestScore = score;
                target = projectile;
                opacity = projectileOpacity;
            }

            return target is not null;
        }

        private static float GetDarkPlasmaOpacity(Projectile projectile)
        {
            if (projectile.timeLeft > 360)
                return Utils.GetLerpValue(420f, 360f, projectile.timeLeft, true);

            if (projectile.timeLeft >= 60)
                return 1f;

            return Utils.GetLerpValue(0f, 60f, projectile.timeLeft, true);
        }

        private static float GetDarksunBlackSunOpacity(Projectile projectile)
        {
            if (projectile.timeLeft < 24)
                return projectile.timeLeft / 24f;

            return Utils.GetLerpValue(0f, 18f, projectile.localAI[0], true);
        }

        private static bool IsWithinScreenInfluence(Vector2 screenCenter, float radius)
        {
            return screenCenter.X > -radius &&
                screenCenter.X < Main.screenWidth + radius &&
                screenCenter.Y > -radius &&
                screenCenter.Y < Main.screenHeight + radius;
        }

        private static Vector2 GetScreenSize()
        {
            return new Vector2(Math.Max(Main.screenWidth, 1), Math.Max(Main.screenHeight, 1));
        }

        private static float GetDarksunOuterVortexRadius(int level)
        {
            float blackSunRadius = DarksunFragmentBlackSun.GetRadiusForLevel(level);
            float pulse = 1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 4.2f) * 0.05f;
            float textureScale = blackSunRadius / DarksunOuterVortexScaleDivisor * pulse * (1.38f + level * 0.035f);
            return DarksunOuterVortexTextureRadius * textureScale + 5f + level * 0.7f + DarksunLensingRadiusPadding;
        }

        private static Vector2 GetZoomedScreenPosition(Vector2 worldPosition)
        {
            Vector2 screenSize = GetScreenSize();
            Vector2 screenCenter = screenSize * 0.5f;
            Vector2 unzoomedPosition = worldPosition - Main.screenPosition;
            Vector2 offset = unzoomedPosition - screenCenter;
            Vector2 zoom = Main.GameViewMatrix.Zoom;

            return screenCenter + new Vector2(offset.X * zoom.X, offset.Y * zoom.Y);
        }

        private static float GetMaximumZoom()
        {
            Vector2 zoom = Main.GameViewMatrix.Zoom;
            return Math.Max(zoom.X, zoom.Y);
        }
    }
}
