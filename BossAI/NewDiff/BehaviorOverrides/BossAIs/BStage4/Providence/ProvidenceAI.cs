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
    // 普罗维登斯 — 高维晶体能量神明. 设计文档: 大计划/I 普罗维登斯/普罗维登斯_重置版设计文档.md
    // 移动哲学(分寸感): 神明不追人 — 她在固定晶核结界的高位车道之间用"翼展滑翔"换位
    // (冲量起步→滑行→减速停驻), 出手时几乎悬停不动, 威压来自编排好的弹幕与结界机关, 而非贴脸.
    // 结界锚点(arenaCenter)开战时落定, 只以极缓速度跟随玩家 — 结界、折射光网、越界惩罚全部
    // 以锚点为基准, 修复旧版"结界跟着Boss乱飘"的问题.
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

        private static readonly Color HolyGold = new(255, 220, 100);
        private static readonly Color HolyRed = new(255, 90, 60);
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
        private readonly float[] crystalFlash = new float[3]; // hit feedback per crystal

        // Profaned Crystal Cage — anchored, not glued to the boss
        private Vector2 arenaCenter = Vector2.Zero;
        private int refractionTimer = 0;
        private bool refractionHitThisActivation = false;
        private int arenaHurtCooldown = 0;
        private float transitionFlashAlpha = 0f;

        // Per-attack A/B variant toggle: flips deterministically each time that attack comes up (no RNG).
        private readonly bool[] attackVariant = new bool[19];
        private bool UseVariantB(AttackState state)
        {
            int i = (int)state;
            bool v = attackVariant[i];
            attackVariant[i] = !v;
            return v;
        }
        private bool currentVariantB = false;
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

            npc.defense = npc.defDefense;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;

            if (currentPhase == 0)
            {
                currentPhase = 1;
                npc.ai[0] = 1f;
                state = AttackState.HolyCollider;
                npc.ai[1] = (float)state;
                currentRepetition = 0;
                attackCycleIndex = 0;
                currentVariantB = UseVariantB(state);
                arenaCenter = target.Center; // the cage locks onto the battlefield, not the goddess
                npc.netUpdate = true;
            }

            // The cage drifts only barely — it is a place, not a leash tied to anyone's back
            arenaCenter = Vector2.Lerp(arenaCenter, target.Center, 0.005f);

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
                CleanupHeldWeapons(npc);
                npc.netUpdate = true;
            }

            float borderSize = currentPhase == 1 ? 1500f : 1100f;
            UpdateArenaBorder(npc, target, borderSize);
            UpdateRefractionLaser(npc, target, borderSize);
            UpdateCrystalsRespawn(npc);
            if (crystalFxCooldown > 0) crystalFxCooldown--;
            if (arenaHurtCooldown > 0) arenaHurtCooldown--;
            for (int i = 0; i < 3; i++)
                if (crystalFlash[i] > 0f) crystalFlash[i] -= 0.08f;

            // Ambient: holy embers rise through the cage, thickening as the goddess weakens
            float emberIntensity = 0.15f + (1f - lifeRatio) * 0.3f;
            if (Main.rand.NextFloat() < emberIntensity)
            {
                Vector2 spawnPos = target.Center + new Vector2(Main.rand.NextFloat(-900f, 900f), Main.rand.NextFloat(300f, 500f));
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Torch, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(1.5f, 3.5f)), 150, default, Main.rand.NextFloat(0.9f, 1.5f));
                d.noGravity = true;
                d.fadeIn = 1.1f;
            }

            if (stunTimer > 0)
            {
                // 神力反噬 — wings drooped, body sagging, embers bleeding out. The 8s punish window is a spectacle.
                stunTimer--;
                npc.velocity.X *= 0.95f;
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + 0.06f, -2f, 1.6f); // slow sag
                npc.rotation = MathF.Sin(ticksRunning * 0.07f) * 0.12f;
                npc.damage = 0;
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(90f, 70f), DustID.Torch, new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(1f, 3f)), 120, default, 1.4f);
                    d.noGravity = true;
                }
                if (stunTimer == 0)
                {
                    // Recovery flare — the goddess reignites
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f }, npc.Center);
                    ProvFx.Burst(npc.Center, 8f, 30);
                    target.Calamity().GeneralScreenShakePower = 6f;
                }
            }
            else
            {
                npc.damage = npc.defDamage;
                npc.rotation = npc.velocity.X * 0.02f;

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

            data.CurrentPhase = currentPhase;
            data.AttackState = (IUMWAttackState)Math.Clamp((int)state, 0, 4);
            data.PatternTimer = (int)timer;

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) => ProcessCrystalHits(npc, player.Center, ref modifiers, item.damage);
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) => ProcessCrystalHits(npc, projectile.Center, ref modifiers, projectile.damage);
        #endregion

        #region Arena, Refraction & Crystals
        private void UpdateArenaBorder(NPC npc, Player target, float borderSize)
        {
            float half = borderSize / 2f;

            // The gold/red cage frame must be SEEN: embers trace the square perimeter
            for (int i = 0; i < 4; i++)
            {
                // Pick a random point on the square's perimeter
                float t = Main.rand.NextFloat(4f);
                Vector2 pos;
                if (t < 1f) pos = arenaCenter + new Vector2(MathHelper.Lerp(-half, half, t), -half);
                else if (t < 2f) pos = arenaCenter + new Vector2(half, MathHelper.Lerp(-half, half, t - 1f));
                else if (t < 3f) pos = arenaCenter + new Vector2(MathHelper.Lerp(half, -half, t - 2f), half);
                else pos = arenaCenter + new Vector2(-half, MathHelper.Lerp(half, -half, t - 3f));

                Dust d = Dust.NewDustPerfect(pos, Main.rand.NextBool() ? DustID.Torch : DustID.CrimsonTorch, Vector2.Zero, 150, default, 1.15f);
                d.noGravity = true;
                d.fadeIn = 1f;
            }

            Vector2 dist = target.Center - arenaCenter;
            // Densify the wall the player is drifting toward
            if (Math.Abs(dist.X) > half - 160f || Math.Abs(dist.Y) > half - 160f)
            {
                Vector2 edge = arenaCenter + new Vector2(
                    Math.Abs(dist.X) > half - 160f ? Math.Sign(dist.X) * half : dist.X,
                    Math.Abs(dist.Y) > half - 160f ? Math.Sign(dist.Y) * half : dist.Y);
                for (int i = 0; i < 2; i++)
                {
                    Dust d = Dust.NewDustPerfect(edge + Main.rand.NextVector2Circular(90f, 90f), DustID.CrimsonTorch, Vector2.Zero, 100, default, 1.4f);
                    d.fadeIn = 1.2f;
                    d.noGravity = true;
                }
            }

            if (Math.Abs(dist.X) > half || Math.Abs(dist.Y) > half)
            {
                target.AddBuff(BuffID.Daybreak, 180); // Profaned Weakness
                target.AddBuff(BuffID.BrokenArmor, 120);
                if (arenaHurtCooldown <= 0)
                {
                    arenaHurtCooldown = 30;
                    target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 25, 0);
                }
            }
        }

        // 晶格折射 (every 8s): corner prisms flash a warning X for 1s, then the refracted holy net goes live for 1.5s.
        private void UpdateRefractionLaser(NPC npc, Player target, float borderSize)
        {
            refractionTimer++;
            if (refractionTimer >= 480)
            {
                refractionTimer = 0;
                refractionHitThisActivation = false;
            }

            bool warning = refractionTimer >= 330 && refractionTimer < 390;
            bool live = refractionTimer >= 390;

            if (refractionTimer == 330)
                SoundEngine.PlaySound(SoundID.Item60 with { Volume = 0.7f }, npc.Center);
            if (refractionTimer == 390)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.8f }, npc.Center);
                ProvFx.Burst(arenaCenter, 5f, 14);
            }

            float half = borderSize / 2f;
            Vector2 topLeft = arenaCenter + new Vector2(-half, -half);
            Vector2 topRight = arenaCenter + new Vector2(half, -half);
            Vector2 bottomLeft = arenaCenter + new Vector2(-half, half);
            Vector2 bottomRight = arenaCenter + new Vector2(half, half);

            // Prism glow: dust condenses at the four corners during the warning
            if (warning && Main.rand.NextBool(2))
            {
                Vector2[] corners = { topLeft, topRight, bottomLeft, bottomRight };
                foreach (Vector2 c in corners)
                {
                    Dust d = Dust.NewDustPerfect(c + Main.rand.NextVector2Circular(30f, 30f), DustID.GoldFlame, (c - arenaCenter).SafeNormalize(Vector2.UnitX) * -0.5f, 100, default, 1.3f);
                    d.noGravity = true;
                }
            }

            // Live beams shed sparks along their length
            if (live && Main.rand.NextBool(2))
            {
                float lerp = Main.rand.NextFloat();
                Vector2 onLine = Main.rand.NextBool() ? Vector2.Lerp(topLeft, bottomRight, lerp) : Vector2.Lerp(topRight, bottomLeft, lerp);
                Dust d = Dust.NewDustPerfect(onLine, DustID.GoldFlame, Main.rand.NextVector2Circular(1.5f, 1.5f), 100, default, 1.2f);
                d.noGravity = true;
            }

            if (live && !refractionHitThisActivation)
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

        private void UpdateCrystalsRespawn(NPC npc)
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
                    // Re-condensation ceremony: three colored rings snap back into orbit
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.8f }, npc.Center);
                    ProvFx.Burst(CrystalPos(npc, 0), 4f, 12);
                    ProvFx.Burst(CrystalPos(npc, 1), 4f, 12, DustID.Torch);
                    ProvFx.Burst(CrystalPos(npc, 2), 4f, 12, DustID.PurpleTorch);
                }
            }
        }

        private Vector2 CrystalPos(NPC npc, int index) => npc.Center + (ticksRunning * 0.03f + index * MathHelper.TwoPi / 3f).ToRotationVector2() * 120f;

        private void ProcessCrystalHits(NPC npc, Vector2 hitPos, ref NPC.HitModifiers modifiers, int damage)
        {
            if (stunTimer > 0)
            {
                modifiers.FinalDamage *= 1.5f; // all crystals broken — 150% damage taken (design doc)
                return;
            }

            int activeCount = 0;
            if (yellowCrystalHP > 0f) activeCount++;
            if (orangeCrystalHP > 0f) activeCount++;
            if (purpleCrystalHP > 0f) activeCount++;
            if (activeCount > 0)
                modifiers.FinalDamage *= (1f - 0.30f * activeCount); // up to 90% DR

            for (int i = 0; i < 3; i++)
            {
                ref float hp = ref (i == 0 ? ref yellowCrystalHP : ref (i == 1 ? ref orangeCrystalHP : ref purpleCrystalHP));
                if (hp <= 0f || Vector2.Distance(hitPos, CrystalPos(npc, i)) >= 80f)
                    continue;

                hp -= damage;
                crystalFlash[i] = 1f;
                if (crystalFxCooldown <= 0)
                {
                    crystalFxCooldown = 8;
                    SoundEngine.PlaySound(SoundID.NPCHit5 with { Volume = 0.4f }, CrystalPos(npc, i));
                }
                if (hp <= 0f)
                {
                    int dustType = i == 0 ? DustID.GoldFlame : i == 1 ? DustID.Torch : DustID.PurpleTorch;
                    SoundEngine.PlaySound(SoundID.NPCDeath4, CrystalPos(npc, i));
                    ProvFx.Burst(CrystalPos(npc, i), 5f, 16, dustType);
                    CheckAllCrystalsBroken(npc);
                }
                break;
            }
        }

        private void CheckAllCrystalsBroken(NPC npc)
        {
            if (yellowCrystalHP <= 0f && orangeCrystalHP <= 0f && purpleCrystalHP <= 0f)
            {
                stunTimer = 480; // 8s stun (design doc)
                npc.velocity = Vector2.Zero;
                SoundEngine.PlaySound(SoundID.NPCHit53, npc.Center);
                SoundEngine.PlaySound(SoundID.Item78 with { Volume = 0.8f, Pitch = -0.5f }, npc.Center);
                ProvFx.Burst(npc.Center, 7f, 30);
                ProvFx.Burst(npc.Center, 4f, 16, DustID.CrimsonTorch);
                if (Main.netMode != NetmodeID.Server)
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower = 10f;
            }
        }
        #endregion

        #region Movement Helpers
        private void SmoothMove(NPC npc, Vector2 desiredPosition, float acceleration, float maxSpeed)
        {
            Vector2 desiredVelocity = (desiredPosition - npc.Center) * acceleration;
            if (desiredVelocity.Length() > maxSpeed)
                desiredVelocity = Vector2.Normalize(desiredVelocity) * maxSpeed;
            npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, 0.13f);
        }

        private static Vector2 DirectedHoverSpot(NPC npc, Player target, float sideOffset, float heightOffset, float lead = 0f)
        {
            float side = Math.Sign(npc.Center.X - target.Center.X);
            if (side == 0f) side = Main.rand.NextBool() ? 1f : -1f;
            Vector2 predicted = target.Center + target.velocity * lead;
            return predicted + new Vector2(side * sideOffset, heightOffset);
        }

        // Wing-flare glide: a committed repositioning impulse — the goddess's dash-equivalent.
        // Hard launch, then the caller decays it; embers burst off the wings at the moment of launch.
        private void WingGlide(NPC npc, Vector2 dest, float speed = 16f)
        {
            npc.velocity = SafeNormalize(dest - npc.Center, -Vector2.UnitY) * speed;
            SoundEngine.PlaySound(SoundID.Item32 with { Volume = 0.45f, Pitch = 0.2f }, npc.Center);
            ProvFx.Burst(npc.Center, 4.5f, 14);
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

        // Charge-up shimmer: gold dust drawn into the goddess before a volley — every attack telegraphs.
        private static void ChargeShimmer(NPC npc, int density = 2)
        {
            if (!Main.rand.NextBool(density))
                return;
            Vector2 around = npc.Center + Main.rand.NextVector2CircularEdge(110f, 110f);
            Dust d = Dust.NewDustPerfect(around, DustID.GoldFlame, (npc.Center - around) * 0.08f, 100, default, 1.2f);
            d.fadeIn = 1.2f;
            d.noGravity = true;
        }
        #endregion

        #region Attack Rotation
        private void RotateAttack(NPC npc, int currentPhase, AttackState current)
        {
            CleanupHeldWeapons(npc);
            if (currentPhase == 1)
            {
                currentRepetition++;
                if (currentRepetition < 3)
                {
                    // Same rite again, but the A/B read flips so 3 reps never feel like 3 copies
                    currentVariantB = UseVariantB(current);
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

        #region P1 Attack States
        // 神圣碰撞器 · 圣火刀痕 — 变体A: 单道锁向刀痕连锁爆柱; 变体B: 左右双刀痕交叉成V, 双排火柱夹击.
        private void ExecuteHolyCollider(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 40f, -300f, 8f), 14f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldHolyCollider>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 12 && timer < 40)
            {
                npc.velocity *= 0.94f; // glide settles — the sword raises in stillness
                ChargeShimmer(npc);
            }

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<HolyColliderTrailProj>(), npc.damage / 3, 0f, Main.myPlayer, dir.X, dir.Y);
                if (currentVariantB)
                {
                    // The mirrored slash: a second trail from the player's other flank, crossing the first
                    Vector2 mirrorStart = target.Center + new Vector2(-Math.Sign(dir.X == 0f ? 1f : dir.X) * 500f, -300f);
                    Vector2 mirrorDir = SafeNormalize(target.Center - mirrorStart, Vector2.UnitY);
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), mirrorStart, Vector2.Zero, ModContent.ProjectileType<HolyColliderTrailProj>(), npc.damage / 3, 0f, Main.myPlayer, mirrorDir.X, mirrorDir.Y);
                    if (idx >= 0) Main.projectile[idx].netUpdate = true;
                }
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.9f, Pitch = -0.2f }, npc.Center);
                ProvFx.Burst(npc.Center, 5f, 12);
                FindHeldWeapon<ProvHeldHolyCollider>(npc)?.Pulse(16f);
            }

            if (timer > 40 && timer < 110)
                npc.velocity *= 0.97f;
            else if (timer >= 110)
                SmoothMove(npc, DirectedHoverSpot(npc, target, 200f, -280f, 4f), 0.05f, 10f);

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.HolyCollider);
        }

        // 燃烧启示录 — 变体A: 单核投掷(内缩外扩双环由弹幕自理); 变体B: 双核先后掷向玩家两翼, 双重夹层.
        private void ExecuteBurningRevelation(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 320f, -240f, 5f), 13f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldBurningRevelation>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 12 && timer < 30)
            {
                npc.velocity *= 0.94f;
                ChargeShimmer(npc);
            }

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 5f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir, ModContent.ProjectileType<RevelationCoreProj>(), npc.damage / 3, 0f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item73 with { Volume = 0.7f }, npc.Center);
                FindHeldWeapon<ProvHeldBurningRevelation>(npc)?.Pulse(-10f);
            }

            if (currentVariantB && timer == 62 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Second core lobbed at the player's other flank — two pinch-rings overlapping
                Vector2 flank = target.Center + new Vector2(-Math.Sign(npc.Center.X - target.Center.X) * 260f, 0f);
                Vector2 dir2 = SafeNormalize(flank - npc.Center, Vector2.UnitY) * 5.5f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir2, ModContent.ProjectileType<RevelationCoreProj>(), npc.damage / 3, 0f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item73 with { Volume = 0.7f, Pitch = 0.2f }, npc.Center);
                FindHeldWeapon<ProvHeldBurningRevelation>(npc)?.Pulse(-10f);
            }

            if (timer > 30)
                SmoothMove(npc, DirectedHoverSpot(npc, target, 340f, -240f, 5f), 0.05f, 9f);

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.BurningRevelation);
        }

        // 大地耀目 · 平行光矢 — 变体A: 四道横矢同时落位(±150/±50), 窄道站位题;
        // 变体B: 先外二后内二错时落位, 窄道会"变窄" — 中途必须换道.
        private void ExecuteTelluricGlare(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 0f, -340f, 0f), 13f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<ProvHeldTelluricGlare>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 12)
            {
                npc.velocity *= 0.95f;
                FindHeldWeapon<ProvHeldTelluricGlare>(npc)?.SetAim((target.Center - npc.Center).ToRotation());
            }
            if (timer > 12 && timer < 24)
                ChargeShimmer(npc, 1);

            if (timer == 24 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int[] offsets = currentVariantB ? new[] { -150, 150 } : new[] { -150, -50, 50, 150 };
                foreach (int off in offsets)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + new Vector2(0f, off), Vector2.Zero, ModContent.ProjectileType<TelluricBeamProj>(), npc.damage / 3, 0f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item75 with { Volume = 0.8f }, npc.Center);
                FindHeldWeapon<ProvHeldTelluricGlare>(npc)?.Pulse(12f);
            }

            if (currentVariantB && timer == 48 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // The inner pair lands late — the safe lane halves mid-attack
                foreach (int off in new[] { -50, 50 })
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + new Vector2(0f, off), Vector2.Zero, ModContent.ProjectileType<TelluricBeamProj>(), npc.damage / 3, 0f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item75 with { Volume = 0.8f, Pitch = 0.25f }, npc.Center);
                FindHeldWeapon<ProvHeldTelluricGlare>(npc)?.Pulse(12f);
            }

            if (timer >= 155)
                RotateAttack(npc, phase, AttackState.TelluricGlare);
        }

        // 至福轰炸器 — 变体A: 单发龙首导弹(近身裂环由弹幕自理); 变体B: 双翼齐射两发, 合围从两个方向到来.
        private void ExecuteBlissfulBombardier(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 300f, -220f, 6f), 13f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<ProvHeldBlissfulBombardier>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 12 && timer < 30)
            {
                npc.velocity *= 0.94f;
                ChargeShimmer(npc);
            }

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (currentVariantB)
                {
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy(s * 0.55f) * 8f;
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<BombardierRocketProj>(), npc.damage / 3, 0f, Main.myPlayer);
                    }
                }
                else
                {
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 8f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<BombardierRocketProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.7f }, npc.Center);
                ProvFx.Burst(npc.Center, 4f, 10);
                FindHeldWeapon<ProvHeldBlissfulBombardier>(npc)?.Pulse(-12f);
            }

            if (timer > 30)
                SmoothMove(npc, DirectedHoverSpot(npc, target, 320f, -220f, 6f), 0.05f, 9f);

            if (timer >= 190)
                RotateAttack(npc, phase, AttackState.BlissfulBombardier);
        }

        // 净化吞食者 · 三角激光阵 — 变体A: 正三角锁玩家当前位; 变体B: 倒三角锁玩家预判位, 阵面旋转60°.
        private void ExecutePurgeGuzzler(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 0f, -320f, 4f), 13f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldPurgeGuzzler>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 12 && timer < 24)
            {
                npc.velocity *= 0.94f;
                ChargeShimmer(npc, 1);
            }

            if (timer == 24 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 anchor = currentVariantB ? target.Center + target.velocity * 22f : target.Center;
                float baseAngle = currentVariantB ? MathHelper.Pi / 3f : 0f;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pt = anchor + (i * MathHelper.TwoPi / 3f + baseAngle - MathHelper.PiOver2).ToRotationVector2() * 240f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pt, Vector2.Zero, ModContent.ProjectileType<PurgeCoreProj>(), npc.damage / 3, 0f, Main.myPlayer, anchor.X, anchor.Y);
                }
                SoundEngine.PlaySound(SoundID.Item78 with { Volume = 0.7f }, anchor);
                FindHeldWeapon<ProvHeldPurgeGuzzler>(npc)?.Pulse(10f);
            }

            if (timer > 24)
                SmoothMove(npc, DirectedHoverSpot(npc, target, 260f, -300f, 4f), 0.045f, 8f);

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.PurgeGuzzler);
        }

        // 炫目刺击 · 飞天神枪 — 变体A: 单枪垂落玩家头顶(落点碎屑瀑布); 变体B: 三枪错时落位左中右, 逼定时横穿.
        private void ExecuteDazzlingStabber(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 260f, -280f, 6f), 13f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldDazzlingStabber>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 12 && timer < 30)
            {
                npc.velocity *= 0.94f;
                ChargeShimmer(npc);
            }

            int[] spearTimes = currentVariantB ? new[] { 30, 55, 80 } : new[] { 30 };
            foreach (int st in spearTimes)
            {
                if (timer == st && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float xOff = currentVariantB ? (Array.IndexOf(spearTimes, st) - 1) * 260f : 0f;
                    Vector2 spawn = target.Center + new Vector2(xOff, -600f);
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(0f, 15f), ModContent.ProjectileType<DazzlingSpearProj>(), npc.damage / 3, 0f, Main.myPlayer);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[0] = target.Center.Y;
                    SoundEngine.PlaySound(SoundID.Item60 with { Volume = 0.6f, Pitch = 0.1f }, spawn);
                    FindHeldWeapon<ProvHeldDazzlingStabber>(npc)?.Pulse(14f);
                }
            }

            if (timer > 30)
                SmoothMove(npc, DirectedHoverSpot(npc, target, 280f, -260f, 6f), 0.05f, 9f);

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.DazzlingStabber);
        }

        // 熔火截肢者 · 重力熔岩镖 — 变体A: 单镖直取(沿途落熔滴); 变体B: 双镖自两翼交叉, 熔滴幕从两边合拢.
        private void ExecuteMoltenAmputator(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 300f, -240f, 6f), 13f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<ProvHeldMoltenAmputator>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 12 && timer < 30)
            {
                npc.velocity *= 0.94f;
                // Molten sparks dripping off the raised sickle
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(60f, 40f), DustID.Torch, new Vector2(0f, Main.rand.NextFloat(1f, 2.5f)), 100, default, 1.3f);
                    d.noGravity = true;
                }
            }

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 11f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<MoltenSickleProj>(), npc.damage / 3, 0f, Main.myPlayer);
                if (currentVariantB)
                {
                    Vector2 mirrorSpawn = target.Center + new Vector2(-Math.Sign(npc.Center.X - target.Center.X) * 560f, -240f);
                    Vector2 vel2 = SafeNormalize(target.Center - mirrorSpawn, Vector2.UnitY) * 11f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), mirrorSpawn, vel2, ModContent.ProjectileType<MoltenSickleProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f }, npc.Center);
                ProvFx.Burst(npc.Center, 4f, 10, DustID.Torch);
                FindHeldWeapon<ProvHeldMoltenAmputator>(npc)?.Pulse(12f);
            }

            if (timer > 30)
                SmoothMove(npc, DirectedHoverSpot(npc, target, 320f, -220f, 6f), 0.05f, 9f);

            if (timer >= 190)
                RotateAttack(npc, phase, AttackState.MoltenAmputator);
        }

        // 圣洁怒火 · 纯白火风暴 — 变体A: 左→右 120° 洗礼横扫; 变体B: 右→左, 且中段留一个两拍的喘息缺口.
        private void ExecutePristineFury(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 0f, -280f, 4f), 13f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldPristineFury>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 12 && timer < 30)
            {
                npc.velocity *= 0.94f;
                ChargeShimmer(npc, 1); // the body trembles as the flame builds (design doc)
            }

            if (timer >= 30 && timer <= 130 && timer % 5 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Variant B: the sweep runs mirrored and skips two beats mid-arc — a readable breath to cross through
                bool inGap = currentVariantB && timer >= 70 && timer <= 85;
                if (!inGap)
                {
                    float sweep = (timer - 30f) / 100f;
                    float angle = currentVariantB ? MathHelper.Lerp(1.05f, -1.05f, sweep) : MathHelper.Lerp(-1.05f, 1.05f, sweep);
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy(angle) * 9f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<PristineFlameProj>(), npc.damage / 3, 0f, Main.myPlayer);
                    FindHeldWeapon<ProvHeldPristineFury>(npc)?.Pulse(4f);
                }
            }

            if (timer > 30)
                npc.velocity *= 0.97f; // rooted mid-sweep — the storm turns, not the goddess

            if (timer >= 170)
                RotateAttack(npc, phase, AttackState.PristineFury);
        }
        #endregion

        #region P2 Attack States
        // 以太通量炮 — 变体A: 三连弧线变轨激光; 变体B: 三发自交替高低位滑翔点射出, 弧线从不同方向咬合.
        private void ExecuteAetherfluxCannon(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 360f, -200f, 6f), 15f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<ProvHeldAetherfluxCannon>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 12 && timer < 30)
            {
                npc.velocity *= 0.94f;
                ChargeShimmer(npc, 1);
            }

            if ((timer == 30 || timer == 55 || timer == 80) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 10f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<AetherfluxLaserProj>(), npc.damage / 3, 0f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item91 with { Volume = 0.7f, Pitch = 0.1f }, npc.Center);
                ProvFx.Burst(npc.Center, 4f, 8);
                FindHeldWeapon<ProvHeldAetherfluxCannon>(npc)?.Pulse(10f);

                // Variant B: glide to a new firing perch between shots — the arcs bite from changing directions
                if (currentVariantB && timer < 80)
                {
                    float height = timer == 30 ? -340f : -80f;
                    WingGlide(npc, DirectedHoverSpot(npc, target, 380f, height, 4f), 14f);
                }
            }

            if (timer > 80)
                npc.velocity *= 0.96f;

            if (timer >= 140)
                RotateAttack(npc, phase, AttackState.AetherfluxCannon);
        }

        // 天使霰弹枪 — 变体A: 六珠一轮反弹散射; 变体B: 两轮各四珠, 两轮之间滑翔换位, 反弹网交织.
        private void ExecuteAngelicShotgun(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 380f, -160f, 8f), 15f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<ProvHeldAngelicShotgun>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 12 && timer < 40)
            {
                npc.velocity *= 0.94f;
                FindHeldWeapon<ProvHeldAngelicShotgun>(npc)?.SetAim((target.Center - npc.Center).ToRotation());
                ChargeShimmer(npc, 1);
            }

            int pelletCount = currentVariantB ? 4 : 6;
            if ((timer == 40 || (currentVariantB && timer == 90)) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 baseDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                for (int i = 0; i < pelletCount; i++)
                {
                    Vector2 vel = baseDir.RotatedBy((i - (pelletCount - 1) / 2f) * 0.15f) * 9f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<AngelicPelletProj>(), npc.damage / 3, 0f, Main.myPlayer, 550f, arenaCenter.X, arenaCenter.Y);
                }
                SoundEngine.PlaySound(SoundID.Item36 with { Volume = 0.8f }, npc.Center);
                ProvFx.Burst(npc.Center + baseDir * 50f, 5f, 10);
                FindHeldWeapon<ProvHeldAngelicShotgun>(npc)?.Pulse(8f);

                if (currentVariantB && timer == 40)
                    WingGlide(npc, DirectedHoverSpot(npc, target, -380f, -220f, 6f), 15f);
            }

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.AngelicShotgun);
        }

        // 暗黑火花 — 变体A: 单星十字虚空射线; 变体B: 双星先后炸开, 十字网错位旋切.
        private void ExecuteDarkSpark(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 320f, -260f, 4f), 14f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldDarkSpark>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 12 && timer < 20 && Main.rand.NextBool(2))
            {
                // Void motes — this one is the profaned, not the holy
                Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(70f, 70f), DustID.PurpleTorch, -Vector2.UnitY * Main.rand.NextFloat(1f, 2f), 100, default, 1.2f);
                d.noGravity = true;
            }

            if (timer == 20 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawn = target.Center + new Vector2(Main.rand.NextFloat(-200f, 200f), -60f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<DarkSparkProj>(), npc.damage / 3, 0f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.6f, Pitch = -0.2f }, spawn);
                FindHeldWeapon<ProvHeldDarkSpark>(npc)?.Pulse(10f);
            }
            if (currentVariantB && timer == 55 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawn = target.Center + new Vector2(Main.rand.NextFloat(-200f, 200f), 80f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<DarkSparkProj>(), npc.damage / 3, 0f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.6f, Pitch = 0.1f }, spawn);
                FindHeldWeapon<ProvHeldDarkSpark>(npc)?.Pulse(10f);
            }

            if (timer > 20)
                SmoothMove(npc, DirectedHoverSpot(npc, target, 340f, -240f, 4f), 0.05f, 9f);

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.DarkSpark);
        }

        // 星河吞噬之刃 — 变体A: 随机流星雨; 变体B: 流星按横向行进波依次砸落, 一堵会走路的星墙.
        private void ExecuteGalactusBlade(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 220f, -320f, 0f), 14f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldGalactusBlade>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                tracker = Main.rand.NextBool() ? 1f : -1f; // variant B: wave direction
            }

            if (timer > 12 && timer < 30)
            {
                npc.velocity *= 0.94f;
                ChargeShimmer(npc, 1);
            }

            if (timer >= 30 && timer <= 130 && timer % 8 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float x;
                if (currentVariantB)
                {
                    // Marching wave: meteors sweep across the cage in order
                    float progress = (timer - 30f) / 100f;
                    x = tracker > 0f ? MathHelper.Lerp(-420f, 420f, progress) : MathHelper.Lerp(420f, -420f, progress);
                }
                else
                {
                    x = Main.rand.NextFloat(-400f, 400f);
                }
                Vector2 spawn = target.Center + new Vector2(x, -600f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(Main.rand.NextFloat(-2f, 2f), 13f), ModContent.ProjectileType<GalactusMeteorProj>(), npc.damage / 3, 0f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item88 with { Volume = 0.4f, Pitch = -0.2f }, spawn);
            }

            if (timer > 30)
                npc.velocity *= 0.97f;

            if (timer >= 170)
                RotateAttack(npc, phase, AttackState.GalactusBlade);
        }

        // 卡兰德拉之镜 — 变体A: 镜落玩家脚下; 变体B: 镜镇结界中心 + 三轮天使弹逼停火角度.
        private void ExecuteMirrorOfKalandra(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 0f, -340f, 0f), 13f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldMirrorOfKalandra>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                    Vector2 mirrorPos = currentVariantB ? arenaCenter : target.Center + new Vector2(0f, 150f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), mirrorPos, Vector2.Zero, ModContent.ProjectileType<KalandraMirrorProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item160, npc.Center);
            }

            if (currentVariantB && (timer == 50 || timer == 85 || timer == 120) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 baseDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                for (int i = -1; i <= 1; i++)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, baseDir.RotatedBy(i * 0.14f) * 9f, ModContent.ProjectileType<AngelicPelletProj>(), npc.damage / 3, 0f, Main.myPlayer, 550f, arenaCenter.X, arenaCenter.Y);
                SoundEngine.PlaySound(SoundID.Item36 with { Volume = 0.6f }, npc.Center);
            }

            SmoothMove(npc, DirectedHoverSpot(npc, target, 260f, -320f, 0f), 0.04f, 8f);

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.MirrorOfKalandra);
        }

        // 哀悼之星 — 变体A: 双螺旋火线; 变体B: 三线相位差 120°, 更密的编织网.
        private void ExecuteMourningstar(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 0f, -300f, 0f), 13f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldMourningstar>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                    float[] offsets = currentVariantB
                        ? new[] { 0f, MathHelper.TwoPi / 3f, MathHelper.TwoPi * 2f / 3f }
                        : new[] { 0f, MathHelper.Pi };
                    foreach (float phaseOffset in offsets)
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<MourningstarLineProj>(), npc.damage / 3, 0f, Main.myPlayer, npc.whoAmI, phaseOffset);
                }
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f }, npc.Center);
            }

            // The helix turns around a slowly gliding pivot — never parked directly on the player
            SmoothMove(npc, DirectedHoverSpot(npc, target, 200f, -260f, 4f), 0.04f, 7f);

            if (timer >= 180)
                RotateAttack(npc, phase, AttackState.Mourningstar);
        }

        // 破晓碎光 — 变体A: 法盘掷向玩家(24向爆裂由弹幕自理); 变体B: 法盘镇结界中心炸, 位置题而非反应题.
        private void ExecuteShatteredDawn(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 300f, -240f, 6f), 14f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<ProvHeldShatteredDawn>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 12 && timer < 30)
            {
                npc.velocity *= 0.94f;
                ChargeShimmer(npc);
            }

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 aim = currentVariantB ? arenaCenter : target.Center;
                Vector2 vel = SafeNormalize(aim - npc.Center, Vector2.UnitY) * 6f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<ShatteredDiscProj>(), npc.damage / 3, 0f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item68 with { Volume = 0.7f }, npc.Center);
                ProvFx.Burst(npc.Center, 4f, 10);
                FindHeldWeapon<ProvHeldShatteredDawn>(npc)?.Pulse(10f);
            }

            if (timer > 30)
                SmoothMove(npc, DirectedHoverSpot(npc, target, 320f, -240f, 6f), 0.05f, 9f);

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.ShatteredDawn);
        }

        // 追踪灼炎 — 变体A: 单环公转留火; 变体B: 环 + 两轮通量激光, 圈内也不得安逸.
        private void ExecuteSeekingScorcher(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 340f, -260f, 4f), 14f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldSeekingScorcher>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 12 && timer < 30)
            {
                npc.velocity *= 0.94f;
                ChargeShimmer(npc, 1);
            }

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<SeekingScorcherRingProj>(), npc.damage / 3, 0f, Main.myPlayer, 0f);
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.7f }, npc.Center);
            }

            if (currentVariantB && (timer == 90 || timer == 140) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 10f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<AetherfluxLaserProj>(), npc.damage / 3, 0f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item91 with { Volume = 0.6f }, npc.Center);
                ProvFx.Burst(npc.Center, 4f, 8);
            }

            if (timer > 30)
                SmoothMove(npc, DirectedHoverSpot(npc, target, 360f, -240f, 4f), 0.045f, 8f);

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.SeekingScorcher);
        }

        // 大漩涡 — 变体A: 漩涡锁玩家位; 变体B: 漩涡镇结界中心 + 十字通量光配合吸力(设计文档的对射).
        private void ExecuteMaelstrom(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 0f, -320f, 0f), 13f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldMaelstrom>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 12 && timer < 30)
            {
                npc.velocity *= 0.94f;
                ChargeShimmer(npc, 1);
            }

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 pos = currentVariantB ? arenaCenter : target.Center;
                Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<MaelstromVortexProj>(), npc.damage / 3, 0f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item78 with { Volume = 0.8f, Pitch = -0.2f }, pos);
                FindHeldWeapon<ProvHeldMaelstrom>(npc)?.Pulse(10f);
            }

            if (currentVariantB && timer == 80 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // The cross-aurora: four flux lasers in a + around the vortex, fired while the pull drags inward
                for (int i = 0; i < 4; i++)
                {
                    Vector2 vel = (i * MathHelper.PiOver2).ToRotationVector2() * 9f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), arenaCenter, vel, ModContent.ProjectileType<AetherfluxLaserProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item91 with { Volume = 0.7f, Pitch = -0.15f }, arenaCenter);
            }

            SmoothMove(npc, DirectedHoverSpot(npc, target, 260f, -300f, 0f), 0.04f, 8f);

            if (timer >= 180)
                RotateAttack(npc, phase, AttackState.Maelstrom);
        }

        // 王子随从 — 变体A: 召唤王子; 变体B: 王子 + 两轮天使弹掩护(魔像投火时神明亲自压制).
        private void ExecutePrince(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                WingGlide(npc, DirectedHoverSpot(npc, target, 280f, -280f, 0f), 13f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ProvHeldPrince>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 12 && timer < 30)
            {
                npc.velocity *= 0.94f;
                ChargeShimmer(npc);
            }

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int princeType = ModContent.Find<ModNPC>("CalamityMod/ProvidenceGuardianOffensive").Type;
                int p = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y + 120, princeType);
                if (p >= 0 && p < Main.maxNPCs)
                {
                    Main.npc[p].ai[0] = npc.whoAmI;
                    Main.npc[p].netUpdate = true;
                }
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.8f, Pitch = -0.3f }, npc.Center);
                ProvFx.Burst(npc.Center + new Vector2(0f, 120f), 5f, 16);
                FindHeldWeapon<ProvHeldPrince>(npc)?.Pulse(8f);
            }

            if (currentVariantB && (timer == 80 || timer == 130) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 baseDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                for (int i = -1; i <= 1; i++)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, baseDir.RotatedBy(i * 0.14f) * 9f, ModContent.ProjectileType<AngelicPelletProj>(), npc.damage / 3, 0f, Main.myPlayer, 550f, arenaCenter.X, arenaCenter.Y);
                SoundEngine.PlaySound(SoundID.Item36 with { Volume = 0.6f }, npc.Center);
            }

            if (timer > 30)
                SmoothMove(npc, DirectedHoverSpot(npc, target, 300f, -280f, 0f), 0.045f, 8f);

            if (timer >= 180)
                RotateAttack(npc, phase, AttackState.Prince);
        }

        // 形态转变 (50%): 金甲熔落, 结界收缩至 1100. 白闪保留, 追加熔甲飞屑与结界收缩尘埃波.
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

            // Molten armor sloughing off in embers throughout the reveal
            if (timer > 10 && timer < 80 && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(90f, 70f), Main.rand.NextBool() ? DustID.Torch : DustID.CrimsonTorch, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(1f, 4f)), 100, default, 1.5f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }

            if (timer == 45)
            {
                ProvFx.Burst(npc.Center, 8f, 40);
                ProvFx.Burst(npc.Center, 5f, 20, DustID.CrimsonTorch);
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.4f }, npc.Center);
                // Cage contraction wave: the old 1500 border collapses visibly onto the new 1100 line
                for (int i = 0; i < 48; i++)
                {
                    float a = i * MathHelper.TwoPi / 48f;
                    Vector2 pos = arenaCenter + a.ToRotationVector2() * 750f;
                    Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, -a.ToRotationVector2() * 5f, 100, default, 1.5f);
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

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tex = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() / 2f;

            // Trail only when actually moving with intent — a parked goddess shouldn't smear
            if (npc.velocity.Length() > 5f)
            {
                for (int i = 0; i < oldPositions.Length; i++)
                {
                    int idx = (oldPositionsIndex - i - 1 + oldPositions.Length) % oldPositions.Length;
                    if (oldPositions[idx] == Vector2.Zero) continue;
                    float alpha = (1f - i / (float)oldPositions.Length) * 0.4f;
                    spriteBatch.Draw(tex, oldPositions[idx] - screenPos, frame, new Color(255, 230, 120, 0) * alpha, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
                }
            }

            if (transitionFlashAlpha > 0f)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * transitionFlashAlpha);

            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float borderSize = (int)npc.ai[0] >= 2 ? 1100f : 1500f;
            float half = borderSize / 2f;

            // ---- Refraction net: warning flicker then live X-beams, corner prisms always visible ----
            bool warning = refractionTimer >= 330 && refractionTimer < 390;
            bool live = refractionTimer >= 390;
            Vector2[] corners =
            {
                arenaCenter + new Vector2(-half, -half), arenaCenter + new Vector2(half, -half),
                arenaCenter + new Vector2(-half, half), arenaCenter + new Vector2(half, half),
            };

            // Corner prisms: small gold diamonds, brightening as the net charges
            float prismGlow = live ? 1f : warning ? 0.5f + 0.5f * MathF.Sin(ticksRunning * 0.5f) : 0.35f;
            foreach (Vector2 c in corners)
            {
                Color prismColor = HolyGold * prismGlow;
                prismColor.A = 0;
                spriteBatch.Draw(pixel, c - screenPos, new Rectangle(0, 0, 1, 1), prismColor, MathHelper.PiOver4, new Vector2(0.5f), new Vector2(26f, 26f), SpriteEffects.None, 0f);
            }

            if (warning || live)
            {
                float lineWidth = live ? 20f : 3f + 2f * MathF.Sin(ticksRunning * 0.6f);
                Color lineColor = live ? HolyGold : HolyRed * 0.7f;
                lineColor.A = 0;
                foreach ((Vector2 a, Vector2 b) in new[] { (corners[0], corners[3]), (corners[1], corners[2]) })
                {
                    float len = Vector2.Distance(a, b);
                    float rot = (b - a).ToRotation();
                    spriteBatch.Draw(pixel, (a + b) * 0.5f - screenPos, new Rectangle(0, 0, 1, 1), lineColor * (live ? 0.85f : 0.6f), rot, new Vector2(0.5f), new Vector2(len, lineWidth), SpriteEffects.None, 0f);
                }
            }

            // ---- Tri-Source Crystals: layered diamonds with HP-scaled size and hit flash ----
            Color[] crystalColors = { new Color(255, 230, 90), new Color(255, 140, 50), new Color(190, 90, 240) };
            float[] hps = { yellowCrystalHP, orangeCrystalHP, purpleCrystalHP };
            for (int i = 0; i < 3; i++)
            {
                if (hps[i] <= 0f) continue;
                Vector2 pos = CrystalPos(npc, i);
                float hpScale = MathHelper.Lerp(0.55f, 1f, MathHelper.Clamp(hps[i] / 800f, 0f, 1f));
                float flash = MathHelper.Clamp(crystalFlash[i], 0f, 1f);
                float spin = ticksRunning * 0.04f + i;

                Color outer = crystalColors[i];
                outer.A = 0;
                Color inner = Color.Lerp(crystalColors[i], Color.White, 0.4f + flash * 0.6f);
                inner.A = 0;

                spriteBatch.Draw(pixel, pos - screenPos, new Rectangle(0, 0, 1, 1), outer * 0.75f, spin, new Vector2(0.5f), new Vector2(34f, 34f) * hpScale, SpriteEffects.None, 0f);
                spriteBatch.Draw(pixel, pos - screenPos, new Rectangle(0, 0, 1, 1), inner * 0.9f, spin + MathHelper.PiOver4, new Vector2(0.5f), new Vector2(20f, 20f) * hpScale, SpriteEffects.None, 0f);
            }

            // ---- Stun halo: a dimming crown flickers above the drooped goddess ----
            if (stunTimer > 0)
            {
                float sag = 0.3f + 0.2f * MathF.Sin(ticksRunning * 0.15f);
                Color halo = HolyGold * sag;
                halo.A = 0;
                spriteBatch.Draw(pixel, npc.Center + new Vector2(0f, -90f) - screenPos, new Rectangle(0, 0, 1, 1), halo, 0f, new Vector2(0.5f), new Vector2(80f, 5f), SpriteEffects.None, 0f);
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
