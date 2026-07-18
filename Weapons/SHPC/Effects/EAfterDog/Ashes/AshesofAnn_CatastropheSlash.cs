using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ashes
{
    // Weapon-side version of Supreme Catastrophe's slash. Its direction is intentionally locked:
    // acceleration and the animated blade are its entire threat profile, never homing.
    internal sealed class AshesofAnn_CatastropheSlash : ModProjectile, ILocalizedModType
    {
        private static readonly Color OutlineColor = new(27, 155, 212);
        private static readonly Color AccentColor = new(191, 250, 255);

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private ref float Time => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 9;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 96;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * MathHelper.Max(Projectile.velocity.Length(), 30f);
            if (!Main.dedServ)
                SpawnBladeCastEffects();
        }

        public override void AI()
        {
            Time++;
            // The blade is locked to its launch lane. It shares the fist's fast opening pass,
            // then slows down so a large target can receive multiple local-immunity hits.
            float speed = MathHelper.Max(6f, Projectile.velocity.Length() * 0.981f);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
            Projectile.frame = (int)(Time / 4f) % Main.projFrames[Type];
            Projectile.spriteDirection = Projectile.velocity.X < 0f ? -1 : 1;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = Utils.GetLerpValue(0f, 5f, Time, true) * Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, OutlineColor.ToVector3() * Projectile.Opacity * 0.5f);

            if (!Main.dedServ && (int)Time % 2 == 0)
                SpawnBladeEffects();
        }

        private void SpawnBladeEffects()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center - direction * Main.rand.NextFloat(12f, 28f) + normal * Main.rand.NextFloat(-11f, 11f),
                DustID.RainbowTorch,
                -direction * Main.rand.NextFloat(2f, 4.8f) + normal * Main.rand.NextFloat(-1f, 1f));
            dust.noGravity = true;
            dust.color = Color.Lerp(OutlineColor, AccentColor, Main.rand.NextFloat());
            dust.scale = Main.rand.NextFloat(0.55f, 0.94f);

            if ((int)Time % 4 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center - direction * 17f + normal * Main.rand.NextFloat(-9f, 9f),
                    -direction * Main.rand.NextFloat(1.1f, 2.6f),
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.22f, 0.34f),
                    OutlineColor,
                    true,
                    false));
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    Projectile.Center - direction * 20f,
                    -direction * 0.8f + normal * Main.rand.NextFloat(-0.35f, 0.35f),
                    AccentColor,
                    new Color(0, 28, 70),
                    Main.rand.NextFloat(0.24f, 0.36f),
                    Main.rand.NextFloat(120f, 165f),
                    0.025f));
            }
        }

        private void SpawnBladeCastEffects()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                direction * 0.2f,
                OutlineColor,
                new Vector2(0.98f, 0.25f),
                direction.ToRotation(),
                0.025f,
                0.72f,
                15));
            GeneralParticleHandler.SpawnParticle(new BloomParticle(
                Projectile.Center,
                Vector2.Zero,
                AccentColor,
                0.04f,
                0.58f,
                15,
                false));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<VulnerabilityHex>(), 90);
            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                direction * 0.15f,
                OutlineColor,
                new Vector2(1.02f, 0.30f),
                direction.ToRotation(),
                0.05f,
                0.82f,
                16));
            GeneralParticleHandler.SpawnParticle(new BloomParticle(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(AccentColor, Color.White, 0.4f),
                0.05f,
                0.74f,
                17,
                false));
            for (int i = 0; i < 5; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.82f) * Main.rand.NextFloat(3.5f, 7.5f);
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    Projectile.Center + velocity * 0.3f,
                    velocity,
                    false,
                    Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.38f, 0.62f),
                    AccentColor));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/SupremeCatastropheSlash").Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            SpriteEffects effects = Projectile.spriteDirection < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            for (int i = 5; i >= 1; i--)
            {
                float opacity = (6 - i) / 6f * 0.30f * Projectile.Opacity;
                Vector2 afterimagePosition = Projectile.Center - Main.screenPosition - direction * i * 7f;
                Main.EntitySpriteDraw(texture, afterimagePosition, frame, OutlineColor * opacity, Projectile.rotation, origin, Projectile.scale, effects, 0);
            }

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2[] outlineOffsets =
            [
                Vector2.UnitX * 1.5f, -Vector2.UnitX * 1.5f, Vector2.UnitY * 1.5f, -Vector2.UnitY * 1.5f,
                new Vector2(1.1f, 1.1f), new Vector2(-1.1f, 1.1f), new Vector2(1.1f, -1.1f), new Vector2(-1.1f, -1.1f)
            ];
            foreach (Vector2 offset in outlineOffsets)
                Main.EntitySpriteDraw(texture, drawPosition + offset, frame, OutlineColor * Projectile.Opacity * 0.8f, Projectile.rotation, origin, Projectile.scale, effects, 0);

            Main.EntitySpriteDraw(texture, drawPosition, frame, Color.White * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, effects, 0);
            return false;
        }
    }
}
