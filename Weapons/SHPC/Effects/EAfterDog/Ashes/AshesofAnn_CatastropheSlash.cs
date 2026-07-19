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
        private static readonly Color OutlineColor = new(0, 104, 230);
        private static readonly Color AccentColor = new(50, 221, 255);
        private static readonly Color ImpactColor = new(166, 248, 255);

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
            ApplyApproachSlowdown();
            // The blade is locked to its launch lane. It shares the fist's fast opening pass,
            // then slows down so a large target can receive multiple local-immunity hits.
            float speed = MathHelper.Max(6f, Projectile.velocity.Length() * 0.981f);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
            Projectile.frame = (int)(Time / 4f) % Main.projFrames[Type];
            Projectile.spriteDirection = Projectile.velocity.X < 0f ? -1 : 1;
            Projectile.rotation = Projectile.velocity.ToRotation();
            // A longer alpha fade keeps the blade's exit clean instead of letting it pop.
            Projectile.Opacity = Utils.GetLerpValue(0f, 5f, Time, true) * Utils.GetLerpValue(0f, 24f, Projectile.timeLeft, true);
            Projectile.alpha = (int)MathHelper.Clamp(255f * (1f - Projectile.Opacity), 0f, 255f);
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

        private void ApplyApproachSlowdown()
        {
            if (Time != 1f || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs || !Main.npc[targetIndex].CanBeChasedBy(Projectile, false))
                return;

            NPC target = Main.npc[targetIndex];
            target.velocity *= 0.99f;
            target.netUpdate = true;
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
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                target.velocity *= 0.88f;
                target.netUpdate = true;
            }
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
                1.10f,
                20));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                ImpactColor,
                new Vector2(0.68f, 0.68f),
                direction.ToRotation(),
                0.03f,
                0.88f,
                17));
            GeneralParticleHandler.SpawnParticle(new BloomParticle(
                Projectile.Center,
                Vector2.Zero,
                OutlineColor,
                0.05f,
                1.08f,
                20,
                false));
            GeneralParticleHandler.SpawnParticle(new BloomParticle(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(ImpactColor, Color.White, 0.42f),
                0.03f,
                0.78f,
                16,
                false));
            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(1.02f) * Main.rand.NextFloat(3.5f, 10.5f);
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    Projectile.Center + velocity * 0.3f,
                    velocity,
                    false,
                    Main.rand.Next(10, 17),
                    Main.rand.NextFloat(0.48f, 0.82f),
                    Main.rand.NextBool() ? AccentColor : ImpactColor));
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.74f) * Main.rand.NextFloat(2f, 5f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + velocity * 0.35f,
                    velocity,
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.46f, 0.76f),
                    Main.rand.NextBool() ? AccentColor : ImpactColor,
                    true,
                    false));
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
            // The blue tech rim is a persistent two-layer outline, including during fade-out.
            Vector2[] outerOutlineOffsets =
            [
                Vector2.UnitX * 2.35f, -Vector2.UnitX * 2.35f, Vector2.UnitY * 2.35f, -Vector2.UnitY * 2.35f,
                new Vector2(1.7f, 1.7f), new Vector2(-1.7f, 1.7f), new Vector2(1.7f, -1.7f), new Vector2(-1.7f, -1.7f)
            ];
            Vector2[] innerOutlineOffsets = [Vector2.UnitX * 1.15f, -Vector2.UnitX * 1.15f, Vector2.UnitY * 1.15f, -Vector2.UnitY * 1.15f];
            foreach (Vector2 offset in outerOutlineOffsets)
                Main.EntitySpriteDraw(texture, drawPosition + offset, frame, OutlineColor * Projectile.Opacity * 0.70f, Projectile.rotation, origin, Projectile.scale, effects, 0);
            foreach (Vector2 offset in innerOutlineOffsets)
                Main.EntitySpriteDraw(texture, drawPosition + offset, frame, ImpactColor * Projectile.Opacity * 0.84f, Projectile.rotation, origin, Projectile.scale, effects, 0);

            Main.EntitySpriteDraw(texture, drawPosition, frame, Color.White * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, effects, 0);
            return false;
        }
    }
}
