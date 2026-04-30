using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.D_Laser
{
    public class YC_HelixLaserHoldout : YC_BaseHoldout
    {
        public const int BeamCount = 5;
        public const float MaxCharge = 120f;
        public const float DamageStart = 18f;

        private ref float BeamsSpawned => ref Projectile.localAI[1];

        public bool FocusActive => Projectile.ai[0] == 1f;
        public float FocusBlend => MathHelper.Clamp(Projectile.ai[2], 0f, 1f);
        public float ChargeRatio => MathHelper.Clamp(HoldFrameCounter / MaxCharge, 0f, 1f);
        public float FocusDistance => MathHelper.Clamp(Projectile.ai[1] <= 0f ? 900f : Projectile.ai[1], 220f, 1900f);
        public Vector2 FocusPoint => Projectile.Center + ForwardDirection * FocusDistance;
        public float HoldFrames => HoldFrameCounter;

        protected override float HoldoutDistance => 2f;
        protected override float SoundPitch => 0.12f;

        protected override void OnHoldoutAI()
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Owner.Calamity().rightClickListener = true;
                bool focus = Main.mouseRight && !Main.mapFullscreen && !Main.blockMouse;
                float focusFlag = focus ? 1f : 0f;
                float focusDistance = Vector2.Dot(Main.MouseWorld - Projectile.Center, ForwardDirection);
                focusDistance = MathHelper.Clamp(focusDistance, 220f, 1900f);

                if (Projectile.ai[0] != focusFlag || System.Math.Abs(Projectile.ai[1] - focusDistance) > 8f)
                {
                    Projectile.ai[0] = focusFlag;
                    Projectile.ai[1] = focusDistance;
                    Projectile.netUpdate = true;
                }
            }

            UpdateFocusBlend();

            if (BeamsSpawned == 0f && Projectile.owner == Main.myPlayer)
            {
                BeamsSpawned = 1f;
                KillOwnedBeams();

                for (int i = 0; i < BeamCount; i++)
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        ForwardDirection,
                        ModContent.ProjectileType<YC_HelixLaserBeam>(),
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner,
                        i,
                        Projectile.whoAmI);
                }

                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.28f, Pitch = 0.1f }, Projectile.Center);
            }

            if (Projectile.owner == Main.myPlayer && ChargeRatio > 0.36f)
                SpawnRefractionLasers();

            if (Main.dedServ)
                return;

            Vector2 forward = ForwardDirection;
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float charge = ChargeRatio;
            float focusBlend = FocusBlend;

            if (HoldFrameCounter > 12f && Main.GameUpdateCount % (focusBlend > 0.45f ? 2 : 3) == 0)
            {
                float unfocusedRadius = MathHelper.Lerp(18f, 28f, charge);
                float focusedRadius = MathHelper.Lerp(12f, 5f, charge);
                float radius = MathHelper.Lerp(unfocusedRadius, focusedRadius, focusBlend);
                float phase = HoldFrameCounter * MathHelper.Lerp(0.1f, 0.22f, focusBlend);
                Vector2 offset = side * (float)System.Math.Sin(phase) * radius + forward * 18f;
                EmitDust(
                    Projectile.Center + offset + Main.rand.NextVector2Circular(2f, 2f),
                    forward.RotatedByRandom(0.28f) * Main.rand.NextFloat(0.8f, 2.2f),
                    Color.Lerp(new Color(255, 190, 95), Color.White, Main.rand.NextFloat(0.15f, 0.55f)),
                    Main.rand.NextFloat(0.75f, 1.05f),
                    DustID.GoldFlame);
            }
        }

        public override void OnKill(int timeLeft)
        {
            KillOwnedBeams();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);

            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center + ForwardDirection * 18f - Main.screenPosition;
            float charge = ChargeRatio;
            float focusBlend = FocusBlend;
            float focusPulse = 1f + focusBlend * 0.12f * (float)System.Math.Sin(HoldFrameCounter * 0.35f);
            Color color = Color.Lerp(new Color(255, 176, 92, 0), new Color(255, 236, 170, 0), focusBlend) * (0.28f + charge * 0.62f);

            Main.EntitySpriteDraw(glow, drawPosition, null, color, Projectile.rotation, glow.Size() * 0.5f, (0.12f + charge * 0.18f) * focusPulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(glow, drawPosition, null, (Color.White with { A = 0 }) * (0.12f + charge * 0.28f), Projectile.rotation, glow.Size() * 0.5f, (0.05f + charge * 0.08f) * focusPulse, SpriteEffects.None, 0);

            if (focusBlend > 0.03f)
            {
                Vector2 focusDraw = FocusPoint - Main.screenPosition;
                Main.EntitySpriteDraw(glow, focusDraw, null, new Color(255, 220, 135, 0) * (0.32f * focusBlend), Projectile.rotation, glow.Size() * 0.5f, (0.18f + charge * 0.18f) * focusBlend, SpriteEffects.None, 0);
            }

            return false;
        }

        private void UpdateFocusBlend()
        {
            float desiredBlend = FocusActive ? 1f : 0f;
            float currentBlend = FocusBlend;
            float response = FocusActive
                ? MathHelper.Lerp(0.018f, 0.082f, currentBlend)
                : 0.065f;

            float nextBlend = MathHelper.Lerp(currentBlend, desiredBlend, response);
            if (System.Math.Abs(nextBlend - desiredBlend) < 0.003f)
                nextBlend = desiredBlend;

            Projectile.ai[2] = MathHelper.Clamp(nextBlend, 0f, 1f);
        }

        private void SpawnRefractionLasers()
        {
            int frame = (int)HoldFrameCounter;
            float focusBlend = FocusBlend;

            int needleInterval = focusBlend > 0.55f ? 10 : 14;
            if (frame % needleInterval == 0)
            {
                Vector2 forward = ForwardDirection;
                Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
                int needleCount = focusBlend > 0.55f ? 4 : 3;

                for (int i = 0; i < needleCount; i++)
                {
                    float centered = i - (needleCount - 1f) * 0.5f;
                    Vector2 origin = Projectile.Center + forward * 34f + side * centered * MathHelper.Lerp(22f, 10f, focusBlend);
                    Vector2 targetDirection = focusBlend > 0.12f
                        ? (FocusPoint - origin).SafeNormalize(forward)
                        : forward.RotatedBy(centered * 0.045f + Main.rand.NextFloat(-0.025f, 0.025f));

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        origin,
                        targetDirection * Main.rand.NextFloat(3.35f, 4.15f),
                        ModContent.ProjectileType<YC_PrismNeedleLaser>(),
                        (int)(Projectile.damage * MathHelper.Lerp(0.34f, 0.46f, focusBlend)),
                        Projectile.knockBack * 0.2f,
                        Projectile.owner,
                        i + Main.rand.NextFloat(),
                        MathHelper.Lerp(0.82f, 1.1f, focusBlend));
                }
            }

            int lanceInterval = focusBlend > 0.55f ? 42 : 64;
            if (frame % lanceInterval != 0)
                return;

            Vector2 baseForward = ForwardDirection;
            Vector2 lateral = baseForward.RotatedBy(MathHelper.PiOver2);
            int lanceCount = focusBlend > 0.55f ? 3 : 2;

            for (int i = 0; i < lanceCount; i++)
            {
                float centered = i - (lanceCount - 1f) * 0.5f;
                Vector2 origin = Projectile.Center + baseForward * 28f + lateral * centered * MathHelper.Lerp(36f, 15f, focusBlend);
                Vector2 direction = focusBlend > 0.18f
                    ? (FocusPoint - origin).SafeNormalize(baseForward)
                    : baseForward.RotatedBy(centered * 0.055f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    origin,
                    direction,
                    ModContent.ProjectileType<SODLazer>(),
                    (int)(Projectile.damage * MathHelper.Lerp(0.58f, 0.76f, focusBlend)),
                    Projectile.knockBack * 0.25f,
                    Projectile.owner);
            }
        }

        private void KillOwnedBeams()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner || other.type != ModContent.ProjectileType<YC_HelixLaserBeam>())
                    continue;

                if ((int)other.ai[1] == Projectile.whoAmI)
                    other.Kill();
            }
        }
    }
}
