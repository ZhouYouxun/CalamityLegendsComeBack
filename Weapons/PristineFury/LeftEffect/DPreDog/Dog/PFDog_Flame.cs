using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFDog_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/FireProj";

        private const int Lifetime = 96;
        private const int FadeTime = 80;
        private ref float Timer => ref Projectile.localAI[0];
        private int mistFrame = -1;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 7;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 4;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 3;
        }

        public override void AI()
        {
            Timer++;
            mistFrame = mistFrame == -1 ? Main.rand.Next(3) : mistFrame;

            if (Timer > FadeTime)
                Projectile.velocity *= 0.95f;

            if (Timer > 6f && Timer < FadeTime)
            {
                if (Main.rand.NextBool(16))
                {
                    Dust dust = Dust.NewDustDirect(Projectile.Center + Main.rand.NextVector2Circular(60f, 60f) * Utils.Remap(Timer, 0f, FadeTime, 0.5f, 1f), 4, 4, DustID.CorruptTorch, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100);
                    dust.velocity += Projectile.velocity * Utils.Remap(Timer, 0f, FadeTime * 0.75f, 1f, 0.1f);
                    dust.velocity *= 1.1f;
                    if (Main.rand.NextBool(5))
                    {
                        dust.noGravity = true;
                        dust.scale *= 2f;
                    }
                }

                if (Main.rand.NextBool(19))
                {
                    float size = Utils.Remap(Utils.GetLerpValue(0f, Lifetime, Timer), 0.2f, 0.5f, 0.25f, 1f);
                    Particle trail = new CustomSpark(Projectile.Center, Projectile.velocity + Vector2.UnitY * Main.rand.NextFloat(-10f, -24f) * size, "CalamityMod/Particles/BloomCircle", false, 14, 0.9f * size, (Main.rand.NextBool() ? Color.DarkBlue : Color.BlueViolet) * 0.5f, new Vector2(Main.rand.NextFloat(2f, 3f), 1f), true, true, shrinkSpeed: 0.3f, glowOpacity: 0.5f);
                    GeneralParticleHandler.SpawnParticle(trail);
                }
            }
            else if (Timer == 5f)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? 295 : 181, Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(30f)) * Main.rand.NextFloat(0.5f, 1f));
                dust.scale = Main.rand.NextFloat(0.8f, 1.8f);
                dust.noGravity = true;
                dust.fadeIn = 0.5f;
            }
        }

        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            int size = (int)Utils.Remap(Timer, 0f, FadeTime, 10f, 42f);
            if (Timer > FadeTime)
                size = (int)Utils.Remap(Timer, FadeTime, Lifetime, 42f, 0f);
            hitbox.Inflate(size, size);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 1200);

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                Projectile.damage = System.Math.Max(1, (int)(Projectile.damage * 0.75f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D fire = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Texture2D mist = ModContent.Request<Texture2D>("CalamityMod/Particles/MediumMist").Value;
            float timeRatio = Utils.GetLerpValue(0f, Lifetime, Timer);
            float fireSize = Utils.Remap(timeRatio, 0.2f, 0.5f, 0.25f, 1f);
            float length = Timer > FadeTime - 10f ? 0.1f : 0.15f;
            float vOffset = MathHelper.Min(Timer, 20f);

            if (timeRatio >= 1f || mistFrame < 0)
                return false;

            for (float j = 1f; j >= 0f; j -= length)
            {
                Color fireColor =
                    timeRatio < 0.1f ? Color.Lerp(Color.Transparent, new Color(160, 100, 255, 200), Utils.GetLerpValue(0f, 0.1f, timeRatio)) :
                    timeRatio < 0.2f ? Color.Lerp(new Color(160, 100, 255, 200), new Color(160, 50, 255, 70), Utils.GetLerpValue(0.1f, 0.2f, timeRatio)) :
                    timeRatio < 0.7f ? Color.Lerp(new Color(160, 50, 255, 70), new Color(120, 100, 255, 100), Utils.GetLerpValue(0.35f, 0.7f, timeRatio)) :
                    Color.Lerp(new Color(120, 100, 255, 100), Color.Transparent, Utils.GetLerpValue(0.7f, 1f, timeRatio));

                fireColor *= (1f - j) * Utils.GetLerpValue(0f, 0.2f, timeRatio, true);
                Vector2 firePos = Projectile.Center - Main.screenPosition - Projectile.velocity * vOffset * j;
                float mainRot = (-j * MathHelper.PiOver2 - Main.GlobalTimeWrappedHourly * (j + 1f) * 2f / length) * System.Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);
                Vector2 trailOffset = Projectile.velocity * vOffset * length * 0.5f;

                Main.EntitySpriteDraw(fire, firePos - trailOffset, null, fireColor * 0.25f, MathHelper.PiOver4 - mainRot, fire.Size() * 0.5f, fireSize, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(fire, firePos, null, fireColor, mainRot, fire.Size() * 0.5f, fireSize, SpriteEffects.None, 0);

                Rectangle frame = mist.Frame(1, 3, 0, mistFrame);
                Main.EntitySpriteDraw(mist, firePos, frame, Color.Lerp(fireColor, Color.White, 0.3f) with { A = 0 }, mainRot, frame.Size() * 0.5f, fireSize, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(mist, firePos, frame, fireColor with { A = 0 }, mainRot, frame.Size() * 0.5f, fireSize * 3f, SpriteEffects.None, 0);
            }

            return false;
        }
    }
}
