using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC
{
    public class TwistingNether_Blade : ModProjectile, ILocalizedModType
    {
        private static readonly Color BladePurple = new(185, 35, 255);
        private static readonly Color BladeBlood = new(135, 0, 42);
        private static readonly Color BladeDark = new(12, 0, 20);
        private static readonly Color GlitchGreen = new(70, 255, 185);

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int flightTimer;
        private float helixAngle;
        private float pulseAngle;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 100;
            Projectile.extraUpdates = 4;
            Projectile.Opacity = 1f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1;
        }

        public override bool? CanCutTiles() => false;

        public override void OnSpawn(IEntitySource source)
        {
            if (Projectile.velocity.LengthSquared() < 0.001f)
                Projectile.velocity = -Vector2.UnitY * 24f;

            Projectile.Opacity = 1f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.damage *= 2;
            helixAngle = Projectile.identity * 0.37f;
            pulseAngle = Projectile.identity * 0.21f;
            flightTimer = 6;

            SoundEngine.PlaySound(SoundID.Item104 with { Pitch = -0.25f, Volume = 0.55f }, Projectile.Center);
        }

        public override void AI()
        {
            // 在每次更新时（每tick更新5次）平滑累加角度，使得旋转速度和之前完全保持一致
            helixAngle += 0.28f / 5f;
            pulseAngle += 0.16f / 5f;

            if (Projectile.numUpdates == 0)
            {
                flightTimer++;
                SpawnVisibleFlightEffects();
            }

            // 以两倍的频率释放 VoidSparkParticle (在 numUpdates == 0 和 2 时分别释放一次)
            if (Projectile.numUpdates == 0 || Projectile.numUpdates == 2)
            {
                SpawnVoidSparkParticles();
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = 1f;
        }

        private void SpawnVoidSparkParticles()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);

            for (int strand = 0; strand < 2; strand++)
            {
                float phase = helixAngle + strand * MathHelper.Pi;
                float sideOffset = (float)Math.Sin(phase) * 30f;
                float forwardOffset = MathHelper.Lerp(-28f, 14f, 0.5f + 0.5f * (float)Math.Cos(phase));
                Vector2 sparkPosition = Projectile.Center + forward * forwardOffset + side * sideOffset;
                Vector2 sparkVelocity = -forward * (1.05f + strand * 0.22f);
                Color sparkColor = Color.Lerp(BladePurple, strand == 0 ? GlitchGreen : BladeBlood, 0.32f);

                GeneralParticleHandler.SpawnParticle(new VoidSparkParticle(
                    sparkPosition,
                    sparkVelocity,
                    false,
                    18,
                    0.12f,
                    sparkColor,
                    0.78f));
            }
        }

        private void SpawnVisibleFlightEffects()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 center = Projectile.Center + forward * Main.rand.NextFloat(18f, 42f) + side * Main.rand.NextFloat(-10f, 10f);
            Color mainColor = Color.Lerp(BladePurple, BladeBlood, Main.rand.NextFloat(0.15f, 0.55f));

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

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch,
                    -forward * Main.rand.NextFloat(0.3f, 1.2f) + side * Main.rand.NextFloat(-0.5f, 0.5f),
                    0,
                    mainColor,
                    Main.rand.NextFloat(1.0f, 1.45f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ShadowboltReflect"), target.Center);
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 slashVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 8f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    slashVelocity,
                    ModContent.ProjectileType<TwistingNether_BlackSLASH>(),
                    (int)(Projectile.damage * 0.77f),
                    Projectile.knockBack,
                    Projectile.owner);
            }

            if (Main.dedServ)
                return;

            for (int i = 0; i < 3; i++)
            {
                GeneralParticleHandler.SpawnParticle(new CustomPulse(
                    target.Center,
                    Vector2.Zero,
                    i == 0 ? BladeDark : (i % 2 == 0 ? BladePurple : BladeBlood),
                    i == 0 ? "CalamityMod/Particles/LargeBloom" : "CalamityMod/Particles/BloomCircle",
                    i == 0 ? new Vector2(1.8f, 0.8f) : Vector2.One,
                    Main.rand.NextFloat(-0.2f, 0.2f),
                    MathHelper.Max(0.12f, 0.78f - i * 0.1f) * 0.05f,
                    0f,
                    18,
                    true));
            }

            for (int i = 0; i < 32; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    target.Center,
                    Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 11.5f),
                    0,
                    Color.Lerp(BladePurple, BladeBlood, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.25f, 2.4f));
                dust.noGravity = true;
            }

            for (int i = 0; i < 7; i++)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    target.Center + Main.rand.NextVector2Circular(18f, 18f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.5f, 5f),
                    Main.rand.NextBool(3) ? BladeDark : new Color(28, 0, 38),
                    Main.rand.Next(24, 42),
                    Main.rand.NextFloat(0.8f, 1.55f),
                    0.45f,
                    Main.rand.NextFloat(-0.12f, 0.12f),
                    false));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float opacity = Projectile.Opacity *
                Utils.GetLerpValue(0f, 4f, flightTimer, true) *
                Utils.GetLerpValue(0f, 22f, Projectile.timeLeft, true);

            if (opacity <= 0f)
                return false;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmear").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;

            float time = Main.GlobalTimeWrappedHourly;
            float bladePulse = 0.5f + 0.5f * (float)Math.Sin(time * 10.2f + Projectile.identity * 0.31f);
            float bladeSpin = time * 7.4f + pulseAngle * 0.35f;
            float forwardOffset = MathHelper.Lerp(34f, 48f, bladePulse);
            Vector2 bladeCenter = drawPos + forward * forwardOffset + side * (float)Math.Sin(time * 5.6f + Projectile.identity) * 2.5f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            DrawOldTrail(pixel, forward, opacity);
            DrawBladeBloom(bloom, bladeCenter, forward, opacity, bladePulse);
            DrawTwistedBlade(smear, bladeCenter, forward, side, opacity, bladeSpin, bladePulse);
            DrawForwardEdge(smear, bladeCenter, forward, side, opacity, bladePulse);
            DrawGlitchCuts(pixel, bladeCenter, forward, side, opacity);
            DrawBladeStar(star, bladeCenter, opacity, bladeSpin);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private void DrawOldTrail(Texture2D pixel, Vector2 forward, float opacity)
        {
            for (int i = 1; i < Projectile.oldPos.Length; i += 2)
            {
                if (Projectile.oldPos[i] == Vector2.Zero || Projectile.oldPos[i - 1] == Vector2.Zero)
                    continue;

                Vector2 current = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Vector2 previous = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f - Main.screenPosition;
                Vector2 delta = previous - current;
                float length = delta.Length();
                if (length <= 2f)
                    continue;

                float completion = i / (float)Projectile.oldPos.Length;
                float width = MathHelper.Lerp(7.5f, 1.2f, completion);
                Color trailColor = Color.Lerp(BladePurple, BladeDark, completion);
                trailColor.A = 0;

                Main.EntitySpriteDraw(
                    pixel,
                    current - forward * 12f,
                    new Rectangle(0, 0, 1, 1),
                    trailColor * opacity * (0.45f - completion * 0.25f),
                    delta.ToRotation(),
                    new Vector2(0f, 0.5f),
                    new Vector2(length * 0.8f, width),
                    SpriteEffects.None);
            }
        }

        private static void DrawBladeBloom(Texture2D bloom, Vector2 center, Vector2 forward, float opacity, float pulse)
        {
            Vector2 origin = bloom.Size() * 0.5f;
            Color dark = BladeDark;
            Color purple = BladePurple;
            Color blood = BladeBlood;
            dark.A = purple.A = blood.A = 0;

            Main.EntitySpriteDraw(bloom, center, null, dark * opacity * 1.85f, 0f, origin, new Vector2(0.52f, 0.34f) * (1f + pulse * 0.08f), SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, center + forward * 4f, null, purple * opacity * 0.92f, 0f, origin, new Vector2(0.28f, 0.18f) * (1f + pulse * 0.12f), SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, center + forward * 8f, null, Color.White * opacity * 0.18f, 0f, origin, new Vector2(0.1f, 0.065f), SpriteEffects.None);
        }

        private static void DrawTwistedBlade(Texture2D smear, Vector2 center, Vector2 forward, Vector2 side, float opacity, float spin, float pulse)
        {
            Vector2 origin = smear.Size() * 0.5f;
            float baseRotation = forward.ToRotation() + MathHelper.PiOver2;

            for (int i = 0; i < 14; i++)
            {
                float completion = i / 13f;
                float angle = MathHelper.TwoPi * completion + spin;
                float twist = (float)Math.Sin(spin * 1.35f + i * 0.74f) * 0.18f;
                Vector2 offset = forward * (float)Math.Cos(angle) * 6f + side * (float)Math.Sin(angle) * 9f;
                Color color = i % 4 == 0
                    ? BladeDark
                    : Color.Lerp(BladePurple, BladeBlood, 0.25f + completion * 0.55f);
                color.A = 0;

                Main.EntitySpriteDraw(
                    smear,
                    center + offset,
                    null,
                    color * opacity * (0.78f - completion * 0.22f),
                    baseRotation + angle * 0.55f + twist,
                    origin,
                    new Vector2(0.07f + completion * 0.025f, 0.55f + pulse * 0.15f) * 0.72f,
                    SpriteEffects.None);
            }
        }

        private static void DrawForwardEdge(Texture2D smear, Vector2 center, Vector2 forward, Vector2 side, float opacity, float pulse)
        {
            Vector2 origin = smear.Size() * 0.5f;
            float rotation = forward.ToRotation() + MathHelper.PiOver2;

            for (int i = 0; i < 5; i++)
            {
                float lane = i - 2f;
                float wave = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f + i * 1.4f) * 2f;
                Vector2 position = center + forward * (12f + i * 3.8f) + side * (lane * 5.5f + wave);
                Color color = i == 2 ? Color.Lerp(Color.White, BladePurple, 0.45f) : Color.Lerp(BladePurple, BladeBlood, i / 4f);
                color.A = 0;

                Main.EntitySpriteDraw(
                    smear,
                    position,
                    null,
                    color * opacity * (0.32f + pulse * 0.12f),
                    rotation + lane * 0.035f,
                    origin,
                    new Vector2(0.05f, 0.72f + pulse * 0.12f) * 0.6f,
                    SpriteEffects.None);
            }
        }

        private static void DrawGlitchCuts(Texture2D pixel, Vector2 center, Vector2 forward, Vector2 side, float opacity)
        {
            int glitchStep = (int)(Main.GameUpdateCount / 5UL);
            float baseRotation = forward.ToRotation();

            for (int i = 0; i < 7; i++)
            {
                float phase = glitchStep * 0.63f + i * 1.917f;
                float flicker = 0.65f + 0.35f * (float)Math.Sin(phase * 2.3f);
                float lane = (float)Math.Sin(phase * 1.37f) * 30f;
                float along = (float)Math.Cos(phase * 0.81f) * 18f;
                float length = 9f + 16f * (0.5f + 0.5f * (float)Math.Sin(phase * 1.71f));
                Color color = (i % 3) switch
                {
                    0 => BladePurple,
                    1 => BladeBlood,
                    _ => GlitchGreen
                };
                color.A = 0;

                Main.EntitySpriteDraw(
                    pixel,
                    center + forward * along + side * lane,
                    new Rectangle(0, 0, 1, 1),
                    color * opacity * (0.08f + flicker * 0.09f),
                    baseRotation + (float)Math.Sin(phase) * 0.035f,
                    new Vector2(0f, 0.5f),
                    new Vector2(length, 1.1f + flicker * 0.7f),
                    SpriteEffects.None);
            }
        }

        private void DrawBladeStar(Texture2D star, Vector2 center, float opacity, float spin)
        {
            Vector2 origin = star.Size() * 0.5f;

            for (int i = 0; i < 3; i++)
            {
                Color color = Color.Lerp(Color.White, i == 0 ? BladePurple : BladeBlood, 0.42f);
                color.A = 0;

                Main.EntitySpriteDraw(
                    star,
                    center,
                    null,
                    color * opacity * (0.32f - i * 0.06f),
                    Projectile.rotation + spin * (0.12f + i * 0.04f) + i * MathHelper.PiOver2,
                    origin,
                    new Vector2(0.32f + i * 0.08f, 1.05f + i * 0.22f) * 0.34f,
                    SpriteEffects.None);
            }
        }
    }
}
