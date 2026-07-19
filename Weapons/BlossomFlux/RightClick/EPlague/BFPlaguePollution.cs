using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    internal sealed class BFPlagueWitherBuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/贴图/疫亡";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
    }

    internal sealed class BFPlagueDeathBuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/贴图/疫亡2";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
    }

    // Shared authority for Plague doctrine's two tactical debuffs and their staged disease package.
    internal sealed class BFPlaguePollutionNPC : GlobalNPC
    {
        public const int WitherDuration = 4 * 60;
        public const int DeathDuration = 60 * 60;

        private ulong lastLeafFlowDeathProcFrame;

        public override bool InstancePerEntity => true;

        public void ApplyWither(NPC npc)
        {
            AddOrExtendBuff(npc, ModContent.BuffType<BFPlagueWitherBuff>(), WitherDuration, int.MaxValue);
            ApplyStagedDiseaseDebuffs(npc);
        }

        public void ApplyDeath(NPC npc)
        {
            AddOrExtendBuff(npc, ModContent.BuffType<BFPlagueDeathBuff>(), DeathDuration, DeathDuration);
            ApplyStagedDiseaseDebuffs(npc);
        }

        public void ApplySupportingDebuffs(NPC npc) => ApplyStagedDiseaseDebuffs(npc);

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            bool withered = npc.HasBuff(ModContent.BuffType<BFPlagueWitherBuff>());
            bool dying = npc.HasBuff(ModContent.BuffType<BFPlagueDeathBuff>());

            if (withered)
            {
                npc.lifeRegen -= 88;
                damage = System.Math.Max(damage, 44);
            }

            if (!dying)
                return;

            int diseaseCount = CountPlagueDiseaseDebuffs(npc, withered);
            int damagePerSecond = 140 + 40 * diseaseCount;
            npc.lifeRegen -= damagePerSecond * 2;
            damage = System.Math.Max(damage, damagePerSecond);
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (npc.HasBuff(ModContent.BuffType<BFPlagueWitherBuff>()))
            {
                modifiers.FinalDamage *= 1.05f;
                modifiers.Defense.Base -= 1f;
            }

            if (npc.HasBuff(ModContent.BuffType<BFPlagueDeathBuff>()))
                modifiers.FinalDamage *= 1.1f;
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (!npc.HasBuff(ModContent.BuffType<BFPlagueDeathBuff>()) || !IsPlagueLeafFlow(projectile))
                return;

            ulong frame = Main.GameUpdateCount;
            if (frame < lastLeafFlowDeathProcFrame + 2)
                return;

            lastLeafFlowDeathProcFrame = frame;
            int extraDamage = System.Math.Max(1, (int)System.MathF.Round(damageDone * 0.1f));
            npc.SimpleStrikeNPC(extraDamage, hit.HitDirection, false, 0f, DamageClass.Ranged, false, noPlayerInteraction: true);
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            bool withered = npc.HasBuff(ModContent.BuffType<BFPlagueWitherBuff>());
            bool dying = npc.HasBuff(ModContent.BuffType<BFPlagueDeathBuff>());
            if (!withered && !dying)
                return;

            float deathIntensity = dying ? 1f : 0f;
            drawColor = Color.Lerp(drawColor, new Color(112, 178, 62), 0.12f + deathIntensity * 0.1f);
            Lighting.AddLight(npc.Center, new Vector3(0.035f, 0.10f, 0.018f) * (0.65f + deathIntensity * 0.5f));

            if (Main.dedServ || Main.GameUpdateCount % (dying ? 8 : 14) != 0)
                return;

            Dust dust = Dust.NewDustPerfect(
                npc.Center + Main.rand.NextVector2Circular(npc.width * 0.32f, npc.height * 0.32f),
                DustID.GreenTorch,
                new Vector2(Main.rand.NextFloat(-0.25f, 0.25f), Main.rand.NextFloat(-0.85f, -0.3f)),
                120,
                dying ? new Color(204, 236, 96) : new Color(96, 174, 64),
                dying ? Main.rand.NextFloat(0.75f, 1.05f) : Main.rand.NextFloat(0.55f, 0.82f));
            dust.noGravity = true;
        }

        private static void ApplyStagedDiseaseDebuffs(NPC npc)
        {
            npc.AddBuff(BuffID.Venom, 180);
            npc.AddBuff(BuffID.Poisoned, 180);

            if (DownedBossSystem.downedPlaguebringer)
                npc.AddBuff(ModContent.BuffType<Plague>(), 180);
            if (DownedBossSystem.downedSignus)
                npc.AddBuff(ModContent.BuffType<WhisperingDeath>(), 180);
            if (DownedBossSystem.downedAquaticScourge)
                npc.AddBuff(ModContent.BuffType<SulphuricPoisoning>(), 180);
            if (DownedBossSystem.downedExoMechs)
                npc.AddBuff(ModContent.BuffType<Irradiated>(), 180);
        }

        private static void AddOrExtendBuff(NPC npc, int buffType, int timeToAdd, int maxTime)
        {
            int index = npc.FindBuffIndex(buffType);
            if (index < 0)
            {
                npc.AddBuff(buffType, timeToAdd);
                return;
            }

            long extendedTime = (long)npc.buffTime[index] + timeToAdd;
            npc.buffTime[index] = (int)System.Math.Min(extendedTime, maxTime);
        }

        private static int CountPlagueDiseaseDebuffs(NPC npc, bool withered)
        {
            int count = withered ? 1 : 0;
            count += npc.HasBuff(BuffID.Venom) ? 1 : 0;
            count += npc.HasBuff(BuffID.Poisoned) ? 1 : 0;
            count += npc.HasBuff(ModContent.BuffType<Plague>()) ? 1 : 0;
            count += npc.HasBuff(ModContent.BuffType<WhisperingDeath>()) ? 1 : 0;
            count += npc.HasBuff(ModContent.BuffType<SulphuricPoisoning>()) ? 1 : 0;
            count += npc.HasBuff(ModContent.BuffType<Irradiated>()) ? 1 : 0;
            return count;
        }

        private static bool IsPlagueLeafFlow(Projectile projectile)
        {
            BFArrow_CDetecEffect leafFlow = projectile.GetGlobalProjectile<BFArrow_CDetecEffect>();
            return leafFlow.BlossomFluxLeftArrow && leafFlow.Preset == BlossomFluxChloroplastPresetType.Chlo_EPlague;
        }
    }

    internal sealed class BFPlagueTacticalGlobalProjectile : GlobalProjectile
    {
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            BFArrow_CDetecEffect leafFlow = projectile.GetGlobalProjectile<BFArrow_CDetecEffect>();
            if (!leafFlow.BlossomFluxLeftArrow || leafFlow.Preset != BlossomFluxChloroplastPresetType.Chlo_EPlague)
                return;

            target.GetGlobalNPC<BFPlaguePollutionNPC>().ApplyWither(target);
        }
    }
}
