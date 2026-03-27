using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Magic;
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
        public override int EffectID => 30;

        public override int AmmoType => ModContent.ItemType<RuinousSoul>();

        // ===== 鬼白主题 =====
        public override Color ThemeColor => new Color(220, 235, 255);
        public override Color StartColor => new Color(255, 255, 255);
        public override Color EndColor => new Color(150, 170, 205);

        public override float SquishyLightParticleFactor => 1.85f;
        public override float ExplosionPulseFactor => 1.85f;

        // ===== 粘附状态表（原封不动搬入核心逻辑）=====
        private readonly Dictionary<int, bool> stuckState = new();
        private readonly Dictionary<int, int> stuckTargetIndex = new();
        private readonly Dictionary<int, int> hitCountOnCurrentTarget = new();
        private readonly Dictionary<int, int> stickVisualTimer = new();
        private readonly Dictionary<int, float> orbitAngle = new();

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            int id = projectile.whoAmI;

            // 初始速度三倍
            projectile.velocity *= 1.1f;
            projectile.extraUpdates = 3;
            projectile.timeLeft = 3000;

            // ===== 粘附系统需要的配置 =====
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 50;

            stuckState[id] = false;
            stuckTargetIndex[id] = -1;
            hitCountOnCurrentTarget[id] = 0;
            stickVisualTimer[id] = 0;
            orbitAngle[id] = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            int id = projectile.whoAmI;
            EnsureStateExists(id);

            // ===== 常规飞行：保留本体速度风格 =====
            if (!stuckState[id])
            {
                projectile.velocity *= 1.02f;
                Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.28f);
                return;
            }

            // ===== 粘附状态：锁在敌人身上 =====
            int targetIndex = stuckTargetIndex[id];
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
            {
                stuckState[id] = false;
                stuckTargetIndex[id] = -1;
                hitCountOnCurrentTarget[id] = 0;
                projectile.friendly = true;
                return;
            }

            NPC target = Main.npc[targetIndex];
            if (!target.active || !target.CanBeChasedBy(projectile))
            {
                stuckState[id] = false;
                stuckTargetIndex[id] = -1;
                hitCountOnCurrentTarget[id] = 0;
                projectile.friendly = true;
                return;
            }

            stickVisualTimer[id]++;
            //orbitAngle[id] += 0.18f;

            Vector2 stickDir = (projectile.Center - target.Center).SafeNormalize(Vector2.UnitY);
            Vector2 orbitOffset = stickDir * 18f;

            projectile.Center = target.Center + orbitOffset;
            projectile.velocity = Vector2.Zero;
            projectile.rotation += 0.22f;

            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.18f);
        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            projectile.netUpdate = true;

            int id = projectile.whoAmI;
            EnsureStateExists(id);

            // ===== 命中音效 =====
            SoundEngine.PlaySound(SoundID.Item103 with
            {
                Volume = 0.8f,
                Pitch = 0.05f,
                PitchVariance = 0.18f
            }, target.Center);

            Vector2 forward = (target.Center - projectile.Center).SafeNormalize(Vector2.UnitX);

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

            // ===== 额外效果：随机对立双发 PhantasmalFuryProj =====
            float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < 6; i++)
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
                    ModContent.ProjectileType<PhantasmalFuryProj>(),
                    (int)(projectile.damage * 0.35f),
                    1f,
                    projectile.owner
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

            // ================= 粘附原逻辑（原封不动核心搬入） =================
            if (!stuckState[id] || stuckTargetIndex[id] != target.whoAmI)
            {
                stuckState[id] = true;
                stuckTargetIndex[id] = target.whoAmI;
                hitCountOnCurrentTarget[id] = 1;
                stickVisualTimer[id] = 0;
                orbitAngle[id] = Main.rand.NextFloat(MathHelper.TwoPi);
                projectile.velocity = Vector2.Zero;
                projectile.friendly = false;
                projectile.netUpdate = true;
                return;
            }

            hitCountOnCurrentTarget[id]++;

       
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            ClearState(projectile.whoAmI);
        }

        // ========================= 工具区 =========================

        private void EnsureStateExists(int id)
        {
            if (!stuckState.ContainsKey(id))
                stuckState[id] = false;
            if (!stuckTargetIndex.ContainsKey(id))
                stuckTargetIndex[id] = -1;
            if (!hitCountOnCurrentTarget.ContainsKey(id))
                hitCountOnCurrentTarget[id] = 0;
            if (!stickVisualTimer.ContainsKey(id))
                stickVisualTimer[id] = 0;
            if (!orbitAngle.ContainsKey(id))
                orbitAngle[id] = 0f;
        }

        private void ClearState(int id)
        {
            stuckState.Remove(id);
            stuckTargetIndex.Remove(id);
            hitCountOnCurrentTarget.Remove(id);
            stickVisualTimer.Remove(id);
            orbitAngle.Remove(id);
        }

     
    }
}