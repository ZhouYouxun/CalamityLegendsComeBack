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

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage2.AquaticScourge
{
    // Head, Body/BodyAlt and Tail all route through this shared AI instance (registered in
    // IUMWBossAIRegistry). Only the Head runs the attack state machine; other segments return true
    // immediately (pure vanilla chain positioning) but still receive ModifyHitBy* so hits on the
    // specific pustule-carrying segments can be detected. Our PreAI override skips vanilla
    // Head.AI() entirely, which is where the real body-spawn loop lives — SpawnWormChain() replicates
    // it manually, matching the fix already applied to Astrum Deus and Storm Weaver.
    internal sealed class AquaticScourgeAI : IUMWBossAI
    {
        #region Constants & Configurations
        public override int NPCType => ModContent.Find<ModNPC>("CalamityMod/AquaticScourgeHead").Type;
        public override string BossName => "Aquatic Scourge";
        public override Color DebugColor => new(120, 220, 200);

        // Design doc specifies a single 50% HP transition, not the old 3-phase ladder.
        public override int MaxPhaseCount => 2;
        public override float[] PhaseLifeRatios => new[] { 0.50f };
        public override int AttackCycleLength => 120;
        public override float MotionIntensity => 1.2f;
        #endregion

        #region Attack States
        public enum AttackState
        {
            SubmarineShocker = 0,
            Barinautical = 1,
            Downpour = 2,
            DeepseaStaff = 3,
            ScourgeSeas = 4,

            FlakToxicannon = 5,
            SlitheringEels = 6,
            CausticCroaker = 7,
            SkyfinBombers = 8,
            SpentFuel = 9,
            SulphurousGrabber = 10,

            Transition = 11,
        }

        private static bool IsP1(AttackState s) =>
            s == AttackState.SubmarineShocker || s == AttackState.Barinautical || s == AttackState.Downpour ||
            s == AttackState.DeepseaStaff || s == AttackState.ScourgeSeas;

        // Only 5 named P1 weapons — one short of the 6-slot floor — so SubmarineShocker gets a second slot.
        private static readonly AttackState[] P1Cycle =
        {
            AttackState.SubmarineShocker, AttackState.Barinautical, AttackState.Downpour,
            AttackState.DeepseaStaff, AttackState.ScourgeSeas, AttackState.SubmarineShocker,
        };
        private static readonly AttackState[] P2Cycle =
        {
            AttackState.FlakToxicannon, AttackState.SlitheringEels, AttackState.CausticCroaker,
            AttackState.SkyfinBombers, AttackState.SpentFuel, AttackState.SulphurousGrabber,
        };
        #endregion

        #region Fields
        private int currentRepetition = 0;
        private int attackCycleIndex = 0;

        private readonly bool[] attackVariant = new bool[12];
        private bool UseVariantB(AttackState state)
        {
            int i = (int)state;
            bool v = attackVariant[i];
            attackVariant[i] = !v;
            return v;
        }

        // Pustules: 6 designated body segments, tracked by their own whoAmI (assigned once at chain spawn,
        // stable for the whole fight since Aquatic Scourge segments never die individually).
        private readonly int[] pustuleSegment = { -1, -1, -1, -1, -1, -1 };
        private readonly float[] pustuleHPs = new float[6];
        private int pustuleStunTimer = 0;
        private int pustuleRespawnTimer = 0;

        private int tideTimer = 0;
        #endregion

        #region Core AI Hooks
        public override bool PreAI(NPC npc, IUMWGlobalNPC data)
        {
            int headType = ModContent.Find<ModNPC>("CalamityMod/AquaticScourgeHead").Type;
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
                npc.dontTakeDamage = true;
                npc.netUpdate = true;
            }

            UpdatePustuleRespawn();
            UpdateSulphurTide(npc, target);

            int destroyedPustules = 0;
            for (int i = 0; i < 6; i++) if (pustuleHPs[i] <= 0f) destroyedPustules++;

            if (pustuleStunTimer > 0)
            {
                pustuleStunTimer--;
                npc.velocity *= 0.85f;
            }
            else if (state != AttackState.Transition)
            {
                float speedPenalty = 1f - 0.15f * destroyedPustules;
                float baseSpeed = IsP1(state) ? 13f : 19f;
                float speed = (baseSpeed + (1f - lifeRatio) * 5f) * speedPenalty;
                float turnSpeed = (0.045f + (1f - lifeRatio) * 0.02f) * speedPenalty;
                Vector2 desiredVel = SafeNormalize(target.Center - npc.Center, Vector2.Zero) * speed;
                npc.velocity = Vector2.Lerp(npc.velocity, desiredVel, turnSpeed);
            }
            npc.rotation = npc.velocity.SafeNormalize(Vector2.UnitY).ToRotation() + MathHelper.PiOver2;

            if (pustuleStunTimer == 0)
            {
                switch (state)
                {
                    case AttackState.SubmarineShocker: ExecuteSubmarineShocker(npc, target, ref timer, ref tracker); break;
                    case AttackState.Barinautical: ExecuteBarinautical(npc, target, ref timer, ref tracker); break;
                    case AttackState.Downpour: ExecuteDownpour(npc, target, ref timer, ref tracker); break;
                    case AttackState.DeepseaStaff: ExecuteDeepseaStaff(npc, target, ref timer, ref tracker); break;
                    case AttackState.ScourgeSeas: ExecuteScourgeSeas(npc, target, ref timer, ref tracker); break;
                    case AttackState.FlakToxicannon: ExecuteFlakToxicannon(npc, target, ref timer, ref tracker); break;
                    case AttackState.SlitheringEels: ExecuteSlitheringEels(npc, target, ref timer, ref tracker); break;
                    case AttackState.CausticCroaker: ExecuteCausticCroaker(npc, target, ref timer, ref tracker); break;
                    case AttackState.SkyfinBombers: ExecuteSkyfinBombers(npc, target, ref timer, ref tracker); break;
                    case AttackState.SpentFuel: ExecuteSpentFuel(npc, target, ref timer, ref tracker); break;
                    case AttackState.SulphurousGrabber: ExecuteSulphurousGrabber(npc, target, ref timer, ref tracker); break;
                    case AttackState.Transition: ExecuteTransition(npc, target, ref timer, ref tracker); break;
                }
            }

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            ApplyStunDamageBonus(ref modifiers);
            ProcessPustuleHit(npc, player.Center, ref modifiers, item.damage);
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            ApplyStunDamageBonus(ref modifiers);
            ProcessPustuleHit(npc, projectile.Center, ref modifiers, projectile.damage);
        }

        private void ApplyStunDamageBonus(ref NPC.HitModifiers modifiers)
        {
            if (pustuleStunTimer > 0)
                modifiers.FinalDamage *= 1.5f;
        }
        #endregion

        #region Worm Chain Spawn
        private void SpawnWormChain(NPC head)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int bodyType = ModContent.Find<ModNPC>("CalamityMod/AquaticScourgeBody").Type;
            int bodyAltType = ModContent.Find<ModNPC>("CalamityMod/AquaticScourgeBodyAlt").Type;
            int tailType = ModContent.Find<ModNPC>("CalamityMod/AquaticScourgeTail").Type;
            const int totalLength = 30;
            int[] pustuleIndices = { 4, 9, 14, 19, 24, 28 };
            int previous = head.whoAmI;
            int pustuleSlot = 0;

            for (int segments = 0; segments < totalLength; segments++)
            {
                bool isTail = segments == totalLength - 1;
                int type = isTail ? tailType : (segments % 2 == 0 ? bodyAltType : bodyType);
                int lol = NPC.NewNPC(head.GetSource_FromAI(), (int)head.position.X + head.width / 2, (int)head.position.Y + head.height / 2, type, head.whoAmI);
                if (lol < 0 || lol >= Main.maxNPCs)
                    continue;

                NPC seg = Main.npc[lol];
                seg.realLife = head.whoAmI;
                seg.ai[2] = head.whoAmI;
                seg.ai[1] = previous;
                Main.npc[previous].ai[0] = lol;
                seg.netUpdate = true;
                previous = lol;

                if (pustuleSlot < 6 && !isTail && pustuleIndices[pustuleSlot] == segments)
                {
                    pustuleSegment[pustuleSlot] = lol;
                    pustuleSlot++;
                }
            }

            for (int i = 0; i < 6; i++) pustuleHPs[i] = 700f;
            head.netUpdate = true;
        }
        #endregion

        #region Pustule Helpers
        private void UpdatePustuleRespawn()
        {
            bool allDead = true;
            for (int i = 0; i < 6; i++) if (pustuleHPs[i] > 0f) allDead = false;

            if (allDead && pustuleStunTimer == 0)
            {
                pustuleRespawnTimer++;
                if (pustuleRespawnTimer >= 1200) // 20s
                {
                    for (int i = 0; i < 6; i++) pustuleHPs[i] = 700f;
                    pustuleRespawnTimer = 0;
                }
            }
            else
            {
                pustuleRespawnTimer = 0;
            }
        }

        private void ProcessPustuleHit(NPC npc, Vector2 hitPos, ref NPC.HitModifiers modifiers, int damage)
        {
            for (int i = 0; i < 6; i++)
            {
                if (pustuleSegment[i] != npc.whoAmI) continue;
                if (pustuleHPs[i] <= 0f) break;

                modifiers.FinalDamage *= 0.05f; // 95% DR on the pustule itself while it's still alive
                pustuleHPs[i] -= damage;
                if (pustuleHPs[i] <= 0f)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath4, hitPos);
                    ScourgeFx.Burst(hitPos, 6f, 16, DustID.ToxicBubble);

                    bool allDead = true;
                    for (int j = 0; j < 6; j++) if (pustuleHPs[j] > 0f) allDead = false;
                    if (allDead && npc.realLife >= 0 && npc.realLife < Main.maxNPCs && Main.npc[npc.realLife].active)
                    {
                        NPC head = Main.npc[npc.realLife];
                        pustuleStunTimer = 360; // 6s
                        head.velocity = Vector2.Zero;
                        SoundEngine.PlaySound(SoundID.NPCHit53, head.Center);
                    }
                }
                break;
            }
        }
        #endregion

        #region Sulphur Tide
        private void UpdateSulphurTide(NPC npc, Player target)
        {
            tideTimer++;
            if (tideTimer >= 480) // 8s cycle
            {
                tideTimer = 0;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float sign = Main.rand.NextBool() ? 1f : -1f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<SulphurTideWallProj>(), npc.damage / 6, 0f, Main.myPlayer, sign);
                }
            }
        }
        #endregion

        #region Movement Helper
        private static Vector2 DirectedHoverSpot(NPC npc, Player target, float sideOffset, float heightOffset, float lead = 0f)
        {
            float side = Math.Sign(npc.Center.X - target.Center.X);
            if (side == 0f) side = Main.rand.NextBool() ? 1f : -1f;
            Vector2 predicted = target.Center + target.velocity * lead;
            return predicted + new Vector2(side * sideOffset, heightOffset);
        }
        #endregion

        #region Attack Rotation
        private void RotateAttack(NPC npc, AttackState current)
        {
            bool isP1 = IsP1(current);
            if (isP1)
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
            }

            attackCycleIndex++;
            AttackState[] cycle = isP1 ? P1Cycle : P2Cycle;
            npc.ai[1] = (float)cycle[attackCycleIndex % cycle.Length];
            npc.ai[2] = 0;
            npc.ai[3] = 0;
            npc.netUpdate = true;
        }
        #endregion

        #region P1 Attacks
        private void ExecuteSubmarineShocker(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1) tracker = UseVariantB(AttackState.SubmarineShocker) ? 1f : 0f;
            var w = FindHeldWeapon<ScourgeHeldSubmarineShocker>(npc);
            w?.SetAim(SafeNormalize(target.Center - npc.Center, Vector2.UnitY).ToRotation());

            if (timer == 40)
            {
                SoundEngine.PlaySound(SoundID.Item94, npc.Center);
                npc.velocity = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 15f;
                w?.Pulse(14f);
            }
            if (timer == 55 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                int count = tracker > 0f ? 5 : 3;
                float spread = tracker > 0f ? 0.22f : 0.35f;
                for (int i = 0; i < count; i++)
                {
                    Vector2 vel = dir.RotatedBy((i - (count - 1) / 2f) * spread) * 8f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<ShockAnchorProj>(), npc.damage / 3, 0f, Main.myPlayer, target.whoAmI);
                }
            }
            if (timer >= 170) RotateAttack(npc, AttackState.SubmarineShocker);
        }

        private void ExecuteBarinautical(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            var w = FindHeldWeapon<ScourgeHeldBarinautical>(npc);
            w?.SetAim(SafeNormalize(target.Center - npc.Center, Vector2.UnitY).ToRotation());

            if ((timer == 30 || timer == 70) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item94 with { Pitch = 0.2f }, npc.Center);
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 13f, ModContent.ProjectileType<HarpoonBoomerangProj>(), npc.damage / 3, 0f, Main.myPlayer, 0f, 0f, npc.whoAmI);
                w?.Pulse(12f);
            }
            if (timer >= 160) RotateAttack(npc, AttackState.Barinautical);
        }

        private void ExecuteDownpour(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            Vector2 hover = DirectedHoverSpot(npc, target, 0f, -260f, 5f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.05f, 0.1f);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item29, npc.Center);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = target.Center + new Vector2((i - 1) * 220f, -400f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<RainCloudProj>(), npc.damage / 4, 0f, Main.myPlayer);
                }
                FindHeldWeapon<ScourgeHeldDownpour>(npc)?.Pulse(10f);
            }
            if (timer >= 220) RotateAttack(npc, AttackState.Downpour);
        }

        private void ExecuteDeepseaStaff(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item21, npc.Center);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = npc.Center + Main.rand.NextVector2CircularEdge(300f, 300f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<AnglerfishProj>(), npc.damage / 3, 0f, Main.myPlayer, target.whoAmI);
                }
                FindHeldWeapon<ScourgeHeldDeepseaStaff>(npc)?.Pulse(10f);
            }
            if (timer >= 220) RotateAttack(npc, AttackState.DeepseaStaff);
        }

        private void ExecuteScourgeSeas(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ScourgeHeldScourgeSeas>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 26 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.2f }, npc.Center);
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<BarbedTendrilProj>(), npc.damage / 2, 0f, Main.myPlayer, dir.X, dir.Y, npc.whoAmI);
            }
            if (timer >= 100) RotateAttack(npc, AttackState.ScourgeSeas);
        }
        #endregion

        #region P2 Attacks
        private void ExecuteFlakToxicannon(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            var w = FindHeldWeapon<ScourgeHeldFlakToxicannon>(npc);
            w?.SetAim(SafeNormalize(target.Center - npc.Center, Vector2.UnitY).ToRotation());

            if (timer >= 30 && timer <= 70 && (timer - 30) % 20 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 11f + new Vector2(0f, -6f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<FlakShellProj>(), npc.damage / 3, 0f, Main.myPlayer);
                w?.Pulse(-10f);
            }
            if (timer >= 170) RotateAttack(npc, AttackState.FlakToxicannon);
        }

        private void ExecuteSlitheringEels(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item9, npc.Center);
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                for (int i = 0; i < 5; i++)
                {
                    Vector2 vel = dir.RotatedBy((i - 2) * 0.18f) * 7f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<EelBoltProj>(), npc.damage / 3, 0f, Main.myPlayer, target.whoAmI);
                }
                FindHeldWeapon<ScourgeHeldSlitheringEels>(npc)?.Pulse(10f);
            }
            if (timer >= 200) RotateAttack(npc, AttackState.SlitheringEels);
        }

        private void ExecuteCausticCroaker(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 pos = target.Center + new Vector2(Main.rand.NextFloat(-250f, 250f), -350f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<CroakerSentryProj>(), npc.damage / 4, 0f, Main.myPlayer, target.whoAmI);
                FindHeldWeapon<ScourgeHeldCausticCroaker>(npc)?.Pulse(10f);
            }
            if (timer >= 220) RotateAttack(npc, AttackState.CausticCroaker);
        }

        private void ExecuteSkyfinBombers(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ScourgeHeldSkyfinBombers>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 22 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item21 with { Pitch = 0.3f }, npc.Center);
                for (int i = 0; i < 2; i++)
                {
                    float dir = i == 0 ? -1f : 1f;
                    Vector2 pos = target.Center + new Vector2(dir * 500f, -450f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, new Vector2(-dir * 6f, 0f), ModContent.ProjectileType<BomberFishProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
            }
            if (timer >= 160) RotateAttack(npc, AttackState.SkyfinBombers);
        }

        private void ExecuteSpentFuel(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ScourgeHeldSpentFuel>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer >= 30 && timer <= 70 && (timer - 30) % 20 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 pos = target.Center + Main.rand.NextVector2Circular(260f, 60f) - new Vector2(0f, 400f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), pos, new Vector2(0f, 3f), ModContent.ProjectileType<FuelBarrelProj>(), npc.damage / 3, 0f, Main.myPlayer);
            }
            if (timer >= 180) RotateAttack(npc, AttackState.SpentFuel);
        }

        private void ExecuteSulphurousGrabber(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ScourgeHeldSulphurousGrabber>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 24 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.4f }, npc.Center);
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<BarbedTendrilProj>(), npc.damage / 2, 0f, Main.myPlayer, dir.X, dir.Y, npc.whoAmI);
            }
            if (timer == 60)
                npc.velocity += SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 6f;
            if (timer >= 110) RotateAttack(npc, AttackState.SulphurousGrabber);
        }
        #endregion

        #region Transition
        private void ExecuteTransition(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            npc.velocity *= 0.9f;

            if (timer == 1)
                SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
            if (timer == 40)
                ScourgeFx.Burst(npc.Center, 7f, 24, DustID.ToxicBubble);

            if (timer >= 80)
            {
                attackCycleIndex = 0;
                currentRepetition = 0;
                npc.ai[1] = (float)AttackState.FlakToxicannon;
                npc.ai[2] = 0;
                npc.ai[3] = 0;
                npc.dontTakeDamage = false;
                npc.netUpdate = true;
            }
        }
        #endregion

        #region Drawing
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (npc.type != ModContent.Find<ModNPC>("CalamityMod/AquaticScourgeHead").Type)
                return;

            Texture2D glowTex = TextureAssets.Dust.Value;
            Rectangle sourceRect = new Rectangle(0, 0, 8, 8);
            for (int i = 0; i < 6; i++)
            {
                if (pustuleHPs[i] <= 0f) continue;
                int segIdx = pustuleSegment[i];
                if (segIdx < 0 || segIdx >= Main.maxNPCs || !Main.npc[segIdx].active) continue;
                Vector2 pos = Main.npc[segIdx].Center - screenPos;
                spriteBatch.Draw(glowTex, pos, sourceRect, new Color(150, 255, 120) * 0.7f, 0f, new Vector2(4f, 4f), 2.6f, SpriteEffects.None, 0f);
            }
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
