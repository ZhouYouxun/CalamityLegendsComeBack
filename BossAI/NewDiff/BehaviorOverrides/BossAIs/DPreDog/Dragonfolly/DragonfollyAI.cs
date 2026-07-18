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

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.Dragonfolly
{
    internal sealed class DragonfollyAI : LegendsBossAI
    {
        #region Constants & Configurations
        public override int NPCType => ModContent.NPCType<CalamityMod.NPCs.Bumblebirb.Dragonfolly>();
        public override string BossName => "Dragonfolly";
        public override Color DebugColor => new(240, 200, 20);

        public override int MaxPhaseCount => 2;
        public override float[] PhaseLifeRatios => new[] { 0.50f };
        public override int AttackCycleLength => 120;
        public override float MotionIntensity => 1.4f;
        #endregion

        #region Attack States
        public enum AttackState
        {
            GildedProboscis = 0,
            GoldenEagle = 1,
            RougeSlash = 2,
            DraconicSwarmSigil = 3,
            ThunderboltWrath = 4,
            SonicBoomOverdrive = 5,
            Transition = 6,
            DeathAnimation = 7
        }

        // Only 3 named weapons per phase — half the "at least 6 slots" floor — so every weapon gets two
        // rotation slots. UseVariantB then naturally alternates A/B between those two appearances.
        private static readonly AttackState[] P1Cycle =
        {
            AttackState.GildedProboscis, AttackState.GoldenEagle, AttackState.RougeSlash,
            AttackState.GildedProboscis, AttackState.GoldenEagle, AttackState.RougeSlash,
        };
        private static readonly AttackState[] P2Cycle =
        {
            AttackState.DraconicSwarmSigil, AttackState.ThunderboltWrath, AttackState.SonicBoomOverdrive,
            AttackState.DraconicSwarmSigil, AttackState.ThunderboltWrath, AttackState.SonicBoomOverdrive,
        };
        #endregion

        #region Fields
        private int ticksRunning = 0;
        private int currentRepetition = 0;
        private int attackCycleIndex = 0;
        private readonly Vector2[] oldPositions = new Vector2[14];
        private int oldPositionsIndex;

        private readonly bool[] attackVariant = new bool[7];
        private bool UseVariantB(AttackState state)
        {
            int i = (int)state;
            bool v = attackVariant[i];
            attackVariant[i] = !v;
            return v;
        }

        // Golden Feather Wing Armor
        private float leftWingHP = 2000f;
        private float rightWingHP = 2000f;
        private int stunTimer = 0;
        private int respawnArmorTimer = 0;
        private int wingFxCooldown = 0;

        // Tesla grid — anchored to a slow-drifting arena center so the diagonals are readable geometry,
        // not lines glued to a bird moving at 30px/f.
        private int teslaGridTimer = 0;
        private bool teslaHitThisActivation = false;
        private Vector2 arenaCenter = Vector2.Zero;
        private bool centerSet = false;
        private float transitionFlashAlpha = 0f;

        // Committed-dash state: attacks own their movement; the bird never beelines nonstop.
        private Vector2 dashDir = Vector2.Zero;
        #endregion

        #region Movement Helpers
        // Raptor patrol: circle the prey at altitude instead of beelining into it.
        private void CirclePatrol(NPC npc, Player target, float radius, float speed)
        {
            float orbitAngle = (npc.Center - target.Center).ToRotation() + 0.05f;
            Vector2 orbitSpot = target.Center + orbitAngle.ToRotationVector2() * radius;
            Vector2 desired = SafeNormalize(orbitSpot - npc.Center, Vector2.UnitX) * speed;
            npc.velocity = Vector2.Lerp(npc.velocity, desired, 0.09f);
        }

        // Windup posture: hold a launch point, golden feathers streaming back — the dash announces itself.
        private void DashWindup(NPC npc, Vector2 holdSpot)
        {
            Vector2 desired = (holdSpot - npc.Center) * 0.08f;
            if (desired.Length() > 13f) desired = Vector2.Normalize(desired) * 13f;
            npc.velocity = Vector2.Lerp(npc.velocity, desired, 0.12f);
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(50f, 40f), DustID.GoldFlame, -npc.velocity * 0.2f + Main.rand.NextVector2Circular(1f, 1f), 100, default, 1.2f);
                d.noGravity = true;
            }
        }

        // Committed dash. Broken wings blunt speed and corner control (design doc numbers preserved).
        private void LaunchDash(NPC npc, Player target, float baseSpeed, float lifeRatio, int brokenWings)
        {
            float speedMultiplier = brokenWings == 0 ? 1.4f : (brokenWings == 1 ? 0.65f : 0.4f);
            float speed = Math.Min((baseSpeed + (1f - lifeRatio) * 6f) * speedMultiplier, 30f);
            dashDir = SafeNormalize(target.Center + target.velocity * 7f - npc.Center, Vector2.UnitX);
            npc.velocity = dashDir * speed;
            SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.5f, Pitch = 0.55f }, npc.Center);
            FollyFx.Burst(npc.Center, 5f, 14);
        }

        private void DashRecoveryArc(NPC npc, Player target, int brokenWings)
        {
            float turnRate = 0.035f * (brokenWings == 1 ? 0.5f : 1f);
            Vector2 toTarget = SafeNormalize(target.Center - npc.Center, Vector2.UnitX);
            Vector2 desired = toTarget * MathHelper.Max(npc.velocity.Length() * 0.965f, 8f);
            npc.velocity = Vector2.Lerp(npc.velocity, desired, turnRate);
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
                state = AttackState.GildedProboscis;
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

            if (!centerSet)
            {
                arenaCenter = target.Center;
                centerSet = true;
            }
            arenaCenter = Vector2.Lerp(arenaCenter, target.Center, 0.006f);

            int brokenWings = (leftWingHP <= 0f ? 1 : 0) + (rightWingHP <= 0f ? 1 : 0);
            UpdateTeslaGrid(npc, target, currentPhase, brokenWings);
            UpdateArmorRespawn();
            if (wingFxCooldown > 0) wingFxCooldown--;

            if (stunTimer > 0)
            {
                // Double wing-break: the bird flutters, molting sparks and feathers
                stunTimer--;
                npc.velocity.X *= 0.9f;
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + 0.06f, -2f, 1.8f);
                npc.rotation = npc.velocity.X * 0.05f + MathF.Sin(ticksRunning * 0.3f) * 0.08f;
                npc.damage = 0;
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(60f, 40f), DustID.GoldFlame, new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(1f, 3f)), 100, default, 1.3f);
                    d.noGravity = true;
                }
                if (stunTimer == 0)
                {
                    SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.8f, Pitch = 0.3f }, npc.Center);
                    FollyFx.Burst(npc.Center, 6f, 24);
                }
            }
            else
            {
                npc.damage = npc.defDamage;
            }
            // Movement is owned by each attack below — the constant全局追尾 that ate every dash is gone.
            if (state != AttackState.DeathAnimation)
                npc.rotation = npc.velocity.X * 0.05f;

            if (stunTimer == 0)
            {
                switch (state)
                {
                    case AttackState.GildedProboscis:
                        ExecuteGildedProboscis(npc, target, ref timer, ref tracker, currentPhase);
                        break;
                    case AttackState.GoldenEagle:
                        ExecuteGoldenEagle(npc, target, ref timer, ref tracker, currentPhase);
                        break;
                    case AttackState.RougeSlash:
                        ExecuteRougeSlash(npc, target, ref timer, ref tracker, currentPhase);
                        break;
                    case AttackState.DraconicSwarmSigil:
                        ExecuteDraconicSwarmSigil(npc, target, ref timer, ref tracker, currentPhase);
                        break;
                    case AttackState.ThunderboltWrath:
                        ExecuteThunderboltWrath(npc, target, ref timer, ref tracker, currentPhase);
                        break;
                    case AttackState.SonicBoomOverdrive:
                        ExecuteSonicBoomOverdrive(npc, target, ref timer, ref tracker, currentPhase);
                        break;
                    case AttackState.Transition:
                        ExecuteTransition(npc, target, ref timer, ref tracker, currentPhase);
                        break;
                    case AttackState.DeathAnimation:
                        ExecuteDeathAnimation(npc, target, ref timer);
                        break;
                }
            }

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            ProcessWingHits(npc, player.Center, ref modifiers, item.damage);
            InterceptLethalHit(npc, ref modifiers, (int)AttackState.DeathAnimation, () => BeginDeathAnimation(npc, player));
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            ProcessWingHits(npc, projectile.Center, ref modifiers, projectile.damage);
            InterceptLethalHit(npc, ref modifiers, (int)AttackState.DeathAnimation, () => BeginDeathAnimation(npc, Main.player[projectile.owner]));
        }
        #endregion

        #region Helpers
        private void UpdateTeslaGrid(NPC npc, Player target, int currentPhase, int brokenWings)
        {
            teslaGridTimer++;
            if (teslaGridTimer >= 420)
            {
                teslaGridTimer = 0;
                teslaHitThisActivation = false;
            }

            float borderSize = currentPhase == 1 ? 1200f : 900f;

            if (teslaGridTimer >= 288 && teslaGridTimer < 420)
            {
                int mode = teslaGridTimer - 288;
                if (mode == 0)
                    SoundEngine.PlaySound(SoundID.Item94, npc.Center);

                Vector2 topLeft = arenaCenter + new Vector2(-borderSize / 2f, -borderSize / 2f);
                Vector2 bottomRight = arenaCenter + new Vector2(borderSize / 2f, borderSize / 2f);
                Vector2 topRight = arenaCenter + new Vector2(borderSize / 2f, -borderSize / 2f);
                Vector2 bottomLeft = arenaCenter + new Vector2(-borderSize / 2f, borderSize / 2f);

                // Live sparks crawl the diagonals so the net reads even in peripheral vision
                if (Main.rand.NextBool(2))
                {
                    float lerp = Main.rand.NextFloat();
                    Vector2 onLine = Main.rand.NextBool() ? Vector2.Lerp(topLeft, bottomRight, lerp) : Vector2.Lerp(topRight, bottomLeft, lerp);
                    Dust d = Dust.NewDustPerfect(onLine, DustID.Electric, Main.rand.NextVector2Circular(1.5f, 1.5f), 100, default, mode >= 60 ? 1.2f : 0.8f);
                    d.noGravity = true;
                }

                bool solid = mode >= 60; // 1s flicker (288-348), 1.2s solid (348-420)
                if (solid && !teslaHitThisActivation)
                {
                    // Destroying one wing permanently disables one electrode line (design doc).
                    bool diagonalOneLive = brokenWings < 2;
                    bool diagonalTwoLive = brokenWings == 0 || (brokenWings == 1 && leftWingHP > 0f);
                    bool hit = false;
                    if (diagonalOneLive && Collision.CheckAABBvLineCollision(target.position, target.Size, topLeft, bottomRight))
                        hit = true;
                    if (diagonalTwoLive && Collision.CheckAABBvLineCollision(target.position, target.Size, topRight, bottomLeft))
                        hit = true;

                    if (hit)
                    {
                        teslaHitThisActivation = true;
                        target.AddBuff(BuffID.Electrified, 240); // Tesla Stun — wing charge zeroed for 4s
                        target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 20, 0);
                    }
                }
            }
        }

        private void UpdateArmorRespawn()
        {
            if (leftWingHP <= 0f && rightWingHP <= 0f && stunTimer == 0)
            {
                respawnArmorTimer++;
                if (respawnArmorTimer >= 1200) // 20s regen (design doc)
                {
                    leftWingHP = 2000f;
                    rightWingHP = 2000f;
                    respawnArmorTimer = 0;
                }
            }
        }

        private void ProcessWingHits(NPC npc, Vector2 hitPos, ref NPC.HitModifiers modifiers, int damage)
        {
            if (stunTimer > 0)
            {
                modifiers.FinalDamage *= 1.4f; // both wings broken — 140% damage taken (design doc)
                return;
            }

            int activeCount = 0;
            if (leftWingHP > 0f) activeCount++;
            if (rightWingHP > 0f) activeCount++;
            if (activeCount > 0)
                modifiers.FinalDamage *= 0.20f; // 80% DR while any wing armor survives

            Vector2 leftWingPos = npc.Center + new Vector2(-50f, 0f).RotatedBy(npc.rotation);
            Vector2 rightWingPos = npc.Center + new Vector2(50f, 0f).RotatedBy(npc.rotation);

            if (leftWingHP > 0f && Vector2.Distance(hitPos, leftWingPos) < 50f)
            {
                leftWingHP -= damage;
                if (wingFxCooldown <= 0) { wingFxCooldown = 8; SoundEngine.PlaySound(SoundID.NPCHit5 with { Volume = 0.4f }, leftWingPos); }
                if (leftWingHP <= 0f) { SoundEngine.PlaySound(SoundID.NPCDeath4, leftWingPos); FollyFx.Burst(leftWingPos, 5f, 16); CheckDoubleWingBreak(npc); }
            }
            else if (rightWingHP > 0f && Vector2.Distance(hitPos, rightWingPos) < 50f)
            {
                rightWingHP -= damage;
                if (wingFxCooldown <= 0) { wingFxCooldown = 8; SoundEngine.PlaySound(SoundID.NPCHit5 with { Volume = 0.4f }, rightWingPos); }
                if (rightWingHP <= 0f) { SoundEngine.PlaySound(SoundID.NPCDeath4, rightWingPos); FollyFx.Burst(rightWingPos, 5f, 16); CheckDoubleWingBreak(npc); }
            }
        }

        private void CheckDoubleWingBreak(NPC npc)
        {
            if (leftWingHP <= 0f && rightWingHP <= 0f)
            {
                stunTimer = 360; // 6s stun (design doc)
                npc.velocity = Vector2.Zero;
                SoundEngine.PlaySound(SoundID.NPCHit53, npc.Center);
                FollyFx.Burst(npc.Center, 7f, 30);
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
        private void RotateAttack(NPC npc, int currentPhase, AttackState current)
        {
            if (currentPhase == 1)
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
        // GILDED PROBOSCIS — A: single diagonal dash, 45-degree fan flame burst at impact (documented).
        //                     B: two quick successive dashes, smaller flame bursts each.
        private void ExecuteGildedProboscis(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.GildedProboscis) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<FollyHeldProboscis>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            float lifeRatio = npc.lifeMax <= 0 ? 1f : npc.life / (float)npc.lifeMax;
            int brokenWings = (leftWingHP <= 0f ? 1 : 0) + (rightWingHP <= 0f ? 1 : 0);
            int[] dashTimes = variantB ? new[] { 40, 100 } : new[] { 50 };

            // Windup: hold the diagonal launch perch, feathers streaming — the pierce announces itself
            if (timer < dashTimes[0])
                DashWindup(npc, target.Center + new Vector2(Math.Sign(npc.Center.X - target.Center.X) * 460f, -300f));

            foreach (int dt in dashTimes)
            {
                if (timer == dt)
                {
                    LaunchDash(npc, target, variantB ? 16f : 19f, lifeRatio, brokenWings);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int dmg = variantB ? npc.defDamage / 4 : npc.defDamage / 3;
                        int flames = variantB ? 4 : 6;
                        for (int i = 0; i < flames; i++)
                        {
                            Vector2 vel = dashDir.RotatedBy((i - (flames - 1) / 2f) * (variantB ? 0.12f : 0.15f)) * 6f;
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<ProboscisFlameProj>(), dmg, 0f, Main.myPlayer);
                        }
                    }
                    FindHeldWeapon<FollyHeldProboscis>(npc)?.Pulse(variantB ? 14f : 18f);
                }
            }

            // Between/after dashes: bank away and bleed speed, then re-perch for the second pass
            int lastDash = dashTimes[dashTimes.Length - 1];
            if (variantB && timer > 40 && timer < 76)
                DashRecoveryArc(npc, target, brokenWings);
            else if (variantB && timer >= 76 && timer < 100)
                DashWindup(npc, target.Center + new Vector2(-Math.Sign(npc.Center.X - target.Center.X) * 460f, -300f));
            else if (timer > lastDash && timer < lastDash + 50)
                DashRecoveryArc(npc, target, brokenWings);
            else if (timer >= lastDash + 50)
                CirclePatrol(npc, target, 380f, 12f);

            if (timer >= 180)
                RotateAttack(npc, phase, AttackState.GildedProboscis);
        }

        // GOLDEN EAGLE — A: hover, dual crossbows spray in a curving wing-shaped spread (documented).
        //                B: point-blank straight barrage, no curve — forces lateral dodging instead of a safe pocket.
        private void ExecuteGoldenEagle(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.GoldenEagle) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<FollyHeldGoldenEagle>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            Vector2 hover = DirectedHoverSpot(npc, target, 500f, -100f, 8f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.06f, 0.11f);
            FindHeldWeapon<FollyHeldGoldenEagle>(npc)?.SetAim((target.Center - npc.Center).ToRotation());

            if (timer >= 40 && timer <= 160 && timer % 6 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                float side = (timer / 6) % 2 == 0 ? 1f : -1f;
                int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 13f, ModContent.ProjectileType<GoldenEagleBoltProj>(), dmg, 0f, Main.myPlayer, variantB ? 0f : 1f, side);
                FindHeldWeapon<FollyHeldGoldenEagle>(npc)?.Pulse(4f);
            }

            if (timer >= 200)
                RotateAttack(npc, phase, AttackState.GoldenEagle);
        }

        // ROUGE SLASH — A: 3 ascending-size slashes, last one huge and slow (documented).
        //               B: 3 slashes launched simultaneously from 3 converging angles.
        private void ExecuteRougeSlash(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.RougeSlash) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<FollyHeldRougeSlash>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            // Settle at a slashing perch; feathers gather along the blade before each swing
            if (timer < 40)
            {
                DashWindup(npc, DirectedHoverSpot(npc, target, 340f, -180f, 6f));
            }
            else
            {
                npc.velocity *= 0.95f; // rooted through the combo — each slash pushes the body back instead
            }

            if (!variantB)
            {
                if ((timer == 40 || timer == 70 || timer == 100) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int dmg = npc.defDamage / 3;
                    int wave = timer == 40 ? 0 : (timer == 70 ? 1 : 2);
                    float scale = 1f + wave * 0.8f;
                    float speed = 14f - wave * 4f;
                    Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * speed, ModContent.ProjectileType<RougeSlashProj>(), dmg, 0f, Main.myPlayer);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = scale;
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.7f, Pitch = 0.2f - wave * 0.15f }, npc.Center);
                    FollyFx.Burst(npc.Center + dir * 50f, 4f, 8);
                    npc.velocity -= dir * (3f + wave * 2f); // bigger slash, bigger kick
                }
            }
            else
            {
                if (timer == 50 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int dmg = npc.defDamage / 3;
                    foreach (float spread in new float[] { -0.3f, 0f, 0.3f })
                    {
                        Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy(spread);
                        int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 10f, ModContent.ProjectileType<RougeSlashProj>(), dmg, 0f, Main.myPlayer);
                        if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = 1.5f;
                    }
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, Pitch = -0.1f }, npc.Center);
                    FollyFx.Burst(npc.Center, 5f, 12);
                    npc.velocity -= SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 6f;
                }
            }

            if (timer >= 190)
                RotateAttack(npc, phase, AttackState.RougeSlash);
        }
        #endregion

        #region P2 Attacks
        // DRACONIC SWARM SIGIL — A: 4 drones crisscross, tesla trails (documented). B: 2 drones, denser double trail.
        private void ExecuteDraconicSwarmSigil(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
                tracker = UseVariantB(AttackState.DraconicSwarmSigil) ? 1f : 0f;
            bool variantB = tracker != 0f;

            CirclePatrol(npc, target, 420f, 13f);

            // Sigil condensation: golden motes gather on the drone entry ring before they scream in
            if (timer > 24 && timer < 40 && Main.rand.NextBool(2))
            {
                float warnAng = Main.rand.NextFloat(MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(target.Center + warnAng.ToRotationVector2() * 500f, DustID.GoldFlame, -warnAng.ToRotationVector2() * 2f, 100, default, 1.3f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath10 with { Pitch = 0.4f }, npc.Center);
                int dmg = npc.damage / 3;
                int count = variantB ? 2 : 4;
                for (int i = 0; i < count; i++)
                {
                    float angle = i * MathHelper.TwoPi / count + Main.rand.NextFloat(0.3f);
                    Vector2 spawn = target.Center + angle.ToRotationVector2() * 500f;
                    Vector2 vel = (target.Center - spawn).SafeNormalize(Vector2.UnitY) * 11f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, vel, ModContent.ProjectileType<DraconicDroneProj>(), dmg, 0f, Main.myPlayer);
                }
            }

            if (timer >= 200)
                RotateAttack(npc, phase, AttackState.DraconicSwarmSigil);
        }

        // THUNDERBOLT WRATH — A: single bolt -> waterfall wall (documented). B: twin bolts, pincer waterfalls.
        private void ExecuteThunderboltWrath(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
                tracker = UseVariantB(AttackState.ThunderboltWrath) ? 1f : 0f;
            bool variantB = tracker != 0f;

            CirclePatrol(npc, target, 400f, 12f);

            // Static crackle above the drop lanes before the bolts fall
            if (timer > 22 && timer < 40 && Main.rand.NextBool(2))
            {
                float[] lanes = variantB ? new[] { -260f, 260f } : new[] { 0f };
                foreach (float xOff in lanes)
                {
                    Dust d = Dust.NewDustPerfect(target.Center + new Vector2(xOff + Main.rand.NextFloat(-30f, 30f), -420f), DustID.Electric, new Vector2(0f, Main.rand.NextFloat(1f, 3f)), 100, default, 1f);
                    d.noGravity = true;
                }
            }

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.7f }, target.Center);
                if (!variantB)
                {
                    Vector2 spawn = target.Center + new Vector2(0f, -420f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(0f, 16f), ModContent.ProjectileType<ThunderboltArrowProj>(), dmg, 0f, Main.myPlayer);
                }
                else
                {
                    foreach (float xOff in new float[] { -260f, 260f })
                    {
                        Vector2 spawn = target.Center + new Vector2(xOff, -420f);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(0f, 16f), ModContent.ProjectileType<ThunderboltArrowProj>(), dmg, 0f, Main.myPlayer);
                    }
                }
            }

            if (timer >= 200)
                RotateAttack(npc, phase, AttackState.ThunderboltWrath);
        }

        // SONIC BOOM OVERDRIVE — A: teleport to center, continuous expanding rings (documented).
        //                        B: rings breathe (expand/contract) instead of pure outward growth.
        private void ExecuteSonicBoomOverdrive(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
                tracker = UseVariantB(AttackState.SonicBoomOverdrive) ? 1f : 0f;
            bool variantB = tracker != 0f;

            // Telegraphed arrival: golden dust converges on the epicenter (offset above the player, never
            // ON them — the old version hard-teleported onto the player's pixel with contact damage live)
            Vector2 epicenter = target.Center + new Vector2(0f, -220f);
            if (timer < 20)
            {
                npc.velocity *= 0.9f;
                npc.damage = 0;
                npc.Opacity = MathHelper.Lerp(1f, 0.2f, timer / 20f);
                for (int i = 0; i < 2; i++)
                {
                    Vector2 around = epicenter + Main.rand.NextVector2CircularEdge(90f, 90f);
                    Dust d = Dust.NewDustPerfect(around, DustID.GoldFlame, (epicenter - around) * 0.09f, 100, default, 1.3f);
                    d.fadeIn = 1.2f;
                    d.noGravity = true;
                }
            }
            if (timer == 20)
            {
                npc.Center = epicenter;
                npc.velocity = Vector2.Zero;
                npc.Opacity = 1f;
                SoundEngine.PlaySound(SoundID.Item68, npc.Center);
                FollyFx.Burst(npc.Center, 6f, 20);
            }
            if (timer > 20)
                npc.velocity *= 0.92f; // the bird hangs in the boom's eye

            if (timer >= 40 && timer <= 180 && timer % 20 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 4;
                int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<SonicBoomRingProj>(), dmg, 0f, Main.myPlayer);
                if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = variantB ? 1f : 0f;
                SoundEngine.PlaySound(SoundID.Item66 with { Volume = 0.4f, Pitch = -0.3f }, npc.Center);
            }

            if (timer >= 200)
                RotateAttack(npc, phase, AttackState.SonicBoomOverdrive);
        }

        private void ExecuteTransition(NPC npc, Player target, ref float timer, ref float tracker, int phase)
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
                FollyFx.Burst(npc.Center, 7f, 30);

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

        #region Death Animation
        // 金羽陨落 — 五段演出, 让金羽/雷网身份成为演出主角, 而不是通用爆炸:
        // 金羽剥离 -> 残翼癫狂振翅 -> 雷网回响震颤 -> 雷霆凝聚振翅上腾 -> 终末雷光爆发.
        private void BeginDeathAnimation(NPC npc, Player target)
        {
            npc.ai[1] = (float)AttackState.DeathAnimation;
            npc.ai[2] = 0f;
            npc.ai[3] = 0f;
            stunTimer = 0;
            npc.netUpdate = true;

            TriggerDeathCinematic(npc, target, focusStrength: 0.55f, holdFrames: 55, shakePower: 10f);
            SoundEngine.PlaySound(SoundID.Roar with { Volume = 1f, Pitch = 0.4f }, npc.Center);
        }

        private void ExecuteDeathAnimation(NPC npc, Player target, ref float timer)
        {
            npc.damage = 0;
            npc.dontTakeDamage = true;

            if (timer < 25f)
            {
                // 金羽剥离 — the same feather-shedding visual as the phase transition, this time for good
                npc.velocity *= 0.9f;
                npc.rotation += MathF.Sin(timer * 1.2f) * 0.1f;
                if ((int)timer % 2 == 0)
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(60f, 40f), DustID.GoldFlame, Main.rand.NextVector2Circular(3f, 3f), 100, default, 1.3f);
                    d.noGravity = true;
                }
            }
            else if (timer < 70f)
            {
                // 残翼癫狂振翅 — the broken-wing flutter identity gone fully erratic, no recovery left in it
                npc.velocity += Main.rand.NextVector2Circular(0.7f, 0.7f);
                npc.velocity *= 0.9f;
                npc.rotation += MathF.Sin(timer * 0.6f) * 0.15f;
                if ((int)timer % 3 == 0)
                    FollyFx.Burst(npc.Center, 4f, 8);
            }
            else if (timer < 105f)
            {
                // 雷网回响震颤 — the Tesla-grid identity crawls across the body itself as a last discharge
                float t = timer - 70f;
                if ((int)t % 2 == 0)
                {
                    Vector2 spawn = npc.Center + Main.rand.NextVector2Circular(90f, 60f);
                    Dust d = Dust.NewDustPerfect(spawn, DustID.Electric, (npc.Center - spawn) * 0.05f, 100, default, 1.3f);
                    d.noGravity = true;
                }
            }
            else if (timer < 140f)
            {
                // 雷霆凝聚振翅上腾 — rises while a final thunderbolt gathers, the cinematic pull peaks here
                npc.velocity = Vector2.Lerp(npc.velocity, new Vector2(0f, -7f), 0.06f);
                if ((int)timer % 2 == 0)
                {
                    Vector2 spawn = npc.Center + Main.rand.NextVector2CircularEdge(100f, 100f);
                    Dust d = Dust.NewDustPerfect(spawn, DustID.GoldFlame, (npc.Center - spawn) * 0.07f, 100, default, 1.3f);
                    d.noGravity = true;
                }
            }
            else
            {
                // 终末雷光爆发 — the actual kill fires once, everything after is the lingering burst
                if (timer == 140f)
                {
                    npc.velocity = Vector2.Zero;
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1.1f, Pitch = 0.2f }, npc.Center);
                    SoundEngine.PlaySound(SoundID.NPCDeath4, npc.Center);
                    target.Calamity().GeneralScreenShakePower = 13f;
                    FollyFx.Burst(npc.Center, 8f, 40);
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
                float alpha = (1f - i / (float)oldPositions.Length) * 0.5f;
                spriteBatch.Draw(tex, oldPositions[idx] - screenPos, frame, new Color(255, 210, 60, 0) * alpha, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            }

            if (transitionFlashAlpha > 0f)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * transitionFlashAlpha);

            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            // ---- Tesla grid diagonals: thin flicker during the warning, thick blazing lines when live ----
            if (teslaGridTimer >= 288 && teslaGridTimer < 420)
            {
                int currentPhase = (int)npc.ai[0];
                float borderSize = currentPhase == 1 ? 1200f : 900f;
                float half = borderSize / 2f;
                int brokenWings = (leftWingHP <= 0f ? 1 : 0) + (rightWingHP <= 0f ? 1 : 0);
                bool solid = teslaGridTimer - 288 >= 60;

                bool diagonalOneLive = brokenWings < 2;
                bool diagonalTwoLive = brokenWings == 0 || (brokenWings == 1 && leftWingHP > 0f);

                float width = solid ? 6f : 1.5f + 1.5f * MathF.Sin(ticksRunning * 0.6f);
                Color lineColor = (solid ? Color.Yellow : Color.Gold * 0.6f);
                lineColor.A = 0;

                Vector2 tl = arenaCenter + new Vector2(-half, -half);
                Vector2 br = arenaCenter + new Vector2(half, half);
                Vector2 tr = arenaCenter + new Vector2(half, -half);
                Vector2 bl = arenaCenter + new Vector2(-half, half);
                if (diagonalOneLive)
                    spriteBatch.Draw(pixel, (tl + br) * 0.5f - screenPos, new Rectangle(0, 0, 1, 1), lineColor * 0.8f, (br - tl).ToRotation(), new Vector2(0.5f), new Vector2(Vector2.Distance(tl, br), width), SpriteEffects.None, 0f);
                if (diagonalTwoLive)
                    spriteBatch.Draw(pixel, (tr + bl) * 0.5f - screenPos, new Rectangle(0, 0, 1, 1), lineColor * 0.8f, (bl - tr).ToRotation(), new Vector2(0.5f), new Vector2(Vector2.Distance(tr, bl), width), SpriteEffects.None, 0f);
            }

            // ---- Wing armor: HP-scaled golden feather plates ----
            Vector2 leftWingPos = npc.Center + new Vector2(-50f, 0f).RotatedBy(npc.rotation);
            Vector2 rightWingPos = npc.Center + new Vector2(50f, 0f).RotatedBy(npc.rotation);

            if (leftWingHP > 0f)
            {
                float hpScale = MathHelper.Lerp(0.55f, 1f, MathHelper.Clamp(leftWingHP / 2000f, 0f, 1f));
                Color gold = Color.Gold; gold.A = 0;
                spriteBatch.Draw(pixel, leftWingPos - screenPos, new Rectangle(0, 0, 1, 1), gold * 0.8f, ticksRunning * 0.05f, new Vector2(0.5f), new Vector2(30f, 30f) * hpScale, SpriteEffects.None, 0f);
                spriteBatch.Draw(pixel, leftWingPos - screenPos, new Rectangle(0, 0, 1, 1), gold * 0.5f, ticksRunning * 0.05f + MathHelper.PiOver4, new Vector2(0.5f), new Vector2(20f, 20f) * hpScale, SpriteEffects.None, 0f);
            }
            if (rightWingHP > 0f)
            {
                float hpScale = MathHelper.Lerp(0.55f, 1f, MathHelper.Clamp(rightWingHP / 2000f, 0f, 1f));
                Color gold = Color.Gold; gold.A = 0;
                spriteBatch.Draw(pixel, rightWingPos - screenPos, new Rectangle(0, 0, 1, 1), gold * 0.8f, -ticksRunning * 0.05f, new Vector2(0.5f), new Vector2(30f, 30f) * hpScale, SpriteEffects.None, 0f);
                spriteBatch.Draw(pixel, rightWingPos - screenPos, new Rectangle(0, 0, 1, 1), gold * 0.5f, -ticksRunning * 0.05f + MathHelper.PiOver4, new Vector2(0.5f), new Vector2(20f, 20f) * hpScale, SpriteEffects.None, 0f);
            }

            // ---- Stun: drooping halo over the molting bird ----
            if (stunTimer > 0)
            {
                float sag = 0.3f + 0.2f * MathF.Sin(ticksRunning * 0.14f);
                Color halo = Color.Gold * sag;
                halo.A = 0;
                spriteBatch.Draw(pixel, npc.Center + new Vector2(0f, -70f) - screenPos, new Rectangle(0, 0, 1, 1), halo, 0f, new Vector2(0.5f), new Vector2(76f, 5f), SpriteEffects.None, 0f);
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
