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

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.AstrumDeus
{
    // Head, Body and Tail all route through this one shared AI instance (see LegendsBossAIRegistry). Body/Tail
    // return true from PreAI and keep running their REAL, unmodified vanilla follow-the-leader AI — including
    // the real spawn loop that is supposed to grow the worm's body. That loop lives inside vanilla Head.AI(),
    // which our override never calls (PreAI returns false for Head), so without replicating it manually the
    // worm would summon as a bodyless flying head forever. SpawnWormChain() below is that replication.
    //
    // Every Head-only counter lives on the HEAD NPC'S OWN ai[]/localAI[] — never as a class-level field —
    // because after the 50% split there are TWO Head entities sharing this one AI instance, and a class field
    // would let them stomp each other's rotation state. ai[0] is reserved for the vanilla "next segment"
    // chain pointer (never touch it after the initial spawn); ai[1]=AttackState, ai[2]=timer, ai[3]=variant
    // tracker. localAI[0]=P1 repetition count, localAI[1]=rotation cycle index, localAI[2]=cores destroyed,
    // localAI[3]=stun timer.
    internal sealed class AstrumDeusAI : LegendsBossAI
    {
        #region Constants & Configurations
        public override int NPCType => ModContent.Find<ModNPC>("CalamityMod/AstrumDeusHead").Type;
        public override string BossName => "Astrum Deus";
        public override Color DebugColor => new(160, 60, 220);

        public override int MaxPhaseCount => 2;
        public override float[] PhaseLifeRatios => new[] { 0.50f };
        public override int AttackCycleLength => 120;
        public override float MotionIntensity => 1.3f;

        // Dedicated sound identity — pitch-varied vanilla SoundIDs, matching Cryogen/OldDuke/Signus's
        // convention. This fight previously had zero SoundStyle fields and zero Pitch variance at all.
        private static readonly SoundStyle StarSputterSound = SoundID.Item9 with { Volume = 0.7f, Pitch = 0.2f };
        private static readonly SoundStyle StarSpawnBreakSound = SoundID.NPCDeath4 with { Volume = 0.85f, Pitch = -0.15f };
        #endregion

        #region Attack States
        public enum AttackState
        {
            MicrowaveBeam = 0,
            StarSputter = 1,
            StarShower = 2,
            StarspawnHelix = 3,
            RegulusRiot = 4,
            AstralPike = 5,
            AstralBlaster = 6,
            AstralStaff = 7,
            RadiantStar = 8,
            TrueBiome = 9,
            Transition = 10,
            DeathAnimation = 11,
        }

        private static bool IsP1(AttackState s) => s == AttackState.MicrowaveBeam || s == AttackState.StarSputter ||
            s == AttackState.StarShower || s == AttackState.StarspawnHelix || s == AttackState.RegulusRiot;

        // P1 has only 5 weapons — one short of the "at least 6 rotation slots" floor — so Microwave gets two
        // slots in the cycle; its UseVariantB toggle then naturally alternates A/B between those appearances
        // (same trick used for Astrum Aureus's Nebulash). P2 is the same story with RadiantStar.
        private static readonly AttackState[] P1Cycle =
        {
            AttackState.MicrowaveBeam, AttackState.StarSputter, AttackState.StarShower,
            AttackState.StarspawnHelix, AttackState.RegulusRiot, AttackState.MicrowaveBeam,
        };
        private static readonly AttackState[] P2Cycle =
        {
            AttackState.AstralPike, AttackState.AstralBlaster, AttackState.AstralStaff,
            AttackState.RadiantStar, AttackState.TrueBiome, AttackState.RadiantStar,
        };
        #endregion

        #region Fields
        private int ticksRunning = 0;

        // Per-attack variant toggle: flips deterministically each time that attack slot comes up (no RNG).
        // Safe as a class field even with two post-split Heads: each AttackState is only ever "in progress"
        // on one Head at a time in practice, and a harmless extra flip costs nothing but a swapped flavor.
        private readonly bool[] attackVariant = new bool[11];
        private bool UseVariantB(AttackState state)
        {
            int i = (int)state;
            bool v = attackVariant[i];
            attackVariant[i] = !v;
            return v;
        }

        // Stellar Segment Cores — keyed by the Body segment's own whoAmI, so it's correct no matter which
        // worm (pre- or post-split) that segment belongs to, without touching vanilla Body's own ai/localAI.
        private readonly Dictionary<int, float> segmentCoreHP = new();
        private const float CoreMaxHP = 250f;

        // 穿过式咬合 timer, per-head (Deus can split into two Heads; all four localAI slots are taken).
        private readonly Dictionary<int, int> carveTimers = new();

        // Motion afterimages, per-head (keyed by whoAmI like carveTimers above — two Heads can be alive
        // simultaneously post-split, each needs its own independent trail).
        private readonly Dictionary<int, Vector2[]> headTrails = new();
        private readonly Dictionary<int, int> headTrailIndex = new();

        private float transitionFlashAlpha = 0f;
        #endregion

        #region Core AI Hooks
        public override bool PreAI(NPC npc, LegendsGlobalNPC data)
        {
            ticksRunning++;

            int headType = ModContent.Find<ModNPC>("CalamityMod/AstrumDeusHead").Type;
            if (npc.type != headType)
                return true; // Body/Tail: fully vanilla positioning AND vanilla ambient attacks stay active

            if (!TryGetTarget(npc, out Player target))
            {
                npc.velocity.Y -= 0.5f;
                if (npc.timeLeft > 60) npc.timeLeft = 60;
                return false;
            }

            // First frame for THIS Head (fresh summon, or a freshly split worm) — grow its body.
            if (npc.ai[0] == 0f)
            {
                bool isSplit = npc.Calamity().newAI[0] != 0f;
                SpawnWormChain(npc, isSplit ? 26 : 51);
            }

            AttackState state = (AttackState)(int)npc.ai[1];
            ref float timer = ref npc.ai[2];
            ref float tracker = ref npc.ai[3];

            // Single real transition at 50% HP (per-Head life ratio — only the original P1 worm ever crosses
            // this, since post-split worms start fresh already in P2).
            float lifeRatio = npc.lifeMax <= 0 ? 1f : npc.life / (float)npc.lifeMax;
            if (IsP1(state) && lifeRatio <= PhaseLifeRatios[0] && state != AttackState.Transition)
            {
                state = AttackState.Transition;
                npc.ai[1] = (float)state;
                timer = 0;
                tracker = 0;
                npc.netUpdate = true;
            }

            // Stun from stellar-core overload — the god-worm sheds star-motes while dazed
            if (npc.localAI[3] > 0f)
            {
                npc.localAI[3]--;
                npc.velocity *= 0.85f;
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(50f, 50f), DustID.PurpleTorch, new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(1f, 2.5f)), 100, default, 1.2f);
                    d.noGravity = true;
                }
                return false;
            }

            if (state != AttackState.DeathAnimation)
                UpdateConstellationLink(npc, target);

            if (state != AttackState.Transition && state != AttackState.DeathAnimation)
            {
                float baseSpeed = IsP1(state) ? 14f : 20f;
                float speed = baseSpeed + (1f - lifeRatio) * 6f;
                float turnSpeed = 0.05f + (1f - lifeRatio) * 0.03f;

                // 星神游龙的分寸感: 贴近后咬定直线掠过(34帧不转向), 平时以正弦蜿蜒追踪 — 神龙划过星空,
                // 不做像素级黏着. carve窗口就是玩家的侧移机会. (localAI四槽全被占用, 用whoAmI字典per-head)
                int carve = carveTimers.TryGetValue(npc.whoAmI, out int c) ? c : 0;
                if (carve > 0)
                {
                    carveTimers[npc.whoAmI] = carve - 1;
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.velocity.SafeNormalize(Vector2.UnitX) * speed, 0.05f);
                }
                else
                {
                    Vector2 pursueDir = SafeNormalize(target.Center - npc.Center, Vector2.Zero).RotatedBy((float)Math.Sin(Main.GameUpdateCount * 0.05f) * 0.3f);
                    npc.velocity = Vector2.Lerp(npc.velocity, pursueDir * speed, turnSpeed);
                    if (Vector2.Distance(npc.Center, target.Center) < 200f)
                        carveTimers[npc.whoAmI] = 34;
                }
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            }

            switch (state)
            {
                case AttackState.MicrowaveBeam:
                    ExecuteMicrowaveBeam(npc, target, ref timer, ref tracker);
                    break;
                case AttackState.StarSputter:
                    ExecuteStarSputter(npc, target, ref timer, ref tracker);
                    break;
                case AttackState.StarShower:
                    ExecuteStarShower(npc, target, ref timer, ref tracker);
                    break;
                case AttackState.StarspawnHelix:
                    ExecuteStarspawnHelix(npc, target, ref timer, ref tracker);
                    break;
                case AttackState.RegulusRiot:
                    ExecuteRegulusRiot(npc, target, ref timer, ref tracker);
                    break;
                case AttackState.AstralPike:
                    ExecuteAstralPike(npc, target, ref timer, ref tracker);
                    break;
                case AttackState.AstralBlaster:
                    ExecuteAstralBlaster(npc, target, ref timer, ref tracker);
                    break;
                case AttackState.AstralStaff:
                    ExecuteAstralStaff(npc, target, ref timer, ref tracker);
                    break;
                case AttackState.RadiantStar:
                    ExecuteRadiantStar(npc, target, ref timer, ref tracker);
                    break;
                case AttackState.TrueBiome:
                    ExecuteTrueBiome(npc, target, ref timer, ref tracker);
                    break;
                case AttackState.Transition:
                    ExecuteTransition(npc, target, ref timer, ref tracker);
                    break;
                case AttackState.DeathAnimation:
                    ExecuteDeathAnimation(npc, target, ref timer);
                    break;
            }

            if (!headTrails.TryGetValue(npc.whoAmI, out Vector2[] trail))
            {
                trail = new Vector2[9];
                headTrails[npc.whoAmI] = trail;
                headTrailIndex[npc.whoAmI] = 0;
            }
            int trailIdx = headTrailIndex[npc.whoAmI];
            trail[trailIdx] = npc.Center;
            headTrailIndex[npc.whoAmI] = (trailIdx + 1) % trail.Length;

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            ProcessCoreHit(npc, item.damage, ref modifiers);
            if (npc.type == ModContent.Find<ModNPC>("CalamityMod/AstrumDeusHead").Type)
                InterceptLethalHit(npc, ref modifiers, (int)AttackState.DeathAnimation, () => BeginDeathAnimation(npc, player));
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            ProcessCoreHit(npc, projectile.damage, ref modifiers);
            if (npc.type == ModContent.Find<ModNPC>("CalamityMod/AstrumDeusHead").Type)
                InterceptLethalHit(npc, ref modifiers, (int)AttackState.DeathAnimation, () => BeginDeathAnimation(npc, Main.player[projectile.owner]));
        }

        // Design doc: Head/Tail take 100% damage. Body segments carry a 250 HP stellar core each — intact,
        // that segment has 95% DR; once the core is broken, DR is gone AND the segment takes 200% damage.
        // 10 broken cores (across this worm) staggers the whole chain for 360 frames.
        private void ProcessCoreHit(NPC npc, int damage, ref NPC.HitModifiers modifiers)
        {
            int bodyType = ModContent.Find<ModNPC>("CalamityMod/AstrumDeusBody").Type;
            if (npc.type != bodyType)
                return;

            if (!segmentCoreHP.TryGetValue(npc.whoAmI, out float hp))
                hp = CoreMaxHP;

            if (hp <= 0f)
            {
                modifiers.FinalDamage *= 2f; // core broken — 200% damage taken
                return;
            }

            modifiers.FinalDamage *= 0.05f; // core intact — 95% DR
            hp -= damage;
            segmentCoreHP[npc.whoAmI] = hp;

            if (hp <= 0f)
            {
                SoundEngine.PlaySound(StarSpawnBreakSound, npc.Center);
                DeusFx.Burst(npc.Center, 5f, 14, DustID.PurpleTorch);

                int headIdx = (int)npc.ai[2];
                if (headIdx >= 0 && headIdx < Main.maxNPCs && Main.npc[headIdx].active)
                {
                    Main.npc[headIdx].localAI[2]++;
                    if (Main.npc[headIdx].localAI[2] >= 10f)
                    {
                        Main.npc[headIdx].localAI[3] = 360f;
                        Main.npc[headIdx].velocity = Vector2.Zero;
                        SoundEngine.PlaySound(SoundID.NPCHit53, Main.npc[headIdx].Center);
                        DeusFx.Burst(Main.npc[headIdx].Center, 7f, 30);
                    }
                }
            }
        }
        #endregion

        #region Worm Chain Spawn (replicates vanilla AstrumDeusHead.AI()'s spawn loop, which our override skips)
        private void SpawnWormChain(NPC head, int maxLength)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int bodyType = ModContent.Find<ModNPC>("CalamityMod/AstrumDeusBody").Type;
            int tailType = ModContent.Find<ModNPC>("CalamityMod/AstrumDeusTail").Type;
            int previous = head.whoAmI;

            for (int segments = 0; segments < maxLength; segments++)
            {
                bool isTail = segments == maxLength - 1;
                Vector2 dir = head.velocity.SafeNormalize(Vector2.UnitY);
                Vector2 offset = isTail ? dir * head.height * maxLength / 2f : dir * head.height * (segments + 1) / 2f;
                Vector2 spawnPos = head.Center - offset;

                int lol = NPC.NewNPC(head.GetSource_FromAI(), (int)spawnPos.X, (int)spawnPos.Y, isTail ? tailType : bodyType, head.whoAmI);
                if (lol < 0 || lol >= Main.maxNPCs)
                    continue;

                NPC seg = Main.npc[lol];
                seg.realLife = head.whoAmI;
                seg.ai[3] = segments + 1;
                seg.ai[2] = head.whoAmI;
                seg.ai[1] = previous;
                seg.Calamity().newAI[0] = head.Calamity().newAI[0];
                Main.npc[previous].ai[0] = lol;
                seg.netUpdate = true;
                previous = lol;
            }

            head.netUpdate = true;
        }
        #endregion

        #region Constellation Link
        private void UpdateConstellationLink(NPC head, Player target)
        {
            int tailType = ModContent.Find<ModNPC>("CalamityMod/AstrumDeusTail").Type;
            NPC tail = null;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == tailType && (int)Main.npc[i].ai[2] == head.whoAmI)
                {
                    tail = Main.npc[i];
                    break;
                }
            }
            if (tail == null)
                return;

            int cycle = (int)(ticksRunning % 360); // 6s
            bool overloaded = cycle >= 300 && cycle < 336; // flicker 1s, solid 1.2s (300-336 covers the beat)
            if (cycle == 300)
                SoundEngine.PlaySound(SoundID.Item60, head.Center);

            if (overloaded && cycle >= 318)
            {
                float collisionPoint = 0f;
                if (Collision.CheckAABBvLineCollision(target.position, target.Size, head.Center, tail.Center, 25f, ref collisionPoint))
                {
                    target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(head.whoAmI), 20, 0);
                    target.AddBuff(BuffID.Frostburn2, 120);
                }
            }
        }
        #endregion

        #region Attack Rotation
        private void RotateAttack(NPC npc, AttackState current)
        {
            if (IsP1(current))
            {
                npc.localAI[0]++;
                if (npc.localAI[0] < 3f)
                {
                    npc.ai[2] = 0;
                    npc.ai[3] = 0;
                    npc.netUpdate = true;
                    return;
                }
                npc.localAI[0] = 0;
                npc.localAI[1]++;
                npc.ai[1] = (float)P1Cycle[(int)npc.localAI[1] % P1Cycle.Length];
            }
            else
            {
                npc.localAI[1]++;
                npc.ai[1] = (float)P2Cycle[(int)npc.localAI[1] % P2Cycle.Length];
            }
            npc.ai[2] = 0;
            npc.ai[3] = 0;
            npc.netUpdate = true;
        }
        #endregion

        #region P1 Attacks
        // MICROWAVE — A: 30-degree sweeping beam, heavy knockback (documented). B: narrow straight beam, real damage.
        private void ExecuteMicrowaveBeam(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.MicrowaveBeam) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<DeusHeldMicrowave>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = variantB ? npc.damage / 2 : npc.damage / 6;
                float baseAngle = (target.Center - npc.Center).ToRotation();
                int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<MicrowaveBeamProj>(), dmg, 0f, Main.myPlayer, npc.whoAmI, baseAngle);
                if (idx >= 0 && idx < Main.maxProjectiles)
                    Main.projectile[idx].ai[2] = variantB ? 0f : 1f;
            }

            if (timer >= 110)
                RotateAttack(npc, AttackState.MicrowaveBeam);
        }

        // STAR SPUTTER — A: outward then straight reel-back (documented). B: outward then spiral inward.
        private void ExecuteStarSputter(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.StarSputter) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<DeusHeldStarSputter>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(StarSputterSound, npc.Center);
                int dmg = npc.damage / 3;
                int bodyType = ModContent.Find<ModNPC>("CalamityMod/AstrumDeusBody").Type;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC seg = Main.npc[i];
                    if (seg.active && seg.type == bodyType && (int)seg.ai[2] == npc.whoAmI && Main.rand.NextBool(4))
                    {
                        Vector2 dir = (seg.Center - npc.Center).SafeNormalize(Vector2.UnitY).RotatedBy(Main.rand.NextFloat(-0.3f, 0.3f));
                        int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), seg.Center, dir * 6f, ModContent.ProjectileType<StarSputterProj>(), dmg, 0f, Main.myPlayer);
                        if (idx >= 0 && idx < Main.maxProjectiles)
                            Main.projectile[idx].ai[2] = variantB ? 1f : 0f;
                    }
                }
            }

            if (timer >= 180)
                RotateAttack(npc, AttackState.StarSputter);
        }

        // STAR SHOWER — A: odd-then-even lane order (documented). B: even-then-odd, faster fall.
        private void ExecuteStarShower(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.StarShower) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<DeusHeldStarShower>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            if ((timer == 40 || timer == 90) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                bool firstWave = timer == 40;
                bool oddLanes = variantB ? !firstWave : firstWave;
                for (int lane = 0; lane < 6; lane++)
                {
                    bool isOdd = lane % 2 == 0;
                    if (isOdd != oddLanes) continue;
                    Vector2 spawn = target.Center + new Vector2(lane * 180f - 450f, -700f);
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(0f, variantB ? 5f : 3f), ModContent.ProjectileType<ColumnStarProj>(), dmg, 0f, Main.myPlayer);
                    if (idx >= 0 && idx < Main.maxProjectiles)
                        Main.projectile[idx].ai[0] = variantB ? 14f : 10f;
                }
            }

            if (timer >= 200)
                RotateAttack(npc, AttackState.StarShower);
        }

        // STARSPAWN HELIX — A: twin strands weave a double helix (documented). B: single wider-orbit strand.
        private void ExecuteStarspawnHelix(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.StarspawnHelix) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<DeusHeldStarspawnHelix>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (!variantB)
                {
                    foreach (float phase in new float[] { 0f, MathHelper.Pi })
                    {
                        int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<StarspawnHelixProj>(), npc.damage / 4, 0f, Main.myPlayer, npc.whoAmI, phase);
                        if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[2] = 130f;
                    }
                }
                else
                {
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<StarspawnHelixProj>(), npc.damage / 4, 0f, Main.myPlayer, npc.whoAmI, 0f);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[2] = 220f;
                }
            }

            if (timer >= 220)
                RotateAttack(npc, AttackState.StarspawnHelix);
        }

        // REGULUS RIOT — A: sequential launch+pause+burst (documented). B: simultaneous ring, synced burst.
        private void ExecuteRegulusRiot(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.RegulusRiot) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<DeusHeldRegulusRiot>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 vel = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 6f;
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<RegulusCoreProj>(), dmg, 0f, Main.myPlayer);
                    if (idx >= 0 && idx < Main.maxProjectiles)
                        Main.projectile[idx].ai[0] = variantB ? 60f : 40f + i * 6f;
                }
            }

            if (timer >= 200)
                RotateAttack(npc, AttackState.RegulusRiot);
        }
        #endregion

        #region P2 Attacks
        private void ExecuteAstralPike(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<DeusHeldAstralPike>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 20 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 20f, ModContent.ProjectileType<AstralPikeProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<DeusHeldAstralPike>(npc)?.Pulse(16f);
            }

            if (timer >= 160)
                RotateAttack(npc, AttackState.AstralPike);
        }

        private void ExecuteAstralBlaster(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<DeusHeldAstralBlaster>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer >= 30 && timer <= 140 && timer % 8 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 13f, ModContent.ProjectileType<AstralRoundProj>(), npc.damage / 3, 0f, Main.myPlayer);
                if (idx >= 0 && idx < Main.maxProjectiles)
                {
                    Main.projectile[idx].ai[0] = 900f;
                    Main.projectile[idx].ai[1] = target.Center.X;
                }
                FindHeldWeapon<DeusHeldAstralBlaster>(npc)?.Pulse(6f);
            }

            if (timer >= 200)
                RotateAttack(npc, AttackState.AstralBlaster);
        }

        private void ExecuteAstralStaff(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<DeusHeldAstralStaff>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer >= 30 && timer <= 150 && timer % 20 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawn = target.Center + new Vector2(Main.rand.NextFloat(-300f, 300f), -500f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(0f, 4f), ModContent.ProjectileType<AstralCrystalProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<DeusHeldAstralStaff>(npc)?.Pulse(8f);
            }

            if (timer >= 220)
                RotateAttack(npc, AttackState.AstralStaff);
        }

        // RADIANT STAR — A: 6 knives orbit-constrict then release tangentially (documented). B: pulsing ring.
        private void ExecuteRadiantStar(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.RadiantStar) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<DeusHeldRadiantStar>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 6; i++)
                {
                    float ang = i * MathHelper.TwoPi / 6f;
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<RadiantStarKnifeProj>(), npc.damage / 3, 0f, Main.myPlayer, npc.whoAmI, ang);
                    if (idx >= 0 && idx < Main.maxProjectiles)
                        Main.projectile[idx].ai[2] = variantB ? 1f : 0f;
                }
                FindHeldWeapon<DeusHeldRadiantStar>(npc)?.Pulse(10f);
            }

            if (timer >= 200)
                RotateAttack(npc, AttackState.RadiantStar);
        }

        private void ExecuteTrueBiome(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<DeusHeldTrueBiomeBlade>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 20 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float rot = Main.rand.NextFloat(MathHelper.TwoPi);
                Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<TrueBiomeRiftProj>(), npc.damage / 3, 0f, Main.myPlayer, rot);
            }

            if (timer >= 180)
                RotateAttack(npc, AttackState.TrueBiome);
        }

        private void ExecuteTransition(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            npc.velocity *= 0.9f;
            npc.dontTakeDamage = true;

            transitionFlashAlpha = MathHelper.Clamp(1f - Math.Abs(timer - 22f) / 22f, 0f, 1f);

            if (timer == 1)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
                target.Calamity().GeneralScreenShakePower = 10f;
            }

            if (timer == 45 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int bodyType = ModContent.Find<ModNPC>("CalamityMod/AstrumDeusBody").Type;
                int tailType = ModContent.Find<ModNPC>("CalamityMod/AstrumDeusTail").Type;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC seg = Main.npc[i];
                    if (seg.active && (seg.type == bodyType || seg.type == tailType) && (int)seg.ai[2] == npc.whoAmI)
                        seg.active = false;
                }

                DeusFx.Burst(npc.Center, 8f, 40);
                for (int w = 0; w < 2; w++)
                {
                    int newHead = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X + (w == 0 ? -60 : 60), (int)npc.Center.Y, ModContent.Find<ModNPC>("CalamityMod/AstrumDeusHead").Type);
                    if (newHead >= 0 && newHead < Main.maxNPCs)
                    {
                        NPC h = Main.npc[newHead];
                        h.Calamity().newAI[0] = w + 1;
                        h.ai[1] = (float)P2Cycle[0];
                        h.velocity = (w == 0 ? new Vector2(-8f, -6f) : new Vector2(8f, -6f));
                        h.netUpdate = true;
                    }
                }

                npc.life = 0;
                npc.active = false;
            }
        }
        #endregion

        #region Death Animation
        // 星神殒落 — 五段演出, 把星芒/群星/星座链身份用在自己身上, 而不是通用爆炸:
        // 星核震颤 -> 星芒溃散(甩尾) -> 群星回收(呼应星座链) -> 超新星凝聚上腾 -> 终末超新星爆发.
        // Only ever called on a Head (ModifyHitByItem/Projectile gate it), so ai[]/localAI[] here are always
        // that specific worm's own — safe even with two Heads alive at once post-split.
        private void BeginDeathAnimation(NPC npc, Player target)
        {
            npc.ai[1] = (float)AttackState.DeathAnimation;
            npc.ai[2] = 0f;
            npc.ai[3] = 0f;
            npc.localAI[3] = 0f;
            carveTimers[npc.whoAmI] = 0;
            npc.netUpdate = true;

            TriggerDeathCinematic(npc, target, focusStrength: 0.55f, holdFrames: 55, shakePower: 10f);
            SoundEngine.PlaySound(SoundID.Roar with { Volume = 1f, Pitch = 0.1f }, npc.Center);
        }

        private void ExecuteDeathAnimation(NPC npc, Player target, ref float timer)
        {
            npc.damage = 0;
            npc.dontTakeDamage = true;

            if (timer < 30f)
            {
                // 星核震颤 — cosmic dust jitters loose from the whole segmented body
                npc.velocity *= 0.9f;
                npc.rotation += MathF.Sin(timer * 1.3f) * 0.1f;
                if ((int)timer % 3 == 0)
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(50f, 50f), DustID.PurpleTorch, Main.rand.NextVector2Circular(4f, 4f), 100, default, 1.3f);
                    d.noGravity = true;
                }
            }
            else if (timer < 75f)
            {
                // 星芒溃散 — whip-thrash, the same serpentine lash as the fight's own carve-pass motif gone loose
                float t = timer - 30f;
                float whipAngle = MathF.Sin(t * 0.35f) * 2.2f;
                Vector2 whipDir = Vector2.UnitX.RotatedBy(whipAngle);
                npc.velocity = Vector2.Lerp(npc.velocity, whipDir * 17f, 0.2f);
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                if ((int)t % 4 == 0)
                    DeusFx.Burst(npc.Center, 3f, 6, DustID.PurpleTorch);
            }
            else if (timer < 110f)
            {
                // 群星回收 — the constellation-link identity turns inward: stray star-motes get pulled back into the head
                float t = timer - 75f;
                if ((int)t % 3 == 0)
                {
                    Vector2 spawn = npc.Center + Main.rand.NextVector2CircularEdge(200f, 200f);
                    Dust d = Dust.NewDustPerfect(spawn, DustID.GoldFlame, (npc.Center - spawn) * 0.07f, 100, default, 1.3f);
                    d.noGravity = true;
                }
            }
            else if (timer < 150f)
            {
                // 超新星凝聚上腾 — rises while a supernova core builds, the cinematic pull peaks here
                npc.velocity = Vector2.Lerp(npc.velocity, new Vector2(0f, -7f), 0.05f);
                float t = timer - 110f;
                float ringRadius = MathHelper.Lerp(10f, 90f, t / 40f);
                if ((int)t % 2 == 0)
                {
                    Vector2 spawn = npc.Center + (t * 0.5f).ToRotationVector2() * ringRadius;
                    Dust d = Dust.NewDustPerfect(spawn, DustID.PurpleTorch, (npc.Center - spawn) * 0.05f, 100, default, 1.4f);
                    d.noGravity = true;
                }
            }
            else
            {
                // 终末超新星爆发 — the actual kill fires once, everything after is the lingering burst
                if (timer == 150f)
                {
                    npc.velocity = Vector2.Zero;
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1.1f, Pitch = 0f }, npc.Center);
                    SoundEngine.PlaySound(SoundID.NPCDeath4, npc.Center);
                    target.Calamity().GeneralScreenShakePower = 13f;
                    DeusFx.Burst(npc.Center, 8f, 40);
                    DeusFx.Burst(npc.Center, 5f, 24, DustID.GoldFlame);
                }

                if (timer >= 172f)
                {
                    // The head is what StrikeInstantKill removes — its trailing Body/Tail segments are separate
                    // NPCs with no self-cleanup, so they'd otherwise be left drifting headless (same reason the
                    // 50%-split transition above manually deactivates the old chain).
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int bodyType = ModContent.Find<ModNPC>("CalamityMod/AstrumDeusBody").Type;
                        int tailType = ModContent.Find<ModNPC>("CalamityMod/AstrumDeusTail").Type;
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC seg = Main.npc[i];
                            if (seg.active && (seg.type == bodyType || seg.type == tailType) && (int)seg.ai[2] == npc.whoAmI)
                                seg.active = false;
                        }
                    }

                    npc.dontTakeDamage = false;
                    npc.StrikeInstantKill();
                }
            }
        }
        #endregion

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Motion afterimages, per-head (see headTrails field comment) — sells the carve-pass speed the
            // same way every other worm boss in this roster (Cryogen/StormWeaver/AquaticScourge) already does.
            if (npc.type == ModContent.Find<ModNPC>("CalamityMod/AstrumDeusHead").Type && npc.velocity.Length() > 10f
                && headTrails.TryGetValue(npc.whoAmI, out Vector2[] trail) && headTrailIndex.TryGetValue(npc.whoAmI, out int trailIdx))
            {
                Texture2D tex = TextureAssets.Npc[npc.type].Value;
                Vector2 origin = npc.frame.Size() * 0.5f;
                for (int i = 1; i < trail.Length; i++)
                {
                    int idx = (trailIdx - i + trail.Length * 2) % trail.Length;
                    if (trail[idx] == Vector2.Zero) continue;
                    float fade = (1f - i / (float)trail.Length) * 0.35f * npc.Opacity;
                    Color ghost = new Color(160, 60, 220, 0) * fade;
                    spriteBatch.Draw(tex, trail[idx] - screenPos, npc.frame, ghost, npc.rotation, origin, npc.scale * (1f - i * 0.02f), SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            int headType = ModContent.Find<ModNPC>("CalamityMod/AstrumDeusHead").Type;
            if (npc.type != headType)
                return;

            if (transitionFlashAlpha > 0f)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * transitionFlashAlpha);

            int tailType = ModContent.Find<ModNPC>("CalamityMod/AstrumDeusTail").Type;
            NPC tail = null;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == tailType && (int)Main.npc[i].ai[2] == npc.whoAmI)
                {
                    tail = Main.npc[i];
                    break;
                }
            }
            if (tail == null)
                return;

            int cycle = (int)(ticksRunning % 360);
            bool visible = cycle >= 300;
            if (!visible)
                return;

            bool solid = cycle >= 318;
            float width = solid ? 50f : MathHelper.Lerp(2f, 10f, (cycle - 300) / 18f);
            Color core = solid ? new Color(220, 140, 255) : new Color(160, 60, 220);
            LegendsWeaponBossVisuals.DrawLine(spriteBatch, npc.Center, tail.Center, core * 0.8f, width);
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
