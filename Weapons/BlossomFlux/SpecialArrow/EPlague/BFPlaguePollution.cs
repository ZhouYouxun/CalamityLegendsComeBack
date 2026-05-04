using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    internal class BFPlaguePollutionBuff : ModBuff
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
    }

    internal class BFPlaguePollutionNPC : GlobalNPC
    {
        public const int PollutionDuration = 15 * 60;
        public const int MaxPollutionStacks = 15;
        public const float DefenseReductionPerStack = 0.05f;

        private readonly BalanceBlossomFlux balance = new();
        private int pollutionTimeLeft;
        private int pollutionStacks;
        private int permanentSporeStacks;

        public override bool InstancePerEntity => true;

        public void ApplyPollution(NPC npc, bool markedTarget = false)
        {
            int duration = markedTarget ? PollutionDuration * 2 : PollutionDuration;
            pollutionTimeLeft = System.Math.Max(pollutionTimeLeft, duration);
            pollutionStacks = Utils.Clamp(pollutionStacks + 1, 1, MaxPollutionStacks);
            npc.AddBuff(ModContent.BuffType<BFPlaguePollutionBuff>(), pollutionTimeLeft);
        }

        public void ApplyPlagueDebuffs(NPC npc, bool markedTarget)
        {
            BalanceBlossomFlux.PlagueDebuffStats stats = balance.GetPlagueDebuffStats();
            bool alreadyAfflicted = HasAnyPlagueDebuff(npc);
            int addTime = alreadyAfflicted ? stats.StackDuration : stats.InitialDuration;
            int maxTime = stats.MaxDuration;
            if (markedTarget)
            {
                addTime *= 2;
                maxTime *= 2;
            }

            AddOrExtendBuff(npc, BuffID.Ichor, addTime, maxTime);
            AddOrExtendBuff(npc, BuffID.Venom, addTime, maxTime);
            AddOrExtendBuff(npc, ModContent.BuffType<Irradiated>(), addTime, maxTime);
            AddOrExtendBuff(npc, ModContent.BuffType<Plague>(), addTime, maxTime);
            AddOrExtendBuff(npc, ModContent.BuffType<MarkedforDeath>(), addTime, maxTime);

            if (stats.InflictDragonfire)
                AddOrExtendBuff(npc, ModContent.BuffType<Dragonfire>(), addTime, maxTime);

            if (stats.InflictAstralInfection)
                AddOrExtendBuff(npc, ModContent.BuffType<AstralInfectionDebuff>(), addTime, maxTime);

            if (stats.InflictWither)
                AddOrExtendBuff(npc, ModContent.BuffType<WitherDebuff>(), addTime, maxTime);

            if (stats.InflictWhisperingDeath)
                AddOrExtendBuff(npc, ModContent.BuffType<WhisperingDeath>(), addTime, maxTime);

            if (stats.InflictAbsorberAffliction)
                AddOrExtendBuff(npc, ModContent.BuffType<AbsorberAffliction>(), addTime, maxTime);
        }

        public void ApplyPermanentSpore(NPC npc)
        {
            BalanceBlossomFlux.PlagueChargeStats stats = balance.GetPlagueChargeStats();
            permanentSporeStacks = Utils.Clamp(permanentSporeStacks + 1, 0, stats.MaxPermanentStacks);

            if (Main.dedServ)
                return;

            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    npc.Center + Main.rand.NextVector2Circular(npc.width * 0.36f, npc.height * 0.36f),
                    DustID.GreenTorch,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 3.8f),
                    120,
                    Color.Lerp(new Color(62, 170, 58), new Color(210, 255, 96), Main.rand.NextFloat(0.15f, 0.65f)),
                    Main.rand.NextFloat(0.72f, 1.2f));
                dust.noGravity = true;
            }
        }

        public override void PostAI(NPC npc)
        {
            if (pollutionTimeLeft <= 0)
            {
                pollutionStacks = 0;
            }
            else
            {
                pollutionTimeLeft--;
                int buffType = ModContent.BuffType<BFPlaguePollutionBuff>();
                int buffIndex = npc.FindBuffIndex(buffType);
                if (buffIndex >= 0)
                    npc.buffTime[buffIndex] = pollutionTimeLeft;
                else
                    npc.AddBuff(buffType, pollutionTimeLeft);
            }

            EmitPollutionDust(npc);
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            float pollutionReduction = GetDefenseReduction();
            if (pollutionReduction > 0f)
                modifiers.Defense *= 1f - pollutionReduction;

            if (permanentSporeStacks <= 0)
                return;

            BalanceBlossomFlux.PlagueChargeStats stats = balance.GetPlagueChargeStats();
            modifiers.Defense.Base -= stats.DefenseReductionPerStack * permanentSporeStacks;
            modifiers.FinalDamage *= 1f + stats.VulnerabilityPerStack * permanentSporeStacks;
        }

        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            if (permanentSporeStacks <= 0)
                return;

            BalanceBlossomFlux.PlagueChargeStats stats = balance.GetPlagueChargeStats();
            modifiers.FinalDamage *= 1f - stats.NpcDamageReductionPerStack * permanentSporeStacks;
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            float reduction = GetDefenseReduction();
            if (reduction <= 0f && permanentSporeStacks <= 0)
                return;

            float intensity = MathHelper.Clamp(0.14f + reduction * 0.28f + permanentSporeStacks * 0.06f, 0.12f, 0.48f);
            drawColor = Color.Lerp(drawColor, new Color(94, 180, 72), intensity);
            Lighting.AddLight(npc.Center, new Vector3(0.05f, 0.12f, 0.035f) * (0.4f + reduction + permanentSporeStacks * 0.12f));
        }

        private static void AddOrExtendBuff(NPC npc, int buffType, int addTime, int maxTime)
        {
            int index = npc.FindBuffIndex(buffType);
            int finalTime = index >= 0 ? System.Math.Min(npc.buffTime[index] + addTime, maxTime) : addTime;
            npc.AddBuff(buffType, finalTime);
        }

        private static bool HasAnyPlagueDebuff(NPC npc)
        {
            return
                npc.FindBuffIndex(BuffID.Ichor) >= 0 ||
                npc.FindBuffIndex(BuffID.Venom) >= 0 ||
                npc.FindBuffIndex(ModContent.BuffType<Irradiated>()) >= 0 ||
                npc.FindBuffIndex(ModContent.BuffType<Plague>()) >= 0 ||
                npc.FindBuffIndex(ModContent.BuffType<MarkedforDeath>()) >= 0;
        }

        private void EmitPollutionDust(NPC npc)
        {
            if (Main.dedServ || Main.GameUpdateCount % 18 != 0)
                return;

            if (pollutionTimeLeft <= 0 && permanentSporeStacks <= 0)
                return;

            Vector2 dustPosition = npc.Center + Main.rand.NextVector2Circular(npc.width * 0.36f, npc.height * 0.36f);
            Dust dust = Dust.NewDustPerfect(
                dustPosition,
                DustID.GreenTorch,
                -Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.25f, 0.85f),
                150,
                Color.Lerp(new Color(58, 155, 50), new Color(160, 220, 76), GetDefenseReduction()),
                Main.rand.NextFloat(0.5f, 0.82f));
            dust.noGravity = true;
        }

        private float GetDefenseReduction()
        {
            if (pollutionTimeLeft <= 0 || pollutionStacks <= 0)
                return 0f;

            return MathHelper.Clamp(pollutionStacks * DefenseReductionPerStack, 0f, MaxPollutionStacks * DefenseReductionPerStack);
        }
    }
}
