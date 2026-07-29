using System;
using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.General;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    // 青霆剑左键施法器：武器本体不再绘制，双手动作直接驱动四段雷法。
    internal sealed class AzureThunderSwingHoldout : ModProjectile, ILocalizedModType
    {
        private const int FirstStageDuration = 28;
        private const int SecondStageDuration = 30;
        private const int ThirdStageDuration = FirstStageDuration + SecondStageDuration;
        private const int FourthStageDuration = 42;
        private const int StageGap = 5;
        private const int HarmonyBarrageCount = 3;

        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Player Owner => Main.player[Projectile.owner];
        private AzureThunderPlayer ThunderPlayer => Owner.GetModPlayer<AzureThunderPlayer>();
        private bool HarmonyActive => ThunderPlayer.HarmonyActive;
        private int ComboLength => HarmonyActive ? 3 : 4;

        private int comboIndex;
        private int currentStage;
        private int stageTimer;
        private int stageDuration;
        private int gapTimer;
        private int harmonyShots;
        private int primaryTargetIndex = -1;
        private bool stageActive;
        private bool stageEventTriggered;
        private bool releaseRequested;
        private Vector2 lockedMouseWorld;
        private Vector2 lockedAimDirection;
        private float frontArmRotation;
        private float backArmRotation;
        private Player.CompositeArmStretchAmount frontStretch;
        private Player.CompositeArmStretchAmount backStretch;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            lockedMouseWorld = AzureThunderPlayer.GetMouseWorld(Owner);
            lockedAimDirection = (lockedMouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction);
            Owner.direction = lockedAimDirection.X >= 0f ? 1 : -1;

            if (ThunderPlayer.ConsumeDashHeavyStrike())
                comboIndex = 3;
            else if (ThunderPlayer.TryConsumeRetainedLeftCombo(out int retainedComboIndex))
                comboIndex = retainedComboIndex;
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem?.type != ModContent.ItemType<AzureThunder>())
            {
                Projectile.Kill();
                return;
            }

            Owner.Calamity().mouseWorldListener = true;
            Owner.Calamity().rightClickListener = true;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Projectile.Center = Owner.MountedCenter;
            Projectile.timeLeft = 2;

            bool holdingLeft = Owner.channel &&
                (Main.myPlayer != Projectile.owner || Main.mouseLeft) &&
                !Main.mapFullscreen &&
                !Main.blockMouse;
            if (!holdingLeft)
                releaseRequested = true;

            if (!stageActive)
            {
                if (gapTimer > 0)
                {
                    gapTimer--;
                    UpdateAimFromMouse();
                    SetRestingArms();
                    ApplyArms();
                    SpawnHandGlow();
                    return;
                }

                StartStage();
                if (!Projectile.active)
                    return;
            }

            stageTimer++;
            if (HarmonyActive)
                RunHarmonyStage();
            else
                RunNormalStage();

            UpdateArmAnimation();
            ApplyArms();
            SpawnHandGlow();

            if (stageTimer >= stageDuration)
                EndStage();
        }

        private void StartStage()
        {
            if (Main.myPlayer == Projectile.owner && !ThunderPlayer.TrySpendMana())
            {
                Projectile.Kill();
                return;
            }

            stageActive = true;
            stageEventTriggered = false;
            harmonyShots = 0;
            stageTimer = 0;
            currentStage = comboIndex % ComboLength;
            stageDuration = HarmonyActive ? (currentStage == 2 ? 52 : 48) : currentStage switch
            {
                0 => FirstStageDuration,
                1 => SecondStageDuration,
                2 => ThirdStageDuration,
                _ => FourthStageDuration
            };

            UpdateAimFromMouse();
            NPC target = ResolvePrimaryTarget();
            if (target != null)
                primaryTargetIndex = target.whoAmI;

            if (!HarmonyActive && currentStage == 3)
                ThunderPlayer.RestoreManaForOwnedSwords(includeLeftClickGrowth: true);
        }

        private void RunNormalStage()
        {
            switch (currentStage)
            {
                case 0:
                    if (!stageEventTriggered && stageTimer >= 14)
                    {
                        SpawnOpeningLightningLine();
                        stageEventTriggered = true;
                    }
                    break;

                case 1:
                    if (!stageEventTriggered && stageTimer >= 15)
                    {
                        SpawnSecondStageAttack();
                        stageEventTriggered = true;
                    }
                    break;

                case 2:
                    if (!stageEventTriggered && stageTimer >= 22)
                    {
                        SpawnThirdStageAttack();
                        stageEventTriggered = true;
                    }
                    break;

                case 3:
                    if (!stageEventTriggered && stageTimer >= 18)
                    {
                        SpawnFinalCannonStrike();
                        stageEventTriggered = true;
                    }
                    break;
            }
        }

        private void RunHarmonyStage()
        {
            if (currentStage <= 1)
            {
                if (harmonyShots < HarmonyBarrageCount && stageTimer >= 10 + harmonyShots * 8)
                {
                    SpawnHarmonyBarrage(harmonyShots);
                    harmonyShots++;
                }

                if (!stageEventTriggered && harmonyShots >= HarmonyBarrageCount && stageTimer >= 38)
                {
                    SpawnGrandSword(0.78f);
                    stageEventTriggered = true;
                }
                return;
            }

            if (!stageEventTriggered && stageTimer >= 18)
            {
                SpawnHarmonyFinalJudgement();
                stageEventTriggered = true;
            }
        }

        private void SpawnOpeningLightningLine()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            NPC target = ResolvePrimaryTarget();
            Vector2 targetPoint = target?.Center ?? lockedMouseWorld;
            Vector2 start = GetHandPosition(frontArmRotation, true);
            AzureThunderPlayer.SpawnFlatLightning(
                Projectile.GetSource_FromThis(),
                start,
                targetPoint - start,
                Math.Max(1, (int)(Projectile.damage * 0.65f)),
                Projectile.knockBack,
                Projectile.owner,
                0.82f,
                AzureThunderFlatLightning.GainChargeFlag | AzureThunderFlatLightning.NormalVisualIntensityFlag);

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.58f, Pitch = 0.32f }, start);
        }

        private void SpawnSecondStageAttack()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            NPC target = ResolvePrimaryTarget();
            Vector2 impact = target?.Center ?? lockedMouseWorld;
            int groupToken = BuildGroupToken(2);
            SpawnFlyingSwords(2, target, -1, AzureThunderFlyingSword.AttackModeNormal, groupToken, false);

            int lightning = AzureThunderPlayer.SpawnVerticalLightning(
                Projectile.GetSource_FromThis(),
                impact,
                target,
                Math.Max(1, (int)(Projectile.damage * 0.55f)),
                Projectile.knockBack,
                Projectile.owner,
                gainCharge: true,
                spawnHeightMultiplier: 0.78f,
                weak: true,
                lightningScale: 0.82f);

            if (Main.projectile.IndexInRange(lightning))
            {
                Main.projectile[lightning].localAI[0] = groupToken;
                Main.projectile[lightning].localAI[1] = 1f;
            }
            else
                AzureThunderFlyingSword.ReleaseWaitingGroup(Projectile.owner, groupToken);
        }

        private void SpawnThirdStageAttack()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            NPC target = ResolvePrimaryTarget();
            int groupToken = BuildGroupToken(3);
            SpawnFlyingSwords(4, target, 2, AzureThunderFlyingSword.AttackModeTriggerCannon, groupToken, true);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 3.5f);
        }

        private void SpawnFinalCannonStrike()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            NPC target = ResolvePrimaryTarget();
            Vector2 impact = target?.Center ?? lockedMouseWorld;
            AzureThunderCannonStrike.Spawn(
                Projectile.GetSource_FromThis(),
                impact,
                target,
                Math.Max(1, (int)(Projectile.damage * 5.4f)),
                Projectile.knockBack,
                Projectile.owner,
                1.45f,
                AzureThunderCannonStrike.RedFinaleFlag | AzureThunderCannonStrike.LeftComboFlag);
        }

        private void SpawnFlyingSwords(int count, NPC target, int delay, int attackMode, int groupToken, bool wideFormation)
        {
            Vector2 forward = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            AzureThunderSounds.PlaySwordMaterialize(Owner.Center);

            for (int i = 0; i < count; i++)
            {
                float centered = i - (count - 1) * 0.5f;
                float side = centered * (wideFormation ? 44f : 56f);
                float rear = wideFormation ? 72f + Math.Abs(centered) * 16f : 52f;
                Vector2 spawnPosition = Owner.MountedCenter - forward * rear + normal * side - Vector2.UnitY * (wideFormation ? 18f : 8f);
                int sword = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    Vector2.Zero,
                    ModContent.ProjectileType<AzureThunderFlyingSword>(),
                    Math.Max(1, (int)(Projectile.damage * (wideFormation ? 0.32f : 0.28f))),
                    Projectile.knockBack,
                    Projectile.owner,
                    delay < 0 ? -1f : delay + i * 2,
                    target?.whoAmI ?? -1,
                    attackMode);

                if (!Main.projectile.IndexInRange(sword))
                    continue;

                Main.projectile[sword].localAI[2] = groupToken;
                AzureThunderPlayer.ApplyProjectileGrowth(Main.projectile[sword]);
            }
        }

        private void SpawnHarmonyBarrage(int index)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 axis = lockedAimDirection.RotatedBy(MathHelper.PiOver2);
            Vector2 impact = lockedMouseWorld + axis * ((index - 1f) * 138f);
            AzureThunderPlayer.SpawnVerticalLightning(
                Projectile.GetSource_FromThis(), impact, null,
                Math.Max(1, (int)(Projectile.damage * 0.8f)), Projectile.knockBack, Projectile.owner,
                applyStaticDischarge: true, big: true, spawnHeightMultiplier: 0.82f,
                fixedTiltRadians: GetFixedLightningTilt(), oneThirdVisualIntensity: true, lightningScale: 2.5f);
        }

        private void SpawnGrandSword(float damageMultiplier)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            NPC target = ResolvePrimaryTarget();
            Vector2 impact = target?.Center ?? lockedMouseWorld;
            int sword = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(), impact - Vector2.UnitY * 780f, Vector2.Zero,
                ModContent.ProjectileType<AzureThunderGrandSword>(),
                Math.Max(1, (int)(Projectile.damage * AzureThunderProgression.UltimateGrandSwordDamageFactor * damageMultiplier)),
                Projectile.knockBack, Projectile.owner, target?.whoAmI ?? -1, impact.X, impact.Y);

            if (Main.projectile.IndexInRange(sword))
            {
                Main.projectile[sword].localAI[1] = 1f;
                AzureThunderPlayer.ApplyProjectileGrowth(Main.projectile[sword]);
            }
        }

        private void SpawnHarmonyFinalJudgement()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            NPC target = ResolvePrimaryTarget();
            Vector2 impact = target?.Center ?? lockedMouseWorld;
            int judgement = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(), impact - Vector2.UnitY * 920f, Vector2.UnitY,
                ModContent.ProjectileType<AzureThunderFinalJudgementBolt>(),
                Math.Max(1, (int)(Projectile.damage * AzureThunderProgression.UltimateRightClickFinalDamageFactor * 1.12f)),
                Projectile.knockBack, Projectile.owner, target?.whoAmI ?? -1, impact.X, impact.Y);

            if (Main.projectile.IndexInRange(judgement))
                AzureThunderPlayer.ApplyProjectileGrowth(Main.projectile[judgement]);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 10f);
        }

        private NPC ResolvePrimaryTarget()
        {
            if (Main.npc.IndexInRange(primaryTargetIndex))
            {
                NPC retained = Main.npc[primaryTargetIndex];
                if (retained.active && retained.CanBeChasedBy(Projectile) && retained.Distance(Owner.Center) <= 1800f)
                    return retained;
            }

            NPC target = FindCenterlineTarget();
            if (target != null)
                primaryTargetIndex = target.whoAmI;
            return target;
        }

        private NPC FindCenterlineTarget()
        {
            Vector2 origin = Owner.MountedCenter;
            Vector2 direction = (lockedMouseWorld - origin).SafeNormalize(Vector2.UnitX * Owner.direction);
            NPC bestTarget = null;
            float bestScore = float.MaxValue;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                Vector2 offset = npc.Center - origin;
                float forward = Vector2.Dot(offset, direction);
                if (forward < -40f || forward > 1700f)
                    continue;

                float perpendicular = Math.Abs(offset.X * direction.Y - offset.Y * direction.X);
                float score = perpendicular + forward * 0.012f;
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestTarget = npc;
            }

            return bestTarget ?? AzureThunderPlayer.FindMouseNearestTarget(Owner);
        }

        private void UpdateAimFromMouse()
        {
            lockedMouseWorld = AzureThunderPlayer.GetMouseWorld(Owner);
            lockedAimDirection = (lockedMouseWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
            Owner.direction = lockedAimDirection.X >= 0f ? 1 : -1;
        }

        private void UpdateArmAnimation()
        {
            float progress = MathHelper.Clamp(stageTimer / (float)Math.Max(1, stageDuration), 0f, 1f);
            float aimRotation = lockedAimDirection.ToRotation() - MathHelper.PiOver2;
            float frontOffset;
            float backOffset;

            if (HarmonyActive)
            {
                float cast = EaseBackOut(Utils.GetLerpValue(0f, 0.42f, progress, true));
                float settle = SmoothStep(Utils.GetLerpValue(0.68f, 1f, progress, true));
                float spread = MathHelper.Lerp(0.18f, currentStage == 2 ? 0.2f : 0.72f, cast) * (1f - settle);
                frontOffset = spread;
                backOffset = -spread;
            }
            else
            {
                switch (currentStage)
                {
                    case 0:
                    {
                        float throwUp = EaseBackOut(Utils.GetLerpValue(0f, 0.58f, progress, true));
                        float recover = SmoothStep(Utils.GetLerpValue(0.62f, 1f, progress, true));
                        frontOffset = MathHelper.Lerp(1.28f, -0.28f, throwUp);
                        frontOffset = MathHelper.Lerp(frontOffset, 0.08f, recover);
                        backOffset = MathHelper.Lerp(-0.42f, 0.22f, SmoothStep(progress));
                        break;
                    }
                    case 1:
                    {
                        float throwUp = EaseBackOut(Utils.GetLerpValue(0f, 0.56f, progress, true));
                        float recover = SmoothStep(Utils.GetLerpValue(0.64f, 1f, progress, true));
                        backOffset = MathHelper.Lerp(-1.22f, 0.32f, throwUp);
                        backOffset = MathHelper.Lerp(backOffset, -0.08f, recover);
                        frontOffset = MathHelper.Lerp(0.38f, -0.18f, SmoothStep(progress));
                        break;
                    }
                    case 2:
                    {
                        float open = EaseBackOut(Utils.GetLerpValue(0f, 0.44f, progress, true));
                        float settle = SmoothStep(Utils.GetLerpValue(0.72f, 1f, progress, true));
                        float spread = MathHelper.Lerp(0.08f, 1.06f, open);
                        spread = MathHelper.Lerp(spread, 0.42f, settle);
                        frontOffset = spread;
                        backOffset = -spread;
                        break;
                    }
                    default:
                    {
                        float open = EaseBackOut(Utils.GetLerpValue(0f, 0.34f, progress, true));
                        float close = CalamityUtils.ExpInOutEasing(Utils.GetLerpValue(0.34f, 0.62f, progress, true), 2);
                        float recover = SmoothStep(Utils.GetLerpValue(0.72f, 1f, progress, true));
                        float spread = MathHelper.Lerp(0.32f, 1.02f, open);
                        spread = MathHelper.Lerp(spread, 0.04f, close);
                        spread = MathHelper.Lerp(spread, 0.22f, recover);
                        frontOffset = spread;
                        backOffset = -spread;
                        break;
                    }
                }
            }

            frontArmRotation = aimRotation + frontOffset;
            backArmRotation = aimRotation + backOffset;
            frontStretch = stageTimer < 5 ? Player.CompositeArmStretchAmount.ThreeQuarters : Player.CompositeArmStretchAmount.Full;
            backStretch = stageTimer < 5 ? Player.CompositeArmStretchAmount.ThreeQuarters : Player.CompositeArmStretchAmount.Full;
        }

        private void SetRestingArms()
        {
            float aimRotation = lockedAimDirection.ToRotation() - MathHelper.PiOver2;
            frontArmRotation = aimRotation + 0.18f;
            backArmRotation = aimRotation - 0.18f;
            frontStretch = Player.CompositeArmStretchAmount.ThreeQuarters;
            backStretch = Player.CompositeArmStretchAmount.ThreeQuarters;
        }

        private void ApplyArms()
        {
            Owner.SetCompositeArmFront(true, frontStretch, frontArmRotation);
            Owner.SetCompositeArmBack(true, backStretch, backArmRotation);
        }

        private void SpawnHandGlow()
        {
            if (Main.dedServ)
                return;

            SpawnSingleHandGlow(GetHandPosition(frontArmRotation, true), currentStage == 3 ? new Color(255, 76, 92) : new Color(77, 255, 218));
            SpawnSingleHandGlow(GetHandPosition(backArmRotation, false), currentStage == 3 ? new Color(255, 116, 96) : new Color(122, 245, 255));
        }

        private static void SpawnSingleHandGlow(Vector2 position, Color color)
        {
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(4f, 4f), DustID.FireworksRGB, Main.rand.NextVector2Circular(0.55f, 0.55f), 0, color, Main.rand.NextFloat(0.55f, 0.9f));
                dust.noGravity = true;
            }

            if (Main.GameUpdateCount % 8 == 0)
                GeneralParticleHandler.SpawnParticle(new CustomPulse(position, Vector2.Zero, color, "CalamityMod/Particles/BloomCircle", Vector2.One, 0f, 0.2f, 0.02f, 8, true, 0.5f));
        }

        private Vector2 GetHandPosition(float armRotation, bool front)
        {
            Vector2 shoulder = Owner.MountedCenter + new Vector2((front ? 3f : -2f) * Owner.direction, -5f + Owner.gfxOffY);
            return shoulder + (armRotation + MathHelper.PiOver2).ToRotationVector2() * 22f;
        }

        private void EndStage()
        {
            stageActive = false;
            stageTimer = 0;
            comboIndex++;

            if (releaseRequested)
            {
                if (!HarmonyActive)
                    ThunderPlayer.RetainLeftCombo(comboIndex % 4);
                Projectile.Kill();
                return;
            }

            gapTimer = StageGap;
        }

        private int BuildGroupToken(int salt) => (Projectile.identity + 1) * 16 + comboIndex * 3 + salt;

        private float GetFixedLightningTilt()
        {
            int direction = Math.Abs(lockedAimDirection.X) > 0.05f ? Math.Sign(lockedAimDirection.X) : Owner.direction;
            return MathHelper.ToRadians(direction < 0 ? 10f : -10f);
        }

        private static float SmoothStep(float value) => value * value * (3f - 2f * value);

        private static float EaseBackOut(float value)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float t = value - 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;
        public override bool PreDraw(ref Color lightColor) => false;
    }
}
