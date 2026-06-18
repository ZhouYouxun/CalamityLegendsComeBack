using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.YC
{
    internal enum YCAccessoryKind
    {
        Alpha,
        Chi,
        Delta,
        Gamma,
        Omega,
        Phi,
        Pi,
        Psi,
        Sigma,
        Theta,
        Upsilon
    }

    internal sealed class YCAccessoryPlayer : ModPlayer
    {
        public bool Alpha;
        public bool Chi;
        public bool Delta;
        public bool Gamma;
        public bool Omega;
        public bool Phi;
        public bool Pi;
        public bool Psi;
        public bool Sigma;
        public bool Theta;
        public bool Upsilon;

        public float WeaponDamageMultiplier => 1f + (Alpha ? 0.1f : 0f) + (Chi ? 0.06f : 0f) + (Omega ? 0.04f : 0f);
        public float SlaughterDamageMultiplier => 1f + (Delta ? 0.22f : 0f) + (Omega ? 0.08f : 0f);
        public float SlaughterCooldownMultiplier => Delta ? 0.85f : 1f;
        public int PrismVolleyBulletBonus => (Sigma ? 1 : 0) + (Psi ? 1 : 0);
        public float PrismProjectileSpeedMultiplier => 1f + (Gamma ? 0.12f : 0f) + (Upsilon ? 0.08f : 0f);
        public int AetherfluxInterval => Pi ? 64 : 86;
        public float HeavySalvoDamageMultiplier => Pi ? 1.18f : 1f;
        public float ManaCostMultiplier => Theta ? 0.82f : 1f;
        public int ExChargeGain => 1 + (Phi ? 1 : 0) + (Omega ? 1 : 0);
        public float ExCooldownMultiplier => Phi ? 0.82f : 1f;

        public override void ResetEffects()
        {
            Alpha = false;
            Chi = false;
            Delta = false;
            Gamma = false;
            Omega = false;
            Phi = false;
            Pi = false;
            Psi = false;
            Sigma = false;
            Theta = false;
            Upsilon = false;
        }

        public void Equip(YCAccessoryKind kind)
        {
            switch (kind)
            {
                case YCAccessoryKind.Alpha:
                    Alpha = true;
                    break;
                case YCAccessoryKind.Chi:
                    Chi = true;
                    break;
                case YCAccessoryKind.Delta:
                    Delta = true;
                    break;
                case YCAccessoryKind.Gamma:
                    Gamma = true;
                    break;
                case YCAccessoryKind.Omega:
                    Omega = true;
                    break;
                case YCAccessoryKind.Phi:
                    Phi = true;
                    break;
                case YCAccessoryKind.Pi:
                    Pi = true;
                    break;
                case YCAccessoryKind.Psi:
                    Psi = true;
                    break;
                case YCAccessoryKind.Sigma:
                    Sigma = true;
                    break;
                case YCAccessoryKind.Theta:
                    Theta = true;
                    break;
                case YCAccessoryKind.Upsilon:
                    Upsilon = true;
                    break;
            }
        }
    }

    internal abstract class YCAccessoryBase : ModItem
    {
        public override string LocalizationCategory => "Items.Accessories";
        public override string Texture
        {
            get
            {
                string typeName = GetType().Name;
                string category = typeName is
                    nameof(YharimsCrystalAlpha) or
                    nameof(YharimsCrystalDelta) or
                    nameof(YharimsCrystalSigma) or
                    nameof(YharimsCrystalGamma) or
                    nameof(YharimsCrystalOmega)
                    ? "Mainline"
                    : "Sideline";

                return $"CalamityLegendsComeBack/Accssory/YC/{category}/{typeName}/{typeName}";
            }
        }

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
