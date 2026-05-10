using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill.SpecialEffects
{
    internal sealed class BFEXConvergingWisp : ModProjectile, ILocalizedModType
    {
        private const int FadeFrames = 22;
        private static readonly Color MossColor = new(126, 255, 138);
        private static readonly Color LimeColor = new(198, 255, 118);
        private static readonly Color BloomWhite = new(238, 255, 218);

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.ai[0];
        private ref float Seed => ref Projectile.ai[1];
        private ref float FadeSignal => ref Projectile.ai[2];

        private float Opacity =>
            Utils.GetLerpValue(0f, 8f, Timer, true) *
            Utils.GetLerpValue(0f, FadeFrames, Projectile.timeLeft, true);

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 92;
        }

        public override bool? CanDamage() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Seed == 0f)
                Seed = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void AI()
        {
            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers)
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Timer++;

            if (FadeSignal != 0f)
            {
                if (Projectile.timeLeft > FadeFrames)
                    Projectile.timeLeft = FadeFrames;

                Projectile.velocity *= 0.92f;
            }
            else
            {
                HomeTowardOwner(owner);
            }

            Projectile.rotation += 0.12f + Projectile.velocity.Length() * 0.006f;
            Lighting.AddLight(Projectile.Center, MossColor.ToVector3() * (0.28f * Opacity));

            if (Projectile.Hitbox.Intersects(owner.Hitbox) || Projectile.Distance(owner.Center) < 34f)
                Projectile.Kill();

            SpawnParticleTrail();
        }

        private void HomeTowardOwner(Player owner)
        {
            Vector2 swirlOffset = new(
                (float)Math.Cos(Timer * 0.08f + Seed) * 28f,
                (float)Math.Sin(Timer * 0.067f + Seed * 1.3f) * 20f);

            Vector2 target = owner.Center + swirlOffset;
            Vector2 currentDirection = Projectile.velocity.SafeNormalize((target - Projectile.Center).SafeNormalize(Vector2.UnitY));
            Vector2 desiredDirection = (target - Projectile.Center).SafeNormalize(currentDirection);
            Vector2 sideCurl = desiredDirection.RotatedBy(MathHelper.PiOver2) * (float)Math.Sin(Timer * 0.12f + Seed) * 3.2f;
            float speed = MathHelper.Lerp(9f, 19f, Utils.GetLerpValue(0f, 42f, Timer, true));
            Vector2 desiredVelocity = (desiredDirection * speed + sideCurl).SafeNormalize(desiredDirection) * speed;
            float inertia = MathHelper.Lerp(22f, 6f, Utils.GetLerpValue(0f, 58f, Timer, true));

            Projectile.velocity = (Projectile.velocity * inertia + desiredVelocity) / (inertia + 1f);
        }

        private void SpawnParticleTrail()
        {
            if (Main.dedServ)
                return;

            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitY);

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    back * Main.rand.NextFloat(0.4f, 1.2f) + Main.rand.NextVector2Circular(0.18f, 0.18f),
                    false,
                    Main.rand.Next(11, 17),
                    Main.rand.NextFloat(0.22f, 0.36f),
                    Color.Lerp(MossColor, BloomWhite, Main.rand.NextFloat(0.12f, 0.45f)) * Opacity,
                    true,
                    false,
                    true));
            }

            if (!Main.rand.NextBool(3))
                return;

            GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                Projectile.Center - Projectile.velocity * 0.15f,
                back.RotatedByRandom(0.38f) * Main.rand.NextFloat(1.2f, 3.4f),
                false,
                Main.rand.Next(8, 13),
                Main.rand.NextFloat(0.035f, 0.055f),
                Color.Lerp(LimeColor, BloomWhite, Main.rand.NextFloat(0.1f, 0.42f)) * Opacity,
                new Vector2(1.35f, 0.38f),
                true,
                false));
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ || Timer < 4f)
                return;

            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(MossColor, BloomWhite, 0.25f),
                0.36f,
                12));

            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 3.5f),
                    false,
                    Main.rand.Next(8, 12),
                    Main.rand.NextFloat(0.18f, 0.28f),
                    Main.rand.NextBool() ? MossColor : BloomWhite,
                    true,
                    false,
                    true));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Opacity <= 0f)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D soft = ModContent.Request<Texture2D>("CalamityMod/Particles/SmallBloom").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/PulseStar").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float pulse = 0.86f + 0.14f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 9f + Seed);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                MossColor with { A = 0 } * (0.48f * Opacity),
                0f,
                bloom.Size() * 0.5f,
                0.26f * pulse,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                soft,
                drawPosition,
                null,
                LimeColor with { A = 0 } * (0.42f * Opacity),
                -Projectile.rotation * 0.6f,
                soft.Size() * 0.5f,
                0.18f * pulse,
                SpriteEffects.None,
                0);

            for (int i = 0; i < 5; i++)
            {
                float angle = MathHelper.TwoPi * i / 5f + Projectile.rotation;
                Vector2 offset = angle.ToRotationVector2() * (3f + pulse * 2.5f);
                Main.EntitySpriteDraw(
                    star,
                    drawPosition + offset,
                    null,
                    BloomWhite with { A = 0 } * (0.24f * Opacity),
                    angle,
                    star.Size() * 0.5f,
                    new Vector2(0.12f, 0.22f) * pulse,
                    SpriteEffects.None,
                    0);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
