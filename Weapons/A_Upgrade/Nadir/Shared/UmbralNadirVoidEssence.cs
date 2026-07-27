using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.Shared
{
    /// <summary>
    /// 冥融虚空核 —— 冥蚀天底的连锁反应载体。
    /// 天底原版 VoidEssence 的「换骨」版：保留 4 帧纯黑虚空核 + 追踪 + 触须召唤，
    /// 叠加冥思系荧光绿光晕，并在命中时向其它敌人分裂出新的一代（有限连锁）。
    /// 行为阶段：延迟激活(无伤) → 追踪 → 命中减速/施加虚空霜冻 → 分裂连锁 → 近身召唤虚空触须 → 死亡爆散。
    /// </summary>
    public class UmbralNadirVoidEssence : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Upgrade/Nadir/Shared/UmbralNadirVoidEssence";

        private const int NumAnimationFrames = 4;
        private const int AnimationFrameTime = 12;
        private const int ActivationDelay = 18;      // 激活前不造成伤害
        private const int HomingStartTime = 26;      // 之后开始追踪
        private const float TentacleRange = 150f;
        private const float TentacleCooldown = 22f;

        private static readonly Color MeldGreen = Color.LightGreen;

        public bool StartFading;

        /// <summary>连锁代数（0 为初代）。</summary>
        public ref float Generation => ref Projectile.ai[0];
        /// <summary>存活计时。</summary>
        public ref float Time => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = NumAnimationFrames;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 340;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 80;
            Projectile.penetrate = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
            Projectile.extraUpdates = 1;
        }

        public override bool? CanDamage() => Time >= ActivationDelay;

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            DrawOffsetX = 1;
            DrawOriginOffsetY = 4;

            // 帧动画
            if (++Projectile.frameCounter > AnimationFrameTime)
            {
                Projectile.frame = (Projectile.frame + 1) % NumAnimationFrames;
                Projectile.frameCounter = 0;
            }

            // 荧光绿光照 + 反向虚空尘拖尾（内黑外绿的冥思质感）
            Lighting.AddLight(Projectile.Center, 0.35f, 0.95f, 0.45f);
            if (Main.rand.NextBool(2))
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    ModContent.DustType<VoidDustInverted>(), 0f, 0f, 0, MeldGreen, Main.rand.NextFloat(0.8f, 1.25f));
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = Projectile.velocity * 0.3f;
                Main.dust[d].color = MeldGreen;
            }

            // 跟随黑核的荧光绿加色冕（事件视界外缘），只在激活后出现
            if (Time >= ActivationDelay && Main.rand.NextBool(3))
                GeneralParticleHandler.SpawnParticle(new GenericBloom(
                    Projectile.Center, Projectile.velocity * 0.2f, MeldGreen with { A = 0 },
                    Main.rand.NextFloat(0.22f, 0.32f), Main.rand.Next(8, 12), false, true),
                    false, GeneralDrawLayer.AfterEverything);

            if (Projectile.localAI[0] > 0f)
                Projectile.localAI[0] -= 1f;

            // 追踪
            if (Time >= HomingStartTime)
                HomingAI();

            if (StartFading)
                Projectile.alpha += 12;

            Time++;
        }

        private void HomingAI()
        {
            NPC target = Projectile.Center.ClosestNPCAt(520f);
            if (target is null)
                return;

            Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 13f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.09f);

            // 近身召唤虚空触须（连锁的近战部分）
            float dist = Projectile.Distance(target.Center);
            if (Projectile.localAI[0] <= 0f && dist <= TentacleRange && Projectile.owner == Main.myPlayer)
            {
                Vector2 tentacleVel = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 6f;
                SpawnTentacle(tentacleVel);
                Projectile.localAI[0] = TentacleCooldown;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 纯黑虚空核 + 残影（绿色光晕由拖尾虚空尘与粒子提供，避免加色批次状态问题）
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Voidfrost>(), 90);
            UmbralCorrosionGlobalNPC.AddStacks(target, 1);

            // 命中减速
            Projectile.velocity *= 0.55f;
            StartFading = true;

            // 命中火花（黑底绿点）
            for (int i = 0; i < 8; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.6f, 4.2f);
                GeneralParticleHandler.SpawnParticle(new AltSparkParticle(Projectile.Center, vel, false,
                    Main.rand.Next(12, 18), Main.rand.NextFloat(0.6f, 1f), Color.Black));
                if (Main.rand.NextBool())
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(Projectile.Center, vel * 0.8f, false,
                        Main.rand.Next(10, 15), Main.rand.NextFloat(0.4f, 0.7f), MeldGreen),
                        false, GeneralDrawLayer.AfterEverything);
            }

            // 连锁：向另一名附近敌人分裂出下一代虚空核
            if (Generation < UmbralNadirBalance.VoidEssenceMaxGeneration && Projectile.owner == Main.myPlayer &&
                Main.player[Projectile.owner].ownedProjectileCounts[Projectile.type] < UmbralNadirBalance.MaxActiveVoidEssence)
            {
                NPC next = FindChainTarget(target);
                if (next != null)
                {
                    Vector2 vel = (next.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 8f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel,
                        Projectile.type, Projectile.damage, Projectile.knockBack, Projectile.owner,
                        Generation + 1f, 0f);
                }
            }
        }

        private NPC FindChainTarget(NPC exclude)
        {
            NPC best = null;
            float bestDist = 700f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.whoAmI == exclude.whoAmI || !npc.CanBeChasedBy(Projectile, false))
                    continue;
                float dist = Projectile.Distance(npc.Center);
                if (dist < bestDist && Collision.CanHit(Projectile.Center, 1, 1, npc.Center, 1, 1))
                {
                    best = npc;
                    bestDist = dist;
                }
            }
            return best;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < Main.rand.Next(16, 24); i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    ModContent.DustType<VoidDustInverted>(), 0f, 0f, 0, MeldGreen, Main.rand.NextFloat(1.0f, 1.7f));
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= Main.rand.NextFloat(1.4f, 2.6f);
                Main.dust[d].color = MeldGreen;
            }

            if (Projectile.owner == Main.myPlayer)
            {
                for (int i = 0; i < 2; i++)
                    SpawnTentacle(Vector2.One.RotatedByRandom(MathHelper.TwoPi) * 4f);
            }
        }

        private void SpawnTentacle(Vector2 velocity)
        {
            float curl0 = Main.rand.NextFloat(0.01f, 0.08f) * (Main.rand.NextBool() ? -1f : 1f);
            float curl1 = Main.rand.NextFloat(0.01f, 0.08f) * (Main.rand.NextBool() ? -1f : 1f);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity,
                ModContent.ProjectileType<VoidTentacle>(),
                (int)(Projectile.damage * 1.1f), Projectile.knockBack, Projectile.owner, curl0, curl1);
        }
    }
}
