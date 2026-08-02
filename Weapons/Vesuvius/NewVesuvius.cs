using CalamityLegendsComeBack.Weapons.Vesuvius.EXSkill;
using CalamityLegendsComeBack.Weapons.Vesuvius.Passive;
using CalamityLegendsComeBack.Weapons.Vesuvius.RightClick;
using CalamityLegendsComeBack.Weapons.Vesuvius.RightClick.Javelin;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using System;
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
        private bool suppressRightClickUntilRelease;

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
                Item.useTime = 30;
                Item.useAnimation = 30;
                Item.channel = true;
                Item.noUseGraphic = true;
                Item.shoot = ModContent.ProjectileType<VesuviusSuperFlameHoldout>();
                Item.shootSpeed = 1f;
                Item.UseSound = null;
                return false;
            }

            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.channel = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<VesuviusLeftHoldout>();
            Item.shootSpeed = 1f;
            Item.UseSound = null;

            return vPlayer.LeftClickCooldown <= 0 &&
                   player.ownedProjectileCounts[ModContent.ProjectileType<VesuviusSuperFlameHoldout>()] <= 0 &&
                   player.ownedProjectileCounts[ModContent.ProjectileType<VesuviusLeftHoldout>()] <= 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 aimDirection = (player.Calamity().mouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);

            if (player.altFunctionUse == 2)
                return false;

            bool empoweredShot = player.GetModPlayer<VesuviusPassivePlayer>().TryConsumeEmpoweredLeft();

            Projectile.NewProjectile(
                source,
                player.MountedCenter,
                aimDirection,
                ModContent.ProjectileType<VesuviusLeftHoldout>(),
                damage,
                knockback,
                player.whoAmI,
                empoweredShot ? 1f : 0f);

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

            bool rightHeld = player.Calamity().mouseRight;
            if (!rightHeld)
                suppressRightClickUntilRelease = false;
            else if (!suppressRightClickUntilRelease && CanStartRightClickHoldout(player))
            {
                // 超级火焰之后必须先松开一次右键，下一次按下才会消费陨石窗口。
                // 这样长按喷火不会在同一次输入里顺手把陨石也砸下来。
                if (passivePlayer.MeteorFollowupTimer > 0)
                {
                    if (player.CheckMana(Item, Item.mana, false, false) && passivePlayer.TryConsumeMeteorFollowup())
                    {
                        player.CheckMana(Item, Item.mana, true, false);
                        SpawnFollowupMeteor(player);
                    }
                }
                else
                    StartRightClickHoldout(player);

                suppressRightClickUntilRelease = true;
            }

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
                    (int)(player.GetWeaponDamage(Item) * VesuviusProgression.GetUltimateDamageMultiplier()),
                    Item.knockBack,
                    player.whoAmI);

                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1f, Pitch = -0.35f }, player.Center);
            }
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base += Item.damage * (0.12f * (VesuviusProgression.GetWorldPowerStage() - 1));
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string keyText = KeybindSystem.LegendarySkill.GetAssignedKeys().FirstOrDefault() ?? "Unbound";
            bool legendaryEmblemEquipped = Main.LocalPlayer.GetModPlayer<global::CalamityLegendsComeBack.Accssory.LegendaryEmblemPlayer>().EXAccessoryEquipped;
            string exText = legendaryEmblemEquipped
                ? string.Format(this.GetLocalizedValue("EXHint"), keyText)
                : this.GetLocalizedValue("EXDisabledHint");

            string finalText =
                this.GetLocalizedValue("LeftClick").TrimEnd('\n') + "\n" +
                this.GetLocalizedValue($"ChargeStage{VesuviusProgression.GetMaxStage()}") + "\n" +
                this.GetLocalizedValue("RightClick").TrimEnd('\n') + "\n" +
                this.GetLocalizedValue("Passive").TrimEnd('\n') + "\n" +
                exText.TrimEnd('\n') + "\n" +
                this.GetLocalizedValue("Final") + "\n";

            bool shiftPressed = Main.keyState.PressingShift();
            if (shiftPressed)
                tooltips.RemoveAll(t => t.Text == "[GFB]");
            else
                tooltips.FindAndReplace("[GFB]", finalText);
            string legendarySection = shiftPressed ? this.GetLocalizedValue("LegendaryText") : this.GetLocalizedValue("LegendaryHint");
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

        private bool CanStartRightClickHoldout(Player player)
        {
            return player.whoAmI == Main.myPlayer &&
                   player.Calamity().mouseRight &&
                   !HasActiveUltimate(player) &&
                   !Main.mapFullscreen &&
                   !Main.blockMouse &&
                   !player.mouseInterface &&
                   !player.noItems &&
                   !player.CCed &&
                   player.ownedProjectileCounts[ModContent.ProjectileType<VesuviusSuperFlameHoldout>()] <= 0;
        }

        private void StartRightClickHoldout(Player player)
        {
            int leftHoldoutType = ModContent.ProjectileType<VesuviusLeftHoldout>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == leftHoldoutType)
                    projectile.Kill();
            }

            Vector2 aimDirection = (player.Calamity().mouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            int projIndex = Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                player.MountedCenter,
                aimDirection,
                ModContent.ProjectileType<VesuviusSuperFlameHoldout>(),
                Math.Max(1, (int)(player.GetWeaponDamage(Item) * 2.5f)),
                Item.knockBack,
                player.whoAmI,
                VesuviusProgression.GetWorldPowerStage());

            if (Main.projectile.IndexInRange(projIndex))
                Main.projectile[projIndex].CritChance = player.GetWeaponCrit(Item);
        }

        private void SpawnFollowupMeteor(Player player)
        {
            int leftHoldoutType = ModContent.ProjectileType<VesuviusLeftHoldout>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner == player.whoAmI && projectile.type == leftHoldoutType)
                    projectile.Kill();
            }

            Vector2 target = player.Calamity().mouseWorld;
            Vector2 spawnPosition = target + new Vector2(Main.rand.NextFloat(-120f, 120f), -620f);
            Vector2 velocity = (target - spawnPosition).SafeNormalize(Vector2.UnitY) * 20f;
            int projIndex = Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                spawnPosition,
                velocity,
                ModContent.ProjectileType<VesuviusFollowupMeteor>(),
                Math.Max(1, (int)(player.GetWeaponDamage(Item) * 1.85f)),
                Item.knockBack * 1.6f,
                player.whoAmI,
                target.X,
                target.Y,
                VesuviusProgression.GetWorldPowerStage());

            if (Main.projectile.IndexInRange(projIndex))
                Main.projectile[projIndex].CritChance = player.GetWeaponCrit(Item);

            SoundEngine.PlaySound(SoundID.Item73 with { Volume = 0.72f, Pitch = -0.32f }, target);
        }

        private static void KillOwnedVesuviusAttacks(Player player)
        {
            int leftHoldoutType = ModContent.ProjectileType<VesuviusLeftHoldout>();
            int rightHoldoutType = ModContent.ProjectileType<VesuviusRightJavelinHoldout>();
            int flameHoldoutType = ModContent.ProjectileType<VesuviusSuperFlameHoldout>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI &&
                    (projectile.type == leftHoldoutType || projectile.type == rightHoldoutType || projectile.type == flameHoldoutType))
                    projectile.Kill();
            }
        }
    }
}
