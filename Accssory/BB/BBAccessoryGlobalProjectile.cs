using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken;
using CalamityLegendsComeBack.Weapons.BrinyBaron.Passive_QuickDash;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB
{
    internal sealed class BBAccessoryGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => false;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (projectile.ModProjectile is not BBSwing_Wave || !Main.player.IndexInRange(projectile.owner))
                return;

            if (Main.player[projectile.owner].GetModPlayer<BBAccessoryPlayer>().WaveInfinitePenetration)
                projectile.penetrate = -1;
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!IsBrinyBaronProjectile(projectile) || !Main.player.IndexInRange(projectile.owner))
                return;

            Main.player[projectile.owner].GetModPlayer<BBAccessoryPlayer>().RegisterBrinyBaronBladeHit(target);
        }

        private static bool IsBrinyBaronProjectile(Projectile projectile)
        {
            return projectile.ModProjectile is BrinyBaron_LeftClick_Swing or
                BBSwing_Wave or
                BrinyBaron_RightClick_Shuriken or
                BrinyBaron_SkillDashTornado_BladeDash or
                BrinyBaron_SkillSlashDash_SlashDash;
        }
    }
}
