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
    // Weapon-side version of Supreme Cataclysm's fist. CurseFire impact supplies a locked
    // target, which the fist actively tracks until it connects.
    internal sealed class AshesofAnn_CataclysmFist : ModProjectile, ILocalizedModType
    {
        private const float VisualBrightness = 0.67f;
        private static readonly Color OutlineColor = new Color(181, 17, 72) * VisualBrightness;
        private static readonly Color AccentColor = new Color(255, 87, 152) * VisualBrightness;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Time => ref Projectile.localAI[0];
        // ai[1] is synchronized, so the hit-fade is identical for every multiplayer client.
        private ref float HitFadeStarted => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 126;
            Projectile.height = 54;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 96;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            // Each spawned fist owns its immunity record, then becomes visual-only after its first hit.
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * MathHelper.Max(Projectile.velocity.Length(), 30f);
            if (!Main.dedServ)
                SpawnFistCastEffects();
        }

        public override void AI()
        {
            Time++;
            ApplyApproachSlowdown();
            if (HitFadeStarted <= 0f)
                HomeTowardTrackedTarget();
            // A hit turns the fist into a short, fading aftershock instead of allowing it to
            // travel across the encounter after connecting.
            float speed = HitFadeStarted > 0f
                ? Projectile.velocity.Length() * 0.92f
                : MathHelper.Max(6f, Projectile.velocity.Length() * 0.9825f);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;

            Projectile.frame = (int)(Time / 4f) % Main.projFrames[Type];
            Projectile.spriteDirection = Projectile.velocity.X < 0f ? -1 : 1;
            Projectile.rotation = Projectile.velocity.ToRotation();
            float fadeOut = HitFadeStarted > 0f
                ? Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true)
                : Utils.GetLerpValue(0f, 24f, Projectile.timeLeft, true);
            Projectile.Opacity = Utils.GetLerpValue(0f, 5f, Time, true) * fadeOut;
            Projectile.alpha = (int)MathHelper.Clamp(255f * (1f - Projectile.Opacity), 0f, 255f);
            Lighting.AddLight(Projectile.Center, OutlineColor.ToVector3() * Projectile.Opacity * 0.45f);

            if (!Main.dedServ)
            {
                if (HitFadeStarted > 0f && Projectile.timeLeft % 3 == 0)
                    SpawnHitDissipationEffects();
                else if (HitFadeStarted <= 0f && (int)Time % 2 == 0)
                    SpawnFlightEffects();
            }
        }

        private void SpawnFlightEffects()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            float longitudinalExtent = Projectile.width * 0.56f;
            for (int i = 0; i < 3; i++)
            {
                Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(-longitudinalExtent, longitudinalExtent) + normal * Main.rand.NextFloat(-14f, 14f);
                Vector2 velocity = -direction * Main.rand.NextFloat(2f, 5f) + normal * Main.rand.NextFloat(-1.6f, 1.6f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    position,
                    velocity,
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.28f, 0.46f),
                    Main.rand.NextBool() ? OutlineColor : AccentColor,
                    true,
                    false));
            }

            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + direction * Main.rand.NextFloat(-longitudinalExtent, longitudinalExtent) + normal * Main.rand.NextFloat(-11f, 11f),
                    DustID.SilverFlame,
                    -direction * Main.rand.NextFloat(1.2f, 3.6f));
                dust.noGravity = true;
                dust.color = OutlineColor;
                dust.scale = Main.rand.NextFloat(0.65f, 1.05f);
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
            Color fadeColor = Color.Lerp(AccentColor, OutlineColor, fadeCompletion);
            Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(-longitudinalExtent, longitudinalExtent) + normal * Main.rand.NextFloat(-14f, 14f);
            Vector2 velocity = -direction * Main.rand.NextFloat(0.8f, 2.4f) + normal * Main.rand.NextFloat(-1.2f, 1.2f);

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
                    Projectile.Center + direction * Main.rand.NextFloat(-longitudinalExtent, longitudinalExtent) + normal * Main.rand.NextFloat(-9f, 9f),
                    -direction * 0.45f + normal * Main.rand.NextFloat(-0.3f, 0.3f),
                    fadeColor,
                    new Color(45, 0, 22),
                    Main.rand.NextFloat(0.26f, 0.40f),
                    Main.rand.NextFloat(125f, 175f),
                    0.02f));
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

        private void SpawnFistCastEffects()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                direction * 0.25f,
                OutlineColor,
                new Vector2(0.92f, 0.30f),
                direction.ToRotation(),
                0.03f,
                0.66f,
                14));
            GeneralParticleHandler.SpawnParticle(new BloomParticle(
                Projectile.Center,
                Vector2.Zero,
                AccentColor * 0.5f,
                0.04f,
                0.58f,
                15,
                false));

            for (int i = 0; i < 4; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.34f) * Main.rand.NextFloat(3.5f, 7f);
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    Projectile.Center + velocity * 0.35f,
                    velocity,
                    false,
                    Main.rand.Next(10, 15),
                    Main.rand.NextFloat(0.42f, 0.62f),
                    Main.rand.NextBool() ? OutlineColor : AccentColor));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 150);
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
                AccentColor,
                new Vector2(0.90f, 0.34f),
                Projectile.rotation,
                0.04f,
                0.82f,
                15));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                OutlineColor,
                new Vector2(0.70f, 0.70f),
                direction.ToRotation(),
                0.03f,
                0.56f,
                12));
            GeneralParticleHandler.SpawnParticle(new BloomParticle(
                Projectile.Center,
                Vector2.Zero,
                OutlineColor * 0.25f,
                0.05f,
                0.72f,
                15,
                false));
            GeneralParticleHandler.SpawnParticle(new BloomParticle(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(AccentColor, Color.White * VisualBrightness, 0.35f) * 0.25f,
                0.03f,
                0.40f,
                11,
                false));

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.92f) * Main.rand.NextFloat(3f, 7.5f);
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    Projectile.Center + velocity * 0.45f,
                    velocity,
                    false,
                    Main.rand.Next(11, 18),
                    Main.rand.NextFloat(0.42f, 0.68f),
                    Main.rand.NextBool() ? OutlineColor : AccentColor));
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.72f) * Main.rand.NextFloat(1.8f, 4.8f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + velocity * 0.4f,
                    velocity,
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.34f, 0.56f),
                    Main.rand.NextBool() ? OutlineColor : AccentColor,
                    true,
                    false));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/SupremeCataclysmFist").Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            SpriteEffects effects = Projectile.spriteDirection < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
            Vector2 origin = frame.Size() * 0.5f;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 oldDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, oldDrawPosition, frame, OutlineColor * completion * 0.22f * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale * (0.82f + completion * 0.18f), effects, 0);
            }

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            // These two outline layers are always drawn while the projectile exists. The
            // red outer rim keeps the fist legible even when its body is fading out.
            Vector2[] outerOutlineOffsets =
            [
                Vector2.UnitX * 2.8f, -Vector2.UnitX * 2.8f, Vector2.UnitY * 2.8f, -Vector2.UnitY * 2.8f,
                new Vector2(2f, 2f), new Vector2(-2f, 2f), new Vector2(2f, -2f), new Vector2(-2f, -2f)
            ];
            Vector2[] innerOutlineOffsets = [Vector2.UnitX * 1.35f, -Vector2.UnitX * 1.35f, Vector2.UnitY * 1.35f, -Vector2.UnitY * 1.35f];
            foreach (Vector2 offset in outerOutlineOffsets)
                Main.EntitySpriteDraw(texture, drawPosition + offset, frame, OutlineColor * Projectile.Opacity * 0.66f, Projectile.rotation, origin, Projectile.scale, effects, 0);
            foreach (Vector2 offset in innerOutlineOffsets)
                Main.EntitySpriteDraw(texture, drawPosition + offset, frame, AccentColor * Projectile.Opacity * 0.82f, Projectile.rotation, origin, Projectile.scale, effects, 0);

            Main.EntitySpriteDraw(texture, drawPosition, frame, Color.White * Projectile.Opacity * VisualBrightness, Projectile.rotation, origin, Projectile.scale, effects, 0);
            return false;
        }
    }
}
