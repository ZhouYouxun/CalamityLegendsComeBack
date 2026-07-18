using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityLegendsComeBack.Shader;
using CalamityLegendsComeBack.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Tools.DebugTools.ShaderLab
{
    public sealed class ShaderTestItem : ModItem, ILocalizedModType
    {
        public static readonly string DemoTexture = "Terraria/Images/Item_" + ItemID.LastPrism;

        private static int PanelType => ModContent.ProjectileType<ShaderTestPanel>();
        private static int TestProjectileType => ModContent.ProjectileType<ShaderTestProjectile>();

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => DemoTexture;

        public override bool AltFunctionUse(Player player) => true;

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            // 最终棱镜主题：随时间流转的彩虹色包边
            float hue = Main.GlobalTimeWrappedHourly * 0.35f % 1f;
            Color rainbow = Main.hslToRgb(hue, 1f, 0.6f);
            DebugToolOutline.Draw(spriteBatch, TextureAssets.Item[Type].Value, position, frame, origin, scale, rainbow);
            return true;
        }

        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 48;
            Item.damage = 1;
            Item.knockBack = 0f;
            Item.DamageType = DamageClass.Generic;
            Item.useTime = 18;
            Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.autoReuse = false;
            Item.shoot = TestProjectileType;
            Item.shootSpeed = 13f;
            Item.UseSound = null;
            Item.value = 0;
            Item.rare = ItemRarityID.Cyan;
            Item.Calamity().devItem = true;
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
            if (player.altFunctionUse == 2)
            {
                TogglePanel(player, source);
                return false;
            }

            ShaderTestPlayer shaderPlayer = player.GetModPlayer<ShaderTestPlayer>();
            if (!shaderPlayer.TryGetSelectedShader(out ShaderGames.ShaderDefinition shader))
                return false;

            if (shader.Category == ShaderCategory.Screen)
            {
                shaderPlayer.ToggleSelectedScreenShader();
                return false;
            }

            Projectile.NewProjectile(
                source,
                position,
                velocity,
                TestProjectileType,
                1,
                0f,
                player.whoAmI,
                shaderPlayer.SelectedCatalogIndex);

            return false;
        }

        private static void TogglePanel(Player player, IEntitySource source)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != PanelType)
                    continue;

                ShaderTestPanel.RequestClose(projectile);
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.05f }, player.Center);
                return;
            }

            Projectile.NewProjectile(source, player.Center, Vector2.Zero, PanelType, 0, 0f, player.whoAmI, 0f, Main.MouseScreen.X, Main.MouseScreen.Y);
            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.68f, Pitch = 0.08f }, player.Center);
        }
    }

    internal sealed class ShaderTestPlayer : ModPlayer
    {
        private int selectedCatalogIndex = -1;
        private string activeScreenRegistrationName;

        public int SelectedCatalogIndex
        {
            get
            {
                EnsureSelection();
                return selectedCatalogIndex;
            }
        }

        public bool TryGetSelectedShader(out ShaderGames.ShaderDefinition shader)
        {
            EnsureSelection();
            return ShaderGames.TryGetShaderByCatalogIndex(selectedCatalogIndex, out shader);
        }

        public void SelectShader(int catalogIndex)
        {
            if (!ShaderGames.TryGetShaderByCatalogIndex(catalogIndex, out _))
                return;

            DisableScreenShader();
            selectedCatalogIndex = catalogIndex;
        }

        public void ToggleSelectedScreenShader()
        {
            if (!TryGetSelectedShader(out ShaderGames.ShaderDefinition shader) || shader.Category != ShaderCategory.Screen)
                return;

            if (activeScreenRegistrationName == shader.RegistrationName)
            {
                DisableScreenShader();
                return;
            }

            DisableScreenShader();
            activeScreenRegistrationName = shader.RegistrationName;
            Player.ManageSpecialBiomeVisuals(ShaderGames.SceneFilterKey(activeScreenRegistrationName), true);
        }

        public override void UpdateDead()
        {
            DisableScreenShader();
        }

        public override void PostUpdate()
        {
            if (string.IsNullOrEmpty(activeScreenRegistrationName))
                return;

            bool shouldRemainActive =
                !Player.dead &&
                Player.HeldItem?.type == ModContent.ItemType<ShaderTestItem>() &&
                TryGetSelectedShader(out ShaderGames.ShaderDefinition shader) &&
                shader.Category == ShaderCategory.Screen &&
                shader.RegistrationName == activeScreenRegistrationName;

            if (!shouldRemainActive)
            {
                DisableScreenShader();
                return;
            }

            if (!Main.dedServ && Player.whoAmI == Main.myPlayer)
                Player.ManageSpecialBiomeVisuals(ShaderGames.SceneFilterKey(activeScreenRegistrationName), true);
        }

        private void EnsureSelection()
        {
            if (ShaderGames.TryGetShaderByCatalogIndex(selectedCatalogIndex, out _))
                return;

            selectedCatalogIndex = ShaderGames.FindCatalogIndex(ShaderCategory.Overlay, "ScanlineShader");
            if (selectedCatalogIndex < 0)
                selectedCatalogIndex = 0;
        }

        private void DisableScreenShader()
        {
            if (string.IsNullOrEmpty(activeScreenRegistrationName))
                return;

            if (!Main.dedServ && Player.whoAmI == Main.myPlayer)
            {
                Player.ManageSpecialBiomeVisuals(ShaderGames.SceneFilterKey(activeScreenRegistrationName), false);
                ShaderFullScreenSystem.DeactivateScreenShader(activeScreenRegistrationName);
            }

            activeScreenRegistrationName = null;
        }
    }

    internal sealed class ShaderTestProjectile : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => ShaderTestItem.DemoTexture;

        private int CatalogIndex => (int)Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            if (Projectile.velocity.LengthSquared() > 0.01f)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!ShaderGames.TryGetShaderByCatalogIndex(CatalogIndex, out ShaderGames.ShaderDefinition shader))
                return true;

            if (shader.Category is ShaderCategory.Trail or ShaderCategory.CalamityTrail)
                return false;

            if (shader.Category != ShaderCategory.Overlay)
                return true;

            Effect overlayEffect = ShaderGames.GetEffect(shader.Name);
            if (overlayEffect is null)
                return true;

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame();
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            ConfigureOverlayEffect(overlayEffect, texture, drawPosition);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                overlayEffect,
                Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(
                texture,
                drawPosition,
                frame,
                Color.White,
                Projectile.rotation,
                frame.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            if (!ShaderGames.TryGetShaderByCatalogIndex(CatalogIndex, out ShaderGames.ShaderDefinition shader) ||
                shader.Category is not (ShaderCategory.Trail or ShaderCategory.CalamityTrail))
            {
                return;
            }

            bool calamityTrail = shader.Category == ShaderCategory.CalamityTrail;
            MiscShaderData trailShader;
            bool foundShader = calamityTrail
                ? ShaderGames.TryGetCalamityMiscShader(shader.RegistrationName, out trailShader)
                : ShaderGames.TryGetMiscShader(shader.RegistrationName, out trailShader);
            if (!foundShader)
                return;

            Vector2[] trailPoints = BuildTrailPoints();
            if (trailPoints.Length < 2)
                return;

            if (!calamityTrail)
                ConfigureTrailEffect(ShaderGames.GetEffect(shader.Name));

            trailShader
                .SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"))
                .UseColor(new Color(255, 125, 35))
                .UseSecondaryColor(new Color(120, 210, 255))
                .UseOpacity(0.95f);

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    TrailWidth,
                    TrailColor,
                    (_, _) => Projectile.Size * 0.5f,
                    true,
                    true,
                    trailShader),
                trailPoints.Length * 2);
        }

        private Vector2[] BuildTrailPoints()
        {
            Vector2[] points = new Vector2[Projectile.oldPos.Length + 1];
            points[0] = Projectile.Center;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 oldPosition = Projectile.oldPos[i];
                points[i + 1] = oldPosition == Vector2.Zero
                    ? Projectile.Center - Projectile.velocity * (i + 1)
                    : oldPosition + Projectile.Size * 0.5f;
            }

            return points;
        }

        private static float TrailWidth(float completion, Vector2 _)
        {
            float body = Utils.GetLerpValue(1f, 0.08f, completion, true);
            return MathHelper.SmoothStep(24f, 4f, completion) * body;
        }

        private static Color TrailColor(float completion, Vector2 _)
        {
            Color hot = new(255, 160, 45, 0);
            Color cool = new(100, 210, 255, 0);
            float opacity = Utils.GetLerpValue(1f, 0.12f, completion, true);
            return Color.Lerp(hot, cool, completion) * opacity;
        }

        private static void ConfigureTrailEffect(Effect effect)
        {
            if (effect is null)
                return;

            SetFloat(effect, "uTime", Main.GlobalTimeWrappedHourly);
            SetFloat(effect, "uOpacity", 0.95f);
            SetFloat(effect, "uFlameIntensity", 0.55f);
            SetFloat(effect, "uBlurAmount", 0.08f);
            SetFloat(effect, "uSpeedFactor", 1f);
            SetFloat(effect, "uGhostFade", 0.7f);
            SetFloat(effect, "uWarpIntensity", 0.12f);
            SetFloat(effect, "uDistortionSpeed", 1f);
            SetFloat(effect, "uScanlineSpeed", 1.4f);
            SetFloat(effect, "uGlowIntensity", 1f);
            SetFloat(effect, "uSaturation", 1f);
            SetFloat(effect, "uRotation", 0f);
            SetFloat(effect, "uDirection", 1f);
        }

        private static void ConfigureOverlayEffect(Effect effect, Texture2D texture, Vector2 drawPosition)
        {
            SetFloat(effect, "uTime", Main.GlobalTimeWrappedHourly);
            SetFloat(effect, "uOpacity", 1f);
            SetFloat(effect, "uLineDensity", 92f);
            SetFloat(effect, "uGlowIntensity", 0.9f);
            SetFloat(effect, "uRefractStrength", 0.08f);
            SetFloat(effect, "uBurnSpeed", 1.5f);
            SetFloat(effect, "uIntensity", 0.85f);
            SetFloat(effect, "uThreshold", 0.16f);
            SetFloat(effect, "uWaveSpeed", 2.5f);
            SetFloat(effect, "uFlowIntensity", 0.04f);
            SetFloat(effect, "uFlowSpeed", 1.5f);
            SetFloat(effect, "uSegments", 6f);
            SetFloat(effect, "uBlockSize", 0.08f);
            SetFloat(effect, "uPulseSpeed", 2.5f);
            SetFloat(effect, "uGlowRange", 0.02f);
            SetFloat(effect, "uRadius", 0.45f);
            SetFloat(effect, "uStrength", 1.45f);
            SetFloat(effect, "uPixelSize", 5f);
            SetFloat(effect, "uDarkness", 0.7f);
            SetFloat(effect, "uDistortionStrength", 0.018f);

            SetVector2(effect, "uCenter", new Vector2(0.5f));
            SetVector2(effect, "uFlowDirection", new Vector2(1f, 0.35f));
            SetVector2(effect, "uResolution", texture.Size());
            SetVector2(effect, "uScreenResolution", new Vector2(Main.screenWidth, Main.screenHeight));
            SetVector2(effect, "uScreenPosition", drawPosition);
            SetVector3(effect, "uNeonColor", new Vector3(0.1f, 0.9f, 1f));
            SetVector4(effect, "uGlowColor", new Vector4(0.2f, 0.9f, 1f, 1f));
            SetVector4(effect, "uColor", new Vector4(1f));
        }

        private static void SetFloat(Effect effect, string parameterName, float value)
        {
            effect.Parameters[parameterName]?.SetValue(value);
        }

        private static void SetVector2(Effect effect, string parameterName, Vector2 value)
        {
            effect.Parameters[parameterName]?.SetValue(value);
        }

        private static void SetVector3(Effect effect, string parameterName, Vector3 value)
        {
            effect.Parameters[parameterName]?.SetValue(value);
        }

        private static void SetVector4(Effect effect, string parameterName, Vector4 value)
        {
            effect.Parameters[parameterName]?.SetValue(value);
        }
    }

    internal sealed class ShaderTestPanel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private const int PanelPadding = 14;
        private const int BorderThickness = 2;
        private const int RowHeight = 56;
        private const int RowGap = 12;
        private const int SlotWidth = 136;
        private const int SlotGap = 6;
        private const int PagerWidth = 28;
        private const int MaxPanelWidth = 1180;

        private static readonly ShaderCategory[] Categories =
        [
            ShaderCategory.Trail,
            ShaderCategory.CalamityTrail,
            ShaderCategory.Overlay,
            ShaderCategory.Screen
        ];

        private readonly int[] pageStarts = new int[Categories.Length];
        private Vector2 panelTopLeft;
        private bool panelPositionInitialized;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.A_Dev";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static int PanelWidth => Math.Min(MaxPanelWidth, Math.Max(520, Main.screenWidth - 24));
        private static int PanelHeight => PanelPadding * 2 + Categories.Length * RowHeight + (Categories.Length - 1) * RowGap;
        private static int VisibleColumnCount => Math.Max(1, (PanelWidth - PanelPadding * 2 - PagerWidth * 2 - SlotGap * 2 + SlotGap) / (SlotWidth + SlotGap));
        private static Rectangle MouseRectangle => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 2, 2);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
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

            if (owner.HeldItem.type != ModContent.ItemType<ShaderTestItem>())
                FadeOut = true;

            if (!panelPositionInitialized && Main.myPlayer == Projectile.owner)
            {
                Vector2 requestedTopLeft = Projectile.ai[1] != 0f || Projectile.ai[2] != 0f
                    ? new Vector2(Projectile.ai[1], Projectile.ai[2])
                    : Main.MouseScreen;
                panelTopLeft = GetClampedPanelTopLeft(requestedTopLeft);
                panelPositionInitialized = true;
            }

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
            ShaderTestPlayer shaderPlayer = owner.GetModPlayer<ShaderTestPlayer>();
            Rectangle panelArea = new((int)panelTopLeft.X, (int)panelTopLeft.Y, PanelWidth, PanelHeight);
            bool mouseOverPanel = panelArea.Intersects(MouseRectangle);
            bool leftClickPressed = Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClickPressed = Main.mouseRight && Main.mouseRightRelease;

            DrawPanel(panelArea, Projectile.Opacity);

            for (int row = 0; row < Categories.Length; row++)
            {
                ShaderCategory category = Categories[row];
                IReadOnlyList<ShaderGames.ShaderDefinition> shaders = ShaderGames.GetShaders(category);
                ClampPageStart(row, shaders.Count);

                Rectangle leftPager = GetLeftPagerArea(row);
                Rectangle rightPager = GetRightPagerArea(row);
                bool canPageLeft = pageStarts[row] > 0;
                bool canPageRight = pageStarts[row] + VisibleColumnCount < shaders.Count;

                DrawPager(leftPager, "<", CategoryColor(category), canPageLeft, leftPager.Intersects(MouseRectangle), Projectile.Opacity);
                DrawPager(rightPager, ">", CategoryColor(category), canPageRight, rightPager.Intersects(MouseRectangle), Projectile.Opacity);

                if (leftClickPressed && Projectile.Opacity >= 0.95f)
                {
                    if (canPageLeft && leftPager.Intersects(MouseRectangle))
                    {
                        pageStarts[row] = Math.Max(0, pageStarts[row] - VisibleColumnCount);
                        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.42f, Pitch = -0.08f }, owner.Center);
                    }
                    else if (canPageRight && rightPager.Intersects(MouseRectangle))
                    {
                        pageStarts[row] = Math.Min(Math.Max(0, shaders.Count - VisibleColumnCount), pageStarts[row] + VisibleColumnCount);
                        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.42f, Pitch = 0.08f }, owner.Center);
                    }
                }

                int visibleEnd = Math.Min(shaders.Count, pageStarts[row] + VisibleColumnCount);
                for (int index = pageStarts[row]; index < visibleEnd; index++)
                {
                    ShaderGames.ShaderDefinition shader = shaders[index];
                    Rectangle slotArea = GetSlotArea(row, index - pageStarts[row]);
                    bool hovered = slotArea.Intersects(MouseRectangle);
                    int catalogIndex = ShaderGames.GetCatalogIndex(category, index);
                    bool selected = shaderPlayer.SelectedCatalogIndex == catalogIndex;

                    if (hovered)
                    {
                        mouseOverPanel = true;
                        Main.hoverItemName = shader.Name;

                        if (leftClickPressed && Projectile.Opacity >= 0.95f)
                        {
                            shaderPlayer.SelectShader(catalogIndex);
                            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.48f, Pitch = 0.16f }, owner.Center);
                        }
                    }

                    DrawShaderSlot(shader.Name, slotArea, CategoryColor(category), selected, hovered, Projectile.Opacity);
                }
            }

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

        public static void RequestClose(Projectile projectile)
        {
            if (projectile.ModProjectile is ShaderTestPanel panel)
                panel.FadeOut = true;
            else
                projectile.ai[0] = 1f;
        }

        private static Vector2 GetClampedPanelTopLeft(Vector2 desiredTopLeft)
        {
            const float screenMargin = 12f;
            float maxX = Math.Max(screenMargin, Main.screenWidth - PanelWidth - screenMargin);
            float maxY = Math.Max(screenMargin, Main.screenHeight - PanelHeight - screenMargin);

            return new Vector2(
                MathHelper.Clamp(desiredTopLeft.X, screenMargin, maxX),
                MathHelper.Clamp(desiredTopLeft.Y, screenMargin, maxY));
        }

        private Rectangle GetLeftPagerArea(int row)
        {
            int x = (int)panelTopLeft.X + PanelPadding;
            int y = (int)panelTopLeft.Y + PanelPadding + row * (RowHeight + RowGap);
            return new Rectangle(x, y, PagerWidth, RowHeight);
        }

        private Rectangle GetRightPagerArea(int row)
        {
            int x = (int)panelTopLeft.X + PanelWidth - PanelPadding - PagerWidth;
            int y = (int)panelTopLeft.Y + PanelPadding + row * (RowHeight + RowGap);
            return new Rectangle(x, y, PagerWidth, RowHeight);
        }

        private Rectangle GetSlotArea(int row, int visibleColumn)
        {
            int x = (int)panelTopLeft.X + PanelPadding + PagerWidth + SlotGap + visibleColumn * (SlotWidth + SlotGap);
            int y = (int)panelTopLeft.Y + PanelPadding + row * (RowHeight + RowGap);
            return new Rectangle(x, y, SlotWidth, RowHeight);
        }

        private void ClampPageStart(int row, int shaderCount)
        {
            pageStarts[row] = Math.Clamp(pageStarts[row], 0, Math.Max(0, shaderCount - VisibleColumnCount));
        }

        private static void DrawPanel(Rectangle panelArea, float opacity)
        {
            DrawRectangle(panelArea, new Color(12, 14, 20, 232) * opacity);
            DrawBorder(panelArea, new Color(94, 110, 132) * opacity, BorderThickness);

            for (int row = 0; row < Categories.Length; row++)
            {
                int y = panelArea.Y + PanelPadding + row * (RowHeight + RowGap);
                DrawRectangle(
                    new Rectangle(panelArea.X + PanelPadding, y + RowHeight - 3, panelArea.Width - PanelPadding * 2, 3),
                    CategoryColor(Categories[row]) * (opacity * 0.78f));
            }
        }

        private static void DrawShaderSlot(string shaderName, Rectangle slotArea, Color categoryColor, bool selected, bool hovered, float opacity)
        {
            Color backColor = selected
                ? Color.Lerp(new Color(32, 38, 48), categoryColor, 0.34f)
                : new Color(42, 46, 56);
            Color borderColor = selected
                ? Color.Lerp(categoryColor, Color.White, 0.34f)
                : new Color(112, 120, 136);

            if (hovered)
            {
                backColor = Color.Lerp(backColor, new Color(76, 86, 104), 0.58f);
                borderColor = Color.Lerp(borderColor, Color.White, 0.34f);
            }

            DrawRectangle(slotArea, backColor * (opacity * 0.96f));
            DrawBorder(slotArea, borderColor * opacity, selected ? 3 : 2);

            Rectangle textArea = new(slotArea.X + 7, slotArea.Y + 4, slotArea.Width - 14, slotArea.Height - 8);
            DrawFitText(shaderName, textArea, Color.White, 0.72f, 0.34f, opacity);
        }

        private static void DrawPager(Rectangle area, string symbol, Color categoryColor, bool enabled, bool hovered, float opacity)
        {
            Color backColor = enabled ? new Color(42, 46, 56) : new Color(28, 30, 36);
            Color borderColor = enabled ? categoryColor : new Color(68, 72, 82);
            Color textColor = enabled ? Color.White : new Color(112, 116, 126);

            if (enabled && hovered)
                backColor = Color.Lerp(backColor, new Color(82, 92, 108), 0.58f);

            DrawRectangle(area, backColor * (opacity * 0.94f));
            DrawBorder(area, borderColor * opacity, 2);
            DrawFitText(symbol, new Rectangle(area.X + 5, area.Y + 4, area.Width - 10, area.Height - 8), textColor, 0.88f, 0.58f, opacity);
        }

        private static Color CategoryColor(ShaderCategory category)
        {
            return category switch
            {
                ShaderCategory.Trail => new Color(255, 152, 72),
                ShaderCategory.CalamityTrail => new Color(255, 96, 120),
                ShaderCategory.Overlay => new Color(88, 218, 202),
                ShaderCategory.Screen => new Color(172, 126, 255),
                _ => Color.White
            };
        }

        private static void DrawFitText(string text, Rectangle area, Color color, float maxScale, float minScale, float opacity)
        {
            if (string.IsNullOrWhiteSpace(text) || area.Width <= 0 || area.Height <= 0)
                return;

            var font = FontAssets.MouseText.Value;
            Vector2 size = font.MeasureString(text);
            if (size.X <= 0f || size.Y <= 0f)
                return;

            float scale = Math.Min(maxScale, Math.Min(area.Width / size.X, area.Height / size.Y));
            scale = MathHelper.Clamp(scale, minScale, maxScale);
            Vector2 position = new(
                area.X + Math.Max(0f, (area.Width - size.X * scale) * 0.5f),
                area.Y + Math.Max(0f, (area.Height - size.Y * scale) * 0.5f));

            CalamityUtils.DrawBorderStringEightWay(
                Main.spriteBatch,
                FontAssets.MouseText.Value,
                text,
                position,
                color * opacity,
                Color.Black * (0.76f * opacity),
                scale);
        }

        private static void DrawRectangle(Rectangle rectangle, Color color)
        {
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rectangle, color);
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
    }
}
