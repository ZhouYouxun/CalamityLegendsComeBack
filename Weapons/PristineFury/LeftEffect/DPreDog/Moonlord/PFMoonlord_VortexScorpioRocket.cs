using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Enums;
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

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFMoonlord_VortexScorpioRocket : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        private const int Lifetime = 180;
        private const float TimeToLaunch = 9f;
        private const float TimeForFullPropulsion = 13f;
        private const float TrackingRange = 620f;
        private const float TrackingSpeed = 0.045f;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Ranged/ScorpioRocket";

        private ref float ProjectileSpeed => ref Projectile.ai[0];
        private ref float Variant => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private Color VortexColor => new(76, 255, 166);
        private Color StaticColor => new(126, 240, 255);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = Lifetime;
            Projectile.localNPCHitCooldown = -1;
            Projectile.extraUpdates = 1;
            Projectile.friendly = true;
            Projectile.penetrate = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (ProjectileSpeed <= 0f)
                ProjectileSpeed = 13.5f;

            if (Timer >= TimeToLaunch)
            {
                float speed = Utils.Remap(Timer, TimeToLaunch, TimeToLaunch + TimeForFullPropulsion, 5f, ProjectileSpeed, true);
                NPC target = FindTarget();
                if (target != null)
                {
                    float desiredRotation = Projectile.SafeDirectionTo(target.Center).ToRotation();
                    float turnSpeed = Utils.Remap(Timer, TimeToLaunch, TimeToLaunch + TimeForFullPropulsion, 0.012f, TrackingSpeed, true);
                    Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(desiredRotation, turnSpeed).ToRotationVector2() * speed;
                }
                else
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy((float)Math.Sin((Timer + Variant * 17f) * 0.08f) * 0.01f) * speed;
            }
            else
                Projectile.velocity *= 0.92f;

            Projectile.rotation = Projectile.velocity.ToRotation();
            UpdateAnimation();
            SpawnFlightEffects();
            Timer++;
        }

        private NPC FindTarget()
        {
            NPC closest = null;
            float bestDistance = TrackingRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                closest = npc;
            }

            return closest;
        }

        private void UpdateAnimation()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter < 4)
                return;

            Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            Projectile.frameCounter = 0;
        }

        private void SpawnFlightEffects()
        {
            if (Main.dedServ)
                return;

            Projectile.alpha = (int)Utils.Remap(Projectile.timeLeft, 30f, 0f, 0f, 255f, true);
            Lighting.AddLight(Projectile.Center, VortexColor.ToVector3() * 0.42f);

            Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Vortex, 0f, 0f, 0, default, Main.rand.NextFloat(0.75f, 1f));
            dust.noGravity = true;
            dust.noLight = true;
            dust.noLightEmittence = true;

            if (Timer >= TimeToLaunch && Projectile.timeLeft % 3 == 0)
            {
                Particle nano = new NanoParticle(
                    Projectile.Center,
                    -Projectile.velocity.RotatedByRandom(0.18f) * Main.rand.NextFloat(0.45f, 0.95f),
                    Main.rand.NextBool(3) ? VortexColor : StaticColor,
                    Main.rand.NextFloat(0.62f, 0.9f),
                    Main.rand.Next(15, 22),
                    Main.rand.NextBool(),
                    true);

                GeneralParticleHandler.SpawnParticle(nano);
            }

            if (Timer >= TimeToLaunch)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center + Projectile.velocity * 1.1f,
                    Vector2.Zero,
                    StaticColor * 1.5f,
                    Vector2.One,
                    Projectile.rotation,
                    0.12f,
                    0.28f,
                    4));
            }
        }

        public override bool? CanDamage() => Timer >= TimeToLaunch;

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                modifiers.SourceDamage *= 0.72f;
        }

        public override void OnKill(int timeLeft)
        {
            int oldWidth = Projectile.width;
            int oldHeight = Projectile.height;
            Vector2 oldCenter = Projectile.Center;

            Projectile.ExpandHitboxBy(66f);
            Projectile.Damage();
            Projectile.width = oldWidth;
            Projectile.height = oldHeight;
            Projectile.Center = oldCenter;

            SpawnExplosionEffects();
        }

        private void SpawnExplosionEffects()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 26; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Vortex, (MathHelper.TwoPi * i / 26f).ToRotationVector2() * Main.rand.NextFloat(4f, 10f));
                dust.noGravity = true;
                dust.noLight = true;
                dust.noLightEmittence = true;
                dust.scale = Main.rand.NextFloat(1f, 1.45f);
            }

            for (int i = 0; i < 14; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f) * Main.rand.NextFloat(0.5f, 1.8f);
                Particle nano = new NanoParticle(
                    Projectile.Center,
                    velocity,
                    Main.rand.NextBool(3) ? VortexColor : StaticColor,
                    Main.rand.NextFloat(1.1f, 1.8f),
                    Main.rand.Next(24, 39),
                    Main.rand.NextBool(),
                    true);

                GeneralParticleHandler.SpawnParticle(nano);
            }

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, StaticColor, Vector2.One, Projectile.rotation, 0.08f, 1.05f, 22));
            GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center, Vector2.Zero, VortexColor, 1.25f, 13, false));
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.38f, Pitch = 0.55f, MaxInstances = 8 }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Ranged/ScorpioRocket_Glow").Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Size() * 0.5f;
            float rotation = Projectile.rotation + MathHelper.PiOver2;

            Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(glow, drawPosition, frame, Color.White * Projectile.Opacity, rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            if (Timer < TimeToLaunch)
                return;

            Vector2[] trailPoints = Projectile.oldPos
                .Where(position => position != Vector2.Zero)
                .Select(position => position + Projectile.Size * 0.5f)
                .ToArray();

            if (trailPoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    TrailWidthFunction,
                    TrailColorFunction,
                    (_, _) => Vector2.Zero,
                    false,
                    false,
                    GameShaders.Misc["CalamityMod:TrailStreak"]),
                25);
        }

        private float TrailWidthFunction(float completionRatio, Vector2 _) =>
            Utils.Remap(completionRatio, 0f, 0.8f, 6.5f, 0f, true);

        private Color TrailColorFunction(float completionRatio, Vector2 _)
        {
            Color color = Color.Lerp(VortexColor, StaticColor * 0.75f, Utils.GetLerpValue(0f, 0.5f, completionRatio, true));
            color.A = 0;
            return color * Utils.GetLerpValue(255f, 0f, Projectile.alpha, true);
        }
    }
}
