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

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.Polterghast
{
    internal sealed class PolterghastAI : IUMWBossAI
    {
        #region Constants & Configurations
        public override int NPCType => ModContent.Find<ModNPC>("CalamityMod/Polterghast").Type;
        public override string BossName => "Polterghast";
        public override Color DebugColor => new(200, 60, 200);

        // Design doc specifies a single 50% HP unseal, not a 3-phase ladder.
        public override int MaxPhaseCount => 2;
        public override float[] PhaseLifeRatios => new[] { 0.50f };
        public override int AttackCycleLength => 120;
        public override float MotionIntensity => 1.1f;
        #endregion

        #region Attack States
        public enum AttackState
        {
            TerrorBlade = 0,
            BansheeHook = 1,
            DaemonsFlame = 2,
            FatesReveal = 3,
            GhastlyVisage = 4,
            EtherealSubjugator = 5,
            GhoulishGouger = 6,
            GalileoGladius = 7,
            CrescentMoon = 8,
            HalleysInferno = 9,
            AlphaDraconis = 10,
            StratusSphere = 11,
            Sirius = 12,
            WarloksMoon = 13,
            Vega = 14,
            Transition = 15,
        }

        private static bool IsP1(AttackState s) => s == AttackState.TerrorBlade || s == AttackState.BansheeHook ||
            s == AttackState.DaemonsFlame || s == AttackState.FatesReveal || s == AttackState.GhastlyVisage ||
            s == AttackState.EtherealSubjugator || s == AttackState.GhoulishGouger;

        // P1 already has 7 named weapons (>6) — single execution each, no padding needed.
        private static readonly AttackState[] P1Cycle =
        {
            AttackState.TerrorBlade, AttackState.BansheeHook, AttackState.DaemonsFlame, AttackState.FatesReveal,
            AttackState.GhastlyVisage, AttackState.EtherealSubjugator, AttackState.GhoulishGouger,
        };
        // Design doc pairs these into 4 combo-rounds, but (matching the precedent set for every other
        // >6-weapon P2 this project) each of the 8 named weapons gets its own independent rotation slot.
        private static readonly AttackState[] P2Cycle =
        {
            AttackState.GalileoGladius, AttackState.CrescentMoon, AttackState.HalleysInferno, AttackState.AlphaDraconis,
            AttackState.StratusSphere, AttackState.Sirius, AttackState.WarloksMoon, AttackState.Vega,
        };
        #endregion

        #region Fields
        private int ticksRunning = 0;
        private int currentRepetition = 0;
        private int attackCycleIndex = 0;

        // Dungeon Brick Cage wall slams
        private int slamTimer = 0;
        private int activeSlamSide = -1; // 0: left, 1: right, 2: top, 3: bottom
        private float wallSlamOffset = 0f;
        private int slamHurtCooldown = 0;

        // Ghostly Twin Mirror Clones
        private float hateCloneHP = 1500f;
        private float fearCloneHP = 1500f;
        private int stunTimer = 0;
        private int respawnClonesTimer = 0;
        private int cloneFxCooldown = 0;

        private int arenaHurtCooldown = 0;
        private float transitionFlashAlpha = 0f;
        #endregion

        #region Core AI Hooks
        public override bool PreAI(NPC npc, IUMWGlobalNPC data)
        {
            ticksRunning++;

            if (!TryGetTarget(npc, out Player target))
            {
                npc.velocity.Y -= 0.5f;
                if (npc.timeLeft > 60) npc.timeLeft = 60;
                return false;
            }

            int currentPhase = (int)npc.ai[0];
            AttackState state = (AttackState)(int)npc.ai[1];
            ref float timer = ref npc.ai[2];
            ref float tracker = ref npc.ai[3];

            if (currentPhase == 0)
            {
                currentPhase = 1;
                npc.ai[0] = 1f;
                state = AttackState.TerrorBlade;
                npc.ai[1] = (float)state;
                currentRepetition = 0;
                attackCycleIndex = 0;
                npc.netUpdate = true;
            }

            float lifeRatio = npc.lifeMax <= 0 ? 1f : npc.life / (float)npc.lifeMax;
            int nextPhase = lifeRatio <= PhaseLifeRatios[0] ? 2 : 1;

            if (nextPhase > currentPhase && state != AttackState.Transition)
            {
                currentPhase = nextPhase;
                npc.ai[0] = currentPhase;
                state = AttackState.Transition;
                npc.ai[1] = (float)state;
                timer = 0;
                tracker = 0;
                npc.netUpdate = true;
            }

            float borderSize = currentPhase == 1 ? 1400f : 900f;
            if (arenaHurtCooldown > 0) arenaHurtCooldown--;
            Vector2 dist = target.Center - npc.Center;
            if (Math.Abs(dist.X) > borderSize / 2f || Math.Abs(dist.Y) > borderSize / 2f)
            {
                target.AddBuff(BuffID.Bleeding, 180);
                target.AddBuff(BuffID.Silenced, 180); // Necro Choke: locks dash for 3s
                if (arenaHurtCooldown <= 0)
                {
                    arenaHurtCooldown = 30;
                    target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 20, 0);
                }
            }

            UpdateWallSlams(npc, target, borderSize);
            UpdateClonesRespawn();
            UpdateCloneReflection(npc, target);
            if (cloneFxCooldown > 0) cloneFxCooldown--;

            if (stunTimer > 0)
            {
                stunTimer--;
                npc.velocity *= 0.85f;
            }
            else if (state != AttackState.Transition)
            {
                float speed = 10f + (1f - lifeRatio) * 5f;
                Vector2 desiredPos = target.Center + new Vector2((float)Math.Sin(ticksRunning * 0.04f) * 260f, -120f);
                Vector2 desiredVel = (desiredPos - npc.Center) * 0.04f;
                if (desiredVel.Length() > speed) desiredVel = SafeNormalize(desiredVel, Vector2.Zero) * speed;
                npc.velocity = Vector2.Lerp(npc.velocity, desiredVel, 0.1f);
            }
            npc.rotation = npc.velocity.X * 0.04f;

            if (stunTimer == 0)
            {
                switch (state)
                {
                    case AttackState.TerrorBlade: ExecuteTerrorBlade(npc, target, ref timer, ref tracker); break;
                    case AttackState.BansheeHook: ExecuteBansheeHook(npc, target, ref timer, ref tracker); break;
                    case AttackState.DaemonsFlame: ExecuteDaemonsFlame(npc, target, ref timer, ref tracker); break;
                    case AttackState.FatesReveal: ExecuteFatesReveal(npc, target, ref timer, ref tracker); break;
                    case AttackState.GhastlyVisage: ExecuteGhastlyVisage(npc, target, ref timer, ref tracker); break;
                    case AttackState.EtherealSubjugator: ExecuteEtherealSubjugator(npc, target, ref timer, ref tracker); break;
                    case AttackState.GhoulishGouger: ExecuteGhoulishGouger(npc, target, ref timer, ref tracker); break;
                    case AttackState.GalileoGladius: ExecuteGalileoGladius(npc, target, ref timer, ref tracker); break;
                    case AttackState.CrescentMoon: ExecuteCrescentMoon(npc, target, ref timer, ref tracker); break;
                    case AttackState.HalleysInferno: ExecuteHalleysInferno(npc, target, ref timer, ref tracker); break;
                    case AttackState.AlphaDraconis: ExecuteAlphaDraconis(npc, target, ref timer, ref tracker); break;
                    case AttackState.StratusSphere: ExecuteStratusSphere(npc, target, ref timer, ref tracker); break;
                    case AttackState.Sirius: ExecuteSirius(npc, target, ref timer, ref tracker); break;
                    case AttackState.WarloksMoon: ExecuteWarloksMoon(npc, target, ref timer, ref tracker); break;
                    case AttackState.Vega: ExecuteVega(npc, target, ref timer, ref tracker); break;
                    case AttackState.Transition: ExecuteTransition(npc, target, ref timer, ref tracker); break;
                }
            }

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) => ProcessCloneHits(npc, player.Center, ref modifiers, item.damage);
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) => ProcessCloneHits(npc, projectile.Center, ref modifiers, projectile.damage);
        #endregion

        #region Helpers
        private void UpdateWallSlams(NPC npc, Player target, float borderSize)
        {
            slamTimer++;
            if (slamTimer >= 480) // every 8s
            {
                slamTimer = 0;
                activeSlamSide = Main.rand.Next(4);
                wallSlamOffset = 0f;
                slamHurtCooldown = 0;
            }

            if (activeSlamSide != -1)
            {
                if (slamTimer < 60)
                {
                    wallSlamOffset = MathHelper.Lerp(0f, 300f, slamTimer / 60f);
                }
                else if (slamTimer < 180)
                {
                    wallSlamOffset = 300f;
                    if (slamHurtCooldown > 0) { slamHurtCooldown--; }
                    else
                    {
                        Vector2 dist = target.Center - npc.Center;
                        bool collided =
                            (activeSlamSide == 0 && dist.X < -borderSize / 2f + 300f) ||
                            (activeSlamSide == 1 && dist.X > borderSize / 2f - 300f) ||
                            (activeSlamSide == 2 && dist.Y < -borderSize / 2f + 300f) ||
                            (activeSlamSide == 3 && dist.Y > borderSize / 2f - 300f);
                        if (collided)
                        {
                            slamHurtCooldown = 30;
                            target.AddBuff(BuffID.Silenced, 180);
                            target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 20, 0);
                        }
                    }
                }
                else
                {
                    wallSlamOffset = MathHelper.Lerp(300f, 0f, (slamTimer - 180f) / 60f);
                    if (slamTimer >= 240)
                    {
                        activeSlamSide = -1;
                        wallSlamOffset = 0f;
                    }
                }
            }
        }

        private void UpdateClonesRespawn()
        {
            if (hateCloneHP <= 0f && fearCloneHP <= 0f && stunTimer == 0)
            {
                respawnClonesTimer++;
                if (respawnClonesTimer >= 1500) // 25s respawn (design doc)
                {
                    hateCloneHP = 1500f;
                    fearCloneHP = 1500f;
                    respawnClonesTimer = 0;
                }
            }
        }

        private void UpdateCloneReflection(NPC npc, Player target)
        {
            Vector2 targetOffset = target.Center - npc.Center;
            Vector2 hatePos = npc.Center - targetOffset;
            Vector2 fearPos = npc.Center - targetOffset.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || proj.hostile || proj.owner != target.whoAmI)
                    continue;

                if (hateCloneHP > 0f && Vector2.Distance(proj.Center, hatePos) < 60f)
                {
                    proj.Kill();
                    int dmg = npc.damage / 3;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(npc.GetSource_FromAI(), hatePos, SafeNormalize(target.Center - hatePos, Vector2.UnitY) * 12f, ModContent.ProjectileType<GhostFireProj>(), dmg, 0f, Main.myPlayer);
                }
                else if (fearCloneHP > 0f && Vector2.Distance(proj.Center, fearPos) < 60f)
                {
                    proj.Kill();
                    int dmg = npc.damage / 3;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(npc.GetSource_FromAI(), fearPos, SafeNormalize(target.Center - fearPos, Vector2.UnitY) * 12f, ModContent.ProjectileType<GhostFireProj>(), dmg, 0f, Main.myPlayer);
                }
            }
        }

        private void ProcessCloneHits(NPC npc, Vector2 hitPos, ref NPC.HitModifiers modifiers, int damage)
        {
            if (npc.ai[1] == (float)AttackState.Transition)
            {
                modifiers.FinalDamage *= 0f;
                return;
            }

            int activeCount = 0;
            if (hateCloneHP > 0f) activeCount++;
            if (fearCloneHP > 0f) activeCount++;
            if (activeCount > 0)
                modifiers.FinalDamage *= 0.15f; // 85% DR while any clone survives

            if (stunTimer > 0)
                return;

            Vector2 targetOffset = Main.player[npc.target].Center - npc.Center;
            Vector2 hatePos = npc.Center - targetOffset;
            Vector2 fearPos = npc.Center - targetOffset.RotatedBy(MathHelper.PiOver2);

            if (hateCloneHP > 0f && Vector2.Distance(hitPos, hatePos) < 80f)
            {
                hateCloneHP -= damage;
                if (cloneFxCooldown <= 0) { cloneFxCooldown = 8; SoundEngine.PlaySound(SoundID.NPCHit5 with { Volume = 0.4f }, hatePos); }
                if (hateCloneHP <= 0f) { SoundEngine.PlaySound(SoundID.NPCDeath4, hatePos); GhastFx.Burst(hatePos, 5f, 16); CheckAllClonesBroken(npc); }
            }
            else if (fearCloneHP > 0f && Vector2.Distance(hitPos, fearPos) < 80f)
            {
                fearCloneHP -= damage;
                if (cloneFxCooldown <= 0) { cloneFxCooldown = 8; SoundEngine.PlaySound(SoundID.NPCHit5 with { Volume = 0.4f }, fearPos); }
                if (fearCloneHP <= 0f) { SoundEngine.PlaySound(SoundID.NPCDeath4, fearPos); GhastFx.Burst(fearPos, 5f, 16, DustID.BlueTorch); CheckAllClonesBroken(npc); }
            }
        }

        private void CheckAllClonesBroken(NPC npc)
        {
            if (hateCloneHP <= 0f && fearCloneHP <= 0f)
            {
                stunTimer = 420; // 7s stun (design doc)
                npc.velocity = Vector2.Zero;
                SoundEngine.PlaySound(SoundID.NPCHit53, npc.Center);
                GhastFx.Burst(npc.Center, 7f, 30);
            }
        }

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
                AttackState next = P1Cycle[attackCycleIndex % P1Cycle.Length];

                // Clone-disable skip logic (design doc): Hate (red) -> BansheeHook + GhoulishGouger;
                // Fear (blue) -> FatesReveal + EtherealSubjugator.
                for (int guard = 0; guard < P1Cycle.Length; guard++)
                {
                    bool skip = (next == AttackState.BansheeHook && hateCloneHP <= 0f) ||
                                (next == AttackState.GhoulishGouger && hateCloneHP <= 0f) ||
                                (next == AttackState.FatesReveal && fearCloneHP <= 0f) ||
                                (next == AttackState.EtherealSubjugator && fearCloneHP <= 0f);
                    if (!skip) break;
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
            npc.ai[2] = 0; npc.ai[3] = 0; npc.netUpdate = true;
        }
        #endregion

        #region P1 Attacks
        private void ExecuteTerrorBlade(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldTerrorBlade>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy((i - 1) * 0.15f) * 12f;
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<TerrorBladeWaveProj>(), npc.damage / 3, 0f, Main.myPlayer, 3f, 650f);
                    if (idx >= 0 && idx < Main.maxProjectiles)
                    {
                        Main.projectile[idx].ai[2] = npc.Center.X;
                        Main.projectile[idx].ai[3] = npc.Center.Y;
                    }
                }
            }

            if (timer >= 190)
                RotateAttack(npc, AttackState.TerrorBlade);
        }

        private void ExecuteBansheeHook(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldBansheeHook>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 4; i++)
                {
                    float a = i * MathHelper.PiOver2 + MathHelper.PiOver4;
                    Vector2 dir = a.ToRotationVector2();
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + dir * 400f, Vector2.Zero, ModContent.ProjectileType<BansheeChainProj>(), npc.damage / 3, 0f, Main.myPlayer, dir.X, dir.Y);
                }
                FindHeldWeapon<GhastHeldBansheeHook>(npc)?.Pulse(14f);
            }

            if (timer >= 180)
                RotateAttack(npc, AttackState.BansheeHook);
        }

        private void ExecuteDaemonsFlame(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldDaemonsFlame>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 8; i++)
                {
                    float a = i * MathHelper.TwoPi / 8f;
                    Vector2 dir = a.ToRotationVector2();
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir, ModContent.ProjectileType<DaemonsFireballProj>(), npc.damage / 3, 0f, Main.myPlayer, dir.X, dir.Y);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[2] = a;
                }
                FindHeldWeapon<GhastHeldDaemonsFlame>(npc)?.Pulse(-10f);
            }

            if (timer >= 180)
                RotateAttack(npc, AttackState.DaemonsFlame);
        }

        private void ExecuteFatesReveal(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldFatesReveal>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = target.Center + new Vector2(i * 120f - 120f, -300f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<FatesRevealSigilProj>(), npc.damage / 3, 0f, Main.myPlayer, i * 15f + 20f);
                }
                FindHeldWeapon<GhastHeldFatesReveal>(npc)?.Pulse(10f);
            }

            if (timer >= 190)
                RotateAttack(npc, AttackState.FatesReveal);
        }

        private void ExecuteGhastlyVisage(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldGhastlyVisage>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 3f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<GhastlyVisageFaceProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<GhastHeldGhastlyVisage>(npc)?.Pulse(-12f);
            }

            if (timer >= 180)
                RotateAttack(npc, AttackState.GhastlyVisage);
        }

        private void ExecuteEtherealSubjugator(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldEtherealSubjugator>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    float a = i * MathHelper.TwoPi / 3f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + a.ToRotationVector2() * 200f, Vector2.Zero, ModContent.ProjectileType<SubjugatorMiniProj>(), npc.damage / 4, 0f, Main.myPlayer, 200f, a);
                }
                FindHeldWeapon<GhastHeldEtherealSubjugator>(npc)?.Pulse(8f);
            }

            if (timer >= 200)
                RotateAttack(npc, AttackState.EtherealSubjugator);
        }

        private void ExecuteGhoulishGouger(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<GhastHeldGhoulishGouger>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 14f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<GougerDrillProj>(), npc.damage / 3, 0f, Main.myPlayer, 650f, npc.Center.X, npc.Center.Y);
                FindHeldWeapon<GhastHeldGhoulishGouger>(npc)?.Pulse(14f);
            }

            if (timer >= 220)
                RotateAttack(npc, AttackState.GhoulishGouger);
        }
        #endregion

        #region P2 Attacks
        private void ExecuteGalileoGladius(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldGalileoGladius>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer >= 30 && timer <= 150 && (timer - 30) % 17 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 blinkPos = target.Center + Main.rand.NextVector2Circular(220f, 220f);
                npc.Center = blinkPos;
                npc.netUpdate = true;
                SoundEngine.PlaySound(SoundID.Item8, npc.Center);
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 16f, ModContent.ProjectileType<GalileoSlashProj>(), npc.damage / 3, 0f, Main.myPlayer);
                if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].rotation = dir.ToRotation();
            }

            if (timer >= 180)
                RotateAttack(npc, AttackState.GalileoGladius);
        }

        private void ExecuteCrescentMoon(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldCrescentMoon>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<CrescentPendulumProj>(), npc.damage / 3, 0f, Main.myPlayer, npc.Center.X, npc.Center.Y);
            }

            if (timer >= 190)
                RotateAttack(npc, AttackState.CrescentMoon);
        }

        private void ExecuteHalleysInferno(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<GhastHeldHalleysInferno>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 10f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<HalleysCometProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<GhastHeldHalleysInferno>(npc)?.Pulse(10f);
            }

            if (timer >= 170)
                RotateAttack(npc, AttackState.HalleysInferno);
        }

        private void ExecuteAlphaDraconis(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldAlphaDraconis>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer >= 30 && timer <= 150 && timer % 24 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy((i - 1) * 0.2f) * 9f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<DraconisFireballProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                FindHeldWeapon<GhastHeldAlphaDraconis>(npc)?.Pulse(6f);
            }

            if (timer >= 180)
                RotateAttack(npc, AttackState.AlphaDraconis);
        }

        private void ExecuteStratusSphere(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldStratusSphere>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = target.Center + new Vector2(i * 180f - 180f, -320f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<StratusCloudProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
            }

            if (timer >= 200)
                RotateAttack(npc, AttackState.StratusSphere);
        }

        private void ExecuteSirius(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldSirius>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 20 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<SiriusStarProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<GhastHeldSirius>(npc)?.Pulse(10f);
            }

            if (timer >= 150)
                RotateAttack(npc, AttackState.Sirius);
        }

        private void ExecuteWarloksMoon(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldWarloksMoon>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawn = target.Center + new Vector2(0f, -420f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(0f, 15f), ModContent.ProjectileType<MoonFistProj>(), npc.damage / 3, 0f, Main.myPlayer, 28f);
                FindHeldWeapon<GhastHeldWarloksMoon>(npc)?.Pulse(-14f);
            }

            if (timer >= 170)
                RotateAttack(npc, AttackState.WarloksMoon);
        }

        private void ExecuteVega(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldVega>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<VegaLightNetProj>(), npc.damage / 3, 0f, Main.myPlayer);
            }

            if (timer >= 200)
                RotateAttack(npc, AttackState.Vega);
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
                GhastFx.Burst(npc.Center, 7f, 30);

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
            if (transitionFlashAlpha > 0f)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * transitionFlashAlpha);
            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D glowTex = TextureAssets.Dust.Value;
            Rectangle sourceRect = new Rectangle(0, 0, 8, 8);

            Vector2 targetOffset = Main.player[npc.target].Center - npc.Center;
            Vector2 hatePos = npc.Center - targetOffset;
            Vector2 fearPos = npc.Center - targetOffset.RotatedBy(MathHelper.PiOver2);

            if (hateCloneHP > 0f)
                spriteBatch.Draw(glowTex, hatePos - screenPos, sourceRect, Color.Red * 0.7f, ticksRunning * 0.05f, new Vector2(4f, 4f), 5f, SpriteEffects.None, 0f);
            if (fearCloneHP > 0f)
                spriteBatch.Draw(glowTex, fearPos - screenPos, sourceRect, Color.Blue * 0.7f, -ticksRunning * 0.05f, new Vector2(4f, 4f), 5f, SpriteEffects.None, 0f);
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
