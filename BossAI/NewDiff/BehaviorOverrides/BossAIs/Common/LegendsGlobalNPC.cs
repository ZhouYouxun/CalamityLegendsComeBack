using System.IO;
using CalamityLegendsComeBack.BossAI.NewDiff.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common
{
    public class LegendsGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        internal int CurrentPhase = 1;

        internal int AttackTimer;

        internal int PatternTimer;

        internal int AttackIndex;

        internal int TransitionTimer;

        internal LegendsAttackState AttackState;

        internal int BroadcastedPhase;

        internal int BroadcastedAttackIndex = -1;

        // 开场重置的信号位。InstancePerEntity 保证每个 NPC 个体拿到自己的一份，SetDefaults 生成时置位，
        // 第一次 PreAI 消费掉 —— 也就是"这个个体刚刚出生"这一帧。见 LegendsBossAI.ResetFightState 的说明。
        private bool needsFightReset = true;

        public override void SetDefaults(NPC npc)
        {
            CurrentPhase = 1;
            AttackTimer = 0;
            PatternTimer = 0;
            AttackIndex = 0;
            TransitionTimer = 0;
            AttackState = LegendsAttackState.MatrixHover;
            BroadcastedPhase = 0;
            BroadcastedAttackIndex = -1;
            needsFightReset = true;
        }

        public override bool PreAI(NPC npc)
        {
            if (!LegendsWorldSystem.LegendsModeEnabled)
                return true;

            if (!LegendsBossAIRegistry.TryGetAI(npc.type, out LegendsBossAI ai))
                return true;

            TryResetFightState(npc, ai);

            return ai.PreAI(npc, this);
        }

        // 跨场次状态清理的唯一触发点。两道闸门缺一不可：
        //   1) npc.type == ai.NPCType —— 虫类 Boss（AstrumDeus/StormWeaver/AquaticScourge）的身体和尾巴
        //      共用同一个 AI 实例，不筛主体的话，每生成一节身体都会把打到一半的状态清一次。
        //   2) 场上没有别的同类个体 —— 防住"一场战斗里出现第二个主体"的情况（例如虫类分裂），
        //      那种时候是同一场战斗的延续，不是新一场。
        private void TryResetFightState(NPC npc, LegendsBossAI ai)
        {
            if (!needsFightReset)
                return;

            needsFightReset = false;

            if (npc.type != ai.NPCType || !IsOnlyOneOfTypeAlive(npc))
                return;

            if (npc.target < 0 || npc.target >= Main.maxPlayers || !Main.player[npc.target].active)
                npc.TargetClosest(false);

            Player target = npc.target >= 0 && npc.target < Main.maxPlayers ? Main.player[npc.target] : Main.LocalPlayer;
            ai.ResetFightState(npc, target);
        }

        private static bool IsOnlyOneOfTypeAlive(NPC npc)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                if (other.active && other.whoAmI != npc.whoAmI && other.type == npc.type)
                    return false;
            }
            return true;
        }

        // 自定义状态同步。刻意【不】检查 LegendsModeEnabled：收发两端必须写入/读出完全相同的字节数，
        // 若一端因为开关状态不同而跳过，字节流就会读串。TryGetAI 只依赖 npc.type，各端结果必然一致，
        // 用它做唯一门槛才安全。不覆写的 Boss 写 0 字节，天然对称。
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            if (!LegendsBossAIRegistry.TryGetAI(npc.type, out LegendsBossAI ai))
                return;

            ai.SendExtraAI(npc, this, binaryWriter);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            if (!LegendsBossAIRegistry.TryGetAI(npc.type, out LegendsBossAI ai))
                return;

            ai.ReceiveExtraAI(npc, this, binaryReader);
        }

        public override void PostAI(NPC npc)
        {
            if (!LegendsWorldSystem.LegendsModeEnabled)
                return;

            if (!LegendsBossAIRegistry.TryGetAI(npc.type, out LegendsBossAI ai))
                return;

            ai.PostAI(npc, this);

            // 声明过的玩法状态一旦真的变了就推送。放在这里而不是让各 Boss 在每一处扣血/破盾后手写
            // netUpdate —— 那种约定漏一处就静默失效，而且新 Boss 作者根本不会知道有这条规矩。
            // 只有服务端推：NPC 的权威状态归服务端，客户端置 netUpdate 没有意义。
            if (Main.netMode == NetmodeID.Server && ai.SyncedStateChanged())
                npc.netUpdate = true;

            LegendsDebugSystem.Report(npc, ai, this);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!LegendsWorldSystem.LegendsModeEnabled)
                return true;

            if (!LegendsBossAIRegistry.TryGetAI(npc.type, out LegendsBossAI ai))
                return true;

            return ai.PreDraw(npc, spriteBatch, screenPos, drawColor);
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!LegendsWorldSystem.LegendsModeEnabled)
                return;

            if (!LegendsBossAIRegistry.TryGetAI(npc.type, out LegendsBossAI ai))
                return;

            ai.PostDraw(npc, spriteBatch, screenPos, drawColor);
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            if (!LegendsWorldSystem.LegendsModeEnabled)
                return;

            if (!LegendsBossAIRegistry.TryGetAI(npc.type, out LegendsBossAI ai))
                return;

            ai.FindFrame(npc, frameHeight);
        }

        public override bool? CanBeHitByItem(NPC npc, Player player, Item item)
        {
            if (!LegendsWorldSystem.LegendsModeEnabled)
                return null;

            if (!LegendsBossAIRegistry.TryGetAI(npc.type, out LegendsBossAI ai))
                return null;

            return ai.CanBeHitByItem(npc, player, item);
        }

        public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
        {
            if (!LegendsWorldSystem.LegendsModeEnabled)
                return null;

            if (!LegendsBossAIRegistry.TryGetAI(npc.type, out LegendsBossAI ai))
                return null;

            return ai.CanBeHitByProjectile(npc, projectile);
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (!LegendsWorldSystem.LegendsModeEnabled)
                return;

            if (!LegendsBossAIRegistry.TryGetAI(npc.type, out LegendsBossAI ai))
                return;

            ai.ModifyHitByItem(npc, player, item, ref modifiers);
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (!LegendsWorldSystem.LegendsModeEnabled)
                return;

            if (!LegendsBossAIRegistry.TryGetAI(npc.type, out LegendsBossAI ai))
                return;

            ai.ModifyHitByProjectile(npc, projectile, ref modifiers);
        }
    }
}
