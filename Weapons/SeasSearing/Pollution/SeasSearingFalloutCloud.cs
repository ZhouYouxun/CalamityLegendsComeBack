using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal sealed class SeasSearingFalloutCloud : ModProjectile, ILocalizedModType
    {
        private float Radius => MathHelper.Clamp(Projectile.ai[0] * 5f + 120f, 140f, 520f);

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width          = 220;
            Projectile.height         = 220;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.penetrate      = -1;
            Projectile.timeLeft       = 150;
            Projectile.tileCollide    = false;
            Projectile.ignoreWater    = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 24;
        }

        public override void AI()
        {
            Vector2 center = Projectile.Center;
            int size = (int)(Radius * 2f);
            Projectile.width  = size;
            Projectile.height = size;
            Projectile.Center = center;
            Projectile.damage = System.Math.Max(1, (int)(Projectile.damage * 0.998f));

            if (Main.rand.NextBool(3))
            {
                Vector2 offset = Main.rand.NextVector2Circular(Radius * 0.85f, Radius * 0.55f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + offset,
                    Main.rand.NextBool(2) ? DustID.Smoke : DustID.GemEmerald,
                    Main.rand.NextVector2Circular(0.8f, 0.8f) - Vector2.UnitY * Main.rand.NextFloat(0.15f, 0.65f),
                    150,
                    Main.rand.NextBool(3) ? SeasSearingPalette.FalloutAsh : SeasSearingPalette.ToxicGreen,
                    Main.rand.NextFloat(0.55f, 1.05f));
                dust.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, SeasSearingPalette.DeepBlue.ToVector3() * 0.12f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, 2, 8 * 60, fromSpread: true);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float opacity    = MathHelper.Clamp(Projectile.timeLeft / 70f, 0f, 1f) * 0.55f;
            Vector2 center   = Projectile.Center - Main.screenPosition;
            Color deep  = (SeasSearingPalette.AbyssBlack with { A = 0 }) * opacity;
            Color toxic = (SeasSearingPalette.ToxicGreen  with { A = 0 }) * opacity;

            Main.EntitySpriteDraw(bloom, center, null, deep  * 0.8f,  0f, bloom.Size() * 0.5f, new Vector2(Radius / bloom.Width * 2.2f, Radius / bloom.Height * 1.35f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, center, null, toxic * 0.18f, Main.GlobalTimeWrappedHourly, bloom.Size() * 0.5f, new Vector2(Radius / bloom.Width * 1.4f, Radius / bloom.Height * 0.9f), SpriteEffects.None, 0);
            return false;
        }
    }
}
