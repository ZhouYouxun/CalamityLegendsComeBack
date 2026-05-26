using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    internal sealed class BFChargeGlowSparkFX : ModProjectile
    {
        private const int Lifetime = 24;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float HoldoutIndex => ref Projectile.ai[0];
        private ref float Mode => ref Projectile.ai[1];
        private ref float Seed => ref Projectile.ai[2];
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 21;
        }

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.alpha = 255;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Timer++;
            if (!TryGetHoldout(out Projectile holdout, out Vector2 aimDirection))
            {
                Projectile.Kill();
                return;
            }

            int mode = (int)Mode;
            float progress = MathHelper.Clamp(Timer / Lifetime, 0f, 1f);
            Vector2 muzzle = holdout.Center + aimDirection * 42f;
            Vector2 side = aimDirection.RotatedBy(MathHelper.PiOver2);
            Vector2 oldCenter = Projectile.Center;

            switch (mode)
            {
                case 0:
                    UpdateBreakthroughSpiral(muzzle, aimDirection, side, progress);
                    break;
                case 1:
                    UpdateRecoveryOrbit(holdout.Center, muzzle, aimDirection, side, progress);
                    break;
                case 2:
                    UpdateReconContract(muzzle, aimDirection, side, progress);
                    break;
                case 3:
                    UpdateBombardSlash(muzzle, aimDirection, side, progress);
                    break;
                default:
                    UpdatePlagueDiffusion(muzzle, aimDirection, side, progress);
                    break;
            }

            Projectile.velocity = Projectile.Center - oldCenter;
            Projectile.rotation = Projectile.velocity.SafeNormalize(aimDirection).ToRotation();
            Projectile.Opacity = Utils.GetLerpValue(0f, 3f, Timer, true) * Utils.GetLerpValue(0f, 8f, Projectile.timeLeft, true);

            Lighting.AddLight(Projectile.Center, GetModeColor(mode).ToVector3() * 0.28f * Projectile.Opacity);
        }

        private void UpdateBreakthroughSpiral(Vector2 muzzle, Vector2 aimDirection, Vector2 side, float progress)
        {
            float angle = Seed + Timer * 0.15f;
            float radius = MathHelper.Lerp(26f, 8f, MathHelper.SmoothStep(0f, 1f, progress));
            Vector2 target = muzzle - aimDirection * MathHelper.Lerp(20f, -6f, progress) + ProjectSphereOffset(aimDirection, side, angle, angle * 0.7f + Seed, radius, 0.28f);
            MoveElegantly(target, 0.48f, 9f);
        }

        private void UpdateRecoveryOrbit(Vector2 holdoutCenter, Vector2 muzzle, Vector2 aimDirection, Vector2 side, float progress)
        {
            float angle = Seed + Timer * 0.12f;
            float radius = 19f + (float)System.Math.Sin(Seed + Timer * 0.05f) * 2f;
            Vector2 orbitCenter = Vector2.Lerp(holdoutCenter - aimDirection * 10f, muzzle, progress * 0.65f);
            Vector2 target = orbitCenter + ProjectSphereOffset(aimDirection, side, angle, angle + MathHelper.PiOver2, radius * MathHelper.Lerp(1f, 0.55f, progress), 0.18f);
            MoveElegantly(target, 0.44f, 8f);
        }

        private void UpdateReconContract(Vector2 muzzle, Vector2 aimDirection, Vector2 side, float progress)
        {
            float angle = Seed + Timer * 0.1f;
            float radius = MathHelper.Lerp(30f, 14f, MathHelper.SmoothStep(0f, 1f, progress));
            Vector2 target = muzzle - aimDirection * 8f + ProjectRoundedSquareOffset(aimDirection, side, angle, radius, 0.2f);
            MoveElegantly(target, 0.42f, 8f);
        }

        private void UpdateBombardSlash(Vector2 muzzle, Vector2 aimDirection, Vector2 side, float progress)
        {
            float angle = Seed + Timer * 0.13f;
            float fall = MathHelper.Lerp(-22f, 12f, MathHelper.SmoothStep(0f, 1f, progress));
            Vector2 squareDrift = ProjectRoundedSquareOffset(aimDirection, side, angle, 18f, 0.16f);
            Vector2 target = muzzle + squareDrift + aimDirection * fall;
            MoveElegantly(target, 0.5f, 10f);
        }

        private void UpdatePlagueDiffusion(Vector2 muzzle, Vector2 aimDirection, Vector2 side, float progress)
        {
            float angle = Seed + Timer * 0.09f;
            float radius = MathHelper.Lerp(8f, 27f, MathHelper.SmoothStep(0f, 1f, progress));
            Vector2 target = muzzle - aimDirection * 14f + ProjectSphereOffset(aimDirection, side, angle, Seed * 0.5f + Timer * 0.06f, radius, 0.12f);
            MoveElegantly(target, 0.38f, 7.5f);
        }

        private Vector2 ProjectSphereOffset(Vector2 aimDirection, Vector2 side, float horizontalAngle, float verticalAngle, float radius, float depthStrength)
        {
            float x = (float)System.Math.Cos(horizontalAngle) * radius;
            float y = (float)System.Math.Sin(verticalAngle) * radius * 0.72f;
            float z = (float)System.Math.Sin(horizontalAngle) * radius;
            Vector2 screenUp = Vector2.UnitY * Main.player[Projectile.owner].gravDir;
            return side * x + screenUp * y + aimDirection * z * depthStrength;
        }

        private Vector2 ProjectRoundedSquareOffset(Vector2 aimDirection, Vector2 side, float angle, float radius, float depthStrength)
        {
            float x = SoftSquare((float)System.Math.Cos(angle));
            float y = SoftSquare((float)System.Math.Sin(angle));
            float z = (float)System.Math.Sin(angle + MathHelper.PiOver4);
            Vector2 screenUp = Vector2.UnitY * Main.player[Projectile.owner].gravDir;
            return side * x * radius + screenUp * y * radius * 0.78f + aimDirection * z * radius * depthStrength;
        }

        private static float SoftSquare(float value)
        {
            float sign = System.Math.Sign(value);
            return sign * (float)System.Math.Pow(System.Math.Abs(value), 0.58f);
        }

        private void MoveElegantly(Vector2 target, float responsiveness, float maxStep)
        {
            if (Timer <= 1f)
            {
                Projectile.Center = target;
                return;
            }

            Vector2 delta = target - Projectile.Center;
            float distance = delta.Length();
            if (distance > maxStep)
                delta *= maxStep / distance;

            Projectile.Center += delta * responsiveness;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            Vector2 offset = Projectile.Size * 0.5f + Projectile.velocity * 0.14f;
            PrimitiveRenderer.RenderTrail(
                Projectile.oldPos,
                new PrimitiveSettings(
                    PrimitiveWidthFunction,
                    PrimitiveColorFunction,
                    (_, _) => offset,
                    shader: GameShaders.Misc["CalamityMod:TrailStreak"]),
                46);

            return false;
        }

        private float PrimitiveWidthFunction(float completionRatio, Vector2 vertexPos)
        {
            const float arrowheadCutoff = 0.36f;
            const float maxWidth = 2.4f;
            const float minHeadWidth = 0.003f;

            if (completionRatio > arrowheadCutoff)
                return maxWidth;

            return MathHelper.Lerp(minHeadWidth, maxWidth, Utils.GetLerpValue(0f, arrowheadCutoff, completionRatio, true));
        }

        private Color PrimitiveColorFunction(float completionRatio, Vector2 vertexPos)
        {
            int mode = (int)Mode;
            Color themeColor = GetModeColor(mode);
            Color darkColor = Color.Lerp(themeColor, Color.Black, mode == 4 ? 0.42f : 0.28f);
            Color endColor = Color.Lerp(themeColor, Color.White, 0.36f);

            const float endFadeRatio = 0.41f;
            float endFadeTerm = Utils.GetLerpValue(0f, endFadeRatio * 0.5f, completionRatio, true) * 3.2f;
            float pulse = (float)System.Math.Cos(completionRatio * 2.7f - Main.GlobalTimeWrappedHourly * 5.3f + endFadeTerm) * 0.5f + 0.5f;

            Color startingColor = Color.Lerp(themeColor, darkColor, pulse * 0.6f);
            Color result = Color.Lerp(startingColor, endColor, MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(0f, endFadeRatio, completionRatio, true)));
            return result * Projectile.Opacity;
        }

        private Color GetModeColor(int mode)
        {
            BlossomFluxChloroplastPresetType preset = mode switch
            {
                1 => BlossomFluxChloroplastPresetType.Chlo_BRecov,
                2 => BlossomFluxChloroplastPresetType.Chlo_CDetec,
                3 => BlossomFluxChloroplastPresetType.Chlo_DBomb,
                4 => BlossomFluxChloroplastPresetType.Chlo_EPlague,
                _ => BlossomFluxChloroplastPresetType.Chlo_ABreak
            };

            Color baseColor = BFArrowCommon.GetPresetColor(preset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(preset);
            return Color.Lerp(baseColor, accentColor, 0.35f + 0.25f * (float)System.Math.Sin(Seed + Timer * 0.2f));
        }

        private bool TryGetHoldout(out Projectile holdout, out Vector2 aimDirection)
        {
            holdout = null;
            aimDirection = Vector2.UnitX;

            if (!BFArrowCommon.InBounds(Projectile.owner, Main.maxPlayers))
                return false;

            Player owner = Main.player[Projectile.owner];
            aimDirection = Vector2.UnitX * owner.direction;
            if (!BFArrowCommon.InBounds(HoldoutIndex, Main.maxProjectiles))
                return false;

            Projectile candidate = Main.projectile[(int)HoldoutIndex];
            if (!candidate.active || candidate.type != ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>())
                return false;

            holdout = candidate;
            aimDirection = candidate.velocity.SafeNormalize(aimDirection);
            return true;
        }
    }
}
