using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    // 侦查效果挂在普通箭上，让左键常态箭学会优先追踪并触发补爆。
    internal class BFArrow_CDetecEffect : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool BlossomFluxLeftArrow;
        private bool triggeredPriorityExplosion;

        public override void AI(Projectile projectile)
        {
            if (!BlossomFluxLeftArrow || !projectile.active || !projectile.friendly)
                return;

            if (!BFArrowCommon.InBounds(projectile.owner, Main.maxPlayers))
                return;

            Player owner = Main.player[projectile.owner];
            if (!owner.active || owner.dead)
                return;

            BFRightUIPlayer rightUiPlayer = owner.GetModPlayer<BFRightUIPlayer>();
            int priorityTargetIndex = rightUiPlayer.ReconPriorityTargetIndex;
            if (!BFArrowCommon.InBounds(priorityTargetIndex, Main.maxNPCs))
                return;

            NPC priorityTarget = Main.npc[priorityTargetIndex];
            if (!priorityTarget.active || priorityTarget.dontTakeDamage || !priorityTarget.CanBeChasedBy(projectile))
            {
                rightUiPlayer.ClearReconPriorityTarget();
                return;
            }

            BFArrow_CDetecNPC markData = priorityTarget.GetGlobalNPC<BFArrow_CDetecNPC>();
            if (!markData.IsPriorityMarkedBy(projectile.owner))
                return;

            if (Vector2.Distance(projectile.Center, priorityTarget.Center) > 1400f)
                return;

            float speed = System.Math.Max(projectile.velocity.Length(), 8f);
            BFArrowCommon.WeakHomeTowards(projectile, priorityTarget, 30f, speed);
            BFArrowCommon.MaintainSpeed(projectile, speed, 0.09f);
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!BlossomFluxLeftArrow || triggeredPriorityExplosion || !BFArrowCommon.InBounds(projectile.owner, Main.maxPlayers))
                return;

            BFRightUIPlayer rightUiPlayer = Main.player[projectile.owner].GetModPlayer<BFRightUIPlayer>();
            if (target.whoAmI != rightUiPlayer.ReconPriorityTargetIndex)
                return;

            BFArrow_CDetecNPC markData = target.GetGlobalNPC<BFArrow_CDetecNPC>();
            if (!markData.IsPriorityMarkedBy(projectile.owner))
                return;

            triggeredPriorityExplosion = true;
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<BFFuckyouExplosion>(),
                (int)(projectile.damage * 0.7f),
                projectile.knockBack * 0.65f,
                projectile.owner);
        }
    }
}
