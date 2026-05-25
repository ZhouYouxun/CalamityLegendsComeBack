using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ashes
{
    internal sealed class AshesofAnn_BurstRelay : ModProjectile, ILocalizedModType
    {
        private const int ShotCount = 17;
        private const int WarmupFrames = 6;
        private const int FireInterval = 4;
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
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.timeLeft = WarmupFrames + ShotCount * FireInterval + 24;
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
            Projectile.Opacity = 0f;
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.55f, Pitch = -0.25f, PitchVariance = 0.08f, MaxInstances = 4 }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.35f, Pitch = -0.45f, PitchVariance = 0.1f, MaxInstances = 4 }, Projectile.Center);

            if (!Main.dedServ)
                SpawnOpeningFlash(forward);
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
            Projectile.rotation = forward.ToRotation() + Timer * 0.045f;
            Projectile.Opacity = Utils.GetLerpValue(0f, 10f, Timer, true) * Utils.GetLerpValue(0f, 14f, Projectile.timeLeft, true);

            if (Projectile.owner == Main.myPlayer && Timer >= WarmupFrames && ShotsFired < ShotCount && (Timer - WarmupFrames) % FireInterval == 0f)
                FireCurseShot((int)ShotsFired++);

            Lighting.AddLight(Projectile.Center, new Vector3(0.75f, 0.08f, 0.02f) * (0.55f * Projectile.Opacity));

            if (!Main.dedServ)
                SpawnRelayEffects(forward);
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

        private void FireCurseShot(int shotIndex)
        {
            Vector2 forward = ForwardDirection;
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            NPC target = FindTarget(4200f);
            float shotCompletion = shotIndex / (float)(ShotCount - 1);
            float weave = (float)Math.Sin(shotIndex * 2.399963f) * MathHelper.Lerp(0.48f, 0.1f, shotCompletion);
            Vector2 direction = forward.RotatedBy(weave);

            if (target is not null)
            {
                Vector2 predictedCenter = target.Center + target.velocity * MathHelper.Lerp(8f, 18f, shotCompletion);
                Vector2 targetDirection = (predictedCenter - Projectile.Center).SafeNormalize(direction);
                direction = Vector2.Lerp(direction, targetDirection, 0.82f).SafeNormalize(targetDirection);
            }

            Vector2 spawnPosition = Projectile.Center + forward * 20f + normal * (float)Math.Sin(shotIndex * 1.618034f) * 24f;
            float speed = MathHelper.Lerp(13.5f, 18.8f, shotCompletion) + Main.rand.NextFloat(-0.35f, 0.35f);
            int damage = Math.Max(1, (int)(Projectile.damage * MathHelper.Lerp(0.62f, 0.84f, shotCompletion)));
            if (shotIndex == ShotCount - 1)
                damage = Math.Max(1, (int)(Projectile.damage * 1.16f));

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                direction * speed,
                ModContent.ProjectileType<AshesofAnn_CurseFire>(),
                damage,
                Projectile.knockBack,
                Projectile.owner,
                target?.whoAmI ?? -1f,
                shotIndex,
                Main.rand.NextFloat(0.7f, 1.3f));

            if (shotIndex == 0 || shotIndex == ShotCount - 1 || shotIndex % 4 == 3)
            {
                SoundStyle style = shotIndex == ShotCount - 1 ? SoundID.Item74 : SoundID.Item20;
                SoundEngine.PlaySound(style with { Volume = shotIndex == ShotCount - 1 ? 0.42f : 0.22f, Pitch = -0.28f + shotIndex * 0.015f, PitchVariance = 0.08f, MaxInstances = 6 }, spawnPosition);
            }

            SpawnShotRelease(spawnPosition, direction, shotIndex);
        }

        private NPC FindTarget(float range)
        {
            NPC bestTarget = null;
            float bestScore = range;
            Vector2 forward = ForwardDirection;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                Vector2 toTarget = npc.Center - Projectile.Center;
                float distance = toTarget.Length();
                if (distance > range)
                    continue;

                float angularPenalty = (1f - MathHelper.Clamp(Vector2.Dot(forward, toTarget.SafeNormalize(forward)), -1f, 1f)) * 520f;
                float score = distance + angularPenalty;
                if (npc.boss)
                    score *= 0.7f;

                if (score >= bestScore)
                    continue;

                bestTarget = npc;
                bestScore = score;
            }

            return bestTarget;
        }

        private void SpawnOpeningFlash(Vector2 forward)
        {
            Color mainColor = new(255, 64, 32);
            Color darkColor = new(78, 0, 0);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                mainColor,
                new Vector2(0.95f, 0.44f),
                forward.ToRotation(),
                0.08f,
                1.55f,
                20));

            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = forward.RotatedByRandom(0.38f) * Main.rand.NextFloat(3.5f, 9f) + normal * Main.rand.NextFloat(-1.6f, 1.6f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center + normal * Main.rand.NextFloat(-16f, 16f),
                    velocity,
                    "CalamityMod/Particles/SmallBloom",
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.18f, 0.32f),
                    Main.rand.NextBool(3) ? Color.White : mainColor,
                    new Vector2(Main.rand.NextFloat(1.15f, 2.1f), Main.rand.NextFloat(0.45f, 0.75f)),
                    true,
                    false,
                    0f,
                    false,
                    false,
                    0.65f));
            }

            for (int i = 0; i < 6; i++)
                CalamitasMetaball.SpawnParticle(Projectile.Center - forward * i * 5f, -forward * Main.rand.NextFloat(0.4f, 1.4f) + Main.rand.NextVector2Circular(0.7f, 0.7f), Main.rand.NextFloat(24f, 42f));

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(18f, 18f),
                    Main.rand.NextBool(4) ? DustID.Smoke : (int)CalamityDusts.Brimstone,
                    forward.RotatedByRandom(0.72f) * Main.rand.NextFloat(1.5f, 5.4f),
                    115,
                    Main.rand.NextBool() ? mainColor : darkColor,
                    Main.rand.NextFloat(0.9f, 1.45f));
                dust.noGravity = !Main.rand.NextBool(4);
            }
        }

        private void SpawnRelayEffects(Vector2 forward)
        {
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Timer * 0.24f);
            Color mainColor = Color.Lerp(new Color(160, 6, 10), new Color(255, 96, 38), pulse);

            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                Projectile.Center + normal * Main.rand.NextFloat(-10f, 10f),
                -forward * Main.rand.NextFloat(0.25f, 1.2f),
                "CalamityMod/Particles/VerticalSmear",
                false,
                Main.rand.Next(12, 17),
                Main.rand.NextFloat(0.9f, 1.25f),
                mainColor,
                new Vector2(0.18f, 0.74f),
                true,
                true,
                shrinkSpeed: 0.76f,
                glowOpacity: 0.36f));

            for (int i = 0; i < 7; i++)
            {
                float completion = i / 6f;
                Vector2 ribbonPosition = Projectile.Center
                    - forward * Main.rand.NextFloat(0f, 48f)
                    + normal * Main.rand.NextFloat(-24f, 24f) * MathHelper.Lerp(1f, 0.45f, completion)
                    + Main.rand.NextVector2Circular(3f, 3f);

                CalamitasMetaball.SpawnParticle(
                    ribbonPosition,
                    -forward * Main.rand.NextFloat(0.35f, 1.45f) + normal * Main.rand.NextFloat(-0.45f, 0.45f) + Main.rand.NextVector2Circular(0.35f, 0.35f),
                    Main.rand.NextFloat(24f, 46f) * MathHelper.Lerp(1.08f, 0.72f, completion));

                if (i % 2 == 0)
                    RancorLavaMetaball.SpawnParticle(ribbonPosition + Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextFloat(18f, 34f));
            }
        }

        private void SpawnShotRelease(Vector2 center, Vector2 direction, int shotIndex)
        {
            if (Main.dedServ)
                return;

            Color mainColor = shotIndex == ShotCount - 1 ? new Color(255, 190, 92) : new Color(255, 72, 32);
            Color darkColor = new(74, 0, 0);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                direction * 0.4f,
                mainColor,
                new Vector2(0.72f, 0.24f),
                direction.ToRotation(),
                0.05f,
                1.25f,
                14));

            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextBool(3) ? DustID.CursedTorch : (int)CalamityDusts.Brimstone,
                    -direction.RotatedByRandom(0.45f) * Main.rand.NextFloat(0.8f, 3.2f),
                    100,
                    Main.rand.NextBool() ? mainColor : darkColor,
                    Main.rand.NextFloat(0.82f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.Opacity <= 0f)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float pulse = 0.88f + (float)Math.Sin(Timer * 0.2f) * 0.12f;
            Color ember = new(255, 74, 30, 0);
            Color blood = new(146, 0, 0, 0);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null, blood * (0.58f * Projectile.Opacity), Projectile.rotation, bloom.Size() * 0.5f, 0.42f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPosition, null, ember * (0.38f * Projectile.Opacity), -Projectile.rotation * 0.6f, bloom.Size() * 0.5f, new Vector2(0.18f, 0.46f) * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(star, drawPosition, null, Color.White with { A = 0 } * (0.48f * Projectile.Opacity), Projectile.rotation, star.Size() * 0.5f, new Vector2(0.24f, 0.58f) * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(star, drawPosition, null, ember * (0.42f * Projectile.Opacity), Projectile.rotation + MathHelper.PiOver2, star.Size() * 0.5f, new Vector2(0.16f, 0.42f) * pulse, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            return false;
        }
    }
}
