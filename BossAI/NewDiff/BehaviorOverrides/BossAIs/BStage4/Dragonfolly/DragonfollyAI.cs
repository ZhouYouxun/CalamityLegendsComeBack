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
    internal sealed class DragonfollyAI : IUMWBossAI
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
            Transition = 6
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

        // Tesla grid
        private int teslaGridTimer = 0;
        private bool teslaHitThisActivation = false;
        private float transitionFlashAlpha = 0f;
        #endregion

        #region Core AI Hooks
        public override bool PreAI(NPC npc, IUMWGlobalNPC data)
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

            int brokenWings = (leftWingHP <= 0f ? 1 : 0) + (rightWingHP <= 0f ? 1 : 0);
            UpdateTeslaGrid(npc, target, currentPhase, brokenWings);
            UpdateArmorRespawn();
            if (wingFxCooldown > 0) wingFxCooldown--;

            if (stunTimer > 0)
            {
                stunTimer--;
                npc.velocity *= 0.82f;
            }
            else
            {
                // Doc: both wings intact = max dash speed (30f cap). Each broken wing costs speed and control.
                float speedMultiplier = brokenWings == 0 ? 1f : (brokenWings == 1 ? 0.65f : 0.4f);
                float baseSpeed = currentPhase == 1 ? 15f : 22f;
                float speed = Math.Min((baseSpeed + (1f - lifeRatio) * 6f) * (brokenWings == 0 ? 1.4f : speedMultiplier), 30f);
                float turnSpeed = (0.06f + (1f - lifeRatio) * 0.03f) * (brokenWings == 1 ? 0.5f : 1f); // wider turn radius on single-wing loss

                Vector2 desiredVel = SafeNormalize(target.Center - npc.Center, Vector2.Zero) * speed;
                npc.velocity = Vector2.Lerp(npc.velocity, desiredVel, turnSpeed);
            }
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
                }
            }

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) => ProcessWingHits(npc, player.Center, ref modifiers, item.damage);
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) => ProcessWingHits(npc, projectile.Center, ref modifiers, projectile.damage);
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

                Vector2 topLeft = npc.Center + new Vector2(-borderSize / 2f, -borderSize / 2f);
                Vector2 bottomRight = npc.Center + new Vector2(borderSize / 2f, borderSize / 2f);
                Vector2 topRight = npc.Center + new Vector2(borderSize / 2f, -borderSize / 2f);
                Vector2 bottomLeft = npc.Center + new Vector2(-borderSize / 2f, borderSize / 2f);

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

            if (!variantB)
            {
                if (timer == 50 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int dmg = npc.damage / 3;
                    Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                    npc.velocity = dir * 18f;
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 vel = dir.RotatedBy((i - 2.5f) * 0.15f) * 6f;
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<ProboscisFlameProj>(), dmg, 0f, Main.myPlayer);
                    }
                    FindHeldWeapon<FollyHeldProboscis>(npc)?.Pulse(18f);
                }
            }
            else
            {
                if ((timer == 40 || timer == 100) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int dmg = npc.damage / 4;
                    Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                    npc.velocity = dir * 16f;
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 vel = dir.RotatedBy((i - 1.5f) * 0.12f) * 6f;
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<ProboscisFlameProj>(), dmg, 0f, Main.myPlayer);
                    }
                    FindHeldWeapon<FollyHeldProboscis>(npc)?.Pulse(14f);
                }
            }

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

            if (!variantB)
            {
                if ((timer == 40 || timer == 70 || timer == 100) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int dmg = npc.damage / 3;
                    int wave = timer == 40 ? 0 : (timer == 70 ? 1 : 2);
                    float scale = 1f + wave * 0.8f;
                    float speed = 14f - wave * 4f;
                    Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * speed, ModContent.ProjectileType<RougeSlashProj>(), dmg, 0f, Main.myPlayer);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = scale;
                }
            }
            else
            {
                if (timer == 50 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int dmg = npc.damage / 3;
                    foreach (float spread in new float[] { -0.3f, 0f, 0.3f })
                    {
                        Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy(spread);
                        int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 10f, ModContent.ProjectileType<RougeSlashProj>(), dmg, 0f, Main.myPlayer);
                        if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = 1.5f;
                    }
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

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 3;
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
            {
                tracker = UseVariantB(AttackState.SonicBoomOverdrive) ? 1f : 0f;
                npc.Center = target.Center;
                npc.velocity = Vector2.Zero;
                SoundEngine.PlaySound(SoundID.Item68, npc.Center);
                FollyFx.Burst(npc.Center, 6f, 20);
            }
            bool variantB = tracker != 0f;

            if (timer >= 20 && timer <= 180 && timer % 20 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = npc.damage / 4;
                int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<SonicBoomRingProj>(), dmg, 0f, Main.myPlayer);
                if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = variantB ? 1f : 0f;
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
            Texture2D glowTex = TextureAssets.Dust.Value;
            Rectangle sourceRect = new Rectangle(0, 0, 8, 8);

            Vector2 leftWingPos = npc.Center + new Vector2(-50f, 0f).RotatedBy(npc.rotation);
            Vector2 rightWingPos = npc.Center + new Vector2(50f, 0f).RotatedBy(npc.rotation);

            if (leftWingHP > 0f)
                spriteBatch.Draw(glowTex, leftWingPos - screenPos, sourceRect, Color.Gold * 0.8f, ticksRunning * 0.05f, new Vector2(4f, 4f), 4f, SpriteEffects.None, 0f);
            if (rightWingHP > 0f)
                spriteBatch.Draw(glowTex, rightWingPos - screenPos, sourceRect, Color.Gold * 0.8f, -ticksRunning * 0.05f, new Vector2(4f, 4f), 4f, SpriteEffects.None, 0f);
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
