using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.Passive
{
    internal sealed class PristineFuryCrystal : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Boss/ProvidenceCrystal";

        internal const float HoverHeight = 90f;
        private const float DrawScale = 0.45f;
        private const int ShardPairInterval = 3;
        private const int ShardWaveInterval = 20;
        private const int ShardWaveCount = 7;
        private const int ShardPairCount = 4;
        private const float ShardFallSpeed = 36f;
        private const float ShardSpawnHeight = 50f * 16f;
        private const float ShardSpawnHalfWidth = 12f * 16f;
        private const int LaserPairCooldown = 85;
        private const float LaserRotPerFrame = MathHelper.Pi / 160f;
        private static readonly Color RadiantGold = new(255, 222, 76);
        private static readonly Color RadiantWhite = new(255, 248, 196);

        private ref float IntroProgress => ref Projectile.localAI[0];
        private ref float IntroSoundFired => ref Projectile.localAI[1];
        private ref float ShardTimer => ref Projectile.ai[0];
        private ref float LaserCooldown => ref Projectile.ai[1];
        private ref float ShardPairIndex => ref Projectile.ai[2];

        private bool IntroComplete => IntroProgress >= 1f;

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || owner.HeldItem.type != ModContent.ItemType<NewLegendPristineFury>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.damage = Math.Max(1, (int)(owner.GetWeaponDamage(owner.HeldItem) * 2.0f));
            Projectile.Center = owner.Center + new Vector2(0f, -HoverHeight * owner.gravDir);

            if (!IntroComplete)
            {
                IntroProgress = Math.Min(1f, IntroProgress + 0.025f);
                if (IntroProgress >= 1f && IntroSoundFired == 0f)
                {
                    IntroSoundFired = 1f;
                    SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact with { Pitch = -0.5f }, Projectile.Center);
                    SoundEngine.PlaySound(SoundID.DD2_CrystalCartImpact, Projectile.Center);
                    SpawnIntroExplosion();
                }
                return;
            }

            if (!Main.dedServ && Main.rand.NextBool(14))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(36f, 36f),
                    DustID.GoldFlame,
                    Main.rand.NextVector2Circular(0.6f, 0.6f),
                    100, Main.rand.NextBool(3) ? RadiantWhite : RadiantGold, 0.6f);
                d.noGravity = true;
                d.fadeIn = 1f;
            }

            Lighting.AddLight(Projectile.Center, RadiantGold.ToVector3() * 0.45f);

            if (Main.myPlayer != Projectile.owner)
                return;

            bool validMouse = !Main.mapFullscreen && !Main.blockMouse && !owner.mouseInterface;
            bool leftHeld = validMouse && Main.mouseLeft;
            bool rightHeld = validMouse && (Main.mouseRight || owner.Calamity().mouseRight);

            if (leftHeld)
            {
                UpdateShardRain(owner);
            }
            else
            {
                ShardTimer = 0f;
                ShardPairIndex = 0f;
            }

            if (rightHeld && !leftHeld)
            {
                if (LaserCooldown <= 0f)
                {
                    FireScissorPair(owner);
                    LaserCooldown = LaserPairCooldown;
                }
            }

            if (LaserCooldown > 0f)
                LaserCooldown--;
        }

        private void UpdateShardRain(Player owner)
        {
            if (ShardTimer > 0f)
            {
                ShardTimer--;
                return;
            }

            if (FireShardPair(owner, (int)ShardPairIndex))
            {
                ShardPairIndex++;
                int nextDelay = ShardPairIndex >= ShardPairCount ? ShardWaveInterval : ShardPairInterval;
                ShardTimer = nextDelay - 1;

                if (ShardPairIndex >= ShardPairCount)
                    ShardPairIndex = 0f;
            }
            else
                ShardTimer = 5f;
        }

        private bool FireShardPair(Player owner, int pairIndex)
        {
            NPC target = FindTarget(owner, 1500f);
            if (target == null)
                return false;

            SpawnShardReleaseFlash(target.Center, pairIndex);

            int shardType = ModContent.ProjectileType<PristineFuryCrystalShard>();
            int alreadyFired = pairIndex * 2;
            int shotsThisPair = Math.Min(2, ShardWaveCount - alreadyFired);

            for (int i = 0; i < shotsThisPair; i++)
            {
                int shotIndex = alreadyFired + i;
                float waveRatio = shotIndex / (float)(ShardWaveCount - 1);
                float arcOffset = MathHelper.Lerp(-ShardSpawnHalfWidth, ShardSpawnHalfWidth, waveRatio);
                float waveCurl = (float)Math.Sin((Main.GameUpdateCount + shotIndex * 17) * 0.11f) * 46f;

                Vector2 spawnPosition = target.Center + new Vector2(
                    arcOffset + waveCurl + Main.rand.NextFloat(-18f, 18f),
                    -ShardSpawnHeight + Main.rand.NextFloat(-38f, 38f));

                Vector2 aimPosition =
                    target.Center
                    + target.velocity * Main.rand.NextFloat(9f, 17f)
                    + new Vector2(
                        (float)Math.Sin(waveRatio * MathHelper.TwoPi + Main.GameUpdateCount * 0.05f) * 44f,
                        (float)Math.Cos(waveRatio * MathHelper.Pi) * 18f)
                    + Main.rand.NextVector2Circular(18f, 12f);

                Vector2 velocity = (aimPosition - spawnPosition).SafeNormalize(Vector2.UnitY) * ShardFallSpeed * Main.rand.NextFloat(0.92f, 1.08f);
                SpawnShardTelegraph(spawnPosition, aimPosition, shotIndex);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    shardType,
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    waveRatio,
                    target.whoAmI + 1f);
            }

            if (pairIndex == 0)
                SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact with { Volume = 0.55f, Pitch = 0.4f }, Projectile.Center);

            return true;
        }

        private void SpawnShardReleaseFlash(Vector2 targetCenter, int pairIndex)
        {
            if (Main.dedServ)
                return;

            Vector2 toTarget = (targetCenter - Projectile.Center).SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = toTarget.RotatedByRandom(0.5f) * Main.rand.NextFloat(2.8f, 7.5f);
                Color color = Main.rand.NextBool(3) ? RadiantWhite : RadiantGold;
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), velocity, false,
                    Main.rand.Next(12, 18), Main.rand.NextFloat(0.24f, 0.38f),
                    color with { A = 0 }, false, false, false));
            }

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, Vector2.Zero, RadiantGold with { A = 0 },
                new Vector2(1.25f + pairIndex * 0.18f, 1.25f + pairIndex * 0.18f), toTarget.ToRotation(), 0.5f, 0.08f, 14));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center, Vector2.Zero, RadiantWhite with { A = 0 },
                "CalamityMod/Particles/BloomCircle",
                Vector2.One, 0f, 0.32f, 0.08f, 16));
        }

        private void SpawnShardTelegraph(Vector2 spawnPosition, Vector2 aimPosition, int shotIndex)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = (aimPosition - spawnPosition).SafeNormalize(Vector2.UnitY);
            Vector2 right = direction.RotatedBy(MathHelper.PiOver2);
            Color warningGold = Color.Lerp(RadiantGold, Color.White, 0.18f) with { A = 0 };
            Color warningOrange = new Color(255, 156, 42, 0);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                aimPosition,
                Vector2.Zero,
                warningGold * 0.78f,
                new Vector2(0.75f, 0.75f),
                direction.ToRotation(),
                0.18f,
                0.85f,
                18));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                aimPosition,
                Vector2.Zero,
                warningOrange * 0.42f,
                new Vector2(1.2f, 0.42f),
                direction.ToRotation() + MathHelper.PiOver2,
                0.26f,
                1.35f,
                22));

            for (int i = 0; i < 4; i++)
            {
                float progress = (i + 1f) / 5f;
                Vector2 linePos =
                    Vector2.Lerp(spawnPosition, aimPosition, progress)
                    + right * (float)Math.Sin(progress * MathHelper.TwoPi + shotIndex) * 12f;

                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    linePos,
                    direction * Main.rand.NextFloat(2.6f, 4.8f),
                    false,
                    Main.rand.Next(14, 20),
                    Main.rand.NextFloat(0.16f, 0.24f),
                    Color.Lerp(warningGold, warningOrange, progress) * MathHelper.Lerp(0.3f, 0.85f, progress)));
            }
        }

        private void FireScissorPair(Player owner)
        {
            NPC target = FindTarget(owner, 1500f);
            Vector2 baseDir = target != null
                ? (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY)
                : Vector2.UnitY;

            Vector2 dir1 = baseDir.RotatedBy(-MathHelper.Pi / 3f);
            Vector2 dir2 = -dir1;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                dir1,
                ModContent.ProjectileType<PristineFuryCrystalLaser>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                LaserRotPerFrame,
                Projectile.whoAmI);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                dir2,
                ModContent.ProjectileType<PristineFuryCrystalLaser>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                -LaserRotPerFrame,
                Projectile.whoAmI);

            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.7f, Pitch = -0.1f }, Projectile.Center);
        }

        private static NPC FindTarget(Player owner, float maxRange)
        {
            NPC best = null;
            float bestDistSq = maxRange * maxRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distSq = Vector2.DistanceSquared(owner.Center, npc.Center);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = npc;
                }
            }
            return best;
        }

        private void SpawnIntroExplosion()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 7; i++)
            {
                GeneralParticleHandler.SpawnParticle(new CustomPulse(
                    Projectile.Center, Vector2.Zero, RadiantGold,
                    "CalamityMod/Particles/BlastCone",
                    new Vector2(Main.rand.NextFloat(2.5f, 6f), 3f),
                    Main.rand.NextFloat(MathHelper.TwoPi), 1f, 0.5f, 20));
            }
            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new CustomPulse(
                    Projectile.Center, Vector2.Zero, RadiantWhite,
                    "CalamityMod/Particles/BloomCircle",
                    Vector2.One, 0f, 3f, 0f, 35));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!IntroComplete)
            {
                DrawIntroHalves(lightColor);
                return false;
            }

            DrawFullCrystal(lightColor);
            return false;
        }

        private void DrawIntroHalves(Color lightColor)
        {
            Texture2D halvesTexture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/ProvidenceCrystal_Halves").Value;
            float p = IntroProgress;
            float circIn = CalamityUtils.CircInEasing(p, 1);
            float sineBump = CalamityUtils.SineBumpEasing(p, 1);
            float separation = MathHelper.Lerp(60f, 0f, circIn - sineBump) * DrawScale;
            float rotAngle = MathHelper.ToRadians(MathHelper.Lerp(70f, 0f, p));
            float dirAngle = MathHelper.ToRadians(MathHelper.Lerp(0f, -90f, circIn));

            for (int i = 0; i < 2; i++)
            {
                float dir = i == 0 ? -1f : 1f;
                Vector2 halfOffset = new Vector2(dir * separation, 0f).RotatedBy(dirAngle);
                Vector2 drawPos = Projectile.Center + halfOffset - Main.screenPosition;
                Rectangle source = new Rectangle(i * 50, 0, 50, halvesTexture.Height);
                Color c = Projectile.GetAlpha(lightColor);
                Main.EntitySpriteDraw(halvesTexture, drawPos, source, c, rotAngle, new Vector2(25f, halvesTexture.Height * 0.5f), DrawScale, SpriteEffects.None, 0);
            }
        }

        private void DrawFullCrystal(Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Color crystalColor = Projectile.GetAlpha(lightColor);

            float time = Main.GlobalTimeWrappedHourly;
            float pulse = (float)Math.Cos(time * MathHelper.TwoPi * 0.3f);
            float radius = 3.5f + pulse * 1.5f;
            for (float i = 0f; i < 4f; i += 0.5f)
            {
                float angle = i * MathHelper.PiOver2 + time * 1.8f;
                Vector2 offset = angle.ToRotationVector2() * radius;
                Color ghostColor = (RadiantGold with { A = 0 }) * 0.65f;
                Main.EntitySpriteDraw(texture, center + offset, texture.Frame(), ghostColor, Projectile.rotation, texture.Frame().Center(), DrawScale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, center, texture.Frame(), crystalColor, Projectile.rotation, texture.Frame().Center(), DrawScale, SpriteEffects.None, 0);
        }

        public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, 220);
    }
}
