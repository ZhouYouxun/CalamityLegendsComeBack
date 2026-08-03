using System.IO;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
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
                Vector2? safeDestination = FindSafeDestination(owner, Main.MouseWorld);
                if (!safeDestination.HasValue)
                {
                    Projectile.Kill();
                    return;
                }

                destination = safeDestination.Value;
                vortexTeleport.BeginCycle(origin);
            }

            Vector2 direction = (destination - origin).SafeNormalize(Vector2.UnitX * owner.direction);
            TeleportOwner(owner, destination, direction);

            if (Main.myPlayer == Projectile.owner)
            {
                BrinyBaronVortexEyeTeleportEffects.SpawnDeparture(origin, direction);
                BrinyBaronVortexEyeTeleportEffects.SpawnArrival(destination, direction);
                SpawnForcedLeftClickDoubleSlash(owner, direction);
            }

            Projectile.netUpdate = true;
        }

        // Keep the Normality Relocator's important rules: a destination must be inside
        // the world borders and the whole player hitbox must fit without clipping tiles.
        internal static bool IsSafeDestination(Player owner, Vector2 desiredDestination)
        {
            Vector2 topLeft = desiredDestination - owner.Size * 0.5f;
            bool insideWorld = topLeft.X > 50f && topLeft.X < Main.maxTilesX * 16f - 50f &&
                               topLeft.Y > 50f && topLeft.Y < Main.maxTilesY * 16f - 50f;
            return insideWorld && !Collision.SolidCollision(topLeft, owner.width, owner.height);
        }

        private static Vector2? FindSafeDestination(Player owner, Vector2 desiredDestination)
        {
            return IsSafeDestination(owner, desiredDestination) ? desiredDestination : null;
        }

        private static void TeleportOwner(Player owner, Vector2 target, Vector2 direction)
        {
            Vector2 destinationTopLeft = target - owner.Size * 0.5f;
            owner.Teleport(destinationTopLeft, 4, 0);
            NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, owner.whoAmI, destinationTopLeft.X, destinationTopLeft.Y, 1, 0, 0);
            owner.velocity = Vector2.Zero;
            owner.fallStart = (int)(owner.position.Y / 16f);
            if (direction.X != 0f)
                owner.ChangeDir(System.Math.Sign(direction.X));

            SoundEngine.PlaySound(SoundID.Item6 with { Volume = 0.72f, Pitch = 0.2f }, target);
        }

        private void SpawnForcedLeftClickDoubleSlash(Player owner, Vector2 direction)
        {
            // ai[0] tells the normal left-click holdout to play exactly two of its own
            // stages without requiring held input. This deliberately does not reuse the
            // passive slash-dash holdout, whose hit feel and follow-up effects differ.
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                owner.MountedCenter,
                direction,
                ModContent.ProjectileType<BrinyBaron_LeftClick_Swing>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                1f);
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
