//using CalamityMod;
//using CalamityMod.Buffs.DamageOverTime;
//using CalamityMod.Dusts;
//using CalamityMod.Enums;
//using CalamityMod.Graphics.Metaballs;
//using CalamityMod.Graphics.Primitives;
//using CalamityMod.Particles;
//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using System;
//using System.Linq;
//using Terraria;
//using Terraria.Audio;
//using Terraria.GameContent;
//using Terraria.Graphics.Shaders;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
//{
//    internal sealed class AshesofCalamity_Fire : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
//    {
//        private const float BaseLineForwardLength = 96f;
//        private const float BaseLineBackLength = 210f;
//        private const float BaseLineWidth = 46f;
//        private const float HomingRange = 6000f;
//        private const float HomingRearTolerance = -0.18f;
//        private const float HomingMaxTurnPerUpdate = 0.034f;
//        private const float HomingLeadFramesMax = 14f;
//        private const float HomingStartFrames = 30f;

//        public new string LocalizationCategory => "Projectiles.SHPC";
//        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

//        private ref float Timer => ref Projectile.localAI[0];
//        private ref float HomingTimer => ref Projectile.localAI[1];

//        private int targetIndex = -1;

//        private Vector2 FixedDirection
//        {
//            get
//            {
//                Vector2 storedDirection = new(Projectile.ai[0], Projectile.ai[1]);
//                if (storedDirection.LengthSquared() > 0.0001f)
//                    return storedDirection.SafeNormalize(Vector2.UnitX);

//                return Projectile.velocity.SafeNormalize(Vector2.UnitX);
//            }
//            set
//            {
//                Vector2 safeDirection = value.SafeNormalize(Vector2.UnitX);
//                Projectile.ai[0] = safeDirection.X;
//                Projectile.ai[1] = safeDirection.Y;
//            }
//        }

//        private float LineWidth => BaseLineWidth * Projectile.scale;

//        private float LineForwardLength => BaseLineForwardLength * Projectile.scale;

//        private float LineBackLength => BaseLineBackLength * Projectile.scale;

//        public override void SetStaticDefaults()
//        {
//            ProjectileID.Sets.TrailCacheLength[Type] = 28;
//            ProjectileID.Sets.TrailingMode[Type] = 2;
//        }

//        public override void SetDefaults()
//        {
//            Projectile.width = Projectile.height = 200;
//            Projectile.friendly = true;
//            Projectile.hostile = false;
//            Projectile.ignoreWater = true;
//            Projectile.tileCollide = false;
//            Projectile.hide = true;
//            Projectile.DamageType = DamageClass.Magic;
//            Projectile.penetrate = -1;
//            Projectile.MaxUpdates = 10;
//            Projectile.timeLeft = 105;
//            Projectile.usesLocalNPCImmunity = true;
//            Projectile.localNPCHitCooldown = 10;
//            Projectile.alpha = 255;
//        }

//        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
//        {
//            Vector2 direction = FixedDirection;
//            if (direction.LengthSquared() <= 0.0001f)
//                direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);

//            FixedDirection = direction;
//            Projectile.velocity = direction * MathHelper.Clamp(Projectile.velocity.Length(), 20f, 34f);
//            Projectile.rotation = direction.ToRotation();
//            Projectile.scale = 0.9f;
//            Projectile.Opacity = 0f;
//            Projectile.netUpdate = true;

//            float shakePower = 3.8f;
//            float distanceFactor = Utils.GetLerpValue(900f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true);
//            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, shakePower * distanceFactor);

//            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.95f, Pitch = -0.32f, PitchVariance = 0.12f }, Projectile.Center);
//            SoundEngine.PlaySound(SoundID.Item38 with { Volume = 0.52f, Pitch = -0.48f, PitchVariance = 0.08f }, Projectile.Center);

//            //if (!Main.dedServ)
//            //    SpawnMuzzleLine(direction);
//        }

//        public override void AI()
//        {
//            Timer++;

//            // 只在每个真实游戏帧加一次，避免 MaxUpdates = 10 导致追踪计时过快。
//            if (Projectile.numUpdates == 0)
//                HomingTimer++;

//            Vector2 direction = FixedDirection;
//            if (direction.LengthSquared() <= 0.0001f)
//                direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);

