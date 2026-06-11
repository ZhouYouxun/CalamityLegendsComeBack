using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClick
{
    public class SHPCRight_DeBuff : ModBuff
    {
        public static int FireMode = 1;

        private static readonly Color TechBlue = new(54, 190, 255);
        private static readonly Color TechWhite = new(235, 250, 255);
        private static readonly Color DeepIonBlue = new(16, 82, 210);

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            int stage = player.GetModPlayer<SHPCRight_Player>().HeatStage;
            if (stage <= 0)
                return;

            if (player.statLife <= 0)
            {
                KillFromOverheat(player, 1);
                return;
            }

            if (stage < 4)
                return;

            player.lifeRegenTime = 0;
            if (player.lifeRegen > 0)
                player.lifeRegen = 0;

            if (stage == 4)
            {
                if (player.statLife > 100 && Main.GameUpdateCount % 30 == 0)
                    ApplyOverheatDamage(player, 1);
            }
            else if (stage >= 5 && Main.GameUpdateCount % 5 == 0)
                ApplyOverheatDamage(player, Main.rand.Next(1, 3));

            ApplyIonizedVisual(player, stage);
        }

        private static void ApplyOverheatDamage(Player player, int damage)
        {
            if (damage <= 0 || player.dead)
                return;

            if (player.statLife > damage)
            {
                player.statLife -= damage;
                return;
            }

            KillFromOverheat(player, damage);
        }

        private static void KillFromOverheat(Player player, int damage)
        {
            if (player.dead)
                return;

            player.statLife = 0;
            player.KillMe(PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral($"{player.name} was burned out by SHPC overload.")), System.Math.Max(1, damage), 0);
        }

        private void ApplyIonizedVisual(Player player, int stage)
        {
            if (Main.dedServ || stage < 4)
                return;

            float spawnChance = stage >= 5 ? 0.42f : 0.24f;
            if (Main.rand.NextFloat() > spawnChance)
                return;

            ApplyIonLeak(player);

            if (stage >= 5)
            {
                ApplyPulseHalo(player);
                ApplyHighHeatIonArcs(player);
            }
        }

        private static Vector2 RandomBodyPoint(Player player, float widthScale = 0.5f, float heightScale = 0.58f)
        {
            return player.Center + new Vector2(
                Main.rand.NextFloat(-player.width * widthScale, player.width * widthScale),
                Main.rand.NextFloat(-player.height * heightScale, player.height * heightScale));
        }

        private void ApplyIonLeak(Player player)
        {
            Vector2 position = RandomBodyPoint(player, 0.42f, 0.48f);
            Vector2 drift = new(Main.rand.NextFloat(-0.35f, 0.35f), Main.rand.NextFloat(-1.6f, -0.35f));
            Color color = Color.Lerp(TechBlue, TechWhite, Main.rand.NextFloat(0.18f, 0.72f));

            if (Main.rand.NextBool(2))
            {
                SquishyLightParticle mote = new(
                    position,
                    drift.RotatedByRandom(0.25f),
                    Main.rand.NextFloat(0.16f, 0.28f),
                    color * 0.62f,
                    Main.rand.Next(9, 15),
                    1f,
                    Main.rand.NextFloat(0.36f, 0.58f));
                GeneralParticleHandler.SpawnParticle(mote);
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    position,
                    Main.rand.NextBool(3) ? DustID.IceTorch : DustID.Electric,
                    drift.RotatedByRandom(0.55f) * Main.rand.NextFloat(0.55f, 1.35f),
                    0,
                    color,
                    Main.rand.NextFloat(0.55f, 0.92f));
                dust.noGravity = true;
            }

            if (Main.rand.NextBool(5))
            {
                PointParticle spark = new(
                    position,
                    drift * 0.45f,
                    false,
                    Main.rand.Next(7, 11),
                    Main.rand.NextFloat(0.55f, 0.82f),
                    TechWhite * 0.7f);
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        private void ApplyPulseHalo(Player player)
        {
            if (Main.GameUpdateCount % 10 != 0 || !Main.rand.NextBool(2))
                return;

            Vector2 center = player.Center + Main.rand.NextVector2Circular(player.width * 0.14f, player.height * 0.18f);
            Color ringColor = Color.Lerp(TechBlue, TechWhite, Main.rand.NextFloat(0.25f, 0.58f)) * 0.36f;
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Vector2.Zero,
                ringColor,
                new Vector2(0.5f, 1.65f),
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.04f,
                0.18f,
                13));
        }

        private void ApplyHighHeatIonArcs(Player player)
        {
            Vector2 center = player.Center;
            Vector2 source = RandomBodyPoint(player, 0.58f, 0.62f);
            Color arcColor = Main.rand.NextBool(4) ? TechWhite : Color.Lerp(DeepIonBlue, TechBlue, Main.rand.NextFloat(0.35f, 0.9f));

            if (Main.rand.NextBool(2))
            {
                Vector2 tangent = Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.8f, 2.2f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    source,
                    tangent + player.velocity * 0.12f,
                    false,
                    Main.rand.Next(6, 10),
                    Main.rand.NextFloat(0.12f, 0.24f),
                    arcColor));
            }

            if (Main.rand.NextBool(3))
            {
                GlowOrbParticle core = new(
                    center + Main.rand.NextVector2Circular(player.width * 0.18f, player.height * 0.22f),
                    Main.rand.NextVector2Circular(0.18f, 0.18f) + new Vector2(0f, Main.rand.NextFloat(-0.45f, -0.05f)),
                    false,
                    Main.rand.Next(7, 11),
                    Main.rand.NextFloat(0.2f, 0.34f),
                    Color.Lerp(TechBlue, TechWhite, Main.rand.NextFloat(0.25f, 0.72f)) * 0.55f,
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(core);
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    source,
                    Main.rand.NextBool() ? DustID.Electric : DustID.BlueTorch,
                    (source - center).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.7f) * Main.rand.NextFloat(0.8f, 1.9f),
                    0,
                    arcColor,
                    Main.rand.NextFloat(0.78f, 1.15f));
                dust.noGravity = true;
            }
        }
    }
}
