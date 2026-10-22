using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Items.Accessories;
using CalamityMod.DataStructures;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory;

public sealed class ShrineModel : ModItem, IDyeableShaderRenderer
{
    // A separate renderer avoids sharing mutable ownership with the registered Rover Drive.
    private static readonly RoverDrive ShieldRenderer = new();
    public int OwnerPlayer { get; set; } = -1;
    public float RenderDepth => IDyeableShaderRenderer.RoverDriveDepth;

    public bool ShouldDrawDyeableShader
    {
        get
        {
            ShieldRenderer.OwnerPlayer = OwnerPlayer;
            return ShieldRenderer.ShouldDrawDyeableShader;
        }
    }

    public void DrawDyeableShader(SpriteBatch spriteBatch)
    {
        ShieldRenderer.OwnerPlayer = OwnerPlayer;
        ShieldRenderer.DrawDyeableShader(spriteBatch);
    }

    public override string Texture => "CalamityLegendsComeBack/Accssory/ShrineModel";

    // Other languages resolve Calamity's names; Chinese lists verified names explicitly
    // because Calamity may fall back to English when its translation pack is absent.
    public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(
        CalamityUtils.GetItemName<LuxorsGift>().Value,
        CalamityUtils.GetItemName<FrozenCube>().Value,
        CalamityUtils.GetItemName<RoverDrive>().Value,
        CalamityUtils.GetItemName<TrinketofChi>().Value,
        CalamityUtils.GetItemName<GladiatorsLocket>().Value,
        CalamityUtils.GetItemName<UnstableGraniteCore>().Value,
        CalamityUtils.GetItemName<FungalSymbiote>().Value,
        CalamityUtils.GetItemName<ThePointer>().Value,
        CalamityUtils.GetItemName<SpringStool>().Value,
        CalamityUtils.GetItemName<ViciousTonic>().Value,
        CalamityUtils.GetItemName<UnholyTonic>().Value,
        CalamityUtils.GetItemName<BlackGlassBand>().Value,
        CalamityUtils.GetItemName<IlmerisSpark>().Value,
        CalamityUtils.GetItemName<AncientFossil>().Value);

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.FindAndReplaceAll("[POINTER]", AbilityKey<ThePointer>());
        tooltips.FindAndReplaceAll("[STOOL]", AbilityKey<SpringStool>());
    }

    private static string AbilityKey<T>() where T : ModItem
    {
        if (Main.dedServ)
            return "";

        var key = ModContent.GetInstance<T>().Item.GetDynamicModHotkey();
        if (Language.ActiveCulture.Name.StartsWith("zh"))
        {
            if (key == null)
                return "装备后分配饰品能力快捷键";
            var keys = key.GetAssignedKeysOrEmpty(Terraria.GameInput.PlayerInput.CurrentInputMode);
            return keys.Count > 0 ? string.Join(" / ", keys).Replace("NumPad", "小键盘") : "未绑定饰品能力快捷键";
        }
        return key?.TooltipHotkeyString() ?? CalamityUtils.GetTextValue("Misc.HotkeyNotEquipped");
    }

    public override void SetDefaults()
    {
        Item.width = 40;
        Item.height = 40;
        Item.accessory = true;
        Item.rare = ItemRarityID.Orange;
        Item.value = Item.sellPrice(gold: 25);
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.GetModPlayer<ShrineModelPlayer>().Equipped = true;
        player.GetModPlayer<ShrineModelPlayer>().HideVisual = hideVisual;
        // Delegate whole effects, including projectiles, team effects and mushroom growth.
        ModContent.GetInstance<LuxorsGift>().UpdateAccessory(player, hideVisual);
        ModContent.GetInstance<FrozenCube>().UpdateAccessory(player, hideVisual);
        ModContent.GetInstance<RoverDrive>().UpdateAccessory(player, hideVisual);
        ModContent.GetInstance<TrinketofChi>().UpdateAccessory(player, hideVisual);
        ModContent.GetInstance<GladiatorsLocket>().UpdateAccessory(player, hideVisual);
        ModContent.GetInstance<UnstableGraniteCore>().UpdateAccessory(player, hideVisual);
        ModContent.GetInstance<FungalSymbiote>().UpdateAccessory(player, hideVisual);
        // These native items look up their own item type in Calamity's rental queue.
        // Reserve separate keys so aiming and the stool jump never trigger together.
        if (player.whoAmI == Main.myPlayer)
        {
            CalamityKeybinds.KeybindRentalQueue.Add(ModContent.ItemType<ThePointer>());
            CalamityKeybinds.KeybindRentalQueue.Add(ModContent.ItemType<SpringStool>());
        }
        ModContent.GetInstance<ThePointer>().UpdateAccessory(player, hideVisual);
        ModContent.GetInstance<SpringStool>().UpdateAccessory(player, hideVisual);
        ModContent.GetInstance<ViciousTonic>().UpdateAccessory(player, hideVisual);
        ModContent.GetInstance<UnholyTonic>().UpdateAccessory(player, hideVisual);
        ModContent.GetInstance<BlackGlassBand>().UpdateAccessory(player, hideVisual);
        ModContent.GetInstance<IlmerisSpark>().UpdateAccessory(player, hideVisual);
        ModContent.GetInstance<AncientFossil>().UpdateAccessory(player, hideVisual);
    }

    public override void AddRecipes() => CreateRecipe()
        .AddIngredient<LuxorsGift>()
        .AddIngredient<FrozenCube>()
        .AddIngredient<RoverDrive>()
        .AddIngredient<TrinketofChi>()
        .AddIngredient<GladiatorsLocket>()
        .AddIngredient<UnstableGraniteCore>()
        .AddIngredient<FungalSymbiote>()
        .AddIngredient<ThePointer>()
        .AddIngredient<SpringStool>()
        .AddIngredient<ViciousTonic>()
        .AddIngredient<UnholyTonic>()
        .AddIngredient<BlackGlassBand>()
        .AddIngredient<IlmerisSpark>()
        .AddIngredient<AncientFossil>()
        .AddTile(TileID.TinkerersWorkbench)
        .Register();
}
