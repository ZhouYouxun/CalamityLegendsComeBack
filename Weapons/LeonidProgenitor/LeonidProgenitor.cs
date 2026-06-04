using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityMod;
using CalamityMod.Items.Weapons.Rogue;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    public class LeonidProgenitor : RogueWeapon, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/LeonidProgenitor";
        public new string LocalizationCategory => "Items.Weapons";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 48;
            Item.damage = 112;
            Item.knockBack = 4f;
            Item.useAnimation = Item.useTime = 20;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LeonidProgenitorBombshell>();
            Item.shootSpeed = 15.5f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.UseSound = SoundID.Item61;
            Item.value = Item.sellPrice(0, 10);
            Item.rare = ItemRarityID.Yellow;
        }

        public override float StealthDamageMultiplier => 1.2f;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            bool stealthStrike = player.Calamity().StealthStrikeAvailable();
            int[] effectIDs = LeonidMetalSelection.CaptureEffectIDs(player);

            int projectileID = Projectile.NewProjectile(
                source,
                position,
                velocity,
                type,
                damage,
                knockback,
                player.whoAmI,
                stealthStrike ? 1f : 0f,
                effectIDs[0],
                effectIDs[1]);

            if (projectileID.WithinBounds(Main.maxProjectiles))
            {
                Projectile projectile = Main.projectile[projectileID];
                projectile.Calamity().stealthStrike = stealthStrike;
            }

            return false;
        }

        public override void HoldItem(Player player)
        {
            LeonidSelectedMetal[] selection = LeonidMetalSelection.Scan(player);
            player.GetModPlayer<LeonidMetalPlayer>().UpdateHighlights(selection);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            LeonidSelectedMetal[] selection = LeonidMetalSelection.Scan(Main.LocalPlayer);
            string leftClick = this.GetLocalizedValue("LeftClick");
            string stealthClick = this.GetLocalizedValue("StealthLeftClick");
            string rightClick = this.GetLocalizedValue("RightClick");
            string currentMetalHeader = this.GetLocalizedValue("CurrentMetals");
            string lineA = BuildMetalLine(selection, 0);
            string lineB = BuildMetalLine(selection, 1);
            string legendaryBody = this.GetLocalizedValue("LegendaryText");
            string legendaryHint = this.GetLocalizedValue("LegendaryHint");
            string legendarySection = Main.keyState.PressingShift() ? legendaryBody : legendaryHint;

            string merged =
                leftClick + "\n" +
                stealthClick + "\n" +
                rightClick + "\n\n" +
                currentMetalHeader + "\n" +
                lineA + "\n" +
                lineB + "\n\n" +
                legendarySection;

            tooltips.FindAndReplace("[GFB]", merged);
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Item.DrawItemGlowmaskSingleFrame(spriteBatch, rotation, ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Rogue/LeonidProgenitorGlow").Value);
        }

        private string BuildMetalLine(LeonidSelectedMetal[] selection, int index)
        {
            if (selection == null || index < 0 || index >= selection.Length || !selection[index].IsValid)
                return this.GetLocalizedValue("EmptyMetalLine");

            int effectID = selection[index].Entry.EffectID;
            string metalName = this.GetLocalizedValue($"MetalName{effectID}");
            string metalDesc = this.GetLocalizedValue($"MetalDesc{effectID}");
            return string.Format(this.GetLocalizedValue("MetalLine"), metalName, metalDesc);
        }
    }
}
