using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Systems;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    internal sealed class SHPCAmmoSelectionPanel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private const int MaxCandidates = NewLegendSHPC.MagazineCount;
        private const int SlotSize = 56;
        private const int InnerFrameSize = 44;
        private const int BorderThickness = 2;
        private const float MaxIconDrawSize = 40f;
        private const float DeadZone = 14f;

        private readonly int[] clickFeedbackTimers = new int[MaxCandidates];
        private readonly bool[] hoveredLastFrame = new bool[MaxCandidates];

        private Vector2 screenCenter;
        private bool positionInitialized;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.SHPC";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static Rectangle MouseRectangle => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 2, 2);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 160;
            Projectile.height = 160;
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

            if (owner.HeldItem.ModItem is not NewLegendSHPC)
                FadeOut = true;

            if (Main.myPlayer == Projectile.owner &&
                KeybindSystem.LegendaryWeaponFormSwitch?.Current != true &&
                KeybindSystem.LegendaryWeaponFormSwitch?.JustReleased != true)
            {
                FadeOut = true;
            }

            if (!positionInitialized && Main.myPlayer == Projectile.owner)
            {
                screenCenter = CLCBClientConfig.Instance?.LockAmmoWheelToScreenCenter == true
                    ? Main.ScreenSize.ToVector2() * 0.5f
                    : Main.MouseScreen;
                positionInitialized = true;
            }

            Projectile.Center = Main.myPlayer == Projectile.owner ? Main.screenPosition + screenCenter : owner.Center;
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
            if (owner.HeldItem.ModItem is not NewLegendSHPC weapon)
                return false;

            int activeSlotCount = weapon.GetActiveMagazineCount(owner);
            NewLegendSHPC.SHPCMagazineSlot[] slots = new NewLegendSHPC.SHPCMagazineSlot[activeSlotCount];
            for (int i = 0; i < activeSlotCount; i++)
                slots[i] = weapon.GetMagazineSlot(i, owner);

            int hoveredIndex = GetHoveredCandidateIndex(slots.Length);
            bool keyReleased = KeybindSystem.LegendaryWeaponFormSwitch?.JustReleased == true;

            DrawPanelRings(slots.Length, hoveredIndex);

            for (int i = 0; i < slots.Length; i++)
            {
                NewLegendSHPC.SHPCMagazineSlot slot = slots[i];
                Rectangle slotArea = GetSlotArea(i, slots.Length);
                bool hovered = i == hoveredIndex;

                if (hovered)
                {
                    Main.hoverItemName = GetHoverText(slot, owner);

                    if (!hoveredLastFrame[i] && Projectile.Opacity >= 0.92f)
                        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.42f, Pitch = 0.16f }, owner.Center);
                }

                DrawAmmoSlot(slot, slotArea, hovered, clickFeedbackTimers[i], Projectile.Opacity);

                hoveredLastFrame[i] = hovered;
                if (clickFeedbackTimers[i] > 0)
                    clickFeedbackTimers[i]--;
            }

            if (keyReleased && Projectile.Opacity > 0.08f)
            {
                if (hoveredIndex >= 0 && hoveredIndex < slots.Length)
                {
                    weapon.SelectMagazine(hoveredIndex, owner);
                    clickFeedbackTimers[hoveredIndex] = 10;
                    NewLegendSHPC.SHPCMagazineSlot selectedSlot = weapon.GetMagazineSlot(hoveredIndex, owner);
                    Color textColor = selectedSlot.IsConfigured ? GetEffectColor(selectedSlot.EffectID) : Color.LightGray;
                    CombatText.NewText(owner.Hitbox, textColor, GetMagazineSwitchText(selectedSlot), dramatic: false, dot: false);
                    SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.58f, Pitch = 0.08f }, owner.Center);
                }
                else
                {
                    SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.42f, Pitch = 0.2f }, owner.Center);
                }

                FadeOut = true;
            }

            if (Projectile.Opacity > 0.08f)
            {
                Main.blockMouse = true;
                owner.mouseInterface = true;
            }

            return false;
        }

        private int GetHoveredCandidateIndex(int candidateCount)
        {
            if (candidateCount <= 0)
                return -1;

            Vector2 mouseOffset = Main.MouseScreen - screenCenter;
            if (mouseOffset.LengthSquared() < DeadZone * DeadZone)
                return -1;

            Vector2 mouseDirection = mouseOffset.SafeNormalize(-Vector2.UnitY);
            int bestIndex = 0;
            float bestDot = -2f;

            for (int i = 0; i < candidateCount; i++)
            {
                Vector2 direction = GetSlotOffset(i, candidateCount).SafeNormalize(-Vector2.UnitY);
                float dot = Vector2.Dot(mouseDirection, direction);
                if (dot <= bestDot)
                    continue;

                bestDot = dot;
                bestIndex = i;
            }

            return bestIndex;
        }

        private Rectangle GetSlotArea(int index, int count)
        {
            Vector2 center = screenCenter + GetSlotOffset(index, count);
            return Utils.CenteredRectangle(center, new Vector2(SlotSize));
        }

        private static Vector2 GetSlotOffset(int index, int count)
        {
            if (count <= 1)
                return new Vector2(0f, -58f);

            if (count == 2)
                return index == 0 ? new Vector2(-50f, -22f) : new Vector2(50f, -22f);

            if (count == 3)
            {
                return index switch
                {
                    0 => new Vector2(0f, -64f),
                    1 => new Vector2(58f, 34f),
                    _ => new Vector2(-58f, 34f)
                };
            }

            float radius = count <= 4 ? 76f : 90f;
            float angle = -MathHelper.PiOver2 + MathHelper.TwoPi * index / count;
            return angle.ToRotationVector2() * radius;
        }

        private void DrawPanelRings(int candidateCount, int hoveredIndex)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color outer = new Color(92, 176, 255, 0) * (0.16f * Projectile.Opacity);
            Color inner = new Color(255, 238, 160, 0) * (0.12f * Projectile.Opacity);
            float pulse = 0.94f + 0.06f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f);

            Main.EntitySpriteDraw(bloom, screenCenter, null, outer, 0f, bloom.Size() * 0.5f, 0.42f * pulse, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, screenCenter, null, inner, 0f, bloom.Size() * 0.5f, 0.22f * pulse, SpriteEffects.None, 0f);

            if (candidateCount <= 1)
                return;

            Color lineColor = new Color(132, 190, 240, 0) * (0.26f * Projectile.Opacity);
            float radius = 86f;
            for (int i = 0; i < candidateCount; i++)
            {
                Vector2 current = GetSlotOffset(i, candidateCount).SafeNormalize(-Vector2.UnitY);
                Vector2 next = GetSlotOffset((i + 1) % candidateCount, candidateCount).SafeNormalize(-Vector2.UnitY);
                Vector2 boundary = (current + next).SafeNormalize(current);
                DrawScreenLine(screenCenter, screenCenter + boundary * radius, lineColor, 1.4f);
            }

            if (hoveredIndex < 0)
                return;

            Vector2 hoverDirection = GetSlotOffset(hoveredIndex, candidateCount).SafeNormalize(-Vector2.UnitY);
            DrawScreenLine(screenCenter, screenCenter + hoverDirection * (radius * 0.92f), Color.White * (0.3f * Projectile.Opacity), 3f);
        }

        private static void DrawAmmoSlot(NewLegendSHPC.SHPCMagazineSlot slot, Rectangle slotArea, bool hovered, int clickTimer, float opacity)
        {
            Color effectColor = slot.IsConfigured ? GetEffectColor(slot.EffectID) : new Color(86, 92, 104);
            Color slotBack = Color.Lerp(new Color(24, 28, 36), effectColor, 0.18f);
            Color slotBorder = Color.Lerp(new Color(112, 126, 150), effectColor, 0.42f);

            if (hovered)
            {
                slotBack = Color.Lerp(slotBack, new Color(74, 84, 102), 0.48f);
                slotBorder = Color.Lerp(slotBorder, Color.White, 0.35f);
            }

            if (clickTimer > 0)
            {
                slotBack = Color.Lerp(slotBack, new Color(132, 116, 70), 0.35f);
                slotBorder = Color.Lerp(slotBorder, new Color(255, 228, 150), 0.5f);
            }

            DrawRectangle(slotArea, slotBack * (opacity * 0.92f));
            DrawBorder(slotArea, slotBorder * opacity, BorderThickness);

            Rectangle innerArea = Utils.CenteredRectangle(slotArea.Center.ToVector2(), new Vector2(InnerFrameSize));
            DrawRectangle(innerArea, Color.Lerp(new Color(10, 12, 18), effectColor, 0.08f) * (opacity * 0.82f));
            DrawBorder(innerArea, slotBorder * (opacity * 0.68f), 1);

            if (!slot.IsConfigured)
            {
                CalamityUtils.DrawBorderStringEightWay(
                    Main.spriteBatch,
                    FontAssets.MouseText.Value,
                    (slot.Index + 1).ToString(),
                    slotArea.Center.ToVector2() - new Vector2(5f, 11f),
                    Color.Gray * opacity,
                    Color.Black * opacity,
                    0.8f);
                return;
            }

            Texture2D texture = TryGetAmmoTexture(slot.EffectID, slot.AmmoType);
            if (texture == null)
                return;

            Rectangle source = GetCurrentFrame(texture, GetFrameCount(slot.EffectID));
            Vector2 iconCenter = slotArea.Center.ToVector2();
            Vector2 sourceSize = source.Size();
            float fitScale = Math.Min(MaxIconDrawSize / Math.Max(1f, sourceSize.X), MaxIconDrawSize / Math.Max(1f, sourceSize.Y));
            float hoverScale = hovered ? 1.1f : 1f;
            float clickScale = clickTimer > 0 ? 1.08f : 1f;
            float bob = 1f + (hovered ? 0.025f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 10f) : 0f);
            Color iconColor = hovered ? Color.White : Color.Lerp(Color.White, effectColor, slot.HasAmmo ? 0.12f : 0.55f);

            Main.EntitySpriteDraw(
                texture,
                iconCenter,
                source,
                iconColor * opacity,
                hovered ? 0.03f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f) : 0f,
                sourceSize * 0.5f,
                fitScale * hoverScale * clickScale * bob,
                SpriteEffects.None,
                0f);

            CalamityUtils.DrawBorderStringEightWay(
                Main.spriteBatch,
                FontAssets.MouseText.Value,
                slot.Power.ToString(),
                new Vector2(slotArea.Right, slotArea.Bottom) - new Vector2(18f, 18f),
                (slot.HasAmmo ? Color.White : Color.LightGray) * opacity,
                Color.Black * opacity,
                0.55f);
        }

        internal static Rectangle GetCurrentFrame(Texture2D texture, int frameCount)
        {
            frameCount = Math.Max(1, frameCount);
            int frameHeight = Math.Max(1, texture.Height / frameCount);
            int frameIndex = frameCount <= 1 ? 0 : (int)((Main.GameUpdateCount / 6UL) % (ulong)frameCount);
            return new Rectangle(0, frameHeight * frameIndex, texture.Width, frameHeight);
        }

        internal static Texture2D TryGetAmmoTexture(int effectID, int ammoType)
        {
            string texturePath = GetTexturePath(effectID);
            if (!string.IsNullOrEmpty(texturePath))
            {
                try
                {
                    return ModContent.Request<Texture2D>(texturePath).Value;
                }
                catch
                {
                    // Fall back to the item texture below.
                }
            }

            return TextureAssets.Item[ammoType].Value;
        }

        private static string GetTexturePath(int effectID)
        {
            return effectID switch
            {
                1 => "CalamityMod/Items/Materials/EnergyCore",
                2 => "CalamityMod/Items/Materials/StormlionMandible",
                3 => "CalamityMod/Items/Materials/SulphuricScale",
                4 => "CalamityMod/Items/Materials/PurifiedGel",
                5 => "CalamityMod/Items/Materials/EssenceofHavoc",
                6 => "CalamityMod/Items/Materials/EssenceofEleum",
                7 => "CalamityMod/Items/Materials/EssenceofSunlight",
                8 => "CalamityMod/Items/Materials/StarblightSoot",
                9 => "Terraria/Images/Item_520",
                10 => "Terraria/Images/Item_521",
                11 => "Terraria/Images/Item_575",
                12 => "Terraria/Images/Item_547",
                13 => "Terraria/Images/Item_548",
                14 => "Terraria/Images/Item_549",
                15 => "CalamityMod/Items/Materials/LivingShard",
                17 => "CalamityMod/Items/Materials/DepthCells",
                18 => "CalamityMod/Items/Materials/PlagueCellCanister",
                19 => "CalamityMod/Items/Materials/AshesofCalamity",
                21 => "Terraria/Images/Item_3458",
                22 => "Terraria/Images/Item_3456",
                23 => "Terraria/Images/Item_3457",
                24 => "Terraria/Images/Item_3459",
                25 => "CalamityMod/Items/Materials/MeldBlob",
                26 => "CalamityMod/Items/Materials/UnholyEssence",
                28 => "CalamityMod/Items/Materials/DivineGeode",
                29 => "CalamityMod/Items/Materials/Bloodstone",
                30 => "CalamityMod/Items/Materials/RuinousSoul",
                31 => "CalamityMod/Items/Materials/Necroplasm",
                32 => "CalamityMod/Items/Materials/DarkPlasma",
                33 => "CalamityMod/Items/Materials/TwistingNether",
                34 => "CalamityMod/Items/Materials/EndothermicEnergy",
                35 => "CalamityMod/Items/Materials/NightmareFuel",
                36 => "CalamityMod/Items/Materials/AscendantSpiritEssence",
                37 => "CalamityMod/Items/Materials/YharonSoulFragment",
                38 => "CalamityMod/Items/Materials/ExoPrism",
                39 => "CalamityMod/Items/Materials/AshesofAnnihilation",
                40 => "CalamityMod/Items/Materials/ArmoredShell",
                41 => "CalamityMod/Items/Materials/PearlShard",
                42 => "CalamityMod/Items/Materials/DarksunFragment",
                43 => "CalamityMod/Items/LoreItems/LoreCynosure",
                44 => "CalamityMod/Items/Materials/CoreofCalamity",
                _ => null
            };
        }

        internal static int GetFrameCount(int effectID)
        {
            return effectID switch
            {
                2 => 8,
                9 or 10 or 11 or 12 or 13 or 14 => 4,
                19 => 5,
                26 => 7,
                30 or 31 or 34 or 35 or 36 or 39 => 6,
                32 => 4,
                42 => 8,
                _ => 1
            };
        }

        internal static Color GetEffectColor(int effectID)
        {
            Color color = EffectRegistry.GetEffectByID(effectID).ThemeColor;
            if (color == Color.Transparent || color == default)
                return new Color(120, 184, 255);

            return color;
        }

        private static string GetHoverText(NewLegendSHPC.SHPCMagazineSlot slot, Player owner)
        {
            if (!slot.IsConfigured)
                return $"{slot.Index + 1}号弹夹: 空";

            string itemName = Lang.GetItemNameValue(slot.AmmoType);
            int capacity = NewLegendSHPC.GetAdjustedAmmoCapacity(owner, slot.EffectID);
            return $"{slot.Index + 1}号弹夹: {Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.SHPCAmmoWheelHover", itemName, capacity)} ({slot.Power}/{capacity})";
        }

        private static string GetMagazineSwitchText(NewLegendSHPC.SHPCMagazineSlot slot)
        {
            if (!slot.IsConfigured)
                return $"{slot.Index + 1}号弹夹: 空";

            return $"{slot.Index + 1}号弹夹: {Lang.GetItemNameValue(slot.AmmoType)}";
        }

        private static void DrawScreenLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 edge = end - start;
            if (edge.LengthSquared() <= 0.001f)
                return;

            Main.EntitySpriteDraw(
                pixel,
                start,
                new Rectangle(0, 0, 1, 1),
                color,
                edge.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(edge.Length(), thickness),
                SpriteEffects.None,
                0f);
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
