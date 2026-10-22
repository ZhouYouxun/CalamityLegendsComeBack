using CalamityMod;
using CalamityMod.Items.Weapons.Melee;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.LowEndMurasama;

// Use Calamity's item defaults and MurasamaSlash, including its full three-swing combo.
public sealed class LowEndMurasama : Murasama
{
    public override string Texture => "CalamityMod/Items/Weapons/Melee/Murasama";
    public override LocalizedText DisplayName => Language.GetText("Mods.CalamityLegendsComeBack.Items.LowEndMurasama.DisplayName");
    public override LocalizedText Tooltip => Language.GetText("Mods.CalamityLegendsComeBack.Items.LowEndMurasama.Tooltip");

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.damage = 25;
        Item.rare = ItemRarityID.Orange;
        Item.value = Item.sellPrice(gold: 1);
    }

    // The replica is always unsheathed, but cannot create overlapping holdouts.
    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frameI,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
        spriteBatch.Draw(texture, position, Item.GetCurrentFrame(ref frame, ref frameCounter, 2, 13),
            Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
        return false;
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
        ref float rotation, ref float scale, int whoAmI)
    {
        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
        spriteBatch.Draw(texture, Item.position - Main.screenPosition,
            Item.GetCurrentFrame(ref frame, ref frameCounter, 2, 13), lightColor,
            0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        return false;
    }

    public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
        float rotation, float scale, int whoAmI)
    {
        Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Melee/MurasamaGlow").Value;
        spriteBatch.Draw(glow, Item.position - Main.screenPosition,
            Item.GetCurrentFrame(ref frame, ref frameCounter, 2, 13, false), Color.White,
            0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
    }
}
