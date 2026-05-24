using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.TheEnforcer
{
    internal sealed class NewEssenceFlame2 : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        private const int Lifetime = 300;
        private const float HomingRange = 2800f;

        private ref float TargetIndex => ref Projectile.ai[0];
        private ref float Phase => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private int LaneIndex => PositiveModulo((int)MathF.Floor(Phase), 10);
        private int FlameProfile => PositiveModulo((int)MathF.Floor(Phase / 10f), 4);
        private float CenteredLane => LaneIndex - 3f;
        private float Seed => Phase * 0.613f + Projectile.identity * 0.173f;
        private float ReleaseDelay => 12f + LaneIndex * 2.5f + FlameProfile * 1.75f;

        public new string LocalizationCategory => "Projectiles.TheEnforcer";
        public override string Texture => "CalamityMod/Projectiles/Healing/EssenceFlame";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 34;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.alpha = 255;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool? CanHitNPC(NPC target) => Timer > ReleaseDelay * 0.55f && target.CanBeChasedBy(Projectile);

        public override void AI()
        {
            Timer++;
            Projectile.alpha = Math.Max(0, Projectile.alpha - 18);
            Projectile.Opacity = MathHelper.Clamp(1f - Projectile.alpha / 255f, 0f, 1f) * Utils.GetLerpValue(0f, 24f, Projectile.timeLeft, true);

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 5)
            {
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
                Projectile.frameCounter = 0;
            }

            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float currentSpeed = MathHelper.Clamp(Projectile.velocity.Length(), 12f, 32f);
            NPC target = ResolveTarget(currentDirection);

            if (target is not null)
            {
                float distance = Projectile.Distance(target.Center);
                float lockIn = Utils.GetLerpValue(ReleaseDelay, ReleaseDelay + 70f, Timer, true);
                float terminal = Utils.GetLerpValue(320f, 85f, distance, true);
                float farBoost = Utils.GetLerpValue(620f, 1650f, distance, true);
                Vector2 aimPoint = ComputeHomingAimPoint(target, currentDirection, currentSpeed, distance, lockIn, terminal);
                Vector2 desiredDirection = (aimPoint - Projectile.Center).SafeNormalize(currentDirection);

                if (Timer < ReleaseDelay)
                {
                    float driftTurn = MathF.Sin(Timer * 0.13f + Seed) * 0.055f + CenteredLane * 0.006f;
                    Vector2 driftDirection = currentDirection.RotatedBy(driftTurn);
                    float driftSpeed = MathHelper.Clamp(currentSpeed + 0.05f, 11f, 18f);
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, driftDirection * driftSpeed, 0.08f);
                }
                else
                {
                    float side = CenteredLane == 0f ? (Projectile.identity % 2 == 0 ? 1f : -1f) : Math.Sign(CenteredLane);
                    Vector2 tangent = desiredDirection.RotatedBy(MathHelper.PiOver2 * side);
                    float slip = MathF.Sin(Timer * (0.085f + FlameProfile * 0.008f) + Seed) * (1f - terminal) * (0.22f + Math.Abs(CenteredLane) * 0.045f);
                    Vector2 curvedDirection = (desiredDirection + tangent * slip).SafeNormalize(desiredDirection);
                    float targetSpeed = MathHelper.Lerp(13.5f, 24f + FlameProfile * 1.4f, lockIn) + farBoost * 8.5f + terminal * 5.5f;
                    float turnStrength = MathHelper.Lerp(0.045f, 0.18f, lockIn) + terminal * 0.14f + farBoost * 0.035f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, curvedDirection * targetSpeed, turnStrength);
                }

                TargetIndex = target.whoAmI;
            }
            else
            {
                float searchWeave = MathF.Sin(Timer * 0.06f + Seed) * 0.035f;
                Vector2 searchDirection = currentDirection.RotatedBy(searchWeave);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, searchDirection * MathHelper.Clamp(currentSpeed + 0.08f, 12f, 24f), 0.055f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.42f, 0.16f, 0.8f) * Projectile.Opacity);
            SpawnFlightVFX();
        }

        private NPC ResolveTarget(Vector2 currentDirection)
        {
            int targetIndex = (int)TargetIndex;
            NPC lockedTarget = null;

            if (targetIndex >= 0 && targetIndex < Main.maxNPCs)
            {
                lockedTarget = Main.npc[targetIndex];
                if (lockedTarget.active && lockedTarget.CanBeChasedBy(Projectile) && Projectile.Distance(lockedTarget.Center) <= HomingRange * 1.25f)
                {
                    if (Timer < ReleaseDelay + 42f)
                        return lockedTarget;
                }
                else
                    lockedTarget = null;
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
                float anglePenalty = (1f - forwardness) * 260f;
                float lanePenalty = Math.Abs(CenteredLane) * 7f;
                float score = distance + anglePenalty + lanePenalty;
                if (lockedTarget is not null && npc.whoAmI == lockedTarget.whoAmI)
                    score *= 0.68f;
                if (npc.whoAmI == Main.player[Projectile.owner].MinionAttackTargetNPC)
                    score *= 0.72f;

                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestTarget = npc;
            }

            return bestTarget;
        }

        private Vector2 ComputeHomingAimPoint(NPC target, Vector2 currentDirection, float currentSpeed, float distance, float lockIn, float terminal)
        {
            float side = CenteredLane == 0f ? (Projectile.identity % 2 == 0 ? 1f : -1f) : Math.Sign(CenteredLane);
            float laneMagnitude = Math.Abs(CenteredLane);
            float leadTime = MathHelper.Clamp(distance / Math.Max(currentSpeed, 1f), 8f, 54f);
            float leadScale = MathHelper.Lerp(0.35f, 0.88f, lockIn);
            Vector2 predictedCenter = target.Center + target.velocity * leadTime * leadScale;
            Vector2 targetDirection = (predictedCenter - Projectile.Center).SafeNormalize(currentDirection);
            Vector2 tangent = targetDirection.RotatedBy(MathHelper.PiOver2 * side);

            float profileBias = FlameProfile switch
            {
                0 => 1f,
                1 => -0.72f,
                2 => MathF.Sin(Timer * 0.035f + Seed) > 0f ? 1.28f : -1.28f,
                _ => 0.34f
            };

            float orbitRadius = MathHelper.Lerp(190f + laneMagnitude * 24f, 10f + laneMagnitude * 4f, MathHelper.Clamp(lockIn + terminal * 0.65f, 0f, 1f));
            float weave = MathF.Sin(Timer * (0.055f + FlameProfile * 0.006f) + Seed) * (46f + laneMagnitude * 11f) * (1f - terminal) * (1f - lockIn * 0.32f);
            float pullBack = MathHelper.Lerp(118f, 0f, lockIn) * (FlameProfile == 1 ? 1.25f : 0.56f) * (1f - terminal);

            return predictedCenter + tangent * (orbitRadius * profileBias + weave) - targetDirection * pullBack;
        }

        private static int PositiveModulo(int value, int divisor)
        {
            int result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        private void SpawnFlightVFX()
        {
            if (Main.dedServ || Timer % 2f != 0f)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Color mainColor = EnergyColor(Timer * 0.025f + Phase * 0.07f);

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center - direction * 10f + normal * Main.rand.NextFloat(-4f, 4f),
                Main.rand.NextBool() ? DustID.Shadowflame : DustID.BlueTorch,
                -direction * Main.rand.NextFloat(0.8f, 2.8f) + normal * Main.rand.NextFloat(-0.7f, 0.7f),
                100,
                Color.Lerp(mainColor, Color.White, Main.rand.NextFloat(0.05f, 0.24f)),
                Main.rand.NextFloat(0.75f, 1.15f) * Projectile.scale);
            dust.noGravity = true;

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center - direction * 16f,
                    -direction * Main.rand.NextFloat(0.8f, 1.8f) + Main.rand.NextVector2Circular(0.35f, 0.35f),
                    "CalamityMod/Particles/FadeStreak",
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.045f, 0.075f) * Projectile.scale,
                    mainColor * 0.78f,
                    new Vector2(0.72f, 1.8f),
                    true,
                    false,
                    shrinkSpeed: 0.52f,
                    glowOpacity: 0.65f));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);

            if (Main.myPlayer != Projectile.owner)
                return;

            for (int i = 0; i < 3; i++)
            {
                float angle = MathHelper.TwoPi * i / 3f + Main.rand.NextFloat(-0.34f, 0.34f);
                Vector2 orbit = angle.ToRotationVector2();
                Vector2 spawnPosition = target.Center + orbit * Main.rand.NextFloat(38f, 72f);
                Vector2 velocity = (-orbit).RotatedByRandom(0.42f) * Main.rand.NextFloat(3.4f, 5.6f) + target.velocity * 0.2f;

                Projectile.NewProjectile(
                    Projectile.GetSource_OnHit(target),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<OldEssenceFlame2>(),
                    Math.Max(1, (int)(Projectile.damage * 0.55f)),
                    Projectile.knockBack * 0.35f,
                    Projectile.owner);
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.42f, Pitch = 0.25f }, Projectile.Center);

            if (Main.dedServ)
                return;

            Color color = EnergyColor(Timer * 0.04f);
            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 6.2f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool() ? DustID.Shadowflame : DustID.BlueTorch,
                    velocity,
                    100,
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.08f, 0.35f)),
                    Main.rand.NextFloat(0.9f, 1.55f));
                dust.noGravity = true;
            }

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                color,
                Vector2.One,
                Projectile.velocity.ToRotation(),
                0.08f,
                1.65f,
                18));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D streak = ModContent.Request<Texture2D>("CalamityMod/Particles/FadeStreak").Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color color = EnergyColor(Timer * 0.018f + Phase);
            Color additiveColor = color;
            additiveColor.A = 0;
            float fade = Projectile.Opacity;
            float pulse = 0.88f + 0.12f * MathF.Sin(Main.GlobalTimeWrappedHourly * 14f + Phase);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                streak,
                drawPosition - direction * 8f,
                null,
                additiveColor * 0.58f * fade,
                direction.ToRotation(),
                streak.Size() * 0.5f,
                new Vector2(0.82f, 0.24f) * Projectile.scale * pulse,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                additiveColor * 0.42f * fade,
                0f,
                bloom.Size() * 0.5f,
                0.18f * Projectile.scale * pulse,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                frame,
                Color.Lerp(color, Color.White, 0.18f) * fade,
                Projectile.rotation,
                frame.Size() * 0.5f,
                Projectile.scale * 1.12f,
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
                    (_, _) => Projectile.Size * 0.5f,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                trailPoints.Length * 3);

            Vector2[] corePoints = trailPoints.Take(Math.Min(12, trailPoints.Length)).ToArray();
            if (corePoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                corePoints,
                new PrimitiveSettings(
                    CoreTrailWidth,
                    CoreTrailColor,
                    (_, _) => Projectile.Size * 0.5f,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                corePoints.Length * 3);
        }

        private float TrailWidth(float completion, Vector2 _) =>
            Utils.Remap(completion, 0f, 0.86f, Projectile.scale * 34f, 0f) * Projectile.Opacity;

        private Color TrailColor(float completion, Vector2 _)
        {
            Color head = EnergyColor(Phase + completion * 0.25f);
            Color tail = Color.Lerp(new Color(28, 8, 64), Color.Transparent, Utils.GetLerpValue(0.58f, 1f, completion, true));
            head.A = 0;
            tail.A = 0;
            return Color.Lerp(head, tail, completion) * (1f - completion * 0.32f) * Projectile.Opacity;
        }

        private float CoreTrailWidth(float completion, Vector2 _) =>
            Utils.Remap(completion, 0f, 0.78f, Projectile.scale * 11f, 0f) * Projectile.Opacity;

        private Color CoreTrailColor(float completion, Vector2 _)
        {
            Color color = Color.Lerp(Color.White, EnergyColor(Phase + completion * 0.35f), 0.34f);
            color.A = 0;
            return color * (1f - Utils.GetLerpValue(0.72f, 1f, completion, true)) * Projectile.Opacity;
        }

        private Color EnergyColor(float offset)
        {
            float pulse = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.6f + offset + Projectile.identity * 0.11f) * 0.5f + 0.5f;
            Color violet = new(126, 58, 255);
            Color cyan = new(72, 230, 255);
            Color color = Color.Lerp(violet, cyan, 0.18f + pulse * 0.24f);
            color.A = 0;
            return color;
        }
    }
}
