using System;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Melee;
using CalamityLegendsComeBack.Weapons.CosmicDischarge.General;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public partial class CosmicDischargeComboHoldout : BaseFlailProjectile, ILocalizedModType
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
        private const int SwordFinisherWindup = 36;
        private const int SwordFinisherSlamFrame = 43;
        private const int QuickDrawDuration = 48;
        private const int QuickDrawWindup = 20;
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
        private bool apexSoundPlayed;
        private bool retractSoundPlayed;
        private bool impactEffectsPlayed;
        private bool spawnedSwordWave;
        private bool quickDrawBurstPlayed;
        private int spawnedBombBursts;
        private int hitStopTimer;
        private int impactFlashTimer;
        private int legacyCosmicBurstCooldown;
        private float currentCollisionWidth = 30f;
        private bool currentlyRetracting;
        private float currentArmRotationOffset;

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
        public override int ExudeDustType => DustID.TintableDustLighted;
        public override int WhipDustType => DustID.TintableDustLighted;
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
            if (legacyCosmicBurstCooldown > 0)
                legacyCosmicBurstCooldown--;

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
            quickDrawBurstPlayed = false;
            releaseSoundPlayed = false;
            apexSoundPlayed = false;
            retractSoundPlayed = false;
            impactEffectsPlayed = false;
            Projectile.localNPCHitCooldown = 3;
            Projectile.netUpdate = true;

            Owner.GetModPlayer<CosmicDischargePlayer>().QuickDrawCooldownTimer = 1800; // 30 seconds
            Owner.GetModPlayer<CosmicDischargePlayer>().AddUltimateEnergy(CosmicDischargePlayer.RightThrustEnergyGain);
            Owner.velocity += direction * 1.8f;
            Owner.SetImmuneTimeForAllTypes(8);
            ApplyScreenShake(5.8f);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftOpen") { Volume = 0.64f, Pitch = 0.38f, MaxInstances = 2 }, Owner.Center);

            // 起手只给一档提示。QuickDraw 真正的视觉留到命中那一下，不在这里预支。
            CosmicDischargeCommon.SpawnRiftBurst(Owner.MountedCenter, RiftTier.Light, direction, CosmicDischargeCommon.RiftMagenta);
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
            // 顿帧期间刃尖的滞留能量。DoGFire 的 dust 频率（1/12），不是每帧撒。
            if (!Main.dedServ && Main.rand.NextBool(12))
            {
                Dust dust = Dust.NewDustPerfect(
                    TipPosition + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(1.2f, 1.2f),
                    0,
                    CosmicDischargeCommon.RiftColor(),
                    Main.rand.NextFloat(0.6f, 0.8f));
                dust.noGravity = true;
            }
        }

        private Vector2 TipPosition => Owner.MountedCenter + Projectile.velocity;


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

            // 挥击破空。每次挥击只触发一次，强度决定档位，不额外加料。
            RiftTier tier = intensity >= 1.2f ? RiftTier.Heavy
                          : intensity >= 0.9f ? RiftTier.Medium
                          : RiftTier.Light;

            CosmicDischargeCommon.SpawnRiftBurst(center, tier, direction, ModeAccentColor);
        }

        /// <summary>当前形态的强调色。只喂给冲击环与核心光，火花一律走 RiftColor()。</summary>
        private Color ModeAccentColor => CosmicDischargeCommon.GetModeColor(
            Owner.GetModPlayer<CosmicDischargePlayer>().AttackMode);




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


        private void SpawnBladeWakeDust()
        {
            if (Main.dedServ || Projectile.velocity.LengthSquared() < 4f)
                return;

            // 逐帧拖尾只在刃尖。观感主体由 DrawDoGFireTrail 的双层图元承担，
            // 这里严格照 DoGFire 的配比（50%×3 烟雾 + 1/12×2 dust），不再沿整条刃身撒粒子。
            Vector2 direction = Projectile.velocity.SafeNormalize(AimDirection);
            CosmicDischargeCommon.SpawnTrailWake(
                TipPosition,
                -direction * 0.7f,
                ModeAccentColor,
                impactFlashTimer > 0 ? 1.2f : 1f);
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
                CosmicDischargeAttackKind.ChainKnifeSingle or CosmicDischargeAttackKind.ChainKnifeScatter
                    => Time >= 13f && Time <= 48f,
                CosmicDischargeAttackKind.ChainKnifeBiteAll
                    => Time >= 24f && Time <= 68f,
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

            // Check collision for mirror whip if Devourer Ascension is active
            bool isWhip = Kind == CosmicDischargeAttackKind.WhipOver || Kind == CosmicDischargeAttackKind.WhipUnder;
            if (isWhip && Owner.GetModPlayer<CosmicDischargePlayer>().DevourerAscensionActive)
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
                    if (CosmicDischargeCommon.TargetIntersectsTip(extraPoints, target.Hitbox, radius))
                        return true;
                }
            }

            // Also check mirror whip tip!
            bool isWhip = Kind == CosmicDischargeAttackKind.WhipOver || Kind == CosmicDischargeAttackKind.WhipUnder;
            if (isWhip && Owner.GetModPlayer<CosmicDischargePlayer>().DevourerAscensionActive)
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
            bool empActive = modPlayer.DevourerAscensionActive;

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
                    modifiers.FinalDamage *= tip ? 2.2f : 1.1f;
                    modifiers.Knockback *= tip ? 1.5f : 1.0f;
                    break;

                case CosmicDischargeAttackKind.ChainKnifeBiteAll:
                    if (tip)
                    {
                        modifiers.SetCrit(); // 尖端必定暴击
                        modifiers.FinalDamage *= 2.2f;
                    }
                    else
                    {
                        modifiers.DisableCrit(); // 链身无法暴击且伤害衰减
                        modifiers.FinalDamage *= 0.5f;
                    }
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
            bool empActive = modPlayer.DevourerAscensionActive;

            CosmicDischargeCommon.ApplyDoGDebuffs(target, Kind == CosmicDischargeAttackKind.QuickDraw ? 420 : 300);
            ApplyHitStop(heavy ? 6 : 3);
            ApplyScreenShake(empActive ? 12.5f : (heavy ? 9.5f : 4.6f));
            SpawnHitEffects(target, heavy, tip);

            if (Main.myPlayer == Projectile.owner)
            {
                SpawnLegacyCosmicBurst(target, heavy);

                // 1. WHIP FORM SYNERGIES
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

        private void SpawnLegacyCosmicBurst(NPC target, bool heavy)
        {
            if (legacyCosmicBurstCooldown > 0)
                return;

            legacyCosmicBurstCooldown = heavy ? 6 : 9;
            float scale = heavy ? Main.rand.NextFloat(1.05f, 1.4f) : Main.rand.NextFloat(0.78f, 1.08f);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<CosmicIceBurst>(),
                (int)(Projectile.damage * (heavy ? 0.52f : 0.36f)),
                10f,
                Projectile.owner,
                0f,
                scale);
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

            // 链刃终结额外带索敌激光（玩法），其余全部走同一个爆发出口。
            if (Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll)
            {
                CosmicDischargeCommon.SpawnChainFinisherBurst(
                    Projectile.GetSource_FromThis(), Owner, target.Center, direction, Projectile.damage, Projectile.knockBack);
                return;
            }

            CosmicDischargeCommon.SpawnRiftBurst(target.Center, GetHitTier(tip), direction, ModeAccentColor);
        }

        /// <summary>
        /// 命中档位表。形态之间**不换粒子种类**，只换档位和强调色 ——
        /// 这是整套特效有秩序感的核心：九种攻击共用一套视觉语言，差别只在强度。
        /// </summary>
        private RiftTier GetHitTier(bool tip) => Kind switch
        {
            CosmicDischargeAttackKind.QuickDraw => RiftTier.Finisher,
            CosmicDischargeAttackKind.SwordFinisher => RiftTier.Finisher,

            CosmicDischargeAttackKind.SwordSwingTwo => RiftTier.Heavy,
            CosmicDischargeAttackKind.WhipThrust => tip ? RiftTier.Heavy : RiftTier.Light,

            CosmicDischargeAttackKind.SwordSwingOne => RiftTier.Medium,
            CosmicDischargeAttackKind.ChainKnifeSingle or CosmicDischargeAttackKind.ChainKnifeScatter
                => tip ? RiftTier.Heavy : RiftTier.Medium,

            // 挥鞭：收鞭阶段只算轻击，跟伤害衰减保持一致。
            _ => currentlyRetracting ? RiftTier.Light : RiftTier.Medium,
        };

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

            bool isWhip = Kind == CosmicDischargeAttackKind.WhipOver ||
                          Kind == CosmicDischargeAttackKind.WhipUnder;
            bool isChainArc = Kind == CosmicDischargeAttackKind.ChainKnifeSingle ||
                              Kind == CosmicDischargeAttackKind.ChainKnifeScatter ||
                              Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll;

            if (isChainArc)
            {
                // Chain-blade form stays visually taut and readable. The moving heads carry
                // the fan motion; the connector itself no longer inherits their curve.
                CosmicDischargeCommon.DrawChain(Main.spriteBatch, points[0], points[^1], lightColor, Projectile.scale, true, Owner.gfxOffY);
                DrawChainTipHalo(points[^1], 0.8f);
                DrawConstellationOverlay(points, CosmicDischargeCommon.RiftTwilight, 0.52f);
            }
            else
            {
                DrawDoGFireTrail(points);
                DrawCurvedBladeGlow(points);
                CosmicDischargeCommon.DrawCurvedChain(Main.spriteBatch, points, lightColor, Projectile.scale, Owner.gfxOffY);
                DrawConstellationOverlay(points, ModeAccentColor, 0.72f);
            }

            if (isChainArc)
            {
                int chainCount = GetChainCount();
                float reach = Projectile.velocity.Length();
                Color chainColor = Color.Lerp(lightColor, CosmicDischargeCommon.RiftTwilight, 0.35f);
                for (int i = 0; i < chainCount; i++)
                {
                    float lane = i - (chainCount - 1) * 0.5f;
                    if (Math.Abs(lane) < 0.01f)
                        continue;

                    var extraPoints = GenerateChainConvergencePoints(direction, reach, lane);
                    CosmicDischargeCommon.DrawChain(Main.spriteBatch, extraPoints[0], extraPoints[^1], chainColor, Projectile.scale * 0.88f, true, Owner.gfxOffY);
                    DrawChainTipHalo(extraPoints[^1], 0.56f);
                }
            }

            // Under Devourer Ascension, draw the mirror whip simultaneously
            if (isWhip && Owner.GetModPlayer<CosmicDischargePlayer>().DevourerAscensionActive)
            {
                float mirrorSide = Kind == CosmicDischargeAttackKind.WhipOver ? 1f : -1f;
                var mirrorPoints = GenerateWhipPointsForSide(direction, Projectile.velocity.Length(), mirrorSide);
                DrawDoGFireTrail(mirrorPoints);
                DrawCurvedBladeGlow(mirrorPoints);
                Color mirrorColor = Color.Lerp(lightColor, CosmicDischargeCommon.DoGSpecialColor, 0.35f);
                CosmicDischargeCommon.DrawCurvedChain(Main.spriteBatch, mirrorPoints, mirrorColor, Projectile.scale, Owner.gfxOffY);
                DrawConstellationOverlay(mirrorPoints, CosmicDischargeCommon.RiftLightBlue, 0.5f);
            }

            if (CanBecomeQuickDraw)
                CosmicDischargeCommon.DrawRightHoldIndicator(Main.spriteBatch, Owner, 1f + 0.18f * MathF.Sin(Time * 0.45f));

            return false;
        }

        private void DrawDoGFireTrail(System.Collections.Generic.List<Vector2> points)
        {
            if (points == null || points.Count < 2)
                return;

            bool empowered = Owner.GetModPlayer<CosmicDischargePlayer>().UltimateFieldActive ||
                             Owner.GetModPlayer<CosmicDischargePlayer>().DevourerAscensionActive ||
                             Kind == CosmicDischargeAttackKind.QuickDraw ||
                             Kind == CosmicDischargeAttackKind.SwordFinisher ||
                             Kind == CosmicDischargeAttackKind.ChainKnifeBiteAll;
            float opacity = Projectile.Opacity * (empowered ? 0.72f : 0.48f);
            float outerWidth = MathHelper.Clamp(currentCollisionWidth * (empowered ? 1.9f : 1.45f), 32f, empowered ? 108f : 72f);
            float innerWidth = MathHelper.Clamp(currentCollisionWidth * (empowered ? 0.86f : 0.58f), 14f, empowered ? 64f : 28f);

            float OuterWidth(float completion, Vector2 _) => outerWidth * (1f - completion) + 8f * MathF.Sin(Main.GlobalTimeWrappedHourly * 20f + completion * MathHelper.Pi);
            Color OuterColor(float completion, Vector2 _)
            {
                Color main = empowered ? Color.Purple : CosmicDischargeCommon.RiftLightBlue;
                return Color.Lerp(main, Color.Transparent, completion) * opacity;
            }

            float InnerWidth(float completion, Vector2 _) => innerWidth * (1f - completion);
            Color InnerColor(float completion, Vector2 _)
            {
                Color main = empowered ? CosmicDischargeCommon.RiftMagenta : CosmicDischargeCommon.RiftLightBlue;
                return Color.Lerp(Color.Lerp(main, Color.White, 0.175f), Color.Transparent, completion) * opacity;
            }

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(points, new PrimitiveSettings(OuterWidth, OuterColor, smoothen: true, pixelate: false, shader: GameShaders.Misc["CalamityMod:ImpFlameTrail"]), points.Count + 12);

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            PrimitiveRenderer.RenderTrail(points, new PrimitiveSettings(InnerWidth, InnerColor, smoothen: true, pixelate: false, shader: GameShaders.Misc["CalamityMod:ImpFlameTrail"]), points.Count + 8);
        }

        private void DrawCurvedBladeGlow(System.Collections.Generic.List<Vector2> points)
        {
            if (points == null || points.Count < 2)
                return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float flash = impactFlashTimer > 0 ? impactFlashTimer / 8f : 0f;
            Color cyanGlow = CosmicDischargeCommon.Transparent(CosmicDischargeCommon.RiftLightBlue) * (0.15f + flash * 0.15f);
            Color core = CosmicDischargeCommon.DoGWhiteColor * (0.25f + flash * 0.38f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 start = points[i] - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
                Vector2 end = points[i + 1] - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
                Vector2 segment = end - start;
                if (segment.LengthSquared() < 0.1f)
                    continue;

                DrawLine(pixel, start, segment, cyanGlow, 3f);
                DrawLine(pixel, start, segment, core, 1.5f);
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

        private void DrawConstellationOverlay(System.Collections.Generic.IReadOnlyList<Vector2> points, Color accent, float intensity)
        {
            if (points == null || points.Count < 2)
                return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Projectiles/StarProj").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawOffset = -Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
            float pulse = 0.82f + 0.18f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f + Projectile.identity * 0.7f);
            int step = Math.Max(2, points.Count / 5);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Vector2 previousNode = points[0] + drawOffset;
            for (int i = step; i < points.Count; i += step)
            {
                Vector2 node = points[Math.Min(i, points.Count - 1)] + drawOffset;
                Vector2 segment = node - previousNode;
                Color lineColor = Color.Lerp(accent, Color.White, 0.18f) * (0.18f * intensity);
                if (segment.LengthSquared() > 1f)
                    DrawLine(pixel, previousNode, segment, lineColor, 1.1f + intensity * 0.65f);

                float nodeScale = (0.11f + 0.035f * ((i / step) % 2)) * pulse * intensity;
                Main.EntitySpriteDraw(bloom, node, null, accent * (0.2f * intensity), 0f, bloom.Size() * 0.5f, nodeScale * 1.8f, SpriteEffects.None);
                Main.EntitySpriteDraw(star, node, null, Color.White * (0.62f * intensity), Main.GlobalTimeWrappedHourly * 1.6f + i, star.Size() * 0.5f, nodeScale, SpriteEffects.None);
                previousNode = node;
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private void DrawChainTipHalo(Vector2 worldPosition, float intensity)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Projectiles/StarProj").Value;
            Vector2 drawPosition = worldPosition - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
            float pulse = 0.9f + 0.1f * MathF.Sin(Main.GlobalTimeWrappedHourly * 7f + worldPosition.X * 0.01f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null, CosmicDischargeCommon.RiftTwilight * (0.24f * intensity), 0f, bloom.Size() * 0.5f, 0.18f * pulse * intensity, SpriteEffects.None);
            Main.EntitySpriteDraw(star, drawPosition, null, Color.White * (0.58f * intensity), Projectile.rotation, star.Size() * 0.5f, 0.13f * pulse * intensity, SpriteEffects.None);
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

        private static float SmootherStep(float value)
        {
            value = MathHelper.Clamp(value, 0f, 1f);
            return value * value * value * (value * (value * 6f - 15f) + 10f);
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







    }
}
