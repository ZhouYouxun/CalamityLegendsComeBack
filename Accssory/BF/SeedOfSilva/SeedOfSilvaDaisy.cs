using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using Microsoft.Xna.Framework;
using Terraria;

namespace CalamityLegendsComeBack.Accssory.BF.SeedOfSilva
{
    internal sealed class SeedOfSilvaDaisy : SeedOfSilvaFlowerProjectile
    {
        private const float DaisyMaxLife = 15f;
        private const float DaisyRegenPerFrame = 1.2f / 60f;

        private float daisyLife = DaisyMaxLife;
        private float daisyStoredHeal;
        private int daisyHitCooldown;

        protected override int FlowerSlot => 1;
        protected override BlossomFluxChloroplastPresetType FlowerPreset => BlossomFluxChloroplastPresetType.Chlo_BRecov;
        protected override string FlowerTexturePath => "CalamityLegendsComeBack/Accssory/BF/SeedOfSilva/种子包/雏菊";

        protected override void UpdateBlooming(Player owner, BFAccessoryPlayer accessoryPlayer)
        {
            daisyLife = System.Math.Min(DaisyMaxLife, daisyLife + DaisyRegenPerFrame);
            daisyStoredHeal += DaisyRegenPerFrame;

            if (daisyHitCooldown > 0)
            {
                daisyHitCooldown--;
                return;
            }

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy() || !npc.Hitbox.Intersects(Projectile.Hitbox))
                    continue;

                int incomingDamage = System.Math.Max(1, npc.damage);
                if (incomingDamage < 20)
                    continue;

                daisyHitCooldown = 30;
                daisyLife = System.Math.Max(0f, daisyLife - System.Math.Max(1f, incomingDamage * (1f - owner.endurance) - owner.statDefense * 0.5f));
                int healAmount = (int)System.Math.Floor(daisyStoredHeal);
                if (healAmount > 0 && owner.statLife < owner.statLifeMax2)
                {
                    owner.statLife = System.Math.Min(owner.statLifeMax2, owner.statLife + healAmount);
                    owner.HealEffect(healAmount, true);
                    daisyStoredHeal -= healAmount;
                }

                EmitFlowerBurst(new Color(150, 255, 174), 5);
                break;
            }
        }
    }
}
