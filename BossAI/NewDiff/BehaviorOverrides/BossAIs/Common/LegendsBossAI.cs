using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityLegendsComeBack.BossAI.NewDiff.Core.Systems;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common
{
    internal abstract class LegendsBossAI
    {
        public abstract int NPCType { get; }
        public abstract string BossName { get; }
        public virtual Color DebugColor => new(88, 255, 211);
        public virtual int MaxPhaseCount => PhaseLifeRatios.Length + 1;
        public virtual float[] PhaseLifeRatios => new[] { 0.75f, 0.5f, 0.25f };
        public virtual int AttackCycleLength => 150;
        public virtual float MotionIntensity => 1f;
        public virtual string PhaseName(int phase) => $"Phase {phase}";
        public virtual string StateName(LegendsGlobalNPC data) => $"Attack {data.AttackIndex} T:{data.PatternTimer}";

        public virtual bool PreAI(NPC npc, LegendsGlobalNPC data) => true;
        public virtual void PostAI(NPC npc, LegendsGlobalNPC data) { }

        /// <summary>
        /// 自定义状态同步通道。传奇模式的 Boss AI 把大量玩法状态（护盾、部位血量、阶段计时器）存在
        /// 实例字段里，而这些字段默认【完全不过网】——服务端和客户端各算各的，一旦被用来 gate 伤害
        /// 就会出现"我明明在打却不掉血"。想同步的 Boss 覆写这两个方法即可；不覆写的写 0 字节，
        /// 收发字节数天然对称，不会影响其它 Boss。
        ///
        /// 注意：写入方与读取方必须严格对称（同样的字段、同样的顺序、同样的宽度），否则会读串。
        /// 改动时务必两边一起改。字段变化后记得 npc.netUpdate = true 才会真正下发。
        /// </summary>
        public virtual void SendExtraAI(NPC npc, LegendsGlobalNPC data, BinaryWriter writer) { }
        public virtual void ReceiveExtraAI(NPC npc, LegendsGlobalNPC data, BinaryReader reader) { }
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

        protected static bool PhaseTransitionGuard(NPC npc, LegendsGlobalNPC data)
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

        protected static void BeginTransition(NPC npc, LegendsGlobalNPC data, int duration = 90)
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

        // Shared death-performance skeleton (generalized from Cryogen's InterceptLethalHit). Works purely
        // in terms of npc.ai[1] as an int so it doesn't need to know each boss's own AttackState enum type —
        // callers pass their own death-state id and a callback that switches them into it. The killing blow
        // itself never lands; life is clamped to 1 and the boss becomes untouchable, then the caller's own
        // callback drives whatever themed performance that boss wants before it actually dies for real.
        protected static void InterceptLethalHit(NPC npc, ref NPC.HitModifiers modifiers, int deathAnimationStateId, Action beginDeathCallback)
        {
            if ((int)npc.ai[1] == deathAnimationStateId)
            {
                modifiers.FinalDamage *= 0f;
                return;
            }

            modifiers.ModifyHitInfo += (ref NPC.HitInfo info) =>
            {
                if (npc.life - info.Damage > 1)
                    return;
                info.Damage = Math.Max(npc.life - 1, 0);
                npc.dontTakeDamage = true;
                beginDeathCallback?.Invoke();
            };
        }

        // Generic half of a death cinematic: pull the camera toward the boss and layer a screen-shake burst
        // on top of whatever the caller's own theme requires (dust colors, sound cues, unique flourish stay
        // boss-specific — this only owns the "the camera and the screen both react" beat all of them share).
        protected static void TriggerDeathCinematic(NPC npc, Player target, float focusStrength = 0.6f, int holdFrames = 50, float shakePower = 12f)
        {
            if (target is null || !target.active)
                return;

            target.LegendsCamera().RequestFocus(npc.Center, focusStrength, holdFrames, riseFrames: 12, fallFrames: 34);
            target.Calamity().GeneralScreenShakePower = Math.Max(target.Calamity().GeneralScreenShakePower, shakePower);
        }
    }
}
