using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    public class RuinousSoulEffect : DefaultEffect
    {
        private const int BaseLifetime = 2010;
        private const int MaxPenetrations = 4;
        private const int HomingSoulsPerHit = 2;
        private const int DeathHomingSoulCount = 4;
        private const int ColossusDeathHomingSoulCount = 8;
        private const int ColossusLifeThreshold = 1_000_000;

        public override int EffectID => 30;

        public override int AmmoType => ModContent.ItemType<RuinousSoul>();

        // ===== 鬼白主题 =====
        public override Color ThemeColor => new Color(220, 235, 255);
        public override Color StartColor => new Color(255, 255, 255);
        public override Color EndColor => new Color(150, 170, 205);

        public override float SquishyLightParticleFactor => 1.85f;
        public override float ExplosionPulseFactor => 1.85f;

        private static readonly Dictionary<int, int> nextOrbitOrderByOwner = new();

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            if (projectile.owner == Main.myPlayer)
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/EarthMeteor"), projectile.Center);

            // 初始速度三倍
            projectile.velocity *= 1.1f;
            projectile.extraUpdates = 3;
            projectile.timeLeft = 40;

            projectile.penetrate = MaxPenetrations;
            projectile.tileCollide = false;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 50;
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            // ===== 常规飞行：保留本体速度风格 =====
            projectile.velocity *= 1.02f;
            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.28f);
        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        public override bool? CanHitNPC(Projectile projectile, Player owner, NPC target)
        {
            if (!AnyBossActive())
                return null;

            return target.boss ? null : false;
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            projectile.netUpdate = true;

            // ===== 命中音效 =====
            SoundEngine.PlaySound(SoundID.Item103 with
            {
                Volume = 0.8f,
                Pitch = 0.05f,
                PitchVariance = 0.18f
            }, target.Center);

            Vector2 forward = (target.Center - projectile.Center).SafeNormalize(Vector2.UnitX);
            SpawnRuinousGhostExplosionVisuals(target.Center);

            if (projectile.owner == Main.myPlayer)
            {
                SpawnGhastlyVisageShards(projectile, target, HomingSoulsPerHit);
                SpawnOrReleaseOrbitGhosts(projectile);

                if (target.lifeMax > ColossusLifeThreshold)
                {
                    projectile.localAI[2] = 1f;
                    projectile.Kill();
                    return;
                }
            }

            // ===== 命中时：扇形 SquishyLightParticle（鬼白）=====
            for (int i = 0; i < 14; i++)
            {
                float factor = i / 13f;
                float angle = MathHelper.Lerp(-(MathHelper.Pi / 3f), MathHelper.Pi / 3f, factor);
                Vector2 velocity = forward.RotatedBy(angle) * Main.rand.NextFloat(2.5f, 7.5f);

                SquishyLightParticle particle = new(
                    target.Center + velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(0f, 8f),
                    velocity,
                    Main.rand.NextFloat(0.45f, 0.95f),
                    Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat(0.15f, 0.75f)),
                    Main.rand.Next(18, 28)
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            // ===== 命中外圈线性粒子（保留本文件风格，但改为鬼白）=====
            int points = 14;
            float radians = MathHelper.TwoPi / points;
            Vector2 spinningPoint = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));

            for (int i = 0; i < points; i++)
            {
                Vector2 dir = spinningPoint.RotatedBy(radians * i);
                LineParticle line = new(
                    target.Center + dir * 10f,
                    dir * Main.rand.NextFloat(3f, 9f),
                    false,
                    Main.rand.Next(16, 24),
                    Main.rand.NextFloat(0.4f, 0.8f),
                    Color.Lerp(ThemeColor, EndColor, Main.rand.NextFloat())
                );
                GeneralParticleHandler.SpawnParticle(line);
            }

            // ===== Dust补层（鬼白）=====
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    target.Center,
                    DustID.GemDiamond,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 4.8f),
                    0,
                    Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1f, 1.6f)
                );
                dust.noGravity = true;
            }

            // ===== 额外效果：释放夺舍后的毁灭幽魂 =====
            for (int i = 0; i < 0; i++)
            {
                // ===== 在命中点下方随机生成位置 =====
                Vector2 spawnPos = target.Center + new Vector2(
                    Main.rand.NextFloat(-6f * 16f, -15f * 16f),   // 左右随机
                    Main.rand.NextFloat(12f * 16f, 32f * 16f)     // 下方偏移
                );

                // ===== 向上发射，带一点散射 =====
                Vector2 shootDir = -Vector2.UnitY.RotatedByRandom(0.25f);
                Vector2 shootVelocity = shootDir * Main.rand.NextFloat(9f, 12f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    spawnPos,
                    shootVelocity,
                    ModContent.ProjectileType<RuinousSoul_OrbitGhost>(),
                    (int)(projectile.damage * 0.90f),
                    1f,
                    projectile.owner,
                    0f,
                    -1f,
                    i
                );

                // ===== 朝该方向喷射鬼魂线性特效 =====
                for (int j = 0; j < 5; j++)
                {
                    Vector2 lineVelocity = shootDir.RotatedByRandom(0.22f) * Main.rand.NextFloat(2.5f, 6.5f);

                    LineParticle line = new(
                        target.Center + shootDir * Main.rand.NextFloat(4f, 10f),
                        lineVelocity,
                        false,
                        Main.rand.Next(16, 26),
                        Main.rand.NextFloat(0.45f, 0.85f),
                        Color.Lerp(StartColor, EndColor, Main.rand.NextFloat(0.1f, 0.65f))
                    );
                    GeneralParticleHandler.SpawnParticle(line);
                }

                for (int j = 0; j < 6; j++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        target.Center + shootDir * Main.rand.NextFloat(2f, 8f),
                        DustID.SpectreStaff,
                        shootDir.RotatedByRandom(0.35f) * Main.rand.NextFloat(1.2f, 4.2f),
                        0,
                        Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat(0.2f, 0.8f)),
                        Main.rand.NextFloat(0.95f, 1.35f)
                    );
                    dust.noGravity = true;
                }
            }

        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (projectile.owner == Main.myPlayer)
            {
                int soulCount = projectile.localAI[2] > 0f ? ColossusDeathHomingSoulCount : DeathHomingSoulCount;
                SpawnDeathHomingSouls(projectile, soulCount);
            }
        }

        // ========================= 工具区 =========================

        private static void SpawnGhastlyVisageShards(Projectile projectile, NPC target, int count)
        {
            Vector2 forward = projectile.velocity.SafeNormalize((target.Center - projectile.Center).SafeNormalize(Vector2.UnitX));
            SpawnHomingSoulBurst(projectile, target.Center, forward, count, target.whoAmI, 0.88f, 0.72f);
        }

        private static void SpawnDeathHomingSouls(Projectile projectile, int count)
        {
            int targetIndex = FindClosestHomingTarget(projectile);
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            SpawnHomingSoulBurst(projectile, projectile.Center, forward, count, targetIndex, 0.77f, MathHelper.TwoPi);
        }

        private static void SpawnHomingSoulBurst(Projectile projectile, Vector2 center, Vector2 forward, int count, int targetIndex, float damageMultiplier, float totalSpread)
        {
            count = System.Math.Max(1, count);
            float startAngle = forward.ToRotation();
            float randomOffset = totalSpread >= MathHelper.TwoPi
                ? Main.rand.NextFloat(MathHelper.TwoPi / count)
                : 0f;

            for (int i = 0; i < count; i++)
            {
                float angle = totalSpread >= MathHelper.TwoPi
                    ? startAngle + randomOffset + MathHelper.TwoPi * i / count
                    : startAngle + MathHelper.Lerp(-totalSpread, totalSpread, count == 1 ? 0.5f : i / (count - 1f));

                Vector2 direction = angle.ToRotationVector2();
                Vector2 velocity = direction * Main.rand.NextFloat(8f, 13.5f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    center + direction * 8f,
                    velocity,
                    ModContent.ProjectileType<RuinousSoul_GhastlyES>(),
                    (int)(projectile.damage * 1.13f),
                    projectile.knockBack * 0.5f,
                    projectile.owner,
                    targetIndex,
                    i);
            }
        }

        private static int FindClosestHomingTarget(Projectile projectile)
        {
            int targetIndex = -1;
            float bestDistance = 1450f;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(projectile))
                    continue;

                float distance = projectile.Distance(npc.Center);
                if (npc.boss)
                    distance *= 0.74f;

                if (distance >= bestDistance)
                    continue;

                targetIndex = npc.whoAmI;
                bestDistance = distance;
            }

            return targetIndex;
        }

        private static void SpawnRuinousGhostExplosionVisuals(Vector2 center)
        {
            Lighting.AddLight(center, new Color(200, 220, 255).ToVector3() * 0.45f);

            if (Main.dedServ)
                return;

            for (int i = 0; i < 26; i++)
            {
                float angle = MathHelper.TwoPi * i / 26f;
                float xScale = 1f + 0.22f * (float)System.Math.Sin(angle * 3f);
                Vector2 offset = new Vector2((float)System.Math.Cos(angle) * 54f * xScale, (float)System.Math.Sin(angle) * 76f);
                Vector2 velocity = offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(2f, 6f);

                SquishyLightParticle particle = new(
                    center + offset,
                    velocity,
                    Main.rand.NextFloat(0.45f, 0.9f),
                    Color.Lerp(new Color(240, 250, 255), new Color(130, 160, 220), Main.rand.NextFloat()),
                    Main.rand.Next(18, 28)
                );
                GeneralParticleHandler.SpawnParticle(particle);
            }

            for (int i = 0; i < 10; i++)
            {
                Vector2 eyeOffset = new Vector2(i < 5 ? -22f : 22f, -22f) + Main.rand.NextVector2Circular(4f, 4f);
                Dust eye = Dust.NewDustPerfect(
                    center + eyeOffset,
                    DustID.SpectreStaff,
                    Main.rand.NextVector2Circular(1.5f, 1.5f),
                    0,
                    Color.White,
                    Main.rand.NextFloat(1f, 1.4f));
                eye.noGravity = true;
            }
        }

        private static void SpawnOrReleaseOrbitGhosts(Projectile projectile)
        {
            int ghostType = ModContent.ProjectileType<RuinousSoul_OrbitGhost>();
            List<Projectile> orbitGhosts = new();

            foreach (Projectile ghost in Main.ActiveProjectiles)
            {
                if (ghost.owner == projectile.owner && ghost.type == ghostType && ghost.ai[0] == 0f)
                    orbitGhosts.Add(ghost);
            }

            int ghostsToRelease = orbitGhosts.Count + RuinousSoul_OrbitGhost.SpawnBatchSize - RuinousSoul_OrbitGhost.ReleaseCap;
            if (ghostsToRelease > 0)
            {
                // localAI is not synchronized.  Using it here made the release order
                // undefined outside of the owner client, so a full orbit could keep
                // selecting the wrong ghosts. ai[2] is the synchronized spawn order.
                orbitGhosts.Sort((a, b) => a.ai[2].CompareTo(b.ai[2]));

                int targetIndex = FindReleaseTarget(projectile);
                for (int i = 0; i < ghostsToRelease && i < orbitGhosts.Count; i++)
                {
                    if (orbitGhosts[i].ModProjectile is RuinousSoul_OrbitGhost orbitGhost)
                        orbitGhost.Release(targetIndex);
                }
            }

            Player owner = Main.player[projectile.owner];
            for (int i = 0; i < RuinousSoul_OrbitGhost.SpawnBatchSize; i++)
            {
                int orbitOrder = GetNextOrbitOrder(projectile.owner);
                int orbitSlot = orbitOrder % RuinousSoul_OrbitGhost.ReleaseCap;
                float spawnAngle = MathHelper.TwoPi * orbitSlot / RuinousSoul_OrbitGhost.ReleaseCap;
                Vector2 spawnOffset = new Vector2(0f, -30f).RotatedBy(spawnAngle);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    owner.Center + spawnOffset,
                    Vector2.Zero,
                    ghostType,
                    (int)(projectile.damage * 0.81f),
                    projectile.knockBack,
                    projectile.owner,
                    0f,
                    -1f,
                    orbitOrder);
            }
        }

        private static int GetNextOrbitOrder(int owner)
        {
            nextOrbitOrderByOwner.TryGetValue(owner, out int nextOrder);
            nextOrbitOrderByOwner[owner] = nextOrder + 1;
            return nextOrder;
        }

        private static int FindReleaseTarget(Projectile projectile)
        {
            int targetIndex = -1;
            float bestDistance = 1400f;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(projectile))
                    continue;

                float distance = Vector2.Distance(Main.MouseWorld, npc.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    targetIndex = npc.whoAmI;
                }
            }

            return targetIndex;
        }

        private static bool AnyBossActive()
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.active && npc.boss && !npc.friendly && !npc.dontTakeDamage)
                    return true;
            }

            return false;
        }

     
    }
}
