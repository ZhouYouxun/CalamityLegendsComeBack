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

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.Providence
{
    internal sealed class ProvidenceAI : IUMWBossAI
    {
        #region Constants & Configurations
        public override int NPCType => ModContent.Find<ModNPC>("CalamityMod/Providence").Type;
        public override string BossName => "Providence";
        public override Color DebugColor => new(255, 220, 60);

        // Design doc specifies a single 50% HP unseal, not a 3-phase ladder.
        public override int MaxPhaseCount => 2;
        public override float[] PhaseLifeRatios => new[] { 0.50f };
        public override int AttackCycleLength => 120;
        public override float MotionIntensity => 0.9f;
        #endregion

        #region Attack States
        public enum AttackState
        {
            HolyCollider = 0,
            BurningRevelation = 1,
            TelluricGlare = 2,
            BlissfulBombardier = 3,
            PurgeGuzzler = 4,
            DazzlingStabber = 5,
            MoltenAmputator = 6,
            PristineFury = 7,
            AetherfluxCannon = 8,
            AngelicShotgun = 9,
            DarkSpark = 10,
            GalactusBlade = 11,
            MirrorOfKalandra = 12,
            Mourningstar = 13,
            ShatteredDawn = 14,
            SeekingScorcher = 15,
            Maelstrom = 16,
            Prince = 17,
            Transition = 18,
        }

        private static readonly AttackState[] P1Cycle =
        {
            AttackState.HolyCollider, AttackState.BurningRevelation, AttackState.TelluricGlare,
            AttackState.BlissfulBombardier, AttackState.PurgeGuzzler, AttackState.DazzlingStabber,
            AttackState.MoltenAmputator, AttackState.PristineFury,
        };
        // Design doc pairs these into 5 combo-rounds, but (matching the precedent set for Astrum
        // Aureus/Ravager P2) each of the 10 named weapons gets its own independent rotation slot —
        // already well past the 6-slot floor, so no padding is needed here.
        private static readonly AttackState[] P2Cycle =
        {
            AttackState.AetherfluxCannon, AttackState.AngelicShotgun, AttackState.DarkSpark, AttackState.GalactusBlade,
            AttackState.MirrorOfKalandra, AttackState.Mourningstar, AttackState.ShatteredDawn,
            AttackState.SeekingScorcher, AttackState.Maelstrom, AttackState.Prince,
        };
        #endregion

        #region Fields
        private int ticksRunning = 0;
        private int currentRepetition = 0;
        private int attackCycleIndex = 0;
        private readonly Vector2[] oldPositions = new Vector2[14];
        private int oldPositionsIndex;

        // Sacred Tri-Source Crystals
        private float yellowCrystalHP = 800f;
        private float orangeCrystalHP = 800f;
        private float purpleCrystalHP = 800f;
        private int stunTimer = 0;
        private int respawnCrystalsTimer = 0;
        private int crystalFxCooldown = 0;

        private int refractionTimer = 0;
        private bool refractionHitThisActivation = false;
        private int arenaHurtCooldown = 0;
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
            ref float stateTracker = ref npc.ai[3];

            if (currentPhase == 0)
            {
                currentPhase = 1;
                npc.ai[0] = 1f;
                state = AttackState.HolyCollider;
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
                stateTracker = 0;
                npc.netUpdate = true;
            }

            float borderSize = currentPhase == 1 ? 1500f : 1100f;
            if (arenaHurtCooldown > 0) arenaHurtCooldown--;
            Vector2 dist = target.Center - npc.Center;
            if (Math.Abs(dist.X) > borderSize / 2f || Math.Abs(dist.Y) > borderSize / 2f)
            {
                target.AddBuff(BuffID.Daybreak, 180);
                target.AddBuff(BuffID.BrokenArmor, 120);
                if (arenaHurtCooldown <= 0)
                {
                    arenaHurtCooldown = 30;
                    target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 25, 0);
                }
            }

            UpdateRefractionLaser(npc, target, borderSize);
            UpdateCrystalsRespawn();
            if (crystalFxCooldown > 0) crystalFxCooldown--;

            if (stunTimer > 0)
            {
                stunTimer--;
                npc.velocity *= 0.85f;
            }
            else
            {
                float speed = 9f + (1f - lifeRatio) * 5f;
                Vector2 desiredPos = target.Center + new Vector2((float)Math.Sin(ticksRunning * 0.04f) * 200f, -220f);
                Vector2 desiredVel = (desiredPos - npc.Center) * 0.04f;
                if (desiredVel.Length() > speed) desiredVel = SafeNormalize(desiredVel, Vector2.Zero) * speed;
                npc.velocity = Vector2.Lerp(npc.velocity, desiredVel, 0.12f);
            }
            npc.rotation = npc.velocity.X * 0.03f;

            if (stunTimer == 0)
            {
                switch (state)
                {
                    case AttackState.HolyCollider: ExecuteHolyCollider(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.BurningRevelation: ExecuteBurningRevelation(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.TelluricGlare: ExecuteTelluricGlare(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.BlissfulBombardier: ExecuteBlissfulBombardier(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.PurgeGuzzler: ExecutePurgeGuzzler(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.DazzlingStabber: ExecuteDazzlingStabber(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.MoltenAmputator: ExecuteMoltenAmputator(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.PristineFury: ExecutePristineFury(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.AetherfluxCannon: ExecuteAetherfluxCannon(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.AngelicShotgun: ExecuteAngelicShotgun(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.DarkSpark: ExecuteDarkSpark(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.GalactusBlade: ExecuteGalactusBlade(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.MirrorOfKalandra: ExecuteMirrorOfKalandra(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.Mourningstar: ExecuteMourningstar(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.ShatteredDawn: ExecuteShatteredDawn(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.SeekingScorcher: ExecuteSeekingScorcher(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.Maelstrom: ExecuteMaelstrom(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.Prince: ExecutePrince(npc, target, ref timer, ref stateTracker, currentPhase); break;
                    case AttackState.Transition: ExecuteTransition(npc, target, ref timer, ref stateTracker, currentPhase); break;
                }
            }

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) => ProcessCrystalHits(npc, player.Center, ref modifiers, item.damage);
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) => ProcessCrystalHits(npc, projectile.Center, ref modifiers, projectile.damage);
        #endregion

        #region Refraction & Crystals Logic
        private void UpdateRefractionLaser(NPC npc, Player target, float borderSize)
        {
            refractionTimer++;
            if (refractionTimer >= 480)
            {
                refractionTimer = 0;
                refractionHitThisActivation = false;
            }

            if (refractionTimer >= 420 && refractionTimer < 480)
            {
                int mode = refractionTimer - 420;
                if (mode == 0)
                    SoundEngine.PlaySound(SoundID.Item60, npc.Center);

                Vector2 topLeft = npc.Center + new Vector2(-borderSize / 2f, -borderSize / 2f);
                Vector2 topRight = npc.Center + new Vector2(borderSize / 2f, -borderSize / 2f);
                Vector2 bottomLeft = npc.Center + new Vector2(-borderSize / 2f, borderSize / 2f);
                Vector2 bottomRight = npc.Center + new Vector2(borderSize / 2f, borderSize / 2f);

                if (mode >= 45 && !refractionHitThisActivation)
                {
                    if (Collision.CheckAABBvLineCollision(target.position, target.Size, topLeft, bottomRight) ||
                        Collision.CheckAABBvLineCollision(target.position, target.Size, topRight, bottomLeft))
                    {
                        refractionHitThisActivation = true;
                        target.AddBuff(BuffID.Daybreak, 180); // Profaned Weakness: defense to 0 for 3s
                        target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 30, 0);
                    }
                }
            }
        }

        private void UpdateCrystalsRespawn()
        {
            if (yellowCrystalHP <= 0f && orangeCrystalHP <= 0f && purpleCrystalHP <= 0f && stunTimer == 0)
            {
                respawnCrystalsTimer++;
                if (respawnCrystalsTimer >= 1500) // 25s respawn (design doc)
                {
                    yellowCrystalHP = 800f;
                    orangeCrystalHP = 800f;
                    purpleCrystalHP = 800f;
                    respawnCrystalsTimer = 0;
                }
            }
        }

        private void ProcessCrystalHits(NPC npc, Vector2 hitPos, ref NPC.HitModifiers modifiers, int damage)
        {
            if (stunTimer > 0)
            {
                modifiers.FinalDamage *= 1.5f; // all crystals broken — 150% damage taken (design doc)
                return;
            }

            Vector2 yellowPos = npc.Center + (ticksRunning * 0.03f).ToRotationVector2() * 120f;
            Vector2 orangePos = npc.Center + (ticksRunning * 0.03f + MathHelper.TwoPi / 3f).ToRotationVector2() * 120f;
            Vector2 purplePos = npc.Center + (ticksRunning * 0.03f + 2f * MathHelper.TwoPi / 3f).ToRotationVector2() * 120f;

            int activeCount = 0;
            if (yellowCrystalHP > 0f) activeCount++;
            if (orangeCrystalHP > 0f) activeCount++;
            if (purpleCrystalHP > 0f) activeCount++;
            if (activeCount > 0)
                modifiers.FinalDamage *= (1f - 0.30f * activeCount); // up to 90% DR

            if (Vector2.Distance(hitPos, yellowPos) < 80f && yellowCrystalHP > 0f)
            {
                yellowCrystalHP -= damage;
                if (crystalFxCooldown <= 0) { crystalFxCooldown = 8; SoundEngine.PlaySound(SoundID.NPCHit5 with { Volume = 0.4f }, yellowPos); }
                if (yellowCrystalHP <= 0f) { SoundEngine.PlaySound(SoundID.NPCDeath4, yellowPos); ProvFx.Burst(yellowPos, 5f, 16); CheckAllCrystalsBroken(npc); }
            }
            else if (Vector2.Distance(hitPos, orangePos) < 80f && orangeCrystalHP > 0f)
            {
                orangeCrystalHP -= damage;
                if (crystalFxCooldown <= 0) { crystalFxCooldown = 8; SoundEngine.PlaySound(SoundID.NPCHit5 with { Volume = 0.4f }, orangePos); }
                if (orangeCrystalHP <= 0f) { SoundEngine.PlaySound(SoundID.NPCDeath4, orangePos); ProvFx.Burst(orangePos, 5f, 16, DustID.Torch); CheckAllCrystalsBroken(npc); }
            }
            else if (Vector2.Distance(hitPos, purplePos) < 80f && purpleCrystalHP > 0f)
            {
                purpleCrystalHP -= damage;
                if (crystalFxCooldown <= 0) { crystalFxCooldown = 8; SoundEngine.PlaySound(SoundID.NPCHit5 with { Volume = 0.4f }, purplePos); }
                if (purpleCrystalHP <= 0f) { SoundEngine.PlaySound(SoundID.NPCDeath4, purplePos); ProvFx.Burst(purplePos, 5f, 16, DustID.PurpleTorch); CheckAllCrystalsBroken(npc); }
            }
        }

        private void CheckAllCrystalsBroken(NPC npc)
        {
            if (yellowCrystalHP <= 0f && orangeCrystalHP <= 0f && purpleCrystalHP <= 0f)
            {
                stunTimer = 480; // 8s stun (design doc)
                npc.velocity = Vector2.Zero;
                SoundEngine.PlaySound(SoundID.NPCHit53, npc.Center);
                ProvFx.Burst(npc.Center, 7f, 30);
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
                AttackState next = P1Cycle[attackCycleIndex % P1Cycle.Length];

                // Crystal-disable skip logic (design doc): Yellow -> TelluricGlare; Orange -> MoltenAmputator
                // + BlissfulBombardier; Purple -> PurgeGuzzler + DazzlingStabber.
                for (int guard = 0; guard < P1Cycle.Length; guard++)
                {
                    bool skip = (next == AttackState.TelluricGlare && yellowCrystalHP <= 0f) ||
                                (next == AttackState.MoltenAmputator && orangeCrystalHP <= 0f) ||
                                (next == AttackState.BlissfulBombardier && orangeCrystalHP <= 0f) ||
                                (next == AttackState.PurgeGuzzler && purpleCrystalHP <= 0f) ||
                                (next == AttackState.DazzlingStabber && purpleCrystalHP <= 0f);
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

        #region P1 Attack States
        private void ExecuteHolyCollider(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldHolyCollider>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 hover = DirectedHoverSpot(npc, target, 0f, -280f, 6f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.06f, 0.12f);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<HolyColliderTrailProj>(), npc.damage / 3, 0f, Main.myPlayer, dir.X, dir.Y);
            }

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.HolyCollider);
        }

        private void ExecuteBurningRevelation(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldBurningRevelation>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 hover = DirectedHoverSpot(npc, target, 300f, -240f, 5f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.05f, 0.1f);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 5f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir, ModContent.ProjectileType<RevelationCoreProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<ProvHeldBurningRevelation>(npc)?.Pulse(-10f);
            }

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.BurningRevelation);
        }

        private void ExecuteTelluricGlare(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<ProvHeldTelluricGlare>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 hover = DirectedHoverSpot(npc, target, 0f, -320f, 0f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.05f, 0.1f);
            FindHeldWeapon<ProvHeldTelluricGlare>(npc)?.SetAim((target.Center - npc.Center).ToRotation());

            if (timer == 20 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 pos = target.Center + new Vector2(0f, i * 100f - 150f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<TelluricBeamProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                FindHeldWeapon<ProvHeldTelluricGlare>(npc)?.Pulse(12f);
            }

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.TelluricGlare);
        }

        private void ExecuteBlissfulBombardier(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<ProvHeldBlissfulBombardier>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 hover = DirectedHoverSpot(npc, target, 280f, -220f, 6f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.06f, 0.12f);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 8f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<BombardierRocketProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<ProvHeldBlissfulBombardier>(npc)?.Pulse(-12f);
            }

            if (timer >= 200)
                RotateAttack(npc, phase, AttackState.BlissfulBombardier);
        }

        private void ExecutePurgeGuzzler(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldPurgeGuzzler>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 hover = DirectedHoverSpot(npc, target, 0f, -300f, 4f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.05f, 0.1f);

            if (timer == 20 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2[] pts = new Vector2[3];
                for (int i = 0; i < 3; i++)
                    pts[i] = target.Center + (i * MathHelper.TwoPi / 3f).ToRotationVector2() * 240f;
                for (int i = 0; i < 3; i++)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pts[i], Vector2.Zero, ModContent.ProjectileType<PurgeCoreProj>(), npc.damage / 3, 0f, Main.myPlayer, target.Center.X, target.Center.Y);
                FindHeldWeapon<ProvHeldPurgeGuzzler>(npc)?.Pulse(10f);
            }

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.PurgeGuzzler);
        }

        private void ExecuteDazzlingStabber(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldDazzlingStabber>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 hover = DirectedHoverSpot(npc, target, 260f, -260f, 6f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.06f, 0.12f);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawn = target.Center + new Vector2(0f, -600f);
                int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(0f, 15f), ModContent.ProjectileType<DazzlingSpearProj>(), npc.damage / 3, 0f, Main.myPlayer);
                if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = target.Center.Y;
                FindHeldWeapon<ProvHeldDazzlingStabber>(npc)?.Pulse(14f);
            }

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.DazzlingStabber);
        }

        private void ExecuteMoltenAmputator(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<ProvHeldMoltenAmputator>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 hover = DirectedHoverSpot(npc, target, 300f, -220f, 6f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.06f, 0.12f);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 11f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<MoltenSickleProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<ProvHeldMoltenAmputator>(npc)?.Pulse(12f);
            }

            if (timer >= 190)
                RotateAttack(npc, phase, AttackState.MoltenAmputator);
        }

        private void ExecutePristineFury(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldPristineFury>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 hover = DirectedHoverSpot(npc, target, 0f, -260f, 4f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.05f, 0.1f);

            if (timer >= 30 && timer <= 130 && timer % 5 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float angle = MathHelper.Lerp(-1.05f, 1.05f, (timer - 30f) / 100f);
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy(angle) * 9f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<PristineFlameProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<ProvHeldPristineFury>(npc)?.Pulse(4f);
            }

            if (timer >= 170)
                RotateAttack(npc, phase, AttackState.PristineFury);
        }
        #endregion

        #region P2 Attack States
        private void ExecuteAetherfluxCannon(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<ProvHeldAetherfluxCannon>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if ((timer == 30 || timer == 55 || timer == 80) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 10f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<AetherfluxLaserProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<ProvHeldAetherfluxCannon>(npc)?.Pulse(10f);
            }

            if (timer >= 140)
                RotateAttack(npc, phase, AttackState.AetherfluxCannon);
        }

        private void ExecuteAngelicShotgun(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<ProvHeldAngelicShotgun>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 baseDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                for (int i = 0; i < 6; i++)
                {
                    Vector2 vel = baseDir.RotatedBy((i - 2.5f) * 0.15f) * 9f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<AngelicPelletProj>(), npc.damage / 3, 0f, Main.myPlayer, 700f, npc.Center.X, npc.Center.Y);
                }
                FindHeldWeapon<ProvHeldAngelicShotgun>(npc)?.Pulse(8f);
            }

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.AngelicShotgun);
        }

        private void ExecuteDarkSpark(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldDarkSpark>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 20 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawn = target.Center + new Vector2(Main.rand.NextFloat(-200f, 200f), -60f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<DarkSparkProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<ProvHeldDarkSpark>(npc)?.Pulse(10f);
            }

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.DarkSpark);
        }

        private void ExecuteGalactusBlade(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldGalactusBlade>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer >= 30 && timer <= 130 && timer % 8 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawn = target.Center + new Vector2(Main.rand.NextFloat(-400f, 400f), -600f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(Main.rand.NextFloat(-2f, 2f), 13f), ModContent.ProjectileType<GalactusMeteorProj>(), npc.damage / 3, 0f, Main.myPlayer);
            }

            if (timer >= 170)
                RotateAttack(npc, phase, AttackState.GalactusBlade);
        }

        private void ExecuteMirrorOfKalandra(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldMirrorOfKalandra>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + new Vector2(0f, 150f), Vector2.Zero, ModContent.ProjectileType<KalandraMirrorProj>(), npc.damage / 3, 0f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item160, npc.Center);
            }

            Vector2 hover = DirectedHoverSpot(npc, target, 0f, -320f, 0f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.04f, 0.09f);

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.MirrorOfKalandra);
        }

        private void ExecuteMourningstar(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldMourningstar>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                foreach (float phaseOffset in new float[] { 0f, MathHelper.Pi })
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<MourningstarLineProj>(), npc.damage / 3, 0f, Main.myPlayer, npc.whoAmI, phaseOffset);
            }

            if (timer >= 180)
                RotateAttack(npc, phase, AttackState.Mourningstar);
        }

        private void ExecuteShatteredDawn(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<ProvHeldShatteredDawn>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 6f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<ShatteredDiscProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<ProvHeldShatteredDawn>(npc)?.Pulse(10f);
            }

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.ShatteredDawn);
        }

        private void ExecuteSeekingScorcher(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldSeekingScorcher>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<SeekingScorcherRingProj>(), npc.damage / 3, 0f, Main.myPlayer, 0f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.SeekingScorcher);
        }

        private void ExecuteMaelstrom(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldMaelstrom>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 hover = DirectedHoverSpot(npc, target, 0f, -300f, 0f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hover - npc.Center) * 0.04f, 0.09f);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<MaelstromVortexProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<ProvHeldMaelstrom>(npc)?.Pulse(10f);
            }

            if (timer >= 180)
                RotateAttack(npc, phase, AttackState.Maelstrom);
        }

        private void ExecutePrince(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldPrince>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int princeType = ModContent.Find<ModNPC>("CalamityMod/ProvidenceGuardianOffensive").Type;
                int p = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y + 120, princeType);
                if (p >= 0 && p < Main.maxNPCs)
                {
                    Main.npc[p].ai[0] = npc.whoAmI;
                    Main.npc[p].netUpdate = true;
                }
                FindHeldWeapon<ProvHeldPrince>(npc)?.Pulse(8f);
            }

            if (timer >= 180)
                RotateAttack(npc, phase, AttackState.Prince);
        }

        private void ExecuteTransition(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            npc.velocity *= 0.8f;
            npc.dontTakeDamage = true;
            transitionFlashAlpha = MathHelper.Clamp(1f - Math.Abs(timer - 22f) / 22f, 0f, 1f);

            if (timer == 1)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
                target.Calamity().GeneralScreenShakePower = 9f;
            }

            if (timer == 45)
                ProvFx.Burst(npc.Center, 8f, 40);

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
                spriteBatch.Draw(tex, oldPositions[idx] - screenPos, frame, new Color(255, 230, 120, 0) * alpha, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            }

            if (transitionFlashAlpha > 0f)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * transitionFlashAlpha);

            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D glowTex = TextureAssets.Dust.Value;
            Rectangle sourceRect = new Rectangle(0, 0, 8, 8);

            Vector2 yellowPos = npc.Center + (ticksRunning * 0.03f).ToRotationVector2() * 120f;
            Vector2 orangePos = npc.Center + (ticksRunning * 0.03f + MathHelper.TwoPi / 3f).ToRotationVector2() * 120f;
            Vector2 purplePos = npc.Center + (ticksRunning * 0.03f + 2f * MathHelper.TwoPi / 3f).ToRotationVector2() * 120f;

            if (yellowCrystalHP > 0f)
                spriteBatch.Draw(glowTex, yellowPos - screenPos, sourceRect, Color.Yellow * 0.8f, 0f, new Vector2(4f, 4f), 4f, SpriteEffects.None, 0f);
            if (orangeCrystalHP > 0f)
                spriteBatch.Draw(glowTex, orangePos - screenPos, sourceRect, Color.Orange * 0.8f, 0f, new Vector2(4f, 4f), 4f, SpriteEffects.None, 0f);
            if (purpleCrystalHP > 0f)
                spriteBatch.Draw(glowTex, purplePos - screenPos, sourceRect, Color.Purple * 0.8f, 0f, new Vector2(4f, 4f), 4f, SpriteEffects.None, 0f);
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
