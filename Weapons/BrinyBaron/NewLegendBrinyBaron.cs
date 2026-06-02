using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using CalamityLegendsComeBack.Weapons.BrinyBaron.EXSkill;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash;
using CalamityLegendsComeBack.Weapons.BrinyBaron.Passive_QuickDash;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        private static bool CanUseQuickDash => Main.hardMode;
        private static bool HasDesignedSuperDashUnlock => NPC.downedFishron;

        public override void SetDefaults()
        {
            Item.width = 120;
            Item.height = 120;
            Item.damage = 120;
            Item.DamageType = DamageClass.Melee;

            Item.useAnimation = 60;
            Item.useTime = 60;
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
                if (HasActiveRightClickDash(player))
                    return false;

                Projectile activeLeftSwing = FindOwnedProjectile(player, ModContent.ProjectileType<BrinyBaron_LeftClick_Swing>());
                if (activeLeftSwing != null)
                {
                    if (IsLeftHeld(player))
                        return false;

                    activeLeftSwing.Kill();
                }

                BrinyBaronRightClickDashCooldownPlayer dashCooldown = player.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>();
                if (!dashCooldown.CanUseDash)
                    return false;

                Item.useTime = 20;
                Item.useAnimation = 20;
                Item.shoot = ModContent.ProjectileType<BrinyBaron_SkillDashTornado_BladeDash>();
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

                Item.useTime = Item.useAnimation = 25;
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

                Projectile.NewProjectile(
                    source,
                    player.MountedCenter,
                    shootVelocity,
                    ModContent.ProjectileType<BrinyBaron_SkillDashTornado_BladeDash>(),
                    rightClickDamage,
                    knockback,
                    player.whoAmI);

                player.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>().StartCooldown();
                return false;
            }

            int holdoutType = ModContent.ProjectileType<BrinyBaron_LeftClick_Swing>();
            return player.ownedProjectileCounts[holdoutType] <= 0;
        }

        public override bool CanShoot(Player player)
        {
            if (player.altFunctionUse != 2)
                return player.ownedProjectileCounts[ModContent.ProjectileType<BrinyBaron_LeftClick_Swing>()] <= 0 && !HasActiveRightClickDash(player);

            return !HasActiveRightClickDash(player);
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
        }

        public override void HoldItem(Player player)
        {
            BBEXPlayer tidePlayer = player.GetModPlayer<BBEXPlayer>();
            BBSuperDashCooldownPlayer superDashCooldown = player.GetModPlayer<BBSuperDashCooldownPlayer>();

            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            if (player.Calamity().cooldowns.TryGetValue(BBEXCoolDown.ID, out var cooldown))
                cooldown.timeLeft = tidePlayer.TideValue;
            else
                player.AddCooldown(BBEXCoolDown.ID, tidePlayer.TideValue);

            if (player.Calamity().cooldowns.TryGetValue(BBSuperDashCooldownHandler.ID, out var superDashVisualCooldown))
                superDashVisualCooldown.timeLeft = superDashCooldown.IsCoolingDown ? superDashCooldown.RemainingFrames : 0;
            else if (superDashCooldown.IsCoolingDown)
                player.AddCooldown(BBSuperDashCooldownHandler.ID, superDashCooldown.RemainingFrames);

            if (!superDashCooldown.CanUseSuperDash || !HasDesignedSuperDashUnlock || !tidePlayer.TideFull)
                return;

            if (!KeybindSystem.LegendarySkill.JustPressed)
                return;

            if (!player.GetModPlayer<global::CalamityLegendsComeBack.Accssory.LegendaryEmblemPlayer>().EXAccessoryEquipped)
                return;

            int target = BBSuperDashTargeting.FindBestTargetIndex(player, player.Center);
            if (target == -1)
            {
                if (player.whoAmI == Main.myPlayer)
                    CombatText.NewText(player.Hitbox, new Color(255, 80, 80), "大招被拒绝");

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
                Item.damage * 5,
                Item.knockBack,
                player.whoAmI,
                consumedTide);

            superDashCooldown.StartCooldown();
            tidePlayer.TideValue = 0;
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            BrinyBaronRightClickDashCooldownPlayer dashCooldown = Main.LocalPlayer.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>();

            Texture2D barBackground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barForeground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;
            Vector2 barOrigin = barBackground.Size() * 0.5f;
            Vector2 totalScale = Vector2.One * scale * 3.34f;

            if (dashCooldown.IsCoolingDown)
            {
                float progress = dashCooldown.CooldownCompletion;
                Rectangle frameCrop = new Rectangle(0, 0, (int)(barForeground.Width * progress), barForeground.Height);
                Vector2 drawPos = position + Vector2.UnitY * scale * (frame.Height - 20f);
                Color barColor = new Color(92, 210, 255);

                spriteBatch.Draw(barBackground, drawPos, null, barColor * 0.55f, 0f, barOrigin, totalScale, SpriteEffects.None, 0f);
                spriteBatch.Draw(barForeground, drawPos, frameCrop, barColor, 0f, barOrigin, totalScale, SpriteEffects.None, 0f);
            }
        }

        public override void AddRecipes()
        {
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BB_Balance.GetLeftClickBaseDamage();
        }

        private int GetCurrentRightClickDamage(Player player)
        {
            int baseDamage = BB_Balance.GetLeftClickBaseDamage();
            return (int)player.GetTotalDamage(Item.DamageType).ApplyTo(baseDamage * RightClickDamageMultiplier);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            BBEXPlayer tidePlayer = player.GetModPlayer<BBEXPlayer>();
            Dash_Trigger dashPlayer = player.GetModPlayer<Dash_Trigger>();

            string left = this.GetLocalizedValue("BB_Left");
            string tide = this.GetLocalizedValue("BB_Tide") + tidePlayer.TideValue;
            string right = this.GetLocalizedValue("BB_Right");
            string dash1 = this.GetLocalizedValue("Dash1_Unlock");
            string dash2 = this.GetLocalizedValue("Dash2_Lock");
            string dash3 = CanUseQuickDash
                ? this.GetLocalizedValue("Dash3_Unlock")
                : this.GetLocalizedValue("Dash3_Lock");
            string dash4 = HasDesignedSuperDashUnlock
                ? this.GetLocalizedValue("Dash4_Unlock")
                : this.GetLocalizedValue("Dash4_Lock");
            string passiveState = this.GetLocalizedValue(dashPlayer.DashEnabled ? "PassiveStateOn" : "PassiveStateOff");
            string passiveDevice = this.GetLocalizedValue(dashPlayer.EquippedDashDeviceLocalizationKey);
            string passive = string.Format(this.GetLocalizedValue("BB_Passive"), passiveState, passiveDevice);
            string final = this.GetLocalizedValue("BB_Final");
            string legendaryText = this.GetLocalizedValue("LegendaryText");
            string shiftHint = this.GetLocalizedValue("LegendaryHint");
            string legendarySection = Main.keyState.PressingShift() ? legendaryText : shiftHint;

            string finalText =
               left + "\n\n" +
               tide + "\n" +
               right + "\n" +
               dash1 + "\n" +
               dash2 + "\n" +
               dash3 + "\n" +
               passive + "\n" +
               dash4 + "\n\n" +
               final + "\n\n" +
               legendarySection + "\n";

            tooltips.FindAndReplace("[GFB]", finalText);
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
