using CalamityLegendsComeBack.Accssory.BB;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken;
using CalamityLegendsComeBack.Weapons.BrinyBaron.TideValue;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash;
using CalamityLegendsComeBack.Weapons.BrinyBaron.Passive_QuickDash;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash;
using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    public class NewLegendBrinyBaron : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        private const float RightClickDamageMultiplier = 1.08f;
        private static bool CanUseQuickDash => true;
        private static bool HasDesignedSuperDashUnlock => NPC.downedFishron;

        public override void SetDefaults()
        {
            Item.width = 120;
            Item.height = 120;
            Item.damage = BB_Balance.GetInitialLeftClickBaseDamage();
            Item.DamageType = DamageClass.Melee;

            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useTurn = true;
            Item.knockBack = 6f;
            Item.autoReuse = true;
            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<BrinyBaron_LeftClick_Swing>();
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shootSpeed = 0f;
            Item.rare = ItemRarityID.Pink;
            Item.UseSound = null;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                BBRightClickMode rightClickMode = player.GetModPlayer<BBAccessoryPlayer>().RightClickMode;
                bool usesDashBody = rightClickMode == BBRightClickMode.DefaultShuriken || rightClickMode == BBRightClickMode.LostGarment;
                bool usesAccessoryCooldown = usesDashBody || rightClickMode == BBRightClickMode.VortexPortal;

                if (usesDashBody && HasActiveRightClickDash(player))
                    return false;

                Projectile activeLeftSwing = FindOwnedProjectile(player, ModContent.ProjectileType<BrinyBaron_LeftClick_Swing>());
                if (activeLeftSwing != null)
                {
                    if (IsLeftHeld(player))
                        return false;

                    activeLeftSwing.Kill();
                }

                if (usesAccessoryCooldown && !player.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>().CanUseDash)
                    return false;

                bool bubbleShield = rightClickMode == BBRightClickMode.CeruleanShield;
                if (bubbleShield && player.ownedProjectileCounts[ModContent.ProjectileType<BrinyBaron_BubbleShield>()] <= 0 &&
                    !player.GetModPlayer<BrinyBaronBubbleShieldPlayer>().CanSpawnBubble)
                    return false;

                Item.useTime = bubbleShield ? 10 : 18;
                Item.useAnimation = bubbleShield ? 10 : 18;
                Item.shoot = rightClickMode switch
                {
                    BBRightClickMode.CeruleanShield => ModContent.ProjectileType<BrinyBaron_BubbleShield>(),
                    BBRightClickMode.LostGarment or BBRightClickMode.DefaultShuriken => ModContent.ProjectileType<BrinyBaron_SkillDashTornado_BladeDash>(),
                    BBRightClickMode.VortexPortal => ModContent.ProjectileType<BrinyBaron_SkillSlashDash_SlashDash>(),
                };
                Item.channel = false;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.shootSpeed = 0f;
                Item.UseSound = SoundID.Item39;
            }
            else
            {
                if (HasActiveRightClickDash(player))
                    return false;

                Item.useTime = Item.useAnimation = 28;
                Item.channel = true;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.shoot = ModContent.ProjectileType<BrinyBaron_LeftClick_Swing>();
                Item.shootSpeed = 0f;
                Item.UseSound = null;
            }

            return base.CanUseItem(player);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                CancelLeftClickHoldout(player);

                Vector2 shootVelocity = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
                int rightClickDamage = GetCurrentRightClickDamage(player);
                BBRightClickMode rightClickMode = player.GetModPlayer<BBAccessoryPlayer>().RightClickMode;

                if (rightClickMode == BBRightClickMode.VortexPortal)
                {
                    ExecuteVortexPortal(player, source, shootVelocity, rightClickDamage, knockback);
                    player.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>().StartCooldown(360);
                    return false;
                }

                if (rightClickMode == BBRightClickMode.CeruleanShield)
                {
                    ToggleBubbleShield(player, source);
                    return false;
                }

                if (rightClickMode == BBRightClickMode.LostGarment || rightClickMode == BBRightClickMode.DefaultShuriken)
                {
                    Projectile.NewProjectile(
                        source,
                        player.MountedCenter,
                        shootVelocity,
                        ModContent.ProjectileType<BrinyBaron_SkillDashTornado_BladeDash>(),
                        rightClickDamage,
                        knockback,
                        player.whoAmI,
                        rightClickMode == BBRightClickMode.DefaultShuriken ? 2f : 0f);

                    player.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>().StartCooldown();
                    return false;
                }

                return false;
            }

            int holdoutType = ModContent.ProjectileType<BrinyBaron_LeftClick_Swing>();
            return player.ownedProjectileCounts[holdoutType] <= 0;
        }

        public override bool CanShoot(Player player)
        {
            if (player.altFunctionUse != 2)
                return player.ownedProjectileCounts[ModContent.ProjectileType<BrinyBaron_LeftClick_Swing>()] <= 0 && !HasActiveRightClickDash(player);

            BBRightClickMode rightClickMode = player.GetModPlayer<BBAccessoryPlayer>().RightClickMode;
            bool usesDashBody = rightClickMode == BBRightClickMode.DefaultShuriken || rightClickMode == BBRightClickMode.LostGarment;
            bool usesAccessoryCooldown = usesDashBody || rightClickMode == BBRightClickMode.VortexPortal;
            return (!usesDashBody || !HasActiveRightClickDash(player)) &&
                   (!usesAccessoryCooldown || player.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>().CanUseDash);
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
        }

        public override void HoldItem(Player player)
        {
            BBTideValuePlayer tidePlayer = player.GetModPlayer<BBTideValuePlayer>();
            BBSuperDashCooldownPlayer superDashCooldown = player.GetModPlayer<BBSuperDashCooldownPlayer>();

            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            if (player.Calamity().cooldowns.TryGetValue(BBTideValueCooldown.ID, out var cooldown))
            {
                cooldown.duration = BBTideValuePlayer.TideChargeMax;
                cooldown.timeLeft = Math.Max(1, tidePlayer.TideChargeValue);
            }
            else
            {
                player.AddCooldown(BBTideValueCooldown.ID, BBTideValuePlayer.TideChargeMax).timeLeft = Math.Max(1, tidePlayer.TideChargeValue);
            }

            int superDashVisualValue = Math.Max(1, (int)(superDashCooldown.CooldownDuration * superDashCooldown.CooldownCompletion));
            if (player.Calamity().cooldowns.TryGetValue(BBSuperDashCooldownHandler.ID, out var superDashVisualCooldown))
            {
                superDashVisualCooldown.duration = superDashCooldown.CooldownDuration;
                superDashVisualCooldown.timeLeft = superDashVisualValue;
            }
            else
            {
                player.AddCooldown(BBSuperDashCooldownHandler.ID, superDashCooldown.CooldownDuration).timeLeft = superDashVisualValue;
            }

            bool exUnlocked = player.GetModPlayer<global::CalamityLegendsComeBack.Accssory.LegendaryEmblemPlayer>().EXAccessoryEquipped;
            if (!superDashCooldown.CanUseSuperDash || !exUnlocked || !tidePlayer.TideFull || !tidePlayer.TideChargeFull)
                return;

            if (!KeybindSystem.LegendarySkill.JustPressed)
                return;

            int target = BBSuperDashTargeting.FindBestTargetIndex(player, player.Center);
            if (target == -1)
            {
                if (player.whoAmI == Main.myPlayer)
                    CombatText.NewText(player.Hitbox, new Color(255, 80, 80), this.GetLocalizedValue("UltimateOutOfRange"));

                return;
            }

            foreach (Projectile proj in Main.projectile)
            {
                if (proj.active && proj.owner == player.whoAmI &&
                    proj.type == ModContent.ProjectileType<Z_BrinyBaron_SkillSuperCharge_SuperDash>())
                    return;
            }

            Vector2 dir = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitX * player.direction);
            int consumedTide = Math.Max(1, tidePlayer.TideValue);

            Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                player.Center,
                dir,
                ModContent.ProjectileType<Z_BrinyBaron_SkillSuperCharge_SuperDash>(),
                BB_Balance.GetLeftClickBaseDamage() * 5,
                Item.knockBack,
                player.whoAmI,
                consumedTide);

            superDashCooldown.StartCooldown();
            tidePlayer.ResetTideCharge();
            tidePlayer.TideValue = 0;
        }

        public override void AddRecipes()
        {
        }

        private void ExecuteVortexPortal(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 direction, int damage, float knockback)
        {
            Vector2 destination = Main.MouseWorld;
            Vector2 topLeft = destination - player.Size * 0.5f;
            if (Collision.SolidCollision(topLeft, player.width, player.height))
                destination = player.Center + direction * 240f;

            player.Center = destination;
            player.velocity = Vector2.Zero;
            player.ChangeDir(direction.X >= 0f ? 1 : -1);

            SoundEngine.PlaySound(SoundID.Item6 with { Volume = 0.75f, Pitch = 0.18f }, player.Center);
            Projectile.NewProjectile(
                source,
                player.MountedCenter,
                direction,
                ModContent.ProjectileType<BrinyBaron_SkillSlashDash_SlashDash>(),
                damage,
                knockback,
                player.whoAmI,
                0f,
                player.direction);
        }

        private static void ToggleBubbleShield(Player player, IEntitySource source)
        {
            int bubbleType = ModContent.ProjectileType<BrinyBaron_BubbleShield>();
            foreach (Projectile projectile in Main.projectile)
            {
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == bubbleType)
                {
                    projectile.Kill();
                    return;
                }
            }

            BrinyBaronBubbleShieldPlayer bubblePlayer = player.GetModPlayer<BrinyBaronBubbleShieldPlayer>();
            if (!bubblePlayer.CanSpawnBubble || Main.myPlayer != player.whoAmI)
                return;

            Projectile.NewProjectile(source, player.Center, Vector2.Zero, bubbleType, 0, 0f, player.whoAmI);
            bubblePlayer.StartCooldown();
            SoundEngine.PlaySound(SoundID.Item54 with { Volume = 0.65f, Pitch = 0.25f }, player.Center);
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base += BB_Balance.GetLeftClickBaseDamage() - Item.damage;
            damage *= player.GetModPlayer<BBTideValuePlayer>().TideDamageMultiplier;
        }

        private int GetCurrentRightClickDamage(Player player)
        {
            int baseDamage = BB_Balance.GetLeftClickBaseDamage();
            float scaledDamage = player.GetTotalDamage(Item.DamageType).ApplyTo(baseDamage * RightClickDamageMultiplier);
            return Math.Max(1, (int)(scaledDamage * player.GetModPlayer<BBTideValuePlayer>().TideDamageMultiplier));
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            BBTideValuePlayer tidePlayer = player.GetModPlayer<BBTideValuePlayer>();
            Dash_Trigger dashPlayer = player.GetModPlayer<Dash_Trigger>();

            int growthStage = BB_Balance.GetGrowthStage();
            string left = this.GetLocalizedValue("BB_Left_" + growthStage);
            if (growthStage >= 2)
            {
                left = left.Trim() + "\n" + this.GetLocalizedValue("BB_Right_Spin_Unlocked").Trim();
            }

            BBRightClickMode rightClickMode = player.GetModPlayer<BBAccessoryPlayer>().RightClickMode;
            string rightDesc = rightClickMode switch
            {
                BBRightClickMode.LostGarment => this.GetLocalizedValue("BB_Right_LostGarment"),
                BBRightClickMode.CeruleanShield => this.GetLocalizedValue("BB_Right_CeruleanShield"),
                BBRightClickMode.VortexPortal => this.GetLocalizedValue("BB_Right_VortexPortal"),
                _ => this.GetLocalizedValue("BB_Right_DefaultShuriken"),
            };

            string rightSection = rightDesc.Trim();

            bool hasDashAcc = rightClickMode != BBRightClickMode.DefaultShuriken;
            string tideDesc = hasDashAcc
                ? this.GetLocalizedValue("BB_Tide_Desc_HasAccessory")
                : this.GetLocalizedValue("BB_Tide_Desc_NoAccessory");
            string tide = this.GetLocalizedValue("BB_Tide") + tidePlayer.TideValue + "\n" + tideDesc;

            string passiveState = this.GetLocalizedValue(dashPlayer.DashEnabled ? "PassiveStateOn" : "PassiveStateOff");
            string passiveDevice = this.GetLocalizedValue(dashPlayer.EquippedDashDeviceLocalizationKey);
            string passive = string.Format(this.GetLocalizedValue("BB_Passive"), passiveState, passiveDevice);

            bool exUnlocked = player.GetModPlayer<global::CalamityLegendsComeBack.Accssory.LegendaryEmblemPlayer>().EXAccessoryEquipped;
            string final = exUnlocked
                ? this.GetLocalizedValue("BB_Final")
                : this.GetLocalizedValue("Dash4_Lock");

            string legendaryText = this.GetLocalizedValue("LegendaryText");
            string shiftHint = this.GetLocalizedValue("LegendaryHint");
            bool shiftPressed = Main.keyState.PressingShift();
            string legendarySection = shiftPressed ? legendaryText : shiftHint;

            if (shiftPressed)
            {
                tooltips.RemoveAll(t => t.Text == "[GFB]");
            }
            else
            {
                string finalText =
                   left.TrimEnd('\r', '\n') + "\n" +
                   rightSection.TrimEnd('\r', '\n') + "\n" +
                   tide.TrimEnd('\r', '\n') + "\n" +
                   passive.TrimEnd('\r', '\n') + "\n" +
                   final.TrimEnd('\r', '\n') + "\n";

                tooltips.FindAndReplace("[GFB]", finalText);
            }

            tooltips.Add(new TooltipLine(Mod, "BrinyBaronOceanLegendaryText", legendarySection));
        }

        public override bool CanRightClick()
        {
            return true;
        }

        public override void RightClick(Player player)
        {
            Dash_Trigger dashPlayer = player.GetModPlayer<Dash_Trigger>();
            dashPlayer.DashEnabled = !dashPlayer.DashEnabled;
            SoundEngine.PlaySound(SoundID.MenuTick, player.Center);
        }

        public override bool ConsumeItem(Player player)
        {
            return false;
        }

        private static bool IsLeftHeld(Player player)
        {
            return player.channel &&
                   (Main.myPlayer != player.whoAmI || Main.mouseLeft) &&
                   !Main.mapFullscreen &&
                   !Main.blockMouse;
        }

        private static bool HasActiveRightClickDash(Player player)
        {
            return FindOwnedProjectile(player, ModContent.ProjectileType<BrinyBaron_SkillDashTornado_BladeDash>()) != null;
        }

        private static void CancelLeftClickHoldout(Player player)
        {
            int holdoutType = ModContent.ProjectileType<BrinyBaron_LeftClick_Swing>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == holdoutType)
                    projectile.Kill();
            }
        }

        private static Projectile FindOwnedProjectile(Player player, int projectileType)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == projectileType)
                    return projectile;
            }

            return null;
        }
    }

}
