using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.F_PostLunar
{
    public class ShadowspecEffect : LeonidMetalEffect
    {
        public override int EffectID => 32;

        public override void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
            meteor.DisableGravity();
            meteor.EnableSimpleHoming(0.07f, 1040f);
            meteor.Projectile.velocity *= 1.08f;
            meteor.Projectile.penetrate = System.Math.Max(meteor.Projectile.penetrate, 3);
            meteor.SetState("shadowspec_echo_timer", Main.rand.Next(10, 18));
        }

        public override void AI(LeonidCometSmall meteor, Player owner)
        {
            Projectile projectile = meteor.Projectile;
            projectile.velocity *= 1.002f;

            float timer = meteor.GetState("shadowspec_echo_timer") - 1f;
            if (timer <= 0f)
            {
                timer = meteor.FromStealthRain ? 9f : 15f;
                if (Main.myPlayer == projectile.owner)
                {
                    int echo = Projectile.NewProjectile(
                        projectile.GetSource_FromThis(),
                        projectile.Center - projectile.velocity.SafeNormalize(Vector2.UnitY) * 30f,
                        projectile.velocity.RotatedBy(MathHelper.Pi + Main.rand.NextFloat(-0.35f, 0.35f)) * 0.38f,
                        ModContent.ProjectileType<Shadowspec_Echo>(),
                        System.Math.Max(1, projectile.damage / 4),
                        projectile.knockBack * 0.1f,
                        projectile.owner,
                        -1f,
                        Main.rand.NextFloat(MathHelper.TwoPi),
                        0f);

                    if (echo >= 0 && echo < Main.maxProjectiles)
                        Main.projectile[echo].DamageType = projectile.DamageType;
                }
            }

            meteor.SetState("shadowspec_echo_timer", timer);

            if (Main.rand.NextBool())
            {
                Dust dust = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(10f, 10f), DustID.Shadowflame, -projectile.velocity * Main.rand.NextFloat(0.02f, 0.07f), 100, new Color(168, 90, 255), Main.rand.NextFloat(0.85f, 1.3f));
                dust.noGravity = true;
            }
        }

        public override void ModifyHitNPC(LeonidCometSmall meteor, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 1.1f;
        }

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.ShadowFlame, 300);
            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), 180);

            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            int rift = Projectile.NewProjectile(
                meteor.Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<Shadowspec_Rift>(),
                System.Math.Max(1, meteor.Projectile.damage / 2),
                0f,
                meteor.Projectile.owner,
                target.whoAmI);

            if (rift >= 0 && rift < Main.maxProjectiles)
                Main.projectile[rift].DamageType = meteor.Projectile.DamageType;

            int echoCount = meteor.FromStealthRain ? 6 : 4;
            for (int i = 0; i < echoCount; i++)
            {
                float angle = MathHelper.TwoPi * i / echoCount + Main.rand.NextFloat(-0.18f, 0.18f);
                int echo = Projectile.NewProjectile(
                    meteor.Projectile.GetSource_FromThis(),
                    target.Center + angle.ToRotationVector2() * Main.rand.NextFloat(44f, 78f),
                    angle.ToRotationVector2() * Main.rand.NextFloat(3.5f, 6.5f),
                    ModContent.ProjectileType<Shadowspec_Echo>(),
                    System.Math.Max(1, meteor.Projectile.damage / 3),
                    meteor.Projectile.knockBack * 0.2f,
                    meteor.Projectile.owner,
                    target.whoAmI,
                    angle,
                    1f);

                if (echo >= 0 && echo < Main.maxProjectiles)
                    Main.projectile[echo].DamageType = meteor.Projectile.DamageType;
            }
        }
    }
}
