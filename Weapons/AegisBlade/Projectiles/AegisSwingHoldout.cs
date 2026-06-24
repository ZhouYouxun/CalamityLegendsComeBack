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
        private const float BladeReach = 128f;
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
            float speed = 14f;
            float[] spread = BalanceAegisBlade.FourFireballsUnlocked()
                ? new[] { -0.58f, -0.2f, 0.2f, 0.58f }
                : new[] { -0.38f, 0.38f };

            Vector2 bladeDirection = currentAngle.ToRotationVector2();
            Vector2 spawnPosition = Owner.MountedCenter + bladeDirection * BladeReach * scale;
            foreach (float angleOffset in spread)
            {
                Vector2 velocity = bladeDirection.RotatedBy(angleOffset).SafeNormalize(bladeDirection) * speed;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPosition + velocity * 0.55f,
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
            float collisionPoint = 0f;
            Vector2 bladeTip = Owner.MountedCenter + currentAngle.ToRotationVector2() * BladeReach * scale;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Owner.MountedCenter, bladeTip, 24f * scale, ref collisionPoint) ? null : false;
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
            Vector2 tipOffset = currentAngle.ToRotationVector2() * BladeReach * scale;
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
                Main.EntitySpriteDraw(swooshTexture, drawPosition, null, BladeGold with { A = 0 } * slashOpacity * 0.46f,
                    drawRotation + MathHelper.PiOver2 * swipeDirection, swooshTexture.Size() * 0.5f,
                    scale * 0.8f, SpriteEffects.None);

                for (int i = 0; i < 12; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 3.4f * slashOpacity;
                    Main.EntitySpriteDraw(swordTexture, drawPosition + offset, null, BladeGold with { A = 0 } * slashOpacity * 0.08f,
                        drawRotation, origin, scale, SpriteEffects.None);
                }

                Vector2 bladeTip = Owner.MountedCenter + currentAngle.ToRotationVector2() * BladeReach * scale - Main.screenPosition;
                Main.EntitySpriteDraw(bloomTexture, bladeTip, null, BladeLight with { A = 0 } * slashOpacity * 0.52f,
                    0f, bloomTexture.Size() * 0.5f, scale * 0.5f, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
            }

            Main.EntitySpriteDraw(swordTexture, drawPosition, null, lightColor, drawRotation, origin, scale, SpriteEffects.None);
            DrawBladeTrail();
            return false;
        }
    }
}
