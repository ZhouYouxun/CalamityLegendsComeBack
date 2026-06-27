using CalamityMod.Dusts;
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

        private static readonly Color BrimstoneRed = new(135, 20, 36);
        private static readonly Color BrimstoneDark = new(72, 12, 22);
        private static readonly Color DemonicViolet = new(128, 55, 175);
        private static readonly Color DragonEmber = new(210, 64, 26);
        private static readonly Color SmokeGray = new(58, 50, 48);

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

            ApplyOverheatVisual(player, stage);
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

        private void ApplyOverheatVisual(Player player, int stage)
        {
            if (Main.dedServ || stage < 4)
                return;

            // Adapted from Calamity's Brimstone Flames (main dust + spread dusts) and
            // Demonic Flames (VelChangingSpark) and Dragonfire (SparkParticle + SmallSmokeParticle).
            // Reference baseline: Brimstone Flames at ~1.33 particles/frame.
            // Stage IV target: 1.5× reference = ~2.0/frame.
            // Stage V  target: 2.0× reference = ~2.67/frame.

            // Core burst — 1/2 chance per frame: 1 upward dust + 2 spread dusts + 1 VelChangingSpark = 4 items.
            // 0.5 × 4 = 2.0/frame — meets the Stage IV cap and forms the base for Stage V.
            if (Main.rand.NextBool(2))
            {
                // Main upward dust (Brimstone Flames primary, upward drift)
                Dust mainDust = Dust.NewDustPerfect(
                    RandomBodyPoint(player, 0.30f, 0.40f),
                    Main.rand.NextBool(3) ? DustID.RuneWizard : (int)CalamityDusts.Brimstone,
                    player.velocity + new Vector2(0f, Main.rand.NextFloat(-5f, -3f)).RotatedByRandom(0.18f),
                    90,
                    Color.Lerp(BrimstoneDark, BrimstoneRed, Main.rand.NextFloat(0.4f, 0.9f)),
                    1.6f);
                mainDust.noGravity = true;

                // Two spread dusts (Brimstone Flames secondary, horizontal scatter)
                for (int i = 0; i < 2; i++)
                {
                    Dust secDust = Dust.NewDustPerfect(
                        player.Center + new Vector2(Main.rand.NextFloat(-10f, 10f), player.height * 0.1f),
                        (int)CalamityDusts.Brimstone,
                        player.velocity + new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-3f, -1f)),
                        110,
                        Main.rand.NextBool() ? BrimstoneRed : BrimstoneDark,
                        1.4f);
                    secDust.noGravity = true;
                }

                // VelChangingSpark (Demonic Flames style — velocity-accelerating bloom spark)
                Vector2 sparkVel = new(
                    Main.rand.NextFloat(-player.width / 8f, player.width / 8f),
                    Main.rand.NextFloat(-player.height / 18f, -player.height / 24f));
                GeneralParticleHandler.SpawnParticle(new VelChangingSpark(
                    player.Center + new Vector2(Main.rand.NextFloat(-10f, 10f), player.height * 0.25f) + sparkVel * 0.5f,
                    sparkVel + player.velocity,
                    new Vector2(-sparkVel.X * 0.5f, sparkVel.Y * 2f) * 2.4f,
                    "CalamityMod/Particles/SmallBloom",
                    Main.rand.Next(13, 19),
                    Main.rand.NextFloat(0.045f, stage >= 5 ? 0.082f : 0.065f),
                    Color.Lerp(DemonicViolet, BrimstoneRed, Main.rand.NextFloat(0.2f, 0.55f)) * 0.55f,
                    new Vector2(0.56f, 1.05f),
                    true, false, 0, false,
                    0.35f, 0.08f));
            }

            // Stage V extras — two independent 1/3 rolls add 0.33 + 0.33 = 0.67/frame → total 2.67/frame.
            if (stage >= 5)
            {
                // SparkParticle (Dragonfire style — ember spark drifting upward)
                if (Main.rand.NextBool(3))
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(
                        RandomBodyPoint(player, 0.42f, 0.46f),
                        player.velocity * 0.15f - Vector2.UnitY.RotatedByRandom(0.32f) * Main.rand.NextFloat(1.5f, 3.5f),
                        false,
                        Main.rand.Next(8, 12),
                        Main.rand.NextFloat(0.22f, 0.38f),
                        Color.Lerp(DragonEmber, BrimstoneRed, Main.rand.NextFloat(0.2f, 0.7f)) * 0.65f));

                // SmallSmokeParticle (Dragonfire style — thick rising smoke)
                if (Main.rand.NextBool(3))
                    GeneralParticleHandler.SpawnParticle(new SmallSmokeParticle(
                        RandomBodyPoint(player, 0.46f, 0.52f),
                        player.velocity * 0.2f - Vector2.UnitY.RotatedByRandom(0.45f) * Main.rand.NextFloat(1.2f, 3.4f),
                        SmokeGray,
                        Color.Lerp(Color.Black, SmokeGray, 0.35f),
                        Main.rand.NextFloat(0.36f, 0.78f),
                        0.42f,
                        Main.rand.NextFloat(-0.04f, 0.04f)));
            }
        }

        private static Vector2 RandomBodyPoint(Player player, float widthScale = 0.5f, float heightScale = 0.58f)
        {
            return player.Center + new Vector2(
                Main.rand.NextFloat(-player.width * widthScale, player.width * widthScale),
                Main.rand.NextFloat(-player.height * heightScale, player.height * heightScale));
        }
    }
}
