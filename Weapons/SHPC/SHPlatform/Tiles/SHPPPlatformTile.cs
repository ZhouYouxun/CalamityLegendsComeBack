using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace CalamityLegendsComeBack.Weapons.SHPC.SHPlatform.Tiles
{
    public class SHPPPlatformTile : ModTile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/SHPlatform/SHPPPlatformPlace";

        public override void SetStaticDefaults()
        {
            Main.tileLighted[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileSolidTop[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileTable[Type] = true;
            Main.tileLavaDeath[Type] = false;

            TileID.Sets.Platforms[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;

            TileObjectData.newTile.CoordinateHeights = new[] { 16 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.StyleMultiplier = 27;
            TileObjectData.newTile.StyleWrapLimit = 27;
            TileObjectData.newTile.UsesCustomCanPlace = false;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.LavaPlacement = LiquidPlacement.Allowed;
            TileObjectData.addTile(Type);

            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
            AddMapEntry(new Color(56, 164, 218));
            AdjTiles = new[] { (int)TileID.Platforms };
            HitSound = SoundID.Tink;
        }

        public override void PostSetDefaults()
        {
            Main.tileNoSunLight[Type] = false;
        }

        public override void FloorVisuals(Player player)
        {
            base.FloorVisuals(player);

            if (Main.dedServ || player.whoAmI != Main.myPlayer || player.velocity.Y != 0f || Math.Abs(player.velocity.X) < 0.25f || !Main.rand.NextBool(15))
                return;

            SpawnMatrixPulse(player);
        }

        private static void SpawnMatrixPulse(Player player)
        {
            int direction = Math.Sign(player.velocity.X);
            int tileY = (int)(player.Bottom.Y / 16f);
            int tileX = (int)(player.Center.X / 16f);
            Color dataColor = Color.Lerp(new Color(78, 255, 196), new Color(86, 196, 255), Main.rand.NextFloat());

            // A small packet races along connected platform segments instead of filling the screen with particles.
            for (int offset = 0; offset <= 4; offset++)
            {
                int pulseX = tileX + direction * offset;
                Tile tile = Framing.GetTileSafely(pulseX, tileY);
                if (!tile.HasTile || tile.TileType != ModContent.TileType<SHPPPlatformTile>())
                    break;

                Vector2 packetPosition = new Vector2(pulseX * 16f + 8f, tileY * 16f + 5f);
                Dust packet = Dust.NewDustPerfect(packetPosition, DustID.Electric, new Vector2(direction * (0.35f + offset * 0.08f), -0.06f), 145, dataColor, 0.32f);
                packet.noGravity = true;
                packet.fadeIn = 0.55f;

                if (offset == 0 || Main.rand.NextBool(3))
                {
                    Vector2 bitPosition = packetPosition + new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-7f, -2f));
                    Dust bit = Dust.NewDustPerfect(bitPosition, DustID.GemEmerald, new Vector2(direction * 0.12f, -Main.rand.NextFloat(0.12f, 0.3f)), 120, dataColor, 0.26f);
                    bit.noGravity = true;
                }
            }

            Vector2 codePosition = player.Bottom + new Vector2(Main.rand.NextFloat(-player.width * 0.25f, player.width * 0.25f), -5f);
            Dust code = Dust.NewDustPerfect(codePosition, DustID.Electric, new Vector2(-direction * 0.1f, -0.42f), 170, dataColor, 0.38f);
            code.noGravity = true;
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, DustID.Electric, 0f, 0f, 1, new Color(86, 196, 255), 0.85f);
            return false;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }

        public override IEnumerable<Item> GetItemDrops(int i, int j)
        {
            // 不返回任何 Item，也就是不掉落任何东西
            yield break;
        }
    }
}
