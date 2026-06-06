using CalamityLegendsComeBack.Accssory.MC;
using CalamityLegendsComeBack.Weapons.Malachite.EXSkill;
using CalamityLegendsComeBack.Weapons;
using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.Items;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Malachite
{
    public class Malachite : RogueWeapon, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/Malachite";

        public new string LocalizationCategory => "Items.Weapons";
        private bool wasFinaleReady;

        public override bool WeaponPrefix() => true;

        public override bool RangedPrefix() => false;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 58;
            Item.damage = 180;
            Item.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = 14;
            Item.useTime = 14;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 2.5f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<MalachiteKunai>();
            Item.shootSpeed = 18f;
            Item.value = CalamityGlobalItem.RarityYellowBuyPrice;
            Item.rare = ModContent.RarityType<BurnishedAuric>();
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            MalachitePlayer malachitePlayer = player.GetModPlayer<MalachitePlayer>();
            if (player.Calamity().StealthStrikeAvailable())
            {
                Item.useAnimation = 34;
                Item.useTime = 34;
                Item.UseSound = SoundID.Item109;
                return true;
            }

            if (player.altFunctionUse == 2)
            {
                if (!malachitePlayer.CanUseRightClick)
                    return false;

                Item.useAnimation = 24;
                Item.useTime = 24;
                Item.UseSound = SoundID.Item109;
                return true;
            }

            Item.useAnimation = 14;
            Item.useTime = 14;
            Item.UseSound = SoundID.Item1;
            return true;
        }

        public override float UseSpeedMultiplier(Player player)
        {
            if (player.altFunctionUse == 2 || player.Calamity().StealthStrikeAvailable())
                return 1f;

            return MalachiteProgression.LeftClickUseSpeedMultiplier;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 mouseWorld = GetMouseWorld(player);
            CalamityPlayer calamity = player.Calamity();
            MalachiteAccessoryPlayer accessoryPlayer = player.GetModPlayer<MalachiteAccessoryPlayer>();
            MalachitePlayer malachitePlayer = player.GetModPlayer<MalachitePlayer>();

            if (calamity.StealthStrikeAvailable())
            {
                MalachiteKunai.FireStoredPeacockKunaiAsLeftThrows(player, mouseWorld);
                MalachiteKunai.SpawnPeacockFan(player, source, damage, knockback);
                calamity.ConsumeStealthByAttacking();
                player.GetModPlayer<MalachitePlayer>().RestoreStealthPoints(15f);
                SoundEngine.PlaySound(SoundID.Item109 with { Volume = 0.95f, Pitch = -0.12f }, player.Center);
                return false;
            }

            if (player.altFunctionUse == 2)
            {
                MalachiteKunai.PrepareForNewFrenzyFan(player, mouseWorld);
                MalachiteKunai.SpawnFrenzyFan(player, source, damage, knockback);
                player.GetModPlayer<MalachitePlayer>().StartRightClickCooldown();
                SoundEngine.PlaySound(SoundID.Item109 with { Volume = 0.85f, Pitch = 0.08f }, player.Center);
                return false;
            }

            if (MalachiteKunai.TryThrowStoredKunai(player, mouseWorld))
            {
                TryRegenerateFrenzyFanFromScroll(player, source, damage, knockback, mouseWorld, accessoryPlayer);
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.78f, Pitch = 0.22f }, player.Center);
                return false;
            }

            TryRegenerateFrenzyFanFromScroll(player, source, damage, knockback, mouseWorld, accessoryPlayer);
            int normalKunaiCount = malachitePlayer.DepletionBurstActive
                ? MalachiteProgression.DepletionBurstKunaiCount
                : MalachiteProgression.NormalLeftClickKunaiCount;

            if (normalKunaiCount > 1)
            {
                MalachiteKunai.SpawnNormalLeftClickVolley(
                    player,
                    source,
                    damage,
                    knockback,
                    mouseWorld,
                    normalKunaiCount,
                    malachitePlayer.DepletionBurstActive);
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.86f, Pitch = normalKunaiCount >= 5 ? 0.38f : 0.18f }, player.Center);
                return false;
            }

            return true;
        }

        public override void ModifyStatsExtra(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            velocity *= player.GetModPlayer<MalachiteAccessoryPlayer>().MalachiteProjectileVelocityMultiplier;
        }

        public override void HoldItem(Player player)
        {
            player.GetModPlayer<MalachitePlayer>().SetHoldingMalachite();
            player.Calamity().mouseWorldListener = true;

            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            CalamityPlayer calamity = player.Calamity();
            bool finaleReady = calamity.rogueStealthMax > 0f &&
                calamity.rogueStealth >= calamity.rogueStealthMax * 0.999f &&
                !IsFinaleCoolingDown(player);
            LegendaryUltimateReadySound.PlayIfReadyTransition(player, ref wasFinaleReady, finaleReady);

            TryUseFinale(player);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string keyText = KeybindSystem.LegendarySkill.GetAssignedKeys().FirstOrDefault() ?? "Unbound";
            string finalText =
                this.GetLocalizedValue("LeftClick") + "\n\n" +
                this.GetLocalizedValue("RightClick") + "\n\n" +
                this.GetLocalizedValue("StealthStrike") + "\n\n" +
                string.Format(this.GetLocalizedValue("Finale"), keyText) + "\n\n" +
                this.GetLocalizedValue("Passive") + "\n\n" +
                (Main.keyState.PressingShift()
                    ? this.GetLocalizedValue("LegendaryText")
                    : this.GetLocalizedValue("LegendaryHint")) + "\n";

            tooltips.FindAndReplace("[GFB]", finalText);
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            MalachitePlayer malachitePlayer = Main.LocalPlayer.GetModPlayer<MalachitePlayer>();
            if (malachitePlayer.CanUseRightClick || Main.LocalPlayer.HeldItem.type != Type)
                return;

            Texture2D barBackground = TextureAssets.MagicPixel.Value;
            float completion = MathHelper.Clamp(malachitePlayer.RightClickCooldownCompletion, 0f, 1f);
            Vector2 barPosition = position + new Vector2(-18f, frame.Height * 0.55f) * scale;
            Rectangle back = new((int)barPosition.X, (int)barPosition.Y, (int)(36f * scale), (int)(4f * scale));
            Rectangle front = new(back.X, back.Y, (int)(back.Width * completion), back.Height);

            spriteBatch.Draw(barBackground, back, Color.Black * 0.65f);
            spriteBatch.Draw(barBackground, front, new Color(90, 245, 128) * 0.9f);
        }

        private void TryUseFinale(Player player)
        {
            CalamityPlayer calamity = player.Calamity();
            if (player.whoAmI != Main.myPlayer ||
                KeybindSystem.LegendarySkill?.JustPressed != true ||
                calamity.rogueStealthMax <= 0f ||
                calamity.rogueStealth < calamity.rogueStealthMax * 0.999f)
            {
                return;
            }

            int finaleType = ModContent.ProjectileType<MalachiteFinaleController>();
            if (player.ownedProjectileCounts[finaleType] > 0 || IsFinaleCoolingDown(player))
                return;

            calamity.ConsumeStealthByAttacking();

            Vector2 direction = (GetMouseWorld(player) - player.Center).SafeNormalize(Vector2.UnitX * player.direction);
            if (player.GetModPlayer<MalachiteAccessoryPlayer>().GaleAceEquipped)
                MalachiteKunai.ActivateStoredKunaiAsAces(player, GetMouseWorld(player));

            int finaleDamage = (int)player.GetTotalDamage(Item.DamageType).ApplyTo(Item.damage * 4.5f);

            Projectile.NewProjectile(
                player.GetSource_ItemUse(Item),
                player.Center,
                direction,
                finaleType,
                finaleDamage,
                Item.knockBack,
                player.whoAmI);

            StartFinaleCooldown(player);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.95f, Pitch = -0.25f }, player.Center);
        }

        private static bool IsFinaleCoolingDown(Player player)
        {
            return player.Calamity().cooldowns.TryGetValue(MalachiteEXCooldown.ID, out var cooldown) &&
                cooldown.timeLeft > 0;
        }

        private static void StartFinaleCooldown(Player player)
        {
            if (player.Calamity().cooldowns.TryGetValue(MalachiteEXCooldown.ID, out var cooldown))
                cooldown.timeLeft = MalachiteEXCooldown.CooldownFrames;
            else
                player.AddCooldown(MalachiteEXCooldown.ID, MalachiteEXCooldown.CooldownFrames);
        }

        private static void TryRegenerateFrenzyFanFromScroll(
            Player player,
            IEntitySource source,
            int damage,
            float knockback,
            Vector2 mouseWorld,
            MalachiteAccessoryPlayer accessoryPlayer)
        {
            if (!accessoryPlayer.ShouldRegenerateFrenzyFan)
                return;

            MalachiteKunai.PrepareForNewFrenzyFan(player, mouseWorld);
            MalachiteKunai.SpawnFrenzyFan(player, source, damage, knockback);
        }

        private static Vector2 GetMouseWorld(Player player)
        {
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }
    }
}
