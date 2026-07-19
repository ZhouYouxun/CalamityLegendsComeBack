using System;
using System.Collections.Generic;
using System.Linq;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityMod;
using CalamityMod.Items.Weapons.Rogue;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
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
            Item.knockBack = 4f;
            Item.useAnimation = Item.useTime = 20;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LeonidCometSmall>();
            Item.shootSpeed = 15.5f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.UseSound = SoundID.Item61;
            Item.value = Item.sellPrice(0, 10);
            Item.rare = ItemRarityID.Yellow;
            Item.channel = true;
        }

        public override float StealthDamageMultiplier => 1.2f;

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base += LP_Balance.GetLeftClickBaseDamage() - Item.damage;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            bool stealthStrike = player.Calamity().StealthStrikeAvailable();
            LeonidConstellationPlayer constellation = player.GetModPlayer<LeonidConstellationPlayer>();

            if (player.altFunctionUse == 2)
            {
                // Right Click
                if (stealthStrike)
                {
                    // Right Click Stealth Strike: Lion Head
                    int p = Projectile.NewProjectile(
                        source,
                        player.Center,
                        (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitX * player.direction) * 12f,
                        ModContent.ProjectileType<LeonidLionHead>(),
                        damage * 3, // High damage
                        knockback * 1.5f,
                        player.whoAmI);

                    if (constellation.IsUnlocked(LeonidStar.Alterf) && p.WithinBounds(Main.maxProjectiles))
                        Main.projectile[p].damage = (int)(Main.projectile[p].damage * 1.25f);
                    
                    if (p.WithinBounds(Main.maxProjectiles))
                        Main.projectile[p].Calamity().stealthStrike = true;
                }
                else
                {
                    // Right Click Charge: LeonidRightClickHoldout
                    Projectile.NewProjectile(
                        source,
                        player.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<LeonidRightClickHoldout>(),
                        damage,
                        knockback,
                        player.whoAmI);
                }
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
                        Main.MouseWorld,
                        Vector2.Zero,
                        ModContent.ProjectileType<LeonidStealthRainSpawner>(),
                        damage,
                        knockback,
                        player.direction);
                }
                else
                {
                    // Left Click Normal: 2 small comets with launch delay
                    Vector2 targetVelocity = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitY) * 14f;
                    
                    // Spawn at left and right sides of the player
                    Vector2 offset1 = new Vector2(-36f * player.direction, -20f);
                    Vector2 offset2 = new Vector2(36f * player.direction, -20f);

                    int p1 = SpawnSmallComet(source, player, player.Center + offset1, targetVelocity, damage, knockback);
                    int p2 = SpawnSmallComet(source, player, player.Center + offset2, targetVelocity, damage, knockback);

                    int p3 = -1;
                    if (constellation.IsUnlocked(LeonidStar.Adhafera))
                    {
                        Vector2 centerOffset = new Vector2(0f, -48f);
                        p3 = SpawnSmallComet(source, player, player.Center + centerOffset, targetVelocity, damage, knockback);
                    }

                    if (p1.WithinBounds(Main.maxProjectiles))
                    {
                        Main.projectile[p1].localAI[1] = 12f; // launch delay 12 ticks
                    }
                    if (p2.WithinBounds(Main.maxProjectiles))
                    {
                        Main.projectile[p2].localAI[1] = 24f; // launch delay 24 ticks
                    }
                    if (p3.WithinBounds(Main.maxProjectiles))
                    {
                        Main.projectile[p3].localAI[1] = 18f;
                    }
                }
            }

            // Shoot is fully custom, so RogueWeapon's normal stealth-consumption path is
            // bypassed. Consume once after either mouse button has produced its stealth attack.
            if (stealthStrike)
                player.Calamity().ConsumeStealthByAttacking();

            return false;
        }

        public override void HoldItem(Player player)
        {
            // Spawn UI
            int uiType = ModContent.ProjectileType<LeonidUltimateUI>();
            if (Main.myPlayer == player.whoAmI && player.ownedProjectileCounts[uiType] == 0)
            {
                Projectile.NewProjectile(Item.GetSource_FromThis(), player.Center, Vector2.Zero, uiType, 0, 0f, player.whoAmI);
            }

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

                bool ultimateReady = modPlayer.UltimateEnergy >= 100 && calamityPlayer.rogueStealth >= calamityPlayer.rogueStealthMax * 0.999f;
                if (KeybindSystem.LegendarySkill.JustPressed && ultimateReady)
                {
                    // Trigger ultimate!
                    modPlayer.UltimateEnergy = 0;
                    calamityPlayer.rogueStealth = 0f;
                    calamityPlayer.ConsumeStealthByAttacking();

                    // Spawn the Gravity Field projectile
                    Vector2 spawnPos = Main.MouseWorld;
                    int damage = (int)player.GetTotalDamage(Item.DamageType).ApplyTo(Item.damage * 10);
                    Projectile.NewProjectile(Item.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<LeonidGravityField>(), damage, Item.knockBack, player.whoAmI);

                    // Sound and Visual effects
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1.0f, Pitch = -0.5f }, player.Center);
                    SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.85f, Pitch = -0.2f }, player.Center);
                    calamityPlayer.GeneralScreenShakePower = Math.Max(calamityPlayer.GeneralScreenShakePower, 12f);

                    CombatText.NewText(player.getRect(), LeonidVisualUtils.GetReadyGold(), "狮子座回响 / Leonid Echo!", true, true);
                }
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            LeonidConstellationPlayer constellation = Main.LocalPlayer.GetModPlayer<LeonidConstellationPlayer>();
            string leftClick = this.GetLocalizedValue("LeftClick");
            string stealthClick = this.GetLocalizedValue("StealthLeftClick");
            string rightClick = this.GetLocalizedValue("RightClick");

            string ultimateKey = KeybindSystem.LegendarySkill.GetAssignedKeys().FirstOrDefault() ?? "Unbound";
            string ultimateClick = string.Format(this.GetLocalizedValue("UltimateSkill"), ultimateKey);
            string passives = this.GetLocalizedValue("Passives");

            string constellationStatus = string.Format(
                this.GetLocalizedValue("ConstellationStatus"),
                constellation.AvailablePoints,
                constellation.SpentPoints,
                constellation.EarnedPoints,
                LeonidConstellation.TotalCost);
            string constellationHint = this.GetLocalizedValue("ConstellationHint");
            string legendaryBody = this.GetLocalizedValue("LegendaryText");
            string legendaryHint = this.GetLocalizedValue("LegendaryHint");
            bool shiftPressed = Main.keyState.PressingShift();
            string legendarySection = shiftPressed ? legendaryBody : legendaryHint;

            string merged =
                leftClick + "\n" +
                stealthClick + "\n" +
                rightClick + "\n" +
                ultimateClick + "\n" +
                passives + "\n\n" +
                constellationStatus + "\n" +
                constellationHint;

            if (shiftPressed)
                tooltips.RemoveAll(t => t.Text == "[GFB]");
            else
                tooltips.FindAndReplace("[GFB]", merged);
            tooltips.Add(new TooltipLine(Mod, "LeonidProgenitorMeteorRainLegendaryText", legendarySection));
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Item.DrawItemGlowmaskSingleFrame(spriteBatch, rotation, ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Rogue/LeonidProgenitorGlow").Value);
        }

        private static int SpawnSmallComet(EntitySource_ItemUse_WithAmmo source, Player player, Vector2 spawnPosition, Vector2 velocity, int damage, float knockback)
        {
            float speedMultiplier = player.GetModPlayer<LeonidConstellationPlayer>().IsUnlocked(LeonidStar.Algieba) ? 1.2f : 1f;
            return Projectile.NewProjectile(source, spawnPosition, velocity * speedMultiplier, ModContent.ProjectileType<LeonidCometSmall>(), damage, knockback, player.whoAmI);
        }
    }
}
