using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Enums;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.TheEnforcer
{
    internal static class TheNewEnforcerDogPalette
    {
        internal static readonly Color Cyan = new(72, 230, 255);
        internal static readonly Color Fuchsia = new(255, 48, 210);
        internal static readonly Color Twilight = new(126, 58, 255);

        internal static Color SpecialMoveColor(float offset = 0f)
        {
            float pulse = MathF.Sin(Main.GlobalTimeWrappedHourly * 4.6f + offset) * 0.5f + 0.5f;
            Color color = Color.Lerp(Fuchsia, Cyan, 0.16f + pulse * 0.72f);
            color = Color.Lerp(Twilight, color, 0.76f);
            color.A = 0;
            return color;
        }

        internal static Color RiftColor(float offset = 0f)
        {
            Color color = SpecialMoveColor(offset);
            return Color.Lerp(color, Color.White, 0.12f);
        }
    }

    internal static class TheNewEnforcerMagicCoreDraw
    {
        internal static void Draw(Vector2 drawPosition, float rotation, float visualScale, float opacity, Color primaryColor, int identity)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D magicRing = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_03").Value;
            Texture2D reticle = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_04").Value;
            Texture2D runeStar = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/star_05").Value;
            Texture2D perfectGlow = TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;

            float time = Main.GlobalTimeWrappedHourly;
            float pulse = 0.92f + 0.08f * MathF.Sin(time * 12f + identity * 0.41f);
            float spin = time * 1.7f + identity * 0.23f;
            Color primary = primaryColor with { A = 0 };
            Color cyan = Color.Lerp(primary, TheNewEnforcerDogPalette.Cyan, 0.52f) with { A = 0 };
            Color fuchsia = Color.Lerp(primary, TheNewEnforcerDogPalette.Fuchsia, 0.46f) with { A = 0 };
            Color white = Color.Lerp(primary, Color.White, 0.72f) with { A = 0 };

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                primary * 0.58f * opacity,
                0f,
                bloom.Size() * 0.5f,
                0.2f * visualScale * pulse,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                magicRing,
                drawPosition,
                null,
                cyan * 0.34f * opacity,
                rotation * 0.2f + spin,
                magicRing.Size() * 0.5f,
                0.058f * visualScale * pulse,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                reticle,
                drawPosition,
                null,
                fuchsia * 0.3f * opacity,
                -rotation * 0.16f - spin * 0.78f,
                reticle.Size() * 0.5f,
                0.052f * visualScale,
                SpriteEffects.FlipHorizontally,
                0f);

            Main.EntitySpriteDraw(
                runeStar,
                drawPosition,
                null,
                Color.Lerp(cyan, fuchsia, 0.5f) * 0.28f * opacity,
                rotation + spin * 0.34f,
                runeStar.Size() * 0.5f,
                0.052f * visualScale * pulse,
                SpriteEffects.None,
                0f);

            for (int i = 0; i < 3; i++)
            {
                float layerRotation = rotation + spin * (i % 2 == 0 ? 0.62f : -0.54f) + MathHelper.TwoPi * i / 3f;
                Color layerColor = i == 0 ? white : Color.Lerp(cyan, fuchsia, i * 0.5f);
                Main.EntitySpriteDraw(
                    perfectGlow,
                    drawPosition,
                    null,
                    layerColor * (0.5f - i * 0.09f) * opacity,
                    layerRotation,
                    perfectGlow.Size() * 0.5f,
                    (0.2f + i * 0.045f) * visualScale * pulse,
                    SpriteEffects.None,
                    0f);
            }
        }
    }

    internal sealed class TheNewEnforcerRiftSeed : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        private const int Lifetime = 150;
        private const float HomingRange = 3600f;
        private const float RetentionRange = 5200f;

        private ref float TargetIndex => ref Projectile.ai[0];
        private ref float Variant => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private bool Detonated
        {
            get => Projectile.localAI[1] == 1f;
            set => Projectile.localAI[1] = value ? 1f : 0f;
        }

        private float Fade =>
            Utils.GetLerpValue(0f, 10f, Timer, true) *
            Utils.GetLerpValue(0f, 22f, Projectile.timeLeft, true);

        public new string LocalizationCategory => "Projectiles.TheEnforcer";
        public override string Texture => "CalamityMod/Projectiles/Healing/EssenceFlame";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 38;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.Opacity = 0f;
            SpawnSeedOpenVFX();
        }

        public override bool? CanHitNPC(NPC target) =>
            Timer > 12f && target.CanBeChasedBy(Projectile);

        public override void AI()
        {
            Timer++;
            Projectile.alpha = Math.Max(0, Projectile.alpha - 18);
            Projectile.Opacity = Fade;

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 5)
            {
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
                Projectile.frameCounter = 0;
            }

            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float currentSpeed = MathHelper.Clamp(Projectile.velocity.Length(), 10f, 30f);
            NPC target = ResolveTarget(currentDirection);

            if (target is not null)
            {
                float distance = Projectile.Distance(target.Center);
                float lockIn = Utils.GetLerpValue(8f, 48f, Timer, true);
                float terminal = Utils.GetLerpValue(420f, 78f, distance, true);
                float farBoost = Utils.GetLerpValue(900f, 2500f, distance, true);
                float leadTime = MathHelper.Clamp(distance / Math.Max(currentSpeed, 1f), 8f, 44f);
                Vector2 aimPoint = target.Center + target.velocity * leadTime * MathHelper.Lerp(0.38f, 0.82f, lockIn);
                Vector2 desiredDirection = (aimPoint - Projectile.Center).SafeNormalize(currentDirection);
                float side = ((int)Variant % 2 == 0) ? 1f : -1f;
                Vector2 tangent = desiredDirection.RotatedBy(MathHelper.PiOver2 * side);
                float swirl = MathF.Sin(Timer * 0.12f + Projectile.identity * 0.37f + Variant * 1.13f) * MathHelper.Lerp(0.32f, 0.07f, lockIn) * (1f - terminal * 0.75f);
                Vector2 curvedDirection = (desiredDirection + tangent * swirl).SafeNormalize(desiredDirection);
                float targetSpeed = MathHelper.Lerp(14f, 28f + farBoost * 7f, lockIn) + terminal * 5f;
                float turnStrength = MathHelper.Lerp(0.045f, 0.2f, lockIn) + terminal * 0.08f + farBoost * 0.04f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, curvedDirection * targetSpeed, turnStrength);
                TargetIndex = target.whoAmI;
            }
            else
            {
                float drift = MathF.Sin(Timer * 0.075f + Variant * 0.91f) * 0.035f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, currentDirection.RotatedBy(drift) * MathHelper.Clamp(currentSpeed + 0.08f, 12f, 26f), 0.06f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, TheNewEnforcerDogPalette.RiftColor(Timer * 0.04f).ToVector3() * Projectile.Opacity * 0.72f);
            SpawnFlightVFX();
        }

        private NPC ResolveTarget(Vector2 currentDirection)
        {
            int targetIndex = (int)TargetIndex;
            if (targetIndex >= 0 && targetIndex < Main.maxNPCs)
            {
                NPC lockedTarget = Main.npc[targetIndex];
                if (lockedTarget.active && lockedTarget.CanBeChasedBy(Projectile) && Projectile.Distance(lockedTarget.Center) <= RetentionRange)
                    return lockedTarget;
            }

            NPC bestTarget = null;
            float bestScore = float.MaxValue;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance > HomingRange)
                    continue;

                Vector2 toTarget = (npc.Center - Projectile.Center).SafeNormalize(currentDirection);
                float forwardness = Vector2.Dot(currentDirection, toTarget);
                float score = distance + (1f - forwardness) * 300f;
                if (npc.whoAmI == Main.player[Projectile.owner].MinionAttackTargetNPC)
                    score *= 0.72f;

                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestTarget = npc;
            }

            return bestTarget;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 210);
            Detonate(target.Center);
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (!Detonated && Timer > 18f)
                Detonate(Projectile.Center);
        }

        private void Detonate(Vector2 center)
        {
            if (Detonated)
                return;

            Detonated = true;

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    center,
                    Vector2.Zero,
                    ModContent.ProjectileType<TheNewEnforcerRiftField>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    360f,
                    Variant);
            }

            SpawnDetonationVFX(center);
            ApplyScreenShake(center, 3.5f);
        }

        private void SpawnSeedOpenVFX()
        {
            if (Main.dedServ)
                return;

            Color color = TheNewEnforcerDogPalette.RiftColor(Variant);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                color,
                Vector2.One,
                Projectile.velocity.ToRotation(),
                0.08f,
                0.94f,
                14));
        }

        private void SpawnFlightVFX()
        {
            if (Main.dedServ || Timer % 2f != 0f)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Color color = TheNewEnforcerDogPalette.SpecialMoveColor(Timer * 0.04f + Variant);

            if (Main.rand.NextBool(2))
            {
                for (int i = 0; i < 2; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        Projectile.Center - direction * Main.rand.NextFloat(8f, 20f) + normal * Main.rand.NextFloat(-6f, 6f),
                        -direction * Main.rand.NextFloat(0.8f, 2.4f) + normal * Main.rand.NextFloat(-0.5f, 0.5f),
                        false,
                        Main.rand.Next(12, 18),
                        Main.rand.NextFloat(0.055f, 0.09f) * Projectile.scale,
                        color,
                        new Vector2(2.6f, 0.55f),
                        true));
                }
            }

            if (Main.rand.NextBool(3))
            {
                for (int i = 0; i < 2; i++)
                {
                    Particle light = new SquishyLightParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                        -direction * Main.rand.NextFloat(0.3f, 1.1f),
                        Main.rand.NextFloat(0.34f, 0.58f) * Projectile.scale,
                        Color.Lerp(color, Color.White, 0.18f),
                        Main.rand.Next(14, 22));

                    GeneralParticleHandler.SpawnParticle(light);
                }
            }

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center - direction * 8f + normal * Main.rand.NextFloat(-5f, 5f),
                Main.rand.NextBool() ? DustID.Shadowflame : DustID.BlueTorch,
                -direction * Main.rand.NextFloat(0.5f, 1.8f),
                100,
                Color.Lerp(color, Color.White, 0.18f),
                Main.rand.NextFloat(0.75f, 1.05f) * Projectile.scale);
            dust.noGravity = true;
        }

        private static void SpawnDetonationVFX(Vector2 center)
        {
            if (Main.dedServ)
                return;

            Color cyan = TheNewEnforcerDogPalette.Cyan;
            Color fuchsia = TheNewEnforcerDogPalette.Fuchsia;

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.62f, Pitch = -0.08f, PitchVariance = 0.08f }, center);

            for (int i = 0; i < 52; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 8.4f);
                Color color = Main.rand.NextBool() ? cyan : fuchsia;
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    center + Main.rand.NextVector2Circular(16f, 16f),
                    velocity,
                    false,
                    Main.rand.Next(16, 28),
                    Main.rand.NextFloat(0.54f, 1.05f),
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.04f, 0.28f))));
            }

            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 5.4f);
                Color color = Main.rand.NextBool() ? cyan : fuchsia;
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    center + Main.rand.NextVector2Circular(12f, 12f),
                    velocity,
                    Main.rand.NextFloat(0.34f, 0.72f),
                    Color.Lerp(color, Color.White, 0.18f),
                    Main.rand.Next(14, 24)));
            }

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                Color.Lerp(fuchsia, cyan, 0.5f) * 0.62f,
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One,
                Main.rand.NextFloat(-0.35f, 0.35f),
                0.06f,
                0.44f,
                18,
                true));

            for (int i = 0; i < 10; i++)
            {
                float rot = MathHelper.TwoPi * i / 10f;
                Vector2 vel = new Vector2(5f, 0f).RotatedBy(rot) * Main.rand.NextFloat(0.8f, 1.4f);
                GalaxyMetaball.SpawnParticle(center + Main.rand.NextVector2Circular(14f, 14f), vel, 44f * Main.rand.NextFloat(0.8f, 1.25f));
            }
        }

        private static void ApplyScreenShake(Vector2 center, float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(2200f, 260f, Main.LocalPlayer.Distance(center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color color = TheNewEnforcerDogPalette.SpecialMoveColor(Timer * 0.04f + Variant);
            float pulse = 0.9f + 0.1f * MathF.Sin(Main.GlobalTimeWrappedHourly * 13f + Projectile.identity);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                color * 0.58f * Fade,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.34f * pulse,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                ring,
                drawPosition,
                null,
                Color.Lerp(color, Color.White, 0.16f) * 0.46f * Fade,
                -Projectile.rotation + Timer * 0.06f,
                ring.Size() * 0.5f,
                Projectile.scale * 0.2f,
                SpriteEffects.None,
                0f);

            TheNewEnforcerMagicCoreDraw.Draw(
                drawPosition,
                Projectile.rotation,
                Projectile.scale * 1.7f,
                Fade,
                color,
                Projectile.identity);

            Main.EntitySpriteDraw(
                texture,
                drawPosition + direction * 2f,
                frame,
                Color.Lerp(color, Color.White, 0.38f) * Fade,
                Projectile.rotation,
                frame.Size() * 0.5f,
                Projectile.scale * 1.26f,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] trailPoints = Projectile.oldPos
                .Where(position => position != Vector2.Zero)
                .Select(position => position + Projectile.Size * 0.5f)
                .ToArray();

            if (trailPoints.Length == 0)
                trailPoints = new[] { Projectile.Center - Projectile.velocity, Projectile.Center };
            else if (trailPoints[0] != Projectile.Center)
                trailPoints = new[] { Projectile.Center }.Concat(trailPoints).ToArray();

            if (trailPoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    TrailWidth,
                    TrailColor,
                    (_, _) => Vector2.Zero,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                trailPoints.Length * 3);

            Vector2[] corePoints = trailPoints.Take(Math.Min(16, trailPoints.Length)).ToArray();
            if (corePoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                corePoints,
                new PrimitiveSettings(
                    CoreTrailWidth,
                    CoreTrailColor,
                    (_, _) => Vector2.Zero,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                corePoints.Length * 3);
        }

        private float TrailWidth(float completion, Vector2 _) =>
            Utils.Remap(completion, 0f, 0.86f, Projectile.scale * 56f, 0f) * Projectile.Opacity;

        private Color TrailColor(float completion, Vector2 _)
        {
            Color head = Color.Lerp(Color.White, TheNewEnforcerDogPalette.SpecialMoveColor(Variant + completion * 0.4f), 0.42f);
            Color tail = Color.Lerp(TheNewEnforcerDogPalette.Twilight, Color.Transparent, Utils.GetLerpValue(0.56f, 1f, completion, true));
            head.A = 0;
            tail.A = 0;
            return Color.Lerp(head, tail, completion) * Projectile.Opacity;
        }

        private float CoreTrailWidth(float completion, Vector2 _) =>
            Utils.Remap(completion, 0f, 0.78f, Projectile.scale * 18f, 0f) * Projectile.Opacity;

        private Color CoreTrailColor(float completion, Vector2 _)
        {
            Color color = Color.Lerp(Color.White, TheNewEnforcerDogPalette.Cyan, 0.28f);
            color.A = 0;
            return color * (1f - Utils.GetLerpValue(0.72f, 1f, completion, true)) * Projectile.Opacity;
        }
    }

    internal sealed class TheNewEnforcerRiftField : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 74;
        private const int BurstTime = 24;

        private ref float Timer => ref Projectile.localAI[0];
        private float Radius => Projectile.ai[0] <= 0f ? 360f : Projectile.ai[0];
        private int Pattern => PositiveModulo((int)MathF.Floor(Projectile.ai[1]), 3);
        private float Fade =>
            Utils.GetLerpValue(0f, 12f, Timer, true) *
            Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);

        public new string LocalizationCategory => "Projectiles.TheEnforcer";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4200;
        }

        public override void SetDefaults()
        {
            Projectile.width = 720;
            Projectile.height = 720;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
            Projectile.alpha = 255;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(IEntitySource source)
        {
            Vector2 center = Projectile.Center;
            int size = (int)(Radius * 2f);
            Projectile.width = size;
            Projectile.height = size;
            Projectile.Center = center;
            SpawnOpeningVFX();
        }

        public override bool? CanDamage() =>
            Timer >= BurstTime - 2f && Timer <= Lifetime - 10f ? null : false;

        public override void AI()
        {
            Timer++;
            Projectile.Opacity = Fade;
            Projectile.rotation += 0.035f + Pattern * 0.006f;
            Lighting.AddLight(Projectile.Center, TheNewEnforcerDogPalette.SpecialMoveColor(Timer * 0.05f).ToVector3() * Fade * 1.15f);

            ApplyGravityPull();
            SpawnAmbientVFX();

            if (Timer == BurstTime)
            {
                SpawnLaserPattern();
                SpawnBurstVFX();
                ApplyScreenShake(7f);
                SoundEngine.PlaySound(SoundID.Item84 with { Volume = 0.72f, Pitch = -0.18f, PitchVariance = 0.06f }, Projectile.Center);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float activeRadius = Radius * MathHelper.Lerp(0.64f, 1f, Utils.GetLerpValue(BurstTime - 6f, BurstTime + 12f, Timer, true));
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, activeRadius, targetHitbox);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 240);
            if (!IsBossLike(target))
                target.AddBuff(BuffID.Confused, 90);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 pull = (Projectile.Center - target.Center).SafeNormalize(Vector2.Zero);
                target.velocity += pull * (IsBossLike(target) ? 1.2f : 4.8f);
            }
        }

        private void ApplyGravityPull()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float pullRadius = Radius * 1.38f;
            float pullFade = Utils.GetLerpValue(0f, BurstTime, Timer, true) * Utils.GetLerpValue(Lifetime, BurstTime, Timer, true);

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                Vector2 toCenter = Projectile.Center - npc.Center;
                float distance = toCenter.Length();
                if (distance > pullRadius || distance < 1f)
                    continue;

                bool bossLike = IsBossLike(npc);
                float distanceInterpolant = 1f - distance / pullRadius;
                float strength = MathHelper.Lerp(0.055f, 0.28f, distanceInterpolant) * pullFade;
                if (bossLike)
                    strength *= 0.32f;

                Vector2 desiredVelocity = toCenter.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(2.2f, 8.8f, distanceInterpolant);
                npc.velocity = Vector2.Lerp(npc.velocity * (bossLike ? 0.96f : 0.9f), desiredVelocity, strength);

                if (!bossLike && distance < Radius * 0.96f)
                    npc.velocity *= 0.93f;

                if (Timer % 20f == 0f)
                    npc.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 120);
            }
        }

        private void SpawnLaserPattern()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            int laserCount = Pattern switch
            {
                0 => 3,
                1 => 4,
                _ => 5
            };

            float baseRotation = Projectile.ai[1] * 0.72f + Projectile.identity * 0.19f;
            float length = Pattern == 2 ? 1850f : 1580f;

            for (int i = 0; i < laserCount; i++)
            {
                float rotation = baseRotation + MathHelper.TwoPi * i / laserCount;
                if (Pattern == 1)
                    rotation += MathHelper.PiOver4;

                Vector2 direction = rotation.ToRotationVector2();
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + direction * Main.rand.NextFloat(-22f, 22f),
                    direction,
                    ModContent.ProjectileType<TheNewEnforcerLaserWall>(),
                    Math.Max(1, (int)(Projectile.damage * 0.46f)),
                    Projectile.knockBack * 0.46f,
                    Projectile.owner,
                    rotation,
                    length);
            }

            int crackCount = Pattern == 2 ? 9 : 6;
            for (int i = 0; i < crackCount; i++)
            {
                float rotation = baseRotation + MathHelper.TwoPi * i / crackCount + Main.rand.NextFloat(-0.3f, 0.3f);
                Vector2 direction = rotation.ToRotationVector2();
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + direction * Main.rand.NextFloat(32f, Radius * 0.45f) + Main.rand.NextVector2Circular(18f, 18f),
                    Vector2.Zero,
                    ModContent.ProjectileType<TheNewEnforcerRiftCrack>(),
                    Math.Max(1, (int)(Projectile.damage * 0.28f)),
                    Projectile.knockBack * 0.28f,
                    Projectile.owner,
                    rotation,
                    Main.rand.NextFloat(210f, 370f));
            }
        }

        private void SpawnOpeningVFX()
        {
            if (Main.dedServ)
                return;

            Color color = TheNewEnforcerDogPalette.RiftColor(Projectile.ai[1]);
            SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.46f, Pitch = 0.08f, PitchVariance = 0.06f }, Projectile.Center);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                color,
                Vector2.One,
                Projectile.ai[1],
                0.12f,
                1.55f,
                24));

            for (int i = 0; i < 40; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(Radius * Main.rand.NextFloat(0.28f, 0.72f), Radius * Main.rand.NextFloat(0.28f, 0.72f));
                Vector2 velocity = -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(2.2f, 6.4f);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + offset,
                    velocity,
                    false,
                    Main.rand.Next(16, 30),
                    Main.rand.NextFloat(0.055f, 0.12f),
                    Main.rand.NextBool() ? TheNewEnforcerDogPalette.Cyan : TheNewEnforcerDogPalette.Fuchsia,
                    new Vector2(2.6f, 0.5f),
                    true));
            }
        }

        private void SpawnAmbientVFX()
        {
            if (Main.dedServ || Main.rand.NextBool(3))
                return;

            Vector2 offset = Main.rand.NextVector2CircularEdge(Radius * Main.rand.NextFloat(0.22f, 0.88f), Radius * Main.rand.NextFloat(0.22f, 0.88f));
            Vector2 velocity = -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.6f, 2.2f);
            Color color = TheNewEnforcerDogPalette.SpecialMoveColor(Timer * 0.05f + offset.X * 0.01f);

            if (Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center + offset,
                    velocity * 0.35f,
                    Color.Lerp(color, Color.DarkViolet, 0.42f),
                    Main.rand.Next(24, 42),
                    Main.rand.NextFloat(0.42f, 0.82f),
                    0.62f,
                    Main.rand.NextFloat(-0.05f, 0.05f),
                    glowing: true));
            }
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                        Projectile.Center + offset + Main.rand.NextVector2Circular(6f, 6f),
                        velocity.RotatedByRandom(0.16f) * Main.rand.NextFloat(0.86f, 1.16f),
                        Main.rand.NextFloat(0.36f, 0.74f),
                        color,
                        Main.rand.Next(16, 28)));
                }
            }
        }

        private void SpawnBurstVFX()
        {
            if (Main.dedServ)
                return;

            Color color = TheNewEnforcerDogPalette.RiftColor(Timer);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                color * 0.72f,
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One,
                Projectile.rotation,
                0.06f,
                0.675f,
                22,
                true));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(TheNewEnforcerDogPalette.Fuchsia, TheNewEnforcerDogPalette.Cyan, 0.5f),
                Vector2.One,
                Projectile.rotation,
                0.16f,
                1.025f,
                24));

            for (int i = 0; i < 68; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 12f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(24f, 24f),
                    velocity,
                    false,
                    Main.rand.Next(16, 28),
                    Main.rand.NextFloat(0.6f, 1.28f),
                    Main.rand.NextBool() ? TheNewEnforcerDogPalette.Cyan : TheNewEnforcerDogPalette.Fuchsia));
            }

            for (int i = 0; i < 16; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 7.2f);
                Color lightColor = Main.rand.NextBool() ? TheNewEnforcerDogPalette.Cyan : TheNewEnforcerDogPalette.Fuchsia;
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(20f, 20f),
                    velocity,
                    Main.rand.NextFloat(0.38f, 0.82f),
                    Color.Lerp(lightColor, Color.White, 0.16f),
                    Main.rand.Next(16, 28)));
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.8f, 9.5f);
                GalaxyMetaball.SpawnParticle(Projectile.Center + Main.rand.NextVector2Circular(22f, 22f), velocity, 56f * Main.rand.NextFloat(0.75f, 1.3f));
            }
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(2600f, 320f, Main.LocalPlayer.Distance(Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmear").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float charge = Utils.GetLerpValue(0f, BurstTime, Timer, true);
            float collapse = Utils.GetLerpValue(Lifetime, BurstTime, Timer, true);
            float pulse = 0.92f + 0.08f * MathF.Sin(Main.GlobalTimeWrappedHourly * 10f + Projectile.identity);
            float scale = Radius / 120f;
            Color color = TheNewEnforcerDogPalette.SpecialMoveColor(Timer * 0.05f + Projectile.ai[1]);
            Color secondary = Color.Lerp(TheNewEnforcerDogPalette.Fuchsia, TheNewEnforcerDogPalette.Cyan, charge);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                color * 0.22f * Fade,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                scale * (0.72f + charge * 0.36f) * pulse,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                smear,
                drawPosition,
                null,
                secondary with { A = 0 } * 0.22f * Fade,
                -Projectile.rotation * 0.7f,
                smear.Size() * 0.5f,
                scale * (0.46f + charge * 0.52f),
                SpriteEffects.None,
                0f);

            for (int i = 0; i < 3; i++)
            {
                float ringScale = scale * (0.28f + charge * (0.42f + i * 0.14f)) * collapse;
                Main.EntitySpriteDraw(
                    ring,
                    drawPosition,
                    null,
                    (i % 2 == 0 ? color : secondary with { A = 0 }) * Fade * (0.36f - i * 0.07f),
                    Projectile.rotation * (i % 2 == 0 ? 1f : -1f) + i * 0.7f,
                    ring.Size() * 0.5f,
                    ringScale,
                    SpriteEffects.None,
                    0f);
            }

            int armCount = 10 + Pattern * 2;
            for (int i = 0; i < armCount; i++)
            {
                float rotation = Projectile.rotation + MathHelper.TwoPi * i / armCount + MathF.Sin(Timer * 0.04f + i) * 0.08f;
                Vector2 direction = rotation.ToRotationVector2();
                float length = Radius * MathHelper.Lerp(0.36f, 0.92f, (i % 4) / 3f) * charge * collapse;
                Color armColor = (i % 2 == 0 ? TheNewEnforcerDogPalette.Cyan : TheNewEnforcerDogPalette.Fuchsia) * 0.26f * Fade;
                DrawLine(pixel, Projectile.Center - direction * 16f, Projectile.Center + direction * length, armColor, MathHelper.Lerp(3f, 9f, charge));
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private static void DrawLine(Texture2D pixel, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 line = end - start;
            if (line.LengthSquared() <= 0.01f)
                return;

            Main.EntitySpriteDraw(
                pixel,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                line.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(line.Length(), width),
                SpriteEffects.None,
                0f);
        }

        private static int PositiveModulo(int value, int divisor)
        {
            int result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        private static bool IsBossLike(NPC npc) =>
            npc.boss ||
            NPCID.Sets.ShouldBeCountedAsBoss[npc.type] ||
            npc.type == NPCID.CultistBoss ||
            npc.type == NPCID.CultistBossClone;
    }

    internal sealed class TheNewEnforcerLaserWall : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 46;
        private const int WarningTime = 18;
        private const int ActiveTime = 18;

        private ref float Timer => ref Projectile.localAI[0];
        private float Length => Projectile.ai[1] <= 0f ? 1580f : Projectile.ai[1];
        private Vector2 Direction
        {
            get
            {
                if (Projectile.velocity.LengthSquared() > 0.001f)
                    return Projectile.velocity.SafeNormalize(Vector2.UnitX);

                return Projectile.ai[0].ToRotationVector2();
            }
        }

        private bool IsActive => Timer >= WarningTime && Timer <= WarningTime + ActiveTime;

        public new string LocalizationCategory => "Projectiles.TheEnforcer";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 6000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.rotation = Direction.ToRotation();
            Projectile.velocity = Direction;
        }

        public override bool? CanDamage() => IsActive ? null : false;

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Direction.ToRotation();
            Projectile.velocity = Direction;

            if (Timer == WarningTime)
            {
                ApplyScreenShake(3.2f);
                SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.56f, Pitch = 0.22f, PitchVariance = 0.04f }, Projectile.Center);
            }

            if (Main.rand.NextBool(IsActive ? 2 : 5))
                SpawnLineDust();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!IsActive)
                return false;

            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center - Direction * Length * 0.5f,
                Projectile.Center + Direction * Length * 0.5f,
                28f,
                ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);
            if (!target.boss)
                target.AddBuff(BuffID.Confused, 60);
        }

        private void SpawnLineDust()
        {
            if (Main.dedServ)
                return;

            Vector2 direction = Direction;
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(-Length * 0.48f, Length * 0.48f);
            Color color = TheNewEnforcerDogPalette.SpecialMoveColor(Timer * 0.09f + Projectile.identity);
            Dust dust = Dust.NewDustPerfect(
                position + normal * Main.rand.NextFloat(-10f, 10f),
                Main.rand.NextBool() ? DustID.Shadowflame : DustID.BlueTorch,
                normal * Main.rand.NextFloat(-1.8f, 1.8f),
                100,
                Color.Lerp(color, Color.White, 0.2f),
                Main.rand.NextFloat(0.75f, 1.1f));
            dust.noGravity = true;
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(2500f, 260f, Main.LocalPlayer.Distance(Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 start = Projectile.Center - Direction * Length * 0.5f;
            Vector2 end = Projectile.Center + Direction * Length * 0.5f;
            float warningFade = Utils.GetLerpValue(0f, 8f, Timer, true) * Utils.GetLerpValue(WarningTime + 6f, WarningTime, Timer, true);
            float activeFade = Utils.GetLerpValue(WarningTime, WarningTime + 4f, Timer, true) * Utils.GetLerpValue(Lifetime, WarningTime + ActiveTime, Timer, true);
            Color warningColor = Color.Lerp(TheNewEnforcerDogPalette.Cyan, TheNewEnforcerDogPalette.Fuchsia, 0.35f + 0.25f * MathF.Sin(Timer * 0.13f)) with { A = 0 };
            Color activeColor = TheNewEnforcerDogPalette.SpecialMoveColor(Timer * 0.09f + Projectile.identity);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            if (warningFade > 0f)
            {
                DrawLine(pixel, start, end, warningColor * 0.36f * warningFade, 4f);
                DrawLine(pixel, start, end, Color.White with { A = 0 } * 0.2f * warningFade, 1.6f);
            }

            if (activeFade > 0f)
            {
                DrawLine(pixel, start, end, activeColor * 0.48f * activeFade, 34f);
                DrawLine(pixel, start, end, activeColor * 0.86f * activeFade, 14f);
                DrawLine(pixel, start, end, Color.White with { A = 0 } * 0.78f * activeFade, 4f);

                Main.EntitySpriteDraw(
                    bloom,
                    Projectile.Center - Main.screenPosition,
                    null,
                    activeColor * 0.55f * activeFade,
                    Projectile.rotation,
                    bloom.Size() * 0.5f,
                    new Vector2(0.82f, 0.18f),
                    SpriteEffects.None,
                    0f);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private static void DrawLine(Texture2D pixel, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 line = end - start;
            if (line.LengthSquared() <= 0.01f)
                return;

            Main.EntitySpriteDraw(
                pixel,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                line.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(line.Length(), width),
                SpriteEffects.None,
                0f);
        }
    }

    internal sealed class TheNewEnforcerRiftCrack : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 30;

        private ref float Timer => ref Projectile.localAI[0];
        private float Length => Projectile.ai[1] <= 0f ? 230f : Projectile.ai[1];
        private Vector2 Direction => Projectile.ai[0].ToRotationVector2();
        private float Fade =>
            Utils.GetLerpValue(0f, 5f, Timer, true) *
            Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);

        public new string LocalizationCategory => "Projectiles.TheEnforcer";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() =>
            Timer >= 3f && Timer <= 22f ? null : false;

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Projectile.ai[0];
            Lighting.AddLight(Projectile.Center, TheNewEnforcerDogPalette.RiftColor(Timer * 0.08f).ToVector3() * Fade * 0.48f);

            if (Timer == 1f)
                SpawnInitialVFX();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Timer < 3f || Timer > 22f)
                return false;

            float collisionPoint = 0f;
            Vector2 start = Projectile.Center - Direction * Length * 0.5f;
            Vector2 end = Projectile.Center + Direction * Length * 0.5f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 18f, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 120);
        }

        private void SpawnInitialVFX()
        {
            if (Main.dedServ)
                return;

            Color color = TheNewEnforcerDogPalette.RiftColor(Projectile.ai[0]);
            Vector2 direction = Direction;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                color,
                Vector2.One,
                Projectile.ai[0],
                0.04f,
                0.72f,
                12));

            for (int i = 0; i < 16; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.55f) * Main.rand.NextFloat(-4.2f, 4.2f);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + direction * Main.rand.NextFloat(-Length * 0.4f, Length * 0.4f),
                    velocity,
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.045f, 0.08f),
                    Main.rand.NextBool() ? TheNewEnforcerDogPalette.Cyan : TheNewEnforcerDogPalette.Fuchsia,
                    new Vector2(2.2f, 0.42f),
                    true));
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 direction = Direction;
            Vector2 start = Projectile.Center - direction * Length * 0.5f;
            Vector2 end = Projectile.Center + direction * Length * 0.5f;
            Color color = TheNewEnforcerDogPalette.SpecialMoveColor(Timer * 0.08f + Projectile.identity);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            DrawLine(pixel, start, end, color * 0.56f * Fade, 18f);
            DrawLine(pixel, start, end, Color.White with { A = 0 } * 0.72f * Fade, 3f);

            for (int i = 0; i < 4; i++)
            {
                float completion = 0.2f + i * 0.18f;
                float signed = PseudoRandomSigned(i);
                Vector2 branchStart = Vector2.Lerp(start, end, completion);
                Vector2 branchDirection = direction.RotatedBy(Math.Sign(signed == 0f ? 1f : signed) * MathHelper.Lerp(0.44f, 0.94f, Math.Abs(signed)));
                float branchLength = Length * MathHelper.Lerp(0.11f, 0.21f, Math.Abs(PseudoRandomSigned(i + 7)));
                DrawLine(pixel, branchStart, branchStart + branchDirection * branchLength, (i % 2 == 0 ? TheNewEnforcerDogPalette.Cyan : TheNewEnforcerDogPalette.Fuchsia) * 0.36f * Fade, 8f);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private float PseudoRandomSigned(int index) =>
            MathF.Sin((Projectile.identity + 1) * 12.9898f + index * 78.233f);

        private static void DrawLine(Texture2D pixel, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 line = end - start;
            if (line.LengthSquared() <= 0.01f)
                return;

            Main.EntitySpriteDraw(
                pixel,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                line.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(line.Length(), width),
                SpriteEffects.None,
                0f);
        }
    }
}
