using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ashes
{
    internal sealed class AshesofAnn_CurseFire : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        private const float BaseSpeed = 16.2f;
        private const float HomingRange = 200f * 16f;
        private const int TotalRelayShots = 17;
        private const float VisualEffectScale = 0.7f;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float HitSomething => ref Projectile.localAI[1];

        private int PreferredTargetIndex => (int)Projectile.ai[0];
        private int ShotIndex => (int)Projectile.ai[1];
        private bool IsPiercingShot => Projectile.ai[2] < 0f;
        private float ShotCompletion => MathHelper.Clamp(ShotIndex / (float)(TotalRelayShots - 1), 0f, 1f);

        private Color MainColor => Color.Lerp(new Color(192, 0, 12), new Color(255, 118, 42), 0.25f + 0.45f * ShotCompletion);
        private Color SecondaryColor => Color.Lerp(new Color(62, 0, 0), new Color(128, 12, 128), 0.22f + 0.32f * (float)Math.Sin(Projectile.ai[2] * 3.1f));
        private Color CoreColor => Color.Lerp(Color.White, new Color(255, 206, 112), 0.2f + ShotCompletion * 0.22f);

        private static int ScaledVisualCount(int count) => Math.Max(1, (int)MathF.Round(count * VisualEffectScale));

        private bool PassVisualBudget(int cadence, int salt)
        {
            int emissionIndex = (int)Timer / Math.Max(1, cadence);
            return (emissionIndex + ShotIndex * 3 + salt) % 10 < 7;
        }

        private static bool ScaledChance(int originalDenominator) => Main.rand.NextFloat() < VisualEffectScale / originalDenominator;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 32;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 190;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.alpha = 255;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Projectile.velocity == Vector2.Zero)
                Projectile.velocity = Vector2.UnitX * BaseSpeed;

            Projectile.scale = MathHelper.Lerp(0.96f, 1.22f, ShotCompletion);
            Projectile.Opacity = 0f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (IsPiercingShot)
            {
                Projectile.penetrate = -1;
                Projectile.localNPCHitCooldown = 6;
            }
        }

        public override void AI()
        {
            Timer++;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float homingDelay = MathHelper.Lerp(6f, 3f, ShotCompletion) * (Projectile.extraUpdates + 1f);
            bool homingActive = !IsPiercingShot && Timer >= homingDelay;

            if (IsPiercingShot)
                UpdatePiercingFlight(direction);
            else if (homingActive)
                UpdateHoming(direction, homingDelay);
            else
                UpdateLaunchCurve(direction, homingDelay);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = Utils.GetLerpValue(0f, 10f, Timer, true) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            Projectile.alpha = (int)MathHelper.Lerp(255f, 0f, Projectile.Opacity);

            Lighting.AddLight(Projectile.Center, MainColor.ToVector3() * (0.66f * Projectile.Opacity));

            if (!Main.dedServ)
                SpawnFlightParticles(homingActive);
        }

        private void UpdateLaunchCurve(Vector2 direction, float homingDelay)
        {
            float launchPower = Utils.GetLerpValue(0f, homingDelay, Timer, true);
            float speed = MathHelper.Lerp(Projectile.velocity.Length(), BaseSpeed * MathHelper.Lerp(0.88f, 1.08f, launchPower), 0.08f);
            float curve = (float)Math.Sin((Timer + Projectile.ai[2] * 23f) * 0.18f) * 0.012f;
            Projectile.velocity = direction.RotatedBy(curve) * speed;
        }

        private void UpdatePiercingFlight(Vector2 direction)
        {
            float speed = MathHelper.Lerp(Projectile.velocity.Length(), BaseSpeed * MathHelper.Lerp(1.18f, 1.42f, ShotCompletion), 0.045f);
            float curve = (float)Math.Sin((Timer + ShotIndex * 17f) * 0.045f) * 0.004f;
            Projectile.velocity = direction.RotatedBy(curve) * speed;
        }

        private void UpdateHoming(Vector2 currentDirection, float homingDelay)
        {
            NPC target = FindTarget(HomingRange, currentDirection);
            float idealSpeed = BaseSpeed * MathHelper.Lerp(1.02f, 1.32f, ShotCompletion);

            if (target is null)
            {
                float curve = (float)Math.Sin((Timer + Projectile.ai[2] * 19f) * 0.075f) * 0.018f;
                Projectile.velocity = currentDirection.RotatedBy(curve) * MathHelper.Lerp(Projectile.velocity.Length(), idealSpeed, 0.07f);
                return;
            }

            float distance = Projectile.Distance(target.Center);
            float predictionFrames = MathHelper.Clamp(distance / Math.Max(idealSpeed, 1f), 8f, 24f);
            Vector2 predictedCenter = target.Center + target.velocity * predictionFrames;
            Vector2 desiredDirection = (predictedCenter - Projectile.Center).SafeNormalize(currentDirection);
            float timePower = Utils.GetLerpValue(0f, 76f * (Projectile.extraUpdates + 1f), Timer - homingDelay, true);
            float closePower = Utils.GetLerpValue(780f, 120f, distance, true);
            float trackingPower = MathHelper.Max(timePower, closePower * 0.82f);
            float targetSpeed = idealSpeed * MathHelper.Lerp(1.06f, 1.72f, trackingPower);
            float inertia = MathHelper.Lerp(12f, 1.85f, trackingPower);

            Projectile.velocity = (Projectile.velocity * inertia + desiredDirection * targetSpeed) / (inertia + 1f);

            float speed = Projectile.velocity.Length();
            Projectile.velocity = Projectile.velocity.SafeNormalize(desiredDirection) * MathHelper.Clamp(speed, idealSpeed * 0.9f, idealSpeed * 1.86f);
        }

        private NPC FindTarget(float range, Vector2 currentDirection)
        {
            if (Main.npc.IndexInRange(PreferredTargetIndex))
            {
                NPC preferred = Main.npc[PreferredTargetIndex];
                if (preferred.CanBeChasedBy(Projectile, false) && Projectile.Distance(preferred.Center) <= range)
                    return preferred;
            }

            NPC bestTarget = null;
            float bestScore = range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                Vector2 toTarget = npc.Center - Projectile.Center;
                float distance = toTarget.Length();
                if (distance > range)
                    continue;

                float angularPenalty = (1f - MathHelper.Clamp(Vector2.Dot(currentDirection, toTarget.SafeNormalize(currentDirection)), -1f, 1f)) * 680f;
                float score = distance + angularPenalty;
                if (npc.boss)
                    score *= 0.68f;

                if (score >= bestScore)
                    continue;

                bestTarget = npc;
                bestScore = score;
            }

            return bestTarget;
        }

        public override bool? CanDamage() => Timer > 4f && Projectile.Opacity > 0.18f ? null : false;

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float finalShotBoost = ShotIndex == TotalRelayShots - 1 ? 1.2f : 1f;
            modifiers.SourceDamage *= MathHelper.Lerp(0.9f, 1.08f, ShotCompletion) * finalShotBoost;
            modifiers.FlatBonusDamage += Math.Min(target.lifeMax / 260f, Projectile.damage * 0.85f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            HitSomething = 1f;
            SoundEngine.PlaySound(SoundID.NPCDeath6, target.Center);
            target.AddBuff(ModContent.BuffType<VulnerabilityHex>(), 180);
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 240);
            target.AddBuff(BuffID.CursedInferno, 210);

            if (!IsPiercingShot)
                Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            bool impact = HitSomething == 1f || timeLeft > 2;
            SpawnDeathEffects(impact);

            if (impact)
            {
                if (HitSomething != 1f)
                {
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.28f, Pitch = -0.08f + ShotCompletion * 0.18f, PitchVariance = 0.12f, MaxInstances = 8 }, Projectile.Center);
                    if (ShotIndex == TotalRelayShots - 1)
                        SoundEngine.PlaySound(SoundID.Item110 with { Volume = 0.42f, Pitch = -0.18f, PitchVariance = 0.1f, MaxInstances = 4 }, Projectile.Center);
                }
            }
        }

        private void SpawnFlightParticles(bool homingActive)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 center = Projectile.Center - direction * Main.rand.NextFloat(2f, 12f) + normal * Main.rand.NextFloat(-4.5f, 4.5f);
            Color mainColor = MainColor;

            if ((int)Timer % 2 == 0 && PassVisualBudget(2, 0))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    center,
                    Projectile.velocity * 0.02f,
                    "CalamityMod/Particles/VerticalSmear",
                    false,
                    Main.rand.Next(13, 18),
                    Main.rand.NextFloat(1.45f, 2.15f),
                    mainColor,
                    new Vector2(0.2f, 1f),
                    true,
                    true,
                    shrinkSpeed: 0.82f,
                    glowOpacity: 0.45f));
            }

            if ((int)Timer % 3 == 0 && PassVisualBudget(3, 2))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center - direction * 10f + normal * Main.rand.NextFloat(-5f, 5f),
                    -direction * Main.rand.NextFloat(0.4f, 1.6f) + normal * Main.rand.NextFloat(-0.35f, 0.35f),
                    "CalamityMod/Particles/FadeStreak",
                    false,
                    Main.rand.Next(15, 23),
                    Main.rand.NextFloat(0.065f, 0.11f) * Projectile.scale,
                    Color.Lerp(mainColor, SecondaryColor, Main.rand.NextFloat(0.18f, 0.62f)),
                    new Vector2(Main.rand.NextFloat(0.36f, 0.58f), Main.rand.NextFloat(1.5f, 2.35f)),
                    shrinkSpeed: 0.7f,
                    extraRotation: direction.ToRotation() + MathHelper.PiOver2));
            }

            if (ScaledChance(homingActive ? 2 : 3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(4f, 16f) + normal * Main.rand.NextFloat(-7f, 7f),
                    Main.rand.NextBool(4) ? DustID.CursedTorch : (int)CalamityDusts.Brimstone,
                    -direction * Main.rand.NextFloat(0.6f, 2.8f) + normal * Main.rand.NextFloat(-0.8f, 0.8f),
                    110,
                    Color.Lerp(SecondaryColor, CoreColor, Main.rand.NextFloat(0.12f, 0.4f)),
                    Main.rand.NextFloat(0.9f, 1.45f) * Projectile.scale);
                dust.noGravity = true;
                dust.fadeIn = Main.rand.NextFloat(0.2f, 0.55f);
            }

            SpawnDenseMetaballs(direction, normal, homingActive);
        }

        private void SpawnDenseMetaballs(Vector2 direction, Vector2 normal, bool homingActive)
        {
            int metaballCount = ScaledVisualCount(homingActive ? 4 : 3);

            for (int i = 0; i < metaballCount; i++)
            {
                float completion = i / (float)Math.Max(1, metaballCount - 1);
                Vector2 position = Projectile.Center
                    - direction * Main.rand.NextFloat(0f, homingActive ? 38f : 28f)
                    + normal * Main.rand.NextFloat(-9f, 9f) * MathHelper.Lerp(1f, 0.55f, completion)
                    + Main.rand.NextVector2Circular(2.5f, 2.5f);

                Vector2 velocity = -direction * Main.rand.NextFloat(0.18f, 1.1f)
                    + normal * Main.rand.NextFloat(-0.38f, 0.38f)
                    + Main.rand.NextVector2Circular(0.35f, 0.35f);

                CalamitasMetaball.SpawnParticle(
                    position,
                    velocity,
                    Main.rand.NextFloat(18f, 34f) * Projectile.scale * MathHelper.Lerp(1.05f, 0.7f, completion));

                if (ScaledChance(2))
                    RancorLavaMetaball.SpawnParticle(position + Main.rand.NextVector2Circular(4f, 4f), Main.rand.NextFloat(14f, 26f) * Projectile.scale);
            }
        }

        private void SpawnDeathEffects(bool impact)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color mainColor = MainColor;
            Color secondary = SecondaryColor;
            float power = impact ? 1f : 0.52f;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                direction * 0.45f,
                Color.Lerp(mainColor, CoreColor, 0.18f),
                new Vector2(0.82f, 0.34f) * power,
                direction.ToRotation(),
                0.06f,
                1.45f * power,
                ScaledVisualCount(16)));

            int sparkCount = ScaledVisualCount(impact ? 18 : 8);
            for (int i = 0; i < sparkCount; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.82f) * Main.rand.NextFloat(2.2f, 8.6f) + Main.rand.NextVector2Circular(1.4f, 1.4f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    velocity,
                    Main.rand.NextBool(3) ? "CalamityMod/Particles/SmallBloom" : "CalamityMod/Particles/VerticalSmear",
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.18f, 0.38f) * power,
                    Main.rand.NextBool(4) ? CoreColor : Color.Lerp(mainColor, secondary, Main.rand.NextFloat(0.2f, 0.65f)),
                    new Vector2(Main.rand.NextFloat(0.8f, 1.6f), Main.rand.NextFloat(0.42f, 0.9f)),
                    true,
                    false,
                    0f,
                    false,
                    false,
                    0.64f));
            }

            for (int i = 0; i < ScaledVisualCount(impact ? 22 : 10); i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7.5f) * power;
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
                    Main.rand.NextBool(5) ? DustID.Smoke : (int)CalamityDusts.Brimstone,
                    velocity,
                    110,
                    Main.rand.NextBool() ? mainColor : secondary,
                    Main.rand.NextFloat(0.85f, 1.55f) * power);
                dust.noGravity = !Main.rand.NextBool(5);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.Opacity <= 0f)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmear").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float pulse = 0.9f + (float)Math.Sin((Timer + Projectile.ai[2] * 31f) * 0.16f) * 0.1f;
            Color mainColor = MainColor with { A = 0 };
            Color secondary = SecondaryColor with { A = 0 };
            Color core = CoreColor with { A = 0 };
            float opacity = Projectile.Opacity;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null, secondary * (0.44f * opacity), Projectile.rotation, bloom.Size() * 0.5f, Projectile.scale * 0.34f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPosition, null, mainColor * (0.32f * opacity), -Projectile.rotation * 0.7f, bloom.Size() * 0.5f, Projectile.scale * new Vector2(0.18f, 0.42f) * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(smear, drawPosition, null, mainColor * (0.62f * opacity), Projectile.rotation, smear.Size() * 0.5f, Projectile.scale * new Vector2(0.18f, 0.68f) * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(star, drawPosition, null, core * (0.72f * opacity), Projectile.rotation, star.Size() * 0.5f, Projectile.scale * new Vector2(0.24f, 0.52f) * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(star, drawPosition, null, mainColor * (0.48f * opacity), Projectile.rotation + MathHelper.PiOver2, star.Size() * 0.5f, Projectile.scale * new Vector2(0.16f, 0.44f) * pulse, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] trailPoints = BuildTrailPoints();
            if (trailPoints.Length < 2 || Projectile.Opacity <= 0f)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    TrailWidthFunction,
                    TrailColorFunction,
                    TrailOffsetFunction,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                trailPoints.Length * 3);

            Vector2[] coreTrail = trailPoints.Take(Math.Min(16, trailPoints.Length)).ToArray();
            if (coreTrail.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                coreTrail,
                new PrimitiveSettings(
                    CoreTrailWidthFunction,
                    CoreTrailColorFunction,
                    (_, _) => Projectile.Size * 0.5f,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:TrailStreak"]),
                coreTrail.Length * 3);
        }

        private Vector2[] BuildTrailPoints()
        {
            Vector2[] trailPoints = Projectile.oldPos
                .Where(position => position != Vector2.Zero)
                .Select(position => position + Projectile.Size * 0.5f)
                .ToArray();

            if (trailPoints.Length == 0)
                return new[] { Projectile.Center - Projectile.velocity, Projectile.Center };

            if (trailPoints[0] != Projectile.Center)
                trailPoints = new[] { Projectile.Center }.Concat(trailPoints).ToArray();

            return trailPoints;
        }

        private Vector2 TrailOffsetFunction(float completion, Vector2 _)
        {
            Vector2 normal = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            float wave = (float)Math.Sin(completion * MathHelper.TwoPi * 2.2f + Timer * 0.11f + Projectile.ai[2]) * 2.2f;
            return Projectile.Size * 0.5f + normal * wave;
        }

        private float TrailWidthFunction(float completion, Vector2 _)
        {
            float width = MathHelper.Lerp(26f, 40f, ShotCompletion) * Projectile.scale * Projectile.Opacity;
            float headFade = MathHelper.Lerp(0.28f, 1f, Utils.GetLerpValue(0.04f, 0.24f, completion, true));
            return MathF.Sin((1f - completion) * MathHelper.PiOver2) * width * headFade;
        }

        private Color TrailColorFunction(float completion, Vector2 _)
        {
            Color head = Color.Lerp(CoreColor, MainColor, 0.22f);
            Color mid = Color.Lerp(MainColor, SecondaryColor, completion * 0.72f);
            Color tail = Color.Lerp(SecondaryColor, Color.Transparent, Utils.GetLerpValue(0.58f, 1f, completion, true));
            head.A = 0;
            mid.A = 0;
            tail.A = 0;
            return Color.Lerp(Color.Lerp(head, mid, completion), tail, completion) * Projectile.Opacity;
        }

        private float CoreTrailWidthFunction(float completion, Vector2 _)
        {
            float width = MathHelper.Lerp(7f, 12f, ShotCompletion) * Projectile.scale * Projectile.Opacity;
            return MathF.Sin((1f - completion) * MathHelper.PiOver2) * width;
        }

        private Color CoreTrailColorFunction(float completion, Vector2 _)
        {
            Color color = Color.Lerp(CoreColor, new Color(255, 178, 70), 0.28f);
            Color tail = Color.Lerp(color, Color.Transparent, Utils.GetLerpValue(0.64f, 1f, completion, true));
            color.A = 0;
            tail.A = 0;
            return Color.Lerp(color, tail, completion) * Projectile.Opacity;
        }
    }
}
