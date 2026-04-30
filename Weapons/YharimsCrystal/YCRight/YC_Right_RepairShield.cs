using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.C_Warships;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRight
{
    public class YC_Right_RepairShield : ModProjectile, ILocalizedModType
    {
        public const int ShieldCount = 5;

        private static readonly float[] ShieldArcDegrees = { -42f, -21f, 0f, 21f, 42f };

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float CooldownTimer => ref Projectile.localAI[0];

        private Player Owner => Main.player[Projectile.owner];
        private int HoldoutIndex => (int)Projectile.ai[1];
        private int ShieldIndex => Utils.Clamp((int)Projectile.ai[0], 0, ShieldArcDegrees.Length - 1);
        private bool ShieldActive => CooldownTimer <= 0f;
        private float ShieldOpacity => ShieldActive ? 1f : Utils.GetLerpValue(48f, 0f, CooldownTimer, true);

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!TryResolveForwardDirection(out Vector2 forward))
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            UpdateShieldPosition(forward);

            if (CooldownTimer > 0f)
            {
                CooldownTimer--;
                if (CooldownTimer == 1f)
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.14f, Pitch = 0.42f }, Projectile.Center);
            }
            else
            {
                TryBlockProjectiles(forward);
            }

            EmitShieldFX(forward);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (ShieldOpacity <= 0f)
                return false;

            DrawShieldBubble();
            DrawShieldArc();
            return false;
        }

        private bool TryResolveForwardDirection(out Vector2 forward)
        {
            forward = Vector2.UnitY;

            if (YC_RightHelper.TryGetHoldout(Projectile.owner, HoldoutIndex, out _, out YC_RightHoldOut rightHoldout))
            {
                forward = rightHoldout.ForwardDirection;
                return true;
            }

            if (YC_WarshipHelper.TryGetHoldout(Projectile.owner, HoldoutIndex, out _, out YC_WarshipHoldout warshipHoldout))
            {
                forward = warshipHoldout.ForwardDirection;
                return true;
            }

            return false;
        }

        private void UpdateShieldPosition(Vector2 forward)
        {
            Vector2 radial = forward.RotatedBy(MathHelper.ToRadians(ShieldArcDegrees[ShieldIndex]));
            Vector2 desiredCenter = Owner.Center + radial * 82f;

            Projectile.Center = Vector2.Lerp(Projectile.Center == Vector2.Zero ? desiredCenter : Projectile.Center, desiredCenter, 0.32f);
            Projectile.rotation = radial.ToRotation() + MathHelper.PiOver2;
            Projectile.scale = 0.95f + 0.04f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f + ShieldIndex * 0.8f);
        }

        private void TryBlockProjectiles(Vector2 forward)
        {
            Vector2 radial = (Projectile.Center - Owner.Center).SafeNormalize(forward);
            Vector2 tangent = radial.RotatedBy(MathHelper.PiOver2);
            Vector2 segmentStart = Projectile.Center - tangent * 24f;
            Vector2 segmentEnd = Projectile.Center + tangent * 24f;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile hostileProjectile = Main.projectile[i];
                if (!hostileProjectile.active || !hostileProjectile.hostile || hostileProjectile.friendly || hostileProjectile.damage <= 0)
                    continue;

                float collisionPoint = 0f;
                if (!Collision.CheckAABBvLineCollision(
                        hostileProjectile.Hitbox.TopLeft(),
                        hostileProjectile.Hitbox.Size(),
                        segmentStart,
                        segmentEnd,
                        20f,
                        ref collisionPoint))
                {
                    continue;
                }

                hostileProjectile.Kill();
                CooldownTimer = 50f;
                SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact with { Volume = 0.3f, Pitch = 0.18f }, Projectile.Center);
                EmitBlockBurst();
                break;
            }
        }

        private void EmitShieldFX(Vector2 forward)
        {
            Lighting.AddLight(Projectile.Center, new Color(100, 230, 255).ToVector3() * 0.35f * ShieldOpacity);

            if (Main.dedServ || Main.GameUpdateCount % 7 != 0 || ShieldOpacity <= 0f)
                return;

            Vector2 radial = (Projectile.Center - Owner.Center).SafeNormalize(forward);
            Vector2 tangent = radial.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 2; i++)
            {
                float arcAngle = MathHelper.Lerp(-0.7f, 0.7f, Main.rand.NextFloat());
                Vector2 arcPoint = Projectile.Center + tangent.RotatedBy(arcAngle) * Main.rand.NextFloat(8f, 18f);
                YC_RightHelper.EmitRightDust(
                    arcPoint,
                    tangent.RotatedBy(arcAngle + MathHelper.PiOver2) * Main.rand.NextFloat(0.4f, 1.4f),
                    Color.Lerp(new Color(120, 230, 255), Color.White, Main.rand.NextFloat(0.35f)),
                    Main.rand.NextFloat(0.7f, 1f),
                    DustID.RainbowTorch);
            }
        }

        private void EmitBlockBurst()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 14; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                YC_RightHelper.EmitRightDust(
                    Projectile.Center,
                    direction * Main.rand.NextFloat(1.6f, 4.2f),
                    Color.Lerp(new Color(100, 220, 255), Color.White, Main.rand.NextFloat(0.45f)),
                    Main.rand.NextFloat(0.8f, 1.15f),
                    DustID.RainbowTorch);
            }
        }

        private void DrawShieldBubble()
        {
            float scale = 0.14f + 0.03f * (0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.8f + Projectile.whoAmI * 0.2f));
            float noiseScale = MathHelper.Lerp(0.4f, 0.75f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.35f + ShieldIndex) * 0.5f + 0.5f);

            Effect shieldEffect = Filters.Scene["CalamityMod:RoverDriveShield"].GetShader().Shader;
            shieldEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.24f);
            shieldEffect.Parameters["blowUpPower"].SetValue(2.3f);
            shieldEffect.Parameters["blowUpSize"].SetValue(0.48f);
            shieldEffect.Parameters["noiseScale"].SetValue(noiseScale);
            shieldEffect.Parameters["shieldOpacity"].SetValue((0.78f + 0.12f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f)) * ShieldOpacity);
            shieldEffect.Parameters["shieldEdgeBlendStrenght"].SetValue(4f);
            shieldEffect.Parameters["shieldColor"].SetValue(new Color(65, 170, 255).ToVector3());
            shieldEffect.Parameters["shieldEdgeColor"].SetValue(Color.Lerp(new Color(110, 255, 245), Color.White, 0.35f).ToVector3());

            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/FrozenCrust").Value;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float texRotation = Main.GlobalTimeWrappedHourly * 0.75f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, shieldEffect, Main.Transform);
            Main.spriteBatch.Draw(tex, pos, null, Color.White, texRotation, tex.Size() / 2f, scale, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        }

        private void DrawShieldArc()
        {
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 radial = (Projectile.Center - Owner.Center).SafeNormalize(Vector2.UnitY);
            Vector2 tangent = radial.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 7; i++)
            {
                float completion = i / 6f;
                float angle = MathHelper.Lerp(-0.95f, 0.95f, completion);
                Vector2 arcPoint = Projectile.Center + tangent.RotatedBy(angle) * 14f + radial * 5f;
                float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f + completion * MathHelper.TwoPi);
                Main.EntitySpriteDraw(
                    glow,
                    arcPoint - Main.screenPosition,
                    null,
                    new Color(110, 240, 255, 0) * (0.18f * ShieldOpacity),
                    0f,
                    glow.Size() * 0.5f,
                    0.08f * pulse,
                    SpriteEffects.None,
                    0);
            }
        }
    }
}
