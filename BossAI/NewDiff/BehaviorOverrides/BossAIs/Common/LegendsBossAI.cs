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
        /// 开场重置钩子 —— 每场战斗开始时由框架调用一次，子类只管把自己的实例字段清干净。
        ///
        /// 为什么必须有：Registry 里每个 Boss 类型只 new 一个 AI 实例（静态字典持有到卸载），所以子类的
        /// 实例字段是【整个游戏会话共享、跨场次不重置】的。上一场残留的护盾血量、传送计时器、招式轮转
        /// 下标、拖尾坐标会原封不动带进下一场。典型症状：第二次召唤的 Boss 开局就没护盾（上一场破盾死的）、
        /// 刚出生就瞬移到上一场的死亡点（teleportDuration 还大于 0）、残影从地图另一头拖一条线过来。
        ///
        /// 以前这件事靠每个 Boss 自己在 PreAI 里写 <c>if (ai[0]==0) ResetFightState()</c> —— 18 个里漏了 8 个，
        /// 其中还包括唯一有护盾机制的 Cryogen。现在改由 <see cref="LegendsGlobalNPC"/> 在"本类型第一个个体
        /// 生成"的那一帧统一调用，物理上漏不掉，新写的 Boss 也不需要知道该在哪调。
        ///
        /// 注意：不要在这里读 npc.ai[]，此刻它们还没被本 Boss 的 PreAI 初始化过。
        /// </summary>
        public virtual void ResetFightState(NPC npc, Player target) { }

        /// <summary>
        /// 声明需要过网的实例字段。默认不声明任何字段（写 0 字节，收发天然对称，不影响其它 Boss）。
        ///
        /// 传奇模式的 Boss AI 把大量玩法状态（护盾、部位血量、阶段计时器）存在实例字段里，而这些字段
        /// 默认【完全不过网】——服务端和各客户端各算各的。一旦被用来 gate 伤害就会出现"我明明在打却
        /// 不掉血"；被用来 gate 招式选择（例如 Providence 用晶核血量决定跳过哪些招）更严重，会让各端
        /// 走出完全不同的招式序列。
        ///
        /// 收发对称性由 <see cref="LegendsSyncedFields"/> 从同一份声明推导，不需要手工维护两边的
        /// 字段顺序和宽度。用法和取舍标准见该类的文档。
        ///
        /// 记得：字段变化后要 <c>npc.netUpdate = true</c> 才会真正下发。
        /// </summary>
        protected virtual void DeclareSyncedFields(LegendsSyncedFields fields) { }

        private LegendsSyncedFields syncedFields;
        private LegendsSyncedFields Synced
        {
            get
            {
                if (syncedFields is null)
                {
                    syncedFields = new LegendsSyncedFields();
                    DeclareSyncedFields(syncedFields);
                }
                return syncedFields;
            }
        }

        public virtual void SendExtraAI(NPC npc, LegendsGlobalNPC data, BinaryWriter writer) => Synced.Write(writer);
        public virtual void ReceiveExtraAI(NPC npc, LegendsGlobalNPC data, BinaryReader reader) => Synced.Read(reader);

        /// <summary>声明过的字段是否相对上一帧发生了变化。由框架每帧调用来决定要不要推送，见 LegendsGlobalNPC.PostAI。</summary>
        internal bool SyncedStateChanged() => Synced.HasChanged();
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

        #region Telegraphed Teleport
        // 预告式传送：溶解 → 目的地聚拢 → 重组。原本只有 Cryogen 有这一套，其余 17 个 boss 换位要么是
        // 硬 npc.Center 赋值（玩家眼里就是"闪了一下人没了"，读不出意图，像 bug），要么干脆不换位。
        //
        // 基类固定两件不许改的事：
        //   1) 时序 —— 前半程淡出、正中点落位、后半程淡入。玩家靠这个节奏预判"它要出现在哪"。
        //   2) 隐身期间不许有接触伤害 —— 否则就是趁看不见白嫖一次撞伤，是纯粹的耍赖。
        // 视觉（粒子、音效、颜色）全部交给子类的三个钩子，各 boss 用自己的语言表达同一个动作。
        protected int TeleportTimer;
        protected int TeleportDuration;
        protected Vector2 TeleportDestination;

        /// <summary>是否正在传送途中。攻击逻辑可以用它决定要不要让路。</summary>
        protected bool IsTeleporting => TeleportDuration > 0;

        protected void BeginTeleport(NPC npc, Vector2 destination, int windup = 26)
        {
            TeleportDestination = destination;
            TeleportDuration = windup;
            TeleportTimer = 0;
            OnTeleportBegin(npc, destination);
            npc.netUpdate = true;
        }

        /// <summary>
        /// 每帧调用（没在传送时是空转）。传送期间它接管速度/透明度/位置，所以要放在攻击逻辑【之后】跑，
        /// 让它有最终决定权。返回是否正处在传送中。
        /// </summary>
        protected bool UpdateTeleport(NPC npc)
        {
            if (TeleportDuration <= 0)
                return false;

            TeleportTimer++;
            int half = Math.Max(1, TeleportDuration / 2);
            npc.velocity *= 0.8f;

            if (TeleportTimer < half)
            {
                npc.Opacity = MathHelper.Lerp(1f, 0f, TeleportTimer / (float)half);
                OnTeleportDissolve(npc, TeleportDestination, TeleportTimer / (float)half);
            }
            else if (TeleportTimer == half)
            {
                npc.Center = TeleportDestination;
                npc.velocity = Vector2.Zero;
                OnTeleportArrive(npc);
            }
            else
            {
                npc.Opacity = MathHelper.Lerp(0f, 1f, (TeleportTimer - half) / (float)half);
            }

            // 看不见的时候不许撞人
            if (npc.Opacity < 0.5f)
                npc.damage = 0;

            if (TeleportTimer >= TeleportDuration)
            {
                TeleportDuration = 0;
                npc.Opacity = 1f;
            }
            return true;
        }

        /// <summary>传送状态清零。跨场次重置时必须调用 —— 否则死在传送途中的话，下一场刚出生就会被瞬移到上一场的坐标。</summary>
        protected void ResetTeleport()
        {
            TeleportTimer = 0;
            TeleportDuration = 0;
            TeleportDestination = Vector2.Zero;
        }

        /// <summary>起手：这里放"我要走了"的音效。</summary>
        protected virtual void OnTeleportBegin(NPC npc, Vector2 destination) { }

        /// <summary>淡出期间每帧。这里把粒子往【目的地】聚拢 —— 传送的预告性全靠这一步，别省。</summary>
        protected virtual void OnTeleportDissolve(NPC npc, Vector2 destination, float progress) { }

        /// <summary>落位那一帧：重组特效，以及把拖尾坍缩到新位置（否则残影会横穿整个竞技场）。</summary>
        protected virtual void OnTeleportArrive(NPC npc) { }
        #endregion

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