//            if (Projectile.owner == Main.myPlayer)
//                direction = UpdateHomingDirection(direction);

//            float speed = MathHelper.Clamp(Projectile.velocity.Length() * 1.004f + 0.03f, 20f, 36f);
//            Projectile.velocity = direction * speed;
//            Projectile.rotation = direction.ToRotation();

//            float fadeIn = Utils.GetLerpValue(0f, 7f, Timer, true);
//            float fadeOut = Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
//            Projectile.Opacity = fadeIn * fadeOut;
//            Projectile.scale = MathHelper.Lerp(0.76f, 1.16f, Utils.GetLerpValue(0f, 18f, Timer, true)) * fadeOut;
//            Projectile.alpha = (int)MathHelper.Lerp(255f, 0f, Projectile.Opacity);

//            Lighting.AddLight(Projectile.Center, new Vector3(0.82f, 0.06f, 0.02f) * (0.68f * Projectile.Opacity));

//            if (!Main.dedServ)
//            {
//                SpawnFlightEffects(direction);
//                if (Projectile.numUpdates == 0)
//                    SpawnCalamitousFireballMetaballs();
//            }
//        }

//        private Vector2 UpdateHomingDirection(Vector2 currentDirection)
//        {
//            // 生成后前 30 个真实游戏帧保持直线飞行，之后才开始追踪。
//            if (HomingTimer < HomingStartFrames)
//                return currentDirection;

//            NPC target = FindHomingTarget(currentDirection);
//            if (target == null)
//                return currentDirection;

//            float speed = MathHelper.Clamp(Projectile.velocity.Length(), 20f, 36f);
//            float distance = Projectile.Distance(target.Center);
//            float leadFrames = MathHelper.Clamp(distance / Math.Max(1f, speed), 0f, HomingLeadFramesMax);
//            Vector2 aimPoint = target.Center + target.velocity * leadFrames;
//            Vector2 desiredDirection = (aimPoint - Projectile.Center).SafeNormalize(currentDirection);
//            float closeTurnBoost = Utils.GetLerpValue(HomingRange, 120f, distance, true);
//            float maxTurn = MathHelper.Lerp(HomingMaxTurnPerUpdate * 0.45f, HomingMaxTurnPerUpdate, closeTurnBoost);
//            Vector2 adjustedDirection = currentDirection
//                .ToRotation()
//                .AngleTowards(desiredDirection.ToRotation(), maxTurn)
//                .ToRotationVector2();

//            FixedDirection = adjustedDirection;
//            if (Timer % 12f == 0f)
//                Projectile.netUpdate = true;

//            return adjustedDirection;
//        }

//        private NPC FindHomingTarget(Vector2 currentDirection)
//        {
//            if (Main.npc.IndexInRange(targetIndex) && IsValidHomingTarget(Main.npc[targetIndex], currentDirection))
//                return Main.npc[targetIndex];

//            NPC bestTarget = null;
//            float bestScore = float.MaxValue;
//            foreach (NPC npc in Main.ActiveNPCs)
//            {
//                if (!IsValidHomingTarget(npc, currentDirection))
//                    continue;

//                Vector2 toTarget = npc.Center - Projectile.Center;
//                float distance = toTarget.Length();
//                Vector2 targetDirection = toTarget.SafeNormalize(currentDirection);
//                float forwardDot = Vector2.Dot(currentDirection, targetDirection);
//                float lateralOffset = Math.Abs(currentDirection.X * toTarget.Y - currentDirection.Y * toTarget.X);
//                float score = distance + lateralOffset * 0.72f - forwardDot * 180f;

//                if (npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type])
//                    score -= 120f;

//                if (score >= bestScore)
//                    continue;

//                bestScore = score;
//                bestTarget = npc;
//            }

//            targetIndex = bestTarget?.whoAmI ?? -1;
//            return bestTarget;
//        }

//        private bool IsValidHomingTarget(NPC npc, Vector2 currentDirection)
//        {
//            if (npc == null || !npc.CanBeChasedBy(Projectile, false))
//                return false;

//            Vector2 toTarget = npc.Center - Projectile.Center;
//            float distanceSquared = toTarget.LengthSquared();
//            if (distanceSquared > HomingRange * HomingRange)
//                return false;

//            Vector2 targetDirection = toTarget.SafeNormalize(currentDirection);
//            return Vector2.Dot(currentDirection, targetDirection) >= HomingRearTolerance;
//        }

