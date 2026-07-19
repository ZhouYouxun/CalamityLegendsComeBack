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
    // Weapon-side version of Supreme Catastrophe's slash. CurseFire impact supplies a locked
    // target, which the blade actively tracks until it connects.
    internal sealed class AshesofAnn_CatastropheSlash : ModProjectile, ILocalizedModType
    {
        private const float VisualBrightness = 0.67f;
        private static readonly Color OutlineColor = new Color(0, 104, 230) * VisualBrightness;
        private static readonly Color AccentColor = new Color(50, 221, 255) * VisualBrightness;
        private static readonly Color ImpactColor = new Color(166, 248, 255) * VisualBrightness;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private ref float Time => ref Projectile.localAI[0];
        // ai[1] remains synchronized and is reserved for the post-hit fade state.
        private ref float HitFadeStarted => ref Projectile.ai[1];

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
            // Each spawned blade owns its immunity record, then becomes visual-only after its first hit.
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
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
            if (HitFadeStarted <= 0f)
                HomeTowardTrackedTarget();
            // After contact it brakes sharply and dissolves in
            // its own blue aftershock rather than continuing through the arena.
            float speed = HitFadeStarted > 0f
                ? Projectile.velocity.Length() * 0.92f
                : MathHelper.Max(6f, Projectile.velocity.Length() * 0.981f);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
            Projectile.frame = (int)(Time / 4f) % Main.projFrames[Type];
            Projectile.spriteDirection = Projectile.velocity.X < 0f ? -1 : 1;
            Projectile.rotation = Projectile.velocity.ToRotation();
            float fadeOut = HitFadeStarted > 0f
                ? Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true)
                : Utils.GetLerpValue(0f, 24f, Projectile.timeLeft, true);
            Projectile.Opacity = Utils.GetLerpValue(0f, 5f, Time, true) * fadeOut;
            Projectile.alpha = (int)MathHelper.Clamp(255f * (1f - Projectile.Opacity), 0f, 255f);
            Lighting.AddLight(Projectile.Center, OutlineColor.ToVector3() * Projectile.Opacity * 0.5f);

            if (!Main.dedServ)
            {
                if (HitFadeStarted > 0f && Projectile.timeLeft % 3 == 0)
                    SpawnHitDissipationEffects();
                else if (HitFadeStarted <= 0f && (int)Time % 2 == 0)
                    SpawnBladeEffects();
            }
        }

        private void SpawnBladeEffects()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            float longitudinalExtent = Projectile.width * 0.56f;
            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + direction * Main.rand.NextFloat(-longitudinalExtent, longitudinalExtent) + normal * Main.rand.NextFloat(-12f, 12f),
                    DustID.RainbowTorch,
                    -direction * Main.rand.NextFloat(2f, 4.8f) + normal * Main.rand.NextFloat(-1f, 1f));
                dust.noGravity = true;
                dust.color = Color.Lerp(OutlineColor, AccentColor, Main.rand.NextFloat());
                dust.scale = Main.rand.NextFloat(0.55f, 0.94f);
            }

            if ((int)Time % 2 == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + direction * Main.rand.NextFloat(-longitudinalExtent, longitudinalExtent) + normal * Main.rand.NextFloat(-10f, 10f),
                        -direction * Main.rand.NextFloat(1.1f, 2.6f),
                        false,
                        Main.rand.Next(8, 13),
                        Main.rand.NextFloat(0.22f, 0.34f),
                        OutlineColor,
                        true,
                        false));
                }
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

        private void SpawnHitDissipationEffects()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            float longitudinalExtent = Projectile.width * 0.56f;
            float fadeCompletion = 1f - MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);
            Color fadeColor = Color.Lerp(ImpactColor, OutlineColor, fadeCompletion);
            Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(-longitudinalExtent, longitudinalExtent) + normal * Main.rand.NextFloat(-12f, 12f);
            Vector2 velocity = -direction * Main.rand.NextFloat(0.8f, 2.5f) + normal * Main.rand.NextFloat(-1.1f, 1.1f);

            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                position,
                velocity,
                false,
                Main.rand.Next(10, 16),
                Main.rand.NextFloat(0.28f, 0.48f),
                fadeColor,
                true,
                false));
            GeneralParticleHandler.SpawnParticle(new PointParticle(
                position,
                velocity * 1.45f,
                false,
                Main.rand.Next(9, 14),
                Main.rand.NextFloat(0.36f, 0.58f),
                fadeColor));

            if (Projectile.timeLeft % 6 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    Projectile.Center + direction * Main.rand.NextFloat(-longitudinalExtent, longitudinalExtent) + normal * Main.rand.NextFloat(-8f, 8f),
                    -direction * 0.45f + normal * Main.rand.NextFloat(-0.3f, 0.3f),
                    fadeColor,
                    new Color(0, 25, 82),
                    Main.rand.NextFloat(0.24f, 0.38f),
                    Main.rand.NextFloat(125f, 175f),
                    0.025f));
            }
        }

        private void HomeTowardTrackedTarget()
        {
            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs || !Main.npc[targetIndex].CanBeChasedBy(Projectile, false))
                return;

            NPC target = Main.npc[targetIndex];
            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(currentDirection);
            float targetSpeed = MathHelper.Clamp(MathHelper.Lerp(Projectile.velocity.Length(), 38f, 0.08f), 28f, 42f);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredDirection * targetSpeed, 0.12f);
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
                AccentColor * 0.5f,
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
                target.velocity *= 0.85f;
                target.netUpdate = true;
            }
            if (HitFadeStarted <= 0f)
            {
                HitFadeStarted = 1f;
                Projectile.friendly = false;
                Projectile.timeLeft = 20;
                Projectile.velocity *= 0.5f;
                Projectile.netUpdate = true;
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
                0.86f,
                15));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                ImpactColor,
                new Vector2(0.68f, 0.68f),
                direction.ToRotation(),
                0.03f,
                0.58f,
                12));
            GeneralParticleHandler.SpawnParticle(new BloomParticle(
                Projectile.Center,
                Vector2.Zero,
                OutlineColor * 0.25f,
                0.05f,
                0.74f,
                15,
                false));
            GeneralParticleHandler.SpawnParticle(new BloomParticle(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(ImpactColor, Color.White * VisualBrightness, 0.42f) * 0.25f,
                0.03f,
                0.42f,
                11,
                false));
            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.90f) * Main.rand.NextFloat(3.5f, 8f);
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    Projectile.Center + velocity * 0.3f,
                    velocity,
                    false,
                    Main.rand.Next(10, 17),
                    Main.rand.NextFloat(0.40f, 0.66f),
                    Main.rand.NextBool() ? AccentColor : ImpactColor));
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.74f) * Main.rand.NextFloat(2f, 5f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + velocity * 0.35f,
                    velocity,
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.34f, 0.56f),
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

            Main.EntitySpriteDraw(texture, drawPosition, frame, Color.White * Projectile.Opacity * VisualBrightness, Projectile.rotation, origin, Projectile.scale, effects, 0);
            return false;
        }
    }
}
