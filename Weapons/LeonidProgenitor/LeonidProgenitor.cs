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
            int[] effectIDs = LeonidMetalSelection.CaptureEffectIDs(player);

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
                        player.whoAmI,
                        effectIDs[0],
                        effectIDs[1]);
                    
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
                        player.whoAmI,
                        effectIDs[0],
                        effectIDs[1]);
                }
            }
            else
            {
                // Left Click
                if (stealthStrike)
                {
                    // Left Click Stealth Strike: 7 comets falling diagonally
                    Vector2 mousePos = Main.MouseWorld;
                    for (int i = 0; i < 7; i++)
                    {
                        // Spawn in the upper-diagonal area of the mouse cursor
                        Vector2 spawnPos = mousePos + new Vector2(Main.rand.Next(-250, -100) * player.direction, Main.rand.Next(-600, -450));
                        Vector2 targetPos = mousePos + Main.rand.NextVector2Circular(80f, 80f);
                        Vector2 vel = (targetPos - spawnPos).SafeNormalize(Vector2.UnitY) * 16f;

                        int p = Projectile.NewProjectile(
                            source,
                            spawnPos,
                            vel,
                            ModContent.ProjectileType<LeonidCometSmall>(),
                            damage,
                            knockback,
                            player.whoAmI,
                            effectIDs[0],
                            effectIDs[1],
                            LeonidCometSmall.FromStealthFlag);
                        
                        if (p.WithinBounds(Main.maxProjectiles))
                        {
                            Main.projectile[p].Calamity().stealthStrike = true;
                            Main.projectile[p].localAI[1] = 0f; // no launch delay
                        }
                    }
                }
                else
                {
                    // Left Click Normal: 2 small comets with launch delay
                    Vector2 targetVelocity = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitY) * 14f;
                    
                    // Spawn at left and right sides of the player
                    Vector2 offset1 = new Vector2(-36f * player.direction, -20f);
                    Vector2 offset2 = new Vector2(36f * player.direction, -20f);

                    int p1 = Projectile.NewProjectile(
                        source,
                        player.Center + offset1,
                        targetVelocity,
                        ModContent.ProjectileType<LeonidCometSmall>(),
                        damage,
                        knockback,
                        player.whoAmI,
                        effectIDs[0],
                        effectIDs[1],
                        0f);
                    
                    int p2 = Projectile.NewProjectile(
                        source,
                        player.Center + offset2,
                        targetVelocity,
                        ModContent.ProjectileType<LeonidCometSmall>(),
                        damage,
                        knockback,
                        player.whoAmI,
                        effectIDs[0],
                        effectIDs[1],
                        0f);

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

            return false;
        }

        public override void HoldItem(Player player)
        {
            LeonidSelectedMetal[] selection = LeonidMetalSelection.Scan(player);
            player.GetModPlayer<LeonidMetalPlayer>().UpdateHighlights(selection);

            // Spawn UI
            int uiType = ModContent.ProjectileType<LeonidUltimateUI>();
            if (Main.myPlayer == player.whoAmI && player.ownedProjectileCounts[uiType] == 0)
            {
                Projectile.NewProjectile(Item.GetSource_FromThis(), player.Center, Vector2.Zero, uiType, 0, 0f, player.whoAmI);
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
                    int[] effectIDs = LeonidMetalSelection.CaptureEffectIDs(player);
                    Projectile.NewProjectile(Item.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<LeonidGravityField>(), damage, Item.knockBack, player.whoAmI, effectIDs[0], effectIDs[1]);

                    // Sound and Visual effects
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1.0f, Pitch = -0.5f }, player.Center);
                    SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.85f, Pitch = -0.2f }, player.Center);
                    calamityPlayer.GeneralScreenShakePower = Math.Max(calamityPlayer.GeneralScreenShakePower, 12f);

                    CombatText.NewText(player.getRect(), new Color(180, 150, 255), "狮子座回响 / Leonid Echo!", true, true);
                }
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            LeonidSelectedMetal[] selection = LeonidMetalSelection.Scan(Main.LocalPlayer);
            string leftClick = this.GetLocalizedValue("LeftClick");
            string stealthClick = this.GetLocalizedValue("StealthLeftClick");
            string rightClick = this.GetLocalizedValue("RightClick");

            string ultimateKey = KeybindSystem.LegendarySkill.GetAssignedKeys().FirstOrDefault() ?? "Unbound";
            string ultimateClick = string.Format(this.GetLocalizedValue("UltimateSkill"), ultimateKey);
            string passives = this.GetLocalizedValue("Passives");

            string currentMetalHeader = this.GetLocalizedValue("CurrentMetals");
            string lineA = BuildMetalLine(selection, 0);
            string lineB = BuildMetalLine(selection, 1);
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
                currentMetalHeader + "\n" +
                lineA + "\n" +
                lineB;

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

        private string BuildMetalLine(LeonidSelectedMetal[] selection, int index)
        {
            if (selection == null || index < 0 || index >= selection.Length || !selection[index].IsValid)
                return this.GetLocalizedValue("EmptyMetalLine");

            int effectID = selection[index].Entry.EffectID;
            string metalName = this.GetLocalizedValue($"MetalName{effectID}");
            string metalDesc = this.GetLocalizedValue($"MetalDesc{effectID}");
            return string.Format(this.GetLocalizedValue("MetalLine"), metalName, metalDesc);
        }
    }
}
