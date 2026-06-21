using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace.EXSkill;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace.General;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace.LeftClick
{
    public class IceBlockMinion : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Type] = false; // 不可被顶替
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 0f; // 不占用仆从栏
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false; // 冰块本身不造成直接接触伤害，仅作为辅助核心

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            // 维持生命周期
            if (!player.active || player.dead || !modPlayer.GlacialEmbraceMinion)
            {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 2;

            // 检查是否有终结技钻头正在被释放
            int drillType = ModContent.ProjectileType<GlacialDrillProj>();
            Projectile drill = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == drillType)
                {
                    drill = p;
                    break;
                }
            }

            if (drill != null)
            {
                // 终结技进行时，冰块吸附于神钻中心作为能源核心
                Projectile.Center = drill.Center;
                Projectile.velocity = Vector2.Zero;
            }
            else
            {
                // 平时悬浮于玩家后上方，做正弦波上下浮动
                float hoverOffset = player.direction * -45f;
                Vector2 targetPos = player.Center + new Vector2(hoverOffset, -40f);
                targetPos.Y += MathF.Sin(Main.GlobalTimeWrappedHourly * 3f) * 6f;

                Vector2 toTarget = targetPos - Projectile.Center;
                float dist = toTarget.Length();
                if (dist > 1200f)
                {
                    Projectile.Center = player.Center;
                }
                else
                {
                    float speed = dist > 200f ? 15f : 6f;
                    Projectile.velocity = (Projectile.velocity * 12f + toTarget.SafeNormalize(Vector2.Zero) * speed) / 13f;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D coreTex = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_02").Value;
            Texture2D glowTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ringTex = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_03").Value;

            float time = Main.GlobalTimeWrappedHourly;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // 1. 绘制外部冷色科技光晕
            Color auraCol = new Color(0, 150, 255, 0) * (0.35f + 0.1f * MathF.Sin(time * 4f));
            Main.spriteBatch.Draw(glowTex, drawPos, null, auraCol, 0f, glowTex.Size() * 0.5f, 0.4f, SpriteEffects.None, 0f);

            // 2. 绘制旋转的矩阵环
            Color ringCol = new Color(50, 200, 255, 0) * 0.5f;
            Main.spriteBatch.Draw(ringTex, drawPos, null, ringCol, time * 1.5f, ringTex.Size() * 0.5f, 0.28f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos, null, ringCol * 0.6f, -time * 0.8f, ringTex.Size() * 0.5f, 0.22f, SpriteEffects.None, 0f);

            // 3. 绘制中心发光的矩阵核心
            Color coreCol = Color.Lerp(new Color(130, 230, 255), Color.White, 0.3f + 0.3f * MathF.Sin(time * 8f));
            Main.spriteBatch.Draw(coreTex, drawPos, null, coreCol * 0.9f, -time * 2f, coreTex.Size() * 0.5f, 0.12f, SpriteEffects.None, 0f);

            // 4. 平时（非钻头状态下）绘制与所有围绕玩家的冰刺之间的几何辅助线
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();
            int drillType = ModContent.ProjectileType<GlacialDrillProj>();
            bool drillActive = player.ownedProjectileCounts[drillType] > 0;

            if (!drillActive)
            {
                int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
                Color lineCol = new Color(0, 195, 255, 0) * 0.25f;
                Texture2D pixel = TextureAssets.MagicPixel.Value;

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.owner == Projectile.owner && p.type == spikeType)
                    {
                        var modProj = p.ModProjectile as IceSpikeMinion;
                        if (modProj != null && modProj.IsCirclingPlayer())
                        {
                            // 绘制冰块与各个冰刺的连接线
                            Vector2 spikePos = p.Center;
                            Vector2 start = Projectile.Center;
                            Vector2 end = spikePos;
                            Vector2 edge = end - start;
                            if (edge.LengthSquared() > 0.001f)
                            {
                                Main.spriteBatch.Draw(pixel, start - Main.screenPosition, new Rectangle(0, 0, 1, 1), lineCol,
                                    edge.ToRotation(), new Vector2(0f, 0.5f), new Vector2(edge.Length(), 1f),
                                    SpriteEffects.None, 0f);
                            }
                        }
                    }
                }
            }

            // 5. 绘制 UI (极地能量条、打击模式对齐指示线、QTE卡点提示)
            if (player.HeldItem.type == ModContent.ItemType<GlacialEmbrace>())
            {
                // 1. 绘制极地充能条（在玩家头顶 -80 像素处）
                Vector2 headPos = player.Top - Main.screenPosition + new Vector2(0f, -80f);
                
                Texture2D barBG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
                Texture2D barFG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;

                float chargeProgress = (float)modPlayer.UltimateCharge / GlacialEmbracePlayer.MaxUltimateCharge;
                Vector2 barPos = headPos + new Vector2(-barBG.Width * 0.5f, 0f);
                
                // 能量条颜色：未满时为冰青色，蓄满时为霓虹蓝紫色
                Color barCol = chargeProgress >= 1f 
                    ? Color.Lerp(new Color(0, 180, 255), new Color(180, 50, 255), 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 6f)) 
                    : new Color(100, 210, 255) * 0.8f;

                Main.spriteBatch.Draw(barBG, barPos, new Color(40, 60, 80) * 0.9f);
                Rectangle chargeFrame = new Rectangle(0, 0, (int)(chargeProgress * barFG.Width), barFG.Height);
                Main.spriteBatch.Draw(barFG, barPos, chargeFrame, barCol);

                // 绘制能量百分比文字
                string chargeText = $"{modPlayer.UltimateCharge} / {GlacialEmbracePlayer.MaxUltimateCharge}";
                Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, chargeText, headPos.X - 25, headPos.Y - 18, Color.Cyan, Color.Black, Vector2.Zero, 0.75f);

                // 1.5. 如果是打击模式对齐状态且冷却完毕，绘制“对齐指示线”
                if (modPlayer.CurrentMode == 2 && modPlayer.StrikeAligned)
                {
                    Texture2D lineTex = TextureAssets.MagicPixel.Value;
                    Vector2 lineStart = player.Center - Main.screenPosition;
                    Vector2 lineEnd = player.Calamity().mouseWorld - Main.screenPosition;
                    Color matrixLineCol = Color.Lerp(Color.Cyan, Color.White, 0.3f + 0.3f * MathF.Sin(Main.GlobalTimeWrappedHourly * 6f)) * 0.35f;

                    // 绘制中线
                    Main.spriteBatch.Draw(lineTex, lineStart, new Rectangle(0, 0, 1, 1), matrixLineCol,
                        (lineEnd - lineStart).ToRotation(), new Vector2(0f, 0.5f), new Vector2((lineEnd - lineStart).Length(), 1.5f), SpriteEffects.None, 0f);
                    
                    // 绘制两侧的对齐网格线
                    int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
                    Vector2 dir = (player.Calamity().mouseWorld - player.Center).SafeNormalize(Vector2.UnitX * player.direction);

                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile p = Main.projectile[i];
                        if (p.active && p.type == spikeType && p.owner == player.whoAmI)
                        {
                            var spike = p.ModProjectile as IceSpikeMinion;
                            if (spike != null && spike.embedded == false && !spike.hibernating && !spike.smashing)
                            {
                                // 绘制冰刺到对向中线的垂直网格线
                                Vector2 start = p.Center - Main.screenPosition;
                                float distance = Vector2.Dot(p.Center - player.Center, dir);
                                Vector2 end = player.Center + dir * distance - Main.screenPosition;
                                Vector2 edge = end - start;
                                if (edge.LengthSquared() > 0.001f)
                                {
                                    Main.spriteBatch.Draw(lineTex, start, new Rectangle(0, 0, 1, 1), matrixLineCol * 0.7f,
                                        edge.ToRotation(), new Vector2(0f, 0.5f), new Vector2(edge.Length(), 1f), SpriteEffects.None, 0f);
                                }
                            }
                        }
                    }
                }

                // 2. 绘制 QTE 节奏环或卡点指示线（如果激活）
                if (modPlayer.QteActive)
                {
                    float qteProgress = (float)modPlayer.QteTimer / modPlayer.QteMax;
                    Vector2 qtePos = player.Center - Main.screenPosition + new Vector2(0f, -110f);

                    // 画一条细长的横向卡点条
                    int barWidth = 100;
                    int barHeight = 8;
                    Rectangle bgRect = new Rectangle((int)(qtePos.X - barWidth * 0.5f), (int)qtePos.Y, barWidth, barHeight);
                    
                    // 绘制灰色底板
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, bgRect, new Color(40, 40, 40, 200));

                    // 绘制甜点区 (Sweet Spot)：在 70% 至 90% 处，呈现亮绿色霓虹高亮
                    int sweetStart = (int)(barWidth * 0.7f);
                    int sweetWidth = (int)(barWidth * 0.2f);
                    Rectangle sweetRect = new Rectangle((int)(bgRect.X + sweetStart), (int)qtePos.Y, sweetWidth, barHeight);
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, sweetRect, Color.LimeGreen * 0.7f);

                    // 绘制白色游游标指针
                    int cursorX = (int)(bgRect.X + qteProgress * barWidth);
                    Rectangle cursorRect = new Rectangle(cursorX - 2, (int)qtePos.Y - 2, 4, barHeight + 4);
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, cursorRect, Color.White);

                    // 显示提示文字
                    string hintText = modPlayer.QteType == 0 ? "Switch Mode!" : "Release!";
                    Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, hintText, qtePos.X - 35, qtePos.Y - 20, Color.Yellow, Color.Black, Vector2.Zero, 0.75f);
                }
            }

            return false;
        }
    }
}
