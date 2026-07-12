using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;


namespace CalamityLegendsComeBack.Weapons.GaelsGreatsword
{
    public class NewLegendGaelsGreatsword : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        private const int FollowupSlashWindow = 45;
        private const float PlungeDamageMultiplier = 1.9f;
        private const int FinisherCooldown = 18 * 60;

        private static int SwingHoldoutType => ModContent.ProjectileType<GaelGreatswordSwingHoldout>();
        private static int PlungeHoldoutType => ModContent.ProjectileType<GaelGreatswordPlungeHoldout>();
        private static int GuardHoldoutType => ModContent.ProjectileType<GaelGreatswordGuardHoldout>();
        private static int CapeFinisherType => ModContent.ProjectileType<GaelGreatswordCapeFinisher>();
        private static int EmberUIType => ModContent.ProjectileType<GaelGreatswordEmberUI>();

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 86;
            Item.height = 86;
            Item.damage = GaelGreatswordProgression.GetBaseDamage();
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 7f;
            Item.useTime = 23;
            Item.useAnimation = 23;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.shoot = SwingHoldoutType;
            Item.shootSpeed = 0f;
            Item.UseSound = null;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 8);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.ownedProjectileCounts[SwingHoldoutType] > 0 ||
                player.ownedProjectileCounts[PlungeHoldoutType] > 0 ||
                player.ownedProjectileCounts[GuardHoldoutType] > 0 ||
                player.ownedProjectileCounts[CapeFinisherType] > 0)
            {
                return false;
            }

            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.shootSpeed = 0f;
            Item.UseSound = null;

            if (player.altFunctionUse == 2)
            {
                GaelGreatswordPlayer gaelPlayer = player.GetModPlayer<GaelGreatswordPlayer>();
                bool canGuard = gaelPlayer.GuardCooldown <= 0;
                Item.channel = canGuard;
                Item.autoReuse = false;
                Item.useTime = canGuard ? 14 : 31;
                Item.useAnimation = Item.useTime;
                Item.shoot = canGuard ? GuardHoldoutType : PlungeHoldoutType;
                return base.CanUseItem(player);
            }

            Item.channel = true;
            Item.autoReuse = true;
            Item.useTime = Math.Max(12, GaelGreatswordProgression.GetLeftUseTime(player));
            Item.useAnimation = Item.useTime;
            Item.shoot = SwingHoldoutType;
            return base.CanUseItem(player);
        }

        public override bool CanShoot(Player player)
        {
            return player.ownedProjectileCounts[SwingHoldoutType] <= 0 &&
                player.ownedProjectileCounts[PlungeHoldoutType] <= 0 &&
                player.ownedProjectileCounts[GuardHoldoutType] <= 0 &&
                player.ownedProjectileCounts[CapeFinisherType] <= 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 aimDirection = (GetMouseWorld(player) - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);

            if (player.altFunctionUse == 2)
            {
                GaelGreatswordPlayer gaelState = player.GetModPlayer<GaelGreatswordPlayer>();
                if (gaelState.GuardCooldown <= 0)
                {
                    Projectile.NewProjectile(source, player.MountedCenter, aimDirection, GuardHoldoutType, damage, knockback, player.whoAmI);
                    return false;
                }

                Vector2 targetPoint = GetMouseWorld(player);
                int plungeDamage = (int)(damage * PlungeDamageMultiplier);
                Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, PlungeHoldoutType, plungeDamage, knockback + 2f, player.whoAmI, targetPoint.X, targetPoint.Y);
                return false;
            }

            GaelGreatswordPlayer gaelPlayer = player.GetModPlayer<GaelGreatswordPlayer>();
            bool followupSlash = gaelPlayer.ConsumeFollowupSlash();
            Projectile.NewProjectile(source, player.MountedCenter, aimDirection, SwingHoldoutType, damage, knockback, player.whoAmI, followupSlash ? 1f : 0f);
            return false;
        }

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            GaelGreatswordPlayer gaelPlayer = player.GetModPlayer<GaelGreatswordPlayer>();
            gaelPlayer.ApplyHeldEffects();

            if (Main.myPlayer != player.whoAmI)
                return;

            if (player.ownedProjectileCounts[EmberUIType] <= 0)
                Projectile.NewProjectile(Item.GetSource_FromThis(), player.Center, Vector2.Zero, EmberUIType, 0, 0f, player.whoAmI);

            if (!GaelGreatswordRageInterop.RageHotKeyJustPressed())
                return;

            TryActivateFinisher(player, gaelPlayer);
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base += GaelGreatswordProgression.GetBaseDamage() - Item.damage;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string rageKeyText = GaelGreatswordRageInterop.GetRageKeyText();
            string text = string.Format(this.GetLocalizedValue("FunctionalTooltip"), rageKeyText);
            tooltips.FindAndReplace("[GFB]", text);

            bool shiftPressed = Main.keyState.PressingShift();
            string legendarySection = shiftPressed
                ? this.GetLocalizedValue("LegendaryText")
                : this.GetLocalizedValue("LegendaryHint");
            tooltips.Add(new TooltipLine(Mod, "GaelDarkSoulLegendaryText", legendarySection));
        }

        private void TryActivateFinisher(Player player, GaelGreatswordPlayer gaelPlayer)
        {
            if (!Main.hardMode)
            {
                CombatText.NewText(player.Hitbox, Color.Gray, Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.NewLegendGaelsGreatsword.FinisherLocked"));
                return;
            }

            if (gaelPlayer.FinisherCooldown > 0 ||
                player.ownedProjectileCounts[CapeFinisherType] > 0 ||
                player.noItems ||
                player.CCed)
            {
                return;
            }

            if (!gaelPlayer.ConsumeDarkEmbers(GaelGreatswordPlayer.DarkEmberMax))
            {
                CombatText.NewText(player.Hitbox, new Color(120, 56, 170), Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.NewLegendGaelsGreatsword.FinisherNeedsEmbers"));
                return;
            }

            int damage = (int)(player.GetWeaponDamage(Item) * 0.92f);
            Projectile.NewProjectile(Item.GetSource_FromThis(), player.Center, Vector2.Zero, CapeFinisherType, damage, Item.knockBack, player.whoAmI);
            gaelPlayer.FinisherCooldown = FinisherCooldown;

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.85f, Pitch = -0.55f }, player.Center);
            SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.55f, Pitch = -0.2f }, player.Center);
        }

        internal static Vector2 GetMouseWorld(Player player)
        {
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }
    }
}
