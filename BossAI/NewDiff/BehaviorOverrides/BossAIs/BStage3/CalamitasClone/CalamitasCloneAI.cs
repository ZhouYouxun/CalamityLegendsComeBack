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

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.CalamitasClone
{
    internal sealed class CalamitasCloneAI : IUMWBossAI
    {
        #region Constants & Configuration
        public override int NPCType => ModContent.NPCType<CalamityMod.NPCs.CalClone.CalamitasClone>();
        public override string BossName => "Calamitas Clone";
        public override Color DebugColor => new(220, 60, 60);

        public override int MaxPhaseCount => 4;
        public override float[] PhaseLifeRatios => new[] { 0.70f, 0.35f, 0.10f };
        public override int AttackCycleLength => 120;
        public override float MotionIntensity => 1.0f;
        #endregion

        #region Attack States
        public enum AttackState
        {
            Oblivion = 0,
            Animosity = 1,
            LashesOfChaos = 2,
            EntropysVigil = 3,
            CrushsawCrasher = 4,
            HavocsBreath = 5,
            DesperationOverload = 6,
            BrotherTransition = 7
        }
        #endregion

        #region Fields
        private int ticksRunning = 0;
        private int currentRepetition = 0;
        private readonly Vector2[] oldPositions = new Vector2[14];
        private int oldPositionsIndex;
        private Vector2 arenaCenter = Vector2.Zero;
        private bool centerSet = false;
        private int arenaHurtCooldown = 0;

        // Shield status
        private bool shieldActive = true;
        private int shieldRegenTimer = 0;
        private int shieldStunTimer = 0;
        private int shieldFxCooldown = 0;
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

            if (!centerSet)
            {
                arenaCenter = npc.Center;
                centerSet = true;
            }

            int currentPhase = (int)npc.ai[0];
            AttackState state = (AttackState)(int)npc.ai[1];
            ref float timer = ref npc.ai[2];
            ref float stateTracker = ref npc.ai[3];

            if (currentPhase == 0)
            {
                currentPhase = 1;
                npc.ai[0] = 1f;
                state = AttackState.Oblivion;
                npc.ai[1] = (float)state;
                currentRepetition = 0;
                npc.netUpdate = true;
            }

            float lifeRatio = npc.lifeMax <= 0 ? 1f : npc.life / (float)npc.lifeMax;
            int nextPhase = 1;
            foreach (float threshold in PhaseLifeRatios)
            {
                if (lifeRatio <= threshold)
                    nextPhase++;
            }

            if (nextPhase > currentPhase)
            {
                currentPhase = nextPhase;
                npc.ai[0] = currentPhase;
                if (currentPhase == 3)
                    state = AttackState.BrotherTransition;
                else if (currentPhase == 4)
                    state = AttackState.DesperationOverload;
                else
                    state = AttackState.Oblivion;
                npc.ai[1] = (float)state;
                timer = 0;
                stateTracker = 0;
                npc.netUpdate = true;
            }

            float borderSize = 1400f;
            if (currentPhase == 2) borderSize = 1100f;
            else if (currentPhase == 3) borderSize = 900f;
            else if (currentPhase == 4) borderSize = 650f;

            // Boundary push + damage — throttled. The previous version called target.Hurt() every single
            // frame the player was outside the box (60 hits/sec); now it's one hit per half-second.
            Vector2 dist = target.Center - arenaCenter;
            if (arenaHurtCooldown > 0)
                arenaHurtCooldown--;
            if (Math.Abs(dist.X) > borderSize / 2f || Math.Abs(dist.Y) > borderSize / 2f)
            {
                if (Math.Abs(dist.X) > borderSize / 2f)
                    target.velocity.X = -Math.Sign(dist.X) * 5f;
                if (Math.Abs(dist.Y) > borderSize / 2f)
                    target.velocity.Y = -Math.Sign(dist.Y) * 5f;

                if (arenaHurtCooldown <= 0)
                {
                    arenaHurtCooldown = 30;
                    target.AddBuff(BuffID.OnFire, 180);
                    target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 12, 0);
                }
            }

            UpdateProjectiles(borderSize);
            UpdateSoulSeekers(npc, currentPhase);

            npc.rotation = npc.velocity.X * 0.04f;
            npc.scale = 1f + (float)Math.Sin(ticksRunning * 0.06f) * 0.03f;

            if (shieldFxCooldown > 0)
                shieldFxCooldown--;

            switch (state)
            {
                case AttackState.Oblivion:
                    ExecuteOblivion(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.Animosity:
                    ExecuteAnimosity(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.LashesOfChaos:
                    ExecuteLashes(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.EntropysVigil:
                    ExecuteVigil(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.CrushsawCrasher:
                    ExecuteCrushsaw(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.HavocsBreath:
                    ExecuteHavoc(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.DesperationOverload:
                    ExecuteDesperation(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.BrotherTransition:
                    ExecuteBrotherTransition(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
            }

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) => ApplyDefense(npc, ref modifiers);
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) => ApplyDefense(npc, ref modifiers);

        private void ApplyDefense(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (npc.ai[1] == (float)AttackState.BrotherTransition)
            {
                modifiers.FinalDamage *= 0f;
                return;
            }
            if (shieldActive && npc.ai[0] <= 2)
            {
                modifiers.FinalDamage *= 0.05f; // 95% DR
                if (shieldFxCooldown <= 0)
                {
                    shieldFxCooldown = 10;
                    SoundEngine.PlaySound(SoundID.Item50 with { Volume = 0.6f, Pitch = -0.1f }, npc.Center);
                }
            }
        }
        #endregion

        #region Anti-Cheese Positioning
        private static Vector2 DirectedHoverSpot(NPC npc, Player target, float sideOffset, float heightOffset, float lead = 0f)
        {
            float side = Math.Sign(npc.Center.X - target.Center.X);
            if (side == 0f)
                side = Main.rand.NextBool() ? 1f : -1f;
            Vector2 predicted = target.Center + target.velocity * lead;
            return predicted + new Vector2(side * sideOffset, heightOffset);
        }
        #endregion

        #region Bouncing & Orbiter Systems
        private void UpdateProjectiles(float borderSize)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.hostile)
                {
                    Vector2 dist = proj.Center - arenaCenter;
                    bool bounced = false;

                    if (Math.Abs(dist.X) > borderSize / 2f)
                    {
                        proj.velocity.X = -proj.velocity.X;
                        bounced = true;
                    }
                    if (Math.Abs(dist.Y) > borderSize / 2f)
                    {
                        proj.velocity.Y = -proj.velocity.Y;
                        bounced = true;
                    }

                    if (bounced)
                    {
                        proj.localAI[0]++;
                        if (proj.localAI[0] >= 2)
                        {
                            proj.Kill();
                        }
                        else
                        {
                            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.3f }, proj.Center);
                            for (int k = 0; k < 8; k++)
                            {
                                Dust d = Dust.NewDustPerfect(proj.Center, DustID.Torch, Main.rand.NextVector2Circular(3f, 3f), 100, Color.Cyan, 1.3f);
                                d.fadeIn = 1.3f;
                                d.noGravity = true;
                            }
                        }
                    }
                }
            }
        }

        private void UpdateSoulSeekers(NPC npc, int currentPhase)
        {
            if (currentPhase >= 3)
            {
                shieldActive = false;
                return;
            }

            int orbiterType = ModContent.NPCType<CalamityMod.NPCs.CalClone.SoulSeeker>();

            if (shieldActive)
            {
                bool alive = false;
                int activeSeekerIndex = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC m = Main.npc[i];
                    if (m.active && m.type == orbiterType && m.ai[0] == npc.whoAmI)
                    {
                        alive = true;
                        float angle = activeSeekerIndex * MathHelper.TwoPi / 6f + ticksRunning * 0.02f;
                        m.Center = npc.Center + angle.ToRotationVector2() * 120f;
                        m.velocity = Vector2.Zero;
                        activeSeekerIndex++;

                        for (int p = 0; p < Main.maxProjectiles; p++)
                        {
                            Projectile proj = Main.projectile[p];
                            if (proj.active && proj.hostile && (proj.ModProjectile?.Name == "BrimstoneBarrage" || proj.ModProjectile?.Name == "BrimstoneHellblast" || proj.ModProjectile?.Name == "BrimstoneGigablast"))
                            {
                                if (Vector2.Distance(proj.Center, m.Center) < 40f)
                                {
                                    proj.Kill();
                                    BrimstoneFx.Burst(m.Center, 5f, 16);
                                    SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.5f }, m.Center);
                                    if (Main.netMode != NetmodeID.MultiplayerClient)
                                    {
                                        Vector2 dir = SafeNormalize(Main.player[npc.target].Center - m.Center, Vector2.UnitY);
                                        for (int s = -1; s <= 1; s++)
                                        {
                                            Projectile.NewProjectile(npc.GetSource_FromAI(), m.Center, dir.RotatedBy(s * 0.2f) * 12f, ModContent.ProjectileType<MiniAmplifiedLaserProj>(), npc.damage / 3, 0f, Main.myPlayer);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (!alive)
                {
                    shieldActive = false;
                    shieldStunTimer = 360;
                    npc.velocity = Vector2.Zero;
                    Player t = Main.player[npc.target];
                    if (t.active)
                        t.Calamity().GeneralScreenShakePower = 7f;
                    BrimstoneFx.Burst(npc.Center, 6f, 30);
                    SoundEngine.PlaySound(SoundID.NPCDeath52, npc.Center);
                }
            }
            else
            {
                if (shieldStunTimer > 0)
                {
                    shieldStunTimer--;
                    npc.defense = 0;
                    if (shieldStunTimer == 0)
                        shieldRegenTimer = 720;
                }
                else if (shieldRegenTimer > 0)
                {
                    shieldRegenTimer--;
                    if (shieldRegenTimer == 0)
                    {
                        shieldActive = true;
                        BrimstoneFx.Burst(npc.Center, 4f, 24);
                        SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f }, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                int minion = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, orbiterType);
                                if (minion >= 0 && minion < Main.maxNPCs)
                                {
                                    Main.npc[minion].ai[0] = npc.whoAmI;
                                    Main.npc[minion].netUpdate = true;
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Attack Rotations
        private void RotateAttack(NPC npc, int currentPhase, AttackState current)
        {
            currentRepetition++;
            if (currentPhase <= 2)
            {
                if (currentRepetition < 3)
                {
                    npc.ai[2] = 0;
                    npc.ai[3] = 0;
                }
                else
                {
                    currentRepetition = 0;
                    AttackState next = current switch
                    {
                        AttackState.Oblivion => AttackState.Animosity,
                        AttackState.Animosity => AttackState.LashesOfChaos,
                        AttackState.LashesOfChaos => AttackState.EntropysVigil,
                        _ => AttackState.Oblivion
                    };
                    npc.ai[1] = (float)next;
                    npc.ai[2] = 0;
                    npc.ai[3] = 0;
                }
            }
            else if (currentPhase == 3)
            {
                currentRepetition = 0;
                AttackState next = current switch
                {
                    AttackState.CrushsawCrasher => AttackState.HavocsBreath,
                    _ => AttackState.CrushsawCrasher
                };
                npc.ai[1] = (float)next;
                npc.ai[2] = 0;
                npc.ai[3] = 0;
            }
            npc.netUpdate = true;
        }
        #endregion

        #region Attack State Machine

        // P1 Attack 1: Oblivion — 环形撕裂型 · 悠悠球投掷停滞后以玩家为圆心360°扫场, 轨迹留下延迟上升的火浪.
        private void ExecuteOblivion(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            Vector2 spot = DirectedHoverSpot(npc, target, 260f, -280f, 8f);
            HoverToward(npc, spot, timer < 40 ? 12f : 3f, 20f);

            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<CalHeldOblivion>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 50 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float side = Math.Sign(npc.Center.X - target.Center.X);
                Vector2 spawn = target.Center + new Vector2(side * 210f, 0f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<OblivionYoyoProj>(), npc.damage / 2, 0f, Main.myPlayer);
                FindHeldWeapon<CalHeldOblivion>(npc)?.Pulse(-14f);
                SoundEngine.PlaySound(SoundID.Item35 with { Volume = 0.5f }, npc.Center);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.Oblivion);
        }

        // P1 Attack 2: Animosity — 超视距阻击型 · 0.6秒锁定后40f超高速穿刺, 击墙未中则爆出酸雾区.
        private void ExecuteAnimosity(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<CalHeldAnimosity>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 300f, -200f, 6f);
            HoverToward(npc, spot, 10f, 15f);

            if (timer == 50 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 36f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<AnimosityBulletProj>(), npc.damage / 2, 0f, Main.myPlayer);
                FindHeldWeapon<CalHeldAnimosity>(npc)?.Pulse(-18f);
                SoundEngine.PlaySound(SoundID.Item41 with { Volume = 0.6f }, npc.Center);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.Animosity);
        }

        // P1 Attack 3: Lashes of Chaos — 吸力火球型 · 三枚火球飞行后碎裂成带引力的漩涡气旋.
        private void ExecuteLashes(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<CalHeldLashes>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 300f, -240f, 6f);
            HoverToward(npc, spot, 11f, 16f);

            if (timer == 50 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy((i - 1) * 0.15f) * 8f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<BrimstoneHellfireballProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                FindHeldWeapon<CalHeldLashes>(npc)?.Pulse(-12f);
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f }, npc.Center);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.LashesOfChaos);
        }

        // P1 Attack 4: Entropy's Vigil — 对角俯冲型 · 两只迷你守卫在方框顶角瞬时现身, 呈X形俯冲交叉.
        private void ExecuteVigil(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<CalHeldVigil>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 0f, -260f, 0f);
            HoverToward(npc, spot, 9f, 22f);

            if (timer == 40)
            {
                FindHeldWeapon<CalHeldVigil>(npc)?.Pulse(-10f);
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f }, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int c1 = NPC.NewNPC(npc.GetSource_FromAI(), (int)arenaCenter.X - 400, (int)arenaCenter.Y - 400, ModContent.Find<ModNPC>("CalamityMod/Catastromini").Type);
                    int c2 = NPC.NewNPC(npc.GetSource_FromAI(), (int)arenaCenter.X + 400, (int)arenaCenter.Y - 400, ModContent.Find<ModNPC>("CalamityMod/Cataclymini").Type);
                    if (c1 >= 0 && c1 < Main.maxNPCs)
                    {
                        Main.npc[c1].velocity = new Vector2(10f, 10f);
                        Main.npc[c1].ai[0] = npc.whoAmI;
                        Main.npc[c1].netUpdate = true;
                    }
                    if (c2 >= 0 && c2 < Main.maxNPCs)
                    {
                        Main.npc[c2].velocity = new Vector2(-10f, 10f);
                        Main.npc[c2].ai[0] = npc.whoAmI;
                        Main.npc[c2].netUpdate = true;
                    }
                }
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.EntropysVigil);
        }

        // P2 Attack 1: Crushsaw Crasher — 贴边旋转轮型 · 锯齿轮直飞撞墙后贴边高速滚动1.5圈.
        private void ExecuteCrushsaw(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<CalHeldCrushsaw>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 300f, -220f, 6f);
            HoverToward(npc, spot, 10f, 18f);

            if (timer == 50 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 14f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<CrushaxProj>(), npc.damage / 2, 0f, Main.myPlayer, arenaCenter.X, arenaCenter.Y);
                FindHeldWeapon<CalHeldCrushsaw>(npc)?.Pulse(20f);
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.5f }, npc.Center);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.CrushsawCrasher);
        }

        // P2 Attack 2: Havoc's Breath — 燃烧边界型 · 扇形摆头喷射火舌.
        private void ExecuteHavoc(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<CalHeldHavoc>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 300f, -250f, 6f);
            HoverToward(npc, spot, 11f, 15f);

            if (timer >= 50 && timer <= 170 && timer % 5 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float angle = MathHelper.Lerp(-0.6f, 0.6f, (timer - 50f) / 120f);
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy(angle) * 12f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<BrimstoneFireFriendlyProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<CalHeldHavoc>(npc)?.Pulse(6f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.HavocsBreath);
        }

        // P2 Final: Desperation Overload — 硫火大十字扫射型 · 四道缓慢自转的十字激光, 配合坠落爆炸.
        private void ExecuteDesperation(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            npc.Center = Vector2.Lerp(npc.Center, arenaCenter, 0.1f);
            npc.velocity = Vector2.Zero;

            if (timer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.8f }, npc.Center);
                target.Calamity().GeneralScreenShakePower = 10f;
                BrimstoneFx.Burst(npc.Center, 7f, 40);
            }

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int s = 0; s < 4; s++)
                {
                    float a = s * MathHelper.PiOver2;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, a.ToRotationVector2(), ModContent.ProjectileType<RotatingBrimstoneLaserProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
            }

            if (timer >= 40 && timer % 20 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 fallPos = arenaCenter + new Vector2(Main.rand.NextFloat(-300f, 300f), -300f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), fallPos, new Vector2(0f, 6f), ModContent.ProjectileType<HellfireStarExplosionProj>(), npc.damage / 2, 0f, Main.myPlayer);
            }
        }

        private void ExecuteBrotherTransition(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            npc.velocity *= 0.9f;
            npc.dontTakeDamage = true;
            npc.alpha = (int)MathHelper.Lerp(0f, 255f, timer / 90f);

            if (timer == 45)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
                target.Calamity().GeneralScreenShakePower = 8f;
                BrimstoneFx.Burst(npc.Center, 6f, 30);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int c1 = NPC.NewNPC(npc.GetSource_FromAI(), (int)arenaCenter.X - 250, (int)arenaCenter.Y, ModContent.Find<ModNPC>("CalamityMod/Cataclysm").Type);
                    int c2 = NPC.NewNPC(npc.GetSource_FromAI(), (int)arenaCenter.X + 250, (int)arenaCenter.Y, ModContent.Find<ModNPC>("CalamityMod/Catastrophe").Type);
                    if (c1 >= 0 && c1 < Main.maxNPCs)
                    {
                        Main.npc[c1].ai[0] = npc.whoAmI;
                        Main.npc[c1].netUpdate = true;
                    }
                    if (c2 >= 0 && c2 < Main.maxNPCs)
                    {
                        Main.npc[c2].ai[0] = npc.whoAmI;
                        Main.npc[c2].netUpdate = true;
                    }
                }
            }

            bool brothersAlive = false;
            int cataclysm = ModContent.Find<ModNPC>("CalamityMod/Cataclysm").Type;
            int catastrophe = ModContent.Find<ModNPC>("CalamityMod/Catastrophe").Type;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && (Main.npc[i].type == cataclysm || Main.npc[i].type == catastrophe))
                {
                    brothersAlive = true;
                    break;
                }
            }

            if (!brothersAlive && timer >= 90)
            {
                npc.alpha = 0;
                npc.dontTakeDamage = false;
                npc.ai[1] = (float)AttackState.CrushsawCrasher;
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
                float alpha = (1f - i / (float)oldPositions.Length) * 0.55f;
                Color trailColor = new Color(220, 60, 60, 0) * alpha;
                spriteBatch.Draw(tex, oldPositions[idx] - screenPos, frame, trailColor, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            }

            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tex = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() / 2f;

            int currentPhase = (int)npc.ai[0];
            float borderSize = 1400f;
            if (currentPhase == 2) borderSize = 1100f;
            else if (currentPhase == 3) borderSize = 900f;
            else if (currentPhase == 4) borderSize = 650f;

            Vector2 tl = arenaCenter + new Vector2(-borderSize / 2f, -borderSize / 2f);
            Vector2 tr = arenaCenter + new Vector2(borderSize / 2f, -borderSize / 2f);
            Vector2 bl = arenaCenter + new Vector2(-borderSize / 2f, borderSize / 2f);
            Vector2 br = arenaCenter + new Vector2(borderSize / 2f, borderSize / 2f);

            IUMWWeaponBossVisuals.DrawLine(spriteBatch, tl, tr, Color.Red * 0.7f, 4f);
            IUMWWeaponBossVisuals.DrawLine(spriteBatch, tr, br, Color.Red * 0.7f, 4f);
            IUMWWeaponBossVisuals.DrawLine(spriteBatch, br, bl, Color.Red * 0.7f, 4f);
            IUMWWeaponBossVisuals.DrawLine(spriteBatch, bl, tl, Color.Red * 0.7f, 4f);

            if (shieldActive)
            {
                int orbiterType = ModContent.NPCType<CalamityMod.NPCs.CalClone.SoulSeeker>();
                Vector2[] seekerPositions = new Vector2[6];
                int seekerCount = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == orbiterType && Main.npc[i].ai[0] == npc.whoAmI)
                    {
                        if (seekerCount < 6)
                            seekerPositions[seekerCount++] = Main.npc[i].Center;
                    }
                }

                for (int i = 0; i < seekerCount; i++)
                {
                    Vector2 start = seekerPositions[i];
                    Vector2 end = seekerPositions[(i + 1) % seekerCount];
                    IUMWWeaponBossVisuals.DrawLine(spriteBatch, start, end, Color.Red * 0.8f, 3f);
                }
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            Color glowColor = new Color(220, 60, 60, 0) * 0.35f;
            spriteBatch.Draw(tex, npc.Center - screenPos, frame, glowColor, npc.rotation, origin, npc.scale * 1.08f, SpriteEffects.None, 0f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
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
