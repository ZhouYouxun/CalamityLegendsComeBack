using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.Passive_QuickDash
{
    // A single Vortex Eye activation. It teleports immediately; the return endpoint
    // is retained by BrinyBaronVortexEyeTeleportPlayer until the cooldown ends.
    internal sealed class BrinyBaron_VortexEyeDash : ModProjectile
    {
        private Vector2 origin;
        private Vector2 destination;
        private bool initialized;

        private Player Owner => Main.player[Projectile.owner];
        private bool IsReturnTeleport => Projectile.ai[0] == 1f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(origin);
            writer.WriteVector2(destination);
            writer.Write(initialized);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            origin = reader.ReadVector2();
            destination = reader.ReadVector2();
            initialized = reader.ReadBoolean();
        }

        public override void AI()
        {
            Player owner = Owner;
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!initialized)
                Initialize(owner);

            Projectile.Center = owner.Center;
        }

        private void Initialize(Player owner)
        {
            initialized = true;
            origin = owner.Center;
            BrinyBaronVortexEyeTeleportPlayer vortexTeleport = owner.GetModPlayer<BrinyBaronVortexEyeTeleportPlayer>();

            if (IsReturnTeleport && vortexTeleport.CanReturn)
                destination = vortexTeleport.UseReturnAnchor(origin);
            else
            {
                destination = FindSafeDestination(owner, Main.MouseWorld);
                vortexTeleport.BeginCycle(origin);
            }

            Vector2 direction = (destination - origin).SafeNormalize(Vector2.UnitX * owner.direction);
            TeleportOwner(owner, destination, direction);

            if (Main.myPlayer == Projectile.owner)
            {
                BrinyBaronVortexEyeTeleportEffects.SpawnDeparture(origin, direction);
                BrinyBaronVortexEyeTeleportEffects.SpawnArrival(destination, direction);
                SpawnPathCutters(origin, destination, direction);
                SpawnTwinSlash(owner, direction);
            }

            Projectile.netUpdate = true;
        }

        private static Vector2 FindSafeDestination(Player owner, Vector2 desiredDestination)
        {
            Vector2 topLeft = desiredDestination - owner.Size * 0.5f;
            if (!Collision.SolidCollision(topLeft, owner.width, owner.height))
                return desiredDestination;

            Vector2 fallbackDirection = (desiredDestination - owner.Center).SafeNormalize(Vector2.UnitX * owner.direction);
            return owner.Center + fallbackDirection * 240f;
        }

        private static void TeleportOwner(Player owner, Vector2 target, Vector2 direction)
        {
            owner.Center = target;
            owner.velocity = Vector2.Zero;
            owner.fallStart = (int)(owner.position.Y / 16f);
            if (direction.X != 0f)
                owner.ChangeDir(System.Math.Sign(direction.X));

            SoundEngine.PlaySound(SoundID.Item6 with { Volume = 0.72f, Pitch = 0.2f }, target);
        }

        private void SpawnTwinSlash(Player owner, Vector2 direction)
        {
            // This holdout automatically chains its first slash into the second one.
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                owner.MountedCenter,
                direction,
                ModContent.ProjectileType<BrinyBaron_SkillSlashDash_SlashDash>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                0f,
                direction.X < 0f ? -1f : 1f);
        }

        private void SpawnPathCutters(Vector2 start, Vector2 end, Vector2 direction)
        {
            float distance = Vector2.Distance(start, end);
            int cutterCount = (int)MathHelper.Clamp(distance / 58f, 5f, 11f);
            int damage = System.Math.Max(1, (int)(Projectile.damage * 0.28f));

            for (int i = 0; i < cutterCount; i++)
            {
                float completion = (i + 0.25f) / cutterCount;
                Vector2 position = Vector2.Lerp(start, end, completion);
                Vector2 velocity = direction.RotatedBy(Main.rand.NextFloat(-0.09f, 0.09f)) * Main.rand.NextFloat(19f, 25f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    position,
                    velocity,
                    ModContent.ProjectileType<BrinyBaron_VortexEyePathCutter>(),
                    damage,
                    Projectile.knockBack * 0.25f,
                    Projectile.owner);
            }
        }
    }

    // The portal remains a quiet aiming marker. The actual teleport effect is much larger
    // and deliberately appears only once the player commits with right click.
    internal sealed class BrinyBaron_VortexEyePortalPreview : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 36;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.timeLeft = 2;
            Projectile.Center = Main.MouseWorld;
            if (Main.GameUpdateCount % 4 != 0)
                return;

            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(1.6f, 1.6f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f, 20f), DustID.Water, velocity, 90, new Color(80, 210, 255), Main.rand.NextFloat(0.85f, 1.25f));
                dust.noGravity = true;
            }
        }
    }
}
