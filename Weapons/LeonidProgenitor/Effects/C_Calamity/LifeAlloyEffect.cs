using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.C_Calamity
{
    public class LifeAlloyEffect : LeonidMetalEffect
    {
        public override int EffectID => 33;

        public override void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
            meteor.EnableSimpleHoming(0.04f, 820f);
            meteor.Projectile.penetrate = System.Math.Max(meteor.Projectile.penetrate, 2);
            meteor.SetState("life_alloy_pulse_timer", 20f);
        }

        public override void AI(LeonidCometSmall meteor, Player owner)
        {
            Projectile projectile = meteor.Projectile;
            projectile.scale = MathHelper.Lerp(projectile.scale, 1.12f, 0.035f);

            float timer = meteor.GetState("life_alloy_pulse_timer") - 1f;
            if (timer <= 0f)
            {
                timer = meteor.FromStealthRain ? 18f : 28f;
                if (Main.myPlayer == projectile.owner)
                {
                    int pulse = Projectile.NewProjectile(
                        projectile.GetSource_FromThis(),
                        projectile.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<LifeAlloy_ReconstructionPulse>(),
                        System.Math.Max(1, projectile.damage / 5),
                        0f,
                        projectile.owner,
                        0f,
                        Main.rand.Next(3));

                    if (pulse >= 0 && pulse < Main.maxProjectiles)
                        Main.projectile[pulse].DamageType = projectile.DamageType;
                }
            }

            meteor.SetState("life_alloy_pulse_timer", timer);

            if (Main.rand.NextBool(2))
            {
                Color color = Utils.SelectRandom(Main.rand, new Color(90, 245, 255), new Color(255, 92, 215), new Color(126, 255, 118));
                Dust dust = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(9f, 9f), DustID.RainbowTorch, -projectile.velocity * Main.rand.NextFloat(0.01f, 0.05f), 100, color, Main.rand.NextFloat(0.75f, 1.1f));
                dust.noGravity = true;
            }
        }

        public override void ModifyHitNPC(LeonidCometSmall meteor, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 1.08f;
        }

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            int gleamCount = meteor.FromStealthRain ? 4 : 3;
            for (int i = 0; i < gleamCount; i++)
            {
                Vector2 spawnPosition = target.Center + Main.rand.NextVector2CircularEdge(76f, 76f);
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 3.6f);
                int gleam = Projectile.NewProjectile(
                    meteor.Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<LifeAlloy_Gleam>(),
                    meteor.Projectile.damage / 2,
                    0f,
                    meteor.Projectile.owner,
                    target.whoAmI,
                    i % 3);

                if (gleam >= 0 && gleam < Main.maxProjectiles)
                    Main.projectile[gleam].DamageType = meteor.Projectile.DamageType;
            }

            int burst = Projectile.NewProjectile(
                meteor.Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<LifeAlloy_ReconstructionPulse>(),
                System.Math.Max(1, meteor.Projectile.damage / 2),
                0f,
                meteor.Projectile.owner,
                1f,
                Main.rand.Next(3));

            if (burst >= 0 && burst < Main.maxProjectiles)
                Main.projectile[burst].DamageType = meteor.Projectile.DamageType;
        }
    }
}
