using CalamityLegendsComeBack.Weapons.PristineFury;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.PF
{
    // Hooks PF left-click projectile hits to register purification progress.
    internal sealed class PFAccessoryGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => false;

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 月饼：右键新星命中时在命中位置留下月华烙印。
            bool isNova = projectile.type == ModContent.ProjectileType<PristineFuryRightNovaFireball>();
            if (isNova && Main.player.IndexInRange(projectile.owner))
            {
                Player owner = Main.player[projectile.owner];
                if (owner.active && owner.GetModPlayer<PFAccessoryPlayer>().MooncakeEquipped && Main.myPlayer == projectile.owner)
                    SpawnMoonhaloStamp(projectile, target.Center);
            }

            // 纯化进度：左键弹幕命中时推进。
            if (IsPFLeftClickProjectile(projectile))
                target.GetGlobalNPC<PFPurificationGlobalNPC>().RegisterPFHit(projectile.owner);
        }

        private static void SpawnMoonhaloStamp(Projectile source, Vector2 position)
        {
            Projectile.NewProjectile(
                source.GetSource_FromThis(),
                position,
                Vector2.Zero,
                ModContent.ProjectileType<Skill.PFMoonhaloStamp>(),
                0,
                0f,
                source.owner);
        }

        // Any friendly, non-hostile projectile fired while the owner holds Pristine Fury.
        private static bool IsPFLeftClickProjectile(Projectile proj)
        {
            if (!Main.player.IndexInRange(proj.owner) || !proj.friendly || proj.hostile)
                return false;

            // Right-click nova fireballs are deliberately excluded — they trigger purification
            // only indirectly via 月饼 (Mooncake) rather than directly advancing the gauge.
            if (proj.type == ModContent.ProjectileType<PristineFuryRightNovaFireball>())
                return false;

            Player owner = Main.player[proj.owner];
            return owner.active && owner.HeldItem?.type == ModContent.ItemType<NewLegendPristineFury>();
        }
    }
}
