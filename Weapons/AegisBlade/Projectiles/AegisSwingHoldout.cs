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
        private const float FireballSpawnProgress = 0.3f;
        private const float SwordVisualScale = 1.55f;
        private const float BladeReach = 104f;
        private const int BladeTrailHistoryFrames = 16;
        private const float FirstSwingStartOffsetDegrees = -20f;
        private const float FirstSwingSlowPointDegrees = 80f;
        private const float SwingLoopEndOffsetDegrees = 135f;
        private const float LoopSweepDegrees = 1080f;
        private const float FirstSwingSlowPointProgress = 0.26f;

        private Player Owner => Main.player[Projectile.owner];
        private readonly Vector2[] bladeTipHistory = new Vector2[BladeTrailHistoryFrames];
        private int bladeTipHistoryLength;
        private int swingCount;
        private int stateTimer;
        private int swingDirection = 1;
        private bool fireballSpawned;
        private float currentAngle;
        private float startAngle;
        private float slowPointAngle;
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

            if (swingCount == 0)
            {
                swingDirection = -Math.Sign(Owner.Center.X - AegisBlade.GetMouseWorld(Owner).X);
                if (swingDirection == 0)
                    swingDirection = Owner.direction;
            }
            Owner.direction = swingDirection;

            float baseAngle = lockedMouseDirection.ToRotation();
            if (swingCount == 0)
            {
                startAngle = baseAngle + MathHelper.ToRadians(FirstSwingStartOffsetDegrees * swingDirection);
                slowPointAngle = baseAngle + MathHelper.ToRadians(FirstSwingSlowPointDegrees * swingDirection);
                endAngle = baseAngle + MathHelper.ToRadians((LoopSweepDegrees + SwingLoopEndOffsetDegrees) * swingDirection);
                currentAngle = startAngle;
                return;
            }

            startAngle = currentAngle;
            slowPointAngle = startAngle;
            endAngle = startAngle + MathHelper.ToRadians(LoopSweepDegrees * swingDirection);
            currentAngle = startAngle;
        }

        private void DoSwing()
        {
            if (stateTimer == 0)
                StartSwing();

            float progress = GetCurrentSwingProgress();
            currentAngle = EvaluateSwingAngle(progress);

            if (!fireballSpawned && progress >= FireballSpawnProgress && Main.myPlayer == Projectile.owner)
            {
                SpawnFireballs();
                SpawnBorrowedLazharShots();
                fireballSpawned = true;
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.82f, Pitch = Main.rand.NextFloat(-0.12f, 0.16f) }, Owner.Center);
            }

            bool hitWindow = progress >= 0.23f && progress <= 0.87f;
            TrackBladeTrail(hitWindow);
            EmitSwingTrail(hitWindow);

            if (progress < 1f)
            {
                stateTimer++;
                return;
            }

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

        private float GetCurrentSwingProgress()
        {
            return MathHelper.Clamp(stateTimer / (float)SwingDuration, 0f, 1f);
        }

        private float EvaluateSwingAngle(float progress)
        {
            if (swingCount == 0 && progress < FirstSwingSlowPointProgress)
            {
                float localProgress = MathHelper.Clamp(progress / FirstSwingSlowPointProgress, 0f, 1f);
                float eased = 1f - MathF.Pow(1f - localProgress, 3f);
                return MathHelper.Lerp(startAngle, slowPointAngle, eased);
            }

            float spinProgress = swingCount == 0
                ? MathHelper.Clamp((progress - FirstSwingSlowPointProgress) / (1f - FirstSwingSlowPointProgress), 0f, 1f)
                : progress;
            float easedSpin = spinProgress * spinProgress * (3f - 2f * spinProgress);
            float spinStart = swingCount == 0 ? slowPointAngle : startAngle;
            return MathHelper.Lerp(spinStart, endAngle, easedSpin);
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
                float speed       = Main.rand.NextFloat(10.8f, 15f);
                float angleOffset = MathHelper.ToRadians(centeredIndex * (5f / 3f) + Main.rand.NextFloat(-0.4f, 0.4f));
                Vector2 velocity  = mouseDir.RotatedBy(angleOffset) * speed;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Owner.MountedCenter,
                    velocity, fireballType, damage, 2f, Projectile.owner);
            }
        }

        private void SpawnBorrowedLazharShots()
        {
            Vector2 shootDirection = lockedMouseDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 normal = shootDirection.RotatedBy(MathHelper.PiOver2);
            int laserType = ModContent.ProjectileType<AegisBorrowedLazharLaser>();
            int orbitalType = ModContent.ProjectileType<AegisBorrowedOrbitalStrike>();
            int laserDamage = Math.Max(1, (int)(Projectile.damage * 0.42f));
            int orbitalDamage = Math.Max(1, (int)(Projectile.damage * 0.52f));

            for (int i = 0; i < 4; i++)
            {
                float offset = (i - 1.5f) * 18f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Owner.MountedCenter + shootDirection * 44f + normal * offset,
                    shootDirection * 30f,
                    laserType,
                    laserDamage,
                    Projectile.knockBack * 0.35f,
                    Projectile.owner);
            }

            for (int i = 0; i < 4; i++)
            {
                int targetIndex = FindOrbitalTargetIndex();
                Vector2 destination = targetIndex >= 0
                    ? Main.npc[targetIndex].Center + Main.rand.NextVector2Circular(42f, 34f)
                    : Owner.Center + Main.rand.NextVector2Circular(900f, 520f);

                Vector2 spawnPosition = new(
                    destination.X,
                    destination.Y - Main.rand.NextFloat(940f, 1220f));

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    Vector2.UnitY * Main.rand.NextFloat(34f, 42f),
                    orbitalType,
                    orbitalDamage,
                    Projectile.knockBack * 0.55f,
                    Projectile.owner,
                    targetIndex,
                    destination.X,
                    destination.Y);
            }
        }

        private int FindOrbitalTargetIndex()
        {
            const float range = 100f * 16f;
            int bestIndex = -1;
            float bestDistance = range;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.life <= 0 || !npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.Distance(Owner.Center, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestIndex = i;
            }

            return bestIndex;
        }

        public override bool? CanDamage()
        {
            float progress = GetCurrentSwingProgress();
            return progress >= 0.23f && progress <= 0.87f ? null : false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = currentAngle.ToRotationVector2();
            float swipeDirection = Math.Sign(endAngle - startAngle);
            if (swipeDirection == 0f)
                swipeDirection = 1f;

            if (CheckBladeLine(targetHitbox, direction, 42f * scale, BladeReach * scale))
                return null;

            Vector2 trailingDirection = (currentAngle - swipeDirection * 0.26f).ToRotationVector2();
            if (CheckBladeLine(targetHitbox, trailingDirection, 34f * scale, BladeReach * scale * 0.96f))
                return null;

            Vector2 leadingDirection = (currentAngle + swipeDirection * 0.14f).ToRotationVector2();
            return CheckBladeLine(targetHitbox, leadingDirection, 30f * scale, BladeReach * scale * 0.92f) ? null : false;
        }

        private bool CheckBladeLine(Rectangle targetHitbox, Vector2 direction, float width, float reach)
        {
            float collisionPoint = 0f;
            Vector2 start = Owner.MountedCenter + direction * 8f * scale;
            Vector2 end = Owner.MountedCenter + direction * reach;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                start,
                end,
                width,
                ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                ModContent.ProjectileType<AegisSparkExplosion>(), Math.Max(1, (int)(Projectile.damage * 0.42f)),
                Projectile.knockBack * 0.35f, Projectile.owner);
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

                // BB-style disc swoosh centered on player
                Main.EntitySpriteDraw(swooshTexture, drawPosition, null, BladeGold with { A = 0 } * slashOpacity * 0.52f,
                    drawRotation + MathHelper.PiOver2 * swipeDirection, swooshTexture.Size() * 0.5f,
                    scale * 1.15f, SpriteEffects.None);

                // Ghost glow rings (BB-style, 16 offsets)
                for (int i = 0; i < 16; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * 4.5f * slashOpacity;
                    Main.EntitySpriteDraw(swordTexture, drawPosition + offset, null, BladeGold with { A = 0 } * slashOpacity * 0.09f,
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