//        public override bool? CanDamage() => Timer > 2f && Projectile.Opacity > 0.12f ? null : false;

//        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
//        {
//            Vector2 direction = FixedDirection;
//            Vector2 start = Projectile.Center - direction * LineBackLength;
//            Vector2 end = Projectile.Center + direction * LineForwardLength;
//            float collisionPoint = 0f;

//            return Collision.CheckAABBvLineCollision(
//                targetHitbox.TopLeft(),
//                targetHitbox.Size(),
//                start,
//                end,
//                LineWidth,
//                ref collisionPoint);
//        }

//        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
//        {
//            float repeatedHitFalloff = Utils.Remap(Projectile.numHits, 0f, 7f, 1f, 0.64f, true);
//            modifiers.SourceDamage *= repeatedHitFalloff;
//        }

//        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
//        {
//            SoundEngine.PlaySound(SoundID.Item74, target.Center);
//            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 240);
//            target.AddBuff(BuffID.OnFire, 240);
//            target.AddBuff(BuffID.CursedInferno, 180);

//            //if (!Main.dedServ)
//            //    SpawnHitEffects(target.Center);
//        }

//        public override void OnKill(int timeLeft)
//        {
//            if (Main.dedServ)
//                return;

//            Vector2 direction = FixedDirection;
//            Color burstColor = Main.rand.NextBool() ? new Color(255, 52, 34) : new Color(160, 16, 12);

//            for (int i = 0; i < 24; i++)
//            {
//                Vector2 velocity = direction.RotatedByRandom(0.72f) * Main.rand.NextFloat(2.8f, 9.5f) + Main.rand.NextVector2Circular(1.5f, 1.5f);
//                Dust dust = Dust.NewDustPerfect(
//                    Projectile.Center,
//                    Main.rand.NextBool(4) ? DustID.Smoke : (int)CalamityDusts.Brimstone,
//                    velocity,
//                    120,
//                    Color.Lerp(burstColor, Color.Black, Main.rand.NextFloat(0.05f, 0.38f)),
//                    Main.rand.NextFloat(1.0f, 1.8f));
//                dust.noGravity = !Main.rand.NextBool(4);
//            }

//            //GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
//            //    Projectile.Center,
//            //    Vector2.Zero,
//            //    burstColor,
//            //    new Vector2(1.25f, 0.54f),
//            //    direction.ToRotation(),
//            //    0.08f,
//            //    1.85f,
//            //    18));
//        }

//        public override bool PreDraw(ref Color lightColor)
//        {
//            if (Projectile.Opacity <= 0f)
//                return false;

//            Vector2 direction = FixedDirection;
//            Vector2 start = Projectile.Center - direction * LineBackLength;
//            Vector2 end = Projectile.Center + direction * LineForwardLength;
//            Texture2D pixel = TextureAssets.MagicPixel.Value;
//            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
//            Vector2 head = end - Main.screenPosition;
//            float pulse = 0.86f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 16f + Projectile.identity * 0.17f) * 0.14f;

//            Main.spriteBatch.SetBlendState(BlendState.Additive);
//            DrawSegment(pixel, start, end, new Color(108, 0, 0, 0) * (0.52f * Projectile.Opacity), LineWidth * 1.18f * pulse);
//            DrawSegment(pixel, start + direction * 26f, end, new Color(242, 26, 18, 0) * (0.75f * Projectile.Opacity), LineWidth * 0.58f);
//            DrawSegment(pixel, start + direction * 64f, end, new Color(255, 204, 112, 0) * (0.64f * Projectile.Opacity), LineWidth * 0.22f);
//            DrawSegment(pixel, Projectile.Center - direction * 42f, end, Color.White with { A = 0 } * (0.32f * Projectile.Opacity), LineWidth * 0.08f);

//            Main.EntitySpriteDraw(
//                bloom,
//                head,
//                null,
//                new Color(255, 72, 36, 0) * (0.45f * Projectile.Opacity),
//                Projectile.rotation,
//                bloom.Size() * 0.5f,
//                new Vector2(0.22f, 0.08f) * Projectile.scale * pulse,
//                SpriteEffects.None);

//            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
//            return false;
//        }

//        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
//        {
//            Vector2[] trailPoints = BuildTrailPoints();
//            if (trailPoints.Length < 2)
//                return;

