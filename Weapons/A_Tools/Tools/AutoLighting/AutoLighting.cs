using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Tools.Tools.AutoLighting
{
    public class AutoLighting : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Tools/Tools/AutoLighting/AutoLighting";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.value = Item.sellPrice(gold: 1);
            Item.rare = ItemRarityID.Orange;
            Item.maxStack = 1;
        }

        public override void UpdateInventory(Player player)
        {
            if (Item.favorited)
                player.GetModPlayer<AutoLightingPlayer>().AutoLightingActive = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Torch, 50)
                .AddIngredient(ItemID.Glowstick, 10)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }

    public class AutoLightingPlayer : ModPlayer
    {
        public bool AutoLightingActive;
        private int lightingCooldown;

        public override void ResetEffects() => AutoLightingActive = false;

        public override void PostUpdate()
        {
            if (!AutoLightingActive || Main.dedServ || Main.myPlayer != Player.whoAmI) return;

            if (lightingCooldown > 0) { lightingCooldown--; return; }

            Point pt = Player.Center.ToTileCoordinates();
            Color lightColor = Lighting.GetColor(pt.X, pt.Y);
            float brightness = (lightColor.R + lightColor.G + lightColor.B) / (3f * 255f);

            const float DarkThreshold = 0.22f;
            if (brightness >= DarkThreshold) return;

            bool inWater = Player.wet && !Player.lavaWet && !Player.honeyWet;

            if (inWater)
            {
                if (TryThrowGlowstick())
                    lightingCooldown = 900; // 15s
            }
            else
            {
                if (TryPlaceTorch())
                    lightingCooldown = 600; // 10s
            }
        }

        private bool TryPlaceTorch()
        {
            // Find topmost torch in inventory
            int torchSlot = -1;
            int torchTileType = TileID.Torches;

            for (int i = 0; i < 50; i++)
            {
                Item inv = Player.inventory[i];
                if (inv.IsAir || inv.createTile < TileID.Dirt || inv.stack <= 0) continue;
                if (TileID.Sets.Torch[inv.createTile])
                {
                    torchSlot = i;
                    torchTileType = inv.createTile;
                    break;
                }
            }

            // Default to regular torch if none found
            if (torchSlot < 0)
            {
                for (int i = 0; i < 50; i++)
                {
                    if (Player.inventory[i].type == ItemID.Torch && Player.inventory[i].stack > 0)
                    {
                        torchSlot = i;
                        torchTileType = TileID.Torches;
                        break;
                    }
                }
            }

            if (torchSlot < 0) return false;

            int px = (int)(Player.Center.X / 16f);
            int py = (int)(Player.Bottom.Y / 16f);

            (int dx, int dy)[] candidates = { (0, 0), (-1, 0), (1, 0), (0, -1), (-1, -1), (1, -1), (0, 1), (-2, 0), (2, 0) };

            foreach (var (dx, dy) in candidates)
            {
                int tx = px + dx;
                int ty = py + dy;
                if (!WorldGen.InWorld(tx, ty, 3)) continue;
                if (Framing.GetTileSafely(tx, ty).HasTile) continue;

                WorldGen.PlaceTile(tx, ty, torchTileType, mute: false, forced: false, plr: Player.whoAmI);

                if (Framing.GetTileSafely(tx, ty).HasTile)
                {
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendTileSquare(-1, tx, ty, 1);
                    ConsumeSlot(torchSlot);
                    return true;
                }
            }
            return false;
        }

        private bool TryThrowGlowstick()
        {
            int slot = -1;
            for (int i = 0; i < 50; i++)
            {
                if (Player.inventory[i].type == ItemID.Glowstick && Player.inventory[i].stack > 0)
                { slot = i; break; }
            }
            if (slot < 0) return false;

            Projectile.NewProjectile(
                Player.GetSource_ItemUse(Player.inventory[slot]),
                Player.Center,
                new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), -3f),
                ProjectileID.Glowstick,
                0, 0f, Player.whoAmI);

            ConsumeSlot(slot);
            return true;
        }

        private void ConsumeSlot(int slot)
        {
            Player.inventory[slot].stack--;
            if (Player.inventory[slot].stack <= 0)
                Player.inventory[slot] = new Item();
        }
    }
}
