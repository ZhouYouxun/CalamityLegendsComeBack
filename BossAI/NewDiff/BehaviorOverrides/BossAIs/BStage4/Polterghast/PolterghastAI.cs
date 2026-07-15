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
    // 噬魂幽花 — 压缩战场、操控镜像分身的地牢梦魇. 设计文档: 大计划/M 噬魂幽花/噬魂幽花_重置版设计文档.md
    // 移动哲学(分寸感): 幽魂在怨能石墙内侧的暗角之间飘移(正弦幽浮), 从不径直扑脸;
    // P2按文档开启"高频瞬移" — 每次瞬移都有怨雾汇聚预告, 绝不无征兆闪现.
    // 结界石墙、砖墙拍击、双子分身全部可视化(旧版全是隐形碰撞, 是最重的公平性欠账).
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

        private static readonly Color GhostPink = new(230, 120, 230);
        private static readonly Color BrickGray = new(150, 140, 170);
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
            DeathAnimation = 16,
        }

        private static bool IsP1(AttackState s) => s == AttackState.TerrorBlade || s == AttackState.BansheeHook ||
            s == AttackState.DaemonsFlame || s == AttackState.FatesReveal || s == AttackState.GhastlyVisage ||
            s == AttackState.EtherealSubjugator || s == AttackState.GhoulishGouger;

        // P1 already has 7 named weapons (>6) — 3-rep rule per design doc.
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

        // The dungeon cage is a PLACE: anchored at fight start, drifting only barely.
        private Vector2 arenaCenter = Vector2.Zero;
        private bool centerSet = false;

        // Dungeon Brick Cage wall slams. Doc rhythm: 1s dashed warning -> fast slam -> 2s hold -> retract.
        private int slamTimer = 0;
        private int activeSlamSide = -1; // 0: left, 1: right, 2: top, 3: bottom
        private float wallSlamOffset = 0f;
        private int slamHurtCooldown = 0;

        // Ghostly Twin Mirror Clones
        private float hateCloneHP = 1500f;
        private float fearCloneHP = 1500f;
        private readonly float[] cloneFlash = new float[2];
        private int stunTimer = 0;
        private int respawnClonesTimer = 0;
        private int cloneFxCooldown = 0;

        private int arenaHurtCooldown = 0;
        private float transitionFlashAlpha = 0f;

        // Per-attack A/B variant toggle: flips deterministically each time that attack comes up (no RNG).
        private readonly bool[] attackVariant = new bool[16];
        private bool UseVariantB(AttackState state)
        {
            int i = (int)state;
            bool v = attackVariant[i];
            attackVariant[i] = !v;
            return v;
        }
        private bool currentVariantB = false;

        // Telegraphed ghost-blink (ectoplasm converges on the destination before the body re-forms there)
        private int blinkTimer = 0;
        private int blinkDuration = 0;
        private Vector2 blinkDestination = Vector2.Zero;

        // Galileo fold-blink bookkeeping
        private Vector2 galileoNextPos = Vector2.Zero;

        private float weavePhase = 0f;
        private readonly Vector2[] oldPos = new Vector2[10];
        private int oldPosIndex = 0;
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

            if (!centerSet)
            {
                arenaCenter = target.Center;
                centerSet = true;
            }
            arenaCenter = Vector2.Lerp(arenaCenter, target.Center, 0.004f);

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
                currentVariantB = UseVariantB(state);
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
                CleanupHeldWeapons(npc);
                npc.netUpdate = true;
            }

            float borderSize = currentPhase == 1 ? 1400f : 900f;
            if (arenaHurtCooldown > 0) arenaHurtCooldown--;
            Vector2 dist = target.Center - arenaCenter;
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
            UpdateClonesRespawn(npc);
            UpdateCloneReflection(npc, target);
            if (cloneFxCooldown > 0) cloneFxCooldown--;
            for (int i = 0; i < 2; i++)
                if (cloneFlash[i] > 0f) cloneFlash[i] -= 0.08f;

            // Ambient: dungeon wisps drifting through the cage, thicker as the ghost weakens
            if (Main.rand.NextFloat() < 0.12f + (1f - lifeRatio) * 0.2f)
            {
                Vector2 spawnPos = arenaCenter + Main.rand.NextVector2Circular(borderSize / 2f, borderSize / 2f);
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.PinkTorch, new Vector2(0f, -Main.rand.NextFloat(0.5f, 1.5f)), 160, default, Main.rand.NextFloat(0.8f, 1.3f));
                d.noGravity = true;
                d.fadeIn = 1f;
            }

            if (stunTimer > 0)
            {
                // 双碎大硬直: the phantom gutters like a dying lamp
                stunTimer--;
                npc.velocity *= 0.9f;
                npc.rotation = MathF.Sin(ticksRunning * 0.2f) * 0.1f;
                npc.damage = 0;
                npc.Opacity = 0.75f + 0.25f * MathF.Sin(ticksRunning * 0.35f); // flickering weakness
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(70f, 70f), DustID.PinkTorch, new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(1f, 2.5f)), 120, default, 1.3f);
                    d.noGravity = true;
                }
                if (stunTimer == 0)
                {
                    npc.Opacity = 1f;
                    SoundEngine.PlaySound(SoundID.Zombie39 with { Volume = 0.8f }, npc.Center);
                    GhastFx.Burst(npc.Center, 6f, 24);
                }
            }
            else if (blinkDuration <= 0)
            {
                npc.damage = npc.defDamage;
                npc.rotation = npc.velocity.X * 0.04f;

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
                    case AttackState.DeathAnimation: ExecuteDeathAnimation(npc, target, ref timer); break;
                }
            }
            else
            {
                timer++; // attack timelines tick through blinks — blinks belong to the attack rhythm
            }

            UpdateBlink(npc);

            oldPos[oldPosIndex] = npc.Center;
            oldPosIndex = (oldPosIndex + 1) % oldPos.Length;

            data.CurrentPhase = currentPhase;
            data.AttackState = (IUMWAttackState)Math.Clamp((int)state, 0, 4);
            data.PatternTimer = (int)timer;

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            ProcessCloneHits(npc, player.Center, ref modifiers, item.damage);
            InterceptLethalHit(npc, ref modifiers, (int)AttackState.DeathAnimation, () => BeginDeathAnimation(npc, player));
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            ProcessCloneHits(npc, projectile.Center, ref modifiers, projectile.damage);
            InterceptLethalHit(npc, ref modifiers, (int)AttackState.DeathAnimation, () => BeginDeathAnimation(npc, Main.player[projectile.owner]));
        }
        #endregion

        #region Movement & Blink Helpers
        private void SmoothMove(NPC npc, Vector2 desiredPosition, float acceleration, float maxSpeed)
        {
            Vector2 desiredVelocity = (desiredPosition - npc.Center) * acceleration;
            if (desiredVelocity.Length() > maxSpeed)
                desiredVelocity = Vector2.Normalize(desiredVelocity) * maxSpeed;
            npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, 0.12f);
        }

        // Haunting drift: flank hover with a slow spectral weave. The ghost circles its prey, never charges it.
        private void HauntDrift(NPC npc, Player target, float sideDist, float height, float accel = 0.045f, float maxSpeed = 10f)
        {
            weavePhase += 0.04f;
            Vector2 spot = DirectedHoverSpot(npc, target, sideDist, height, 7f);
            spot += new Vector2(MathF.Sin(weavePhase) * 70f, MathF.Cos(weavePhase * 0.8f) * 50f);
            SmoothMove(npc, spot, accel, maxSpeed);
        }

        private static Vector2 DirectedHoverSpot(NPC npc, Player target, float sideOffset, float heightOffset, float lead = 0f)
        {
            float side = Math.Sign(npc.Center.X - target.Center.X);
            if (side == 0f) side = Main.rand.NextBool() ? 1f : -1f;
            Vector2 predicted = target.Center + target.velocity * lead;
            return predicted + new Vector2(side * sideOffset, heightOffset);
        }

        // Telegraphed ghost-blink: ectoplasm converges on the destination while the body dissolves.
        private void BeginBlink(NPC npc, Vector2 destination, int windup = 20)
        {
            blinkDestination = destination;
            blinkDuration = windup;
            blinkTimer = 0;
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.5f, Pitch = -0.35f }, npc.Center);
            npc.netUpdate = true;
        }

        private void UpdateBlink(NPC npc)
        {
            if (blinkDuration <= 0)
                return;

            blinkTimer++;
            int half = Math.Max(1, blinkDuration / 2);
            npc.velocity *= 0.8f;
            npc.damage = 0;

            if (blinkTimer < half)
            {
                npc.Opacity = MathHelper.Lerp(1f, 0.1f, blinkTimer / (float)half);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 around = blinkDestination + (MathHelper.TwoPi * Main.rand.NextFloat()).ToRotationVector2() * Main.rand.NextFloat(50f, 110f);
                    Dust d = Dust.NewDustPerfect(around, DustID.PinkTorch, (blinkDestination - around) * 0.08f, 100, GhostPink, Main.rand.NextFloat(1.1f, 1.4f));
                    d.fadeIn = 1.3f;
                    d.noGravity = true;
                }
            }
            else if (blinkTimer == half)
            {
                npc.Center = blinkDestination;
                npc.velocity = Vector2.Zero;
                SoundEngine.PlaySound(SoundID.Item104 with { Volume = 0.4f, Pitch = 0.2f }, npc.Center);
                GhastFx.Burst(npc.Center, 4f, 14);
                for (int i = 0; i < oldPos.Length; i++)
                    oldPos[i] = npc.Center;
            }
            else
            {
                npc.Opacity = MathHelper.Lerp(0.1f, 1f, (blinkTimer - half) / (float)half);
            }

            if (blinkTimer >= blinkDuration)
            {
                blinkDuration = 0;
                npc.Opacity = 1f;
            }
        }

        private static void CleanupHeldWeapons(NPC npc)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.ModProjectile is BossHeldWeaponBase && (int)p.ai[0] == npc.whoAmI)
                    p.Kill();
            }
        }

        // Ectoplasm drawn into the ghost before a volley — every attack telegraphs.
        private static void ChargeWisps(NPC npc, int density = 2)
        {
            if (!Main.rand.NextBool(density))
                return;
            Vector2 around = npc.Center + Main.rand.NextVector2CircularEdge(100f, 100f);
            Dust d = Dust.NewDustPerfect(around, DustID.PinkTorch, (npc.Center - around) * 0.08f, 100, default, 1.2f);
            d.fadeIn = 1.2f;
            d.noGravity = true;
        }
        #endregion

        #region Cage, Slams & Clones
        // Doc rhythm: 60f dashed warning (no hitbox) -> 12f slam-out -> 120f solid hold -> 48f retract.
        private void UpdateWallSlams(NPC npc, Player target, float borderSize)
        {
            slamTimer++;
            if (slamTimer >= 480) // every 8s
            {
                slamTimer = 0;
                activeSlamSide = Main.rand.Next(4);
                wallSlamOffset = 0f;
                slamHurtCooldown = 0;
                SoundEngine.PlaySound(SoundID.Zombie39 with { Volume = 0.6f, Pitch = -0.5f }, target.Center); // the bricks scream (design doc)
            }

            if (activeSlamSide == -1)
                return;

            if (slamTimer < 60)
            {
                // Dashed warning — drawn in PostDraw; dust seeps from the wall about to slam
                wallSlamOffset = 0f;
                if (Main.rand.NextBool(2))
                {
                    Vector2 wallPoint = SlamWarnPoint(borderSize);
                    Dust d = Dust.NewDustPerfect(wallPoint, DustID.PinkTorch, SlamInwardDir() * Main.rand.NextFloat(1f, 2.5f), 100, default, 1.2f);
                    d.noGravity = true;
                }
            }
            else if (slamTimer < 72)
            {
                wallSlamOffset = MathHelper.Lerp(0f, 300f, (slamTimer - 60f) / 12f); // the slam itself is FAST
                if (slamTimer == 71 && Main.netMode != NetmodeID.Server)
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower = 5f;
            }
            else if (slamTimer < 192)
            {
                wallSlamOffset = 300f;
                if (slamHurtCooldown > 0) { slamHurtCooldown--; }
                else
                {
                    Vector2 dist = target.Center - arenaCenter;
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
                wallSlamOffset = MathHelper.Lerp(300f, 0f, (slamTimer - 192f) / 48f);
                if (slamTimer >= 240)
                {
                    activeSlamSide = -1;
                    wallSlamOffset = 0f;
                }
            }
        }

        private Vector2 SlamWarnPoint(float borderSize)
        {
            float half = borderSize / 2f;
            float along = Main.rand.NextFloat(-half, half);
            return activeSlamSide switch
            {
                0 => arenaCenter + new Vector2(-half, along),
                1 => arenaCenter + new Vector2(half, along),
                2 => arenaCenter + new Vector2(along, -half),
                _ => arenaCenter + new Vector2(along, half),
            };
        }

        private Vector2 SlamInwardDir() => activeSlamSide switch
        {
            0 => Vector2.UnitX,
            1 => -Vector2.UnitX,
            2 => Vector2.UnitY,
            _ => -Vector2.UnitY,
        };

        private void UpdateClonesRespawn(NPC npc)
        {
            if (hateCloneHP <= 0f && fearCloneHP <= 0f && stunTimer == 0)
            {
                respawnClonesTimer++;
                if (respawnClonesTimer >= 1500) // 25s respawn (design doc)
                {
                    hateCloneHP = 1500f;
                    fearCloneHP = 1500f;
                    respawnClonesTimer = 0;
                    SoundEngine.PlaySound(SoundID.Zombie40 with { Volume = 0.7f }, npc.Center);
                    GhastFx.Burst(ClonePos(npc, 0), 4f, 12);
                    GhastFx.Burst(ClonePos(npc, 1), 4f, 12, DustID.BlueTorch);
                }
            }
        }

        // 0 = Hate (red, inverse-mirrors the player), 1 = Fear (blue, perpendicular mirror).
        private Vector2 ClonePos(NPC npc, int index)
        {
            Vector2 targetOffset = Main.player[npc.target].Center - npc.Center;
            return index == 0 ? npc.Center - targetOffset : npc.Center - targetOffset.RotatedBy(MathHelper.PiOver2);
        }

        private void UpdateCloneReflection(NPC npc, Player target)
        {
            Vector2 hatePos = ClonePos(npc, 0);
            Vector2 fearPos = ClonePos(npc, 1);

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || proj.hostile || proj.owner != target.whoAmI)
                    continue;

                if (hateCloneHP > 0f && Vector2.Distance(proj.Center, hatePos) < 60f)
                {
                    proj.Kill();
                    int dmg = npc.defDamage / 3;
                    GhastFx.Burst(hatePos, 3f, 8);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(npc.GetSource_FromAI(), hatePos, SafeNormalize(target.Center - hatePos, Vector2.UnitY) * 12f, ModContent.ProjectileType<GhostFireProj>(), dmg, 0f, Main.myPlayer);
                }
                else if (fearCloneHP > 0f && Vector2.Distance(proj.Center, fearPos) < 60f)
                {
                    proj.Kill();
                    int dmg = npc.defDamage / 3;
                    GhastFx.Burst(fearPos, 3f, 8, DustID.BlueTorch);
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
            {
                modifiers.FinalDamage *= 1.5f; // paralyzed: punish window
                return;
            }

            Vector2 hatePos = ClonePos(npc, 0);
            Vector2 fearPos = ClonePos(npc, 1);

            if (hateCloneHP > 0f && Vector2.Distance(hitPos, hatePos) < 80f)
            {
                hateCloneHP -= damage;
                cloneFlash[0] = 1f;
                if (cloneFxCooldown <= 0) { cloneFxCooldown = 8; SoundEngine.PlaySound(SoundID.NPCHit5 with { Volume = 0.4f }, hatePos); }
                if (hateCloneHP <= 0f) { SoundEngine.PlaySound(SoundID.NPCDeath4, hatePos); GhastFx.Burst(hatePos, 5f, 16); CheckAllClonesBroken(npc); }
            }
            else if (fearCloneHP > 0f && Vector2.Distance(hitPos, fearPos) < 80f)
            {
                fearCloneHP -= damage;
                cloneFlash[1] = 1f;
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
                SoundEngine.PlaySound(SoundID.Zombie40 with { Volume = 0.9f, Pitch = -0.5f }, npc.Center);
                GhastFx.Burst(npc.Center, 7f, 30);
                if (Main.netMode != NetmodeID.Server)
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower = 9f;
            }
        }
        #endregion

        #region Attack Rotation
        private void RotateAttack(NPC npc, AttackState current)
        {
            CleanupHeldWeapons(npc);
            if (IsP1(current))
            {
                currentRepetition++;
                if (currentRepetition < 3)
                {
                    // Same weapon again, but the A/B read flips so 3 reps never feel like 3 copies
                    currentVariantB = UseVariantB(current);
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

                currentVariantB = UseVariantB(next);
                npc.ai[1] = (float)next;
            }
            else
            {
                attackCycleIndex++;
                AttackState next = P2Cycle[attackCycleIndex % P2Cycle.Length];
                currentVariantB = UseVariantB(next);
                npc.ai[1] = (float)next;
            }
            npc.ai[2] = 0; npc.ai[3] = 0; npc.netUpdate = true;
        }
        #endregion

        #region P1 Attacks
        // 惊惧之刃 · 墙面三向弹跳 — 变体A: 三道锁向反弹剑气; 变体B: 五道宽扇低速, 反弹网更密但更慢.
        private void ExecuteTerrorBlade(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldTerrorBlade>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer < 40)
            {
                HauntDrift(npc, target, 340f, -200f);
                if (timer > 20)
                    ChargeWisps(npc);
            }

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int count = currentVariantB ? 5 : 3;
                float speed = currentVariantB ? 9f : 12f;
                float spread = currentVariantB ? 0.24f : 0.15f;
                for (int i = 0; i < count; i++)
                {
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy((i - (count - 1) / 2f) * spread) * speed;
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<TerrorBladeWaveProj>(), npc.defDamage / 3, 0f, Main.myPlayer, 3f, 650f);
                    if (idx >= 0 && idx < Main.maxProjectiles)
                    {
                        Main.projectile[idx].ai[2] = arenaCenter.X;
                        Main.projectile[idx].ai[3] = arenaCenter.Y;
                    }
                }
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, Pitch = -0.2f }, npc.Center);
                GhastFx.Burst(npc.Center, 4f, 10);
                npc.velocity -= SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 4f; // swing recoil
            }

            if (timer > 40)
                HauntDrift(npc, target, 360f, -180f);

            if (timer >= 160)
                RotateAttack(npc, AttackState.TerrorBlade);
        }

        // 女妖之钩 · 锁链收割线 — 变体A: 对角X四链; 变体B: 十字四链. 链自带0.5秒回抽前摇(弹幕层).
        private void ExecuteBansheeHook(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldBansheeHook>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer < 30)
            {
                HauntDrift(npc, target, 300f, -240f);
                if (timer > 14)
                    ChargeWisps(npc, 1);
            }

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float baseAngle = currentVariantB ? 0f : MathHelper.PiOver4;
                for (int i = 0; i < 4; i++)
                {
                    float a = i * MathHelper.PiOver2 + baseAngle;
                    Vector2 dir = a.ToRotationVector2();
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + dir * 400f, Vector2.Zero, ModContent.ProjectileType<BansheeChainProj>(), npc.defDamage / 3, 0f, Main.myPlayer, dir.X, dir.Y);
                }
                SoundEngine.PlaySound(SoundID.Item65 with { Volume = 0.7f }, target.Center);
                FindHeldWeapon<GhastHeldBansheeHook>(npc)?.Pulse(14f);
            }

            if (timer > 30)
                HauntDrift(npc, target, 340f, -220f);

            if (timer >= 150)
                RotateAttack(npc, AttackState.BansheeHook);
        }

        // 魔鬼之焰 · 螺旋飞火 — 变体A: 八向螺旋环; 变体B: 五发锁向螺旋束(更窄更快).
        private void ExecuteDaemonsFlame(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldDaemonsFlame>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer < 40)
            {
                // Bow drawn skyward: the ghost rises and stills
                SmoothMove(npc, target.Center + new Vector2(0f, -380f), 0.05f, 9f);
                if (timer > 20 && Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + new Vector2(Main.rand.NextFloat(-30f, 30f), -40f), DustID.BlueTorch, -Vector2.UnitY * Main.rand.NextFloat(1f, 2.5f), 100, default, 1.2f);
                    d.fadeIn = 1.2f;
                    d.noGravity = true;
                }
            }

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (currentVariantB)
                {
                    Vector2 baseDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 dir = baseDir.RotatedBy((i - 2) * 0.12f);
                        int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 1.4f, ModContent.ProjectileType<DaemonsFireballProj>(), npc.defDamage / 3, 0f, Main.myPlayer, dir.X, dir.Y);
                        if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[2] = dir.ToRotation();
                    }
                }
                else
                {
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * MathHelper.TwoPi / 8f;
                        Vector2 dir = a.ToRotationVector2();
                        int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir, ModContent.ProjectileType<DaemonsFireballProj>(), npc.defDamage / 3, 0f, Main.myPlayer, dir.X, dir.Y);
                        if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[2] = a;
                    }
                }
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = 0.2f }, npc.Center);
                GhastFx.Burst(npc.Center, 4f, 12, DustID.BlueTorch);
                FindHeldWeapon<GhastHeldDaemonsFlame>(npc)?.Pulse(-10f);
            }

            if (timer > 40)
                HauntDrift(npc, target, 320f, -240f);

            if (timer >= 150)
                RotateAttack(npc, AttackState.DaemonsFlame);
        }

        // 命运揭示 · 骷髅怒火 — 变体A: 三阵横列头顶; 变体B: 三阵三角合围. 怨灵飞过后回头(弹幕层).
        private void ExecuteFatesReveal(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldFatesReveal>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer < 30)
            {
                HauntDrift(npc, target, 300f, -260f);
                if (timer > 14)
                    ChargeWisps(npc, 1);
            }

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = currentVariantB
                        ? target.Center + (i * MathHelper.TwoPi / 3f - MathHelper.PiOver2).ToRotationVector2() * 300f
                        : target.Center + new Vector2(i * 120f - 120f, -300f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<FatesRevealSigilProj>(), npc.defDamage / 3, 0f, Main.myPlayer, i * 15f + 20f);
                }
                SoundEngine.PlaySound(SoundID.Zombie53 with { Volume = 0.6f }, target.Center);
                FindHeldWeapon<GhastHeldFatesReveal>(npc)?.Pulse(10f);
            }

            if (timer > 30)
                HauntDrift(npc, target, 340f, -240f);

            if (timer >= 160)
                RotateAttack(npc, AttackState.FatesReveal);
        }

        // 幽魂面容 · 延迟引爆 — 变体A: 单张巨面; 变体B: 双面错拍, 两次俯冲交错.
        private void ExecuteGhastlyVisage(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldGhastlyVisage>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer < 40)
            {
                HauntDrift(npc, target, 320f, -220f);
                if (timer > 24)
                    ChargeWisps(npc);
            }

            int[] spitTimes = currentVariantB ? new[] { 40, 76 } : new[] { 40 };
            foreach (int st in spitTimes)
            {
                if (timer == st && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 3f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<GhastlyVisageFaceProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Zombie53 with { Volume = 0.7f, Pitch = -0.3f }, npc.Center);
                    GhastFx.Burst(npc.Center, 4f, 10);
                    FindHeldWeapon<GhastHeldGhastlyVisage>(npc)?.Pulse(-12f);
                    npc.velocity -= vel.SafeNormalize(Vector2.Zero) * 4f; // spitting a 200px face has recoil
                }
            }

            if (timer > spitTimes[spitTimes.Length - 1])
                HauntDrift(npc, target, 360f, -200f);

            if (timer >= (currentVariantB ? 180 : 150))
                RotateAttack(npc, AttackState.GhastlyVisage);
        }

        // 虚灵支配者 · 亡灵公转圆 — 变体A: 三仆从半径200; 变体B: 四仆从半径270, 圈大但弹更密.
        private void ExecuteEtherealSubjugator(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldEtherealSubjugator>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer < 40)
            {
                HauntDrift(npc, target, 340f, -240f);
                if (timer > 24)
                    ChargeWisps(npc, 1);
            }

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int count = currentVariantB ? 4 : 3;
                float radius = currentVariantB ? 270f : 200f;
                for (int i = 0; i < count; i++)
                {
                    float a = i * MathHelper.TwoPi / count;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + a.ToRotationVector2() * radius, Vector2.Zero, ModContent.ProjectileType<SubjugatorMiniProj>(), npc.defDamage / 4, 0f, Main.myPlayer, radius, a);
                }
                SoundEngine.PlaySound(SoundID.Zombie40 with { Volume = 0.6f }, target.Center);
                FindHeldWeapon<GhastHeldEtherealSubjugator>(npc)?.Pulse(8f);
            }

            if (timer > 40)
                HauntDrift(npc, target, 380f, -220f);

            if (timer >= 180)
                RotateAttack(npc, AttackState.EtherealSubjugator);
        }

        // 食尸鬼钻掘者 · 贴墙电钻 — 变体A: 单钻贴墙滚行; 变体B: 双钻反向对滚, 两圈相向合围.
        private void ExecuteGhoulishGouger(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<GhastHeldGhoulishGouger>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer < 40)
            {
                HauntDrift(npc, target, 320f, -200f);
                // Drill spin-up sparks
                if (timer > 24 && Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(50f, 50f), DustID.Electric, Main.rand.NextVector2CircularEdge(2.5f, 2.5f), 100, default, 0.9f);
                    d.noGravity = true;
                }
            }

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 14f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<GougerDrillProj>(), npc.defDamage / 3, 0f, Main.myPlayer, 650f, arenaCenter.X, arenaCenter.Y);
                if (currentVariantB)
                {
                    Vector2 vel2 = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy(MathHelper.Pi * 0.35f) * 14f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel2, ModContent.ProjectileType<GougerDrillProj>(), npc.defDamage / 3, 0f, Main.myPlayer, 650f, arenaCenter.X, arenaCenter.Y);
                }
                SoundEngine.PlaySound(SoundID.Item22 with { Volume = 0.8f, Pitch = -0.2f }, npc.Center);
                GhastFx.Burst(npc.Center, 4f, 10);
                FindHeldWeapon<GhastHeldGhoulishGouger>(npc)?.Pulse(14f);
                npc.velocity -= vel.SafeNormalize(Vector2.Zero) * 5f;
            }

            if (timer > 40)
                HauntDrift(npc, target, 360f, -220f);

            if (timer >= 190)
                RotateAttack(npc, AttackState.GhoulishGouger);
        }
        #endregion

        #region P2 Attacks
        // 伽利略短剑 · 七星对折闪 — 七次对折瞬移刺杀, 每次瞬移前尘埃先在落点汇聚(公平预告).
        // 变体A: 左右对折交替; 变体B: 收缩螺旋(半径260→140), 越刺越近.
        private void ExecuteGalileoGladius(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldGalileoGladius>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer < 30)
                HauntDrift(npc, target, 300f, -200f); // stalk while the gladius materializes

            // 7 fold-blinks, one every 20 frames from t=30: warn (10f) -> blink -> slash
            if (timer >= 30 && timer <= 170)
            {
                int cyclePos = (int)(timer - 30) % 20;
                int strikeIndex = (int)(timer - 30) / 20;
                if (strikeIndex < 7)
                {
                    if (cyclePos == 0)
                    {
                        // Choose the next fold point
                        if (currentVariantB)
                        {
                            float radius = MathHelper.Lerp(260f, 140f, strikeIndex / 6f);
                            float a = strikeIndex * 2.4f;
                            galileoNextPos = target.Center + a.ToRotationVector2() * radius;
                        }
                        else
                        {
                            float side = strikeIndex % 2 == 0 ? -1f : 1f;
                            galileoNextPos = target.Center + new Vector2(side * 230f, Main.rand.NextFloat(-140f, 60f));
                        }
                    }
                    else if (cyclePos < 10)
                    {
                        // Ectoplasm converges on the fold point — the player reads where the blade will appear
                        for (int i = 0; i < 2; i++)
                        {
                            Vector2 around = galileoNextPos + Main.rand.NextVector2CircularEdge(60f, 60f);
                            Dust d = Dust.NewDustPerfect(around, DustID.PinkTorch, (galileoNextPos - around) * 0.12f, 100, GhostPink, 1.2f);
                            d.fadeIn = 1.2f;
                            d.noGravity = true;
                        }
                        npc.velocity *= 0.9f;
                    }
                    else if (cyclePos == 10)
                    {
                        npc.Center = galileoNextPos;
                        npc.velocity = Vector2.Zero;
                        for (int i = 0; i < oldPos.Length; i++) oldPos[i] = npc.Center;
                        SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.5f, Pitch = 0.1f }, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                            int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 16f, ModContent.ProjectileType<GalileoSlashProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                            if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].rotation = dir.ToRotation();
                        }
                        GhastFx.Burst(npc.Center, 4f, 8);
                    }
                }
            }

            if (timer > 170)
                HauntDrift(npc, target, 340f, -220f);

            if (timer >= 200)
                RotateAttack(npc, AttackState.GalileoGladius);
        }

        // 新月链刃摆 — 变体A: 锚在幽魂身位(高摆); 变体B: 锚在玩家上方(低扫), 封底更狠.
        private void ExecuteCrescentMoon(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, target.Center + new Vector2(0f, -400f), 18);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldCrescentMoon>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                    // A: anchored where the ghost is ABOUT to re-form (its blink destination — not the stale
                    // pre-blink spot); B: low over the player for the bottom-locking sweep
                    Vector2 anchor = currentVariantB ? target.Center + new Vector2(0f, -320f) : target.Center + new Vector2(0f, -400f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), anchor, Vector2.Zero, ModContent.ProjectileType<CrescentPendulumProj>(), npc.defDamage / 3, 0f, Main.myPlayer, anchor.X, anchor.Y);
                }
                SoundEngine.PlaySound(SoundID.Item65 with { Volume = 0.7f, Pitch = -0.2f }, target.Center);
            }

            if (timer > 18)
                HauntDrift(npc, target, 360f, -260f);

            if (timer >= 190)
                RotateAttack(npc, AttackState.CrescentMoon);
        }

        // 哈雷彗星炮 — 变体A: 单发巨彗星; 变体B: 双彗星±17°, 彗尾散弹交叉.
        private void ExecuteHalleysInferno(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, DirectedHoverSpot(npc, target, 420f, -160f, 8f), 18);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<GhastHeldHalleysInferno>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 18 && timer < 40)
            {
                npc.velocity *= 0.93f;
                ChargeWisps(npc, 1);
                FindHeldWeapon<GhastHeldHalleysInferno>(npc)?.SetAim((target.Center - npc.Center).ToRotation());
            }

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int comets = currentVariantB ? 2 : 1;
                for (int i = 0; i < comets; i++)
                {
                    float off = currentVariantB ? (i == 0 ? -0.17f : 0.17f) : 0f;
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy(off) * 10f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<HalleysCometProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.8f }, npc.Center);
                GhastFx.Burst(npc.Center, 5f, 12, DustID.BlueTorch);
                FindHeldWeapon<GhastHeldHalleysInferno>(npc)?.Pulse(10f);
                npc.velocity -= SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 6f; // cannon recoil
            }

            if (timer > 40)
                npc.velocity *= 0.97f;

            if (timer >= 150)
                RotateAttack(npc, AttackState.HalleysInferno);
        }

        // 右枢天龙 — 变体A: 每24帧三连蓝火; 变体B: 每36帧五连宽扇. 期间幽魂缓慢绕玩家换角度.
        private void ExecuteAlphaDraconis(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, target.Center + new Vector2(0f, -440f), 18);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldAlphaDraconis>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            int interval = currentVariantB ? 36 : 24;
            int perVolley = currentVariantB ? 5 : 3;
            if (timer >= 30 && timer <= 150 && (timer - 30) % interval == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < perVolley; i++)
                {
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy((i - (perVolley - 1) / 2f) * 0.2f) * 9f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<DraconisFireballProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.5f }, npc.Center);
                GhastFx.Burst(npc.Center, 3f, 6, DustID.BlueTorch);
                FindHeldWeapon<GhastHeldAlphaDraconis>(npc)?.Pulse(6f);
            }

            if (timer > 18)
            {
                weavePhase += 0.02f;
                Vector2 orbit = target.Center + new Vector2(MathF.Cos(weavePhase) * 380f, -340f + MathF.Sin(weavePhase * 1.2f) * 60f);
                SmoothMove(npc, orbit, 0.04f, 8f);
            }

            if (timer >= 180)
                RotateAttack(npc, AttackState.AlphaDraconis);
        }

        // 层云雷暴球 — 变体A: 三球横列; 变体B: 三球三角包夹, 电荷连线换向.
        private void ExecuteStratusSphere(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, DirectedHoverSpot(npc, target, 380f, -260f, 0f), 18);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldStratusSphere>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 pos = currentVariantB
                            ? target.Center + (i * MathHelper.TwoPi / 3f - MathHelper.PiOver2).ToRotationVector2() * 300f
                            : target.Center + new Vector2(i * 180f - 180f, -320f);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<StratusCloudProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                    }
                }
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.6f, Pitch = -0.2f }, target.Center);
            }

            if (timer > 18)
                HauntDrift(npc, target, 400f, -240f);

            if (timer >= 200)
                RotateAttack(npc, AttackState.StratusSphere);
        }

        // 天狼超新星 — 变体A: 亮星锁玩家位; 变体B: 双小星前后夹击. (星体自带1秒蓄能, 弹幕层)
        private void ExecuteSirius(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, target.Center + new Vector2(0f, -420f), 18);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldSirius>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer == 24 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (currentVariantB)
                {
                    Vector2 lead = target.velocity.SafeNormalize(Vector2.UnitX) * 260f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + lead, Vector2.Zero, ModContent.ProjectileType<SiriusStarProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center - lead, Vector2.Zero, ModContent.ProjectileType<SiriusStarProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                }
                else
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<SiriusStarProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f }, target.Center);
                FindHeldWeapon<GhastHeldSirius>(npc)?.Pulse(10f);
            }

            if (timer > 18)
                HauntDrift(npc, target, 380f, -280f);

            if (timer >= 150)
                RotateAttack(npc, AttackState.Sirius);
        }

        // 战月重拳 — 变体A: 单拳锁X; 变体B: 双拳左右错拍砸落, 冲击波交叠.
        private void ExecuteWarloksMoon(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, DirectedHoverSpot(npc, target, 340f, -300f, 0f), 18);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldWarloksMoon>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            int[] fistTimes = currentVariantB ? new[] { 30, 62 } : new[] { 30 };
            foreach (int ft in fistTimes)
            {
                if (timer == ft && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float xOff = currentVariantB ? (ft == fistTimes[0] ? -170f : 170f) : 0f;
                    Vector2 spawn = target.Center + new Vector2(xOff, -420f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(0f, 15f), ModContent.ProjectileType<MoonFistProj>(), npc.defDamage / 3, 0f, Main.myPlayer, 28f);
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.4f }, spawn);
                    FindHeldWeapon<GhastHeldWarloksMoon>(npc)?.Pulse(-14f);
                }
            }

            if (timer > 18)
                HauntDrift(npc, target, 360f, -260f);

            if (timer >= 170)
                RotateAttack(npc, AttackState.WarloksMoon);
        }

        // 织女星光网 — 变体A: 光网锁玩家; 变体B: 光网镇结界中心 + 两轮怨火压制.
        private void ExecuteVega(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, target.Center + new Vector2(0f, -400f), 18);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<GhastHeldVega>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                    Vector2 netPos = currentVariantB ? arenaCenter : target.Center;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), netPos, Vector2.Zero, ModContent.ProjectileType<VegaLightNetProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.7f }, target.Center);
            }

            if (currentVariantB && (timer == 70 || timer == 130) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = -1; i <= 1; i++)
                {
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy(i * 0.16f) * 11f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<GhostFireProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.6f }, npc.Center);
                GhastFx.Burst(npc.Center, 3f, 8);
            }

            if (timer > 18)
                HauntDrift(npc, target, 380f, -240f);

            if (timer >= 200)
                RotateAttack(npc, AttackState.Vega);
        }

        // 形态转变 (50%): 地牢坍缩 — 石甲崩解, 结界收缩至900. 白闪保留, 加砖屑喷发与收缩尘埃波.
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

            // Stone shards spall off the collapsing ghost-armor
            if (timer > 10 && timer < 70 && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(70f, 70f), DustID.Stone, Main.rand.NextVector2CircularEdge(3f, 3f) - Vector2.UnitY, 80, default, 1.3f);
                d.fadeIn = 1.1f;
            }

            if (timer == 45)
            {
                GhastFx.Burst(npc.Center, 7f, 30);
                SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.8f, Pitch = -0.4f }, npc.Center);
                // Cage contraction wave: the old border collapses visibly toward the new 900 line
                for (int i = 0; i < 44; i++)
                {
                    float a = i * MathHelper.TwoPi / 44f;
                    Vector2 pos = arenaCenter + a.ToRotationVector2() * 700f;
                    Dust d = Dust.NewDustPerfect(pos, DustID.PinkTorch, -a.ToRotationVector2() * 5.5f, 100, GhostPink, 1.4f);
                    d.noGravity = true;
                }
            }

            if (timer >= 90)
            {
                npc.dontTakeDamage = false;
                transitionFlashAlpha = 0f;
                attackCycleIndex = 0;
                AttackState next = P2Cycle[0];
                currentVariantB = UseVariantB(next);
                npc.ai[1] = (float)next;
                npc.ai[2] = 0;
                npc.ai[3] = 0;
                npc.netUpdate = true;
            }
        }
        #endregion

        #region Death Animation
        // 虚无回收 — 五段演出,呼应本战的分身/瞬移/石墙身份而不是通用爆炸:
        // 幻象崩解(残余分身被吸回本体) -> 虚空回音闪烁(急速微瞬移) -> 石墙回响坍塌 -> 灵魂虹吸内爆 -> 终末虚无爆发.
        private void BeginDeathAnimation(NPC npc, Player target)
        {
            npc.ai[1] = (float)AttackState.DeathAnimation;
            npc.ai[2] = 0f;
            npc.ai[3] = 0f;
            stunTimer = 0;
            blinkDuration = 0;
            hateCloneHP = 0f;
            fearCloneHP = 0f;
            CleanupHeldWeapons(npc);
            npc.netUpdate = true;

            TriggerDeathCinematic(npc, target, focusStrength: 0.55f, holdFrames: 55, shakePower: 10f);
            SoundEngine.PlaySound(SoundID.Zombie40 with { Volume = 1f, Pitch = -0.5f }, npc.Center);
        }

        private void ExecuteDeathAnimation(NPC npc, Player target, ref float timer)
        {
            npc.damage = 0;
            npc.dontTakeDamage = true;
            npc.velocity *= 0.92f;

            if (timer < 30f)
            {
                // 幻象崩解 — the two clone anchor points get yanked back into the body, trailing dust behind them
                Vector2 hatePos = ClonePos(npc, 0);
                Vector2 fearPos = ClonePos(npc, 1);
                if ((int)timer % 4 == 0)
                {
                    Dust dh = Dust.NewDustPerfect(hatePos, DustID.PinkTorch, (npc.Center - hatePos) * 0.12f, 100, GhostPink, 1.3f);
                    dh.noGravity = true;
                    Dust df = Dust.NewDustPerfect(fearPos, DustID.BlueTorch, (npc.Center - fearPos) * 0.12f, 100, default, 1.3f);
                    df.noGravity = true;
                }
            }
            else if (timer < 70f)
            {
                // 虚空回音闪烁 — rapid strobing near-invisibility, an echo of the blink identity without going anywhere
                float strobe = (int)(timer - 30f) % 10;
                npc.Opacity = strobe < 4f ? 0.15f : 1f;
                if (strobe == 0f)
                {
                    SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.4f, Pitch = -0.3f + Main.rand.NextFloat(-0.1f, 0.1f) }, npc.Center);
                    GhastFx.Burst(npc.Center, 4f, 10);
                }
            }
            else if (timer < 110f)
            {
                // 石墙回响坍塌 — dungeon stone shards rain inward, same motif as the phase-transition cage collapse
                npc.Opacity = 1f;
                if (Main.rand.NextBool(2))
                {
                    Vector2 around = npc.Center + Main.rand.NextVector2Circular(220f, 220f);
                    Dust d = Dust.NewDustPerfect(around, DustID.Stone, (npc.Center - around) * 0.07f, 80, default, 1.2f);
                    d.fadeIn = 1.1f;
                }
            }
            else if (timer < 150f)
            {
                // 灵魂虹吸内爆 — a spiral of soul-motes accelerates inward, peak of the cinematic pull
                float t = timer - 110f;
                if ((int)t % 3 == 0)
                {
                    float ang = t * 0.35f;
                    Vector2 spawn = npc.Center + ang.ToRotationVector2() * MathHelper.Lerp(260f, 20f, t / 40f);
                    Dust d = Dust.NewDustPerfect(spawn, DustID.PinkTorch, (npc.Center - spawn).SafeNormalize(Vector2.Zero) * MathHelper.Lerp(2f, 9f, t / 40f), 100, GhostPink, 1.4f);
                    d.noGravity = true;
                }
            }
            else
            {
                // 终末虚无爆发 — the actual kill fires once, everything after is the lingering burst
                if (timer == 150f)
                {
                    SoundEngine.PlaySound(SoundID.Zombie39 with { Volume = 1.1f, Pitch = -0.4f }, npc.Center);
                    SoundEngine.PlaySound(SoundID.NPCDeath4, npc.Center);
                    target.Calamity().GeneralScreenShakePower = 13f;
                    GhastFx.Burst(npc.Center, 8f, 40);
                    GhastFx.Burst(npc.Center, 5f, 24, DustID.BlueTorch);
                }

                if (timer >= 172f)
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
            // Spectral afterimages when the ghost moves with intent
            if (npc.velocity.Length() > 7f)
            {
                Texture2D tex = TextureAssets.Npc[npc.type].Value;
                Vector2 origin = npc.frame.Size() * 0.5f;
                for (int i = 1; i < oldPos.Length; i++)
                {
                    int idx = (oldPosIndex - i + oldPos.Length * 2) % oldPos.Length;
                    if (oldPos[idx] == Vector2.Zero) continue;
                    float fade = (1f - i / (float)oldPos.Length) * 0.35f * npc.Opacity;
                    Color ghost = GhostPink * fade;
                    ghost.A = 0;
                    spriteBatch.Draw(tex, oldPos[idx] - screenPos, npc.frame, ghost, npc.rotation, origin, npc.scale * (1f - i * 0.02f), npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
                }
            }

            if (transitionFlashAlpha > 0f)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * transitionFlashAlpha);
            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float borderSize = (int)npc.ai[0] >= 2 ? 900f : 1400f;
            float half = borderSize / 2f;

            // ---- Dungeon cage frame: four brick-gray wall lines ----
            Vector2 tl = arenaCenter + new Vector2(-half, -half);
            Vector2 tr = arenaCenter + new Vector2(half, -half);
            Vector2 bl = arenaCenter + new Vector2(-half, half);
            Vector2 br = arenaCenter + new Vector2(half, half);
            Color frameColor = BrickGray * 0.65f;
            frameColor.A = 0;
            DrawWallLine(spriteBatch, screenPos, tl, tr, frameColor, 5f);
            DrawWallLine(spriteBatch, screenPos, tr, br, frameColor, 5f);
            DrawWallLine(spriteBatch, screenPos, br, bl, frameColor, 5f);
            DrawWallLine(spriteBatch, screenPos, bl, tl, frameColor, 5f);

            // ---- Brick slam: dashed warning line during the scream, then the solid slab ----
            if (activeSlamSide != -1)
            {
                bool warning = slamTimer < 60;
                float depth = warning ? 300f : wallSlamOffset;
                (Vector2 a, Vector2 b) = SlamEdge(borderSize, depth);

                if (warning)
                {
                    // Dashed pulse line at the future slab face (design doc: 1s 虚线警示)
                    float pulse = 0.35f + 0.3f * MathF.Sin(ticksRunning * 0.4f);
                    Color warnColor = Color.Red * pulse;
                    warnColor.A = 0;
                    int dashes = 14;
                    for (int i = 0; i < dashes; i++)
                    {
                        if (i % 2 == 1) continue;
                        Vector2 p1 = Vector2.Lerp(a, b, i / (float)dashes);
                        Vector2 p2 = Vector2.Lerp(a, b, (i + 1) / (float)dashes);
                        DrawWallLine(spriteBatch, screenPos, p1, p2, warnColor, 4f);
                    }
                }
                else if (wallSlamOffset > 4f)
                {
                    // The slab itself: a filled brick panel from the wall to the slam face
                    (Vector2 wa, Vector2 wb) = SlamEdge(borderSize, 0f);
                    Vector2 mid = (a + b + wa + wb) * 0.25f;
                    Vector2 span = b - a;
                    float rot = span.ToRotation();
                    Color slabColor = BrickGray * 0.55f;
                    slabColor.A = 0;
                    spriteBatch.Draw(pixel, mid - screenPos, new Rectangle(0, 0, 1, 1), slabColor, rot, new Vector2(0.5f), new Vector2(span.Length(), wallSlamOffset), SpriteEffects.None, 0f);
                    Color faceColor = Color.Red * 0.7f;
                    faceColor.A = 0;
                    DrawWallLine(spriteBatch, screenPos, a, b, faceColor, 5f);
                }
            }

            // ---- Twin mirror clones: full spectral bodies, not dots ----
            Texture2D tex = TextureAssets.Npc[npc.type].Value;
            Vector2 origin2 = npc.frame.Size() * 0.5f;
            if (hateCloneHP > 0f)
            {
                Vector2 pos = ClonePos(npc, 0);
                float hpScale = MathHelper.Lerp(0.7f, 1f, MathHelper.Clamp(hateCloneHP / 1500f, 0f, 1f));
                Color hateColor = Color.Lerp(new Color(255, 70, 70), Color.White, MathHelper.Clamp(cloneFlash[0], 0f, 1f) * 0.6f) * 0.5f;
                hateColor.A = 0;
                spriteBatch.Draw(tex, pos - screenPos, npc.frame, hateColor, -npc.rotation, origin2, npc.scale * 0.9f * hpScale, SpriteEffects.FlipHorizontally, 0f);
            }
            if (fearCloneHP > 0f)
            {
                Vector2 pos = ClonePos(npc, 1);
                float hpScale = MathHelper.Lerp(0.7f, 1f, MathHelper.Clamp(fearCloneHP / 1500f, 0f, 1f));
                Color fearColor = Color.Lerp(new Color(80, 120, 255), Color.White, MathHelper.Clamp(cloneFlash[1], 0f, 1f) * 0.6f) * 0.5f;
                fearColor.A = 0;
                spriteBatch.Draw(tex, pos - screenPos, npc.frame, fearColor, npc.rotation + MathHelper.PiOver2, origin2, npc.scale * 0.9f * hpScale, SpriteEffects.None, 0f);
            }

            // ---- Stun: guttering halo ----
            if (stunTimer > 0)
            {
                float gutter = 0.25f + 0.25f * MathF.Sin(ticksRunning * 0.3f);
                Color halo = GhostPink * gutter;
                halo.A = 0;
                spriteBatch.Draw(pixel, npc.Center + new Vector2(0f, -84f) - screenPos, new Rectangle(0, 0, 1, 1), halo, 0f, new Vector2(0.5f), new Vector2(84f, 5f), SpriteEffects.None, 0f);
            }
        }

        private (Vector2, Vector2) SlamEdge(float borderSize, float depth)
        {
            float half = borderSize / 2f;
            return activeSlamSide switch
            {
                0 => (arenaCenter + new Vector2(-half + depth, -half), arenaCenter + new Vector2(-half + depth, half)),
                1 => (arenaCenter + new Vector2(half - depth, -half), arenaCenter + new Vector2(half - depth, half)),
                2 => (arenaCenter + new Vector2(-half, -half + depth), arenaCenter + new Vector2(half, -half + depth)),
                _ => (arenaCenter + new Vector2(-half, half - depth), arenaCenter + new Vector2(half, half - depth)),
            };
        }

        private static void DrawWallLine(SpriteBatch spriteBatch, Vector2 screenPos, Vector2 a, Vector2 b, Color color, float width)
        {
            float len = Vector2.Distance(a, b);
            if (len < 1f) return;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, (a + b) * 0.5f - screenPos, new Rectangle(0, 0, 1, 1), color, (b - a).ToRotation(), new Vector2(0.5f), new Vector2(len, width), SpriteEffects.None, 0f);
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
