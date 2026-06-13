using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    internal class FragmentNebula_BurstRelay : ModProjectile, ILocalizedModType
    {
        private const float MinMuzzleDistance = 42f;
        private const float MaxMuzzleDistance = 92f;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];

        private Vector2 ForwardDirection
        {
            get
            {
                Vector2 storedDirection = new(Projectile.ai[0], Projectile.ai[1]);
                if (storedDirection.LengthSquared() > 0.0001f)
                    return storedDirection.SafeNormalize(Vector2.UnitX);

                return Projectile.velocity.SafeNormalize(Vector2.UnitX);
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.timeLeft = 22;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Player owner = Main.player[Projectile.owner];
            Vector2 forward = GetOwnerAimDirection(owner, ForwardDirection);

            Projectile.ai[2] = MathHelper.Clamp(Vector2.Distance(owner.Center, Projectile.Center), MinMuzzleDistance, MaxMuzzleDistance);
            SetForwardDirection(forward);
            Projectile.Center = GetAnchoredMuzzlePosition(owner, forward);
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = forward.ToRotation();
            Projectile.netUpdate = true;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Vector2 forward = GetOwnerAimDirection(owner, ForwardDirection);
            SetForwardDirection(forward);
            Projectile.Center = GetAnchoredMuzzlePosition(owner, forward);
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = forward.ToRotation();

            if (Projectile.owner == Main.myPlayer && ((int)Timer == 0 || (int)Timer == 5 || (int)Timer == 10))
                FireBurstShot((int)(Timer / 5f));

            SpawnRelayDust(forward);
            Timer++;
        }

        private void SetForwardDirection(Vector2 direction)
        {
            Vector2 safeDirection = direction.SafeNormalize(Vector2.UnitX);
            Projectile.ai[0] = safeDirection.X;
            Projectile.ai[1] = safeDirection.Y;
        }

        private Vector2 GetAnchoredMuzzlePosition(Player owner, Vector2 forward)
        {
            float muzzleDistance = Projectile.ai[2];
            if (muzzleDistance <= 0f)
                muzzleDistance = 68f;

            return owner.Center + forward * MathHelper.Clamp(muzzleDistance, MinMuzzleDistance, MaxMuzzleDistance);
        }

        private static Vector2 GetOwnerAimDirection(Player owner, Vector2 fallback)
        {
            Vector2 mouseWorld = owner.whoAmI == Main.myPlayer && !Main.dedServ ? Main.MouseWorld : owner.Calamity().mouseWorld;
            return (mouseWorld - owner.Center).SafeNormalize(fallback.SafeNormalize(Vector2.UnitX * owner.direction));
        }

        private void FireBurstShot(int shotIndex)
        {
            Vector2 forward = new Vector2(Projectile.ai[0], Projectile.ai[1]).SafeNormalize(Vector2.UnitX);
            float angleOffset = MathHelper.ToRadians((shotIndex - 1) * 5f);
            bool isBlueVariant = shotIndex == 2;
            Vector2 direction = forward.RotatedBy(angleOffset).SafeNormalize(forward);
            float speed = isBlueVariant ? 17.8f : 17.2f;
            int damage = isBlueVariant ? Projectile.damage * 3 : Projectile.damage;
            float homingDelayFrames = 12f;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + forward * 8f,
                direction * speed,
                ModContent.ProjectileType<FragmentNebula_Star>(),
                Math.Max(1, damage),
                Projectile.knockBack,
                Projectile.owner,
                isBlueVariant ? 1f : 0f,
                homingDelayFrames,
                shotIndex);

            SoundEngine.PlaySound(SoundID.Item75, Projectile.Center);
        }

        private void SpawnRelayDust(Vector2 forward)
        {
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + normal * Main.rand.NextFloat(-8f, 8f),
                    Main.rand.NextBool() ? DustID.PurpleCrystalShard : DustID.BlueTorch,
                    -forward * Main.rand.NextFloat(0.6f, 1.8f) + normal * Main.rand.NextFloat(-0.4f, 0.4f),
                    0,
                    Main.rand.NextBool() ? new Color(185, 72, 255) : new Color(72, 196, 255),
                    Main.rand.NextFloat(0.8f, 1.25f));

                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
