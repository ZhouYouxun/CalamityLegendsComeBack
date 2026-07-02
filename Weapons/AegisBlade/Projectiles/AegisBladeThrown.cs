using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    public class AegisBladeThrown : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/AegisBlade/AegisBlade";

        private bool embedded;
        private bool wallSpawned;
        private int embedTimer;

        private static readonly Color CoreColor = new(255, 244, 190);
        private static readonly Color GoldColor = new(255, 196, 62);
        private static readonly Color FireColor = new(255, 120, 38);

        private float EmbeddedFade => embedded ? Utils.GetLerpValue(40f, 0f, embedTimer, true) : 1f;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.ignoreWater = true;
            Projectile.scale = 1.35f;
        }

        public override void AI()
        {
            if (!embedded)
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
                Projectile.velocity.Y = System.MathF.Min(Projectile.velocity.Y + 0.34f, 22f);
                Lighting.AddLight(Projectile.Center, GoldColor.ToVector3() * 0.65f);

                if (!Main.dedServ && Main.rand.NextBool(2))
                    EmitFallFlame();
                return;
            }

            embedTimer++;
            float fade = EmbeddedFade;
            Lighting.AddLight(Projectile.Center, GoldColor.ToVector3() * (0.35f + fade * 0.55f));
            if (!Main.dedServ && embedTimer % 3 == 0)
                EmitEmbeddedDissolve(fade);

            if (!wallSpawned && embedTimer == 1)
            {
                SpawnWalls();
                wallSpawned = true;
            }

            if (embedTimer > 40)
                Projectile.Kill();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            embedded = true;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60;
            Projectile.rotation = SnapEmbeddedRotation(oldVelocity);
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 1f, Pitch = -0.38f }, Projectile.Center);
            EmitImpact(Projectile.Center, oldVelocity.SafeNormalize(Vector2.UnitY));
            return false;
        }

        private static float SnapEmbeddedRotation(Vector2 impactVelocity)
        {
            bool horizontal = System.Math.Abs(impactVelocity.X) >= System.Math.Abs(impactVelocity.Y);
            float axisRotation = horizontal
                ? (impactVelocity.X >= 0f ? 0f : MathHelper.Pi)
                : (impactVelocity.Y >= 0f ? MathHelper.PiOver2 : -MathHelper.PiOver2);
            return axisRotation + MathHelper.PiOver4;
        }

        private void EmitFallFlame()
        {
            Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(8f, 8f) + Vector2.UnitY * Main.rand.NextFloat(8f, 24f);
            GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                position, new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(1.2f, 4.8f)),
                Color.Lerp(FireColor, GoldColor, Main.rand.NextFloat(0.25f, 0.85f)), Color.Transparent,
                Main.rand.NextFloat(0.38f, 0.64f), Main.rand.Next(16, 25), Main.rand.NextFloat(-0.07f, 0.07f)));
        }

        private void EmitImpact(Vector2 position, Vector2 direction)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(position, Vector2.Zero,
                CoreColor, new Vector2(1.2f, 0.78f), direction.ToRotation(), 0f, 0.88f, 22));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(position, Vector2.Zero,
                GoldColor, new Vector2(2.1f, 0.66f), 0f, 0.08f, 1.45f, 20));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(position, Vector2.Zero,
                FireColor, new Vector2(1.35f, 0.46f), MathHelper.PiOver2, 0.1f, 1.2f, 18));
            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-6.5f, 6.5f), -Main.rand.NextFloat(2.5f, 11f));
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    position + Main.rand.NextVector2Circular(12f, 5f), velocity,
                    Color.Lerp(FireColor, GoldColor, Main.rand.NextFloat()), Color.Transparent,
                    Main.rand.NextFloat(0.42f, 0.82f), Main.rand.Next(18, 30), Main.rand.NextFloat(-0.08f, 0.08f)));
            }
        }

        private void EmitEmbeddedDissolve(float fade)
        {
            Vector2 outward = Main.rand.NextVector2CircularEdge(1f, 1f);
            Vector2 position = Projectile.Center + outward * Main.rand.NextFloat(4f, 22f);
            Dust dust = Dust.NewDustPerfect(position, DustID.RainbowMk2,
                outward * Main.rand.NextFloat(0.25f, 1.1f), 80,
                Color.Lerp(CoreColor, GoldColor, Main.rand.NextFloat()), Main.rand.NextFloat(0.7f, 1.25f) * fade);
            dust.noGravity = true;

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    position, outward * Main.rand.NextFloat(0.35f, 1.6f),
                    Color.Lerp(FireColor, GoldColor, Main.rand.NextFloat()), Color.Transparent,
                    Main.rand.NextFloat(0.18f, 0.36f) * fade, Main.rand.Next(12, 20), Main.rand.NextFloat(-0.04f, 0.04f)));
            }
        }

        private void SpawnWalls()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            int wallType = ModContent.ProjectileType<AegisWallProjectile>();
            float riseSpeed = AegisWallProjectile.WallHalfHeight / (float)BalanceAegisBlade.WallRiseTime;
            int wallDamage = System.Math.Max(1, (int)(Projectile.damage * 0.8f));

            // 速凝掩体最多同时存在 2 个，超出时先清除剩余时间最少（最老）的
            int wallCount = 0;
            int oldestIdx = -1;
            int minTimeLeft = int.MaxValue;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || p.type != wallType || p.owner != Projectile.owner) continue;
                wallCount++;
                if (p.timeLeft < minTimeLeft) { minTimeLeft = p.timeLeft; oldestIdx = i; }
            }
            if (wallCount >= 2 && oldestIdx >= 0)
                Main.projectile[oldestIdx].Kill();

            Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                new Vector2(Projectile.Center.X, Projectile.Center.Y), new Vector2(0f, -riseSpeed),
                wallType, wallDamage, 4f, Projectile.owner);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D swordTexture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = new(0f, swordTexture.Height);
            float fade = EmbeddedFade;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            float glowStrength = embedded ? fade : 0.55f;
            for (int i = 0; i < 6; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 2.4f * glowStrength;
                Main.EntitySpriteDraw(swordTexture, drawPosition + offset, null, GoldColor with { A = 0 } * glowStrength * 0.1f,
                    Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            }
            if (embedded)
            {
                float dissolveProgress = 1f - fade;
                for (int i = 0; i < 16; i++)
                {
                    float angle = MathHelper.TwoPi * i / 16f + dissolveProgress * 1.7f;
                    Vector2 offset = angle.ToRotationVector2() * MathHelper.Lerp(3f, 22f, dissolveProgress);
                    Color copyColor = Color.Lerp(CoreColor, GoldColor, i / 16f) with { A = 0 };
                    Main.EntitySpriteDraw(swordTexture, drawPosition + offset, null, copyColor * fade * 0.13f,
                        Projectile.rotation, origin, Projectile.scale * (1f + dissolveProgress * 0.08f), SpriteEffects.None);
                }
            }
            Main.EntitySpriteDraw(bloomTexture, drawPosition, null, CoreColor with { A = 0 } * glowStrength * 0.5f,
                0f, bloomTexture.Size() * 0.5f, 0.55f + glowStrength * 0.25f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();

            Color bodyColor = embedded ? Color.Lerp(lightColor, CoreColor, 0.55f) * fade : lightColor;
            Main.EntitySpriteDraw(swordTexture, drawPosition, null, bodyColor,
                Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
