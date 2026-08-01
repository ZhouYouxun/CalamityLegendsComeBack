using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Passive.Pa5
{
    internal sealed class BFPa5PlaguePlayer : ModPlayer
    {
    }

    internal sealed class BFPa5PlagueGlobalProjectile : GlobalProjectile
    {
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!projectile.friendly || projectile.damage <= 0 || projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;

            Player owner = Main.player[projectile.owner];
            if (!BFPa5PassiveSystem.IsActive(owner, BlossomFluxChloroplastPresetType.Chlo_EPlague))
                return;

            if (!BFAccessorySystem.TryGetBlossomFluxPreset(projectile, out _))
                return;

            ExtendExistingDebuffs(target);
        }

        private static void ExtendExistingDebuffs(NPC target)
        {
            BFPlagueLeftStats stats = BFPlagueLeftBalance.GetStats();
            int addTime = stats.StackDuration;
            int maxTime = stats.MaxDuration;
            for (int i = 0; i < target.buffType.Length; i++)
            {
                int buffType = target.buffType[i];
                if (buffType <= 0 || buffType >= Main.debuff.Length || target.buffTime[i] <= 0 || !Main.debuff[buffType])
                    continue;

                // The staged Plague package already adds exactly ten seconds per
                // application. Skip it here so this general passive cannot double it.
                if (BFPlaguePollutionNPC.IsStagedDiseaseDebuff(buffType))
                    continue;

                int cap = System.Math.Max(maxTime, target.buffTime[i]);
                target.buffTime[i] = System.Math.Min(target.buffTime[i] + addTime, cap);
            }
        }
    }

    internal sealed class BFPa5PlagueGlobalNPC : GlobalNPC
    {
        public override void PostAI(NPC npc)
        {
            if (!BFPa5PassiveSystem.IsCountedEnemy(npc) ||
                !BFPa5PassiveSystem.AnyPlayerActive(BlossomFluxChloroplastPresetType.Chlo_EPlague) ||
                Main.GameUpdateCount % 2 != 0)
            {
                return;
            }

            for (int i = 0; i < npc.buffType.Length; i++)
            {
                int buffType = npc.buffType[i];
                if (buffType <= 0 || buffType >= Main.debuff.Length || npc.buffTime[i] <= 1 || !Main.debuff[buffType])
                    continue;

                npc.buffTime[i]++;
            }
        }
    }
}
