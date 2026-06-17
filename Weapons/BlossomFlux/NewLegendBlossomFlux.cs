using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Passive.PaRevo;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityMod;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    public class NewLegendBlossomFlux : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        private BalanceBlossomFlux damageBalance = new();

        public override void SetDefaults()
        {
            Item.width = 78;
            Item.height = 78;
            Item.damage = BFBalanceTable.Get(BFStat.Breakthrough_Left_Damage, 0);
            Item.DamageType = DamageClass.Ranged;
            Item.useAnimation = 2;
            Item.useTime = 2;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.knockBack = 3.5f;
            Item.UseSound = SoundID.Item5;
            Item.shoot = ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>();
            Item.shootSpeed = 15f;
            Item.useAmmo = AmmoID.Arrow;
            Item.value = Item.sellPrice(0, 9);
            Item.rare = ItemRarityID.Pink;
        }

        public override Vector2? HoldoutOffset() => new Vector2(-10f, 0f);
        public override bool CanUseItem(Player player) => false;
        public override bool CanShoot(Player player) => false;

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            BFPassivePlayer passivePlayer = player.GetModPlayer<BFPassivePlayer>();
            passivePlayer.SetHoldingBlossomFlux();
            passivePlayer.SyncPassiveDisplay();
            BFEXPlayer exPlayer = player.GetModPlayer<BFEXPlayer>();
            exPlayer.SetHoldingBlossomFlux();

            if (player.Calamity().cooldowns.TryGetValue(BFEXCooldown.ID, out var exCooldown))
                exCooldown.timeLeft = exPlayer.EXValue;
            else
                player.AddCooldown(BFEXCooldown.ID, 0);

            bool exWeaponActive = player.ownedProjectileCounts[ModContent.ProjectileType<BFEXWeapon>()] > 0;

            if (Main.myPlayer == player.whoAmI &&
                KeybindSystem.LegendarySkill.JustPressed &&
                player.GetModPlayer<global::CalamityLegendsComeBack.Accssory.LegendaryEmblemPlayer>().EXAccessoryEquipped &&
                exPlayer.ConsumeAllEX() &&
                !exWeaponActive)
            {
                Vector2 direction = (player.Calamity().mouseWorld - player.Center).SafeNormalize(Vector2.UnitX * player.direction);
                Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    player.Center,
                    direction,
                    ModContent.ProjectileType<BFEXWeapon>(),
                    player.GetWeaponDamage(Item),
                    Item.knockBack,
                    player.whoAmI);

                exWeaponActive = true;
            }

            if (Main.myPlayer == player.whoAmI &&
                !exWeaponActive &&
                player.ownedProjectileCounts[ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>()] <= 0)
            {
                Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    player.Center,
                    Vector2.UnitX * player.direction,
                    ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>(),
                    player.GetWeaponDamage(Item),
                    Item.knockBack,
                    player.whoAmI);
            }
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base += damageBalance.GetLeftClickBaseDamage() - Item.damage;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            BlossomFluxChloroplastPresetType currentPreset = GetDisplayedPreset();
            BFPassivePlayer passivePlayer = Main.LocalPlayer.GetModPlayer<BFPassivePlayer>();
            string leftPresetText = this.GetLocalizedValue($"PresetLeft{(int)currentPreset}");
            string rightPresetText = this.GetLocalizedValue($"PresetRight{(int)currentPreset}");
            
            var formAssignedKeys = KeybindSystem.LegendaryWeaponFormSwitch.GetAssignedKeys();
            string formKeyText = formAssignedKeys.Count > 0 ? formAssignedKeys[0] : "Unbound";
            string formWheelHint = string.Format(this.GetLocalizedValue("BF_FormWheelHint"), formKeyText);

            string passiveStatus = !passivePlayer.PassiveUnlocked
                ? this.GetLocalizedValue("PassiveStateLocked")
                : passivePlayer.FinalStandActive
                    ? this.GetLocalizedValue("PassiveStateActive")
                    : passivePlayer.PassiveReady
                        ? this.GetLocalizedValue("PassiveStateReady")
                        : string.Format(this.GetLocalizedValue("PassiveStateCooldown"), passivePlayer.ChargeSeconds, passivePlayer.RequiredChargeSeconds);
            string passiveText = string.Format(this.GetLocalizedValue("BF_Passive"), passiveStatus);

            var assignedKeys = KeybindSystem.LegendarySkill.GetAssignedKeys();
            string keyText = assignedKeys.Count > 0 ? assignedKeys[0] : "Unbound";
            bool legendaryEmblemEquipped = Main.LocalPlayer.GetModPlayer<global::CalamityLegendsComeBack.Accssory.LegendaryEmblemPlayer>().EXAccessoryEquipped;
            string exHint = legendaryEmblemEquipped
                ? string.Format(this.GetLocalizedValue("BF_EXHint"), keyText)
                : this.GetLocalizedValue("BF_EXDisabledHint");

            string legendaryText = this.GetLocalizedValue("LegendaryText");
            string shiftHint = this.GetLocalizedValue("LegendaryHint");
            string legendarySection = Main.keyState.PressingShift() ? legendaryText : shiftHint;

            string merged =
                leftPresetText + "\n" +
                rightPresetText + "\n\n" +
                formWheelHint + "\n\n" +
                passiveText + "\n\n" +
                exHint + "\n";

            tooltips.FindAndReplace("[GFB]", merged);
            tooltips.Add(new TooltipLine(Mod, "BlossomFluxForestLegendaryText", legendarySection));
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = BlossomFluxTacticalTextures.GetWeaponTexture(GetDisplayedPreset());
            spriteBatch.Draw(texture, position, null, drawColor, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = BlossomFluxTacticalTextures.GetWeaponTexture(GetDisplayedPreset());
            spriteBatch.Draw(texture, Item.Center - Main.screenPosition, null, lightColor, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }

        private static BlossomFluxChloroplastPresetType GetDisplayedPreset()
        {
            if (Main.LocalPlayer?.active != true)
                return BlossomFluxChloroplastPresetType.Chlo_BRecov;

            return BlossomFluxTacticalTextures.GetLocalDisplayedPreset();
        }

        public override bool CanRightClick() => false;

        public override bool ConsumeItem(Player player) => false;
    }
}
