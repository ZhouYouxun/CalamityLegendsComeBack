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

        private const int SwingDuration = 52;
        private const float FireballSpawnProgress = 0.3f;
        private const float SwordVisualScale = 1.55f;
        private const float BladeReach = 104f;
        private const float SpinDiscRadiusScale = 0.134f;
        private const float SpinDiscSpriteScale = 0.201f;
        private const float SpinSprayRadiusScale = 0.8f;    // 线性粒子喷射覆盖半径（独立于刀盘）
        private const float SpinSpraySpriteScale = 0.3f;    // 线性粒子本体大小（独立于刀盘）
        private const int BladeTrailHistoryFrames = 16;
        private const int TrackingSoulInterval = 4;
        private const int TrackingSoulBurstCount = 2;
        private const float TrackingSoulSpeed = 30f * 0.67f;
        private const float LoopSweepDegrees = 1080f;   // 每次挥动 3 圈，线性匀速

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
        private float outlineBaseOpacity;
        private float outlinePulseOpacity;
        private float discRingOpacity;   // 刀盘环形光圈强度
        private float discFadeIn;        // 刀盘整体淡入（20帧内透明度逐渐降低至完全显示）
        private const int DiscFadeInFrames = 20;

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

            discFadeIn = Math.Min(discFadeIn + 1f / DiscFadeInFrames, 1f);
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
                // 第一圈从瞄准方向直接起步，无初始偏移
                currentAngle = lockedMouseDirection.ToRotation();
            }
            Owner.direction = swingDirection;

            startAngle = currentAngle;
            endAngle   = startAngle + MathHelper.ToRadians(LoopSweepDegrees * swingDirection);
        }

        private void DoSwing()
        {
            if (stateTimer == 0)
                StartSwing();

            float progress = GetCurrentSwingProgress();
            currentAngle = EvaluateSwingAngle(progress);

            // 每帧跟刀尖方向实时更新玩家朝向（仿 BB ApplyRightSpinHeldPose）
            Owner.direction = MathF.Cos(currentAngle) >= 0f ? 1 : -1;

            if (Main.myPlayer == Projectile.owner && stateTimer % TrackingSoulInterval == 0)
                SpawnTrackingSouls();

            if (!fireballSpawned && progress >= FireballSpawnProgress && Main.myPlayer == Projectile.owner)
            {
                SpawnFireballs();
                SpawnOrbitalStrikes();
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
            // 线性匀速旋转，消除循环之间的加减速停顿感
            return startAngle + MathHelper.ToRadians(LoopSweepDegrees * swingDirection) * progress;
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

        private void SpawnTrackingSouls()
        {
            int laserType = ModContent.ProjectileType<AegisBorrowedLazharLaser>();
            int laserDamage = Math.Max(1, (int)(Projectile.damage * 0.42f));

            for (int i = 0; i < TrackingSoulBurstCount; i++)
            {
                Vector2 shootDirection = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2();
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Owner.MountedCenter + shootDirection * 44f * scale,
                    shootDirection * TrackingSoulSpeed,
                    laserType,
                    laserDamage,
                    Projectile.knockBack * 0.35f,
                    Projectile.owner);
            }
        }

        private void SpawnOrbitalStrikes()
        {
            int orbitalType = ModContent.ProjectileType<AegisBorrowedOrbitalStrike>();
            int orbitalDamage = Math.Max(1, (int)(Projectile.damage * 0.52f));

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

        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            hitbox = GetSwingDamageHitbox();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Rectangle swingHitbox = GetSwingDamageHitbox();
            bool boxHit = Collision.CheckAABBvAABBCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                swingHitbox.TopLeft(),
                swingHitbox.Size());

            if (boxHit)
                return null;

            Vector2 direction = currentAngle.ToRotationVector2().SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 start = Owner.MountedCenter + direction * (26f * scale);
            Vector2 end = Owner.MountedCenter + direction * ((BalanceAegisBlade.LeftClickCoreHitboxOutset + BalanceAegisBlade.LeftClickCoreHitboxSize * 0.55f) * scale);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, BalanceAegisBlade.LeftClickCoreHitboxSize * 0.36f * scale, ref collisionPoint);
        }

        private Rectangle GetSwingDamageHitbox()
        {
            Vector2 direction = currentAngle.ToRotationVector2().SafeNormalize(Vector2.UnitX * Owner.direction);
            float hitboxSize = BalanceAegisBlade.LeftClickCoreHitboxSize * scale;
            Vector2 center = Owner.MountedCenter + direction * BalanceAegisBlade.LeftClickCoreHitboxOutset * scale;
            return new Rectangle(
                (int)(center.X - hitboxSize * 0.5f),
                (int)(center.Y - hitboxSize * 0.5f),
                (int)hitboxSize,
                (int)hitboxSize);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnBodyHitEffects(target);

            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                ModContent.ProjectileType<AegisSparkExplosion>(), Math.Max(1, (int)(Projectile.damage * 0.42f)),
                Projectile.knockBack * 0.35f, Projectile.owner);
        }

        private void SpawnBodyHitEffects(NPC target)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = currentAngle.ToRotationVector2().SafeNormalize(Vector2.UnitX * Owner.direction);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                target.Center, Vector2.Zero, BladeGold, new Vector2(1.05f, 0.62f),
                direction.ToRotation(), 0.12f, 0.82f, 14));
            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.72f) * Main.rand.NextFloat(3f, 10f);
                Dust dust = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(12f, 12f),
                    DustID.GoldFlame, velocity, 0, Main.rand.NextBool(3) ? BladeLight : BladeGold, Main.rand.NextFloat(0.8f, 1.35f));
                dust.noGravity = true;
            }
        }

        private void TrackBladeTrail(bool hitWindow)
        {
            if (!hitWindow)
            {
                slashOpacity    = MathHelper.Lerp(slashOpacity,    0f, 0.25f);
                discRingOpacity = MathHelper.Lerp(discRingOpacity, 0f, 0.18f);
                outlineBaseOpacity = MathHelper.Lerp(outlineBaseOpacity, 0f, 0.12f);
                outlinePulseOpacity = MathHelper.Lerp(outlinePulseOpacity, 0f, 0.32f);
                if (slashOpacity < 0.01f)
                    bladeTipHistoryLength = 0;
                return;
            }

            slashOpacity    = MathHelper.Lerp(slashOpacity,    1f, 0.42f);
            discRingOpacity = MathHelper.Lerp(discRingOpacity, 1f, 0.22f);
            outlineBaseOpacity = MathHelper.Clamp(outlineBaseOpacity + 0.018f, 0f, 0.58f);
            float pulseDrop = MathHelper.Lerp(0.09f, 0.24f, outlinePulseOpacity * outlinePulseOpacity);
            outlinePulseOpacity = MathHelper.Max(0f, outlinePulseOpacity - pulseDrop);
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

            float bladeRadius = BladeReach * scale * SpinSprayRadiusScale;
            // 沿刀轨道均匀分布 3 个粒子（仿 BB SpawnRightSpinParticles）
            float ringPhase = stateTimer * 0.38f * swingDirection;
            for (int i = 0; i < 7; i++)
            {
                float angle = ringPhase + MathHelper.TwoPi * i / 7f + Main.rand.NextFloat(-0.22f, 0.22f);
                Vector2 orbitDir = angle.ToRotationVector2();
                Vector2 pos = Owner.MountedCenter + orbitDir * Main.rand.NextFloat(bladeRadius * 0.42f, bladeRadius * 1.22f);
                Vector2 tangent = orbitDir.RotatedBy(MathHelper.PiOver2 * swingDirection);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    pos,
                    tangent * Main.rand.NextFloat(1.2f, 4.2f) + orbitDir * Main.rand.NextFloat(-0.25f, 0.9f),
                    false, Main.rand.Next(9, 19), Main.rand.NextFloat(0.28f, 0.72f) * SpinSpraySpriteScale,
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

            Texture2D swordTexture  = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D swooshTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge").Value;
            Texture2D bloomTexture  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ringTexture   = ModContent.Request<Texture2D>("CalamityMod/Particles/HollowCircleHardEdge").Value;
            Vector2 origin       = new(0f, swordTexture.Height);
            float drawRotation   = currentAngle + MathHelper.PiOver4;
            Vector2 drawPosition = Owner.MountedCenter - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            float swipeDir       = Math.Sign(endAngle - startAngle);
            if (swipeDir == 0f) swipeDir = 1f;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            DrawAegisSpinDisc(swordTexture, swooshTexture, ringTexture, drawPosition, origin, swipeDir);

            // ── 刀盘底层辉光环 ───────────────────────────────────────────
            if (discRingOpacity > 0.01f)
            {
                // 外环辉光（固定尺寸，覆盖轨道半径）
                Main.EntitySpriteDraw(ringTexture, drawPosition, null,
                    BladeGold with { A = 0 } * discRingOpacity * 0.45f,
                    Main.GlobalTimeWrappedHourly * swipeDir * 0.85f,
                    ringTexture.Size() * 0.5f, scale * 2.4f * SpinDiscRadiusScale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(ringTexture, drawPosition, null,
                    BladeLight with { A = 0 } * discRingOpacity * 0.28f,
                    Main.GlobalTimeWrappedHourly * swipeDir * -1.1f,
                    ringTexture.Size() * 0.5f, scale * 1.8f * SpinDiscRadiusScale, SpriteEffects.None, 0);

                // ── 20 个武器残影均匀布满一圈（仿 BB DrawMaxRightSpinWeaponAfterimages）──
                float ghostOpacity = MathHelper.Clamp(0.52f + discRingOpacity * 0.28f, 0.52f, 0.8f) * discRingOpacity;
                for (int i = 0; i < 20; i++)
                {
                    float ghostAngle = MathHelper.TwoPi * i / 20f + currentAngle;
                    float pulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 22f + i * 0.7f);
                    Vector2 jitter = Main.rand.NextVector2Circular(2.4f, 2.4f);
                    Color ghostColor = Color.Lerp(BladeGold, BladeLight, pulse) with { A = 0 };
                    Main.EntitySpriteDraw(swordTexture, drawPosition + jitter, null,
                        ghostColor * 0.16f * ghostOpacity,
                        ghostAngle + MathHelper.PiOver4,
                        origin, scale * SpinDiscSpriteScale, SpriteEffects.None, 0);
                }
            }

            // ── 挥舞扫弧残影 ─────────────────────────────────────────────
            float outlineOpacity = MathHelper.Clamp(outlineBaseOpacity + MathF.Pow(outlinePulseOpacity, 1.8f), 0f, 1f);
            if (outlineOpacity > 0.01f)
            {
                int outlineCopies = outlinePulseOpacity > 0.01f ? 16 : 8;
                float outlineRadius = MathHelper.Lerp(4.5f, 8f, outlinePulseOpacity) * scale;
                for (int i = 0; i < outlineCopies; i++)
                {
                    Vector2 outlineOffset = (MathHelper.TwoPi * i / outlineCopies).ToRotationVector2() * outlineRadius;
                    Color outlineColor = Color.Lerp(BladeGold, BladeLight, 0.35f + 0.45f * outlinePulseOpacity) with { A = 0 };
                    Main.EntitySpriteDraw(swordTexture, drawPosition + outlineOffset, null,
                        outlineColor * outlineOpacity * 0.32f,
                        drawRotation, origin, scale, SpriteEffects.None, 0);
                }
            }

            if (slashOpacity > 0.01f)
            {
                Color swooshGold = Color.Lerp(BladeGold, BladeLight, 0.28f);
                Main.EntitySpriteDraw(swooshTexture, drawPosition, null,
                    swooshGold * slashOpacity * 0.85f,
                    drawRotation + MathHelper.PiOver2 * swipeDir,
                    swooshTexture.Size() * 0.5f, scale * 1.55f * SpinDiscSpriteScale, SpriteEffects.None, 0);

                Main.EntitySpriteDraw(swooshTexture, drawPosition, null,
                    BladeLight * slashOpacity * 0.38f,
                    drawRotation + MathHelper.PiOver2 * swipeDir + 0.16f * swipeDir,
                    swooshTexture.Size() * 0.5f, scale * 1.18f * SpinDiscSpriteScale, SpriteEffects.None, 0);

                Vector2 bladeMid = Owner.MountedCenter + currentAngle.ToRotationVector2() * BladeReach * scale * SpinDiscRadiusScale * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(bloomTexture, bladeMid, null,
                    BladeLight with { A = 0 } * slashOpacity * 0.52f,
                    0f, bloomTexture.Size() * 0.5f, scale * 0.5f * SpinDiscSpriteScale, SpriteEffects.None, 0);
            }

            for (int i = 0; i < 20; i++)
            {
                Vector2 glowOffset = (MathHelper.TwoPi * i / 20f).ToRotationVector2() * 4f;
                Main.EntitySpriteDraw(swordTexture, drawPosition + glowOffset, null,
                    BladeGold with { A = 0 } * 0.18f,
                    drawRotation, origin, scale, SpriteEffects.None);
            }

            Main.spriteBatch.ExitShaderRegion();

            Main.EntitySpriteDraw(swordTexture, drawPosition, null, lightColor, drawRotation, origin, scale, SpriteEffects.None);
            return false;
        }

        private void DrawAegisSpinDisc(Texture2D swordTexture, Texture2D swooshTexture, Texture2D ringTexture, Vector2 drawPosition, Vector2 origin, float swipeDir)
        {
            float discOpacity = MathHelper.Clamp(0.68f + discRingOpacity * 0.22f, 0.68f, 0.92f) * discFadeIn;
            float spin = Main.GlobalTimeWrappedHourly * swipeDir * 5.8f;

            Main.EntitySpriteDraw(ringTexture, drawPosition, null,
                BladeGold with { A = 0 } * discOpacity * 0.62f,
                spin * 0.28f,
                ringTexture.Size() * 0.5f,
                scale * 2.9f * SpinDiscRadiusScale,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(ringTexture, drawPosition, null,
                BladeLight with { A = 0 } * discOpacity * 0.42f,
                -spin * 0.34f,
                ringTexture.Size() * 0.5f,
                scale * 2.15f * SpinDiscRadiusScale,
                SpriteEffects.None,
                0);

            for (int i = 0; i < 4; i++)
            {
                float rotation = spin + MathHelper.TwoPi * i / 4f;
                Color smearColor = Color.Lerp(BladeGold, BladeLight, i % 2 == 0 ? 0.2f : 0.55f);
                Main.EntitySpriteDraw(swooshTexture, drawPosition, null,
                    smearColor * discOpacity * 0.72f,
                    rotation,
                    swooshTexture.Size() * 0.5f,
                    scale * 1.9f * SpinDiscSpriteScale,
                    SpriteEffects.None,
                    0);
            }

            for (int i = 0; i < 32; i++)
            {
                float progress = i / 32f;
                float ghostAngle = currentAngle + MathHelper.TwoPi * progress + spin * 0.18f;
                float pulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 24f + i * 0.72f);
                Vector2 jitter = Main.rand.NextVector2Circular(2.8f, 2.8f);
                Color ghostColor = Color.Lerp(BladeGold, BladeLight, pulse) with { A = 0 };

                Main.EntitySpriteDraw(swordTexture, drawPosition + jitter, null,
                    ghostColor * discOpacity * 0.24f,
                    ghostAngle + MathHelper.PiOver4,
                    origin,
                    scale * SpinDiscSpriteScale,
                    SpriteEffects.None,
                    0);
            }
        }
    }
}
