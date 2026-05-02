using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Passive;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityMod;
using CalamityMod.World;
using Microsoft.Xna.Framework;
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
            Item.damage = 12;
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
            damage.Base = damageBalance.GetLeftClickBaseDamage();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            BlossomFluxChloroplastPresetType currentPreset = GetDisplayedPreset();
            BFRightUIPlayer rightUIPlayer = Main.LocalPlayer.GetModPlayer<BFRightUIPlayer>();
            BFPassivePlayer passivePlayer = Main.LocalPlayer.GetModPlayer<BFPassivePlayer>();
            string leftText = this.GetLocalizedValue("BF_Left");
            string rightText = this.GetLocalizedValue("BF_Right");
            string presetName = this.GetLocalizedValue($"PresetName{(int)currentPreset}");
            string presetText = string.Format(this.GetLocalizedValue("BF_Preset"), presetName);
            string leftPresetText = this.GetLocalizedValue($"PresetLeft{(int)currentPreset}");
            string rightPresetText = this.GetLocalizedValue($"PresetRight{(int)currentPreset}");
            string passiveStatus = !passivePlayer.PassiveUnlocked
                ? this.GetLocalizedValue("PassiveStateLocked")
                : passivePlayer.FinalStandActive
                    ? this.GetLocalizedValue("PassiveStateActive")
                    : passivePlayer.PassiveReady
                        ? this.GetLocalizedValue("PassiveStateReady")
                        : string.Format(this.GetLocalizedValue("PassiveStateCooldown"), passivePlayer.RemainingSeconds);
            string passiveText = string.Format(this.GetLocalizedValue("BF_Passive"), passiveStatus);
            string unlockRecovery = this.GetLocalizedValue(rightUIPlayer.IsPresetUnlocked(BlossomFluxChloroplastPresetType.Chlo_BRecov) ? "PresetUnlock1" : "PresetLock1");
            string unlockRecon = this.GetLocalizedValue(rightUIPlayer.IsPresetUnlocked(BlossomFluxChloroplastPresetType.Chlo_CDetec) ? "PresetUnlock2" : "PresetLock2");
            string unlockBombard = this.GetLocalizedValue(rightUIPlayer.IsPresetUnlocked(BlossomFluxChloroplastPresetType.Chlo_DBomb) ? "PresetUnlock3" : "PresetLock3");
            string unlockPlague = this.GetLocalizedValue(rightUIPlayer.IsPresetUnlocked(BlossomFluxChloroplastPresetType.Chlo_EPlague) ? "PresetUnlock4" : "PresetLock4");
            string finalText = this.GetLocalizedValue("BF_Final");
            string exText = this.GetLocalizedValue("BF_EX");
            string legendaryText = this.GetLocalizedValue("LegendaryText");
            string shiftHint = this.GetLocalizedValue("LegendaryHint");
            string legendarySection = Main.keyState.PressingShift() ? legendaryText : shiftHint;

            string merged =
                leftText + "\n" +
                presetText + "\n" +
                leftPresetText + "\n\n" +
                rightText + "\n" +
                rightPresetText + "\n\n" +
                passiveText + "\n" +
                unlockRecovery + "\n" +
                unlockRecon + "\n" +
                unlockBombard + "\n" +
                unlockPlague + "\n\n" +
                finalText + "\n" +
                exText + "\n\n" +
                legendarySection + "\n";

            tooltips.FindAndReplace("[GFB]", merged);
        }

        private static BlossomFluxChloroplastPresetType GetDisplayedPreset()
        {
            if (Main.LocalPlayer?.active != true)
                return BlossomFluxChloroplastPresetType.Chlo_ABreak;

            return Main.LocalPlayer.GetModPlayer<BFRightUIPlayer>().CurrentPreset;
        }

        public override bool CanRightClick() => false;

        public override bool ConsumeItem(Player player) => false;
    }
}
