using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.A_Drill
{
    public class YC_DrillHoldout : YC_BaseHoldout
    {
        private const int StartupFrames = 30;
        private const float HitLength = 104f;
        private const float HitWidth = 30f;
        private const int SpiralStrands = 2;

        private float spin;

        private float ChargeRatio => Utils.GetLerpValue(0f, StartupFrames, HoldFrameCounter, true);
        private bool SpunUp => HoldFrameCounter >= StartupFrames;

        protected override float HoldoutDistance => 8f;
        protected override float SoundPitch => -0.22f;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.friendly = true;
            Projectile.ownerHitCheck = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override bool? CanDamage() => SpunUp ? null : false;

        protected override void OnHoldoutAI()
        {
            float charge = ChargeRatio;
            spin += MathHelper.Lerp(0.38f, 1.22f, charge);
            Projectile.scale = 1f + 0.05f * charge * (float)System.Math.Sin(spin * 1.6f);

            if (Projectile.soundDelay <= 0 && SpunUp)
                SoundEngine.PlaySound(SoundID.Item22 with { Volume = 0.18f, Pitch = -0.35f }, Projectile.Center);

            if (Main.dedServ)
                return;

            Vector2 forward = ForwardDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 tip = Projectile.Center + forward * MathHelper.Lerp(28f, HitLength, charge);

            if (Main.GameUpdateCount % (SpunUp ? 1 : 3) == 0)
            {
                for (int i = 0; i < (SpunUp ? 2 : 1); i++)
                {
                    float sawOffset = (float)System.Math.Sin(spin + i * MathHelper.Pi) * HitWidth * 0.32f;
                    EmitDust(
                        tip + right * sawOffset + Main.rand.NextVector2Circular(3f, 3f),
                        -forward * Main.rand.NextFloat(0.5f, 1.3f) + right * Main.rand.NextFloat(-0.8f, 0.8f),
                        Color.Lerp(new Color(255, 150, 72), Color.White, Main.rand.NextFloat(0.12f, 0.45f)),
                        Main.rand.NextFloat(0.75f, 1.15f),
                        Main.rand.NextBool(4) ? DustID.Smoke : DustID.GoldFlame);
                }
            }

            if (charge > 0.18f)
                EmitSpiralJets(forward, right, tip, charge);

            Lighting.AddLight(tip, new Color(255, 175, 90).ToVector3() * (0.35f + charge * 0.45f));
        }

        private void EmitSpiralJets(Vector2 forward, Vector2 right, Vector2 tip, float charge)
        {
            int loopCount = SpunUp ? 3 : 1;
            float activeLength = MathHelper.Lerp(24f, HitLength * 0.96f, charge);

            for (int i = 0; i < loopCount; i++)
            {
                float progress = Main.rand.NextFloat(0.16f, 1.02f);
                float helixAngle = spin * 2.85f + progress * MathHelper.TwoPi * 2.4f + i * MathHelper.TwoPi / loopCount;
                float sideWave = (float)System.Math.Sin(helixAngle);
                float tangentWave = (float)System.Math.Cos(helixAngle);
                float radius = MathHelper.Lerp(3.5f, HitWidth * 0.48f, progress) * charge;
                Vector2 axisPosition = Projectile.Center + forward * MathHelper.Lerp(16f, activeLength, progress);
                Vector2 position = axisPosition + right * sideWave * radius + Main.rand.NextVector2Circular(1.4f, 1.4f);
                Vector2 velocity = forward * Main.rand.NextFloat(1.2f, 3.4f) + right * tangentWave * Main.rand.NextFloat(1.8f, 4.2f);
                Color color = Color.Lerp(new Color(255, 118, 54), new Color(255, 230, 130), Main.rand.NextFloat(0.12f, 0.64f));
                int dustType = Main.rand.NextBool(5) ? DustID.SolarFlare : Main.rand.NextBool(4) ? DustID.Smoke : DustID.GoldFlame;

                Dust dust = Dust.NewDustPerfect(position, dustType, velocity, 0, color, Main.rand.NextFloat(0.62f, 1.15f) * charge);
                dust.noGravity = true;
                dust.alpha = dustType == DustID.Smoke ? 80 : 20;
                dust.fadeIn = Main.rand.NextFloat(0.12f, 0.32f);
            }

            if (!SpunUp)
                return;

            for (int strand = 0; strand < SpiralStrands; strand++)
            {
                float helixAngle = spin * 3.25f + strand * MathHelper.Pi;
                float sideWave = (float)System.Math.Sin(helixAngle);
                float tangentWave = (float)System.Math.Cos(helixAngle);
                Vector2 position = tip + right * sideWave * HitWidth * 0.42f + Main.rand.NextVector2Circular(2.2f, 2.2f);
                Vector2 velocity = -forward * Main.rand.NextFloat(2.1f, 5.2f) + right * tangentWave * Main.rand.NextFloat(3.2f, 6.5f);
                Color color = Color.Lerp(new Color(255, 142, 72), Color.White, Main.rand.NextFloat(0.18f, 0.48f));
                int dustType = Main.rand.NextBool(6) ? DustID.Smoke : Main.rand.NextBool(3) ? DustID.Torch : DustID.GoldFlame;

                Dust dust = Dust.NewDustPerfect(position, dustType, velocity, 0, color, Main.rand.NextFloat(0.82f, 1.34f));
                dust.noGravity = true;
                dust.alpha = dustType == DustID.Smoke ? 105 : 12;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!SpunUp)
                return false;

            float collisionPoint = 0f;
            Vector2 forward = ForwardDirection;
            Vector2 start = Projectile.Center + forward * 12f;
            Vector2 end = Projectile.Center + forward * HitLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, HitWidth, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner == Main.myPlayer)
                Main.LocalPlayer.SetScreenshake(5.2f);

            if (Main.dedServ)
                return;

            Vector2 forward = ForwardDirection;
            EmitCuttingHitBurst(target, forward);

            for (int i = 0; i < 12; i++)
            {
                EmitDust(
                    target.Center + Main.rand.NextVector2Circular(10f, 10f),
                    forward.RotatedByRandom(0.9f) * Main.rand.NextFloat(1.6f, 4.8f),
                    Color.Lerp(new Color(255, 130, 75), Color.White, Main.rand.NextFloat(0.15f, 0.55f)),
                    Main.rand.NextFloat(0.85f, 1.35f),
                    Main.rand.NextBool(3) ? DustID.Smoke : DustID.Torch);
            }
        }

        private void EmitCuttingHitBurst(NPC target, Vector2 forward)
        {
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float targetRadius = MathHelper.Clamp(target.Size.Length() * 0.22f, 14f, 48f);

            for (int i = 0; i < 44; i++)
            {
                float side = Main.rand.NextBool() ? 1f : -1f;
                Vector2 slashDirection = right * side;
                Vector2 spawnPosition = target.Center + slashDirection * Main.rand.NextFloat(-targetRadius * 0.55f, targetRadius * 0.55f) + forward * Main.rand.NextFloat(-targetRadius * 0.35f, targetRadius * 0.35f);
                Vector2 sprayVelocity = slashDirection.RotatedByRandom(0.36f) * Main.rand.NextFloat(3.6f, 12.8f) + forward * Main.rand.NextFloat(0.8f, 4.6f);
                int dustType = i % 5 == 0 ? DustID.SolarFlare : Main.rand.NextBool(5) ? DustID.Smoke : Main.rand.NextBool(3) ? DustID.Torch : DustID.GoldFlame;
                Color color = dustType == DustID.Smoke
                    ? new Color(120, 96, 80)
                    : Color.Lerp(new Color(255, 102, 52), Color.White, Main.rand.NextFloat(0.12f, 0.58f));

                Dust dust = Dust.NewDustPerfect(spawnPosition, dustType, sprayVelocity, 0, color, Main.rand.NextFloat(0.76f, 1.55f));
                dust.noGravity = true;
                dust.alpha = dustType == DustID.Smoke ? 95 : 0;
                dust.fadeIn = Main.rand.NextFloat(0.15f, 0.45f);
            }

            for (int i = 0; i < 18; i++)
            {
                float angle = spin + i * MathHelper.TwoPi / 18f + Main.rand.NextFloat(-0.08f, 0.08f);
                Vector2 sprayDirection = angle.ToRotationVector2();
                Vector2 spawnPosition = target.Center + Main.rand.NextVector2Circular(targetRadius * 0.36f, targetRadius * 0.36f);
                Vector2 sprayVelocity = sprayDirection * Main.rand.NextFloat(4.4f, 10.5f) + forward * Main.rand.NextFloat(1.4f, 3.8f);
                Color color = Color.Lerp(new Color(255, 174, 86), Color.White, Main.rand.NextFloat(0.2f, 0.7f));

                Dust dust = Dust.NewDustPerfect(spawnPosition, Main.rand.NextBool(3) ? DustID.SolarFlare : DustID.GoldFlame, sprayVelocity, 0, color, Main.rand.NextFloat(0.58f, 1.02f));
                dust.noGravity = true;
                dust.alpha = 10;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 forward = ForwardDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float charge = ChargeRatio;
            Vector2 drawCenter = Projectile.Center + Main.rand.NextVector2Circular(1.5f, 1.5f) * charge;
            Color hot = new Color(255, 150, 70, 0);

            for (int i = 0; i < 4; i++)
            {
                Vector2 outlineOffset = (MathHelper.PiOver2 * i + spin * 0.3f).ToRotationVector2() * (1.2f + charge * 1.8f);
                DrawPrism(hot * (0.22f * charge), Projectile.scale * 1.05f, drawCenter + outlineOffset, Projectile.rotation);
            }

            DrawPrism(Color.White, Projectile.scale, drawCenter, Projectile.rotation);

            if (charge > 0f)
            {
                Vector2 start = Projectile.Center + forward * 18f - Main.screenPosition;
                float length = MathHelper.Lerp(38f, HitLength * 1.18f, charge);
                Vector2 center = start + forward * length * 0.5f;
                float railWidth = 0.05f + 0.055f * charge;

                Main.EntitySpriteDraw(
                    line,
                    center + right * (float)System.Math.Sin(spin) * 3f,
                    null,
                    hot * (0.55f * charge),
                    forward.ToRotation() + MathHelper.PiOver2,
                    line.Size() * 0.5f,
                    new Vector2(railWidth, length / line.Height),
                    SpriteEffects.None,
                    0);

                for (int i = 0; i < 3; i++)
                {
                    float gearAngle = spin + i * MathHelper.TwoPi / 3f;
                    Vector2 gearPos = start + forward * (length * (0.34f + i * 0.22f)) + right * (float)System.Math.Sin(gearAngle) * 7f * charge;
                    Main.EntitySpriteDraw(glow, gearPos, null, hot * (0.4f * charge), gearAngle, glow.Size() * 0.5f, 0.12f + charge * 0.08f, SpriteEffects.None, 0);
                }
            }

            return false;
        }
    }
}
