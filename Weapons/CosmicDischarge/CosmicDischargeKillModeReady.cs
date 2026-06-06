using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeKillModeReady : ModProjectile, ILocalizedModType
    {
        private const int ReadyLifetime = 240;

        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float AimAngle => ref Projectile.ai[0];
        private ref float ReadyTime => ref Projectile.ai[1];
        private ref float ExtendProgress => ref Projectile.ai[2];
        private Player Owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            SoundEngine.PlaySound(SoundID.Item119 with { Pitch = -0.2f, Volume = 0.6f }, Owner.Center);

            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * Main.rand.NextFloat(3f, 6.5f);
                Dust dust = Dust.NewDustPerfect(Owner.Center, Main.rand.NextBool() ? 67 : 187, velocity, 120, CosmicDischargeCommon.FrostCoreColor, Main.rand.NextFloat(1.1f, 1.45f));
                dust.noGravity = true;
            }
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<NewLegendCosmicDischarge>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            ReadyTime++;
            Projectile.Center = Owner.MountedCenter;
            Projectile.rotation = AimAngle;

            Vector2 aimDirection = CosmicDischargeCommon.GetAimDirection(Owner, AimAngle.ToRotationVector2());
            AimAngle = aimDirection.ToRotation();
            ExtendProgress = MathHelper.Clamp(ExtendProgress + 0.1f, 0f, 1f);

            if (ReadyTime >= ReadyLifetime)
            {
                Projectile.Kill();
                return;
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 endpoint = Owner.MountedCenter + aimDirection * MathHelper.Lerp(90f, 350f, ExtendProgress);
                Dust dust = Dust.NewDustPerfect(
                    endpoint + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextBool() ? 67 : 187,
                    aimDirection.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.4f, 1.5f),
                    120,
                    CosmicDischargeCommon.FrostCoreColor,
                    Main.rand.NextFloat(1.15f, 1.55f));
                dust.noGravity = true;
            }

            if (Main.rand.NextBool(4))
            {
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(
                    Owner.Bottom + new Vector2(0f, -14f),
                    Vector2.Zero,
                    CosmicDischargeCommon.FrostCoreColor,
                    CosmicDischargeCommon.FrostGlowColor,
                    Main.rand.NextFloat(1.2f, 1.8f),
                    14,
                    Main.rand.NextFloat(-0.04f, 0.04f),
                    1.6f));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 aimDirection = AimAngle.ToRotationVector2();
            Vector2 endpoint = Owner.MountedCenter + aimDirection * MathHelper.Lerp(90f, 350f, ExtendProgress);
            Color drawColor = Color.Lerp(CosmicDischargeCommon.FrostDarkColor, CosmicDischargeCommon.FrostCoreColor, 0.68f);

            CosmicDischargeCommon.DrawChain(Main.spriteBatch, Owner.MountedCenter, endpoint, drawColor, 1f, true, Owner.gfxOffY);
            CosmicDischargeCommon.DrawRightHoldIndicator(Main.spriteBatch, Owner, 1.15f);
            return false;
        }
    }
}
