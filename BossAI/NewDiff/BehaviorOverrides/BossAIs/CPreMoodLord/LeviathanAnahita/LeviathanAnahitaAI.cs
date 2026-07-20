using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.WeaponAttacks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.LeviathanAnahita
{
    // Leviathan and Anahita are TWO NPC entities that both route through this ONE shared AI instance (see
    // LegendsBossAIRegistry.Load — both npc.type values map to the same object), so every instance field here
    // is genuinely boss-wide state, not per-entity. Leviathan acts as the "conductor": its branch owns
    // timer++ and RotateAttack for every state (including Anahita's), while Anahita's branch only reads
    // timer/tracker to drive her own spell logic. This mirrors the file's original design; the fixes here
    // are: real invulnerability during the transition cutscene, a real Ocean-biome enrage (verified against
    // CalamityMod/NPCs/Leviathan/Leviathan.cs's own notOcean check), a cooldown-gated arena hurt instead of
    // per-frame Hurt(), anti-cheese hover, a persistent attack-cycle rotation with dual variants, and P2
    // padded out to 8 rotation slots (the design doc only names 2 P2 moves, well short of the 6-slot floor).
    internal sealed class LeviathanAnahitaAI : LegendsBossAI
    {
        #region Constants & Configurations
        public override int NPCType => ModContent.Find<ModNPC>("CalamityMod/Leviathan").Type;
        public override string BossName => "Leviathan & Anahita";
        public override Color DebugColor => new(60, 160, 255);

        public override int MaxPhaseCount => 2;
        public override float[] PhaseLifeRatios => new[] { 0.40f };
        public override int AttackCycleLength => 120;
        public override float MotionIntensity => 1.0f;
        #endregion

        #region Attack States
        public enum AttackState
        {
            Greentide = 0,
            Leviatitan = 1,
            AnahitaArpeggio = 2,
            Atlantis = 3,
            GastricBelcher = 4,
            LeviathanTeeth = 5,
            DolphinJump = 6,
            AtlantisNet = 7,
            OceanStormTransition = 8
        }
        #endregion

        #region Fields
        private int ticksRunning = 0;
        private int currentRepetition = 0;
        private readonly Vector2[] oldPositionsL = new Vector2[14];
        private int oldPositionsIndexL;
        private readonly Vector2[] oldPositionsA = new Vector2[14];
        private int oldPositionsIndexA;

        // Rotation persists across HP-threshold bookkeeping; only the one real 40% unseal resets it.
        // P1 is exactly the design doc's 6-weapon order. P2's design doc only names 2 finale moves — well
        // short of the "at least 6 rotation slots" floor — so P2 replays all 6 P1 weapons (now unbound from
        // the 3x-repeat rule, per "攻击不再受3次重复规则限制") followed by the 2 finale moves: 8 slots total.
        private int attackCycleIndex = 0;
        private static readonly AttackState[] P1Cycle =
        {
            AttackState.Greentide, AttackState.Leviatitan, AttackState.AnahitaArpeggio,
            AttackState.Atlantis, AttackState.GastricBelcher, AttackState.LeviathanTeeth,
        };
        private static readonly AttackState[] P2Cycle =
        {
            AttackState.Greentide, AttackState.Leviatitan, AttackState.AnahitaArpeggio, AttackState.Atlantis,
            AttackState.GastricBelcher, AttackState.LeviathanTeeth, AttackState.DolphinJump, AttackState.AtlantisNet,
        };

        // Per-attack variant toggle: flips deterministically each time that attack slot comes up (no RNG),
        // so it can't desync in multiplayer. All 6 P1 weapons get a second, meaningfully different execution.
        private readonly bool[] attackVariant = new bool[9];
        private bool UseVariantB(AttackState state)
        {
            int i = (int)state;
            bool v = attackVariant[i];
            attackVariant[i] = !v;
            return v;
        }

        // Anahita's Ice Prism Shield — tracked as an abstract hit-counter (matches Cryogen's shieldHealth
        // convention) rather than a real destructible sub-entity, since the closest vanilla equivalent
        // (CalamityMod.NPCs.Leviathan.AnahitasIceShield) hardcodes its own dontTakeDamage from Leviathan's
        // life ratio in a way that would make it unbreakable for nearly all of P1 if reused directly.
        private bool shieldActive = true;
        private int shieldHitsRemaining = 45;
        private int shieldStunTimer = 0;
        private int shieldRegenTimer = 0;
        private int shieldFxCooldown = 0;

        // 阿纳西塔的棱镜护盾 gate 80% 减伤。shieldHitsRemaining 是按"命中次数"扣的，而 ModifyHit 只在
        // 攻击方客户端和服务端跑 —— 不同步的话每个玩家都在扣自己那份计数，谁也不知道盾到底还剩几下。
        protected override void DeclareSyncedFields(LegendsSyncedFields f) => f
            .Bool(() => shieldActive, v => shieldActive = v)
            .Int(() => shieldHitsRemaining, v => shieldHitsRemaining = v)
            .Int(() => shieldStunTimer, v => shieldStunTimer = v)
            .Int(() => shieldRegenTimer, v => shieldRegenTimer = v);

        // Tidal Current Forcefield (Y=200px to bottomTideY, rises every 10s)
        private float bottomTideY = 1200f;
        private int tideTimer = 0;
        private int arenaHurtCooldown = 0;

        // Ocean-biome leash — Leviathan/Anahita's real Calamity home biome is Ocean; see
        // CalamityMod/NPCs/Leviathan/Leviathan.cs's own notOcean check (not a simple ZoneBeach flag).
        private int outOfBiomeTimer = 0;
        private float enrageSpeedMultiplier = 1f;
        private bool wasEnraged = false;

        private float transitionFlashAlpha = 0f;

        // Leviathan and Anahita are two independent NPCs that die independently — a death performance here
        // must NOT touch master.ai[] (the shared conductor state both entities read), or killing one would
        // desync or interrupt the other's still-ongoing fight. So each death runs on its own per-whoAmI timer
        // instead of going through the normal AttackState machine at all.
        private readonly Dictionary<int, int> deathAnimTimer = new();
        #endregion

        #region Per-fight State Reset
        // 跨场次状态清理（为什么需要见基类 LegendsBossAI.ResetFightState；调用时机由框架负责）。
        // 本组合残留最要命的两项：
        //   · shieldActive/shieldHitsRemaining —— 上一场破着盾死，下一场开局直接是无盾状态
        //   · deathAnimTimer —— 以 whoAmI 为键，上一场的死亡计时器会被新个体复用同槽位后命中，
        //     表现为刚出生就开始播死亡演出
        // 注意：框架只在主体类型(Leviathan)生成时调用一次，Anahita 侧不会重复触发。
        public override void ResetFightState(NPC npc, Player target)
        {
            ticksRunning = 0;
            currentRepetition = 0;
            attackCycleIndex = 0;
            Array.Clear(attackVariant, 0, attackVariant.Length);

            shieldActive = true;
            shieldHitsRemaining = 45;
            shieldStunTimer = 0;
            shieldRegenTimer = 0;
            shieldFxCooldown = 0;

            bottomTideY = 1200f;
            tideTimer = 0;
            arenaHurtCooldown = 0;
            transitionFlashAlpha = 0f;
            deathAnimTimer.Clear();

            for (int i = 0; i < oldPositionsL.Length; i++)
                oldPositionsL[i] = npc.Center;
            oldPositionsIndexL = 0;
            for (int i = 0; i < oldPositionsA.Length; i++)
                oldPositionsA[i] = npc.Center;
            oldPositionsIndexA = 0;

            outOfBiomeTimer = 0;
            enrageSpeedMultiplier = 1f;
            wasEnraged = false;
        }
        #endregion

        #region Core AI Hooks
        public override bool PreAI(NPC npc, LegendsGlobalNPC data)
        {
            ticksRunning++;

            int anahitaType = ModContent.Find<ModNPC>("CalamityMod/Anahita").Type;
            int leviathanType = ModContent.Find<ModNPC>("CalamityMod/Leviathan").Type;
            bool isAnahita = npc.type == anahitaType;

            if (isAnahita) { oldPositionsA[oldPositionsIndexA] = npc.Center; oldPositionsIndexA = (oldPositionsIndexA + 1) % oldPositionsA.Length; }
            else { oldPositionsL[oldPositionsIndexL] = npc.Center; oldPositionsIndexL = (oldPositionsIndexL + 1) % oldPositionsL.Length; }

            if (!TryGetTarget(npc, out Player target))
            {
                npc.velocity.Y -= 0.5f;
                if (npc.timeLeft > 60) npc.timeLeft = 60;
                return false;
            }

            if (deathAnimTimer.TryGetValue(npc.whoAmI, out int deathTimer) && deathTimer > 0)
            {
                ExecuteDeathAnimation(npc, target, isAnahita, deathTimer);
                deathAnimTimer[npc.whoAmI] = deathTimer + 1;
                return false;
            }

            NPC master = npc;
            if (isAnahita)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == leviathanType)
                    {
                        master = Main.npc[i];
                        break;
                    }
                }
            }

            int currentPhase = (int)master.ai[0];
            AttackState state = (AttackState)(int)master.ai[1];
            ref float timer = ref master.ai[2];
            ref float stateTracker = ref master.ai[3];

            if (currentPhase == 0)
            {
                currentPhase = 1;
                master.ai[0] = 1f;
                state = AttackState.Greentide;
                master.ai[1] = (float)state;
                currentRepetition = 0;
                attackCycleIndex = 0;
                master.netUpdate = true;
            }

            // Single real transition: crossing from P1 into P2 at 40% HP (lowest of either boss).
            float lowestLife = npc.lifeMax <= 0 ? 1f : npc.life / (float)npc.lifeMax;
            if (!isAnahita)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == anahitaType)
                    {
                        float alife = Main.npc[i].life / (float)Main.npc[i].lifeMax;
                        if (alife < lowestLife) lowestLife = alife;
                        break;
                    }
                }
            }

            int nextPhase = lowestLife <= PhaseLifeRatios[0] ? 2 : 1;
            if (nextPhase > currentPhase && state != AttackState.OceanStormTransition)
            {
                currentPhase = nextPhase;
                master.ai[0] = currentPhase;
                state = AttackState.OceanStormTransition;
                master.ai[1] = (float)state;
                timer = 0;
                stateTracker = 0;
                master.netUpdate = true;
            }

            UpdateOceanEnrage(npc, target);

            // Tidal Current forcefield (Y=200px to bottomTideY)
            tideTimer++;
            if (tideTimer >= 600)
            {
                if (tideTimer < 780)
                    bottomTideY = MathHelper.Lerp(1200f, 1050f, (tideTimer - 600f) / 60f);
                else
                {
                    bottomTideY = MathHelper.Lerp(1050f, 1200f, (tideTimer - 780f) / 60f);
                    if (tideTimer >= 840)
                    {
                        tideTimer = 0;
                        bottomTideY = 1200f;
                    }
                }
            }

            if (arenaHurtCooldown > 0)
                arenaHurtCooldown--;
            if (target.Center.Y < 200f || target.Center.Y > bottomTideY)
            {
                target.AddBuff(BuffID.Wet, 180);
                target.AddBuff(BuffID.Slow, 180);
                if (target.Center.Y < 200f) target.velocity.Y = 4f;
                else target.velocity.Y = -4f;
                if (arenaHurtCooldown <= 0)
                {
                    arenaHurtCooldown = 30;
                    target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 8, 0);
                }
            }

            if (isAnahita)
                UpdateIceShield(npc);
            if (shieldFxCooldown > 0)
                shieldFxCooldown--;

            if (isAnahita)
            {
                npc.rotation = npc.velocity.X * 0.05f;
                npc.scale = 1f + (float)Math.Sin(ticksRunning * 0.04f) * 0.02f;
                ExecuteAnahitaAttacks(npc, target, state, ref timer, ref stateTracker, currentPhase);
            }
            else
            {
                npc.rotation = npc.velocity.X * 0.02f;
                npc.scale = 1.1f + (float)Math.Sin(ticksRunning * 0.03f) * 0.02f;
                ExecuteLeviathanAttacks(npc, target, state, ref timer, ref stateTracker, currentPhase);
            }

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            ApplyDefenseModifiers(npc, ref modifiers);
            InterceptEntityDeath(npc, ref modifiers, player);
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            ApplyDefenseModifiers(npc, ref modifiers);
            InterceptEntityDeath(npc, ref modifiers, Main.player[projectile.owner]);
        }

        private void ApplyDefenseModifiers(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (npc.ai[1] == (float)AttackState.OceanStormTransition)
            {
                modifiers.FinalDamage *= 0f; // fully invulnerable through the whitewater cutscene
                return;
            }

            int anahitaType = ModContent.Find<ModNPC>("CalamityMod/Anahita").Type;
            if (npc.type == anahitaType && shieldActive)
            {
                modifiers.FinalDamage *= 0.20f; // 80% DR while any prism survives
                if (shieldFxCooldown <= 0)
                {
                    shieldFxCooldown = 8;
                    shieldHitsRemaining--;
                    SoundEngine.PlaySound(SoundID.NPCHit5 with { Volume = 0.4f, Pitch = 0.3f }, npc.Center);
                    if (shieldHitsRemaining <= 0)
                    {
                        shieldActive = false;
                        shieldStunTimer = 300; // 5s stun (design doc)
                        npc.velocity = Vector2.Zero;
                        SoundEngine.PlaySound(SoundID.Shatter, npc.Center);
                        LeviathanFx.Burst(npc.Center, 6f, 30, DustID.IceRod);
                    }
                }
            }
        }

        // Self-contained lethal-hit intercept keyed by whoAmI (not by the shared master.ai[1] state — see the
        // deathAnimTimer field comment for why) so it works identically and independently for both entities.
        private void InterceptEntityDeath(NPC npc, ref NPC.HitModifiers modifiers, Player target)
        {
            if (deathAnimTimer.TryGetValue(npc.whoAmI, out int t) && t > 0)
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
                BeginDeathAnimation(npc, target);
            };
        }

        private void BeginDeathAnimation(NPC npc, Player target)
        {
            deathAnimTimer[npc.whoAmI] = 1;
            npc.velocity = Vector2.Zero;
            npc.netUpdate = true;

            bool isAnahita = npc.type == ModContent.Find<ModNPC>("CalamityMod/Anahita").Type;
            TriggerDeathCinematic(npc, target, focusStrength: 0.5f, holdFrames: 50, shakePower: 9f);
            if (isAnahita)
                SoundEngine.PlaySound(SoundID.Shatter, npc.Center);
            else
                SoundEngine.PlaySound(SoundID.Roar with { Volume = 1f, Pitch = 0.1f }, npc.Center);
        }
        #endregion

        #region Shield Management
        private void UpdateIceShield(NPC npc)
        {
            if (shieldActive)
                return;

            if (shieldStunTimer > 0)
            {
                // Shield shattered: the leviathan wallows, venting brine — the punish window is visible
                shieldStunTimer--;
                npc.defense = 0;
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(110f, 70f), DustID.DungeonWater, new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(1f, 3f)), 120, default, 1.4f);
                    d.noGravity = true;
                }
                if (shieldStunTimer == 0)
                {
                    shieldRegenTimer = 900; // 15s regen (design doc)
                    SoundEngine.PlaySound(SoundID.Zombie92 with { Volume = 0.6f }, npc.Center);
                }
            }
            else if (shieldRegenTimer > 0)
            {
                shieldRegenTimer--;
                if (shieldRegenTimer == 0)
                {
                    shieldActive = true;
                    shieldHitsRemaining = 45;
                    LeviathanFx.Burst(npc.Center, 4f, 24, DustID.IceRod);
                }
            }
        }
        #endregion

        #region Ocean Enrage
        private void UpdateOceanEnrage(NPC npc, Player target)
        {
            const int graceFrames = 300; // matches CalamityGlobalNPC.biomeEnrageTimerMax
            const float maxMultiplier = 1.35f;

            bool notOcean = target.position.Y < 800f || target.position.Y > Main.worldSurface * 16D ||
                (target.position.X > 6400f && target.position.X < (Main.maxTilesX * 16 - 6400));

            if (notOcean)
                outOfBiomeTimer = Math.Min(outOfBiomeTimer + 1, graceFrames + 120);
            else
                outOfBiomeTimer = Math.Max(outOfBiomeTimer - 3, 0);

            bool enraged = outOfBiomeTimer >= graceFrames;
            npc.Calamity().CurrentlyEnraged = enraged;
            enrageSpeedMultiplier = MathHelper.Lerp(enrageSpeedMultiplier, enraged ? maxMultiplier : 1f, 0.04f);

            if (enraged && !wasEnraged)
            {
                SoundEngine.PlaySound(SoundID.Roar with { Pitch = 0.2f, Volume = 0.6f }, npc.Center);
                LeviathanFx.Burst(npc.Center, 5f, 24);
            }
            wasEnraged = enraged;
        }

        private float Spd(float baseSpeed) => baseSpeed * enrageSpeedMultiplier;

        // Anti-cheese hover spot — offsets to whichever side the boss is already on and leads the target's
        // movement, so it never camps directly overhead for an attack's full duration.
        private static Vector2 DirectedHoverSpot(NPC npc, Player target, float sideOffset, float heightOffset, float lead = 0f)
        {
            float side = Math.Sign(npc.Center.X - target.Center.X);
            if (side == 0f)
                side = Main.rand.NextBool() ? 1f : -1f;
            Vector2 predicted = target.Center + target.velocity * lead;
            return predicted + new Vector2(side * sideOffset, heightOffset);
        }
        #endregion

        #region Attack Rotation
        private void RotateAttack(NPC npc, int currentPhase, AttackState current)
        {
            if (currentPhase <= 1)
            {
                currentRepetition++;
                if (currentRepetition < 3)
                {
                    npc.ai[2] = 0;
                    npc.ai[3] = 0;
                    npc.netUpdate = true;
                    return;
                }
                currentRepetition = 0;
                attackCycleIndex++;
                npc.ai[1] = (float)P1Cycle[attackCycleIndex % P1Cycle.Length];
            }
            else
            {
                attackCycleIndex++;
                npc.ai[1] = (float)P2Cycle[attackCycleIndex % P2Cycle.Length];
            }
            npc.ai[2] = 0;
            npc.ai[3] = 0;
            npc.netUpdate = true;
        }
        #endregion

        #region Anahita Attack States
        private void ExecuteAnahitaAttacks(NPC npc, Player target, AttackState state, ref float timer, ref float tracker, int phase)
        {
            switch (state)
            {
                case AttackState.Greentide:
                    ExecuteGreentide(npc, target, ref timer, ref tracker, phase);
                    break;
                case AttackState.AnahitaArpeggio:
                    ExecuteArpeggio(npc, target, ref timer, ref tracker, phase);
                    break;
                case AttackState.Atlantis:
                    ExecuteAtlantis(npc, target, ref timer, ref tracker, phase);
                    break;
                case AttackState.AtlantisNet:
                    ExecuteAtlantisNet(npc, target, ref timer, ref tracker, phase);
                    break;
                default:
                    Vector2 spot = DirectedHoverSpot(npc, target, 420f, -220f, 6f);
                    npc.velocity = Vector2.Lerp(npc.velocity, (spot - npc.Center) * 0.05f, 0.1f);
                    break;
            }
        }

        // GREENTIDE — A: tide blades slam at the player's tracked column, one after another (documented).
        //             B: blades sweep left-to-right across fixed columns, forcing lateral repositioning.
        private void ExecuteGreentide(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.Greentide) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<AnahitaHeldGreentide>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            Vector2 hover = DirectedHoverSpot(npc, target, 450f, -200f, 15f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.07f, Spd(0.12f));

            if (timer >= 40 && timer <= 160 && (timer - 40) % 20 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                float x = variantB
                    ? target.Center.X - 600f + ((timer - 40) / 20f) * 200f
                    : target.Center.X + Main.rand.NextFloat(-40f, 40f);
                Vector2 spawn = new(x, 400f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<GreentideSlamProj>(), dmg, 0f, Main.myPlayer);
            }

            if (timer >= 190)
                RotateAttack(npc, phase, AttackState.Greentide);
        }

        // ANAHITA'S ARPEGGIO — A: 5 notes ascend low->high in sequence (documented, wave-form bolts).
        //                       B: 5 notes descend high->low AND mirror-fire from both staff ends inward.
        private void ExecuteArpeggio(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.AnahitaArpeggio) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<AnahitaHeldArpeggio>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            Vector2 hover = DirectedHoverSpot(npc, target, 500f, -150f, 10f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.06f, Spd(0.12f));

            if (timer == 20 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                for (int i = 0; i < 5; i++)
                {
                    int order = variantB ? 4 - i : i;
                    Vector2 spawn = target.Center + new Vector2(order * 140f - 280f, -260f);
                    Vector2 dir = SafeNormalize(target.Center - spawn, Vector2.UnitY);
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<AnahitaNoteProj>(), dmg, 0f, Main.myPlayer, i * 15f, i * 0.15f);
                    if (idx >= 0 && idx < Main.maxProjectiles)
                    {
                        Main.projectile[idx].ai[2] = dir.X;
                        Main.projectile[idx].ai[3] = dir.Y;
                    }
                    if (variantB && i < 2)
                    {
                        // Mirror pair: an extra note from the opposite end firing back toward center.
                        Vector2 spawn2 = target.Center + new Vector2(-(order * 140f - 280f), -260f);
                        Vector2 dir2 = SafeNormalize(target.Center - spawn2, Vector2.UnitY);
                        int idx2 = Projectile.NewProjectile(npc.GetSource_FromAI(), spawn2, Vector2.Zero, ModContent.ProjectileType<AnahitaNoteProj>(), dmg, 0f, Main.myPlayer, i * 15f, i * 0.15f);
                        if (idx2 >= 0 && idx2 < Main.maxProjectiles)
                        {
                            Main.projectile[idx2].ai[2] = dir2.X;
                            Main.projectile[idx2].ai[3] = dir2.Y;
                        }
                    }
                }
            }

            if (timer >= 180)
                RotateAttack(npc, phase, AttackState.AnahitaArpeggio);
        }

        // ATLANTIS — A: 3 tridents lock a triangle around the player (documented).
        //            B: 4 tridents lock a rotated diamond/cross — a different escape geometry.
        private void ExecuteAtlantis(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.Atlantis) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<AnahitaHeldAtlantis>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            Vector2 hover = DirectedHoverSpot(npc, target, 0f, -320f, 0f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.06f, Spd(0.11f));

            if (timer == 20 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                int points = variantB ? 4 : 3;
                float radius = variantB ? 300f : 260f;
                Vector2[] pos = new Vector2[points];
                for (int i = 0; i < points; i++)
                    pos[i] = target.Center + (i * MathHelper.TwoPi / points).ToRotationVector2() * radius;

                for (int i = 0; i < points; i++)
                {
                    Vector2 a = pos[i];
                    Vector2 b = pos[(i + 1) % points];
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), a, Vector2.Zero, ModContent.ProjectileType<AtlantisPillarProj>(), dmg, 0f, Main.myPlayer, b.X, b.Y);
                }
                SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.5f }, npc.Center);
            }

            if (timer >= 140)
                RotateAttack(npc, phase, AttackState.Atlantis);
        }

        // P2 finale: Anahita rings 6 Atlantis tridents around herself, spinning a full-screen aurora net.
        private void ExecuteAtlantisNet(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<AnahitaHeldAtlantis>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                for (int i = 0; i < 6; i++)
                {
                    int dmg = npc.damage / 3;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<AtlantisRingBeamProj>(), dmg, 0f, Main.myPlayer, npc.whoAmI, i * MathHelper.TwoPi / 6f);
                }
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.6f }, npc.Center);
            }

            Vector2 hover = DirectedHoverSpot(npc, target, 0f, -280f, 0f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.04f, Spd(0.08f));

            if (timer >= 260)
                RotateAttack(npc, phase, AttackState.AtlantisNet);
        }
        #endregion

        #region Leviathan Attack States (also the shared conductor: advances timer & rotation for every state)
        private void ExecuteLeviathanAttacks(NPC npc, Player target, AttackState state, ref float timer, ref float tracker, int phase)
        {
            timer++;

            switch (state)
            {
                case AttackState.Leviatitan:
                    ExecuteLeviatitan(npc, target, ref timer, ref tracker, phase);
                    break;
                case AttackState.GastricBelcher:
                    ExecuteGastricBelcher(npc, target, ref timer, ref tracker, phase);
                    break;
                case AttackState.LeviathanTeeth:
                    ExecuteLeviathanTeeth(npc, target, ref timer, ref tracker, phase);
                    break;
                case AttackState.DolphinJump:
                    ExecuteDolphinJump(npc, target, ref timer, ref tracker, phase);
                    break;
                case AttackState.OceanStormTransition:
                    ExecuteOceanStormTransition(npc, target, ref timer, ref tracker, phase);
                    break;
                default:
                    // Anahita's turn — Leviathan just hovers and keeps the shared clock/rotation moving.
                    Vector2 spot = DirectedHoverSpot(npc, target, 480f, -100f, 5f);
                    npc.velocity = Vector2.Lerp(npc.velocity, (spot - npc.Center) * 0.05f, Spd(0.1f));
                    if (timer >= 190)
                        RotateAttack(npc, phase, state);
                    break;
            }
        }

        // LEVIATITAN — A: one giant bubble, 8-needle radial burst (documented).
        //              B: twin smaller bubbles from left/right, 5-needle bursts each — a pincer of needle-fields.
        private void ExecuteLeviatitan(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.Leviatitan) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<LeviathanHeldLeviatitan>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            Vector2 hover = DirectedHoverSpot(npc, target, 480f, -100f, 6f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.06f, Spd(0.11f));

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                if (!variantB)
                {
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 3f, ModContent.ProjectileType<LeviatitanBubbleProj>(), dmg, 0f, Main.myPlayer);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = 8f;
                }
                else
                {
                    foreach (float side in new float[] { -1f, 1f })
                    {
                        Vector2 spawn = npc.Center + new Vector2(0f, side * 90f);
                        Vector2 dir = SafeNormalize(target.Center - spawn, Vector2.UnitY) * 3f;
                        int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, dir, ModContent.ProjectileType<LeviatitanBubbleProj>(), dmg, 0f, Main.myPlayer);
                        if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = 5f;
                    }
                }
                FindHeldWeapon<LeviathanHeldLeviatitan>(npc)?.Pulse(-16f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.Leviatitan);
        }

        // GASTRIC BELCHER — A: one stomach, 5 arcing drops (documented). B: twin stomachs, crossing drops.
        private void ExecuteGastricBelcher(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.GastricBelcher) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<LeviathanHeldGastricBelcher>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            Vector2 hover = DirectedHoverSpot(npc, target, 350f, -260f, 8f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.06f, Spd(0.1f));

            float floorY = Math.Min(bottomTideY - 40f, target.Center.Y + 300f);
            if (timer >= 40 && timer <= 120 && (timer - 40) % 20 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                if (!variantB)
                {
                    Vector2 vel = new(Main.rand.NextFloat(-4f, 4f), 9f);
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<GastricAcidDropProj>(), dmg, 0f, Main.myPlayer);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = floorY;
                }
                else
                {
                    foreach (float side in new float[] { -1f, 1f })
                    {
                        Vector2 spawn = npc.Center + new Vector2(side * 120f, 0f);
                        Vector2 vel = new(-side * 5f, 9f);
                        int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, vel, ModContent.ProjectileType<GastricAcidDropProj>(), dmg, 0f, Main.myPlayer);
                        if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = floorY;
                    }
                }
                FindHeldWeapon<LeviathanHeldGastricBelcher>(npc)?.Pulse(10f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.GastricBelcher);
        }

        // LEVIATHAN TEETH — A: one fan of 8 teeth, boomerang curve-back (documented).
        //                    B: staggered 4+4 fans curving opposite directions — a scissoring return.
        private void ExecuteLeviathanTeeth(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.LeviathanTeeth) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<LeviathanHeldTeeth>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            Vector2 hover = DirectedHoverSpot(npc, target, 400f, 0f, 8f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.06f, Spd(0.11f));

            if (!variantB)
            {
                if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
                    FireToothFan(npc, target, 8, 1f);
            }
            else
            {
                if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
                    FireToothFan(npc, target, 4, 1f);
                if (timer == 55 && Main.netMode != NetmodeID.MultiplayerClient)
                    FireToothFan(npc, target, 4, -1f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.LeviathanTeeth);
        }

        private void FireToothFan(NPC npc, Player target, int count, float curveDir)
        {
            int dmg = npc.damage / 3;
            Vector2 baseDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.Lerp(-0.5f, 0.5f, count <= 1 ? 0.5f : i / (float)(count - 1));
                Vector2 vel = baseDir.RotatedBy(angle) * 14f;
                int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<LeviathanToothProj>(), dmg, 0f, Main.myPlayer, curveDir);
            }
            FindHeldWeapon<LeviathanHeldTeeth>(npc)?.Pulse(14f);
        }

        // P2 finale: Leviathan leaps a huge parabola across the screen, slamming down into two tsunami waves.
        private void ExecuteDolphinJump(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            if (timer == 1)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<LeviathanHeldLeviatitan>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer < 40)
            {
                Vector2 hover = DirectedHoverSpot(npc, target, 500f, 300f, 0f);
                npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.08f, Spd(0.14f));
            }
            else if (timer == 45)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                npc.velocity = dir * 27f;
                SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
                target.Calamity().GeneralScreenShakePower = 12f;
            }
            else if (timer > 45 && timer < 100)
            {
                npc.velocity.Y += 0.45f;
            }
            else if (timer == 100 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.8f }, npc.Center);
                target.Calamity().GeneralScreenShakePower = 10f;
                LeviathanFx.Burst(npc.Center, 6f, 30);
                foreach (float dir in new float[] { -1f, 1f })
                {
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + new Vector2(dir * 150f, 0f), new Vector2(dir * 13f, 0f), ModContent.ProjectileType<TsunamiWaveProj>(), dmg, 0f, Main.myPlayer);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = dir;
                }
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.DolphinJump);
        }

        // Untouchable, screen-darkening cutscene: Leviathan dives, Anahita reforms her shield, and twin
        // water walls sweep in from both edges leaving only a moving 200px gap (design doc's "Whitewater").
        private void ExecuteOceanStormTransition(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            npc.velocity *= 0.9f;
            npc.dontTakeDamage = true;

            if (timer == 1)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
                target.Calamity().GeneralScreenShakePower = 10f;
                shieldActive = true;
                shieldHitsRemaining = 45;
                shieldStunTimer = 0;
                shieldRegenTimer = 0;
                transitionFlashAlpha = 1f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float centerX = target.Center.X;
                    float centerY = (200f + bottomTideY) * 0.5f;
                    foreach (float side in new float[] { -1f, 1f })
                    {
                        int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), new Vector2(centerX, centerY), Vector2.Zero, ModContent.ProjectileType<WhitewaterWallProj>(), npc.damage / 3, 0f, Main.myPlayer, side, centerX);
                    }
                }
            }

            transitionFlashAlpha = MathHelper.Clamp(1f - timer / 90f, 0f, 1f) * 0.5f;

            if (timer >= 90)
            {
                npc.dontTakeDamage = false;
                transitionFlashAlpha = 0f;
                attackCycleIndex = 0;
                npc.ai[1] = (float)P2Cycle[0];
                timer = 0;
                tracker = 0;
                npc.netUpdate = true;
            }
        }

        // 独立殒落 — 五段演出, 分别按利维坦(深潜巨兽)/阿纳西塔(冰棱术士)两种身份走不同的中段, 而不是通用爆炸.
        // 两个实体各自独立播放, 互不干扰对方的战斗状态(见 deathAnimTimer 字段注释).
        // 震颤 -> 中段(阿纳西塔:冰棱汇聚回收环绕加速 / 利维坦:甩尾俯冲) -> 独有收尾 -> 上腾聚能 -> 终末爆发.
        private void ExecuteDeathAnimation(NPC npc, Player target, bool isAnahita, int t)
        {
            npc.damage = 0;
            npc.dontTakeDamage = true;
            int themeDust = isAnahita ? DustID.IceRod : DustID.DungeonWater;

            if (t < 25)
            {
                npc.velocity *= 0.9f;
                npc.rotation += MathF.Sin(t * 1.2f) * 0.1f;
                if (t % 2 == 0)
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(60f, 60f), themeDust, Main.rand.NextVector2Circular(3f, 3f), 100, default, 1.3f);
                    d.noGravity = true;
                }
            }
            else if (t < 70)
            {
                if (isAnahita)
                {
                    // 冰环加速环绕 — orbiting shards spin faster as the prism sorceress unravels
                    float a = (t - 25) * 0.3f;
                    npc.velocity *= 0.92f;
                    if (t % 2 == 0)
                    {
                        Vector2 spawn = npc.Center + a.ToRotationVector2() * 80f;
                        Dust d = Dust.NewDustPerfect(spawn, DustID.IceRod, Vector2.Zero, 100, default, 1.2f);
                        d.noGravity = true;
                    }
                }
                else
                {
                    // 甩尾俯冲前奏 — the same whip-thrash lash every worm-like body in this roster uses, gone loose
                    float tt = t - 25;
                    Vector2 whipDir = Vector2.UnitX.RotatedBy(MathF.Sin(tt * 0.35f) * 2f);
                    npc.velocity = Vector2.Lerp(npc.velocity, whipDir * 13f, 0.15f);
                    npc.rotation = npc.velocity.ToRotation();
                    if (t % 4 == 0)
                        LeviathanFx.Burst(npc.Center, 3f, 6);
                }
            }
            else if (t < 105)
            {
                if (isAnahita)
                {
                    // 冰棱残躯回收 — the shattered ice-shield identity pulls its own remnants back inward
                    if (t % 3 == 0)
                    {
                        Vector2 spawn = npc.Center + Main.rand.NextVector2CircularEdge(150f, 150f);
                        Dust d = Dust.NewDustPerfect(spawn, DustID.IceRod, (npc.Center - spawn) * 0.07f, 100, default, 1.3f);
                        d.noGravity = true;
                    }
                }
                else
                {
                    // 深潜 — dives as if sinking back into the trench it came from
                    npc.velocity = Vector2.Lerp(npc.velocity, new Vector2(0f, 7f), 0.06f);
                    if (t % 2 == 0)
                    {
                        Dust d = Dust.NewDustPerfect(npc.Center + new Vector2(Main.rand.NextFloat(-30f, 30f), 60f), DustID.DungeonWater, new Vector2(0f, -2f), 100, default, 1.3f);
                        d.noGravity = true;
                    }
                }
            }
            else if (t < 140)
            {
                // 上腾聚能 — both entities rise together into their finale, the cinematic pull peaks here
                npc.velocity = Vector2.Lerp(npc.velocity, new Vector2(0f, -7f), 0.06f);
                if (t % 2 == 0)
                {
                    Vector2 spawn = npc.Center + Main.rand.NextVector2CircularEdge(90f, 90f);
                    Dust d = Dust.NewDustPerfect(spawn, themeDust, (npc.Center - spawn) * 0.06f, 100, default, 1.3f);
                    d.noGravity = true;
                }
            }
            else
            {
                // 终末爆发 — the actual kill fires once, everything after is the lingering burst
                if (t == 140)
                {
                    npc.velocity = Vector2.Zero;
                    SoundEngine.PlaySound(SoundID.NPCDeath4, npc.Center);
                    target.Calamity().GeneralScreenShakePower = 12f;
                    LeviathanFx.Burst(npc.Center, 7f, 36, themeDust);
                }

                if (t >= 160)
                {
                    npc.dontTakeDamage = false;
                    npc.StrikeInstantKill();
                }
            }
        }
        #endregion

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            int anahitaType = ModContent.Find<ModNPC>("CalamityMod/Anahita").Type;
            bool isAnahita = npc.type == anahitaType;

            Texture2D tex = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() / 2f;

            Vector2[] positions = isAnahita ? oldPositionsA : oldPositionsL;
            int posIdx = isAnahita ? oldPositionsIndexA : oldPositionsIndexL;
            Color trailBase = isAnahita ? new Color(120, 220, 255, 0) : new Color(60, 160, 255, 0);

            for (int i = 0; i < positions.Length; i++)
            {
                int idx = (posIdx - i - 1 + positions.Length) % positions.Length;
                if (positions[idx] == Vector2.Zero) continue;
                float alpha = (1f - i / (float)positions.Length) * 0.55f;
                spriteBatch.Draw(tex, positions[idx] - screenPos, frame, trailBase * alpha, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            }

            if (transitionFlashAlpha > 0f)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * transitionFlashAlpha);

            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            int anahitaType = ModContent.Find<ModNPC>("CalamityMod/Anahita").Type;
            bool isAnahita = npc.type == anahitaType;
            Color glowColor = isAnahita ? new Color(120, 220, 255, 0) * 0.35f : new Color(60, 160, 255, 0) * 0.35f;

            Texture2D tex = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() / 2f;

            if (isAnahita)
            {
                Player target = Main.player[npc.target];
                Vector2 startUpper = new Vector2(target.Center.X - 1200f, 200f);
                Vector2 endUpper = new Vector2(target.Center.X + 1200f, 200f);
                Vector2 startLower = new Vector2(target.Center.X - 1200f, bottomTideY);
                Vector2 endLower = new Vector2(target.Center.X + 1200f, bottomTideY);

                LegendsWeaponBossVisuals.DrawLine(spriteBatch, startUpper, endUpper, Color.DeepSkyBlue * 0.7f, 5f);
                LegendsWeaponBossVisuals.DrawLine(spriteBatch, startLower, endLower, Color.DeepSkyBlue * 0.7f, 5f);

                if (shieldActive)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = i * MathHelper.TwoPi / 3f + ticksRunning * 0.03f;
                        Vector2 pos = npc.Center + angle.ToRotationVector2() * 80f;
                        spriteBatch.Draw(TextureAssets.Dust.Value, pos - screenPos, new Rectangle(0, 0, 8, 8), Color.DeepSkyBlue * 0.9f, ticksRunning * 0.05f, new Vector2(4f, 4f), 4f, SpriteEffects.None, 0f);
                    }
                }
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            spriteBatch.Draw(tex, npc.Center - screenPos, frame, glowColor, npc.rotation, origin, npc.scale * 1.08f, SpriteEffects.None, 0f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }
        #endregion

        private static T FindHeldWeapon<T>(NPC npc) where T : BossHeldWeaponBase
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.ModProjectile is T held && (int)p.ai[0] == npc.whoAmI)
                    return held;
            }
            return null;
        }
    }
}
