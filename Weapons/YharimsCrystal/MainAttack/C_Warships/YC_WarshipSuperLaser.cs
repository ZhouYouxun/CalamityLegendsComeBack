using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.C_Warships
{
    public class YC_WarshipSuperLaser : ModProjectile, ILocalizedModType
    {
        private const float MaxBeamLength = 2000f;
        private const float HitWidth = 7.2f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";

        private int SourceIndex => (int)Projectile.ai[0];
        private ref float BeamLength => ref Projectile.localAI[0];
        private ref float Timer => ref Projectile.localAI[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void DrawBehind(
            int index,
            System.Collections.Generic.List<int> behindNPCsAndTiles,
            System.Collections.Generic.List<int> behindNPCs,
            System.Collections.Generic.List<int> behindProjectiles,
            System.Collections.Generic.List<int> overPlayers,
            System.Collections.Generic.List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override void AI()
        {
            if (!TryGetSource(out Projectile source, out YC_WarshipLaserShip laserShip))
            {
                Projectile.Kill();
                return;
            }

            Timer++;
            Projectile.timeLeft = 2;

            Vector2 direction = laserShip.CurrentForwardDirection.SafeNormalize(source.velocity.SafeNormalize(Vector2.UnitX));
            Projectile.Center = source.Center + direction * 26f;
            Projectile.velocity = direction;
            Projectile.rotation = direction.ToRotation();

            UpdateBeamLength();
            EmitBeamFX(direction);
            DelegateMethods.v3_1 = new Vector3(0.42f, 0.32f, 0.08f);
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + direction * BeamLength, HitWidth, DelegateMethods.CastLight);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
                return false;

            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + Projectile.velocity * BeamLength,
                HitWidth,
                ref collisionPoint);
        }

        public override void CutTiles()
        {
            if (Projectile.velocity == Vector2.Zero)
                return;

            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * BeamLength, HitWidth + 4f, DelegateMethods.CutTiles);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
                return false;

            Texture2D outer = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;
            Texture2D inner = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineThick").Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 center = start + direction * BeamLength * 0.5f;
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            float opacity = Utils.GetLerpValue(0f, 8f, Timer, true);
            Color gold = new Color(255, 210, 70, 0);
            Color pale = new Color(255, 250, 205, 0);

            DrawBeamLayer(outer, center, Projectile.rotation, gold * (0.34f * opacity), 0.22f, BeamLength / outer.Height);
            DrawBeamLayer(inner, center, Projectile.rotation, pale * (0.48f * opacity), 0.072f, BeamLength / inner.Height);
            DrawBeamLayer(inner, center, Projectile.rotation, (Color.White with { A = 0 }) * (0.16f * opacity), 0.027f, BeamLength / inner.Height);

            for (int i = 0; i < 6; i++)
            {
                float completion = (i + 0.5f) / 6f;
                float envelope = (float)System.Math.Sin(completion * MathHelper.Pi);
                Vector2 knot = start + direction * (BeamLength * completion) + normal * ((i % 2 == 0 ? 1f : -1f) * envelope * 2.5f);
                Main.EntitySpriteDraw(glow, knot, null, gold * (0.055f * opacity * envelope), Projectile.rotation, glow.Size() * 0.5f, 0.045f + 0.018f * envelope, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(glow, start + direction * 10f, null, pale * (0.28f * opacity), Projectile.rotation, glow.Size() * 0.5f, 0.075f, SpriteEffects.None, 0);
            return false;
        }

        private bool TryGetSource(out Projectile source, out YC_WarshipLaserShip laserShip)
        {
            source = null;
            laserShip = null;

            if (SourceIndex < 0 || SourceIndex >= Main.maxProjectiles)
                return false;

            source = Main.projectile[SourceIndex];
            if (!source.active || source.owner != Projectile.owner || source.ModProjectile is not YC_WarshipLaserShip ship)
                return false;

            laserShip = ship;
            return true;
        }

        private void UpdateBeamLength()
        {
            float[] samples = new float[3];
            Collision.LaserScan(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), 2f, MaxBeamLength, samples);

            float average = 0f;
            for (int i = 0; i < samples.Length; i++)
                average += samples[i];

            average /= samples.Length;
            if (average <= 0f)
                average = MaxBeamLength;

            BeamLength = MathHelper.Lerp(BeamLength <= 0f ? average : BeamLength, average, 0.78f);
        }

        private void EmitBeamFX(Vector2 direction)
        {
            if (Main.dedServ)
                return;

            Lighting.AddLight(Projectile.Center, new Vector3(0.35f, 0.25f, 0.06f));
            if (Main.GameUpdateCount % 4 != 0)
                return;

            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            float distance = BeamLength > 42f ? Main.rand.NextFloat(20f, BeamLength - 20f) : BeamLength * 0.5f;
            Vector2 position = Projectile.Center + direction * distance + normal * Main.rand.NextFloat(-5f, 5f);
            Dust dust = Dust.NewDustPerfect(
                position,
                Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch,
                normal * Main.rand.NextFloat(-0.7f, 0.7f) - direction * Main.rand.NextFloat(0.1f, 0.45f),
                0,
                Color.Lerp(new Color(255, 206, 75), Color.White, Main.rand.NextFloat(0.16f, 0.48f)),
                Main.rand.NextFloat(0.45f, 0.78f));
            dust.noGravity = true;
        }

        private static void DrawBeamLayer(Texture2D texture, Vector2 center, float rotation, Color color, float width, float lengthScale)
        {
            Main.EntitySpriteDraw(
                texture,
                center,
                null,
                color,
                rotation + MathHelper.PiOver2,
                texture.Size() * 0.5f,
                new Vector2(width, lengthScale),
                SpriteEffects.FlipVertically,
                0f);
        }
    }
}
