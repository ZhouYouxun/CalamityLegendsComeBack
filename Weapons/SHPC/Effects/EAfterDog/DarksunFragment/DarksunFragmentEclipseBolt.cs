using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.DarksunFragment
{
    internal class DarksunFragmentEclipseBolt : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 72;
        private Vector2 startPosition;
        private Vector2 controlPosition;
        private Vector2 targetPosition;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            startPosition = Projectile.Center;
            controlPosition = new Vector2(Projectile.ai[1], Projectile.ai[2]);
            targetPosition = GetParentCenter();
            Projectile.rotation = (targetPosition - Projectile.Center).ToRotation();
        }

        public override void AI()
        {
            if (startPosition == Vector2.Zero)
            {
                startPosition = Projectile.Center;
                controlPosition = new Vector2(Projectile.ai[1], Projectile.ai[2]);
            }

            targetPosition = GetParentCenter();
            float completion = 1f - Projectile.timeLeft / (float)Lifetime;
            completion = MathHelper.Clamp(completion, 0f, 1f);
            float curvedCompletion = 1f - (float)Math.Pow(1f - completion, 1.65f);
            Vector2 previous = Projectile.Center;
            Vector2 point = QuadraticBezier(startPosition, controlPosition, targetPosition, curvedCompletion);
            Projectile.Center = point;
            Projectile.velocity = point - previous;
            if (Projectile.velocity.LengthSquared() > 0.001f)
                Projectile.rotation = Projectile.velocity.ToRotation();

            if (Vector2.Distance(Projectile.Center, targetPosition) < 12f || !ParentIsActive())
                Projectile.Kill();

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.25f, 0, Main.rand.NextBool() ? new Color(255, 205, 66) : Color.Black, Main.rand.NextFloat(0.65f, 1f));
                dust.noGravity = true;
            }
        }

        private bool ParentIsActive()
        {
            int parent = (int)Projectile.ai[0];
            return parent >= 0 && parent < Main.maxProjectiles && Main.projectile[parent].active && Main.projectile[parent].type == ModContent.ProjectileType<DarksunFragmentBlackSun>();
        }

        private Vector2 GetParentCenter()
        {
            int parent = (int)Projectile.ai[0];
            if (parent >= 0 && parent < Main.maxProjectiles && Main.projectile[parent].active)
                return Main.projectile[parent].Center;

            return Projectile.Center;
        }

        private static Vector2 QuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            return Vector2.Lerp(Vector2.Lerp(a, b, t), Vector2.Lerp(b, c, t), t);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 center = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, center, null, new Color(255, 205, 64, 0) * 0.85f, Projectile.rotation, bloom.Size() * 0.5f, 0.12f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        public float WidthFunc(float completionRatio, Vector2 trailPoint)
        {
            completionRatio = MathHelper.Clamp(completionRatio, 0f, 1f);
            float headFade = Utils.GetLerpValue(0f, 0.08f, completionRatio, true);
            float tailFade = Utils.GetLerpValue(1f, 0.68f, completionRatio, true);
            return 14f * headFade * tailFade;
        }

        public Color ColorFunc(float completionRatio, Vector2 trailPoint)
        {
            completionRatio = MathHelper.Clamp(completionRatio, 0f, 1f);
            Color dark = new(34, 21, 4, 220);
            Color gold = new(255, 198, 48, 0);
            Color color = Color.Lerp(gold, dark, Utils.GetLerpValue(0.1f, 0.55f, completionRatio, true));
            return color * Utils.GetLerpValue(1f, 0.72f, completionRatio, true);
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            GameShaders.Misc["CalamityMod:ImpFlameTrail"]
                .SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                Projectile.oldPos,
                new PrimitiveSettings(
                    WidthFunc,
                    ColorFunc,
                    (_, _) => Projectile.Size * 0.5f,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                30);
        }
    }
}
