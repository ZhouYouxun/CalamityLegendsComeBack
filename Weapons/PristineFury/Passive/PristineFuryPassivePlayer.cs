using CalamityLegendsComeBack.Accssory.PF;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.Passive
{
    internal sealed class PristineFuryPassivePlayer : ModPlayer
    {
        private const int DefaultTentacleCount = 3;
        private const int NineTailsCount = 9;
        private const int TailFlightRestoreCooldown = 5;
        private const float TailFlightRestoreRatio = 0.05f;
        private bool holdingPristineFury;
        private int tailFlightRestoreCooldown;

        public override void ResetEffects()
        {
            holdingPristineFury = false;
        }

        public override void UpdateDead()
        {
            holdingPristineFury = false;
            tailFlightRestoreCooldown = 0;
        }

        public void SetHoldingPristineFury()
        {
            holdingPristineFury = true;
        }

        public override void PostUpdate()
        {
            if (tailFlightRestoreCooldown > 0)
                tailFlightRestoreCooldown--;

            if (!holdingPristineFury || Player.HeldItem.type != ModContent.ItemType<NewLegendPristineFury>())
                return;

            if (Player.whoAmI != Main.myPlayer)
                return;

            EnsureTentacles();
        }

        internal void TryRestoreTailFlightTime()
        {
            if (Player.whoAmI != Main.myPlayer || tailFlightRestoreCooldown > 0 || Player.wingTimeMax <= 0)
                return;

            Player.wingTime = Math.Min(Player.wingTimeMax, Player.wingTime + Player.wingTimeMax * TailFlightRestoreRatio);
            tailFlightRestoreCooldown = TailFlightRestoreCooldown;
        }

        private void EnsureTentacles()
        {
            bool nineTails = Player.GetModPlayer<PFAccessoryPlayer>().NineTailsEquipped;
            int count = nineTails ? NineTailsCount : DefaultTentacleCount;
            int tentacleType = ModContent.ProjectileType<PristineFuryPassiveTentacle>();

            for (int index = 0; index < count; index++)
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
