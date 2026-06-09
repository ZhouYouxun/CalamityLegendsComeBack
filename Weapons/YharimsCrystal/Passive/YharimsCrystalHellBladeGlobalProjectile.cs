using System;
using System.Linq;
using CalamityMod;
using CalamityMod.DataStructures;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive
{
    internal sealed class YharimsCrystalHellBladeGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool FromYharimsCrystal;
        public YCWeaponForm Form;
        public bool CountsForPassive = true;

        public static void Mark(Projectile projectile, YCWeaponForm form, bool countsForPassive = true)
        {
            if (projectile?.ModProjectile == null && projectile == null)
                return;

            var global = projectile.GetGlobalProjectile<YharimsCrystalHellBladeGlobalProjectile>();
            global.FromYharimsCrystal = true;
            global.Form = form;
            global.CountsForPassive = countsForPassive;
        }

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!FromYharimsCrystal || !IsOwnerHoldingCrystal(projectile))
                return;

            if (!HasHeatDebuff(target))
                return;

            modifiers.DefenseEffectiveness *= 0f;
            float dr = Math.Clamp(target.Calamity().DR, 0f, 0.95f);
            modifiers.FinalDamage /= 1f - dr;
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!FromYharimsCrystal || !IsOwnerHoldingCrystal(projectile))
                return;

            Player player = Main.player[projectile.owner];
            BalanceYharimsCrystal balance = new();
            target.AddBuff(balance.GetFireDebuffType(), 300);

            if (!CountsForPassive || !hit.Crit || !HasHeatDebuff(target))
                return;

            if (Form == YCWeaponForm.Blade)
            {
                int heal = balance.GetPassiveLifeRestore(player);
                player.statLife += heal;
                if (player.statLife > player.statLifeMax2)
                    player.statLife = player.statLifeMax2;
                player.HealEffect(heal);
            }
            else
            {
                int mana = balance.GetPassiveManaRestore(player);
                player.statMana += mana;
                if (player.statMana > player.statManaMax2)
                    player.statMana = player.statManaMax2;
                player.ManaEffect(mana);
            }
        }

        internal static bool HasHeatDebuff(NPC target)
        {
            return target.buffType.Any(buffID =>
                buffID > 0 &&
                buffID < BuffDatasets.DebuffDataset.Length &&
                BuffDatasets.DebuffDataset[buffID] is not null &&
                BuffDatasets.DebuffDataset[buffID].HeatDebuffScaling > 0);
        }

        private static bool IsOwnerHoldingCrystal(Projectile projectile)
        {
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return false;

            Player player = Main.player[projectile.owner];
            return player.active &&
                player.HeldItem != null &&
                !player.HeldItem.IsAir &&
                player.HeldItem.type == ModContent.ItemType<NewLegendYharimsCrystal>();
        }
    }
}
