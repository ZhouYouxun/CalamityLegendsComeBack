using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ascendant
{
    internal sealed class AscendantSpirit_BurstRelay : ModProjectile, ILocalizedModType
    {
        private const int SpiritCount = 9;
        private const int WarmupFrames = 3;
        private const int FireInterval = 3;
        private const float ReleaseRadius = 0f;
        private const float MinMuzzleDistance = 42f;
        private const float MaxMuzzleDistance = 92f;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float ShotsFired => ref Projectile.localAI[1];

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
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.timeLeft = WarmupFrames + SpiritCount * FireInterval + 24;
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

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.36f, Pitch = 0.18f, PitchVariance = 0.08f, MaxInstances = 4 }, Projectile.Center);
                AscendantSpiritEffect.SpawnCentralReleaseParticles(Projectile.Center, forward);
            }
        }

        public override void AI()
        {
            Timer++;

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

            if (Projectile.owner == Main.myPlayer && Timer >= WarmupFrames && ShotsFired < SpiritCount && (Timer - WarmupFrames) % FireInterval == 0f)
                FireSpirit((int)ShotsFired++);

            Lighting.AddLight(Projectile.Center, new Vector3(0.45f, 0.58f, 1f) * 0.42f);
        }
        // 小弹幕初始速度倍率：0.7f = 降低 30%
        private const float SpiritLaunchSpeedMultiplier = 0.7f;
        private void FireSpirit(int shotIndex)
        {
            Player owner = Main.player[Projectile.owner];
            Vector2 forward = ForwardDirection;
            float lane = shotIndex - (SpiritCount - 1f) * 0.5f;
            Vector2 spawnPosition = Projectile.Center + Main.rand.NextVector2Circular(ReleaseRadius, ReleaseRadius);
            Vector2 targetPoint = GetOwnerMouseWorld(owner);
            Vector2 direction = (targetPoint - spawnPosition).SafeNormalize(forward).RotatedByRandom(MathHelper.ToRadians(5f));
            Color themeColor = AscendantSpirit_PROJ.RandomThemeColor();
            float launchDelay = 2f + Main.rand.NextFloat(0f, 0.75f);

            int projectileIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                direction * AscendantSpirit_PROJ.DefaultLaunchSpeed * SpiritLaunchSpeedMultiplier,
                ModContent.ProjectileType<AscendantSpirit_PROJ>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                targetPoint.X,
                targetPoint.Y,
                launchDelay);

            if (Main.projectile.IndexInRange(projectileIndex) && Main.projectile[projectileIndex].ModProjectile is AscendantSpirit_PROJ spiritProjectile)
            {
                spiritProjectile.InitializeNeedle(targetPoint, themeColor, launchDelay);
                Main.projectile[projectileIndex].netUpdate = true;
            }

            if (!Main.dedServ)
            {
                //SoundEngine.PlaySound(new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/AWM开火"), spawnPosition);
                AscendantSpiritEffect.SpawnNeedleReleaseParticles(spawnPosition, direction, themeColor, Math.Abs(lane) > 2f);
            }
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
            Vector2 mouseWorld = GetOwnerMouseWorld(owner);
            return (mouseWorld - owner.Center).SafeNormalize(fallback.SafeNormalize(Vector2.UnitX * owner.direction));
        }

        private static Vector2 GetOwnerMouseWorld(Player owner)
        {
            return owner.whoAmI == Main.myPlayer && !Main.dedServ ? Main.MouseWorld : owner.Calamity().mouseWorld;
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
