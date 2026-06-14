using CalamityLegendsComeBack.Weapons.Vesuvius.EXSkill;
using CalamityLegendsComeBack.Weapons.Vesuvius.Passive;
using CalamityLegendsComeBack.Weapons.Vesuvius.RightClick;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius
{
    public class NewVesuvius : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        public override void SetStaticDefaults()
        {
            Item.staff[Type] = true;
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 62;
            Item.height = 62;
            Item.damage = 115;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 9;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.autoReuse = false;
            Item.knockBack = 4f;
            Item.shoot = ModContent.ProjectileType<VesuviusLeftHoldout>();
            Item.shootSpeed = 18f;
            Item.UseSound = null;
            Item.value = CalamityGlobalItem.RarityYellowBuyPrice;
            Item.rare = ModContent.RarityType<BurnishedAuric>();
        }

        public override Vector2? HoldoutOffset() => new Vector2(-12f, -6f);

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (HasActiveUltimate(player))
                return false;

            VesuviusPassivePlayer vPlayer = player.GetModPlayer<VesuviusPassivePlayer>();

            if (player.altFunctionUse == 2)
            {
                Item.useTime = 28;
                Item.useAnimation = 28;
                Item.channel = false;
                Item.noUseGraphic = true;
                Item.shoot = ModContent.ProjectileType<VesuviusFaultJavelin>();
                Item.shootSpeed = 25f;
                Item.UseSound = SoundID.Item1 with { Volume = 0.78f, Pitch = -0.22f };
                return player.ownedProjectileCounts[ModContent.ProjectileType<VesuviusFaultJavelin>()] < 3;
            }

            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.channel = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<VesuviusLeftHoldout>();
            Item.shootSpeed = 1f;
            Item.UseSound = null;

            return vPlayer.LeftClickCooldown <= 0 &&
                   player.ownedProjectileCounts[ModContent.ProjectileType<VesuviusLeftHoldout>()] <= 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 aimDirection = (player.Calamity().mouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);

            if (player.altFunctionUse == 2)
            {
                Projectile.NewProjectile(
                    source,
                    player.MountedCenter + aimDirection * 26f,
                    aimDirection * Item.shootSpeed,
                    ModContent.ProjectileType<VesuviusFaultJavelin>(),
                    VesuviusProgression.GetRightDamage(damage),
                    knockback,
                    player.whoAmI,
                    VesuviusProgression.GetMaxStage());

                player.GetModPlayer<VesuviusEXPlayer>().GainEX(1);
                return false;
            }

            Projectile.NewProjectile(
                source,
                player.MountedCenter,
                aimDirection,
                ModContent.ProjectileType<VesuviusLeftHoldout>(),
                damage,
                knockback,
                player.whoAmI);

            return false;
        }

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            VesuviusPassivePlayer passivePlayer = player.GetModPlayer<VesuviusPassivePlayer>();
            passivePlayer.SetHoldingVesuvius();

            VesuviusEXPlayer exPlayer = player.GetModPlayer<VesuviusEXPlayer>();
            exPlayer.SetHoldingVesuvius();
            SyncCooldownDisplay(player, exPlayer);

            if (Main.myPlayer != player.whoAmI)
                return;

            if (KeybindSystem.LegendarySkill.JustPressed &&
                player.GetModPlayer<global::CalamityLegendsComeBack.Accssory.LegendaryEmblemPlayer>().EXAccessoryEquipped &&
                exPlayer.ConsumeAllEX() &&
                !HasActiveUltimate(player))
            {
                KillOwnedVesuviusAttacks(player);

                Projectile.NewProjectile(
                    Item.GetSource_FromThis(),
                    player.MountedCenter,
                    -Vector2.UnitY,
                    ModContent.ProjectileType<VesuviusEXWeapon>(),
                    player.GetWeaponDamage(Item),
                    Item.knockBack,
                    player.whoAmI);

                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1f, Pitch = -0.35f }, player.Center);
            }
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base += Item.damage * (0.12f * (VesuviusProgression.GetMaxStage() - 1));
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string keyText = KeybindSystem.LegendarySkill.GetAssignedKeys().FirstOrDefault() ?? "Unbound";
            bool legendaryEmblemEquipped = Main.LocalPlayer.GetModPlayer<global::CalamityLegendsComeBack.Accssory.LegendaryEmblemPlayer>().EXAccessoryEquipped;
            string exText = legendaryEmblemEquipped
                ? string.Format(this.GetLocalizedValue("EXHint"), keyText)
                : this.GetLocalizedValue("EXDisabledHint");

            string finalText =
                this.GetLocalizedValue("LeftClick") + "\n\n" +
                this.GetLocalizedValue($"ChargeStage{VesuviusProgression.GetMaxStage()}") + "\n\n" +
                this.GetLocalizedValue("RightClick") + "\n\n" +
                this.GetLocalizedValue("Passive") + "\n\n" +
                exText + "\n\n" +
                this.GetLocalizedValue("Final") + "\n";

            tooltips.FindAndReplace("[GFB]", finalText);
            string legendarySection = Main.keyState.PressingShift() ? this.GetLocalizedValue("LegendaryText") : this.GetLocalizedValue("LegendaryHint");
            tooltips.Add(new TooltipLine(Mod, "VesuviusVolcanoLegendaryText", legendarySection));
        }

        private static void SyncCooldownDisplay(Player player, VesuviusEXPlayer exPlayer)
        {
            if (player.Calamity().cooldowns.TryGetValue(VesuviusEXCooldown.ID, out var cooldown))
                cooldown.timeLeft = exPlayer.EXValue;
            else
                player.AddCooldown(VesuviusEXCooldown.ID, 0);
        }

        private static bool HasActiveUltimate(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<VesuviusEXWeapon>()] > 0;
        }

        private static void KillOwnedVesuviusAttacks(Player player)
        {
            int leftHoldoutType = ModContent.ProjectileType<VesuviusLeftHoldout>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == leftHoldoutType)
                    projectile.Kill();
            }
        }
    }
}
