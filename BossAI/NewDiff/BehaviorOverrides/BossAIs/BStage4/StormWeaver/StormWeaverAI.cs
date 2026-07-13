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

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.StormWeaver
{
    // Head, Body and Tail route through this shared AI instance. Only the Head is ever a single entity here
    // (Storm Weaver has no phase-2 split, unlike Astrum Deus), so plain instance fields for rotation state
    // are safe. Body/Tail return true and keep running vanilla positioning — but NOT vanilla attacks, since
    // those depend on a Head-side localAI[0] counter that only increments inside vanilla Head.AI(), which we
    // never call; leaving that counter frozen means vanilla Body/Tail attacks simply never trigger.
    internal sealed class StormWeaverAI : IUMWBossAI
    {
        #region Constants & Configurations
        public override int NPCType => ModContent.Find<ModNPC>("CalamityMod/StormWeaverHead").Type;
        public override string BossName => "Storm Weaver";
        public override Color DebugColor => new(255, 120, 200);

        // Design doc specifies a single 80% HP unseal, not a 3-phase ladder.
        public override int MaxPhaseCount => 2;
        public override float[] PhaseLifeRatios => new[] { 0.80f };
        public override int AttackCycleLength => 120;
        public override float MotionIntensity => 1.25f;
        #endregion

        #region Attack States
        public enum AttackState
        {
            SkytideDragoon = 0,
            Storm = 1,
            Volterion = 2,
            AquasScepter = 3,
            CorinthPrime = 4,
            StellarTorus = 5,
            TeslaStaff = 6,
            TwistingThunder = 7,
            Pack = 8,
            ShadowboltStaff = 9,
            Seadragon = 10,
            FourSeasons = 11,
            RealityRupture = 12,
            Transition = 13,
        }

        private static bool IsP1(AttackState s) => s == AttackState.SkytideDragoon || s == AttackState.Storm || s == AttackState.Volterion;

        // Only 3 named P1 weapons — half the 6-slot floor — so each gets two rotation slots.
        private static readonly AttackState[] P1Cycle =
        {
            AttackState.SkytideDragoon, AttackState.Storm, AttackState.Volterion,
            AttackState.SkytideDragoon, AttackState.Storm, AttackState.Volterion,
        };
        private static readonly AttackState[] P2Cycle =
        {
            AttackState.AquasScepter, AttackState.CorinthPrime, AttackState.StellarTorus, AttackState.TeslaStaff,
            AttackState.TwistingThunder, AttackState.Pack, AttackState.ShadowboltStaff, AttackState.Seadragon,
            AttackState.FourSeasons, AttackState.RealityRupture,
        };
        #endregion

        #region Fields
        private int ticksRunning = 0;
        private int currentRepetition = 0;
        private int attackCycleIndex = 0;

        private readonly bool[] attackVariant = new bool[14];
        private bool UseVariantB(AttackState state)
        {
            int i = (int)state;
            bool v = attackVariant[i];
            attackVariant[i] = !v;
            return v;
        }

        private int teslaHurtCooldown = 0;

        // 穿过式咬合 timer — private field, not npc.localAI, to avoid colliding with vanilla worm bookkeeping.
        private int carvePassTimer = 0;
        private float transitionFlashAlpha = 0f;
        #endregion

        #region Core AI Hooks
        public override bool PreAI(NPC npc, IUMWGlobalNPC data)
        {
            ticksRunning++;

            int headType = ModContent.Find<ModNPC>("CalamityMod/StormWeaverHead").Type;
            if (npc.type != headType)
                return true;

            if (!TryGetTarget(npc, out Player target))
            {
                npc.velocity.Y -= 0.5f;
                if (npc.timeLeft > 60) npc.timeLeft = 60;
                return false;
            }

            if (npc.ai[0] == 0f)
                SpawnWormChain(npc);

            AttackState state = (AttackState)(int)npc.ai[1];
            ref float timer = ref npc.ai[2];
            ref float tracker = ref npc.ai[3];

            float lifeRatio = npc.lifeMax <= 0 ? 1f : npc.life / (float)npc.lifeMax;
            if (IsP1(state) && lifeRatio <= PhaseLifeRatios[0] && state != AttackState.Transition)
            {
                state = AttackState.Transition;
                npc.ai[1] = (float)state;
                timer = 0;
                tracker = 0;
                npc.netUpdate = true;
            }

            UpdateWingDrain(target);
            UpdateTeslaLink(npc, target);
            if (teslaHurtCooldown > 0) teslaHurtCooldown--;

            if (state != AttackState.Transition)
            {
                float baseSpeed = IsP1(state) ? 14f : 22f;
                float speed = baseSpeed + (1f - lifeRatio) * 6f;
                float turnSpeed = 0.045f + (1f - lifeRatio) * 0.03f;

                // 风暴编织者的分寸感: 贴近后咬定直线掠过(34帧不转向), 平时以正弦蜿蜒编行 —
                // 它在风里织弧线, 不做像素级黏着. 掠过窗口就是玩家的侧移机会.
                if (carvePassTimer > 0)
                {
                    carvePassTimer--;
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.velocity.SafeNormalize(Vector2.UnitX) * speed, 0.05f);
                }
                else
                {
                    Vector2 pursueDir = SafeNormalize(target.Center - npc.Center, Vector2.Zero).RotatedBy((float)Math.Sin(Main.GameUpdateCount * 0.05f) * 0.3f);
                    npc.velocity = Vector2.Lerp(npc.velocity, pursueDir * speed, turnSpeed);
                    if (Vector2.Distance(npc.Center, target.Center) < 200f)
                        carvePassTimer = 34;
                }
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            }

            switch (state)
            {
                case AttackState.SkytideDragoon: ExecuteSkytideDragoon(npc, target, ref timer, ref tracker); break;
                case AttackState.Storm: ExecuteStorm(npc, target, ref timer, ref tracker); break;
                case AttackState.Volterion: ExecuteVolterion(npc, target, ref timer, ref tracker); break;
                case AttackState.AquasScepter: ExecuteAquasScepter(npc, target, ref timer, ref tracker); break;
                case AttackState.CorinthPrime: ExecuteCorinthPrime(npc, target, ref timer, ref tracker); break;
                case AttackState.StellarTorus: ExecuteStellarTorus(npc, target, ref timer, ref tracker); break;
                case AttackState.TeslaStaff: ExecuteTeslaStaff(npc, target, ref timer, ref tracker); break;
                case AttackState.TwistingThunder: ExecuteTwistingThunder(npc, target, ref timer, ref tracker); break;
                case AttackState.Pack: ExecutePack(npc, target, ref timer, ref tracker); break;
                case AttackState.ShadowboltStaff: ExecuteShadowboltStaff(npc, target, ref timer, ref tracker); break;
                case AttackState.Seadragon: ExecuteSeadragon(npc, target, ref timer, ref tracker); break;
                case AttackState.FourSeasons: ExecuteFourSeasons(npc, target, ref timer, ref tracker); break;
                case AttackState.RealityRupture: ExecuteRealityRupture(npc, target, ref timer, ref tracker); break;
                case AttackState.Transition: ExecuteTransition(npc, target, ref timer, ref tracker); break;
            }

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) => ApplyDefense(npc, ref modifiers);
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) => ApplyDefense(npc, ref modifiers);

        // Design doc: in P1 only the tail is exposed (head/body are ~immune); once the shell breaks off at
        // 80% HP the whole body becomes normally damageable.
        private void ApplyDefense(NPC npc, ref NPC.HitModifiers modifiers)
        {
            int headType = ModContent.Find<ModNPC>("CalamityMod/StormWeaverHead").Type;
            int tailType = ModContent.Find<ModNPC>("CalamityMod/StormWeaverTail").Type;

            NPC head = npc.type == headType ? npc : null;
            if (head == null)
            {
                int headIdx = (int)npc.ai[2];
                if (headIdx >= 0 && headIdx < Main.maxNPCs && Main.npc[headIdx].active && Main.npc[headIdx].type == headType)
                    head = Main.npc[headIdx];
            }
            if (head == null) return;

            if (head.ai[1] == (float)AttackState.Transition)
            {
                modifiers.FinalDamage *= 0f;
                return;
            }

            bool phase1 = IsP1((AttackState)(int)head.ai[1]);
            if (phase1 && npc.type != tailType)
                modifiers.FinalDamage *= 0.001f;
        }
        #endregion

        #region Worm Chain Spawn (replicates vanilla StormWeaverHead.AI()'s spawn loop, which our override skips)
        private void SpawnWormChain(NPC head)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int bodyType = ModContent.Find<ModNPC>("CalamityMod/StormWeaverBody").Type;
            int tailType = ModContent.Find<ModNPC>("CalamityMod/StormWeaverTail").Type;
            const int totalLength = 40;
            int previous = head.whoAmI;

            for (int segments = 0; segments < totalLength; segments++)
            {
                bool isTail = segments == totalLength - 1;
                int lol = NPC.NewNPC(head.GetSource_FromAI(), (int)head.position.X + head.width / 2, (int)head.position.Y + head.height / 2, isTail ? tailType : bodyType, head.whoAmI);
                if (lol < 0 || lol >= Main.maxNPCs)
                    continue;

                NPC seg = Main.npc[lol];
                seg.realLife = head.whoAmI;
                seg.ai[2] = head.whoAmI;
                seg.ai[1] = previous;
                Main.npc[previous].ai[0] = lol;
                seg.netUpdate = true;
                previous = lol;
            }

            head.netUpdate = true;
        }
        #endregion

        #region Helpers
        private void UpdateWingDrain(Player player)
        {
            if (player.velocity.Y == 0f)
                return;

            bool safe = false;
            int tileX = (int)(player.Center.X / 16f);
            int tileY = (int)(player.Center.Y / 16f);
            for (int y = tileY; y > tileY - 12; y--)
            {
                if (WorldGen.InWorld(tileX, y) && Main.tile[tileX, y].HasTile && (Main.tileSolid[Main.tile[tileX, y].TileType] || Main.tileSolidTop[Main.tile[tileX, y].TileType]))
                {
                    safe = true;
                    break;
                }
            }

            if (!safe && player.wingTime > 0f)
            {
                player.wingTime -= player.wingTimeMax * 0.0066f; // 40%/s drain
                Dust.NewDust(player.position, player.width, player.height, DustID.Electric, 0f, 0f, 100, default, 1f);
            }
        }

        private void UpdateTeslaLink(NPC npc, Player target)
        {
            int tailType = ModContent.Find<ModNPC>("CalamityMod/StormWeaverTail").Type;
            NPC tail = null;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == tailType && (int)Main.npc[i].ai[2] == npc.whoAmI)
                {
                    tail = Main.npc[i];
                    break;
                }
            }
            if (tail == null) return;

            Vector2 ab = tail.Center - npc.Center;
            Vector2 ac = target.Center - npc.Center;
            float abLen = ab.Length();
            if (abLen <= 0f) return;

            // The head-tail arc is a live wire — it must be SEEN. Electric motes crackle along its length,
            // denser near the player so the threat reads exactly where it matters.
            for (int i = 0; i < 3; i++)
            {
                float lerp = Main.rand.NextFloat();
                Vector2 onLine = Vector2.Lerp(npc.Center, tail.Center, lerp);
                Dust d = Dust.NewDustPerfect(onLine + Main.rand.NextVector2Circular(8f, 8f), DustID.Electric, Main.rand.NextVector2Circular(1.2f, 1.2f), 120, default, Main.rand.NextFloat(0.7f, 1.1f));
                d.noGravity = true;
            }

            float proj = Vector2.Dot(ac, ab) / abLen;
            proj = Math.Clamp(proj, 0f, abLen);
            Vector2 closest = npc.Center + SafeNormalize(ab, Vector2.Zero) * proj;
            float playerDist = Vector2.Distance(target.Center, closest);

            if (playerDist < 130f && Main.rand.NextBool(2))
            {
                // Proximity warning: the wire arcs toward whatever comes close
                Dust warn = Dust.NewDustPerfect(closest, DustID.Electric, SafeNormalize(target.Center - closest, Vector2.Zero) * Main.rand.NextFloat(1f, 3f), 100, default, 1.3f);
                warn.noGravity = true;
            }

            if (playerDist < 24f)
            {
                target.AddBuff(BuffID.Electrified, 120);
                target.velocity *= 0.92f;
                if (teslaHurtCooldown <= 0)
                {
                    teslaHurtCooldown = 30;
                    SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.5f }, closest);
                    target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 18, 0);
                }
            }
        }
        #endregion

        #region Attack Rotation
        private void RotateAttack(NPC npc, AttackState current)
        {
            if (IsP1(current))
            {
                currentRepetition++;
                if (currentRepetition < 3)
                {
                    npc.ai[2] = 0; npc.ai[3] = 0; npc.netUpdate = true;
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
            npc.ai[2] = 0; npc.ai[3] = 0; npc.netUpdate = true;
        }
        #endregion

        #region P1 Attacks
        // SKYTIDE DRAGOON — A: zig-zag dash, chain-detonating crystal beacons (documented).
        //                    B: single straight lance dash with one larger delayed burst.
        private void ExecuteSkytideDragoon(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.SkytideDragoon) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<WeaverHeldSkytideDragoon>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                npc.velocity = dir * 20f;
                if (!variantB)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 pos = npc.Center + dir * (i * 220f);
                        int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<SkytideBeaconProj>(), npc.damage / 3, 0f, Main.myPlayer, 20f + i * 10f, 8f);
                    }
                }
                else
                {
                    Vector2 pos = npc.Center + dir * 400f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<SkytideBeaconProj>(), npc.damage / 3, 0f, Main.myPlayer, 45f, 14f);
                }
                FindHeldWeapon<WeaverHeldSkytideDragoon>(npc)?.Pulse(16f);
            }

            if (timer >= 200)
                RotateAttack(npc, AttackState.SkytideDragoon);
        }

        // STORM — A: 4 nodes flash in sequence (documented). B: all 4 flash together after a shared delay.
        private void ExecuteStorm(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.Storm) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<WeaverHeldStorm>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 pos = target.Center + new Vector2(i * 160f - 240f, Main.rand.NextFloat(-150f, 150f));
                    float delay = variantB ? 55f : (i * 15f + 20f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<WeaverStormNodeProj>(), npc.damage / 3, 0f, Main.myPlayer, delay);
                }
                FindHeldWeapon<WeaverHeldStorm>(npc)?.Pulse(10f);
            }

            if (timer >= 190)
                RotateAttack(npc, AttackState.Storm);
        }

        // VOLTERION — A: single slow gravity ball, 12-way radial burst (documented). B: twin smaller balls.
        private void ExecuteVolterion(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.Volterion) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<WeaverHeldVolterion>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                if (!variantB)
                {
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 3f, ModContent.ProjectileType<VolterionSphereProj>(), npc.damage / 3, 0f, Main.myPlayer);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = 12f;
                }
                else
                {
                    foreach (float spread in new float[] { -0.25f, 0.25f })
                    {
                        int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir.RotatedBy(spread) * 3f, ModContent.ProjectileType<VolterionSphereProj>(), npc.damage / 3, 0f, Main.myPlayer);
                        if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = 8f;
                    }
                }
                FindHeldWeapon<WeaverHeldVolterion>(npc)?.Pulse(-10f);
            }

            if (timer >= 220)
                RotateAttack(npc, AttackState.Volterion);
        }
        #endregion

        #region P2 Attacks
        private void ExecuteAquasScepter(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<WeaverHeldAquasScepter>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer >= 30 && timer <= 130 && timer % 4 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy(Main.rand.NextFloat(-0.12f, 0.12f)) * 12f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<AquasSteamProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<WeaverHeldAquasScepter>(npc)?.Pulse(4f);
            }

            if (timer >= 160)
                RotateAttack(npc, AttackState.AquasScepter);
        }

        private void ExecuteCorinthPrime(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<WeaverHeldCorinthPrime>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy((i - 1) * 0.25f) * 11f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<CorinthNukeProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                FindHeldWeapon<WeaverHeldCorinthPrime>(npc)?.Pulse(10f);
            }

            if (timer >= 180)
                RotateAttack(npc, AttackState.CorinthPrime);
        }

        private void ExecuteStellarTorus(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<WeaverHeldStellarTorus>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<StellarTorusRingProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<WeaverHeldStellarTorus>(npc)?.Pulse(10f);
            }

            if (timer >= 200)
                RotateAttack(npc, AttackState.StellarTorus);
        }

        private void ExecuteTeslaStaff(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<WeaverHeldTeslaStaff>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer >= 30 && timer <= 140 && timer % 12 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.Zero) * 14f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<TeslaConductionProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<WeaverHeldTeslaStaff>(npc)?.Pulse(8f);
            }

            if (timer >= 200)
                RotateAttack(npc, AttackState.TeslaStaff);
        }

        private void ExecuteTwistingThunder(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<WeaverHeldTwistingThunder>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                foreach (float phase in new float[] { 0f, MathHelper.Pi })
                {
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 10f, ModContent.ProjectileType<TwistingHelixProj>(), npc.damage / 3, 0f, Main.myPlayer, dir.X, dir.Y);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[2] = phase;
                }
                FindHeldWeapon<WeaverHeldTwistingThunder>(npc)?.Pulse(10f);
            }

            if (timer >= 190)
                RotateAttack(npc, AttackState.TwistingThunder);
        }

        private void ExecutePack(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<WeaverHeldPack>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 12; i++)
                {
                    Vector2 vel = (i * MathHelper.TwoPi / 12f).ToRotationVector2() * 6f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<PackRocketProj>(), npc.damage / 4, 0f, Main.myPlayer);
                }
                FindHeldWeapon<WeaverHeldPack>(npc)?.Pulse(10f);
            }

            if (timer >= 190)
                RotateAttack(npc, AttackState.Pack);
        }

        private void ExecuteShadowboltStaff(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<WeaverHeldShadowboltStaff>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                for (int i = 0; i < 4; i++)
                {
                    Vector2 cloudPos = target.Center + new Vector2(i * 180f - 270f, -420f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), cloudPos, Vector2.Zero, ModContent.ProjectileType<WeaverDarkCloudProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
            }

            if (timer >= 200)
                RotateAttack(npc, AttackState.ShadowboltStaff);
        }

        private void ExecuteSeadragon(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<WeaverHeldSeadragon>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float side = Math.Sign(npc.Center.X - target.Center.X);
                if (side == 0f) side = 1f;
                Vector2 spawn = target.Center + new Vector2(side * 900f, 0f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(-side * 13f, 0f), ModContent.ProjectileType<SeadragonWallProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<WeaverHeldSeadragon>(npc)?.Pulse(10f);
            }

            if (timer >= 160)
                RotateAttack(npc, AttackState.Seadragon);
        }

        private void ExecuteFourSeasons(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<WeaverHeldFourSeasons>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 20 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * MathHelper.PiOver2;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + angle.ToRotationVector2() * 240f, Vector2.Zero, ModContent.ProjectileType<SeasonStarProj>(), npc.damage / 3, 0f, Main.myPlayer, i);
                }
            }

            if (timer >= 190)
                RotateAttack(npc, AttackState.FourSeasons);
        }

        private void ExecuteRealityRupture(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<WeaverHeldRealityRupture>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                foreach (float side in new float[] { -1f, 1f })
                {
                    Vector2 spawn = target.Center + new Vector2(side * 900f, 0f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<WeaverRiftProj>(), npc.damage / 3, 0f, Main.myPlayer, -side * 6f);
                }
                FindHeldWeapon<WeaverHeldRealityRupture>(npc)?.Pulse(10f);
            }

            if (timer >= 180)
                RotateAttack(npc, AttackState.RealityRupture);
        }

        private void ExecuteTransition(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            npc.velocity *= 0.85f;
            npc.dontTakeDamage = true;
            transitionFlashAlpha = MathHelper.Clamp(1f - Math.Abs(timer - 22f) / 22f, 0f, 1f);

            if (timer == 1)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
                target.Calamity().GeneralScreenShakePower = 9f;
            }

            if (timer == 45)
                WeaverFx.Burst(npc.Center, 7f, 30);

            if (timer >= 90)
            {
                npc.dontTakeDamage = false;
                transitionFlashAlpha = 0f;
                attackCycleIndex = 0;
                npc.ai[1] = (float)P2Cycle[0];
                npc.ai[2] = 0;
                npc.ai[3] = 0;
                npc.netUpdate = true;
            }
        }
        #endregion

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            int headType = ModContent.Find<ModNPC>("CalamityMod/StormWeaverHead").Type;
            if (npc.type == headType && transitionFlashAlpha > 0f)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * transitionFlashAlpha);
            return true;
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
