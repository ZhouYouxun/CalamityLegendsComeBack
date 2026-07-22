using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Accssory.YC;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.LeftGeneral;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.RightGeneral;
using CalamityMod;
using CalamityMod.Cooldowns;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal
{
    public class NewLegendYharimsCrystal : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        private readonly BalanceYharimsCrystal balance = new();

        private static int LeftHoldoutType => ModContent.ProjectileType<YC_LeftBladeSwing>();
        private static int RightHoldoutType => ModContent.ProjectileType<YC_RightCrystalHoldout>();
        private static int ThrownBladeType => ModContent.ProjectileType<YC_ThrownBlade>();
        private static int VipType => ModContent.ProjectileType<YC_EX_VIP>();

        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 44;
            Item.damage = BalanceYharimsCrystal.GetInitialLeftClickBaseDamage();
            Item.DamageType = DamageClass.Magic;
            Item.mana = 6;
            Item.useTime = 45;
            Item.useAnimation = 45;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 6f;
            Item.channel = true;
            Item.autoReuse = true;
            Item.shoot = LeftHoldoutType;
            Item.shootSpeed = 0f;
            Item.UseSound = null;
            Item.value = Item.sellPrice(0, 20);
            Item.rare = ItemRarityID.Red;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            bool rightClick = player.altFunctionUse == 2;
            if (HasInputConflict(player, rightClick) || HasActivePrimaryAttack(player))
                return false;

            if (!rightClick && player.GetModPlayer<YharimsCrystalStatePlayer>().LeftClickCooldown > 0)
                return false;

            if (rightClick)
            {
                if (player.GetModPlayer<YharimsCrystalStatePlayer>().RightClickCooldown > 0)
                    return false;

                Item.useTime = 20;
                Item.useAnimation = 20;
                Item.channel = true;
                Item.shoot = RightHoldoutType;
                Item.UseSound = null;
            }
            else
            {
                Item.useTime = 45;
                Item.useAnimation = 45;
                Item.channel = true;
                Item.shoot = LeftHoldoutType;
                Item.UseSound = null;
            }

            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.shootSpeed = 0f;
            return base.CanUseItem(player);
        }

        public override bool CanShoot(Player player)
        {
            bool rightClick = player.altFunctionUse == 2;
            if (HasInputConflict(player, rightClick) || HasActivePrimaryAttack(player))
                return false;

            if (rightClick)
            {
                if (player.GetModPlayer<YharimsCrystalStatePlayer>().RightClickCooldown > 0)
                    return false;
            }

            return true;
        }

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;
            player.Calamity().rightClickListener = true;

            YCEXPlayer exPlayer = player.GetModPlayer<YCEXPlayer>();
            SyncCooldownDisplay(player, exPlayer);

            if (Main.myPlayer != player.whoAmI ||
                !KeybindSystem.LegendarySkill.JustPressed ||
                !player.GetModPlayer<global::CalamityLegendsComeBack.Accssory.LegendaryEmblemPlayer>().EXAccessoryEquipped ||
                !exPlayer.CanActivateUltimate ||
                player.ownedProjectileCounts[VipType] > 0)
            {
                return;
            }

            YharimsCrystalStatePlayer state = player.GetModPlayer<YharimsCrystalStatePlayer>();
            Vector2 aimDirection = (GetMouseWorld(player) - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            float ultimateMultiplier = balance.GetUltimateDamageMultiplier();
            int damage = state.LastWeapon == YCWeaponForm.Blade
                ? GetScaledDamage(player, (int)(balance.GetLeftClickBaseDamage() * ultimateMultiplier))
                : GetScaledDamage(player, (int)(balance.GetRightClickBaseDamage() * ultimateMultiplier));

            int ultimate = Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                player.Center,
                aimDirection,
                VipType,
                damage,
                Item.knockBack,
                player.whoAmI,
                (float)state.LastWeapon);

            if (Main.projectile.IndexInRange(ultimate))
                Main.projectile[ultimate].CritChance = player.GetWeaponCrit(Item);

            SoundEngine.PlaySound(SoundID.Item119 with { Volume = 0.85f, Pitch = -0.1f }, player.Center);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 aimDirection = (GetMouseWorld(player) - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            bool rightClick = player.altFunctionUse == 2;
            if (HasInputConflict(player, rightClick) || HasActivePrimaryAttack(player))
                return false;

            int projectileType = rightClick ? RightHoldoutType : LeftHoldoutType;
            int projectileDamage = rightClick ? GetScaledDamage(player, balance.GetRightClickBaseDamage()) : damage;

            int projectileIndex = Projectile.NewProjectile(
                source,
                player.MountedCenter,
                aimDirection,
                projectileType,
                projectileDamage,
                knockback,
                player.whoAmI);

            if (Main.projectile.IndexInRange(projectileIndex))
                Main.projectile[projectileIndex].CritChance = player.GetWeaponCrit(Item);

            return false;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base += balance.GetLeftClickBaseDamage() - Item.damage;
            damage *= GetConvertedMeleeBonus(player);
            damage *= player.GetModPlayer<YCAccessoryPlayer>().WeaponDamageMultiplier;
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            mult *= player.GetModPlayer<YCAccessoryPlayer>().ManaCostMultiplier;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string bodyInfo = this.GetLocalizedValue("TooltipBody");
            bool shiftPressed = Main.keyState.PressingShift();
            if (shiftPressed)
                tooltips.RemoveAll(t => t.Text == "[GFB]");
            else
                tooltips.FindAndReplace("[GFB]", bodyInfo);

            string legendarySection = shiftPressed ? this.GetLocalizedValue("LegendaryText") : this.GetLocalizedValue("LegendaryHint");
            tooltips.Add(new TooltipLine(Mod, "YharimsCrystalGoldenTechLegendaryText", legendarySection));
        }

        internal int GetScaledDamage(Player player, int baseDamage)
        {
            float magicScaled = player.GetTotalDamage(DamageClass.Magic).ApplyTo(Math.Max(1, baseDamage));
            magicScaled *= GetConvertedMeleeBonus(player);
            magicScaled *= player.GetModPlayer<YCAccessoryPlayer>().WeaponDamageMultiplier;
            return Math.Max(1, (int)magicScaled);
        }

        private static float GetConvertedMeleeBonus(Player player)
        {
            float meleeMultiplier = player.GetTotalDamage(DamageClass.Melee).ApplyTo(1f);
            float convertedBonus = Math.Max(0f, meleeMultiplier - 1f) * 0.33f;
            return 1f + convertedBonus;
        }

        internal static Vector2 GetMouseWorld(Player player)
        {
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }

        private static bool HasActivePrimaryAttack(Player player)
        {
            return player.ownedProjectileCounts[LeftHoldoutType] > 0 ||
                player.ownedProjectileCounts[RightHoldoutType] > 0 ||
                player.ownedProjectileCounts[ThrownBladeType] > 0;
        }

        private static bool HasInputConflict(Player player, bool rightClick)
        {
            // The button held first owns the attack session. Combination attacks stay
            // inside that session instead of spawning the other weapon form.
            return rightClick ? IsPrimaryInputHeld(player) : IsAlternateInputHeld(player);
        }

        private static bool IsPrimaryInputHeld(Player player)
        {
            if (Main.myPlayer != player.whoAmI)
                return player.controlUseItem;

            return Main.mouseLeft &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !player.mouseInterface;
        }

        private static bool IsAlternateInputHeld(Player player)
        {
            if (Main.myPlayer != player.whoAmI)
                return player.controlUseTile;

            return (Main.mouseRight || player.Calamity().mouseRight) &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !player.mouseInterface;
        }

        private static void SyncCooldownDisplay(Player player, YCEXPlayer exPlayer)
        {
            if (player.Calamity().cooldowns.TryGetValue(YCEXCoolDown.ID, out var cooldown))
                cooldown.timeLeft = Math.Max(1, exPlayer.DisplayRawValue);
            else
                player.AddCooldown(YCEXCoolDown.ID, Math.Max(1, exPlayer.DisplayRawValue));
        }
    }
}
