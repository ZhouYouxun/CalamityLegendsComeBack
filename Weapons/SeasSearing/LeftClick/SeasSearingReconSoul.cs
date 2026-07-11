using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal sealed class SeasSearingReconSoul : ModProjectile, ILocalizedModType
    {
        private const int Phase1End = 12;
        private const int Phase2End = 42;
        private const float HomingRange = 920f;
        private const int TrailLength = 26;

        private static readonly BlossomFluxChloroplastPresetType ReconPreset = BlossomFluxChloroplastPresetType.Chlo_CDetec;
        private static readonly float[] PhaseOffsets = { 0f, 2.094f, 4.189f };

        private ref float TargetIndex => ref Projectile.ai[0];
        private ref float Variant => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];

        private int VariantIndex => (int)MathHelper.Clamp(Variant, 0f, 2f);
        private float PhaseOffset => PhaseOffsets[VariantIndex];
        private bool InPhase1 => Timer <= Phase1End;
        private bool InPhase2 => Timer > Phase1End && Timer <= Phase2End;
        private bool InPhase3 => Timer > Phase2End;
        private Color MainColor => BFArrowCommon.GetPresetColor(ReconPreset);
        private Color AccentColor => BFArrowCommon.GetPresetAccentColor(ReconPreset);

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 14;
        }

        public override void AI()
        {
            Timer++;
            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Timer, true) *
                Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);

            NPC target = FindTarget();
            UpdateFlight(target);

            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation();

            Lighting.AddLight(Projectile.Center, MainColor.ToVector3() * (InPhase3 ? 0.42f : 0.25f));

            if (!Main.dedServ)
                EmitReconTrail(target);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer == Projectile.owner)
                Main.player[Projectile.owner].GetModPlayer<SeasSearingPlayer>().OnHitWithSeasSearing();

            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, 3, 8 * 60);
            target.AddBuff(BuffID.Venom, 180);
            SpawnImpactFX(target.Center);
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.22f, Pitch = 0.45f }, target.Center);
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.numHits <= 0)
                SpawnImpactFX(Projectile.Center, false);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            NPC target = FindTarget();
            Vector2 center = Projectile.Center - Main.screenPosition;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            float opacity = Projectile.Opacity;

            BeginAdditive();
            DrawScanRing(center, target, opacity);
            DrawTargetRay(center, target, opacity);
            DrawHelixTrail(center, forward, opacity);
            DrawAfterimages(opacity);
            DrawScoutWings(center, forward, opacity);
            DrawCoreGlow(center, forward, opacity);
            EndAdditive();

            return false;
        }

        private void UpdateFlight(NPC target)
        {
            if (InPhase1)
            {
                float openingProgress = Timer / Phase1End;
                Projectile.velocity *= MathHelper.Lerp(0.998f, 0.9994f, openingProgress);
                Projectile.velocity = Projectile.velocity.RotatedBy(Math.Sin((Timer + Projectile.identity) * 0.22f + PhaseOffset) * 0.013f);
                ClampSpeed(12f);
                return;
            }

            if (target is null)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(Math.Sin((Timer + Projectile.identity) * 0.15f + PhaseOffset) * 0.018f);
                LerpSpeedTo(InPhase3 ? 22f : 16f, 0.08f);
                return;
            }

            float progress = InPhase2 ? Utils.GetLerpValue(Phase1End, Phase2End, Timer, true) : 1f;
            float distance = Vector2.Distance(Projectile.Center, target.Center);
            float close = Utils.GetLerpValue(260f, 70f, distance, true);
            float speed = MathHelper.Lerp(15f, MathHelper.Lerp(24f, 29f, close), progress);
            float inertia = MathHelper.Lerp(34f, MathHelper.Lerp(12f, 4.2f, close), progress);
            float prediction = MathHelper.Lerp(12f, 2f, close);
            Vector2 aimPoint = target.Center + target.velocity * prediction;
            Vector2 desired = (aimPoint - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY)) * speed;

            Projectile.velocity = (Projectile.velocity * (inertia - 1f) + desired) / inertia;
            ClampSpeed(speed);
        }

        private NPC FindTarget()
        {
            if (Main.npc.IndexInRange((int)TargetIndex))
            {
                NPC locked = Main.npc[(int)TargetIndex];
                if (locked.active && locked.CanBeChasedBy(Projectile))
                    return locked;
            }

            NPC best = null;
            float bestDistance = HomingRange * HomingRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = npc;
                }
            }

            if (best is not null && Main.myPlayer == Projectile.owner)
            {
                TargetIndex = best.whoAmI;
                Projectile.netUpdate = true;
            }

            return best;
        }

        private void EmitReconTrail(NPC target)
        {
            if (Main.GameUpdateCount % (InPhase3 ? 2 : 3) == 0)
                BFArrowCommon.EmitPresetTrail(Projectile, ReconPreset, InPhase3 ? 1.05f : 0.8f);

            if (Main.GameUpdateCount % 9 == 0)
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(2f, 10f),
                    -Projectile.velocity * 0.035f + Main.rand.NextVector2Circular(0.25f, 0.25f),
                    false,
                    Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.12f, 0.22f),
                    Color.Lerp(MainColor, AccentColor, Main.rand.NextFloat()),
                    true,
                    false,
                    true));
            }

            if (target is not null && InPhase3 && Main.GameUpdateCount % 15 == 0)
            {
                Vector2 toTarget = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center,
                    toTarget * 0.42f,
                    AccentColor with { A = 0 },
                    new Vector2(0.38f, 1.1f),
                    toTarget.ToRotation(),
                    0.08f,
                    0.018f,
                    8));
            }
        }

        private void DrawScanRing(Vector2 center, NPC target, float opacity)
        {
            if (Timer <= Phase1End)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float time = Main.GlobalTimeWrappedHourly;
            float radius = (target is null ? 19f : 28f) + (float)Math.Sin(time * 3.6f + PhaseOffset) * 2.5f;
            float alpha = (target is null ? 0.20f : 0.44f) * opacity;
            int segments = target is null ? 8 : 14;

            for (int i = 0; i < segments; i++)
            {
                float angle = MathHelper.TwoPi * i / segments + time * 1.35f + PhaseOffset;
                Vector2 drawPosition = center + angle.ToRotationVector2() * radius;
                Color color = Color.Lerp(MainColor, AccentColor, i / (float)segments) with { A = 0 };
                Main.EntitySpriteDraw(bloom, drawPosition, null, color * alpha, 0f,
                    bloom.Size() * 0.5f, 0.025f, SpriteEffects.None, 0);
            }
        }

        private void DrawTargetRay(Vector2 center, NPC target, float opacity)
        {
            if (!InPhase3 || target is null)
                return;

            Vector2 targetScreen = target.Center - Main.screenPosition;
            Vector2 ray = targetScreen - center;
            float distance = ray.Length();
            if (distance > 430f)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 direction = ray.SafeNormalize(Vector2.UnitY);
            int dots = Math.Max(4, (int)(distance / 16f));
            float alpha = Utils.GetLerpValue(430f, 140f, distance, true) * 0.34f * opacity;

            for (int i = 1; i < dots; i++)
            {
                if (i % 3 == 0)
                    continue;

                float progress = i / (float)dots;
                Vector2 drawPosition = center + direction * distance * progress;
                Color color = Color.Lerp(MainColor, AccentColor, progress) with { A = 0 };
                Main.EntitySpriteDraw(bloom, drawPosition, null, color * alpha * (1f - progress * 0.5f),
                    0f, bloom.Size() * 0.5f, MathHelper.Lerp(0.026f, 0.011f, progress), SpriteEffects.None, 0);
            }
        }

        private void DrawHelixTrail(Vector2 center, Vector2 forward, float opacity)
        {
            Texture2D water = ModContent.Request<Texture2D>("CalamityMod/Particles/WaterFlavored").Value;
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float time = Main.GlobalTimeWrappedHourly * 2.8f + PhaseOffset;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float angle = time - i * 0.22f;
                Vector2 offsetA = side.RotatedBy(angle) * (7f * completion);
                Vector2 offsetB = side.RotatedBy(angle + MathHelper.Pi) * (7f * completion);
                Color colorA = (MainColor with { A = 0 }) * (0.34f * completion * opacity);
                Color colorB = (AccentColor with { A = 0 }) * (0.20f * completion * opacity);

                Main.EntitySpriteDraw(spark, trailPosition + offsetA, null, colorA,
                    forward.ToRotation() + angle + MathHelper.PiOver2, spark.Size() * 0.5f,
                    new Vector2(0.016f, 0.09f * completion), SpriteEffects.None, 0);
                Main.EntitySpriteDraw(spark, trailPosition + offsetB, null, colorB,
                    forward.ToRotation() + angle - MathHelper.PiOver2, spark.Size() * 0.5f,
                    new Vector2(0.012f, 0.07f * completion), SpriteEffects.None, 0);
            }

            for (int i = 0; i < 5; i++)
            {
                float progress = i / 4f;
                float angle = time + MathHelper.TwoPi * i / 5f;
                Vector2 spiral = side.RotatedBy(angle) * MathHelper.Lerp(10f, 2f, progress);
                Vector2 back = -forward * MathHelper.Lerp(2f, 22f, progress);
                Color color = Color.Lerp(MainColor, AccentColor, progress * 0.45f) with { A = 0 };

                Main.EntitySpriteDraw(water, center + spiral + back, null, color * (0.38f * (1f - progress * 0.6f) * opacity),
                    forward.ToRotation() - MathHelper.PiOver2, water.Size() * 0.5f,
                    new Vector2(0.12f, MathHelper.Lerp(0.48f, 0.22f, progress)), SpriteEffects.None, 0);
            }
        }

        private void DrawAfterimages(float opacity)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float phaseBrightness = InPhase3 ? 0.95f : 0.55f;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                if (completion < 0.05f)
                    continue;

                Vector2 position = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(bloom, position, null,
                    (MainColor with { A = 0 }) * (0.22f * completion * phaseBrightness * opacity),
                    0f, bloom.Size() * 0.5f, MathHelper.Lerp(0.044f, 0.012f, 1f - completion), SpriteEffects.None, 0);
            }
        }

        private void DrawScoutWings(Vector2 center, Vector2 forward, float opacity)
        {
            float fade = Utils.GetLerpValue(0f, Phase1End, Timer, true) * opacity;
            if (fade < 0.01f)
                return;

            Texture2D optic = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/muzzle_02").Value;
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            DrawOneScoutWing(optic, center, forward, side, 1f, fade);
            DrawOneScoutWing(optic, center, forward, -side, -1f, fade);
        }

        private void DrawOneScoutWing(Texture2D texture, Vector2 center, Vector2 forward, Vector2 side, float sign, float fade)
        {
            Vector2 origin = new(texture.Width * 0.5f, texture.Height * 0.84f);
            Vector2 basePosition = center + side * 3.2f - forward * 1.2f;
            float rotation = side.ToRotation() + MathHelper.PiOver2;
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Timer * 0.36f + sign * 0.82f + Projectile.identity * 0.13f);
            float open = InPhase3 ? 0.38f : (InPhase2 ? 0.22f : 0.10f);
            float flutter = pulse * open * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.66f + sign * 0.84f + PhaseOffset);

            for (int i = 0; i < 3; i++)
            {
                float layerOffset = i - 1f;
                Color color = Color.Lerp(MainColor, Color.Lerp(AccentColor, Color.White, 0.26f), 0.28f + 0.18f * i) with { A = 0 };
                Main.EntitySpriteDraw(texture, basePosition + side * layerOffset * 1.45f, null,
                    color * ((i == 1 ? 0.40f : 0.14f) * fade),
                    rotation + flutter + layerOffset * 0.18f,
                    origin, new Vector2(0.052f, 0.078f + i * 0.005f), SpriteEffects.None, 0);
            }
        }

        private void DrawCoreGlow(Vector2 center, Vector2 forward, float opacity)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7.4f + Projectile.identity * 0.3f + PhaseOffset);

            Main.EntitySpriteDraw(bloom, center, null, (MainColor with { A = 0 }) * (0.45f * pulse * opacity),
                0f, bloom.Size() * 0.5f, 0.24f * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, center, null, (Color.Lerp(AccentColor, Color.White, 0.46f) with { A = 0 }) * (0.62f * opacity),
                0f, bloom.Size() * 0.5f, 0.10f * pulse, SpriteEffects.None, 0);

            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float orbitTime = Main.GlobalTimeWrappedHourly * 3.5f + PhaseOffset;
            for (int i = 0; i < 7; i++)
            {
                float angle = orbitTime + MathHelper.TwoPi * i / 7f;
                Vector2 orbitOffset = side * ((float)Math.Sin(angle) * 3.2f) + forward * ((float)Math.Cos(angle) * 2.4f);
                Main.EntitySpriteDraw(spark, center + orbitOffset, null,
                    (Color.Lerp(MainColor, AccentColor, 0.36f) with { A = 0 }) * (0.22f * opacity),
                    Projectile.rotation + MathHelper.PiOver2,
                    spark.Size() * 0.5f,
                    new Vector2(0.026f, 0.052f) * Projectile.scale,
                    SpriteEffects.None, 0);
            }

            BFArrowCommon.DrawCentredRotatingStar(Projectile, ReconPreset, isLeftClick: true, manageBlendState: false);
        }

        private void SpawnImpactFX(Vector2 center, bool strong = true)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                center,
                Vector2.Zero,
                Color.Lerp(MainColor, Color.White, 0.24f) * (strong ? 0.58f : 0.34f),
                strong ? 0.32f : 0.22f,
                strong ? 10 : 7));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Projectile.velocity.SafeNormalize(Vector2.UnitY) * 0.45f,
                AccentColor with { A = 0 },
                new Vector2(0.62f, 1.6f),
                Projectile.velocity.ToRotation(),
                strong ? 0.12f : 0.08f,
                0.024f,
                strong ? 10 : 7));

            BFArrowCommon.EmitPresetBurst(Projectile, ReconPreset, strong ? 10 : 5, 0.6f, strong ? 3.2f : 1.9f, 0.55f, 0.9f);
        }

        private void ClampSpeed(float max)
        {
            if (Projectile.velocity.LengthSquared() > max * max)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * max;
        }

        private void LerpSpeedTo(float targetSpeed, float rate)
        {
            float currentSpeed = Projectile.velocity.Length();
            if (currentSpeed < 0.01f)
                return;

            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) *
                MathHelper.Lerp(currentSpeed, targetSpeed, rate);
        }

        private static void BeginAdditive()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);
        }

        private static void EndAdditive()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
