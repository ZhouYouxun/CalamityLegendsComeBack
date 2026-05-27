using CalamityLegendsComeBack.Weapons.Malachite;
using CalamityMod;
using CalamityMod.Items.Accessories;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.MC
{
    public sealed class PeacockScroll : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/MC/Test";

        public new string LocalizationCategory => "Items.Accessories";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 7);
            Item.rare = ItemRarityID.LightRed;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<MalachiteAccessoryPlayer>().PeacockScrollEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<SilencingSheath>()
                .AddIngredient(ItemID.SoulofNight, 5)
                .AddIngredient(ItemID.Emerald, 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    public sealed class PrecisionEmblem : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/MC/Test";

        public new string LocalizationCategory => "Items.Accessories";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 7);
            Item.rare = ItemRarityID.LightRed;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<MalachiteAccessoryPlayer>().PrecisionEmblemEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<RogueEmblem>()
                .AddIngredient(ItemID.SoulofLight, 5)
                .AddIngredient(ItemID.Emerald, 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    public sealed class MalachiteFeather : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/MC/Test";

        public new string LocalizationCategory => "Items.Accessories";

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
            player.GetModPlayer<MalachiteAccessoryPlayer>().MalachiteFeatherEquipped = true;
        }
    }

    public sealed class GaleAce : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/MC/Test";

        public new string LocalizationCategory => "Items.Accessories";

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
            player.GetModPlayer<MalachiteAccessoryPlayer>().GaleAceEquipped = true;
        }
    }

    public sealed class MalachiteAccessoryPlayer : ModPlayer
    {
        private const int FeatherGenerationDelay = 30;

        private int featherGenerationTimer;

        public bool PeacockScrollEquipped;
        public bool PrecisionEmblemEquipped;
        public bool MalachiteFeatherEquipped;
        public bool GaleAceEquipped;

        public bool HoldingMalachite => Player.HeldItem.type == ModContent.ItemType<Malachite>();

        public bool ShouldRegenerateFrenzyFan =>
            PeacockScrollEquipped &&
            NPC.downedPlantBoss &&
            HoldingMalachite &&
            Player.GetModPlayer<MalachitePlayer>().RightClickCooldown > 0;

        public float MalachiteProjectileVelocityMultiplier =>
            MalachiteFeatherEquipped && HoldingMalachite ? 1.1f : 1f;

        public override void ResetEffects()
        {
            PeacockScrollEquipped = false;
            PrecisionEmblemEquipped = false;
            MalachiteFeatherEquipped = false;
            GaleAceEquipped = false;
        }

        public override void PostUpdateEquips()
        {
            if (PeacockScrollEquipped)
            {
                Player.GetDamage<RogueDamageClass>() += 0.10f;

                if (HoldingMalachite && Player.GetModPlayer<MalachitePlayer>().RightClickCooldown > 0)
                    Player.GetAttackSpeed<RogueDamageClass>() += 0.15f;
            }

            if (PrecisionEmblemEquipped)
            {
                Player.GetDamage<RogueDamageClass>() += 0.05f;
                Player.GetCritChance<RogueDamageClass>() += 10f;
            }

            if (!HoldingMalachite)
                return;

            if (MalachiteFeatherEquipped)
            {
                Player.GetDamage<RogueDamageClass>() += 0.15f;
                Player.GetCritChance<RogueDamageClass>() += 5f;
            }

            if (GaleAceEquipped)
            {
                Player.GetDamage<RogueDamageClass>() += 0.15f;
                Player.GetCritChance<RogueDamageClass>() += 5f;
            }
        }

        public override void PostUpdate()
        {
            if (!MalachiteFeatherEquipped || !HoldingMalachite || Player.dead)
            {
                featherGenerationTimer = 0;
                return;
            }

            if (MalachiteKunai.HasStoredKunai(Player))
            {
                featherGenerationTimer = 0;
                return;
            }

            if (Player.whoAmI != Main.myPlayer)
                return;

            featherGenerationTimer++;
            if (featherGenerationTimer < FeatherGenerationDelay)
                return;

            featherGenerationTimer = 0;
            Item heldItem = Player.HeldItem;
            int damage = Player.GetWeaponDamage(heldItem);
            MalachiteKunai.SpawnSingleFrenzyKunai(
                Player,
                Player.GetSource_ItemUse(heldItem),
                damage,
                heldItem.knockBack);
        }
    }

    public sealed class MalachitePoisonGlobalNPC : GlobalNPC
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
                if (player.active && !player.dead && player.GetModPlayer<MalachiteAccessoryPlayer>().GaleAceEquipped)
                    return true;
            }

            return false;
        }
    }
}
