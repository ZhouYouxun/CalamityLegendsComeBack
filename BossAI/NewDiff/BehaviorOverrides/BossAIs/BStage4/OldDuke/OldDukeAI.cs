using System;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage2.AquaticScourge;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.WeaponAttacks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.OldDuke
{
    // SlitheringEels, SkyfinBombers, SpentFuelContainer and SulphurousGrabber are shared with Aquatic
    // Scourge per the design docs — their projectiles and held-weapon classes are reused directly from
    // the AquaticScourge namespace rather than duplicated.
    internal sealed class OldDukeAI : IUMWBossAI
    {
        #region Constants & Configurations
        public override int NPCType => ModContent.Find<ModNPC>("CalamityMod/OldDuke").Type;
        public override string BossName => "The Old Duke";
        public override Color DebugColor => new(110, 160, 40);

        // Design doc specifies a single 50% HP transition, not the old 3-phase ladder.
        public override int MaxPhaseCount => 2;
        public override float[] PhaseLifeRatios => new[] { 0.50f };
        public override int AttackCycleLength => 120;
        public override float MotionIntensity => 1.3f;
        #endregion

        #region Attack States
        public enum AttackState
        {
            InsidiousImpaler = 0,
            FetidEmesis = 1,
            SepticSkewer = 2,
            VitriolicViper = 3,
            MutatedTruffle = 4,
            CadaverousCarrion = 5,
            ToxicantTwister = 6,
            OldReaper = 7,

            SulphuricAcid = 8,
            GammaHeart = 9,
            PhosphorescentGauntlet = 10,
            SlitheringEels = 11,
            SkyfinBombers = 12,
            SpentFuel = 13,
            SulphurousGrabber = 14,

            Transition = 15,
        }

        private static bool IsP1(AttackState s) => (int)s <= (int)AttackState.OldReaper;

        // Exactly 8 named P1 weapons — already at the 6-slot floor, no padding needed.
        private static readonly AttackState[] P1Cycle =
        {
            AttackState.InsidiousImpaler, AttackState.FetidEmesis, AttackState.SepticSkewer, AttackState.VitriolicViper,
            AttackState.MutatedTruffle, AttackState.CadaverousCarrion, AttackState.ToxicantTwister, AttackState.OldReaper,
        };
        // Exactly 7 named P2 weapons per the main design doc (excludes the weapon-attack-style doc's extra
        // FlakToxicannon row, which isn't in Old Duke's own section-4 weapon list).
        private static readonly AttackState[] P2Cycle =
        {
            AttackState.SulphuricAcid, AttackState.GammaHeart, AttackState.PhosphorescentGauntlet, AttackState.SlitheringEels,
            AttackState.SkyfinBombers, AttackState.SpentFuel, AttackState.SulphurousGrabber,
        };
        #endregion

        #region Fields
        private int currentRepetition = 0;
        private int attackCycleIndex = 0;

        // Blubber pads: 0 = Back, 1 = Left, 2 = Right.
        private readonly float[] blubberHPs = new float[3];
        private int blubberStunTimer = 0;
        private int blubberRespawnTimer = 0;

        private int exhaustTimer = 0;
        private int exhaustBoundaryHurtCooldown = 0;
        #endregion

        #region Core AI Hooks
        public override bool PreAI(NPC npc, IUMWGlobalNPC data)
        {
            if (!TryGetTarget(npc, out Player target))
            {
                npc.velocity.Y -= 0.5f;
                if (npc.timeLeft > 60) npc.timeLeft = 60;
                return false;
            }

            AttackState state = (AttackState)(int)npc.ai[1];
            ref float timer = ref npc.ai[2];
            ref float tracker = ref npc.ai[3];

            if (npc.ai[0] == 0f)
            {
                npc.ai[0] = 1f;
                state = AttackState.InsidiousImpaler;
                npc.ai[1] = (float)state;
                currentRepetition = 0;
                attackCycleIndex = 0;
                for (int i = 0; i < 3; i++) blubberHPs[i] = 1200f;
                npc.netUpdate = true;
            }

            int currentPhase = (int)npc.ai[0];
            float lifeRatio = npc.lifeMax <= 0 ? 1f : npc.life / (float)npc.lifeMax;
            if (IsP1(state) && lifeRatio <= PhaseLifeRatios[0] && state != AttackState.Transition)
            {
                currentPhase = 2;
                npc.ai[0] = 2f;
                state = AttackState.Transition;
                npc.ai[1] = (float)state;
                timer = 0;
                tracker = 0;
                npc.dontTakeDamage = true;
                npc.netUpdate = true;
            }

            UpdateBlubberRespawn();
            UpdateExhaustCage(npc, target, currentPhase);

            if (blubberStunTimer > 0)
            {
                blubberStunTimer--;
                npc.velocity *= 0.85f;
            }
            else if (state != AttackState.Transition)
            {
                int sideDestroyed = (blubberHPs[1] <= 0f ? 1 : 0) + (blubberHPs[2] <= 0f ? 1 : 0);
                float turnPenalty = 1f - 0.2f * sideDestroyed;
                float baseSpeed = currentPhase == 1 ? 13f : 19f;
                float speed = baseSpeed + (1f - lifeRatio) * 5f;
                float turnSpeed = (0.045f + (1f - lifeRatio) * 0.02f) * turnPenalty;
                Vector2 desiredVel = SafeNormalize(target.Center - npc.Center, Vector2.Zero) * speed;
                npc.velocity = Vector2.Lerp(npc.velocity, desiredVel, turnSpeed);
            }
            npc.rotation = npc.velocity.SafeNormalize(Vector2.UnitX).ToRotation();

            if (blubberStunTimer == 0)
            {
                switch (state)
                {
                    case AttackState.InsidiousImpaler: ExecuteInsidiousImpaler(npc, target, ref timer, ref tracker); break;
                    case AttackState.FetidEmesis: ExecuteFetidEmesis(npc, target, ref timer, ref tracker); break;
                    case AttackState.SepticSkewer: ExecuteSepticSkewer(npc, target, ref timer, ref tracker); break;
                    case AttackState.VitriolicViper: ExecuteVitriolicViper(npc, target, ref timer, ref tracker); break;
                    case AttackState.MutatedTruffle: ExecuteMutatedTruffle(npc, target, ref timer, ref tracker); break;
                    case AttackState.CadaverousCarrion: ExecuteCadaverousCarrion(npc, target, ref timer, ref tracker); break;
                    case AttackState.ToxicantTwister: ExecuteToxicantTwister(npc, target, ref timer, ref tracker); break;
                    case AttackState.OldReaper: ExecuteOldReaper(npc, target, ref timer, ref tracker); break;
                    case AttackState.SulphuricAcid: ExecuteSulphuricAcid(npc, target, ref timer, ref tracker); break;
                    case AttackState.GammaHeart: ExecuteGammaHeart(npc, target, ref timer, ref tracker); break;
                    case AttackState.PhosphorescentGauntlet: ExecutePhosphorescentGauntlet(npc, target, ref timer, ref tracker); break;
                    case AttackState.SlitheringEels: ExecuteSlitheringEels(npc, target, ref timer, ref tracker); break;
                    case AttackState.SkyfinBombers: ExecuteSkyfinBombers(npc, target, ref timer, ref tracker); break;
                    case AttackState.SpentFuel: ExecuteSpentFuel(npc, target, ref timer, ref tracker); break;
                    case AttackState.SulphurousGrabber: ExecuteSulphurousGrabber(npc, target, ref timer, ref tracker); break;
                    case AttackState.Transition: ExecuteTransition(npc, target, ref timer, ref tracker); break;
                }
            }

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) => ProcessBlubberHit(npc, player.Center, ref modifiers, item.damage);
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) => ProcessBlubberHit(npc, projectile.Center, ref modifiers, projectile.damage);
        #endregion

        #region Blubber Pads
        // Back/Left/Right positions relative to the boss's current facing (its rotation).
        private static Vector2 BlubberPos(NPC npc, int i)
        {
            float offset = i == 0 ? MathHelper.Pi : (i == 1 ? -MathHelper.PiOver2 : MathHelper.PiOver2);
            return npc.Center + (npc.rotation + offset).ToRotationVector2() * 90f;
        }

        private void UpdateBlubberRespawn()
        {
            bool allDead = true;
            for (int i = 0; i < 3; i++) if (blubberHPs[i] > 0f) allDead = false;

            if (allDead && blubberStunTimer == 0)
            {
                blubberRespawnTimer++;
                if (blubberRespawnTimer >= 1200) // 20s
                {
                    for (int i = 0; i < 3; i++) blubberHPs[i] = 1200f;
                    blubberRespawnTimer = 0;
                }
            }
            else
            {
                blubberRespawnTimer = 0;
            }
        }

        private void ProcessBlubberHit(NPC npc, Vector2 hitPos, ref NPC.HitModifiers modifiers, int damage)
        {
            int active = 0;
            for (int i = 0; i < 3; i++) if (blubberHPs[i] > 0f) active++;

            if (blubberStunTimer > 0)
                modifiers.FinalDamage *= 1.5f; // fully exhausted: 150% damage taken
            else if (active > 0)
                modifiers.FinalDamage *= 1f - 0.3f * active; // each active pad stacks 30% DR, capping at 90%

            if (blubberStunTimer > 0)
                return;

            for (int i = 0; i < 3; i++)
            {
                if (blubberHPs[i] <= 0f) continue;
                Vector2 padPos = BlubberPos(npc, i);
                if (Vector2.Distance(hitPos, padPos) < 70f)
                {
                    blubberHPs[i] -= damage;
                    if (blubberHPs[i] <= 0f)
                    {
                        SoundEngine.PlaySound(SoundID.NPCDeath4, padPos);
                        ScourgeFx.Burst(padPos, 6f, 14, DustID.ToxicBubble);
                        CheckAllBlubberBroken(npc);
                    }
                    break;
                }
            }
        }

        private void CheckAllBlubberBroken(NPC npc)
        {
            bool allDead = true;
            for (int i = 0; i < 3; i++) if (blubberHPs[i] > 0f) allDead = false;

            if (allDead)
            {
                blubberStunTimer = 420; // 7s
                npc.velocity = Vector2.Zero;
                SoundEngine.PlaySound(SoundID.NPCHit53, npc.Center);
            }
        }
        #endregion

        #region Acidic Exhaust Cage
        private void UpdateExhaustCage(NPC npc, Player target, int phase)
        {
            float cageSize = phase == 1 ? 1400f : 900f;
            Vector2 dist = target.Center - npc.Center;
            if (exhaustBoundaryHurtCooldown > 0) exhaustBoundaryHurtCooldown--;

            if (Math.Abs(dist.X) > cageSize / 2f || Math.Abs(dist.Y) > cageSize / 2f)
            {
                if (ModContent.TryFind("CalamityMod", "SulphuricPoisoning", out ModBuff poison))
                    target.AddBuff(poison.Type, 200);
                target.AddBuff(BuffID.Weak, 200);
                if (exhaustBoundaryHurtCooldown <= 0)
                {
                    target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 6, 0);
                    exhaustBoundaryHurtCooldown = 30;
                }
            }

            exhaustTimer++;
            if (exhaustTimer >= 480) // 8s cycle
            {
                exhaustTimer = 0;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitX);
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<ExhaustTrailProj>(), npc.damage / 6, 0f, Main.myPlayer, dir.X, dir.Y);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[2] = 480f;
                }
            }
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

            AttackState[] cycle = isP1 ? P1Cycle : P2Cycle;
            AttackState next;
            int guard = 0;
            do
            {
                attackCycleIndex++;
                next = cycle[attackCycleIndex % cycle.Length];
                guard++;
                // Back pad destroyed disables ToxicantTwister and TheOldReaper.
            } while (blubberHPs[0] <= 0f && (next == AttackState.ToxicantTwister || next == AttackState.OldReaper) && guard < cycle.Length);

            npc.ai[1] = (float)next;
            npc.ai[2] = 0;
            npc.ai[3] = 0;
            npc.netUpdate = true;
        }
        #endregion

        #region P1 Attacks
        private void ExecuteInsidiousImpaler(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<DukeHeldInsidiousImpaler>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 24 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.3f }, npc.Center);
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<BarbedTendrilProj>(), npc.damage / 2, 0f, Main.myPlayer, dir.X, dir.Y, npc.whoAmI);
            }
            if (timer >= 100) RotateAttack(npc, AttackState.InsidiousImpaler);
        }

        private void ExecuteFetidEmesis(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            var w = FindHeldWeapon<DukeHeldFetidEmesis>(npc);
            w?.SetAim(SafeNormalize(target.Center - npc.Center, Vector2.UnitY).ToRotation());

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.1f }, npc.Center);
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                for (int i = 0; i < 7; i++)
                {
                    Vector2 vel = dir.RotatedBy((i - 3) * 0.18f) * 9f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<AcidGlobProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                w?.Pulse(10f);
            }
            if (timer >= 170) RotateAttack(npc, AttackState.FetidEmesis);
        }

        private void ExecuteSepticSkewer(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            var w = FindHeldWeapon<DukeHeldSepticSkewer>(npc);
            w?.SetAim(SafeNormalize(target.Center - npc.Center, Vector2.UnitY).ToRotation());

            if ((timer == 30 || timer == 70) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item94 with { Pitch = 0.1f }, npc.Center);
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 13f, ModContent.ProjectileType<HarpoonBoomerangProj>(), npc.damage / 3, 0f, Main.myPlayer, 0f, 0f, npc.whoAmI);
                w?.Pulse(12f);
            }
            if (timer >= 160) RotateAttack(npc, AttackState.SepticSkewer);
        }

        private void ExecuteVitriolicViper(NPC npc, Player target, ref float timer, ref float tracker)
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
                FindHeldWeapon<DukeHeldVitriolicViper>(npc)?.Pulse(10f);
            }
            if (timer >= 200) RotateAttack(npc, AttackState.VitriolicViper);
        }

        private void ExecuteMutatedTruffle(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item21, npc.Center);
                for (int i = 0; i < 2; i++)
                {
                    Vector2 pos = target.Center + new Vector2(i == 0 ? -260f : 260f, 60f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<BurrowerMinionProj>(), npc.damage / 3, 0f, Main.myPlayer, target.whoAmI);
                }
                FindHeldWeapon<DukeHeldMutatedTruffle>(npc)?.Pulse(10f);
            }
            if (timer >= 220) RotateAttack(npc, AttackState.MutatedTruffle);
        }

        private void ExecuteCadaverousCarrion(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item21 with { Pitch = -0.2f }, npc.Center);
                for (int i = 0; i < 2; i++)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<CarrionDiveProj>(), npc.damage / 3, 0f, Main.myPlayer, target.whoAmI, i * MathHelper.Pi);
                }
                FindHeldWeapon<DukeHeldCadaverousCarrion>(npc)?.Pulse(10f);
            }
            if (timer >= 230) RotateAttack(npc, AttackState.CadaverousCarrion);
        }

        private void ExecuteToxicantTwister(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<DukeHeldToxicantTwister>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 26 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.1f }, npc.Center);
                for (int i = 0; i < 2; i++)
                {
                    float ang = i == 0 ? -0.6f : 0.6f;
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy(ang) * 6f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<TwisterDiskProj>(), npc.damage / 3, 0f, Main.myPlayer, target.whoAmI);
                }
            }
            if (timer >= 220) RotateAttack(npc, AttackState.ToxicantTwister);
        }

        private void ExecuteOldReaper(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<DukeHeldOldReaper>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 26 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.4f }, npc.Center);
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 14f, ModContent.ProjectileType<HarpoonBoomerangProj>(), npc.damage / 2, 0f, Main.myPlayer, 0f, 0f, npc.whoAmI);
            }
            if (timer >= 160) RotateAttack(npc, AttackState.OldReaper);
        }
        #endregion

        #region P2 Attacks
        private void ExecuteSulphuricAcid(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            var w = FindHeldWeapon<DukeHeldSulphuricAcid>(npc);
            w?.SetAim(SafeNormalize(target.Center - npc.Center, Vector2.UnitY).ToRotation());

            if (timer >= 30 && timer <= 70 && (timer - 30) % 20 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 9f + new Vector2(0f, -6f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<AcidOrbProj>(), npc.damage / 3, 0f, Main.myPlayer);
                w?.Pulse(-10f);
            }
            if (timer >= 180) RotateAttack(npc, AttackState.SulphuricAcid);
        }

        private void ExecuteGammaHeart(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Pitch = -0.3f }, npc.Center);
                FindHeldWeapon<DukeHeldGammaHeart>(npc)?.Pulse(10f);
            }
            if (timer >= 40 && timer <= 160 && (timer - 40) % 40 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<ExpandingRingProj>(), npc.damage / 3, 0f, Main.myPlayer, 260f, 34f);
            }
            if (timer >= 200) RotateAttack(npc, AttackState.GammaHeart);
        }

        private void ExecutePhosphorescentGauntlet(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<DukeHeldPhosphorescentGauntlet>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 18)
                npc.velocity = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 17f;

            if (timer == 26 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item14, npc.Center);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<ExpandingRingProj>(), npc.damage / 2, 0f, Main.myPlayer, 200f, 18f);
            }
            if (timer >= 110) RotateAttack(npc, AttackState.PhosphorescentGauntlet);
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

            // A burst of 5 quick dashes as the exhaust cage collapses down to its phase-2 size.
            if (timer <= 150 && timer % 30f == 1f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center + Main.rand.NextVector2Circular(200f, 200f) - npc.Center, Vector2.UnitY);
                npc.velocity = dir * 20f;
                ScourgeFx.Burst(npc.Center, 6f, 14, DustID.ToxicBubble);
                SoundEngine.PlaySound(SoundID.Item94 with { Pitch = -0.2f }, npc.Center);
            }

            if (timer >= 160)
            {
                attackCycleIndex = 0;
                currentRepetition = 0;
                npc.ai[1] = (float)AttackState.SulphuricAcid;
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
            Texture2D glowTex = TextureAssets.Dust.Value;
            Rectangle sourceRect = new Rectangle(0, 0, 8, 8);
            for (int i = 0; i < 3; i++)
            {
                if (blubberHPs[i] <= 0f) continue;
                Vector2 pos = BlubberPos(npc, i) - screenPos;
                spriteBatch.Draw(glowTex, pos, sourceRect, new Color(150, 255, 120) * 0.7f, 0f, new Vector2(4f, 4f), 3.2f, SpriteEffects.None, 0f);
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
