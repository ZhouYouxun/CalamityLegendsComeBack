using CalamityMod.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    // Cosmic Discharge's compact DoG rift crack. It keeps the original 25-segment recursion,
    // while reducing both segment reach and width to 30% of the Calamity version.
    internal sealed class CosmicDischargeRiftCrack : ModProjectile
    {
        private const int PointCount = 25;
        private const float SizeMultiplier = 0.3f;
        private const float MinSegmentLength = 25f * SizeMultiplier;
        private const float MaxSegmentLength = 50f * SizeMultiplier;

        private Vector2[] crackPoints;
        private ref float Timer => ref Projectile.ai[0];
        private ref float MaxWidth => ref Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
        }

        public override void AI()
        {
            if (crackPoints is null)
            {
                crackPoints = new Vector2[PointCount];
                Vector2 start = Projectile.Center;

                for (int i = 0; i < crackPoints.Length; i++)
                {
                    if (i == 0)
                        crackPoints[i] = start;
                    else if (i == 1)
                        crackPoints[i] = start + Projectile.velocity * Main.rand.NextFloat(0.8f, 1.2f) + Main.rand.NextVector2Circular(30f, 30f) * SizeMultiplier;
                    else
                    {
                        Vector2 previousDirection = crackPoints[i - 2].DirectionTo(crackPoints[i - 1]);
                        float segmentLength = Main.rand.NextFloat(MinSegmentLength, MaxSegmentLength);
                        if (i == crackPoints.Length - 1)
                            segmentLength *= 0.5f;

                        crackPoints[i] = crackPoints[i - 1] + previousDirection.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-5f, 5f) * i * 0.25f)) * segmentLength;
                    }

                    if (Main.rand.NextBool(9))
                        crackPoints[i] += Main.rand.NextVector2Circular(10f, 10f) * SizeMultiplier;
                }
            }

            Projectile.scale = MathHelper.Lerp(1f, 0f, Timer / 30f);
            Timer++;
        }

        private float WidthFunction(float completion, Vector2 _) => Projectile.scale * MathHelper.Lerp(MaxWidth, 0f, completion);
        private Color ColorFunction(float completion, Vector2 _) => Projectile.GetAlpha(Color.White);

        public override bool PreDraw(ref Color lightColor)
        {
            if (crackPoints is not null)
                PrimitiveRenderer.RenderTrail(crackPoints, new PrimitiveSettings(WidthFunction, ColorFunction, null, false));

            return false;
        }
    }
}
