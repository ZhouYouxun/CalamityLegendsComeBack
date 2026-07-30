using System;
using CalamityLegendsComeBack.Weapons.AegisBlade;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BrinyBaron;
using CalamityLegendsComeBack.Weapons.CosmicDischarge;
using CalamityLegendsComeBack.Weapons.GaelsGreatsword;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor;
using CalamityLegendsComeBack.Weapons.Malachite;
using CalamityLegendsComeBack.Weapons.PristineFury;
using CalamityLegendsComeBack.Weapons.SeasSearing;
using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityLegendsComeBack.Weapons.Vesuvius;
using CalamityLegendsComeBack.Weapons.YharimsCrystal;
using CalamityMod.Rarities;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack
{
    public class LegendarySupplyBox : ModItem, ILocalizedModType
    {
        //public override string Texture => "CalamityLegendsComeBack/传奇补给箱";
        public new string LocalizationCategory => "Items.Consumables";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.rare = ModContent.RarityType<BurnishedAuric>();
            Item.value = Item.sellPrice(gold: 1);
        }

        public override bool CanRightClick() => true;

        // The box is consumed only after the player confirms a weapon in the showcase.
        public override bool ConsumeItem(Player player) => false;

        public override void RightClick(Player player)
        {
            if (Main.netMode != NetmodeID.Server && player.whoAmI == Main.myPlayer)
                LegendarySupplyBoxSelectionUI.Open();
        }

        internal static int GetWeaponType(int selectionIndex)
        {
            int[] weapons = GetMainLegendaryWeapons();
            return selectionIndex >= 0 && selectionIndex < weapons.Length ? weapons[selectionIndex] : 0;
        }

        internal static int[] GetMainLegendaryWeapons()
        {
            return new[]
            {
                ModContent.ItemType<AegisBlade>(),
                ModContent.ItemType<NewLegendBrinyBaron>(),
                ModContent.ItemType<NewLegendBlossomFlux>(),
                ModContent.ItemType<NewLegendCosmicDischarge>(),
                ModContent.ItemType<GlacialEmbrace>(),
                ModContent.ItemType<NewLegendGaelsGreatsword>(),
                ModContent.ItemType<LeonidProgenitor>(),
                ModContent.ItemType<Malachite>(),
                ModContent.ItemType<NewLegendPristineFury>(),
                ModContent.ItemType<NewLegendSHPC>(),
                ModContent.ItemType<SeasSearing>(),
                ModContent.ItemType<NewVesuvius>(),
                ModContent.ItemType<NewLegendYharimsCrystal>(),
            };
        }

        internal static bool TryClaimWeapon(Player player, int selectionIndex)
        {
            int itemType = GetWeaponType(selectionIndex);
            if (itemType <= 0)
                return false;

            for (int slot = 0; slot < player.inventory.Length; slot++)
            {
                Item box = player.inventory[slot];
                if (box.type != ModContent.ItemType<LegendarySupplyBox>() || box.stack <= 0)
                    continue;

                box.stack--;
                if (box.stack <= 0)
                    box.TurnToAir();

                QuickSpawnNoPrefixItem(player, player.GetSource_FromThis(), itemType);
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, slot, box.prefix);

                return true;
            }

            return false;
        }

        private static void QuickSpawnNoPrefixItem(Player player, IEntitySource source, int itemType)
        {
            Item spawnedItem = new();
            spawnedItem.SetDefaults(itemType);
            spawnedItem.prefix = 0;
            player.QuickSpawnItem(source, spawnedItem, spawnedItem.stack);
        }
    }
}
