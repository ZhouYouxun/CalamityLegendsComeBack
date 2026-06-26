using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // 200×200 explosion spawned by SeasSearingMissile. More dramatic than torpedo explosion.
    // Spawns pollution spikes, fallout cloud, and screen shake.
    internal sealed class SeasSearingMissileExplosion : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width          = 200;
            Projectile.height         = 200;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.penetrate      = -1;
            Projectile.timeLeft       = 30;
            Projectile.tileCollide    = false;
            Projectile.ignoreWater    = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = -1;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation += 0.07f;

            float lifeRatio = 1f - Projectile.timeLeft / 30f;
            float glow      = MathF.Sin(lifeRatio * MathHelper.Pi);
            Color primary   = Color.Lerp(SeasSearingPalette.WarningOrange, SeasSearingPalette.BiohazardLime, lifeRatio);
            Color secondary = Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.ToxicGreen, lifeRatio);
            Lighting.AddLight(Projectile.Center, (primary * glow).ToVector3() * 2.5f);

            if (!Main.dedServ)
            {
                int count = (int)(5 + glow * 12f);
                for (int i = 0; i < count; i++)
                {
                    float angle  = Main.rand.NextFloat(MathHelper.TwoPi);
                    float radius = 100f * lifeRatio * Main.rand.NextFloat(0.4f, 1.15f);
                    int   dType  = Main.rand.NextBool(3) ? DustID.Torch : DustID.GemEmerald;
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + angle.ToRotationVector2() * radius,
                        dType,
                        angle.ToRotationVector2() * Main.rand.NextFloat(2f, 6f + glow * 6f),
                        100,
                        Color.Lerp(primary, secondary, Main.rand.NextFloat()),
                        Main.rand.NextFloat(0.7f, 1.5f));
                    d.noGravity = true;
                }

                // Cardinal shockwave rays
                if (Projectile.timeLeft == 25)
                {
                    for (int dir = 0; dir < 8; dir++)
                    {
                        float   angle  = MathHelper.TwoPi * dir / 8f;
                        Vector2 vel    = angle.ToRotationVector2() * 5.5f;
                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            Projectile.Center, vel,
                            ModContent.ProjectileType<SSPollutionSpike>(),
                            Math.Max(1, Projectile.damage / 3), 1f, Projectile.owner, 10, 2);
                    }

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center, Vector2.Zero,
                        ModContent.ProjectileType<SeasSearingFalloutCloud>(),
                        Math.Max(1, Projectile.damage / 2), 0f, Projectile.owner, 15f);

                    SeasSearingVisualUtility.ShakeAt(Projectile.Center, 9f, 2200f);
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.9f, Pitch = -0.3f }, Projectile.Center);
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, 15, 16 * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring   = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;

            float lifeRatio  = 1f - Projectile.timeLeft / 30f;
            float alpha      = MathF.Sin(lifeRatio * MathHelper.Pi);
            float scale      = Projectile.width / 100f;

            // Outer ring expanding
            float ringScale  = MathHelper.Lerp(0.3f, 3.5f, lifeRatio) * scale;
            Color outerRing  = SeasSearingPalette.WarningOrange * (alpha * 0.85f); outerRing.A = 0;
            // Inner bloom
            float innerScale = MathHelper.Lerp(0.5f, 1.8f, lifeRatio) * scale;
            Color inner1     = SeasSearingPalette.BiohazardLime  * alpha;           inner1.A = 0;
            Color inner2     = SeasSearingPalette.RadioactiveCyan * (alpha * 0.7f); inner2.A = 0;

            Vector2 pos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(ring,  pos, null, outerRing, 0f, ring.Size()  * 0.5f, ringScale,  SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, pos, null, inner1,    0f, bloom.Size() * 0.5f, innerScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, pos, null, inner2,    0f, bloom.Size() * 0.5f, innerScale * 0.6f, SpriteEffects.None, 0);
            return false;
        }
    }
}
