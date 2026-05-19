using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFBrimstoneElemental_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/FireProj";

        private const int Lifetime = 72;
        private const int FadeTime = 58;
        private ref float Timer => ref Projectile.localAI[0];
        private int mistFrame = -1;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 3;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 6;
        }

        public override void AI()
        {
            Timer++;
            mistFrame = mistFrame == -1 ? Main.rand.Next(3) : mistFrame;

            if (Timer < FadeTime && Main.rand.NextBool(5))
            {
                Vector2 cinderPos = Projectile.Center + Main.rand.NextVector2Circular(62f, 62f) * Utils.Remap(Timer, 0f, Lifetime, 0.45f, 1f);
                Dust cinder = Dust.NewDustDirect(cinderPos, 4, 4, ModContent.DustType<BrimstoneFlame>(), Projectile.velocity.X * 0.25f, Projectile.velocity.Y * 0.25f);
                cinder.noGravity = true;
                cinder.scale *= Utils.GetLerpValue(5f, 13f, Timer, true) * Main.rand.NextFloat(1.1f, 2.4f);
                cinder.velocity += Projectile.velocity * Utils.Remap(Timer, 0f, FadeTime * 0.75f, 1f, 0.1f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= Timer > FadeTime ? 0.95f : 0.995f;
            Lighting.AddLight(Projectile.Center, 0.75f, 0.15f, 0.15f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = oldVelocity * 0.94f;
            Projectile.position -= Projectile.velocity;
            Timer++;
            Projectile.timeLeft--;
            return false;
        }

        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            int size = (int)Utils.Remap(Timer, 0f, FadeTime, 10f, 46f);
            if (Timer > FadeTime)
                size = (int)Utils.Remap(Timer, FadeTime, Lifetime, 46f, 0f);
            hitbox.Inflate(size, size);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => projHitbox.Intersects(targetHitbox) && Collision.CanHit(Projectile.Center, 0, 0, targetHitbox.Center.ToVector2(), 0, 0) ? null : false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 1200);

            if (Main.dedServ)
                return;

            int smokeCount = 4 + (int)MathHelper.Clamp(target.width * 0.08f, 0f, 18f);
            for (int i = 0; i < smokeCount; i++)
            {
                Vector2 pos = target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f);
                Vector2 vel = Vector2.UnitY * Main.rand.NextFloat(-2.2f, -0.35f) * MathHelper.Clamp(target.height * 0.08f, 1f, 9f);
                Particle smoke = new MediumMistParticle(pos, vel, new Color(255, 50, 50), Color.DimGray, Main.rand.NextFloat(0.65f, 1.75f), 170, 0.1f);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D fire = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Texture2D mist = ModContent.Request<Texture2D>("CalamityMod/Particles/MediumMist").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float timeRatio = Utils.GetLerpValue(0f, Lifetime, Timer);
            float fireSize = Utils.Remap(timeRatio, 0.16f, 0.52f, 0.25f, 1.08f);
            float vOffset = MathHelper.Min(Timer, 20f);
            float step = Timer > FadeTime - 10f ? 0.1f : 0.15f;

            if (timeRatio >= 1f || mistFrame < 0)
                return false;

            for (float j = 1f; j >= 0f; j -= step)
            {
                Color fireColor =
                    timeRatio < 0.1f ? Color.Lerp(Color.Transparent, new Color(255, 110, 100, 200), Utils.GetLerpValue(0f, 0.1f, timeRatio)) :
                    timeRatio < 0.2f ? Color.Lerp(new Color(255, 110, 100, 200), new Color(255, 50, 50, 70), Utils.GetLerpValue(0.1f, 0.2f, timeRatio)) :
                    timeRatio < 0.7f ? Color.Lerp(new Color(255, 50, 50, 70), new Color(255, 100, 100, 100), Utils.GetLerpValue(0.35f, 0.7f, timeRatio)) :
                    Color.Lerp(new Color(255, 100, 100, 100), Color.Transparent, Utils.GetLerpValue(0.7f, 1f, timeRatio));

                fireColor *= (1f - j) * Utils.GetLerpValue(0f, 0.2f, timeRatio, true);
                Vector2 firePos = drawPosition - Projectile.velocity * vOffset * j;
                float mainRot = -j * MathHelper.PiOver2 - Main.GlobalTimeWrappedHourly * (j + 1f) * 2f / step;
                Vector2 trailOffset = Projectile.velocity * vOffset * step * 0.5f;

                Main.EntitySpriteDraw(fire, firePos - trailOffset, null, Color.Lerp(fireColor, Color.Black, 0.3f) * 0.25f, MathHelper.PiOver4 - mainRot, fire.Size() * 0.5f, fireSize, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(fire, firePos, null, Color.Lerp(fireColor, Color.Black, 0.3f), mainRot, fire.Size() * 0.5f, fireSize, SpriteEffects.None, 0);

                PFLeftEffectRules.BeginAdditive();
                Rectangle frame = mist.Frame(1, 3, 0, mistFrame);
                Main.EntitySpriteDraw(mist, firePos, frame, Color.Lerp(fireColor, Color.White, 0.3f), mainRot, frame.Size() * 0.5f, fireSize, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(mist, firePos, frame, fireColor, mainRot, frame.Size() * 0.5f, fireSize * 2.8f, SpriteEffects.None, 0);
                PFLeftEffectRules.EndAdditive();
            }

            return false;
        }
    }
}
