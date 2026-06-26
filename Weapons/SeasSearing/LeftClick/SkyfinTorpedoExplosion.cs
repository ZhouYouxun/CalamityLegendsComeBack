using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // 100×100 explosion spawned when a SkyfinTorpedo hits.
    internal sealed class SkyfinTorpedoExplosion : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width          = 100;
            Projectile.height         = 100;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.penetrate      = -1;
            Projectile.timeLeft       = 20;
            Projectile.tileCollide    = false;
            Projectile.ignoreWater    = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = -1;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation += 0.04f;

            float lifeRatio = 1f - Projectile.timeLeft / 20f;
            float glow      = MathF.Sin(lifeRatio * MathHelper.Pi);
            Color color     = Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.ToxicGreen, lifeRatio);
            Lighting.AddLight(Projectile.Center, color.ToVector3() * glow * 1.5f);

            if (!Main.dedServ)
            {
                int dustCount = (int)(3 + glow * 6);
                for (int i = 0; i < dustCount; i++)
                {
                    float angle    = Main.rand.NextFloat(MathHelper.TwoPi);
                    float radius   = 50f * lifeRatio;
                    Vector2 vel    = angle.ToRotationVector2() * Main.rand.NextFloat(1.5f, 3.5f + glow * 3f);
                    Terraria.Dust dust = Terraria.Dust.NewDustPerfect(
                        Projectile.Center + angle.ToRotationVector2() * radius * Main.rand.NextFloat(0.5f, 1.1f),
                        Terraria.ID.DustID.GemEmerald, vel, 100,
                        Color.Lerp(SeasSearingPalette.BiohazardLime, SeasSearingPalette.ToxicGreen, Main.rand.NextFloat()),
                        Main.rand.NextFloat(0.65f, 1.25f));
                    dust.noGravity = true;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, 6, 14 * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float lifeRatio  = 1f - Projectile.timeLeft / 20f;
            float alpha      = MathF.Sin(lifeRatio * MathHelper.Pi);
            float outerScale = MathHelper.Lerp(0.4f, 1.8f, lifeRatio) * (Projectile.width / 100f);
            float innerScale = outerScale * 0.45f;

            Color outer = SeasSearingPalette.ToxicGreen   * alpha;  outer.A = 0;
            Color inner = SeasSearingPalette.RadioactiveCyan * (alpha * 0.9f); inner.A = 0;

            Vector2 pos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bloom, pos, null, outer, 0f, bloom.Size() * 0.5f, outerScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, pos, null, inner, 0f, bloom.Size() * 0.5f, innerScale, SpriteEffects.None, 0);
            return false;
        }
    }
}
