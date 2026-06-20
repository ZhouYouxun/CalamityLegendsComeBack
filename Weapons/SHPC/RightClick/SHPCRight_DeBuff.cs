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

        private static readonly Color BurnOrange = new(230, 85, 20);
        private static readonly Color BurnRed = new(175, 28, 12);
        private static readonly Color EmberGold = new(245, 150, 25);

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

            // Core: DirectionalPulseRing in fire colors (Plague-style signature)
            Vector2 ringPos = RandomBodyPoint(player, 0.45f, 0.50f);
            float endScale = stage >= 5
                ? Main.rand.NextFloat(0.13f, 0.21f)
                : Main.rand.NextFloat(0.08f, 0.15f);
            Color ringColor = Main.rand.NextBool(3) ? BurnOrange : BurnRed;
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                ringPos,
                Vector2.Zero,
                ringColor * 0.70f,
                new Vector2(1f, 1f),
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.02f,
                endScale,
                15));

            // 4 small fire dusts per trigger (mirrors Plague's 4 ToxicSludge dusts)
            for (int i = 0; i < 4; i++)
            {
                Vector2 dustPos = RandomBodyPoint(player, 0.48f, 0.52f);
                bool isGoldAccent = Main.rand.NextBool(30); // ~3.3%, mirrors Plague's SporeColony ratio
                int dustType = isGoldAccent ? DustID.GemTopaz : DustID.CopperCoin;
                float dustScale = isGoldAccent
                    ? Main.rand.NextFloat(0.7f, 1.0f)
                    : Main.rand.NextFloat(0.28f, 0.42f);
                Dust dust = Dust.NewDustPerfect(
                    dustPos,
                    dustType,
                    player.velocity + new Vector2(Main.rand.NextFloat(-2.0f, 2.0f), Main.rand.NextFloat(-2.5f, -0.5f)),
                    0, default, dustScale);
                dust.noGravity = true;
            }

            // Stage 5: low-frequency warm ember accent, not blinding
            if (stage >= 5 && Main.rand.NextBool(4))
            {
                PointParticle ember = new(
                    RandomBodyPoint(player, 0.40f, 0.45f),
                    player.velocity * 0.15f + new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-1.2f, -0.3f)),
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.45f, 0.65f),
                    Color.Lerp(BurnOrange, EmberGold, Main.rand.NextFloat()) * 0.62f);
                GeneralParticleHandler.SpawnParticle(ember);
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
