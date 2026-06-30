using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace.Passive
{
    public class FrostSealAfterglow : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Duration = 120;
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
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 9999; // one hit per enemy per projectile lifetime
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            drawTimer++;
            if (Main.dedServ) return;
            if (Main.rand.NextBool(3))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(10f, 175f);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * dist;
                Dust d = Dust.NewDustDirect(pos, 0, 0, DustID.Frost);
                d.velocity = angle.ToRotationVector2() * Main.rand.NextFloat(0.4f, 1.2f);
                d.scale = Main.rand.NextFloat(0.7f, 1.3f);
                d.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Vector2.Distance(Projectile.Center, targetHitbox.Center.ToVector2())
                   <= 200f + targetHitbox.Width * 0.35f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn2, 360);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_03").Value;
            float time = Main.GlobalTimeWrappedHourly;
            float progress = (float)drawTimer / Duration;
            float alpha = (1f - progress) * (1f - progress);
            Vector2 pos = Projectile.Center - Main.screenPosition;

            Color innerCol = new Color(0, 195, 255, 0) * alpha * 0.5f;
            Color outerCol = new Color(50, 130, 255, 0) * alpha * 0.3f;

            Main.spriteBatch.Draw(bloom, pos, null, innerCol, 0f, bloom.Size() * 0.5f, 0.9f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ring, pos, null, outerCol, time * 1.3f, ring.Size() * 0.5f, 0.72f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ring, pos, null, outerCol * 0.55f, -time * 0.75f, ring.Size() * 0.5f, 0.55f, SpriteEffects.None, 0f);

            return false;
        }
    }
}
