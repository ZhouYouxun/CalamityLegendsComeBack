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

        private static readonly Color TechBlue = new(40, 210, 255);
        private static readonly Color DeepTechBlue = new(0, 120, 220);
        private static readonly Color TechBurnColor = Color.Lerp(TechBlue, Color.White, 0.3f);
        private static readonly Color TechBurnBloom = Color.Lerp(DeepTechBlue, Color.White, 0.3f);

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

            SpawnHolyTechFlames(player, stage);
            Lighting.AddLight(player.Center, TechBurnColor.ToVector3() * (stage >= 5 ? 0.45f : 0.32f));
        }

        private static void SpawnHolyTechFlames(Player player, int stage)
        {
            bool finalStage = stage >= 5;
            int mainChance = finalStage ? 2 : 3;

            if (Main.rand.NextBool(mainChance))
            {
                Vector2 sparkVelocity = new Vector2(0f, Main.rand.NextBool(4) ? -5f : -9f)
                    .RotatedByRandom(MathHelper.ToRadians(25f)) * Main.rand.NextFloat(0.1f, 1.9f);

                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    RandomBodyPoint(player, 0.52f, 0.58f),
                    sparkVelocity + player.velocity * 0.2f,
                    TechBurnColor,
                    TechBurnBloom,
                    finalStage ? 1.12f : 0.96f,
                    finalStage ? 19 : 17,
                    finalStage ? 2.45f : 2.15f,
                    finalStage ? 2.35f : 2.05f));
            }

            if (Main.rand.NextBool(mainChance))
            {
                Vector2 dustCorner = player.position - 2f * Vector2.One;
                Vector2 dustVelocity = player.velocity + new Vector2(0f, Main.rand.NextFloat(-5f, -1f));
                Dust fire = Dust.NewDustDirect(dustCorner, player.width + 4, player.height + 4, DustID.GemTopaz, dustVelocity.X, dustVelocity.Y);
                fire.noGravity = true;
                fire.scale = Main.rand.NextFloat(finalStage ? 0.9f : 0.75f, finalStage ? 1.45f : 1.25f);
                fire.alpha = 225;
                fire.color = TechBurnColor;
            }

            if (!finalStage)
                return;

            if (Main.rand.NextBool(3))
            {
                Vector2 sparkVelocity = new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-7.5f, -3.5f))
                    .RotatedByRandom(MathHelper.ToRadians(18f));

                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    RandomBodyPoint(player, 0.45f, 0.52f),
                    sparkVelocity + player.velocity * 0.15f,
                    Color.White,
                    TechBurnColor,
                    0.72f,
                    13,
                    1.65f,
                    1.75f));
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
