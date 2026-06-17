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
        private const float DarkPlasmaVortexRadius = 8f * 16f;
        private const float DarkPlasmaDistortionStrengthMultiplier = 0.335f;
        private const float DarkPlasmaFadeOutDuration = 30f;
        private const float DarksunOuterVortexTextureRadius = 256f;
        private const float DarksunOuterVortexScaleDivisor = 96f;
        private const float DarksunLensingRadiusPadding = 5f * 16f;
        private const float DarksunLensingFadeOutDuration = 24f;

        private static Vector2 darkPlasmaLastCenter;
        private static float darkPlasmaLastStrength;
        private static float darkPlasmaLastOpacity;
        private static float darkPlasmaFadeOutTime;
        private static Vector2 darksunLastCenter;
        private static float darksunLastOuterRadius;
        private static float darksunLastHorizonRadius;
        private static float darksunLastStrength;
        private static float darksunLensingFadeOutTime;

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
                UpdateDarkPlasmaKamuiVortexFadeOut(filter, key);

                return;
            }

            float lifeProgress = Utils.GetLerpValue(DarkPlasmaEffect.BlackHoleLifetime, 0f, target.timeLeft, true);
            float pulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3.2f + target.identity * 0.17f);
            float strength = (MathHelper.Lerp(1.65f, 1.95f, lifeProgress) + 0.05f * pulse) *
                opacity * DarkPlasmaDistortionStrengthMultiplier;

            darkPlasmaLastCenter = target.Center;
            darkPlasmaLastStrength = strength;
            darkPlasmaLastOpacity = opacity;
            darkPlasmaFadeOutTime = DarkPlasmaFadeOutDuration;
            ApplyDarkPlasmaKamuiVortex(filter, key, target.Center, strength, opacity);
        }

        private static void UpdateDarkPlasmaKamuiVortexFadeOut(Filter filter, string key)
        {
            if (!filter.IsActive() || darkPlasmaFadeOutTime <= 0f || darkPlasmaLastStrength <= 0.001f)
            {
                if (filter.IsActive())
                    Filters.Scene.Deactivate(key);

                ResetDarkPlasmaKamuiVortexFadeOut();
                return;
            }

            darkPlasmaFadeOutTime = Math.Max(0f, darkPlasmaFadeOutTime - 1f);
            float remaining = darkPlasmaFadeOutTime / DarkPlasmaFadeOutDuration;
            float nonlinearFade = remaining * remaining * (3f - 2f * remaining);
            ApplyDarkPlasmaKamuiVortex(
                filter,
                key,
                darkPlasmaLastCenter,
                darkPlasmaLastStrength * nonlinearFade,
                darkPlasmaLastOpacity * nonlinearFade);
        }

        private static void ApplyDarkPlasmaKamuiVortex(Filter filter, string key, Vector2 center, float strength, float opacity)
        {
            if (!filter.IsActive())
            {
                Filters.Scene.Activate(key, center);
                filter = Filters.Scene[key];
            }

            ScreenShaderData shaderData = filter.GetShader();
            shaderData.UseTargetPosition(center);
            Effect effect = shaderData.Shader;
            effect.Parameters["uScreenResolution"]?.SetValue(GetScreenSize());
            effect.Parameters["uScreenPosition"]?.SetValue(Main.screenPosition);
            effect.Parameters["uRadius"]?.SetValue(DarkPlasmaVortexRadius);
            effect.Parameters["uStrength"]?.SetValue(strength);
            effect.Parameters["uOpacity"]?.SetValue(opacity);
            effect.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
        }

        private static void ResetDarkPlasmaKamuiVortexFadeOut()
        {
            darkPlasmaLastCenter = Vector2.Zero;
            darkPlasmaLastStrength = 0f;
            darkPlasmaLastOpacity = 0f;
            darkPlasmaFadeOutTime = 0f;
        }

        private static void UpdateDarksunFragmentGravitationalLensing()
        {
            string key = SceneFilterKey(DarksunFragmentGravitationalLensingRegistrationName);
            Filter filter = Filters.Scene[key];
            if (filter is null)
                return;

            if (!TryFindDarksunBlackSunTarget(out Projectile target, out float opacity))
            {
                UpdateDarksunFragmentGravitationalLensingFadeOut(filter, key);

                return;
            }

            int level = Utils.Clamp((int)target.ai[0], 1, DarksunFragmentBlackSun.MaxLevel);
            float blackSunRadius = DarksunFragmentBlackSun.GetRadiusForLevel(level);
            float levelProgress = Utils.GetLerpValue(1f, DarksunFragmentBlackSun.MaxLevel, level, true);
            float radiusFade = MathHelper.Clamp(opacity, 0f, 1f);
            float outerRadius = GetDarksunOuterVortexRadius(level) * radiusFade;
            float horizonRadius = blackSunRadius * radiusFade;
            float strength = MathHelper.Lerp(0.42f, 0.76f, levelProgress) * opacity;

            darksunLastCenter = target.Center;
            darksunLastOuterRadius = outerRadius;
            darksunLastHorizonRadius = horizonRadius;
            darksunLastStrength = strength;
            darksunLensingFadeOutTime = DarksunLensingFadeOutDuration;

            ApplyDarksunFragmentGravitationalLensing(filter, key, target.Center, outerRadius, horizonRadius, strength);
        }

        private static void UpdateDarksunFragmentGravitationalLensingFadeOut(Filter filter, string key)
        {
            if (!filter.IsActive() || darksunLensingFadeOutTime <= 0f || darksunLastStrength <= 0.001f)
            {
                if (filter.IsActive())
                    Filters.Scene.Deactivate(key);

                ResetDarksunFragmentGravitationalLensingFadeOut();
                return;
            }

            darksunLensingFadeOutTime = Math.Max(0f, darksunLensingFadeOutTime - 1f);
            float remaining = darksunLensingFadeOutTime / DarksunLensingFadeOutDuration;
            ApplyDarksunFragmentGravitationalLensing(
                filter,
                key,
                darksunLastCenter,
                darksunLastOuterRadius * remaining,
                darksunLastHorizonRadius * remaining,
                darksunLastStrength * remaining);
        }

        private static void ApplyDarksunFragmentGravitationalLensing(Filter filter, string key, Vector2 center, float outerRadius, float horizonRadius, float strength)
        {
            if (!filter.IsActive())
            {
                Filters.Scene.Activate(key, center);
                filter = Filters.Scene[key];
            }

            ScreenShaderData shaderData = filter.GetShader();
            shaderData.UseTargetPosition(center);
            Effect effect = shaderData.Shader;
            effect.Parameters["uScreenResolution"]?.SetValue(GetScreenSize());
            effect.Parameters["uScreenPosition"]?.SetValue(Main.screenPosition);
            effect.Parameters["uRadius"]?.SetValue(Math.Max(1f, outerRadius));
            effect.Parameters["uHorizonRadius"]?.SetValue(Math.Max(1f, horizonRadius));
            effect.Parameters["uStrength"]?.SetValue(Math.Max(0f, strength));
        }

        private static void ResetDarksunFragmentGravitationalLensingFadeOut()
        {
            darksunLastCenter = Vector2.Zero;
            darksunLastOuterRadius = 0f;
            darksunLastHorizonRadius = 0f;
            darksunLastStrength = 0f;
            darksunLensingFadeOutTime = 0f;
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

                // Prefer highest-level black sun so a newly-spawned level-1 never steals
                // the shader from an existing high-level one. Level difference dominates;
                // screen-center proximity breaks ties within the same level.
                float score = -level * 1_000_000f + Vector2.Distance(screenCenter, new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f);
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
