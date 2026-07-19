using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ashes
{
    // The primary fire is deliberately a paired sweep. Every beat releases one left and one
    // right homing ember, so the pattern reads as a wide double strafe instead of a random spray.
    internal sealed class AshesofAnn_BurstRelay : ModProjectile, ILocalizedModType
    {
        private const int SweepPairCount = 8;
        private const int WarmupFrames = 3;
        private const int FireInterval = 2;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float SweepPairsFired => ref Projectile.localAI[1];
        private int MarkedTargetIndex = -1;

        private Vector2 ForwardDirection
        {
            get
            {
                Vector2 stored = new(Projectile.ai[0], Projectile.ai[1]);
                return stored.LengthSquared() > 0.001f
                    ? stored.SafeNormalize(Vector2.UnitX)
                    : Projectile.velocity.SafeNormalize(Vector2.UnitX);
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.timeLeft = WarmupFrames + SweepPairCount * FireInterval + 8;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(IEntitySource source)
        {
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.36f, Pitch = -0.20f, PitchVariance = 0.1f, MaxInstances = 4 }, Projectile.Center);

            Player owner = Main.player[Projectile.owner];
            NPC markedTarget = FindMarkedTarget(owner.Center, 2600f);
            MarkedTargetIndex = markedTarget?.whoAmI ?? -1;
            if (Projectile.owner == Main.myPlayer && markedTarget is not null)
            {
                Vector2 attackDirection = (markedTarget.Center - owner.Center).SafeNormalize(ForwardDirection);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    markedTarget.Center,
                    attackDirection,
                    ModContent.ProjectileType<AshesofAnn_Located>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    markedTarget.whoAmI);
            }

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

            if (Projectile.owner == Main.myPlayer && Timer >= WarmupFrames && SweepPairsFired < SweepPairCount && (Timer - WarmupFrames) % FireInterval == 0f)
                FireHomingSweepPair((int)SweepPairsFired++);

            if (!Main.dedServ)
                SpawnChargeEffects(forward);
        }

        private void FireHomingSweepPair(int pairIndex)
        {
            Vector2 forward = ForwardDirection;
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            float completion = pairIndex / (float)Math.Max(1, SweepPairCount - 1);
            float sweepAngle = MathHelper.Lerp(0.38f, 0.055f, completion);
            int damage = Math.Max(1, (int)(Projectile.damage * MathHelper.Lerp(0.78f, 0.94f, completion)));

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 direction = forward.RotatedBy(sweepAngle * side).SafeNormalize(forward);
                Vector2 spawnPosition = Projectile.Center + forward * 20f + normal * side * MathHelper.Lerp(30f, 8f, completion);
                int shotIndex = pairIndex * 2 + (side > 0 ? 1 : 0);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    direction * MathHelper.Lerp(18f, 15.5f, completion),
                    ModContent.ProjectileType<AshesofAnn_CurseFire>(),
                    damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    0f,
                    shotIndex);

                SpawnShotGlow(spawnPosition, direction, side);
            }

            NPC markedTarget = GetMarkedTarget();
            if (markedTarget is not null)
                FireBrotherAmbushPair(markedTarget, pairIndex, completion);

            SoundEngine.PlaySound(SoundID.Item73 with { Volume = 0.54f, Pitch = MathHelper.Lerp(-0.24f, 0.12f, completion), MaxInstances = 8 }, Projectile.Center);
        }

        private void FireBrotherAmbushPair(NPC markedTarget, int pairIndex, float completion)
        {
            // Eight beats produce exactly eight red fists and eight tech-blue blades. Their
            // launch points are genuinely random around the target; only their inward vector
            // is fixed, so the target is crossed from a different lane on every strike.
            for (int attackIndex = 0; attackIndex < 2; attackIndex++)
            {
                bool fist = attackIndex == 0;
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = Main.rand.NextFloat(300f, 460f);
                Vector2 spawnPosition = markedTarget.Center + angle.ToRotationVector2() * radius;
                Vector2 direction = (markedTarget.Center - spawnPosition).SafeNormalize(ForwardDirection);
                float speed = MathHelper.Lerp(42f, 35f, completion) * Main.rand.NextFloat(0.92f, 1.08f);
                int damage = Math.Max(1, (int)(Projectile.damage * MathHelper.Lerp(0.52f, 0.64f, completion)));

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    direction * speed,
                    fist ? ModContent.ProjectileType<AshesofAnn_CataclysmFist>() : ModContent.ProjectileType<AshesofAnn_CatastropheSlash>(),
                    damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    markedTarget.whoAmI,
                    pairIndex);
            }
        }

        private NPC GetMarkedTarget()
        {
            return MarkedTargetIndex >= 0 && MarkedTargetIndex < Main.maxNPCs && Main.npc[MarkedTargetIndex].CanBeChasedBy(null, false)
                ? Main.npc[MarkedTargetIndex]
                : null;
        }

        private static NPC FindMarkedTarget(Vector2 center, float range)
        {
            NPC closest = null;
            float closestDistance = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(null, false))
                    continue;

                float distance = Vector2.Distance(center, npc.Center);
                if (distance >= closestDistance)
                    continue;

                closest = npc;
                closestDistance = distance;
            }

            return closest;
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
            if ((int)Timer % 2 != 0)
                return;

            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                Projectile.Center + normal * Main.rand.NextFloat(-8f, 8f),
                -forward * Main.rand.NextFloat(0.3f, 1.2f),
                "CalamityMod/Particles/VerticalSmear",
                false,
                1,
                Main.rand.NextFloat(0.7f, 1.05f),
                Color.Lerp(new Color(255, 162, 64), new Color(210, 42, 18), Main.rand.NextFloat(0.2f, 0.7f)),
                new Vector2(0.16f, 0.62f),
                true,
                true,
                shrinkSpeed: 0.78f,
                glowOpacity: 0.42f));
        }

        private static void SpawnShotGlow(Vector2 center, Vector2 direction, int side)
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
                return;

            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                center,
                -direction * Main.rand.NextFloat(0.25f, 0.9f) + direction.RotatedBy(MathHelper.PiOver2) * side * 0.4f,
                false,
                Main.rand.Next(7, 12),
                Main.rand.NextFloat(0.16f, 0.25f),
                Color.Lerp(new Color(255, 112, 34), new Color(255, 205, 82), Main.rand.NextFloat(0.2f, 0.75f)),
                true,
                false,
                true));
        }
    }
}
