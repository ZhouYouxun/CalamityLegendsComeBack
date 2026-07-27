using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.Buffs;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General
{
    /// <summary>
    /// 蚀痕叠层系统——冥蚀天底"呼应"的核心。
    /// 左键近战、回旋斩迹、暗影魂针都会往敌人身上叠"蚀痕"；
    /// 右键第三矛的终爆、回旋奇点新星会一次性消耗范围内的蚀痕，伤害随消耗层数暴涨。
    /// 层数越高，敌人身上的黑绿蚀光越浓；一段时间不再叠层则缓慢衰减。
    /// </summary>
    public class UmbralCorrosionGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public const int MaxStacks = 10;
        private const int DecayDelay = 240;    // 4 秒未叠层后开始衰减
        private const int DecayInterval = 24;  // 之后每 0.4 秒 -1

        public int Stacks { get; private set; }
        private int decayTimer;
        private int decayTick;

        // ===== 静态接口 =====

        public static void AddStacks(NPC npc, int amount, int buffTime = 360)
        {
            if (npc == null || !npc.active || npc.friendly || amount <= 0)
                return;
            var g = npc.GetGlobalNPC<UmbralCorrosionGlobalNPC>();
            g.Stacks = Math.Min(MaxStacks, g.Stacks + amount);
            g.decayTimer = DecayDelay;
            g.decayTick = 0;
            npc.AddBuff(ModContent.BuffType<UmbralCorrosion>(), buffTime);
        }

        public static int GetStacks(NPC npc)
            => npc == null || !npc.active ? 0 : npc.GetGlobalNPC<UmbralCorrosionGlobalNPC>().Stacks;

        /// <summary>清空并返回消耗的层数（供终爆 / 奇点新星结算暴发伤害）。</summary>
        public static int ConsumeStacks(NPC npc)
        {
            if (npc == null || !npc.active)
                return 0;
            var g = npc.GetGlobalNPC<UmbralCorrosionGlobalNPC>();
            int s = g.Stacks;
            g.Stacks = 0;
            g.decayTimer = 0;
            return s;
        }

        // ===== 每帧维护 =====

        public override void PostAI(NPC npc)
        {
            if (Stacks <= 0)
                return;

            if (decayTimer > 0)
                decayTimer--;
            else if (++decayTick >= DecayInterval)
            {
                decayTick = 0;
                Stacks--;
            }

            // 蚀光：层数越高越浓
            float t = Stacks / (float)MaxStacks;
            Lighting.AddLight(npc.Center, 0.1f * t, 0.55f * t, 0.22f * t);
            if (Main.rand.NextFloat() < 0.25f + 0.45f * t)
            {
                Vector2 pos = npc.Center + Main.rand.NextVector2Circular(npc.width * 0.5f, npc.height * 0.5f);
                Dust vd = Dust.NewDustPerfect(pos, ModContent.DustType<VoidDustInverted>());
                vd.noGravity = true;
                vd.velocity = Main.rand.NextVector2Circular(1f, 1f) - Vector2.UnitY * 0.5f;
                vd.scale = 0.7f + 0.8f * t;
                vd.color = UmbralNadirPalette.CorrosionAura;
            }
        }

        // 被蚀刻的敌人整体染上一层黑绿
        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (Stacks <= 0)
                return;
            float t = Stacks / (float)MaxStacks;
            drawColor = Color.Lerp(drawColor, new Color(70, 150, 90), 0.28f * t);
        }
    }
}
