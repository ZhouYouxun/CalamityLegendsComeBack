using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // Spawned 2-4 per bullet hit at stage 2+. Floats outward and melts on contact.
    internal sealed class SeasSearingPollutionBubble : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Melee/SulphurousGrabberBubble2";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width          = 18;
            Projectile.height         = 18;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.penetrate      = 1;
            Projectile.timeLeft       = 80;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 20;
            Projectile.tileCollide    = false;
            Projectile.ignoreWater    = true;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.92f;
            Projectile.rotation  = Projectile.velocity.ToRotation();

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame        = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            float lifeRatio = 1f - Projectile.timeLeft / 80f;
            Color bubbleColor = Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.ToxicGreen, lifeRatio);
            Lighting.AddLight(Projectile.Center, bubbleColor.ToVector3() * 0.28f);

            if (!Main.dedServ && Main.rand.NextFloat() < 0.18f)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    Terraria.ID.DustID.Water,
                    Main.rand.NextVector2Circular(1f, 1f), 120, bubbleColor,
                    Main.rand.NextFloat(0.4f, 0.75f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SeasSearingPollutionNPC pollution = target.GetGlobalNPC<SeasSearingPollutionNPC>();
            pollution.ApplyPollution(target, Projectile.owner, 3, 10 * 60);
            target.AddBuff(Terraria.ID.BuffID.Venom, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex      = ModContent.Request<Texture2D>(Texture).Value;
            int       frames   = Main.projFrames[Type];
            int       frameH   = tex.Height / frames;
            Rectangle srcRect  = new(0, Projectile.frame * frameH, tex.Width, frameH);

            float lifeRatio = 1f - Projectile.timeLeft / 80f;
            float alpha     = MathHelper.SmoothStep(0f, 1f, Math.Min(Projectile.timeLeft / 12f, 1f))
                            * MathHelper.SmoothStep(1f, 0f, Math.Max((lifeRatio - 0.7f) / 0.3f, 0f));
            float scale     = Projectile.scale * MathHelper.Lerp(0.65f, 1.0f, alpha);

            Color drawColor = Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.ToxicGreen, lifeRatio)
                            * alpha;
            drawColor.A     = 0;

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition,
                srcRect, drawColor, Projectile.rotation, srcRect.Size() * 0.5f, scale, SpriteEffects.None, 0);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ) return;
            SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 5, 1.4f, 8f, 0.75f);
        }
    }
}
