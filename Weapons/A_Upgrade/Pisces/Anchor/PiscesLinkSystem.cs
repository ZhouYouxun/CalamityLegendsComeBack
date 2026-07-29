using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.Shared;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.Anchor
{
    /// <summary>
    /// 联动的选择与串联逻辑——把左键留下的硫火锚点用右键精确串起来。
    /// 所有伤害性生成都由 owner client 发起并同步（<see cref="PiscesLinkBurst"/> / 锚点消耗）；
    /// 链线（<see cref="PiscesLinkChain"/>）只传达因果，且不能比激光本体更亮。
    /// 选锚点只查 owner 维护的 <see cref="PiscesPlayer.ActiveAnchors"/>，从不遍历所有弹幕。
    /// </summary>
    internal static class PiscesLinkSystem
    {
        /// <summary>触发条件①：Tier III 光弹命中被硫火灼烧的 NPC → 以目标为中心找最近 2 个硫火锚点串链，交点小爆破。消耗 1 个。</summary>
        public static void TryLinkFromTierIIIShot(Projectile shot, NPC target, int baseDamage)
        {
            if (Main.myPlayer != shot.owner)
                return;

            PiscesPlayer mp = Main.player[shot.owner].GetModPlayer<PiscesPlayer>();
            List<Projectile> anchors = FindNearestBrimstoneAnchors(mp, target.Center, PiscesBalance.LinkSearchRadius, PiscesBalance.LinkAnchorsPerShot);
            if (anchors.Count == 0)
                return;

            // 画链：每个锚点 → 目标（细青白，只传因果）
            foreach (Projectile a in anchors)
                SpawnChain(shot, a.Center, target.Center);

            // 交点（目标处）一次小型化学爆破
            int burstDamage = Math.Max(1, (int)(baseDamage * PiscesBalance.LinkBurstDamageRatio));
            SpawnBurst(shot, target.Center, burstDamage, PiscesBalance.LinkBurstRadius);

            // 只消耗最近的 1 个
            ConsumeAnchor(mp, anchors[0]);
        }

        /// <summary>触发条件②：满蓄双束激光擦过锚点 → 沿方向选前方最近 3 个锚点，依次串链，末点最大爆。全链 0.75s 内部冷却，最多消耗 3 个。</summary>
        public static void TryLinkFromBeam(Projectile beam, Vector2 origin, Vector2 dir, int baseDamage)
        {
            if (Main.myPlayer != beam.owner)
                return;

            PiscesPlayer mp = Main.player[beam.owner].GetModPlayer<PiscesPlayer>();
            if (mp.BeamLinkCooldown > 0)
                return;

            List<Projectile> anchors = FindAnchorsAlongDirection(mp, origin, dir, PiscesBalance.LinkAnchorsPerBeam, PiscesBalance.HolyBeamLength);
            if (anchors.Count == 0)
                return;

            mp.BeamLinkCooldown = PiscesBalance.BeamLinkCooldown;

            Vector2 prev = origin;
            for (int i = 0; i < anchors.Count; i++)
            {
                SpawnChain(beam, prev, anchors[i].Center);
                prev = anchors[i].Center;
            }

            // 末点最大爆，前面各点小爆
            for (int i = 0; i < anchors.Count; i++)
            {
                bool terminal = i == anchors.Count - 1;
                float ratio = PiscesBalance.LinkBurstDamageRatio * (terminal ? 1.4f : 0.7f);
                int burstDamage = Math.Max(1, (int)(baseDamage * ratio));
                SpawnBurst(beam, anchors[i].Center, burstDamage, PiscesBalance.LinkBurstRadius * (terminal ? 1.2f : 0.85f));
                ConsumeAnchor(mp, anchors[i]);
            }
        }

        // ---- 选择算法（只查 owner 锚点表）----
        public static List<Projectile> FindNearestBrimstoneAnchors(PiscesPlayer mp, Vector2 center, float radius, int count)
        {
            List<(Projectile p, float dist)> found = new();
            float r2 = radius * radius;
            foreach (Projectile p in mp.EnumerateAnchors())
            {
                if (p.ModProjectile is not PiscesAnchor a || !a.IsBrimstone)
                    continue;
                float d2 = Vector2.DistanceSquared(p.Center, center);
                if (d2 <= r2)
                    found.Add((p, d2));
            }
            found.Sort((x, y) => x.dist.CompareTo(y.dist));
            List<Projectile> result = new();
            for (int i = 0; i < found.Count && i < count; i++)
                result.Add(found[i].p);
            return result;
        }

        public static List<Projectile> FindAnchorsAlongDirection(PiscesPlayer mp, Vector2 origin, Vector2 dir, int count, float maxDist)
        {
            dir = dir.SafeNormalize(Vector2.UnitX);
            List<(Projectile p, float proj)> found = new();
            foreach (Projectile p in mp.EnumerateAnchors())
            {
                Vector2 offset = p.Center - origin;
                float proj = Vector2.Dot(offset, dir);
                if (proj <= 0f || proj > maxDist)
                    continue;
                // 只取贴近激光轴线的锚点（横向偏移不超过 90px）
                float perp = Math.Abs(Vector2.Dot(offset, dir.RotatedBy(MathHelper.PiOver2)));
                if (perp > 90f)
                    continue;
                found.Add((p, proj));
            }
            found.Sort((x, y) => x.proj.CompareTo(y.proj));
            List<Projectile> result = new();
            for (int i = 0; i < found.Count && i < count; i++)
                result.Add(found[i].p);
            return result;
        }

        private static void ConsumeAnchor(PiscesPlayer mp, Projectile anchor)
        {
            if (!anchor.active)
                return;
            mp.UnregisterAnchor(anchor.whoAmI);
            anchor.Kill(); // 消耗 = 击杀（同步），OnKill 播一记 pop
        }

        private static void SpawnChain(Projectile source, Vector2 start, Vector2 end)
        {
            Projectile.NewProjectile(source.GetSource_FromThis(), start, Vector2.Zero,
                ModContent.ProjectileType<PiscesLinkChain>(), 0, 0f, source.owner, end.X, end.Y);
        }

        private static void SpawnBurst(Projectile source, Vector2 pos, int damage, float radius)
        {
            int idx = Projectile.NewProjectile(source.GetSource_FromThis(), pos, Vector2.Zero,
                ModContent.ProjectileType<PiscesLinkBurst>(), damage, 0f, source.owner, radius);
            if (Main.projectile.IndexInRange(idx))
                Main.projectile[idx].netUpdate = true;
        }
    }

    /// <summary>
    /// 联动链线（纯视觉，同步生成让所有客户端看到同一条因果链）。
    /// 绘制顺序：先在起点（锚点）出现 4-6 tick 蓝白内环，再出现 1 tick 细青白线；亮度低于激光本体。
    /// </summary>
    public sealed class PiscesLinkChain : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Pisces";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Vector2 End => new(Projectile.ai[0], Projectile.ai[1]);
        private const int LifeTime = 14;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 8;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifeTime;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            float t = 1f - Projectile.timeLeft / (float)LifeTime;
            Vector2 mid = Vector2.Lerp(Projectile.Center, End, 0.5f);
            Lighting.AddLight(mid, PiscesVisuals.ChainCyan.ToVector3() * 0.25f * (1f - t));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;
            float age = LifeTime - Projectile.timeLeft; // 0..LifeTime
            float ringT = MathHelper.Clamp(age / 6f, 0f, 1f);     // 前 6 tick 内环生长
            float lineT = MathHelper.Clamp((age - 3f) / 4f, 0f, 1f); // 第 3 tick 后线出现
            float fade = MathHelper.Clamp(Projectile.timeLeft / 6f, 0f, 1f);

            PiscesVisuals.BeginAdditive(Main.spriteBatch);
            // 起点蓝白内环
            PiscesVisuals.DrawRing(Main.spriteBatch, Projectile.Center, 8f + ringT * 10f,
                Main.GlobalTimeWrappedHourly * 3f, PiscesVisuals.AuroraWhite, 0.5f * fade * ringT);
            // 细青白链线（低于激光亮度）
            if (lineT > 0f)
            {
                Vector2 drawEnd = Vector2.Lerp(Projectile.Center, End, lineT);
                PiscesVisuals.DrawBeamSegment(Main.spriteBatch, Projectile.Center, drawEnd,
                    PiscesVisuals.ChainCyan with { A = 0 } * (0.55f * fade), 6f);
                PiscesVisuals.DrawBeamSegment(Main.spriteBatch, Projectile.Center, drawEnd,
                    PiscesVisuals.AuroraWhite with { A = 0 } * (0.35f * fade), 2.4f);
            }
            PiscesVisuals.EndAdditive(Main.spriteBatch);
            return false;
        }
    }

    /// <summary>
    /// 联动终点的定向爆发——蓝白收束一瞬后炸成橙红化学爆破（终点 FlameExplosion 感）。
    /// 由 owner 生成并同步；只在很短的伤害帧内造成范围伤害。
    /// </summary>
    public sealed class PiscesLinkBurst : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Pisces";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private float Radius => Projectile.ai[0];
        private const int LifeTime = 16;
        private bool spawnedVisual;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = LifeTime;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            // 伤害只在爆开的一小段生效（第 3-8 tick），此前是蓝白收束。
            int age = LifeTime - Projectile.timeLeft;
            if (age == 3 && !spawnedVisual)
            {
                spawnedVisual = true;
                Projectile.Resize((int)(Radius * 2f), (int)(Radius * 2f)); // 展开判定
                SpawnBurstVisual();
            }
            Lighting.AddLight(Projectile.Center, PiscesVisuals.EmberOrange.ToVector3() * 0.6f * (Projectile.timeLeft / (float)LifeTime));
        }

        public override bool? CanDamage()
        {
            int age = LifeTime - Projectile.timeLeft;
            return age >= 3 && age <= 8;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 90);
        }

        private void SpawnBurstVisual()
        {
            if (Main.dedServ)
                return;
            // 橙红焰爆尘 + 一圈硫火
            for (int i = 0; i < 22; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, PiscesVisuals.BrimstoneDust, vel, 0,
                    PiscesVisuals.BrimLerp(Main.rand.NextFloat()), Main.rand.NextFloat(1.1f, 1.9f));
                d.noGravity = true;
            }
            for (int i = 0; i < 6; i++)
            {
                Dust smoke = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(Radius * 0.4f, Radius * 0.4f),
                    DustID.Smoke, Main.rand.NextVector2Circular(1.5f, 1.5f), 140,
                    Color.Lerp(PiscesVisuals.SulfurGreen, Color.DarkOliveGreen, 0.5f), Main.rand.NextFloat(1f, 1.5f));
                smoke.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;
            int age = LifeTime - Projectile.timeLeft;
            float t = age / (float)LifeTime;

            PiscesVisuals.BeginAdditive(Main.spriteBatch);
            if (age < 3)
            {
                // 蓝白收束点（爆前一瞬）
                float gather = PiscesVisuals.GatherPulse(age / 3f);
                PiscesVisuals.DrawEnergyOrb(Main.spriteBatch, Projectile.Center, Radius * 0.4f * gather,
                    PiscesVisuals.AuroraWhite, 0.7f, Vector2.One);
            }
            else
            {
                // 橙红定向爆发环 + 核心
                float expand = PiscesVisuals.ShockwaveExpand((age - 3f) / (LifeTime - 3f));
                float fade = PiscesVisuals.BurstFade((age - 3f) / (LifeTime - 3f));
                PiscesVisuals.DrawRing(Main.spriteBatch, Projectile.Center, Radius * expand,
                    Main.GlobalTimeWrappedHourly * 2f, PiscesVisuals.BrimstoneRed, 0.8f * fade);
                PiscesVisuals.DrawEnergyOrb(Main.spriteBatch, Projectile.Center, Radius * 0.55f * (1f - expand * 0.4f),
                    PiscesVisuals.EmberOrange, 0.7f * fade, Vector2.One);
            }
            PiscesVisuals.EndAdditive(Main.spriteBatch);
            return false;
        }
    }
}
