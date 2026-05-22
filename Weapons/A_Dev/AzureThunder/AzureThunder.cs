using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.Passive;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    public class AzureThunder : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        public override void SetDefaults()
        {
            Item.width = 86;
            Item.height = 86;
            Item.damage = 175;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 0;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.knockBack = 5.5f;
            Item.shoot = ModContent.ProjectileType<AzureThunderSwingHoldout>();
            Item.shootSpeed = 0f;
            Item.UseSound = null;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(0, 20);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
                return false;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<AzureThunderSwingHoldout>();
            Item.shootSpeed = 0f;
            Item.UseSound = null;

            if (player.ownedProjectileCounts[ModContent.ProjectileType<AzureThunderSwingHoldout>()] > 0)
                return false;

            return player.CheckMana(Item, AzureThunderPlayer.AttackManaCost, false, false);
        }

        public override bool CanShoot(Player player)
        {
            return player.altFunctionUse != 2 &&
                player.ownedProjectileCounts[ModContent.ProjectileType<AzureThunderSwingHoldout>()] <= 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
                return false;

            Projectile.NewProjectile(
                source,
                player.MountedCenter,
                Vector2.Zero,
                ModContent.ProjectileType<AzureThunderSwingHoldout>(),
                damage,
                knockback,
                player.whoAmI);

            return false;
        }

        public override void HoldItem(Player player)
        {
            AzureThunderPlayer thunderPlayer = player.GetModPlayer<AzureThunderPlayer>();
            thunderPlayer.SetHoldingAzureThunder();
            player.GetModPlayer<AzureThunderPassivePlayer>().SetHoldingAzureThunder();

            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            if (Main.myPlayer != player.whoAmI)
                return;

            if (!player.Calamity().mouseRight ||
                !Main.mouseRightRelease ||
                Main.mapFullscreen ||
                Main.blockMouse ||
                Main.playerInventory && Main.HoverItem.type == Item.type)
            {
                return;
            }

            TryUseRightClick(player, thunderPlayer);
        }

        private void TryUseRightClick(Player player, AzureThunderPlayer thunderPlayer)
        {
            if (!AzureThunderProgression.RightClickUnlocked)
                return;

            if (thunderPlayer.RightClickCooldown > 0)
                return;

            if (!thunderPlayer.TrySpendMana())
            {
                CombatText.NewText(player.Hitbox, new Color(255, 80, 80), Language.GetTextValue("Mods.CalamityLegendsComeBack.Common.NoMana"));
                return;
            }

            bool harmony = thunderPlayer.HarmonyActive;
            int consumedCharge = thunderPlayer.ConsumeThunderCharge();
            thunderPlayer.RestoreManaFromConsumedCharge(consumedCharge);
            int existingGroundSwords = AzureThunderPlayer.CountOwnedGroundSwords(player);
            int spawnCount = harmony ? 3 : consumedCharge + (existingGroundSwords < 3 ? 1 : 0);
            spawnCount = Utils.Clamp(spawnCount, 0, AzureThunderGroundSword.MaxGroundSwords - existingGroundSwords);

            Vector2 mouseWorld = AzureThunderPlayer.GetMouseWorld(player);
            for (int i = 0; i < spawnCount; i++)
            {
                float angle = MathHelper.TwoPi * (i / (float)System.Math.Max(1, spawnCount)) + Main.rand.NextFloat(-0.2f, 0.2f);
                float radius = harmony ? 190f : Main.rand.NextFloat(130f, 250f);
                Vector2 spawnPosition = player.Center + angle.ToRotationVector2() * radius;
                if (harmony)
                    spawnPosition = Vector2.Lerp(spawnPosition, mouseWorld + Main.rand.NextVector2Circular(160f, 70f), 0.45f);

                AzureThunderPlayer.SpawnGroundSword(player, spawnPosition, player.GetWeaponDamage(Item), Item.knockBack);
            }

            thunderPlayer.RestoreManaForOwnedSwords();
            thunderPlayer.RestoreLifeFromFourSymbols();
            player.Calamity().GeneralScreenShakePower = System.Math.Max(player.Calamity().GeneralScreenShakePower, harmony ? 7f : 5f);

            NPC target = AzureThunderPlayer.FindMouseNearestTarget(player);
            if (target != null)
            {
                int swordCount = harmony
                    ? System.Math.Max(3, AzureThunderPlayer.CountGroundSwordsNear(player, target.Center, 50f * 16f))
                    : System.Math.Max(1, AzureThunderPlayer.CountGroundSwordsNear(player, player.Center, 50f * 16f));

                int encodedMode = (consumedCharge * 10) + (harmony ? 1 : 0);
                Projectile.NewProjectile(
                    Item.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<AzureThunderStrikeSequencer>(),
                    player.GetWeaponDamage(Item),
                    Item.knockBack,
                    player.whoAmI,
                    target.whoAmI,
                    swordCount,
                    encodedMode);
            }

            thunderPlayer.RightClickCooldown = AzureThunderPlayer.RightClickCooldownMax;
            SoundEngine.PlaySound(SoundID.Item68 with { Volume = 0.82f, Pitch = harmony ? -0.08f : 0.02f }, player.Center);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.65f, Pitch = harmony ? 0.2f : 0f }, player.Center);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string keyText = KeybindSystem.LegendarySkill.GetAssignedKeys().Count > 0
                ? KeybindSystem.LegendarySkill.GetAssignedKeys()[0]
                : "Unbound";

            string text =
                this.GetLocalizedValue("AzureThunderLeft") + "\n\n" +
                this.GetLocalizedValue("AzureThunderRight") + "\n\n" +
                string.Format(this.GetLocalizedValue("AzureThunderUltimate"), keyText) + "\n\n" +
                this.GetLocalizedValue(GetHeavenEarthTooltipKey()) + "\n" +
                this.GetLocalizedValue(GetHeavensWardTooltipKey()) + "\n" +
                this.GetLocalizedValue(GetFourSymbolsTooltipKey()) + "\n\n" +
                this.GetLocalizedValue("AzureThunderThunderCharge") + "\n" +
                this.GetLocalizedValue("AzureThunderDot") + "\n\n" +
                this.GetLocalizedValue("AzureThunderFinal");
            tooltips.FindAndReplace("[GFB]", text);
        }

        private static string GetHeavenEarthTooltipKey()
        {
            if (AzureThunderProgression.DownedYharon)
                return "AzureThunderPassiveHeavenEarth4";
            if (AzureThunderProgression.DownedFishron)
                return "AzureThunderPassiveHeavenEarth3";
            if (AzureThunderProgression.DownedEvilTier2)
                return "AzureThunderPassiveHeavenEarth2";
            if (AzureThunderProgression.DownedDesertScourge)
                return "AzureThunderPassiveHeavenEarth1";

            return "AzureThunderPassiveHeavenEarth0";
        }

        private static string GetHeavensWardTooltipKey()
        {
            if (!AzureThunderProgression.DodgeUnlocked)
                return "AzureThunderPassiveHeavensWard0";
            if (AzureThunderProgression.DownedYharon)
                return "AzureThunderPassiveHeavensWard5";
            if (AzureThunderProgression.DownedMoonLord)
                return "AzureThunderPassiveHeavensWard4";
            if (AzureThunderProgression.DownedPlantera)
                return "AzureThunderPassiveHeavensWard3";
            if (AzureThunderProgression.DownedWallOfFlesh)
                return "AzureThunderPassiveHeavensWard2";

            return "AzureThunderPassiveHeavensWard1";
        }

        private static string GetFourSymbolsTooltipKey()
        {
            if (!AzureThunderProgression.FourSymbolsUnlocked)
                return "AzureThunderPassiveFourSymbols0";
            if (AzureThunderProgression.DownedMoonLord)
                return "AzureThunderPassiveFourSymbols3";
            if (AzureThunderProgression.DownedFishron)
                return "AzureThunderPassiveFourSymbols2";

            return "AzureThunderPassiveFourSymbols1";
        }
    }
}
