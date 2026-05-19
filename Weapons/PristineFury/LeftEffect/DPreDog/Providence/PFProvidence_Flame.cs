using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFProvidence_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Magic/RancorFog";

        private const int PrimaryLife = 120;
        private const int SecondaryLife = 230;
        private const int IgnitionFrames = 38;
        private ref float Timer => ref Projectile.localAI[0];
        private ref float ScaleFactor => ref Projectile.localAI[1];
        private bool Secondary => Projectile.ai[0] == 1f;
        private bool Ignited => Projectile.ai[0] == 1f && Projectile.ai[1] < 0f;
        private float LightPower;
        private Vector2 beamA;
        private Vector2 beamB;
        private Color FogColor => Ignited ? Color.Lerp(Color.OrangeRed, Color.Goldenrod, 0.55f) : Color.Orchid;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = PrimaryLife;
            Projectile.extraUpdates = 5;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (!Secondary)
                return;

            Projectile.width = Projectile.height = 150;
            Projectile.penetrate = -1;
            Projectile.timeLeft = SecondaryLife;
            Projectile.extraUpdates = 0;
            Projectile.scale = Main.rand.NextFloat(0.62f, 1.15f);
            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            ScaleFactor = 0f;
        }

        public override void AI()
        {
            Timer++;
            if (Secondary)
                SecondaryAI();
            else
                PrimaryAI();
        }

        private void PrimaryAI()
        {
            float mult = Utils.GetLerpValue(140f, 75f, Projectile.timeLeft, true);
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Projectile.timeLeft < 116)
            {
                float sine = (float)Math.Sin(Timer * 0.65f / MathHelper.Pi);
                beamA = Projectile.Center + side * sine * (30f * mult * -1f) * Utils.GetLerpValue(116f, 108f, Projectile.timeLeft, true);
                beamB = Projectile.Center + side * sine * (30f * mult) * Utils.GetLerpValue(116f, 108f, Projectile.timeLeft, true);
                EmitProvidenceBeam(mult);
            }

            TryIgniteSecondary();
            Lighting.AddLight(Projectile.Center, Color.Lerp(Color.Red, Color.Goldenrod, mult).ToVector3() * 0.7f);
            Projectile.velocity *= 0.997f;
        }

        private void SecondaryAI()
        {
            if (Ignited)
            {
                Projectile.scale *= 1.06f;
                ScaleFactor *= 1.035f;
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, IgnitionFrames);
                if (Timer % 8f == 0f && !Main.dedServ)
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, FogColor, "CalamityMod/Particles/ProvidenceMarkParticle", Vector2.One, Projectile.rotation, 0.04f, 0.8f * Projectile.scale, 20, false));
            }

            ScaleFactor += 0.014f;
            ScaleFactor = MathHelper.Clamp(ScaleFactor, 0f, Projectile.scale);
            Projectile.rotation += Projectile.velocity.ToRotation() * 0.01f + 0.01f;
            Projectile.velocity *= Main.rand.NextFloat(0.95f, 0.99f);
            Projectile.Opacity = Utils.GetLerpValue(280f, 135f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 90f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, FogColor.ToVector3() * ScaleFactor);

            if (Main.dedServ)
                return;

            float lightPowerBelow = Lighting.GetColor((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16 + 6).ToVector3().Length() / (float)Math.Sqrt(3D);
            LightPower = MathHelper.Lerp(LightPower, lightPowerBelow, 0.15f);

            if (Projectile.timeLeft < 220 && Main.rand.NextBool(Ignited ? 18 : 36))
            {
                Vector2 vel = Vector2.One.RotatedByRandom(100f) * Main.rand.NextFloat(7f, 25f) * Projectile.Opacity * Projectile.scale;
                Particle mark = new CustomSpark(Projectile.Center + vel * 4f * Projectile.Opacity, vel * 0.05f, "CalamityMod/Particles/ProvidenceMarkParticle", false, 25, Main.rand.NextFloat(0.85f, 1.2f) * (Ignited ? 1.6f : 1f), FogColor * Projectile.Opacity * 0.4f, new Vector2(1.3f, 0.5f), true, false, Ignited ? 0f : Main.rand.NextFloat(-4f, 4f), false, false, Ignited ? 0.5f : 0f);
                GeneralParticleHandler.SpawnParticle(mark);
            }
        }

        private void EmitProvidenceBeam(float mult)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = i == 0 ? beamA : beamB;
                Particle beam = new CustomSpark(pos, Projectile.velocity * 0.1f, "CalamityMod/Particles/SmallBloom", false, 6, 0.065f * mult + 0.01f, Color.Lerp(Color.Red, Color.Goldenrod, mult), new Vector2(1f, 2.5f), true, false);
                GeneralParticleHandler.SpawnParticle(beam);
            }
        }

        private void TryIgniteSecondary()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner || other.type != Type || other.ai[0] != 1f || other.ai[1] < 0f || Vector2.Distance(Projectile.Center, other.Center) > 100f)
                    continue;

                other.ai[1] = -1f;
                other.netUpdate = true;
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceBurn") { Volume = 0.8f, Pitch = Main.rand.NextFloat(0.5f, 0.6f) }, other.Center);
                break;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Secondary)
                return null;

            return Projectile.Opacity < 0.3f ? false : CalamityMod.CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.width * ScaleFactor * 0.5f, targetHitbox);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Secondary || Ignited)
                target.AddBuff(ModContent.BuffType<HolyFlames>(), Secondary ? 1200 : 240);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ || Secondary)
                return;

            for (int i = 0; i < 4; i++)
            {
                Vector2 linePos = i < 2 ? beamA : beamB;
                Vector2 lineVel = linePos.DirectionTo(Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 40f).RotatedByRandom(0.16f) * Main.rand.NextFloat(0.4f, 2.5f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(linePos, lineVel, "CalamityMod/Particles/SmallBloom", false, 11, 0.09f, Main.rand.NextBool() ? Color.Orange : Color.DarkOrange, new Vector2(2f, 1.5f), true, false, glowOpacity: 1.1f));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!Secondary)
                return false;

            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(0f, 0.08f, LightPower, true) * Projectile.Opacity * 0.7f;

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(texture, drawPosition, null, FogColor * opacity, Projectile.rotation, texture.Size() * 0.5f, ScaleFactor, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
