using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.TheEnforcer
{
    internal class TheNewEnforcerHoldOut : BaseCustomUseStyleProjectile, ILocalizedModType
    {
        private const float FlameDamageFactor = 0.42f;
        private const float SlashDamageFactor = 0.34f;
        private const float RiftSeedDamageFactor = 0.32f;
        private const float FlameTargetRange = 4400f;
        private const float lineCollisionLength = 226f;

        private Vector2 mousePos;
        private Vector2 aimVel;
        private bool doSwing = true;
        private bool postSwing;
        private bool firedFlames;
        private bool swingSound = true;
        private float fadeIn;
        private float baseScale;
        private int useAnim;
        private int swingCount;

        public new string LocalizationCategory => "Projectiles.TheEnforcer";
        public override int AssignedItemID => ModContent.ItemType<TheNewEnforcer>();
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Olds/TheEnforcer/TheNewEnforcer";
        public override float HitboxOutset => 125f;
        public override Vector2 HitboxSize => new(190f, 190f);
        public override float HitboxRotationOffset => MathHelper.ToRadians(-45f);
        public override Vector2 SpriteOrigin => new(0f, 100f);

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.noEnchantmentVisuals = true;
            Projectile.scale = 1.35f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void WhenSpawned()
        {
            Projectile.timeLeft = Owner.HeldItem.useAnimation + 1;
            Projectile.ai[1] = -1f;
            useAnim = Math.Max(1, Owner.itemAnimationMax);
            baseScale = Projectile.scale;
            mousePos = Owner.Calamity().mouseWorld;
            aimVel = (Owner.Center - mousePos).SafeNormalize(Vector2.UnitX) * 65f;
            FacePoint(mousePos);
            FlipAsSword = Owner.direction == -1;
            doSwing = true;
            postSwing = false;
            firedFlames = false;
            swingSound = true;
            fadeIn = 0f;
            swingCount = 0;
        }

        public override void UseStyle()
        {
            Player owner = Owner;
            AnimationProgress = Animation % useAnim;
            DrawUnconditionally = false;

            mousePos = CanHit || postSwing ? owner.Center - aimVel : owner.Calamity().mouseWorld;
            fadeIn = MathHelper.Lerp(fadeIn, CanHit ? 1f : 0f, CanHit ? 0.3f : 0.35f);

            if (!doSwing)
            {
                ResetSwing(owner);
            }
            else
            {
                if (!CanHit && !postSwing)
                    FacePoint(mousePos);
                else
                    FacePoint(owner.Center - aimVel);

                Projectile.rotation = Utils.AngleLerp(Projectile.rotation, owner.AngleTo(mousePos) + MathHelper.PiOver4, 0.1f);

                if (AnimationProgress < useAnim / 1.5f)
                    DoGrandDadWindup(owner);
                else
                    DoGrandDadSwing(owner);
            }

            ArmRotationOffset = MathHelper.ToRadians(-140f);
            ArmRotationOffsetBack = MathHelper.ToRadians(-140f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.42f, 0.18f, 0.74f) * Projectile.Opacity);
        }

        private void ResetSwing(Player owner)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
                Projectile.localNPCImmunity[i] = 0;

            Projectile.numHits = 0;
            mousePos = owner.Calamity().mouseWorld;
            aimVel = (owner.Center - mousePos).SafeNormalize(Vector2.UnitX) * 65f;
            CanHit = false;
            postSwing = false;
            FacePoint(mousePos);
            FlipAsSword = owner.direction == -1;
            doSwing = true;
            firedFlames = false;
            swingSound = true;
            swingCount++;
        }

        private void DoGrandDadWindup(Player owner)
        {
            aimVel = (owner.Center - owner.Calamity().mouseWorld).SafeNormalize(Vector2.UnitX) * 65f;
            CanHit = false;
            postSwing = false;

            if (AnimationProgress == 0f)
            {
                doSwing = false;
                Projectile.ai[1] = -Projectile.ai[1];
            }

            float windupOverpull = 1f + Utils.GetLerpValue(useAnim * 0.8f, useAnim, Animation, true) * 0.35f;
            float targetOffset = 120f * Projectile.ai[1] * owner.direction * windupOverpull;
            RotationOffset = MathHelper.Lerp(RotationOffset, MathHelper.ToRadians(targetOffset), 0.2f);
            Projectile.scale = MathHelper.Lerp(Projectile.scale, baseScale * 1.02f, 0.14f);
        }

        private void DoGrandDadSwing(Player owner)
        {
            FlipAsSword = owner.direction < 0;

            float swingStart = useAnim / 3f;
            float swingTimer = AnimationProgress - swingStart;
            float swingDuration = useAnim - swingStart;
            float swingCompletion = MathHelper.Clamp(swingTimer / swingDuration, 0f, 1f);
            float bell = MathF.Sin(swingCompletion * MathHelper.Pi);

            if (swingTimer >= (int)(swingDuration * 0.4f) && swingSound)
            {
                SoundEngine.PlaySound(SoundID.Item71 with
                {
                    Volume = 0.82f,
                    Pitch = -0.12f,
                    PitchVariance = 0.08f
                }, Projectile.Center);

                FireEssenceVolley(owner);
                firedFlames = true;
                swingSound = false;
            }

            if (swingTimer > (int)(swingDuration * 0.4f) && swingTimer < (int)(swingDuration * 0.7f))
            {
                CanHit = true;
                Projectile.scale = baseScale * MathHelper.Lerp(1.02f, 1.36f, bell);
                SpawnSwingVFX(owner, bell);
            }
            else
            {
                CanHit = false;
                Projectile.scale = MathHelper.Lerp(Projectile.scale, baseScale * 0.94f, 0.16f);
            }

            float startAngle = 150f * Projectile.ai[1] * owner.direction;
            float endAngle = 120f * -Projectile.ai[1] * owner.direction;
            float easedSwing = CalamityUtils.ExpInOutEasing(swingCompletion, 1);
            RotationOffset = MathHelper.Lerp(RotationOffset, MathHelper.ToRadians(MathHelper.Lerp(startAngle, endAngle, easedSwing)), 0.2f);

            if (swingTimer >= swingDuration)
                doSwing = false;

            if (swingTimer < (int)(swingDuration * 0.7f))
                postSwing = true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Player owner = Main.player[Projectile.owner];
            Vector2 bladeStart = Vector2.Lerp(owner.MountedCenter, Projectile.Center, 0.16f);
            Vector2 bladeEnd = GetBladeTip(owner);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                bladeStart,
                bladeEnd,
                36f * Projectile.scale,
                ref collisionPoint);
        }

        private void FacePoint(Vector2 point)
        {
            Owner.direction = point.X < Owner.Center.X ? -1 : 1;
        }

        private void FireEssenceVolley(Player owner)
        {
            if (Main.myPlayer != Projectile.owner || firedFlames)
                return;

            Vector2 bladeDirection = GetBladeDirection(owner);
            Vector2 bladeTip = GetBladeTip(owner);
            Vector2 bladeBase = Vector2.Lerp(owner.MountedCenter, Projectile.Center, 0.45f);
            Vector2 normal = bladeDirection.RotatedBy(MathHelper.PiOver2);
            NPC target = FindBestTarget(bladeTip, bladeDirection, FlameTargetRange);
            const int flameCount = 7;

            for (int i = 0; i < flameCount; i++)
            {
                float centeredIndex = i - (flameCount - 1) * 0.5f;
                float side = centeredIndex == 0f ? Main.rand.NextBool().ToDirectionInt() : Math.Sign(centeredIndex);
                float bladeCompletion = MathHelper.Clamp(0.48f + i * 0.085f + Main.rand.NextFloat(-0.05f, 0.05f), 0.24f, 1.05f);
                float sideOffset = centeredIndex * Main.rand.NextFloat(12f, 20f);
                Vector2 spawnPosition = Vector2.Lerp(bladeBase, bladeTip, bladeCompletion);
                spawnPosition += normal * sideOffset;
                spawnPosition -= bladeDirection * Main.rand.NextFloat(6f, 34f);
                spawnPosition += Main.rand.NextVector2Circular(9f, 9f);

                float openingSpread = centeredIndex * 0.115f + Main.rand.NextFloat(-0.08f, 0.08f);
                Vector2 openingDirection = bladeDirection.RotatedBy(openingSpread);
                Vector2 tangentDirection = normal * side;
                openingDirection = Vector2.Lerp(openingDirection, tangentDirection, 0.18f + Math.Abs(centeredIndex) * 0.035f).SafeNormalize(bladeDirection);

                if (target is not null)
                {
                    float openingSpeed = 12.5f + i * 0.75f;
                    Vector2 predictedTarget = PredictTargetPosition(target, spawnPosition, openingSpeed);
                    Vector2 targetDirection = (predictedTarget - spawnPosition).SafeNormalize(bladeDirection);
                    float targetInfluence = MathHelper.Clamp(0.36f + 0.04f * i, 0.34f, 0.62f);
                    openingDirection = Vector2.Lerp(openingDirection, targetDirection.RotatedBy(openingSpread * 0.35f), targetInfluence).SafeNormalize(targetDirection);
                }

                Vector2 flameVelocity = openingDirection * Main.rand.NextFloat(11.5f, 16.5f);
                int profile = Main.rand.Next(4);
                float flameProfile = i + profile * 10f + Main.rand.NextFloat(0.01f, 0.99f);

                int flame = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    flameVelocity,
                    ModContent.ProjectileType<NewEssenceFlame2>(),
                    Math.Max(1, (int)(Projectile.damage * FlameDamageFactor)),
                    Projectile.knockBack * 0.42f,
                    Projectile.owner,
                    target?.whoAmI ?? -1f,
                    flameProfile);

                if (Main.projectile.IndexInRange(flame))
                {
                    Main.projectile[flame].scale = Main.rand.NextFloat(0.92f, 1.16f);
                    Main.projectile[flame].netUpdate = true;
                }

                SpawnFlameReleaseVFX(spawnPosition, flameVelocity, i);
            }

            FireRiftSeed(owner, bladeTip, bladeDirection, target);
            SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.55f, Pitch = 0.16f }, bladeTip);
        }

        private void FireRiftSeed(Player owner, Vector2 bladeTip, Vector2 bladeDirection, NPC target)
        {
            Vector2 spawnPosition = bladeTip + bladeDirection * 18f + Main.rand.NextVector2Circular(8f, 8f);
            Vector2 seedDirection = bladeDirection;

            if (target is not null)
            {
                Vector2 predictedTarget = PredictTargetPosition(target, spawnPosition, 18f);
                seedDirection = Vector2.Lerp(
                    bladeDirection,
                    (predictedTarget - spawnPosition).SafeNormalize(bladeDirection),
                    0.64f).SafeNormalize(bladeDirection);
            }

            Vector2 seedVelocity = seedDirection.RotatedBy(Main.rand.NextFloat(-0.08f, 0.08f)) * Main.rand.NextFloat(15f, 18.5f);
            int seed = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                seedVelocity,
                ModContent.ProjectileType<TheNewEnforcerRiftSeed>(),
                Math.Max(1, (int)(Projectile.damage * RiftSeedDamageFactor)),
                Projectile.knockBack * 0.72f,
                Projectile.owner,
                target?.whoAmI ?? -1f,
                swingCount % 3);

            if (Main.projectile.IndexInRange(seed))
            {
                Main.projectile[seed].scale = Main.rand.NextFloat(0.94f, 1.12f);
                Main.projectile[seed].netUpdate = true;
            }

            SpawnRiftReleaseVFX(spawnPosition, seedVelocity, swingCount);
        }

        private void SpawnSwingVFX(Player owner, float intensity)
        {
            if (Main.dedServ || Animation % Projectile.MaxUpdates != 0f)
                return;

            Vector2 bladeDirection = GetBladeDirection(owner);
            Vector2 bladeTip = GetBladeTip(owner);
            Vector2 normal = bladeDirection.RotatedBy(MathHelper.PiOver2);
            Color violet = new(125, 70, 255);
            Color cyan = new(80, 225, 255);
            Color fuchsia = new(255, 48, 210);

            for (int i = 0; i < 2; i++)
            {
                Vector2 position = Vector2.Lerp(owner.MountedCenter, bladeTip, Main.rand.NextFloat(0.3f, 1f)) + normal * Main.rand.NextFloat(-18f, 18f);
                Vector2 velocity = -bladeDirection.RotatedByRandom(0.34f) * Main.rand.NextFloat(1.8f, 4.8f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    position,
                    velocity,
                    false,
                    Main.rand.Next(12, 19),
                    Main.rand.NextFloat(0.5f, 0.95f) * Projectile.scale,
                    Main.rand.NextBool(3) ? cyan : (Main.rand.NextBool() ? fuchsia : violet)));
            }

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    bladeTip + Main.rand.NextVector2Circular(10f, 10f),
                    -bladeDirection * Main.rand.NextFloat(1.2f, 3.4f),
                    "CalamityMod/Particles/VerticalSmearLarge",
                    false,
                    Main.rand.Next(10, 15),
                    Main.rand.NextFloat(0.035f, 0.06f) * Projectile.scale,
                    Color.Lerp(fuchsia, cyan, Main.rand.NextFloat(0.18f, 0.72f)) * (0.58f + intensity * 0.36f),
                    new Vector2(0.85f, 1.55f),
                    true));
            }

            Dust dust = Dust.NewDustPerfect(
                bladeTip + Main.rand.NextVector2Circular(18f, 18f),
                Main.rand.NextBool() ? DustID.Shadowflame : DustID.BlueTorch,
                -bladeDirection.RotatedByRandom(0.45f) * Main.rand.NextFloat(1.5f, 5f),
                100,
                Color.Lerp(violet, cyan, Main.rand.NextFloat()),
                Main.rand.NextFloat(0.9f, 1.35f));
            dust.noGravity = true;
        }

        private void SpawnExecutionSlashes(NPC target)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            const int slashCount = 5;
            for (int i = 0; i < slashCount; i++)
            {
                float angleOffset = MathHelper.TwoPi * i / slashCount + Main.rand.NextFloat(-0.26f, 0.26f);
                Vector2 direction = angleOffset.ToRotationVector2();
                Vector2 spawnPosition = target.Center - direction * Main.rand.NextFloat(24f, 62f) + Main.rand.NextVector2Circular(18f, 18f);

                Projectile.NewProjectile(
                    Projectile.GetSource_OnHit(target),
                    spawnPosition,
                    direction * 0.01f,
                    ModContent.ProjectileType<TheNewEnforcerSlash>(),
                    Math.Max(1, (int)(Projectile.damage * SlashDamageFactor)),
                    Projectile.knockBack * 0.28f,
                    Projectile.owner,
                    Main.rand.NextFloat(1.35f, 2.2f),
                    angleOffset + MathHelper.PiOver2);
            }

            SoundEngine.PlaySound(SoundID.Item84 with { Volume = 0.48f, Pitch = 0.22f }, target.Center);
        }

        private Vector2 GetBladeDirection(Player owner) =>
            (FinalRotation + MathHelper.ToRadians(-45f)).ToRotationVector2();

        private Vector2 GetBladeTip(Player owner) =>
            Projectile.Center + GetBladeDirection(owner) * lineCollisionLength * 0.48f * Projectile.scale;

        private static NPC FindBestTarget(Vector2 origin, Vector2 aimDirection, float range)
        {
            NPC bestTarget = null;
            float bestScore = range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                Vector2 toTarget = npc.Center - origin;
                float distance = toTarget.Length();
                if (distance > range)
                    continue;

                float anglePenalty = 1f - MathHelper.Clamp(Vector2.Dot(aimDirection, toTarget.SafeNormalize(aimDirection)), -1f, 1f);
                float score = distance + anglePenalty * 340f;
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestTarget = npc;
            }

            return bestTarget;
        }

        private static Vector2 PredictTargetPosition(NPC target, Vector2 origin, float speed)
        {
            float distance = Vector2.Distance(origin, target.Center);
            float travelTime = MathHelper.Clamp(distance / Math.Max(speed, 1f), 8f, 42f);
            return target.Center + target.velocity * travelTime * 0.55f;
        }

        private static void SpawnFlameReleaseVFX(Vector2 position, Vector2 velocity, int index)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            Color violet = new(126, 58, 255);
            Color cyan = new(72, 230, 255);
            Color fuchsia = new(255, 48, 210);
            Color color = Color.Lerp(Color.Lerp(fuchsia, violet, 0.24f), cyan, index / 6f);

            Dust dust = Dust.NewDustPerfect(
                position,
                Main.rand.NextBool() ? DustID.Shadowflame : DustID.BlueTorch,
                -direction * Main.rand.NextFloat(0.6f, 1.8f) + Main.rand.NextVector2Circular(0.8f, 0.8f),
                100,
                color,
                Main.rand.NextFloat(0.8f, 1.25f));
            dust.noGravity = true;

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    position - direction * 4f,
                    -direction * Main.rand.NextFloat(1.2f, 3f),
                    false,
                    Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.48f, 0.78f),
                    color));
            }
        }

        private static void SpawnRiftReleaseVFX(Vector2 position, Vector2 velocity, int swingIndex)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            Color cyan = new(72, 230, 255);
            Color fuchsia = new(255, 48, 210);
            Color twilight = new(126, 58, 255);
            Color theme = Color.Lerp(fuchsia, cyan, swingIndex % 3 / 2f);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                position,
                Vector2.Zero,
                Color.Lerp(theme, twilight, 0.24f),
                Vector2.One,
                direction.ToRotation(),
                0.08f,
                1.18f,
                16));

            for (int i = 0; i < 20; i++)
            {
                Vector2 outward = direction.RotatedByRandom(0.9f) * Main.rand.NextFloat(1.8f, 5.2f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    position + Main.rand.NextVector2Circular(10f, 10f),
                    outward,
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.45f, 0.82f),
                    Main.rand.NextBool() ? cyan : fuchsia));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
            SpawnExecutionSlashes(target);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if ((useAnim <= 0 && !DrawUnconditionally) || !Owner.ItemAnimationActive)
                return false;

            Asset<Texture2D> texture = ModContent.Request<Texture2D>(Texture);
            Texture2D blade = texture.Value;
            float flipRotation = FlipAsSword ? MathHelper.PiOver2 : 0f;
            SpriteEffects effects = FlipAsSword ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 origin = FlipAsSword ? new Vector2(blade.Width - SpriteOrigin.X, SpriteOrigin.Y) : SpriteOrigin;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);

            if (fadeIn > 0.02f)
            {
                Asset<Texture2D> smear = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge");
                Color smearColor = Color.Lerp(new Color(115, 65, 255), new Color(60, 230, 255), 0.25f);
                smearColor.A = 0;
                Main.EntitySpriteDraw(
                    smear.Value,
                    drawPosition,
                    null,
                    smearColor * fadeIn * 0.48f,
                    FinalRotation + MathHelper.PiOver4 + MathHelper.ToRadians(swingCount % 2 == 0 ? -70f : 70f) * -Owner.direction,
                    smear.Value.Size() * 0.5f,
                    Projectile.scale * 0.46f,
                    effects,
                    0f);
            }

            for (int i = 0; i < 10; i++)
            {
                Color ghostColor = new(130, 95, 255);
                ghostColor.A = 0;
                Vector2 ghostOffset = MathHelper.TwoPi.ToRotationVector2().RotatedBy(MathHelper.TwoPi * i / 10f) * 4f * fadeIn;
                Main.EntitySpriteDraw(
                    blade,
                    drawPosition + ghostOffset,
                    null,
                    ghostColor * 0.08f * fadeIn,
                    Projectile.rotation + RotationOffset + flipRotation,
                    origin,
                    Projectile.scale,
                    effects,
                    0f);
            }

            Main.EntitySpriteDraw(
                blade,
                drawPosition,
                null,
                lightColor,
                Projectile.rotation + RotationOffset + flipRotation,
                origin,
                Projectile.scale,
                effects,
                0f);

            return false;
        }

        public override void PostDraw(Color lightColor)
        {
            if (Main.dedServ)
                return;

            Player owner = Main.player[Projectile.owner];
            Vector2 bladeDirection = GetBladeDirection(owner);
            Vector2 drawPosition = GetBladeTip(owner) - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Asset<Texture2D> bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Asset<Texture2D> streak = ModContent.Request<Texture2D>("CalamityMod/Particles/FadeStreak");
            float pulse = 0.72f + 0.18f * MathF.Sin(Main.GlobalTimeWrappedHourly * 12f + Projectile.identity);
            Color color = Color.Lerp(new Color(115, 65, 255), new Color(60, 230, 255), 0.25f);
            color.A = 0;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                bloom.Value,
                drawPosition,
                null,
                color * 0.42f,
                0f,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.34f * pulse,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                streak.Value,
                drawPosition,
                null,
                Color.White with { A = 0 } * 0.34f,
                bladeDirection.ToRotation(),
                new Vector2(streak.Width() * 0.5f, streak.Height() * 0.5f),
                new Vector2(0.74f, 0.18f) * Projectile.scale,
                SpriteEffects.None,
                0f);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }
    }
}
