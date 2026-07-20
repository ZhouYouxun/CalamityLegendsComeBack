using System;
using System.Collections.Generic;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using CalamityLegendsComeBack.Systems;
using CalamityLegendsComeBack.Weapons.SHPC;

namespace CalamityLegendsComeBack
{
    public class LegendaryCodex : ModItem, ILocalizedModType
    {
        private enum DropStuntState
        {
            Dormant,
            HoverCharge,
            Rising,
            SlamWindup,
            Slamming,
            ImpactFlash,
            Complete
        }

        private const int HoverChargeTime = 42;
        private const int RisingTime = 58;
        private const int SlamWindupTime = 16;
        private const int ImpactFlashTime = 34;
        private const float DropStuntLaunchSpeedSquared = 1.4f;

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityLegendsComeBack/LegendaryCodex";

        private DropStuntState dropStuntState;
        private int worldTimer;
        private int dropStuntTimer;
        private Vector2 dropStuntAnchor;
        private Vector2 previousWorldVelocity;
        private float dropStuntRotation;
        private float impactFlash;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: 1);
            Item.useTime = 12;
            Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<LegendaryCodexPanel>();
            Item.shootSpeed = 0f;
            Item.UseSound = null;
        }

        public override bool CanUseItem(Player player)
        {
            return Main.myPlayer == player.whoAmI &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !player.mouseInterface &&
                !(Main.playerInventory && Main.HoverItem.type == Type);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return LegendaryCodexPanel.OpenOrClose(player, source);
        }

        public override void OnSpawn(IEntitySource source)
        {
            ResetDropStunt();
        }

        public override void UpdateInventory(Player player)
        {
            ResetDropStunt();
        }

        private void ResetDropStunt()
        {
            dropStuntState = DropStuntState.Dormant;
            worldTimer = 0;
            dropStuntTimer = 0;
            dropStuntAnchor = Vector2.Zero;
            previousWorldVelocity = Vector2.Zero;
            dropStuntRotation = 0f;
            impactFlash = 0f;
        }

        public override void Update(ref float gravity, ref float maxFallSpeed)
        {
            worldTimer++;
            impactFlash = MathHelper.Clamp(impactFlash - 0.035f, 0f, 1f);

            if (dropStuntState == DropStuntState.Dormant)
            {
                if (worldTimer <= 16 && Item.velocity.LengthSquared() > DropStuntLaunchSpeedSquared)
                    StartDropStunt();

                previousWorldVelocity = Item.velocity;
                return;
            }

            if (dropStuntState == DropStuntState.Complete)
            {
                if (Item.noGrabDelay > 0 && Item.velocity.LengthSquared() > DropStuntLaunchSpeedSquared)
                {
                    StartDropStunt();
                    return;
                }

                previousWorldVelocity = Item.velocity;
                return;
            }

            gravity = 0f;
            maxFallSpeed = 52f;
            Item.noGrabDelay = Math.Max(Item.noGrabDelay, 8);

            if (dropStuntState == DropStuntState.Slamming && previousWorldVelocity.Y > 12f && Item.velocity.Y == 0f)
            {
                TriggerDropImpact();
                previousWorldVelocity = Item.velocity;
                return;
            }

            dropStuntTimer++;
            switch (dropStuntState)
            {
                case DropStuntState.HoverCharge:
                    UpdateHoverCharge();
                    break;

                case DropStuntState.Rising:
                    UpdateRising();
                    break;

                case DropStuntState.SlamWindup:
                    UpdateSlamWindup();
                    break;

                case DropStuntState.Slamming:
                    UpdateSlamming();
                    break;

                case DropStuntState.ImpactFlash:
                    UpdateImpactFlash();
                    break;
            }

            EmitDropStuntDust();
            previousWorldVelocity = Item.velocity;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = TextureAssets.Item[Type].Value;
            Vector2 drawPosition = Item.Center - Main.screenPosition;
            float activeOpacity = DropStuntActive ? 1f : impactFlash;

            if (activeOpacity <= 0f)
                return true;

            rotation = dropStuntRotation;
            float pulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 9f + Item.whoAmI);
            Color outline = Color.Lerp(new Color(52, 255, 190), new Color(255, 222, 98), pulse) * (0.46f + activeOpacity * 0.34f);
            Color deepOutline = new Color(18, 82, 68) * (0.35f * activeOpacity);

            DrawWorldOutline(spriteBatch, texture, drawPosition, rotation, scale, deepOutline, 5f + activeOpacity * 2f);
            DrawWorldOutline(spriteBatch, texture, drawPosition, rotation, scale, outline, 2f + pulse * 1.5f);
            // Disabled: the world-item scanner brackets can stretch into long screen-space lines.
            // DrawScannerBrackets(spriteBatch, drawPosition, texture.Size() * scale, outline * activeOpacity, pulse, activeOpacity);
            DrawDropStuntBeam(spriteBatch, drawPosition, texture.Height * scale, activeOpacity);
            return true;
        }

        private bool DropStuntActive =>
            dropStuntState is DropStuntState.HoverCharge or DropStuntState.Rising or DropStuntState.SlamWindup or DropStuntState.Slamming;

        private void StartDropStunt()
        {
            dropStuntState = DropStuntState.HoverCharge;
            dropStuntTimer = 0;
            dropStuntAnchor = Item.Center;
            previousWorldVelocity = Item.velocity;
            Item.velocity = Vector2.Zero;
            Item.noGrabDelay = Math.Max(Item.noGrabDelay, HoverChargeTime + RisingTime + SlamWindupTime + 20);

            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.62f, Pitch = 0.25f }, Item.Center);
        }

        private void UpdateHoverCharge()
        {
            float completion = dropStuntTimer / (float)HoverChargeTime;
            Vector2 hoverOffset = new(
                MathF.Sin(dropStuntTimer * 0.24f) * (2f + completion * 4f),
                MathF.Cos(dropStuntTimer * 0.19f) * 2.5f);

            Item.Center = Vector2.Lerp(Item.Center, dropStuntAnchor + hoverOffset, 0.34f);
            Item.velocity = Vector2.Zero;
            dropStuntRotation = MathHelper.Lerp(dropStuntRotation, MathF.Sin(dropStuntTimer * 0.18f) * 0.16f, 0.18f);

            if (dropStuntTimer == 16)
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.5f, Pitch = 0.45f }, Item.Center);

            if (dropStuntTimer >= HoverChargeTime)
            {
                dropStuntState = DropStuntState.Rising;
                dropStuntTimer = 0;
                Item.velocity = new Vector2(0f, -2.6f);
                SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.48f, Pitch = 0.55f }, Item.Center);
            }
        }

        private void UpdateRising()
        {
            float completion = dropStuntTimer / (float)RisingTime;
            Item.velocity.X *= 0.82f;
            Item.velocity.Y = MathHelper.Clamp(Item.velocity.Y - 0.16f - completion * 0.24f, -18.5f, -2.4f);
            dropStuntRotation += MathF.Sin(dropStuntTimer * 0.18f) * 0.018f;

            if (dropStuntTimer >= RisingTime)
            {
                dropStuntState = DropStuntState.SlamWindup;
                dropStuntTimer = 0;
                dropStuntAnchor = Item.Center;
                Item.velocity = Vector2.Zero;
                SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 0.42f, Pitch = -0.1f }, Item.Center);
            }
        }

        private void UpdateSlamWindup()
        {
            Vector2 jitter = Main.rand.NextVector2Circular(1.6f, 1.6f) * Utils.GetLerpValue(0f, SlamWindupTime, dropStuntTimer, true);
            Item.Center = Vector2.Lerp(Item.Center, dropStuntAnchor + jitter, 0.48f);
            Item.velocity = Vector2.Zero;
            dropStuntRotation = MathHelper.Lerp(dropStuntRotation, 0f, 0.26f);

            if (dropStuntTimer >= SlamWindupTime)
            {
                dropStuntState = DropStuntState.Slamming;
                dropStuntTimer = 0;
                Item.velocity = new Vector2(0f, 26f);
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.52f, Pitch = -0.15f }, Item.Center);
            }
        }

        private void UpdateSlamming()
        {
            Item.velocity.X *= 0.45f;
            Item.velocity.Y = MathHelper.Clamp(Item.velocity.Y + 2.85f, 26f, 52f);
            dropStuntRotation += 0.24f * Math.Sign(Item.velocity.Y);

            if (dropStuntTimer > 80)
                TriggerDropImpact();
        }

        private void UpdateImpactFlash()
        {
            Item.velocity *= 0.72f;
            dropStuntRotation = MathHelper.Lerp(dropStuntRotation, 0f, 0.18f);

            if (dropStuntTimer >= ImpactFlashTime)
                dropStuntState = DropStuntState.Complete;
        }

        private void TriggerDropImpact()
        {
            dropStuntState = DropStuntState.ImpactFlash;
            dropStuntTimer = 0;
            impactFlash = 1f;
            Item.velocity = Vector2.Zero;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.52f, Pitch = 0.2f }, Item.Center);
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = 0.34f, Pitch = 0.35f }, Item.Center);

            if (Main.LocalPlayer.Distance(Item.Center) < 900f)
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, 3.8f);

            for (int i = 0; i < 34; i++)
            {
                float angle = MathHelper.TwoPi * i / 34f;
                Vector2 velocity = new Vector2(MathF.Cos(angle) * 6f, MathF.Sin(angle) * 2.2f - 1.2f);
                Dust dust = Dust.NewDustPerfect(Item.Center + velocity * 2f, DustID.FireworkFountain_Blue, velocity, 80, ImpactColor(i / 34f), Main.rand.NextFloat(1.05f, 1.55f));
                dust.noGravity = true;
            }
        }

        private void EmitDropStuntDust()
        {
            if (Main.dedServ)
                return;

            Lighting.AddLight(Item.Center, new Vector3(0.12f, 0.48f, 0.36f) * (0.5f + impactFlash));

            if (dropStuntState == DropStuntState.HoverCharge && Main.rand.NextBool(3))
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(30f, 22f);
                Dust dust = Dust.NewDustPerfect(Item.Center + offset, DustID.Electric, -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.8f, 1.8f), 110, new Color(70, 255, 190), Main.rand.NextFloat(0.65f, 1.05f));
                dust.noGravity = true;
            }
            else if (dropStuntState == DropStuntState.Rising && Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Item.Center + Main.rand.NextVector2Circular(13f, 13f), DustID.GemEmerald, new Vector2(Main.rand.NextFloat(-0.7f, 0.7f), Main.rand.NextFloat(2.2f, 4.2f)), 120, new Color(88, 255, 210), Main.rand.NextFloat(0.75f, 1.15f));
                dust.noGravity = true;
            }
            else if (dropStuntState == DropStuntState.SlamWindup && Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Item.Center + Main.rand.NextVector2Circular(24f, 24f), DustID.GoldFlame, Main.rand.NextVector2Circular(1.1f, 1.1f), 80, new Color(255, 222, 98), Main.rand.NextFloat(0.65f, 1.0f));
                dust.noGravity = true;
            }
            else if (dropStuntState == DropStuntState.Slamming)
            {
                Dust dust = Dust.NewDustPerfect(Item.Center + Main.rand.NextVector2Circular(10f, 10f), DustID.FireworkFountain_Blue, new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-3.5f, -1.5f)), 90, new Color(100, 255, 220), Main.rand.NextFloat(0.9f, 1.35f));
                dust.noGravity = true;
            }
        }

        private static void DrawWorldOutline(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, float rotation, float scale, Color color, float radius)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * radius;
                spriteBatch.Draw(texture, position + offset, null, color, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
        }

        private static void DrawScannerBrackets(SpriteBatch spriteBatch, Vector2 center, Vector2 textureSize, Color color, float pulse, float opacity)
        {
            float width = textureSize.X + 28f + pulse * 8f;
            float height = textureSize.Y + 22f + pulse * 7f;
            float cornerLength = 11f + pulse * 6f;
            int thickness = 2;
            Vector2 topLeft = center - new Vector2(width, height) * 0.5f;
            Vector2 topRight = center + new Vector2(width, -height) * 0.5f;
            Vector2 bottomLeft = center + new Vector2(-width, height) * 0.5f;
            Vector2 bottomRight = center + new Vector2(width, height) * 0.5f;
            Color innerColor = new Color(122, 255, 220) * (0.42f * opacity);
            Color nodeColor = Color.Lerp(new Color(70, 255, 190), new Color(255, 226, 112), pulse) * (0.72f * opacity);

            DrawUiLine(spriteBatch, topLeft, Vector2.UnitX, cornerLength, thickness, color);
            DrawUiLine(spriteBatch, topLeft, Vector2.UnitY, cornerLength, thickness, color);
            DrawUiLine(spriteBatch, topRight, -Vector2.UnitX, cornerLength, thickness, color);
            DrawUiLine(spriteBatch, topRight, Vector2.UnitY, cornerLength, thickness, color);
            DrawUiLine(spriteBatch, bottomLeft, Vector2.UnitX, cornerLength, thickness, color);
            DrawUiLine(spriteBatch, bottomLeft, -Vector2.UnitY, cornerLength, thickness, color);
            DrawUiLine(spriteBatch, bottomRight, -Vector2.UnitX, cornerLength, thickness, color);
            DrawUiLine(spriteBatch, bottomRight, -Vector2.UnitY, cornerLength, thickness, color);

            float innerOffset = 7f + pulse * 3f;
            float innerLength = cornerLength * 0.56f;
            DrawScannerNode(spriteBatch, topLeft + new Vector2(innerOffset), Vector2.UnitX, Vector2.UnitY, innerLength, innerColor, nodeColor);
            DrawScannerNode(spriteBatch, topRight + new Vector2(-innerOffset, innerOffset), -Vector2.UnitX, Vector2.UnitY, innerLength, innerColor, nodeColor);
            DrawScannerNode(spriteBatch, bottomLeft + new Vector2(innerOffset, -innerOffset), Vector2.UnitX, -Vector2.UnitY, innerLength, innerColor, nodeColor);
            DrawScannerNode(spriteBatch, bottomRight - new Vector2(innerOffset), -Vector2.UnitX, -Vector2.UnitY, innerLength, innerColor, nodeColor);
        }

        private static void DrawScannerNode(SpriteBatch spriteBatch, Vector2 corner, Vector2 horizontal, Vector2 vertical, float length, Color lineColor, Color nodeColor)
        {
            DrawUiLine(spriteBatch, corner, horizontal, length, 1, lineColor);
            DrawUiLine(spriteBatch, corner, vertical, length, 1, lineColor);
            DrawUiLine(spriteBatch, corner + horizontal * (length * 0.45f), vertical, length * 0.48f, 1, lineColor * 0.72f);
            DrawUiLine(spriteBatch, corner + vertical * (length * 0.45f), horizontal, length * 0.48f, 1, lineColor * 0.72f);

            Rectangle node = new((int)corner.X - 2, (int)corner.Y - 2, 4, 4);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, node, nodeColor);
        }

        private static void DrawDropStuntBeam(SpriteBatch spriteBatch, Vector2 position, float textureHeight, float opacity)
        {
            if (opacity <= 0f)
                return;

            float height = 42f + 38f * opacity;
            Rectangle beam = new((int)position.X - 1, (int)(position.Y + textureHeight * 0.42f), 2, (int)height);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, beam, (new Color(72, 255, 196) with { A = 0 }) * (0.28f * opacity));
        }

        private static void DrawUiLine(SpriteBatch spriteBatch, Vector2 start, Vector2 direction, float length, int thickness, Color color)
        {
            Vector2 size = new(length, thickness);
            float rotation = direction.ToRotation();
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, start, null, color, rotation, Vector2.Zero, size, SpriteEffects.None, 0f);
        }

        private static Color ImpactColor(float completion)
        {
            return Color.Lerp(new Color(58, 255, 190), new Color(255, 222, 98), completion);
        }
    }

    internal sealed class LegendaryCodexPanel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private const int PreferredPanelWidth = 900;
        private const int PreferredPanelHeight = 560;
        private const int ScreenMargin = 12;
        private const int BorderThickness = 3;
        private const int TransitionDuration = 18;

        private static readonly LegendaryEntry[] Entries =
        {
            new("SHPC", () => ModContent.ItemType<NewLegendSHPC>(), true),
            new("ProjectileOutline", () => ModContent.ItemType<LegendaryCodex>(), true, "ProjectileOutline.Name"),
        };

        private Vector2 panelTopLeft;
        private bool panelPositionInitialized;
        private int entryIndex;
        private int previousEntryIndex;
        private int transitionTimer = TransitionDuration;
        private int transitionDirection = 1;
        private int lastHoveredControl = -1;

        public new string LocalizationCategory => "Projectiles";
        public override string Texture => "CalamityLegendsComeBack/LegendarySupplyBox";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static Rectangle MouseRectangle => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 2, 2);
        private static int PanelWidth => Math.Min(PreferredPanelWidth, Math.Max(620, Main.screenWidth - ScreenMargin * 2));
        private static int PanelHeight => Math.Min(PreferredPanelHeight, Math.Max(430, Main.screenHeight - ScreenMargin * 2));

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = PreferredPanelWidth;
            Projectile.height = PreferredPanelHeight;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.Opacity = 0f;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (owner.HeldItem.type != ModContent.ItemType<LegendaryCodex>())
                FadeOut = true;

            if (!panelPositionInitialized && Main.myPlayer == Projectile.owner)
            {
                Vector2 playerScreenCenter = owner.Center - Main.screenPosition;
                panelTopLeft = GetClampedPanelTopLeft(playerScreenCenter - new Vector2(PanelWidth, PanelHeight) * 0.5f);
                panelPositionInitialized = true;
            }

            if (transitionTimer < TransitionDuration)
                transitionTimer++;

            Vector2 panelCenter = panelTopLeft + new Vector2(PanelWidth, PanelHeight) * 0.5f;
            Projectile.Center = Main.myPlayer == Projectile.owner ? Main.screenPosition + panelCenter : owner.Center;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.14f : 0.18f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            Rectangle panelArea = new((int)panelTopLeft.X, (int)panelTopLeft.Y, PanelWidth, PanelHeight);
            Rectangle contentArea = new(panelArea.X + 38, panelArea.Y + 92, panelArea.Width - 76, panelArea.Height - 132);
            Rectangle leftArrowArea = new(contentArea.X + 10, contentArea.Y + contentArea.Height / 2 - 34, 54, 68);
            Rectangle rightArrowArea = new(contentArea.Right - 64, contentArea.Y + contentArea.Height / 2 - 34, 54, 68);
            Rectangle cardArea = new(contentArea.X + 82, contentArea.Y + 12, contentArea.Width - 164, contentArea.Height - 24);
            bool mouseOverPanel = panelArea.Intersects(MouseRectangle);
            bool leftClickPressed = Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClickPressed = Main.mouseRight && Main.mouseRightRelease;
            bool leftHovered = leftArrowArea.Intersects(MouseRectangle);
            bool rightHovered = rightArrowArea.Intersects(MouseRectangle);
            int hoveredControl = -1;

            DrawPanel(panelArea, Projectile.Opacity);
            DrawMatrixRain(panelArea, Projectile.Opacity);
            DrawHeader(panelArea, Projectile.Opacity);
            DrawContentFrame(contentArea, Projectile.Opacity);

            if (leftHovered)
            {
                hoveredControl = 1;
                if (leftClickPressed && Projectile.Opacity >= 0.95f)
                    ChangeEntry(-1);
            }
            else if (rightHovered)
            {
                hoveredControl = 2;
                if (leftClickPressed && Projectile.Opacity >= 0.95f)
                    ChangeEntry(1);
            }
            else if (mouseOverPanel && rightClickPressed && Projectile.Opacity >= 0.95f)
            {
                ChangeEntry(1);
            }

            DrawEntryCard(cardArea, Projectile.Opacity);
            DrawArrowButton(leftArrowArea, false, leftHovered, Projectile.Opacity);
            DrawArrowButton(rightArrowArea, true, rightHovered, Projectile.Opacity);
            DrawFooter(panelArea, Projectile.Opacity);
            PlayHoverSound(owner, hoveredControl);

            if (!mouseOverPanel && !FadeOut && Projectile.Opacity >= 0.95f && (leftClickPressed || rightClickPressed))
            {
                FadeOut = true;
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.05f }, owner.Center);
            }

            if (mouseOverPanel)
            {
                Main.blockMouse = true;
                owner.mouseInterface = true;
            }

            return false;
        }

        public static bool OpenOrClose(Player player, IEntitySource source)
        {
            if (TryCloseExistingPanel(player))
            {
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.05f }, player.Center);
                return false;
            }

            Projectile.NewProjectile(source, player.Center, Vector2.Zero, ModContent.ProjectileType<LegendaryCodexPanel>(), 0, 0f, player.whoAmI);
            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.68f, Pitch = 0.08f }, player.Center);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.32f, Pitch = 0.22f }, player.Center);
            return false;
        }

        private static bool TryCloseExistingPanel(Player player)
        {
            int panelType = ModContent.ProjectileType<LegendaryCodexPanel>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != panelType)
                    continue;

                if (projectile.ModProjectile is LegendaryCodexPanel panel)
                    panel.FadeOut = true;
                else
                    projectile.ai[0] = 1f;

                return true;
            }

            return false;
        }

        private void ChangeEntry(int direction)
        {
            previousEntryIndex = entryIndex;
            transitionDirection = direction;
            entryIndex = (entryIndex + direction + Entries.Length) % Entries.Length;
            transitionTimer = 0;
            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.66f, Pitch = direction > 0 ? 0.24f : 0.06f }, Main.LocalPlayer.Center);
        }

        private void DrawEntryCard(Rectangle cardArea, float opacity)
        {
            float transition = transitionTimer / (float)TransitionDuration;
            transition = transition * transition * (3f - 2f * transition);

            if (transitionTimer < TransitionDuration)
            {
                Rectangle previousArea = cardArea;
                previousArea.X -= (int)(transitionDirection * MathHelper.Lerp(0f, 52f, transition));
                DrawSingleEntry(Entries[previousEntryIndex], previousArea, opacity * (1f - transition));

                Rectangle nextArea = cardArea;
                nextArea.X += (int)(transitionDirection * MathHelper.Lerp(52f, 0f, transition));
                DrawSingleEntry(Entries[entryIndex], nextArea, opacity * transition);
                return;
            }

            DrawSingleEntry(Entries[entryIndex], cardArea, opacity);
        }

        private static void DrawSingleEntry(LegendaryEntry entry, Rectangle area, float opacity)
        {
            Color theme = entry.Unlocked ? new Color(72, 255, 184) : new Color(52, 92, 74);
            Color text = entry.Unlocked ? new Color(222, 255, 244) : new Color(70, 108, 92);
            DrawBorder(area, theme * (0.52f * opacity), 1);

            int itemType = entry.ItemType();
            Texture2D texture = TextureAssets.Item[itemType].Value;
            Rectangle iconArea = new(area.X + area.Width / 2 - 92, area.Y + 42, 184, 154);
            Vector2 iconCenter = iconArea.Center.ToVector2();
            float scale = Math.Min(1.45f, 132f / Math.Max(texture.Width, texture.Height));
            Color itemColor = entry.Unlocked ? Color.White * opacity : Color.Black * (0.88f * opacity);

            Main.EntitySpriteDraw(texture, iconCenter, texture.Frame(), itemColor, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            if (!entry.Unlocked)
                DrawCenteredText("?", new Rectangle(iconArea.X, iconArea.Y + 18, iconArea.Width, 96), new Color(78, 255, 164), 2.1f, 0.9f, opacity);

            Rectangle nameArea = new(area.X + 26, area.Y + 214, area.Width - 52, 36);
            string name = entry.Unlocked ? entry.GetDisplayName(itemType) : "???";
            DrawCenteredText(name, nameArea, text, 0.9f, 0.48f, opacity);

            Rectangle tagArea = new(area.X + 42, area.Y + 260, area.Width - 84, 28);
            DrawCenteredText(entry.Unlocked ? entry.GetLocalizedTag() : "???", tagArea, theme, 0.58f, 0.42f, opacity);

            Rectangle bodyArea = new(area.X + 48, area.Y + 304, area.Width - 96, 90);
            DrawWrappedText(entry.Unlocked ? entry.GetLocalizedBody() : "???\n???\n???", bodyArea, text, 0.58f, opacity);
        }

        private static void DrawHeader(Rectangle panelArea, float opacity)
        {
            DrawCenteredText("LEGENDARY CODEX", new Rectangle(panelArea.X + 34, panelArea.Y + 22, panelArea.Width - 68, 34), new Color(210, 255, 236), 1f, 0.62f, opacity);
            DrawCenteredText($"{Entries.Length:00} RECORDS // ACTIVE INDEX {(Main.GameUpdateCount / 6) % 100:00}", new Rectangle(panelArea.X + 34, panelArea.Y + 56, panelArea.Width - 68, 22), new Color(82, 255, 174), 0.5f, 0.36f, opacity);
        }

        private static void DrawFooter(Rectangle panelArea, float opacity)
        {
            DrawCenteredText("LEFT / RIGHT NODE SELECT // RMB NEXT", new Rectangle(panelArea.X + 34, panelArea.Bottom - 34, panelArea.Width - 68, 20), new Color(88, 180, 146), 0.48f, 0.36f, opacity);
        }

        private static void DrawArrowButton(Rectangle area, bool right, bool hovered, float opacity)
        {
            Color border = hovered ? new Color(160, 255, 216) : new Color(54, 188, 132);
            Color fill = hovered ? new Color(14, 48, 38, 230) : new Color(5, 20, 17, 210);
            DrawRectangle(area, fill * opacity);
            DrawBorder(area, border * opacity, 2);
            DrawCenteredText(right ? ">" : "<", area, hovered ? Color.White : new Color(92, 255, 178), 1.6f, 0.8f, opacity);
        }

        private static void DrawPanel(Rectangle panelArea, float opacity)
        {
            DrawRectangle(panelArea, new Color(2, 8, 7, 242) * opacity);
            DrawBorder(panelArea, new Color(70, 255, 188) * opacity, BorderThickness);
            Rectangle innerArea = new(panelArea.X + 9, panelArea.Y + 9, panelArea.Width - 18, panelArea.Height - 18);
            DrawBorder(innerArea, new Color(18, 98, 68, 210) * opacity, 1);

            for (int x = panelArea.X + 24; x < panelArea.Right - 24; x += 32)
                DrawRectangle(new Rectangle(x, panelArea.Y + 14, 1, panelArea.Height - 28), new Color(36, 108, 80, 70) * opacity);

            for (int y = panelArea.Y + 24; y < panelArea.Bottom - 24; y += 28)
                DrawRectangle(new Rectangle(panelArea.X + 14, y, panelArea.Width - 28, 1), new Color(36, 108, 80, 70) * opacity);
        }

        private static void DrawContentFrame(Rectangle area, float opacity)
        {
            DrawRectangle(area, new Color(4, 18, 15, 214) * opacity);
            DrawBorder(area, new Color(40, 190, 128, 180) * opacity, 1);
        }

        private static void DrawMatrixRain(Rectangle panelArea, float opacity)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Rectangle rainArea = new(panelArea.X + 18, panelArea.Y + 18, panelArea.Width - 36, panelArea.Height - 36);
            if (rainArea.Width <= 0 || rainArea.Height <= 0)
                return;

            string[] glyphs = { "01", "10", "SH", "PC", "EX", "[]", "//", ">>" };
            int columnCount = Math.Max(10, rainArea.Width / 30);
            float time = Main.GlobalTimeWrappedHourly;
            for (int column = 0; column < columnCount; column++)
            {
                float columnRatio = columnCount <= 1 ? 0f : column / (float)(columnCount - 1);
                float x = MathHelper.Lerp(rainArea.X + 6f, rainArea.Right - 24f, columnRatio) +
                    MathF.Sin(time * 2.4f + column * 1.73f) * 3f;
                int streamLength = 3 + column % 4;
                float lineStep = 17f;
                float speed = 24f + column % 5 * 6f;
                float loopHeight = rainArea.Height + streamLength * lineStep;
                float headY = rainArea.Y + ((time * speed + column * 37f) % loopHeight) - streamLength * lineStep;

                for (int row = 0; row < streamLength; row++)
                {
                    string glyph = glyphs[(column * 3 + row + (int)(Main.GameUpdateCount / 8)) % glyphs.Length];
                    float scale = 0.45f + (row == streamLength - 1 ? 0.08f : 0f);
                    Vector2 size = font.MeasureString(glyph) * scale;
                    Vector2 position = new(x, headY + row * lineStep);
                    if (position.X < rainArea.X || position.X + size.X > rainArea.Right ||
                        position.Y < rainArea.Y || position.Y + size.Y > rainArea.Bottom)
                    {
                        continue;
                    }

                    float rowFade = (row + 1f) / streamLength;
                    Color rainColor = Color.Lerp(new Color(34, 132, 92), new Color(118, 255, 204), rowFade) * (opacity * (0.1f + rowFade * 0.18f));
                    ChatManager.DrawColorCodedString(Main.spriteBatch, font, glyph, position, rainColor, 0f, Vector2.Zero, Vector2.One * scale);
                }
            }

            DrawMatrixCornerNode(panelArea, new Vector2(panelArea.X + 72, panelArea.Y + 72), opacity, time + 0.00f);
            DrawMatrixCornerNode(panelArea, new Vector2(panelArea.Right - 72, panelArea.Y + 72), opacity, time + 0.25f);
            DrawMatrixCornerNode(panelArea, new Vector2(panelArea.X + 72, panelArea.Bottom - 72), opacity, time + 0.50f);
            DrawMatrixCornerNode(panelArea, new Vector2(panelArea.Right - 72, panelArea.Bottom - 72), opacity, time + 0.75f);
        }

        private static void DrawMatrixCornerNode(Rectangle panelArea, Vector2 center, float opacity, float phase)
        {
            const int nodeSize = 42;
            Rectangle outer = new((int)center.X - nodeSize / 2, (int)center.Y - nodeSize / 2, nodeSize, nodeSize);
            Rectangle clipArea = Rectangle.Intersect(panelArea, outer);
            if (clipArea.Width <= 0 || clipArea.Height <= 0)
                return;

            float pulse = 0.5f + 0.5f * MathF.Sin(phase * MathHelper.TwoPi);
            Color border = Color.Lerp(new Color(54, 220, 154), new Color(166, 255, 224), pulse) * (0.32f * opacity);
            Color scan = new Color(92, 255, 190) * (0.45f * opacity);
            DrawBorder(outer, border, 1);
            DrawBorder(new Rectangle(outer.X + 7, outer.Y + 7, outer.Width - 14, outer.Height - 14), border * 0.65f, 1);

            int scanY = outer.Y + 5 + (int)((outer.Height - 10) * ((phase * 1.7f) % 1f));
            DrawRectangle(new Rectangle(outer.X + 5, scanY, outer.Width - 10, 1), scan);
            DrawMatrixCornerBrackets(clipArea, border * 0.55f);
        }

        private void PlayHoverSound(Player owner, int hoveredControl)
        {
            if (hoveredControl >= 0 && hoveredControl != lastHoveredControl && Projectile.Opacity >= 0.95f)
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.34f, Pitch = 0.16f }, owner.Center);

            lastHoveredControl = hoveredControl;
        }

        private static Vector2 GetClampedPanelTopLeft(Vector2 desiredTopLeft)
        {
            float maxX = Math.Max(ScreenMargin, Main.screenWidth - PanelWidth - ScreenMargin);
            float maxY = Math.Max(ScreenMargin, Main.screenHeight - PanelHeight - ScreenMargin);
            return new Vector2(MathHelper.Clamp(desiredTopLeft.X, ScreenMargin, maxX), MathHelper.Clamp(desiredTopLeft.Y, ScreenMargin, maxY));
        }

        private static void DrawWrappedText(string text, Rectangle area, Color color, float scale, float opacity)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            string[] lines = text.Replace("\r", string.Empty).Split('\n');
            int maxLines = Math.Max(1, (int)(area.Height / (font.LineSpacing * scale)));
            for (int i = 0; i < lines.Length && i < maxLines; i++)
            {
                Vector2 size = font.MeasureString(lines[i]) * scale;
                Vector2 position = new(area.Center.X - size.X * 0.5f, area.Y + i * font.LineSpacing * scale);
                DrawTextWithShadow(lines[i], position, color * opacity, scale, opacity);
            }
        }

        private static void DrawCenteredText(string text, Rectangle area, Color color, float maxScale, float minScale, float opacity)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 size = font.MeasureString(text);
            if (size.X <= 0f || size.Y <= 0f)
                return;

            float scale = maxScale;
            if (size.X * scale > area.Width)
                scale = area.Width / size.X;
            if (size.Y * scale > area.Height)
                scale = Math.Min(scale, area.Height / size.Y);

            scale = MathHelper.Clamp(scale, minScale, maxScale);
            Vector2 position = area.Center.ToVector2() - size * scale * 0.5f;
            DrawTextWithShadow(text, position, color * opacity, scale, opacity);
        }

        private static void DrawTextWithShadow(string text, Vector2 position, Color color, float scale, float opacity)
        {
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, text, position, color, 0f, Vector2.Zero, Vector2.One * scale);
        }

        private static void DrawRectangle(Rectangle rectangle, Color color)
        {
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rectangle, color);
        }

        private static void DrawMatrixCornerBrackets(Rectangle bounds, Color color)
        {
            const int inset = 7;
            const int length = 9;
            const int thickness = 1;
            if (bounds.Width < inset * 2 + length || bounds.Height < inset * 2 + length)
                return;

            int left = bounds.Left + inset;
            int top = bounds.Top + inset;
            int right = bounds.Right - inset;
            int bottom = bounds.Bottom - inset;

            DrawRectangle(new Rectangle(left, top, length, thickness), color);
            DrawRectangle(new Rectangle(left, top, thickness, length), color);
            DrawRectangle(new Rectangle(right - length, top, length, thickness), color);
            DrawRectangle(new Rectangle(right - thickness, top, thickness, length), color);
            DrawRectangle(new Rectangle(left, bottom - thickness, length, thickness), color);
            DrawRectangle(new Rectangle(left, bottom - length, thickness, length), color);
            DrawRectangle(new Rectangle(right - length, bottom - thickness, length, thickness), color);
            DrawRectangle(new Rectangle(right - thickness, bottom - length, thickness, length), color);
        }

        private static void DrawBorder(Rectangle rectangle, Color color, int thickness)
        {
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
            DrawRectangle(new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height), color);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
        }

        private readonly struct LegendaryEntry
        {
            public readonly string Key;
            public readonly Func<int> ItemType;
            public readonly bool Unlocked;
            public readonly string DisplayNameKey;

            public LegendaryEntry(string key, Func<int> itemType, bool unlocked, string displayNameKey = null)
            {
                Key = key;
                ItemType = itemType;
                Unlocked = unlocked;
                DisplayNameKey = displayNameKey;
            }

            public string GetDisplayName(int itemType) => DisplayNameKey is null
                ? Lang.GetItemNameValue(itemType)
                : Language.GetTextValue($"Mods.CalamityLegendsComeBack.LegendaryCodex.{DisplayNameKey}");

            public string GetLocalizedTag() => Language.GetTextValue($"Mods.CalamityLegendsComeBack.LegendaryCodex.{Key}.Tag");

            public string GetLocalizedBody() => Language.GetTextValue($"Mods.CalamityLegendsComeBack.LegendaryCodex.{Key}.Body");
        }
    }
}
