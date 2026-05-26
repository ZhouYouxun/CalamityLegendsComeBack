using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.LeafProj
{
    internal class BFLeftPlagueReaper : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/LeafProj/BlossomFluxBOMB";

        // 缩放方向控制
        private bool scaleExpand = true;

        // 追踪模式触发时间（每个弹幕随机）
        private int homingStartTime;

        // 是否已经进入追踪
        private bool homingActivated;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.alpha = 255;
            Projectile.extraUpdates = 1;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.light = 0.2f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // 每个弹幕随机不同的追踪启动时间
            // extraUpdates=1，所以这里实际上会很灵动
            homingStartTime = Main.rand.Next(25, 90);
        }

        public override void AI()
        {
            // 淡入
            Projectile.alpha -= 2;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            // 呼吸缩放效果
            if (scaleExpand)
            {
                Projectile.scale += 0.05f;

                if (Projectile.scale >= 1.2f)
                    scaleExpand = false;
            }
            else
            {
                Projectile.scale -= 0.05f;

                if (Projectile.scale <= 0.8f)
                    scaleExpand = true;
            }

            // 原本的上下浮动飞行
            Projectile.ai[0] += 1f;

            if (Projectile.ai[0] >= 20f && Projectile.ai[0] < 40f)
            {
                Projectile.velocity.Y += 0.3f;
                Projectile.velocity.X *= 0.98f;
            }
            else if (Projectile.ai[0] >= 40f && Projectile.ai[0] < 60f)
            {
                Projectile.velocity.Y -= 0.3f;
                Projectile.velocity.X *= 1.02f;
            }
            else if (Projectile.ai[0] >= 60f)
            {
                Projectile.ai[0] = 0f;
            }

            // 到达随机时间后开启追踪
            if (!homingActivated && Projectile.timeLeft <= (300 - homingStartTime))
            {
                homingActivated = true;
            }

            // 追踪逻辑
            if (homingActivated)
            {
                NPC target = FindClosestNPC(900f);

                if (target != null)
                {
                    Vector2 targetDirection = Projectile.DirectionTo(target.Center);

                    // 当前速度长度
                    float currentSpeed = Projectile.velocity.Length();

                    // 目标速度
                    Vector2 desiredVelocity = targetDirection * currentSpeed;

                    // 平滑追踪
                    Projectile.velocity = Vector2.Lerp(
                        Projectile.velocity,
                        desiredVelocity,
                        0.06f);

                    // 稍微再加一点速度，避免转向后越来越慢
                    Projectile.velocity *= 1.003f;
                }
            }

            // 旋转
            Projectile.rotation += Projectile.velocity.X * 0.03f;
        }

        // 搜索最近敌人
        private NPC FindClosestNPC(float maxDetectDistance)
        {
            NPC closestNPC = null;
            float sqrMaxDistance = maxDetectDistance * maxDetectDistance;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active || npc.friendly || npc.dontTakeDamage)
                    continue;

                float sqrDistance = Vector2.DistanceSquared(Projectile.Center, npc.Center);

                if (sqrDistance < sqrMaxDistance)
                {
                    sqrMaxDistance = sqrDistance;
                    closestNPC = npc;
                }
            }

            return closestNPC;
        }

        public override Color? GetAlpha(Color lightColor)
            => new(Main.DiscoR, 203, 103, Projectile.alpha);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            bool markedTarget = target.GetGlobalNPC<BFArrow_CDetecNPC>().IsPriorityMarkedBy(Projectile.owner);

            BFPlaguePollutionNPC pollution = target.GetGlobalNPC<BFPlaguePollutionNPC>();

            pollution.ApplyPollution(target, markedTarget);
            pollution.ApplyPlagueDebuffs(target, markedTarget);

            target.AddBuff(BuffID.Poisoned, 180);
            target.AddBuff(BuffID.Venom, 100);
            target.AddBuff(ModContent.BuffType<MiracleBlight>(), markedTarget ? 480 : 240);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

            for (int d = 0; d < 25; d++)
            {
                int index = Dust.NewDust(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.ChlorophyteWeapon,
                    0f,
                    0f,
                    0,
                    new Color(Main.DiscoR, 203, 103),
                    1f);

                Main.dust[index].noGravity = true;
                Main.dust[index].velocity *= 1.5f;
                Main.dust[index].scale = 1.5f;
            }

            int sporeAmt = Main.rand.Next(3, 7);

            if (Projectile.owner != Main.myPlayer)
                return;

            for (int s = 0; s < sporeAmt; s++)
            {
                Vector2 velocity = CalamityUtils.RandomVelocity(100f, 70f, 100f);

                int proj = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    velocity,
                    ProjectileID.SporeGas + Main.rand.Next(3),
                    (int)(Projectile.damage * 0.25),
                    0f,
                    Projectile.owner);

                if (!BFArrowCommon.InBounds(proj, Main.maxProjectiles))
                    continue;

                Main.projectile[proj].DamageType = DamageClass.Ranged;
                Main.projectile[proj].usesLocalNPCImmunity = true;
                Main.projectile[proj].usesIDStaticNPCImmunity = false;
                Main.projectile[proj].localNPCHitCooldown = 30;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(
                Projectile,
                ProjectileID.Sets.TrailingMode[Type],
                lightColor,
                1);

            return false;
        }
    }
}