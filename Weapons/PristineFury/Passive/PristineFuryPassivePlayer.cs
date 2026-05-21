using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.Passive
{
    internal sealed class PristineFuryPassivePlayer : ModPlayer
    {
        private const int TentacleCount = 3;
        private bool holdingPristineFury;

        public override void ResetEffects()
        {
            holdingPristineFury = false;
        }

        public override void UpdateDead()
        {
            holdingPristineFury = false;
        }

        public void SetHoldingPristineFury()
        {
            holdingPristineFury = true;
        }

        public override void PostUpdate()
        {
            if (!holdingPristineFury || Player.HeldItem.type != ModContent.ItemType<NewLegendPristineFury>())
                return;

            if (Player.whoAmI != Main.myPlayer)
                return;

            EnsureTentacles();
        }

        private void EnsureTentacles()
        {
            int tentacleType = ModContent.ProjectileType<PristineFuryPassiveTentacle>();
            for (int index = 0; index < TentacleCount; index++)
            {
                bool found = false;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile projectile = Main.projectile[i];
                    if (projectile.active && projectile.owner == Player.whoAmI && projectile.type == tentacleType && (int)projectile.ai[0] == index)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                    continue;

                Projectile.NewProjectile(
                    Player.GetSource_FromThis(),
                    Player.Center,
                    Vector2.Zero,
                    tentacleType,
                    0,
                    0f,
                    Player.whoAmI,
                    index);
            }
        }
    }
}
