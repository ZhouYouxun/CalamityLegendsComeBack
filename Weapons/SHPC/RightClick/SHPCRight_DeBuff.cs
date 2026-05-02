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
        // 1 = 普通过热，2 = 高烈度过热
        public static int FireMode = 1;

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

            // ===== Heat4 =====
            if (stage == 4)
            {
                if (player.statLife > 100)
                {
                    if (Main.GameUpdateCount % 30 == 0)
                        ApplyOverheatDamage(player, 1);
                }
            }

            // ===== Heat5 =====
            else if (stage >= 5)
            {
                if (Main.GameUpdateCount % 5 == 0)
                    ApplyOverheatDamage(player, Main.rand.Next(1, 3));

                if (Main.rand.NextBool(12))
                {
                    Vector2 pos = player.Center;

                    CombatText.NewText(
                        new Rectangle((int)pos.X, (int)pos.Y, player.width, player.height),
                        Color.Red,
                        "锟斤拷烫烫烫！！！"
                    );
                }
            }


            ApplyBurningVisual(player, stage);

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

        private void ApplyBurningVisual(Player player, int stage)
        {
            if (stage < 4)
                return;

            // ===== 基础燃烧（始终存在）=====
            ApplyStage5Burn(player);

            // ===== Heat5 叠加 =====
            if (stage >= 5)
            {
                ApplyStage6PanicBurn(player);
                ApplyStage7Meltdown(player);
            }
        }

        private void ApplyStage5Burn(Player player)
        {
            Vector2 center = player.Center;

            // ===== 从身体范围随机取点 =====
            Vector2 randPos = center + new Vector2(
                Main.rand.NextFloat(-player.width * 0.4f, player.width * 0.4f),
                Main.rand.NextFloat(-player.height * 0.5f, player.height * 0.5f)
            );

            // ===== 向上 + 旋转（火焰感）=====
            Vector2 upward = new Vector2(
                Main.rand.NextFloat(-0.6f, 0.6f),
                Main.rand.NextFloat(-2.5f, -1.2f)
            );

            // ===== ① 火焰主体（Squishy）=====
            if (Main.rand.NextBool(2))
            {
                SquishyLightParticle flame = new(
                    randPos,
                    upward.RotatedByRandom(0.4f),
                    Main.rand.NextFloat(0.35f, 0.6f),
                    Color.Lerp(Color.OrangeRed, Color.Yellow, Main.rand.NextFloat(0.3f, 0.8f)),
                    Main.rand.Next(12, 20),
                    1f,
                    Main.rand.NextFloat(1.2f, 1.8f)
                );

                GeneralParticleHandler.SpawnParticle(flame);
            }

            // ===== ② 火星（Dust）=====
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    randPos,
                    DustID.Torch,
                    upward.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.8f, 2.2f),
                    0,
                    Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.0f, 1.6f)
                );
                d.noGravity = true;
            }

            // ===== ③ 慌张感：微爆闪 =====
            if (Main.rand.NextBool(4))
            {
                PointParticle spark = new PointParticle(
                    randPos,
                    upward * 0.5f,
                    false,
                    10,
                    1.1f,
                    Color.OrangeRed
                );

                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        private void ApplyStage6PanicBurn(Player player)
        {
            Vector2 center = player.Center;
            Vector2 panicSource = center + new Vector2(
                Main.rand.NextFloat(-player.width * 0.55f, player.width * 0.55f),
                Main.rand.NextFloat(-player.height * 0.58f, player.height * 0.18f));

            Vector2 upwardBurst = new Vector2(
                Main.rand.NextFloat(-1.3f, 1.3f),
                Main.rand.NextFloat(-4.6f, -2.2f));

            if (Main.rand.NextBool())
            {
                SquishyLightParticle panicFlame = new(
                    panicSource,
                    upwardBurst.RotatedByRandom(0.65f),
                    Main.rand.NextFloat(0.52f, 0.88f),
                    Color.Lerp(Color.OrangeRed, Color.Yellow, Main.rand.NextFloat(0.35f, 0.95f)),
                    Main.rand.Next(14, 24),
                    1f,
                    Main.rand.NextFloat(1.7f, 2.5f)
                );
                GeneralParticleHandler.SpawnParticle(panicFlame);
            }

            if (Main.rand.NextBool(2))
            {
                Dust flash = Dust.NewDustPerfect(
                    panicSource,
                    Main.rand.NextBool() ? DustID.Torch : DustID.InfernoFork,
                    upwardBurst.RotatedByRandom(0.8f) * Main.rand.NextFloat(1.6f, 3.4f),
                    0,
                    Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat(0.25f, 0.9f)),
                    Main.rand.NextFloat(1.25f, 2.1f));
                flash.noGravity = true;
            }

            if (Main.rand.NextBool(3))
            {
                Vector2 sideKick = new Vector2(Main.rand.NextBool() ? -1f : 1f, 0f) * Main.rand.NextFloat(0.9f, 2.2f);
                PointParticle panicSpark = new PointParticle(
                    panicSource,
                    upwardBurst * 0.42f + sideKick,
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(1.15f, 1.55f),
                    Color.Lerp(Color.OrangeRed, Color.White, Main.rand.NextFloat(0.12f, 0.28f)));
                GeneralParticleHandler.SpawnParticle(panicSpark);
            }

            if (Main.rand.NextBool(5))
            {
                GlowOrbParticle heatCore = new GlowOrbParticle(
                    center + Main.rand.NextVector2Circular(player.width * 0.18f, player.height * 0.22f),
                    upwardBurst * 0.08f,
                    false,
                    Main.rand.Next(7, 10),
                    Main.rand.NextFloat(0.36f, 0.54f),
                    Color.Lerp(Color.OrangeRed, Color.Gold, Main.rand.NextFloat(0.35f, 0.75f)),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(heatCore);
            }
        }

        private void ApplyStage7Meltdown(Player player)
        {
            Vector2 center = player.Center;
            Vector2 bodyCore = center + new Vector2(
                Main.rand.NextFloat(-player.width * 0.62f, player.width * 0.62f),
                Main.rand.NextFloat(-player.height * 0.68f, player.height * 0.26f));

            for (int i = 0; i < 2; i++)
            {
                float fanT = Main.rand.NextFloat(-1f, 1f);
                Vector2 eruptionVelocity = new Vector2(
                    fanT * Main.rand.NextFloat(1.8f, 4.2f),
                    Main.rand.NextFloat(-6.8f, -3.4f));

                SquishyLightParticle meltdownFlame = new(
                    bodyCore + new Vector2(fanT * player.width * 0.18f, Main.rand.NextFloat(-player.height * 0.18f, player.height * 0.12f)),
                    eruptionVelocity.RotatedByRandom(0.8f),
                    Main.rand.NextFloat(0.72f, 1.18f),
                    Color.Lerp(Color.OrangeRed, Color.Yellow, Main.rand.NextFloat(0.45f, 0.98f)),
                    Main.rand.Next(18, 30),
                    1f,
                    Main.rand.NextFloat(2.3f, 3.4f)
                );
                GeneralParticleHandler.SpawnParticle(meltdownFlame);
            }

            for (int i = 0; i < 2; i++)
            {
                Dust infernoDust = Dust.NewDustPerfect(
                    bodyCore + Main.rand.NextVector2Circular(player.width * 0.24f, player.height * 0.3f),
                    Main.rand.NextBool() ? DustID.Torch : DustID.InfernoFork,
                    new Vector2(Main.rand.NextFloat(-2.8f, 2.8f), Main.rand.NextFloat(-6.2f, -2.6f)).RotatedByRandom(0.7f),
                    0,
                    Color.Lerp(Color.OrangeRed, Color.White, Main.rand.NextFloat(0.08f, 0.22f)),
                    Main.rand.NextFloat(1.65f, 2.75f));
                infernoDust.noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                PointParticle panicFlash = new PointParticle(
                    bodyCore + Main.rand.NextVector2Circular(player.width * 0.12f, player.height * 0.16f),
                    new Vector2(Main.rand.NextFloat(-2.6f, 2.6f), Main.rand.NextFloat(-3.8f, -1.6f)),
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(1.5f, 2f),
                    Color.Lerp(Color.OrangeRed, Color.Yellow, Main.rand.NextFloat(0.25f, 0.65f)));
                GeneralParticleHandler.SpawnParticle(panicFlash);
            }

            if (Main.rand.NextBool(3))
            {
                GlowOrbParticle overloadPulse = new GlowOrbParticle(
                    center + Main.rand.NextVector2Circular(player.width * 0.22f, player.height * 0.28f),
                    new Vector2(Main.rand.NextFloat(-0.35f, 0.35f), Main.rand.NextFloat(-0.9f, -0.15f)),
                    false,
                    Main.rand.Next(8, 12),
                    Main.rand.NextFloat(0.48f, 0.7f),
                    Color.Lerp(Color.Red, Color.Gold, Main.rand.NextFloat(0.45f, 0.85f)),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(overloadPulse);
            }
        }










    }
}
