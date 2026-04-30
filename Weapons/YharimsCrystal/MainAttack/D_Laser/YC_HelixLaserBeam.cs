using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.D_Laser
{
    public class YC_HelixLaserBeam : ModProjectile, ILocalizedModType
    {
        private const float MaxBeamLength = 2300f;
        private const int SampleCount = 3;

        public override string Texture => "CalamityMod/Projectiles/Magic/YharimsCrystalBeam";
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";

        private int BeamID => (int)Projectile.ai[0];
        private int HoldoutIndex => (int)Projectile.ai[1];
        private ref float BeamLength => ref Projectile.localAI[0];
        private ref float Timer => ref Projectile.localAI[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.timeLeft = 18000;
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

        public override bool? CanDamage()
        {
            if (!TryGetHoldout(out _, out YC_HelixLaserHoldout holdout))
                return false;

            return holdout.HoldFrames >= YC_HelixLaserHoldout.DamageStart ? null : false;
        }

        public override void AI()
        {
            if (!TryGetHoldout(out Projectile holdoutProjectile, out YC_HelixLaserHoldout holdout))
            {
                Projectile.Kill();
                return;
            }

            Timer++;
            Projectile.timeLeft = 2;

            float charge = holdout.ChargeRatio;
            float focusBlend = holdout.FocusBlend;
            Vector2 baseDirection = holdout.ForwardDirection;
            Vector2 normal = baseDirection.RotatedBy(MathHelper.PiOver2);
            float centeredId = BeamID - (YC_HelixLaserHoldout.BeamCount - 1f) * 0.5f;
            float phase = holdout.HoldFrames * MathHelper.Lerp(0.075f, 0.12f, focusBlend) + BeamID * MathHelper.TwoPi / YC_HelixLaserHoldout.BeamCount;
            float unfocusedRadius = MathHelper.Lerp(30f, 18f, charge);
            float focusedRadius = MathHelper.Lerp(14f, 3f, charge);
            float orbitRadius = MathHelper.Lerp(unfocusedRadius, focusedRadius, focusBlend);
            Vector2 originOffset = baseDirection * 22f + normal * (float)System.Math.Sin(phase) * orbitRadius;
            Projectile.Center = holdoutProjectile.Center + originOffset;

            float spread = MathHelper.Lerp(0.32f, 0.1f, charge);
            float helixAngle = (float)System.Math.Sin(phase) * spread + centeredId * MathHelper.Lerp(0.05f, 0.025f, charge);
            Vector2 helixDirection = baseDirection.RotatedBy(helixAngle);
            Vector2 focusDirection = (holdout.FocusPoint - Projectile.Center).SafeNormalize(baseDirection);
            Vector2 desiredDirection = Vector2.Lerp(helixDirection, focusDirection, (float)System.Math.Pow(focusBlend, 1.35f)).SafeNormalize(baseDirection);

            Projectile.velocity = desiredDirection.SafeNormalize(baseDirection);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.scale = MathHelper.Lerp(0.35f, MathHelper.Lerp(1.18f, 1.55f, focusBlend), charge);
            float damageMultiplier = MathHelper.Lerp(
                MathHelper.Lerp(0.55f, 1.28f, charge),
                MathHelper.Lerp(1.1f, 2.35f, charge),
                focusBlend);
            Projectile.damage = (int)(holdoutProjectile.damage * damageMultiplier);

            UpdateBeamLength();
            EmitBeamDust(holdout, charge, focusBlend);

            DelegateMethods.v3_1 = GetBeamColor(holdout, charge).ToVector3() * (0.35f + charge * 0.55f);
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * BeamLength, 24f * Projectile.scale, DelegateMethods.CastLight);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            float width = MathHelper.Lerp(12f, 28f, Projectile.scale / 1.55f);
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + Projectile.velocity * BeamLength,
                width,
                ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Dragonfire>(), 180);
        }

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * BeamLength, 24f * Projectile.scale, DelegateMethods.CutTiles);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!TryGetHoldout(out _, out YC_HelixLaserHoldout holdout) || Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
                return false;

            Texture2D outer = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;
            Texture2D inner = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineThick").Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 unit = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 center = start + unit * BeamLength * 0.5f;
            float charge = holdout.ChargeRatio;
            float focusBlend = holdout.FocusBlend;
            float opacity = Utils.GetLerpValue(0f, 0.18f, charge, true);
            float lengthScale = BeamLength / outer.Height;
            Color beamColor = GetBeamColor(holdout, charge) with { A = 0 };
            Color texturedBeamColor = GetBeamColor(holdout, charge);
            texturedBeamColor.A = 96;
            Color whiteCore = Color.White with { A = 0 };
            float width = MathHelper.Lerp(0.9f + 0.16f * (float)System.Math.Sin(Timer * 0.18f + BeamID), 1.25f, focusBlend);

            Texture2D beamTexture = ModContent.Request<Texture2D>(Texture).Value;
            DelegateMethods.f_1 = 1f;
            Utils.LaserLineFraming framing = new(DelegateMethods.RainbowLaserDraw);
            Vector2 laserStart = Projectile.Center + unit * 10f * Projectile.scale - Main.screenPosition;
            Vector2 laserEnd = laserStart + unit * MathHelper.Max(8f, BeamLength - 18f * Projectile.scale);
            DelegateMethods.c_1 = texturedBeamColor * (0.78f * opacity);
            Utils.DrawLaser(Main.spriteBatch, beamTexture, laserStart, laserEnd, new Vector2(Projectile.scale * 0.34f * width), framing);
            DelegateMethods.c_1 = Color.White * (0.16f * opacity);
            Utils.DrawLaser(Main.spriteBatch, beamTexture, laserStart, laserEnd, new Vector2(Projectile.scale * 0.14f * width), framing);

            Main.EntitySpriteDraw(
                outer,
                center,
                null,
                beamColor * (0.72f * opacity),
                Projectile.rotation + MathHelper.PiOver2,
                outer.Size() * 0.5f,
                new Vector2(0.16f * width * Projectile.scale, lengthScale),
                SpriteEffects.FlipVertically,
                0f);

            Main.EntitySpriteDraw(
                inner,
                center,
                null,
                whiteCore * (0.45f * opacity),
                Projectile.rotation + MathHelper.PiOver2,
                inner.Size() * 0.5f,
                new Vector2(0.055f * Projectile.scale, BeamLength / inner.Height),
                SpriteEffects.FlipVertically,
                0f);

            Main.EntitySpriteDraw(glow, start, null, beamColor * (0.55f * opacity), Projectile.rotation, glow.Size() * 0.5f, 0.12f * Projectile.scale, SpriteEffects.None, 0);

            if (focusBlend > 0.03f)
            {
                Vector2 focusDraw = holdout.FocusPoint - Main.screenPosition;
                Main.EntitySpriteDraw(glow, focusDraw, null, new Color(255, 224, 145, 0) * (0.28f * opacity * focusBlend), Projectile.rotation, glow.Size() * 0.5f, (0.22f + 0.1f * charge) * focusBlend, SpriteEffects.None, 0);
            }

            return false;
        }

        private bool TryGetHoldout(out Projectile holdoutProjectile, out YC_HelixLaserHoldout holdout)
        {
            holdoutProjectile = null;
            holdout = null;

            if (HoldoutIndex < 0 || HoldoutIndex >= Main.maxProjectiles)
                return false;

            Projectile candidate = Main.projectile[HoldoutIndex];
            if (!candidate.active ||
                candidate.owner != Projectile.owner ||
                candidate.type != ModContent.ProjectileType<YC_HelixLaserHoldout>() ||
                candidate.ModProjectile is not YC_HelixLaserHoldout holdoutMod)
            {
                return false;
            }

            holdoutProjectile = candidate;
            holdout = holdoutMod;
            return true;
        }

        private void UpdateBeamLength()
        {
            float[] samples = new float[SampleCount];
            Collision.LaserScan(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), 2f * Projectile.scale, MaxBeamLength, samples);

            float average = 0f;
            for (int i = 0; i < samples.Length; i++)
                average += samples[i];

            average /= samples.Length;
            if (average <= 0f)
                average = MaxBeamLength;

            BeamLength = MathHelper.Lerp(BeamLength <= 0f ? average : BeamLength, average, 0.72f);
        }

        private void EmitBeamDust(YC_HelixLaserHoldout holdout, float charge, float focusBlend)
        {
            bool focused = focusBlend > 0.5f;
            if (Main.dedServ || Main.GameUpdateCount % (focused ? 3 : 5) != 0 || charge < 0.18f)
                return;

            Vector2 end = Projectile.Center + Projectile.velocity * (BeamLength - 18f);
            Color color = GetBeamColor(holdout, charge);

            for (int i = 0; i < (focused ? 2 : 1); i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    end + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.GoldFlame,
                    Projectile.velocity.RotatedBy(MathHelper.PiOver2 * (Main.rand.NextBool() ? 1f : -1f)) * Main.rand.NextFloat(0.4f, 1.2f),
                    0,
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.15f, 0.55f)),
                    Main.rand.NextFloat(0.8f, 1.2f) * Projectile.scale);
                dust.noGravity = true;
            }
        }

        private Color GetBeamColor(YC_HelixLaserHoldout holdout, float charge)
        {
            float hue = 0.045f + BeamID * 0.018f + 0.012f * (float)System.Math.Sin((Timer + BeamID * 9f) * 0.025f);
            float focusBlend = holdout.FocusBlend;
            Color baseColor = Main.hslToRgb(hue, MathHelper.Lerp(0.62f, 0.78f, focusBlend), MathHelper.Lerp(0.52f, 0.58f, focusBlend));
            return Color.Lerp(baseColor, new Color(255, 245, 198), charge * MathHelper.Lerp(0.14f, 0.32f, focusBlend));
        }
    }
}
