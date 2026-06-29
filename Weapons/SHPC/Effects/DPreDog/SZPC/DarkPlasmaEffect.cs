using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC
{
    public class DarkPlasmaEffect : DefaultEffect
    {
        public const int BlackHoleLifetime = 420;
        private const int BlackHoleHitCooldown = 8;
        private const int MultiStarHitCooldown = 4;
        private const int MultiStarCollisionSize = 160;
        private const float MultiStarMergeDistance = 160f;
        private const float BinaryOrbitRadius = MultiStarCollisionSize * 0.5f;
        private const float TrinaryHeight = 160f;
        private const float TrinaryOrbitRadius = TrinaryHeight * 2f / 3f;
        private const float OrbReleaseIntervalMultiplier = 2f;
        private const float MultiStarExplosionDamagePerBlackHole = 2.5f;
        private const float MultiStarAngularVelocity = 1.7f / 60f;

        public override int EffectID => 32;

        public override int AmmoType => ModContent.ItemType<DarkPlasma>();

        public override Color ThemeColor => new Color(20, 20, 20);
        public override Color StartColor => new Color(80, 80, 80);
        public override Color EndColor => new Color(5, 5, 5);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;

        public override bool EnableDefaultSlowdown => false;
        public override bool EnableProximityExplosion => false;
        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.localAI[0] = 0f;

            DarkPlasma_GP gp = projectile.GetGlobalProjectile<DarkPlasma_GP>();
            gp.ResetForNewBlackHole(projectile.Center);

            projectile.velocity *= 0.8f;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = BlackHoleLifetime;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = BlackHoleHitCooldown;

            // 出生时先来一圈黑暗塌缩感
            for (int i = 0; i < 14; i++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 spawnPos = projectile.Center + dir * Main.rand.NextFloat(18f, 46f);
                Vector2 vel = (projectile.Center - spawnPos).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.5f, 4.5f);

                Dust dust = Dust.NewDustPerfect(
                    spawnPos,
                    ModContent.DustType<VoidDustInverted>(),
                    vel,
                    0,
                    Color.Lerp(new Color(90, 90, 90), Color.Black, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.8f, 1.35f)
                );
                dust.noGravity = true;
                dust.noLightEmittence = true;
            }

            Particle openPulse = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                new Color(35, 35, 35),
                "CalamityMod/Particles/SmallBloom",
                Vector2.One,
                Main.rand.NextFloat(-0.15f, 0.15f),
                0.9f,
                0f,
                22,
                false
            );
            GeneralParticleHandler.SpawnParticle(openPulse);
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            DarkPlasma_GP gp = projectile.GetGlobalProjectile<DarkPlasma_GP>();
            if (gp.releaseOnly)
            {
                projectile.Kill();
                return;
            }

            if (gp.suppressDeathEffects)
                return;

            if (projectile.timeLeft == 120 && !Main.dedServ)
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftBuilding"), projectile.Center);

            gp.portalTimer += 0.03f;
            gp.lifeTimer++;

            TryMergeWithNearbyBlackHoles(projectile, owner, gp);
            if (gp.suppressDeathEffects)
                return;

            bool inMultiStar = UpdateMultiStarSystem(projectile, owner, gp);
            Vector2 gravityCenter = inMultiStar ? gp.systemCenter : projectile.Center;
            bool handlesSystemDamage = !inMultiStar || IsMultiStarDamageOwner(projectile, gp);

            // ===== 缓慢追踪最近敌人 =====
            if (!inMultiStar)
            {
                NPC blackHoleTarget = FindBlackHoleTarget(projectile.Center, 1800f);
                if (blackHoleTarget is not null)
                {
                    Vector2 toTarget = blackHoleTarget.Center - projectile.Center;
                    float dist = toTarget.Length();

                    if (dist > 10f)
                    {
                        Vector2 dir = toTarget / dist;
                        projectile.velocity = (projectile.velocity * 25f + dir * 1.8f) / 26f;
                    }
                }
                else
                    projectile.velocity *= 0.985f;
            }

            projectile.rotation += inMultiStar ? MultiStarAngularVelocity : 0.045f;

            // ===== 吸附敌人 =====
            if (handlesSystemDamage)
            {
                bool bossAlive = BossIsAlive();
                float range = 1800f;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (!npc.active || npc.friendly || npc.dontTakeDamage)
                        continue;

                    float distance = Vector2.Distance(gravityCenter, npc.Center);
                    if (distance < range)
                    {
                        Vector2 pull = (gravityCenter - npc.Center).SafeNormalize(Vector2.UnitY);
                        float closeness = 1f - distance / range;

                        if (!bossAlive)
                        {
                            Vector2 desiredVelocity = pull * MathHelper.Lerp(1.8f, 9.5f, closeness);
                            npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, MathHelper.Lerp(0.025f, 0.18f, closeness));
                            if (distance < 120f)
                                npc.velocity *= 0.72f;
                        }

                        // 吸收轨迹 dust
                        if (Main.rand.NextBool(4))
                        {
                            Vector2 start = npc.Center + Main.rand.NextVector2Circular(24f, 24f);
                            Vector2 vel = (gravityCenter - start).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.5f, 5f);

                            Dust dust = Dust.NewDustPerfect(
                                start,
                                ModContent.DustType<VoidDustInverted>(),
                                vel,
                                0,
                                Color.Lerp(new Color(100, 100, 100), Color.Black, Main.rand.NextFloat()),
                                Main.rand.NextFloat(0.85f, 1.35f)
                            );
                            dust.noGravity = true;
                            dust.noLightEmittence = true;
                        }

                        // 稳定持续伤害
                        if (gp.lifeTimer % 8 == 0)
                        {
                            float damageScale = MathHelper.Lerp(0.75f, 3.4f, closeness);
                            npc.StrikeNPC(npc.CalculateHitInfo(Math.Max(1, (int)(projectile.damage / 10f * damageScale)), 0));
                        }
                    }
                }
            }

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -forward;

            // ===== 1. 黑色主尾焰：CustomSpark =====
            if (Main.myPlayer == projectile.owner && Main.rand.NextBool(2))
            {
                Particle spark = new CustomSpark(
                    projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    back.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.8f, 2.8f),
                    "CalamityMod/Particles/GlowSpark2",
                    false,
                    Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.02f, 0.045f),
                    Color.Black * 0.75f,
                    new Vector2(Main.rand.NextFloat(0.9f, 1.4f), Main.rand.NextFloat(0.3f, 0.65f)),
                    false,
                    shrinkSpeed: 1.05f
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // ===== 4. 黑烟吸收层：HeavySmokeParticle =====
            if (Main.rand.NextBool(3))
            {
                Particle smoke = new HeavySmokeParticle(
                    projectile.Center + Main.rand.NextVector2Circular(18f, 18f),
                    back.RotatedByRandom(0.7f) * Main.rand.NextFloat(0.15f, 1.1f),
                    Main.rand.NextBool(2) ? new Color(15, 15, 15) : new Color(45, 45, 45),
                    Main.rand.Next(18, 30),
                    Main.rand.NextFloat(0.55f, 1.05f),
                    0.42f,
                    Main.rand.NextFloat(-0.06f, 0.06f),
                    false
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            // ===== 5. 轨道虚空尘（随寿命增强）=====

            // 👉 归一化寿命（越接近死亡 → 越接近1）
            float lifeFactor = 1f - projectile.timeLeft / (float)BlackHoleLifetime;
            lifeFactor = MathHelper.Clamp(lifeFactor, 0f, 1f);

            // 👉 平滑强化（避免前期突变）
            lifeFactor = (float)Math.Pow(lifeFactor, 1.4f);

            // ===== 频率：最多提升到原来的 ~3倍 =====
            int spawnChance = (int)MathHelper.Lerp(2f, 1f, lifeFactor); // 2→1（更容易触发）

            if (Main.rand.NextBool(spawnChance))
            {
                Vector2 circle = Main.rand.NextVector2CircularEdge(1f, 1f);

                // ===== 距离范围：逐渐扩大，但有限制 =====
                float minDist = MathHelper.Lerp(20f, 40f, lifeFactor);
                float maxDist = MathHelper.Lerp(90f, 160f, lifeFactor);

                Vector2 spawn = projectile.Center + circle * Main.rand.NextFloat(minDist, maxDist);

                // ===== 吸引强度：明显增强，但封顶 =====
                float velStrength = MathHelper.Lerp(0.045f, 0.18f, lifeFactor);

                Vector2 vel = (projectile.Center - spawn) * velStrength;

                Dust dust = Dust.NewDustPerfect(
                    spawn,
                    ModContent.DustType<VoidDustInverted>(),
                    vel,
                    0,
                    Main.rand.NextBool(3) ? new Color(120, 120, 120) : Color.Black,
                    MathHelper.Lerp(0.7f, 1.8f, lifeFactor) // 尺寸也略增强
                );

                dust.noGravity = true;
                dust.noLightEmittence = true;
            }


            // ===== 2. 黑色高速裂流（随寿命外扩增强）=====

            // 👉 同一套寿命因子（和黑洞吸附形成对照）
            lifeFactor = 1f - projectile.timeLeft / (float)BlackHoleLifetime;
            lifeFactor = MathHelper.Clamp(lifeFactor, 0f, 1f);

            // 👉 这里用更激进的曲线（爆裂感）
            lifeFactor = (float)Math.Pow(lifeFactor, 1.2f);

            // ===== 触发频率提升（最多≈2倍）=====
            int interval = (int)MathHelper.Lerp(3f, 1f, lifeFactor);

            if (gp.lifeTimer % interval == 0)
            {
                // ===== 生成范围扩大（向外爆）=====
                float spawnRadius = MathHelper.Lerp(12f, 38f, lifeFactor);

                Vector2 spawnPos = projectile.Center + Main.rand.NextVector2Circular(spawnRadius, spawnRadius);

                // ===== 扩散角度变大 =====
                float spread = MathHelper.Lerp(0.65f, 1.4f, lifeFactor);

                // ===== 速度暴涨（核心）=====
                float speedMin = MathHelper.Lerp(1.2f, 3.5f, lifeFactor);
                float speedMax = MathHelper.Lerp(4.8f, 10.5f, lifeFactor);

                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(speedMin, speedMax);

                Particle altSpark = new AltSparkParticle(
                    spawnPos,
                    vel,
                    false,
                    (int)MathHelper.Lerp(10f, 22f, lifeFactor), // 生命周期略增加
                    MathHelper.Lerp(0.7f, 1.35f, lifeFactor),   // 尺寸增强
                    Color.Black
                );

                GeneralParticleHandler.SpawnParticle(altSpark);
            }


            // ===== 7. 中心暗核呼吸 =====
            if (gp.lifeTimer % 5 == 0)
            {
                Particle corePulse = new CustomPulse(
                    projectile.Center,
                    Vector2.Zero,
                    new Color(20, 20, 20),
                    "CalamityMod/Particles/SmallBloom",
                    Vector2.One,
                    Main.rand.NextFloat(-0.1f, 0.1f),
                    0.4f,
                    0f,
                    14,
                    false
                );
                GeneralParticleHandler.SpawnParticle(corePulse);
            }

            float orbReleaseFactor = MathHelper.Clamp(gp.lifeTimer / (float)BlackHoleLifetime, 0f, 1f);
            TryReleaseDevourOrbs(projectile, owner, (float)Math.Pow(orbReleaseFactor, 0.72f));
            Lighting.AddLight(projectile.Center, new Vector3(0.06f, 0.06f, 0.06f));
        }

        private static bool IsDarkPlasmaBlackHole(Projectile projectile, int owner)
        {
            if (!projectile.active || projectile.owner != owner)
                return false;

            if (projectile.type != ModContent.ProjectileType<NewLegendSHPB>() || (int)projectile.ai[0] != 32)
                return false;

            DarkPlasma_GP gp = projectile.GetGlobalProjectile<DarkPlasma_GP>();
            return !gp.releaseOnly && !gp.suppressDeathEffects;
        }

        private static void TryMergeWithNearbyBlackHoles(Projectile projectile, Player owner, DarkPlasma_GP gp)
        {
            if (gp.IsInMultiStar || !IsDarkPlasmaBlackHole(projectile, owner.whoAmI))
                return;

            Projectile candidate = null;
            bool candidateIsSystem = false;
            float bestDistance = MultiStarMergeDistance;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.whoAmI == projectile.whoAmI || !IsDarkPlasmaBlackHole(other, owner.whoAmI))
                    continue;

                DarkPlasma_GP otherGP = other.GetGlobalProjectile<DarkPlasma_GP>();
                float distanceToBody = Vector2.Distance(projectile.Center, other.Center);
                float distanceToSystem = otherGP.IsInMultiStar ? Vector2.Distance(projectile.Center, otherGP.systemCenter) : distanceToBody;
                float distance = Math.Min(distanceToBody, distanceToSystem);

                if (distance > bestDistance)
                    continue;

                bestDistance = distance;
                candidate = other;
                candidateIsSystem = otherGP.IsInMultiStar;
            }

            if (candidate == null)
                return;

            if (candidateIsSystem)
            {
                DarkPlasma_GP candidateGP = candidate.GetGlobalProjectile<DarkPlasma_GP>();
                List<Projectile> members = GetSystemMembers(owner.whoAmI, candidateGP.multiStarLeader);

                if (members.Count >= 3)
                    DetonateMultiStarSystem(projectile, members, members.Count + 1);
                else
                    AddBlackHoleToSystem(projectile, members);

                return;
            }

            CreateMultiStarSystem(projectile, candidate);
        }

        private static bool UpdateMultiStarSystem(Projectile projectile, Player owner, DarkPlasma_GP gp)
        {
            if (!gp.IsInMultiStar)
                return false;

            Projectile leader = GetLeaderProjectile(gp.multiStarLeader);
            if (leader == null || !IsDarkPlasmaBlackHole(leader, owner.whoAmI))
            {
                gp.ClearMultiStar();
                projectile.localNPCHitCooldown = BlackHoleHitCooldown;
                return false;
            }

            if (projectile.whoAmI == leader.whoAmI)
            {
                List<Projectile> members = GetSystemMembers(owner.whoAmI, gp.multiStarLeader);
                if (members.Count < 2)
                {
                    foreach (Projectile member in members)
                    {
                        DarkPlasma_GP memberGP = member.GetGlobalProjectile<DarkPlasma_GP>();
                        memberGP.ClearMultiStar();
                        member.localNPCHitCooldown = BlackHoleHitCooldown;
                    }

                    return false;
                }

                DarkPlasma_GP leaderGP = leader.GetGlobalProjectile<DarkPlasma_GP>();
                Vector2 center = leaderGP.systemCenter == Vector2.Zero ? AverageCenter(members) : leaderGP.systemCenter;
                Vector2 velocity = leaderGP.systemVelocity == Vector2.Zero ? AverageVelocity(members) : leaderGP.systemVelocity;

                NPC blackHoleTarget = FindBlackHoleTarget(center, 1800f);
                if (blackHoleTarget is not null)
                {
                    Vector2 toTarget = blackHoleTarget.Center - center;
                    float dist = toTarget.Length();

                    if (dist > 10f)
                    {
                        Vector2 dir = toTarget / dist;
                        velocity = (velocity * 25f + dir * 1.8f) / 26f;
                    }
                }
                else
                    velocity *= 0.985f;

                center += velocity;
                float angle = leaderGP.systemAngle + MultiStarAngularVelocity;
                AssignMultiStarMembers(members, center, velocity, angle, false);
            }

            ApplyMultiStarPlacement(projectile, gp);
            return gp.IsInMultiStar;
        }

        private static void CreateMultiStarSystem(Projectile first, Projectile second)
        {
            List<Projectile> members = new() { first, second };
            members.Sort((a, b) => a.whoAmI.CompareTo(b.whoAmI));

            Vector2 center = (first.Center + second.Center) * 0.5f;
            Vector2 velocity = (first.velocity + second.velocity) * 0.5f;
            float angle = (members[0].Center - center).SafeNormalize(Vector2.UnitX).ToRotation();

            AssignMultiStarMembers(members, center, velocity, angle, true);
            SpawnMultiStarJoinEffects(center, members.Count);
        }

        private static void AddBlackHoleToSystem(Projectile incoming, List<Projectile> members)
        {
            if (members.Count <= 0)
                return;

            DarkPlasma_GP leaderGP = members[0].GetGlobalProjectile<DarkPlasma_GP>();
            int newCount = members.Count + 1;
            Vector2 center = (leaderGP.systemCenter * members.Count + incoming.Center) / newCount;
            Vector2 velocity = (leaderGP.systemVelocity * members.Count + incoming.velocity) / newCount;
            float angle = leaderGP.systemAngle;

            members.Add(incoming);
            members.Sort((a, b) => a.whoAmI.CompareTo(b.whoAmI));

            AssignMultiStarMembers(members, center, velocity, angle, true);
            SpawnMultiStarJoinEffects(center, members.Count);
        }

        private static void AssignMultiStarMembers(List<Projectile> members, Vector2 center, Vector2 velocity, float angle, bool refreshLifetime)
        {
            if (members.Count <= 0)
                return;

            members.Sort((a, b) => a.whoAmI.CompareTo(b.whoAmI));
            int leader = members[0].whoAmI;
            int count = Math.Min(members.Count, 3);

            for (int i = 0; i < members.Count; i++)
            {
                Projectile member = members[i];
                DarkPlasma_GP memberGP = member.GetGlobalProjectile<DarkPlasma_GP>();
                memberGP.multiStarLeader = leader;
                memberGP.multiStarSlot = Math.Min(i, 2);
                memberGP.multiStarCount = count;
                memberGP.systemCenter = center;
                memberGP.systemVelocity = velocity;
                memberGP.systemAngle = angle;
                member.localNPCHitCooldown = MultiStarHitCooldown;

                if (refreshLifetime)
                {
                    member.timeLeft = BlackHoleLifetime;
                    member.localAI[0] = 0f;
                    memberGP.lifeTimer = 0;
                }

                ApplyMultiStarPlacement(member, memberGP);
                member.netUpdate = true;
            }
        }

        private static void ApplyMultiStarPlacement(Projectile projectile, DarkPlasma_GP gp)
        {
            if (!gp.IsInMultiStar)
                return;

            Vector2 offset = GetMultiStarOffset(gp.multiStarSlot, gp.multiStarCount, gp.systemAngle);
            projectile.Center = gp.systemCenter + offset;
            projectile.velocity = gp.systemVelocity + offset.RotatedBy(MathHelper.PiOver2) * MultiStarAngularVelocity;
            projectile.localNPCHitCooldown = MultiStarHitCooldown;
        }

        private static Vector2 GetMultiStarOffset(int slot, int count, float angle)
        {
            if (count <= 1)
                return Vector2.Zero;

            if (count == 2)
                return (angle + MathHelper.Pi * slot).ToRotationVector2() * BinaryOrbitRadius;

            return (angle - MathHelper.PiOver2 + MathHelper.TwoPi * slot / 3f).ToRotationVector2() * TrinaryOrbitRadius;
        }

        private static bool IsMultiStarDamageOwner(Projectile projectile, DarkPlasma_GP gp)
        {
            return gp.IsInMultiStar && projectile.whoAmI == gp.multiStarLeader;
        }

        private static Projectile GetLeaderProjectile(int leader)
        {
            if (leader < 0 || leader >= Main.maxProjectiles)
                return null;

            Projectile projectile = Main.projectile[leader];
            return projectile.active ? projectile : null;
        }

        private static List<Projectile> GetSystemMembers(int owner, int leader)
        {
            List<Projectile> members = new();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!IsDarkPlasmaBlackHole(projectile, owner))
                    continue;

                DarkPlasma_GP gp = projectile.GetGlobalProjectile<DarkPlasma_GP>();
                if (gp.multiStarLeader == leader)
                    members.Add(projectile);
            }

            members.Sort((a, b) => a.whoAmI.CompareTo(b.whoAmI));
            return members;
        }

        private static Vector2 AverageCenter(List<Projectile> projectiles)
        {
            Vector2 center = Vector2.Zero;
            foreach (Projectile projectile in projectiles)
                center += projectile.Center;

            return center / Math.Max(1, projectiles.Count);
        }

        private static Vector2 AverageVelocity(List<Projectile> projectiles)
        {
            Vector2 velocity = Vector2.Zero;
            foreach (Projectile projectile in projectiles)
                velocity += projectile.velocity;

            return velocity / Math.Max(1, projectiles.Count);
        }

        private static void DetonateMultiStarSystem(Projectile incoming, List<Projectile> members, int blackHoleCount)
        {
            if (members.Count <= 0)
                return;

            DarkPlasma_GP leaderGP = members[0].GetGlobalProjectile<DarkPlasma_GP>();
            Vector2 center = leaderGP.systemCenter == Vector2.Zero ? AverageCenter(members) : leaderGP.systemCenter;
            Player owner = Main.player[incoming.owner];
            int count = Math.Min(Math.Max(blackHoleCount, 2), 4);

            foreach (Projectile member in members)
                member.GetGlobalProjectile<DarkPlasma_GP>().suppressDeathEffects = true;

            incoming.GetGlobalProjectile<DarkPlasma_GP>().suppressDeathEffects = true;

            SpawnMultiStarDetonation(incoming, owner, center, count);

            foreach (Projectile member in members)
            {
                if (member.active)
                    member.Kill();
            }

            if (incoming.active)
                incoming.Kill();
        }

        private static void SpawnMultiStarJoinEffects(Vector2 center, int count)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.48f + count * 0.08f, Pitch = -0.35f }, center);

            for (int i = 0; i < 18 + count * 6; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 8f);
                Dust dust = Dust.NewDustPerfect(
                    center,
                    ModContent.DustType<VoidDustInverted>(),
                    velocity,
                    0,
                    Color.Lerp(new Color(80, 80, 80), Color.Black, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.05f, 1.8f));
                dust.noGravity = true;
                dust.noLightEmittence = true;
            }
        }

        private static bool BossIsAlive()
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.boss && !npc.friendly && !npc.dontTakeDamage)
                    return true;
            }

            return false;
        }

        private static NPC FindBlackHoleTarget(Vector2 center, float range)
        {
            NPC bestTarget = null;
            float bestScore = range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.Distance(center, npc.Center);
                if (distance >= bestScore)
                    continue;

                bestScore = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        private static void TryReleaseDevourOrbs(Projectile projectile, Player owner, float lifeFactor)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            projectile.localAI[0]++;
            lifeFactor = MathHelper.Clamp(lifeFactor, 0f, 1f);
            int interval = Math.Max(4, (int)(MathHelper.Lerp(7f, 2f, lifeFactor) * OrbReleaseIntervalMultiplier));
            if ((int)projectile.localAI[0] % interval != 0)
                return;

            int count = 1;
            float phase = projectile.localAI[0] * MathHelper.Lerp(0.16f, 0.52f, lifeFactor) + projectile.identity * 0.41f;
            for (int i = 0; i < count; i++)
            {
                float angle = phase + MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.24f, 0.24f);
                Vector2 direction = angle.ToRotationVector2();
                Vector2 velocity = direction.RotatedByRandom(MathHelper.Lerp(0.35f, 0.12f, lifeFactor)) *
                    Main.rand.NextFloat(8f, MathHelper.Lerp(13f, 23f, lifeFactor));
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + direction * Main.rand.NextFloat(14f, 28f),
                    velocity,
                    ModContent.ProjectileType<EndlessDevourJavOrbSmall>(),
                    (int)(projectile.damage * 0.25f),
                    projectile.knockBack * 0.4f,
                    projectile.owner,
                    0f,
                    Main.rand.NextFloat(MathHelper.TwoPi));

                if (!Main.dedServ)
                {
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(
                        projectile.Center + direction * 8f,
                        velocity * 0.28f,
                        "CalamityMod/Particles/VerticalSmear",
                        false,
                        Main.rand.Next(12, 18),
                        Main.rand.NextFloat(1.2f, 2f) * MathHelper.Lerp(0.75f, 1.35f, lifeFactor),
                        GetDarkPlasmaVisibleBurstColor(owner),
                        new Vector2(0.16f, 1f)));
                }
            }
        }

        // ================= ModifyHitNPC =================
        public override bool? CanDamage(Projectile projectile, Player owner)
        {
            DarkPlasma_GP gp = projectile.GetGlobalProjectile<DarkPlasma_GP>();
            if (gp.suppressDeathEffects)
                return false;

            if (gp.IsInMultiStar && !IsMultiStarDamageOwner(projectile, gp))
                return false;

            return null;
        }

        public override void ModifyDamageHitbox(Projectile projectile, Player owner, ref Rectangle hitbox)
        {
            DarkPlasma_GP gp = projectile.GetGlobalProjectile<DarkPlasma_GP>();
            if (!gp.IsInMultiStar || !IsMultiStarDamageOwner(projectile, gp))
                return;

            hitbox = new Rectangle(
                (int)(gp.systemCenter.X - MultiStarCollisionSize * 0.5f),
                (int)(gp.systemCenter.Y - MultiStarCollisionSize * 0.5f),
                MultiStarCollisionSize,
                MultiStarCollisionSize);
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.DefenseEffectiveness *= 0f;

            float dr = target.Calamity().DR;
            if (dr < 0.95f)
                modifiers.FinalDamage /= 1f - dr;
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            DarkPlasma_GP gp = projectile.GetGlobalProjectile<DarkPlasma_GP>();
            if (gp.suppressDeathEffects)
                return;

            if (gp.releaseOnly)
            {
                SpawnForwardDarkEnergyBurst(projectile);
                return;
            }

            if (gp.IsInMultiStar)
            {
                if (!IsMultiStarDamageOwner(projectile, gp))
                    return;

                List<Projectile> members = GetSystemMembers(owner.whoAmI, gp.multiStarLeader);
                int count = Math.Max(2, members.Count);
                Vector2 center = gp.systemCenter == Vector2.Zero ? projectile.Center : gp.systemCenter;

                foreach (Projectile member in members)
                {
                    if (member.whoAmI != projectile.whoAmI)
                        member.GetGlobalProjectile<DarkPlasma_GP>().suppressDeathEffects = true;
                }

                SpawnMultiStarDetonation(projectile, owner, center, count);

                foreach (Projectile member in members)
                {
                    if (member.whoAmI != projectile.whoAmI && member.active)
                        member.Kill();
                }

                return;
            }

            SpawnDarkPlasmaAccretionDeath(projectile, owner);
            SpawnDarkPlasmaDeathDamage(projectile);
            SpawnDarkPlasmaDeathOrbs(projectile);
            PlayDarkPlasmaDeathSounds(projectile);
        }

        private static void SpawnMultiStarDetonation(Projectile projectile, Player owner, Vector2 center, int blackHoleCount)
        {
            int count = Math.Min(Math.Max(blackHoleCount, 2), 4);
            float power = 1.95f + 0.35f * (count - 1);
            int explosionSize = MultiStarCollisionSize + 80 * count;
            float damageMultiplier = MultiStarExplosionDamagePerBlackHole * count;

            owner.SetScreenshake(8.5f + 2f * count);
            SpawnDarkPlasmaAccretionDeath(projectile, owner, center, power);
            SpawnDarkPlasmaDeathDamage(projectile, center, damageMultiplier, explosionSize, explosionSize);
            SpawnDarkPlasmaDeathOrbs(projectile, center, 10 + count * 8, 0.36f + count * 0.06f);
            PlayDarkPlasmaDeathSounds(projectile, center);
        }

        private static void SpawnDarkPlasmaAccretionDeath(Projectile projectile, Player owner)
        {
            SpawnDarkPlasmaAccretionDeath(projectile, owner, projectile.Center, 1.95f);
        }

        private static void SpawnDarkPlasmaAccretionDeath(Projectile projectile, Player owner, Vector2 center, float power)
        {
            owner.SetScreenshake(8.5f);
            float diskRotation = projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation() + Main.rand.NextFloat(-0.35f, 0.35f);

            for (int i = 0; i < 55; i++)
            {
                Color useColor = GetRandomDarkPlasmaBurstColor(owner);
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 radial = new Vector2((float)Math.Cos(angle) * 1.85f, (float)Math.Sin(angle) * 0.42f).RotatedBy(diskRotation).SafeNormalize(Vector2.UnitX);
                Vector2 tangent = radial.RotatedBy(MathHelper.PiOver2);
                Vector2 velocity = (tangent * Main.rand.NextFloat(4f, 11f) + radial * Main.rand.NextFloat(-2.6f, 4.6f)) * power;

                Dust dust = Dust.NewDustPerfect(
                    center,
                    Main.rand.NextBool(6) ? ModContent.DustType<VoidDustInverted>() : ModContent.DustType<VoidDust>(),
                    velocity);
                dust.noGravity = true;
                dust.noLightEmittence = true;
                dust.scale = Main.rand.NextFloat(1.75f, 2.25f) * power;
                dust.color = useColor;

                if (i % 2 == 0)
                {
                    Color sparkColor = GetDarkPlasmaVisibleBurstColor(owner);
                    Vector2 sparkVelocity = new Vector2(0f, -40f * power)
                        .RotatedByRandom(100f)
                        .RotatedBy(diskRotation)
                        * Main.rand.NextFloat(0.1f, 1f);
                    Particle spark = new CustomSpark(
                        center,
                        sparkVelocity,
                        "CalamityMod/Particles/Sparkle",
                        false,
                        40,
                        Main.rand.NextFloat(1.4f, 2.4f) * power,
                        sparkColor,
                        new Vector2(0.4f, 1.1f));
                    GeneralParticleHandler.SpawnParticle(spark);
                }
            }

            for (int i = 0; i < 3; i++)
            {
                Color useColor = GetDarkPlasmaVisibleBurstColor(owner);
                Particle softExplosion = new CustomPulse(
                    center,
                    Vector2.Zero,
                    useColor,
                    "CalamityMod/Particles/SoftRoundExplosion",
                    Vector2.One,
                    Main.rand.NextFloat(-10f, 10f),
                    0f,
                    0.4f - i * 0.03f * power,
                    13);
                GeneralParticleHandler.SpawnParticle(softExplosion);
            }

            //Particle accretionRing = new CustomPulse(
            //    projectile.Center,
            //    Vector2.Zero,
            //    Color.Black,
            //    "CalamityMod/Particles/BloomRing",
            //    new Vector2(1.75f, 0.48f),
            //    diskRotation,
            //    0.15f * power,
            //    2.5f * power,
            //    38,
            //    false);
            //GeneralParticleHandler.SpawnParticle(accretionRing);

            int parts = 10;
            float rot = Main.rand.NextFloat(-9f, 9f);
            for (int i = 0; i < parts; i++)
            {
                Color useColor = GetDarkPlasmaVisibleBurstColor(owner);
                Vector2 smearVelocity = new Vector2(0f, -15f * (i % 2 == 0 ? 1.8f : 1f) * power)
                    .RotatedBy(i * MathHelper.TwoPi / parts)
                    .RotatedBy(rot)
                    .RotatedBy(diskRotation);
                smearVelocity.X *= 1.65f;
                smearVelocity.Y *= 0.58f;

                Particle smear = new CustomSpark(
                    center,
                    smearVelocity,
                    "CalamityMod/Particles/VerticalSmearRagged",
                    false,
                    19,
                    3f * power,
                    useColor,
                    new Vector2(0.2f, 1f));
                GeneralParticleHandler.SpawnParticle(smear);
            }

            Particle blackCore = new CustomPulse(
                center,
                Vector2.Zero,
                Color.Black,
                "CalamityMod/Particles/SmallBloom",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0f,
                1.2f * power,
                39,
                false);
            GeneralParticleHandler.SpawnParticle(blackCore);
        }

        private static Color GetRandomDarkPlasmaBurstColor(Player owner)
        {
            if (owner.shirtColor == Color.White && Main.rand.NextBool(10))
                return new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB);

            return Main.rand.Next(5) switch
            {
                0 => Color.Black,
                1 => new Color(8, 8, 8),
                2 => new Color(18, 18, 18),
                3 => new Color(34, 34, 34),
                _ => new Color(2, 2, 2)
            };
        }

        private static Color GetDarkPlasmaVisibleBurstColor(Player owner)
        {
            if (owner.shirtColor == Color.White && Main.rand.NextBool(10))
                return new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB);

            return Main.rand.Next(4) switch
            {
                0 => new Color(26, 26, 26),
                1 => new Color(38, 38, 38),
                2 => new Color(18, 18, 24),
                _ => new Color(46, 46, 52)
            };
        }

        private static void SpawnDarkPlasmaDeathDamage(Projectile projectile)
        {
            SpawnDarkPlasmaDeathDamage(projectile, projectile.Center, 2f, 364, 286);
        }

        private static void SpawnDarkPlasmaDeathDamage(Projectile projectile, Vector2 center, float damageMultiplier, int width, int height)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            int projIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                center,
                Vector2.Zero,
                ModContent.ProjectileType<global::CalamityLegendsComeBack.Weapons.SHPC.NewLegendSHPE>(),
                (int)(projectile.damage * damageMultiplier),
                projectile.knockBack,
                projectile.owner);

            if (!Main.projectile.IndexInRange(projIndex))
                return;

            Projectile proj = Main.projectile[projIndex];
            proj.width = width;
            proj.height = height;
            proj.Center = center;
            proj.netUpdate = true;
        }

        private static void SpawnForwardDarkEnergyBurst(Projectile projectile)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            Player owner = Main.player[projectile.owner];
            Vector2 forward = projectile.velocity.SafeNormalize(new Vector2(owner.direction == 0 ? 1f : owner.direction, 0f));

            for (int i = 0; i < 8; i++)
            {
                float spread = MathHelper.Lerp(-0.48f, 0.48f, i / 7f);
                Vector2 direction = forward.RotatedBy(spread).SafeNormalize(forward);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + direction * 18f,
                    direction * Main.rand.NextFloat(13f, 22f),
                    ModContent.ProjectileType<EndlessDevourJavOrbSmall>(),
                    (int)(projectile.damage * 0.37f),
                    projectile.knockBack * 0.35f,
                    projectile.owner,
                    0f,
                    Main.rand.NextFloat(MathHelper.TwoPi));
            }

            SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.72f, Pitch = -0.2f }, projectile.Center);
        }

        private static void SpawnDarkPlasmaDeathOrbs(Projectile projectile)
        {
            SpawnDarkPlasmaDeathOrbs(projectile, projectile.Center, 20, 0.42f);
        }

        private static void SpawnDarkPlasmaDeathOrbs(Projectile projectile, Vector2 center, int orbCount, float damageMultiplier)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            for (int i = 0; i < orbCount; i++)
            {
                float angle = MathHelper.TwoPi * i / Math.Max(1, orbCount) + Main.rand.NextFloat(-0.08f, 0.08f);
                Vector2 direction = angle.ToRotationVector2();
                Vector2 orbVelocity = direction * Main.rand.NextFloat(10f, 22f);
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    center + direction * Main.rand.NextFloat(8f, 28f),
                    orbVelocity,
                    ModContent.ProjectileType<EndlessDevourJavOrbSmall>(),
                    (int)(projectile.damage * damageMultiplier),
                    projectile.knockBack * 0.5f,
                    projectile.owner,
                    0f,
                    Main.rand.NextFloat(MathHelper.TwoPi));
            }
        }

        private static void PlayDarkPlasmaDeathSounds(Projectile projectile)
        {
            PlayDarkPlasmaDeathSounds(projectile, projectile.Center);
        }

        private static void PlayDarkPlasmaDeathSounds(Projectile projectile, Vector2 center)
        {
            for (int i = 0; i < 3; i++)
            {
                SoundStyle fire = new("CalamityMod/Sounds/Item/EarthMeteor");
                SoundEngine.PlaySound(fire with { Volume = 0.6f, Pitch = -0.1f * (i + 1), MaxInstances = 3 }, center);
            }

            SoundStyle reflect = new("CalamityMod/Sounds/Item/ShadowboltReflect");
            SoundEngine.PlaySound(reflect with { Volume = 0.9f, Pitch = -0.4f }, center);
        }

        // ================= PreDraw =================
        public override void PreDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch)
        {
            DarkPlasma_GP gp = projectile.GetGlobalProjectile<DarkPlasma_GP>();
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleVortex").Value;
            Texture2D bloom = TextureAssets.Extra[ExtrasID.SharpTears].Value;
            Texture2D blade = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmear").Value;

            Vector2 drawPos = projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            Vector2 bladeOrigin = blade.Size() * 0.5f;

            float scale01;

            if (projectile.timeLeft > 360)
                scale01 = Utils.GetLerpValue(BlackHoleLifetime, 360f, projectile.timeLeft, true);
            else if (projectile.timeLeft >= 60)
                scale01 = 1f;
            else
                scale01 = Utils.GetLerpValue(0f, 60f, projectile.timeLeft, true);


            MiscShaderData shader;

            if (GameShaders.Misc.TryGetValue("CalamityMod:BasicTrail", out shader))
            {
                shader.UseColor(new Color(15, 15, 20));
                shader.Apply();
            }

            for (int i = 0; i < 7; i++)
            {
                Color c = Color.Lerp(new Color(30, 30, 30), Color.Black, i * 0.1f);
                c.A = 0;

                Main.EntitySpriteDraw(
                    texture,
                    drawPos,
                    null,
                    c * scale01 * 0.42f,
                    projectile.rotation * 3f - i * 0.15f,
                    origin,
                    MathHelper.Clamp(scale01 * 0.22f - i * 0.018f, 0f, 5f),
                    SpriteEffects.None
                );
            }



            // ===== 白色刀盘层：恢复旧版的亮刃切盘感 =====
            float bladePulse = 0.92f + (float)Math.Sin(gp.portalTimer * 7f) * 0.08f;
            float independentBladeSpin = Main.GlobalTimeWrappedHourly * 7.2f +
                (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.8f) * 0.16f;
            for (int i = 0; i < 8; i++)
            {
                float bladeRotation = independentBladeSpin * (i % 2 == 0 ? 1f : -0.82f) + i * MathHelper.TwoPi / 8f;
                Color bladeColor = Color.Lerp(new Color(54, 54, 66), Color.Black, i / 7f);
                bladeColor.A = 0;

                Main.EntitySpriteDraw(
                    blade,
                    drawPos,
                    null,
                    bladeColor * scale01 * 0.38f,
                    bladeRotation,
                    bladeOrigin,
                    new Vector2(0.24f, 1.08f) * scale01 * bladePulse,
                    SpriteEffects.None
                );
            }

            Main.EntitySpriteDraw(
                bloom,
                drawPos,
                null,
                new Color(24, 24, 30) * 0.18f * scale01,
                projectile.rotation * 0.48f,
                bloomOrigin,
                0.18f * scale01 * bladePulse,
                SpriteEffects.None
            );



            {
                // ===== FBM黑洞辅助层（固定朝向 + 呼吸）=====

                // 贴图
                Texture2D fbm = ModContent.Request<Texture2D>(
                    "CalamityLegendsComeBack/Weapons/SHPC/Effects/DPreDog/fbmnoise2_007"
                ).Value;

                Vector2 fbmOrigin = fbm.Size() * 0.5f;

                // ===== 呼吸节奏 =====
                float breathe = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.2f) * 0.5f + 0.5f;

                // ===== 缩放（缓慢变化）=====
                float fbmScale = (0.12f + breathe * 0.13f) * scale01;

                // ===== 亮度变化 =====
                float brightness = 0.34f + breathe * 0.28f;

                // ===== 颜色（暗核描边风格）=====
                Color fbmColor = new Color(20, 20, 20) * brightness;
                fbmColor.A = 0;

                // ===== 完全摆正（不随弹幕旋转）=====
                float rotation = 0f;

                // ===== 位置 =====
                Vector2 pos = projectile.Center - Main.screenPosition;

                // ===== 主体绘制 =====
                Main.EntitySpriteDraw(
                    fbm,
                    pos,
                    null,
                    fbmColor,
                    rotation,
                    fbmOrigin,
                    fbmScale,
                    SpriteEffects.None
                );

                // ===== 轻微外描边（强化橙色高亮）=====
                for (int i = 0; i < 4; i++)
                {
                    Vector2 offset = new Vector2(1.5f, 0).RotatedBy(i * MathHelper.PiOver2);

                    Main.EntitySpriteDraw(
                        fbm,
                        pos + offset,
                        null,
                        fbmColor * 0.18f,
                        rotation,
                        fbmOrigin,
                        fbmScale,
                        SpriteEffects.None
                    );
                }
            }






            // ===== 新增：外圈黑色晕层 =====
            for (int i = 0; i < 2; i++)
            {
                float factor = 1f - i * 0.16f;
                Color c = new Color(10, 10, 10, 0) * 0.16f * factor;

                Main.EntitySpriteDraw(
                    bloom,
                    drawPos,
                    null,
                    c,
                    -projectile.rotation * (0.8f + i * 0.18f),
                    bloomOrigin,
                    (0.22f + i * 0.07f) * scale01,
                    SpriteEffects.None
                );
            }

            // ===== 新增：中心黑色呼吸点 =====
            for (int i = 0; i < 3; i++)
            {
                float pulse = 0.88f + (float)System.Math.Sin(gp.portalTimer * 5f + i * 0.7f) * 0.12f;
                Color c = Color.Black * 0.14f * (1f - i * 0.18f);
                c.A = 0;

                Main.EntitySpriteDraw(
                    bloom,
                    drawPos,
                    null,
                    c,
                    projectile.rotation * (1.5f + i * 0.35f),
                    bloomOrigin,
                    (0.16f + i * 0.07f) * pulse * scale01,
                    SpriteEffects.None
                );
            }
        }
    }

    public class DarkPlasma_GP : GlobalProjectile
    {
        public string LocalizationCategory => "Projectiles.SHPC";
        public override bool InstancePerEntity => true;

        public bool releaseOnly;
        public bool suppressDeathEffects;
        public float portalTimer;
        public int lifeTimer;
        public int multiStarLeader = -1;
        public int multiStarSlot = -1;
        public int multiStarCount = 1;
        public float systemAngle;
        public Vector2 systemCenter;
        public Vector2 systemVelocity;

        public bool IsInMultiStar => multiStarLeader >= 0 && multiStarCount > 1;

        public void ResetForNewBlackHole(Vector2 center)
        {
            releaseOnly = false;
            suppressDeathEffects = false;
            portalTimer = 0f;
            lifeTimer = 0;
            systemCenter = center;
            systemVelocity = Vector2.Zero;
            systemAngle = 0f;
            ClearMultiStar();
        }

        public void ClearMultiStar()
        {
            multiStarLeader = -1;
            multiStarSlot = -1;
            multiStarCount = 1;
        }
    }
}
