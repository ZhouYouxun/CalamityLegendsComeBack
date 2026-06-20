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

        private static readonly Color PlagueGreen = new(60, 180, 55);
        private static readonly Color PlagueLime = new(120, 215, 65);
        private static readonly Color SmolderRed = new(120, 24, 16);
        private const int ToxicSludgeDust = 89;
        private const int SporeColonyDust = 220;

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

            // Frequency aligned with Calamity Plague debuff: 1/3 per frame
            if (!Main.rand.NextBool(3))
                return;

            // Core: Calamity Plague-style DirectionalPulseRing.
            Vector2 ringPos = RandomBodyPoint(player, 0.45f, 0.50f);
            float endScale = stage >= 5
                ? Main.rand.NextFloat(0.12f, 0.19f)
                : Main.rand.NextFloat(0.07f, 0.15f);
            Color ringColor = Main.rand.NextBool(3) ? PlagueLime : PlagueGreen;
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                ringPos,
                Vector2.Zero,
                ringColor * 0.68f,
                new Vector2(1f, 1f),
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.018f,
                endScale,
                15));

            // 4 toxic dusts per trigger, matching Plague's cadence and density.
            for (int i = 0; i < 4; i++)
            {
                Vector2 dustPos = RandomBodyPoint(player, 0.48f, 0.52f);
                bool sporeAccent = Main.rand.NextBool(30); // ~3.3%, mirrors Plague's SporeColony ratio
                int dustType = sporeAccent ? SporeColonyDust : ToxicSludgeDust;
                float dustScale = sporeAccent
                    ? Main.rand.NextFloat(0.9f, 1.15f)
                    : Main.rand.NextFloat(0.28f, 0.40f);
                Dust dust = Dust.NewDustPerfect(
                    dustPos,
                    dustType,
                    player.velocity * 0.25f + Main.rand.NextVector2Circular(1.15f, 1.15f),
                    0, default, dustScale);
                dust.noGravity = true;
            }

            // Stage 5: sparse, dark heat accent with no bright sparks.
            if (stage >= 5 && Main.rand.NextBool(3))
            {
                Dust smolder = Dust.NewDustPerfect(
                    RandomBodyPoint(player, 0.44f, 0.48f),
                    DustID.RuneWizard,
                    player.velocity * 0.2f + new Vector2(Main.rand.NextFloat(-0.7f, 0.7f), Main.rand.NextFloat(-1.5f, -0.3f)),
                    120,
                    SmolderRed,
                    Main.rand.NextFloat(0.65f, 0.95f));
                smolder.noGravity = true;
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
