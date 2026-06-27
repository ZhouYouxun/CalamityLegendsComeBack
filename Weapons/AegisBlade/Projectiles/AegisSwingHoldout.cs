using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    public class AegisSwingHoldout : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/AegisBlade/AegisBlade";

        private const int SwingDuration = 26;
        private const float FireballSpawnProgress = 0.65f;
        private const float SwordVisualScale = 1.55f;
        private const float BladeReach = 192f;
        private const int BladeTrailHistoryFrames = 16;

        private Player Owner => Main.player[Projectile.owner];
        private readonly Vector2[] bladeTipHistory = new Vector2[BladeTrailHistoryFrames];
        private int bladeTipHistoryLength;
        private int swingCount;
        private int stateTimer;
        private int swingDirection = 1;
        private bool fireballSpawned;
        private float currentAngle;
        private float startAngle;
        private float endAngle;
        private Vector2 lockedMouseDirection = Vector2.UnitX;
        private float scale = 1f;
        private float slashOpacity;

        private static readonly Color BladeGold = new(255, 205, 80);
        private static readonly Color BladeLight = new(255, 242, 185);
        private static readonly Color BladeFire = new(255, 145, 52);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = SwingDuration;
            Projectile.timeLeft = 4;
            Projectile.noEnchantmentVisuals = true;
            Projectile.ContinuouslyUpdateDamageStats = true;
        }

        private bool IsLeftHeld()
        {
            return Owner.channel && (Main.myPlayer != Projectile.owner || Main.mouseLeft) &&
                !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface;
        }

        private Vector2 GetMouseDirection()
        {
            return Owner.MountedCenter.DirectionTo(AegisBlade.GetMouseWorld(Owner))
                .SafeNormalize(Vector2.UnitX * Owner.direction);
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<AegisBlade>())
            {
                Projectile.Kill();
                return;
            }

            scale = Owner.GetMeleeScale() * SwordVisualScale;
            Owner.heldProj = Projectile.whoAmI;
            Owner.GetModPlayer<AegisBladePlayer>().IsSwinging = true;
            Owner.itemTime = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            Projectile.timeLeft = 4;
            Projectile.Center = Owner.MountedCenter;

            DoSwing();

            float armAngle = currentAngle - MathHelper.ToRadians(130f);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armAngle);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armAngle);
            Owner.itemLocation = Owner.Center;
            Owner.itemRotation = currentAngle;
        }

        private void StartSwing()
        {
            stateTimer = 0;
            fireballSpawned = false;
            lockedMouseDirection = GetMouseDirection();

            swingDirection = -Math.Sign(Owner.Center.X - AegisBlade.GetMouseWorld(Owner).X);
            if (swingDirection == 0)
                swingDirection = Owner.direction;
            Owner.direction = swingDirection;

            float baseAngle = lockedMouseDirection.ToRotation();
            int parity = swingCount % 2 == 0 ? 1 : -1;
            startAngle = baseAngle + MathHelper.ToRadians(-110f * swingDirection * parity);
            endAngle = baseAngle + MathHelper.ToRadians(-110f * swingDirection * -parity);
            currentAngle = startAngle;
        }

        private void DoSwing()
        {
            if (stateTimer == 0)
                StartSwing();

            stateTimer++;
            float progress = stateTimer / (float)SwingDuration;
            currentAngle = MathHelper.Lerp(startAngle, endAngle, CalamityUtils.EaseInOutExp(progress, 5f, 2f));

            if (!fireballSpawned && progress >= FireballSpawnProgress && Main.myPlayer == Projectile.owner)
            {
                SpawnFireballs();
                fireballSpawned = true;
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.82f, Pitch = Main.rand.NextFloat(-0.12f, 0.16f) }, Owner.Center);
            }

            bool hitWindow = progress >= 0.23f && progress <= 0.87f;
            TrackBladeTrail(hitWindow);
            EmitSwingTrail(hitWindow);

            if (stateTimer < SwingDuration)
                return;

            if (IsLeftHeld())
            {
                swingCount++;
                Projectile.ResetLocalNPCHitImmunity();
                stateTimer = 0;
                fireballSpawned = false;
                return;
            }

            Projectile.Kill();
        }

        private void SpawnFireballs()
        {
            int fireballType = ModContent.ProjectileType<AegisFireball>();
            int damage = Math.Max(1, (int)(Projectile.damage * 0.6f));

            // Four orbs leave as a wide fan. The old 0.9-1.25 speed read as drifting sparks.
            Vector2 mouseDir = Owner.MountedCenter.DirectionTo(AegisBlade.GetMouseWorld(Owner))
                .SafeNormalize(Vector2.UnitX * Owner.direction);

            for (int i = 0; i < 4; i++)
            {
                float centeredIndex = i - 1.5f;
                float speed       = Main.rand.NextFloat(5.4f, 7.5f);
                float angleOffset = MathHelper.ToRadians(centeredIndex * 15f + Main.rand.NextFloat(-3f, 3f));
                Vector2 velocity  = mouseDir.RotatedBy(angleOffset) * speed;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Owner.MountedCenter,
                    velocity, fireballType, damage, 2f, Projectile.owner);
            }
        }

        public override bool? CanDamage()
        {
            float progress = stateTimer / (float)SwingDuration;
            return progress >= 0.23f && progress <= 0.87f ? null : false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = currentAngle.ToRotationVector2();
            float swipeDirection = Math.Sign(endAngle - startAngle);
            if (swipeDirection == 0f)
                swipeDirection = 1f;

            if (CheckBladeLine(targetHitbox, direction, 66f * scale, BladeReach * scale + 36f * scale))
                return null;

            Vector2 trailingDirection = (currentAngle - swipeDirection * 0.26f).ToRotationVector2();
            if (CheckBladeLine(targetHitbox, trailingDirection, 54f * scale, BladeReach * scale + 20f * scale))
                return null;

            Vector2 leadingDirection = (currentAngle + swipeDirection * 0.14f).ToRotationVector2();
            return CheckBladeLine(targetHitbox, leadingDirection, 46f * scale, BladeReach * scale) ? null : false;
        }

        private bool CheckBladeLine(Rectangle targetHitbox, Vector2 direction, float width, float reach)
        {
            float collisionPoint = 0f;
            Vector2 start = Owner.MountedCenter + direction * 20f * scale;
            Vector2 end = Owner.MountedCenter + direction * reach;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                start,
                end,
                width,
                ref collisionPoint);
        }

        private void TrackBladeTrail(bool hitWindow)
        {
            if (!hitWindow)
            {
                slashOpacity = MathHelper.Lerp(slashOpacity, 0f, 0.25f);
                if (slashOpacity < 0.01f)
                    bladeTipHistoryLength = 0;
                return;
            }

            slashOpacity = MathHelper.Lerp(slashOpacity, 1f, 0.42f);
            // 刀光显示在剑身50%处（非剑尖）
            Vector2 tipOffset = currentAngle.ToRotationVector2() * BladeReach * scale * 0.5f;
            Array.Copy(bladeTipHistory, 0, bladeTipHistory, 1, bladeTipHistory.Length - 1);
            bladeTipHistory[0] = tipOffset;
            if (bladeTipHistoryLength < bladeTipHistory.Length)
                bladeTipHistoryLength++;
        }

        private void EmitSwingTrail(bool hitWindow)
        {
            if (Main.dedServ || !hitWindow)
                return;

            Vector2 direction = currentAngle.ToRotationVector2();
            Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2 * Math.Sign(endAngle - startAngle));
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = Owner.MountedCenter + direction * Main.rand.NextFloat(48f, BladeReach) * scale;
                Vector2 velocity = perpendicular * Main.rand.NextFloat(1.5f, 4.2f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(position, velocity, false,
                    Main.rand.Next(10, 16), Main.rand.NextFloat(0.35f, 0.62f),
                    Main.rand.NextBool(3) ? BladeLight : BladeGold));
            }
        }

        private void DrawBladeTrail()
        {
            if (bladeTipHistoryLength < 2 || slashOpacity <= 0.01f || Main.dedServ)
                return;

            Main.spriteBatch.EnterShaderRegion();
            var slashShader = GameShaders.Misc["CalamityMod:ExobladeSlash"];
            slashShader.SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/VoronoiShapes"));
            slashShader.UseColor(BladeGold);
            slashShader.UseSecondaryColor(BladeFire);
            slashShader.Shader.Parameters["fireColor"].SetValue(BladeLight.ToVector3());
            slashShader.Shader.Parameters["flipped"].SetValue(Owner.direction == 1);
            slashShader.Apply();

            List<Vector2> trailPoints = new(bladeTipHistoryLength);
            for (int i = 0; i < bladeTipHistoryLength; i++)
                trailPoints.Add(bladeTipHistory[i]);

            float WidthFunction(float completionRatio, Vector2 _)
            {
                return scale * 24f * Utils.GetLerpValue(1f, 0f, completionRatio, true) * slashOpacity;
            }

            Color ColorFunction(float completionRatio, Vector2 _)
            {
                return Color.White * Utils.GetLerpValue(0.95f, 0.25f, completionRatio, true) * slashOpacity;
            }

            PrimitiveRenderer.RenderTrail(trailPoints,
                new PrimitiveSettings(WidthFunction, ColorFunction, (_, _) => Owner.MountedCenter, shader: slashShader),
                BladeTrailHistoryFrames);
            Main.spriteBatch.ExitShaderRegion();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D swordTexture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D swooshTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge").Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = new(0f, swordTexture.Height);
            float drawRotation = currentAngle + MathHelper.PiOver4;
            Vector2 drawPosition = Owner.MountedCenter - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);

            if (slashOpacity > 0.01f)
            {
                Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
                float swipeDirection = Math.Sign(endAngle - startAngle);
                // 刀光（swoosh和bloom）放在剑身50%处，与刀光trail保持一致
                Vector2 bladeMidOffset = currentAngle.ToRotationVector2() * BladeReach * scale * 0.5f;
                Vector2 swooshCenter   = drawPosition + bladeMidOffset;
                Main.EntitySpriteDraw(swooshTexture, swooshCenter, null, BladeGold with { A = 0 } * slashOpacity * 0.46f,
                    drawRotation + MathHelper.PiOver2 * swipeDirection, swooshTexture.Size() * 0.5f,
                    scale * 0.8f, SpriteEffects.None);

                for (int i = 0; i < 12; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 3.4f * slashOpacity;
                    Main.EntitySpriteDraw(swordTexture, drawPosition + offset, null, BladeGold with { A = 0 } * slashOpacity * 0.08f,
                        drawRotation, origin, scale, SpriteEffects.None);
                }

                Vector2 bladeMid = Owner.MountedCenter + currentAngle.ToRotationVector2() * BladeReach * scale * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(bloomTexture, bladeMid, null, BladeLight with { A = 0 } * slashOpacity * 0.52f,
                    0f, bloomTexture.Size() * 0.5f, scale * 0.5f, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
            }

            Main.EntitySpriteDraw(swordTexture, drawPosition, null, lightColor, drawRotation, origin, scale, SpriteEffects.None);
            DrawBladeTrail();
            return false;
        }
    }
}
