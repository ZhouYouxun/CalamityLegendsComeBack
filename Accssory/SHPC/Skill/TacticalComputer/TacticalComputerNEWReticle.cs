using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.TacticalComputer
{
    internal sealed class TacticalComputerNEWReticle : ModProjectile
    {
        private const int TelegraphSpawnRate = 4;
        private int telegraphTimer;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (Main.dedServ || Projectile.owner != Main.myPlayer)
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[Projectile.owner];
            TacticalComputerPlayer tacticalPlayer = owner.GetModPlayer<TacticalComputerPlayer>();
            if (!owner.active || owner.dead || !tacticalPlayer.TacticalComputerEquipped || tacticalPlayer.TacticalComputerVisualsHidden || tacticalPlayer.ReticleWorld == Vector2.Zero)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = tacticalPlayer.ReticleWorld;
            Projectile.timeLeft = 2;

            if (telegraphTimer++ % TelegraphSpawnRate == 0)
                EmitReticleTelegraph(tacticalPlayer);
        }

        public override bool PreDraw(ref Color lightColor) => false;

        private void EmitReticleTelegraph(TacticalComputerPlayer tacticalPlayer)
        {
            bool locked = tacticalPlayer.ReticleHasTarget;
            float pulse = 0.95f + 0.05f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * (locked ? 9f : 6f) + Projectile.identity * 0.17f);
            Color reticleColor = locked ? new Color(70, 220, 255) : new Color(72, 190, 255);
            float originalScale = (locked ? 0.42f : 0.34f) * pulse;
            float finalScale = (locked ? 0.16f : 0.14f) * pulse;

            GeneralParticleHandler.SpawnParticle(new StaticReticleTelegraphParticle(Projectile.Center, reticleColor, originalScale, finalScale, 18));

            if (!locked)
                return;

            Color sparkColor = new(210, 255, 255);
            Color bloomColor = new(30, 170, 255);
            GeneralParticleHandler.SpawnParticle(new StaticSparkTelegraphParticle(Projectile.Center, sparkColor, bloomColor, 0.58f * pulse, 18, 0.055f, 1.08f));
        }

        private sealed class StaticReticleTelegraphParticle : Particle
        {
            private readonly float originalScale;
            private readonly float finalScale;
            private readonly int rotationDirection;
            private float opacity;

            public override string Texture => "CalamityMod/Particles/DestroyerReticleTelegraph";
            public override bool UseAdditiveBlend => true;
            public override bool SetLifetime => true;
            public override bool UseCustomDraw => true;
            public override bool Important => true;

            public StaticReticleTelegraphParticle(Vector2 position, Color color, float originalScale, float finalScale, int lifetime)
            {
                Position = position;
                Color = color;
                this.originalScale = originalScale;
                this.finalScale = finalScale;
                Scale = originalScale;
                Lifetime = lifetime;
                rotationDirection = Main.rand.NextBool().ToDirectionInt();
            }

            public override void Update()
            {
                float progress = 1f - MathF.Pow(1f - LifetimeCompletion, 4f);
                Scale = MathHelper.Lerp(originalScale, finalScale, progress);
                opacity = progress;
                Rotation += MathHelper.ToRadians(8f) * (1f - progress) * rotationDirection;
            }

            public override void CustomDraw(SpriteBatch spriteBatch)
            {
                Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
                spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color * opacity, Rotation, texture.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
            }
        }

        private sealed class StaticSparkTelegraphParticle : Particle
        {
            private readonly Color bloom;
            private readonly float bloomScale;
            private readonly float spin;
            private readonly int spinDirection;
            private float opacity;

            public override string Texture => "CalamityMod/Particles/Sparkle2";
            public override bool UseAdditiveBlend => true;
            public override bool UseCustomDraw => true;
            public override bool SetLifetime => true;
            public override bool Important => true;

            public StaticSparkTelegraphParticle(Vector2 position, Color color, Color bloom, float scale, int lifetime, float rotationSpeed = 0f, float bloomScale = 1f)
            {
                Position = position;
                Color = color;
                this.bloom = bloom;
                Scale = scale;
                Lifetime = lifetime;
                spin = rotationSpeed;
                this.bloomScale = bloomScale;
                spinDirection = Main.rand.NextBool().ToDirectionInt();
                Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            }

            public override void Update()
            {
                opacity = MathF.Sin(LifetimeCompletion * MathHelper.Pi);
                Rotation += spin * spinDirection;
            }

            public override void CustomDraw(SpriteBatch spriteBatch)
            {
                Texture2D starTexture = ModContent.Request<Texture2D>(Texture).Value;
                Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
                float properBloomSize = starTexture.Height / (float)bloomTexture.Height;

                spriteBatch.Draw(bloomTexture, Position - Main.screenPosition, null, bloom * opacity * 0.5f, 0f, bloomTexture.Size() * 0.5f, Scale * bloomScale * properBloomSize, SpriteEffects.None, 0f);
                spriteBatch.Draw(starTexture, Position - Main.screenPosition, null, Color * opacity * 0.5f, Rotation + MathHelper.PiOver4, starTexture.Size() * 0.5f, Scale * 0.75f, SpriteEffects.None, 0f);
                spriteBatch.Draw(starTexture, Position - Main.screenPosition, null, Color * opacity, Rotation, starTexture.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
            }
        }
    }
}
