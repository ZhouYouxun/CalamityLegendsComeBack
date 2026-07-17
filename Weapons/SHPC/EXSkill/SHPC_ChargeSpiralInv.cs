using CalamityMod;
using CalamityMod.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.EXSkill
{
    internal class SHPC_ChargeSpiralInv : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 96;
        internal const float MaxOrbitRadius = SHPC_SLINV.SpiralAmplitude * 0.52f;
        private const float FinalRadius = 10f;

        private bool initialized;
        private float angularVelocity;
        private float radialPulseOffset;
        private float radialPulseSpeed;
        private float harmonicPhaseOffset;
        private float epicycleFrequency;
        private float epicyclePhaseOffset;
        private Color trailColorA;
        private Color trailColorB;
        private Color trailColorEnd;

        private int OwnerIndex => (int)Projectile.ai[0];
        private ref float OrbitAngle => ref Projectile.ai[1];
        private float InitialRadius => MathHelper.Clamp(Projectile.ai[2], MaxOrbitRadius * 0.82f, MaxOrbitRadius);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 18;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void OnSpawn(IEntitySource source)
        {
            initialized = false;
            angularVelocity = Main.rand.NextFloat(0.21f, 0.35f) * (Main.rand.NextBool() ? 1f : -1f);
            radialPulseOffset = Main.rand.NextFloat(MathHelper.TwoPi);
            radialPulseSpeed = Main.rand.NextFloat(1.8f, 2.6f);
            harmonicPhaseOffset = Main.rand.NextFloat(MathHelper.TwoPi);
            epicycleFrequency = Main.rand.NextFloat(2.6f, 4.4f) * (Main.rand.NextBool() ? 1f : -1f);
            epicyclePhaseOffset = Main.rand.NextFloat(MathHelper.TwoPi);
            trailColorA = Main.rand.NextBool() ? new Color(90, 200, 255) : new Color(120, 235, 255);
            trailColorB = Color.Lerp(trailColorA, Color.White, 0.42f);
            trailColorEnd = Color.Lerp(trailColorA, Color.White, 0.78f);
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

            float progress = Utils.GetLerpValue(Lifetime, 0f, Projectile.timeLeft, true);
            float inwardProgress = Utils.GetLerpValue(0.58f, 1f, progress, true);
            inwardProgress = inwardProgress * inwardProgress * (3f - 2f * inwardProgress);

            float anchorRadius = MathHelper.Lerp(InitialRadius, FinalRadius, inwardProgress);
            float pulse = (float)System.Math.Sin(progress * MathHelper.TwoPi * radialPulseSpeed + radialPulseOffset);
            float rose = (float)System.Math.Sin(OrbitAngle * 2f + harmonicPhaseOffset) * (float)System.Math.Cos(progress * MathHelper.TwoPi);
            float radius = MathHelper.Clamp(
                anchorRadius * (0.97f + 0.03f * pulse) + rose * MaxOrbitRadius * 0.026f * (1f - inwardProgress),
                FinalRadius,
                MaxOrbitRadius);
            Vector2 axis = ownerProj.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 origin = ownerProj.Center + axis * 56f;

            float angularTempo = 1f + 0.12f * (float)System.Math.Sin(OrbitAngle * 2f - harmonicPhaseOffset);
            OrbitAngle += angularVelocity * MathHelper.Lerp(1.1f, 2.4f, progress) * angularTempo;

            float angleRipple = 0.07f * (float)System.Math.Sin(OrbitAngle * 2f + radialPulseOffset) * (1f - inwardProgress * 0.55f);

            // 更复杂的数学运动：主轨道上叠加一个自转更快的小外旋轮(epicycle)，
            // 振幅随内旋进度衰减到0，最终仍然收敛到中心
            float epicycleRadius = MaxOrbitRadius * 0.24f * (1f - inwardProgress) *
                (0.6f + 0.4f * (float)System.Math.Sin(progress * MathHelper.TwoPi * 1.3f + epicyclePhaseOffset));
            float epicycleAngle = OrbitAngle * epicycleFrequency + epicyclePhaseOffset;
            Vector2 epicycleOffset = epicycleAngle.ToRotationVector2() * epicycleRadius;

            Vector2 nextCenter = origin + (OrbitAngle + angleRipple).ToRotationVector2() * radius + epicycleOffset;

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
                Projectile.velocity = (origin - Projectile.Center).SafeNormalize(axis);

            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity =
                Utils.GetLerpValue(0f, 10f, Lifetime - Projectile.timeLeft, true) *
                Utils.GetLerpValue(0f, 22f, Projectile.timeLeft, true);

            if (progress > 0.78f && radius <= FinalRadius + 4f)
                Projectile.Kill();
        }

        private float PrimitiveWidthFunction(float completionRatio, Vector2 vertexPos)
        {
            float tipFade = Utils.GetLerpValue(1f, 0.62f, completionRatio, true);
            float rootGrow = (float)System.Math.Sin(Utils.GetLerpValue(0f, 0.22f, completionRatio, true) * MathHelper.PiOver2);
            return MathHelper.Lerp(1.5f, 11f, rootGrow) * tipFade;
        }

        private Color PrimitiveColorFunction(float completionRatio, Vector2 vertexPos)
        {
            float pulse = 0.5f + 0.5f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 7f + Projectile.identity * 0.31f + completionRatio * 8f);
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

            if (SHPCEXShaderUtilities.TryGetMiniTrailShader(trailColorA, trailColorB, 1f, out MiscShaderData shpcTrailShader))
            {
                Vector2 shaderOffset = Projectile.Size * 0.5f + Projectile.velocity * 0.85f;
                PrimitiveRenderer.RenderTrail(
                    Projectile.oldPos,
                    new PrimitiveSettings(PrimitiveWidthFunction, PrimitiveColorFunction, (_, _) => shaderOffset, shader: shpcTrailShader),
                    42);

                return false;
            }

            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            Vector2 overallOffset = Projectile.Size * 0.5f + Projectile.velocity * 0.85f;
            PrimitiveRenderer.RenderTrail(
                Projectile.oldPos,
                new PrimitiveSettings(PrimitiveWidthFunction, PrimitiveColorFunction, (_, _) => overallOffset, shader: GameShaders.Misc["CalamityMod:TrailStreak"]),
                42);

            return false;
        }
    }
}
