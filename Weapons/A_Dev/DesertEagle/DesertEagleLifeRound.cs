using System;
using CalamityMod;
using CalamityMod.Projectiles.Healing;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle
{
    internal sealed class DesertEagleLifeRound : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/Ranged/ShockblastRound";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.aiStyle = ProjAIStyleID.Arrow;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.light = 0.5f;
            Projectile.extraUpdates = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            AIType = ProjectileID.Bullet;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            DesertEagleEffects.SpawnSilverImpact(Projectile.Center, oldVelocity.SafeNormalize(Vector2.UnitX), 1.15f, true);
            return true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }

        public override bool PreAI()
        {
            Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi) + MathHelper.ToRadians(90f) * Projectile.direction;

            Projectile.localAI[0] += 1f;
            DesertEagleEffects.SpawnBulletTrail(Projectile.Center, Projectile.velocity, 0.95f, false);

            if (Projectile.localAI[0] > 4f)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 dustSpeed = -Projectile.velocity * Main.rand.NextFloat(0.45f, 0.7f);
                    Dust dust = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.1f * i, DustID.SilverCoin, dustSpeed, 120, Color.Lerp(DesertEagleEffects.SilverMain, DesertEagleEffects.SilverAccent, Main.rand.NextFloat()), Main.rand.NextFloat(0.75f, 1.05f));
                    dust.noGravity = true;
                }
            }

            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Main.player[Projectile.owner].SpawnLifeStealProjectile(target, Projectile, ModContent.ProjectileType<TransfusionTrail>(), (int)Math.Round(hit.Damage * 0.08));
            DesertEagleEffects.SpawnSilverImpact(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), 1.25f, true);
        }
    }
}
