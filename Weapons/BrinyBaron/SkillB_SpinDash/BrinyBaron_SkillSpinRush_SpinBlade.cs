using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using CalamityLegendsComeBack.Weapons.BrinyBaron;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillB_SpinDash
{
    public class BrinyBaron_SkillSpinRush_SpinBlade : BaseCustomUseStyleProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron";
        public override int AssignedItemID => ModContent.ItemType<NewLegendBrinyBaron>();

        public override float HitboxOutset => 115f;
        public override Vector2 HitboxSize => new Vector2(175f, 175f);
        public override float HitboxRotationOffset => MathHelper.ToRadians(-45f);
        public override Vector2 SpriteOrigin => new Vector2(0f, 120f);

        private Vector2 mousePos;
        private Vector2 aimVel;

        private bool doSwing = true;
        private bool postSwing = false;
        private bool finalFlip = false;
        private bool swingSound = true;

        private float fadeIn = 0f;
        private int useAnim;

        // 冲刺
        private bool dashStarted = false;
        private Vector2 dashVelocity = Vector2.Zero;
        private Vector2 dashDirection = Vector2.UnitX;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void WhenSpawned()
        {
            Projectile.timeLeft = Owner.HeldItem.useAnimation + 1;
            Projectile.knockBack = 0f;
            Projectile.scale = 1f;
            Projectile.ai[1] = -1f;

            mousePos = Main.MouseWorld;
            aimVel = (Owner.Center - Main.MouseWorld).SafeNormalize(Vector2.UnitX) * 65f;
            useAnim = Owner.itemAnimationMax;

            Owner.direction = mousePos.X < Owner.Center.X ? -1 : 1;
            FlipAsSword = (Owner.Center - Main.MouseWorld).SafeNormalize(Vector2.UnitX).X > 0f;
        }

        public override void ResetStyle()
        {
            CanHit = false;
        }

        public override void UseStyle()
        {
            AnimationProgress = Animation % useAnim;
            DrawUnconditionally = false;

            mousePos = (CanHit || postSwing) ? Owner.Center - aimVel : Main.MouseWorld;

            fadeIn = CanHit
                ? MathHelper.Lerp(fadeIn, 1f, 0.12f)
                : MathHelper.Lerp(fadeIn, 0f, 0.16f);

            if (!doSwing)
            {
                Projectile.Kill();
                return;
            }

            if (!CanHit && !postSwing)
                Owner.direction = mousePos.X < Owner.Center.X ? -1 : 1;
            else
                Owner.direction = (Owner.Center - aimVel).X < Owner.Center.X ? -1 : 1;

            Projectile.rotation = Projectile.rotation.AngleLerp(
                Owner.AngleTo(mousePos) + MathHelper.ToRadians(Owner.direction == -1 ? 0f : 120f),
                0.1f
            );

            // =========================
            // 前摇
            // =========================
            if (AnimationProgress < (useAnim / 3))
            {
                aimVel = (Owner.Center - Main.MouseWorld).SafeNormalize(Vector2.UnitX) * 65f;
                dashDirection = (Main.MouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX);

                CanHit = false;
                postSwing = false;

                if (AnimationProgress == 0)
                    swingSound = true;

                RotationOffset = RotationOffset.AngleLerp(
                    MathHelper.ToRadians(-45f * Projectile.ai[1] * Owner.direction),
                    0.2f
                );

                if (Main.rand.NextBool(2))
                {
                    Vector2 vel = new Vector2(0f, 3f * -Projectile.ai[1] * Owner.direction)
                        .RotatedBy(FinalRotation + MathHelper.ToRadians(-45f));

                    Vector2 pos = Owner.Center +
                        new Vector2(Main.rand.Next(10, 135), 0f)
                        .RotatedBy(FinalRotation + MathHelper.ToRadians(-45f));

                    GeneralParticleHandler.SpawnParticle(
                        new CustomPulse(
                            pos,
                            vel * Main.rand.NextFloat(0.8f, 1.2f),
                            Main.rand.NextBool(4) ? Color.DeepSkyBlue : Color.Cyan,
                            "CalamityMod/Particles/HealingPlus",
                            new Vector2(1f),
                            Main.rand.NextFloat(-2f, 2f),
                            Main.rand.NextFloat(0.8f, 1.2f),
                            0.2f,
                            23
                        )
                    );
                }
            }
            else
            {
                if (!finalFlip)
                    FlipAsSword = Owner.direction < 0;

                float time = AnimationProgress - (useAnim / 8f);
                float timeMax = useAnim - (useAnim / 8f);

                // =========================
                // 音效
                // =========================
                if (time >= timeMax * 0.3f && swingSound)
                {
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, Pitch = 0.25f }, Projectile.Center);
                    SoundEngine.PlaySound(SoundID.Item84 with { Volume = 0.65f, Pitch = 0.1f }, Projectile.Center);
                    swingSound = false;
                }

                // =========================
                // 冲刺 + 加速
                // =========================
                if (!dashStarted && time >= timeMax * 0.18f)
                {
                    dashStarted = true;
                    dashVelocity = dashDirection * 22f;
                }

                if (dashStarted)
                {
                    dashVelocity += dashDirection * 1.45f;

                    if (dashVelocity.Length() > 42f)
                        dashVelocity = dashVelocity.SafeNormalize(Vector2.UnitX) * 42f;

                    Owner.velocity = dashVelocity;

                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 pos = Owner.Center - dashVelocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(10f, 35f);
                        Vector2 vel = -dashVelocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.1f, 0.25f);

                        Dust d1 = Dust.NewDustPerfect(pos, DustID.Water, vel);
                        d1.noGravity = true;
                        d1.scale = Main.rand.NextFloat(1.1f, 1.5f);

                        Dust d2 = Dust.NewDustPerfect(pos, DustID.Frost, vel * 0.7f);
                        d2.noGravity = true;
                        d2.scale = Main.rand.NextFloat(0.9f, 1.3f);
                    }
                }

                // =========================
                // 回旋斩核心（比原版更大角度）
                // =========================
                CanHit = time > timeMax * 0.22f && time < timeMax * 0.9f;

                RotationOffset = MathHelper.Lerp(
                    RotationOffset,
                    MathHelper.ToRadians(
                        MathHelper.Lerp(
                            -45f * Projectile.ai[1] * Owner.direction,
                            465f * -Projectile.ai[1] * Owner.direction,
                            Utils.GetLerpValue(0f, 1f, time / timeMax, true)
                        )
                    ),
                    0.2f
                );

                if (time >= timeMax)
                    doSwing = false;

                if (time < timeMax * 0.88f)
                    postSwing = true;

                // =========================
                // 命中特效
                // =========================
                if (CanHit)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 vel = new Vector2(0f, 3f * -Projectile.ai[1] * Owner.direction)
                            .RotatedBy(FinalRotation + MathHelper.ToRadians(-45f));

                        Vector2 pos = Owner.Center +
                            new Vector2(Main.rand.Next(5, 135), 0f)
                            .RotatedBy(FinalRotation + MathHelper.ToRadians(-45f));

                        GeneralParticleHandler.SpawnParticle(
                            new CustomPulse(
                                pos,
                                -vel * Main.rand.NextFloat(1.1f, 3f),
                                Color.Cyan,
                                "CalamityMod/Particles/Sparkle",
                                new Vector2(2f, 1f),
                                vel.ToRotation(),
                                Main.rand.NextFloat(0.4f, 1f),
                                0.2f,
                                23
                            )
                        );
                    }
                }

                Lighting.AddLight(Projectile.Center, new Vector3(0.05f, 0.23f, 0.32f) * fadeIn);
            }

            ArmRotationOffset = MathHelper.ToRadians(-140f);
            ArmRotationOffsetBack = MathHelper.ToRadians(-140f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if ((useAnim > 0 || DrawUnconditionally) && Owner.ItemAnimationActive)
            {
                var tex = ModContent.Request<Texture2D>(Texture);
                var swoosh = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmearSmokey");

                float r = FlipAsSword ? MathHelper.ToRadians(90f) : 0f;

                Main.EntitySpriteDraw(
                    swoosh.Value,
                    Owner.Center - Main.screenPosition,
                    null,
                    Color.Cyan * fadeIn,
                    Projectile.rotation + MathHelper.PiOver4 * Owner.direction + RotationOffset * 1.75f,
                    swoosh.Size() * 0.5f,
                    Projectile.scale * 2.1f,
                    SpriteEffects.None
                );

                Main.EntitySpriteDraw(
                    tex.Value,
                    Projectile.Center - Main.screenPosition,
                    null,
                    lightColor,
                    Projectile.rotation + RotationOffset + r,
                    SpriteOrigin,
                    Projectile.scale,
                    SpriteEffects.None
                );
            }

            return false;
        }
    }
}