using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common
{
    internal abstract class IUMWBossAI
    {
        public abstract int NPCType { get; }
        public abstract string BossName { get; }
        public virtual Color DebugColor => new(88, 255, 211);
        public virtual int MaxPhaseCount => PhaseLifeRatios.Length + 1;
        public virtual float[] PhaseLifeRatios => new[] { 0.75f, 0.5f, 0.25f };
        public virtual int AttackCycleLength => 150;
        public virtual float MotionIntensity => 1f;
        public virtual string PhaseName(int phase) => $"Phase {phase}";
        public virtual string StateName(IUMWGlobalNPC data) => $"Attack {data.AttackIndex} T:{data.PatternTimer}";

        public virtual bool PreAI(NPC npc, IUMWGlobalNPC data) => true;
        public virtual void PostAI(NPC npc, IUMWGlobalNPC data) { }
        public virtual bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) => true;
        public virtual void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) { }
        public virtual void FindFrame(NPC npc, int frameHeight) { }

        public virtual bool? CanBeHitByItem(NPC npc, Player player, Item item) => null;
        public virtual bool? CanBeHitByProjectile(NPC npc, Projectile projectile) => null;
        public virtual void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) { }
        public virtual void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) { }

        protected static Vector2 SafeNormalize(Vector2 v, Vector2 fallback = default)
        {
            float sq = v.LengthSquared();
            return sq > 0.0001f ? v * (1f / (float)System.Math.Sqrt(sq)) : fallback;
        }

        protected static bool TryGetTarget(NPC npc, out Player target)
        {
            target = null;
            if (npc.target < 0 || npc.target >= Main.maxPlayers || Main.player[npc.target] is null)
                npc.TargetClosest();
            if (npc.target < 0 || npc.target >= Main.maxPlayers)
                return false;
            target = Main.player[npc.target];
            return target.active && !target.dead;
        }

        protected static int SpawnHostile(NPC npc, Vector2 pos, Vector2 vel, string calamityProj, int damage, float kb = 0f)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return -1;
            int type = ProjectileID.Bullet;
            if (ModContent.TryFind("CalamityMod/" + calamityProj, out ModProjectile mp))
                type = mp.Type;
            int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), pos, vel, type, damage, kb);
            if (idx >= 0 && idx < Main.maxProjectiles)
            {
                Main.projectile[idx].hostile = true;
                Main.projectile[idx].friendly = false;
                Main.projectile[idx].netUpdate = true;
            }
            return idx;
        }

        protected static int SpawnHostileVanilla(NPC npc, Vector2 pos, Vector2 vel, int projType, int damage, float kb = 0f)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return -1;
            int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), pos, vel, projType, damage, kb);
            if (idx >= 0 && idx < Main.maxProjectiles)
            {
                Main.projectile[idx].hostile = true;
                Main.projectile[idx].friendly = false;
                Main.projectile[idx].netUpdate = true;
            }
            return idx;
        }

        protected static void HoverToward(NPC npc, Vector2 target, float speed, float inertia = 14f)
        {
            Vector2 dir = SafeNormalize(target - npc.Center, Vector2.Zero);
            npc.velocity = (npc.velocity * (inertia - 1f) + dir * speed) / inertia;
        }

        protected static bool PhaseTransitionGuard(NPC npc, IUMWGlobalNPC data)
        {
            if (data.TransitionTimer <= 0)
                return false;
            data.TransitionTimer--;
            npc.velocity *= 0.88f;
            if (data.TransitionTimer <= 0)
            {
                npc.dontTakeDamage = false;
                npc.immortal = false;
                npc.netUpdate = true;
            }
            return true;
        }

        protected static void BeginTransition(NPC npc, IUMWGlobalNPC data, int duration = 90)
        {
            data.TransitionTimer = duration;
            data.PatternTimer = 0;
            data.AttackIndex = 0;
            npc.ai[0] = 0;
            npc.ai[1] = 0;
            npc.dontTakeDamage = true;
            npc.immortal = true;
            npc.netUpdate = true;
        }
    }
}
