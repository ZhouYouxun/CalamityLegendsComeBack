using CalamityMod.Systems.Collections;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.ProjectilePossessionModule
{
    internal sealed class SHPCProjectilePossessionGlobalProjectile : GlobalProjectile
    {
        private const int MaxAllowedTimeLeft = 3600;
        private const int MaxAllowedDamage = 3600;
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

        public override bool InstancePerEntity => true;

        public bool PossessedBySHPC { get; private set; }
        public bool ReleasedBySHPC { get; private set; }
        public int PossessionOwner { get; private set; } = -1;
        public int PossessionSlot { get; private set; }
        public int OriginalDamage { get; private set; }
        public float OriginalKnockBack { get; private set; }
        public float OriginalSpeed { get; private set; }

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            ResetPossessionState();
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

            if (projectile.GetGlobalProjectile<SHPCProjectilePossessionGlobalProjectile>().PossessedBySHPC ||
                projectile.GetGlobalProjectile<SHPCProjectilePossessionGlobalProjectile>().ReleasedBySHPC)
                return false;

            if (BlacklistedProjectileTypes.Contains(projectile.type))
                return false;

            if (projectile.type < CalamityProjectileSets.ShouldNotBeReflected.Length &&
                CalamityProjectileSets.ShouldNotBeReflected[projectile.type])
                return false;

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

        public void Capture(Projectile projectile, Player owner, int slot, Vector2 anchor)
        {
            PossessedBySHPC = true;
            ReleasedBySHPC = false;
            PossessionOwner = owner.whoAmI;
            PossessionSlot = slot;
            OriginalDamage = Utils.Clamp(projectile.damage, 1, MaxAllowedDamage);
            OriginalKnockBack = projectile.knockBack;
            OriginalSpeed = projectile.velocity.Length();
            if (OriginalSpeed < 4f)
                OriginalSpeed = 12f;

            originalHostile = projectile.hostile;
            originalFriendly = projectile.friendly;
            originalTileCollide = projectile.tileCollide;
            originalIgnoreWater = projectile.ignoreWater;
            originalPenetrate = projectile.penetrate;
            originalMaxPenetrate = projectile.maxPenetrate;
            orbitOffset = Main.rand.NextFloat(MathHelper.TwoPi);

            projectile.hostile = false;
            projectile.friendly = false;
            projectile.owner = owner.whoAmI;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.Center = anchor;
            projectile.velocity = Vector2.Zero;
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
            projectile.damage = Utils.Clamp(System.Math.Max(OriginalDamage, fallbackDamage), 1, MaxAllowedDamage);
            projectile.knockBack = OriginalKnockBack;
            projectile.tileCollide = originalTileCollide;
            projectile.ignoreWater = originalIgnoreWater;
            projectile.timeLeft = Utils.Clamp(projectile.timeLeft, 90, 600);

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

            if (!projectile.friendly)
                projectile.friendly = true;
            projectile.hostile = false;
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(PossessedBySHPC);
            bitWriter.WriteBit(ReleasedBySHPC);
            if (!PossessedBySHPC && !ReleasedBySHPC)
                return;

            binaryWriter.Write(PossessionOwner);
            binaryWriter.Write(PossessionSlot);
            binaryWriter.Write(OriginalDamage);
            binaryWriter.Write(OriginalKnockBack);
            binaryWriter.Write(OriginalSpeed);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            PossessedBySHPC = bitReader.ReadBit();
            ReleasedBySHPC = bitReader.ReadBit();
            if (!PossessedBySHPC && !ReleasedBySHPC)
                return;

            PossessionOwner = binaryReader.ReadInt32();
            PossessionSlot = binaryReader.ReadInt32();
            OriginalDamage = binaryReader.ReadInt32();
            OriginalKnockBack = binaryReader.ReadSingle();
            OriginalSpeed = binaryReader.ReadSingle();
        }

        private bool IsPossessedBy(int owner)
        {
            return PossessedBySHPC && PossessionOwner == owner;
        }

        private void ResetPossessionState()
        {
            PossessedBySHPC = false;
            ReleasedBySHPC = false;
            PossessionOwner = -1;
            PossessionSlot = 0;
            OriginalDamage = 0;
            OriginalKnockBack = 0f;
            OriginalSpeed = 0f;
            originalHostile = false;
            originalFriendly = false;
            originalTileCollide = false;
            originalIgnoreWater = false;
            originalPenetrate = 0;
            originalMaxPenetrate = 0;
            orbitOffset = 0f;
        }
    }
}
