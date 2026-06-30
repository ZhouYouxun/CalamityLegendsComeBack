using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace.Passive
{
    public class FrostSealDetonation : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Duration = 30;
        private int drawTimer = 0;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Duration;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 5;
        }

        public override void OnSpawn(IEntitySource source)
        {
            SoundEngine.PlaySound(SoundID.Item62 with { Pitch = -0.3f, Volume = 1.2f }, Projectile.Center);
            if (Main.dedServ) return;
            for (int i = 0; i < 42; i++)
            {
                float angle = MathHelper.TwoPi * i / 42f;
                float speed = Main.rand.NextFloat(4f, 10f);
                int dType = i % 3 == 2 ? DustID.Electric : DustID.Frost;
                Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, dType);
                d.velocity = angle.ToRotationVector2() * speed;
                d.scale = Main.rand.NextFloat(1.5f, 2.6f);
                d.noGravity = true;
            }
            Player owner = Main.player[Projectile.owner];
            owner.Calamity().GeneralScreenShakePower = Math.Max(owner.Calamity().GeneralScreenShakePower, 5.5f);
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            drawTimer++;
            if (Main.dedServ) return;
            for (int i = 0; i < 3; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(60f, 270f);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * dist;
                int dType = Main.rand.NextBool(3) ? DustID.Electric : DustID.Frost;
                Dust d = Dust.NewDustDirect(pos, 0, 0, dType);
                d.velocity = angle.ToRotationVector2() * Main.rand.NextFloat(1f, 2.5f);
                d.scale = Main.rand.NextFloat(1f, 1.8f);
                d.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Vector2.Distance(Projectile.Center, targetHitbox.Center.ToVector2())
                   <= 300f + targetHitbox.Width * 0.3f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn2, 480);
            target.AddBuff(BuffID.Chilled, 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/star_01").Value;
            Texture2D twirl = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/twirl_02").Value;
            float time = Main.GlobalTimeWrappedHourly;
            float progress = (float)drawTimer / Duration;
            float rampIn = drawTimer < 4 ? drawTimer / 4f : 1f;
            float alpha = rampIn * (1f - progress);
            Vector2 pos = Projectile.Center - Main.screenPosition;

            Color bloomA = Color.Lerp(new Color(0, 220, 255, 0), new Color(180, 80, 255, 0), progress) * alpha * 0.65f;
            float bloomScale = progress * 2.4f + 0.1f;
            Main.spriteBatch.Draw(bloom, pos, null, bloomA, 0f, bloom.Size() * 0.5f, bloomScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(bloom, pos, null, bloomA * 0.5f, 0f, bloom.Size() * 0.5f, bloomScale * 0.65f, SpriteEffects.None, 0f);

            Color twirlCol = new Color(0, 195, 255, 0) * alpha * 0.4f;
            Main.spriteBatch.Draw(twirl, pos, null, twirlCol, time * 10f, twirl.Size() * 0.5f, 0.6f * (1f - progress * 0.6f), SpriteEffects.None, 0f);

            Color starCol = new Color(140, 255, 255, 0) * alpha * 0.9f;
            float starScale = (1f - progress * 0.4f) * 0.75f;
            Main.spriteBatch.Draw(star, pos, null, starCol, time * 6f, star.Size() * 0.5f, starScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(star, pos, null, starCol * 0.55f, -time * 4f + MathHelper.PiOver4, star.Size() * 0.5f, starScale * 0.75f, SpriteEffects.None, 0f);

            return false;
        }
    }
}
