using System;
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

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.Ravager
{
    // Only RavagerBody is routed through this custom AI. RavagerClawLeft/Right, RavagerLegLeft/Right and
    // RavagerHead/Head2 are left completely unregistered — they keep running their REAL, unmodified Calamity
    // AI, which already gives the claws their native "dash out on a chain, then retract" grapple behavior.
    // That's a free ambient pressure layer; the custom rotation below is the main event layered on top of it.
    internal sealed class RavagerAI : LegendsBossAI
    {
        #region Constants & Configurations
        public override int NPCType => ModContent.Find<ModNPC>("CalamityMod/RavagerBody").Type;
        public override string BossName => "Ravager";
        public override Color DebugColor => new(180, 50, 50);

        public override int MaxPhaseCount => 2;
        public override float[] PhaseLifeRatios => new[] { 0.50f };
        public override int AttackCycleLength => 120;
        public override float MotionIntensity => 0.8f;
        #endregion

        #region Attack States
        public enum AttackState
        {
            UltimusCleaver = 0,
            RealmRavager = 1,
            Hematemesis = 2,
            CraniumSmasher = 3,
            Vesuvius = 4,
            CorpusAvertor = 5,
            Mutilator = 6,
            Lacerator = 7,
            ClaretCannon = 8,
            ArterialAssault = 9,
            BloodBoiler = 10,
            SanguineFlare = 11,
            Viscera = 12,
            DragonbloodDisgorger = 13,
            BloodsoakedCrasher = 14,
            ReactorOverloadTransition = 15,
            DeathAnimation = 16
        }
        #endregion

        #region Fields
        private int ticksRunning = 0;
        private int currentRepetition = 0;
        private readonly Vector2[] oldPositions = new Vector2[14];
        private int oldPositionsIndex;

        private int attackCycleIndex = 0;
        private static readonly AttackState[] P1Cycle =
        {
            AttackState.UltimusCleaver, AttackState.RealmRavager, AttackState.Hematemesis,
            AttackState.CraniumSmasher, AttackState.Vesuvius, AttackState.CorpusAvertor,
        };
        private static readonly AttackState[] P2Cycle =
        {
            AttackState.Mutilator, AttackState.Lacerator, AttackState.ClaretCannon, AttackState.ArterialAssault,
            AttackState.BloodBoiler, AttackState.SanguineFlare, AttackState.Viscera,
            AttackState.DragonbloodDisgorger, AttackState.BloodsoakedCrasher,
        };

        // Per-attack variant toggle: flips deterministically each time that attack slot comes up (no RNG).
        private readonly bool[] attackVariant = new bool[16];
        private bool UseVariantB(AttackState state)
        {
            int i = (int)state;
            bool v = attackVariant[i];
            attackVariant[i] = !v;
            return v;
        }

        // Flesh Totem tracking
        private int totemSpawnTimer = 0;
        private int totemNPCIndex = -1;
        private Vector2 totemCenter = Vector2.Zero;
        private bool wasTotemAlive = false;
        private int totemSlowTimer = 0;
        private int arenaHurtCooldown = 0;

        // Limb flags
        private bool legsAlive = true;
        private bool clawsAlive = true;
        private bool limbsActive = true;

        // limbsActive gate 90% 减伤，三个标志位共同决定当前处于哪个肢体阶段 —— 必须过网，
        // 否则打断肢体的玩家在打真伤、队友却还在打 10%，而且他们那端看到的肢体形态也是错的。
        protected override void DeclareSyncedFields(LegendsSyncedFields f) => f
            .Bool(() => legsAlive, v => legsAlive = v)
            .Bool(() => clawsAlive, v => clawsAlive = v)
            .Bool(() => limbsActive, v => limbsActive = v);

        private float transitionFlashAlpha = 0f;
        #endregion

        #region Per-fight State Reset
        // 跨场次状态清理（为什么需要见基类 LegendsBossAI.ResetFightState；调用时机由框架负责）。
        // 本 Boss 残留最要命的是三个肢体标志位：上一场把腿和爪都打断了才杀掉的话，
        // 下一场 Ravager 一出生就是"四肢已断"的形态，整套肢体阶段直接被跳过。
        // totemNPCIndex 也必须还原成 -1，否则会指向上一场那个早就不存在的图腾槽位。
        public override void ResetFightState(NPC npc, Player target)
        {
            ticksRunning = 0;
            currentRepetition = 0;
            attackCycleIndex = 0;
            Array.Clear(attackVariant, 0, attackVariant.Length);

            legsAlive = true;
            clawsAlive = true;
            limbsActive = true;

            totemSpawnTimer = 0;
            totemNPCIndex = -1;
            totemCenter = Vector2.Zero;
            wasTotemAlive = false;
            totemSlowTimer = 0;
            arenaHurtCooldown = 0;

            transitionFlashAlpha = 0f;
            ultimusVariantB = false;

            for (int i = 0; i < oldPositions.Length; i++)
                oldPositions[i] = npc.Center;
            oldPositionsIndex = 0;
        }
        #endregion

        #region Core AI Hooks
        public override bool PreAI(NPC npc, LegendsGlobalNPC data)
        {
            ticksRunning++;
            oldPositions[oldPositionsIndex] = npc.Center;
            oldPositionsIndex = (oldPositionsIndex + 1) % oldPositions.Length;

            if (!TryGetTarget(npc, out Player target))
            {
                npc.velocity.Y += 0.5f;
                if (npc.timeLeft > 60) npc.timeLeft = 60;
                return false;
            }

            int currentPhase = (int)npc.ai[0];
            AttackState state = (AttackState)(int)npc.ai[1];
            ref float timer = ref npc.ai[2];
            ref float stateTracker = ref npc.ai[3];

            if (currentPhase == 0)
            {
                currentPhase = 1;
                npc.ai[0] = 1f;
                state = AttackState.UltimusCleaver;
                npc.ai[1] = (float)state;
                currentRepetition = 0;
                attackCycleIndex = 0;

                int totemType = ModContent.Find<ModNPC>("CalamityMod/FleshTotem").Type;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int idx = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, totemType);
                    if (idx >= 0 && idx < Main.maxNPCs)
                    {
                        Main.npc[idx].netUpdate = true;
                        totemNPCIndex = idx;
                        totemCenter = Main.npc[idx].Center;
                        wasTotemAlive = true;
                    }
                }

                npc.netUpdate = true;
            }

            // Single real transition at 50% HP.
            float lifeRatio = npc.lifeMax <= 0 ? 1f : npc.life / (float)npc.lifeMax;
            int nextPhase = lifeRatio <= PhaseLifeRatios[0] ? 2 : 1;

            if (nextPhase > currentPhase && state != AttackState.ReactorOverloadTransition)
            {
                currentPhase = nextPhase;
                npc.ai[0] = currentPhase;
                state = AttackState.ReactorOverloadTransition;
                npc.ai[1] = (float)state;
                timer = 0;
                stateTracker = 0;
                npc.netUpdate = true;
            }

            if (state != AttackState.DeathAnimation)
            {
                CheckLimbStatus(npc);
                UpdateFleshTotem(npc, target, currentPhase);

                npc.rotation = npc.velocity.X * 0.02f;
                npc.scale = 1.0f + (float)Math.Sin(ticksRunning * 0.05f) * 0.02f;
            }

            switch (state)
            {
                case AttackState.UltimusCleaver:
                    ExecuteUltimusCleaver(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.RealmRavager:
                    ExecuteRealmRavager(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.Hematemesis:
                    ExecuteHematemesis(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.CraniumSmasher:
                    ExecuteCraniumSmasher(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.Vesuvius:
                    ExecuteVesuvius(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.CorpusAvertor:
                    ExecuteCorpusAvertor(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.Mutilator:
                    ExecuteMutilator(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.Lacerator:
                    ExecuteLacerator(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.ClaretCannon:
                    ExecuteClaretCannon(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.ArterialAssault:
                    ExecuteArterialAssault(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.BloodBoiler:
                    ExecuteBloodBoiler(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.SanguineFlare:
                    ExecuteSanguineFlare(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.Viscera:
                    ExecuteViscera(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.DragonbloodDisgorger:
                    ExecuteDragonbloodDisgorger(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.BloodsoakedCrasher:
                    ExecuteBloodsoakedCrasher(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.ReactorOverloadTransition:
                    ExecuteTransition(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.DeathAnimation:
                    ExecuteDeathAnimation(npc, target, ref timer);
                    break;
            }

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            ApplyDefenseModifiers(npc, ref modifiers);
            InterceptLethalHit(npc, ref modifiers, (int)AttackState.DeathAnimation, () => BeginDeathAnimation(npc, player));
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            ApplyDefenseModifiers(npc, ref modifiers);
            InterceptLethalHit(npc, ref modifiers, (int)AttackState.DeathAnimation, () => BeginDeathAnimation(npc, Main.player[projectile.owner]));
        }

        private void ApplyDefenseModifiers(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (npc.ai[1] == (float)AttackState.ReactorOverloadTransition)
            {
                modifiers.FinalDamage *= 0f; // fully invulnerable during the reactor-overload cutscene
                return;
            }

            if (limbsActive)
            {
                modifiers.FinalDamage *= 0.10f; // 90% DR while any limb is active
            }
        }
        #endregion

        #region Segment/Shield Helper Systems
        private void CheckLimbStatus(NPC npc)
        {
            int clawL = ModContent.Find<ModNPC>("CalamityMod/RavagerClawLeft").Type;
            int clawR = ModContent.Find<ModNPC>("CalamityMod/RavagerClawRight").Type;
            int legL = ModContent.Find<ModNPC>("CalamityMod/RavagerLegLeft").Type;
            int legR = ModContent.Find<ModNPC>("CalamityMod/RavagerLegRight").Type;

            legsAlive = false;
            clawsAlive = false;
            limbsActive = false;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n.active)
                {
                    if (n.type == clawL || n.type == clawR) { clawsAlive = true; limbsActive = true; }
                    if (n.type == legL || n.type == legR) { legsAlive = true; limbsActive = true; }
                }
            }
        }

        private void UpdateFleshTotem(NPC npc, Player target, int currentPhase)
        {
            if (arenaHurtCooldown > 0)
                arenaHurtCooldown--;
            if (totemSlowTimer > 0)
                totemSlowTimer--;

            if (currentPhase >= 2)
                return; // Cage expands to full-screen (disabled) once the reactor is unleashed

            int totemType = ModContent.Find<ModNPC>("CalamityMod/FleshTotem").Type;

            bool totemAlive = false;
            if (totemNPCIndex >= 0 && totemNPCIndex < Main.maxNPCs)
            {
                NPC t = Main.npc[totemNPCIndex];
                if (t.active && t.type == totemType)
                {
                    totemAlive = true;
                    totemCenter = t.Center;
                }
            }

            // Rising edge (alive -> dead): totem was just destroyed, punish the Body with a stagger.
            if (wasTotemAlive && !totemAlive)
            {
                totemSlowTimer = 240; // 4s action delay (design doc)
                RavagerFx.Burst(totemCenter, 6f, 24);
                SoundEngine.PlaySound(SoundID.NPCDeath12 with { Volume = 0.8f, Pitch = -0.4f }, totemCenter);
            }
            wasTotemAlive = totemAlive;

            // Staggered: rusted smoke coughs off the frame while the machine reboots
            if (totemSlowTimer > 0 && Main.rand.NextBool(2))
            {
                Dust smoke = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(100f, 80f), DustID.Smoke, new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -Main.rand.NextFloat(1f, 2.5f)), 130, default, 1.5f);
                smoke.noGravity = true;
            }

            if (!totemAlive)
            {
                totemSpawnTimer++;
                if (totemSpawnTimer >= 1500) // 25s regen window
                {
                    totemSpawnTimer = 0;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int idx = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, totemType);
                        if (idx >= 0 && idx < Main.maxNPCs)
                        {
                            Main.npc[idx].netUpdate = true;
                            totemNPCIndex = idx;
                            totemCenter = Main.npc[idx].Center;
                            wasTotemAlive = true;
                        }
                    }
                }
            }
            else
            {
                // The totem's 700px leash ring must be visible: crimson motes trace the circle,
                // denser on the side the player is drifting toward
                for (int i = 0; i < 2; i++)
                {
                    float a = Main.rand.NextFloat(MathHelper.TwoPi);
                    Dust ring = Dust.NewDustPerfect(totemCenter + a.ToRotationVector2() * 700f, DustID.CrimsonTorch, Vector2.Zero, 160, default, 1.05f);
                    ring.noGravity = true;
                    ring.fadeIn = 1f;
                }
                Vector2 dist = target.Center - totemCenter;
                if (dist.Length() > 560f && Main.rand.NextBool(2))
                {
                    Vector2 edge = totemCenter + dist.SafeNormalize(Vector2.UnitX) * 700f;
                    Dust warn = Dust.NewDustPerfect(edge + Main.rand.NextVector2Circular(60f, 60f), DustID.CrimsonTorch, Vector2.Zero, 100, default, 1.35f);
                    warn.fadeIn = 1.2f;
                    warn.noGravity = true;
                }
                if (dist.Length() > 700f)
                {
                    target.AddBuff(BuffID.Poisoned, 180);
                    target.AddBuff(BuffID.Slow, 180);
                    target.velocity += SafeNormalize(totemCenter - target.Center, Vector2.Zero) * 2f;
                    if (arenaHurtCooldown <= 0)
                    {
                        arenaHurtCooldown = 30;
                        target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 15, 0);
                    }
                }
            }

            if (totemSlowTimer > 0)
                npc.velocity *= 0.9f; // staggered — the totem's collapse punished the Body's tempo
        }

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

        #region Attack Rotations
        private void RotateAttack(NPC npc, int currentPhase, AttackState current)
        {
            if (currentPhase == 1)
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
                AttackState next = P1Cycle[attackCycleIndex % P1Cycle.Length];

                // Skip legs/claws dependent attacks once those limbs are destroyed (design doc: destroying
                // the legs removes ground-crack/rock-spike; destroying the claws removes skull-smash).
                if (next == AttackState.UltimusCleaver && !legsAlive)
                {
                    attackCycleIndex++;
                    next = P1Cycle[attackCycleIndex % P1Cycle.Length];
                }
                if (next == AttackState.CraniumSmasher && !clawsAlive)
                {
                    attackCycleIndex++;
                    next = P1Cycle[attackCycleIndex % P1Cycle.Length];
                }

                npc.ai[1] = (float)next;
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

        #region P1 Attack States
        private bool ultimusVariantB;

        // ULTIMUS CLEAVER — A: single leap-slam, rock spires erupt left-to-right from impact (documented).
        //                    B: same leap-slam, spires erupt as a full ring around the impact point instead.
        private void ExecuteUltimusCleaver(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                ultimusVariantB = UseVariantB(AttackState.UltimusCleaver);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<RavagerHeldUltimusCleaver>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer < 40)
            {
                Vector2 spot = DirectedHoverSpot(npc, target, 0f, -320f, 8f);
                npc.velocity = Vector2.Lerp(npc.velocity, (spot - npc.Center) * 0.08f, 0.16f);
            }
            else if (timer == 50)
            {
                npc.velocity = new Vector2(0f, 22f);
            }
            else if (timer > 50 && npc.velocity.Y == 0f && tracker == 0f)
            {
                tracker = 1f;
                int dmg = npc.damage / 3;
                SoundEngine.PlaySound(SoundID.Item14 with { Pitch = 0.15f }, npc.Center);
                RavagerFx.Burst(npc.Center, 5f, 16);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (!ultimusVariantB)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            Vector2 spawn = npc.Center + new Vector2(i * 80f - 280f, 0f);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<SpikecragSpireProj>(), dmg, 0f, Main.myPlayer);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            float angle = i * MathHelper.TwoPi / 8f;
                            Vector2 spawn = npc.Center + angle.ToRotationVector2() * 260f;
                            Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<SpikecragSpireProj>(), dmg, 0f, Main.myPlayer);
                        }
                    }
                }
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.UltimusCleaver);
        }

        // REALM RAVAGER — A: single horizontal rift that blows open into a full-height net (documented).
        //                 B: a slow rift-fire wall presses in from one screen edge.
        private void ExecuteRealmRavager(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.RealmRavager) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<RavagerHeldRealmRavager>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            Vector2 hover = DirectedHoverSpot(npc, target, 380f, -120f, 6f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.07f, 0.14f);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                if (!variantB)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), new Vector2(target.Center.X, target.Center.Y), Vector2.Zero, ModContent.ProjectileType<RealmRiftProj>(), dmg, 0f, Main.myPlayer);
                }
                else
                {
                    float side = Math.Sign(npc.Center.X - target.Center.X);
                    if (side == 0f) side = 1f;
                    Vector2 spawn = target.Center + new Vector2(side * 1100f, 0f);
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<RiftWallProj>(), dmg, 0f, Main.myPlayer);
                    if (idx >= 0 && idx < Main.maxProjectiles)
                        Main.projectile[idx].ai[0] = -side * 6f;
                }
                FindHeldWeapon<RavagerHeldRealmRavager>(npc)?.Pulse(-12f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.RealmRavager);
        }

        // HEMATEMESIS — A: 3 arcing blasts, each splits into 6 radial droplets (documented).
        //               B: single larger blast, splits into a full 12-point ring.
        private void ExecuteHematemesis(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.Hematemesis) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<RavagerHeldHematemesis>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            Vector2 hover = DirectedHoverSpot(npc, target, 0f, -280f, 4f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.06f, 0.1f);

            if (timer == 50 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                if (!variantB)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 vel = new(Main.rand.NextFloat(-4f, 4f), -12f + i * 2f);
                        int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<HematemesisBloodProj>(), dmg, 0f, Main.myPlayer);
                        if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = 6f;
                    }
                }
                else
                {
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, new Vector2(0f, -13f), ModContent.ProjectileType<HematemesisBloodProj>(), dmg, 0f, Main.myPlayer);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = 12f;
                }
                FindHeldWeapon<RavagerHeldHematemesis>(npc)?.Pulse(10f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.Hematemesis);
        }

        // CRANIUM SMASHER — A: single flail, pause-then-retract with bone scatter (documented).
        //                    B: twin flails thrown in a V pattern.
        private void ExecuteCraniumSmasher(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.CraniumSmasher) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<RavagerHeldCraniumSmasher>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            Vector2 hover = DirectedHoverSpot(npc, target, 280f, -240f, 7f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.07f, 0.14f);

            if (timer == 60 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                if (!variantB)
                {
                    Vector2 vel = dir * 16f;
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<CraniumFlailProj>(), dmg, 0f, Main.myPlayer, 40f, npc.Center.X);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[2] = npc.Center.Y;
                }
                else
                {
                    foreach (float spread in new float[] { -0.3f, 0.3f })
                    {
                        Vector2 vel = dir.RotatedBy(spread) * 16f;
                        int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<CraniumFlailProj>(), dmg, 0f, Main.myPlayer, 34f, npc.Center.X);
                        if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[2] = npc.Center.Y;
                    }
                }
                FindHeldWeapon<RavagerHeldCraniumSmasher>(npc)?.Pulse(16f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.CraniumSmasher);
        }

        // VESUVIUS — A: single central eruption, 12 lava bombs (documented). B: twin eruptions, flanking.
        private void ExecuteVesuvius(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.Vesuvius) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<RavagerHeldVesuvius>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            Vector2 hover = DirectedHoverSpot(npc, target, 0f, -340f, 5f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.06f, 0.12f);

            if (timer == 50 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.2f }, npc.Center);
                Vector2[] origins = variantB ? new[] { npc.Center + new Vector2(-200f, 0f), npc.Center + new Vector2(200f, 0f) } : new[] { npc.Center };
                int perOrigin = variantB ? 7 : 12;
                foreach (Vector2 origin in origins)
                {
                    for (int i = 0; i < perOrigin; i++)
                    {
                        Vector2 vel = new(Main.rand.NextFloat(-6f, 6f), -14f + Main.rand.NextFloat(-3f, 3f));
                        Projectile.NewProjectile(npc.GetSource_FromAI(), origin, vel, ModContent.ProjectileType<VesuviusEmberProj>(), dmg, 0f, Main.myPlayer);
                    }
                }
                FindHeldWeapon<RavagerHeldVesuvius>(npc)?.Pulse(-14f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.Vesuvius);
        }

        // CORPUS AVERTOR — A: twin crescents 90-degree turn diverging up/down (documented).
        //                  B: reversed turn direction (down/up) plus a third crescent from below.
        private void ExecuteCorpusAvertor(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.CorpusAvertor) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<RavagerHeldCorpusAvertor>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            Vector2 hover = DirectedHoverSpot(npc, target, 0f, -260f, 5f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.07f, 0.14f);

            if (timer == 60 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                float turnSign = variantB ? -1f : 1f;
                int d1 = Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + new Vector2(-400f, -100f), new Vector2(10f, 0f), ModContent.ProjectileType<CorpusAvertorProj>(), dmg, 0f, Main.myPlayer, 30f, turnSign);
                int d2 = Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + new Vector2(400f, -100f), new Vector2(-10f, 0f), ModContent.ProjectileType<CorpusAvertorProj>(), dmg, 0f, Main.myPlayer, 30f, -turnSign);
                if (variantB)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + new Vector2(0f, 350f), new Vector2(0f, -10f), ModContent.ProjectileType<CorpusAvertorProj>(), dmg, 0f, Main.myPlayer, 30f, 1f);
                }
                FindHeldWeapon<RavagerHeldCorpusAvertor>(npc)?.Pulse(12f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.CorpusAvertor);
        }
        #endregion

        #region P2 Attack States
        private void ExecuteMutilator(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<RavagerHeldMutilator>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 hover = DirectedHoverSpot(npc, target, 260f, -220f, 6f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.08f, 0.15f);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, new Vector2(12f, 4f), ModContent.ProjectileType<MutilatorWaveProj>(), dmg, 0f, Main.myPlayer);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, new Vector2(12f, -4f), ModContent.ProjectileType<MutilatorWaveProj>(), dmg, 0f, Main.myPlayer);
            }

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.Mutilator);
        }

        private void ExecuteLacerator(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<RavagerHeldLacerator>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                int dmg = npc.damage / 3;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<LaceratorYoyoProj>(), dmg, 0f, Main.myPlayer, npc.whoAmI, 0f);
            }

            Vector2 hover = DirectedHoverSpot(npc, target, 300f, -220f, 6f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.06f, 0.11f);

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.Lacerator);
        }

        private void ExecuteClaretCannon(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<RavagerHeldClaretCannon>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 hover = DirectedHoverSpot(npc, target, 0f, -320f, 0f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.05f, 0.1f);
            FindHeldWeapon<RavagerHeldClaretCannon>(npc)?.SetAim((target.Center - npc.Center).ToRotation());

            if (timer >= 50 && timer <= 170 && timer % 8 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 16f;
                int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<ClaretBoltProj>(), dmg, 0f, Main.myPlayer);
                if (idx >= 0 && idx < Main.maxProjectiles)
                {
                    Main.projectile[idx].ai[0] = 700f;
                    Main.projectile[idx].ai[1] = target.Center.X;
                }
                FindHeldWeapon<RavagerHeldClaretCannon>(npc)?.Pulse(4f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.ClaretCannon);
        }

        private void ExecuteArterialAssault(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<RavagerHeldArterialAssault>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 hover = DirectedHoverSpot(npc, target, 0f, -300f, 0f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.06f, 0.11f);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                float side = Math.Sign(npc.Center.X - target.Center.X);
                if (side == 0f) side = 1f;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 spawn = target.Center + new Vector2(side * 1000f - side * i * 90f, 0f);
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<ArterialColumnProj>(), dmg, 0f, Main.myPlayer);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = -side * 6f;
                }
                FindHeldWeapon<RavagerHeldArterialAssault>(npc)?.Pulse(10f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.ArterialAssault);
        }

        private void ExecuteBloodBoiler(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<RavagerHeldBloodBoiler>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 hover = DirectedHoverSpot(npc, target, 250f, -220f, 6f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.07f, 0.13f);

            if (timer >= 50 && timer <= 160 && timer % 4 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy(Main.rand.NextFloat(-0.25f, 0.25f)) * 12f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<BloodBoilerFlameProj>(), dmg, 0f, Main.myPlayer);
                FindHeldWeapon<RavagerHeldBloodBoiler>(npc)?.Pulse(6f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.BloodBoiler);
        }

        private void ExecuteSanguineFlare(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<RavagerHeldSanguineFlare>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 hover = DirectedHoverSpot(npc, target, 0f, -320f, 0f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.05f, 0.1f);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3 + 10;
                Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<SanguineFlareProj>(), dmg, 0f, Main.myPlayer);
                FindHeldWeapon<RavagerHeldSanguineFlare>(npc)?.Pulse(14f);
            }

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.SanguineFlare);
        }

        private void ExecuteViscera(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<RavagerHeldViscera>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 hover = DirectedHoverSpot(npc, target, 300f, -240f, 5f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.06f, 0.12f);

            if (timer == 60 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * MathHelper.TwoPi / 6f;
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, angle.ToRotationVector2() * 8f, ModContent.ProjectileType<VisceraSpireProj>(), dmg, 0f, Main.myPlayer, npc.whoAmI);
                }
                FindHeldWeapon<RavagerHeldViscera>(npc)?.Pulse(8f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.Viscera);
        }

        private void ExecuteDragonbloodDisgorger(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<RavagerHeldDragonbloodDisgorger>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 hover = DirectedHoverSpot(npc, target, 260f, -200f, 6f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.06f, 0.11f);

            if (timer >= 40 && timer <= 90 && timer % 6 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 8f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<DragonbloodLavaProj>(), dmg, 0f, Main.myPlayer);
                FindHeldWeapon<RavagerHeldDragonbloodDisgorger>(npc)?.Pulse(6f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.DragonbloodDisgorger);
        }

        private void ExecuteBloodsoakedCrasher(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<RavagerHeldBloodsoakedCrasher>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer < 45)
            {
                Vector2 hover = DirectedHoverSpot(npc, target, 0f, -360f, 5f);
                npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.1f, 0.18f);
            }
            else if (timer == 50)
            {
                npc.velocity = new Vector2(0f, 26f);
            }
            else if (timer > 50 && npc.velocity.Y == 0f && tracker == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                tracker = 1;
                int dmg = npc.damage / 3;
                SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.15f }, npc.Center);
                Player t = target;
                if (t.active) t.Calamity().GeneralScreenShakePower = 8f;
                RavagerFx.Burst(npc.Center, 6f, 24);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, new Vector2(-14f, 0f), ModContent.ProjectileType<BloodsoakedWaveProj>(), dmg, 0f, Main.myPlayer);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, new Vector2(14f, 0f), ModContent.ProjectileType<BloodsoakedWaveProj>(), dmg, 0f, Main.myPlayer);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.BloodsoakedCrasher);
        }

        private void ExecuteTransition(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            npc.velocity *= 0.9f;
            npc.dontTakeDamage = true;

            const int shellStrip = 45, coreReveal = 90;
            if (timer < shellStrip)
                transitionFlashAlpha = MathHelper.Clamp(timer / (float)shellStrip, 0f, 1f);
            else
                transitionFlashAlpha = MathHelper.Clamp(1f - (timer - shellStrip) / (float)(coreReveal - shellStrip), 0f, 1f);

            if (timer == 1)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
                target.Calamity().GeneralScreenShakePower = 8f;

                int totemType = ModContent.Find<ModNPC>("CalamityMod/FleshTotem").Type;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == totemType)
                        Main.npc[i].active = false;
                }
            }

            if (timer == coreReveal)
            {
                SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.7f }, npc.Center);
                target.Calamity().GeneralScreenShakePower = 12f;
                RavagerFx.Burst(npc.Center, 8f, 40);
            }

            if (timer >= coreReveal + 10)
            {
                transitionFlashAlpha = 0f;
                npc.dontTakeDamage = false;

                attackCycleIndex = 0;
                npc.ai[1] = (float)P2Cycle[0];
                npc.ai[2] = 0;
                npc.ai[3] = 0;
                npc.netUpdate = true;
            }
        }
        #endregion

        #region Death Animation
        // 血肉终焉 — 五段演出, 让躯壳/血肉图腾身份成为演出主角, 而不是通用爆炸:
        // 躯壳崩解 -> 残躯狂暴痉挛(前后踉跄) -> 血肉图腾回响涌动 -> 核心过载上腾 -> 终末血爆.
        private void BeginDeathAnimation(NPC npc, Player target)
        {
            npc.ai[1] = (float)AttackState.DeathAnimation;
            npc.ai[2] = 0f;
            npc.ai[3] = 0f;
            npc.netUpdate = true;

            int totemType = ModContent.Find<ModNPC>("CalamityMod/FleshTotem").Type;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == totemType)
                    Main.npc[i].active = false;
            }

            TriggerDeathCinematic(npc, target, focusStrength: 0.55f, holdFrames: 55, shakePower: 10f);
            SoundEngine.PlaySound(SoundID.Roar with { Volume = 1f, Pitch = -0.3f }, npc.Center);
        }

        private void ExecuteDeathAnimation(NPC npc, Player target, ref float timer)
        {
            npc.damage = 0;
            npc.dontTakeDamage = true;

            if (timer < 25f)
            {
                // 躯壳崩解 — the same shell-strip visual as the reactor-overload transition, this time for good
                npc.velocity *= 0.9f;
                npc.rotation += MathF.Sin(timer * 1.2f) * 0.08f;
                if ((int)timer % 2 == 0)
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(80f, 80f), DustID.Blood, Main.rand.NextVector2Circular(3f, 3f), 100, default, 1.4f);
                    d.noGravity = true;
                }
            }
            else if (timer < 70f)
            {
                // 残躯狂暴痉挛 — a heavy beast lurches forward and back rather than whipping like a worm
                float t = timer - 25f;
                float lurch = MathF.Sin(t * 0.28f) * 8f;
                npc.velocity = Vector2.Lerp(npc.velocity, new Vector2(lurch, 0f), 0.15f);
                npc.rotation += lurch * 0.01f;
                if ((int)t % 4 == 0)
                    RavagerFx.Burst(npc.Center, 3f, 6);
            }
            else if (timer < 105f)
            {
                // 血肉图腾回响涌动 — the Flesh Totem's own pulse rhythm plays out on the boss itself, one last time
                float t = timer - 70f;
                float pulse = (MathF.Sin(t * 0.5f) + 1f) * 0.5f;
                if ((int)t % 2 == 0)
                {
                    Vector2 spawn = npc.Center + Main.rand.NextVector2CircularEdge(MathHelper.Lerp(40f, 160f, pulse), MathHelper.Lerp(40f, 160f, pulse));
                    Dust d = Dust.NewDustPerfect(spawn, DustID.Blood, (npc.Center - spawn) * 0.05f, 100, default, 1.3f);
                    d.noGravity = true;
                }
            }
            else if (timer < 140f)
            {
                // 核心过载上腾 — rises while the final overload builds, the cinematic pull peaks here
                npc.velocity = Vector2.Lerp(npc.velocity, new Vector2(0f, -6f), 0.06f);
                if ((int)timer % 2 == 0)
                {
                    Vector2 spawn = npc.Center + Main.rand.NextVector2CircularEdge(100f, 100f);
                    Dust d = Dust.NewDustPerfect(spawn, DustID.Blood, (npc.Center - spawn) * 0.07f, 100, default, 1.3f);
                    d.noGravity = true;
                }
            }
            else
            {
                // 终末血爆 — the actual kill fires once, everything after is the lingering burst
                if (timer == 140f)
                {
                    npc.velocity = Vector2.Zero;
                    SoundEngine.PlaySound(SoundID.Item62 with { Volume = 1.2f, Pitch = -0.4f }, npc.Center);
                    SoundEngine.PlaySound(SoundID.NPCDeath4, npc.Center);
                    target.Calamity().GeneralScreenShakePower = 13f;
                    RavagerFx.Burst(npc.Center, 8f, 40);
                }

                if (timer >= 162f)
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
            Texture2D tex = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() / 2f;

            for (int i = 0; i < oldPositions.Length; i++)
            {
                int idx = (oldPositionsIndex - i - 1 + oldPositions.Length) % oldPositions.Length;
                if (oldPositions[idx] == Vector2.Zero) continue;
                float alpha = (1f - i / (float)oldPositions.Length) * 0.55f;
                Color trailColor = new Color(200, 50, 50, 0) * alpha;
                spriteBatch.Draw(tex, oldPositions[idx] - screenPos, frame, trailColor, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            }

            if (transitionFlashAlpha > 0f)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * transitionFlashAlpha);

            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tex = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() / 2f;

            int currentPhase = (int)npc.ai[0];
            int totemType = ModContent.Find<ModNPC>("CalamityMod/FleshTotem").Type;
            bool totemAlive = false;
            if (totemNPCIndex >= 0 && totemNPCIndex < Main.maxNPCs)
            {
                NPC t = Main.npc[totemNPCIndex];
                if (t.active && t.type == totemType)
                    totemAlive = true;
            }

            if (currentPhase == 1 && totemAlive)
            {
                float borderSize = 1400f;
                Vector2 tl = totemCenter + new Vector2(-borderSize / 2f, -borderSize / 2f);
                Vector2 tr = totemCenter + new Vector2(borderSize / 2f, -borderSize / 2f);
                Vector2 bl = totemCenter + new Vector2(-borderSize / 2f, borderSize / 2f);
                Vector2 br = totemCenter + new Vector2(borderSize / 2f, borderSize / 2f);

                LegendsWeaponBossVisuals.DrawLine(spriteBatch, tl, tr, Color.Crimson * 0.7f, 4f);
                LegendsWeaponBossVisuals.DrawLine(spriteBatch, tr, br, Color.Crimson * 0.7f, 4f);
                LegendsWeaponBossVisuals.DrawLine(spriteBatch, br, bl, Color.Crimson * 0.7f, 4f);
                LegendsWeaponBossVisuals.DrawLine(spriteBatch, bl, tl, Color.Crimson * 0.7f, 4f);
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            Color glowColor = new Color(200, 50, 50, 0) * 0.35f;
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
