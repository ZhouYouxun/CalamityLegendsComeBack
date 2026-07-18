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
    // Weapon-side version of Supreme Cataclysm's fist. The brand supplies its lane, therefore
    // it never target-searches or turns after launch.
    internal sealed class AshesofAnn_CataclysmFist : ModProjectile, ILocalizedModType
    {
        private static readonly Color OutlineColor = new(181, 17, 72);
        private static readonly Color AccentColor = new(255, 87, 152);

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Time => ref Projectile.localAI[0];
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
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
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
            // A deliberate linear pass: high opening speed, then a gentle falloff that leaves
            // time for the same target to be contacted repeatedly through local immunity.
            float speed = MathHelper.Max(6f, Projectile.velocity.Length() * 0.9825f);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;

            Projectile.frame = (int)(Time / 4f) % Main.projFrames[Type];
            Projectile.spriteDirection = Projectile.velocity.X < 0f ? -1 : 1;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = Utils.GetLerpValue(0f, 5f, Time, true) * Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, OutlineColor.ToVector3() * Projectile.Opacity * 0.45f);

            if (!Main.dedServ && (int)Time % 2 == 0)
                SpawnFlightEffects();
        }

        private void SpawnFlightEffects()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = Projectile.Center - direction * Main.rand.NextFloat(10f, 25f) + normal * Main.rand.NextFloat(-13f, 13f);
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

            Dust dust = Dust.NewDustPerfect(Projectile.Center - direction * 14f + normal * Main.rand.NextFloat(-9f, 9f), DustID.SilverFlame, -direction * Main.rand.NextFloat(1.2f, 3.6f));
            dust.noGravity = true;
            dust.color = OutlineColor;
            dust.scale = Main.rand.NextFloat(0.65f, 1.05f);

            if ((int)Time % 4 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    Projectile.Center - direction * 18f,
                    -direction * Main.rand.NextFloat(0.45f, 1.15f) + normal * Main.rand.NextFloat(-0.4f, 0.4f),
                    AccentColor,
                    new Color(55, 0, 25),
                    Main.rand.NextFloat(0.28f, 0.42f),
                    Main.rand.NextFloat(135f, 180f),
                    0.018f));
            }
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
                AccentColor,
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
                0.72f,
                15));
            GeneralParticleHandler.SpawnParticle(new BloomParticle(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(AccentColor, Color.White, 0.35f),
                0.05f,
                0.68f,
                16,
                false));

            for (int i = 0; i < 6; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.9f) * Main.rand.NextFloat(2.5f, 7.5f);
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    Projectile.Center + velocity * 0.45f,
                    velocity,
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.44f, 0.72f),
                    Main.rand.NextBool() ? OutlineColor : AccentColor));
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
            Vector2[] outlineOffsets =
            [
                Vector2.UnitX * 2f, -Vector2.UnitX * 2f, Vector2.UnitY * 2f, -Vector2.UnitY * 2f,
                new Vector2(1.4f, 1.4f), new Vector2(-1.4f, 1.4f), new Vector2(1.4f, -1.4f), new Vector2(-1.4f, -1.4f)
            ];
            foreach (Vector2 offset in outlineOffsets)
                Main.EntitySpriteDraw(texture, drawPosition + offset, frame, OutlineColor * Projectile.Opacity * 0.78f, Projectile.rotation, origin, Projectile.scale, effects, 0);

            Main.EntitySpriteDraw(texture, drawPosition, frame, Color.White * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, effects, 0);
            return false;
        }
    }
}