//            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
//                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));

//            PrimitiveRenderer.RenderTrail(
//                trailPoints,
//                new PrimitiveSettings(
//                    TrailWidthFunction,
//                    TrailColorFunction,
//                    TrailOffsetFunction,
//                    true,
//                    true,
//                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
//                trailPoints.Length * 3);

//            Vector2[] coreTrail = trailPoints.Take(Math.Min(12, trailPoints.Length)).ToArray();
//            if (coreTrail.Length < 2)
//                return;

//            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
//                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

//            PrimitiveRenderer.RenderTrail(
//                coreTrail,
//                new PrimitiveSettings(
//                    CoreTrailWidthFunction,
//                    CoreTrailColorFunction,
//                    TrailOffsetFunction,
//                    true,
//                    true,
//                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
//                coreTrail.Length * 3);
//        }

//        private Vector2[] BuildTrailPoints()
//        {
//            Vector2[] trailPoints = Projectile.oldPos
//                .Where(position => position != Vector2.Zero)
//                .Select(position => position + Projectile.Size * 0.5f)
//                .ToArray();

//            if (trailPoints.Length == 0)
//                return new[] { Projectile.Center - Projectile.velocity, Projectile.Center };

//            if (trailPoints[0] != Projectile.Center)
//                trailPoints = new[] { Projectile.Center }.Concat(trailPoints).ToArray();

//            return trailPoints;
//        }

//        private Vector2 TrailOffsetFunction(float completion, Vector2 _)
//        {
//            Vector2 normal = FixedDirection.RotatedBy(MathHelper.PiOver2);
//            float heatWave = (float)Math.Sin(completion * MathHelper.TwoPi * 1.5f + Main.GlobalTimeWrappedHourly * 12f) * 1.4f;
//            return normal * heatWave;
//        }

//        private float TrailWidthFunction(float completion, Vector2 _)
//        {
//            float maxWidth = Projectile.scale * 62f * Projectile.Opacity;
//            if (completion < 0.16f)
//                return MathF.Sin(completion / 0.16f * MathHelper.PiOver2) * maxWidth;

//            return Utils.Remap(completion, 0.16f, 1f, maxWidth, 0f);
//        }

//        private Color TrailColorFunction(float completion, Vector2 _)
//        {
//            Color head = Color.Lerp(new Color(255, 80, 38), new Color(255, 178, 66), 0.22f);
//            Color mid = new Color(165, 8, 8);
//            Color tail = Color.Lerp(new Color(38, 0, 0), Color.Transparent, Utils.GetLerpValue(0.58f, 1f, completion, true));
//            head.A = 0;
//            mid.A = 0;
//            tail.A = 0;
//            return Color.Lerp(Color.Lerp(head, mid, completion * 0.72f), tail, completion) * Projectile.Opacity;
//        }

//        private float CoreTrailWidthFunction(float completion, Vector2 _)
//        {
//            float maxWidth = Projectile.scale * 18f * Projectile.Opacity;
//            if (completion < 0.18f)
//                return MathF.Sin(completion / 0.18f * MathHelper.PiOver2) * maxWidth;

//            return Utils.Remap(completion, 0.18f, 1f, maxWidth, 0f);
//        }

//        private Color CoreTrailColorFunction(float completion, Vector2 _)
//        {
//            Color color = Color.Lerp(Color.White, new Color(255, 132, 58), 0.42f);
//            Color tail = Color.Lerp(color, Color.Transparent, Utils.GetLerpValue(0.7f, 1f, completion, true));
//            color.A = 0;
//            tail.A = 0;
//            return Color.Lerp(color, tail, completion) * Projectile.Opacity;
//        }

//        private void SpawnMuzzleLine(Vector2 direction)
//        {
//            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);

