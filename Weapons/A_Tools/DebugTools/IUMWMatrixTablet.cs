using CalamityIUMWMode.Content.Projectiles;
using CalamityIUMWMode.Core.Systems;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityIUMWMode.Content.Items
{
    public class IUMWMatrixTablet : ModItem
    {
        public override string Texture => "CalamityIUMWMode/Assets/UI/IUMWIcon";

        public override void SetDefaults()
        {
            Item.width = 64;
            Item.height = 64;
            Item.rare = ItemRarityID.Cyan;
            Item.value = 0;
            Item.useTime = 12;
            Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<IUMWMatrixPanel>();
            Item.shootSpeed = 0f;
            Item.UseSound = null;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            return Main.myPlayer == player.whoAmI &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !player.mouseInterface &&
                !(Main.playerInventory && Main.HoverItem.type == Type);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
                return IUMWMatrixPanel.OpenOrClose(player, source);

            bool nextState = !IUMWWorldSystem.IUMWModeEnabled;
            IUMWWorldSystem.SetModeEnabled(nextState);

            string textKey = nextState ? "Mods.CalamityIUMWMode.UI.ModeEnabled" : "Mods.CalamityIUMWMode.UI.ModeDisabled";
            Color textColor = nextState ? new Color(88, 255, 211) : new Color(180, 190, 190);
            Main.NewText(Language.GetTextValue(textKey), textColor);
            SoundEngine.PlaySound((nextState ? SoundID.Item4 : SoundID.MenuClose) with { Volume = 0.65f, Pitch = nextState ? 0.16f : -0.05f }, player.Center);

            return false;
        }

        public override bool CanRightClick() => false;

        public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
        {
            string statusKey = IUMWWorldSystem.IUMWModeEnabled ? "Mods.CalamityIUMWMode.UI.StatusOn" : "Mods.CalamityIUMWMode.UI.StatusOff";
            tooltips.Add(new TooltipLine(Mod, "IUMWStatus", Language.GetTextValue(statusKey))
            {
                OverrideColor = IUMWWorldSystem.IUMWModeEnabled ? new Color(88, 255, 211) : new Color(180, 190, 190)
            });
        }

    }
}
