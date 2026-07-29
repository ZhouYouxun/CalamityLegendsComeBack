using System;
using System.Linq;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    internal class BFArrow_ABreak : ModProjectile, IPixelatedPrimitiveRenderer
    {
        private const float SlowdownStartFrame = 52f;
        private const float SlowdownDuration = 56f;
        private const float PostImpactSlowdownStartFrame = 36f;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/RightClick/ABreak/BFArrow_ABreak";
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        private ref float ConfiguredPenetrate => ref Projectile.ai[0];
        private ref float IgnorePenetrationDamageFalloff => ref Projectile.ai[1];
        private ref float FlightTimer => ref Projectile.localAI[0];
        private ref float BounceCounter => ref Projectile.localAI[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 16;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            BFArrowCommon.SetBaseArrowDefaults(Projectile, width: 14, height: 34, timeLeft: 260, penetrate: 5, extraUpdates: 1, tileCollide: true);
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (ConfiguredPenetrate != 0f)
            {
                Projectile.penetrate = (int)ConfiguredPenetrate;
                Vector2 center = Projectile.Center;
                Projectile.width = 32;
                Projectile.height = 32;
                Projectile.Center = center;
                Projectile.velocity *= 1.14f;
            }
        }

        public override bool? CanDamage() => null;

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.DefenseEffectiveness *= 0f;
            float calamityDamageReduction = MathHelper.Clamp(target.Calamity().DR, 0f, 0.95f);
            if (calamityDamageReduction > 0f)
                modifiers.FinalDamage /= 1.65f - calamityDamageReduction;

            if (target.GetGlobalNPC<BFArrow_CDetecNPC>().IsPriorityMarkedBy(Projectile.owner))
                modifiers.FinalDamage *= 1.9f;
        }

        public override void AI()
        {
            FlightTimer++;
            float glowStrength = Utils.GetLerpValue(0f, 36f, FlightTimer, true);
            Lighting.AddLight(Projectile.Center, BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak).ToVector3() * (0.18f + 0.32f * glowStrength));
            BFArrowCommon.EmitPresetTrail(Projectile, BlossomFluxChloroplastPresetType.Chlo_ABreak, 0.74f);
            EmitBreakthroughFlightFX(glowStrength);

            bool chargedShot = ConfiguredPenetrate != 0f;
            AccelerateStraightFlight(chargedShot ? 1.018f : 1.01f, chargedShot ? 52f : 36f);
            BFArrowCommon.FaceForward(Projectile);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(BlossomFluxSounds.RightBreakthroughTileCollide, Projectile.Center);
            if (ConfiguredPenetrate != 0f)
            {
                SpawnBreakthroughImpactFX(Projectile.Center, 1.25f);
                Projectile.Kill();
                return false;
            }

            if (BFArrowCommon.Bounce(Projectile, oldVelocity, ref BounceCounter, 3, 0.98f))
                return true;

            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_ABreak, 10, 0.9f, 2.8f, 0.8f, 1.25f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_ABreak, 10, 0.9f, 3.2f, 0.8f, 1.18f);
            SpawnBreakthroughVanishFX(Projectile.Center, ConfiguredPenetrate != 0f ? 1.15f : 0.82f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.localNPCImmunity[target.whoAmI] = 16;
            Projectile.netUpdate = true;
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_ABreak, 12, 0.9f, 3.4f, 0.8f, 1.2f);
            SpawnBreakthroughImpactFX(Projectile.Center, 1.1f);
            SoundEngine.PlaySound(BlossomFluxSounds.RightBreakthroughProjHit, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            DrawArrowHelix(Projectile.Center - Main.screenPosition, forward);
            BFArrowCommon.DrawPresetArrow(Projectile, lightColor, BlossomFluxChloroplastPresetType.Chlo_ABreak);
            return false;
        }

        // ─── 双层拖尾 ────────────────────────────────────────────────────────
        private Vector2[] BuildTrailPoints()
        {
            Vector2[] pts = Projectile.oldPos
                .Where(p => p != Vector2.Zero)
                .Select(p => p + Projectile.Size * 0.5f)
                .ToArray();
            if (pts.Length == 0)
                return new[] { Projectile.Center - Projectile.velocity, Projectile.Center };
            if (pts[0] != Projectile.Center)
                pts = new[] { Projectile.Center }.Concat(pts).ToArray();
            return pts;
        }

        private float OuterWidthFunc(float t, Vector2 _)
        {
            float max = Projectile.scale * 20f;
            return t < 0.16f
                ? MathF.Sin(t / 0.16f * MathHelper.PiOver2) * max
                : Utils.Remap(t, 0.16f, 1f, max, 0f);
        }

        private Color OuterColorFunc(float t, Vector2 _)
        {
            Color c = Color.Lerp(new Color(255, 215, 80), new Color(200, 130, 20), t * 0.65f) * Projectile.Opacity;
            c = Color.Lerp(c, Color.Transparent, Utils.GetLerpValue(0.70f, 1f, t, true));
            c.A = 0;
            return c;
        }

        private float CoreWidthFunc(float t, Vector2 _)
        {
            float max = Projectile.scale * 10f;
            return t < 0.16f
                ? MathF.Sin(t / 0.16f * MathHelper.PiOver2) * max
                : Utils.Remap(t, 0.16f, 1f, max, 0f);
        }

        private Color CoreColorFunc(float t, Vector2 _)
        {
            Color c = Color.Lerp(Color.White, new Color(255, 245, 170), t * 0.5f) * Projectile.Opacity;
            c = Color.Lerp(c, Color.Transparent, Utils.GetLerpValue(0.72f, 1f, t, true));
            c.A = 0;
            return c;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] pts = BuildTrailPoints();
            if (pts.Length < 2) return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(pts,
                new PrimitiveSettings(OuterWidthFunc, OuterColorFunc, (_, _) => Vector2.Zero, true, true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                pts.Length * 2);

            Vector2[] core = pts.Take(Math.Min(9, pts.Length)).ToArray();
            if (core.Length < 2) return;
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            PrimitiveRenderer.RenderTrail(core,
                new PrimitiveSettings(CoreWidthFunc, CoreColorFunc, (_, _) => Vector2.Zero, true, true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                core.Length * 2);
        }

        // ─── 双股逆旋螺旋 ─────────────────────────────────────────────────────
        private void DrawArrowHelix(Vector2 drawPos, Vector2 forward)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Particles/WaterFlavored").Value;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_ABreak);
            float time = Main.GlobalTimeWrappedHourly * 4.2f + Projectile.identity * 0.31f;
            const int len = 10;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int strand = 0; strand < 2; strand++)
            {
                float dir = strand == 0 ? 1f : -1f;
                Color sc = strand == 0 ? mainColor : accentColor;
                for (int i = 0; i < len; i++)
                {
                    float t = i / (float)(len - 1);
                    float angle = time * dir - t * 3.1f + MathHelper.Pi * strand;
                    float radius = MathHelper.Lerp(14f, 5f, t);
                    Vector2 off = right.RotatedBy(angle) * radius - forward * MathHelper.Lerp(2f, 36f, t);
                    float opacity = MathHelper.Lerp(0.60f, 0.05f, t) * Projectile.Opacity;
                    Main.EntitySpriteDraw(tex, drawPos + off, null, sc with { A = 0 } * opacity,
                        forward.ToRotation() - MathHelper.PiOver2, tex.Size() * 0.5f,
                        new Vector2(0.20f, MathHelper.Lerp(0.88f, 0.30f, t)) * Projectile.scale,
                        SpriteEffects.None, 0);
                }
            }
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        // ─── 原有逻辑（保留） ─────────────────────────────────────────────────
        private float GetSlowdownProgress()
        {
            float startFrame = IgnorePenetrationDamageFalloff == 1f ? PostImpactSlowdownStartFrame : SlowdownStartFrame;
            return Utils.GetLerpValue(startFrame, startFrame + SlowdownDuration, FlightTimer, true);
        }

        private void UpdateSlowdown(float slowdownProgress)
        {
            float drag = MathHelper.Lerp(0.965f, 0.875f, slowdownProgress);
            Projectile.velocity *= drag;
            Projectile.Opacity = MathF.Pow(1f - slowdownProgress, 1.45f);

            if (Projectile.velocity.LengthSquared() < 1.8f * 1.8f || slowdownProgress >= 1f)
            {
                Projectile.Kill();
                return;
            }

            if (Main.dedServ || !Projectile.FinalExtraUpdate() || (int)FlightTimer % 4 != 0)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak);
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                DustID.TerraBlade,
                -Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.45f) * Main.rand.NextFloat(0.45f, 1.25f),
                110,
                Color.Lerp(mainColor, Color.White, 0.22f),
                Main.rand.NextFloat(0.75f, 1.05f) * MathHelper.Lerp(0.9f, 0.35f, slowdownProgress));
            dust.noGravity = true;
        }

        private void EmitBreakthroughFlightFX(float glowStrength)
        {
            if (Main.dedServ || glowStrength <= 0.02f || !Projectile.FinalExtraUpdate())
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);

            if ((int)FlightTimer % 2 == 0)
            {
                DirectionalPulseRing pulse = new(
                    Projectile.Center + direction * 8f,
                    Projectile.velocity * 0.05f,
                    Color.Lerp(BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak), Color.White, 0.2f),
                    new Vector2(0.28f, 0.68f),
                    direction.ToRotation(),
                    0.095f * glowStrength,
                    0.04f,
                    10);
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 sparkVelocity = direction * Main.rand.NextFloat(4.5f, 8.5f) + normal * Main.rand.NextFloat(-1.8f, 1.8f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(4f, 16f) + normal * Main.rand.NextFloat(-5f, 5f),
                    sparkVelocity,
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.28f, 0.52f) * glowStrength,
                    Main.rand.NextBool() ? Color.Gold : Color.Lerp(Color.Yellow, Color.White, 0.35f)));
            }

            if (Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(3f, 12f) + normal * Main.rand.NextFloat(-3f, 3f),
                    -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.2f, 0.2f),
                    false,
                    Main.rand.Next(7, 11),
                    Main.rand.NextFloat(0.18f, 0.3f) * glowStrength,
                    Color.Lerp(Color.Gold, Color.White, 0.25f),
                    true,
                    false,
                    true));
            }

            GenericSparkle edgeSpark = new(
                Projectile.Center + normal * Main.rand.NextFloat(-5f, 5f),
                Projectile.velocity * 0.04f,
                Color.White,
                BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_ABreak),
                0.34f * glowStrength,
                7,
                0f,
                0.46f * glowStrength);
            GeneralParticleHandler.SpawnParticle(edgeSpark);

            EmitBreakthroughOrbitFX(glowStrength);
        }

        // 三臂旋转 CritSpark 光环 + SquishyLight + GenericSparkle 前锋闪
        private void EmitBreakthroughOrbitFX(float glowStrength)
        {
            float time = Main.GlobalTimeWrappedHourly * 7.8f + Projectile.identity * 0.44f;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_ABreak);
            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak);

            for (int arm = 0; arm < 3; arm++)
            {
                float armAngle = time + arm * MathHelper.TwoPi / 3f;
                float radius = 11f + 3.5f * (float)Math.Sin(time * 2.1f + arm * 0.8f);
                Vector2 armOff = armAngle.ToRotationVector2() * radius;
                Vector2 armVel = direction * Main.rand.NextFloat(1.2f, 2.8f) + armOff.SafeNormalize(normal) * 0.5f;
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    Projectile.Center + armOff - direction * Main.rand.NextFloat(2f, 8f),
                    armVel,
                    Color.Lerp(accentColor, Color.White, 0.22f),
                    Color.White,
                    Main.rand.NextFloat(0.20f, 0.34f) * glowStrength,
                    6));
            }

            if ((int)FlightTimer % 4 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    Projectile.Center + normal * Main.rand.NextFloat(-7f, 7f) - direction * Main.rand.NextFloat(3f, 12f),
                    -direction * Main.rand.NextFloat(0.4f, 1.2f) + normal * Main.rand.NextFloat(-0.35f, 0.35f),
                    Main.rand.NextFloat(0.13f, 0.22f) * glowStrength,
                    Color.Lerp(mainColor, Color.White, 0.30f),
                    Main.rand.Next(8, 13)));
            }

            if ((int)FlightTimer % 5 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(
                    Projectile.Center + direction * 6f + normal * Main.rand.NextFloat(-4f, 4f),
                    Projectile.velocity * 0.04f,
                    Color.White,
                    accentColor,
                    0.42f * glowStrength,
                    7,
                    0f,
                    0.50f * glowStrength));
            }
        }

        private void AccelerateStraightFlight(float acceleration, float maxSpeed)
        {
            if (Projectile.velocity.LengthSquared() <= 0.0001f)
                return;

            Projectile.velocity *= acceleration;
            if (Projectile.velocity.LengthSquared() > maxSpeed * maxSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * maxSpeed;
        }

        private void SpawnBreakthroughImpactFX(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_ABreak);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Projectile.velocity.SafeNormalize(Vector2.UnitY) * 0.75f,
                Color.Lerp(mainColor, Color.White, 0.18f),
                new Vector2(0.8f, 1.75f),
                Projectile.velocity.ToRotation(),
                0.15f * intensity,
                0.034f,
                10));

            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                center,
                Vector2.Zero,
                Color.Lerp(mainColor, accentColor, 0.35f),
                0.36f * intensity,
                9));
        }

        private void SpawnBreakthroughVanishFX(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak);
            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                center,
                Vector2.Zero,
                Color.Lerp(mainColor, Color.White, 0.2f),
                0.24f * intensity,
                8));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Vector2.Zero,
                mainColor,
                Vector2.One,
                Main.rand.NextFloat(-0.4f, 0.4f),
                0.08f * intensity,
                0.022f,
                8));

            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_ABreak, 5, 0.65f, 1.8f, 0.55f, 0.9f);
        }
    }
}
