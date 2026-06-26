using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // Stage 4+ only: spawned when the 4th bullet in a burst hits an enemy.
    // Ranged damage class, independent iframes, 2× bullet damage (set externally).
    // Adds heavy pollution stacks and screen shake.
    internal sealed class SeasSearingNukesplosion : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 50;

        public override void SetDefaults()
        {
            Projectile.width          = 100;
            Projectile.height         = 100;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.penetrate      = -1;
            Projectile.timeLeft       = Lifetime;
            Projectile.tileCollide    = false;
            Projectile.ignoreWater    = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 16;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation += 0.03f;

            float completion = 1f - Projectile.timeLeft / (float)Lifetime;
            float glow       = MathF.Sin(completion * MathHelper.Pi);
            Color primary    = Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.ToxicGreen, completion * 0.7f);
            Lighting.AddLight(Projectile.Center, primary.ToVector3() * glow * 2.2f);

            if (!Main.dedServ)
            {
                // Rising toxic spires
                int emitCount = (int)(glow * 6f + 2f);
                for (int i = 0; i < emitCount; i++)
                {
                    Vector2 offset   = Main.rand.NextVector2Circular(50f, 50f);
                    int     dustType = Main.rand.NextBool(3) ? DustID.Vortex : DustID.GemEmerald;
                    Vector2 vel      = new Vector2(
                        Main.rand.NextFloat(-0.8f, 0.8f),
                        -Main.rand.NextFloat(1.2f, 3.5f + glow * 4.5f));
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + offset, dustType, vel, 110,
                        Color.Lerp(primary, SeasSearingPalette.BiohazardLime, Main.rand.NextFloat()),
                        Main.rand.NextFloat(0.65f, 1.3f + glow * 0.45f));
                    d.noGravity = true;
                }

                // Expand ring at midlife
                if (Projectile.timeLeft == Lifetime / 2)
                {
                    SeasSearingVisualUtility.SpawnPressureRing(Projectile.Center, 5f, 50f, 28,
                        Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.BiohazardLime, 0.5f));
                    SeasSearingVisualUtility.ShakeAt(Projectile.Center, 5.5f, 1800f);
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.78f, Pitch = -0.05f, PitchVariance = 0.06f }, Projectile.Center);
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, 20, 18 * 60);
            target.AddBuff(BuffID.Venom, 480);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring   = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;

            float completion = 1f - Projectile.timeLeft / (float)Lifetime;
            float alpha      = MathF.Sin(completion * MathHelper.Pi);

            // Pulsing outer ring
            float ringScale  = MathHelper.Lerp(0.5f, 2.2f, completion);
            Color ringColor  = SeasSearingPalette.RadioactiveCyan * (alpha * 0.9f); ringColor.A = 0;

            // Dense inner bloom
            float innerScale = MathHelper.Lerp(0.4f, 1.1f, completion);
            Color inner1     = SeasSearingPalette.ToxicGreen   * alpha;        inner1.A = 0;
            Color inner2     = SeasSearingPalette.BiohazardLime * (alpha * 0.7f); inner2.A = 0;
            Color coreColor  = Color.White * (alpha * 0.5f * MathF.Max(1f - completion * 2f, 0f)); coreColor.A = 0;

            Vector2 pos = Projectile.Center - Main.screenPosition;

            for (int i = 0; i < 2; i++)
            {
                float offset = (MathHelper.TwoPi * i / 2f) + Main.GlobalTimeWrappedHourly * (1.4f - i * 0.4f);
                Main.EntitySpriteDraw(ring, pos, null, ringColor * (0.85f - i * 0.25f), offset, ring.Size() * 0.5f, ringScale + i * 0.2f, SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(bloom, pos, null, inner1,    0f, bloom.Size() * 0.5f, innerScale,       SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, pos, null, inner2,    0f, bloom.Size() * 0.5f, innerScale * 0.6f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, pos, null, coreColor, 0f, bloom.Size() * 0.5f, innerScale * 0.3f, SpriteEffects.None, 0);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 20, 4f, 50f, 1.2f);
        }
    }
}
