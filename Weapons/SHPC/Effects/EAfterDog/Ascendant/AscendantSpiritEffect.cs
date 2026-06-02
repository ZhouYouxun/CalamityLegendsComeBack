using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ascendant
{
    internal class AscendantSpiritEffect : DefaultEffect
    {
        public override int EffectID => 36;

        public override int AmmoType => ModContent.ItemType<AscendantSpiritEssence>();

        public override Color ThemeColor => new Color(120, 160, 255);
        public override Color StartColor => new Color(200, 220, 255);
        public override Color EndColor => new Color(40, 60, 120);
        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override bool EnableDefaultSlowdown => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.GetGlobalProjectile<AscendantSpiritEffectGlobalProjectile>().firstFrame = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 2;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            AscendantSpiritEffectGlobalProjectile globalProjectile = projectile.GetGlobalProjectile<AscendantSpiritEffectGlobalProjectile>();
            if (!globalProjectile.firstFrame)
                return;

            globalProjectile.firstFrame = false;
            projectile.Kill();
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (owner.whoAmI != Main.myPlayer)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(owner.direction == 0 ? Vector2.UnitX : new Vector2(owner.direction, 0f));
            if (forward == Vector2.Zero)
                forward = Vector2.UnitX;

            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center + forward * 12f,
                forward,
                ModContent.ProjectileType<AscendantSpirit_BurstRelay>(),
                (int)(projectile.damage * 1.5f),
                projectile.knockBack,
                owner.whoAmI,
                forward.X,
                forward.Y);
        }

        internal static void SpawnNeedleReleaseParticles(Vector2 spawnPosition, Vector2 launchDirection, Color color, bool widerArc)
        {
            Vector2 normal = launchDirection.RotatedBy(MathHelper.PiOver2);
            int dustCount = widerArc ? 8 : 6;

            for (int i = 0; i < dustCount; i++)
            {
                Dust dust = Dust.NewDustPerfect(spawnPosition, ModContent.DustType<SquashDust>());
                dust.scale = Main.rand.NextFloat(0.95f, 1.55f);
                dust.velocity = -launchDirection.RotatedByRandom(0.36f) * Main.rand.NextFloat(2.2f, 4.4f);
                dust.noGravity = true;
                dust.color = Color.Lerp(color, Color.White, Main.rand.NextFloat(0.08f, 0.3f));
                dust.fadeIn = Main.rand.NextFloat(1.1f, 2.2f);
            }

            Particle releaseBloom = new CustomSpark(
                spawnPosition,
                Vector2.Zero,
                "CalamityMod/Particles/BloomCircle",
                false,
                widerArc ? 20 : 16,
                widerArc ? 0.34f : 0.26f,
                color,
                new Vector2(0.72f, 1.22f),
                glowCenter: true,
                shrinkSpeed: 0.18f,
                glowOpacity: 0.72f,
                extraRotation: launchDirection.ToRotation());
            GeneralParticleHandler.SpawnParticle(releaseBloom);

            for (int i = 0; i < 4; i++)
            {
                Particle star = new CustomSpark(
                    spawnPosition + normal * Main.rand.NextFloat(-5f, 5f),
                    -launchDirection.RotatedByRandom(0.28f) * Main.rand.NextFloat(1.6f, 3.4f),
                    "CalamityMod/Particles/PulseStar",
                    false,
                    Main.rand.Next(13, 21),
                    Main.rand.NextFloat(0.08f, 0.16f),
                    Color.Lerp(color, Color.White, 0.2f),
                    Vector2.One,
                    glowCenter: true,
                    shrinkSpeed: 0.22f,
                    glowOpacity: 0.68f);
                GeneralParticleHandler.SpawnParticle(star);
            }

        }

        internal static void SpawnCentralReleaseParticles(Vector2 center, Vector2 forward)
        {
            float rotation = Main.rand.NextFloat(MathHelper.TwoPi);

            for (int ring = 1; ring <= 2; ring++)
            {
                for (int i = 0; i < 5; i++)
                {
                    Color color = AscendantSpirit_PROJ.RandomThemeColor();
                    Dust dust = Dust.NewDustPerfect(center, ModContent.DustType<SquashDust>());
                    dust.scale = 4.8f - ring * 0.5f;
                    dust.velocity = -Vector2.UnitY.RotatedBy(MathHelper.TwoPi / 5f * i + rotation) * (ring * 1.35f + 1.5f);
                    dust.noGravity = true;
                    dust.color = color;
                    dust.fadeIn = 5.2f - ring * 0.36f;
                }
            }

            for (int i = 0; i < 6; i++)
            {
                Color color = AscendantSpirit_PROJ.RandomThemeColor();
                Particle sparkle = new CustomSpark(
                    center,
                    -forward.RotatedByRandom(0.55f) * Main.rand.NextFloat(1.4f, 3.6f),
                    "CalamityMod/Particles/PulseStar",
                    false,
                    Main.rand.Next(14, 23),
                    Main.rand.NextFloat(0.08f, 0.17f),
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.12f, 0.35f)),
                    Vector2.One,
                    glowCenter: true,
                    shrinkSpeed: 0.24f,
                    glowOpacity: 0.7f);
                GeneralParticleHandler.SpawnParticle(sparkle);
            }

            Particle bloom = new CustomSpark(
                center,
                Vector2.Zero,
                "CalamityMod/Particles/BloomCircle",
                false,
                24,
                0.48f,
                Color.Lerp(new Color(120, 160, 255), Color.White, 0.22f),
                Vector2.One,
                true,
                true,
                0,
                false,
                false,
                glowOpacity: 0.82f);
            GeneralParticleHandler.SpawnParticle(bloom);
        }
    }

    internal class AscendantSpiritEffectGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool firstFrame;
    }

    internal sealed class AscendantSpirit_BurstRelay : ModProjectile, ILocalizedModType
    {
        private const int SpiritCount = 9;
        private const int WarmupFrames = 3;
        private const int FireInterval = 3;
        private const float ReleaseRadius = 48f;
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

        private void FireSpirit(int shotIndex)
        {
            Player owner = Main.player[Projectile.owner];
            Vector2 forward = ForwardDirection;
            float lane = shotIndex - (SpiritCount - 1f) * 0.5f;
            Vector2 spawnPosition = Projectile.Center + Main.rand.NextVector2Circular(ReleaseRadius, ReleaseRadius);
            Vector2 targetPoint = GetOwnerMouseWorld(owner);
            Vector2 direction = (targetPoint - spawnPosition).SafeNormalize(forward);
            Color themeColor = AscendantSpirit_PROJ.RandomThemeColor();
            float launchDelay = 2f + Main.rand.NextFloat(0f, 0.75f);

            int projectileIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                direction * AscendantSpirit_PROJ.DefaultLaunchSpeed,
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
                SoundEngine.PlaySound(SoundID.Item68 with { Volume = 0.18f, Pitch = 0.28f + shotIndex * 0.015f, PitchVariance = 0.08f, MaxInstances = 6 }, spawnPosition);
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
