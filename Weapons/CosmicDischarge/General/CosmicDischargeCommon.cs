using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Projectiles.Typeless;
using CalamityMod.Skies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    /// <summary>
    /// 事件强度分级。
    ///
    /// 预算上限锚定 DoG 本体最强的一次特效 —— DoGTeleportRift.SpawnExplosionVisuals：
    ///     25 SparkParticle + 20 SquishyLightParticle + 5 CustomPulse + 1 StrongBloom
    ///     + 25 DoGRiftCrack + ~50 DoGDistortionMetaball
    /// Ultimate 档 = 该数值本身；本武器任何单次事件都不得超过它。
    /// </summary>
    internal enum RiftTier
    {
        /// <summary>轻击（挥击擦到、非尖端）</summary>
        Light,
        /// <summary>普通命中</summary>
        Medium,
        /// <summary>重击 / 尖端命中</summary>
        Heavy,
        /// <summary>连招终结技 / QuickDraw</summary>
        Finisher,
        /// <summary>大招爆发 —— DoG 原始数值</summary>
        Ultimate
    }

    /// <summary>单次爆发的粒子配额。每个字段对应一种粒子，绝不在别处临时加料。</summary>
    internal readonly struct RiftBudget
    {
        public readonly int Sparks;
        public readonly int Lights;
        public readonly int Pulses;
        public readonly int Blooms;
        public readonly int Rings;
        public readonly int Cracks;
        public readonly int Metaballs;
        /// <summary>尺寸/速度总系数。DoG 是 Boss 尺度，武器取其一半左右。</summary>
        public readonly float Scale;

        public RiftBudget(int sparks, int lights, int pulses, int blooms, int rings, int cracks, int metaballs, float scale)
        {
            Sparks = sparks;
            Lights = lights;
            Pulses = pulses;
            Blooms = blooms;
            Rings = rings;
            Cracks = cracks;
            Metaballs = metaballs;
            Scale = scale;
        }
    }

    internal static class CosmicDischargeCommon
    {
        public const string ChainTexturePath = "CalamityLegendsComeBack/Weapons/CosmicDischarge/LeftClick/CosmicDischargeFlail";
        public const string RingTexturePath = "CalamityMod/Particles/BloomRing";

        private const int ChainHandleHeight = 62;
        private const int ChainBodyStartY = 64;
        private const int ChainBodyHeight = 28;
        private const int ChainTailStartY = 114;
        private const int ChainTailHeight = 84;
        private const float ChainBodyStartOffset = 30f;

        // ────────────────────────────────────────────────────────────────
        // 一、调色板 —— 全部取自 DoG 源码，绝不新增颜色
        // ────────────────────────────────────────────────────────────────

        /// <summary>DoGSky.DoGLightBlue —— (0, 221, 250)</summary>
        public static readonly Color RiftLightBlue = DoGSky.DoGLightBlue;

        /// <summary>DoGSky.DoGTwlight —— (147, 24, 204)（Calamity 源码拼写如此）</summary>
        public static readonly Color RiftTwilight = DoGSky.DoGTwlight;

        /// <summary>DoG 裂缝三色中的品红分量</summary>
        public static readonly Color RiftMagenta = Color.Fuchsia;

        public static readonly Color DoGWhiteColor = Color.White;

        /// <summary>
        /// 唯一取色入口 —— DoGTeleportRift 的原式。
        /// 三色随机取一再向白色插值 0.65，高度白化、低饱和：
        /// 这正是 DoG 大量粒子叠加仍不糊、仍有秩序感的根本原因。
        /// </summary>
        public static Color RiftColor() => Color.Lerp(
            Utils.SelectRandom(Main.rand, RiftMagenta, RiftLightBlue, RiftTwilight),
            Color.White,
            0.65f);

        /// <summary>
        /// DevourerofGodsHead.SpecialMoveColor 原式。
        /// 仅用于确定性绘制（拖尾内芯、传送门、UI 文本），不用于随机粒子。
        /// </summary>
        public static Color DoGSpecialColor =>
            Color.Lerp(
                RiftMagenta,
                RiftLightBlue,
                MathHelper.SmoothStep(0f, 1f, (MathF.Sin(Main.GlobalTimeWrappedHourly * 2f) + 1f) * 0.5f));

        /// <summary>形态标识色。只用在确定性绘制上，粒子一律走 <see cref="RiftColor"/>。</summary>
        public static Color GetModeColor(CosmicDischargeAttackMode mode) => mode switch
        {
            CosmicDischargeAttackMode.Whip => RiftLightBlue,
            CosmicDischargeAttackMode.Sword => RiftMagenta,
            CosmicDischargeAttackMode.ChainKnife => RiftTwilight,
            _ => DoGSpecialColor
        };

        public static Color Transparent(Color color) => new(color.R, color.G, color.B, 0);

        // ────────────────────────────────────────────────────────────────
        // 二、预算表
        // ────────────────────────────────────────────────────────────────

        private static RiftBudget GetBudget(RiftTier tier) => tier switch
        {
            //                        spark light pulse bloom ring crack metaball scale
            RiftTier.Light => new RiftBudget(5, 0, 0, 0, 1, 0, 0, 0.50f),
            RiftTier.Medium => new RiftBudget(8, 4, 0, 0, 1, 0, 0, 0.60f),
            RiftTier.Heavy => new RiftBudget(12, 7, 1, 0, 1, 3, 0, 0.72f),
            RiftTier.Finisher => new RiftBudget(18, 12, 2, 1, 1, 5, 6, 0.85f),
            _ => new RiftBudget(25, 20, 5, 1, 2, 8, 20, 1.00f),
        };

        /// <summary>DoG 爆炸的原始尺寸基准乘以该系数 —— Boss 尺度 → 武器尺度。</summary>
        private const float WeaponScale = 0.55f;

        // ────────────────────────────────────────────────────────────────
        // 三、唯一的爆发入口
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 所有命中/爆炸特效的唯一出口。传入分级即可，不接受额外加料。
        /// </summary>
        /// <param name="direction">有方向的事件（突刺、斩击）传入朝向，冲击环会沿其拉长；否则传 default。</param>
        /// <param name="accent">形态强调色，只影响冲击环与核心光，不影响火花。</param>
        public static void SpawnRiftBurst(Vector2 center, RiftTier tier, Vector2 direction = default, Color? accent = null)
        {
            if (Main.dedServ)
                return;

            RiftBudget budget = GetBudget(tier);
            float scale = budget.Scale;
            Color accentColor = accent ?? DoGSpecialColor;
            bool directional = direction != default && direction.LengthSquared() > 0.001f;
            if (directional)
                direction = direction.SafeNormalize(Vector2.UnitX);

            // 火花 —— DoG 爆炸原值 scale 1.8~2.0 / 速度 12~16，按武器尺度缩放。
            for (int i = 0; i < budget.Sparks; i++)
            {
                Vector2 velocity = Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(12f, 16f) * scale;
                if (directional)
                    velocity = Vector2.Lerp(velocity, direction * velocity.Length(), 0.35f);

                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    center,
                    velocity,
                    false,
                    Main.rand.Next(30, 45),
                    Main.rand.NextFloat(1.8f, 2f) * WeaponScale * scale,
                    RiftColor()));
            }

            // 能量光点 —— DoG 爆炸原值 scale 1.8~2.0 / 速度 16~20。
            for (int i = 0; i < budget.Lights; i++)
            {
                Vector2 velocity = Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(16f, 20f) * scale;
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    center,
                    velocity,
                    Main.rand.NextFloat(1.8f, 2f) * WeaponScale * scale,
                    RiftColor(),
                    Main.rand.Next(30, 45)));
            }

            // 闪光 —— DoG 用 DoGTwlight 的 PlasmaExplosion 打底，白色 ShineExplosion2 收尾。
            for (int i = 0; i < budget.Pulses; i++)
            {
                bool shine = i >= budget.Pulses - 2;
                string texture = shine
                    ? (i % 2 == 0 ? "CalamityMod/Particles/ShineExplosion1" : "CalamityMod/Particles/ShineExplosion2")
                    : "CalamityMod/Particles/PlasmaExplosion";
                Color pulseColor = shine ? Color.White * 0.6f : RiftTwilight * 0.8f;

                GeneralParticleHandler.SpawnParticle(new CustomPulse(
                    center,
                    Vector2.Zero,
                    pulseColor,
                    texture,
                    Vector2.One,
                    Main.rand.NextFloat(MathHelper.TwoPi),
                    0f,
                    (1.25f + i * 0.05f) * WeaponScale * scale,
                    45));
            }

            // 核心白光 —— DoG 用 8f，武器取其零头。
            for (int i = 0; i < budget.Blooms; i++)
                GeneralParticleHandler.SpawnParticle(new StrongBloom(
                    center,
                    Vector2.Zero,
                    Color.White * 0.8f,
                    8f * WeaponScale * scale * 0.5f,
                    30));

            // 冲击环 —— 唯一的环形元素，有方向就用定向环。
            for (int i = 0; i < budget.Rings; i++)
            {
                float ringScale = (0.7f + i * 0.35f) * scale;
                if (directional)
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                        center,
                        Vector2.Zero,
                        Transparent(accentColor) * 0.5f,
                        new Vector2(1.5f, 0.6f),
                        direction.ToRotation(),
                        0.04f,
                        ringScale,
                        20));
                else
                    GeneralParticleHandler.SpawnParticle(new PulseRing(
                        center,
                        Vector2.Zero,
                        Transparent(accentColor) * 0.5f,
                        0.04f,
                        ringScale,
                        20));
            }

            // 裂纹 —— 短促的线段，读作"现实开裂"。
            for (int i = 0; i < budget.Cracks; i++)
            {
                Vector2 crackDirection = (MathHelper.TwoPi * i / budget.Cracks + Main.rand.NextFloat(-0.18f, 0.18f)).ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    center + crackDirection * Main.rand.NextFloat(4f, 20f),
                    crackDirection * Main.rand.NextFloat(5f, 11f) * scale,
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.5f, 0.8f) * scale,
                    Transparent(RiftColor()) * 0.8f));
            }

            // 空间扭曲元球 —— DoG 爆炸用 15~20 个飞散球，size 30~50。
            for (int i = 0; i < budget.Metaballs; i++)
                DoGDistortionMetaball.SpawnParticle(
                    center,
                    Main.rand.NextVector2Unit() * Main.rand.NextFloat(25f, 35f) * scale,
                    Main.rand.NextFloat(30f, 50f) * scale);
        }

        /// <summary>
        /// 蓄力脉冲。DoG 的蓄力不是逐帧喷粒子，而是整个流程只触发 3 次
        /// (DoGTeleportRift 每 RiftLifetime/3 帧一次：12 Spark + 8 SquishyLight + 1 CustomPulse)。
        /// 调用方必须自己控制触发频率，不要每帧调。
        /// </summary>
        /// <param name="progress">蓄力进度 0→1，用于放大后续脉冲。</param>
        public static void SpawnChargePulse(Vector2 center, float progress, float scale = 1f)
        {
            if (Main.dedServ)
                return;

            // DoG 原式：强度随进度从 1 线性升到 2。
            float intensity = MathHelper.Lerp(1f, 2f, MathHelper.Clamp(progress, 0f, 1f)) * scale;

            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(8f, 12f) * intensity * 0.7f;
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    center,
                    velocity,
                    false,
                    Main.rand.Next(30, 45),
                    Main.rand.NextFloat(1.2f, 1.6f) * intensity * 0.62f * WeaponScale,
                    RiftColor()));
            }

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(12f, 14f) * intensity * 0.5f;
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    center,
                    velocity,
                    Main.rand.NextFloat(1.2f, 1.6f) * intensity * 0.62f * WeaponScale,
                    RiftColor(),
                    Main.rand.Next(30, 45)));
            }

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                Color.White,
                "CalamityMod/Particles/ShineExplosion2",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0f,
                0.5f * intensity * WeaponScale,
                45));
        }

        // ────────────────────────────────────────────────────────────────
        // 四、拖尾 —— 逐帧调用，配比严格照抄 DoGFire
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 逐帧拖尾。DoGFire 的原始配比：50% 概率 3 个烟雾 + 8% 概率 2 个 dust。
        /// 视觉主体由 primitive 拖尾承担，这里只是让边缘"活"起来，不承担观感。
        /// </summary>
        public static void SpawnTrailWake(Vector2 position, Vector2 backwardVelocity, Color innerColor, float scale = 1f)
        {
            if (Main.dedServ)
                return;

            if (Main.rand.NextBool(2))
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 smokeVelocity = backwardVelocity * 0.7f + Main.rand.NextVector2Circular(1f, 1f) * 0.65f;
                    Color flameColor = Color.Lerp(Color.White, innerColor, Main.rand.NextFloat(0.5f, 1f));

                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                        position + Main.rand.NextVector2Circular(8f, 8f),
                        smokeVelocity,
                        flameColor,
                        Main.rand.Next(10, 15),
                        Main.rand.NextFloat(0.25f, 0.45f) * scale,
                        Main.rand.NextFloat(0.7f, 0.9f),
                        0.02f,
                        true));
                }
            }

            if (Main.rand.NextBool(12))
            {
                for (int i = 0; i < 2; i++)
                {
                    Color dustColor = Color.Lerp(Color.White, innerColor, Main.rand.NextFloat(0.5f, 1f));
                    Dust dust = Dust.NewDustPerfect(
                        position + Main.rand.NextVector2Circular(6f, 6f),
                        DustID.TintableDustLighted,
                        backwardVelocity * 1.2f,
                        0,
                        dustColor,
                        Main.rand.NextFloat(0.6f, 0.8f) * scale);
                    dust.noGravity = true;
                }
            }
        }

        /// <summary>
        /// 挥砍弧。每次挥击**只调用一次**，不要逐帧调 —— 逐帧调正是原先"乱"的主因。
        /// </summary>
        public static void SpawnSwingSmear(Vector2 center, float angle, float scale, Color color)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new CircularSmearVFX(
                center,
                Transparent(color) * 0.55f,
                angle,
                scale));
        }

        // ────────────────────────────────────────────────────────────────
        // 五、DoG 原生技术 —— 弹幕裂缝 / 元球 / 激光
        // ────────────────────────────────────────────────────────────────

        /// <summary>生成真正的 DoGRiftCrack 弹幕（0 伤害，纯视觉，会被扭曲元球自动捕获绘制）。</summary>
        public static void SpawnRiftCrackProjectiles(IEntitySource source, Vector2 center, int owner, int count, float minLength, float maxLength, float minWidth, float maxWidth)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < count; i++)
            {
                Vector2 crackLength = (MathHelper.TwoPi * i / count).ToRotationVector2() * Main.rand.NextFloat(minLength, maxLength);
                Projectile.NewProjectile(source, center, crackLength, ModContent.ProjectileType<DoGRiftCrack>(), 0, 0f, owner, 0f, Main.rand.NextFloat(minWidth, maxWidth));
            }
        }

        /// <summary>空间扭曲元球。DoG 爆炸原值：飞散球 15~20 个、size 30~50。</summary>
        public static void SpawnDistortionBurst(Vector2 center, int count, float speed = 30f, float size = 40f)
        {
            for (int i = 0; i < count; i++)
                DoGDistortionMetaball.SpawnParticle(
                    center,
                    Main.rand.NextVector2Unit() * Main.rand.NextFloat(speed * 0.8f, speed * 1.2f),
                    Main.rand.NextFloat(size * 0.75f, size * 1.25f));
        }

        /// <summary>
        /// FriendlyLaserWallBeam 的正确用法：弹幕生成在**落点**上，
        /// 光束起点由弹幕自己算 (beamStart = target + dir * laserLength)。
        /// ai0 = attackSpeed（负值 = 瞬发），ai1 = laserType，ai2 = 震屏强度。
        /// </summary>
        public static void SpawnFriendlyLaser(IEntitySource source, Vector2 center, Vector2 direction, int damage, float knockBack, int owner, float attackSpeed, float scale, float shake)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            direction = direction.SafeNormalize(Vector2.UnitX);
            int laser = Projectile.NewProjectile(source, center, direction, ModContent.ProjectileType<FriendlyLaserWallBeam>(), damage, knockBack, owner, attackSpeed, 0f, shake);
            if (Main.projectile.IndexInRange(laser))
                Main.projectile[laser].scale *= scale;
        }

        // ────────────────────────────────────────────────────────────────
        // 六、复合事件 —— 玩法（激光）+ 特效（走预算表）
        // ────────────────────────────────────────────────────────────────

        /// <summary>链刃终结：索敌激光 + Finisher 档爆发。</summary>
        public static void SpawnChainFinisherBurst(IEntitySource source, Player player, Vector2 center, Vector2 aimDirection, int damage, float knockBack)
        {
            NPC target = FindNearestTarget(center, 1200f);
            if (target != null)
                SpawnFriendlyLaser(source, center, center.DirectionTo(target.Center), damage, knockBack, player.whoAmI, -1.5f, 0.45f, 2f);

            SpawnRiftBurst(center, RiftTier.Finisher, aimDirection, RiftTwilight);
        }

        /// <summary>QuickDraw 爆发：索敌激光 + Finisher 档爆发。</summary>
        public static void SpawnQuickDrawFullBurst(IEntitySource source, Player player, Vector2 center, int damage, float knockBack)
        {
            NPC target = FindNearestTarget(center, 1200f);
            if (target != null)
                SpawnFriendlyLaser(source, center, center.DirectionTo(target.Center), (int)(damage * 0.7f), knockBack, player.whoAmI, -1.5f, 0.5f, 2f);

            SpawnRiftBurst(center, RiftTier.Finisher, default, RiftMagenta);
            SpawnRiftCrackProjectiles(source, center, player.whoAmI, 10, 20f, 60f, 18f, 26f);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DoGLaserWallBigAttack") { Volume = 0.62f, MaxInstances = 2 }, center);
        }

        /// <summary>
        /// 大招爆发：全场索敌激光 + Ultimate 档爆发。
        /// Ultimate 档 = DoGTeleportRift 大爆炸的原始数值，是本武器的视觉天花板。
        /// </summary>
        public static void SpawnUltimateBurst(IEntitySource source, Player player, Vector2 center, int damage, float knockBack)
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy() || Vector2.DistanceSquared(npc.Center, center) > 1800f * 1800f)
                    continue;

                Vector2 direction = center.DirectionTo(npc.Center);
                SpawnFriendlyLaser(source, center, direction, damage, knockBack, player.whoAmI, 0f, 0.5f, 6f);
                SpawnFriendlyLaser(source, center, direction.RotatedBy(0.15f), (int)(damage * 0.6f), knockBack, player.whoAmI, 1.5f, 0.42f, 5f);
                SpawnFriendlyLaser(source, center, direction.RotatedBy(-0.15f), (int)(damage * 0.6f), knockBack, player.whoAmI, -1.5f, 0.42f, 5f);
            }

            // DoG 原始爆炸：25 裂缝弹幕（这里减半，因为武器不该盖满整屏）。
            SpawnRiftCrackProjectiles(source, center, player.whoAmI, 12, 20f, 80f, 20f, 30f);
            SpawnRiftBurst(center, RiftTier.Ultimate, default, DoGSpecialColor);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DoGLaserWallBigAttack") { Volume = 0.78f, MaxInstances = 2 }, center);
        }

        /// <summary>大招力场的持续氛围。每 20 帧一次，不逐帧喷。</summary>
        public static void SpawnUltimateFieldIdle(Vector2 center, float radius, float time)
        {
            if (Main.dedServ || time % 20f != 0f)
                return;

            for (int i = 0; i < 4; i++)
            {
                Vector2 edge = center + Main.rand.NextVector2CircularEdge(radius, radius);
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    edge,
                    (center - edge).SafeNormalize(Vector2.Zero) * 2f,
                    Main.rand.NextFloat(0.5f, 0.7f),
                    RiftColor(),
                    Main.rand.Next(25, 35)));
            }
        }

        /// <summary>形态切换传送门的伴随特效。只在开门(4)与定型(8)两帧触发。</summary>
        public static void SpawnSwitchPortalAI(Player player, Vector2 center, float time, CosmicDischargeAttackMode targetMode)
        {
            if (Main.dedServ)
                return;

            if (time == 4f)
                SpawnChargePulse(center, 0f, 0.5f);
            else if (time == 8f)
                SpawnRiftBurst(center, RiftTier.Medium, default, GetModeColor(targetMode));
        }

        // ────────────────────────────────────────────────────────────────
        // 六、非特效工具 —— 原样保留
        // ────────────────────────────────────────────────────────────────

        public static Vector2 GetAimDirection(Player player, Vector2 fallback)
        {
            Vector2 mouse = player.Calamity().mouseWorld;
            Vector2 direction = mouse - player.MountedCenter;
            if (direction.LengthSquared() < 0.001f)
                direction = fallback;

            if (direction.LengthSquared() < 0.001f)
                direction = Vector2.UnitX * player.direction;

            return direction.SafeNormalize(Vector2.UnitX * player.direction);
        }

        public static void HoldPlayer(Player player, Projectile projectile, Vector2 aimDirection, float armRotationOffset = 0f)
        {
            player.ChangeDir(aimDirection.X >= 0f ? 1 : -1);
            player.heldProj = projectile.whoAmI;
            player.itemTime = 2;
            player.itemAnimation = 2;
            player.itemRotation = aimDirection.ToRotation();

            float armRotation = aimDirection.ToRotation() - MathHelper.PiOver2 + armRotationOffset;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, armRotation);
        }

        public static List<Vector2> BuildCurvedBlade(Player player, Vector2 direction, float reach, float sideBend, float curl, int pointCount = 18)
        {
            List<Vector2> points = new(pointCount);
            Vector2 start = player.MountedCenter;
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                float forward = reach * t;
                float bend = sideBend * MathF.Sin(MathHelper.Pi * t);
                float wave = curl * MathF.Sin(MathHelper.TwoPi * t) * (1f - t * 0.35f);
                points.Add(start + direction * forward + normal * (bend + wave));
            }

            return points;
        }

        public static bool CheckCurveCollision(IReadOnlyList<Vector2> points, Rectangle targetHitbox, float width)
        {
            if (points == null || points.Count < 2)
                return false;

            for (int i = 0; i < points.Count - 1; i++)
            {
                float collisionPoint = 0f;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), points[i], points[i + 1], width, ref collisionPoint))
                    return true;
            }

            return false;
        }

        public static bool TargetIntersectsTip(IReadOnlyList<Vector2> points, Rectangle targetHitbox, float radius)
        {
            if (points == null || points.Count == 0)
                return false;

            Vector2 tip = points[^1];
            Vector2 closest = Vector2.Clamp(targetHitbox.Center.ToVector2(), targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.DistanceSquared(closest, tip) <= radius * radius;
        }

        public static void ApplyDoGDebuffs(NPC target, int duration)
        {
            if (target == null || !target.active)
                return;

            target.AddBuff(ModContent.BuffType<GodSlayerInferno>(), duration);
        }

        public static NPC FindNearestTarget(Vector2 center, float maxDistance)
        {
            NPC best = null;
            float bestDistance = maxDistance;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.Distance(center, npc.Center);
                if (distance < bestDistance)
                {
                    best = npc;
                    bestDistance = distance;
                }
            }

            return best;
        }

        public static bool HasOwnedProjectile(Player player, params int[] projectileTypes)
        {
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != player.whoAmI)
                    continue;

                for (int i = 0; i < projectileTypes.Length; i++)
                {
                    if (projectile.type == projectileTypes[i])
                        return true;
                }
            }

            return false;
        }

        // ────────────────────────────────────────────────────────────────
        // 七、链条绘制 —— 确定性绘制，非粒子
        // ────────────────────────────────────────────────────────────────

        public static void DrawChain(SpriteBatch spriteBatch, Vector2 startWorld, Vector2 endWorld, Color drawColor, float scale, bool rigid, float gfxOffY = 0f)
        {
            Texture2D texture = ModContent.Request<Texture2D>(ChainTexturePath).Value;
            Rectangle handleFrame = new(0, 0, texture.Width, ChainHandleHeight);
            Rectangle bodyFrame = new(0, ChainBodyStartY, texture.Width, ChainBodyHeight);
            Rectangle tailFrame = new(0, ChainTailStartY, texture.Width, ChainTailHeight);

            Vector2 chain = endWorld - startWorld;
            float chainLength = chain.Length();
            if (chainLength < 2f)
                return;

            Vector2 direction = chain / chainLength;
            float rotation = direction.ToRotation() + MathHelper.PiOver2;
            Vector2 drawOffset = Vector2.UnitY * gfxOffY;

            Main.EntitySpriteDraw(
                texture,
                startWorld - Main.screenPosition + drawOffset,
                handleFrame,
                drawColor,
                rotation,
                handleFrame.Size() * 0.5f,
                scale,
                SpriteEffects.FlipVertically);

            float startOffset = Math.Min(ChainBodyStartOffset * scale, chainLength);
            float tailLength = ChainTailHeight * scale;
            float bodyEndDistance = MathHelper.Clamp(chainLength - tailLength, startOffset, chainLength);
            float remaining = System.Math.Max(0f, bodyEndDistance - startOffset);
            Vector2 drawPosition = startWorld + direction * startOffset;

            while (remaining > 2f)
            {
                Rectangle drawFrame = bodyFrame;
                float segmentHeight = drawFrame.Height * scale;
                if (remaining < segmentHeight)
                {
                    int croppedHeight = (int)MathHelper.Clamp(remaining / scale, 2f, bodyFrame.Height);
                    drawFrame.Height = croppedHeight;
                    segmentHeight = croppedHeight * scale;
                }

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition - Main.screenPosition + drawOffset,
                    drawFrame,
                    drawColor,
                    rotation,
                    new Vector2(drawFrame.Width * 0.5f, 0f),
                    scale,
                    SpriteEffects.None);

                drawPosition += direction * segmentHeight;
                remaining -= segmentHeight;
            }

            Main.EntitySpriteDraw(
                texture,
                startWorld + direction * bodyEndDistance - Main.screenPosition + drawOffset,
                tailFrame,
                drawColor,
                rotation,
                new Vector2(tailFrame.Width * 0.5f, 0f),
                scale,
                SpriteEffects.FlipVertically);
        }

        public static void DrawCurvedChain(SpriteBatch spriteBatch, IReadOnlyList<Vector2> points, Color drawColor, float scale, float gfxOffY = 0f)
        {
            if (points == null || points.Count < 2)
                return;

            Texture2D texture = ModContent.Request<Texture2D>(ChainTexturePath).Value;
            Rectangle handleFrame = new(0, 0, texture.Width, ChainHandleHeight);
            Rectangle bodyFrame = new(0, ChainBodyStartY, texture.Width, ChainBodyHeight);
            Rectangle tailFrame = new(0, ChainTailStartY, texture.Width, ChainTailHeight);
            Vector2 drawOffset = Vector2.UnitY * gfxOffY;

            Vector2 firstDirection = (points[1] - points[0]).SafeNormalize(Vector2.UnitY);
            Main.EntitySpriteDraw(
                texture,
                points[0] - Main.screenPosition + drawOffset,
                handleFrame,
                drawColor,
                firstDirection.ToRotation() + MathHelper.PiOver2,
                handleFrame.Size() * 0.5f,
                scale,
                SpriteEffects.FlipVertically);

            float pathLength = 0f;
            for (int i = 0; i < points.Count - 1; i++)
                pathLength += Vector2.Distance(points[i], points[i + 1]);

            float bodyStartDistance = Math.Min(ChainBodyStartOffset * scale, pathLength);
            float bodyEndDistance = MathHelper.Clamp(pathLength - ChainTailHeight * scale, bodyStartDistance, pathLength);
            Vector2 tailPosition = points[^1];
            Vector2 lastDirection = (points[^1] - points[^2]).SafeNormalize(Vector2.UnitY);

            float traveled = 0f;
            bool foundTailPosition = false;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 segment = points[i + 1] - points[i];
                float segmentLength = segment.Length();
                if (segmentLength < 2f)
                    continue;

                Vector2 segmentDirection = segment / segmentLength;
                float segmentStartDistance = traveled;
                float segmentEndDistance = traveled + segmentLength;

                if (segmentEndDistance > bodyStartDistance && segmentStartDistance < bodyEndDistance)
                {
                    float localStart = Math.Max(bodyStartDistance, segmentStartDistance) - segmentStartDistance;
                    float localEnd = Math.Min(bodyEndDistance, segmentEndDistance) - segmentStartDistance;
                    if (localEnd - localStart > 2f)
                        DrawBodySegment(texture, bodyFrame, points[i] + segmentDirection * localStart, points[i] + segmentDirection * localEnd, drawColor, scale, drawOffset);
                }

                if (!foundTailPosition && bodyEndDistance <= segmentEndDistance)
                {
                    tailPosition = points[i] + segmentDirection * (bodyEndDistance - segmentStartDistance);
                    lastDirection = segmentDirection;
                    foundTailPosition = true;
                }

                traveled = segmentEndDistance;
            }

            Main.EntitySpriteDraw(
                texture,
                tailPosition - Main.screenPosition + drawOffset,
                tailFrame,
                Color.Lerp(drawColor, Color.White, 0.22f),
                lastDirection.ToRotation() + MathHelper.PiOver2,
                new Vector2(tailFrame.Width * 0.5f, 0f),
                scale,
                SpriteEffects.FlipVertically);
        }

        public static void DrawRightHoldIndicator(SpriteBatch spriteBatch, Player player, float intensity)
        {
            Texture2D ring = ModContent.Request<Texture2D>(RingTexturePath).Value;
            Vector2 drawPosition = player.Bottom - Main.screenPosition + new Vector2(0f, -6f + player.gfxOffY);
            Color ringColor = Transparent(RiftTwilight) * (0.3f * intensity);

            spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                ring,
                drawPosition,
                null,
                ringColor,
                0f,
                ring.Size() * 0.5f,
                new Vector2(0.85f, 0.28f) * (1f + 0.2f * intensity),
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                ring,
                drawPosition,
                null,
                Transparent(DoGSpecialColor) * (0.16f * intensity),
                Main.GlobalTimeWrappedHourly * 0.8f,
                ring.Size() * 0.5f,
                new Vector2(0.45f, 0.14f) * (1f + 0.15f * intensity),
                SpriteEffects.None);

            spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private static void DrawBodySegment(Texture2D texture, Rectangle frame, Vector2 start, Vector2 end, Color drawColor, float scale, Vector2 drawOffset)
        {
            Vector2 segment = end - start;
            float length = segment.Length();
            if (length < 2f)
                return;

            Vector2 direction = segment / length;
            float rotation = direction.ToRotation() + MathHelper.PiOver2;
            float step = frame.Height * scale;
            Vector2 position = start;

            for (float traveled = 0f; traveled < length; traveled += step)
            {
                float remaining = length - traveled;
                Rectangle drawFrame = frame;
                if (remaining < step)
                    drawFrame.Height = (int)MathHelper.Clamp(remaining / scale, 2f, frame.Height);

                Main.EntitySpriteDraw(
                    texture,
                    position - Main.screenPosition + drawFrame.Size() * 0f + drawOffset,
                    drawFrame,
                    drawColor,
                    rotation,
                    new Vector2(drawFrame.Width * 0.5f, 0f),
                    scale,
                    SpriteEffects.None);

                position += direction * step;
            }
        }
    }
}
