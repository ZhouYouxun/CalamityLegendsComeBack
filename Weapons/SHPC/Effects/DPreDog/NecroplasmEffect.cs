using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    // The travelling orb stays as the anchor of the effect. It is now a narrow, aggressively
    // homing Phantom Spirit and periodically releases Shadowbolt-style frames that stop first,
    // acquire a line, and only then fire their beam.
    public sealed class NecroplasmEffect : DefaultEffect
    {
        private const int FrameCount = 3;
        private const int FrameFirstRelease = 20;
        private const int FrameReleaseInterval = 16;

        public override int EffectID => 31;
        public override int AmmoType => ModContent.ItemType<Necroplasm>();

        public override Color ThemeColor => new(87, 226, 255);
        public override Color StartColor => new(185, 255, 255);
        public override Color EndColor => new(92, 58, 192);
        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0.42f;
        public override float GlowIntensityFactor => 0.72f;

        private static ref float Timer(Projectile projectile) => ref projectile.localAI[0];
        private static ref float FramesReleased(Projectile projectile) => ref projectile.localAI[1];

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.width = 18;
            projectile.height = 18;
            projectile.scale = 0.78f;
            projectile.penetrate = 4;
            projectile.timeLeft = Math.Max(projectile.timeLeft, 210);
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.velocity = projectile.velocity.SafeNormalize(new Vector2(owner.direction, 0f)) * Math.Max(projectile.velocity.Length(), 19f);
            Timer(projectile) = 0f;
            FramesReleased(projectile) = 0f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            Timer(projectile)++;
            NPC target = projectile.Center.ClosestNPCAt(2900f);
            Vector2 fallback = projectile.velocity.SafeNormalize(new Vector2(owner.direction, 0f));

            if (target is not null)
            {
                Vector2 desiredDirection = projectile.SafeDirectionTo(target.Center, fallback);
                float pressure = Utils.GetLerpValue(0f, 28f, Timer(projectile), true);
                float desiredSpeed = MathHelper.Lerp(22f, 39f, pressure);
                float inertia = MathHelper.Lerp(5.5f, 2.2f, pressure);
                projectile.velocity = (projectile.velocity * inertia + desiredDirection * desiredSpeed) / (inertia + 1f);
            }
            else
                projectile.velocity *= 0.993f;

            float speed = MathHelper.Clamp(projectile.velocity.Length(), 14f, 42f);
            projectile.velocity = projectile.velocity.SafeNormalize(fallback) * speed;
            projectile.rotation = projectile.velocity.ToRotation();
            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.52f);

            if (projectile.owner == Main.myPlayer && FramesReleased(projectile) < FrameCount && Timer(projectile) >= FrameFirstRelease + FramesReleased(projectile) * FrameReleaseInterval)
            {
                SpawnShadowFrame(projectile, target, (int)FramesReleased(projectile));
                FramesReleased(projectile)++;
            }

            if (projectile.owner == Main.myPlayer && (int)Timer(projectile) % 11 == 0)
                SpawnDamageOrb(projectile, target);

            if (!Main.dedServ)
                SpawnFlightEffects(projectile);
        }

        private static void SpawnShadowFrame(Projectile projectile, NPC target, int index)
        {
            Vector2 forward = target is null
                ? projectile.velocity.SafeNormalize(Vector2.UnitX)
                : projectile.SafeDirectionTo(target.Center, projectile.velocity.SafeNormalize(Vector2.UnitX));
            float side = index - 1f;
            Vector2 direction = forward.RotatedBy(side * 0.22f + Main.rand.NextFloat(-0.06f, 0.06f));

            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center + direction * 14f,
                direction * (16f + index * 2f),
                ModContent.ProjectileType<SHPCNecroplasmFrame>(),
                Math.Max(1, (int)(projectile.damage * 0.62f)),
                projectile.knockBack * 0.45f,
                projectile.owner,
                index);
        }

        private static void SpawnDamageOrb(Projectile projectile, NPC target)
        {
            Vector2 forward = target is null
                ? projectile.velocity.SafeNormalize(Vector2.UnitX)
                : projectile.SafeDirectionTo(target.Center, projectile.velocity.SafeNormalize(Vector2.UnitX));
            Vector2 direction = forward.RotatedByRandom(0.24f);
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center - forward * 8f,
                direction * Main.rand.NextFloat(7f, 11f),
                ModContent.ProjectileType<SHPCNecroplasmDamage>(),
                Math.Max(1, (int)(projectile.damage * 0.34f)),
                projectile.knockBack * 0.25f,
                projectile.owner);
        }

        private static void SpawnFlightEffects(Projectile projectile)
        {
            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Color outer = new(63, 92, 224);
            Color core = new(133, 245, 255);

            if ((int)Timer(projectile) % 2 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    projectile.Center - direction * Main.rand.NextFloat(4f, 18f) + normal * Main.rand.NextFloat(-4f, 4f),
                    -direction * Main.rand.NextFloat(1.8f, 4.2f),
                    "CalamityMod/Particles/VerticalSmear",
                    false,
                    Main.rand.Next(10, 15),
                    Main.rand.NextFloat(0.45f, 0.72f),
                    Color.Lerp(outer, core, Main.rand.NextFloat()),
                    new Vector2(0.10f, 0.68f),
                    true,
                    true,
                    shrinkSpeed: 0.86f,
                    glowOpacity: 0.46f));
            }

            if ((int)Timer(projectile) % 4 == 0)
            {
                Dust dust = Dust.NewDustPerfect(projectile.Center + normal * Main.rand.NextFloat(-7f, 7f), (int)CalamityDusts.Necroplasm,
                    -direction * Main.rand.NextFloat(0.8f, 2.8f) + normal * Main.rand.NextFloat(-0.7f, 0.7f), 80,
                    Color.Lerp(outer, core, Main.rand.NextFloat()), Main.rand.NextFloat(0.72f, 1.08f));
                dust.noGravity = true;
            }
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ShadowboltReflect") { Volume = 0.55f, Pitch = -0.18f }, projectile.Center);
            owner.SetScreenshake(2.7f);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(projectile.Center, Vector2.Zero, ThemeColor,
                "CalamityMod/Particles/BloomRing", Vector2.One, 0f, 0.08f, 0.82f, 18));

            for (int i = 0; i < 5; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / 5f + Main.rand.NextFloat(-0.12f, 0.12f)).ToRotationVector2();
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + direction * 8f,
                    direction * Main.rand.NextFloat(7f, 12f),
                    ModContent.ProjectileType<SHPCNecroplasmDamage>(),
                    Math.Max(1, (int)(projectile.damage * 0.42f)),
                    projectile.knockBack * 0.3f,
                    projectile.owner);
            }
        }

        public override void PreDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch)
        {
            Texture2D ghost = ModContent.Request<Texture2D>("CalamityMod/NPCs/NormalNPCs/PhantomSpirit").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            int frameHeight = ghost.Height / 3;
            int frameIndex = ((int)(Timer(projectile) / 5f) + projectile.identity) % 3;
            Rectangle frame = new(0, frameHeight * frameIndex, ghost.Width, frameHeight);
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Size() * 0.5f;
            float pulse = 0.78f + MathF.Sin(Timer(projectile) * 0.32f) * 0.16f;

            spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 5f + Timer(projectile) * 0.07f).ToRotationVector2() * 1.5f;
                spriteBatch.Draw(ghost, drawPosition + offset, frame, ThemeColor * (0.19f * pulse), projectile.rotation * 0.08f, origin, 0.93f, SpriteEffects.None, 0f);
            }
            spriteBatch.Draw(bloom, drawPosition, null, ThemeColor * (0.28f * pulse), 0f, bloom.Size() * 0.5f, 0.31f, SpriteEffects.None, 0f);
            spriteBatch.Draw(ghost, drawPosition, frame, StartColor * (0.66f * pulse), projectile.rotation * 0.05f, origin, 0.82f, SpriteEffects.None, 0f);
            spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }
    }
}
