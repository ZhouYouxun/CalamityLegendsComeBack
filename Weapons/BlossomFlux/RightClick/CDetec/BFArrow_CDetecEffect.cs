using CalamityLegendsComeBack.Weapons.BlossomFlux;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    internal class BFArrow_CDetecEffect : GlobalProjectile
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override bool InstancePerEntity => true;

        public bool BlossomFluxLeftArrow;
        public bool ConvertedLeafArrow;
        public BlossomFluxChloroplastPresetType Preset;

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!BlossomFluxLeftArrow || ConvertedLeafArrow || Preset != BlossomFluxChloroplastPresetType.Chlo_ABreak)
                return;

            if (target.GetGlobalNPC<BFArrow_CDetecNPC>().IsPriorityMarkedBy(projectile.owner))
                modifiers.FinalDamage *= 1.4f;
        }
    }
}
