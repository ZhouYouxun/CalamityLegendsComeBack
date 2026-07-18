using System;
using System.Collections.Generic;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Tools.DebugTools.PreDrawLab
{
    public sealed class PreDrawLabItem : ModItem, ILocalizedModType
    {
        public static readonly string DemoTexture = "Terraria/Images/Item_" + ItemID.Paintbrush;

        private static int PanelType => ModContent.ProjectileType<PreDrawLabPanel>();
        private static int TestProjectileType => ModContent.ProjectileType<PreDrawLabProjectile>();

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => DemoTexture;

        public override bool AltFunctionUse(Player player) => true;

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            DebugToolOutline.Draw(spriteBatch, TextureAssets.Item[Type].Value, position, frame, origin, scale, new Color(255, 150, 86));
            return true;
        }

        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 48;
            Item.damage = 0;
            Item.knockBack = 0f;
            Item.DamageType = DamageClass.Generic;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = TestProjectileType;
            Item.shootSpeed = 13f;
            Item.UseSound = SoundID.Item1;
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

            int selectedEffectIndex = player.GetModPlayer<PreDrawLabPlayer>().SelectedEffectIndex;
            Projectile.NewProjectile(source, player.MountedCenter, velocity, TestProjectileType, 0, 0f, player.whoAmI, selectedEffectIndex);
            return false;
        }

        private static void TogglePanel(Player player, IEntitySource source)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != PanelType)
                    continue;

                PreDrawLabPanel.RequestClose(projectile);
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.05f }, player.Center);
                return;
            }

            Projectile.NewProjectile(source, player.Center, Vector2.Zero, PanelType, 0, 0f, player.whoAmI, 0f, Main.MouseScreen.X, Main.MouseScreen.Y);
            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.68f, Pitch = 0.08f }, player.Center);
        }
    }

    internal sealed class PreDrawLabPlayer : ModPlayer
    {
        private int lastPage;
        private int selectedEffectIndex;

        public int LastPage => lastPage;
        public int SelectedEffectIndex => Math.Clamp(selectedEffectIndex, 0, PreDrawEffectCatalog.Count - 1);

        public void SetLastPage(int page) => lastPage = Math.Max(0, page);

        public void SelectEffect(int effectIndex)
        {
            if (PreDrawEffectCatalog.IsValidIndex(effectIndex))
                selectedEffectIndex = effectIndex;
        }
    }

    internal readonly record struct PreDrawEffectDefinition(string Name, string Description);

    internal static class PreDrawEffectCatalog
    {
        public static readonly IReadOnlyList<PreDrawEffectDefinition> Effects = new PreDrawEffectDefinition[]
        {
            new("基础精灵", "按位置、旋转、原点与缩放绘制弹幕本体。"),
            new("帧裁剪", "从多帧贴图中选择当前动画帧。"),
            new("原点偏移", "改变纹理原点，控制旋转与贴图对齐点。"),
            new("精灵翻转", "用 SpriteEffects 水平或垂直翻转本体。"),
            new("角度旋转", "基于速度、AI 或时间改变绘制旋转。"),
            new("缩放脉冲", "随时间放大、缩小或呼吸式缩放。"),
            new("随机抖动", "每帧在小范围内偏移本体，制造震颤。"),
            new("环境受光", "使用 lightColor，使精灵随环境明暗变化。"),
            new("固定染色", "以固定 Color 覆盖本体的绘制颜色。"),
            new("渐变染色", "用 Color.Lerp 在两个或多个颜色间过渡。"),
            new("GetAlpha", "重写 GetAlpha，独立控制颜色与透明度。"),
            new("淡入淡出", "依 timeLeft、Opacity 或 AI 逐步改变不透明度。"),
            new("高亮混色", "向白色或主题色插值，强化发光感。"),
            new("彩虹色相", "按世界时间或索引循环 HSL/RGB 色相。"),
            new("径向包边", "沿圆周重复绘制本体，形成全方向描边。"),
            new("方向包边", "沿速度或攻击方向偏移绘制，形成定向外框。"),
            new("背光轮廓", "先画低透明度的大尺寸背层，再画本体。"),
            new("Glowmask", "本体后叠加专用 Glow/发光贴图。"),
            new("Bloom 光晕", "使用柔边圆形或亮斑纹理扩大光感。"),
            new("圆环光环", "围绕本体按固定半径布置低透明副本。"),
            new("旋转光环", "让环绕副本随时间旋转，形成魔法环。"),
            new("随机光环", "环绕副本加入随机位置、角度或缩放扰动。"),
            new("透明多层", "以不同透明度重复叠画同一贴图。"),
            new("前后分层", "分别绘制后景、本体、前景，制造层次。"),
            new("雾气叠层", "使用雾、火、烟等纹理作为扩散覆盖层。"),
            new("Smear 残像", "绘制拉伸或弧形 Smear 贴图强化运动方向。"),
            new("斩击贴图", "叠加 Slash/Arc 图形，表现挥砍或命中。"),
            new("oldPos 残影", "手动遍历旧位置，绘制位置历史精灵。"),
            new("oldRot 残影", "手动遍历旧角度，绘制旋转历史精灵。"),
            new("居中残影", "使用 DrawAfterimagesCentered 的通用居中残影。"),
            new("边缘残影", "使用 DrawAfterimagesFromEdge 的边缘拖影。"),
            new("色差残影", "用 DrawChromaticAberration 分离彩色边缘。"),
            new("方向拖影", "沿速度反方向连续绘制，强化高速感。"),
            new("蓄力聚集", "让副本、亮斑或符号逐步向中心收拢。"),
            new("脉冲光圈", "以周期缩放和淡出绘制 Pulse/Ring。"),
            new("爆闪", "短时叠加 BrightFlash、星芒或强白光。"),
            new("环绕符号", "让星星、符文或图标绕弹幕公转。"),
            new("火花星点", "叠画 Sparkle、Point、Star 等小型纹理。"),
            new("分段光束", "开始、中段、结束三贴图拼接连续激光。"),
            new("平铺光束", "循环平铺中段纹理，填满指定长度。"),
            new("两点连线", "在两点之间拉伸线条或线段纹理。"),
            new("链条分节", "沿两点间距重复绘制链节或连接件。"),
            new("预警射线", "用低透明高长度纹理展示攻击预警。"),
            new("光束端帽", "单独绘制 Beam 的 Begin/Mid/End 端部。"),
            new("原版 Extra", "使用 TextureAssets.Extra 中的原版特效纹理。"),
            new("灾厄粒子贴图", "直接使用 CalamityMod/Particles 下的通用图形。"),
            new("灾厄额外贴图", "使用 CalamityMod/ExtraTextures 的光束、噪声等资源。")
        };

        public static int Count => Effects.Count;
        public static bool IsValidIndex(int index) => index >= 0 && index < Effects.Count;
    }

    internal sealed class PreDrawLabProjectile : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => PreDrawLabItem.DemoTexture;

        private int EffectIndex => Math.Clamp((int)Projectile.ai[0], 0, PreDrawEffectCatalog.Count - 1);

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation = MathHelper.WrapAngle(Projectile.rotation + MathHelper.PiOver4);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            if (EffectIndex != 10)
                return null;

            float pulse = 0.45f + 0.55f * (float)Math.Sin(Projectile.localAI[0] * 0.14f);
            return Color.Lerp(new Color(255, 132, 54), Color.White, pulse) * 0.85f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);
            float time = Projectile.localAI[0];
            float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 0.16f);
            Color drawColor = Color.White;

            switch (EffectIndex)
            {
                case 1: // Frame clipping.
                    int frameWidth = Math.Max(1, texture.Width / 2);
                    Rectangle frame = new((int)(time / 12f % 2) * frameWidth, 0, frameWidth, texture.Height);
                    DrawSprite(texture, drawPosition, frame, drawColor, Projectile.rotation, new Vector2(frameWidth, texture.Height) * 0.5f, 1f);
                    return false;

                case 2: // Origin offset.
                    DrawSprite(texture, drawPosition, null, drawColor, Projectile.rotation, origin + new Vector2(8f, -5f), 1f);
                    return false;

                case 3: // Sprite flip.
                    DrawSprite(texture, drawPosition, null, drawColor, Projectile.rotation, origin, 1f,
                        ((int)(time / 18f) & 1) == 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.FlipVertically);
                    return false;

                case 4: // Velocity-driven rotation.
                    DrawSprite(texture, drawPosition, null, drawColor, Projectile.velocity.ToRotation() + time * 0.08f, origin, 1f);
                    return false;

                case 5: // Scale pulse.
                    DrawSprite(texture, drawPosition, null, drawColor, Projectile.rotation, origin, 0.65f + pulse * 0.9f);
                    return false;

                case 6: // Deterministic jitter.
                    DrawSprite(texture, drawPosition + new Vector2((float)Math.Sin(time * 2.3f), (float)Math.Cos(time * 1.7f)) * 5f,
                        null, drawColor, Projectile.rotation, origin, 1f);
                    return false;

                case 7: // Environment lighting.
                    DrawSprite(texture, drawPosition, null, lightColor, Projectile.rotation, origin, 1f);
                    return false;

                case 8:
                    DrawSprite(texture, drawPosition, null, new Color(255, 86, 48), Projectile.rotation, origin, 1f);
                    return false;

                case 9:
                    DrawSprite(texture, drawPosition, null, Color.Lerp(new Color(50, 220, 255), new Color(255, 72, 184), pulse), Projectile.rotation, origin, 1f);
                    return false;

                case 10:
                    DrawSprite(texture, drawPosition, null, GetAlpha(lightColor) ?? Color.White, Projectile.rotation, origin, 1f);
                    return false;

                case 11:
                    DrawSprite(texture, drawPosition, null, Color.White * MathHelper.Clamp(Projectile.timeLeft / 70f, 0f, 1f), Projectile.rotation, origin, 1f);
                    return false;

                case 12:
                    DrawSprite(texture, drawPosition, null, Color.Lerp(new Color(255, 180, 64), Color.White, pulse), Projectile.rotation, origin, 1f);
                    return false;

                case 13:
                    DrawSprite(texture, drawPosition, null, Main.hslToRgb((time * 0.012f) % 1f, 0.9f, 0.6f), Projectile.rotation, origin, 1f);
                    return false;

                case 14:
                    DrawRadialCopies(texture, drawPosition, origin, Projectile.rotation, 7, 5f, new Color(255, 92, 50) * 0.7f);
                    break;

                case 15:
                    DrawSprite(texture, drawPosition - direction * 4f, null, new Color(60, 220, 255) * 0.72f, Projectile.rotation, origin, 1f);
                    DrawSprite(texture, drawPosition + direction * 4f, null, new Color(255, 88, 86) * 0.72f, Projectile.rotation, origin, 1f);
                    break;

                case 16:
                case 17:
                    DrawSprite(texture, drawPosition, null, new Color(255, 122, 42) * 0.35f, Projectile.rotation, origin, 1.65f);
                    break;

                case 18:
                    DrawSprite(texture, drawPosition, null, new Color(255, 168, 66) * 0.2f, Projectile.rotation, origin, 2.5f);
                    break;

                case 19:
                case 20:
                case 21:
                    for (int i = 0; i < 6; i++)
                    {
                        float angle = MathHelper.TwoPi * i / 6f + (EffectIndex == 20 ? time * 0.08f : 0f);
                        float radius = EffectIndex == 21 ? 12f + (float)Math.Sin(time * 0.4f + i * 4.1f) * 5f : 15f;
                        DrawSprite(texture, drawPosition + angle.ToRotationVector2() * radius, null,
                            Main.hslToRgb((i / 6f + time * 0.01f) % 1f, 0.8f, 0.65f) * 0.4f, Projectile.rotation, origin, 0.55f);
                    }
                    break;

                case 22:
                    for (int i = 4; i >= 1; i--)
                        DrawSprite(texture, drawPosition - direction * i * 4f, null, Color.White * (0.12f * i), Projectile.rotation, origin, 1f);
                    break;

                case 23:
                    DrawSprite(texture, drawPosition - perpendicular * 4f, null, new Color(80, 120, 255) * 0.55f, Projectile.rotation - 0.18f, origin, 1.15f);
                    DrawSprite(texture, drawPosition + perpendicular * 4f, null, new Color(255, 88, 122) * 0.55f, Projectile.rotation + 0.18f, origin, 0.9f);
                    break;

                case 24:
                    for (int i = 0; i < 5; i++)
                    {
                        float angle = time * 0.05f + i * MathHelper.TwoPi / 5f;
                        DrawSprite(texture, drawPosition + angle.ToRotationVector2() * (8f + pulse * 16f), null,
                            new Color(110, 180, 255) * 0.18f, angle, origin, 1.2f + pulse);
                    }
                    break;

                case 25:
                    DrawSprite(texture, drawPosition - direction * 12f, null, new Color(255, 100, 48) * 0.5f, Projectile.velocity.ToRotation(), origin, new Vector2(2.8f, 0.6f));
                    break;

                case 26:
                    DrawLine(drawPosition - direction * 28f + perpendicular * 18f, drawPosition + direction * 28f - perpendicular * 18f, new Color(255, 212, 112) * 0.8f, 4f);
                    break;

                case 27:
                case 29:
                case 30:
                    DrawPositionTrail(texture, origin, EffectIndex == 30 ? 3f : 0f, EffectIndex == 29 ? 0.75f : 1f);
                    break;

                case 28:
                    for (int i = Projectile.oldRot.Length - 1; i >= 0; i--)
                        DrawSprite(texture, drawPosition - direction * (i + 1) * 3f, null, new Color(176, 92, 255) * (0.05f + 0.035f * i), Projectile.oldRot[i], origin, 1f);
                    break;

                case 31:
                    DrawSprite(texture, drawPosition - perpendicular * 5f, null, Color.Red * 0.72f, Projectile.rotation, origin, 1f);
                    DrawSprite(texture, drawPosition + direction * 5f, null, Color.Lime * 0.72f, Projectile.rotation, origin, 1f);
                    DrawSprite(texture, drawPosition + perpendicular * 5f, null, Color.Cyan * 0.72f, Projectile.rotation, origin, 1f);
                    break;

                case 32:
                    for (int i = 1; i <= 8; i++)
                        DrawSprite(texture, drawPosition - direction * i * 7f, null, new Color(255, 145, 56) * (0.55f / i), Projectile.rotation, origin, 1f - i * 0.07f);
                    break;

                case 33:
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * MathHelper.TwoPi / 8f + time * 0.04f;
                        DrawSprite(texture, drawPosition + angle.ToRotationVector2() * (42f - pulse * 28f), null, new Color(120, 230, 255) * 0.35f, angle, origin, 0.45f);
                    }
                    break;

                case 34:
                    DrawRing(drawPosition, 14f + pulse * 22f, new Color(255, 160, 72) * (1f - pulse) * 0.8f, 2f);
                    break;

                case 35:
                    DrawSprite(texture, drawPosition, null, Color.White * (0.35f + pulse * 0.65f), Projectile.rotation, origin, 2f + pulse * 1.8f);
                    break;

                case 36:
                case 37:
                    for (int i = 0; i < (EffectIndex == 36 ? 5 : 9); i++)
                    {
                        float angle = time * 0.07f + i * MathHelper.TwoPi / (EffectIndex == 36 ? 5 : 9);
                        Vector2 point = drawPosition + angle.ToRotationVector2() * (EffectIndex == 36 ? 28f : 8f + (i % 3) * 10f);
                        DrawStar(point, EffectIndex == 36 ? new Color(255, 224, 92) : new Color(255, 132, 62), EffectIndex == 36 ? 5f : 3f);
                    }
                    break;

                case 38:
                case 39:
                case 40:
                case 41:
                case 42:
                case 43:
                    DrawBeamDemo(drawPosition, direction, perpendicular, EffectIndex, time);
                    break;

                case 44:
                    DrawRadialCopies(texture, drawPosition, origin, Projectile.rotation + time * 0.06f, 12, 11f, Main.hslToRgb((time * 0.01f) % 1f, 0.9f, 0.65f) * 0.5f);
                    break;

                case 45:
                    for (int i = 0; i < 12; i++)
                    {
                        float angle = time * 0.09f + i * 2.4f;
                        DrawStar(drawPosition + angle.ToRotationVector2() * (6f + (i % 4) * 8f), new Color(255, 120, 42) * 0.8f, 2f + i % 3);
                    }
                    break;

                case 46:
                    DrawRing(drawPosition, 26f + pulse * 18f, new Color(100, 230, 255) * 0.65f, 3f);
                    DrawRing(drawPosition, 10f + pulse * 8f, new Color(255, 90, 200) * 0.8f, 2f);
                    break;
            }

            DrawSprite(texture, drawPosition, null, drawColor, Projectile.rotation, origin, 1f);
            return false;
        }

        private static void DrawSprite(Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation,
            Vector2 origin, float scale, SpriteEffects effects = SpriteEffects.None) =>
            Main.EntitySpriteDraw(texture, position, source, color, rotation, origin, scale, effects, 0);

        private static void DrawSprite(Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation,
            Vector2 origin, Vector2 scale, SpriteEffects effects = SpriteEffects.None) =>
            Main.EntitySpriteDraw(texture, position, source, color, rotation, origin, scale, effects, 0);

        private static void DrawRadialCopies(Texture2D texture, Vector2 position, Vector2 origin, float rotation, int count, float radius, Color color)
        {
            for (int i = 0; i < count; i++)
                DrawSprite(texture, position + (rotation + MathHelper.TwoPi * i / count).ToRotationVector2() * radius, null, color, rotation, origin, 1f);
        }

        private void DrawPositionTrail(Texture2D texture, Vector2 origin, float edgeOffset, float scale)
        {
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Vector2 offset = Projectile.velocity.SafeNormalize(Vector2.UnitX) * edgeOffset * (i + 1);
                DrawSprite(texture, trailPosition - offset, null, new Color(255, 130, 64) * (0.06f + 0.035f * (Projectile.oldPos.Length - i)), Projectile.rotation, origin, scale);
            }
        }

        private static void DrawLine(Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 vector = end - start;
            Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, start, null, color, vector.ToRotation(), Vector2.Zero,
                new Vector2(vector.Length(), width), SpriteEffects.None, 0);
        }

        private static void DrawRing(Vector2 center, float radius, Color color, float width)
        {
            const int segments = 24;
            for (int i = 0; i < segments; i++)
            {
                Vector2 start = center + (MathHelper.TwoPi * i / segments).ToRotationVector2() * radius;
                Vector2 end = center + (MathHelper.TwoPi * (i + 1) / segments).ToRotationVector2() * radius;
                DrawLine(start, end, color, width);
            }
        }

        private static void DrawStar(Vector2 center, Color color, float size)
        {
            DrawLine(center - Vector2.UnitX * size, center + Vector2.UnitX * size, color, 1f);
            DrawLine(center - Vector2.UnitY * size, center + Vector2.UnitY * size, color, 1f);
        }

        private static void DrawBeamDemo(Vector2 center, Vector2 direction, Vector2 perpendicular, int effect, float time)
        {
            Vector2 start = center - direction * 48f;
            Vector2 end = center + direction * 72f;
            Color color = effect == 42 ? new Color(255, 72, 72) * 0.38f : new Color(100, 220, 255) * 0.75f;

            if (effect == 41)
            {
                for (int i = 0; i < 8; i++)
                    DrawRing(Vector2.Lerp(start, end, i / 7f), 4f, color, 1f);
            }
            else
            {
                DrawLine(start, end, color, effect == 42 ? 2f : 4f);
                if (effect == 38 || effect == 39)
                {
                    for (int i = 1; i < 6; i++)
                        DrawLine(Vector2.Lerp(start, end, i / 6f) - perpendicular * 4f, Vector2.Lerp(start, end, i / 6f) + perpendicular * 4f, Color.White * 0.6f, 1f);
                }
            }

            if (effect == 43)
            {
                DrawRing(start, 7f + (float)Math.Sin(time * 0.15f) * 2f, Color.White * 0.8f, 2f);
                DrawRing(end, 7f + (float)Math.Cos(time * 0.15f) * 2f, Color.White * 0.8f, 2f);
            }
        }
    }

    internal sealed class PreDrawLabPanel : ModProjectile, ILocalizedModType
    {
        private const int Columns = 6;
        private const int Rows = 5;
        private const int SlotSize = 92;
        private const int SlotGap = 7;
        private const int PanelPadding = 14;
        private const int HeaderHeight = 34;
        private const int FooterHeight = 34;
        private const int BorderThickness = 2;
        private const int ItemsPerPage = Columns * Rows;

        private static int PanelWidth => PanelPadding * 2 + Columns * SlotSize + (Columns - 1) * SlotGap;
        private static int PanelHeight => PanelPadding * 2 + HeaderHeight + Rows * SlotSize + (Rows - 1) * SlotGap + FooterHeight;

        private Vector2 panelTopLeft;
        private bool positionInitialized;
        private bool pageInitialized;
        private int page;

        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static int PageCount => Math.Max(1, (PreDrawEffectCatalog.Count + ItemsPerPage - 1) / ItemsPerPage);
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

            if (owner.HeldItem.type != ModContent.ItemType<PreDrawLabItem>())
                FadeOut = true;

            if (!positionInitialized && Main.myPlayer == Projectile.owner)
            {
                Vector2 requestedCenter = Projectile.ai[1] != 0f || Projectile.ai[2] != 0f
                    ? new Vector2(Projectile.ai[1], Projectile.ai[2])
                    : Main.MouseScreen;
                panelTopLeft = GetClampedPanelTopLeft(requestedCenter - new Vector2(PanelWidth, PanelHeight) * 0.5f);
                positionInitialized = true;
            }

            if (!pageInitialized && Main.myPlayer == Projectile.owner)
            {
                page = Math.Clamp(owner.GetModPlayer<PreDrawLabPlayer>().LastPage, 0, PageCount - 1);
                pageInitialized = true;
            }

            page = Math.Clamp(page, 0, PageCount - 1);
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
            PreDrawLabPlayer labPlayer = owner.GetModPlayer<PreDrawLabPlayer>();
            Rectangle panelArea = new((int)panelTopLeft.X, (int)panelTopLeft.Y, PanelWidth, PanelHeight);
            bool mouseOverPanel = panelArea.Intersects(MouseRectangle);
            bool leftClickPressed = Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClickPressed = Main.mouseRight && Main.mouseRightRelease;

            DrawPanel(panelArea, Projectile.Opacity);
            DrawHeader(panelArea, Projectile.Opacity);

            int start = page * ItemsPerPage;
            int end = Math.Min(PreDrawEffectCatalog.Count, start + ItemsPerPage);
            for (int index = start; index < end; index++)
            {
                int localIndex = index - start;
                PreDrawEffectDefinition effect = PreDrawEffectCatalog.Effects[index];
                Rectangle slotArea = GetSlotArea(localIndex);
                bool hovered = slotArea.Intersects(MouseRectangle);
                bool selected = labPlayer.SelectedEffectIndex == index;

                if (hovered)
                {
                    mouseOverPanel = true;
                    Main.hoverItemName = effect.Name + "\n" + effect.Description;

                    if (leftClickPressed && Projectile.Opacity >= 0.95f)
                    {
                        labPlayer.SelectEffect(index);
                        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.48f, Pitch = 0.16f }, owner.Center);
                    }
                }

                DrawSlot(effect.Name, slotArea, selected, hovered, Projectile.Opacity);
            }

            Rectangle previousPageArea = GetPreviousPageArea(panelArea);
            Rectangle nextPageArea = GetNextPageArea(panelArea);
            bool canPageLeft = page > 0;
            bool canPageRight = page + 1 < PageCount;
            bool previousHovered = previousPageArea.Intersects(MouseRectangle);
            bool nextHovered = nextPageArea.Intersects(MouseRectangle);
            mouseOverPanel |= previousHovered || nextHovered;

            DrawPager(previousPageArea, "<", canPageLeft, previousHovered, Projectile.Opacity);
            DrawPager(nextPageArea, ">", canPageRight, nextHovered, Projectile.Opacity);
            DrawFitText($"{page + 1} / {PageCount}", GetPageTextArea(panelArea), Color.White, 0.72f, 0.42f, Projectile.Opacity);

            if (leftClickPressed && Projectile.Opacity >= 0.95f)
            {
                if (canPageLeft && previousHovered)
                {
                    page--;
                    labPlayer.SetLastPage(page);
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.42f, Pitch = -0.08f }, owner.Center);
                }
                else if (canPageRight && nextHovered)
                {
                    page++;
                    labPlayer.SetLastPage(page);
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.42f, Pitch = 0.08f }, owner.Center);
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
            if (projectile.ModProjectile is PreDrawLabPanel panel)
                panel.FadeOut = true;
            else
                projectile.ai[0] = 1f;
        }

        private static Vector2 GetClampedPanelTopLeft(Vector2 desiredTopLeft)
        {
            const float screenMargin = 12f;
            float maxX = Math.Max(screenMargin, Main.screenWidth - PanelWidth - screenMargin);
            float maxY = Math.Max(screenMargin, Main.screenHeight - PanelHeight - screenMargin);
            return new Vector2(MathHelper.Clamp(desiredTopLeft.X, screenMargin, maxX), MathHelper.Clamp(desiredTopLeft.Y, screenMargin, maxY));
        }

        private static Rectangle GetPreviousPageArea(Rectangle panelArea) =>
            new(panelArea.X + PanelPadding, panelArea.Bottom - PanelPadding - 26, 42, 26);

        private Rectangle GetSlotArea(int localIndex)
        {
            int column = localIndex % Columns;
            int row = localIndex / Columns;
            int x = (int)panelTopLeft.X + PanelPadding + column * (SlotSize + SlotGap);
            int y = (int)panelTopLeft.Y + PanelPadding + HeaderHeight + row * (SlotSize + SlotGap);
            return new Rectangle(x, y, SlotSize, SlotSize);
        }

        private static Rectangle GetNextPageArea(Rectangle panelArea) =>
            new(panelArea.Right - PanelPadding - 42, panelArea.Bottom - PanelPadding - 26, 42, 26);

        private static Rectangle GetPageTextArea(Rectangle panelArea) =>
            new(panelArea.X + PanelPadding + 50, panelArea.Bottom - PanelPadding - 26, panelArea.Width - PanelPadding * 2 - 100, 26);

        private static void DrawPanel(Rectangle panelArea, float opacity)
        {
            DrawRectangle(panelArea, new Color(21, 16, 13, 232) * opacity);
            DrawBorder(panelArea, new Color(255, 150, 86) * opacity, BorderThickness);
        }

        private static void DrawHeader(Rectangle panelArea, float opacity)
        {
            Rectangle headerArea = new(panelArea.X + PanelPadding, panelArea.Y + PanelPadding, panelArea.Width - PanelPadding * 2, HeaderHeight - 8);
            DrawRectangle(new Rectangle(headerArea.X, headerArea.Bottom + 4, headerArea.Width, 2), new Color(255, 150, 86) * (opacity * 0.78f));
            DrawFitText($"PreDraw Lab  {PreDrawEffectCatalog.Count}", headerArea, Color.White, 0.8f, 0.44f, opacity);
        }

        private static void DrawSlot(string name, Rectangle slotArea, bool selected, bool hovered, float opacity)
        {
            Color accent = new(255, 150, 86);
            Color backColor = selected
                ? Color.Lerp(new Color(52, 40, 34), accent, 0.32f)
                : new Color(48, 40, 37);
            Color borderColor = selected
                ? Color.Lerp(accent, Color.White, 0.34f)
                : new Color(138, 116, 102);

            if (hovered)
            {
                backColor = Color.Lerp(backColor, new Color(96, 76, 64), 0.58f);
                borderColor = Color.Lerp(borderColor, Color.White, 0.34f);
            }

            DrawRectangle(slotArea, backColor * (opacity * 0.96f));
            DrawBorder(slotArea, borderColor * opacity, selected ? 3 : 2);
            DrawFitText(name, new Rectangle(slotArea.X + 7, slotArea.Y + 7, slotArea.Width - 14, slotArea.Height - 14), Color.White, 0.62f, 0.24f, opacity);
        }

        private static void DrawPager(Rectangle area, string symbol, bool enabled, bool hovered, float opacity)
        {
            Color backColor = enabled ? new Color(54, 44, 38) : new Color(34, 28, 25);
            Color borderColor = enabled ? new Color(255, 150, 86) : new Color(88, 70, 60);
            if (enabled && hovered)
                backColor = Color.Lerp(backColor, new Color(102, 78, 66), 0.58f);

            DrawRectangle(area, backColor * (opacity * 0.94f));
            DrawBorder(area, borderColor * opacity, 2);
            DrawFitText(symbol, new Rectangle(area.X + 5, area.Y + 3, area.Width - 10, area.Height - 6), enabled ? Color.White : new Color(130, 120, 114), 0.88f, 0.58f, opacity);
        }

        private static void DrawFitText(string text, Rectangle area, Color color, float maxScale, float minScale, float opacity)
        {
            var font = FontAssets.MouseText.Value;
            Vector2 size = font.MeasureString(text);
            float scale = MathHelper.Clamp(Math.Min(maxScale, Math.Min(area.Width / size.X, area.Height / size.Y)), minScale, maxScale);
            Vector2 position = new(area.X + Math.Max(0f, (area.Width - size.X * scale) * 0.5f), area.Y + Math.Max(0f, (area.Height - size.Y * scale) * 0.5f));
            CalamityUtils.DrawBorderStringEightWay(Main.spriteBatch, font, text, position, color * opacity, Color.Black * (opacity * 0.76f), scale);
        }

        private static void DrawRectangle(Rectangle rectangle, Color color) => Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rectangle, color);

        private static void DrawBorder(Rectangle rectangle, Color color, int thickness)
        {
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
            DrawRectangle(new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height), color);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) { }
    }
}
