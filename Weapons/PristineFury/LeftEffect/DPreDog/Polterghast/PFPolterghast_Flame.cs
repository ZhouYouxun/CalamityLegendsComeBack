using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFPolterghast_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float FrameTimer => ref Projectile.localAI[1];
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(115, 232, 255));

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 72;
            Projectile.extraUpdates = 6;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Timer++;
            if (Projectile.numUpdates == 0)
                FrameTimer++;

            bool drawingUpdate = Projectile.numUpdates == 0;
            if (Timer > 3f && drawingUpdate)
            {
                Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center + back * 3f,
                    back * Main.rand.NextFloat(0.6f, 1.4f),
                    false,
                    7,
                    Main.rand.NextFloat(0.24f, 0.38f),
                    Color.Lerp(ThemeColor, Color.White, 0.32f)));

                if (FrameTimer > 5f && Main.rand.NextBool(2))
                    SpawnThreadedTrail(back);
            }
            else if (Timer == 3f)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Projectile.velocity * 0.35f, ThemeColor, new Vector2(0.42f, 1.9f), Projectile.rotation, 0.12f, 0.02f, 14));
                ReleaseCometDust(6, 0.16f);
            }

            if (Projectile.numUpdates == 0)
                Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 0.28f);
        }

        private void ReleaseCometDust(int count, float spread)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch, Projectile.velocity);
                dust.scale = Main.rand.NextFloat(0.72f, 1.1f);
                dust.velocity = -Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(spread) * Main.rand.NextFloat(0.4f, 1.8f);
                dust.noGravity = true;
                dust.color = Main.rand.NextBool() ? ThemeColor : Color.Lerp(ThemeColor, Color.White, 0.45f);
            }
        }

        private void SpawnThreadedTrail(Vector2 back)
        {
            if (Main.dedServ)
                return;

            Vector2 side = back.RotatedBy(MathHelper.PiOver2);
            Vector2 offset = side * Main.rand.NextFloat(-4.5f, 4.5f);
            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                Projectile.Center + offset,
                back * Main.rand.NextFloat(0.9f, 2.1f) + side * Main.rand.NextFloat(-0.45f, 0.45f),
                "CalamityMod/Particles/ThinEndedLine",
                false,
                Main.rand.Next(8, 13),
                Main.rand.NextFloat(0.1f, 0.16f),
                Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.24f, 0.56f)),
                new Vector2(0.34f, 1.45f),
                true,
                false,
                glowOpacity: 0.48f));

            if (Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center + offset * 0.55f,
                    back.RotatedByRandom(0.36f) * Main.rand.NextFloat(0.8f, 1.8f),
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.2f, 0.34f),
                    Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.32f, 0.68f))));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Nightwither>(), 300);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/PhantomSpirit") { Volume = 0.28f, Pitch = 0.16f, MaxInstances = 8 }, target.Center);
            ReleaseCometDust(7, 0.22f);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/SubsumingVortexExplosion") { Volume = 0.22f, Pitch = 0.1f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (FrameTimer <= 5f)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/ForwardSmear").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color color = (Color.Lerp(ThemeColor, Color.White, 0.28f) with { A = 0 }) * Projectile.Opacity;

            PFLeftEffectRules.BeginAdditive();
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float fade = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(bloom, trailPosition, null, color * 0.28f * fade, Projectile.rotation, bloom.Size() * 0.5f, 0.06f * fade, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(smear, drawPosition - direction * 7f, null, color * 0.72f, Projectile.rotation - MathHelper.PiOver2, new Vector2(smear.Width * 0.5f, smear.Height), new Vector2(0.16f, 0.52f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, drawPosition, null, color * 0.46f, Projectile.rotation, bloom.Size() * 0.5f, 0.09f, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
