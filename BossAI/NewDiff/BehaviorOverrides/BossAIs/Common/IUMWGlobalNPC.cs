using CalamityLegendsComeBack.BossAI.NewDiff.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common
{
    public class IUMWGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        internal int CurrentPhase = 1;

        internal int AttackTimer;

        internal int PatternTimer;

        internal int AttackIndex;

        internal int TransitionTimer;

        internal IUMWAttackState AttackState;

        internal int BroadcastedPhase;

        internal int BroadcastedAttackIndex = -1;

        public override void SetDefaults(NPC npc)
        {
            CurrentPhase = 1;
            AttackTimer = 0;
            PatternTimer = 0;
            AttackIndex = 0;
            TransitionTimer = 0;
            AttackState = IUMWAttackState.MatrixHover;
            BroadcastedPhase = 0;
            BroadcastedAttackIndex = -1;
        }

        public override bool PreAI(NPC npc)
        {
            if (!IUMWWorldSystem.IUMWModeEnabled)
                return true;

            if (!IUMWBossAIRegistry.TryGetAI(npc.type, out IUMWBossAI ai))
                return true;

            return ai.PreAI(npc, this);
        }

        public override void PostAI(NPC npc)
        {
            if (!IUMWWorldSystem.IUMWModeEnabled)
                return;

            if (!IUMWBossAIRegistry.TryGetAI(npc.type, out IUMWBossAI ai))
                return;

            ai.PostAI(npc, this);
            IUMWDebugSystem.Report(npc, ai, this);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!IUMWWorldSystem.IUMWModeEnabled)
                return true;

            if (!IUMWBossAIRegistry.TryGetAI(npc.type, out IUMWBossAI ai))
                return true;

            return ai.PreDraw(npc, spriteBatch, screenPos, drawColor);
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!IUMWWorldSystem.IUMWModeEnabled)
                return;

            if (!IUMWBossAIRegistry.TryGetAI(npc.type, out IUMWBossAI ai))
                return;

            ai.PostDraw(npc, spriteBatch, screenPos, drawColor);
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            if (!IUMWWorldSystem.IUMWModeEnabled)
                return;

            if (!IUMWBossAIRegistry.TryGetAI(npc.type, out IUMWBossAI ai))
                return;

            ai.FindFrame(npc, frameHeight);
        }

        public override bool? CanBeHitByItem(NPC npc, Player player, Item item)
        {
            if (!IUMWWorldSystem.IUMWModeEnabled)
                return null;

            if (!IUMWBossAIRegistry.TryGetAI(npc.type, out IUMWBossAI ai))
                return null;

            return ai.CanBeHitByItem(npc, player, item);
        }

        public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
        {
            if (!IUMWWorldSystem.IUMWModeEnabled)
                return null;

            if (!IUMWBossAIRegistry.TryGetAI(npc.type, out IUMWBossAI ai))
                return null;

            return ai.CanBeHitByProjectile(npc, projectile);
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (!IUMWWorldSystem.IUMWModeEnabled)
                return;

            if (!IUMWBossAIRegistry.TryGetAI(npc.type, out IUMWBossAI ai))
                return;

            ai.ModifyHitByItem(npc, player, item, ref modifiers);
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (!IUMWWorldSystem.IUMWModeEnabled)
                return;

            if (!IUMWBossAIRegistry.TryGetAI(npc.type, out IUMWBossAI ai))
                return;

            ai.ModifyHitByProjectile(npc, projectile, ref modifiers);
        }
    }
}
