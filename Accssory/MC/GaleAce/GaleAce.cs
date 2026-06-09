using CalamityLegendsComeBack.Weapons.Malachite;
using CalamityMod;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.MC.GaleAce
{
    public sealed class GaleAce : ModItem
    {
        public override string LocalizationCategory => "Items.Accessories";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 10);
            Item.rare = ItemRarityID.Yellow;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<GaleAcePlayer>().GaleAceEquipped = true;
        }
    }

    public sealed class GaleAcePlayer : ModPlayer
    {
        public bool GaleAceEquipped;

        public bool HoldingMalachite => Player.HeldItem.type == ModContent.ItemType<Weapons.Malachite.Malachite>();

        public override void ResetEffects()
        {
            GaleAceEquipped = false;
        }

        public override void PostUpdateEquips()
        {
            if (HoldingMalachite && GaleAceEquipped)
            {
                Player.GetDamage<RogueDamageClass>() += 0.15f;
                Player.GetCritChance<RogueDamageClass>() += 5f;
            }
        }
    }

    public sealed class GaleAceGlobalNPC : GlobalNPC
    {
        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (!npc.HasBuff(BuffID.Poisoned) || !AnyGaleAcePlayerActive())
                return;

            if (npc.lifeRegen > 0)
                npc.lifeRegen = 0;

            npc.lifeRegen -= 4;
            damage = Math.Max(damage, 2);
        }

        private static bool AnyGaleAcePlayerActive()
        {
            foreach (Player player in Main.ActivePlayers)
            {
                if (player.active && !player.dead && player.GetModPlayer<GaleAcePlayer>().GaleAceEquipped)
                    return true;
            }

            return false;
        }
    }
}
