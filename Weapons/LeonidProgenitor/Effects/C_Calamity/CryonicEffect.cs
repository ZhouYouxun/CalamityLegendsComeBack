using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.Shared;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.C_Calamity
{
    public class CryonicEffect : LeonidMetalEffect
    {
        public override int EffectID => 20;

        protected override int EnergyVariant => 3;
        protected override float EnergySizeFactor => 0.94f;
        protected override int EnergyMoteCount => 3;
        protected override int EnergyDustInterval => 12;
        protected override float EnergySpinOffset => 0.45f;

        public override void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
            meteor.EnableSimpleHoming(0.035f, 740f);
            meteor.Projectile.velocity *= 0.94f;
            meteor.Projectile.penetrate = System.Math.Max(meteor.Projectile.penetrate, 2);
        }

        public override void AI(LeonidCometSmall meteor, Player owner)
        {
            Projectile projectile = meteor.Projectile;
            if (Main.rand.NextBool(2))
            {
                Dust frost = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.IceTorch,
                    -projectile.velocity * Main.rand.NextFloat(0.02f, 0.09f),
                    100,
                    new Color(150, 238, 255),
                    Main.rand.NextFloat(0.75f, 1.15f));
                frost.noGravity = true;
            }
        }

        public override void ModifyHitNPC(LeonidCometSmall meteor, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ScalingArmorPenetration += 0.12f;
        }

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn2, 240);

            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            int field = Projectile.NewProjectile(
                meteor.Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<Shared_LingeringField>(),
                System.Math.Max(1, meteor.Projectile.damage / 4),
                0f,
                meteor.Projectile.owner,
                1f);
            if (field >= 0 && field < Main.maxProjectiles)
                Main.projectile[field].DamageType = meteor.Projectile.DamageType;

            int shardCount = meteor.FromStealthRain ? 7 : 5;
            for (int i = 0; i < shardCount; i++)
            {
                float angle = MathHelper.TwoPi * i / shardCount + Main.rand.NextFloat(-0.12f, 0.12f);
                int shard = Projectile.NewProjectile(
                    meteor.Projectile.GetSource_FromThis(),
                    target.Center + angle.ToRotationVector2() * Main.rand.NextFloat(95f, 130f),
                    Vector2.Zero,
                    ModContent.ProjectileType<Cryonic_PrismShard>(),
                    System.Math.Max(1, meteor.Projectile.damage / 3),
                    meteor.Projectile.knockBack * 0.2f,
                    meteor.Projectile.owner,
                    target.whoAmI,
                    angle,
                    Main.rand.Next(12, 28));

                if (shard >= 0 && shard < Main.maxProjectiles)
                    Main.projectile[shard].DamageType = meteor.Projectile.DamageType;
            }
        }
    }
}
