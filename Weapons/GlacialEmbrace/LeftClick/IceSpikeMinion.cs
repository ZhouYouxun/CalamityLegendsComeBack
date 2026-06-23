using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace.EXSkill;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace.General;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace.LeftClick
{
    // 自主攻击状态枚举
    public enum IceSpikeAttackState
    {
        OrbitGuard,       // 0 - 环绕守护（默认/间隔态）
        FrostLanceRush,   // 1 - 寒冰突刺（高速冲锋）
        CryoShardVolley,  // 2 - 冰晶弹幕（远程射击）
        CrescentSweep,    // 3 - 新月横扫（弧形斩击）
        AvalancheDive,    // 4 - 雪崩坠击（空中俯冲）
        FrostRing,        // 5 - 霜环绞杀（环绕目标）
    }

    public class IceSpikeMinion : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Summon/GlacialEmbrace"; // 复用原版贴图

        // ===== 突刺模式与运行状态变量 =====
        public bool embedded = false;
        public int embedNPCIndex = -1;
        public Vector2 embedOffset = Vector2.Zero;
        public int existTimer = 0;
        public bool flying = false;
        public bool isThrusting = false; // 是否处于触发后的贯穿推射状态
        public int pierceTimer = 0;

        // ===== 打击模式与休眠变量 =====
        public bool hibernating = false;
        public int hibernateTimer = 0;
        public bool smashing = false;
        public float smashProgress = 0f;

        // ===== 自主攻击系统变量 =====
        public IceSpikeAttackState attackState = IceSpikeAttackState.OrbitGuard;
        public int attackTimer = 0;
        public int attackCycleIndex = 0;
        public int spikeIndex = 0; // 用于错开攻击计时
        public NPC currentTarget = null;

        // 各攻击子状态变量
        private int attackSubTimer = 0;
        private Vector2 attackStartPos = Vector2.Zero;
        private Vector2 attackTargetPos = Vector2.Zero;
        private float sweepAngle = 0f;
        private int shardsShot = 0;

        // 攻击循环定义
        private static readonly IceSpikeAttackState[] AttackCycle = {
            IceSpikeAttackState.FrostLanceRush,
            IceSpikeAttackState.CryoShardVolley,
            IceSpikeAttackState.CrescentSweep,
            IceSpikeAttackState.AvalancheDive,
            IceSpikeAttackState.FrostRing
        };

        // 环绕守护持续帧数（攻击间隔）
        private const int BaseGuardDuration = 180; // 3秒
        private const int ShortGuardDuration = 120; // 2秒

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailingMode[Type] = 0;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 48;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 1f; // 默认占用 1 栏位
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
            
            // 启用局部无敌帧机制
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 90;
        }

        // 辅助方法：判断是否正在环绕玩家（非发射、非钉入、非终结技组装）
        public bool IsCirclingPlayer()
        {
            return !embedded && !flying && !smashing && !IsUltimateActive()
                && attackState == IceSpikeAttackState.OrbitGuard;
        }

        private bool IsUltimateActive()
        {
            Player player = Main.player[Projectile.owner];
            return player.ownedProjectileCounts[ModContent.ProjectileType<GlacialDrillProj>()] > 0;
        }

        // 是否处于自主攻击动作中（非环绕）
        private bool IsInAttackAction()
        {
            return attackState != IceSpikeAttackState.OrbitGuard;
        }

        public override bool? CanDamage()
        {
            if (hibernating) return false;
            if (embedded) return false; // 钉在怪身上时不直接造成伤害
            return null;
        }

        // ===== 目标搜索 =====
        private NPC FindTarget(Player player)
        {
            return CalamityUtils.MinionHoming(Projectile.Center, 800f, player);
        }

        // ===== 初始化冰刺索引（由武器主文件在排布时调用） =====
        public void SetSpikeIndex(int index)
        {
            spikeIndex = index;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            // 维持生命周期
            if (!player.active || player.dead || !modPlayer.GlacialEmbraceMinion)
            {
                Projectile.Kill();
                return;
            }

            // 处理休眠冷却
            if (hibernating)
            {
                Projectile.friendly = false;
                hibernateTimer--;
                if (hibernateTimer <= 0)
                {
                    hibernating = false;
                    Projectile.friendly = true;
                    // 激活时释放粒子
                    for (int i = 0; i < 15; i++)
                    {
                        Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Ice);
                        d.velocity = Main.rand.NextVector2Circular(3f, 3f);
                        d.noGravity = true;
                    }
                }
            }

            // ===== 分支1：终结技樊寒神钻状态 =====
            if (IsUltimateActive())
            {
                HandleUltimateAssembly(player);
                return;
            }

            // ===== 突刺模式特殊状态：嵌入/飞行中不参与自主攻击 =====
            if (modPlayer.CurrentMode == 1 && (embedded || flying))
            {
                HandlePierceSpecialStates(player, modPlayer);
                return;
            }

            // ===== 打击模式特殊状态：碰撞中不参与自主攻击 =====
            if (smashing)
            {
                HandleSmashAnimation(player);
                return;
            }

            // ===== 自主攻击状态机 =====
            // 搜索目标
            if (currentTarget != null && (!currentTarget.active || currentTarget.life <= 0))
                currentTarget = null;
            if (currentTarget == null)
                currentTarget = FindTarget(player);

            if (currentTarget != null && !hibernating)
            {
                // 有目标时：攻击计时器递增（含错开偏移）
                attackTimer++;

                if (attackState == IceSpikeAttackState.OrbitGuard)
                {
                    // 环绕守护阶段，判断是否该切换到攻击
                    int guardDur = attackCycleIndex == 0 ? BaseGuardDuration : ShortGuardDuration;
                    // 错开：每个冰刺的攻击时间点偏移 spikeIndex * 30帧
                    int offset = spikeIndex * 30;
                    if (attackTimer >= guardDur + offset)
                    {
                        // 进入下一个攻击状态
                        attackState = AttackCycle[attackCycleIndex];
                        attackCycleIndex = (attackCycleIndex + 1) % AttackCycle.Length;
                        attackTimer = 0;
                        attackSubTimer = 0;
                        shardsShot = 0;
                        attackStartPos = Projectile.Center;
                        attackTargetPos = currentTarget.Center;
                        Projectile.netUpdate = true;
                    }
                }
            }
            else
            {
                // 无目标时重置为环绕
                if (attackState != IceSpikeAttackState.OrbitGuard)
                {
                    ReturnToOrbit();
                }
            }

            // ===== 根据当前状态执行 =====
            switch (attackState)
            {
                case IceSpikeAttackState.OrbitGuard:
                    HandleOrbitGuard(player, modPlayer);
                    break;
                case IceSpikeAttackState.FrostLanceRush:
                    HandleFrostLanceRush(player, modPlayer);
                    break;
                case IceSpikeAttackState.CryoShardVolley:
                    HandleCryoShardVolley(player, modPlayer);
                    break;
                case IceSpikeAttackState.CrescentSweep:
                    HandleCrescentSweep(player, modPlayer);
                    break;
                case IceSpikeAttackState.AvalancheDive:
                    HandleAvalancheDive(player, modPlayer);
                    break;
                case IceSpikeAttackState.FrostRing:
                    HandleFrostRing(player, modPlayer);
                    break;
            }
        }

        private void ReturnToOrbit()
        {
            attackState = IceSpikeAttackState.OrbitGuard;
            attackTimer = 0;
            attackSubTimer = 0;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.netUpdate = true;
        }

        #region ===== 环绕守护（保留现有模式逻辑） =====
        private void HandleOrbitGuard(Player player, GlacialEmbracePlayer modPlayer)
        {
            if (modPlayer.CurrentMode == 0)
                HandleSlashOrbit(player, modPlayer);
            else if (modPlayer.CurrentMode == 1)
                HandlePierceOrbit(player, modPlayer);
            else
                HandleStrikeOrbit(player, modPlayer);
        }

        // ----- 斩击模式环绕 -----
        private void HandleSlashOrbit(Player player, GlacialEmbracePlayer modPlayer)
        {
            Projectile.minionSlots = 0.5f;
            Projectile.timeLeft = 2;
            Projectile.friendly = true;

            bool isOuter = Projectile.ai[1] == 1f;
            float speedMult = modPlayer.GlacialDivinityTimer > 0 ? 1.1f : 1.0f;
            float angularSpeed = MathHelper.ToRadians((isOuter ? 2.8f : 4f) * speedMult);

            Projectile.ai[0] -= angularSpeed;
            float angle = Projectile.ai[0];

            float radius = isOuter ? 140f : 90f;
            Projectile.Center = player.Center + angle.ToRotationVector2() * radius + Vector2.UnitY * player.gfxOffY;
            Projectile.rotation = angle + MathHelper.PiOver2;

            Projectile.localNPCHitCooldown = isOuter ? 128 : 90;
        }

        // ----- 突刺模式环绕 -----
        private void HandlePierceOrbit(Player player, GlacialEmbracePlayer modPlayer)
        {
            Projectile.minionSlots = 1.0f;
            Projectile.timeLeft = 2;
            Projectile.friendly = true;

            float angle = Projectile.ai[0];
            Projectile.Center = player.Center + angle.ToRotationVector2() * 90f + Vector2.UnitY * player.gfxOffY;
            Projectile.rotation = angle + MathHelper.PiOver2;

            float speed = MathHelper.ToRadians(4f * (modPlayer.GlacialDivinityTimer > 0 ? 1.1f : 1.0f));
            Projectile.ai[0] -= speed;
        }

        // ----- 打击模式环绕 -----
        private void HandleStrikeOrbit(Player player, GlacialEmbracePlayer modPlayer)
        {
            Projectile.minionSlots = 1.0f;
            Projectile.timeLeft = 2;

            float angle = Projectile.ai[0];
            Projectile.Center = player.Center + angle.ToRotationVector2() * 90f + Vector2.UnitY * player.gfxOffY;
            Projectile.rotation = angle - MathHelper.PiOver2; // 朝内反转

            if (!hibernating)
            {
                float speed = MathHelper.ToRadians(4f * (modPlayer.GlacialDivinityTimer > 0 ? 1.1f : 1.0f));
                Projectile.ai[0] -= speed;
            }
        }
        #endregion

        #region ===== 攻击1：寒冰突刺 (FrostLanceRush) =====
        private void HandleFrostLanceRush(Player player, GlacialEmbracePlayer modPlayer)
        {
            Projectile.timeLeft = 2;
            attackSubTimer++;

            if (currentTarget == null || !currentTarget.active)
            {
                ReturnToOrbit();
                return;
            }

            if (attackSubTimer <= 10)
            {
                // 准备阶段：蓄力，冰刺指向目标
                Projectile.friendly = true;
                Vector2 toTarget = currentTarget.Center - Projectile.Center;
                Projectile.rotation = toTarget.ToRotation() + MathHelper.PiOver2;

                // 蓄力粒子
                if (attackSubTimer % 3 == 0)
                {
                    Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Frost);
                    d.velocity = -toTarget.SafeNormalize(Vector2.Zero) * 2f;
                    d.scale = 1.2f;
                    d.noGravity = true;
                }
            }
            else if (attackSubTimer <= 40)
            {
                // 冲锋阶段：高速飞向目标
                Projectile.friendly = true;
                Projectile.localNPCHitCooldown = 15;

                Vector2 toTarget = currentTarget.Center - Projectile.Center;
                float dist = toTarget.Length();

                if (dist > 30f)
                {
                    Projectile.velocity = toTarget.SafeNormalize(Vector2.Zero) * 20f;
                }
                else
                {
                    // 命中附近，产生命中效果
                    SpawnLanceHitEffect();
                    ReturnToOrbitSmooth(player);
                    return;
                }

                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

                // 冲锋拖尾粒子
                Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Ice);
                d.velocity = -Projectile.velocity * 0.1f;
                d.scale = Main.rand.NextFloat(0.8f, 1.2f);
                d.noGravity = true;
            }
            else
            {
                // 回归阶段
                ReturnToOrbitSmooth(player);
            }

            // 模式变体
            ApplyLanceModeVariant(modPlayer);
        }

        private void SpawnLanceHitEffect()
        {
            for (int i = 0; i < 10; i++)
            {
                int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Ice;
                Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, dType);
                d.velocity = Main.rand.NextVector2Circular(5f, 5f);
                d.scale = Main.rand.NextFloat(1.0f, 1.4f);
                d.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item30 with { Pitch = 0.3f, Volume = 0.6f }, Projectile.Center);
            Main.player[Projectile.owner].Calamity().GeneralScreenShakePower = Math.Max(
                Main.player[Projectile.owner].Calamity().GeneralScreenShakePower, 1.5f);
        }

        private void ApplyLanceModeVariant(GlacialEmbracePlayer modPlayer)
        {
            if (modPlayer.CurrentMode == 0)
            {
                // 斩击：只有内环冰刺执行突刺（外环继续环绕）
                if (Projectile.ai[1] == 1f) // 外环
                {
                    ReturnToOrbit(); // 外环不参与突刺
                }
            }
            // 突刺模式：命中时会在OnHitNPC中嵌入
            // 打击模式：命中产生小型冲击波（在OnHitNPC中处理）
        }

        private void ReturnToOrbitSmooth(Player player)
        {
            // 平滑回归到玩家身边
            Vector2 toPlayer = player.Center - Projectile.Center;
            if (toPlayer.Length() > 40f)
            {
                Projectile.velocity = toPlayer.SafeNormalize(Vector2.Zero) * 14f;
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }
            else
            {
                Projectile.velocity = Vector2.Zero;
                ReturnToOrbit();
            }
        }
        #endregion

        #region ===== 攻击2：冰晶弹幕 (CryoShardVolley) =====
        private void HandleCryoShardVolley(Player player, GlacialEmbracePlayer modPlayer)
        {
            Projectile.timeLeft = 2;
            attackSubTimer++;

            if (currentTarget == null || !currentTarget.active)
            {
                ReturnToOrbit();
                return;
            }

            if (attackSubTimer <= 10)
            {
                // 悬停阶段：移动到射击位置
                Projectile.velocity *= 0.85f;
                Projectile.rotation = (currentTarget.Center - Projectile.Center).ToRotation() + MathHelper.PiOver2;
            }
            else if (attackSubTimer <= 50)
            {
                // 射击阶段
                Projectile.velocity *= 0.9f;
                Projectile.rotation = (currentTarget.Center - Projectile.Center).ToRotation() + MathHelper.PiOver2;

                int maxShards = modPlayer.CurrentMode switch
                {
                    0 => 5, // 斩击：5枚扇形
                    1 => 3, // 突刺：3枚追踪
                    _ => 2  // 打击：2枚大型
                };

                int interval = 40 / maxShards;
                if ((attackSubTimer - 10) % interval == 0 && shardsShot < maxShards)
                {
                    FireIceShard(player, modPlayer);
                    shardsShot++;
                }

                // 射击后坐力粒子
                if (attackSubTimer % 5 == 0)
                {
                    Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Frost);
                    d.velocity = -(currentTarget.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 3f;
                    d.scale = 0.9f;
                    d.noGravity = true;
                }
            }
            else
            {
                // 回归阶段
                ReturnToOrbitSmooth(player);
            }
        }

        private void FireIceShard(Player player, GlacialEmbracePlayer modPlayer)
        {
            if (currentTarget == null) return;

            Vector2 toTarget = (currentTarget.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
            float speed = 13f;
            int damage = (int)(Projectile.damage * 0.35f);

            var source = player.GetSource_ItemUse(player.HeldItem);

            if (modPlayer.CurrentMode == 0)
            {
                // 斩击：扇形散射
                float spread = MathHelper.ToRadians(15f) * (shardsShot - 2); // -30° to +30°
                Vector2 vel = toTarget.RotatedBy(spread) * speed;
                Projectile.NewProjectile(source, Projectile.Center, vel,
                    ModContent.ProjectileType<IceShardProj>(), damage, 1f, player.whoAmI);
            }
            else if (modPlayer.CurrentMode == 1)
            {
                // 突刺：轻微追踪（通过给予朝向目标的初速）
                Vector2 vel = toTarget * (speed + 1f);
                int p = Projectile.NewProjectile(source, Projectile.Center, vel,
                    ModContent.ProjectileType<IceShardProj>(), damage, 1f, player.whoAmI);
                if (Main.projectile.IndexInRange(p))
                    Main.projectile[p].ai[0] = 1f; // 标记为追踪模式
            }
            else
            {
                // 打击：大型冰晶（更高伤害）
                Vector2 vel = toTarget * (speed - 2f);
                int p = Projectile.NewProjectile(source, Projectile.Center, vel,
                    ModContent.ProjectileType<IceShardProj>(), (int)(damage * 1.5f), 3f, player.whoAmI);
                if (Main.projectile.IndexInRange(p))
                    Main.projectile[p].ai[0] = 2f; // 标记为大型模式
            }

            SoundEngine.PlaySound(SoundID.Item30 with { Pitch = 0.8f, Volume = 0.4f }, Projectile.Center);
        }
        #endregion

        #region ===== 攻击3：新月横扫 (CrescentSweep) =====
        private void HandleCrescentSweep(Player player, GlacialEmbracePlayer modPlayer)
        {
            Projectile.timeLeft = 2;
            attackSubTimer++;

            if (currentTarget == null || !currentTarget.active)
            {
                ReturnToOrbit();
                return;
            }

            if (attackSubTimer <= 5)
            {
                // 蓄力：短暂向外扩展
                float expandRadius = 90f * 1.5f;
                sweepAngle = (currentTarget.Center - player.Center).ToRotation() - MathHelper.Pi; // 起始角度对着目标的反方向
                Projectile.Center = player.Center + sweepAngle.ToRotationVector2() * expandRadius;
                Projectile.rotation = sweepAngle + MathHelper.PiOver2;
                Projectile.localNPCHitCooldown = 10;
            }
            else if (attackSubTimer <= 25)
            {
                // 横扫：做180°快速弧形（9°/帧 × 20帧 = 180°）
                float sweepSpeed = MathHelper.ToRadians(9f);
                sweepAngle += sweepSpeed;

                float radius = 90f * 1.5f;
                Projectile.Center = player.Center + sweepAngle.ToRotationVector2() * radius + Vector2.UnitY * player.gfxOffY;
                Projectile.rotation = sweepAngle + MathHelper.PiOver2;
                Projectile.friendly = true;

                // 横扫拖尾粒子
                if (attackSubTimer % 2 == 0)
                {
                    int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Electric;
                    Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, dType);
                    d.velocity = Main.rand.NextVector2Circular(2f, 2f);
                    d.scale = Main.rand.NextFloat(1.0f, 1.2f);
                    d.noGravity = true;
                }

                // 模式变体：打击模式在中点产生冲击波
                if (modPlayer.CurrentMode == 2 && attackSubTimer == 15)
                {
                    var source = player.GetSource_ItemUse(player.HeldItem);
                    int wave = Projectile.NewProjectile(source, Projectile.Center, Vector2.Zero,
                        ProjectileID.SolarWhipSwordExplosion, (int)(Projectile.damage * 0.8f), 3f, player.whoAmI);
                    if (Main.projectile.IndexInRange(wave))
                    {
                        Main.projectile[wave].DamageType = DamageClass.Summon;
                        Main.projectile[wave].friendly = true;
                        Main.projectile[wave].hostile = false;
                    }
                    player.Calamity().GeneralScreenShakePower = Math.Max(
                        player.Calamity().GeneralScreenShakePower, 1.0f);
                }
            }
            else
            {
                // 回归
                ReturnToOrbitSmooth(player);
            }
        }
        #endregion

        #region ===== 攻击4：雪崩坠击 (AvalancheDive) =====
        private void HandleAvalancheDive(Player player, GlacialEmbracePlayer modPlayer)
        {
            Projectile.timeLeft = 2;
            attackSubTimer++;

            if (currentTarget == null || !currentTarget.active)
            {
                ReturnToOrbit();
                return;
            }

            if (attackSubTimer <= 15)
            {
                // 上升阶段：飞到目标上方
                Vector2 aboveTarget = currentTarget.Center + new Vector2(0, -300f);
                Vector2 toAbove = aboveTarget - Projectile.Center;
                Projectile.velocity = toAbove.SafeNormalize(Vector2.Zero) * Math.Min(toAbove.Length() * 0.15f, 18f);
                Projectile.rotation = -MathHelper.PiOver2; // 朝上

                // 上升粒子
                if (attackSubTimer % 3 == 0)
                {
                    Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Ice);
                    d.velocity = new Vector2(0, 2f);
                    d.scale = 0.8f;
                    d.noGravity = true;
                }
            }
            else if (attackSubTimer <= 20)
            {
                // 短暂悬停
                Projectile.velocity *= 0.3f;
                Projectile.rotation = MathHelper.PiOver2; // 朝下

                // 瞄准粒子
                Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Frost);
                d.velocity = new Vector2(0, 4f);
                d.scale = 1.3f;
                d.noGravity = true;
            }
            else if (attackSubTimer <= 40)
            {
                // 俯冲阶段
                Projectile.friendly = true;
                Projectile.localNPCHitCooldown = 10;

                // 刷新目标位置以命中移动中的目标
                Vector2 toDive = currentTarget.Center - Projectile.Center;
                Projectile.velocity = toDive.SafeNormalize(new Vector2(0, 1)) * 22f;
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

                // 已经到达或穿过目标
                if (toDive.Length() < 40f || Projectile.Center.Y > currentTarget.Center.Y + 50f)
                {
                    SpawnDiveImpactEffect(modPlayer);
                    ReturnToOrbitSmooth(player);
                    return;
                }

                // 俯冲拖尾
                for (int i = 0; i < 2; i++)
                {
                    int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Ice;
                    Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, dType);
                    d.velocity = -Projectile.velocity * 0.05f + Main.rand.NextVector2Circular(1f, 1f);
                    d.scale = Main.rand.NextFloat(1.2f, 1.6f);
                    d.noGravity = true;
                }
            }
            else
            {
                // 超时回归
                ReturnToOrbitSmooth(player);
            }
        }

        private void SpawnDiveImpactEffect(GlacialEmbracePlayer modPlayer)
        {
            // 碎片粒子
            for (int i = 0; i < 15; i++)
            {
                int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Ice;
                Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, dType);
                d.velocity = Main.rand.NextVector2Circular(6f, 4f) + new Vector2(0, -2f);
                d.scale = Main.rand.NextFloat(1.2f, 1.6f);
                d.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item62 with { Pitch = 0.4f, Volume = 0.5f }, Projectile.Center);
            Main.player[Projectile.owner].Calamity().GeneralScreenShakePower = Math.Max(
                Main.player[Projectile.owner].Calamity().GeneralScreenShakePower, 2.5f);
        }
        #endregion

        #region ===== 攻击5：霜环绞杀 (FrostRing) =====
        private void HandleFrostRing(Player player, GlacialEmbracePlayer modPlayer)
        {
            Projectile.timeLeft = 2;
            attackSubTimer++;

            if (currentTarget == null || !currentTarget.active)
            {
                ReturnToOrbit();
                return;
            }

            if (attackSubTimer <= 15)
            {
                // 移动至目标阶段
                Vector2 toTarget = currentTarget.Center - Projectile.Center;
                Projectile.velocity = toTarget.SafeNormalize(Vector2.Zero) * Math.Min(toTarget.Length() * 0.2f, 16f);
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }
            else if (attackSubTimer <= 55)
            {
                // 环绕目标高频伤害
                Projectile.friendly = true;
                Projectile.localNPCHitCooldown = 8; // 高频多段

                float ringAngle = (attackSubTimer - 15) * MathHelper.ToRadians(12f); // 12°/帧
                float ringRadius = modPlayer.CurrentMode == 0 ? 90f : 60f; // 斩击更大半径

                Projectile.Center = currentTarget.Center + ringAngle.ToRotationVector2() * ringRadius;
                Projectile.rotation = ringAngle + MathHelper.PiOver2;
                Projectile.velocity = Vector2.Zero;

                // 环绕粒子
                if (attackSubTimer % 3 == 0)
                {
                    Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Frost);
                    d.velocity = Main.rand.NextVector2Circular(1.5f, 1.5f);
                    d.scale = Main.rand.NextFloat(0.6f, 0.8f);
                    d.noGravity = true;
                }

                // 模式变体：突刺在结束时嵌入
                if (modPlayer.CurrentMode == 1 && attackSubTimer == 54)
                {
                    // 尝试嵌入
                    TryEmbed(currentTarget, player);
                }
            }
            else
            {
                // 打击模式：结束时爆破
                if (modPlayer.CurrentMode == 2 && attackSubTimer == 56)
                {
                    var source = player.GetSource_ItemUse(player.HeldItem);
                    int wave = Projectile.NewProjectile(source, Projectile.Center, Vector2.Zero,
                        ProjectileID.SolarWhipSwordExplosion, (int)(Projectile.damage * 0.6f), 2f, player.whoAmI);
                    if (Main.projectile.IndexInRange(wave))
                    {
                        Main.projectile[wave].DamageType = DamageClass.Summon;
                        Main.projectile[wave].friendly = true;
                        Main.projectile[wave].hostile = false;
                    }
                }

                // 回归
                ReturnToOrbitSmooth(player);
            }
        }

        private void TryEmbed(NPC target, Player player)
        {
            int embeddedCount = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == Type && p.owner == player.whoAmI)
                {
                    var modProj = p.ModProjectile as IceSpikeMinion;
                    if (modProj != null && modProj.embedded && modProj.embedNPCIndex == target.whoAmI)
                        embeddedCount++;
                }
            }
            if (embeddedCount < 6)
            {
                embedded = true;
                embedNPCIndex = target.whoAmI;
                embedOffset = Projectile.Center - target.Center;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
                attackState = IceSpikeAttackState.OrbitGuard; // 重置状态
                SoundEngine.PlaySound(SoundID.Item30 with { Pitch = -0.3f, Volume = 0.8f }, Projectile.Center);
            }
        }
        #endregion

        #region ===== 突刺模式特殊状态 =====
        private void HandlePierceSpecialStates(Player player, GlacialEmbracePlayer modPlayer)
        {
            Projectile.minionSlots = 1.0f;

            if (embedded)
            {
                // 钉在敌怪身上
                NPC targetNPC = Main.npc[embedNPCIndex];
                if (!targetNPC.active || targetNPC.life <= 0)
                {
                    Projectile.Kill();
                    return;
                }
                Projectile.Center = targetNPC.Center + embedOffset.RotatedBy(targetNPC.rotation);
                Projectile.rotation = embedOffset.ToRotation() + MathHelper.PiOver2;
                Projectile.friendly = false;
                Projectile.timeLeft = 2;
            }
            else if (flying)
            {
                // 直线贯穿发射阶段
                Projectile.friendly = true;
                Projectile.penetrate = -1;
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

                pierceTimer++;
                if (pierceTimer >= 25)
                    Projectile.Kill();
            }
        }

        // 被鞭子或冰楔触发贯穿
        public void PierceThrust(Vector2 direction)
        {
            if (!embedded) return;
            embedded = false;
            flying = true;
            isThrusting = true;
            pierceTimer = 0;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.velocity = direction * 18f;
            Projectile.netUpdate = true;
            SoundEngine.PlaySound(SoundID.Item30 with { Pitch = 0.5f, Volume = 0.7f }, Projectile.Center);
        }
        #endregion

        #region ===== 打击模式碰撞动画 =====
        private void HandleSmashAnimation(Player player)
        {
            Projectile.timeLeft = 2;
            smashProgress += 0.12f;
            if (smashProgress >= 1f)
            {
                TriggerSmashCollision(player);
            }
            else
            {
                Vector2 startOffset = new Vector2(Projectile.ai[1], Projectile.ai[2]);
                Projectile.Center = player.Center + Vector2.Lerp(startOffset, Vector2.Zero, smashProgress);
            }
        }

        // 由控制器触发的对齐排列
        public void AlignForStrike(Vector2 centerLine, Vector2 orthoVec, int index, int totalCount)
        {
            if (hibernating || smashing) return;
            // 强制回归环绕状态以执行打击
            if (IsInAttackAction()) ReturnToOrbit();

            bool isLeftRow = index % 2 == 0;
            float rowOffset = isLeftRow ? 70f : -70f;
            float lineStep = (index / 2) * 50f + 40f;

            Vector2 targetPos = centerLine * lineStep + orthoVec * rowOffset;
            Projectile.Center = Vector2.Lerp(Projectile.Center, targetPos, 0.25f);
            Projectile.rotation = (centerLine * lineStep - (Projectile.Center - Main.player[Projectile.owner].Center)).ToRotation() + MathHelper.PiOver2;

            Projectile.ai[1] = (targetPos - Main.player[Projectile.owner].Center).X;
            Projectile.ai[2] = (targetPos - Main.player[Projectile.owner].Center).Y;
        }

        public void ExecuteSmash()
        {
            if (hibernating || smashing) return;
            smashing = true;
            smashProgress = 0f;
            if (IsInAttackAction()) ReturnToOrbit();
            SoundEngine.PlaySound(SoundID.Item30 with { Pitch = 0.4f, Volume = 0.5f }, Projectile.Center);
        }

        private void TriggerSmashCollision(Player player)
        {
            smashing = false;
            hibernating = true;
            hibernateTimer = 240; // 4 秒
            Projectile.friendly = false;

            var source = player.GetSource_ItemUse(player.HeldItem);
            int wave = Projectile.NewProjectile(source, Projectile.Center, Vector2.Zero,
                ProjectileID.SolarWhipSwordExplosion, (int)(Projectile.damage * 2.2f), 6f, player.whoAmI);
            if (Main.projectile.IndexInRange(wave))
            {
                Main.projectile[wave].DamageType = DamageClass.Summon;
                Main.projectile[wave].friendly = true;
                Main.projectile[wave].hostile = false;
            }

            for (int i = 0; i < 35; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(7f, 7f);
                int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Electric;
                Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, dType, vel.X, vel.Y);
                d.scale = Main.rand.NextFloat(1.3f, 2.2f);
                d.noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.8f, Pitch = 0.2f }, Projectile.Center);
            player.Calamity().GeneralScreenShakePower = Math.Max(player.Calamity().GeneralScreenShakePower, 3.5f);
        }
        #endregion

        #region ===== 终结技神钻装配 =====
        private void HandleUltimateAssembly(Player player)
        {
            Projectile.minionSlots = 0f;
            Projectile.friendly = true;

            int drillType = ModContent.ProjectileType<GlacialDrillProj>();
            Projectile drill = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == drillType)
                {
                    drill = p;
                    break;
                }
            }

            if (drill == null)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
        }
        #endregion

        #region ===== 碰撞与命中 =====
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            if (modPlayer.CurrentMode == 0 && IsCirclingPlayer())
            {
                bool isOuter = Projectile.ai[1] == 1f;
                float radius = isOuter ? 24f : 16f;
                Vector2 targetCenter = targetHitbox.Center.ToVector2();
                return Vector2.Distance(Projectile.Center, targetCenter) <= radius + targetHitbox.Width * 0.5f;
            }

            return base.Colliding(projHitbox, targetHitbox);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            if (modPlayer.CurrentMode == 0 && IsCirclingPlayer())
            {
                bool isOuter = Projectile.ai[1] == 1f;
                modifiers.SourceDamage *= isOuter ? 1.35f : 0.85f;
            }

            // 攻击状态增伤
            if (attackState == IceSpikeAttackState.FrostLanceRush)
                modifiers.SourceDamage *= 1.2f;
            else if (attackState == IceSpikeAttackState.AvalancheDive)
                modifiers.SourceDamage *= 1.5f;
            else if (attackState == IceSpikeAttackState.FrostRing)
                modifiers.SourceDamage *= 0.8f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            // 冻伤 Debuff
            target.AddBuff(BuffID.Frostburn2, 300);

            // 突刺模式嵌入逻辑（自动飞出时命中）
            if (modPlayer.CurrentMode == 1 && flying && !embedded && !isThrusting)
            {
                int embeddedCount = 0;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == Type && p.owner == player.whoAmI)
                    {
                        var modProj = p.ModProjectile as IceSpikeMinion;
                        if (modProj != null && modProj.embedded && modProj.embedNPCIndex == target.whoAmI)
                            embeddedCount++;
                    }
                }

                if (embeddedCount < 6)
                {
                    embedded = true;
                    flying = false;
                    embedNPCIndex = target.whoAmI;
                    embedOffset = Projectile.Center - target.Center;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.netUpdate = true;
                    SoundEngine.PlaySound(SoundID.Item30 with { Pitch = -0.3f, Volume = 0.8f }, Projectile.Center);
                }
                else
                {
                    Projectile.Kill();
                }
            }

            // 突刺模式寒冰突刺攻击命中嵌入
            if (modPlayer.CurrentMode == 1 && attackState == IceSpikeAttackState.FrostLanceRush)
            {
                TryEmbed(target, player);
            }

            // 打击模式攻击命中产生小冲击波
            if (modPlayer.CurrentMode == 2 && IsInAttackAction()
                && attackState != IceSpikeAttackState.CryoShardVolley)
            {
                for (int i = 0; i < 6; i++)
                {
                    Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Electric);
                    d.velocity = Main.rand.NextVector2Circular(4f, 4f);
                    d.scale = Main.rand.NextFloat(1.0f, 1.3f);
                    d.noGravity = true;
                }
            }
        }
        #endregion

        #region ===== 生死与绘制 =====
        public override void OnKill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            if (player.active && player.ownedProjectileCounts[ModContent.ProjectileType<GlacialDrillProj>()] == 0)
            {
                for (int i = 0; i < 15; i++)
                {
                    Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Ice);
                    d.velocity = Main.rand.NextVector2Circular(2.5f, 2.5f);
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            var modPlayer = Main.player[Projectile.owner].GetModPlayer<GlacialEmbracePlayer>();

            if (IsUltimateActive()) return false;

            float time = Main.GlobalTimeWrappedHourly;
            Vector2 drawCenter = Projectile.Center - Main.screenPosition;

            // 1. 斩击模式环绕绘制
            if (modPlayer.CurrentMode == 0 && attackState == IceSpikeAttackState.OrbitGuard && !embedded && !flying && !smashing)
            {
                bool isOuter = Projectile.ai[1] == 1f;
                float scale = isOuter ? 1.3f : 0.8f;
                Color drawCol = isOuter
                    ? Color.Lerp(new Color(0, 180, 255), Color.White, 0.3f + 0.3f * MathF.Sin(time * 6f)) * 0.95f
                    : new Color(130, 230, 255) * 0.9f;

                Main.spriteBatch.Draw(texture, drawCenter, null, drawCol, Projectile.rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                return false;
            }

            // 2. 休眠状态绘制
            if (hibernating)
            {
                Color sleepCol = new Color(50, 100, 150) * 0.3f;
                Main.spriteBatch.Draw(texture, drawCenter, null, sleepCol, Projectile.rotation, texture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                return false;
            }

            // 3. 攻击状态绘制：更亮的发光
            Color glowColor;
            float drawScale = 1.0f;

            if (IsInAttackAction())
            {
                // 攻击中更亮更大
                glowColor = Color.Lerp(new Color(100, 220, 255), Color.White, 0.4f + 0.3f * MathF.Sin(time * 10f));
                drawScale = 1.15f;
            }
            else
            {
                glowColor = Color.Lerp(new Color(0, 195, 255), Color.White, 0.2f + 0.2f * MathF.Sin(time * 6f));
            }

            Main.spriteBatch.Draw(texture, drawCenter, null, glowColor * 0.95f, Projectile.rotation, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
            return false;
        }
        #endregion
    }
}
