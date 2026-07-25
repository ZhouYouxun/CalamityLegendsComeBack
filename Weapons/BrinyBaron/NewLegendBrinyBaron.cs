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
                bool usesDashBody = rightClickMode == BBRightClickMode.DefaultShuriken;
                bool usesAccessoryCooldown = usesDashBody || rightClickMode == BBRightClickMode.VortexEye;

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

                Item.useTime = 18;
                Item.useAnimation = 18;
                Item.shoot = rightClickMode switch
                {
                    BBRightClickMode.VortexEye => ModContent.ProjectileType<BrinyBaron_VortexEyeDash>(),
                    _ => ModContent.ProjectileType<BrinyBaron_SkillDashTornado_BladeDash>()
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

                if (rightClickMode == BBRightClickMode.VortexEye)
                {
                    Projectile.NewProjectile(
                        source,
                        player.MountedCenter,
                        CardinalizeDirection(shootVelocity, player.direction),
                        ModContent.ProjectileType<BrinyBaron_VortexEyeDash>(),
                        rightClickDamage,
                        knockback,
                        player.whoAmI);
                    player.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>().StartCooldown(360);
                    return false;
                }

                if (rightClickMode == BBRightClickMode.DefaultShuriken)
                {
                    Projectile.NewProjectile(
                        source,
                        player.MountedCenter,
                        shootVelocity,
                        ModContent.ProjectileType<BrinyBaron_SkillDashTornado_BladeDash>(),
                        rightClickDamage,
                        knockback,
                        player.whoAmI,
                        2f);

                    player.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>().StartCooldown(
                        player.GetModPlayer<BBAccessoryPlayer>().AbyssalBastionEquipped ? 120 : BrinyBaronRightClickDashCooldownPlayer.DashCooldown);
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
            bool usesDashBody = rightClickMode == BBRightClickMode.DefaultShuriken;
            bool usesAccessoryCooldown = usesDashBody || rightClickMode == BBRightClickMode.VortexEye;
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

            if (Main.myPlayer == player.whoAmI &&
                player.GetModPlayer<BBAccessoryPlayer>().RightClickMode == BBRightClickMode.VortexEye &&
                player.ownedProjectileCounts[ModContent.ProjectileType<BrinyBaron_VortexEyePortalPreview>()] == 0)
            {
                Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    Main.MouseWorld,
                    Vector2.Zero,
                    ModContent.ProjectileType<BrinyBaron_VortexEyePortalPreview>(),
                    0,
                    0f,
                    player.whoAmI);
            }

            if (player.Calamity().cooldowns.TryGetValue(BBTideValueCooldown.ID, out var cooldown))
            {
                cooldown.duration = Math.Max(1, tidePlayer.CurrentTideMax);
                cooldown.timeLeft = Math.Max(1, tidePlayer.TideValue);
            }
            else
            {
                player.AddCooldown(BBTideValueCooldown.ID, Math.Max(1, tidePlayer.CurrentTideMax)).timeLeft = Math.Max(1, tidePlayer.TideValue);
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
                (int)player.GetTotalDamage(Item.DamageType).ApplyTo(BB_Balance.GetLeftClickBaseDamage() * BB_Balance.GetUltimateDamageMultiplier()),
                Item.knockBack,
                player.whoAmI,
                consumedTide);

            superDashCooldown.StartCooldown();
            tidePlayer.ResetTideCharge();
        }

        public override void AddRecipes()
        {
        }

        private void ExecuteVortexEyeDash(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 direction, int damage, float knockback)
        {
            direction = Math.Abs(direction.X) >= Math.Abs(direction.Y)
                ? new Vector2(Math.Sign(direction.X == 0f ? player.direction : direction.X), 0f)
                : new Vector2(0f, Math.Sign(direction.Y == 0f ? -1f : direction.Y));
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
            string left = this.GetLocalizedValue("BB_Left_" + Math.Min(growthStage, 4));
            if (growthStage >= 2)
            {
                left = left.Trim() + "\n" + this.GetLocalizedValue("BB_Right_Spin_Unlocked").Trim();
            }

            BBRightClickMode rightClickMode = player.GetModPlayer<BBAccessoryPlayer>().RightClickMode;
            string rightDesc = rightClickMode switch
            {
                BBRightClickMode.VortexEye => this.GetLocalizedValue("BB_Right_VortexEye"),
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
            // On the owning client, mouse input is stable across auto-use rollovers while
            // Player.channel can momentarily clear. Keep the remote-player fallback on the
            // synchronized channel state.
            bool leftInputHeld = Main.myPlayer == player.whoAmI ? Main.mouseLeft : player.channel;
            return leftInputHeld &&
                   !Main.mapFullscreen &&
                   !Main.blockMouse;
        }

        private static Vector2 CardinalizeDirection(Vector2 direction, int fallbackDirection)
        {
            return Math.Abs(direction.X) >= Math.Abs(direction.Y)
                ? new Vector2(Math.Sign(direction.X == 0f ? fallbackDirection : direction.X), 0f)
                : new Vector2(0f, Math.Sign(direction.Y == 0f ? -1f : direction.Y));
        }

        public override float UseTimeMultiplier(Player player)
        {
            var acc = player.GetModPlayer<BBAccessoryPlayer>();
            if (acc.OceanHormoneEquipped && player.Calamity().adrenalineModeActive)
            {
                return Math.Max(1, Item.useTime - 6) / (float)Item.useTime;
            }
            return base.UseTimeMultiplier(player);
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
