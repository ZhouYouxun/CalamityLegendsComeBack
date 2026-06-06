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

        public MatrixForm CurrentForm => (MatrixForm)((int)(Projectile.ai[0] / Balance.FormDuration) % FormCount);
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
            Projectile.ai[0] = nextForm * Balance.FormDuration;
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
            if (Projectile.ai[0] >= Balance.FormDuration * FormCount)
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
                    Vector2.Distance(owner.Center, designatedTarget.Center) <= Balance.TargetingRange * 1.35f)
                {
                    return designatedTarget;
                }
            }

            NPC closestTarget = null;
            float closestDistance = Balance.TargetingRange;
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
            if ((attackTimer - 1) % Balance.PiercingAttackInterval != 0)
                return;

            float startingAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < Balance.PiercingProjectileCount; i++)
            {
                Vector2 direction = (startingAngle + MathHelper.TwoPi * i / Balance.PiercingProjectileCount)
                    .ToRotationVector2()
                    .RotatedByRandom(0.045f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + direction * Balance.PiercingProjectileSpawnRadius,
                    direction * Balance.PiercingProjectileInitialSpeed,
                    ModContent.ProjectileType<MatrixDataNeedle>(),
                    Math.Max(1, (int)(Projectile.damage * Balance.PiercingProjectileDamageMultiplier)),
                    Projectile.knockBack,
                    Projectile.owner,
                    target.whoAmI);
            }

            SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.18f, Pitch = 0.45f, MaxInstances = 8 }, Projectile.Center);
        }

        private void RunOrbitalAttack(NPC target)
        {
            int burstFrame = (attackTimer - 1) % Balance.OrbitalBurstCycleLength;
            int finalBurstShotFrame = (Balance.OrbitalBurstCount - 1) * Balance.OrbitalBurstShotInterval;
            if (burstFrame > finalBurstShotFrame || burstFrame % Balance.OrbitalBurstShotInterval != 0)
                return;

            int projectileCount = Main.rand.Next(Balance.OrbitalProjectileCountMin, Balance.OrbitalProjectileCountMax + 1);
            List<NPC> orbitalTargets = FindOrbitalTargets(target);
            int targetOffset = orbitalTargets.Count > 0 ? Main.rand.Next(orbitalTargets.Count) : 0;
            for (int i = 0; i < projectileCount; i++)
            {
                NPC selectedTarget = orbitalTargets.Count > 0
                    ? orbitalTargets[(i + targetOffset) % orbitalTargets.Count]
                    : target;
                Vector2 spawnPosition = selectedTarget.Center + new Vector2(
                    Main.rand.NextFloat(-Balance.OrbitalProjectileSpawnSpreadX, Balance.OrbitalProjectileSpawnSpreadX),
                    -Main.rand.NextFloat(500f, 720f));
                Vector2 aimPosition = selectedTarget.Center + new Vector2(
                    Main.rand.NextFloat(-Balance.OrbitalProjectileAimSpreadX, Balance.OrbitalProjectileAimSpreadX),
                    Main.rand.NextFloat(-Balance.OrbitalProjectileAimSpreadY, Balance.OrbitalProjectileAimSpreadY));
                Vector2 velocity = (aimPosition - spawnPosition).SafeNormalize(Vector2.UnitY) *
                    Main.rand.NextFloat(17f, 22f) *
                    Balance.OrbitalProjectileSpeedMultiplier;

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<MatrixOrbitalProjection>(),
                    Math.Max(1, (int)(Projectile.damage * Balance.OrbitalProjectileDamageMultiplier)),
                    Projectile.knockBack,
                    Projectile.owner,
                    selectedTarget.whoAmI,
                    Main.rand.NextFloat(MathHelper.TwoPi));
            }

            SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.24f, Pitch = 0.18f, MaxInstances = 5 }, target.Center);
        }

        private List<NPC> FindOrbitalTargets(NPC priorityTarget)
        {
            List<NPC> targets = new(Balance.OrbitalMaxSimultaneousTargets);
            if (priorityTarget.CanBeChasedBy(Projectile, false))
                targets.Add(priorityTarget);

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (targets.Count >= Balance.OrbitalMaxSimultaneousTargets)
                    break;

                if (npc.whoAmI == priorityTarget.whoAmI ||
                    !npc.CanBeChasedBy(Projectile, false) ||
                    Vector2.Distance(Projectile.Center, npc.Center) > Balance.TargetingRange)
                {
                    continue;
                }

                targets.Add(npc);
            }

            return targets;
        }

        private void RunFractureAttack(NPC target)
        {
            if ((attackTimer - 1) % Balance.FractureAttackInterval != 0)
                return;

            for (int i = 0; i < Balance.FractureProjectileCount; i++)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<MatrixFractureField>(),
                    Math.Max(1, (int)(Projectile.damage * Balance.FractureProjectileDamageMultiplier)),
                    Projectile.knockBack,
                    Projectile.owner,
                    target.whoAmI,
                    Main.rand.NextFloat(MathHelper.TwoPi));
            }

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.28f, Pitch = 0.28f, MaxInstances = 5 }, target.Center);
        }

        private void RunHyperdimensionalAttack(NPC target)
        {
            if ((attackTimer - 1) % Balance.HyperdimensionalAttackInterval != 0)
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

            for (int i = 0; i < Balance.HyperdimensionalProjectileCount; i++)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY),
                    beamType,
                    Math.Max(1, (int)(Projectile.damage * Balance.HyperdimensionalProjectileDamageMultiplier)),
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
            float transition = MathHelper.Clamp((Projectile.ai[0] % Balance.FormDuration) / 30f, 0f, 1f);
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
