using System;
using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder;
using CalamityMod;
using CalamityMod.Cooldowns;
using CalamityMod.Items.Accessories.Wings;
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

namespace CalamityLegendsComeBack.Accssory.TS
{
    internal enum AzureThunderDashTier
    {
        None,
        QingDianFa,
        JiDianFa,
        QingTingJue,
        WanJunFengLei
    }

    internal abstract class AzureThunderDashAccessory : ModItem
    {
        protected abstract AzureThunderDashTier DashTier { get; }
        protected virtual int FlightTime => 0;
        protected virtual float FlightSpeed => 0f;
        protected virtual float FlightAcceleration => 0f;

        public new string LocalizationCategory => "Items";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 16);
            Item.rare = DashTier switch
            {
                AzureThunderDashTier.QingDianFa => ItemRarityID.Green,
                AzureThunderDashTier.JiDianFa => ItemRarityID.LightRed,
                AzureThunderDashTier.QingTingJue => ItemRarityID.Cyan,
                _ => ItemRarityID.Red
            };

            // Reuse Calamity's Elysian wing visual until these accessories receive their own art.
            if (FlightTime > 0)
                Item.wingSlot = ModContent.GetInstance<ElysianWings>().Item.wingSlot;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<AzureThunderDashPlayer>().Equip(DashTier);
        }

        public override void HorizontalWingSpeeds(Player player, ref float speed, ref float acceleration)
        {
            if (FlightTime <= 0)
                return;

            speed = FlightSpeed;
            acceleration = FlightAcceleration;
        }

        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            if (FlightTime <= 0)
                return;

            if (DashTier == AzureThunderDashTier.QingTingJue)
            {
                ascentWhenFalling = 1f;
                ascentWhenRising = 0.17f;
                maxCanAscendMultiplier = 1.2f;
                maxAscentMultiplier = 3f;
                constantAscend = 0.15f;
                return;
            }

            ascentWhenFalling = 0.95f;
            ascentWhenRising = 0.16f;
            maxCanAscendMultiplier = 1.2f;
            maxAscentMultiplier = 2.9f;
            constantAscend = 0.145f;
        }
    }

    internal sealed class AzureThunderDashPlayer : ModPlayer
    {
        private const int DoubleTapInputWindow = 15;
        private const int DashCooldownFrames = 40;
        private const int DashIFrames = 12;
        private const float TerrariaVelocityToMph = 216000f / 42240f;
        private const float DashSpeedMph = 128f;

        private int doubleTapTimer;
        private int lastTapDirection;
        private int dashCooldownTimer;
        private int lightningCooldownTimer;
        private int lightningCooldownDuration;
        private int invulnerabilityTimer;
        private int dashFramesRemaining;
        private Vector2 dashVelocity;

        public AzureThunderDashTier EquippedTier { get; private set; }
        public bool LightningCoolingDown => lightningCooldownTimer > 0;
        public int LightningCooldownRemaining => lightningCooldownTimer;
        public float LightningCooldownCompletion => !LightningCoolingDown ? 1f : 1f - lightningCooldownTimer / (float)Math.Max(1, lightningCooldownDuration);

        public void Equip(AzureThunderDashTier tier)
        {
            if (tier > EquippedTier)
                EquippedTier = tier;
        }

        public override void ResetEffects()
        {
            EquippedTier = AzureThunderDashTier.None;
        }

        public override void UpdateDead()
        {
            doubleTapTimer = 0;
            lastTapDirection = 0;
            dashCooldownTimer = 0;
            lightningCooldownTimer = 0;
            lightningCooldownDuration = 0;
            invulnerabilityTimer = 0;
            dashFramesRemaining = 0;
            dashVelocity = Vector2.Zero;
        }

        public override void PostUpdateEquips()
        {
            if (EquippedTier != AzureThunderDashTier.None)
                ApplyAccessoryStats();
        }

        public override void PostUpdate()
        {
            if (dashFramesRemaining > 0)
            {
                Player.velocity = dashVelocity;
                Player.direction = dashVelocity.X >= 0f ? 1 : -1;
                dashFramesRemaining--;
            }

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
            if (Main.myPlayer != Player.whoAmI || !CanReadDashInput() || Player.controlLeft && Player.controlRight)
                return;

            int dashDirection = 0;
            if (Player.controlLeft && Player.releaseLeft)
                dashDirection = ProcessDoubleTap(-1);
            if (Player.controlRight && Player.releaseRight)
                dashDirection = ProcessDoubleTap(1);

            if (dashDirection != 0)
                StartLightningDash(dashDirection);
        }

        private void ApplyAccessoryStats()
        {
            (float movementSpeed, float jumpSpeed, float accelerationMultiplier, float maxMph, float wingMultiplier, int flightTime) = EquippedTier switch
            {
                AzureThunderDashTier.QingDianFa => (0.15f, 0.75f, 1.27f, 36f, 0f, 0),
                AzureThunderDashTier.JiDianFa => (0.24f, 1.2f, 1.36f, 42f, 1.15f, 0),
                AzureThunderDashTier.QingTingJue => (0.27f, 1.35f, 1.36f, 45f, 1.25f, 200),
                AzureThunderDashTier.WanJunFengLei => (0.36f, 1.8f, 1.45f, 49f, 1.5f, 270),
                _ => default
            };

            Player.moveSpeed += movementSpeed;
            Player.jumpSpeedBoost += jumpSpeed;
            Player.runAcceleration *= accelerationMultiplier;

            float maxRunSpeed = maxMph / TerrariaVelocityToMph;
            Player.maxRunSpeed = Math.Max(Player.maxRunSpeed, maxRunSpeed);
            Player.accRunSpeed = Math.Max(Player.accRunSpeed, maxRunSpeed);

            if (flightTime > 0)
                Player.wingTimeMax = Math.Max(Player.wingTimeMax, flightTime);
            else if (wingMultiplier > 0f && Player.wingTimeMax > 0)
                Player.wingTimeMax = (int)Math.Ceiling(Player.wingTimeMax * wingMultiplier);
        }

        private bool CanReadDashInput()
        {
            return EquippedTier != AzureThunderDashTier.None &&
                dashCooldownTimer <= 0 &&
                Player.active &&
                !Player.dead &&
                !Player.CCed &&
                !Player.mount.Active;
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

        private void StartLightningDash(int tapDirection)
        {
            Vector2 dashDirection = ResolveDashDirection(tapDirection);
            Vector2 start = Player.Center;
            Vector2 destination = ResolveDashDestination(start, dashDirection, GetDashDistance());
            if (Vector2.DistanceSquared(start, destination) < 16f * 16f)
                return;

            SpawnDashLightningVisual(start, destination, dashDirection);
            BeginLightningDash(destination, dashDirection);
            dashCooldownTimer = DashCooldownFrames;
            invulnerabilityTimer = Math.Max(DashIFrames, dashFramesRemaining);

            if (IsHoldingAzureThunder())
                TryReleaseAzureThunderLightning(start, destination, dashDirection);
        }

        private Vector2 ResolveDashDirection(int tapDirection)
        {
            return Vector2.UnitX * tapDirection;
        }

        private float GetDashDistance()
        {
            int tiles = EquippedTier switch
            {
                AzureThunderDashTier.QingDianFa => 9,
                AzureThunderDashTier.JiDianFa => 18,
                AzureThunderDashTier.QingTingJue => 27,
                AzureThunderDashTier.WanJunFengLei => 36,
                _ => 0
            };

            return tiles * 16f;
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

        private void BeginLightningDash(Vector2 destination, Vector2 dashDirection)
        {
            float dashSpeed = DashSpeedMph / TerrariaVelocityToMph;
            dashVelocity = dashDirection * dashSpeed;
            dashFramesRemaining = Math.Max(1, (int)Math.Ceiling(Vector2.Distance(Player.Center, destination) / dashSpeed));
            Player.velocity = dashVelocity;
            Player.fallStart = (int)(Player.position.Y / 16f);
            Player.immune = true;
            Player.immuneTime = Math.Max(Player.immuneTime, dashFramesRemaining);
            Player.noKnockback = true;
            Player.direction = dashDirection.X >= 0f ? 1 : -1;
            Player.Calamity().GeneralScreenShakePower = Math.Max(Player.Calamity().GeneralScreenShakePower, 2.5f);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.7f, Pitch = 0.22f }, Player.Center);
        }

        private void SpawnDashLightningVisual(Vector2 start, Vector2 destination, Vector2 dashDirection)
        {
            int flags = AzureThunderFlatLightning.VisualOnlyFlag | AzureThunderFlatLightning.SpeedLineFlag;
            AzureThunderPlayer.SpawnFlatLightning(
                Player.GetSource_FromThis(),
                start,
                destination - start,
                1,
                0f,
                Player.whoAmI,
                0.58f,
                flags);
        }

        private void SpawnDashEndEffects(Vector2 destination, Vector2 dashDirection)
        {
            for (int i = 0; i < 22; i++)
            {
                Vector2 velocity = (-dashDirection).RotatedByRandom(0.8f) * Main.rand.NextFloat(2.5f, 9f);
                Dust dust = Dust.NewDustPerfect(destination + Main.rand.NextVector2Circular(18f, 24f), DustID.FireworksRGB, velocity, 0, Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure, Main.rand.NextFloat(0.85f, 1.45f));
                dust.noGravity = true;
            }
        }

        private void TryReleaseAzureThunderLightning(Vector2 pathStart, Vector2 pathEnd, Vector2 dashDirection)
        {
            if (lightningCooldownTimer > 0)
                return;

            float radius = AzureThunderAccessoryPlayer.GetGroundSwordEffectRadius(Player);
            int nearbySwordCount = AzureThunderPlayer.CountGroundSwordsNear(Player, pathStart, radius);
            float knockback = Player.GetWeaponKnockback(Player.HeldItem);
            switch (EquippedTier)
            {
                case AzureThunderDashTier.QingDianFa:
                    ReleaseTargetedLightning(1, 30, knockback, dashDirection, AzureThunderFlatLightning.ShortStaticDischargeFlag, applyStaticDischarge: true);
                    break;

                case AzureThunderDashTier.JiDianFa:
                    ReleaseTargetedLightning(Math.Clamp(Math.Max(1, nearbySwordCount), 1, 3), 50, knockback, dashDirection, AzureThunderFlatLightning.ShortElectrifiedFlag);
                    break;

                case AzureThunderDashTier.QingTingJue:
                    ReleasePathLightning(pathStart, pathEnd, 3 + nearbySwordCount / 3, 6, 270, knockback, AzureThunderFlatLightning.VermillionFluxFlag);
                    Player.GetModPlayer<AzureThunderPlayer>().ArmDashHeavyStrike();
                    break;

                case AzureThunderDashTier.WanJunFengLei:
                    ReleasePathLightning(pathStart, pathEnd, 3 + nearbySwordCount, 9, 360, knockback, AzureThunderFlatLightning.AuricRebukeFlag);
                    Player.GetModPlayer<AzureThunderPlayer>().ArmDashHeavyStrike();
                    break;
            }

            lightningCooldownDuration = EquippedTier == AzureThunderDashTier.QingDianFa ? 12 * 60 : 9 * 60;
            lightningCooldownTimer = lightningCooldownDuration;
            SyncLightningCooldownDisplay();
        }

        private void ReleaseTargetedLightning(int strikeCount, int damage, float knockback, Vector2 dashDirection, int additionalFlags, bool applyStaticDischarge = false)
        {
            for (int i = 0; i < strikeCount; i++)
            {
                Vector2 strikeDirection = dashDirection.RotatedBy((i - (strikeCount - 1) * 0.5f) * 0.18f);
                NPC target = FindDashLightningTarget(Player.Center, strikeDirection);
                if (target == null)
                    continue;

                AzureThunderPlayer.SpawnVerticalLightning(
                    Player.GetSource_FromThis(),
                    target.Center,
                    target,
                    damage,
                    knockback,
                    Player.whoAmI,
                    applyStaticDischarge: applyStaticDischarge,
                    big: strikeCount > 1 && i == strikeCount - 1,
                    applyBaseElectricDebuff: !applyStaticDischarge,
                    fixedTiltRadians: MathHelper.ToRadians(strikeDirection.X < 0f ? 10f : -10f),
                    normalVisualIntensity: true,
                    lightningScale: strikeCount > 1 && i == strikeCount - 1 ? 1.12f : 0.9f,
                    additionalFlags: additionalFlags);
            }
        }

        private void ReleasePathLightning(Vector2 pathStart, Vector2 pathEnd, int requestedCount, int maximumCount, int damage, float knockback, int additionalFlags)
        {
            int strikeCount = Math.Clamp(requestedCount, 3, maximumCount);
            Vector2 path = pathEnd - pathStart;
            float tilt = MathHelper.ToRadians(path.X < 0f ? 8f : -8f);

            for (int i = 0; i < strikeCount; i++)
            {
                float progress = (i + 1f) / (strikeCount + 1f);
                Vector2 impactPosition = Vector2.Lerp(pathStart, pathEnd, progress);
                impactPosition += path.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(-14f, 14f);

                AzureThunderPlayer.SpawnVerticalLightning(
                    Player.GetSource_FromThis(),
                    impactPosition,
                    null,
                    damage,
                    knockback,
                    Player.whoAmI,
                    big: true,
                    applyBaseElectricDebuff: false,
                    fixedTiltRadians: tilt,
                    normalVisualIntensity: true,
                    lightningScale: 1.2f,
                    additionalFlags: additionalFlags);
            }
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
                if (distance > 1400f || !Collision.CanHitLine(source, 1, 1, npc.Center, 1, 1))
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
            if (EquippedTier == AzureThunderDashTier.None || lightningCooldownTimer <= 0)
                return;

            if (Player.Calamity().cooldowns.TryGetValue(AzureThunderDashLightningCooldown.ID, out var cooldown))
                cooldown.timeLeft = lightningCooldownTimer;
            else
                Player.AddCooldown(AzureThunderDashLightningCooldown.ID, lightningCooldownTimer);
        }
    }

    internal sealed class AzureThunderDashLightningCooldown : CooldownHandler
    {
        public static new string ID => "AzureThunderDash_Lightning";

        private AzureThunderDashPlayer DashPlayer => instance.player.GetModPlayer<AzureThunderDashPlayer>();
        private float AdjustedCompletion => DashPlayer.LightningCooldownCompletion;

        public override bool CanTickDown => false;
        public override bool ShouldDisplay => DashPlayer.EquippedTier != AzureThunderDashTier.None && DashPlayer.LightningCoolingDown;
        public override LocalizedText DisplayName => Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.AzureThunderDash_Lightning");
        public override string Texture => "CalamityLegendsComeBack/Accssory/TS/JiDianFa/EXCoolDown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Accssory/TS/JiDianFa/EXCoolDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Accssory/TS/JiDianFa/EXCoolDownOverlay";

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
            CalamityUtils.DrawBorderStringEightWay(spriteBatch, FontAssets.MouseText.Value, displayValue.ToString(), position + textOffset * scale, Color.White * opacity, Color.Black * opacity, scale);
        }
    }
}
