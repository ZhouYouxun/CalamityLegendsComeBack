using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    internal sealed class DepthCells_Drop : ModProjectile
    {
        internal static readonly Color AbyssDeep = new(6, 16, 30);
        internal static readonly Color AbyssBlue = new(18, 74, 96);
        internal static readonly Color AbyssCyan = new(72, 208, 255);
        internal static readonly Color AbyssToxic = new(108, 255, 176);
        internal static readonly Color AbyssFoam = new(210, 255, 236);

        private const float GravityDelay = 10f;
        private const float GravityStrength = 0.055f;
        private const float HomingStartDistance = 15f * 16f;
        private const float HomingRange = 920f;
        private const float MaxHomingSpeed = 9.9f;
        private const float HomingInertia = 27f;
        private const float NoTargetDamping = 0.992f;
        private const float WanderingTurnStrength = 0.006f;
        private const int StickTime = 150;
        private const int StuckDamageInterval = 30;
        private const float StuckDamageMultiplier = 0.4f;
        private static readonly int[] AbyssDustTypes = { 191, 29, 104 };

        private bool IsStuck => Projectile.ai[0] == 1f;
        private int spreadDust;
        private float traveledDistance;
        private Color waterColor = Color.DeepSkyBlue;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 180;
            Projectile.penetrate = 3;
            Projectile.extraUpdates = 2;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            waterColor = Main.rand.NextBool() ? Color.DodgerBlue : Color.DeepSkyBlue;
            SpawnLaunchEffects();
        }

        public override void AI()
        {
            if (IsStuck)
            {
                UpdateStuckState();
                return;
            }

            Projectile.localAI[0]++;
            traveledDistance += Projectile.velocity.Length();
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            bool homingActive = traveledDistance >= HomingStartDistance && HomeTowardTarget();

            if (!homingActive && Projectile.localAI[0] > GravityDelay)
            {
                float sway = (float)System.Math.Sin((Projectile.identity * 0.6f) + Projectile.localAI[0] * 0.17f) * 0.012f;
                Projectile.velocity = Projectile.velocity.RotatedBy(sway);
                Projectile.velocity.Y += GravityStrength;
                Projectile.velocity.X *= 0.998f;
            }

            Lighting.AddLight(Projectile.Center, Color.Lerp(AbyssToxic, AbyssCyan, 0.35f).ToVector3() * 0.55f);
            SpawnFlightEffects();
        }

        private bool HomeTowardTarget()
        {
            NPC target = FindHomingTarget();
            if (target is null)
            {
                FreeDrift(NoTargetDamping);
                return false;
            }

            Vector2 currentVelocity = Projectile.velocity;
            float currentSpeed = currentVelocity.Length();
            if (currentSpeed < 0.1f)
                currentVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 4f;

            Vector2 currentDirection = currentVelocity.SafeNormalize(Vector2.UnitX);
            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(currentDirection);
            float closePressure = Utils.GetLerpValue(360f, 70f, Projectile.Distance(target.Center), true);
            float pullStrength = MathHelper.Lerp(0.35f, 1f, closePressure);
            float targetSpeed = MathHelper.Lerp(6.3f, MaxHomingSpeed, pullStrength);
            Vector2 desiredVelocity = desiredDirection * targetSpeed;

            Projectile.velocity = (currentVelocity * HomingInertia + desiredVelocity) / (HomingInertia + 1f);
            float sideSway = (float)System.Math.Sin((Projectile.localAI[0] + Projectile.identity * 7f) * 0.075f) *
                MathHelper.Lerp(0.012f, 0.004f, pullStrength);
            Projectile.velocity = Projectile.velocity.RotatedBy(sideSway);

            if (Projectile.velocity.Length() > MaxHomingSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(desiredDirection) * MaxHomingSpeed;

            return true;
        }

        private void FreeDrift(float damping)
        {
            float wander = (float)System.Math.Sin((Projectile.localAI[0] + Projectile.identity * 5f) * 0.08f) * WanderingTurnStrength;
            Projectile.velocity = Projectile.velocity.RotatedBy(wander) * damping;
        }

        private NPC FindHomingTarget()
        {
            int preferredTargetIndex = (int)Projectile.ai[2] - 1;
            if (Main.npc.IndexInRange(preferredTargetIndex))
            {
                NPC preferredTarget = Main.npc[preferredTargetIndex];
                if (preferredTarget.CanBeChasedBy(Projectile) && Projectile.Distance(preferredTarget.Center) <= HomingRange)
                    return preferredTarget;
            }

            NPC bestTarget = null;
            float bestDistance = HomingRange;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                modifiers.SourceDamage *= 0.82f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<CrushDepth>(), 240);
            target.AddBuff(ModContent.BuffType<Eutrophication>(), 240);
            SpawnImpactEffects(target.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), 0.9f);

            if (!IsStuck)
                StickToTarget(target);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnImpactEffects(Projectile.Center, oldVelocity.SafeNormalize(Vector2.UnitY), 1.05f);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            SpawnDeathEffects();
            SoundEngine.PlaySound(SoundID.NPCDeath13 with { Volume = 0.26f, Pitch = 0.32f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        private void StickToTarget(NPC target)
        {
            Projectile.ai[0] = 1f;
            Projectile.ai[1] = target.whoAmI;
            Projectile.localAI[1] = 0f;
            Projectile.velocity = target.Center - Projectile.Center;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 0;
            Projectile.netUpdate = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        private void UpdateStuckState()
        {
            int targetIndex = (int)Projectile.ai[1];

            if (!Main.npc.IndexInRange(targetIndex) || !Main.npc[targetIndex].active)
            {
                Projectile.Kill();
                return;
            }

            NPC target = Main.npc[targetIndex];
            Projectile.localAI[1]++;
            Projectile.Center = target.Center - Projectile.velocity;
            Projectile.gfxOffY = target.gfxOffY;
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 2);

            Lighting.AddLight(Projectile.Center, Color.Lerp(AbyssToxic, AbyssCyan, 0.35f).ToVector3() * 0.28f);

            if (Projectile.numUpdates == 0 && Main.rand.NextBool(4))
            {
                Dust seep = CreateAbyssDust(
                    Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                    Main.rand.NextVector2Circular(0.35f, 0.35f),
                    Main.rand.NextFloat(0.85f, 1.15f),
                    Main.rand.NextFloat(0.3f, 0.85f),
                    140);
                seep.velocity *= 0.35f;
            }

            if (Projectile.owner == Main.myPlayer && Projectile.localAI[1] % StuckDamageInterval == 0f)
                ApplyStuckDamage(target);

            if (Projectile.localAI[1] >= StickTime)
                Projectile.Kill();
        }

        private void ApplyStuckDamage(NPC target)
        {
            int damage = Math.Max(1, (int)(Projectile.damage * StuckDamageMultiplier));
            int hitDirection = (target.Center.X >= Projectile.Center.X).ToDirectionInt();
            target.StrikeNPC(target.CalculateHitInfo(damage, hitDirection));

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(2.8f, 2.8f);
                Dust burst = CreateFoamDust(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    velocity,
                    Main.rand.NextFloat(0.9f, 1.25f),
                    Main.rand.NextFloat(0.35f, 1f),
                    110);
                burst.velocity *= 0.8f;
            }
        }

        private void SpawnLaunchEffects()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -forward;

            GeneralParticleHandler.SpawnParticle(new WaterFlavoredParticle(
                Projectile.Center,
                back * 0.6f,
                affectedByGravity: false,
                24,
                0.9f,
                waterColor * 0.18f));

            for (int i = 0; i < 12; i++)
            {
                Dust jetDust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextBool(3) ? 104 : 29,
                    forward.RotatedByRandom(0.72f) * Main.rand.NextFloat(0.8f, 3.2f) + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    100,
                    Color.Lerp(AbyssDeep, AbyssToxic, Main.rand.NextFloat(0.45f, 1f)),
                    Main.rand.NextFloat(1f, 1.65f));
                jetDust.noGravity = true;
                jetDust.fadeIn = jetDust.scale * 1.05f;
            }

            for (int i = 0; i < 7; i++)
            {
                Particle foam = new CustomSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    back.RotatedByRandom(0.32f) * Main.rand.NextFloat(0.5f, 1.6f),
                    "CalamityMod/Particles/WaterFoam",
                    false,
                    Main.rand.Next(5, 8),
                    Main.rand.NextFloat(0.16f, 0.24f),
                    Color.Lerp(waterColor, AbyssFoam, Main.rand.NextFloat(0.25f, 0.65f)) * 0.85f,
                    Vector2.One,
                    true,
                    false);
                GeneralParticleHandler.SpawnParticle(foam);
            }
        }

        private void SpawnFlightEffects()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -forward;
            Vector2 spawnCenter = Projectile.Center + back * Main.rand.NextFloat(1f, 7f);
            float jetAge = Projectile.localAI[0];

            if (Projectile.timeLeft % 2 == 0 && jetAge > 3f)
            {
                Particle foam = new CustomSpark(
                    Projectile.Center,
                    Projectile.velocity * Main.rand.NextFloat(0.1f, 0.5f),
                    "CalamityMod/Particles/WaterFoam",
                    false,
                    Main.rand.Next(4, 7),
                    Main.rand.NextFloat(0.15f, 0.22f),
                    Color.DodgerBlue * 0.75f,
                    Vector2.One,
                    true,
                    false,
                    Main.rand.NextFloat(-10f, 10f));
                GeneralParticleHandler.SpawnParticle(foam);
            }

            if (Projectile.timeLeft > 20)
            {
                Particle waterLine = new CustomSpark(
                    Projectile.Center,
                    back * Projectile.velocity.Length() * 0.05f,
                    "CalamityMod/Particles/WaterFlavored",
                    false,
                    2,
                    MathHelper.Min(1.45f, 0.85f + jetAge * 0.013f),
                    waterColor * MathHelper.Clamp(1f - jetAge * 0.006f, 0.25f, 0.9f),
                    new Vector2(0.2f + MathHelper.Min(jetAge * 0.008f, 0.35f), 1f));
                GeneralParticleHandler.SpawnParticle(waterLine);
            }

            if (Projectile.numUpdates == 0 && Projectile.timeLeft % 2 == 0)
            {
                Particle smoke = new HeavySmokeParticle(
                    Projectile.Center,
                    Projectile.velocity * -Main.rand.NextFloat(0.2f, 0.6f),
                    Color.MediumBlue,
                    30,
                    Main.rand.NextFloat(0.35f, 0.5f),
                    0.3f,
                    Main.rand.NextFloat(-0.2f, 0.2f),
                    false,
                    0f,
                    true);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (Main.rand.NextBool())
            {
                int dustType = Main.rand.NextBool(5) ? 267 : 278;
                Dust waterDust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(3f + spreadDust, 3f + spreadDust),
                    dustType,
                    -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.35f),
                    0,
                    Main.rand.NextBool(5) ? Color.Aqua : waterColor,
                    Main.rand.NextFloat(0.4f, 0.6f));
                waterDust.noGravity = true;
                if (waterDust.type == 278)
                    waterDust.scale *= 0.7f;
            }

            for (int i = 0; i < 2; i++)
            {
                int dustType = Main.rand.NextBool(3) ? 104 : (Main.rand.NextBool() ? 96 : 29);
                Dust jetDust = Dust.NewDustPerfect(
                    spawnCenter,
                    dustType,
                    Projectile.velocity * Main.rand.NextFloat(0.25f, 0.7f) + new Vector2(0.5f, 0.5f).RotatedByRandom(100f) * Main.rand.NextFloat(0.2f, 1.1f),
                    0,
                    Color.Lerp(AbyssBlue, AbyssToxic, Main.rand.NextFloat(0.25f, 0.85f)),
                    Main.rand.NextFloat(0.9f, 1.55f));
                jetDust.noGravity = true;
            }

            if (Projectile.timeLeft < 20)
                spreadDust += 2;
        }

        private void SpawnImpactEffects(Vector2 center, Vector2 forward, float intensity)
        {
            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = forward.RotatedByRandom(0.95f) * Main.rand.NextFloat(1.1f, 4.4f) * intensity + Main.rand.NextVector2Circular(0.65f, 0.65f);
                CreateAbyssDust(
                    center + Main.rand.NextVector2Circular(6f, 6f),
                    velocity,
                    Main.rand.NextFloat(1.05f, 1.75f) * intensity,
                    Main.rand.NextFloat(0.25f, 0.95f),
                    120);
            }

            for (int i = 0; i < 7; i++)
            {
                Dust foam = CreateFoamDust(
                    center + Main.rand.NextVector2Circular(5f, 5f),
                    forward.RotatedByRandom(1.1f) * Main.rand.NextFloat(0.7f, 2.4f) * intensity + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    Main.rand.NextFloat(0.9f, 1.25f) * intensity,
                    Main.rand.NextFloat(0.2f, 1f),
                    130);
                foam.velocity *= 0.75f;
            }
        }

        private void SpawnDeathEffects()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            SpawnImpactEffects(Projectile.Center, forward, 1f);

            for (int i = 0; i < 6; i++)
            {
                Dust mist = CreateAbyssDust(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextVector2Circular(1.1f, 1.1f) - forward * Main.rand.NextFloat(0.1f, 0.5f),
                    Main.rand.NextFloat(1.2f, 1.75f),
                    Main.rand.NextFloat(0.15f, 0.65f),
                    130);
                mist.velocity *= 0.9f;
            }
        }

        private static Dust CreateAbyssDust(Vector2 position, Vector2 velocity, float scale, float colorInterpolant, int alpha)
        {
            Dust dust = Dust.NewDustPerfect(
                position,
                AbyssDustTypes[Main.rand.Next(AbyssDustTypes.Length)],
                velocity,
                alpha,
                Color.Lerp(AbyssDeep, AbyssToxic, colorInterpolant),
                scale);
            dust.noGravity = true;
            dust.fadeIn = scale * 1.05f;
            return dust;
        }

        private static Dust CreateFoamDust(Vector2 position, Vector2 velocity, float scale, float colorInterpolant, int alpha)
        {
            Dust dust = Dust.NewDustPerfect(
                position,
                DustID.Water,
                velocity,
                alpha,
                Color.Lerp(AbyssCyan, AbyssFoam, colorInterpolant),
                scale);
            dust.noGravity = true;
            return dust;
        }
    }
}
