using CalamityMod;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.AshesofCala
{
    internal sealed class AshesofCalamity_SoulRelay : ModProjectile, ILocalizedModType
    {
        private const int ShotCount = 9;
        private const int WarmupFrames = 4;
        private const int FireInterval = 4;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float ShotsFired => ref Projectile.localAI[1];

        private Vector2 ForwardDirection
        {
            get
            {
                Vector2 stored = new(Projectile.ai[0], Projectile.ai[1]);
                if (stored.LengthSquared() > 0.001f)
                    return stored.SafeNormalize(Vector2.UnitX);

                return Projectile.velocity.SafeNormalize(Vector2.UnitX);
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.timeLeft = WarmupFrames + ShotCount * FireInterval + 18;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(IEntitySource source)
        {
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.34f, Pitch = -0.18f, PitchVariance = 0.1f, MaxInstances = 4 }, Projectile.Center);
            if (!Main.dedServ)
                SpawnMuzzleFlash(ForwardDirection);
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
            Projectile.ai[0] = forward.X;
            Projectile.ai[1] = forward.Y;
            Projectile.Center = owner.Center + forward * 68f;
            Projectile.rotation = forward.ToRotation();

            if (Projectile.owner == Main.myPlayer && Timer >= WarmupFrames && ShotsFired < ShotCount && (Timer - WarmupFrames) % FireInterval == 0f)
                FireSoul((int)ShotsFired++);

            if (!Main.dedServ)
                SpawnChargeEffects(forward);
        }

        private void FireSoul(int shotIndex)
        {
            Vector2 forward = ForwardDirection;
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            bool piercingShot = shotIndex % 3 == 2;

            // 保留“正前方附近”的扇形偏移。
            // 第一发偏移最大，后面的偏移逐渐收束，让弹幕看起来像往前方喷射，而不是锁定敌人。
            float spreadStrength = MathHelper.Lerp(0.26f, 0.06f, shotIndex / (float)(ShotCount - 1));
            float wave = (float)Math.Sin(shotIndex * 1.83f) * spreadStrength;
            Vector2 direction = forward.RotatedBy(wave).SafeNormalize(forward);

            // 保留发射点的左右错位，让它不是从同一个点挤出来。
            float sideOffset = (float)Math.Sin(shotIndex * 2.41f) * 18f;
            Vector2 spawnPosition = Projectile.Center + forward * 18f + normal * sideOffset;
            float speed = piercingShot
                ? MathHelper.Lerp(5.4f, 7.2f, shotIndex / (float)(ShotCount - 1))
                : Main.rand.NextFloat(13.5f, 16.5f);

            int damage = Math.Max(1, (int)(Projectile.damage * 0.77f));

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                direction * speed,
                ModContent.ProjectileType<AshesofCalamity_Soul>(),
                damage,
                Projectile.knockBack,
                Projectile.owner,
                piercingShot ? 1f : 0f,
                shotIndex);

            SoundEngine.PlaySound(SoundID.Item73, spawnPosition);
        }

        private static Vector2 GetOwnerAimDirection(Player owner, Vector2 fallback)
        {
            Vector2 mouseWorld = owner.whoAmI == Main.myPlayer && !Main.dedServ ? Main.MouseWorld : owner.Calamity().mouseWorld;
            return (mouseWorld - owner.Center).SafeNormalize(fallback.SafeNormalize(Vector2.UnitX * owner.direction));
        }

        private void SpawnMuzzleFlash(Vector2 forward)
        {
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                new Color(255, 180, 68),
                new Vector2(0.82f, 0.28f),
                forward.ToRotation(),
                0.06f,
                1.25f,
                16));
        }

        private void SpawnChargeEffects(Vector2 forward)
        {
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            Color orange = new(255, 162, 64);
            Color ember = new(210, 42, 18);

            if ((int)Timer % 2 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center + normal * Main.rand.NextFloat(-8f, 8f),
                    -forward * Main.rand.NextFloat(0.3f, 1.2f),
                    "CalamityMod/Particles/VerticalSmear",
                    false,
                    1,
                    Main.rand.NextFloat(0.7f, 1.05f),
                    Color.Lerp(orange, ember, Main.rand.NextFloat(0.2f, 0.7f)),
                    new Vector2(0.16f, 0.62f),
                    true,
                    true,
                    shrinkSpeed: 0.78f,
                    glowOpacity: 0.42f));
            }
        }
    }
}
