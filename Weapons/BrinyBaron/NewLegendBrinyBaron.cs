using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using CalamityLegendsComeBack.Weapons.BrinyBaron.POWER;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillB_SpinDash;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillC_QuickDash;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash;
using CalamityMod;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    public class NewLegendBrinyBaron : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        private bool CanUseDashTornado => BB_Balance.CanUseShortDash;
        private bool CanUseSpinRush => BB_Balance.CanUseSpinRush;

        public override void SetDefaults()
        {
            Item.width = 120;
            Item.height = 120;
            Item.damage = 120;
            Item.DamageType = DamageClass.Melee;

            // =========================
            // 左键基础项：对齐 GrandDad 的思路
            // =========================
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
                if (!CanUseDashTornado && !CanUseSpinRush)
                    return false;

                BrinyBaronRightClickDashCooldownPlayer dashCooldown = player.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>();
                if (!dashCooldown.CanUseDash)
                    return false;

                BBEXPlayer tidePlayer = player.GetModPlayer<BBEXPlayer>();
                int spinRushType = ModContent.ProjectileType<BrinyBaron_SkillSpinRush_SpinBlade>();
                int dashType = ModContent.ProjectileType<BrinyBaron_SkillDashTornado_BladeDash>();

                Item.useTime = 24;
                Item.useAnimation = 24;
                Item.shoot = tidePlayer.TideFull ? spinRushType : dashType;

                Item.channel = Item.shoot == dashType;
                Item.channel = false;

                Item.noUseGraphic = true;
                Item.noMelee = true;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.shootSpeed = 0f;
                Item.UseSound = SoundID.Item39;
            }
            else
            {
                // =========================
                // 左键：可以参�?GrandDad【以及更多其他特殊剑�?一样的 Holdout 武器配置
                // =========================
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
            // =========================
            // 右键：保持原�?
            // =========================
            if (player.altFunctionUse == 2)
            {
                BrinyBaronRightClickDashCooldownPlayer dashCooldown = player.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>();

                if (type == ModContent.ProjectileType<BrinyBaron_SkillSpinRush_SpinBlade>())
                {
                    Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
                    dashCooldown.StartCooldown();
                    BBEXPlayer tidePlayer = player.GetModPlayer<BBEXPlayer>();
                    tidePlayer.TideValue = Math.Max(0, tidePlayer.TideValue - 1);
                    return false;
                }


                Vector2 shootVelocity = velocity.SafeNormalize(Vector2.UnitX * player.direction);
                Projectile.NewProjectile(source, position, shootVelocity, type, damage, knockback, player.whoAmI);
                dashCooldown.StartCooldown();
                return false;
            }


            // =========================
            // 左键：永远只允许存在一�?Holdout
            // =========================
            int holdoutType = ModContent.ProjectileType<BrinyBaron_LeftClick_Swing>();
            if (player.ownedProjectileCounts[holdoutType] > 0)
                return false;

            return true;
        }

        public override bool CanShoot(Player player)
        {
            if (player.altFunctionUse != 2)
                return player.ownedProjectileCounts[ModContent.ProjectileType<BrinyBaron_LeftClick_Swing>()] <= 0;

            int dashType = ModContent.ProjectileType<BrinyBaron_SkillDashTornado_BladeDash>();
            int spinRushType = ModContent.ProjectileType<BrinyBaron_SkillSpinRush_SpinBlade>();
            foreach (Projectile projectile in Main.projectile)
            {
                if (!projectile.active || projectile.owner != player.whoAmI)
                    continue;

                if (projectile.type == dashType || projectile.type == spinRushType)
                    return false;
            }

            return true;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
        }

        public override void HoldItem(Player player)
        {
            BBEXPlayer tidePlayer = player.GetModPlayer<BBEXPlayer>();
            BBSuperDashCooldownPlayer superDashCooldown = player.GetModPlayer<BBSuperDashCooldownPlayer>();
            BrinyBaronFocusModePlayer focusPlayer = player.GetModPlayer<BrinyBaronFocusModePlayer>();
            focusPlayer.SetHoldingBrinyBaron();

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

            if (!superDashCooldown.CanUseSuperDash)
                return;


            // ===== 大招释放 =====
            //if (KeybindSystem.LegendarySkill.JustPressed && NPC.downedFishron && tidePlayer.TideFull)
            if (KeybindSystem.LegendarySkill.JustPressed)
            {

                // 防止重复生成
                foreach (Projectile proj in Main.projectile)
                {
                    if (proj.active && proj.owner == player.whoAmI &&
                        proj.type == ModContent.ProjectileType<Z_BrinyBaron_SkillSuperCharge_SuperDash>())
                    {
                        return;
                    }
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
                    consumedTide
                );
                superDashCooldown.StartCooldown();
                tidePlayer.TideValue = 0;
            }
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

        #region 传奇属性成�?

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            bool[] downStages =
            {
                NPC.downedBoss1,
                NPC.downedBoss2,                
                DownedBossSystem.downedHiveMind || DownedBossSystem.downedPerforator,
                NPC.downedBoss3,
                DownedBossSystem.downedSlimeGod,
                Main.hardMode,
                NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3,
                DownedBossSystem.downedCalamitasClone,
                NPC.downedPlantBoss,
                NPC.downedFishron,
                NPC.downedAncientCultist,
                NPC.downedMoonlord,
                DownedBossSystem.downedProvidence,
                DownedBossSystem.downedSignus && DownedBossSystem.downedStormWeaver && DownedBossSystem.downedCeaselessVoid,
                DownedBossSystem.downedPolterghast,
                DownedBossSystem.downedDoG,
                DownedBossSystem.downedYharon,
                DownedBossSystem.downedExoMechs && DownedBossSystem.downedCalamitas,
                DownedBossSystem.downedPrimordialWyrm
            };

            int[] stageDamage =
            {
                15,
                24,
                31,
                33,
                34,
                42,
                79,
                108,
                121,
                144,
                210,
                465,
                472,
                489,
                505,
                1248,
                1351,
                16590,
                21469
            };

            int finalDamage = 10;

            for (int i = 0; i < downStages.Length; i++)
            {
                if (downStages[i])
                    finalDamage = stageDamage[i];
                else
                    break;
            }

            damage.Base = finalDamage;
        }

        #endregion



        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            BBEXPlayer tidePlayer = player.GetModPlayer<BBEXPlayer>();
            Dash_Trigger dashPlayer = player.GetModPlayer<Dash_Trigger>();

            // ===== 左键 =====
            string left = this.GetLocalizedValue("BB_Left");

            // ===== 潮汐 =====
            string tide = this.GetLocalizedValue("BB_Tide") + tidePlayer.TideValue;

            // ===== 右键通用 =====
            string right = this.GetLocalizedValue("BB_Right");

            // ===== Dash解锁 =====
            string dash1 = BB_Balance.CanUseShortDash
                ? this.GetLocalizedValue("Dash1_Unlock")
                : this.GetLocalizedValue("Dash1_Lock");

            string dash2 = BB_Balance.CanUseSpinRush
                ? this.GetLocalizedValue("Dash2_Unlock")
                : this.GetLocalizedValue("Dash2_Lock");

            string dash3 = BB_Balance.CanUseQuickDash
                ? this.GetLocalizedValue("Dash3_Unlock")
                : this.GetLocalizedValue("Dash3_Lock");

            string dash4 = BB_Balance.HasDesignedSuperDashUnlock
                ? this.GetLocalizedValue("Dash4_Unlock")
                : this.GetLocalizedValue("Dash4_Lock");

            string passiveState = this.GetLocalizedValue(dashPlayer.DashEnabled ? "PassiveStateOn" : "PassiveStateOff");
            string passive = string.Format(this.GetLocalizedValue("BB_Passive"), passiveState);

            // ===== 最终文�?=====
            string final = this.GetLocalizedValue("BB_Final");

            // ===== 传奇文本 =====
            string legendaryText = this.GetLocalizedValue("LegendaryText");

            // ===== Shift提示 =====
            string shiftHint = this.GetLocalizedValue("LegendaryHint");

            // ===== Shift切换 =====
            string legendarySection = shiftHint;
            if (Main.keyState.PressingShift())
                legendarySection = legendaryText;

            // ===== 拼接 =====
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




        // 背包里右键手动开关快刀�?
        public override bool CanRightClick()
        {
            return true;
        }

        public override void RightClick(Player player)
        {
            var dashPlayer = player.GetModPlayer<Dash_Trigger>();

            // 切换开�?
            dashPlayer.DashEnabled = !dashPlayer.DashEnabled;

            // 音效
            SoundEngine.PlaySound(SoundID.MenuTick, player.Center);
        }


        public override bool ConsumeItem(Player player)
        {
            return false; // ❗阻止右键把武器消耗掉
        }
    }
}

