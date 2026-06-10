using CalamityLegendsComeBack.Weapons.Malachite;
using CalamityMod;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.MC.MalachiteFeather
{
    public sealed class MalachiteFeather : ModItem
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
            player.GetModPlayer<MalachiteFeatherPlayer>().MalachiteFeatherEquipped = true;
        }
    }

    public sealed class MalachiteFeatherPlayer : ModPlayer
    {
        private const int FeatherGenerationDelay = 30;

        private int featherGenerationTimer;

        public bool MalachiteFeatherEquipped;

        public bool HoldingMalachite => Player.HeldItem.type == ModContent.ItemType<Weapons.Malachite.Malachite>();

        public float MalachiteProjectileVelocityMultiplier =>
            MalachiteFeatherEquipped && HoldingMalachite ? 1.1f : 1f;

        public override void ResetEffects()
        {
            MalachiteFeatherEquipped = false;
        }

        public override void PostUpdateEquips()
        {
            if (HoldingMalachite && MalachiteFeatherEquipped)
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
                Player.GetSource_FromThis(),
                damage,
                heldItem.knockBack);
        }
    }
}
