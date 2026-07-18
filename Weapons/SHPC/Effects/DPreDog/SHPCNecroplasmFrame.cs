using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    // A small spectral frame follows Shadowbolt's rhythm: travel, arrest in open air, lock a
    // target line, and fire. It is a telegraph and never deals contact damage by itself.
    internal sealed class SHPCNecroplasmFrame : ModProjectile, ILocalizedModType
    {
        private const int TravelFrames = 18;
        private const int ChargeFrames = 18;
        private const int FireFrame = TravelFrames + ChargeFrames;

        private static readonly Color FrameOuter = new(68, 58, 190);
        private static readonly Color FrameCore = new(115, 240, 255);

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float BeamRotation => ref Projectile.localAI[1];

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = FireFrame + 12;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;

        public override void OnSpawn(IEntitySource source)
        {
            BeamRotation = Projectile.velocity.ToRotation();
        }

        public override void AI()
        {
            Timer++;
            Projectile.Opacity = Utils.GetLerpValue(0f, 6f, Timer, true) * Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);

            if (Timer <= TravelFrames)
            {
                Projectile.velocity *= 0.87f;
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            else
            {
                Projectile.velocity = Vector2.Zero;
                NPC target = Projectile.Center.ClosestNPCAt(1800f);
                if (target is not null)
                {
                    float desiredRotation = Projectile.AngleTo(target.Center);
                    BeamRotation = BeamRotation.AngleTowards(desiredRotation, 0.095f);
                }

                Projectile.rotation += 0.11f;
                if (!Main.dedServ && (int)Timer % 3 == 0)
                    SpawnChargeEffects();
            }

            if (Projectile.owner == Main.myPlayer && Timer == FireFrame)
                FireBeam();
        }

        private void FireBeam()
        {
            Vector2 direction = BeamRotation.ToRotationVector2();
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + direction * 12f,
                direction,
                ModContent.ProjectileType<SHPCNecroplasmBeam>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                BeamRotation);

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ShadowboltReflect") { Volume = 0.42f, Pitch = 0.12f, PitchVariance = 0.07f, MaxInstances = 6 }, Projectile.Center);
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center,
                    direction * 0.35f,
                    FrameCore,
                    new Vector2(0.38f, 1.08f),
                    BeamRotation,
                    0.05f,
                    0.72f,
                    14));
            }
        }

        private void SpawnChargeEffects()
        {
            Vector2 direction = BeamRotation.ToRotationVector2();
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 position = Projectile.Center + normal * Main.rand.NextFloat(-15f, 15f) - direction * Main.rand.NextFloat(-3f, 10f);
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                position,
                (Projectile.Center - position).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.5f, 1.8f),
                false,
                Main.rand.Next(8, 13),
                Main.rand.NextFloat(0.14f, 0.24f),
                Main.rand.NextBool() ? FrameOuter : FrameCore,
                true,
                false));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D square = ModContent.Request<Texture2D>("CalamityMod/Particles/TechyHolosquare").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float charge = Utils.GetLerpValue(TravelFrames, FireFrame, Timer, true);
            float pulse = 0.88f + MathF.Sin(Timer * 0.42f) * 0.12f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < 4; i++)
            {
                float rotation = Projectile.rotation + i * MathHelper.PiOver2;
                Main.EntitySpriteDraw(square, drawPosition, null, FrameOuter * (0.26f * Projectile.Opacity), rotation,
                    square.Size() * 0.5f, new Vector2(0.32f, 0.32f + charge * 0.10f) * pulse, SpriteEffects.None);
            }
            Main.EntitySpriteDraw(square, drawPosition, null, FrameCore * (0.48f * Projectile.Opacity), Projectile.rotation,
                square.Size() * 0.5f, new Vector2(0.25f, 0.25f + charge * 0.08f), SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPosition, null, FrameCore * (0.24f * charge), 0f, bloom.Size() * 0.5f, 0.24f * pulse, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