//            for (int i = 0; i < 48; i++)
//            {
//                float fanT = Main.rand.NextFloat(-1f, 1f);
//                float angle = MathHelper.ToRadians(34f) * fanT;
//                Vector2 spawnPosition = Projectile.Center + normal * fanT * Main.rand.NextFloat(10f, 34f) + direction * Main.rand.NextFloat(-10f, 18f);
//                Vector2 velocity = direction.RotatedBy(angle) * Main.rand.NextFloat(7f, 26f) + normal * fanT * Main.rand.NextFloat(1.2f, 5.4f);
//                Dust dust = Dust.NewDustPerfect(
//                    spawnPosition,
//                    Main.rand.NextBool(4) ? DustID.Smoke : (int)CalamityDusts.Brimstone,
//                    velocity,
//                    120,
//                    Main.rand.NextBool(5) ? new Color(36, 0, 0) : Color.Lerp(new Color(255, 42, 22), new Color(255, 174, 70), Main.rand.NextFloat(0.1f, 0.42f)),
//                    Main.rand.NextFloat(1.45f, 2.85f));
//                dust.noGravity = !Main.rand.NextBool(5);
//                dust.fadeIn = Main.rand.NextFloat(0.15f, 0.45f);
//            }

//            for (int i = 0; i < 26; i++)
//            {
//                float fanT = Main.rand.NextFloat(-1f, 1f);
//                float angle = MathHelper.ToRadians(25f) * fanT;
//                Vector2 sparkPosition = Projectile.Center + normal * fanT * Main.rand.NextFloat(4f, 30f) + direction * Main.rand.NextFloat(-4f, 24f);
//                Vector2 velocity = direction.RotatedBy(angle) * Main.rand.NextFloat(10f, 30f) + normal * fanT * Main.rand.NextFloat(1.4f, 5f);
//                GeneralParticleHandler.SpawnParticle(new CustomSpark(
//                    sparkPosition,
//                    velocity,
//                    Main.rand.NextBool(3) ? "CalamityMod/Particles/VerticalSmear" : "CalamityMod/Particles/SmallBloom",
//                    false,
//                    Main.rand.Next(12, 18),
//                    Main.rand.NextFloat(0.2f, 0.42f),
//                    Main.rand.NextBool(5) ? Color.White : Color.Lerp(new Color(255, 48, 22), new Color(255, 184, 84), Main.rand.NextFloat(0.16f, 0.55f)),
//                    new Vector2(Main.rand.NextFloat(1.5f, 2.8f), Main.rand.NextFloat(0.55f, 1.15f)),
//                    true,
//                    false,
//                    0f,
//                    false,
//                    false,
//                    0.64f));
//            }
//        }

//        private void SpawnFlightEffects(Vector2 direction)
//        {
//            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
//            Color mainColor = Color.Lerp(new Color(255, 42, 20), new Color(255, 126, 46), Main.rand.NextFloat(0.1f, 0.45f));
//            Color darkColor = Main.rand.NextBool() ? new Color(42, 0, 0) : new Color(82, 4, 2);
//            Color emberColor = Color.Lerp(mainColor, new Color(255, 204, 116), Main.rand.NextFloat(0.12f, 0.5f));
//            float fanHalfAngle = MathHelper.ToRadians(44f);
//            float bodyOpacity = Projectile.Opacity;

//            for (int i = 0; i < 22; i++)
//            {
//                float fanT = Main.rand.NextFloat(-1f, 1f);
//                float curvedT = (float)Math.Sin(fanT * MathHelper.PiOver2) * 0.74f;
//                float lineT = Main.rand.NextFloat(-0.82f, 1f);
//                Vector2 linePoint = Projectile.Center
//                    + direction * MathHelper.Lerp(-LineBackLength, LineForwardLength, (lineT + 1f) * 0.5f)
//                    + normal * fanT * Main.rand.NextFloat(LineWidth * 0.1f, LineWidth * 0.72f);
//                Vector2 velocity = direction.RotatedBy(fanHalfAngle * curvedT) * Main.rand.NextFloat(2.6f, 13f)
//                    + normal * fanT * Main.rand.NextFloat(0.8f, 4.8f)
//                    - direction * Main.rand.NextFloat(0.2f, 1.6f);

//                Dust dust = Dust.NewDustPerfect(
//                    linePoint,
//                    Main.rand.NextBool(5) ? DustID.Smoke : (int)CalamityDusts.Brimstone,
//                    velocity,
//                    135,
//                    Main.rand.NextBool(4) ? darkColor : Color.Lerp(mainColor, emberColor, Main.rand.NextFloat(0.05f, 0.42f)),
//                    Main.rand.NextFloat(1.15f, 2.25f) * Projectile.scale * MathHelper.Lerp(0.7f, 1.15f, bodyOpacity));
//                dust.noGravity = !Main.rand.NextBool(5);
//                dust.alpha = Main.rand.Next(30, 120);
//                dust.fadeIn = Main.rand.NextFloat(0.12f, 0.5f);
//            }

