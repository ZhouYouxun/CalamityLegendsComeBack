using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    public class BloodfireArrowProj : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";

        //Used by Arterial Assault
        public bool DisableEffects = false;

        public override string Texture => "CalamityMod/Items/Ammo/BloodfireArrow";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 9;
            Projectile.timeLeft = 1200;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Projectile.spriteDirection = Projectile.direction;

            if (Projectile.localAI[0] == 0)
            {
                if (DisableEffects)
                {

                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 9;
                } 
                else
                {
                    Projectile.damage = (int)(Projectile.damage * 1.3f); // damage boost
                    player.statLife -= 1;
                    if (player.statLife <= 0)
                    {
                        PlayerDeathReason pdr = PlayerDeathReason.ByCustomReason(CalamityUtils.GetText("Status.Death.BloodFireArrow" + Main.rand.Next(1, 2 + 1)).ToNetworkText(player.name));
                        player.KillMe(pdr, 1000.0, 0, false);
                    }
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 9;
                }

            }

            Player Owner = Main.player[Projectile.owner];
            float targetDist = Vector2.Distance(Owner.Center, Projectile.Center);

            // Lighting
            Lighting.AddLight(Projectile.Center, (!ChildSafety.Disabled ? Color.CornflowerBlue : Color.Red).ToVector3() * 0.7f);

            // Dust
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > 6f && targetDist < 1400f)
            {
                if (Main.rand.NextBool())
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? 130 : 60, -Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.2f, 0.6f));
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.3f, 0.7f);
                    if (dust.type == 130)
                        dust.scale = Main.rand.NextFloat(0.25f, 0.45f);
                }
                if (Projectile.localAI[0] % 2 == 0)
                {
                    Particle spark = new CustomSpark(Projectile.Center - Projectile.velocity * 2, -Projectile.velocity * 0.01f, "CalamityMod/Particles/BloomLineFade", false, 6, 0.025f, (!ChildSafety.Disabled ? Color.CornflowerBlue : Color.Firebrick), new Vector2(1, 1), true, true, shrinkSpeed: 1.4f, glowOpacity: 0.4f);
                    GeneralParticleHandler.SpawnParticle(spark);
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int b = 0; b < 6; b++)
            {
                int dustType = ModContent.DustType<DiamondDust>();
                float velMulti = Main.rand.NextFloat(0.1f, 0.75f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, dustType, (Projectile.velocity * 2).RotatedByRandom(0.3) * velMulti);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.75f, 0.95f);
                dust.color = (!ChildSafety.Disabled ? Color.CornflowerBlue : Color.Firebrick);
                dust.noLightEmittence = true;
                dust.noLight = true;
                dust.fadeIn = 15;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (DisableEffects)
                return;
            Player player = Main.player[Projectile.owner];
            player.lifeRegenTime += 2;

            float lifeRatio = (float)player.statLife / player.statLifeMax2;
            float averageHealAmount = MathHelper.Lerp(4.0f, 0.5f, lifeRatio); // Average heal increases from 1/2 to 4 HP based on missing health
            int guaranteedHeal = (int)averageHealAmount;

            float chanceOfOneMoreHP = averageHealAmount - guaranteedHeal;
            bool bonusHeal = Main.rand.NextFloat() < chanceOfOneMoreHP;
            int finalHeal = guaranteedHeal + (bonusHeal ? 1 : 0);
            player.SpawnLifeStealProjectile(target, Projectile, ProjectileID.VampireHeal, finalHeal, 0.5f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.White, 2);
            return false;
        }
    }
}
