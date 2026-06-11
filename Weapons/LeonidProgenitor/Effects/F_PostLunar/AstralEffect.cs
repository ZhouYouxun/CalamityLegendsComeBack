using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.F_PostLunar
{
    public class AstralEffect : LeonidMetalEffect
    {
        public override int EffectID => 28;

        protected override int EnergyVariant => 6;
        protected override float EnergySizeFactor => 1f;
        protected override int EnergyMoteCount => 4;
        protected override int EnergyDustInterval => 9;
        protected override float EnergyOpacity => 0.3f;

        public override void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
            meteor.DisableGravity();
            meteor.EnableSimpleHoming(0.052f, 920f);
            meteor.Projectile.velocity *= 1.12f;
            meteor.SetState("astral_node_timer", Main.rand.Next(8, 14));
        }

        public override void AI(LeonidCometSmall meteor, Player owner)
        {
            Projectile projectile = meteor.Projectile;
            float timer = meteor.GetState("astral_node_timer") - 1f;
            if (timer <= 0f)
            {
                timer = meteor.FromStealthRain ? 11f : 18f;
                if (Main.myPlayer == projectile.owner)
                {
                    int node = Projectile.NewProjectile(
                        projectile.GetSource_FromThis(),
                        projectile.Center - projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(22f, 46f),
                        Main.rand.NextVector2Circular(1f, 1f),
                        ModContent.ProjectileType<Astral_ConstellationNode>(),
                        System.Math.Max(1, projectile.damage / 4),
                        projectile.knockBack * 0.1f,
                        projectile.owner,
                        -1f,
                        Main.rand.NextFloat(MathHelper.TwoPi));

                    if (node >= 0 && node < Main.maxProjectiles)
                        Main.projectile[node].DamageType = projectile.DamageType;
                }
            }

            meteor.SetState("astral_node_timer", timer);

            if (Main.rand.NextBool(2))
            {
                Dust astral = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextBool() ? DustID.PinkTorch : DustID.BlueTorch,
                    -projectile.velocity * Main.rand.NextFloat(0.015f, 0.06f),
                    90,
                    Main.rand.NextBool() ? new Color(255, 150, 78) : new Color(100, 210, 255),
                    Main.rand.NextFloat(0.75f, 1.2f));
                astral.noGravity = true;
            }
        }

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 300);

            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            int blastCount = meteor.FromStealthRain ? 5 : 3;
            for (int i = 0; i < blastCount; i++)
            {
                Vector2 position = target.Center + Main.rand.NextVector2Circular(170f, 120f);
                int blast = Projectile.NewProjectile(
                    meteor.Projectile.GetSource_FromThis(),
                    position,
                    Vector2.Zero,
                    ModContent.ProjectileType<Astral_Blast>(),
                    System.Math.Max(1, meteor.Projectile.damage / 2),
                    0f,
                    meteor.Projectile.owner,
                    Main.rand.NextFloat(MathHelper.TwoPi),
                    i % 2);
                if (blast >= 0 && blast < Main.maxProjectiles)
                    Main.projectile[blast].DamageType = meteor.Projectile.DamageType;
            }

            int nodeCount = meteor.FromStealthRain ? 7 : 5;
            for (int i = 0; i < nodeCount; i++)
            {
                float angle = MathHelper.TwoPi * i / nodeCount;
                int node = Projectile.NewProjectile(
                    meteor.Projectile.GetSource_FromThis(),
                    target.Center + angle.ToRotationVector2() * 118f,
                    angle.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 3.2f,
                    ModContent.ProjectileType<Astral_ConstellationNode>(),
                    System.Math.Max(1, meteor.Projectile.damage / 3),
                    meteor.Projectile.knockBack * 0.2f,
                    meteor.Projectile.owner,
                    target.whoAmI,
                    angle);

                if (node >= 0 && node < Main.maxProjectiles)
                    Main.projectile[node].DamageType = meteor.Projectile.DamageType;
            }
        }
    }
}
