using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace CalamityLegendsComeBack.Weapons.A_Tools.SHPlatform.Tiles
{
    public class SHPCPlatformTile : ModTile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Tools/SHPlatform/SHPCPlatformPlace";

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

            if (Main.dedServ || player.velocity.Y != 0f || player.velocity.X > -0.25f && player.velocity.X < 0.25f || !Main.rand.NextBool(12))
                return;

            Vector2 dustPosition = player.Bottom + new Vector2(Main.rand.NextFloat(-player.width * 0.35f, player.width * 0.35f), -4f);
            Dust dust = Dust.NewDustDirect(dustPosition, 2, 2, DustID.Electric, player.velocity.X * -0.04f, -0.25f, 180, new Color(86, 196, 255), 0.45f);
            dust.noGravity = true;
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
    }
}
