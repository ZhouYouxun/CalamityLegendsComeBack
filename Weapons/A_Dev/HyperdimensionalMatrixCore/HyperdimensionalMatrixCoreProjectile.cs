using System;
using System.Collections.Generic;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore
{
    internal static class MatrixCoreNumbers
    {
        public const int FormDuration = 420;
        public const float TargetingRange = 2400f;

        public const int PiercingAttackInterval = 18;
        public const int PiercingProjectileCount = 8;
        public const float PiercingProjectileDamageMultiplier = 0.24f;
        public const int PiercingProjectilePenetration = 8;
        public const int PiercingProjectileLocalHitCooldown = 8;
        public const float PiercingProjectileSpawnRadius = 24f;
        public const float PiercingProjectileInitialSpeed = 24f;
        public const float PiercingProjectileHomingSpeed = 33f;
        public const float PiercingProjectileMaxTurn = 0.15f;
        public const float PiercingProjectileAcceleration = 0.08f;

        public const int OrbitalBurstCount = 5;
        public const int OrbitalBurstShotInterval = 5;
        public const int OrbitalBurstCooldown = 48;
        public const int OrbitalProjectileCountMin = 6;
        public const int OrbitalProjectileCountMax = 9;
        public const float OrbitalProjectileDamageMultiplier = 0.5f;
        public const int OrbitalProjectilePenetration = 1;
        public const int OrbitalProjectileLocalHitCooldown = 10;
        public const int OrbitalProjectileLifetime = 270;
        public const int OrbitalMaxSimultaneousTargets = 6;
        public const float OrbitalProjectileSpeedMultiplier = 1.3f;
        public const float OrbitalProjectileGravity = 0.55f;
        public const float OrbitalProjectileMaxFallSpeed = 39f;
        public const float OrbitalProjectileSpawnSpreadX = 260f;
        public const float OrbitalProjectileAimSpreadX = 110f;
        public const float OrbitalProjectileAimSpreadY = 70f;
        public const float OrbitalProjectileHorizontalCorrectionStrength = 0.006f;
        public const float OrbitalProjectileMaxHorizontalCorrection = 1.6f;
        public const float OrbitalProjectileHorizontalCorrectionLerp = 0.04f;
        public const float OrbitalExplosionDamageMultiplier = 1.15f;
        public const int OrbitalExplosionPenetration = -1;
        public const int OrbitalExplosionLocalHitCooldown = -1;
        public const int OrbitalExplosionSize = 75;

        public const int FractureProjectileLifetime = 132;
        public const int FractureAttackInterval = FractureProjectileLifetime;
        public const int FractureProjectileCount = 1;
        public const float FractureProjectileDamageMultiplier = 1f;
        public const int FractureProjectilePenetration = -1;
        public const int FractureProjectileLocalHitCooldown = 5;

        public const int HyperdimensionalAttackInterval = 1;
        public const int HyperdimensionalProjectileCount = 1;
        public const float HyperdimensionalProjectileDamageMultiplier = 0.48f;
        public const int HyperdimensionalProjectilePenetration = -1;
        public const int HyperdimensionalProjectileLocalHitCooldown = 5;
        public const float HyperdimensionalBeamLength = 3600f;

        public static int OrbitalBurstCycleLength =>
            (OrbitalBurstCount - 1) * OrbitalBurstShotInterval + OrbitalBurstCooldown;
    }

    public sealed class HyperdimensionalMatrixCoreProjectile : ModProjectile, ILocalizedModType
    {
        public enum MatrixForm
        {
            Piercing,
            Orbital,
            Fracture,
            Hyperdimensional
        }

        public const int FormCount = 4;

        private int attackTimer;
        private int targetRefreshTimer;
        private MatrixForm previousForm = (MatrixForm)(-1);

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public MatrixForm CurrentForm => (MatrixForm)((int)(Projectile.ai[0] / MatrixCoreNumbers.FormDuration) % FormCount);
        public int TargetIndex => (int)Projectile.ai[1] - 1;

        public override void SetStaticDefaults()
        {
            Main.projPet[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = false;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!owner.HasBuff<HyperdimensionalMatrixCoreBuff>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.minionSlots = Math.Max(1f, owner.maxMinions);
            if (Main.myPlayer == Projectile.owner && (Main.GameUpdateCount + Projectile.identity) % 30 == 0)
                HyperdimensionalMatrixCore.RemoveOtherSlotConsumingMinions(owner, Type);

            UpdatePosition(owner);
            UpdateDamage(owner);
            UpdateForm();
            UpdateTarget(owner);

            NPC target = GetTarget();
            if (Main.myPlayer == Projectile.owner && target != null)
                RunCurrentFormAttack(target);
            else if (target == null)
                attackTimer = 0;

            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(0.25f).ToVector3() * 0.5f);
        }

        public NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC target = Main.npc[TargetIndex];
            return target.CanBeChasedBy(Projectile, false) ? target : null;
        }

        public static float GetSlotDamageMultiplier(Player owner)
        {
            int slotsUsed = Math.Max(1, owner.maxMinions);
            return 1f + (slotsUsed - 1) * 0.5f;
        }

        public void AdvanceToNextForm()
        {
            int nextForm = ((int)CurrentForm + 1) % FormCount;
            Projectile.ai[0] = nextForm * MatrixCoreNumbers.FormDuration;
            previousForm = (MatrixForm)(-1);
            attackTimer = 0;
            Projectile.netUpdate = true;
        }

        private void UpdatePosition(Player owner)
        {
            float time = Main.GlobalTimeWrappedHourly;
            Vector2 hoverPosition = owner.Top + new Vector2(
                (float)Math.Sin(time * 1.5f + Projectile.identity * 0.1f) * 9f,
                -72f + (float)Math.Sin(time * 2.2f) * 5f);

            if (Vector2.DistanceSquared(Projectile.Center, hoverPosition) > 900f * 900f)
            {
                Projectile.Center = hoverPosition;
                Projectile.netUpdate = true;
            }
            else
            {
                Projectile.Center = Vector2.Lerp(Projectile.Center, hoverPosition, 0.24f);
            }

            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = time;
        }

        private void UpdateDamage(Player owner)
        {
            int baseDamage = Projectile.originalDamage > 0
                ? Projectile.originalDamage
                : HyperdimensionalMatrixCore.BaseDamage;
            float slotMultiplier = GetSlotDamageMultiplier(owner);
            Projectile.damage = Math.Max(
                1,
                (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(baseDamage * slotMultiplier));
        }

        private void UpdateForm()
        {
            Projectile.ai[0]++;
            if (Projectile.ai[0] >= MatrixCoreNumbers.FormDuration * FormCount)
            {
                Projectile.ai[0] = 0f;
                Projectile.netUpdate = true;
            }

            if (CurrentForm == previousForm)
                return;

            previousForm = CurrentForm;
            attackTimer = 0;
            Projectile.netUpdate = true;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(
                    SoundID.Item15 with
                    {
                        Volume = 0.48f,
                        Pitch = -0.18f + (int)CurrentForm * 0.12f,
                        MaxInstances = 6
                    },
                    Projectile.Center);
            }
        }

        private void UpdateTarget(Player owner)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            targetRefreshTimer--;
            if (targetRefreshTimer > 0 && GetTarget() != null)
                return;

            targetRefreshTimer = 12;
            NPC target = FindTarget(owner);
            int encodedTarget = target?.whoAmI + 1 ?? 0;
            if ((int)Projectile.ai[1] == encodedTarget)
                return;

            Projectile.ai[1] = encodedTarget;
            Projectile.netUpdate = true;
        }

        private NPC FindTarget(Player owner)
        {
            if (owner.HasMinionAttackTargetNPC && Main.npc.IndexInRange(owner.MinionAttackTargetNPC))
            {
                NPC designatedTarget = Main.npc[owner.MinionAttackTargetNPC];
                if (designatedTarget.CanBeChasedBy(Projectile, false) &&
                    Vector2.Distance(owner.Center, designatedTarget.Center) <= MatrixCoreNumbers.TargetingRange * 1.35f)
                {
                    return designatedTarget;
                }
            }

            NPC closestTarget = null;
            float closestDistance = MatrixCoreNumbers.TargetingRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                float distance = Vector2.Distance(owner.Center, npc.Center);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestTarget = npc;
            }

            return closestTarget;
        }

        private void RunCurrentFormAttack(NPC target)
        {
            attackTimer++;
            switch (CurrentForm)
            {
                case MatrixForm.Piercing:
                    RunPiercingAttack(target);
                    break;
                case MatrixForm.Orbital:
                    RunOrbitalAttack(target);
                    break;
                case MatrixForm.Fracture:
                    RunFractureAttack(target);
                    break;
                case MatrixForm.Hyperdimensional:
                    RunHyperdimensionalAttack(target);
                    break;
            }
        }

        private void RunPiercingAttack(NPC target)
        {
            if ((attackTimer - 1) % MatrixCoreNumbers.PiercingAttackInterval != 0)
                return;

            float startingAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < MatrixCoreNumbers.PiercingProjectileCount; i++)
            {
                Vector2 direction = (startingAngle + MathHelper.TwoPi * i / MatrixCoreNumbers.PiercingProjectileCount)
                    .ToRotationVector2()
                    .RotatedByRandom(0.045f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + direction * MatrixCoreNumbers.PiercingProjectileSpawnRadius,
                    direction * MatrixCoreNumbers.PiercingProjectileInitialSpeed,
                    ModContent.ProjectileType<MatrixDataNeedle>(),
                    Math.Max(1, (int)(Projectile.damage * MatrixCoreNumbers.PiercingProjectileDamageMultiplier)),
                    Projectile.knockBack,
                    Projectile.owner,
                    target.whoAmI);
            }

            SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.18f, Pitch = 0.45f, MaxInstances = 8 }, Projectile.Center);
        }

        private void RunOrbitalAttack(NPC target)
        {
            int burstFrame = (attackTimer - 1) % MatrixCoreNumbers.OrbitalBurstCycleLength;
            int finalBurstShotFrame = (MatrixCoreNumbers.OrbitalBurstCount - 1) * MatrixCoreNumbers.OrbitalBurstShotInterval;
            if (burstFrame > finalBurstShotFrame || burstFrame % MatrixCoreNumbers.OrbitalBurstShotInterval != 0)
                return;

            int projectileCount = Main.rand.Next(MatrixCoreNumbers.OrbitalProjectileCountMin, MatrixCoreNumbers.OrbitalProjectileCountMax + 1);
            List<NPC> orbitalTargets = FindOrbitalTargets(target);
            int targetOffset = orbitalTargets.Count > 0 ? Main.rand.Next(orbitalTargets.Count) : 0;
            for (int i = 0; i < projectileCount; i++)
            {
                NPC selectedTarget = orbitalTargets.Count > 0
                    ? orbitalTargets[(i + targetOffset) % orbitalTargets.Count]
                    : target;
                Vector2 spawnPosition = selectedTarget.Center + new Vector2(
                    Main.rand.NextFloat(-MatrixCoreNumbers.OrbitalProjectileSpawnSpreadX, MatrixCoreNumbers.OrbitalProjectileSpawnSpreadX),
                    -Main.rand.NextFloat(500f, 720f));
                Vector2 aimPosition = selectedTarget.Center + new Vector2(
                    Main.rand.NextFloat(-MatrixCoreNumbers.OrbitalProjectileAimSpreadX, MatrixCoreNumbers.OrbitalProjectileAimSpreadX),
                    Main.rand.NextFloat(-MatrixCoreNumbers.OrbitalProjectileAimSpreadY, MatrixCoreNumbers.OrbitalProjectileAimSpreadY));
                Vector2 velocity = (aimPosition - spawnPosition).SafeNormalize(Vector2.UnitY) *
                    Main.rand.NextFloat(17f, 22f) *
                    MatrixCoreNumbers.OrbitalProjectileSpeedMultiplier;

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<MatrixOrbitalProjection>(),
                    Math.Max(1, (int)(Projectile.damage * MatrixCoreNumbers.OrbitalProjectileDamageMultiplier)),
                    Projectile.knockBack,
                    Projectile.owner,
                    selectedTarget.whoAmI,
                    Main.rand.NextFloat(MathHelper.TwoPi));
            }

            SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.24f, Pitch = 0.18f, MaxInstances = 5 }, target.Center);
        }

        private List<NPC> FindOrbitalTargets(NPC priorityTarget)
        {
            List<NPC> targets = new(MatrixCoreNumbers.OrbitalMaxSimultaneousTargets);
            if (priorityTarget.CanBeChasedBy(Projectile, false))
                targets.Add(priorityTarget);

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (targets.Count >= MatrixCoreNumbers.OrbitalMaxSimultaneousTargets)
                    break;

                if (npc.whoAmI == priorityTarget.whoAmI ||
                    !npc.CanBeChasedBy(Projectile, false) ||
                    Vector2.Distance(Projectile.Center, npc.Center) > MatrixCoreNumbers.TargetingRange)
                {
                    continue;
                }

                targets.Add(npc);
            }

            return targets;
        }

        private void RunFractureAttack(NPC target)
        {
            if ((attackTimer - 1) % MatrixCoreNumbers.FractureAttackInterval != 0)
                return;

            for (int i = 0; i < MatrixCoreNumbers.FractureProjectileCount; i++)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<MatrixFractureField>(),
                    Math.Max(1, (int)(Projectile.damage * MatrixCoreNumbers.FractureProjectileDamageMultiplier)),
                    Projectile.knockBack,
                    Projectile.owner,
                    target.whoAmI,
                    Main.rand.NextFloat(MathHelper.TwoPi));
            }

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.28f, Pitch = 0.28f, MaxInstances = 5 }, target.Center);
        }

        private void RunHyperdimensionalAttack(NPC target)
        {
            if ((attackTimer - 1) % MatrixCoreNumbers.HyperdimensionalAttackInterval != 0)
                return;

            int beamType = ModContent.ProjectileType<HyperdimensionalMatrixBeam>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner == Projectile.owner &&
                    projectile.type == beamType &&
                    (int)projectile.ai[0] == Projectile.whoAmI)
                {
                    return;
                }
            }

            for (int i = 0; i < MatrixCoreNumbers.HyperdimensionalProjectileCount; i++)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY),
                    beamType,
                    Math.Max(1, (int)(Projectile.damage * MatrixCoreNumbers.HyperdimensionalProjectileDamageMultiplier)),
                    Projectile.knockBack,
                    Projectile.owner,
                    Projectile.whoAmI);
            }

            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.38f, Pitch = -0.2f, MaxInstances = 4 }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            float time = Main.GlobalTimeWrappedHourly;
            float transition = MathHelper.Clamp((Projectile.ai[0] % MatrixCoreNumbers.FormDuration) / 30f, 0f, 1f);
            float opacity = 0.72f + transition * 0.28f;

            HyperdimensionalMatrixVisuals.DrawShield(owner, 0.74f);

            NPC target = GetTarget();
            if (target != null)
                HyperdimensionalMatrixVisuals.DrawTargetingLine(Projectile.Center, target.Center, 0.3f);

            switch (CurrentForm)
            {
                case MatrixForm.Piercing:
                    HyperdimensionalMatrixVisuals.DrawGeometry(
                        Projectile.Center,
                        MatrixGeometryShape.Tetrahedron,
                        59f,
                        time * 1.45f,
                        opacity,
                        Projectile.identity);
                    break;
                case MatrixForm.Orbital:
                    HyperdimensionalMatrixVisuals.DrawGeometry(
                        Projectile.Center,
                        MatrixGeometryShape.Icosahedron,
                        68f,
                        time * 1.05f,
                        opacity,
                        Projectile.identity);
                    break;
                case MatrixForm.Fracture:
                    HyperdimensionalMatrixVisuals.DrawGeometry(
                        Projectile.Center,
                        MatrixGeometryShape.Cube,
                        64f,
                        time * 0.82f,
                        opacity,
                        Projectile.identity);
                    break;
                case MatrixForm.Hyperdimensional:
                    HyperdimensionalMatrixVisuals.DrawHypercube(Projectile.Center, 57f, time, opacity);
                    break;
            }

            return false;
        }
    }
}
