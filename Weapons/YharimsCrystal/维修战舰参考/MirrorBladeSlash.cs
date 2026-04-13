using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Melee
{
    public class MirrorBladeSlash : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";

        public override string Texture => "CalamityMod/Projectiles/Melee/ExobeamSlash";

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 512;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 2;
            Projectile.Opacity = 1f;
            Projectile.timeLeft = 35;
            Projectile.MaxUpdates = 2;
            Projectile.scale = 0.75f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.noEnchantmentVisuals = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = Projectile.timeLeft / 35f;
            if (Projectile.timeLeft == 34)
            {
                Vector2 vel = new Vector2(0.1f, 0.1f).RotatedByRandom(100);
                VoidSparkParticle spark2 = new VoidSparkParticle(Projectile.Center, vel, false, 9, Main.rand.NextFloat(0.25f, 0.35f), Main.rand.NextBool() ? Color.Silver : Color.BlueViolet);
                GeneralParticleHandler.SpawnParticle(spark2);

                for (int j = -1; j <= 1; j += 2)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), vel.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.1f) * Main.rand.NextFloat(2f, 12.5f) * j);
                        dust.noGravity = true;
                        dust.scale = Main.rand.NextFloat(1.2f, 1.7f);
                        dust.color = Main.rand.NextBool() ? Color.SteelBlue : Color.BlueViolet;
                        dust.noLightEmittence = true;
                        dust.fadeIn = 1;
                    }
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                Projectile.damage = (int)(Projectile.damage * 0.67f);
            if (Projectile.damage < 1)
                Projectile.damage = 1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Nightwither>(), 120);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Nightwither>(), 120);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Projectile.RotatingHitboxCollision(targetHitbox);

        public override bool ShouldUpdatePosition() => true;

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(Projectile.ai[2] == 0f ? Color.Blue : Color.White, Projectile.ai[2] == 0f ? Color.DarkBlue : Color.White, Projectile.identity / 7f % 1f) * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
