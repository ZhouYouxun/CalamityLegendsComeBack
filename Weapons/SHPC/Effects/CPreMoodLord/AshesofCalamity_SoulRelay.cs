using CalamityMod;
using CalamityMod.Dusts;
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

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
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
            NPC target = FindTarget(3200f, forward);
            float wave = (float)Math.Sin(shotIndex * 1.83f) * MathHelper.Lerp(0.26f, 0.06f, shotIndex / (float)(ShotCount - 1));
            Vector2 direction = forward.RotatedBy(wave);

            if (target is not null)
            {
                Vector2 targetDirection = (target.Center + target.velocity * 12f - Projectile.Center).SafeNormalize(direction);
                direction = Vector2.Lerp(direction, targetDirection, 0.72f).SafeNormalize(targetDirection);
            }

            Vector2 spawnPosition = Projectile.Center + forward * 18f + normal * (float)Math.Sin(shotIndex * 2.41f) * 18f;
            int damage = Math.Max(1, (int)(Projectile.damage * 0.86f));

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                direction * Main.rand.NextFloat(13.5f, 16.5f),
                ModContent.ProjectileType<AshesofCalamity_Soul>(),
                damage,
                Projectile.knockBack,
                Projectile.owner,
                target?.whoAmI ?? -1f,
                shotIndex);

            if (shotIndex == 0 || shotIndex == ShotCount - 1 || shotIndex % 3 == 2)
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.24f, Pitch = -0.22f + shotIndex * 0.025f, PitchVariance = 0.08f, MaxInstances = 6 }, spawnPosition);
        }

        private NPC FindTarget(float range, Vector2 forward)
        {
            NPC bestTarget = null;
            float bestScore = range;

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
                    score *= 0.72f;

                if (score >= bestScore)
                    continue;

                bestTarget = npc;
                bestScore = score;
            }

            return bestTarget;
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
                    Main.rand.Next(12, 17),
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

    internal sealed class AshesofCalamity_Soul : ModProjectile, ILocalizedModType
    {
        private const float BaseSpeed = 15.5f;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private int PreferredTargetIndex => (int)Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 26;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (Projectile.velocity == Vector2.Zero)
                Projectile.velocity = Vector2.UnitX * BaseSpeed;
        }

        public override void AI()
        {
            Timer++;
            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            NPC target = FindTarget(4200f, currentDirection);
            if (target is not null)
            {
                Vector2 desiredDirection = (target.Center + target.velocity * 12f - Projectile.Center).SafeNormalize(currentDirection);
                float tracking = Utils.GetLerpValue(0f, 72f, Timer, true);
                float targetSpeed = BaseSpeed * MathHelper.Lerp(1f, 1.42f, tracking);
                float inertia = MathHelper.Lerp(13f, 2.1f, tracking);
                Projectile.velocity = (Projectile.velocity * inertia + desiredDirection * targetSpeed) / (inertia + 1f);
            }
            else
                Projectile.velocity = currentDirection.RotatedBy((float)Math.Sin(Timer * 0.07f) * 0.012f) * MathHelper.Lerp(Projectile.velocity.Length(), BaseSpeed, 0.06f);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.alpha = Math.Max(0, Projectile.alpha - 24);
            Lighting.AddLight(Projectile.Center, new Color(255, 120, 48).ToVector3() * 0.55f);

            if (!Main.dedServ)
            {
                SpawnFlightEffects();
                SpawnCalamitousDartMetaballs(Projectile.velocity.SafeNormalize(Vector2.UnitX));
            }
        }

        private NPC FindTarget(float range, Vector2 currentDirection)
        {
            if (Main.npc.IndexInRange(PreferredTargetIndex))
            {
                NPC preferred = Main.npc[PreferredTargetIndex];
                if (preferred.CanBeChasedBy(Projectile, false) && Projectile.Distance(preferred.Center) <= range)
                    return preferred;
            }

            NPC bestTarget = null;
            float bestScore = range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                Vector2 toTarget = npc.Center - Projectile.Center;
                float distance = toTarget.Length();
                if (distance > range)
                    continue;

                float angularPenalty = (1f - MathHelper.Clamp(Vector2.Dot(currentDirection, toTarget.SafeNormalize(currentDirection)), -1f, 1f)) * 640f;
                float score = distance + angularPenalty;
                if (npc.boss)
                    score *= 0.68f;

                if (score >= bestScore)
                    continue;

                bestTarget = npc;
                bestScore = score;
            }

            return bestTarget;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(3) ? DustID.CursedTorch : (int)CalamityDusts.Brimstone,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 6f),
                    100,
                    Main.rand.NextBool() ? new Color(255, 176, 68) : new Color(210, 42, 18),
                    Main.rand.NextFloat(0.85f, 1.35f));
                dust.noGravity = true;
            }
        }

        private void SpawnFlightEffects()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Color orange = new(255, 140, 42);
            Color red = new(180, 12, 8);

            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                Projectile.Center - direction * Main.rand.NextFloat(2f, 12f) + normal * Main.rand.NextFloat(-4f, 4f),
                Projectile.velocity * 0.02f,
                "CalamityMod/Particles/VerticalSmear",
                false,
                Main.rand.Next(13, 18),
                Main.rand.NextFloat(1.25f, 1.85f),
                Color.Lerp(orange, red, Main.rand.NextFloat(0.15f, 0.65f)),
                new Vector2(0.18f, 0.86f),
                true,
                true,
                shrinkSpeed: 0.82f,
                glowOpacity: 0.48f));
        }

        private void SpawnCalamitousDartMetaballs(Vector2 direction)
        {
            CalamitasMetaball.Particle point = CalamitasMetaball.SpawnParticle(
                Projectile.Center + Projectile.velocity * 2f,
                Vector2.Zero,
                40f * Projectile.scale);

            point.rotation = direction.ToRotation() + MathHelper.PiOver2;
            point.TextureToUse = ModContent.Request<Texture2D>("CalamityMod/Particles/PointParticle").Value;
            point.SizeScaling = 0.65f;

            CalamitasMetaball.Particle body = CalamitasMetaball.SpawnParticle(
                Projectile.Center,
                Main.rand.NextVector2Circular(3f, 3f),
                24f * Projectile.scale);

            body.SizeScaling = 0.8f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmear").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(255f, 0f, Projectile.alpha, true);
            Color orange = new Color(255, 140, 42, 0) * opacity;
            Color red = new Color(170, 8, 8, 0) * opacity;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null, red * 0.52f, Projectile.rotation, bloom.Size() * 0.5f, 0.28f, SpriteEffects.None);
            Main.EntitySpriteDraw(smear, drawPosition, null, orange * 0.68f, Projectile.rotation, smear.Size() * 0.5f, new Vector2(0.12f, 0.58f), SpriteEffects.None);
            Main.EntitySpriteDraw(star, drawPosition, null, Color.White * (0.28f * opacity), Projectile.rotation, star.Size() * 0.5f, new Vector2(0.18f, 0.42f), SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
