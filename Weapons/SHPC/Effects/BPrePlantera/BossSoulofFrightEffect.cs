using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Particles;
using CalamityLegendsComeBack.Weapons.SHPC;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class BossSoulofFrightEffect : DefaultEffect
    {
        private const float MaxTravelDistance = 18f * 16f;
        private const int SplitCount = 9;
        private const int SecondaryCount = 3;
        private const int MaxDamagingHits = 3;

        public override int EffectID => 12;
        public override int AmmoType => ItemID.SoulofFright;

        // ===== 赤红色 =====
        public override Color ThemeColor => new Color(200, 40, 40);
        public override Color StartColor => new Color(255, 80, 80);
        public override Color EndColor => new Color(120, 10, 10);

        public override bool EnableDefaultSlowdown => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.localAI[0] = 0f;
            projectile.localAI[1] = 0f;
            projectile.ai[1] = 0f;
            projectile.ai[2] = 0f;
            projectile.timeLeft = System.Math.Max(45, (int)(MaxTravelDistance / System.Math.Max(projectile.velocity.Length(), 1f)) + 30);
            projectile.penetrate = -1;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 14;
            projectile.Resize(48, 48);
        }

        public override bool? CanDamage(Projectile projectile, Player owner) => projectile.localAI[1] < MaxDamagingHits ? null : false;

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.ai[1] = 0f;
            projectile.ai[2] = 0f;
            projectile.Resize(48, 48);
            projectile.localAI[0] += projectile.velocity.Length();

            if (projectile.owner != Main.myPlayer || projectile.localAI[0] < MaxTravelDistance)
                return;

            projectile.Kill();
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 1.54f;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            projectile.localAI[1]++;
        }

        internal static int GetSplitDamage(Projectile projectile) => System.Math.Max(1, (int)(projectile.damage * 0.88f));

        internal static void SpawnEvenSplit(Projectile projectile, int splitCount)
        {
            float baseAngle = projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
            float offset = Main.rand.NextFloat(MathHelper.TwoPi / splitCount);
            int splitDamage = GetSplitDamage(projectile);

            for (int i = 0; i < splitCount; i++)
            {
                float angle = baseAngle + offset + MathHelper.TwoPi * i / splitCount;
                Vector2 direction = angle.ToRotationVector2();
                float speed = 7.5f + (i % 2) * 1.2f;

                int soulIndex = Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + direction * 18f,
                    direction * speed,
                    ModContent.ProjectileType<NewSHPS>(),
                    splitDamage,
                    projectile.knockBack,
                    projectile.owner,
                    3);

                if (Main.projectile.IndexInRange(soulIndex))
                    Main.projectile[soulIndex].timeLeft = 105;
            }
        }

        private static void SpawnFrightExplosions(Projectile projectile)
        {
            int numExplosions = Main.rand.Next(5, 8); // 5 to 7
            List<Vector2> placedCenters = new();

            // 1. 优先以敌人为中心进行产生 (锁敌范围为 50 格方块，即 800 像素)
            float detectRange = 800f;
            List<NPC> targetNPCs = new();
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.CanBeChasedBy(projectile) && Vector2.Distance(npc.Center, projectile.Center) <= detectRange)
                {
                    targetNPCs.Add(npc);
                }
            }

            // 按距离排序，优先选择较近的敌人
            targetNPCs.Sort((n1, n2) => Vector2.Distance(n1.Center, projectile.Center).CompareTo(Vector2.Distance(n2.Center, projectile.Center)));

            foreach (NPC npc in targetNPCs)
            {
                if (placedCenters.Count >= numExplosions)
                    break;

                Vector2 candidate = npc.Center;
                bool overlap = false;
                foreach (Vector2 placed in placedCenters)
                {
                    if (Vector2.Distance(candidate, placed) < 224f) // 边长 224，因此最小距离保持在 224
                    {
                        overlap = true;
                        break;
                    }
                }

                if (!overlap)
                {
                    placedCenters.Add(candidate);
                }
            }

            // 2. 如果爆炸位置不足，在弹幕周围随机生成，同时确保不重叠
            int attempts = 0;
            while (placedCenters.Count < numExplosions && attempts < 150)
            {
                attempts++;
                Vector2 offset = Main.rand.NextVector2Circular(400f, 400f);
                if (offset.Length() < 90f)
                    offset = offset.SafeNormalize(Vector2.UnitY) * 90f;

                Vector2 candidate = projectile.Center + offset;

                bool overlap = false;
                foreach (Vector2 placed in placedCenters)
                {
                    if (Vector2.Distance(candidate, placed) < 224f)
                    {
                        overlap = true;
                        break;
                    }
                }

                if (!overlap)
                {
                    placedCenters.Add(candidate);
                }
            }

            // 兜底逻辑：如果尝试后依然不够，至少放入中心点
            if (placedCenters.Count == 0)
            {
                placedCenters.Add(projectile.Center);
            }

            // 3. 产生爆炸 (NewLegendSHPE) 弹幕并设置属性
            int damage = (int)(projectile.damage * 1.5f);
            foreach (Vector2 center in placedCenters)
            {
                int idx = Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    center,
                    Vector2.Zero,
                    ModContent.ProjectileType<NewLegendSHPE>(),
                    damage,
                    projectile.knockBack,
                    projectile.owner
                );

                if (Main.projectile.IndexInRange(idx))
                {
                    Projectile explosion = Main.projectile[idx];
                    explosion.width = 224;
                    explosion.height = 224;
                    explosion.Center = center;
                    explosion.DamageType = DamageClass.Magic;
                    explosion.netUpdate = true;
                }
            }
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/CalamitasClone/CalClone_Explosion", 3), projectile.Center);

            if (projectile.owner == Main.myPlayer)
            {
                SpawnFrightExplosions(projectile);
            }

            // ===== 光粒子核心：中心炸亮，制造"灵魂爆裂"感 =====
            for (int i = 0; i < 16; i++)
            {
                Vector2 dir = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 6f);

                SquishyLightParticle light = new(
                    projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    dir,
                    Main.rand.NextFloat(0.45f, 0.95f),
                    Color.Lerp(ThemeColor, StartColor, Main.rand.NextFloat(0.2f, 0.6f)),
                    Main.rand.Next(18, 30),
                    opacity: 1f,
                    squishStrenght: Main.rand.NextFloat(0.8f, 1.35f),
                    maxSquish: Main.rand.NextFloat(2.2f, 3.8f),
                    hueShift: 0f
                );

                GeneralParticleHandler.SpawnParticle(light);
            }

            // ===== 外层光粒子：向外拉出一圈凌厉感 =====
            for (int i = 0; i < 10; i++)
            {
                Vector2 dir = Main.rand.NextVector2Unit();
                Vector2 spawnPos = projectile.Center + dir * Main.rand.NextFloat(12f, 26f);
                Vector2 velocity = dir * Main.rand.NextFloat(3.5f, 7f);

                SquishyLightParticle light = new(
                    spawnPos,
                    velocity,
                    Main.rand.NextFloat(0.35f, 0.7f),
                    Color.Lerp(EndColor, ThemeColor, Main.rand.NextFloat(0.4f, 0.85f)),
                    Main.rand.Next(16, 24),
                    opacity: 1f,
                    squishStrenght: Main.rand.NextFloat(0.7f, 1.2f),
                    maxSquish: Main.rand.NextFloat(2f, 3.2f),
                    hueShift: 0f
                );

                GeneralParticleHandler.SpawnParticle(light);
            }

            // ===== 保留一个冲击波 =====
            Particle expandingPulse = new DirectionalPulseRing(
                projectile.Center,
                Vector2.Zero,
                ThemeColor,
                new Vector2(1.2f, 1.2f),
                0f,
                0.5f,
                6.0f,
                20
            );

            GeneralParticleHandler.SpawnParticle(expandingPulse);
        }



















    }

    internal class BossSoulofFright_SecondarySoul : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float DistanceTraveled => ref Projectile.localAI[0];
        private int SplitCount => System.Math.Max(1, (int)Projectile.ai[0]);
        private float MaxDistance => Projectile.ai[1] <= 0f ? 10f * 16f : Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 38;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            DistanceTraveled += Projectile.velocity.Length();
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Color(200, 40, 40).ToVector3() * 0.35f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.RedTorch,
                    -Projectile.velocity * Main.rand.NextFloat(0.08f, 0.24f),
                    120,
                    new Color(220, 45, 45),
                    Main.rand.NextFloat(0.65f, 1.05f));
                dust.noGravity = true;
            }

            if (Projectile.owner == Main.myPlayer && DistanceTraveled >= MaxDistance)
                Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
                BossSoulofFrightEffect.SpawnEvenSplit(Projectile, SplitCount);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                new Color(200, 40, 40),
                Vector2.One,
                0f,
                0.22f,
                1.8f,
                14));
        }
    }
}
