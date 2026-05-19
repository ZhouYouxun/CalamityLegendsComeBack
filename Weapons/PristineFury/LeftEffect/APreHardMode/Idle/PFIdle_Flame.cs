using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFIdle_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Particles/MediumMist";

        private ref float Timer => ref Projectile.localAI[0];
        private Vector2 beamA;
        private Vector2 beamB;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 96;
            Projectile.extraUpdates = 4;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 0.997f;

            float age = Utils.GetLerpValue(96f, 44f, Projectile.timeLeft, true);
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float sine = (float)Math.Sin(Timer * 0.54f / MathHelper.Pi);
            beamA = Projectile.Center + side * sine * -30f * age;
            beamB = Projectile.Center + side * sine * 30f * age;

            Lighting.AddLight(Projectile.Center, Color.Lerp(Color.Red, Color.Goldenrod, age).ToVector3() * 0.7f);
            if (Main.dedServ || Projectile.timeLeft > 92)
                return;

            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = i == 0 ? beamA : beamB;
                Particle beam = new CustomSpark(
                    pos,
                    Projectile.velocity * 0.08f,
                    "CalamityMod/Particles/SmallBloom",
                    false,
                    7,
                    0.04f + 0.04f * age,
                    Color.Lerp(Color.Red, Color.Goldenrod, age),
                    new Vector2(1f, 2.2f),
                    true,
                    false);
                GeneralParticleHandler.SpawnParticle(beam);
            }

            if (Timer % 7f == 0f)
            {
                Particle ember = new SparkParticle(
                    Projectile.Center - forward * 8f + Main.rand.NextVector2Circular(3f, 3f),
                    -forward.RotatedByRandom(0.22f) * Main.rand.NextFloat(1.2f, 3.4f),
                    true,
                    Main.rand.Next(14, 22),
                    Main.rand.NextFloat(0.55f, 0.95f),
                    Main.rand.NextBool() ? Color.Orange : Color.DarkOrange);
                GeneralParticleHandler.SpawnParticle(ember);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(ModContent.BuffType<HolyFlames>(), 240);

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 6; i++)
            {
                Vector2 linePos = i < 3 ? beamA : beamB;
                Vector2 velocity = linePos.DirectionTo(Projectile.Center + forward * 40f).RotatedByRandom(0.18f) * Main.rand.NextFloat(1.2f, 4f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(linePos, velocity, "CalamityMod/Particles/SmallBloom", false, 12, 0.075f, Main.rand.NextBool() ? Color.Orange : Color.DarkOrange, new Vector2(1.8f, 1.2f), true, false));
            }

            for (int i = 0; i < 5; i++)
            {
                Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(26f, 26f);
                Vector2 vel = Vector2.UnitY * Main.rand.NextFloat(-10f, -6f) * Main.rand.NextFloat(0.4f, 1.4f);
                Particle smoke = new MediumMistParticle(pos, vel, Main.rand.NextBool() ? Color.Orange : Color.DarkOrange, Color.Black, Main.rand.NextFloat(0.6f, 1.35f), 170, 0.08f);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D mist = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(0f, 12f, Timer, true) * Utils.GetLerpValue(0f, 24f, Projectile.timeLeft, true);
            float scale = MathHelper.Lerp(0.18f, 0.62f, Utils.GetLerpValue(0f, 34f, Timer, true));
            Color color = Color.Lerp(Color.OrangeRed, Color.Goldenrod, Utils.GetLerpValue(80f, 36f, Projectile.timeLeft, true)) with { A = 0 };

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(bloom, drawPosition, null, color * opacity * 0.35f, Projectile.rotation, bloom.Size() * 0.5f, scale * 0.55f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(mist, drawPosition, null, color * opacity * 0.55f, Projectile.rotation + MathHelper.PiOver2, mist.Size() * 0.5f, new Vector2(scale * 0.38f, scale * 0.82f), SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
