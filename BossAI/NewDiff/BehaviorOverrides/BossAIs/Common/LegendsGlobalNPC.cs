using CalamityLegendsComeBack.BossAI.NewDiff.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

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
        }

        public override bool PreAI(NPC npc)
        {
            if (!LegendsWorldSystem.LegendsModeEnabled)
                return true;

            if (!LegendsBossAIRegistry.TryGetAI(npc.type, out LegendsBossAI ai))
                return true;

            return ai.PreAI(npc, this);
        }

        public override void PostAI(NPC npc)
        {
            if (!LegendsWorldSystem.LegendsModeEnabled)
                return;

            if (!LegendsBossAIRegistry.TryGetAI(npc.type, out LegendsBossAI ai))
                return;

            ai.PostAI(npc, this);
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
