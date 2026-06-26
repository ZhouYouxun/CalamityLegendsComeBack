using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal sealed class SeasSearingFalloutRain : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "Terraria/Images/Projectile_618";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type]     = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width          = 12;
            Projectile.height         = 18;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.penetrate      = 2;
            Projectile.timeLeft       = 160;
            Projectile.tileCollide    = true;
            Projectile.ignoreWater    = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 14;
        }

        public override void AI()
        {
            Projectile.rotation   = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.08f, 18f);
            Lighting.AddLight(Projectile.Center, SeasSearingPalette.ToxicGreen.ToVector3() * 0.22f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center, Terraria.ID.DustID.GemEmerald,
                    -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    120, SeasSearingPalette.ToxicGreen, Main.rand.NextFloat(0.5f, 0.85f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, 5, 10 * 60, fromSpread: true);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 240);
        }

        public override void OnKill(int timeLeft) =>
            SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 8, 2.8f, 5f, 0.75f);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2   center = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bloom, center, null, (SeasSearingPalette.ToxicGreen with { A = 0 }) * 0.55f,
                Projectile.rotation, bloom.Size() * 0.5f, new Vector2(0.08f, 0.16f), SpriteEffects.None, 0);
            return true;
        }
    }
}
