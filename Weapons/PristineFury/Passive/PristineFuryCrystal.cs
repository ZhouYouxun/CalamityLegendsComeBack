using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;

namespace CalamityLegendsComeBack.Weapons.PristineFury.Passive
{
    internal sealed class PristineFuryCrystal : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Boss/ProvidenceCrystal";

        private const float HoverHeight = 300f;
        private const float DrawScale = 0.45f;
        private const int ShardFireInterval = 16;   // 5枚一组，每16帧一次
        private const int ShardBurstCount = 5;
        private const float ShardInitSpeed = 22f;
        private const int LaserPairCooldown = 85;
        private const float LaserRotPerFrame = MathHelper.Pi / 160f;

        private ref float IntroProgress => ref Projectile.localAI[0];
        private ref float IntroSoundFired => ref Projectile.localAI[1];
        private ref float ShardTimer => ref Projectile.ai[0];
        private ref float LaserCooldown => ref Projectile.ai[1];

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

            // Position: directly above the player
            Projectile.Center = owner.Center + new Vector2(0f, -HoverHeight * owner.gravDir);

            // Intro animation progress
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

            // Idle sparkles
            if (!Main.dedServ && Main.rand.NextBool(14))
            {
                float hue = (Main.GlobalTimeWrappedHourly * 0.25f) % 1f;
                Color dustColor = Main.hslToRgb(hue, 1f, 0.65f);
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(36f, 36f),
                    DustID.RainbowMk2,
                    Main.rand.NextVector2Circular(0.6f, 0.6f),
                    100, dustColor, 0.6f);
                d.noGravity = true;
                d.fadeIn = 1f;
            }

            // Ambient light
            Lighting.AddLight(Projectile.Center, new Vector3(0.45f, 0.22f, 0.72f));

            if (Main.myPlayer != Projectile.owner)
                return;

            bool validMouse = !Main.mapFullscreen && !Main.blockMouse && !owner.mouseInterface;
            bool leftHeld = validMouse && Main.mouseLeft;
            bool rightHeld = validMouse && (Main.mouseRight || owner.Calamity().mouseRight);

            // Left held → fire homing shards
            if (leftHeld)
            {
                ShardTimer++;
                if (ShardTimer >= ShardFireInterval)
                {
                    ShardTimer = 0f;
                    FireShardBurst(owner);
                }
            }
            else
            {
                ShardTimer = 0f;
            }

            // Right held (not left) → maintain scissor lasers
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

        // 5枚星射：每次以随机基准角均匀分布在 360° 内弹出，各带独立色相；
        // 碎片各自随机减速飘移，减速结束后才惰性追踪同一目标从不同角度收束。
        private void FireShardBurst(Player owner)
        {
            NPC target = FindTarget(owner, 1500f);
            float targetIndex = target != null ? target.whoAmI + 1f : 0f;

            // 每次射击随机旋转整体星形，保证不单调
            float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);

            // 开火时水晶爆出彩色粒子环（与碎片方向对齐 + 中心闪光）
            if (!Main.dedServ)
            {
                for (int i = 0; i < ShardBurstCount; i++)
                {
                    float hue = (float)i / ShardBurstCount;
                    Color c = Main.hslToRgb(hue, 1f, 0.62f);
                    float angle = baseAngle + MathHelper.TwoPi * i / ShardBurstCount;
                    Vector2 burstVel = angle.ToRotationVector2() * Main.rand.NextFloat(4.5f, 7.5f);
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center, burstVel, false,
                        Main.rand.Next(10, 16), Main.rand.NextFloat(0.22f, 0.32f),
                        c with { A = 0 }, false, false, false));
                }
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center, Vector2.Zero, Color.White with { A = 0 },
                    new Vector2(1.4f, 1.4f), 0f, 0.55f, 0.06f, 12));
            }

            // 生成 5 枚碎片，均匀分布在 360° 内（从随机基准角出发）
            int shardType = ModContent.ProjectileType<PristineFuryCrystalShard>();
            for (int i = 0; i < ShardBurstCount; i++)
            {
                float angle = baseAngle + MathHelper.TwoPi * i / ShardBurstCount;
                Vector2 dir = angle.ToRotationVector2();
                float hue = (float)i / ShardBurstCount;

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    dir * ShardInitSpeed,
                    shardType,
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    hue,
                    targetIndex);
            }

            SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact with { Volume = 0.55f, Pitch = 0.4f }, Projectile.Center);
        }

        private void FireScissorPair(Player owner)
        {
            NPC target = FindTarget(owner, 1500f);
            Vector2 baseDir = target != null
                ? (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY)
                : Vector2.UnitY;

            // Start laser A offset by -60° from base, rotate clockwise
            Vector2 dir1 = baseDir.RotatedBy(-MathHelper.Pi / 3f);
            // Laser B is exactly opposite, rotates counter-clockwise
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
                    Projectile.Center, Vector2.Zero, Color.Violet,
                    "CalamityMod/Particles/BlastCone",
                    new Vector2(Main.rand.NextFloat(2.5f, 6f), 3f),
                    Main.rand.NextFloat(MathHelper.TwoPi), 1f, 0.5f, 20));
            }
            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new CustomPulse(
                    Projectile.Center, Vector2.Zero, Color.Violet,
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

            // Orbiting violet ghost copies
            float time = Main.GlobalTimeWrappedHourly;
            float pulse = (float)Math.Cos(time * MathHelper.TwoPi * 0.3f);
            float radius = 3.5f + pulse * 1.5f;
            for (float i = 0f; i < 4f; i += 0.5f)
            {
                float angle = i * MathHelper.PiOver2 + time * 1.8f;
                Vector2 offset = angle.ToRotationVector2() * radius;
                Color ghostColor = (Color.Violet with { A = 0 }) * 0.65f;
                Main.EntitySpriteDraw(texture, center + offset, texture.Frame(), ghostColor, Projectile.rotation, texture.Frame().Center(), DrawScale, SpriteEffects.None, 0);
            }

            // Main crystal
            Main.EntitySpriteDraw(texture, center, texture.Frame(), crystalColor, Projectile.rotation, texture.Frame().Center(), DrawScale, SpriteEffects.None, 0);
        }

        public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, 220);
    }
}
