using System;
using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder;
using CalamityLegendsComeBack.Weapons.BrinyBaron;
using CalamityLegendsComeBack.Weapons.BrinyBaron.Passive_QuickDash;
using CalamityMod;
using CalamityMod.Cooldowns;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace CalamityLegendsComeBack.Accssory.TS
{
    internal sealed class JiDianFa : ModItem
    {
        public new string LocalizationCategory => "Items";
        public override string Texture => "CalamityLegendsComeBack/Accssory/TS/Jidianfa/疾电法";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 16);
            Item.rare = ItemRarityID.Cyan;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<JiDianFaPlayer>().Equipped = true;
        }
    }

    internal sealed class JiDianFaPlayer : ModPlayer
    {
        public const int LightningCooldownFrames = 15 * 60;

        private const int DoubleTapInputWindow = 15;
        private const int BaseDashCooldown = 40;
        private const int DashIFrames = 12;
        private const float BaseDashDistance = 16f * 16f;
        private const float HeldAzureThunderDistanceMult = 1.3f;
        private const float HeldAzureThunderCooldownMult = 0.7f;
        private const float ExitVelocity = 14f;

        private int doubleTapTimer;
        private int lastTapDirection;
        private int dashCooldownTimer;
        private int lightningCooldownTimer;
        private int lightningCooldownDuration = LightningCooldownFrames;
        private int invulnerabilityTimer;

        public bool Equipped;
        public bool LightningCoolingDown => lightningCooldownTimer > 0;
        public int LightningCooldownRemaining => lightningCooldownTimer;
        public float LightningCooldownCompletion => !LightningCoolingDown ? 1f : 1f - lightningCooldownTimer / (float)Math.Max(1, lightningCooldownDuration);

        public override void ResetEffects()
        {
            Equipped = false;
        }

        public override void UpdateDead()
        {
            doubleTapTimer = 0;
            lastTapDirection = 0;
            dashCooldownTimer = 0;
            lightningCooldownTimer = 0;
            invulnerabilityTimer = 0;
        }

        public override void PostUpdate()
        {
            if (doubleTapTimer > 0)
                doubleTapTimer--;
            else
                lastTapDirection = 0;

            if (dashCooldownTimer > 0)
                dashCooldownTimer--;

            if (lightningCooldownTimer > 0)
                lightningCooldownTimer--;

            if (invulnerabilityTimer > 0)
            {
                invulnerabilityTimer--;
                Player.immune = true;
                Player.immuneTime = Math.Max(Player.immuneTime, 2);
                Player.noKnockback = true;
            }

            SyncLightningCooldownDisplay();
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (Main.myPlayer != Player.whoAmI || !CanReadDashInput())
                return;

            if (ShouldYieldToSingleSwordDash())
                return;

            if (Player.controlLeft && Player.controlRight)
                return;

            int dashDirection = 0;
            if (Player.controlLeft && Player.releaseLeft)
                dashDirection = ProcessDoubleTap(-1);

            if (Player.controlRight && Player.releaseRight)
                dashDirection = ProcessDoubleTap(1);

            if (dashDirection != 0)
                StartJoltingDash(dashDirection);
        }

        private int ProcessDoubleTap(int direction)
        {
            if (doubleTapTimer > 0 && lastTapDirection == direction)
            {
                doubleTapTimer = 0;
                lastTapDirection = 0;
                return direction;
            }

            doubleTapTimer = DoubleTapInputWindow;
            lastTapDirection = direction;
            return 0;
        }

        private bool CanReadDashInput()
        {
            return Equipped &&
                dashCooldownTimer <= 0 &&
                Player.active &&
                !Player.dead &&
                !Player.CCed &&
                !Player.mount.Active;
        }

        private bool ShouldYieldToSingleSwordDash()
        {
            return Player.HeldItem?.type == ModContent.ItemType<NewLegendBrinyBaron>() &&
                Main.hardMode &&
                Player.GetModPlayer<Dash_Trigger>().DashEnabled;
        }

        private void StartJoltingDash(int tapDirection)
        {
            bool holdingAzureThunder = IsHoldingAzureThunder();
            Vector2 dashDirection = ResolveDashDirection(tapDirection);
            float dashDistance = ResolveDashDistance(holdingAzureThunder);
            Vector2 start = Player.Center;
            Vector2 destination = ResolveDashDestination(start, dashDirection, dashDistance);

            if (Vector2.DistanceSquared(start, destination) < 16f * 16f)
                return;

            SpawnDashLightningVisual(start, destination, dashDirection);
            TeleportPlayer(destination, dashDirection);
            SpawnDashEndEffects(destination, dashDirection);

            dashCooldownTimer = holdingAzureThunder ?
                Math.Max(1, (int)Math.Round(BaseDashCooldown * HeldAzureThunderCooldownMult)) :
                BaseDashCooldown;
            invulnerabilityTimer = DashIFrames;

            if (holdingAzureThunder)
                TryReleaseAzureThunderLightning(dashDirection);
        }

        private Vector2 ResolveDashDirection(int tapDirection)
        {
            float xMagnitude = Math.Max(5f, Math.Abs(Player.velocity.X));
            Vector2 direction = new(tapDirection * xMagnitude, Player.velocity.Y);

            if (direction.LengthSquared() < 9f)
            {
                direction = new Vector2(tapDirection, 0f);
                if (Player.controlUp || Player.controlJump)
                    direction.Y -= 0.45f;
                if (Player.controlDown)
                    direction.Y += 0.45f;
            }

            return direction.SafeNormalize(Vector2.UnitX * tapDirection);
        }

        private float ResolveDashDistance(bool holdingAzureThunder)
        {
            float speedBonus = MathHelper.Clamp(Player.velocity.Length() * 2.5f, 0f, 64f);
            float distance = BaseDashDistance + speedBonus;
            if (holdingAzureThunder)
                distance *= HeldAzureThunderDistanceMult;

            return distance;
        }

        private Vector2 ResolveDashDestination(Vector2 start, Vector2 direction, float distance)
        {
            for (float checkDistance = distance; checkDistance >= 32f; checkDistance -= 16f)
            {
                Vector2 candidate = start + direction * checkDistance;
                Vector2 topLeft = candidate - Player.Size * 0.5f;

                if (Collision.SolidCollision(topLeft, Player.width, Player.height))
                    continue;

                if (!Collision.CanHitLine(Player.position, Player.width, Player.height, topLeft, Player.width, Player.height))
                    continue;

                return candidate;
            }

            return start;
        }

        private void TeleportPlayer(Vector2 destination, Vector2 dashDirection)
        {
            Player.Center = destination;
            Player.velocity = dashDirection * ExitVelocity;
            Player.fallStart = (int)(Player.position.Y / 16f);
            Player.immune = true;
            Player.immuneTime = Math.Max(Player.immuneTime, DashIFrames);
            Player.noKnockback = true;
            Player.direction = dashDirection.X >= 0f ? 1 : -1;

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.75f, Pitch = 0.28f }, destination);
        }

        private void SpawnDashLightningVisual(Vector2 start, Vector2 destination, Vector2 dashDirection)
        {
            int flags = AzureThunderFlatLightning.VisualOnlyFlag | AzureThunderFlatLightning.BigLightningFlag;
            AzureThunderPlayer.SpawnFlatLightning(
                Player.GetSource_FromThis(),
                start - dashDirection * 72f,
                destination - start,
                1,
                0f,
                Player.whoAmI,
                1.25f,
                flags);

            AzureThunderPlayer.SpawnFlatLightning(
                Player.GetSource_FromThis(),
                destination + dashDirection.RotatedBy(MathHelper.PiOver2) * 26f,
                start - destination,
                1,
                0f,
                Player.whoAmI,
                0.7f,
                AzureThunderFlatLightning.VisualOnlyFlag);
        }

        private void SpawnDashEndEffects(Vector2 destination, Vector2 dashDirection)
        {
            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = (-dashDirection).RotatedByRandom(0.8f) * Main.rand.NextFloat(2.5f, 8.5f);
                Dust dust = Dust.NewDustPerfect(
                    destination + Main.rand.NextVector2Circular(18f, 24f),
                    DustID.FireworksRGB,
                    velocity,
                    0,
                    Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                    Main.rand.NextFloat(0.85f, 1.45f));
                dust.noGravity = true;
            }
        }

        private void TryReleaseAzureThunderLightning(Vector2 dashDirection)
        {
            if (lightningCooldownTimer > 0)
                return;

            float effectRadius = AzureThunderAccessoryPlayer.GetGroundSwordEffectRadius(Player);
            int swordCount = AzureThunderPlayer.CountGroundSwordsNear(Player, Player.Center, effectRadius);
            int strikeCount = Utils.Clamp(Math.Max(1, swordCount), 1, 3);
            int damage = Math.Max(1, (int)(Player.GetWeaponDamage(Player.HeldItem) * 0.6f));
            float knockback = Player.GetWeaponKnockback(Player.HeldItem);
            bool harmony = Player.GetModPlayer<AzureThunderPlayer>().HarmonyActive;

            for (int i = 0; i < strikeCount; i++)
            {
                Vector2 launchDirection = dashDirection.RotatedBy((i - (strikeCount - 1) * 0.5f) * 0.18f);
                NPC target = FindDashLightningTarget(Player.Center, launchDirection);
                Vector2 destination = target?.Center ?? Player.Center + launchDirection * 900f;
                int flags = harmony ?
                    AzureThunderFlatLightning.StaticDischargeFlag :
                    AzureThunderFlatLightning.NoBaseElectricDebuffFlag;

                if (i == strikeCount - 1)
                    flags |= AzureThunderFlatLightning.BigLightningFlag;

                AzureThunderPlayer.SpawnFlatLightning(
                    Player.GetSource_FromThis(),
                    Player.Center - launchDirection * 48f,
                    destination - Player.Center,
                    damage,
                    knockback,
                    Player.whoAmI,
                    i == strikeCount - 1 ? 1.15f : 0.85f,
                    flags,
                    AzureThunderAccessoryPlayer.GetRightClickLightningEnergyGain(Player));
            }

            lightningCooldownDuration = LightningCooldownFrames;
            lightningCooldownTimer = lightningCooldownDuration;
            SyncLightningCooldownDisplay();
        }

        private static NPC FindDashLightningTarget(Vector2 source, Vector2 preferredDirection)
        {
            NPC bestTarget = null;
            float bestScore = 1450f;
            preferredDirection = preferredDirection.SafeNormalize(Vector2.UnitX);

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                Vector2 toTarget = npc.Center - source;
                float distance = toTarget.Length();
                if (distance > 1400f)
                    continue;

                if (!Collision.CanHitLine(source, 1, 1, npc.Center, 1, 1))
                    continue;

                float alignment = Vector2.Dot(toTarget.SafeNormalize(preferredDirection), preferredDirection);
                float score = distance - alignment * 260f;
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestTarget = npc;
            }

            return bestTarget;
        }

        private bool IsHoldingAzureThunder()
        {
            return Player.HeldItem != null &&
                !Player.HeldItem.IsAir &&
                Player.HeldItem.type == ModContent.ItemType<AzureThunder>();
        }

        private void SyncLightningCooldownDisplay()
        {
            if (!Equipped || lightningCooldownTimer <= 0)
                return;

            if (Player.Calamity().cooldowns.TryGetValue(JiDianFaLightningCooldown.ID, out var cooldown))
                cooldown.timeLeft = lightningCooldownTimer;
            else
                Player.AddCooldown(JiDianFaLightningCooldown.ID, lightningCooldownTimer);
        }
    }

    internal sealed class JiDianFaLightningCooldown : CooldownHandler
    {
        public static new string ID => "JoltingArts_Lightning";

        private JiDianFaPlayer DashPlayer => instance.player.GetModPlayer<JiDianFaPlayer>();
        private float AdjustedCompletion => DashPlayer.LightningCooldownCompletion;

        public override bool CanTickDown => false;
        public override bool ShouldDisplay => DashPlayer.Equipped && DashPlayer.LightningCoolingDown;

        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.JoltingArts_Lightning");

        public override string Texture => "CalamityLegendsComeBack/Accssory/TS/Jidianfa/EXCoolDown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Accssory/TS/Jidianfa/EXCoolDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Accssory/TS/Jidianfa/EXCoolDownOverlay";

        public override Color OutlineColor => new(16, 86, 122);
        public override Color CooldownStartColor => Color.Lerp(AzureThunderColors.Azure, Color.White, instance.Completion);
        public override Color CooldownEndColor => Color.Lerp(new Color(140, 255, 255), AzureThunderColors.PaleYellow, instance.Completion);

        public override void ApplyBarShaders(float opacity)
        {
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseOpacity(opacity);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseSaturation(AdjustedCompletion);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseColor(CooldownStartColor);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseSecondaryColor(CooldownEndColor);
            GameShaders.Misc["CalamityMod:CircularBarShader"].Apply();
        }

        public override void DrawExpanded(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            base.DrawExpanded(spriteBatch, position, opacity, scale);
            DrawSeconds(spriteBatch, position, opacity, scale);
        }

        public override void DrawCompact(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            Texture2D sprite = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D outline = ModContent.Request<Texture2D>(OutlineTexture).Value;
            Texture2D overlay = ModContent.Request<Texture2D>(OverlayTexture).Value;

            spriteBatch.Draw(outline, position, null, OutlineColor * opacity, 0f, outline.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(sprite, position, null, Color.White * opacity, 0f, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            int lostHeight = (int)Math.Ceiling(overlay.Height * AdjustedCompletion);
            Rectangle crop = new(0, lostHeight, overlay.Width, overlay.Height - lostHeight);
            spriteBatch.Draw(overlay, position + Vector2.UnitY * lostHeight * scale, crop, OutlineColor * opacity * 0.9f, 0f, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            DrawSeconds(spriteBatch, position, opacity, scale);
        }

        private void DrawSeconds(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            int displayValue = Math.Max(1, (int)Math.Ceiling(DashPlayer.LightningCooldownRemaining / 60f));
            Vector2 textOffset = new(displayValue > 9 ? -11f : -6f, 10f);
            DrawBorderStringEightWay(spriteBatch, FontAssets.MouseText.Value, displayValue.ToString(), position + textOffset * scale, Color.White * opacity, Color.Black * opacity, scale);
        }
    }
}