//            //for (int i = 0; i < 7; i++)
//            //{
//            //    float fanT = Main.rand.NextFloat(-1f, 1f);
//            //    float lineT = Main.rand.NextFloat(-0.75f, 1f);
//            //    Vector2 glowPosition = Projectile.Center
//            //        + direction * MathHelper.Lerp(-LineBackLength * 0.68f, LineForwardLength * 0.94f, (lineT + 1f) * 0.5f)
//            //        + normal * fanT * Main.rand.NextFloat(4f, LineWidth * 0.58f);
//            //    Vector2 glowVelocity = direction.RotatedBy(fanHalfAngle * fanT * 0.54f) * Main.rand.NextFloat(6.5f, 18f)
//            //        + normal * fanT * Main.rand.NextFloat(1.1f, 4.6f)
//            //        - direction * Main.rand.NextFloat(0.1f, 1.1f);

//            //    GlowSparkParticle coreSpark = new GlowSparkParticle(
//            //        glowPosition,
//            //        glowVelocity,
//            //        false,
//            //        Main.rand.Next(8, 14),
//            //        Main.rand.NextFloat(0.018f, 0.034f) * Projectile.scale,
//            //        Main.rand.NextBool(3) ? emberColor : mainColor,
//            //        new Vector2(Main.rand.NextFloat(2.25f, 3.9f), Main.rand.NextFloat(0.62f, 1.02f)),
//            //        true,
//            //        false,
//            //        1.12f);
//            //    GeneralParticleHandler.SpawnParticle(coreSpark);
//            //}

//            //for (int i = 0; i < 5; i++)
//            //{
//            //    float fanT = Main.rand.NextFloat(-1f, 1f);
//            //    Vector2 sparkPosition = Projectile.Center
//            //        + direction * Main.rand.NextFloat(-LineBackLength * 0.55f, LineForwardLength * 0.9f)
//            //        + normal * fanT * Main.rand.NextFloat(8f, LineWidth * 0.72f);
//            //    Vector2 sparkVelocity = direction.RotatedBy(fanHalfAngle * fanT * 0.8f) * Main.rand.NextFloat(4.2f, 13f)
//            //        + normal * fanT * Main.rand.NextFloat(1f, 4.8f);

//            //    SparkParticle emberSpark = new SparkParticle(
//            //        sparkPosition,
//            //        sparkVelocity,
//            //        false,
//            //        Main.rand.Next(13, 23),
//            //        Main.rand.NextFloat(0.58f, 1.05f) * Projectile.scale,
//            //        Main.rand.NextBool(4) ? emberColor : Color.Lerp(darkColor, mainColor, 0.72f));
//            //    GeneralParticleHandler.SpawnParticle(emberSpark);
//            //}

//            for (int i = 0; i < 3; i++)
//            {
//                float fanT = Main.rand.NextFloat(-1f, 1f);
//                Vector2 orbPosition = Projectile.Center
//                    + direction * Main.rand.NextFloat(-LineBackLength * 0.46f, LineForwardLength * 0.72f)
//                    + normal * fanT * Main.rand.NextFloat(2f, LineWidth * 0.42f);
//                Vector2 orbVelocity = direction * Main.rand.NextFloat(0.25f, 1.15f)
//                    + normal * fanT * Main.rand.NextFloat(0.25f, 1.2f);

//                GlowOrbParticle heatOrb = new GlowOrbParticle(
//                    orbPosition,
//                    orbVelocity,
//                    false,
//                    Main.rand.Next(8, 13),
//                    Main.rand.NextFloat(0.42f, 0.74f) * Projectile.scale,
//                    Color.Lerp(mainColor, emberColor, Main.rand.NextFloat(0.28f, 0.68f)),
//                    true,
//                    false,
//                    true);
//                GeneralParticleHandler.SpawnParticle(heatOrb);
//            }

