using CalamityMod.Systems.Collections;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.ProjectilePossessionModule
{
    internal sealed class SHPCProjectilePossessionGlobalProjectile : GlobalProjectile
    {
        private const int MaxAllowedTimeLeft = 3600;
        private const int MaxAllowedDamage = 3600;
        private const float AbsorbedSourceMultiplier = 0.8f;
        private static readonly HashSet<int> BlacklistedProjectileTypes = new()
        {
            ProjectileID.SaucerDeathray,
            ProjectileID.PhantasmalDeathray
        };

        private bool originalHostile;
        private bool originalFriendly;
        private bool originalTileCollide;
        private bool originalIgnoreWater;
        private int originalPenetrate;
        private int originalMaxPenetrate;
        private float orbitOffset;
        private int empoweredDamage;
        private int weakenedDamage;
        private float weakenedMaxSpeed;

        public override bool InstancePerEntity => true;

        public bool PossessedBySHPC { get; private set; }
        public bool ReleasedBySHPC { get; private set; }
        public bool WeakenedBySHPC { get; private set; }
        public int PossessionOwner { get; private set; } = -1;
        public int PossessionSlot { get; private set; }
        public int OriginalDamage { get; private set; }
        public float OriginalKnockBack { get; private set; }
        public float OriginalSpeed { get; private set; }

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            ResetPossessionState();

            if (TryGetReleasedParent(source, out Projectile parentProjectile))
                InheritFriendlyRelease(projectile, parentProjectile);
        }

        public static bool CanBePossessed(Projectile projectile)
        {
            if (!projectile.active ||
                !projectile.hostile ||
                projectile.friendly ||
                projectile.damage <= 0 ||
                projectile.damage > MaxAllowedDamage ||
                projectile.timeLeft <= 0 ||
                projectile.timeLeft > MaxAllowedTimeLeft ||
                projectile.trap ||
                projectile.type <= ProjectileID.None ||
                projectile.width <= 2 ||
                projectile.height <= 2)
            {
                return false;
            }

            SHPCProjectilePossessionGlobalProjectile possession = projectile.GetGlobalProjectile<SHPCProjectilePossessionGlobalProjectile>();
            if (possession.PossessedBySHPC ||
                possession.ReleasedBySHPC ||
                possession.WeakenedBySHPC)
                return false;

            if (BlacklistedProjectileTypes.Contains(projectile.type))
                return false;

            if (projectile.type < CalamityProjectileSets.ShouldNotBeReflected.Length &&
                CalamityProjectileSets.ShouldNotBeReflected[projectile.type])
                return false;

            return true;
        }

        public static bool TryCreatePossessedClone(Projectile sourceProjectile, Player owner, int slot, out Projectile possessedClone)
        {
            possessedClone = null;
            if (!CanBePossessed(sourceProjectile))
                return false;

            int originalDamage = Utils.Clamp(sourceProjectile.damage, 1, MaxAllowedDamage);
            float originalKnockBack = sourceProjectile.knockBack;
            float originalSpeed = sourceProjectile.velocity.Length();
            bool sourceTileCollide = sourceProjectile.tileCollide;
            bool sourceIgnoreWater = sourceProjectile.ignoreWater;
            int sourcePenetrate = sourceProjectile.penetrate;
            int sourceMaxPenetrate = sourceProjectile.maxPenetrate;
            int cloneDamage = GetEmpoweredDamage(originalDamage);

            Projectile clone = Projectile.NewProjectileDirect(
                sourceProjectile.GetSource_FromThis(),
                sourceProjectile.Center,
                Vector2.Zero,
                sourceProjectile.type,
                cloneDamage,
                originalKnockBack,
                owner.whoAmI,
                sourceProjectile.ai[0],
                sourceProjectile.ai[1],
                sourceProjectile.ai[2]);

            if (clone is null || !clone.active)
                return false;

            CopyCloneState(sourceProjectile, clone, cloneDamage);

            SHPCProjectilePossessionGlobalProjectile clonePossession = clone.GetGlobalProjectile<SHPCProjectilePossessionGlobalProjectile>();
            clonePossession.CaptureClone(
                clone,
                owner,
                slot,
                originalDamage,
                originalKnockBack,
                originalSpeed,
                sourceTileCollide,
                sourceIgnoreWater,
                sourcePenetrate,
                sourceMaxPenetrate);

            sourceProjectile.GetGlobalProjectile<SHPCProjectilePossessionGlobalProjectile>().WeakenSourceProjectile(sourceProjectile);
            possessedClone = clone;
            return true;
        }

        public static int CountPossessedProjectiles(int owner)
        {
            int count = 0;
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.GetGlobalProjectile<SHPCProjectilePossessionGlobalProjectile>().IsPossessedBy(owner))
                    count++;
            }

            return count;
        }

        public static List<Projectile> GetPossessedProjectiles(int owner)
        {
            List<Projectile> projectiles = new();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.GetGlobalProjectile<SHPCProjectilePossessionGlobalProjectile>().IsPossessedBy(owner))
                    projectiles.Add(projectile);
            }

            projectiles.Sort((a, b) =>
                a.GetGlobalProjectile<SHPCProjectilePossessionGlobalProjectile>().PossessionSlot.CompareTo(
                    b.GetGlobalProjectile<SHPCProjectilePossessionGlobalProjectile>().PossessionSlot));

            return projectiles;
        }

        public static void ReleaseAllForOwner(Player owner, Vector2 direction, float speed, int fallbackDamage)
        {
            direction = direction.SafeNormalize(Vector2.UnitX * owner.direction);
            List<Projectile> projectiles = GetPossessedProjectiles(owner.whoAmI);
            int count = projectiles.Count;
            if (count <= 0)
                return;

            float spread = MathHelper.Lerp(MathHelper.ToRadians(2f), MathHelper.ToRadians(24f), count / (float)ProjectilePossessionModulePlayer.MaxAbsorbedProjectiles);
            for (int i = 0; i < count; i++)
            {
                Projectile projectile = projectiles[i];
                SHPCProjectilePossessionGlobalProjectile possession = projectile.GetGlobalProjectile<SHPCProjectilePossessionGlobalProjectile>();
                float progress = count <= 1 ? 0.5f : i / (float)(count - 1);
                float releaseSpeed = MathHelper.Clamp(System.MathF.Max(speed, possession.OriginalSpeed), 10f, 30f);
                Vector2 velocity = direction.RotatedBy(MathHelper.Lerp(-spread, spread, progress)) * releaseSpeed;
                possession.Release(projectile, owner, velocity, fallbackDamage);
            }
        }

        private void CaptureClone(
            Projectile projectile,
            Player owner,
            int slot,
            int sourceDamage,
            float sourceKnockBack,
            float sourceSpeed,
            bool sourceTileCollide,
            bool sourceIgnoreWater,
            int sourcePenetrate,
            int sourceMaxPenetrate)
        {
            PossessedBySHPC = true;
            ReleasedBySHPC = false;
            WeakenedBySHPC = false;
            PossessionOwner = owner.whoAmI;
            PossessionSlot = slot;
            OriginalDamage = sourceDamage;
            OriginalKnockBack = sourceKnockBack;
            OriginalSpeed = sourceSpeed;
            if (OriginalSpeed < 4f)
                OriginalSpeed = 12f;
            empoweredDamage = GetEmpoweredDamage(OriginalDamage);

            originalHostile = projectile.hostile;
            originalFriendly = projectile.friendly;
            originalTileCollide = sourceTileCollide;
            originalIgnoreWater = sourceIgnoreWater;
            originalPenetrate = sourcePenetrate;
            originalMaxPenetrate = sourceMaxPenetrate;
            orbitOffset = Main.rand.NextFloat(MathHelper.TwoPi);

            projectile.hostile = false;
            projectile.friendly = false;
            projectile.owner = owner.whoAmI;
            projectile.damage = empoweredDamage;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.velocity = Vector2.Zero;
            EnsureMagicDamageTypeIfDefault(projectile);
            projectile.netUpdate = true;
        }

        public void Release(Projectile projectile, Player owner, Vector2 velocity, int fallbackDamage)
        {
            if (!PossessedBySHPC)
                return;

            PossessedBySHPC = false;
            ReleasedBySHPC = true;
            PossessionOwner = owner.whoAmI;

            projectile.hostile = false;
            projectile.friendly = true;
            projectile.owner = owner.whoAmI;
            projectile.velocity = velocity;
            projectile.damage = empoweredDamage > 0
                ? empoweredDamage
                : GetEmpoweredDamage(System.Math.Max(OriginalDamage, fallbackDamage));
            projectile.knockBack = OriginalKnockBack;
            projectile.tileCollide = originalTileCollide;
            projectile.ignoreWater = originalIgnoreWater;
            projectile.timeLeft = Utils.Clamp(projectile.timeLeft, 90, 600);
            EnsureMagicDamageTypeIfDefault(projectile);

            if (projectile.penetrate == 0)
                projectile.penetrate = originalPenetrate != 0 ? originalPenetrate : 1;
            if (projectile.maxPenetrate == 0)
                projectile.maxPenetrate = originalMaxPenetrate != 0 ? originalMaxPenetrate : 1;

            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 10;
            projectile.netUpdate = true;
        }

        public override void PostAI(Projectile projectile)
        {
            if (ReleasedBySHPC)
            {
                EnforceReleasedProjectile(projectile);
                return;
            }

            if (WeakenedBySHPC)
            {
                EnforceWeakenedProjectile(projectile);
                return;
            }

            if (!PossessedBySHPC)
                return;

            if (!Main.player.IndexInRange(PossessionOwner))
            {
                projectile.Kill();
                return;
            }

            Player owner = Main.player[PossessionOwner];
            if (!owner.active || owner.dead)
            {
                projectile.Kill();
                return;
            }

            float time = Main.GlobalTimeWrappedHourly * 3.4f + orbitOffset;
            int ring = PossessionSlot / 5;
            int indexInRing = PossessionSlot % 5;
            float angle = time + MathHelper.TwoPi / 5f * indexInRing + ring * 0.41f;
            float radius = 36f + ring * 16f;
            Vector2 targetCenter = owner.Center + new Vector2(0f, -24f) + Vector2.UnitX.RotatedBy(angle) * radius;

            projectile.Center = Vector2.Lerp(projectile.Center, targetCenter, 0.38f);
            projectile.velocity = owner.velocity;
            projectile.hostile = false;
            projectile.friendly = false;
            if (empoweredDamage > 0)
                projectile.damage = empoweredDamage;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = System.Math.Max(projectile.timeLeft, 4);
        }

        public override bool CanHitPlayer(Projectile projectile, Player target)
        {
            if (PossessedBySHPC || ReleasedBySHPC)
                return false;

            return true;
        }

        public override bool? CanHitNPC(Projectile projectile, NPC target)
        {
            if (PossessedBySHPC)
                return false;

            return null;
        }

        public override void AI(Projectile projectile)
        {
            if (!ReleasedBySHPC)
                return;

            EnforceReleasedProjectile(projectile);
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(PossessedBySHPC);
            bitWriter.WriteBit(ReleasedBySHPC);
            bitWriter.WriteBit(WeakenedBySHPC);
            if (!PossessedBySHPC && !ReleasedBySHPC && !WeakenedBySHPC)
                return;

            binaryWriter.Write(PossessionOwner);
            binaryWriter.Write(PossessionSlot);
            binaryWriter.Write(OriginalDamage);
            binaryWriter.Write(empoweredDamage);
            binaryWriter.Write(OriginalKnockBack);
            binaryWriter.Write(OriginalSpeed);
            binaryWriter.Write(weakenedDamage);
            binaryWriter.Write(weakenedMaxSpeed);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            PossessedBySHPC = bitReader.ReadBit();
            ReleasedBySHPC = bitReader.ReadBit();
            WeakenedBySHPC = bitReader.ReadBit();
            if (!PossessedBySHPC && !ReleasedBySHPC && !WeakenedBySHPC)
                return;

            PossessionOwner = binaryReader.ReadInt32();
            PossessionSlot = binaryReader.ReadInt32();
            OriginalDamage = binaryReader.ReadInt32();
            empoweredDamage = binaryReader.ReadInt32();
            OriginalKnockBack = binaryReader.ReadSingle();
            OriginalSpeed = binaryReader.ReadSingle();
            weakenedDamage = binaryReader.ReadInt32();
            weakenedMaxSpeed = binaryReader.ReadSingle();
        }

        private bool IsPossessedBy(int owner)
        {
            return PossessedBySHPC && PossessionOwner == owner;
        }

        private void ResetPossessionState()
        {
            PossessedBySHPC = false;
            ReleasedBySHPC = false;
            WeakenedBySHPC = false;
            PossessionOwner = -1;
            PossessionSlot = 0;
            OriginalDamage = 0;
            OriginalKnockBack = 0f;
            OriginalSpeed = 0f;
            empoweredDamage = 0;
            weakenedDamage = 0;
            weakenedMaxSpeed = 0f;
            originalHostile = false;
            originalFriendly = false;
            originalTileCollide = false;
            originalIgnoreWater = false;
            originalPenetrate = 0;
            originalMaxPenetrate = 0;
            orbitOffset = 0f;
        }

        private static void CopyCloneState(Projectile source, Projectile clone, int cloneDamage)
        {
            clone.Center = source.Center;
            clone.rotation = source.rotation;
            clone.scale = source.scale;
            clone.alpha = source.alpha;
            clone.spriteDirection = source.spriteDirection;
            clone.direction = source.direction;
            clone.frame = source.frame;
            clone.frameCounter = source.frameCounter;
            clone.timeLeft = source.timeLeft;
            clone.penetrate = source.penetrate;
            clone.maxPenetrate = source.maxPenetrate;
            clone.DamageType = source.DamageType;
            clone.damage = cloneDamage;

            for (int i = 0; i < clone.ai.Length && i < source.ai.Length; i++)
                clone.ai[i] = source.ai[i];
            for (int i = 0; i < clone.localAI.Length && i < source.localAI.Length; i++)
                clone.localAI[i] = source.localAI[i];
        }

        private void WeakenSourceProjectile(Projectile projectile)
        {
            if (WeakenedBySHPC)
                return;

            WeakenedBySHPC = true;
            weakenedDamage = System.Math.Max(1, (int)System.MathF.Round(projectile.damage * AbsorbedSourceMultiplier));
            weakenedMaxSpeed = projectile.velocity.Length() * AbsorbedSourceMultiplier;
            projectile.velocity *= AbsorbedSourceMultiplier;
            projectile.damage = weakenedDamage;
            projectile.netUpdate = true;
        }

        private static int GetEmpoweredDamage(int baseDamage)
        {
            return System.Math.Max(1, baseDamage);
        }

        private static void EnsureMagicDamageTypeIfDefault(Projectile projectile)
        {
            if (projectile.DamageType == null || projectile.DamageType == DamageClass.Default)
                projectile.DamageType = DamageClass.Magic;
        }

        private static bool TryGetReleasedParent(IEntitySource source, out Projectile parentProjectile)
        {
            if (source is EntitySource_Parent { Entity: Projectile projectile })
            {
                SHPCProjectilePossessionGlobalProjectile parentPossession = projectile.GetGlobalProjectile<SHPCProjectilePossessionGlobalProjectile>();
                if (parentPossession.ReleasedBySHPC)
                {
                    parentProjectile = projectile;
                    return true;
                }
            }

            parentProjectile = null;
            return false;
        }

        private void InheritFriendlyRelease(Projectile projectile, Projectile parentProjectile)
        {
            SHPCProjectilePossessionGlobalProjectile parentPossession = parentProjectile.GetGlobalProjectile<SHPCProjectilePossessionGlobalProjectile>();
            PossessedBySHPC = false;
            ReleasedBySHPC = true;
            WeakenedBySHPC = false;
            PossessionOwner = parentPossession.PossessionOwner >= 0 ? parentPossession.PossessionOwner : parentProjectile.owner;
            PossessionSlot = parentPossession.PossessionSlot;
            OriginalDamage = projectile.damage;
            OriginalKnockBack = projectile.knockBack;
            OriginalSpeed = projectile.velocity.Length();
            empoweredDamage = projectile.damage;

            projectile.owner = PossessionOwner;
            projectile.hostile = false;
            projectile.friendly = true;
            EnsureMagicDamageTypeIfDefault(projectile);
            projectile.netUpdate = true;
        }

        private void EnforceReleasedProjectile(Projectile projectile)
        {
            if (!projectile.friendly)
                projectile.friendly = true;

            projectile.hostile = false;
            if (empoweredDamage > 0)
                projectile.damage = empoweredDamage;

            EnsureMagicDamageTypeIfDefault(projectile);
        }

        private void EnforceWeakenedProjectile(Projectile projectile)
        {
            if (weakenedDamage > 0 && projectile.damage > weakenedDamage)
                projectile.damage = weakenedDamage;

            if (weakenedMaxSpeed <= 0f)
                return;

            float speedSquared = projectile.velocity.LengthSquared();
            float maxSpeedSquared = weakenedMaxSpeed * weakenedMaxSpeed;
            if (speedSquared > maxSpeedSquared)
                projectile.velocity = projectile.velocity.SafeNormalize(Vector2.Zero) * weakenedMaxSpeed;
        }
    }
}
