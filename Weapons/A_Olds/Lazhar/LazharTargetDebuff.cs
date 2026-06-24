using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.Lazhar
{
    /// <summary>
    /// 雷达锁定 (LazharTargetDebuff) 减益
    /// 特效：被此状态影响的敌怪身上会绘制高科技全息雷达准星，
    /// 左键的拉扎尔射线对其伤害提升 50% 且强制 100% 拐角锁定追踪，击中时从天而降轨道激光卫星打击。
    /// </summary>
    public class LazharTargetDebuff : ModBuff
    {
        // 借用灾厄的“死亡标记”减益图标，红黑配色的骷髅锁准心，完美匹配高能锁定主题
        public override string Texture => "CalamityMod/Buffs/StatDebuffs/MarkedForDeath";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;      // 属于减益，不能被护士直接驱散或右键取消
            Main.pvpBuff[Type] = true;     // 允许PVP生效
            Main.buffNoSave[Type] = true;  // 重新进入世界时自动清除，不保存存档
            
            // 确保月后BOSS等抗性怪不会因专家模式缩短此Debuff的时间，保持射线的连射配合节奏
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // Debuff 期间，产生微弱的金色静电粒子环绕目标NPC
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustDirect(
                    npc.position, 
                    npc.width, 
                    npc.height, 
                    DustID.GoldCoin, 
                    0f, 
                    0f, 
                    100, 
                    default, 
                    Main.rand.NextFloat(0.8f, 1.3f)
                );
                d.velocity = npc.velocity * 0.5f + Main.rand.NextVector2Circular(2f, 2f);
                d.noGravity = true;
            }
        }
    }

    /// <summary>
    /// 全局NPC渲染挂载器 (LazharGlobalNPC)
    /// 监测带有 LazharTargetDebuff 的敌怪，在其脚底绘制红色全息锁定光圈，并在身体周围绘制高精度的收缩锁定框。
    /// </summary>
    public class LazharGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => false;

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // 仅对被拉扎尔锁定信标击中的活动目标绘制高保真雷达HUD
            if (!npc.active || npc.friendly || !npc.HasBuff<LazharTargetDebuff>())
                return;

            // 获取灾厄高科技粒子贴图资源
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D fadeStreak = ModContent.Request<Texture2D>("CalamityMod/Particles/FadeStreak").Value;
            Texture2D sparkle = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;

            Vector2 npcCenter = npc.Center - screenPos;
            float time = Main.GlobalTimeWrappedHourly;

            // 计算准星环绕尺寸 (取NPC身体最大半径加宽 30%)
            float npcRadius = Math.Max(npc.width, npc.height) * 0.5f;
            float reticleSize = npcRadius * 1.3f + 16f;

            // 锁定闪烁频率和透明度控制
            float glowPulse = 0.7f + 0.3f * (float)Math.Sin(time * 12f);
            Color hudOrange = Color.Lerp(Color.Gold, Color.OrangeRed, 0.4f) * glowPulse;
            Color hudWhite = Color.Lerp(Color.White, Color.Gold, 0.2f) * glowPulse;

            // ── 步骤 1：脚底/身后的全息底座环 (BloomCircle 压缩扁圆，营造投影感) ──
            Vector2 floorPos = new Vector2(npc.Center.X, npc.Bottom.Y) - screenPos;
            float scaleY = 0.22f;
            float breath = reticleSize / bloom.Width * (1.1f + 0.08f * (float)Math.Sin(time * 6f));
            spriteBatch.Draw(
                bloom,
                floorPos,
                null,
                hudOrange with { A = 0 } * 0.45f,
                0f,
                bloom.Size() * 0.5f,
                new Vector2(breath * 2f, breath * scaleY * 2f),
                SpriteEffects.None,
                0f
            );

            // ── 步骤 2：绘制旋转圆周虚线和全息准心臂 ──
            float rotAngle = time * 2.2f;
            int bracketCount = 4;
            for (int i = 0; i < bracketCount; i++)
            {
                // 四角定位方向角
                float angle = rotAngle + i * MathHelper.PiOver2;
                
                // 收缩抖动：科技框会不断进行高频微幅脉冲收缩，指示“完美锁死”状态
                float distanceOffset = reticleSize + (float)Math.Sin(time * 15f) * 3f;
                Vector2 bracketPos = npcCenter + angle.ToRotationVector2() * distanceOffset;

                // 绘制朝内指向的 FadeStreak 全息刻度指示条
                spriteBatch.Draw(
                    fadeStreak,
                    bracketPos,
                    null,
                    hudOrange with { A = 0 } * 0.85f,
                    angle + MathHelper.Pi, // 指向中心
                    new Vector2(fadeStreak.Width, fadeStreak.Height * 0.5f), // 锚定在前段
                    new Vector2(0.5f, 1.2f) * (reticleSize / 100f),
                    SpriteEffects.None,
                    0f
                );

                // 在每个刻度条的端点绘制微型金芒，模拟雷达捕获十字星
                spriteBatch.Draw(
                    sparkle,
                    bracketPos,
                    null,
                    hudWhite with { A = 0 } * 0.9f,
                    angle + MathHelper.PiOver4,
                    sparkle.Size() * 0.5f,
                    new Vector2(0.18f, 0.45f) * (reticleSize / 100f),
                    SpriteEffects.None,
                    0f
                );
            }

            // ── 步骤 3：中心核心锁定微小十字瞄准星 ──
            spriteBatch.Draw(
                sparkle,
                npcCenter,
                null,
                hudWhite with { A = 0 } * 0.8f,
                -time * 1.5f,
                sparkle.Size() * 0.5f,
                0.25f,
                SpriteEffects.None,
                0f
            );
            spriteBatch.Draw(
                sparkle,
                npcCenter,
                null,
                hudWhite with { A = 0 } * 0.8f,
                -time * 1.5f + MathHelper.PiOver2,
                sparkle.Size() * 0.5f,
                0.25f,
                SpriteEffects.None,
                0f
            );
        }
    }
}