//            for (int i = 0; i < 2; i++)
//            {
//                float fanT = Main.rand.NextFloat(-1f, 1f);
//                Vector2 critPosition = Projectile.Center
//                    + direction * Main.rand.NextFloat(-LineBackLength * 0.22f, LineForwardLength * 0.86f)
//                    + normal * fanT * Main.rand.NextFloat(6f, LineWidth * 0.5f);
//                Vector2 critVelocity = direction.RotatedBy(fanHalfAngle * fanT * 0.66f) * Main.rand.NextFloat(3.6f, 8.8f)
//                    + normal * fanT * Main.rand.NextFloat(0.6f, 2.6f);

//                CritSpark critSpark = new CritSpark(
//                    critPosition,
//                    critVelocity,
//                    Color.White,
//                    Main.rand.NextBool() ? emberColor : mainColor,
//                    Main.rand.NextFloat(0.72f, 1.08f) * Projectile.scale,
//                    Main.rand.Next(12, 18),
//                    0.12f,
//                    1.24f);
//                GeneralParticleHandler.SpawnParticle(critSpark);
//            }

//            for (int i = 0; i < 3; i++)
//            {
//                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
//                    Projectile.Center - direction * Main.rand.NextFloat(14f, LineBackLength * 0.72f) + normal * Main.rand.NextFloat(-LineWidth * 0.7f, LineWidth * 0.7f),
//                    -direction * Main.rand.NextFloat(0.2f, 1.4f) + normal * Main.rand.NextFloat(-0.9f, 0.9f) + Main.rand.NextVector2Circular(0.5f, 0.5f),
//                    Main.rand.NextBool() ? new Color(60, 20, 18) : new Color(26, 10, 10),
//                    Main.rand.Next(18, 34),
//                    Main.rand.NextFloat(0.34f, 0.76f) * Projectile.scale,
//                    0.48f,
//                    Main.rand.NextFloat(-0.05f, 0.05f),
//                    true));
//            }
//        }

//        private void SpawnCalamitousFireballMetaballs()
//        {
//            CalamitasMetaball.SpawnParticle(
//                Projectile.Center + Projectile.velocity,
//                Main.rand.NextVector2Circular(2f, 2f),
//                64f * Projectile.scale);
//        }

//        private void SpawnHitEffects(Vector2 hitCenter)
//        {
//            Vector2 direction = FixedDirection;
//            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);

//            for (int i = 0; i < 8; i++)
//            {
//                Vector2 velocity = direction.RotatedByRandom(0.48f) * Main.rand.NextFloat(4f, 12f) + normal * Main.rand.NextFloat(-3.2f, 3.2f);
//                GeneralParticleHandler.SpawnParticle(new CustomSpark(
//                    hitCenter + Main.rand.NextVector2Circular(14f, 14f),
//                    velocity,
//                    "CalamityMod/Particles/SmallBloom",
//                    false,
//                    Main.rand.Next(12, 20),
//                    Main.rand.NextFloat(0.18f, 0.36f),
//                    Main.rand.NextBool(3) ? Color.White : Color.OrangeRed,
//                    new Vector2(Main.rand.NextFloat(1.2f, 2.4f), Main.rand.NextFloat(0.42f, 0.86f)),
//                    true,
//                    false,
//                    0f,
//                    false,
//                    false,
//                    0.65f));
//            }

//            for (int i = 0; i < 12; i++)
//            {
//                Dust dust = Dust.NewDustPerfect(
//                    hitCenter + Main.rand.NextVector2Circular(16f, 16f),
//                    Main.rand.NextBool(5) ? DustID.Smoke : (int)CalamityDusts.Brimstone,
//                    direction.RotatedByRandom(0.8f) * Main.rand.NextFloat(1.8f, 7.4f),
//                    120,
//                    Main.rand.NextBool() ? Color.Crimson : Color.OrangeRed,
//                    Main.rand.NextFloat(1f, 1.75f));
//                dust.noGravity = !Main.rand.NextBool(4);
//            }
//        }

//        private static void DrawSegment(Texture2D pixel, Vector2 start, Vector2 end, Color color, float width)
//        {
//            Vector2 edge = end - start;
//            if (edge.LengthSquared() <= 0.001f)
//                return;

//            Main.EntitySpriteDraw(
//                pixel,
//                start - Main.screenPosition,
//                new Rectangle(0, 0, 1, 1),
//                color,
//                edge.ToRotation(),
//                new Vector2(0f, 0.5f),
//                new Vector2(edge.Length(), width),
//                SpriteEffects.None);
//        }
//    }
//}