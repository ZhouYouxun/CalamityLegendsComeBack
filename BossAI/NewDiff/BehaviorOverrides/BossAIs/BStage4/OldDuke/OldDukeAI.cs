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
using CalamityMod;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.OldDuke
{
    // 硫海遗爵 — 引擎高频过载的暴虐飞兽. 设计文档: 大计划/O 硫海遗爵/硫海遗爵_重置版设计文档.md
    // 移动哲学(分寸感): 这是一头靠"冲刺动能"说话的鲨鲸 — 平时以10-12px/f绕玩家盘旋(猛禽巡猎),
    // 出手时才有真正的贯穿冲锋: 竖鳍蓄势(前摇尘埃) -> 22-26px/f直线贯穿 -> 半圆减速绕行归位.
    // 冲刺沿途铺设废气轨迹, 45帧后自燃成电离火壁(文档核心机制, 旧版因没有冲刺而完全空转).
    // 击碎侧脂肪垫会让对应侧的转向惯性变钝(更好躲), 并停喷废气 — 部位破坏直接改变冲刺手感.
    //
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

        private static readonly Color SulphurYellow = new(228, 200, 80);
        private static readonly Color AcidGreen = new(150, 255, 120);
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
        private int ticksRunning = 0;
        private int currentRepetition = 0;
        private int attackCycleIndex = 0;

        // Blubber pads: 0 = Back, 1 = Left, 2 = Right.
        private readonly float[] blubberHPs = new float[3];
        private readonly float[] blubberFlash = new float[3];
        private int blubberStunTimer = 0;
        private int blubberRespawnTimer = 0;

        private int exhaustTimer = 0;
        private int exhaustBoundaryHurtCooldown = 0;

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

        // Committed-dash state
        private Vector2 dashDir = Vector2.Zero;
        private bool crossedTargetThisDash = false;

        // Grabber claw-line telegraph (design doc: the claw line lights up 0.4s before the grab)
        private Vector2 clawAimDir = Vector2.Zero;
        private float clawLineBright = 0f;

        // Motion afterimages for the dashes
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
                currentVariantB = UseVariantB(state);
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
                CleanupHeldWeapons(npc);
                npc.netUpdate = true;
            }

            UpdateBlubberRespawn();
            UpdateExhaustCage(npc, target, currentPhase);
            for (int i = 0; i < 3; i++)
                if (blubberFlash[i] > 0f) blubberFlash[i] -= 0.07f;
            if (clawLineBright > 0f && state != AttackState.SulphurousGrabber)
                clawLineBright = 0f;

            if (blubberStunTimer > 0)
            {
                // 能量脱水 — the beast pants in place, sinking slightly, punished at 150%
                blubberStunTimer--;
                npc.velocity.X *= 0.9f;
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + 0.05f, -2f, 1.4f);
                npc.rotation = npc.velocity.X * 0.02f + MathF.Sin(ticksRunning * 0.3f) * 0.05f; // heaving breaths
                npc.damage = 0;

                // Rhythmic gasp bursts — visible exhaustion, in sync with the wobble
                if (ticksRunning % 24 == 0)
                {
                    SoundEngine.PlaySound(SoundID.NPCHit13 with { Volume = 0.3f, Pitch = -0.6f }, npc.Center);
                    ScourgeFx.Burst(npc.Center + new Vector2(0f, -20f), 2.5f, 6, DustID.ToxicBubble);
                }
                if (blubberStunTimer == 0)
                {
                    SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.8f }, npc.Center);
                    ScourgeFx.Burst(npc.Center, 6f, 20, DustID.ToxicBubble);
                }
            }
            else
            {
                npc.damage = npc.defDamage;
                npc.rotation = npc.velocity.SafeNormalize(Vector2.UnitX).ToRotation();

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

            oldPos[oldPosIndex] = npc.Center;
            oldPosIndex = (oldPosIndex + 1) % oldPos.Length;

            data.CurrentPhase = currentPhase;
            data.AttackState = (IUMWAttackState)Math.Clamp((int)state, 0, 4);
            data.PatternTimer = (int)timer;

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) => ProcessBlubberHit(npc, player.Center, ref modifiers, item.damage);
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) => ProcessBlubberHit(npc, projectile.Center, ref modifiers, projectile.damage);
        #endregion

        #region Movement: Circling & Committed Dashes
        // Raptor patrol: circle the player at radius ~360 instead of beelining. The shark stalks, it does not leash.
        private void CirclePatrol(NPC npc, Player target, float radius = 360f, float speed = 11f)
        {
            float orbitAngle = (npc.Center - target.Center).ToRotation() + 0.045f;
            Vector2 orbitSpot = target.Center + orbitAngle.ToRotationVector2() * radius;
            Vector2 desired = SafeNormalize(orbitSpot - npc.Center, Vector2.UnitX) * speed;
            npc.velocity = Vector2.Lerp(npc.velocity, desired, 0.08f);
        }

        // Windup posture: fins raised, holding a launch point, dust streaming backward — the dash announces itself.
        private void DashWindup(NPC npc, Vector2 holdSpot)
        {
            Vector2 desired = (holdSpot - npc.Center) * 0.08f;
            if (desired.Length() > 12f) desired = Vector2.Normalize(desired) * 12f;
            npc.velocity = Vector2.Lerp(npc.velocity, desired, 0.12f);
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(60f, 40f), DustID.ToxicBubble, -npc.velocity * 0.2f + Main.rand.NextVector2Circular(1f, 1f), 100, default, 1.2f);
                d.noGravity = true;
            }
        }

        // The committed pierce: launch through the player's predicted spot, roar, burst, lay the exhaust trail.
        private void LaunchDash(NPC npc, Player target, float speed, bool layExhaust = true, float lead = 8f)
        {
            dashDir = SafeNormalize(target.Center + target.velocity * lead - npc.Center, Vector2.UnitX);
            npc.velocity = dashDir * speed;
            crossedTargetThisDash = false;
            SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.55f, Pitch = 0.2f }, npc.Center);
            ScourgeFx.Burst(npc.Center, 5f, 14, DustID.ToxicBubble);

            // Exhaust trail along the dash lane; the trail self-ignites into a firewall 45 frames later.
            // Side blubber pads gate the exhaust (design doc): both side pads destroyed = no trail at all.
            bool exhaustAvailable = blubberHPs[1] > 0f || blubberHPs[2] > 0f;
            if (layExhaust && exhaustAvailable && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float dashLength = speed * 22f;
                int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<ExhaustTrailProj>(), npc.defDamage / 6, 0f, Main.myPlayer, dashDir.X, dashDir.Y);
                if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[2] = dashLength;
            }
        }

        // Post-dash semicircle: bank away and bleed speed. Destroyed side pads stiffen the turn (design doc:
        // +20% side-slip inertia per side pad lost = the beast corners worse and reads easier).
        private void DashRecoveryArc(NPC npc, Player target)
        {
            int sidesDestroyed = (blubberHPs[1] <= 0f ? 1 : 0) + (blubberHPs[2] <= 0f ? 1 : 0);
            float turnRate = 0.035f * (1f - 0.2f * sidesDestroyed);
            Vector2 toTarget = SafeNormalize(target.Center - npc.Center, Vector2.UnitX);
            Vector2 desired = toTarget * MathHelper.Max(npc.velocity.Length() * 0.965f, 7f);
            npc.velocity = Vector2.Lerp(npc.velocity, desired, turnRate);
        }

        private bool HasCrossedTarget(NPC npc, Player target)
        {
            if (crossedTargetThisDash)
                return false;
            if (Vector2.Dot(target.Center - npc.Center, dashDir) < 0f)
            {
                crossedTargetThisDash = true;
                return true;
            }
            return false;
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
                    SoundEngine.PlaySound(SoundID.NPCHit13 with { Volume = 0.6f, Pitch = -0.3f }, Main.player[Main.myPlayer].Center);
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
                    blubberFlash[i] = 1f;
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
                SoundEngine.PlaySound(SoundID.NPCHit13 with { Volume = 0.8f, Pitch = -0.5f }, npc.Center);
                ScourgeFx.Burst(npc.Center, 7f, 30, DustID.ToxicBubble);
                if (Main.netMode != NetmodeID.Server)
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower = 8f;
            }
        }
        #endregion

        #region Acidic Exhaust Cage
        private void UpdateExhaustCage(NPC npc, Player target, int phase)
        {
            float cageSize = phase == 1 ? 1400f : 900f;
            float half = cageSize / 2f;
            Vector2 dist = target.Center - npc.Center;
            if (exhaustBoundaryHurtCooldown > 0) exhaustBoundaryHurtCooldown--;

            // The golden toxic-mist frame must be visible: sulphur motes trace the square
            for (int i = 0; i < 3; i++)
            {
                float t = Main.rand.NextFloat(4f);
                Vector2 pos;
                if (t < 1f) pos = npc.Center + new Vector2(MathHelper.Lerp(-half, half, t), -half);
                else if (t < 2f) pos = npc.Center + new Vector2(half, MathHelper.Lerp(-half, half, t - 1f));
                else if (t < 3f) pos = npc.Center + new Vector2(MathHelper.Lerp(half, -half, t - 2f), half);
                else pos = npc.Center + new Vector2(-half, MathHelper.Lerp(half, -half, t - 3f));
                Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, Vector2.Zero, 150, default, 1.1f);
                d.noGravity = true;
                d.fadeIn = 1f;
            }

            if (Math.Abs(dist.X) > half || Math.Abs(dist.Y) > half)
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

            // 电离引爆 spark (every 8s): ignites a trail along the beast's current heading — never spawned
            // on the player, always along where the boss has actually been flying.
            exhaustTimer++;
            if (exhaustTimer >= 480)
            {
                exhaustTimer = 0;
                bool exhaustAvailable = blubberHPs[1] > 0f || blubberHPs[2] > 0f;
                if (exhaustAvailable && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 dir = npc.velocity.SafeNormalize(SafeNormalize(target.Center - npc.Center, Vector2.UnitX));
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<ExhaustTrailProj>(), npc.defDamage / 6, 0f, Main.myPlayer, dir.X, dir.Y);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[2] = 480f;
                    SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.5f, Pitch = -0.3f }, npc.Center);
                }
            }
        }
        #endregion

        #region Attack Rotation
        private void RotateAttack(NPC npc, AttackState current)
        {
            CleanupHeldWeapons(npc);
            clawLineBright = 0f;
            bool isP1 = IsP1(current);
            if (isP1)
            {
                currentRepetition++;
                if (currentRepetition < 3)
                {
                    // Same weapon again, but the A/B read flips so 3 reps never feel like 3 copies
                    currentVariantB = UseVariantB(current);
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

            currentVariantB = UseVariantB(next);
            npc.ai[1] = (float)next;
            npc.ai[2] = 0;
            npc.ai[3] = 0;
            npc.netUpdate = true;
        }
        #endregion

        #region P1 Attacks
        // 阴险穿刺者 · 爆裂毒柱 — 高速水平贯穿冲锋, 刺过玩家坐标的瞬间爆出上下毒气水幕柱(文档原题).
        // 变体A: 水平贯穿; 变体B: 自斜上方45°贯穿, 水幕柱改为左右封堵.
        private void ExecuteInsidiousImpaler(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<DukeHeldInsidiousImpaler>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer < 34)
            {
                // Windup at the launch flank: horizontal lane (A) or upper-diagonal perch (B)
                Vector2 holdSpot = currentVariantB
                    ? target.Center + new Vector2(Math.Sign(npc.Center.X - target.Center.X) * 420f, -420f)
                    : target.Center + new Vector2(Math.Sign(npc.Center.X - target.Center.X) * 520f, 0f);
                DashWindup(npc, holdSpot);
                if (timer == 20)
                    SoundEngine.PlaySound(SoundID.NPCHit13 with { Volume = 0.5f, Pitch = -0.4f }, npc.Center);
            }

            if (timer == 34)
                LaunchDash(npc, target, 26f, layExhaust: true, lead: 6f);

            if (timer > 34 && timer < 90)
            {
                // The harpoon bursts the instant the beast crosses the player's coordinate
                if (HasCrossedTarget(npc, target) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.3f }, npc.Center);
                    Vector2 curtainDir = currentVariantB ? Vector2.UnitX : Vector2.UnitY;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<BarbedTendrilProj>(), npc.defDamage / 2, 0f, Main.myPlayer, curtainDir.X, curtainDir.Y, npc.whoAmI);
                    ScourgeFx.Burst(npc.Center, 5f, 12, DustID.ToxicBubble);
                }
                if (timer > 60)
                    DashRecoveryArc(npc, target);
            }

            if (timer >= 90 && timer < 120)
                CirclePatrol(npc, target);

            if (timer >= 120) RotateAttack(npc, AttackState.InsidiousImpaler);
        }

        // 恶臭呕吐 · 反弹酸泡 — 盘旋蓄气(身体膨胀感)后急停仰喷60°酸泡扇. 变体A: 单轮7泡; 变体B: 两轮各5泡, 中间滑步换位.
        private void ExecuteFetidEmesis(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            var w = FindHeldWeapon<DukeHeldFetidEmesis>(npc);
            w?.SetAim(SafeNormalize(target.Center - npc.Center, Vector2.UnitY).ToRotation());

            if (timer < 40)
            {
                CirclePatrol(npc, target, 350f, 10f);
                // Bloating charge: bubbles leak from the maw as pressure builds
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + npc.rotation.ToRotationVector2() * 50f, DustID.ToxicBubble, -Vector2.UnitY * Main.rand.NextFloat(0.5f, 1.5f), 100, default, 1.15f);
                    d.noGravity = true;
                }
            }

            int[] volleys = currentVariantB ? new[] { 40, 96 } : new[] { 40 };
            foreach (int vt in volleys)
            {
                if (timer == vt && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.1f }, npc.Center);
                    Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                    int count = currentVariantB ? 5 : 7;
                    for (int i = 0; i < count; i++)
                    {
                        Vector2 vel = dir.RotatedBy((i - (count - 1) / 2f) * 0.18f) * 9f;
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<AcidGlobProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                    }
                    w?.Pulse(10f);
                    ScourgeFx.Burst(npc.Center + dir * 50f, 4f, 10, DustID.ToxicBubble);
                    npc.velocity -= dir * 5f; // the heave knocks the beast back
                }
            }

            if (timer > 40 && timer < 96 && currentVariantB)
                CirclePatrol(npc, target, 380f, 12f); // slide to a new spew angle between volleys
            else if (timer > volleys[volleys.Length - 1])
                npc.velocity *= 0.97f;

            if (timer >= 150) RotateAttack(npc, AttackState.FetidEmesis);
        }

        // 败血穿叉 · 三叉戟分裂 — 悬停摇摆蓄力, 甩叉后16px/f反冲拉开(文档后坐力). 变体A: 单叉; 变体B: 双叉交叉角.
        private void ExecuteSepticSkewer(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            var w = FindHeldWeapon<DukeHeldSepticSkewer>(npc);
            w?.SetAim(SafeNormalize(target.Center - npc.Center, Vector2.UnitY).ToRotation());

            if (timer < 30)
            {
                // Hover sway windup (design doc: ±15px sway)
                Vector2 holdSpot = DirectedHoverSpotStatic(npc, target, 380f, -120f) + new Vector2(MathF.Sin(ticksRunning * 0.15f) * 15f, 0f);
                DashWindup(npc, holdSpot);
            }

            int[] throwTimes = currentVariantB ? new[] { 30, 34 } : new[] { 30, 70 };
            foreach (int tt in throwTimes)
            {
                if (timer == tt && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SoundEngine.PlaySound(SoundID.Item94 with { Pitch = 0.1f }, npc.Center);
                    float angleOff = currentVariantB ? (tt == throwTimes[0] ? -0.22f : 0.22f) : 0f;
                    Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy(angleOff);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 13f, ModContent.ProjectileType<HarpoonBoomerangProj>(), npc.defDamage / 3, 0f, Main.myPlayer, 0f, 0f, npc.whoAmI);
                    w?.Pulse(12f);
                    npc.velocity = -dir * 16f; // recoil retreat (design doc)
                }
            }

            if (timer > 34 && timer < 70)
                npc.velocity *= 0.95f;
            else if (timer > 70)
                CirclePatrol(npc, target, 340f, 10f);

            if (timer >= 150) RotateAttack(npc, AttackState.SepticSkewer);
        }

        // 硫酸毒蛇 · 蛇形毒刃 — 原地轻浮动作为稳定发射源(文档), 喷出蛇形爬行的毒刃群.
        // 变体A: 五刃扇形; 变体B: 三刃两波, 相位相反的正弦交织.
        private void ExecuteVitriolicViper(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;

            // Stable firing platform: gentle bob, no drift (design doc)
            npc.velocity *= 0.93f;
            npc.velocity.Y += MathF.Sin(ticksRunning * 0.05f) * 0.05f;

            if (timer > 20 && timer < 40 && Main.rand.NextBool(2))
            {
                // Venom drips off the raised blade
                Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(50f, 40f), DustID.CursedTorch, new Vector2(0f, Main.rand.NextFloat(1f, 2f)), 100, default, 1.1f);
                d.noGravity = true;
            }

            int[] waves = currentVariantB ? new[] { 40, 76 } : new[] { 40 };
            foreach (int wt in waves)
            {
                if (timer == wt && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SoundEngine.PlaySound(SoundID.Item9, npc.Center);
                    Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                    int count = currentVariantB ? 3 : 5;
                    for (int i = 0; i < count; i++)
                    {
                        Vector2 vel = dir.RotatedBy((i - (count - 1) / 2f) * 0.18f) * 7f;
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<EelBoltProj>(), npc.defDamage / 3, 0f, Main.myPlayer, target.whoAmI);
                    }
                    FindHeldWeapon<DukeHeldVitriolicViper>(npc)?.Pulse(10f);
                }
            }

            if (timer >= 170) RotateAttack(npc, AttackState.VitriolicViper);
        }

        // 变异松露 · 猪鲨幻影撞 — 掘地兽从下方切入的同时, 本体从对侧发起相向对冲夹击(文档的镜像对冲).
        // 变体A: 掘地兽两侧+本体水平对冲; 变体B: 掘地兽脚下+本体自上俯冲.
        private void ExecuteMutatedTruffle(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer < 40)
            {
                Vector2 holdSpot = currentVariantB
                    ? target.Center + new Vector2(0f, -460f)
                    : target.Center + new Vector2(Math.Sign(npc.Center.X - target.Center.X) * 520f, -60f);
                DashWindup(npc, holdSpot);
            }

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item21, npc.Center);
                for (int i = 0; i < 2; i++)
                {
                    Vector2 pos = currentVariantB
                        ? target.Center + new Vector2(i == 0 ? -110f : 110f, 220f)
                        : target.Center + new Vector2(i == 0 ? -260f : 260f, 60f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<BurrowerMinionProj>(), npc.defDamage / 3, 0f, Main.myPlayer, target.whoAmI);
                }
                FindHeldWeapon<DukeHeldMutatedTruffle>(npc)?.Pulse(10f);
            }

            // The pincer: the beast counter-charges while the burrowers erupt
            if (timer == 64)
                LaunchDash(npc, target, 23f, layExhaust: true, lead: 4f);

            if (timer > 64 && timer < 130)
            {
                if (timer > 92)
                    DashRecoveryArc(npc, target);
            }
            else if (timer >= 130)
                CirclePatrol(npc, target);

            if (timer >= 180) RotateAttack(npc, AttackState.MutatedTruffle);
        }

        // 腐尸秃鹫 · 俯冲爪袭 — 双爪先在玩家两侧凝聚(警示尘), 再俯冲合拢. 变体A: 反相双爪; 变体B: 同相错拍.
        private void ExecuteCadaverousCarrion(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            CirclePatrol(npc, target, 400f, 10f);

            // Claw condensation warning at the player's flanks
            if (timer > 20 && timer < 40 && Main.rand.NextBool(2))
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector2 warnPos = target.Center + new Vector2(s * 240f, -180f);
                    Dust d = Dust.NewDustPerfect(warnPos + Main.rand.NextVector2Circular(40f, 40f), DustID.CursedTorch, Vector2.Zero, 100, default, 1.25f);
                    d.fadeIn = 1.2f;
                    d.noGravity = true;
                }
            }

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item21 with { Pitch = -0.2f }, npc.Center);
                for (int i = 0; i < 2; i++)
                {
                    float phaseOff = currentVariantB ? i * MathHelper.PiOver2 : i * MathHelper.Pi;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<CarrionDiveProj>(), npc.defDamage / 3, 0f, Main.myPlayer, target.whoAmI, phaseOff);
                }
                FindHeldWeapon<DukeHeldCadaverousCarrion>(npc)?.Pulse(10f);
            }

            if (timer >= 200) RotateAttack(npc, AttackState.CadaverousCarrion);
        }

        // 毒素旋风 · 离心酸风暴 — 头部自转甩出双旋风向外平移(文档). 变体A: 双风外扩; 变体B: 四风两对, 内外双层.
        private void ExecuteToxicantTwister(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<DukeHeldToxicantTwister>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer < 26)
            {
                // Head-spin windup: the beast whirls in place, dust spiraling off
                npc.velocity *= 0.92f;
                npc.rotation += (timer / 26f) * 0.35f;
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2CircularEdge(70f, 70f), DustID.ToxicBubble, (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * 3f, 100, default, 1.2f);
                    d.noGravity = true;
                }
            }

            if (timer == 26 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.1f }, npc.Center);
                int pairs = currentVariantB ? 2 : 1;
                for (int p = 0; p < pairs; p++)
                {
                    float speed = 6f - p * 2f; // inner pair slower — layered walls
                    for (int i = 0; i < 2; i++)
                    {
                        float ang = i == 0 ? -0.6f : 0.6f;
                        Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy(ang) * speed;
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<TwisterDiskProj>(), npc.defDamage / 3, 0f, Main.myPlayer, target.whoAmI);
                    }
                }
                ScourgeFx.Burst(npc.Center, 5f, 12, DustID.ToxicBubble);
            }

            if (timer > 26)
                CirclePatrol(npc, target, 380f, 11f);

            if (timer >= 190) RotateAttack(npc, AttackState.ToxicantTwister);
        }

        // 老收割者 · 大摆钟斩 — 掠过玩家的贯穿冲刺, 持有巨镰随冲刺方向自动挥出大圆弧(文档的270°摆钟),
        // 途中甩出回旋鱼叉. 变体A: 单次掠过; 变体B: 两次交叉掠过画X.
        private void ExecuteOldReaper(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<DukeHeldOldReaper>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer < 40)
            {
                Vector2 holdSpot = target.Center + new Vector2(Math.Sign(npc.Center.X - target.Center.X) * 480f, -260f);
                DashWindup(npc, holdSpot);
                if (timer == 26)
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.6f, Pitch = -0.4f }, npc.Center);
            }

            if (timer == 40)
                LaunchDash(npc, target, 24f, layExhaust: true, lead: 6f);

            if (timer > 40 && timer < 100)
            {
                // Release the ricochet harpoon mid-pass, right as the scythe crosses the player
                if (HasCrossedTarget(npc, target) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.4f }, npc.Center);
                    Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 14f, ModContent.ProjectileType<HarpoonBoomerangProj>(), npc.defDamage / 2, 0f, Main.myPlayer, 0f, 0f, npc.whoAmI);
                }
                if (timer > 66)
                    DashRecoveryArc(npc, target);
            }

            if (currentVariantB)
            {
                if (timer == 100)
                {
                    // Second pass cuts the other diagonal
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<DukeHeldOldReaper>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                    LaunchDash(npc, target, 24f, layExhaust: true, lead: 6f);
                }
                if (timer > 100 && timer > 126)
                    DashRecoveryArc(npc, target);
            }

            int endTime = currentVariantB ? 170 : 140;
            if (timer >= endTime) RotateAttack(npc, AttackState.OldReaper);
        }
        #endregion

        #region P2 Attacks
        // 硫酸炮 — 盘旋中连射4发迫击酸弹(文档数量), 弹落成核废水池. 变体A: 依次锁玩家; 变体B: 四发左右交替夹标.
        private void ExecuteSulphuricAcid(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            var w = FindHeldWeapon<DukeHeldSulphuricAcid>(npc);
            w?.SetAim(SafeNormalize(target.Center - npc.Center, Vector2.UnitY).ToRotation());

            CirclePatrol(npc, target, 380f, 11f);

            if (timer >= 30 && timer <= 90 && (timer - 30) % 20 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int shot = (int)(timer - 30) / 20;
                Vector2 aim = currentVariantB
                    ? target.Center + new Vector2((shot % 2 == 0 ? -1f : 1f) * 180f, 0f)
                    : target.Center;
                Vector2 vel = SafeNormalize(aim - npc.Center, Vector2.UnitY) * 9f + new Vector2(0f, -6f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<AcidOrbProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item61 with { Volume = 0.5f }, npc.Center);
                ScourgeFx.Burst(npc.Center + vel.SafeNormalize(Vector2.Zero) * 40f, 3f, 6, DustID.ToxicBubble);
                w?.Pulse(-10f);
            }

            if (timer >= 170) RotateAttack(npc, AttackState.SulphuricAcid);
        }

        // 伽马之心 — 胸口能量环自Boss膨出推向玩家(修复旧版"环直接生成在玩家身上"的零预警问题).
        // 变体A: 四环连推; 变体B: 三环 + 尾随一环自侧翼夹击.
        private void ExecuteGammaHeart(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            npc.velocity *= 0.95f; // rooted: the chest-core needs a stable anchor

            if (timer > 10 && timer < 30 && Main.rand.NextBool(2))
            {
                // The core condenses: green energy drawn into the chest
                Vector2 around = npc.Center + Main.rand.NextVector2CircularEdge(90f, 90f);
                Dust d = Dust.NewDustPerfect(around, DustID.CursedTorch, (npc.Center - around) * 0.08f, 100, default, 1.2f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Pitch = -0.3f }, npc.Center);
                FindHeldWeapon<DukeHeldGammaHeart>(npc)?.Pulse(10f);
            }

            int ringCount = currentVariantB ? 3 : 4;
            if (timer >= 40 && timer <= 40 + (ringCount - 1) * 35 && (timer - 40) % 35 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Rings bloom from the boss's own chest and expand outward — dodge by moving off the radius
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<ExpandingRingProj>(), npc.defDamage / 3, 0f, Main.myPlayer, 320f, 30f);
                SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.5f }, npc.Center);
            }
            if (currentVariantB && timer == 130 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 flank = target.Center + new Vector2(Math.Sign(target.velocity.X == 0f ? 1f : target.velocity.X) * 300f, 0f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), flank, Vector2.Zero, ModContent.ProjectileType<ExpandingRingProj>(), npc.defDamage / 3, 0f, Main.myPlayer, 260f, 26f);
                SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.5f, Pitch = 0.2f }, flank);
            }

            if (timer >= 190) RotateAttack(npc, AttackState.GammaHeart);
        }

        // 磷光拳套 — 文档: 瞬移到玩家上方300px, 重拳直砸. 淡出+尘埃汇聚预告落点, 再垂直坠拳+冲击环.
        // 变体A: 正上方直落; 变体B: 斜上方45°斩落.
        private void ExecutePhosphorescentGauntlet(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<DukeHeldPhosphorescentGauntlet>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 perch = currentVariantB
                ? target.Center + new Vector2(Math.Sign(npc.Center.X - target.Center.X) * 240f, -300f)
                : target.Center + new Vector2(0f, -300f);

            if (timer > 4 && timer < 18)
            {
                // Flash-fade toward the perch: the body blurs out, dust condenses at the strike origin
                npc.Opacity = MathHelper.Lerp(1f, 0.25f, (timer - 4f) / 14f);
                npc.damage = 0;
                for (int i = 0; i < 2; i++)
                {
                    Vector2 around = perch + Main.rand.NextVector2Circular(60f, 60f);
                    Dust d = Dust.NewDustPerfect(around, DustID.CursedTorch, (perch - around) * 0.1f, 100, default, 1.25f);
                    d.fadeIn = 1.2f;
                    d.noGravity = true;
                }
            }

            if (timer == 18)
            {
                npc.Center = perch;
                npc.velocity = Vector2.Zero;
                npc.Opacity = 1f;
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.5f, Pitch = -0.2f }, npc.Center);
                ScourgeFx.Burst(npc.Center, 4f, 12, DustID.CursedTorch);
                for (int i = 0; i < oldPos.Length; i++) oldPos[i] = npc.Center;
            }

            if (timer > 18 && timer < 36)
                npc.velocity *= 0.9f; // hang: the fist cocks back

            if (timer == 36)
                LaunchDash(npc, target, 25f, layExhaust: false, lead: 0f);

            if (timer > 36 && timer < 80)
            {
                if (HasCrossedTarget(npc, target) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SoundEngine.PlaySound(SoundID.Item14, npc.Center);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<ExpandingRingProj>(), npc.defDamage / 2, 0f, Main.myPlayer, 200f, 18f);
                    if (Main.netMode != NetmodeID.Server)
                        Main.LocalPlayer.Calamity().GeneralScreenShakePower = 5f;
                }
                if (timer > 56)
                    DashRecoveryArc(npc, target);
            }

            if (timer >= 110) RotateAttack(npc, AttackState.PhosphorescentGauntlet);
        }

        // 滑行电鳗 — 蓄势后放出鳗群. 变体A: 五鳗扇形; 变体B: 两翼各三鳗交叉网.
        private void ExecuteSlitheringEels(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            CirclePatrol(npc, target, 360f, 11f);

            if (timer > 24 && timer < 40 && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(60f, 40f), DustID.Electric, Main.rand.NextVector2Circular(2f, 2f), 100, default, 0.9f);
                d.noGravity = true;
            }

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item9, npc.Center);
                if (currentVariantB)
                {
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Vector2 origin = target.Center + new Vector2(s * 480f, -120f);
                        Vector2 dir = SafeNormalize(target.Center - origin, Vector2.UnitX);
                        for (int i = 0; i < 3; i++)
                            Projectile.NewProjectile(npc.GetSource_FromAI(), origin, dir.RotatedBy((i - 1) * 0.16f) * 7f, ModContent.ProjectileType<EelBoltProj>(), npc.defDamage / 3, 0f, Main.myPlayer, target.whoAmI);
                    }
                }
                else
                {
                    Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 vel = dir.RotatedBy((i - 2) * 0.18f) * 7f;
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<EelBoltProj>(), npc.defDamage / 3, 0f, Main.myPlayer, target.whoAmI);
                    }
                }
                FindHeldWeapon<ScourgeHeldSlitheringEels>(npc)?.Pulse(10f);
            }

            if (timer >= 170) RotateAttack(npc, AttackState.SlitheringEels);
        }

        // 天鳍轰炸机 — 变体A: 两翼各一架对飞; 变体B: 三架(文档数量)含一架自顶部直落.
        private void ExecuteSkyfinBombers(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ScourgeHeldSkyfinBombers>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            CirclePatrol(npc, target, 400f, 10f);

            if (timer == 22 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item21 with { Pitch = 0.3f }, npc.Center);
                for (int i = 0; i < 2; i++)
                {
                    float dir = i == 0 ? -1f : 1f;
                    Vector2 pos = target.Center + new Vector2(dir * 500f, -450f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, new Vector2(-dir * 6f, 0f), ModContent.ProjectileType<BomberFishProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                    ScourgeFx.Burst(pos, 3f, 8, DustID.ToxicBubble);
                }
                if (currentVariantB)
                {
                    Vector2 topPos = target.Center + new Vector2(0f, -560f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), topPos, new Vector2(0f, 5f), ModContent.ProjectileType<BomberFishProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                    ScourgeFx.Burst(topPos, 3f, 8, DustID.ToxicBubble);
                }
            }

            if (timer >= 160) RotateAttack(npc, AttackState.SkyfinBombers);
        }

        // 废燃料容器 — 变体A: 随机三桶坠落; 变体B: 三桶行进线依次砸落(会走路的爆雾墙).
        private void ExecuteSpentFuel(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ScourgeHeldSpentFuel>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                tracker = Main.rand.NextBool() ? 1f : -1f; // variant B: march direction
            }

            CirclePatrol(npc, target, 380f, 11f);

            if (timer >= 30 && timer <= 70 && (timer - 30) % 20 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 pos;
                if (currentVariantB)
                {
                    float progress = (timer - 30f) / 40f;
                    float x = tracker > 0f ? MathHelper.Lerp(-320f, 320f, progress) : MathHelper.Lerp(320f, -320f, progress);
                    pos = target.Center + new Vector2(x, -420f);
                }
                else
                {
                    pos = target.Center + Main.rand.NextVector2Circular(260f, 60f) - new Vector2(0f, 400f);
                }
                Projectile.NewProjectile(npc.GetSource_FromAI(), pos, new Vector2(0f, 3f), ModContent.ProjectileType<FuelBarrelProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                // Drop twinkle so the lane reads a beat early
                for (int i = 0; i < 4; i++)
                {
                    Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, Main.rand.NextVector2Circular(2f, 2f), 100, default, 1.2f);
                    d.noGravity = true;
                }
            }

            if (timer >= 160) RotateAttack(npc, AttackState.SpentFuel);
        }

        // 硫磺抓取器 — 爪线亮起0.4秒(文档硬性要求)后骨爪射出; 抓中会把玩家拖向Boss冲锋的身前.
        // 变体A: 单爪+跟进冲刺; 变体B: 双爪±13°夹射.
        private void ExecuteSulphurousGrabber(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ScourgeHeldSulphurousGrabber>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            // 0.4s claw-line lock (24 frames): the line tracks, then freezes and flares
            if (timer >= 4 && timer < 16)
            {
                npc.velocity *= 0.93f;
                clawAimDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                clawLineBright = 0.45f;
            }
            else if (timer >= 16 && timer < 28)
            {
                npc.velocity *= 0.93f;
                clawLineBright = 1f; // frozen and lethal-bright — this is the dodge window
            }
            else if (timer == 28 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                clawLineBright = 0f;
                SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.4f }, npc.Center);
                int claws = currentVariantB ? 2 : 1;
                for (int i = 0; i < claws; i++)
                {
                    float off = currentVariantB ? (i == 0 ? -0.13f : 0.13f) : 0f;
                    Vector2 d = clawAimDir.RotatedBy(off);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<BarbedTendrilProj>(), npc.defDamage / 2, 0f, Main.myPlayer, d.X, d.Y, npc.whoAmI);
                }
                ScourgeFx.Burst(npc.Center + clawAimDir * 50f, 4f, 10, DustID.CursedTorch);
            }

            // The follow-up rush: dragged prey meets the beast head-on
            if (timer == 60 && !currentVariantB)
                LaunchDash(npc, target, 20f, layExhaust: false, lead: 0f);
            if (timer > 60 && timer > 84)
                DashRecoveryArc(npc, target);

            if (timer >= 110) RotateAttack(npc, AttackState.SulphurousGrabber);
        }
        #endregion

        #region Transition
        // 形态转变 (50%): 引擎过载 — 恒定5次冲刺(文档), 每刺附带废气轨迹与电光爆裂.
        private void ExecuteTransition(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;

            if (timer == 1)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
                SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.9f, Pitch = -0.3f }, npc.Center);
                target.Calamity().GeneralScreenShakePower = 9f;
            }

            // Five overload dashes as the exhaust cage collapses down to its phase-2 size (design doc: 恒定5次)
            if (timer <= 150 && timer % 30f == 1f && timer > 1f)
            {
                LaunchDash(npc, target, 22f, layExhaust: true, lead: 4f);
                // Lightning burst along the launch (design doc: 冲刺轨迹附带闪电爆裂)
                for (int i = 0; i < 10; i++)
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(50f, 50f), DustID.Electric, dashDir.RotatedBy(Main.rand.NextFloat(-0.5f, 0.5f)) * Main.rand.NextFloat(3f, 7f), 100, default, 1.1f);
                    d.noGravity = true;
                }
            }
            else if (timer % 30f > 16f)
            {
                npc.velocity *= 0.96f;
            }

            if (timer >= 160)
            {
                attackCycleIndex = 0;
                currentRepetition = 0;
                AttackState next = AttackState.SulphuricAcid;
                currentVariantB = UseVariantB(next);
                npc.ai[1] = (float)next;
                npc.ai[2] = 0;
                npc.ai[3] = 0;
                npc.dontTakeDamage = false;
                npc.netUpdate = true;
            }
        }
        #endregion

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Afterimages when the beast is truly moving — dashes leave a sulphur wake
            if (npc.velocity.Length() > 13f)
            {
                Texture2D tex = TextureAssets.Npc[npc.type].Value;
                Vector2 origin = npc.frame.Size() * 0.5f;
                for (int i = 1; i < oldPos.Length; i++)
                {
                    int idx = (oldPosIndex - i + oldPos.Length * 2) % oldPos.Length;
                    if (oldPos[idx] == Vector2.Zero) continue;
                    float fade = (1f - i / (float)oldPos.Length) * 0.38f * npc.Opacity;
                    Color ghost = new Color(150, 200, 60, 0) * fade;
                    spriteBatch.Draw(tex, oldPos[idx] - screenPos, npc.frame, ghost, npc.rotation, origin, npc.scale * (1f - i * 0.02f), npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            // Blubber pads: HP-scaled pulsing blobs with hit flash
            for (int i = 0; i < 3; i++)
            {
                if (blubberHPs[i] <= 0f) continue;
                Vector2 pos = BlubberPos(npc, i) - screenPos;
                float hpScale = MathHelper.Lerp(0.55f, 1f, MathHelper.Clamp(blubberHPs[i] / 1200f, 0f, 1f));
                float pulse = 1f + 0.1f * MathF.Sin(ticksRunning * 0.1f + i * 2f);
                Color padColor = Color.Lerp(AcidGreen, Color.White, MathHelper.Clamp(blubberFlash[i], 0f, 1f) * 0.7f);
                padColor.A = 0;
                spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), padColor * 0.7f, ticksRunning * 0.03f + i, new Vector2(0.5f), new Vector2(34f, 34f) * hpScale * pulse, SpriteEffects.None, 0f);
                spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), padColor * 0.45f, -ticksRunning * 0.02f + i, new Vector2(0.5f), new Vector2(48f, 20f) * hpScale * pulse, SpriteEffects.None, 0f);
            }

            // Grabber claw lock-line
            if (clawLineBright > 0.05f && clawAimDir != Vector2.Zero)
            {
                float width = clawLineBright >= 1f ? 4f : 1.5f;
                Color lineColor = Color.Lerp(AcidGreen, Color.White, clawLineBright >= 1f ? 0.5f : 0f) * (0.35f + clawLineBright * 0.5f);
                lineColor.A = 0;
                Vector2 lineEnd = npc.Center + clawAimDir * 1000f;
                spriteBatch.Draw(pixel, (npc.Center + lineEnd) * 0.5f - screenPos, new Rectangle(0, 0, 1, 1), lineColor, clawAimDir.ToRotation(), new Vector2(0.5f), new Vector2(Vector2.Distance(npc.Center, lineEnd), width), SpriteEffects.None, 0f);
            }

            // Exhaustion: sagging sulphur halo above the panting beast
            if (blubberStunTimer > 0)
            {
                float sag = 0.3f + 0.2f * MathF.Sin(ticksRunning * 0.12f);
                Color halo = SulphurYellow * sag;
                halo.A = 0;
                spriteBatch.Draw(pixel, npc.Center + new Vector2(0f, -80f) - screenPos, new Rectangle(0, 0, 1, 1), halo, 0f, new Vector2(0.5f), new Vector2(90f, 5f), SpriteEffects.None, 0f);
            }
        }
        #endregion

        private static Vector2 DirectedHoverSpotStatic(NPC npc, Player target, float sideOffset, float heightOffset)
        {
            float side = Math.Sign(npc.Center.X - target.Center.X);
            if (side == 0f) side = 1f;
            return target.Center + new Vector2(side * sideOffset, heightOffset);
        }

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
