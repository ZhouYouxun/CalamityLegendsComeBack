using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.B_Flamethrower
{
    public class YC_FlamethrowerHoldout : YC_BaseHoldout
    {
        private const int WarmupFrames = 24;
        private const int BurstFrames = 84;
        private const int RestFrames = 34;
        private const float MinFanHalfAngle = 18f;
        private const float MaxFanHalfAngle = 34f;

        private ref float CycleTimer => ref Projectile.localAI[1];
        private bool IsFiring => CycleTimer >= WarmupFrames && CycleTimer < WarmupFrames + BurstFrames;
        private float BurstCompletion => Utils.GetLerpValue(WarmupFrames, WarmupFrames + BurstFrames, CycleTimer, true);

        protected override float HoldoutDistance => 4f;
        protected override float SoundPitch => -0.08f;

        protected override void OnHoldoutAI()
        {
            CycleTimer++;
            int totalCycle = WarmupFrames + BurstFrames + RestFrames;
            if (CycleTimer >= totalCycle)
                CycleTimer = 0f;

            Vector2 muzzle = Projectile.Center + ForwardDirection * 34f;
            Projectile.scale = 1f + (IsFiring ? 0.04f * (float)System.Math.Sin(HoldFrameCounter * 0.9f) : 0f);

            if (Main.dedServ)
                return;

            if (!IsFiring)
            {
                if (Main.GameUpdateCount % 5 == 0)
                {
                    EmitDust(
                        muzzle + Main.rand.NextVector2Circular(4f, 4f),
                        ForwardDirection.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.6f, 1.7f),
                        Color.Lerp(new Color(255, 94, 44), new Color(255, 224, 120), Main.rand.NextFloat(0.2f, 0.6f)),
                        Main.rand.NextFloat(0.8f, 1.1f),
                        DustID.Torch);
                }

                return;
            }

            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 direction = ForwardDirection;
                NPC target = FindTargetAhead(920f, 20f, false);
                if (target != null)
                {
                    Vector2 desired = (target.Center - muzzle).SafeNormalize(direction);
                    direction = Vector2.Lerp(direction, desired, 0.16f).SafeNormalize(direction);
                }

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    muzzle,
                    direction * Main.rand.NextFloat(20f, 28f),
                    ModContent.ProjectileType<YC_FlameLance>(),
                    (int)(Projectile.damage * MathHelper.Lerp(0.42f, 0.58f, BurstCompletion)),
                    Projectile.knockBack * 0.25f,
                    Projectile.owner,
                    Main.rand.NextFloat(0.78f, 1.18f),
                    Main.rand.NextFloat(0f, 10000f));

                int extraTongues = 2 + (BurstCompletion > 0.35f ? 1 : 0) + (Main.rand.NextBool(3) ? 1 : 0);
                float fanHalfAngle = MathHelper.ToRadians(MathHelper.Lerp(MinFanHalfAngle, MaxFanHalfAngle, Utils.GetLerpValue(0.1f, 0.75f, BurstCompletion, true)));
                Vector2 right = direction.RotatedBy(MathHelper.PiOver2);
                for (int i = 0; i < extraTongues; i++)
                {
                    float angleOffset = Main.rand.NextFloat(-fanHalfAngle, fanHalfAngle);
                    float edgeBias = Main.rand.NextBool(3) ? Main.rand.NextFloat(-fanHalfAngle, fanHalfAngle) * 0.55f : 0f;
                    Vector2 flameDirection = direction.RotatedBy(angleOffset + edgeBias).SafeNormalize(direction);
                    Vector2 spawnPosition = muzzle + right * Main.rand.NextFloat(-10f, 10f) + flameDirection * Main.rand.NextFloat(-4f, 14f);
                    float speed = Main.rand.NextFloat(14.5f, 25.5f) * MathHelper.Lerp(0.95f, 1.12f, BurstCompletion);
                    float scale = Main.rand.NextFloat(0.62f, 1.24f) * MathHelper.Lerp(0.92f, 1.12f, BurstCompletion);

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        spawnPosition,
                        flameDirection * speed,
                        ModContent.ProjectileType<YC_FlameLance>(),
                        (int)(Projectile.damage * Main.rand.NextFloat(0.24f, 0.38f)),
                        Projectile.knockBack * 0.18f,
                        Projectile.owner,
                        scale,
                        HoldFrameCounter + Main.rand.NextFloat(0f, 10000f));
                }
            }

            if (HoldFrameCounter % 5f == 0f)
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.23f, Pitch = -0.23f }, muzzle);

            float muzzleFan = MathHelper.ToRadians(28f);
            for (int i = 0; i < 7; i++)
            {
                Vector2 dustDirection = ForwardDirection.RotatedBy(Main.rand.NextFloat(-muzzleFan, muzzleFan));
                int dustType = Main.rand.NextBool(5) ? DustID.Smoke : Main.rand.NextBool(4) ? DustID.SolarFlare : Main.rand.NextBool(2) ? DustID.Torch : DustID.GoldFlame;
                EmitDust(
                    muzzle + Main.rand.NextVector2Circular(8f, 8f),
                    dustDirection * Main.rand.NextFloat(2.2f, 8.8f) + Main.rand.NextVector2Circular(0.8f, 0.8f),
                    Color.Lerp(new Color(255, 78, 36), new Color(255, 235, 128), Main.rand.NextFloat(0.08f, 0.58f)),
                    Main.rand.NextFloat(0.78f, 1.62f),
                    dustType);
            }

            Lighting.AddLight(muzzle, new Color(255, 94, 44).ToVector3() * 0.82f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);

            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 muzzle = Projectile.Center + ForwardDirection * 34f - Main.screenPosition;
            float opacity = IsFiring ? 0.72f + 0.18f * (float)System.Math.Sin(HoldFrameCounter * 0.75f) : 0.18f;
            Color color = new Color(255, 86, 42, 0);

            Main.EntitySpriteDraw(glow, muzzle, null, color * opacity, Projectile.rotation, glow.Size() * 0.5f, IsFiring ? 0.24f : 0.12f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(glow, muzzle, null, (Color.White with { A = 0 }) * (opacity * 0.35f), Projectile.rotation, glow.Size() * 0.5f, IsFiring ? 0.1f : 0.05f, SpriteEffects.None, 0);
            return false;
        }
    }
}
