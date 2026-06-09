using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.YC
{
    internal enum YCAccessoryKind
    {
        Alpha,
        Beta,
        Sigma,
        Gamma,
        Lambda,
        Pi,
        Epsilon,
        Tau
    }

    internal sealed class YCAccessoryPlayer : ModPlayer
    {
        public bool Alpha;
        public bool Beta;
        public bool Sigma;
        public bool Gamma;
        public bool Lambda;
        public bool Pi;
        public bool Epsilon;
        public bool Tau;

        public float WeaponDamageMultiplier => 1f + (Alpha ? 0.1f : 0f) + (Tau ? 0.04f : 0f);
        public float SlaughterDamageMultiplier => 1f + (Beta ? 0.22f : 0f) + (Tau ? 0.08f : 0f);
        public float SlaughterCooldownMultiplier => Beta ? 0.85f : 1f;
        public int PrismVolleyBulletBonus => Sigma ? 1 : 0;
        public float PrismProjectileSpeedMultiplier => Gamma ? 1.12f : 1f;
        public int AetherfluxInterval => Pi ? 64 : 86;
        public float HeavySalvoDamageMultiplier => Pi ? 1.18f : 1f;
        public float ManaCostMultiplier => Epsilon ? 0.82f : 1f;
        public int ExChargeGain => 1 + (Lambda ? 1 : 0) + (Tau ? 1 : 0);
        public float ExCooldownMultiplier => Lambda ? 0.82f : 1f;

        public override void ResetEffects()
        {
            Alpha = false;
            Beta = false;
            Sigma = false;
            Gamma = false;
            Lambda = false;
            Pi = false;
            Epsilon = false;
            Tau = false;
        }

        public void Equip(YCAccessoryKind kind)
        {
            switch (kind)
            {
                case YCAccessoryKind.Alpha:
                    Alpha = true;
                    break;
                case YCAccessoryKind.Beta:
                    Beta = true;
                    break;
                case YCAccessoryKind.Sigma:
                    Sigma = true;
                    break;
                case YCAccessoryKind.Gamma:
                    Gamma = true;
                    break;
                case YCAccessoryKind.Lambda:
                    Lambda = true;
                    break;
                case YCAccessoryKind.Pi:
                    Pi = true;
                    break;
                case YCAccessoryKind.Epsilon:
                    Epsilon = true;
                    break;
                case YCAccessoryKind.Tau:
                    Tau = true;
                    break;
            }
        }
    }

    internal abstract class YCAccessoryBase : ModItem
    {
        public override string LocalizationCategory => "Items.Accessories";
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/NewLegendYharimsCrystal";

        protected abstract YCAccessoryKind Kind { get; }
        protected virtual int LunarFragmentType => ItemID.FragmentSolar;

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.value = Item.sellPrice(0, 8);
            Item.rare = ItemRarityID.Red;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<YCAccessoryPlayer>().Equip(Kind);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.CrystalShard, 20)
                .AddIngredient(LunarFragmentType, 8)
                .AddIngredient(ItemID.LunarBar, 4)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
