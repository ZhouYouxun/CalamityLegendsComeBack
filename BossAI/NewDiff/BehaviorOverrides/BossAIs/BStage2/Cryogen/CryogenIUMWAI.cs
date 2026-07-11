using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;
using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Events;
using CalamityMod.World;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.WeaponAttacks;
using CalamityLegendsComeBack.BossAI.NewDiff.Core.Systems;
using Terraria.DataStructures;
using ReLogic.Content;

using CalamityCryogen = CalamityMod.NPCs.Cryogen.Cryogen;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage2.Cryogen
{
    internal sealed class CryogenIUMWAI : IUMWBossAI
    {
        #region Constants & Configuration
        public override int NPCType => ModContent.NPCType<CalamityCryogen>();
        public override string BossName => "Cryogen";

        // 6 distinct subphases (Phase Life Ratios matching Infernum)
        public override int MaxPhaseCount => 6;
        public override float[] PhaseLifeRatios => new[] { 0.90f, 0.70f, 0.50f, 0.30f, 0.12f };
        public override int AttackCycleLength => 118;
        public override float MotionIntensity => 0.84f;
        public override Color DebugColor => new(118, 226, 255);

        // Sound Hooks
        public static readonly SoundStyle ShieldRegenSound = new("CalamityMod/Sounds/Custom/CryogenShieldRegenerate");
        public static readonly SoundStyle HitSound = new("CalamityMod/Sounds/NPCHit/CryogenHit", 3);
        public static readonly SoundStyle TransitionSound = new("CalamityMod/Sounds/NPCHit/CryogenPhaseTransitionCrack");
        public static readonly SoundStyle ShieldBreakSound = new("CalamityMod/Sounds/NPCKilled/CryogenShieldBreak");
        public static readonly SoundStyle BlastSound = SoundID.Item27; // Shatter sound

        // Proj Texture key for Calamity shield request
        private const string ProjShield = "CryogenShield";
        #endregion

        #region State Machine Enumeration
        public enum AttackState
        {
            // P1 (Sealed Ice Core)
            HoarfrostBow = 0,             // 白霜弓
            Icebreaker = 1,               // 破冰者
            Avalanche = 2,                // 雪崩
            SnowstormStaff = 3,           // 冰晶风暴
            SoulofCryogen = 4,            // 极寒之魂
            GlacialEmbrace = 5,           // 冰川之拥 (Used in select next attack)

            // P2 (Unsealed Weapon Core)
            DarklightGreatsword = 6,      // 巨剑夜光
            StarnightLance = 7,           // 星夜长枪
            ShadecrystalBarrage = 8,      // 暗晶风暴
            DaedalusGolemStaff = 9,       // 代达罗斯守卫
            DarkechoAndShimmerspark = 10,  // 暗之回响+炽光
            CrystalPiercer = 11,           // 水晶穿刺者

            // Transitions & Special
            FreezeTransition = 12,
            DeathAnimation = 13,
            VictoryDespawn = 14
        }
        #endregion

        #region Local Fields
        private float shieldRotation = 0f;
        private float coreRotation = 0f;
        private int ticksRunning = 0;

        // Transition details
        private int targetSubphase = 1;
        private float transitionFlashAlpha = 0f;

        // Rotation slot within the current phase's attack cycle (persists across phase transitions)
        private int attackCycleIndex = 0;

        // Per-attack variant toggle: every attack now has two distinct executions of the same weapon
        // (same identity, different spatial puzzle). Flipped deterministically each time that attack comes
        // up in the rotation — no RNG involved, so it can't desync and stays predictable across a run.
        private readonly bool[] attackVariant = new bool[12];
        private bool UseVariantB(AttackState state)
        {
            int i = (int)state;
            bool v = attackVariant[i];
            attackVariant[i] = !v;
            return v;
        }

        // P1 Glacial Embrace 10-Hit Shield variables
        private bool shieldActive = false;
        private int shieldHealth = 10;
        private int shieldRegenTimer = 0;
        private float shieldChargeProgress = 0f;

        // P1 Soul of Cryogen wing visuals
        private float wingDrawScale = 0f;

        // Telegraphed teleport system — replaces hard npc.Center warps so blinks read as intent, not glitches
        private int teleportTimer = 0;
        private int teleportDuration = 0;
        private Vector2 teleportDestination = Vector2.Zero;

        // Hoarfrost Bow "read the gap" state
        private float hoarfrostLaneStartX = 0f;
        private float hoarfrostLaneDir = 1f;
        private int hoarfrostVolleyIndex = 0;
        private bool hoarfrostVariantB = false;

        // Avalanche traveling-shockwave state
        private bool avalancheImpacted = false;
        private float avalancheImpactX = 0f;
        private int avalancheImpactTime = 0;
        private bool avalancheVariantB = false;
        private int avalancheBeat = 0; // variant B only: 0=approaching beat1, 1=beat1 impacted, 2=approaching beat2, 3=beat2 impacted
        private int avalancheBeatStart = 0; // variant B only: attack-relative timer value when the current beat's rise began

        // Icebreaker variant toggle
        private bool icebreakerVariantB = false;

        // Soul of Cryogen variant toggle + second-pass tracking
        private bool soulVariantB = false;
        private int soulPass = 1;

        // Darklight Greatsword variant toggle
        private bool darklightVariantB = false;

        // Starnight Lance variant toggle
        private bool starnightVariantB = false;

        // Shadecrystal Barrage variant toggle
        private bool shadecrystalVariantB = false;

        // Daedalus Golem Staff variant toggle
        private bool daedalusVariantB = false;

        // Darkecho and Shimmerspark variant toggle
        private bool darkechoVariantB = false;

        // Crystal Piercer variant toggle
        private bool piercerVariantB = false;

        // Starnight Lance direction tracking
        private Vector2 lanceDir1 = Vector2.Zero;
        private Vector2 lanceDir2 = Vector2.Zero;
        private Vector2 lanceDir3 = Vector2.Zero;

        // Daedalus staff beam aim (tracks during the warning window, locks at fire)
        private Vector2 daedalusAimDir = Vector2.Zero;

        // Motion afterimages — gives weight to dashes & blinks
        private readonly Vector2[] cryoOldPos = new Vector2[9];
        private int cryoOldPosIndex = 0;

        // Cooldown so the on-hit splash feedback doesn't spam on every single hit
        private int hitFxCooldown = 0;
        private int shieldFxCooldown = 0;

        // Snow-biome leash: standard practice across Calamity's biome-bound bosses (AquaticScourge, Brimstone
        // Elemental, DesertScourge, Dragonfolly...) and matches Infernum's own Cryogen (which sets this exact
        // flag off ZoneSnow). Our version had none — a player could drag Cryogen out of its arena into open
        // ground with zero penalty. outOfBiomeTimer counts frames away from snow; enrageSpeedMultiplier ramps
        // hover speed up once the grace period expires, and decays back down quickly once back in the biome.
        private int outOfBiomeTimer = 0;
        private float enrageSpeedMultiplier = 1f;
        private bool wasEnraged = false;
        #endregion

        #region Core AI Hooks
        public override bool PreAI(NPC npc, IUMWGlobalNPC data)
        {
            ticksRunning++;

            // Target selection
            if (npc.target < 0 || npc.target >= Main.maxPlayers || !Main.player[npc.target].active || Main.player[npc.target].dead)
            {
                npc.TargetClosest(true);
            }

            Player target = Main.player[npc.target];
            if (!target.active || target.dead)
            {
                ExecuteVictoryDespawn(npc);
                return false;
            }

            // Sync phase/state variables
            int currentPhase = (int)npc.ai[0];
            AttackState state = (AttackState)(int)npc.ai[1];
            ref float timer = ref npc.ai[2];
            ref float stateTracker = ref npc.ai[3];

            // Normalize stats
            npc.damage = npc.defDamage;
            npc.defense = npc.defDefense;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;

            // Initialize Phase 1
            if (currentPhase == 0)
            {
                currentPhase = 1;
                npc.ai[0] = 1f;
                state = AttackState.HoarfrostBow;
                npc.ai[1] = (float)state;

                // Spawn CryoStone barrier immediately on client & server
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(
                        npc.GetSource_FromAI(),
                        target.Center + new Vector2(0f, 250f),
                        Vector2.Zero,
                        ModContent.ProjectileType<CryoStoneBarrier>(),
                        0,
                        0f,
                        Main.myPlayer,
                        npc.whoAmI
                    );
                }

                npc.netUpdate = true;
            }

            // Handle P1 10-Hit Shield Logic
            UpdateGlacialEmbraceShield(npc, currentPhase);

            // Phase transition checks
            CheckPhaseTransitions(npc, target, ref currentPhase, ref state, ref timer, ref stateTracker);

            // Snow-biome leash — must run before any movement this frame so enrageSpeedMultiplier is current
            UpdateBiomeEnrage(npc, target);

            // Update physics/rotations and dynamic swaying/breathing
            UpdateBossVisuals(npc, target);

            // Execute modular state machine
            switch (state)
            {
                // P1
                case AttackState.HoarfrostBow:
                    ExecuteState_HoarfrostBow(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.Icebreaker:
                    ExecuteState_Icebreaker(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.Avalanche:
                    ExecuteState_Avalanche(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.SnowstormStaff:
                    ExecuteState_SnowstormStaff(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.SoulofCryogen:
                    ExecuteState_SoulofCryogen(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;

                // P2
                case AttackState.DarklightGreatsword:
                    ExecuteState_DarklightGreatsword(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.StarnightLance:
                    ExecuteState_StarnightLance(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.ShadecrystalBarrage:
                    ExecuteState_ShadecrystalBarrage(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.DaedalusGolemStaff:
                    ExecuteState_DaedalusGolemStaff(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.DarkechoAndShimmerspark:
                    ExecuteState_DarkechoAndShimmerspark(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.CrystalPiercer:
                    ExecuteState_CrystalPiercer(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;

                // Transitions
                case AttackState.FreezeTransition:
                    ExecuteState_FreezeTransition(npc, ref timer);
                    break;
                case AttackState.DeathAnimation:
                    ExecuteState_DeathAnimation(npc, ref timer);
                    break;
                case AttackState.VictoryDespawn:
                    ExecuteVictoryDespawn(npc);
                    break;
            }

            timer++;

            // Telegraphed teleport gets the final say over velocity/opacity/position this frame
            UpdateTeleport(npc);

            // Record trail positions (after teleport so the ghosts follow the real body)
            cryoOldPos[cryoOldPosIndex] = npc.Center;
            cryoOldPosIndex = (cryoOldPosIndex + 1) % cryoOldPos.Length;

            // Sync structural local variables to GlobalNPC to comply with difficulty UI
            data.CurrentPhase = currentPhase;
            data.AttackState = (IUMWAttackState)Math.Clamp((int)state, 0, 4);
            data.PatternTimer = (int)timer;

            return false;
        }

        public override void PostAI(NPC npc, IUMWGlobalNPC data)
        {
        }
        #endregion

        #region Phase Transition & Core Systems
        private void CheckPhaseTransitions(NPC npc, Player target, ref int phase, ref AttackState state, ref float timer, ref float stateTracker)
        {
            if (state == AttackState.FreezeTransition || state == AttackState.DeathAnimation || state == AttackState.VictoryDespawn)
                return;

            float lifeRatio = npc.lifeMax <= 0 ? 1f : npc.life / (float)npc.lifeMax;
            int nextPhase = 1;

            for (int i = 0; i < PhaseLifeRatios.Length; i++)
            {
                if (lifeRatio <= PhaseLifeRatios[i])
                {
                    nextPhase = i + 2;
                }
            }

            if (nextPhase > phase)
            {
                targetSubphase = nextPhase;
                TransitionToAttack(npc, AttackState.FreezeTransition);
                return;
            }
        }

        private void TransitionToAttack(NPC npc, AttackState newState)
        {
            npc.ai[1] = (float)newState;
            npc.ai[2] = 0f;
            npc.ai[3] = 0f;

            wingDrawScale = 0f;

            // Retire lingering attack projectiles: held weapons, plus orbiters that would otherwise
            // carry into the next attack and burst with zero warning (snowflakes explode into stars on death,
            // so they get a quiet-kill flag; the yoyo's burst lives in its AI, so a plain Kill is silent).
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active)
                    continue;
                if (p.ModProjectile is BossHeldWeaponBase && (int)p.ai[0] == npc.whoAmI)
                    p.Kill();
                else if (p.type == ModContent.ProjectileType<CryogenSnowflake>() && (int)p.ai[0] == npc.whoAmI)
                {
                    p.localAI[1] = -1f; // quiet-kill sentinel — localAI[1] >= 0 is the flake's live state machine
                    p.Kill();
                }
                else if (p.type == ModContent.ProjectileType<CryogenShimmersparkYoyo>() && (int)p.ai[0] == npc.whoAmI)
                    p.Kill();
            }

            npc.netUpdate = true;
        }

        // Finds this boss's live held weapon of the given style, for driving Pulse/SetAim from the attack code.
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

        // Attack rotations per subphase. Later phases don't replace the list — they extend it with one new pressure.
        // P1: 弹幕(Bow) · 冲刺(Icebreaker) · 封路(Avalanche/Soul) · 定位(Snowstorm); P2: the unsealed arsenal.
        // Phase 6 encodes the intended double-Piercer loop explicitly.
        private static readonly AttackState[][] PhaseCycles =
        {
            new[] { AttackState.HoarfrostBow, AttackState.Icebreaker, AttackState.Avalanche },
            new[] { AttackState.HoarfrostBow, AttackState.Icebreaker, AttackState.SnowstormStaff, AttackState.Avalanche },
            new[] { AttackState.HoarfrostBow, AttackState.Icebreaker, AttackState.SoulofCryogen, AttackState.Avalanche, AttackState.SnowstormStaff },
            new[] { AttackState.DarklightGreatsword, AttackState.StarnightLance, AttackState.ShadecrystalBarrage },
            new[] { AttackState.DarklightGreatsword, AttackState.DaedalusGolemStaff, AttackState.DarkechoAndShimmerspark, AttackState.StarnightLance, AttackState.ShadecrystalBarrage },
            new[] { AttackState.DarklightGreatsword, AttackState.StarnightLance, AttackState.CrystalPiercer, AttackState.ShadecrystalBarrage, AttackState.DaedalusGolemStaff, AttackState.CrystalPiercer },
        };

        // The rotation index PERSISTS across phase transitions. The old version reset to the first attack of the
        // cycle on every phase change — with 10-20% HP phase windows, players only ever saw the first two attacks
        // loop. Now a phase change simply continues at the next slot of the (longer) new cycle, so every weapon
        // in the stage gets its turn. Only the P1->P2 unseal resets the rotation (new arsenal debuts in order).
        private void SelectNextAttack(NPC npc, int phase)
        {
            AttackState[] cycle = PhaseCycles[Math.Clamp(phase, 1, PhaseCycles.Length) - 1];
            attackCycleIndex++;
            TransitionToAttack(npc, cycle[attackCycleIndex % cycle.Length]);
        }

        private void UpdateGlacialEmbraceShield(NPC npc, int currentPhase)
        {
            if (currentPhase >= 4)
            {
                // Disable shield completely in Phase 2 (Unsealed Core Form)
                shieldActive = false;
                shieldChargeProgress = 0f;
                return;
            }

            if (!shieldActive)
            {
                if (shieldRegenTimer > 0)
                {
                    shieldRegenTimer--;
                }
                else
                {
                    // Start regenerating the shield
                    shieldActive = true;
                    shieldHealth = 10;
                    shieldChargeProgress = 0f;
                    SoundEngine.PlaySound(ShieldRegenSound, npc.Center);
                    npc.netUpdate = true;
                }
            }
            else
            {
                if (shieldChargeProgress < 1f)
                {
                    // Regenerating takes 5 seconds (300 frames)
                    shieldChargeProgress += 1f / 300f;
                    if (shieldChargeProgress >= 1f)
                    {
                        shieldChargeProgress = 1f;
                    }

                    // Spawn contracting particles sucked into Cryogen's core
                    if (Main.rand.NextFloat() < 0.35f)
                    {
                        Vector2 offset = (MathHelper.TwoPi * Main.rand.NextFloat()).ToRotationVector2() * Main.rand.NextFloat(130f, 180f);
                        Vector2 vel = -offset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(3f, 5f);
                        Dust d = Dust.NewDustPerfect(npc.Center + offset, DustID.Ice, vel, 100, Color.Cyan, Main.rand.NextFloat(0.7f, 1.2f));
                        d.noGravity = true;
                    }
                }
            }
        }

        // Snow-biome leash. Grace period is generous (5s) so a brief dash out of ZoneSnow mid-fight (e.g. a
        // teleport arena edge or a player platform built just outside the biome tag) isn't punished — this is
        // about stopping the fight from being dragged somewhere else entirely, not about pixel-perfect biome edges.
        private void UpdateBiomeEnrage(NPC npc, Player target)
        {
            const int graceFrames = 300;
            const float maxMultiplier = 1.4f;

            if (!target.ZoneSnow)
                outOfBiomeTimer = Math.Min(outOfBiomeTimer + 1, graceFrames + 120);
            else
                outOfBiomeTimer = Math.Max(outOfBiomeTimer - 3, 0);

            bool enraged = outOfBiomeTimer >= graceFrames;
            npc.Calamity().CurrentlyEnraged = enraged;
            enrageSpeedMultiplier = MathHelper.Lerp(enrageSpeedMultiplier, enraged ? maxMultiplier : 1f, 0.04f);

            // One-time cue on the rising edge only — the speed ramp is gradual (Lerp above) so without this,
            // a player would just notice "it feels faster somehow" with no clear cause.
            if (enraged && !wasEnraged)
            {
                SoundEngine.PlaySound(SoundID.Roar with { Pitch = 0.4f, Volume = 0.6f }, npc.Center);
                EmitFrostBurst(npc.Center, 5f, 30);
            }
            wasEnraged = enraged;
        }

        private void UpdateBossVisuals(NPC npc, Player target)
        {
            // Inner core and outer shield rotations
            float rotationMultiplier = 1f + (1f - npc.life / (float)npc.lifeMax) * 0.5f;
            coreRotation += (0.015f + npc.velocity.Length() * 0.005f) * rotationMultiplier;
            shieldRotation -= (0.025f) * rotationMultiplier;

            // Organic swaying: Tilt based on X velocity
            npc.rotation = npc.velocity.X * 0.04f;
            // Hover wobble
            npc.rotation += (float)Math.Sin(ticksRunning * 0.06f) * 0.08f;

            // Breathing scale pulse
            npc.scale = 1.0f + (float)Math.Sin(ticksRunning * 0.08f) * 0.04f;

            // Ambient snowfall drifting through the arena — thickens as the fight gets more desperate
            float snowIntensity = 0.2f + (1f - npc.life / (float)npc.lifeMax) * 0.35f;
            if (Main.rand.NextFloat() < snowIntensity)
            {
                Vector2 spawnPos = target.Center + new Vector2(Main.rand.NextFloat(-1000f, 1000f), -Main.rand.NextFloat(500f, 700f));
                Vector2 fall = new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(2f, 4.5f));
                var flake = new CalamityMod.Particles.SnowflakeSparkle(spawnPos, fall, Color.White * 0.75f, Color.Cyan * 0.3f, Main.rand.NextFloat(0.35f, 0.8f), Main.rand.Next(90, 150), 0.3f, 0.6f);
                CalamityMod.Particles.GeneralParticleHandler.SpawnParticle(flake);
            }

            if (hitFxCooldown > 0)
                hitFxCooldown--;
            if (shieldFxCooldown > 0)
                shieldFxCooldown--;
        }
        #endregion

        #region Damage Hit Interception (10-Hit Shield)
        public override bool? CanBeHitByItem(NPC npc, Player player, Item item) => null;
        public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile) => null;

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            ProcessShieldHit(npc, player, ref modifiers);
            InterceptLethalHit(npc, ref modifiers);
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            // Splash feedback: a cone of frost sprays back against the shot (throttled so rapid weapons don't flood)
            if (hitFxCooldown <= 0)
            {
                hitFxCooldown = 15;
                Vector2 away = -projectile.velocity.SafeNormalize(Vector2.UnitY);
                for (int i = 0; i < 20; i++)
                {
                    Vector2 vel = away.RotatedBy(Main.rand.NextFloat(-0.6f, 0.6f)) * Main.rand.NextFloat(2f, 6f) + npc.velocity;
                    if (Main.rand.NextBool(3))
                        vel = vel.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
                    Dust d = Dust.NewDustPerfect(projectile.Center, DustID.AncientLight, vel, 100, Color.Lerp(Color.White, Color.Cyan, Main.rand.NextFloat(0.5f)), Main.rand.NextFloat(1.1f, 1.5f));
                    d.fadeIn = 1.3f;
                    d.noGravity = true;
                }
            }

            ProcessShieldHit(npc, Main.player[projectile.owner], ref modifiers);
            InterceptLethalHit(npc, ref modifiers);
        }

        // The killing blow doesn't delete the boss — it triggers the death performance.
        // The final strike is capped at 1 HP, the boss becomes untouchable, and DeathAnimation
        // plays out before a proper loot-dropping kill.
        private void InterceptLethalHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if ((AttackState)(int)npc.ai[1] == AttackState.DeathAnimation)
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
                TransitionToAttack(npc, AttackState.DeathAnimation);
            };
        }

        private void ProcessShieldHit(NPC npc, Player player, ref NPC.HitModifiers modifiers)
        {
            if (shieldActive && shieldChargeProgress >= 1f)
            {
                // Decrement shield health by 1
                shieldHealth--;

                // Hit sound & ring are throttled so rapid-fire weapons don't flood the screen and ears;
                // the shield HP itself still ticks down on every hit.
                if (shieldFxCooldown <= 0)
                {
                    shieldFxCooldown = 12;
                    SoundEngine.PlaySound(SoundID.Item50 with { Volume = 0.8f, Pitch = 0.1f }, npc.Center);
                    EmitCryoDustRing(npc.Center, 100f, 15, 3f);
                }

                // Absorb damage (Modify final damage to 0 or 1 so it triggers visual hit but does no core damage)
                modifiers.FinalDamage *= 0f;
                modifiers.DisableCrit();
                modifiers.DisableKnockback();

                if (shieldHealth <= 0)
                {
                    // Shatter the shield! Dedicated shield-break cue (was reusing the phase-transition crack,
                    // which made two conceptually different events — "shield popped" vs "boss is evolving" —
                    // sound identical).
                    shieldActive = false;
                    shieldRegenTimer = 600; // 10 seconds vulnerability
                    SoundEngine.PlaySound(ShieldBreakSound, npc.Center);

                    // Screen shake
                    player.Calamity().GeneralScreenShakePower = 10f;

                    // Release 24-directional shards
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 24; i++)
                        {
                            Vector2 vel = (MathHelper.TwoPi * i / 24f).ToRotationVector2() * 6f;
                            Projectile.NewProjectile(
                                npc.GetSource_FromAI(),
                                npc.Center,
                                vel,
                                ModContent.ProjectileType<CryogenJavelinShard>(),
                                npc.damage / 3,
                                0f,
                                Main.myPlayer
                            );
                        }
                    }
                    npc.netUpdate = true;
                }
            }
        }
        #endregion

        #region Phase 1 Attack States (3x Repetition)
        // P1 Attack 1: Hoarfrost Bow — 弹幕型 · 两种读法共用同一套冰柱幕基础设施
        // 变体A "读缝隙"：横向冰柱幕留一条滑动安全缝, Boss横移逼玩家追缝走.
        // 变体B "夹击"：冰柱幕从竞技场两端向中央合拢, 安全区是逐渐收窄的中央走廊, 逼玩家主动向中间靠拢而非左右追缝.
        // 每次这招轮到都会切换一次, 同一把弓的两种完全不同的空间题.
        private void ExecuteState_HoarfrostBow(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            const int repositionEnd = 46;
            const int volleyInterval = 62;
            const int volleyCount = 3;
            const int telegraphTime = 44;
            int duration = repositionEnd + volleyInterval * (volleyCount - 1) + 130;

            float arenaX = TryGetArenaCenterX(target);
            float floorY = TryGetArenaFloorY(target);

            // ---- Phase A: reposition to side-above (anti-cheese), nock the bow ----
            if (timer <= repositionEnd)
            {
                if (timer == 1)
                {
                    hoarfrostVariantB = UseVariantB(AttackState.HoarfrostBow);

                    // The gap starts under the player (a fair first read), then sweeps across so they must chase it.
                    hoarfrostLaneStartX = MathHelper.Clamp(target.Center.X, arenaX - 640f, arenaX + 640f);
                    hoarfrostLaneDir = target.Center.X < arenaX ? 1f : -1f;
                    hoarfrostVolleyIndex = 0;

                    // Held bow, drawn skyward — cosmetic (damage 0), recoils on every volley
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<CryogenHeldHoarfrostBow>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                }
                Vector2 spot = DirectedHoverSpot(npc, target, 300f, -360f, 10f);
                SmoothMove(npc, spot, 0.09f, 23f);
            }
            else
            {
                // ---- Phase B: lay telegraph curtains, one per volley ----
                if (hoarfrostVolleyIndex < volleyCount && (int)timer >= repositionEnd + hoarfrostVolleyIndex * volleyInterval)
                {
                    if (hoarfrostVariantB)
                        LayFrostPincer(npc, target, arenaX, floorY, telegraphTime, phase);
                    else
                        LayFrostCurtain(npc, target, arenaX, floorY, telegraphTime, phase);
                    hoarfrostVolleyIndex++;
                    SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.7f }, npc.Center);
                    FindHeldWeapon<CryogenHeldHoarfrostBow>(npc)?.Pulse(-22f);
                }

                if (hoarfrostVolleyIndex < volleyCount)
                {
                    if (hoarfrostVariantB)
                    {
                        // Boss holds a high central perch — the pincer itself is the threat, not the boss's position
                        SmoothMove(npc, new Vector2(arenaX, target.Center.Y - 400f), 0.06f, 16f);
                    }
                    else
                    {
                        // Cryogen sweeps opposite the gap, so the drawn bow visibly tracks where the danger will land.
                        float laneX = hoarfrostLaneStartX + hoarfrostLaneDir * hoarfrostVolleyIndex * 300f;
                        float bossX = MathHelper.Clamp(arenaX * 2f - laneX, arenaX - 620f, arenaX + 620f);
                        SmoothMove(npc, new Vector2(bossX, target.Center.Y - 380f), 0.07f, 18f);
                    }
                }
                else
                {
                    // ---- Phase C: resolution — drift horizontally, keep light aimed pressure (the phase sublayer) ----
                    Vector2 spot = DirectedHoverSpot(npc, target, 360f, -300f, 16f);
                    SmoothMove(npc, spot, 0.06f, 15f);

                    if (phase >= 2 && (int)timer % 26 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 dir = (target.Center - npc.Center).SafeNormalize(Vector2.UnitY);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + new Vector2(0f, -npc.height * 0.5f), dir * 11f, ModContent.ProjectileType<CryogenMistArrow>(), npc.damage / 3, 0f, Main.myPlayer);
                        SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.4f }, npc.Center);
                        FindHeldWeapon<CryogenHeldHoarfrostBow>(npc)?.Pulse(-10f);
                    }
                }
            }

            if (timer >= duration)
                SelectNextAttack(npc, phase);
        }

        // Variant A: lays a horizontal row of telegraph columns across the arena, skipping a safe lane that
        // slides each volley. P3 narrows the lane by ~half a column — the only escalation to the main solution.
        private void LayFrostCurtain(NPC npc, Player target, float arenaX, float floorY, int telegraphTime, int phase)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            const float half = 800f;
            const float spacing = 96f;
            float laneCenter = hoarfrostLaneStartX + hoarfrostLaneDir * hoarfrostVolleyIndex * 300f;
            laneCenter = MathHelper.Clamp(laneCenter, arenaX - half + 120f, arenaX + half - 120f);
            float laneHalfWidth = phase >= 3 ? spacing * 1.05f : spacing * 1.6f;

            for (float x = arenaX - half; x <= arenaX + half + 1f; x += spacing)
            {
                if (Math.Abs(x - laneCenter) <= laneHalfWidth)
                    continue;
                Projectile.NewProjectile(npc.GetSource_FromAI(), new Vector2(x, floorY), Vector2.Zero, ModContent.ProjectileType<CryogenFrostColumnTelegraph>(), 0, 0f, Main.myPlayer, telegraphTime, npc.damage / 3);
            }
        }

        // Variant B: columns fill in from BOTH arena edges toward the center, leaving a central safe corridor
        // that shrinks with each volley (500 -> 380 -> 260px). Same telegraph column, opposite spatial read:
        // instead of chasing a sliding lane sideways, the player must actively migrate toward the middle.
        private void LayFrostPincer(NPC npc, Player target, float arenaX, float floorY, int telegraphTime, int phase)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            const float half = 800f;
            const float spacing = 96f;
            float corridorHalfWidth = MathHelper.Lerp(250f, 130f, hoarfrostVolleyIndex / 2f);
            if (phase >= 3)
                corridorHalfWidth -= 30f; // P3 sublayer: the corridor closes a little tighter, same main solution

            for (float x = arenaX - half; x <= arenaX + half + 1f; x += spacing)
            {
                if (Math.Abs(x - arenaX) <= corridorHalfWidth)
                    continue;
                Projectile.NewProjectile(npc.GetSource_FromAI(), new Vector2(x, floorY), Vector2.Zero, ModContent.ProjectileType<CryogenFrostColumnTelegraph>(), 0, 0f, Main.myPlayer, telegraphTime, npc.damage / 3);
            }
        }

        // P1 Attack 2: Icebreaker — 两种执行方式共用同一把回旋锤:
        // 变体A "冲刺连打"：Boss自己冲两次, 每次带3枚小回旋镖(方向即锤线).
        // 变体B "双面夹锤"：Boss原地悬停不冲, 改成朝竞技场两端各扔1枚大回旋锤, 两枚独立错峰暂停+回冲,
        // 逼玩家同时追踪左右两条会合的回程线, 而不是"看Boss冲哪就往哪反方向躲"这一种读法.
        private void ExecuteState_Icebreaker(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            int duration = 260;

            if (timer == 1)
            {
                icebreakerVariantB = UseVariantB(AttackState.Icebreaker);
                // Held spinning hammer (cosmetic) — shared by both variants
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<CryogenHeldIcebreaker>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (!icebreakerVariantB)
            {
                if (timer < 30)
                {
                    // Slow down and tilt back preparing dash
                    npc.velocity *= 0.85f;

                    // Spawn focus particles towards Cryogen center
                    if (Main.rand.NextFloat() < 0.45f)
                    {
                        Vector2 offset = (MathHelper.TwoPi * Main.rand.NextFloat()).ToRotationVector2() * Main.rand.NextFloat(90f, 160f);
                        Vector2 vel = -offset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(3f, 6f);
                        Dust d = Dust.NewDustPerfect(npc.Center + offset, DustID.Ice, vel, 100, Color.Cyan, 1.1f);
                        d.noGravity = true;
                    }
                }
                else if (timer == 30)
                {
                    // First dash
                    Vector2 dashVel = (target.Center - npc.Center).SafeNormalize(Vector2.UnitY) * 21f;
                    npc.velocity = dashVel;
                    SoundEngine.PlaySound(SoundID.Item30, npc.Center);
                    EmitCryoDustRing(npc.Center, 40f, 18, 6f);
                    FindHeldWeapon<CryogenHeldIcebreaker>(npc)?.Pulse(30f);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Throw 3 Icebreaker boomerangs
                        float spread = MathHelper.ToRadians(15f);
                        for (int i = -1; i <= 1; i++)
                        {
                            Vector2 vel = dashVel.RotatedBy(i * spread) * 0.7f;
                            Projectile.NewProjectile(
                                npc.GetSource_FromAI(),
                                npc.Center,
                                vel,
                                ModContent.ProjectileType<CryogenIcebreaker>(),
                                npc.damage / 3,
                                0f,
                                Main.myPlayer
                            );
                        }
                    }
                }
                else if (timer > 30 && timer < 100)
                {
                    // Decelerate dash
                    npc.velocity *= 0.96f;
                }
                else if (timer == 100)
                {
                    // Telegraphed blink to the player's far side (was a hard npc.Center warp)
                    float side = Math.Sign(target.Center.X - npc.Center.X);
                    if (side == 0f)
                        side = 1f;
                    Vector2 dest = target.Center + new Vector2(-side * 360f, -220f);
                    BeginTeleport(npc, dest, 22);
                }
                else if (timer == 130)
                {
                    // Second dash
                    Vector2 dashVel = (target.Center - npc.Center).SafeNormalize(Vector2.UnitY) * 21f;
                    npc.velocity = dashVel;
                    SoundEngine.PlaySound(SoundID.Item30, npc.Center);
                    EmitCryoDustRing(npc.Center, 40f, 18, 6f);
                    FindHeldWeapon<CryogenHeldIcebreaker>(npc)?.Pulse(30f);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Throw another 3 Icebreaker boomerangs
                        float spread = MathHelper.ToRadians(15f);
                        for (int i = -1; i <= 1; i++)
                        {
                            Vector2 vel = dashVel.RotatedBy(i * spread) * 0.7f;
                            Projectile.NewProjectile(
                                npc.GetSource_FromAI(),
                                npc.Center,
                                vel,
                                ModContent.ProjectileType<CryogenIcebreaker>(),
                                npc.damage / 3,
                                0f,
                                Main.myPlayer
                            );
                        }
                    }
                }
                else if (timer > 130 && timer < 200)
                {
                    npc.velocity *= 0.96f;
                }
                else if (timer >= 200)
                {
                    // Smoothly float back
                    Vector2 desiredPos = target.Center + new Vector2(0f, -280f);
                    SmoothMove(npc, desiredPos, 0.06f, 14f);
                }
            }
            else
            {
                // Variant B: hold a steady high perch — the threat is entirely in the two hammers, not the body
                Vector2 holdSpot = DirectedHoverSpot(npc, target, 0f, -400f, 0f);
                SmoothMove(npc, holdSpot, 0.045f, 12f);

                float arenaX = TryGetArenaCenterX(target);
                if (timer == 20 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 vel = new Vector2(arenaX - 760f - npc.Center.X, -30f).SafeNormalize(Vector2.UnitX) * 13f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<CryogenIcebreaker>(), npc.damage / 3, 0f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Item30 with { Pitch = -0.15f }, npc.Center);
                    EmitCryoDustRing(npc.Center, 30f, 12, 5f);
                    FindHeldWeapon<CryogenHeldIcebreaker>(npc)?.Pulse(18f);
                }
                else if (timer == 55 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 vel = new Vector2(arenaX + 760f - npc.Center.X, -30f).SafeNormalize(Vector2.UnitX) * 13f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<CryogenIcebreaker>(), npc.damage / 3, 0f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Item30 with { Pitch = 0.15f }, npc.Center);
                    EmitCryoDustRing(npc.Center, 30f, 12, 5f);
                    FindHeldWeapon<CryogenHeldIcebreaker>(npc)?.Pulse(18f);
                }

                if (timer >= 210)
                {
                    Vector2 desiredPos = target.Center + new Vector2(0f, -280f);
                    SmoothMove(npc, desiredPos, 0.06f, 14f);
                }
            }

            if (timer >= duration)
            {
                SelectNextAttack(npc, phase);
            }
        }

        // P1 Attack 3: Avalanche — 两种执行方式共用同一把冰斧:
        // 变体A "跳过冲击波"：单次中央砸地, 双向扩散冰刺墙, 考验纵向起跳时机.
        // 变体B "双点齐落"：分别在竞技场左右两侧各砸一次(局部小范围冲击, 非扩散全场), 逼玩家在两次落点间主动横移站队,
        // 而不是原地等一道墙经过 — 同一套"雪崩"身份, 完全不同的横向空间题.
        private void ExecuteState_Avalanche(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            float floorY = TryGetArenaFloorY(target);

            if (timer == 1)
            {
                avalancheVariantB = UseVariantB(AttackState.Avalanche);
                avalancheImpacted = false;
                avalancheBeat = 0;
                avalancheBeatStart = 1;

                // Held Avalanche axe: its windup/quiver/whip timeline is tuned to land exactly on the dive (92f) and impact.
                // The whip is a real melee arc — the slam now has a blade, not just a falling body.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float swingDir = Math.Sign(target.Center.X - npc.Center.X);
                    if (swingDir == 0f)
                        swingDir = 1f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.UnitY, ModContent.ProjectileType<CryogenHeldAvalanche>(), npc.damage / 3, 0f, Main.myPlayer, npc.whoAmI, swingDir);
                }
            }

            if (avalancheVariantB)
            {
                ExecuteAvalancheTwinDrop(npc, target, ref timer, floorY, phase);
                return;
            }

            const int duration = 300;
            if (!avalancheImpacted)
            {
                if (timer < 70)
                {
                    // Rise high above the player, still tracking
                    SmoothMove(npc, target.Center + new Vector2(0f, -380f), 0.07f, 15f);
                }
                else if (timer < 92)
                {
                    // Lock X (stop tracking so the drop is dodgeable) and telegraph the slam lane with falling frost
                    npc.velocity *= 0.8f;
                    for (int k = 0; k < 3; k++)
                    {
                        float yy = MathHelper.Lerp(npc.Center.Y, floorY, Main.rand.NextFloat());
                        Dust d = Dust.NewDustPerfect(new Vector2(npc.Center.X + Main.rand.NextFloat(-6f, 6f), yy), DustID.Ice, new Vector2(0f, Main.rand.NextFloat(2f, 5f)), 100, Color.Cyan, 0.9f);
                        d.noGravity = true;
                    }
                }
                else if (timer == 92)
                {
                    npc.velocity = new Vector2(0f, 12f);
                    SoundEngine.PlaySound(SoundID.Item71, npc.Center);
                }
                else
                {
                    // Accelerating dive — weight builds as it falls, Infernum-style
                    npc.velocity.Y = Math.Min(npc.velocity.Y + 2.4f, 34f);
                    if (npc.Center.Y >= floorY - 32f || timer >= 150)
                        DoAvalancheImpact(npc, target, ref timer, floorY);
                }
            }
            else
            {
                int since = (int)timer - avalancheImpactTime;

                // Two eruption fronts race outward from the impact point — the player jumps each wall as it passes under them
                if (since > 0 && since <= 60 && since % 4 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float dist = (since / 4) * 62f;
                    SpawnEruption(npc, target, avalancheImpactX + dist, floorY);
                    SpawnEruption(npc, target, avalancheImpactX - dist, floorY);
                }

                // Phase 2+: exactly two ice bombs bloom in the jump zone after the walls pass — a delayed second beat.
                // (One bomb per eruption step would mean 31 bombs x 8 shards ≈ 248 projectiles — far past the ≤15 red line.)
                if (since == 24 && phase >= 2 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float arenaX = TryGetArenaCenterX(target);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        float bx = MathHelper.Clamp(avalancheImpactX + s * 380f, arenaX - 780f, arenaX + 780f);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), new Vector2(bx, floorY - 110f), Vector2.Zero, ModContent.ProjectileType<CryogenIceBomb>(), npc.damage / 3, 0f, Main.myPlayer);
                    }
                }

                if (since > 60)
                    SmoothMove(npc, target.Center + new Vector2(0f, -280f), 0.05f, 12f);
                else
                    npc.velocity *= 0.85f;
            }

            if (timer >= duration)
                SelectNextAttack(npc, phase);
        }

        private void DoAvalancheImpact(NPC npc, Player target, ref float timer, float floorY)
        {
            npc.Center = new Vector2(npc.Center.X, floorY - 32f);
            npc.velocity = Vector2.Zero;
            avalancheImpacted = true;
            avalancheImpactX = npc.Center.X;
            avalancheImpactTime = (int)timer;
            npc.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Item14, npc.Center);
            target.Calamity().GeneralScreenShakePower = 9f;
            EmitCryoDustRing(npc.Center, 60f, 30, 5f);
            EmitFrostBurst(npc.Center, 5f, 40, 4);

            if (Main.netMode != NetmodeID.MultiplayerClient)
                SpawnEruption(npc, target, avalancheImpactX, floorY); // step 0 at the impact point
        }

        private void SpawnEruption(NPC npc, Player target, float x, float floorY)
        {
            float arenaX = TryGetArenaCenterX(target);
            if (Math.Abs(x - arenaX) > 820f)
                return; // stay inside the arena

            // Ground physical spike
            Projectile.NewProjectile(npc.GetSource_FromAI(), new Vector2(x, floorY - 8f), Vector2.Zero, ModContent.ProjectileType<CryogenIceEruption>(), npc.damage / 3, 0f, Main.myPlayer);

        }

        // Variant B: two smaller, spatially-fixed impacts (left third, then right third of the arena) instead
        // of one big central slam. Each beat reuses the exact rise->lock->telegraph->dive->impact shape of
        // variant A, just localized to a shorter timeline and a tighter eruption radius, and fires at a FIXED
        // arena-relative X rather than tracking the player — so the read is "which side is about to get hit",
        // not "dodge the wall coming from wherever I was standing."
        private void ExecuteAvalancheTwinDrop(NPC npc, Player target, ref float timer, float floorY, int phase)
        {
            float arenaX = TryGetArenaCenterX(target);
            float lockX = avalancheBeat < 2 ? arenaX - 300f : arenaX + 300f;
            int localT = (int)timer - avalancheBeatStart;

            if (avalancheBeat == 0 || avalancheBeat == 2)
            {
                if (localT < 46)
                {
                    SmoothMove(npc, new Vector2(lockX, target.Center.Y - 380f), 0.07f, 15f);
                }
                else if (localT < 68)
                {
                    npc.velocity *= 0.8f;
                    for (int k = 0; k < 3; k++)
                    {
                        float yy = MathHelper.Lerp(npc.Center.Y, floorY, Main.rand.NextFloat());
                        Dust d = Dust.NewDustPerfect(new Vector2(lockX + Main.rand.NextFloat(-6f, 6f), yy), DustID.Ice, new Vector2(0f, Main.rand.NextFloat(2f, 5f)), 100, Color.Cyan, 0.9f);
                        d.noGravity = true;
                    }
                }
                else if (localT == 68)
                {
                    npc.velocity = new Vector2(0f, 12f);
                    SoundEngine.PlaySound(SoundID.Item71, npc.Center);
                }
                else
                {
                    npc.velocity.Y = Math.Min(npc.velocity.Y + 2.4f, 34f);
                    if (npc.Center.Y >= floorY - 32f || localT >= 126)
                    {
                        npc.Center = new Vector2(lockX, floorY - 32f);
                        npc.velocity = Vector2.Zero;
                        avalancheImpactX = lockX;
                        avalancheImpactTime = (int)timer;
                        avalancheBeat++;
                        npc.netUpdate = true;

                        SoundEngine.PlaySound(SoundID.Item14, npc.Center);
                        target.Calamity().GeneralScreenShakePower = 7f;
                        EmitCryoDustRing(npc.Center, 46f, 22, 4.5f);
                        EmitFrostBurst(npc.Center, 4f, 26, 3);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            SpawnEruption(npc, target, lockX, floorY);
                    }
                }
            }
            else
            {
                // Beat just impacted: a single tight eruption step on each side (localized splash, not a
                // traveling wall), then either regroup into beat 2 or, after beat 2, tail off.
                int since = (int)timer - avalancheImpactTime;
                if (since == 20 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SpawnEruption(npc, target, avalancheImpactX + 70f, floorY);
                    SpawnEruption(npc, target, avalancheImpactX - 70f, floorY);
                }

                if (since > 40)
                {
                    if (avalancheBeat == 1)
                    {
                        // Regroup and re-arm the axe for beat 2
                        if (since == 41 && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float swingDir = Math.Sign((arenaX + 300f) - npc.Center.X);
                            if (swingDir == 0f)
                                swingDir = 1f;
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.UnitY, ModContent.ProjectileType<CryogenHeldAvalanche>(), npc.damage / 3, 0f, Main.myPlayer, npc.whoAmI, swingDir);
                            avalancheBeatStart = (int)timer;
                            avalancheBeat = 2;
                        }
                    }
                    else
                    {
                        SmoothMove(npc, target.Center + new Vector2(0f, -280f), 0.05f, 12f);
                    }
                }
                else
                {
                    npc.velocity *= 0.85f;
                }
            }

            // Fixed total budget measured from the attack's own start (NOT avalancheBeatStart, which gets
            // reassigned mid-attack when beat 2 begins) — beat1(~126f) + regroup(~41f) + beat2(~126f) + tail.
            if (timer >= 360f)
                SelectNextAttack(npc, phase);
        }

        // P1 Attack 4: Snowstorm Staff — 两种执行方式共用同一根法杖:
        // 变体A "游标雪花"：3片雪花错开110帧陆续放出, 逐渐升级的持续压力.
        // 变体B "雪之罗盘"：开局一次性放出4片雪花, 90°等分同步公转, 因为同一帧生成所以星爆节拍完全同步——
        // 从"渐进式加码"变成"开局就是一个四方向同步脉冲的旋转罗盘", 逼玩家从第一秒就读懂整个环形节奏.
        private void ExecuteState_SnowstormStaff(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            bool variantB = timer == 1 ? UseVariantB(AttackState.SnowstormStaff) : stateTracker != 0f;
            stateTracker = variantB ? 1f : 0f;
            int duration = variantB ? 300 : 340;

            // Normal elliptical hover
            Vector2 desiredPos = target.Center + new Vector2(MathF.Sin(ticksRunning * 0.015f) * 220f, -280f);
            SmoothMove(npc, desiredPos, 0.045f, 13f);

            // Held staff raised skyward, recoiling with each conjuration
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<CryogenHeldSnowstormStaff>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (!variantB)
            {
                if (timer == 30 || timer == 70 || timer == 110)
                {
                    FindHeldWeapon<CryogenHeldSnowstormStaff>(npc)?.Pulse(-14f);
                    SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.5f, Pitch = 0.2f }, npc.Center);
                }

                // Spawn orbital snowflakes sequentially
                if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<CryogenSnowflake>(), npc.damage / 3, 0f, Main.myPlayer, npc.whoAmI, 0f);
                else if (timer == 70 && Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<CryogenSnowflake>(), npc.damage / 3, 0f, Main.myPlayer, npc.whoAmI, MathHelper.PiOver2);
                else if (timer == 110 && Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<CryogenSnowflake>(), npc.damage / 3, 0f, Main.myPlayer, npc.whoAmI, MathHelper.Pi);
            }
            else
            {
                // One decisive cast — the whole compass appears at once, telegraphed by a single loud staff slam
                if (timer == 30)
                {
                    FindHeldWeapon<CryogenHeldSnowstormStaff>(npc)?.Pulse(-20f);
                    SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.8f, Pitch = -0.1f }, npc.Center);
                    EmitFrostBurst(npc.Center, 4f, 24);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            float ang = MathHelper.PiOver2 * i;
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<CryogenSnowflake>(), npc.damage / 3, 0f, Main.myPlayer, npc.whoAmI, ang);
                        }
                    }
                }
            }

            if (timer >= duration)
            {
                SelectNextAttack(npc, phase);
            }
        }

        // P1 Attack 5: Soul of Cryogen — 两种执行方式共用同一对冰翼:
        // 变体A "单程滑翔"：一次单高度滑翔, 沿途留一条带缝隙的冰棱帘幕.
        // 变体B "交叉之翼"：两次滑翔在不同高度、相反方向各飞一次, 两条航线在空中形成X交叉——
        // 逼玩家在两段滑翔之间重新读一次缝隙位置和高度, 而不是一次读完全场.
        private void ExecuteState_SoulofCryogen(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            int duration;

            if (timer == 1)
            {
                soulVariantB = UseVariantB(AttackState.SoulofCryogen);
                soulPass = 1;

                // Determine flight start (left or right top of player)
                float side = Main.rand.NextBool() ? -1f : 1f;
                stateTracker = side; // Store side direction in stateTracker

                float height = soulVariantB ? -300f : -320f;
                Vector2 spawnPos = target.Center + new Vector2(side * 480f, height);
                BeginTeleport(npc, spawnPos, 22);
            }

            if (!soulVariantB)
            {
                duration = 320;
                if (timer >= 30 && timer <= 260)
                {
                    wingDrawScale = MathHelper.Lerp(wingDrawScale, 1.0f, 0.1f);
                    npc.velocity = new Vector2(-stateTracker * 6.5f, 0f);
                    SoulOfCryogenDropShards(npc, target, timer);
                }
                else
                {
                    wingDrawScale = MathHelper.Lerp(wingDrawScale, 0f, 0.1f);
                    npc.velocity *= 0.95f;
                }
            }
            else
            {
                duration = 380;
                if (soulPass == 1)
                {
                    if (timer >= 26 && timer <= 130)
                    {
                        wingDrawScale = MathHelper.Lerp(wingDrawScale, 1.0f, 0.1f);
                        npc.velocity = new Vector2(-stateTracker * 7f, 0f);
                        SoulOfCryogenDropShards(npc, target, timer);
                    }
                    else if (timer == 140)
                    {
                        // Cross to the far side at a lower altitude, reversed direction — the second leg of the X
                        wingDrawScale = MathHelper.Lerp(wingDrawScale, 0f, 0.2f);
                        stateTracker = -stateTracker;
                        Vector2 crossPos = target.Center + new Vector2(-stateTracker * 480f, -170f);
                        BeginTeleport(npc, crossPos, 20);
                        soulPass = 2;
                    }
                    else
                    {
                        wingDrawScale = MathHelper.Lerp(wingDrawScale, 0f, 0.15f);
                        npc.velocity *= 0.9f;
                    }
                }
                else
                {
                    if (timer >= 172 && timer <= 300)
                    {
                        wingDrawScale = MathHelper.Lerp(wingDrawScale, 1.0f, 0.1f);
                        npc.velocity = new Vector2(-stateTracker * 7f, 0f);
                        SoulOfCryogenDropShards(npc, target, timer);
                    }
                    else
                    {
                        wingDrawScale = MathHelper.Lerp(wingDrawScale, 0f, 0.1f);
                        npc.velocity *= 0.95f;
                    }
                }
            }

            if (timer >= duration)
            {
                SelectNextAttack(npc, phase);
            }
        }

        // Shared shard-drop rhythm for both Soul of Cryogen passes: every 8 frames, unless the boss's current
        // X sits within a periodic 70/80px safe gap relative to the player.
        private void SoulOfCryogenDropShards(NPC npc, Player target, float timer)
        {
            float relativeX = Math.Abs(npc.Center.X - target.Center.X);
            int segment = (int)(relativeX / 80f);
            bool inGap = (relativeX % 80f) <= 70f && segment % 2 == 0;

            if (!inGap && timer % 8 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(
                    npc.GetSource_FromAI(),
                    npc.Center,
                    new Vector2(0f, 2f),
                    ModContent.ProjectileType<CryogenSoulShard>(),
                    npc.damage / 3,
                    0f,
                    Main.myPlayer
                );
            }
        }
        #endregion

        #region Phase 2 Attack States (Unsealed Core Form)
        // P2 Attack 7: Darklight Greatsword — 两种执行方式共用同一把巨剑:
        // 变体A "迟滞斩光"：1-2次远距离对角冲斩, 每次带一发DarkBeam+双LightBeam的重弹幕.
        // 变体B "三连回旋"：围绕玩家三个方位快速连续短距冲斩, 每次只带单发DarkBeam——
        // 用"更高频率、更轻弹幕"换"更少的单次重击", 是完全不同的节奏而非同一套加量.
        private void ExecuteState_DarklightGreatsword(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            if (timer == 1)
                darklightVariantB = UseVariantB(AttackState.DarklightGreatsword);

            if (!darklightVariantB)
            {
                int duration = 240;

                if (timer == 1)
                {
                    // Teleport to player side
                    float side = Math.Sign(npc.Center.X - target.Center.X);
                    if (side == 0f) side = 1f;
                    stateTracker = side; // Store side in stateTracker

                    Vector2 dest = target.Center + new Vector2(side * 360f, -180f);
                    BeginTeleport(npc, dest, 22);
                }

                // Held greatsword per slash: spawned with the telegraph so the windup burns alongside the warning line,
                // then it re-aims along the actual dash velocity at slash start — blade and body always agree.
                // Slash #2's telegraph window is 24f (not 34f), so its windup is overridden via ai[2] to land exactly on the dash.
                if ((timer == 26 || (timer == 126 && phase == 6)) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 aim = (target.Center - npc.Center).SafeNormalize(Vector2.UnitY);
                    float swingDir = aim.X >= 0f ? 1f : -1f;
                    float windupOverride = timer == 126 ? 24f : 0f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, aim, ModContent.ProjectileType<CryogenHeldDarklight>(), npc.damage / 3, 0f, Main.myPlayer, npc.whoAmI, swingDir, windupOverride);
                }

                if (timer == 60)
                {
                    // First diagonal slash dash
                    Vector2 dashVel = (target.Center - npc.Center).SafeNormalize(Vector2.UnitY) * 26f;
                    npc.velocity = dashVel;
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.9f, Pitch = -0.1f }, npc.Center);
                    EmitShadowBurst(npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 dir = dashVel.SafeNormalize(Vector2.UnitY);

                        // Spawn 2.0x scaled purple DarkBeam
                        Projectile.NewProjectile(
                            npc.GetSource_FromAI(),
                            npc.Center + dir * 40f,
                            dir,
                            ModContent.ProjectileType<CryogenDarkBeam>(),
                            npc.damage / 2,
                            0f,
                            Main.myPlayer
                        );

                        // Spawn 2 trailing LightBeams
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center - dir * 20f + dir.RotatedBy(MathHelper.PiOver2) * 26f, dir, ModContent.ProjectileType<CryogenLightBeam>(), npc.damage / 3, 0f, Main.myPlayer);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center - dir * 20f - dir.RotatedBy(MathHelper.PiOver2) * 26f, dir, ModContent.ProjectileType<CryogenLightBeam>(), npc.damage / 3, 0f, Main.myPlayer);
                    }
                }
                else if (timer > 60 && timer < 120)
                {
                    // Decelerate
                    npc.velocity *= 0.94f;
                }
                else if (timer == 120 && phase == 6)
                {
                    // Phase 6 extra telegraphed slash: teleport to opposite side
                    Vector2 dest = target.Center + new Vector2(-stateTracker * 360f, -180f);
                    BeginTeleport(npc, dest, 24);
                }
                else if (timer == 150 && phase == 6)
                {
                    // Second slash dash (Phase 6 only)
                    Vector2 dashVel = (target.Center - npc.Center).SafeNormalize(Vector2.UnitY) * 26f;
                    npc.velocity = dashVel;
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.9f, Pitch = -0.1f }, npc.Center);
                    EmitShadowBurst(npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 dir = dashVel.SafeNormalize(Vector2.UnitY);

                        Projectile.NewProjectile(
                            npc.GetSource_FromAI(),
                            npc.Center + dir * 40f,
                            dir,
                            ModContent.ProjectileType<CryogenDarkBeam>(),
                            npc.damage / 2,
                            0f,
                            Main.myPlayer
                        );

                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center - dir * 20f + dir.RotatedBy(MathHelper.PiOver2) * 26f, dir, ModContent.ProjectileType<CryogenLightBeam>(), npc.damage / 3, 0f, Main.myPlayer);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center - dir * 20f - dir.RotatedBy(MathHelper.PiOver2) * 26f, dir, ModContent.ProjectileType<CryogenLightBeam>(), npc.damage / 3, 0f, Main.myPlayer);
                    }
                }
                else if (timer > 150 && timer < 200)
                {
                    npc.velocity *= 0.94f;
                }
                else if (timer >= 200)
                {
                    Vector2 desiredPos = target.Center + new Vector2(0f, -220f);
                    SmoothMove(npc, desiredPos, 0.06f, 15f);
                }

                if (timer >= duration)
                    SelectNextAttack(npc, phase);
            }
            else
            {
                ExecuteDarklightTripleFlurry(npc, target, timer, ref stateTracker, phase);
            }
        }

        // Variant B: three quick short-range dash-slashes from three positions around the player, each with a
        // single DarkBeam only (no trailing LightBeams — three light hits instead of one or two heavy ones).
        // stateTracker holds the initial side (+-1), persisted via ref so the reposition beats can mirror off it.
        private void ExecuteDarklightTripleFlurry(NPC npc, Player target, float timer, ref float side, int phase)
        {
            if (timer == 1)
            {
                side = Math.Sign(npc.Center.X - target.Center.X);
                if (side == 0f) side = 1f;
                Vector2 dest = target.Center + new Vector2(side * 300f, -160f);
                BeginTeleport(npc, dest, 22);
            }

            for (int strike = 0; strike < 3; strike++)
            {
                float spawnT = 20 + strike * 66;
                float dashT = spawnT + 20;
                float repositionT = dashT + 30;

                if (timer == spawnT && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 aim = (target.Center - npc.Center).SafeNormalize(Vector2.UnitY);
                    float swingDir = aim.X >= 0f ? 1f : -1f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, aim, ModContent.ProjectileType<CryogenHeldDarklight>(), npc.damage / 3, 0f, Main.myPlayer, npc.whoAmI, swingDir, 20f);
                }
                else if (timer == dashT)
                {
                    Vector2 dashVel = (target.Center - npc.Center).SafeNormalize(Vector2.UnitY) * 24f;
                    npc.velocity = dashVel;
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, Pitch = 0.1f * strike }, npc.Center);
                    EmitShadowBurst(npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 dir = dashVel.SafeNormalize(Vector2.UnitY);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + dir * 40f, dir, ModContent.ProjectileType<CryogenDarkBeam>(), npc.damage / 2, 0f, Main.myPlayer);
                    }
                }
                else if (timer > dashT && timer < repositionT)
                {
                    npc.velocity *= 0.94f;
                }
                else if (timer == repositionT && strike < 2 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Alternate sides so the three strikes trace a rough triangle around the player
                    float nextSide = strike == 0 ? -side : 0f;
                    Vector2 dest = nextSide == 0f
                        ? target.Center + new Vector2(0f, -320f)
                        : target.Center + new Vector2(nextSide * 300f, -160f);
                    BeginTeleport(npc, dest, 16);
                }
            }

            if (timer >= 260)
            {
                Vector2 desiredPos = target.Center + new Vector2(0f, -220f);
                SmoothMove(npc, desiredPos, 0.06f, 15f);
            }

            if (timer >= 300)
                SelectNextAttack(npc, phase);
        }

        // P2 Attack 8: Starnight Lance — 两种执行方式共用同一根长枪, 三条线的锁定/发射时机完全不变
        // (预警线绘制是按固定帧数画的, 时机绝不能变), 只改"三条线怎么瞄":
        // 变体A "独立追踪"：三条线各自独立瞄准发射时刻的玩家方向, 会随玩家走位自然散开.
        // 变体B "定角扇形"：第一条线锁定后, 另外两条固定在其±32°处一起画出, 不再逐条重新瞄准玩家——
        // 逼玩家在三条线都发射前就走出整个扇形角宽, 而不是"賭最后瞄准会往哪偏".
        private void ExecuteState_StarnightLance(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            int duration = 240;

            if (timer == 1)
            {
                starnightVariantB = UseVariantB(AttackState.StarnightLance);

                // Teleport to side-above
                float side = Math.Sign(npc.Center.X - target.Center.X);
                if (side == 0f) side = 1f;
                Vector2 dest = target.Center + new Vector2(side * 360f, -220f);
                BeginTeleport(npc, dest, 22);

                // Held lance for the channel: snaps to each locked line, stabs forward as its beam fires
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<CryogenHeldStarnightLance>(), npc.damage / 3, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer < 30)
            {
                // Decelerate and hold position
                npc.velocity *= 0.8f;
            }
            else if (timer >= 30 && timer <= 170)
            {
                // Stay focused / stationary during channeling
                npc.velocity = Vector2.Zero;

                // Lock the three aim lines one by one; they're drawn in PreDraw from these fields so every client sees them.
                // Lock/fire TIMING never changes between variants (the warning-line draw times are hardcoded to
                // these exact breakpoints) — only how lanceDir2/3 are computed changes.
                if (timer == 30)
                {
                    lanceDir1 = (target.Center - npc.Center).SafeNormalize(Vector2.UnitY);
                    FindHeldWeapon<CryogenHeldStarnightLance>(npc)?.SetAim(lanceDir1.ToRotation());
                    SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.4f, Pitch = 0.4f }, npc.Center);

                    if (starnightVariantB)
                    {
                        // Fixed fan around the first lock — committed early, doesn't re-read the player again
                        lanceDir2 = lanceDir1.RotatedBy(MathHelper.ToRadians(32f));
                        lanceDir3 = lanceDir1.RotatedBy(MathHelper.ToRadians(-32f));
                    }
                }
                else if (timer == 60)
                {
                    if (!starnightVariantB)
                    {
                        lanceDir2 = (target.Center - npc.Center).SafeNormalize(Vector2.UnitY);
                        FindHeldWeapon<CryogenHeldStarnightLance>(npc)?.SetAim(lanceDir2.ToRotation());
                    }
                    else
                    {
                        FindHeldWeapon<CryogenHeldStarnightLance>(npc)?.SetAim(lanceDir2.ToRotation());
                    }
                    SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.4f, Pitch = 0.5f }, npc.Center);
                }
                else if (timer == 90)
                {
                    if (!starnightVariantB)
                        lanceDir3 = (target.Center - npc.Center).SafeNormalize(Vector2.UnitY);
                    FindHeldWeapon<CryogenHeldStarnightLance>(npc)?.SetAim(lanceDir3.ToRotation());
                    SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.4f, Pitch = 0.6f }, npc.Center);
                }

                // Fire beams along pre-established warning lines; the held lance stabs forward with each launch
                if (timer == 120 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, lanceDir1, ModContent.ProjectileType<CryogenStarnightBeam>(), npc.damage / 2, 0f, Main.myPlayer);
                    FindHeldWeapon<CryogenHeldStarnightLance>(npc)?.Pulse(46f);
                    SoundEngine.PlaySound(SoundID.Item73 with { Volume = 0.8f, Pitch = 0.2f }, npc.Center);
                }
                else if (timer == 145 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, lanceDir2, ModContent.ProjectileType<CryogenStarnightBeam>(), npc.damage / 2, 0f, Main.myPlayer);
                    FindHeldWeapon<CryogenHeldStarnightLance>(npc)?.Pulse(46f);
                    SoundEngine.PlaySound(SoundID.Item73 with { Volume = 0.8f, Pitch = 0.2f }, npc.Center);
                }
                else if (timer == 170 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, lanceDir3, ModContent.ProjectileType<CryogenStarnightBeam>(), npc.damage / 2, 0f, Main.myPlayer);
                    FindHeldWeapon<CryogenHeldStarnightLance>(npc)?.Pulse(46f);
                    SoundEngine.PlaySound(SoundID.Item73 with { Volume = 0.8f, Pitch = 0.2f }, npc.Center);
                }
            }
            else // timer > 170
            {
                // Float back smoothly
                Vector2 desiredPos = target.Center + new Vector2(0f, -220f);
                SmoothMove(npc, desiredPos, 0.06f, 15f);
            }

            if (timer >= duration)
            {
                SelectNextAttack(npc, phase);
            }
        }

        // P2 Attack 9: Shadecrystal Barrage — 两种执行方式共用同一颗水晶焦点:
        // 变体A "读环出口"：玩家四周凝成一圈暗晶留一个缺口, 然后向心收拢.
        // 变体B "螺旋双臂"：只出2条会共同旋转的晶臂(非满环), 每颗晶体都是"内收+切向"混合速度形成螺旋弧线——
        // 从"找一个静止缺口"变成"追踪两条转动的手臂扫过的节奏", 完全不同的读法.
        private void ExecuteState_ShadecrystalBarrage(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            const int duration = 240;

            if (timer == 1)
                shadecrystalVariantB = UseVariantB(AttackState.ShadecrystalBarrage);

            // Cryogen mostly holds a flank — this attack is about reading the ring, not chasing the boss
            Vector2 spot = DirectedHoverSpot(npc, target, 220f, -300f, 6f);
            SmoothMove(npc, spot, 0.06f, 15f);

            // Held crystal focus, recoiling as each ring condenses
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<CryogenHeldShadecrystal>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (!shadecrystalVariantB)
            {
                // Ring 1 — gap opens along the player's current heading so committed movement stays rewarded
                if ((int)timer == 40)
                {
                    float gap = target.velocity.LengthSquared() > 4f ? target.velocity.ToRotation() : Main.rand.NextFloat(MathHelper.TwoPi);
                    SpawnShadeRing(npc, target, gap, 20, 3, 380f, 11f);
                    FindHeldWeapon<CryogenHeldShadecrystal>(npc)?.Pulse(-12f);
                }

                // Ring 2 (phase sublayer) — gap flips to the opposite side, forcing a direction change
                if (phase >= 5 && (int)timer == 130)
                {
                    float gap = (target.velocity.LengthSquared() > 4f ? target.velocity.ToRotation() : Main.rand.NextFloat(MathHelper.TwoPi)) + MathHelper.Pi;
                    SpawnShadeRing(npc, target, gap, 22, 3, 400f, 12f);
                    FindHeldWeapon<CryogenHeldShadecrystal>(npc)?.Pulse(-12f);
                }
            }
            else
            {
                // Twin spiral arms — co-rotating, sparse (only 2 of a conceptual 8-armed pinwheel), each crystal
                // launched with a blended inward+tangential velocity so it curves in rather than beelining straight.
                if ((int)timer == 40)
                {
                    float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    SpawnShadeSpiralArm(npc, target, baseAngle, 1f, 3, 380f, 10f);
                    SpawnShadeSpiralArm(npc, target, baseAngle + MathHelper.Pi, 1f, 3, 380f, 10f);
                    FindHeldWeapon<CryogenHeldShadecrystal>(npc)?.Pulse(-12f);
                }

                if (phase >= 5 && (int)timer == 130)
                {
                    float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    SpawnShadeSpiralArm(npc, target, baseAngle, -1f, 3, 400f, 11f);
                    SpawnShadeSpiralArm(npc, target, baseAngle + MathHelper.Pi, -1f, 3, 400f, 11f);
                    FindHeldWeapon<CryogenHeldShadecrystal>(npc)?.Pulse(-12f);
                }
            }

            if (timer >= duration)
                SelectNextAttack(npc, phase);
        }

        // Variant A: rings crystals around the player with one missing wedge (the exit). Each crystal
        // materializes in place, then closes inward.
        private void SpawnShadeRing(NPC npc, Player target, float gapAngle, int count, int gapCount, float radius, float speed)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 center = target.Center + target.velocity * 6f;
            float gapHalf = MathHelper.TwoPi * gapCount / count * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float ang = MathHelper.TwoPi * i / count;
                if (Math.Abs(MathHelper.WrapAngle(ang - gapAngle)) < gapHalf)
                    continue; // leave the exit open
                Vector2 pos = center + ang.ToRotationVector2() * radius;
                Vector2 inward = (center - pos).SafeNormalize(Vector2.UnitY) * speed;
                Projectile.NewProjectile(npc.GetSource_FromAI(), pos, inward, ModContent.ProjectileType<CryogenShadecrystal>(), npc.damage / 3, 0f, Main.myPlayer);
            }
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f }, center);
        }

        // Variant B: a tight cluster of crystals ("arm") that spirals inward — 75% radial pull, 50% tangential
        // drift, giving each one a curving path instead of a straight radial line. tangentSign flips the arm's
        // rotation direction; spawning two arms at +-Pi with the SAME sign makes them co-rotate as a pinwheel.
        private void SpawnShadeSpiralArm(NPC npc, Player target, float baseAngle, float tangentSign, int count, float radius, float speed)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 center = target.Center + target.velocity * 6f;
            for (int i = 0; i < count; i++)
            {
                float ang = baseAngle + (i - (count - 1) / 2f) * MathHelper.ToRadians(9f);
                Vector2 pos = center + ang.ToRotationVector2() * radius;
                Vector2 radial = (center - pos).SafeNormalize(Vector2.UnitY);
                Vector2 tangent = new Vector2(-radial.Y, radial.X) * tangentSign;
                Vector2 vel = (radial * 0.8f + tangent * 0.55f).SafeNormalize(Vector2.UnitY) * speed;
                Projectile.NewProjectile(npc.GetSource_FromAI(), pos, vel, ModContent.ProjectileType<CryogenShadecrystal>(), npc.damage / 3, 0f, Main.myPlayer);
            }
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f }, center);
        }

        // P2 Attack 10: Daedalus Golem Staff — 两种执行方式共用同一根召唤杖:
        // 变体A "双守卫"：两个各有专精(弹丸/闪电)的普通守卫分摊压力.
        // 变体B "超载独像"：只召唤一个更大更硬的守卫, 自己在同一节奏上轮流打出两种攻击——
        // 用"打死一个强敌"换"打死两个弱敌", 完全不同的清场决策.
        private void ExecuteState_DaedalusGolemStaff(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            int duration = 440;

            if (timer == 1)
                daedalusVariantB = UseVariantB(AttackState.DaedalusGolemStaff);

            // Slow hover, off to a flank — deliberately gentler than the rest since this attack's threat comes from
            // the golems, not the boss body. Was parked directly overhead for the full 440f; that's the exact
            // "free contact hit while you're busy dodging the golems" cheese the flank offset exists to prevent.
            Vector2 desiredPos = DirectedHoverSpot(npc, target, 260f, -260f, 8f);
            SmoothMove(npc, desiredPos, 0.03f, 9f);

            // Spawn breakable golem NPC(s) + the held summoning staff
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (!daedalusVariantB)
                {
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X - 100, (int)npc.Center.Y, ModContent.NPCType<CryogenDaedalusMinion>(), npc.whoAmI, npc.whoAmI, 0f, 0f);
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X + 100, (int)npc.Center.Y, ModContent.NPCType<CryogenDaedalusMinion>(), npc.whoAmI, npc.whoAmI, 1f, MathHelper.Pi);
                }
                else
                {
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<CryogenDaedalusMinion>(), npc.whoAmI, npc.whoAmI, 2f, 0f);
                }
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<CryogenHeldDaedalusStaff>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            // Staff beam: the aim line tracks the player for 40 warning frames (drawn in PreDraw), then the beam rides the final line.
            // Gated to timer >= 120 so the attack never opens with an untelegraphed shot on frame 0.
            float beamCycle = timer % 120;
            if (beamCycle >= 80 && timer < 400)
                daedalusAimDir = (target.Center - npc.Center).SafeNormalize(Vector2.UnitY);

            if (beamCycle == 0 && timer >= 120 && daedalusAimDir != Vector2.Zero)
            {
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.6f, Pitch = -0.1f }, npc.Center);
                FindHeldWeapon<CryogenHeldDaedalusStaff>(npc)?.Pulse(-18f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, daedalusAimDir * 16f, ModContent.ProjectileType<CryogenLightBeam>(), npc.damage / 2, 0f, Main.myPlayer);
            }

            if (timer >= duration)
            {
                SelectNextAttack(npc, phase);
            }
        }

        // P2 Attack 11: Darkecho bow and Shimmerspark Yoyo — 两种执行方式共用同一副弓+悠悠球:
        // 变体A "双重齐射"：两次重装填齐射(箭+高速镖), 中间瞬移换位.
        // 变体B "回响乱流"：Boss基本定住不挪位, 弓持续单发点射, 瞄准角随时间缓慢摆动形成扫过全场的旋转扇形——
        // 用"持续单点压力"换"两次重击", 玩家要一直跟着扇形摆动重新站位而不是等两次爆发.
        private void ExecuteState_DarkechoAndShimmerspark(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            int duration = 440;

            if (timer == 1)
            {
                darkechoVariantB = UseVariantB(AttackState.DarkechoAndShimmerspark);

                // Spawn orbiting yoyo + the held greatbow
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<CryogenShimmersparkYoyo>(), npc.damage / 3, 0f, Main.myPlayer, npc.whoAmI);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<CryogenHeldDarkechoBow>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                }
            }

            if (!darkechoVariantB)
            {
                if (timer == 60)
                {
                    // Fire arrows and crystal dart
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 baseVel = (target.Center - npc.Center).SafeNormalize(Vector2.UnitY);

                        // Double arrows
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, baseVel.RotatedBy(0.18f) * 11f, ModContent.ProjectileType<CryogenDarkechoArrow>(), npc.damage / 3, 0f, Main.myPlayer);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, baseVel.RotatedBy(-0.18f) * 11f, ModContent.ProjectileType<CryogenDarkechoArrow>(), npc.damage / 3, 0f, Main.myPlayer);

                        // High speed Crystal dart (bounces once)
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, baseVel * 24f, ModContent.ProjectileType<CryogenCrystalDart>(), npc.damage / 2, 0f, Main.myPlayer);
                    }
                    SoundEngine.PlaySound(SoundID.Item5, npc.Center);
                    FindHeldWeapon<CryogenHeldDarkechoBow>(npc)?.Pulse(-20f);
                }
                else if (timer == 176)
                {
                    // Telegraphed blink to the player's far side — fully re-formed before the volley at 202
                    float side = Math.Sign(target.Center.X - npc.Center.X);
                    if (side == 0f)
                        side = 1f;
                    BeginTeleport(npc, target.Center + new Vector2(-side * 300f, -200f), 22);
                }
                else if (timer == 202)
                {
                    // Second volley from the blink destination
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 baseVel = (target.Center - npc.Center).SafeNormalize(Vector2.UnitY);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, baseVel.RotatedBy(0.18f) * 11f, ModContent.ProjectileType<CryogenDarkechoArrow>(), npc.damage / 3, 0f, Main.myPlayer);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, baseVel.RotatedBy(-0.18f) * 11f, ModContent.ProjectileType<CryogenDarkechoArrow>(), npc.damage / 3, 0f, Main.myPlayer);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, baseVel * 24f, ModContent.ProjectileType<CryogenCrystalDart>(), npc.damage / 2, 0f, Main.myPlayer);
                    }
                    SoundEngine.PlaySound(SoundID.Item5, npc.Center);
                    FindHeldWeapon<CryogenHeldDarkechoBow>(npc)?.Pulse(-20f);
                }
                else
                {
                    // Keep orbiting
                    Vector2 desiredPos = target.Center + new Vector2(MathF.Sin(ticksRunning * 0.02f) * 260f, -180f);
                    SmoothMove(npc, desiredPos, 0.045f, 13f);
                }
            }
            else
            {
                // Boss holds a much steadier position — the rotating fan sweep is the whole threat
                Vector2 desiredPos = target.Center + new Vector2(MathF.Sin(ticksRunning * 0.01f) * 120f, -200f);
                SmoothMove(npc, desiredPos, 0.035f, 9f);

                if (timer >= 40 && timer <= 380 && (int)(timer - 40) % 18 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float sweep = MathF.Sin((timer - 40) * 0.025f) * 0.7f;
                    Vector2 vel = (target.Center - npc.Center).SafeNormalize(Vector2.UnitY).RotatedBy(sweep) * 12.5f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<CryogenDarkechoArrow>(), npc.damage / 3, 0f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.35f, Pitch = sweep }, npc.Center);
                    FindHeldWeapon<CryogenHeldDarkechoBow>(npc)?.Pulse(-8f);
                }
            }

            if (timer >= duration)
            {
                SelectNextAttack(npc, phase);
            }
        }

        // P2 Attack 12: Crystal Piercer — 两种执行方式共用同一把标枪:
        // 变体A "三向轮流"：左/右/上三批标枪按140帧节奏依次入场, 逐一应对.
        // 变体B "四面汇聚"：更长的一次性预警后, 三个方向同时发射——从"轮流各个击破"变成"一次性找唯一安全象限".
        private void ExecuteState_CrystalPiercer(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            if (timer == 1)
                piercerVariantB = UseVariantB(AttackState.CrystalPiercer);

            int duration = piercerVariantB ? 300 : 360;

            // Flank hover — was parked directly overhead for all 360f, which put contact damage right where a
            // player dodging vertical javelins (Group 3 drops from above) would naturally drift to.
            Vector2 desiredPos = DirectedHoverSpot(npc, target, 240f, -220f, 6f);
            SmoothMove(npc, desiredPos, 0.05f, 17f);

            // Held javelin that jabs toward the player as each phalanx group launches
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<CryogenHeldCrystalPiercer>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (!piercerVariantB)
            {
                // Group 1: 3 curved gravity javelins from left (Frame 20)
                if (timer == 20 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 spawn = target.Center + new Vector2(-600f, -200f + i * 120f);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(18f, -4f), ModContent.ProjectileType<CryogenJavelin>(), npc.damage / 3, 0f, Main.myPlayer, 0f);
                    }
                    SoundEngine.PlaySound(SoundID.Item1, npc.Center);
                    FindHeldWeapon<CryogenHeldCrystalPiercer>(npc)?.Pulse(24f);
                }

                // Group 2: 3 straight horizontal javelins from right (Frame 80)
                if (timer == 80 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 spawn = target.Center + new Vector2(600f, -200f + i * 120f);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(-16f, 0f), ModContent.ProjectileType<CryogenJavelin>(), npc.damage / 3, 0f, Main.myPlayer, 1f);
                    }
                    SoundEngine.PlaySound(SoundID.Item1, npc.Center);
                    FindHeldWeapon<CryogenHeldCrystalPiercer>(npc)?.Pulse(24f);
                }

                // Group 3: 2 vertical dropping javelins from top (Frame 160)
                if (timer == 160 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + new Vector2(-180f, -500f), new Vector2(0f, 14f), ModContent.ProjectileType<CryogenJavelin>(), npc.damage / 3, 0f, Main.myPlayer, 2f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + new Vector2(180f, -500f), new Vector2(0f, 14f), ModContent.ProjectileType<CryogenJavelin>(), npc.damage / 3, 0f, Main.myPlayer, 2f);
                    SoundEngine.PlaySound(SoundID.Item1, npc.Center);
                    FindHeldWeapon<CryogenHeldCrystalPiercer>(npc)?.Pulse(24f);
                }
            }
            else
            {
                // All three directions telegraph together for a longer window (30-70), then launch simultaneously
                // at 70 — same projectile types/speeds as variant A, just no longer staggered into three beats.
                if (timer == 70 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 spawn = target.Center + new Vector2(-600f, -200f + i * 120f);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(18f, -4f), ModContent.ProjectileType<CryogenJavelin>(), npc.damage / 3, 0f, Main.myPlayer, 0f);
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 spawn = target.Center + new Vector2(600f, -200f + i * 120f);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(-16f, 0f), ModContent.ProjectileType<CryogenJavelin>(), npc.damage / 3, 0f, Main.myPlayer, 1f);
                    }
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + new Vector2(-180f, -500f), new Vector2(0f, 14f), ModContent.ProjectileType<CryogenJavelin>(), npc.damage / 3, 0f, Main.myPlayer, 2f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + new Vector2(180f, -500f), new Vector2(0f, 14f), ModContent.ProjectileType<CryogenJavelin>(), npc.damage / 3, 0f, Main.myPlayer, 2f);
                    SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.2f, Volume = 1.1f }, npc.Center);
                    target.Calamity().GeneralScreenShakePower = 6f;
                    FindHeldWeapon<CryogenHeldCrystalPiercer>(npc)?.Pulse(30f);
                }
            }

            if (timer >= duration)
            {
                SelectNextAttack(npc, phase);
            }
        }
        #endregion

        #region Phase 3->4 Transition & Victory states
        private void ExecuteState_FreezeTransition(NPC npc, ref float timer)
        {
            npc.damage = 0;
            npc.velocity *= 0.85f;

            // Invulnerable for the whole performance. Without this, sustained high DPS (routine in IUMW-tier
            // gear) keeps landing hits through the entire 30-90f cutscene, and life keeps dropping the whole
            // time CheckPhaseTransitions is asleep (its guard skips re-checking while state==FreezeTransition).
            // The instant the transition ends and the check resumes, it sees a life ratio that's already sunk
            // past one or two MORE thresholds and immediately re-triggers another transition — the exact "it
            // skips straight from the P1 cycle to the last P2 cycle" the boss was doing. Every multi-phase
            // Calamity/Infernum boss is untouchable during its own transformation cutscene for this reason.
            npc.dontTakeDamage = true;

            int transitionDuration = 40;
            if (targetSubphase == 4)
            {
                transitionDuration = 90; // Big Transition to Phase 4
            }
            else if (targetSubphase == 2)
            {
                transitionDuration = 30;
            }
            else if (targetSubphase == 6)
            {
                transitionDuration = 50;
            }

            // Flash effect windup
            if (timer < transitionDuration - 10)
            {
                transitionFlashAlpha = MathHelper.Clamp(timer / (transitionDuration - 10f), 0f, 1f);
            }
            else
            {
                transitionFlashAlpha = MathHelper.Clamp((transitionDuration - timer) / 10f, 0f, 1f);
            }

            // Mid-transition shell shatter blast for Phase 4 (at frame 60)
            if (targetSubphase == 4 && (int)timer == 60)
            {
                TriggerShatterBlast(npc);
            }

            // Transition completion
            if (timer >= transitionDuration)
            {
                transitionFlashAlpha = 0f;

                // If not Phase 4, trigger shatter blast at the end of transition
                if (targetSubphase != 4)
                {
                    TriggerShatterBlast(npc);
                }

                // Update phase and sync
                npc.ai[0] = targetSubphase;
                npc.dontTakeDamage = false; // vulnerable again the instant the new phase's attack starts

                // Continue the attack rotation into the new phase's (longer) cycle so new weapons actually
                // get their turn. Only the P1->P2 unseal restarts the rotation: the new arsenal debuts in order.
                if (targetSubphase == 4)
                    attackCycleIndex = -1;
                SelectNextAttack(npc, targetSubphase);
            }
        }

        private void TriggerShatterBlast(NPC npc)
        {
            SoundEngine.PlaySound(TransitionSound, npc.Center);
            SoundEngine.PlaySound(BlastSound, npc.Center);

            Player target = Main.player[npc.target];
            if (target.active && !target.dead)
            {
                target.Calamity().GeneralScreenShakePower = targetSubphase == 4 ? 15f : 12f;
            }

            // Shards explosion on server
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int shardCount = targetSubphase == 4 ? 24 : 20;
                for (int i = 0; i < shardCount; i++)
                {
                    Vector2 vel = (MathHelper.TwoPi * i / (float)shardCount).ToRotationVector2() * 6.5f;
                    Projectile.NewProjectile(
                        npc.GetSource_FromAI(),
                        npc.Center,
                        vel,
                        ModContent.ProjectileType<CryogenJavelinShard>(),
                        npc.damage / 3,
                        0f,
                        Main.myPlayer
                    );
                }
            }

            // 40-60 visual-only shattering dust shards — blooming, with a slight upward drift
            int visualShards = targetSubphase == 4 ? 50 : 25;
            for (int i = 0; i < visualShards; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * Main.rand.NextFloat()).ToRotationVector2() * Main.rand.NextFloat(4f, 14f);
                vel.Y -= 2f;
                Color dustColor = Main.rand.NextBool() ? Color.Cyan : Color.Purple;
                Dust d = Dust.NewDustPerfect(npc.Center, Main.rand.NextBool() ? DustID.Ice : DustID.Vortex, vel, 100, dustColor, Main.rand.NextFloat(1.1f, 1.7f));
                d.fadeIn = 1.5f;
                d.noGravity = true;
            }

            EmitCryoDustRing(npc.Center, 60f, 35, 4.5f);
            EmitCryoDustRing(npc.Center, 40f, 25, 3f);
            EmitFrostBurst(npc.Center, 6f, targetSubphase == 4 ? 20 : 10, targetSubphase == 4 ? 8 : 4);
        }

        private void ExecuteState_DeathAnimation(NPC npc, ref float timer)
        {
            npc.damage = 0;
            npc.velocity *= 0.88f;
            npc.rotation += 0.05f;

            if (timer % 6 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item27, npc.Center);
                EmitCryoDustRing(npc.Center, 30f, 12, 2.5f);
            }

            // Escalating bloom bursts as the core gives out
            if (timer % 12 == 0)
                EmitFrostBurst(npc.Center, 3.5f, 15);

            if (timer >= 120)
            {
                EmitFrostBurst(npc.Center, 7f, 60, 8);

                // Real kill — loot, death sound and gore all fire normally (npc.active = false would eat the drops)
                npc.dontTakeDamage = false;
                npc.StrikeInstantKill();
            }
        }

        private void ExecuteVictoryDespawn(NPC npc)
        {
            npc.velocity.Y -= 0.2f;
            if (npc.velocity.Y < -15f)
            {
                npc.velocity.Y = -15f;
            }

            npc.rotation += 0.05f;

            if (npc.timeLeft > 10)
            {
                npc.timeLeft = 10;
            }
        }
        #endregion

        #region Helper Math & Spawning Functions
        // Instance method (not static) so the snow-biome enrage multiplier can scale every hover call
        // through one choke point instead of touching all 13 call sites individually.
        private void SmoothMove(NPC npc, Vector2 desiredPosition, float acceleration, float maxSpeed)
        {
            maxSpeed *= enrageSpeedMultiplier;
            Vector2 desiredVelocity = (desiredPosition - npc.Center) * acceleration;
            if (desiredVelocity.Length() > maxSpeed)
            {
                desiredVelocity = Vector2.Normalize(desiredVelocity) * maxSpeed;
            }
            npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, 0.145f);
        }

        // --- Director-layer helpers ------------------------------------------------------------------------------------

        // Begin a telegraphed blink: Cryogen dissolves, frost converges on the destination, then it reforms there.
        private void BeginTeleport(NPC npc, Vector2 destination, int windup = 26)
        {
            teleportDestination = destination;
            teleportDuration = windup;
            teleportTimer = 0;
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = 0.2f }, npc.Center);
            npc.netUpdate = true;
        }

        // Runs every frame (no-op unless teleporting). Owns velocity/opacity/position while active. Returns true if mid-blink.
        private bool UpdateTeleport(NPC npc)
        {
            if (teleportDuration <= 0)
                return false;

            teleportTimer++;
            int half = Math.Max(1, teleportDuration / 2);
            npc.velocity *= 0.8f;

            if (teleportTimer < half)
            {
                // Dissolve at the old spot; pull convergent frost toward the destination so the move telegraphs itself
                npc.Opacity = MathHelper.Lerp(1f, 0f, teleportTimer / (float)half);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 around = teleportDestination + (MathHelper.TwoPi * Main.rand.NextFloat()).ToRotationVector2() * Main.rand.NextFloat(70f, 130f);
                    Vector2 vel = (teleportDestination - around) * 0.06f;
                    Dust d = Dust.NewDustPerfect(around, DustID.Ice, vel, 100, Color.Cyan, Main.rand.NextFloat(1.1f, 1.4f));
                    d.fadeIn = 1.3f;
                    d.noGravity = true;
                }
                EmitFrostBurst(teleportDestination, 3f, 3);
            }
            else if (teleportTimer == half)
            {
                npc.Center = teleportDestination;
                npc.velocity = Vector2.Zero;
                SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.7f }, npc.Center);
                EmitCryoDustRing(npc.Center, 46f, 22, 4f);
                EmitFrostBurst(npc.Center, 4f, 40);

                // Collapse the afterimage trail onto the new spot so the blink doesn't smear across the arena
                for (int i = 0; i < cryoOldPos.Length; i++)
                    cryoOldPos[i] = npc.Center;
            }
            else
            {
                npc.Opacity = MathHelper.Lerp(0f, 1f, (teleportTimer - half) / (float)half);
            }

            // Never cheese contact damage while invisible
            if (npc.Opacity < 0.5f)
                npc.damage = 0;

            if (teleportTimer >= teleportDuration)
            {
                teleportDuration = 0;
                npc.Opacity = 1f;
            }
            return true;
        }

        // The CryoStoneBarrier defines a fixed arena. Read its center/floor so attacks can lay geometry relative to it.
        private static float TryGetArenaCenterX(Player target)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == ModContent.ProjectileType<CryoStoneBarrier>())
                    return p.Center.X;
            }
            return target.Center.X;
        }

        private static float TryGetArenaFloorY(Player target)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == ModContent.ProjectileType<CryoStoneBarrier>())
                    return p.Center.Y;
            }
            return target.Center.Y + 260f;
        }

        // Side-above hover spot with anti-cheese: never sits directly on the player, keeps to the side Cryogen is already on,
        // and leads the target a little so it feels like it's reacting rather than snapping.
        private static Vector2 DirectedHoverSpot(NPC npc, Player target, float sideOffset, float heightOffset, float lead = 0f)
        {
            float side = Math.Sign(npc.Center.X - target.Center.X);
            if (side == 0f)
                side = Main.rand.NextBool() ? 1f : -1f;
            Vector2 predicted = target.Center + target.velocity * lead;
            return predicted + new Vector2(side * sideOffset, heightOffset);
        }

        // Standard frost burst — bright AncientLight motes that bloom (fadeIn), drift upward, and scatter wide.
        // This is the workhorse pop for teleports, impacts and deaths; add mist puffs for the heavy moments.
        private static void EmitFrostBurst(Vector2 position, float speed, int count, int mistCount = 0)
        {
            for (int i = 0; i < count; i++)
            {
                Dust ice = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(62f, 62f), DustID.AncientLight);
                ice.color = Color.Lerp(Color.White, Color.Cyan, Main.rand.NextFloat(0.15f, 0.7f));
                ice.velocity = Main.rand.NextVector2Circular(speed, speed) - Vector2.UnitY * 1.6f;
                ice.scale = Main.rand.NextFloat(1.2f, 1.6f);
                ice.fadeIn = 1.5f;
                ice.noGravity = true;
            }

            for (int i = 0; i < mistCount; i++)
            {
                var mist = new CalamityMod.Particles.MediumMistParticle(position + Main.rand.NextVector2Circular(24f, 24f), Main.rand.NextVector2Circular(6f, 6f), new Color(172, 238, 255), new Color(145, 170, 188), Main.rand.NextFloat(0.5f, 1.4f), 220f - Main.rand.Next(50), 0.02f);
                CalamityMod.Particles.GeneralParticleHandler.SpawnParticle(mist);
            }
        }

        // Violet burst ring for P2 dash launches (the P2 counterpart of EmitCryoDustRing)
        private static void EmitShadowBurst(Vector2 center)
        {
            for (int i = 0; i < 16; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * 5.5f;
                Dust d = Dust.NewDustPerfect(center, DustID.Vortex, vel, 120, Color.MediumPurple, 1.2f);
                d.noGravity = true;
            }
        }

        private static void EmitCryoDustRing(Vector2 center, float radius, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / count).ToRotationVector2() * speed;
                Vector2 spawnPos = center + vel.SafeNormalize(Vector2.UnitX) * radius;
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Ice, vel, 100, Color.Cyan, Main.rand.NextFloat(0.8f, 1.3f));
                d.noGravity = true;
            }
        }
        #endregion

        #region Drawing & Visual Layouts
        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;
            if (npc.frameCounter >= 5)
            {
                npc.frame.Y += frameHeight;
                if (npc.frame.Y >= frameHeight * Main.npcFrameCount[npc.type])
                {
                    npc.frame.Y = 0;
                }
                npc.frameCounter = 0;
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D coreTex = TextureAssets.Npc[npc.type].Value;
            Texture2D shieldTex = GetCalamityTexture(ProjShield, coreTex);
            Vector2 drawPos = npc.Center - screenPos;
            int currentSubphase = (int)npc.ai[0];

            // Handle visual stretching & tilting based on dash states
            float drawRotation = npc.rotation;
            Vector2 drawScaleVec = new Vector2(npc.scale);

            AttackState state = (AttackState)(int)npc.ai[1];
            float timer = npc.ai[2];

            if (state == AttackState.Icebreaker)
            {
                // Dash 1 is frame 30-100, Dash 2 is frame 130-200
                bool charging = timer < 30f || (timer >= 100f && timer < 130f);
                bool dashing = (timer >= 30f && timer < 80f) || (timer >= 130f && timer < 180f);

                if (charging)
                {
                    // Tilt back opposing player side
                    Player targetPlayer = Main.player[npc.target];
                    float side = targetPlayer.active ? Math.Sign(npc.Center.X - targetPlayer.Center.X) : 1f;
                    if (side == 0f) side = 1f;
                    drawRotation = side * 0.436f; // ~25 degrees
                    
                    // Compress body to show wind-up
                    drawScaleVec.X *= 1.12f;
                    drawScaleVec.Y *= 0.85f;
                }
                else if (dashing)
                {
                    if (npc.velocity != Vector2.Zero)
                    {
                        drawRotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                        drawScaleVec.Y *= 1.25f; // Stretch along motion
                        drawScaleVec.X *= 0.82f; // Squash sideways
                    }
                }
            }

            // 0. Motion afterimages (behind everything) — frost ghosts in P1, violet in P2
            Color trailTint = currentSubphase >= 4 ? new Color(150, 90, 230) : new Color(120, 200, 255);
            for (int i = 0; i < cryoOldPos.Length; i++)
            {
                int idx = (cryoOldPosIndex - 1 - i + cryoOldPos.Length) % cryoOldPos.Length;
                Vector2 ghostPos = cryoOldPos[idx];
                if (ghostPos == Vector2.Zero)
                    continue;
                float a = (1f - i / (float)cryoOldPos.Length) * 0.33f * npc.Opacity;
                spriteBatch.Draw(coreTex, ghostPos - screenPos, npc.frame, trailTint * a, drawRotation, npc.frame.Size() * 0.5f, drawScaleVec * (1f - i * 0.02f), SpriteEffects.None, 0f);
            }

            // 1. Color shift based on P1 / P2 (interpolated during Phase 3->4 Transition)
            float transitionLerp = 0f;
            if (state == AttackState.FreezeTransition && targetSubphase == 4)
            {
                if (timer >= 60f)
                    transitionLerp = MathHelper.Clamp((timer - 60f) / 30f, 0f, 1f);
            }
            else if (currentSubphase >= 4)
            {
                transitionLerp = 1f;
            }

            Color finalDrawColor = drawColor;
            if (transitionLerp > 0f)
            {
                Color p1Color = Color.Lerp(drawColor, new Color(200, 240, 255), 0.3f);
                Color p2Color = Color.Lerp(drawColor, new Color(90, 70, 200), 0.6f);
                finalDrawColor = Color.Lerp(p1Color, p2Color, transitionLerp) * npc.Opacity;
            }
            else
            {
                finalDrawColor = Color.Lerp(drawColor, new Color(200, 240, 255), 0.3f) * npc.Opacity;
            }

            // 1.5 Draw Crystal Wings (Soul of Cryogen wing visuals)
            if (wingDrawScale > 0.01f)
            {
                Texture2D wingCrystalTex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Summon/GlacialEmbracePointyThing", AssetRequestMode.ImmediateLoad).Value;
                if (wingCrystalTex != null)
                {
                    int feathers = 4;
                    Vector2 wingBase = npc.Center - screenPos;
                    Color wingColor = Color.Lerp(Color.DeepSkyBlue, Color.Violet, 0.5f + 0.5f * (float)Math.Sin(ticksRunning * 0.1f)) * wingDrawScale * npc.Opacity * 0.85f;
                    Color glowColor = Color.Cyan * wingDrawScale * npc.Opacity * 0.4f;

                    for (int side = -1; side <= 1; side += 2)
                    {
                        SpriteEffects effects = side == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                        for (int j = 0; j < feathers; j++)
                        {
                            float baseAngle = side == 1 ? MathHelper.PiOver4 : MathHelper.Pi - MathHelper.PiOver4;
                            float angleOffset = side * (j * 0.16f - 0.24f) * wingDrawScale;
                            float rotation = baseAngle + angleOffset;
                            Vector2 posOffset = new Vector2(side * (40f + j * 16f) * wingDrawScale, -10f + j * 4f);
                            Vector2 finalPos = wingBase + posOffset.RotatedBy(npc.rotation);

                            for (int k = 0; k < 4; k++)
                            {
                                Vector2 off = (MathHelper.TwoPi * k / 4f).ToRotationVector2() * 2f;
                                spriteBatch.Draw(
                                    wingCrystalTex,
                                    finalPos + off,
                                    null,
                                    glowColor,
                                    rotation + npc.rotation,
                                    wingCrystalTex.Size() * 0.5f,
                                    npc.scale * (1.1f - j * 0.12f) * wingDrawScale,
                                    effects,
                                    0f
                                );
                            }

                            spriteBatch.Draw(
                                wingCrystalTex,
                                finalPos,
                                null,
                                wingColor,
                                rotation + npc.rotation,
                                wingCrystalTex.Size() * 0.5f,
                                npc.scale * (1.1f - j * 0.12f) * wingDrawScale,
                                effects,
                                0f
                            );
                        }
                    }
                }
            }

            // 2. Draw 10-Hit Shield (Glacial Embrace) in P1
            if (shieldActive && shieldTex != coreTex && currentSubphase <= 3)
            {
                Vector2 shieldScale = drawScaleVec * (1.1f + 0.05f * (float)Math.Sin(ticksRunning * 0.1f)) * shieldChargeProgress;
                Color c = Color.Cyan * shieldChargeProgress * 0.72f * npc.Opacity;
                
                spriteBatch.Draw(
                    shieldTex,
                    drawPos,
                    null,
                    c,
                    shieldRotation + (drawRotation - npc.rotation),
                    shieldTex.Size() * 0.5f,
                    shieldScale,
                    SpriteEffects.None,
                    0f
                );
            }

            // 3. Draw Core with tilting (摇头晃脑) and breathing
            spriteBatch.Draw(
                coreTex,
                drawPos,
                npc.frame,
                npc.GetAlpha(finalDrawColor),
                drawRotation,
                npc.frame.Size() * 0.5f,
                drawScaleVec,
                SpriteEffects.None,
                0f
            );

            // 3.5 Draw dash warning telegraph lines for Icebreaker
            if (state == AttackState.Icebreaker)
            {
                bool showTelegraph = (timer >= 15f && timer < 30f) || (timer >= 115f && timer < 130f);
                if (showTelegraph)
                {
                    Player playerTarget = Main.player[npc.target];
                    if (playerTarget.active && !playerTarget.dead)
                    {
                        float progress = (timer < 30f) ? (timer - 15f) / 15f : (timer - 115f) / 15f;
                        Vector2 delta = playerTarget.Center - npc.Center;
                        float len = delta.Length();
                        float rot = delta.ToRotation();
                        spriteBatch.End();
                        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                        spriteBatch.Draw(TextureAssets.MagicPixel.Value, npc.Center - screenPos, new Rectangle(0, 0, 1, 1), Color.Red * progress * 0.65f, rot, new Vector2(0f, 0.5f), new Vector2(len, 3f), SpriteEffects.None, 0f);
                        spriteBatch.End();
                        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                    }
                }
            }

            // 3.6 Draw vertical warning telegraph line for Avalanche
            if (state == AttackState.Avalanche)
            {
                if (timer >= 70f && timer < 92f)
                {
                    float floorY = TryGetArenaFloorY(Main.player[npc.target]);
                    Vector2 top = npc.Center;
                    Vector2 bottom = new Vector2(npc.Center.X, floorY);
                    float progress = (timer - 70f) / 22f;

                    spriteBatch.End();
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value, top - screenPos, new Rectangle(0, 0, 1, 1), Color.Red * progress * 0.7f, MathHelper.PiOver2, new Vector2(0f, 0.5f), new Vector2(bottom.Y - top.Y, 4f), SpriteEffects.None, 0f);
                    spriteBatch.End();
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                }
            }

            // 3.7 Draw Transition Ice Sphere (Phase 3->4 Transition)
            if (state == AttackState.FreezeTransition && targetSubphase == 4)
            {
                if (timer < 60f)
                {
                    float radius = MathHelper.Lerp(0f, 180f, Math.Min(timer / 30f, 1f));
                    float sphereAlpha = 0.5f * npc.Opacity;
                    if (timer >= 30f)
                    {
                        // Flicker during crack phase
                        sphereAlpha *= 0.8f + 0.2f * (float)Math.Sin(Main.GameUpdateCount * 0.4f);
                    }

                    spriteBatch.End();
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

                    // Draw outer glow using boss core texture scaled up
                    float capScale = (radius * 2f) / coreTex.Width;
                    Color sphereColor = Color.White * sphereAlpha;
                    spriteBatch.Draw(
                        coreTex,
                        drawPos,
                        npc.frame,
                        sphereColor,
                        ticksRunning * 0.01f,
                        npc.frame.Size() * 0.5f,
                        capScale,
                        SpriteEffects.None,
                        0f
                    );

                    // If cracked, draw bright purple overlay veins
                    if (timer >= 30f)
                    {
                        float crackProgress = (timer - 30f) / 30f;
                        Color crackColor = Color.Lerp(Color.Purple, Color.Magenta, 0.5f + 0.5f * (float)Math.Sin(Main.GameUpdateCount * 0.2f)) * crackProgress * npc.Opacity;
                        
                        // Draw offset copies to look like veins/cracks
                        for (int k = 0; k < 4; k++)
                        {
                            Vector2 off = (MathHelper.TwoPi * k / 4f).ToRotationVector2() * 3f;
                            spriteBatch.Draw(
                                coreTex,
                                drawPos + off,
                                npc.frame,
                                crackColor,
                                -ticksRunning * 0.015f,
                                npc.frame.Size() * 0.5f,
                                capScale * 0.98f,
                                SpriteEffects.None,
                                0f
                            );
                        }
                    }

                    spriteBatch.End();
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                }
            }

            // 3.8 Draw diagonal warning telegraph lines for Darklight Greatsword
            if (state == AttackState.DarklightGreatsword)
            {
                bool showTelegraph = (timer >= 26f && timer < 60f) || (currentSubphase == 6 && timer >= 126f && timer < 150f);
                if (showTelegraph)
                {
                    Player playerTarget = Main.player[npc.target];
                    if (playerTarget.active && !playerTarget.dead)
                    {
                        float progress = (timer < 60f) ? (timer - 26f) / 34f : (timer - 126f) / 24f;
                        Vector2 delta = playerTarget.Center - npc.Center;
                        float len = delta.Length() + 400f; // extend warning line past player
                        float rot = delta.ToRotation();
                        spriteBatch.End();
                        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                        spriteBatch.Draw(TextureAssets.MagicPixel.Value, npc.Center - screenPos, new Rectangle(0, 0, 1, 1), Color.Purple * progress * 0.75f, rot, new Vector2(0f, 0.5f), new Vector2(len, 6f), SpriteEffects.None, 0f);
                        spriteBatch.End();
                        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                    }
                }
            }

            // 3.9 Draw warning lines for Crystal Piercer
            if (state == AttackState.CrystalPiercer)
            {
                Player playerTarget = Main.player[npc.target];
                if (playerTarget.active && !playerTarget.dead && !piercerVariantB)
                {
                    // Group 1: Left warning lines (frames 1-20)
                    if (timer >= 1f && timer < 20f)
                    {
                        float progress = timer / 20f;
                        spriteBatch.End();
                        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 start = playerTarget.Center + new Vector2(-700f, -200f + i * 120f);
                            spriteBatch.Draw(TextureAssets.MagicPixel.Value, start - screenPos, new Rectangle(0, 0, 1, 1), Color.Cyan * progress * 0.55f, 0f, new Vector2(0f, 0.5f), new Vector2(1400f, 2f), SpriteEffects.None, 0f);
                        }
                        spriteBatch.End();
                        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                    }

                    // Group 2: Right warning lines (frames 60-80)
                    if (timer >= 60f && timer < 80f)
                    {
                        float progress = (timer - 60f) / 20f;
                        spriteBatch.End();
                        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 start = playerTarget.Center + new Vector2(700f, -200f + i * 120f);
                            spriteBatch.Draw(TextureAssets.MagicPixel.Value, start - screenPos, new Rectangle(0, 0, 1, 1), Color.Cyan * progress * 0.55f, MathHelper.Pi, new Vector2(0f, 0.5f), new Vector2(1400f, 2f), SpriteEffects.None, 0f);
                        }
                        spriteBatch.End();
                        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                    }

                    // Group 3: Top warning lines (frames 140-160)
                    if (timer >= 140f && timer < 160f)
                    {
                        float progress = (timer - 140f) / 20f;
                        float floorY = TryGetArenaFloorY(playerTarget);
                        spriteBatch.End();
                        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

                        Vector2 pos1 = playerTarget.Center + new Vector2(-180f, -600f);
                        Vector2 pos2 = playerTarget.Center + new Vector2(180f, -600f);
                        float lenY = floorY - pos1.Y;

                        spriteBatch.Draw(TextureAssets.MagicPixel.Value, pos1 - screenPos, new Rectangle(0, 0, 1, 1), Color.Cyan * progress * 0.55f, MathHelper.PiOver2, new Vector2(0f, 0.5f), new Vector2(lenY, 2f), SpriteEffects.None, 0f);
                        spriteBatch.Draw(TextureAssets.MagicPixel.Value, pos2 - screenPos, new Rectangle(0, 0, 1, 1), Color.Cyan * progress * 0.55f, MathHelper.PiOver2, new Vector2(0f, 0.5f), new Vector2(lenY, 2f), SpriteEffects.None, 0f);

                        spriteBatch.End();
                        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                    }
                }

                // Variant B: all three directions telegraph together across a longer 30-70 window, matching the
                // simultaneous launch at 70 exactly (timing here MUST mirror the AI's spawn frame — see the
                // MagicPixel note above about telegraphs silently going stale if the two drift apart).
                if (playerTarget.active && !playerTarget.dead && piercerVariantB && timer >= 30f && timer < 70f)
                {
                    float progress = (timer - 30f) / 40f;
                    float floorY = TryGetArenaFloorY(playerTarget);
                    spriteBatch.End();
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 leftStart = playerTarget.Center + new Vector2(-700f, -200f + i * 120f);
                        spriteBatch.Draw(TextureAssets.MagicPixel.Value, leftStart - screenPos, new Rectangle(0, 0, 1, 1), Color.Cyan * progress * 0.55f, 0f, new Vector2(0f, 0.5f), new Vector2(1400f, 2f), SpriteEffects.None, 0f);

                        Vector2 rightStart = playerTarget.Center + new Vector2(700f, -200f + i * 120f);
                        spriteBatch.Draw(TextureAssets.MagicPixel.Value, rightStart - screenPos, new Rectangle(0, 0, 1, 1), Color.Cyan * progress * 0.55f, MathHelper.Pi, new Vector2(0f, 0.5f), new Vector2(1400f, 2f), SpriteEffects.None, 0f);
                    }

                    Vector2 topPos1 = playerTarget.Center + new Vector2(-180f, -600f);
                    Vector2 topPos2 = playerTarget.Center + new Vector2(180f, -600f);
                    float lenY = floorY - topPos1.Y;
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value, topPos1 - screenPos, new Rectangle(0, 0, 1, 1), Color.Cyan * progress * 0.55f, MathHelper.PiOver2, new Vector2(0f, 0.5f), new Vector2(lenY, 2f), SpriteEffects.None, 0f);
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value, topPos2 - screenPos, new Rectangle(0, 0, 1, 1), Color.Cyan * progress * 0.55f, MathHelper.PiOver2, new Vector2(0f, 0.5f), new Vector2(lenY, 2f), SpriteEffects.None, 0f);

                    spriteBatch.End();
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                }
            }

            // 3.10 Draw the three locked lance aim-lines for Starnight Lance (each burns until its beam fires)
            if (state == AttackState.StarnightLance && timer >= 30f && timer < 170f)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

                DrawLanceAimLine(spriteBatch, npc, screenPos, lanceDir1, timer, 30f, 120f);
                DrawLanceAimLine(spriteBatch, npc, screenPos, lanceDir2, timer, 60f, 145f);
                DrawLanceAimLine(spriteBatch, npc, screenPos, lanceDir3, timer, 90f, 170f);

                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }

            // 3.11 Horizontal strafe-lane warning for Soul of Cryogen — painted at the sweep altitude before the boss enters it
            if (state == AttackState.SoulofCryogen && timer >= 14f && timer < 30f)
            {
                float progress = (timer - 14f) / 16f;
                float arenaX = TryGetArenaCenterX(Main.player[npc.target]);
                Vector2 lineStart = new Vector2(arenaX - 840f, npc.Center.Y);

                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, lineStart - screenPos, new Rectangle(0, 0, 1, 1), Color.Cyan * progress * 0.5f, 0f, new Vector2(0f, 0.5f), new Vector2(1680f, 3f), SpriteEffects.None, 0f);
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }

            // 3.12 Staff beam warning for Daedalus Golem Staff — tracks the player, then the beam rides the final line
            if (state == AttackState.DaedalusGolemStaff && timer >= 80f && timer < 400f && (timer % 120f) >= 80f && daedalusAimDir != Vector2.Zero)
            {
                float progress = ((timer % 120f) - 80f) / 40f;
                Color beamWarnColor = Color.Lerp(Color.DeepSkyBlue, Color.White, progress) * (0.25f + progress * 0.5f);

                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, npc.Center - screenPos, new Rectangle(0, 0, 1, 1), beamWarnColor, daedalusAimDir.ToRotation(), new Vector2(0f, 0.5f), new Vector2(900f, 3f), SpriteEffects.None, 0f);
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }

            // 4. Held weapons are standalone projectiles (BossHeldWeapon framework) and draw themselves

            // 5. Draw screen flash overlay (MagicPixel is white — BlackTile tinted white still renders black)
            if (transitionFlashAlpha > 0f)
            {
                Color flashColor = Color.White * transitionFlashAlpha;
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), flashColor);
            }

            return false;
        }

        // One Starnight aim-line: locked at lockTime, intensifies until its beam fires at fireTime, then vanishes.
        private static void DrawLanceAimLine(SpriteBatch spriteBatch, NPC npc, Vector2 screenPos, Vector2 dir, float timer, float lockTime, float fireTime)
        {
            if (dir == Vector2.Zero || timer < lockTime || timer >= fireTime)
                return;

            float progress = (timer - lockTime) / (fireTime - lockTime);
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GameUpdateCount * 0.45f);
            Color c = Color.Lerp(Color.Cyan, Color.White, progress) * (0.3f + 0.55f * progress) * pulse;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, npc.Center - screenPos, new Rectangle(0, 0, 1, 1), c, dir.ToRotation(), new Vector2(0f, 0.5f), new Vector2(1500f, MathHelper.Lerp(2f, 4f, progress)), SpriteEffects.None, 0f);
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            Vector2 drawPos = npc.Center - screenPos;
            Texture2D coreTex = TextureAssets.Npc[npc.type].Value;
            int currentSubphase = (int)npc.ai[0];

            // Pulsing core aura
            float pulseScale = 1.0f + 0.08f * (float)Math.Sin(ticksRunning * 0.12f);
            Color auraColor;

            if (currentSubphase < 4)
            {
                // P1: Light blue/cyan pulse
                auraColor = Color.Lerp(Color.DeepSkyBlue, Color.Cyan, 0.5f + 0.5f * (float)Math.Sin(ticksRunning * 0.08f)) * 0.45f * npc.Opacity;
            }
            else
            {
                // P2: Violet/magenta/dark blue pulse
                auraColor = Color.Lerp(Color.Purple, Color.DarkBlue, 0.5f + 0.5f * (float)Math.Sin(ticksRunning * 0.08f)) * 0.55f * npc.Opacity;
            }

            spriteBatch.Draw(
                coreTex,
                drawPos,
                npc.frame,
                auraColor,
                npc.rotation,
                npc.frame.Size() * 0.5f,
                npc.scale * pulseScale,
                SpriteEffects.None,
                0f
            );

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }

        private static Texture2D GetCalamityTexture(string path, Texture2D fallback)
        {
            if (ModContent.RequestIfExists<Texture2D>("CalamityMod/NPCs/Cryogen/" + path, out var asset))
            {
                return asset.Value;
            }
            return fallback;
        }
        #endregion
    }
}
