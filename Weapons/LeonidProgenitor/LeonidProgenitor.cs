using System;
using System.Collections.Generic;
using System.Linq;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityLegendsComeBack.LegendaryTooltipEffects;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects;
using CalamityMod;
using CalamityMod.Items.Weapons.Rogue;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    public class LeonidProgenitor : RogueWeapon, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/LeonidProgenitor";
        public new string LocalizationCategory => "Items.Weapons";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 48;
            Item.damage = LP_Balance.GetInitialLeftClickBaseDamage();
            Item.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Item.knockBack = 4f;
            Item.useAnimation = Item.useTime = 20;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LeonidCometSmall>();
            Item.shootSpeed = 15.5f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.UseSound = SoundID.Item61;
            Item.value = CalamityMod.Items.CalamityGlobalItem.RarityPinkBuyPrice;
            Item.rare = ItemRarityID.Yellow;
            Item.channel = true;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            ModContent.GetInstance<CalamityMod.Items.Accessories.InterstellarStompers>().UpdateAccessory(player, hideVisual);
            player.GetModPlayer<LeonidProgenitorPlayer>().SetAccessoryMeteorRain(Item);
        }

        // Each button has its own multiplier below; do not apply the old shared 1.2x twice.
        public override float StealthDamageMultiplier => 1f;

        public override void ModifyStatsExtra(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.Calamity().StealthStrikeAvailable())
                damage = (int)(damage * LP_Balance.GetStealthDamageMultiplier(player.altFunctionUse == 2));
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            int baseDamage = LP_Balance.GetLeftClickBaseDamage();
            damage.Base += baseDamage - Item.damage;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<LeonidRightClickHoldout>()] > 0)
                return false;

            if (player.altFunctionUse != 2)
                return base.CanUseItem(player);

            if (!player.GetModPlayer<LeonidConstellationPlayer>().IsUnlocked(LeonidStar.Zosma))
                return false;

            // Accessories allowing early stealth strikes do not bypass this full-stealth requirement.
            var calamityPlayer = player.Calamity();
            if (calamityPlayer.rogueStealthMax <= 0f || calamityPlayer.rogueStealth < calamityPlayer.rogueStealthMax)
            {
                player.GetModPlayer<LeonidProgenitorPlayer>().ShowRightClickStealthFeedback();
                return false;
            }

            LeonidProgenitorPlayer leonidPlayer = player.GetModPlayer<LeonidProgenitorPlayer>();
            if (leonidPlayer.CanUseRightClick)
                return base.CanUseItem(player);

            leonidPlayer.SpawnRightClickCooldownFeedback();
            return false;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 aimPoint = global::CalamityLegendsComeBack.Accssory.SHPC.Skill.CtrlChip.CtrlChipPlayer.GetFireAimWorld(player, Main.MouseWorld);
            bool stealthStrike = player.Calamity().StealthStrikeAvailable();
            LeonidConstellationPlayer constellation = player.GetModPlayer<LeonidConstellationPlayer>();

            if (player.altFunctionUse == 2)
            {
                player.GetModPlayer<LeonidProgenitorPlayer>().StartRightClickCooldown();
                // Pay the discounted stealth cost once when charging begins; release uses this damage snapshot.
                int p = Projectile.NewProjectile(source, player.Center,
                    (aimPoint - player.Center).SafeNormalize(Vector2.UnitX * player.direction),
                    ModContent.ProjectileType<LeonidRightClickHoldout>(), damage, knockback, player.whoAmI,
                    0f, aimPoint.X, aimPoint.Y);
                if (p.WithinBounds(Main.maxProjectiles))
                    Main.projectile[p].Calamity().stealthStrike = true;
            }
            else
            {
                // Left Click
                if (stealthStrike)
                {
                    // Left Click Stealth Strike: spawner drops 7 comets one by one
                    // (every 5 ticks, from ~50 tiles above the cursor)
                    Projectile.NewProjectile(
                        source,
                        aimPoint,
                        Vector2.Zero,
                        ModContent.ProjectileType<LeonidStealthRainSpawner>(),
                        damage,
                        knockback,
                        player.whoAmI,
                        player.direction);
                }
                else
                {
                    // Left Click Normal: 2 small comets with launch delay
                    Vector2 targetVelocity = (aimPoint - player.Center).SafeNormalize(Vector2.UnitY) * 14f;
                    
                    // Spawn at left and right sides of the player
                    Vector2 offset1 = new Vector2(-36f * player.direction, -20f);
                    Vector2 offset2 = new Vector2(36f * player.direction, -20f);

                    int p1 = SpawnSmallComet(source, player, player.Center + offset1, targetVelocity, damage, knockback, aimPoint);
                    int p2 = SpawnSmallComet(source, player, player.Center + offset2, targetVelocity, damage, knockback, aimPoint);

                    if (p1.WithinBounds(Main.maxProjectiles))
                    {
                        Main.projectile[p1].localAI[1] = 12f; // launch delay 12 ticks
                    }
                    if (p2.WithinBounds(Main.maxProjectiles))
                    {
                        Main.projectile[p2].localAI[1] = 24f; // launch delay 24 ticks
                    }

                }
            }

            // Shoot is fully custom, so RogueWeapon's normal stealth-consumption path is
            // bypassed. Consume once after either mouse button has produced its stealth attack.
            if (stealthStrike)
            {
                var calamity = player.Calamity();
                float before = calamity.rogueStealth;
                calamity.ConsumeStealthByAttacking();
                float costFactor = player.altFunctionUse == 2
                    ? (constellation.IsUnlocked(LeonidStar.Denebola) ? 0.75f : 1f)
                    : (constellation.IsUnlocked(LeonidStar.Algenubi) ? 0.5f : 1f);
                // Multiply the actual accessory-adjusted cost, including flat cost reductions.
                calamity.rogueStealth = MathHelper.Clamp(before - (before - calamity.rogueStealth) * costFactor, 0f, calamity.rogueStealthMax);
            }

            return false;
        }

        public override void HoldItem(Player player)
        {
            // Sync Ultimate energy to CalamityMod's cooldown UI
            if (Main.myPlayer == player.whoAmI)
            {
                var modPlayer = player.GetModPlayer<LeonidProgenitorPlayer>();
                if (player.Calamity().cooldowns.TryGetValue(LeonidEXCooldown.ID, out var exCooldown))
                    exCooldown.timeLeft = modPlayer.UltimateEnergy;
                else
                    player.AddCooldown(LeonidEXCooldown.ID, 100).timeLeft = modPlayer.UltimateEnergy;
            }

            // Ultimate Trigger check
            if (Main.myPlayer == player.whoAmI)
            {
                var modPlayer = player.GetModPlayer<LeonidProgenitorPlayer>();
                var calamityPlayer = player.Calamity();

                bool ultimateReady = Main.hardMode && modPlayer.UltimateEnergy >= 100 && calamityPlayer.rogueStealth >= calamityPlayer.rogueStealthMax * 0.999f;
                if (KeybindSystem.LegendarySkill.JustPressed && ultimateReady)
                {
                    // Trigger ultimate!
                    modPlayer.UltimateEnergy = 0;
                    calamityPlayer.rogueStealth = 0f;
                    calamityPlayer.ConsumeStealthByAttacking();

                    // Spawn the Gravity Field projectile
                    Vector2 spawnPos = Main.MouseWorld;
                    int damage = (int)player.GetTotalDamage(Item.DamageType).ApplyTo(LP_Balance.GetLeftClickBaseDamage());
                    Projectile.NewProjectile(Item.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<LeonidGravityField>(), damage, Item.knockBack, player.whoAmI);

                    // Sound and Visual effects
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1.0f, Pitch = -0.5f }, player.Center);
                    SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.85f, Pitch = -0.2f }, player.Center);
                    global::CalamityLegendsComeBack.Systems.ModScreenShake.Max(player, player.Center, 12f);

                    // 起手：星辉先从玩家身上炸开，再朝引力场展开的方向拉出一道星轨，
                    // 把"从我这里发动 → 落到那边"这条动线连起来。
                    Vector2 toField = (spawnPos - player.Center).SafeNormalize(Vector2.UnitY);
                    LeonidStarlight.Ring(player.Center, 16, 40f, LeonidVisualUtils.GetReadyGold(),
                        LeonidStarlightShape.Shard, speed: 8f, scale: 1.15f, hoverTime: 20, lifetime: 180, lanceSpeed: 22f);
                    LeonidStarlight.Spray(player.Center, toField, 12, LeonidVisualUtils.StratusBlue,
                        LeonidStarlightShape.Needle, speed: 13f, spread: 0.35f, scale: 1.2f,
                        hoverTime: 26, lifetime: 190, lanceSpeed: 24f);
                    LeonidStarlight.Spawn(player.Center, Vector2.Zero, LeonidVisualUtils.StarGold,
                        LeonidStarlightShape.Halo, 1.8f, 14, 96, 11f, linksToSiblings: false);

                    CombatText.NewText(player.getRect(), LeonidVisualUtils.GetReadyGold(), "狮子座回响 / Leonid Echo!", true, true);
                }
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            LeonidConstellationPlayer constellation = Main.LocalPlayer.GetModPlayer<LeonidConstellationPlayer>();
            string leftClick = this.GetLocalizedValue("LeftClick");
            string stealthClick = this.GetLocalizedValue("StealthLeftClick");
            string rightClick = !constellation.IsUnlocked(LeonidStar.Zosma)
                ? this.GetLocalizedValue("RightClickLocked")
                : this.GetLocalizedValue("RightClick") + "\n" + this.GetLocalizedValue(
                    constellation.IsUnlocked(LeonidStar.RLeonis) ? "RightClickImpact" : "RightClickImpactLocked");

            string ultimateKey = KeybindSystem.LegendarySkill.GetAssignedKeys().FirstOrDefault() ?? "Unbound";
            string ultimateClick = string.Format(this.GetLocalizedValue("UltimateSkill"), ultimateKey, constellation.IsUnlocked(LeonidStar.Wolf359) ? 10 : 3);
            string passives = this.GetLocalizedValue("Passives");
            string constellationTitle = this.GetLocalizedValue("ConstellationTitle");

            string constellationStatus = string.Format(
                this.GetLocalizedValue("ConstellationStatus"),
                constellation.AvailablePoints,
                constellation.SpentPoints,
                constellation.EarnedPoints,
                LeonidConstellation.TotalCost);
            string constellationHint = string.Format(
                this.GetLocalizedValue("ConstellationHint"),
                InventoryActivationInput.GetDisplayKeyOrDefault(
                    KeybindSystem.LeonidConstellationChart,
                    Language.ActiveCulture.Name.StartsWith("zh") ? "左 Ctrl" : "Left Ctrl"));

            string merged = string.Join("\n", new[]
            {
                leftClick,
                stealthClick,
                rightClick,
                passives,
                string.Format(this.GetLocalizedValue("EquipEffect"), Lang.GetItemNameValue(ModContent.ItemType<CalamityMod.Items.Accessories.InterstellarStompers>())),
                ultimateClick,
                constellationTitle,
                constellationHint,
                constellationStatus,
            }.Select(text => text.TrimEnd('\r', '\n')));

            tooltips.FindAndReplace("[GFB]", merged);
            tooltips.Add(new TooltipLine(Mod, LeonidProgenitorMeteorRainLegendaryTooltip.TooltipLineName,
                this.GetLocalizedValue("LegendaryText")));
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Item.DrawItemGlowmaskSingleFrame(spriteBatch, rotation, ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Rogue/LeonidProgenitorGlow").Value);
        }

        private static int SpawnSmallComet(EntitySource_ItemUse_WithAmmo source, Player player, Vector2 spawnPosition, Vector2 velocity, int damage, float knockback, Vector2 aimPoint)
        {
            LeonidConstellationPlayer constellation = player.GetModPlayer<LeonidConstellationPlayer>();
            float speedMultiplier = constellation.IsUnlocked(LeonidStar.Algieba) ? 1.2f : 1f;
            int p = Projectile.NewProjectile(source, spawnPosition, velocity * speedMultiplier, ModContent.ProjectileType<LeonidCometSmall>(), damage, knockback, player.whoAmI);
            var fireControl = player.GetModPlayer<global::CalamityLegendsComeBack.Accssory.SHPC.Skill.CtrlChip.CtrlChipPlayer>();
            if (fireControl.CtrlChipEquipped && fireControl.ReticleHasTarget &&
                p.WithinBounds(Main.maxProjectiles) && Main.projectile[p].ModProjectile is LeonidCometSmall small)
            {
                small.SetLaunchAim(aimPoint, velocity.Length() * speedMultiplier);
                Main.projectile[p].netUpdate = true;
            }

            return p;
        }
    }
}
