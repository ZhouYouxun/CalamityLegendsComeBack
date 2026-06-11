using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.C_Calamity
{
    public class ScoriaEffect : LeonidMetalEffect
    {
        public override int EffectID => 26;

        protected override int EnergyVariant => 2;
        protected override float EnergySizeFactor => 1.06f;
        protected override int EnergyMoteCount => 3;
        protected override int EnergyDustInterval => 9;
        protected override float EnergyOpacity => 0.26f;

        public override void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
            meteor.Projectile.velocity *= 1.08f;
            meteor.Projectile.penetrate = System.Math.Max(meteor.Projectile.penetrate, 2);
            meteor.SetState("scoria_glob_timer", Main.rand.Next(8, 16));
        }

        public override void AI(LeonidCometSmall meteor, Player owner)
        {
            Projectile projectile = meteor.Projectile;
            projectile.velocity.Y += 0.035f;

            if (Main.rand.NextBool())
            {
                Dust slag = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextBool(3) ? DustID.LavaMoss : DustID.Torch,
                    -projectile.velocity * Main.rand.NextFloat(0.02f, 0.06f) + new Vector2(Main.rand.NextFloat(-0.7f, 0.7f), Main.rand.NextFloat(0.4f, 1.4f)),
                    90,
                    Color.Lerp(new Color(255, 80, 36), new Color(255, 218, 92), Main.rand.NextFloat(0.35f)),
                    Main.rand.NextFloat(0.85f, 1.35f));
                slag.noGravity = false;
            }

            float timer = meteor.GetState("scoria_glob_timer") - 1f;
            if (timer <= 0f)
            {
                timer = meteor.FromStealthRain ? 12f : 20f;
                if (Main.myPlayer == projectile.owner)
                {
                    Vector2 velocity = new(Main.rand.NextFloat(-3.4f, 3.4f), Main.rand.NextFloat(-7.8f, -4.6f));
                    int glob = Projectile.NewProjectile(
                        projectile.GetSource_FromThis(),
                        projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                        velocity,
                        ModContent.ProjectileType<Scoria_LavaGlob>(),
                        System.Math.Max(1, projectile.damage / 4),
                        projectile.knockBack * 0.15f,
                        projectile.owner);

                    if (glob >= 0 && glob < Main.maxProjectiles)
                        Main.projectile[glob].DamageType = projectile.DamageType;
                }
            }

            meteor.SetState("scoria_glob_timer", timer);
        }

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 240);

            if (Main.myPlayer == meteor.Projectile.owner)
            {
                int geyser = Projectile.NewProjectile(
                    meteor.Projectile.GetSource_FromThis(),
                    target.Center + new Vector2(Main.rand.NextFloat(-36f, 36f), 48f),
                    Vector2.Zero,
                    ModContent.ProjectileType<Scoria_Geyser>(),
                    System.Math.Max(1, meteor.Projectile.damage / 2),
                    meteor.Projectile.knockBack * 0.35f,
                    meteor.Projectile.owner,
                    target.whoAmI);

                if (geyser >= 0 && geyser < Main.maxProjectiles)
                    Main.projectile[geyser].DamageType = meteor.Projectile.DamageType;
            }

            for (int i = 0; i < 32; i++)
            {
                Vector2 velocity = new(Main.rand.NextFloat(-2.2f, 2.2f), Main.rand.NextFloat(-10.5f, -4.8f));
                Dust dust = Dust.NewDustPerfect(
                    target.Center + Main.rand.NextVector2Circular(18f, 10f),
                    Main.rand.NextBool(3) ? DustID.LavaMoss : DustID.Torch,
                    velocity,
                    100,
                    Color.Lerp(new Color(255, 80, 36), new Color(255, 205, 88), Main.rand.NextFloat(0.45f)),
                    Main.rand.NextFloat(1.1f, 1.8f));

                dust.noGravity = false;
                dust.fadeIn = Main.rand.NextFloat(0.4f, 0.9f);
            }
        }
    }
}
