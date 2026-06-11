using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.F_PostLunar
{
    public class AuricEffect : LeonidMetalEffect
    {
        public override int EffectID => 31;

        protected override int EnergyVariant => 2;
        protected override float EnergySizeFactor => 1.12f;
        protected override int EnergyMoteCount => 4;
        protected override int EnergyDustInterval => 11;
        protected override float EnergyOpacity => 0.32f;

        public override void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
            meteor.DisableGravity();
            meteor.EnableSimpleHoming(0.045f, 980f);
            meteor.Projectile.ArmorPenetration += 36;
            meteor.SetState("auric_lance_timer", Main.rand.Next(16, 24));
        }

        public override void AI(LeonidCometSmall meteor, Player owner)
        {
            Projectile projectile = meteor.Projectile;
            float timer = meteor.GetState("auric_lance_timer") - 1f;
            if (timer <= 0f)
            {
                timer = meteor.FromStealthRain ? 22f : 34f;
                NPC target = meteor.FindClosestNPC(920f);
                if (target != null && Main.myPlayer == projectile.owner)
                    SpawnLance(projectile, target.Center, target.whoAmI, 0, 0.42f);
            }

            meteor.SetState("auric_lance_timer", timer);

            if (Main.rand.NextBool())
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric,
                    -projectile.velocity * Main.rand.NextFloat(0.02f, 0.07f),
                    100,
                    Main.rand.NextBool() ? new Color(255, 214, 82) : new Color(82, 226, 255),
                    Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override void ModifyHitNPC(LeonidCometSmall meteor, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ScalingArmorPenetration += 0.18f;
        }

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Dragonfire>(), 240);
            target.AddBuff(ModContent.BuffType<AuricRebuke>(), 120);

            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            int lanceCount = meteor.FromStealthRain ? 5 : 3;
            for (int i = 0; i < lanceCount; i++)
            {
                SpawnLance(meteor.Projectile, target.Center, target.whoAmI, i - lanceCount / 2, 1f);
            }
        }

        private static void SpawnLance(Projectile source, Vector2 targetCenter, int targetIndex, int offsetIndex, float damageFactor)
        {
            Vector2 spawnPosition = targetCenter + new Vector2(offsetIndex * 74f + Main.rand.NextFloat(-24f, 24f), -520f - Main.rand.NextFloat(0f, 120f));
            Vector2 velocity = (targetCenter - spawnPosition).SafeNormalize(Vector2.UnitY) * 24f;
            int lance = Projectile.NewProjectile(
                source.GetSource_FromThis(),
                spawnPosition,
                velocity,
                ModContent.ProjectileType<Auric_SkyLance>(),
                System.Math.Max(1, (int)(source.damage * damageFactor)),
                source.knockBack * 0.35f,
                source.owner,
                targetIndex,
                offsetIndex);

            if (lance >= 0 && lance < Main.maxProjectiles)
                Main.projectile[lance].DamageType = source.DamageType;
        }
    }
}
