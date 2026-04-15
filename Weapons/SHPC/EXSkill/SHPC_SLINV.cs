using CalamityMod.Buffs.DamageOverTime;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.EXSkill
{
    internal class SHPC_SLINV : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int MaxUpdateCount = 1;
        private const int Lifetime = 350;
        private const float SuperLaserVisualWidth = 30f;
        private const float SuperLaserVisualScale = 0.7f;
        private const float SpiralAmplitude = (SuperLaserVisualWidth * SuperLaserVisualScale + 180f) * 0.5f;

        private bool initialized;
        private float axialDistance;
        private float axialSpeed;
        private float waveFrequency;
        private float waveFrequencyDrift;
        private float wavePhaseOffset;
        private float trailWidthScale;
        private Color trailColorA;
        private Color trailColorB;
        private Color trailColorEnd;

        private int OwnerIndex => (int)Projectile.ai[0];
        private float SpawnAxisOffset => Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 21;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.MaxUpdates = MaxUpdateCount;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void OnSpawn(IEntitySource source)
        {
            initialized = false;
            axialDistance = SpawnAxisOffset;
            axialSpeed = Math.Max(Projectile.velocity.Length(), 56f);
            waveFrequency = MathHelper.Lerp(0.015f, 0.029f, StableRandom01(13));
            waveFrequencyDrift = MathHelper.Lerp(-0.0011f, 0.0011f, StableRandom01(29));
            wavePhaseOffset = StableRandom01(47) * MathHelper.TwoPi;
            trailWidthScale = MathHelper.Lerp(0.85f, 1.15f, StableRandom01(71));

            int colorType = (int)(StableRandom01(89) * 4f) % 4;
            trailColorA = colorType switch
            {
                0 => new Color(90, 200, 255),
                1 => new Color(120, 235, 255),
                2 => new Color(150, 240, 255),
                _ => Color.White
            };
            trailColorB = Color.Lerp(trailColorA, Color.White, 0.4f);
            trailColorEnd = Color.Lerp(trailColorA, Color.White, 0.75f);
        }

        public override void AI()
        {
            if (OwnerIndex < 0 || OwnerIndex >= Main.maxProjectiles)
            {
                Projectile.Kill();
                return;
            }

            Projectile ownerProj = Main.projectile[OwnerIndex];
            if (!ownerProj.active || ownerProj.type != ModContent.ProjectileType<NL_SHPC_EXWeapon>())
            {
                Projectile.Kill();
                return;
            }

            Vector2 axisDirection = ownerProj.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 axisNormal = axisDirection.RotatedBy(MathHelper.PiOver2);
            Vector2 axisOrigin = ownerProj.Center + axisDirection * 56f;

            axialDistance += axialSpeed / (MaxUpdateCount + 1f);

            float lifeRatio = 1f - Projectile.timeLeft / (float)Lifetime;
            float wavePhase =
                axialDistance * (waveFrequency + lifeRatio * waveFrequencyDrift) +
                Projectile.identity * 0.071f +
                wavePhaseOffset;

            float lateralOffset = SpiralAmplitude * (float)Math.Sin(wavePhase);
            Vector2 nextCenter = axisOrigin + axisDirection * axialDistance + axisNormal * lateralOffset;

            if (!initialized)
            {
                for (int i = 0; i < Projectile.oldPos.Length; i++)
                    Projectile.oldPos[i] = nextCenter - Projectile.Size * 0.5f;

                Projectile.Center = nextCenter;
                initialized = true;
            }

            Vector2 oldCenter = Projectile.Center;
            Projectile.Center = nextCenter;
            Projectile.velocity = Projectile.Center - oldCenter;
            if (Projectile.velocity == Vector2.Zero)
                Projectile.velocity = axisDirection;

            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity =
                Utils.GetLerpValue(0f, 16f, Lifetime - Projectile.timeLeft, true) *
                Utils.GetLerpValue(0f, 36f, Projectile.timeLeft, true);
        }

        public override Color? GetAlpha(Color lightColor) => trailColorA * Projectile.Opacity;

        private float StableRandom01(int salt)
        {
            uint seed = (uint)(Projectile.identity * 73428767 ^ Projectile.owner * 912673 ^ salt * 19349663);
            seed ^= seed << 13;
            seed ^= seed >> 17;
            seed ^= seed << 5;
            return (seed & 0x00FFFFFF) / 16777215f;
        }

        private float PrimitiveWidthFunction(float completionRatio, Vector2 vertexPos)
        {
            float tipFade = Utils.GetLerpValue(1f, 0.64f, completionRatio, true);
            float rootGrow = (float)Math.Sin(Utils.GetLerpValue(0f, 0.2f, completionRatio, true) * MathHelper.PiOver2);
            return MathHelper.Lerp(4f, 26f, rootGrow) * tipFade * trailWidthScale;
        }

        private Color PrimitiveColorFunction(float completionRatio, Vector2 vertexPos)
        {
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6.2f + Projectile.identity * 0.27f + completionRatio * 9f);
            Color midColor = Color.Lerp(trailColorA, trailColorB, pulse);
            Color finalColor = Color.Lerp(midColor, trailColorEnd, Utils.GetLerpValue(0f, 0.58f, completionRatio, true));
            Color faded = Color.Lerp(finalColor, Color.Transparent, completionRatio * completionRatio);
            faded.A = 0;
            return faded * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!initialized)
                return false;

            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            Vector2 overallOffset = Projectile.Size * 0.5f + Projectile.velocity * 0.85f;
            PrimitiveRenderer.RenderTrail(
                Projectile.oldPos,
                new PrimitiveSettings(PrimitiveWidthFunction, PrimitiveColorFunction, (_, _) => overallOffset, shader: GameShaders.Misc["CalamityMod:TrailStreak"]),
                46);

            return false;
        }
    }
}
