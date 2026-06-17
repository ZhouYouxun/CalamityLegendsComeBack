using System;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeComboHoldout : BaseFlailProjectile, ILocalizedModType
    {
        private const int WhipArcDuration = 46;
        private const int WhipArcWindup = 10;
        private const int WhipArcSnap = 9;
        private const int WhipArcHold = 4;
        private const int WhipThrustDuration = 52;
        private const int WhipThrustWindup = 13;
        private const int SwordSwingDuration = 36;
        private const int SwordSwingWindup = 9;
        private const int SwordFinisherDuration = 72;
        private const int SwordFinisherWindup = 34;
        private const int SwordFinisherSlamFrame = 43;
        private const int QuickDrawDuration = 40;
        private const int QuickDrawWindup = 4;
        private const int SwordTipTrailSubsteps = 100;
        private const int SwordSwingTipTrailFrames = 12;
        private const int SwordFinisherTipTrailFrames = 18;

        private const float WhipReach = 510f;
        private const float ThrustReach = 545f;
        private const float QuickDrawReach = 620f;
        private const float SwordReach = 292f;
        private const float FinisherReach = 358f;

        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => CosmicDischargeCommon.ChainTexturePath;

        private bool wasRightHeld;
        private bool releaseSoundPlayed;
        private bool impactEffectsPlayed;
        private bool spawnedSwordWave;
        private int spawnedBombBursts;
        private int hitStopTimer;
        private int impactFlashTimer;
        private float currentCollisionWidth = 30f;
        private bool currentlyRetracting;
        private float currentArmRotationOffset;
        private readonly System.Collections.Generic.List<Vector2> tipHistory = new();
        private readonly System.Collections.Generic.List<System.Collections.Generic.List<Vector2>> pointsHistory = new();

        private CosmicDischargeAttackKind Kind
        {
            get => (CosmicDischargeAttackKind)(int)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        private ref float AimAngle => ref Projectile.ai[1];
        private ref float Time => ref Projectile.localAI[0];
        private ref float QuickDrawQueued => ref Projectile.localAI[1];
        private Player Owner => Main.player[Projectile.owner];
        private Vector2 AimDirection => AimAngle.ToRotationVector2();

        public bool CanBecomeQuickDraw =>
            Owner.GetModPlayer<CosmicDischargePlayer>().QuickDrawCooldownTimer <= 0 &&
            ((Kind == CosmicDischargeAttackKind.WhipThrust && Time <= WhipThrustWindup) ||
             (Kind == CosmicDischargeAttackKind.SwordFinisher && Time <= SwordFinisherWindup) ||
             (Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll && Time <= 9));

        public override Color SpecialDrawColor => CosmicDischargeCommon.DoGSpecialColor;
        public override int ExudeDustType => DustID.PurpleTorch;
        public override int WhipDustType => DustID.PurpleTorch;
        public override int HandleHeight => 62;
        public override int BodyType1StartY => 64;
        public override int BodyType1SectionHeight => 28;
        public override int BodyType2StartY => 94;
        public override int BodyType2SectionHeight => 18;
        public override int TailStartY => 114;
        public override int TailHeight => 84;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 9;
            Projectile.ownerHitCheck = true;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = 2;
            if (AimAngle == 0f)
                AimAngle = Vector2.UnitX.RotatedByRandom(0.01f).ToRotation();

            for (int i = 0; i < Main.maxNPCs; i++)
                Projectile.localNPCImmunity[i] = 0;
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<NewLegendCosmicDischarge>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Owner.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == Projectile.owner)
                Owner.Calamity().rightClickListener = true;

            bool validMouse = !Main.mapFullscreen && !Main.blockMouse;
            bool rightHeld = Main.myPlayer == Projectile.owner && validMouse && (Main.mouseRight || Owner.Calamity().mouseRight);
            if (rightHeld && !wasRightHeld && CanBecomeQuickDraw)
                QuickDrawQueued = 1f;
            wasRightHeld = rightHeld;

            if (QuickDrawQueued > 0f && Kind != CosmicDischargeAttackKind.QuickDraw)
                BeginQuickDraw();

            if (hitStopTimer > 0)
            {
                hitStopTimer--;
                HoldBladeStill();
                return;
            }

            Time++;
            if (impactFlashTimer > 0)
                impactFlashTimer--;

            currentlyRetracting = false;
            Projectile.localNPCHitCooldown = Kind == CosmicDischargeAttackKind.QuickDraw ? 3 : 9;

            switch (Kind)
            {
                case CosmicDischargeAttackKind.WhipOver:
                    UpdateWhipArc(-1f);
                    break;
                case CosmicDischargeAttackKind.WhipUnder:
                    UpdateWhipArc(1f);
                    break;
                case CosmicDischargeAttackKind.WhipThrust:
                    UpdateThrust(false);
                    break;
                case CosmicDischargeAttackKind.SwordSwingOne:
                    UpdateSwordSwing(false);
                    break;
                case CosmicDischargeAttackKind.SwordSwingTwo:
                    UpdateSwordSwing(true);
                    break;
                case CosmicDischargeAttackKind.SwordFinisher:
                    UpdateSwordFinisher();
                    break;
                case CosmicDischargeAttackKind.ChainKnifeSingle:
                case CosmicDischargeAttackKind.ChainKnifeScatter:
                case CosmicDischargeAttackKind.ChainKnifeBiteAll:
                    UpdateChainArcSwing();
                    break;
                case CosmicDischargeAttackKind.QuickDraw:
                    UpdateThrust(true);
                    break;
            }

            if (Projectile.alpha > 0)
                Projectile.alpha = Math.Max(0, Projectile.alpha - 42);

            SpawnBladeWakeDust();
        }

        public bool TryRequestQuickDraw()
        {
            if (!CanBecomeQuickDraw)
                return false;

            QuickDrawQueued = 1f;
            Projectile.netUpdate = true;
            return true;
        }

        private void BeginQuickDraw()
        {
            Vector2 direction = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection);
            Kind = CosmicDischargeAttackKind.QuickDraw;
            AimAngle = direction.ToRotation();
            Time = 0f;
            QuickDrawQueued = 0f;
            spawnedBombBursts = 0;
            spawnedSwordWave = false;
            releaseSoundPlayed = false;
            impactEffectsPlayed = false;
            Projectile.localNPCHitCooldown = 3;
            Projectile.netUpdate = true;

            Owner.GetModPlayer<CosmicDischargePlayer>().QuickDrawCooldownTimer = 1800; // 30 seconds
            Owner.GetModPlayer<CosmicDischargePlayer>().AddUltimateEnergy(CosmicDischargePlayer.RightThrustEnergyGain);
            Owner.velocity += direction * 1.8f;
            Owner.SetImmuneTimeForAllTypes(8);
            ApplyScreenShake(5.8f);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftOpen") { Volume = 0.64f, Pitch = 0.38f, MaxInstances = 2 }, Owner.Center);

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Owner.MountedCenter,
                    direction * 1.8f,
                    CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.48f,
                    Vector2.One,
                    direction.ToRotation(),
                    0.03f,
                    0.2f,
                    15));
                CosmicDischargeCommon.SpawnDoGSparkBurst(Owner.MountedCenter, 8, 2.4f, 7f, 0.58f, -direction * 1.1f);
                CosmicDischargeCommon.SpawnDoGRiftCracks(Owner.MountedCenter, 2, 3f, 7f, 0.4f);
            }
        }

        private void UpdateWhipArc(float side)
        {
            int snapEnd = WhipArcWindup + WhipArcSnap;
            int holdEnd = snapEnd + WhipArcHold;
            float sign = side * Math.Sign(Owner.direction == 0 ? 1 : Owner.direction);

            var modPlayer = Owner.GetModPlayer<CosmicDischargePlayer>();
            bool ultActive = modPlayer.UltimateFieldActive;
            bool empActive = modPlayer.FrozenEmperorActive;
            float maxReach = WhipReach;
            if (ultActive || empActive)
                maxReach *= 1.25f;

            if (Time <= WhipArcWindup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            Vector2 baseDirection = AimDirection;
            if (Time <= WhipArcWindup)
            {
                float t = Time / WhipArcWindup;
                float prep = EaseOutCubic(t);
                SetBlade(baseDirection.RotatedBy(-sign * MathHelper.Lerp(0.82f, 0.24f, prep)), MathHelper.Lerp(76f, 178f, prep), -0.18f * sign, 24f);
            }
            else if (Time <= snapEnd)
            {
                float t = (Time - WhipArcWindup) / WhipArcSnap;
                float snap = MathF.Sin(t * MathHelper.PiOver2);
                float over = MathF.Sin(MathHelper.Pi * t);
                SetBlade(baseDirection.RotatedBy(MathHelper.Lerp(-0.2f, 0.05f, snap) * sign), MathHelper.Lerp(178f, maxReach + 30f, snap), -0.08f * sign, 38f + over * 8f);
                PlayReleaseOnce(SoundID.Item71, 0.86f, side < 0f ? -0.22f : 0.05f, 4.1f);

                if (!impactEffectsPlayed && t >= 0.7f)
                {
                    EmitAirCrack(TipPosition, baseDirection, 0.8f);
                    SpawnWhipRiftBombFan(baseDirection, ultActive || empActive ? 4 : 3, ultActive || empActive ? 0.48f : 0.36f);
                }
            }
            else if (Time <= holdEnd)
            {
                SetBlade(baseDirection, maxReach, 0f, 40f);
            }
            else
            {
                float t = Utils.GetLerpValue(holdEnd, WhipArcDuration, Time, true);
                currentlyRetracting = true;
                Projectile.localNPCHitCooldown = (ultActive || empActive) ? 4 : 120;
                float retract = MathF.Sin(t * MathHelper.PiOver2);
                SetBlade(baseDirection.RotatedBy(sign * MathHelper.Lerp(0.08f, 0.28f, retract)), MathHelper.Lerp(maxReach, 64f, retract), 0.12f * sign, MathHelper.Lerp(30f, 18f, retract));
            }

            if (Time >= WhipArcDuration)
                Projectile.Kill();
        }

        private void UpdateThrust(bool quickDraw)
        {
            int duration = quickDraw ? 48 : 64;
            int windup = quickDraw ? QuickDrawWindup : WhipThrustWindup;
            int snapFrames = quickDraw ? 7 : 11;
            int holdFrames = quickDraw ? 9 : 8;
            int snapEnd = windup + snapFrames;
            int holdEnd = snapEnd + holdFrames;
            float maxReach = quickDraw ? QuickDrawReach : ThrustReach;

            if (Time <= windup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            Vector2 direction = AimDirection;
            if (Time <= windup)
            {
                float t = quickDraw ? 1f : EaseOutCubic(Time / windup);
                SetBlade(direction.RotatedBy(-0.1f * Owner.direction), MathHelper.Lerp(58f, quickDraw ? 126f : 156f, t), -0.04f * Owner.direction, quickDraw ? 26f : 28f);
            }
            else if (Time <= snapEnd)
            {
                float t = (Time - windup) / snapFrames;
                float snap = MathF.Sin(t * MathHelper.PiOver2);
                float over = MathF.Sin(MathHelper.Pi * t);
                SetBlade(direction, MathHelper.Lerp(126f, maxReach + (quickDraw ? 70f : 34f), snap), 0f, 42f + over * 10f);
                PlayReleaseOnce(SoundID.Item122, quickDraw ? 0.9f : 0.72f, quickDraw ? 0.28f : 0.06f, quickDraw ? 6.2f : 4.4f);

                if (!impactEffectsPlayed && t >= 0.72f)
                    EmitAirCrack(TipPosition, direction, quickDraw ? 1.35f : 0.92f);
            }
            else if (Time <= holdEnd)
            {
                SetBlade(direction, maxReach + (quickDraw ? 38f : 8f), 0f, quickDraw ? 48f : 40f);
                if (quickDraw)
                    SpawnQuickDrawBombs();
            }
            else
            {
                float t = Utils.GetLerpValue(holdEnd, duration, Time, true);
                currentlyRetracting = true;
                Projectile.localNPCHitCooldown = quickDraw ? 3 : 16;
                float retract = MathF.Sin(t * MathHelper.PiOver2);
                SetBlade(direction.RotatedBy(Owner.direction * MathHelper.Lerp(0f, 0.18f, retract)), MathHelper.Lerp(maxReach, 52f, retract), 0.08f * Owner.direction, MathHelper.Lerp(36f, 18f, retract));
                if (quickDraw)
                    SpawnQuickDrawBombs();
            }

            if (Time >= duration)
                Projectile.Kill();
        }

        private void UpdateSwordSwing(bool second)
        {
            if (Time <= SwordSwingWindup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            float swingSign = (second ? -1f : 1f) * Math.Sign(Owner.direction == 0 ? 1 : Owner.direction);
            int strikeEnd = SwordSwingWindup + 8;
            int holdEnd = strikeEnd + 3;

            if (Time <= SwordSwingWindup)
            {
                float prep = EaseOutCubic(Time / SwordSwingWindup);
                float angle = AimAngle + swingSign * MathHelper.Lerp(-1.42f, -0.86f, prep);
                SetBlade(angle.ToRotationVector2(), SwordReach, second ? 0.12f : -0.12f, 28f);
            }
            else if (Time <= strikeEnd)
            {
                float t = (Time - SwordSwingWindup) / 8f;
                float strike = EaseOutCubic(t);
                float angle = AimAngle + MathHelper.Lerp(-0.86f * swingSign, 1.1f * swingSign, strike);
                Vector2 slashDirection = angle.ToRotationVector2();
                SetBlade(slashDirection, SwordReach, second ? 0.12f : -0.12f, 38f);
                PlayReleaseOnce(SoundID.Item71, 0.82f, second ? 0.14f : -0.08f, 3.8f);

                if (!impactEffectsPlayed && t >= 0.58f)
                {
                    EmitAirCrack(TipPosition, slashDirection, 0.74f);
                    SpawnSwordHomingBolts(TipPosition, slashDirection, second ? 4 : 3, second ? 0.42f : 0.36f);
                }
            }
            else if (Time <= holdEnd)
            {
                SetBlade((AimAngle + 1.14f * swingSign).ToRotationVector2(), SwordReach, 0f, 36f);
            }
            else
            {
                float t = Utils.GetLerpValue(holdEnd, SwordSwingDuration, Time, true);
                float recover = MathF.Sin(t * MathHelper.PiOver2);
                float angle = AimAngle + MathHelper.Lerp(1.14f * swingSign, 0.18f * swingSign, recover);
                SetBlade(angle.ToRotationVector2(), SwordReach, 0f, MathHelper.Lerp(30f, 18f, recover));
            }

            if (Time > SwordSwingWindup && Time <= strikeEnd)
            {
                float previousTime = Math.Max(SwordSwingWindup, Time - 1f);
                for (int i = 1; i <= SwordTipTrailSubsteps; i++)
                {
                    float subTime = MathHelper.Lerp(previousTime, Time, i / (float)SwordTipTrailSubsteps);
                    tipHistory.Add(GetSwordSwingTipPosition(subTime, swingSign));
                }

                TrimTipHistory(SwordSwingTipTrailFrames * SwordTipTrailSubsteps);
            }
            else
            {
                FadeTipHistory(SwordTipTrailSubsteps);
            }

            if (Time >= SwordSwingDuration)
                Projectile.Kill();
        }

        private void UpdateSwordFinisher()
        {
            if (Time <= SwordFinisherWindup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            Vector2 direction = AimDirection;
            if (Time <= SwordFinisherWindup)
            {
                float t = Time / SwordFinisherWindup;
                float spinAngle = AimAngle + Owner.direction * (MathHelper.TwoPi * 2f * t - MathHelper.PiOver2);
                float chargeBump = 0.5f + 0.5f * MathF.Sin(MathHelper.Pi * t);
                SetBlade(spinAngle.ToRotationVector2(), FinisherReach, 0.05f * Owner.direction, 32f + chargeBump * 7f);

                bool ultActive = Owner.GetModPlayer<CosmicDischargePlayer>().UltimateFieldActive;

                if (Time == 1f)
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftBuilding") { Volume = 0.45f, Pitch = -0.12f, MaxInstances = 2 }, Owner.Center);

                if (Time % 8f == 0f)
                    ApplyScreenShake(1.2f + t * 1.5f);

                SpawnSpinChargeDust(t);

                if (!Main.dedServ)
                {
                    // Swirling vortex particle effects
                    int spawnCount = ultActive ? 3 : 1;
                    for (int pIndex = 0; pIndex < spawnCount; pIndex++)
                    {
                        float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                        float dist = MathHelper.Lerp(280f, 15f, t) + Main.rand.NextFloat(-20f, 20f);
                        Vector2 particlePos = Owner.Center + angle.ToRotationVector2() * dist;
                        Vector2 toPlayer = Owner.Center - particlePos;
                        Vector2 particleVel = toPlayer.RotatedBy(0.55f * Owner.direction).SafeNormalize(Vector2.Zero) * (ultActive ? 7.5f : 4.8f);
                        
                        GeneralParticleHandler.SpawnParticle(new SparkParticle(
                            particlePos,
                            particleVel,
                            false,
                            Main.rand.Next(12, 22),
                            Main.rand.NextFloat(0.45f, 0.82f),
                            CosmicDischargeCommon.RandomDoGColor()
                        ));
                    }
                }

                // Blizzard vacuum pull and rapid tick damage
                if (Main.myPlayer == Projectile.owner)
                {
                    float pullRadius = ultActive ? 320f : 260f;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (npc.active && !npc.friendly && !npc.dontTakeDamage && !npc.boss && npc.knockBackResist > 0f)
                        {
                            float dist = Vector2.Distance(Owner.Center, npc.Center);
                            if (dist < pullRadius)
                            {
                                float pullSpeed = ultActive ? 8.8f : 5f;
                                Vector2 pull = (Owner.Center - npc.Center).SafeNormalize(Vector2.Zero) * pullSpeed;
                                npc.velocity = pull;
                                npc.netUpdate = true;

                                int tickRate = ultActive ? 4 : 6;
                                if (Time % tickRate == 0)
                                {
                                    npc.StrikeNPC(npc.CalculateHitInfo((int)(Projectile.damage * (ultActive ? 0.45f : 0.33f)), Math.Sign(pull.X), false, 0f));
                                }
                            }
                        }
                    }
                }
                float previousTime = Math.Max(0f, Time - 1f);
                for (int i = 1; i <= SwordTipTrailSubsteps; i++)
                {
                    float subTime = MathHelper.Lerp(previousTime, Time, i / (float)SwordTipTrailSubsteps);
                    float subT = MathHelper.Clamp(subTime / SwordFinisherWindup, 0f, 1f);
                    float subSpinAngle = AimAngle + Owner.direction * (MathHelper.TwoPi * 2f * subT - MathHelper.PiOver2);
                    Vector2 subTip = Owner.MountedCenter + subSpinAngle.ToRotationVector2() * FinisherReach;

                    tipHistory.Add(subTip);
                }
                TrimTipHistory(24 * SwordTipTrailSubsteps);
                return;
            }

            int strikeFrames = 10;
            int strikeEnd = SwordFinisherSlamFrame + strikeFrames;

            if (Time <= SwordFinisherSlamFrame)
            {
                float lift = EaseOutCubic(Utils.GetLerpValue(SwordFinisherWindup, SwordFinisherSlamFrame, Time, true));
                float angle = AimAngle - Owner.direction * MathHelper.Lerp(1.38f, 1.05f, lift);
                SetBlade(angle.ToRotationVector2(), FinisherReach, 0.05f * Owner.direction, 38f);
            }
            else if (Time <= strikeEnd)
            {
                float t = (Time - SwordFinisherSlamFrame) / strikeFrames;
                float slam = EaseOutCubic(t);
                float angle = AimAngle + MathHelper.Lerp(-1.05f * Owner.direction, 1.22f * Owner.direction, slam);
                SetBlade(angle.ToRotationVector2(), FinisherReach, 0f, 48f);

                if (!releaseSoundPlayed && t >= 0.12f)
                {
                    releaseSoundPlayed = true;
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DoGLaserWallBigAttack") { Volume = 0.7f, Pitch = -0.18f, MaxInstances = 2 }, Owner.Center);
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/HeavySwing") { Volume = 0.55f, Pitch = -0.18f }, Owner.Center);
                    ApplyScreenShake(7.2f);
                }

                if (!spawnedSwordWave && t >= 0.45f)
                {
                    spawnedSwordWave = true;
                    SpawnSwordWave(direction);
                    EmitAirCrack(TipPosition, direction, 1.35f);
                    SpawnSwordHomingBolts(TipPosition, direction, 6, 0.52f);
                }
            }
            else
            {
                float t = Utils.GetLerpValue(strikeEnd, SwordFinisherDuration, Time, true);
                float recover = MathF.Sin(t * MathHelper.PiOver2);
                float angle = AimAngle + MathHelper.Lerp(1.22f * Owner.direction, 0.16f * Owner.direction, recover);
                SetBlade(angle.ToRotationVector2(), FinisherReach, 0f, MathHelper.Lerp(38f, 18f, recover));
            }

            if (Time > SwordFinisherWindup && Time <= strikeEnd)
            {
                float previousTime = Math.Max(SwordFinisherWindup, Time - 1f);
                for (int i = 1; i <= SwordTipTrailSubsteps; i++)
                {
                    float subTime = MathHelper.Lerp(previousTime, Time, i / (float)SwordTipTrailSubsteps);

                    if (subTime <= SwordFinisherSlamFrame)
                    {
                        float lift = EaseOutCubic(Utils.GetLerpValue(SwordFinisherWindup, SwordFinisherSlamFrame, subTime, true));
                        float angle = AimAngle - Owner.direction * MathHelper.Lerp(1.38f, 1.05f, lift);
                        Vector2 subTip = Owner.MountedCenter + angle.ToRotationVector2() * FinisherReach;
                        tipHistory.Add(subTip);
                    }
                    else
                    {
                        float t = (subTime - SwordFinisherSlamFrame) / strikeFrames;
                        float slam = EaseOutCubic(t);
                        float angle = AimAngle + MathHelper.Lerp(-1.05f * Owner.direction, 1.22f * Owner.direction, slam);
                        Vector2 subTip = Owner.MountedCenter + angle.ToRotationVector2() * FinisherReach;
                        tipHistory.Add(subTip);
                    }
                }
                TrimTipHistory(SwordFinisherTipTrailFrames * SwordTipTrailSubsteps);
            }
            else
            {
                FadeTipHistory(SwordTipTrailSubsteps);
            }

            if (Time >= SwordFinisherDuration)
                Projectile.Kill();
        }

        private void SetBlade(Vector2 direction, float reach, float armRotationOffset, float collisionWidth)
        {
            float scaleMultiplier = 1f;

            direction = direction.SafeNormalize(Vector2.UnitX * Owner.direction);
            Projectile.velocity = direction * Math.Max(12f, reach);
            Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, true) - Projectile.velocity;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.spriteDirection = Owner.direction;
            currentCollisionWidth = collisionWidth;
            currentArmRotationOffset = armRotationOffset;
            Projectile.scale = scaleMultiplier;

            CosmicDischargeCommon.HoldPlayer(Owner, Projectile, direction, armRotationOffset);
            Owner.itemRotation = (Projectile.velocity * Owner.direction).ToRotation();
        }

        private void HoldBladeStill()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(AimDirection);
            SetBlade(direction, Projectile.velocity.Length(), 0f, 0f);
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    TipPosition + Main.rand.NextVector2Circular(12f, 12f),
                    DustID.PurpleTorch,
                    Main.rand.NextVector2Circular(1.8f, 1.8f),
                    100,
                    CosmicDischargeCommon.RandomDoGColor(),
                    Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }
        }

        private Vector2 TipPosition => Owner.MountedCenter + Projectile.velocity;

        private Vector2 GetSwordSwingTipPosition(float sampleTime, float swingSign)
        {
            float t = MathHelper.Clamp((sampleTime - SwordSwingWindup) / 8f, 0f, 1f);
            float strike = EaseOutCubic(t);
            float angle = AimAngle + MathHelper.Lerp(-0.86f * swingSign, 1.1f * swingSign, strike);
            return Owner.MountedCenter + angle.ToRotationVector2() * SwordReach;
        }

        private void TrimTipHistory(int maxCount)
        {
            while (tipHistory.Count > maxCount)
                tipHistory.RemoveAt(0);
        }

        private void FadeTipHistory(int count)
        {
            if (tipHistory.Count <= 0)
                return;

            int toRemove = Math.Min(tipHistory.Count, count);
            tipHistory.RemoveRange(0, toRemove);
        }

        private void PlayReleaseOnce(SoundStyle sound, float volume, float pitch, float shake)
        {
            if (releaseSoundPlayed)
                return;

            releaseSoundPlayed = true;
            SoundEngine.PlaySound(sound with { Volume = volume, Pitch = pitch }, Owner.Center);
            ApplyScreenShake(shake);
        }

        private void EmitAirCrack(Vector2 center, Vector2 direction, float intensity)
        {
            impactEffectsPlayed = true;
            impactFlashTimer = 8;

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                direction * 0.7f,
                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * (0.38f * intensity),
                Vector2.One,
                direction.ToRotation(),
                0.035f,
                0.22f * intensity,
                14));

            for (int i = 0; i < 6 + (int)(intensity * 8f); i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.72f) * Main.rand.NextFloat(3.2f, 9f) * intensity;
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    center + Main.rand.NextVector2Circular(24f, 18f),
                    velocity,
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.42f, 0.82f),
                    CosmicDischargeCommon.Transparent(CosmicDischargeCommon.RandomDoGColor()) * 0.75f));
            }

            CosmicDischargeCommon.SpawnDoGRiftCracks(center, intensity > 1f ? 5 : 3, 3f, 8f + intensity * 2f, 0.45f * intensity);
        }

        private void SpawnSwordWave(Vector2 direction)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.MountedCenter + direction * 78f,
                direction * 18f,
                ModContent.ProjectileType<CosmicDischargeSwordWave>(),
                (int)(Projectile.damage * 0.86f),
                Projectile.knockBack,
                Projectile.owner);
        }

        private void SpawnWhipRiftBombFan(Vector2 direction, int count, float damageFactor)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            count = Math.Max(1, count);
            float spread = MathHelper.ToRadians(24f);
            for (int i = 0; i < count; i++)
            {
                float offset = count == 1 ? 0f : MathHelper.Lerp(-spread, spread, i / (float)(count - 1));
                Vector2 velocity = direction.RotatedBy(offset) * Main.rand.NextFloat(8.5f, 12.5f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    TipPosition + direction * 8f,
                    velocity,
                    ModContent.ProjectileType<CosmicDischargeDoGRiftBomb>(),
                    (int)(Projectile.damage * damageFactor),
                    Projectile.knockBack * 0.35f,
                    Projectile.owner,
                    Main.rand.NextFloat(20f, 34f));
            }
        }

        private void SpawnSwordHomingBolts(Vector2 position, Vector2 direction, int count, float damageFactor)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            count = Math.Max(1, count);
            float spread = MathHelper.ToRadians(18f);
            for (int i = 0; i < count; i++)
            {
                float offset = count == 1 ? 0f : MathHelper.Lerp(-spread, spread, i / (float)(count - 1));
                Vector2 velocity = direction.RotatedBy(offset) * Main.rand.NextFloat(10f, 14.5f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    position + direction * 12f,
                    velocity,
                    ModContent.ProjectileType<CosmicDischargeDoGEnergyBolt>(),
                    (int)(Projectile.damage * damageFactor),
                    Projectile.knockBack * 0.45f,
                    Projectile.owner);
            }
        }

        private void SpawnRiftExplosion(Vector2 position, float radius, float damageFactor)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                position,
                Vector2.Zero,
                ModContent.ProjectileType<CosmicDischargeDoGConvergenceExplosion>(),
                (int)(Projectile.damage * damageFactor),
                Projectile.knockBack,
                Projectile.owner,
                0f,
                radius);
        }

        private void SpawnQuickDrawBombs()
        {
            int bombCount = CosmicDischargeProgression.QuickDrawRiftBombCount;
            if (bombCount <= 0 || Main.myPlayer != Projectile.owner)
                return;

            if (spawnedBombBursts >= CosmicDischargeProgression.QuickDrawRiftBombBursts)
                return;

            bool shouldBurst = Time == 10f || Time == 16f || Time == 23f;
            if (!shouldBurst)
                return;

            spawnedBombBursts++;
            Vector2 direction = Projectile.velocity.SafeNormalize(AimDirection);
            for (int i = 0; i < bombCount; i++)
            {
                float t = (i + 0.5f) / bombCount;
                Vector2 spawnPosition = Owner.MountedCenter + direction * Projectile.velocity.Length() * t + Main.rand.NextVector2Circular(24f, 24f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    Main.rand.NextVector2Circular(1.8f, 1.8f),
                    ModContent.ProjectileType<CosmicDischargeDoGRiftBomb>(),
                    (int)(Projectile.damage * CosmicDischargeProgression.QuickDrawRiftBombDamageFactor),
                    0f,
                    Projectile.owner,
                    Main.rand.NextFloat(12f, 24f));
            }
        }

        private void SpawnBladeWakeDust()
        {
            if (Main.dedServ || Projectile.velocity.LengthSquared() < 4f)
                return;

            int dustCount = impactFlashTimer > 0 ? 3 : 1;
            Vector2 direction = Projectile.velocity.SafeNormalize(AimDirection);
            for (int i = 0; i < dustCount; i++)
            {
                if (!Main.rand.NextBool(impactFlashTimer > 0 ? 1 : 2))
                    continue;

                Vector2 point = Owner.MountedCenter + direction * Main.rand.NextFloat(32f, Projectile.velocity.Length());
                Dust dust = Dust.NewDustPerfect(
                    point + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.PurpleTorch,
                    direction.RotatedByRandom(0.65f) * Main.rand.NextFloat(0.35f, 2.4f),
                    120,
                    CosmicDischargeCommon.RandomDoGColor(),
                    Main.rand.NextFloat(0.85f, 1.32f));
                dust.noGravity = true;
            }
        }

        private void SpawnSpinChargeDust(float charge)
        {
            if (Main.dedServ || Main.rand.NextBool(2))
                return;

            Vector2 radius = Main.rand.NextVector2CircularEdge(58f + charge * 42f, 58f + charge * 42f);
            Dust dust = Dust.NewDustPerfect(
                Owner.MountedCenter + radius,
                DustID.PurpleTorch,
                radius.RotatedBy(MathHelper.PiOver2 * Owner.direction).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.2f, 3.8f),
                110,
                CosmicDischargeCommon.RandomDoGColor(),
                Main.rand.NextFloat(0.9f, 1.25f));
            dust.noGravity = true;
        }

        private System.Collections.Generic.List<Microsoft.Xna.Framework.Vector2> GetActivePoints()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(AimDirection);
            float reach = Projectile.velocity.Length();

            if (Kind == CosmicDischargeAttackKind.WhipOver || Kind == CosmicDischargeAttackKind.WhipUnder)
            {
                return GenerateWhipPoints(direction, reach);
            }
            else if (Kind == CosmicDischargeAttackKind.WhipThrust)
            {
                return GenerateThrustPoints(direction, reach);
            }
            else if (Kind == CosmicDischargeAttackKind.SwordSwingOne ||
                     Kind == CosmicDischargeAttackKind.SwordSwingTwo ||
                     Kind == CosmicDischargeAttackKind.SwordFinisher)
            {
                return CosmicDischargeCommon.BuildCurvedBlade(Owner, direction, reach, 0f, 0f, 18);
            }
            else
            {
                return GenerateChainConvergencePoints(direction, reach, 0f);
            }
        }

        public override bool? CanDamage()
        {
            if (hitStopTimer > 0)
                return false;

            return Kind switch
            {
                CosmicDischargeAttackKind.WhipOver or CosmicDischargeAttackKind.WhipUnder
                    => Time >= WhipArcWindup + 2f && Time <= WhipArcDuration - 5f,
                CosmicDischargeAttackKind.WhipThrust
                    => Time >= WhipThrustWindup + 2f && Time <= WhipThrustDuration - 6f,
                CosmicDischargeAttackKind.SwordSwingOne or CosmicDischargeAttackKind.SwordSwingTwo
                    => Time >= SwordSwingWindup + 2f && Time <= SwordSwingDuration - 7f,
                CosmicDischargeAttackKind.SwordFinisher
                    => Time >= SwordFinisherSlamFrame + 1f && Time <= SwordFinisherSlamFrame + 16f,
                CosmicDischargeAttackKind.ChainKnifeSingle or CosmicDischargeAttackKind.ChainKnifeScatter or CosmicDischargeAttackKind.ChainKnifeBiteAll
                    => Time >= 15f && Time <= 50f,
                CosmicDischargeAttackKind.QuickDraw
                    => Time >= QuickDrawWindup + 2f && Time <= QuickDrawDuration - 4f,
                _ => false
            };
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            var points = GetActivePoints();
            bool collide = CosmicDischargeCommon.CheckCurveCollision(points, targetHitbox, currentCollisionWidth);
            if (collide)
                return true;

            bool isChainArc = Kind == CosmicDischargeAttackKind.ChainKnifeSingle ||
                              Kind == CosmicDischargeAttackKind.ChainKnifeScatter ||
                              Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll;
            if (isChainArc)
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(AimDirection);
                float reach = Projectile.velocity.Length();
                int chainCount = GetChainCount();
                for (int i = 0; i < chainCount; i++)
                {
                    float lane = i - (chainCount - 1) * 0.5f;
                    if (Math.Abs(lane) < 0.01f)
                        continue;

                    var extraPoints = GenerateChainConvergencePoints(direction, reach, lane);
                    if (CosmicDischargeCommon.CheckCurveCollision(extraPoints, targetHitbox, currentCollisionWidth * 0.86f))
                        return true;
                }
            }

            // Check collision for mirror whip if Frozen Emperor is active
            bool isWhip = Kind == CosmicDischargeAttackKind.WhipOver || Kind == CosmicDischargeAttackKind.WhipUnder;
            if (isWhip && Owner.GetModPlayer<CosmicDischargePlayer>().FrozenEmperorActive)
            {
                float mirrorSide = Kind == CosmicDischargeAttackKind.WhipOver ? 1f : -1f;
                Vector2 direction = Projectile.velocity.SafeNormalize(AimDirection);
                var mirrorPoints = GenerateWhipPointsForSide(direction, Projectile.velocity.Length(), mirrorSide);
                if (CosmicDischargeCommon.CheckCurveCollision(mirrorPoints, targetHitbox, currentCollisionWidth))
                    return true;
            }

            return false;
        }

        private bool TargetNearTip(NPC target, float radius)
        {
            var points = GetActivePoints();
            bool near = CosmicDischargeCommon.TargetIntersectsTip(points, target.Hitbox, radius);
            if (near)
                return true;

            // Also check mirror whip tip!
            bool isWhip = Kind == CosmicDischargeAttackKind.WhipOver || Kind == CosmicDischargeAttackKind.WhipUnder;
            if (isWhip && Owner.GetModPlayer<CosmicDischargePlayer>().FrozenEmperorActive)
            {
                float mirrorSide = Kind == CosmicDischargeAttackKind.WhipOver ? 1f : -1f;
                Vector2 direction = Projectile.velocity.SafeNormalize(AimDirection);
                var mirrorPoints = GenerateWhipPointsForSide(direction, Projectile.velocity.Length(), mirrorSide);
                if (CosmicDischargeCommon.TargetIntersectsTip(mirrorPoints, target.Hitbox, radius))
                    return true;
            }

            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            bool tip = TargetNearTip(target, Kind == CosmicDischargeAttackKind.QuickDraw ? 58f : 46f);
            var modPlayer = Owner.GetModPlayer<CosmicDischargePlayer>();
            bool ultActive = modPlayer.UltimateFieldActive;
            bool empActive = modPlayer.FrozenEmperorActive;

            bool isSword = Kind == CosmicDischargeAttackKind.SwordSwingOne ||
                           Kind == CosmicDischargeAttackKind.SwordSwingTwo ||
                           Kind == CosmicDischargeAttackKind.SwordFinisher;

            if (isSword && target.HasBuff(ModContent.BuffType<CosmicDischargeDoGMarkDebuff>()))
            {
                modifiers.FinalDamage *= 1.4f;
            }

            switch (Kind)
            {
                case CosmicDischargeAttackKind.WhipOver:
                case CosmicDischargeAttackKind.WhipUnder:
                    if (ultActive || empActive)
                    {
                        if (currentlyRetracting)
                        {
                            modifiers.FinalDamage *= 0.66f;
                            modifiers.Knockback *= 0.5f;
                        }
                        else
                        {
                            modifiers.FinalDamage *= 1.25f;
                            modifiers.Knockback *= 1.2f;
                        }
                    }
                    else if (currentlyRetracting)
                    {
                        modifiers.FinalDamage *= 0.2f;
                        modifiers.Knockback *= 0.25f;
                    }
                    else
                    {
                        modifiers.FinalDamage *= 1.08f;
                        modifiers.Knockback *= 1.2f;
                    }
                    break;

                case CosmicDischargeAttackKind.WhipThrust:
                    modifiers.FinalDamage *= tip ? 2.75f : 0.72f;
                    modifiers.Knockback *= tip ? 1.6f : 0.55f;
                    break;

                case CosmicDischargeAttackKind.SwordSwingOne:
                case CosmicDischargeAttackKind.SwordSwingTwo:
                    modifiers.FinalDamage *= 1.12f;
                    modifiers.Knockback *= 1.25f;
                    break;

                case CosmicDischargeAttackKind.SwordFinisher:
                    modifiers.FinalDamage *= 1.9f;
                    modifiers.Knockback *= 1.75f;
                    break;

                case CosmicDischargeAttackKind.ChainKnifeSingle:
                case CosmicDischargeAttackKind.ChainKnifeScatter:
                case CosmicDischargeAttackKind.ChainKnifeBiteAll:
                    modifiers.FinalDamage *= tip ? 2.2f : 1.1f;
                    modifiers.Knockback *= tip ? 1.5f : 1.0f;
                    break;

                case CosmicDischargeAttackKind.QuickDraw:
                    modifiers.FinalDamage *= tip ? 3.35f : 0.46f;
                    modifiers.Knockback *= tip ? 1.9f : 0.28f;
                    break;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            bool tip = TargetNearTip(target, Kind == CosmicDischargeAttackKind.QuickDraw ? 58f : 46f);
            bool heavy = tip ||
                         Kind == CosmicDischargeAttackKind.SwordFinisher ||
                         Kind == CosmicDischargeAttackKind.SwordSwingOne ||
                         Kind == CosmicDischargeAttackKind.SwordSwingTwo ||
                         Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll;

            var modPlayer = Owner.GetModPlayer<CosmicDischargePlayer>();
            bool ultActive = modPlayer.UltimateFieldActive;
            bool empActive = modPlayer.FrozenEmperorActive;

            CosmicDischargeCommon.ApplyDoGDebuffs(target, Kind == CosmicDischargeAttackKind.QuickDraw ? 420 : 300);
            ApplyHitStop(heavy ? 6 : 3);
            ApplyScreenShake(empActive ? 12.5f : (heavy ? 9.5f : 4.6f));
            SpawnHitEffects(target, heavy, tip);

            if (Main.myPlayer == Projectile.owner)
            {
                // 1. WHIP FORM SYNERGIES
                bool isWhip = Kind == CosmicDischargeAttackKind.WhipOver ||
                              Kind == CosmicDischargeAttackKind.WhipUnder ||
                              Kind == CosmicDischargeAttackKind.WhipThrust;
                if (isWhip)
                {
                    target.AddBuff(ModContent.BuffType<CosmicDischargeDoGMarkDebuff>(), 300);

                    // Under Frozen Emperor, trigger double hits (mirror whip strike)
                    if (empActive)
                    {
                        int doubleDamage = (int)(damageDone * 0.85f);
                        NPC.HitInfo mirrorHit = target.CalculateHitInfo(doubleDamage, hit.HitDirection, false, hit.Knockback * 0.8f);
                        target.StrikeNPC(mirrorHit);

                        if (!Main.dedServ)
                        {
                            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                                target.Center,
                                Vector2.Zero,
                                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGWhiteColor) * 0.45f,
                                0.5f,
                                15
                            ));
                        }
                    }
                }

                // 2. SWORD FORM SYNERGIES (DOG MARK DETONATION)
                bool isSword = Kind == CosmicDischargeAttackKind.SwordSwingOne ||
                               Kind == CosmicDischargeAttackKind.SwordSwingTwo ||
                               Kind == CosmicDischargeAttackKind.SwordFinisher;

                int markType = ModContent.BuffType<CosmicDischargeDoGMarkDebuff>();
                if (isSword && target.HasBuff(markType))
                {
                    target.DelBuff(target.FindBuffIndex(markType));

                    int boltCount = empActive ? 10 : 6;
                    SpawnRiftExplosion(target.Center, empActive ? 150f : 118f, empActive ? 0.68f : 0.46f);
                    for (int i = 0; i < boltCount; i++)
                    {
                        Vector2 velocity = (MathHelper.TwoPi * i / (float)boltCount).ToRotationVector2() * (empActive ? 13.5f : 11f);
                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            target.Center,
                            velocity,
                            ModContent.ProjectileType<CosmicDischargeDoGEnergyBolt>(),
                            (int)(Projectile.damage * (empActive ? 0.58f : 0.42f)),
                            Projectile.knockBack * 0.5f,
                            Projectile.owner);
                    }

                    if (!Main.dedServ)
                    {
                        SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftOpen") { Volume = 0.55f, Pitch = -0.05f, MaxInstances = 4 }, target.Center);
                        GeneralParticleHandler.SpawnParticle(new PulseRing(
                            target.Center,
                            Vector2.Zero,
                            CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor),
                            0.04f,
                            1.4f,
                            22
                        ));

                        for (int k = 0; k < 20; k++)
                        {
                            Vector2 sparkVel = Main.rand.NextVector2Circular(6f, 6f) + (MathHelper.TwoPi * k / 20f).ToRotationVector2() * Main.rand.NextFloat(4f, 8.5f);
                            GeneralParticleHandler.SpawnParticle(new SparkParticle(
                                target.Center,
                                sparkVel,
                                false,
                                Main.rand.Next(16, 28),
                                Main.rand.NextFloat(0.7f, 1.1f),
                                CosmicDischargeCommon.RandomDoGColor()
                            ));
                        }
                    }
                }

                // 3. CHAIN ARC FORM SYNERGIES
                bool isChainArc = Kind == CosmicDischargeAttackKind.ChainKnifeSingle ||
                                  Kind == CosmicDischargeAttackKind.ChainKnifeScatter ||
                                  Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll;
                if (isChainArc)
                {
                    bool hadMark = target.HasBuff(markType);
                    target.AddBuff(ModContent.BuffType<CosmicDischargeDoGMarkDebuff>(), 240);
                    if (hadMark)
                    {
                        target.DelBuff(target.FindBuffIndex(markType));
                        int shardCount = empActive ? 5 : 3;
                        for (int i = 0; i < shardCount; i++)
                        {
                            Vector2 shardVel = Main.rand.NextVector2Circular(4f, 4f) + (MathHelper.TwoPi * i / shardCount).ToRotationVector2() * Main.rand.NextFloat(7f, 11f);
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                target.Center,
                                shardVel,
                                ModContent.ProjectileType<CosmicDischargeDoGEnergyBolt>(),
                                (int)(Projectile.damage * 0.48f),
                                0f,
                                Projectile.owner);
                        }
                        SpawnRiftExplosion(target.Center, 110f, 0.42f);
                        SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftOpen") { Volume = 0.45f, Pitch = 0.12f, MaxInstances = 4 }, target.Center);
                    }
                }

                // 4. GENERAL SWORD STRIKES & SPIN ATTACKS
                if (Kind == CosmicDischargeAttackKind.SwordSwingOne)
                {
                    Vector2 slashDir = Projectile.velocity.SafeNormalize(AimDirection);
                    SpawnSwordHomingBolts(target.Center, slashDir, ultActive ? 4 : 2, 0.34f);
                }
                else if (Kind == CosmicDischargeAttackKind.SwordSwingTwo)
                {
                    SpawnRiftExplosion(target.Center, ultActive ? 130f : 104f, 0.44f);
                    SpawnSwordHomingBolts(target.Center, Projectile.velocity.SafeNormalize(AimDirection), ultActive ? 5 : 3, 0.36f);

                    if (!target.boss && target.knockBackResist > 0f)
                    {
                        target.velocity += (Owner.MountedCenter - target.Center).SafeNormalize(Vector2.Zero) * (ultActive ? 6f : 4.5f);
                        target.netUpdate = true;
                    }
                }
                else if (Kind == CosmicDischargeAttackKind.SwordFinisher)
                {
                    SpawnRiftExplosion(target.Center, ultActive ? 172f : 136f, ultActive ? 0.72f : 0.54f);
                    SpawnSwordHomingBolts(target.Center, Projectile.velocity.SafeNormalize(AimDirection), ultActive ? 8 : 5, ultActive ? 0.45f : 0.36f);
                }

                // 5. DoG rift explosions
                if (empActive)
                {
                    SpawnRiftExplosion(target.Center, 170f, 0.64f);
                }
                else if (tip ||
                         Kind == CosmicDischargeAttackKind.WhipThrust ||
                         Kind == CosmicDischargeAttackKind.SwordFinisher ||
                         (ultActive && (Kind == CosmicDischargeAttackKind.WhipOver || Kind == CosmicDischargeAttackKind.WhipUnder || Kind == CosmicDischargeAttackKind.WhipThrust)))
                {
                    SpawnRiftExplosion(target.Center, heavy ? 138f : 108f, Kind == CosmicDischargeAttackKind.QuickDraw ? 0.58f : 0.42f);
                }
            }
        }

        private void ApplyHitStop(int frames)
        {
            hitStopTimer = Math.Max(hitStopTimer, frames);
            impactFlashTimer = Math.Max(impactFlashTimer, frames + 4);
        }

        private void SpawnHitEffects(NPC target, bool heavy, bool tip)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = (target.Center - Owner.MountedCenter).SafeNormalize(AimDirection);
            SoundEngine.PlaySound(new SoundStyle(heavy ? "CalamityMod/Sounds/Item/DemonSwordInsaneImpact" : "CalamityMod/Sounds/Item/LanceofDestinyStrong")
            {
                Volume = heavy ? 0.68f : 0.46f,
                Pitch = tip ? 0.12f : -0.08f,
                MaxInstances = 4
            }, target.Center);
            if (heavy)
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerAttack") { Volume = 0.42f, Pitch = tip ? 0.18f : 0f, MaxInstances = 4 }, target.Center);

            CosmicDischargeCommon.SpawnDoGImpact(target.Center, direction, heavy, tip);
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1500f, 120f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero)
                return false;

            Vector2 direction = Projectile.velocity.SafeNormalize(AimDirection);
            var points = GetActivePoints();

            bool isSword = Kind == CosmicDischargeAttackKind.SwordSwingOne ||
                           Kind == CosmicDischargeAttackKind.SwordSwingTwo ||
                           Kind == CosmicDischargeAttackKind.SwordFinisher;
            bool isWhip = Kind == CosmicDischargeAttackKind.WhipOver ||
                          Kind == CosmicDischargeAttackKind.WhipUnder;
            bool isChainArc = Kind == CosmicDischargeAttackKind.ChainKnifeSingle ||
                              Kind == CosmicDischargeAttackKind.ChainKnifeScatter ||
                              Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll;

            if (isSword)
            {
                pointsHistory.Clear();
                DrawSwordSmear();
            }
            else
            {
                pointsHistory.Clear();
            }

            DrawCurvedBladeGlow(points);
            CosmicDischargeCommon.DrawCurvedChain(Main.spriteBatch, points, lightColor, Projectile.scale, Owner.gfxOffY);

            if (isChainArc)
            {
                int chainCount = GetChainCount();
                float reach = Projectile.velocity.Length();
                Color chainColor = Color.Lerp(lightColor, CosmicDischargeCommon.DoGPurpleColor, 0.35f);
                for (int i = 0; i < chainCount; i++)
                {
                    float lane = i - (chainCount - 1) * 0.5f;
                    if (Math.Abs(lane) < 0.01f)
                        continue;

                    var extraPoints = GenerateChainConvergencePoints(direction, reach, lane);
                    DrawCurvedBladeGlow(extraPoints);
                    CosmicDischargeCommon.DrawCurvedChain(Main.spriteBatch, extraPoints, chainColor, Projectile.scale * 0.92f, Owner.gfxOffY);
                }
            }

            // Under Frozen Emperor, draw the mirror whip simultaneously
            if (isWhip && Owner.GetModPlayer<CosmicDischargePlayer>().FrozenEmperorActive)
            {
                float mirrorSide = Kind == CosmicDischargeAttackKind.WhipOver ? 1f : -1f;
                var mirrorPoints = GenerateWhipPointsForSide(direction, Projectile.velocity.Length(), mirrorSide);
                DrawCurvedBladeGlow(mirrorPoints);
                Color mirrorColor = Color.Lerp(lightColor, CosmicDischargeCommon.DoGSpecialColor, 0.35f);
                CosmicDischargeCommon.DrawCurvedChain(Main.spriteBatch, mirrorPoints, mirrorColor, Projectile.scale, Owner.gfxOffY);
            }

            if (CanBecomeQuickDraw)
                CosmicDischargeCommon.DrawRightHoldIndicator(Main.spriteBatch, Owner, 1f + 0.18f * MathF.Sin(Time * 0.45f));

            return false;
        }

        private void DrawCurvedBladeGlow(System.Collections.Generic.List<Vector2> points)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float flash = impactFlashTimer > 0 ? impactFlashTimer / 8f : 0f;
            Color outer = CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGPurpleColor) * (0.18f + flash * 0.12f);
            Color cyanGlow = CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGCyanColor) * (0.24f + flash * 0.22f);
            Color fuchsiaGlow = CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * (0.18f + flash * 0.18f);
            Color core = CosmicDischargeCommon.DoGWhiteColor * (0.25f + flash * 0.38f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 start = points[i] - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
                Vector2 end = points[i + 1] - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
                Vector2 segment = end - start;
                if (segment.LengthSquared() < 0.1f)
                    continue;

                DrawLine(pixel, start, segment, outer, currentCollisionWidth * 1.48f);
                DrawLine(pixel, start, segment, cyanGlow, currentCollisionWidth * 0.82f);
                DrawLine(pixel, start, segment, fuchsiaGlow, currentCollisionWidth * 0.46f);
                DrawLine(pixel, start, segment, core, MathHelper.Clamp(currentCollisionWidth * 0.16f, 2f, 5f));
            }

            if (points.Count > 0)
            {
                Main.EntitySpriteDraw(
                    bloom,
                    points[^1] - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY,
                    null,
                    CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * (0.22f + flash * 0.24f),
                    0f,
                    bloom.Size() * 0.5f,
                    (0.22f + flash * 0.08f) * Projectile.scale,
                    SpriteEffects.None);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private static void DrawLine(Texture2D pixel, Vector2 start, Vector2 segment, Color color, float width)
        {
            Main.EntitySpriteDraw(
                pixel,
                start,
                new Rectangle(0, 0, 1, 1),
                color,
                segment.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(segment.Length(), width),
                SpriteEffects.None);
        }

        private void GetBendAndCurl(out float sideBend, out float curl)
        {
            sideBend = 0f;
            curl = 0f;

            switch (Kind)
            {
                case CosmicDischargeAttackKind.WhipOver:
                    {
                        float sign = -1f * Math.Sign(Owner.direction == 0 ? 1 : Owner.direction);
                        int snapEnd = WhipArcWindup + WhipArcSnap;
                        int holdEnd = snapEnd + WhipArcHold;
                        if (Time <= WhipArcWindup)
                        {
                            float t = Time / WhipArcWindup;
                            sideBend = -120f * (1f - t) * sign;
                            curl = -60f * (1f - t);
                        }
                        else if (Time <= snapEnd)
                        {
                            float t = (Time - WhipArcWindup) / WhipArcSnap;
                            sideBend = MathHelper.Lerp(-40f, 60f, t) * sign;
                            curl = MathHelper.Lerp(-30f, 20f, t);
                        }
                        else if (Time <= holdEnd)
                        {
                            sideBend = 0f;
                            curl = 0f;
                        }
                        else
                        {
                            float t = Utils.GetLerpValue(holdEnd, WhipArcDuration, Time, true);
                            sideBend = MathHelper.Lerp(0f, -140f, t) * sign;
                            curl = MathHelper.Lerp(0f, -80f, t);
                        }
                    }
                    break;

                case CosmicDischargeAttackKind.WhipUnder:
                    {
                        float sign = 1f * Math.Sign(Owner.direction == 0 ? 1 : Owner.direction);
                        int snapEnd = WhipArcWindup + WhipArcSnap;
                        int holdEnd = snapEnd + WhipArcHold;
                        if (Time <= WhipArcWindup)
                        {
                            float t = Time / WhipArcWindup;
                            sideBend = 120f * (1f - t) * sign;
                            curl = 60f * (1f - t);
                        }
                        else if (Time <= snapEnd)
                        {
                            float t = (Time - WhipArcWindup) / WhipArcSnap;
                            sideBend = MathHelper.Lerp(40f, -60f, t) * sign;
                            curl = MathHelper.Lerp(30f, -20f, t);
                        }
                        else if (Time <= holdEnd)
                        {
                            sideBend = 0f;
                            curl = 0f;
                        }
                        else
                        {
                            float t = Utils.GetLerpValue(holdEnd, WhipArcDuration, Time, true);
                            sideBend = MathHelper.Lerp(0f, 140f, t) * sign;
                            curl = MathHelper.Lerp(0f, 80f, t);
                        }
                    }
                    break;

                case CosmicDischargeAttackKind.WhipThrust:
                case CosmicDischargeAttackKind.QuickDraw:
                    {
                        sideBend = 15f * MathF.Sin(Time * 0.8f) * Owner.direction;
                        curl = 10f * MathF.Cos(Time * 0.8f);
                    }
                    break;

                case CosmicDischargeAttackKind.SwordSwingOne:
                case CosmicDischargeAttackKind.SwordSwingTwo:
                case CosmicDischargeAttackKind.SwordFinisher:
                    break;

                case CosmicDischargeAttackKind.ChainKnifeSingle:
                case CosmicDischargeAttackKind.ChainKnifeScatter:
                case CosmicDischargeAttackKind.ChainKnifeBiteAll:
                    {
                        // Spine-of-Thanatos-style big arc swing: chain curves side-to-side as it sweeps
                        float swingSign = Kind == CosmicDischargeAttackKind.ChainKnifeScatter ? -1f : 1f;
                        float dirSign = swingSign * Math.Sign(Owner.direction == 0 ? 1 : Owner.direction);
                        const int arcWindup = 9;
                        const int arcSwing = 36;
                        int arcSwingEnd = arcWindup + arcSwing;
                        if (Time <= arcWindup)
                        {
                            float t = Time / arcWindup;
                            sideBend = MathHelper.Lerp(45f, -100f, t) * dirSign;
                            curl = MathHelper.Lerp(-18f, 12f, t);
                        }
                        else if (Time <= arcSwingEnd)
                        {
                            float t = (Time - arcWindup) / (float)arcSwing;
                            sideBend = MathHelper.Lerp(-100f, 120f, EaseOutCubic(t)) * dirSign;
                            curl = MathHelper.Lerp(12f, -45f, t);
                        }
                        else
                        {
                            float t = Utils.GetLerpValue(arcSwingEnd, arcSwingEnd + 14f, Time, true);
                            sideBend = MathHelper.Lerp(120f, 25f, t) * dirSign;
                            curl = MathHelper.Lerp(-45f, 0f, t);
                        }
                    }
                    break;
            }
        }

        private static float EaseOutCubic(float value)
        {
            value = MathHelper.Clamp(value, 0f, 1f);
            float inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }

        private static float EaseInCubic(float value)
        {
            value = MathHelper.Clamp(value, 0f, 1f);
            return value * value * value;
        }

        private static float EaseOutExpo(float value)
        {
            value = MathHelper.Clamp(value, 0f, 1f);
            return value >= 1f ? 1f : 1f - MathF.Pow(2f, -10f * value);
        }

        private static float EaseInExpo(float value)
        {
            value = MathHelper.Clamp(value, 0f, 1f);
            return value <= 0f ? 0f : MathF.Pow(2f, 10f * (value - 1f));
        }

        private System.Collections.Generic.List<Vector2> GenerateWhipPoints(Vector2 direction, float reach)
        {
            float side = Kind == CosmicDischargeAttackKind.WhipOver ? -1f : 1f;
            return GenerateWhipPointsForSide(direction, reach, side);
        }

        private System.Collections.Generic.List<Vector2> GenerateWhipPointsForSide(Vector2 direction, float reach, float side)
        {
            System.Collections.Generic.List<Vector2> points = new System.Collections.Generic.List<Vector2>();
            Vector2 startPos = Owner.MountedCenter;
            points.Add(startPos);

            int directionSign = Owner.direction * (int)side;

            float t = Time / (float)WhipArcDuration;
            float progress = 0f;
            float retreatProgress = 0f;

            float p = t * 1.5f;
            if (p > 1.0f)
            {
                retreatProgress = (p - 1.0f) / 0.5f;
                progress = MathHelper.Lerp(1f, 0f, retreatProgress);
            }
            else
            {
                progress = p;
            }

            var modPlayer = Owner.GetModPlayer<CosmicDischargePlayer>();
            if (modPlayer.UltimateFieldActive || modPlayer.FrozenEmperorActive)
            {
                reach *= 1.25f;
            }
            int segments = 18;
            float totalLength = reach;
            float segLen = totalLength / segments;

            float angleStep = (float)Math.PI * 8f * (1f - progress) * (float)(-directionSign) / segments;

            Vector2 currentPos = startPos;
            float baseAngle = direction.ToRotation();
            float leftAngle = baseAngle - (float)Math.PI / 2f;
            float rightAngle = baseAngle + (float)Math.PI / 2f;

            for (int i = 0; i < segments; i++)
            {
                float ratio = (float)i / segments;
                float currentStep = angleStep * ratio;

                Vector2 extendVec = currentPos + baseAngle.ToRotationVector2() * segLen;
                Vector2 leftVec = currentPos + leftAngle.ToRotationVector2() * (segLen * 1.5f);
                Vector2 rightVec = currentPos + rightAngle.ToRotationVector2() * (segLen * 1.5f);

                float invProgress = 1f - progress;
                float lerpWeight = 1f - invProgress * invProgress;

                Vector2 temp = Vector2.Lerp(leftVec, extendVec, lerpWeight * 0.9f + 0.1f);
                Vector2 targetVec = Vector2.Lerp(rightVec, temp, lerpWeight * 0.7f + 0.3f);

                Vector2 rawPoint = startPos + (targetVec - startPos);
                
                float rotFactor = retreatProgress * retreatProgress;
                Vector2 finalPoint = rawPoint.RotatedBy(4.712389f * rotFactor * (float)directionSign, startPos);

                points.Add(finalPoint);

                baseAngle += currentStep;
                leftAngle += currentStep;
                rightAngle += currentStep;
                currentPos = extendVec;
            }

            return points;
        }

        private System.Collections.Generic.List<Vector2> GenerateThrustPoints(Vector2 direction, float reach)
        {
            System.Collections.Generic.List<Vector2> points = new System.Collections.Generic.List<Vector2>();
            Vector2 startPos = Owner.MountedCenter;
            points.Add(startPos);
            int segments = 18;
            
            float shake = MathF.Sin(Time * 0.8f) * 12f * (1f - Time / WhipThrustDuration);
            Vector2 perp = direction.RotatedBy(MathHelper.PiOver2);

            for (int i = 1; i <= segments; i++)
            {
                float ratio = (float)i / segments;
                Vector2 linePos = startPos + direction * (reach * ratio);
                float wave = MathF.Sin(ratio * MathHelper.TwoPi + Time * 0.6f) * shake;
                points.Add(linePos + perp * wave);
            }

            return points;
        }

        private int GetChainCount()
        {
            return Kind switch
            {
                CosmicDischargeAttackKind.ChainKnifeBiteAll => 5,
                CosmicDischargeAttackKind.ChainKnifeScatter => 4,
                _ => 3
            };
        }

        private System.Collections.Generic.List<Vector2> GenerateChainConvergencePoints(Vector2 direction, float reach, float lane)
        {
            System.Collections.Generic.List<Vector2> points = new();
            Vector2 startPos = Owner.MountedCenter;
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            float chainCount = Math.Max(1, GetChainCount() - 1);
            float laneOffset = lane * 18f;
            float curveSign = lane == 0f ? 0f : Math.Sign(lane);
            float curveStrength = Math.Abs(lane) / chainCount;
            int segments = 20;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float convergence = 1f - t;
                float side = laneOffset * convergence + curveSign * MathF.Sin(MathHelper.Pi * t) * MathHelper.Lerp(24f, 92f, curveStrength);
                float wave = MathF.Sin(Time * 0.42f + t * MathHelper.TwoPi) * 6f * curveStrength * convergence;
                points.Add(startPos + direction * (reach * t) + normal * (side + wave));
            }

            return points;
        }

        private void UpdateChainArcSwing()
        {
            // Spine-of-Thanatos-style convergence: several segment chains meet at the cursor and detonate.
            const int arcWindup = 8;
            const int arcExtendFrames = 22;
            const int arcHoldFrames = 16;
            int arcSnapEnd = arcWindup + arcExtendFrames;
            int arcHoldEnd = arcSnapEnd + arcHoldFrames;
            int arcTotalDuration = arcHoldEnd + 12;

            float maxReach = Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll ? 560f : 500f;
            bool ultActive = Owner.GetModPlayer<CosmicDischargePlayer>().UltimateFieldActive;
            bool empActive = Owner.GetModPlayer<CosmicDischargePlayer>().FrozenEmperorActive;
            if (ultActive || empActive) maxReach *= 1.2f;

            if (Time <= arcWindup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            Vector2 desired = Owner.Calamity().mouseWorld - Owner.MountedCenter;
            if (desired.LengthSquared() < 0.001f)
                desired = AimDirection;
            Vector2 direction = desired.SafeNormalize(AimDirection);
            float targetReach = MathHelper.Clamp(desired.Length(), 160f, maxReach);

            if (Time <= arcWindup)
            {
                float prep = EaseOutCubic(Time / arcWindup);
                SetBlade(direction, MathHelper.Lerp(78f, targetReach * 0.36f, prep), 0f, 30f);
            }
            else if (Time <= arcSnapEnd)
            {
                float t = (Time - arcWindup) / (float)arcExtendFrames;
                float extend = EaseOutCubic(t);
                SetBlade(direction, MathHelper.Lerp(targetReach * 0.36f, targetReach, extend), 0f, 52f);

                PlayReleaseOnce(SoundID.Item71, 0.9f, 0.2f, 6.2f);

                if (!impactEffectsPlayed && t >= 0.74f)
                {
                    EmitAirCrack(TipPosition, direction, 1.35f);
                    float radius = Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll ? 176f :
                                   Kind == CosmicDischargeAttackKind.ChainKnifeScatter ? 146f : 126f;
                    SpawnRiftExplosion(TipPosition, empActive ? radius * 1.2f : radius, Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll ? 0.82f : 0.62f);
                }
            }
            else if (Time <= arcHoldEnd)
            {
                SetBlade(direction, targetReach, 0f, 38f);
            }
            else
            {
                float t = Utils.GetLerpValue(arcHoldEnd, arcTotalDuration, Time, true);
                currentlyRetracting = true;
                Projectile.localNPCHitCooldown = 8;
                float retract = MathF.Sin(t * MathHelper.PiOver2);
                SetBlade(direction, MathHelper.Lerp(targetReach, 55f, retract), 0f, MathHelper.Lerp(34f, 16f, retract));
            }

            if (Time >= arcTotalDuration)
                Projectile.Kill();
        }

        private void DrawSwordSmear()
        {
            if (tipHistory.Count < 2)
                return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            bool ultActive = Owner.GetModPlayer<CosmicDischargePlayer>().UltimateFieldActive;
            bool empActive = Owner.GetModPlayer<CosmicDischargePlayer>().FrozenEmperorActive;
            
            float glowWidth = empActive ? 55f : (ultActive ? 40f : 25f);
            float coreWidth = empActive ? 20f : (ultActive ? 14f : 8f);

            for (int i = 0; i < tipHistory.Count - 1; i++)
            {
                float fade = 1f - i / (float)tipHistory.Count;
                Color glowColor = Color.Lerp(CosmicDischargeCommon.DoGFuchsiaColor, CosmicDischargeCommon.DoGCyanColor, 1f - fade);
                glowColor = CosmicDischargeCommon.Transparent(glowColor) * (0.55f * fade * Projectile.Opacity);
                Color coreColor = CosmicDischargeCommon.DoGWhiteColor * (0.78f * fade * Projectile.Opacity);

                Vector2 start = tipHistory[i] - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
                Vector2 end = tipHistory[i + 1] - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
                Vector2 segment = end - start;

                if (segment.LengthSquared() < 0.1f)
                    continue;

                DrawLine(pixel, start, segment, glowColor, glowWidth * fade);
                DrawLine(pixel, start, segment, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGCyanColor) * (0.28f * fade * Projectile.Opacity), glowWidth * 0.42f * fade);
                DrawLine(pixel, start, segment, coreColor, coreWidth * fade);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }
    }
}
