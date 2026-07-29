using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.Shared;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.Anchor
{
    /// <summary>
    /// 联动的唯一货币——“锚点”。两种类型：
    ///   · 硫火锚点（Kind 0）：左键火球落地/爆炸处的贴地化学余烬。自身只负责局部灼烧、可视范围与剩余时间；
    ///     低矮橙红火焰 + 轻微硫黄黄绿烟，绝不是高柱火墙。同时存在上限受 <see cref="PiscesBalance.BrimstoneAnchorCap"/> 约束，超出淘汰最早者。
    ///   · 光学标记锚点（Kind 1）：右键 Tier III 命中处的短暂极光标记，不造成伤害，只作串链节点，透明度低于主弹。
    /// 锚点会把自己注册进 owner 的 <see cref="PiscesPlayer.ActiveAnchors"/>，联动时只查这张表，从不遍历所有弹幕。
    /// </summary>
    public sealed class PiscesAnchor : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Pisces";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // ai[0] = Kind（0 硫火 / 1 光学标记），ai[1] = 强度 0..1
        public ref float Kind => ref Projectile.ai[0];
        public ref float Intensity => ref Projectile.ai[1];

        private bool registered;
        private int burnTimer;
        public bool IsBrimstone => Kind < 0.5f;

        /// <summary>联动串链时用的世界坐标（贴地锚点取脚下略偏）。</summary>
        public Vector2 LinkPoint => Projectile.Center;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 200;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true; // 硫火锚点靠 friendly + CanDamage 灼烧；光学标记在 OnSpawn 关掉 friendly
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = PiscesBalance.BrimstoneAnchorBurnInterval;
            Projectile.timeLeft = PiscesBalance.BrimstoneAnchorBaseLifetime;
        }

        /// <summary>统一生成入口：设置好判定尺寸与寿命，并（owner 侧）执行上限淘汰。</summary>
        public static int Spawn(Projectile source, Vector2 position, int kind, float intensity, int burnDamage)
        {
            int idx = Projectile.NewProjectile(source.GetSource_FromThis(), position, Vector2.Zero,
                ModContent.ProjectileType<PiscesAnchor>(), kind == 0 ? Math.Max(1, burnDamage) : 0, 0f,
                source.owner, kind, MathHelper.Clamp(intensity, 0f, 1f));
            return idx;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (IsBrimstone)
            {
                float radius = PiscesBalance.BrimstoneAnchorBurnRadius * (0.75f + Intensity * 0.5f);
                Projectile.Resize((int)(radius * 2f), (int)(radius * 1.4f)); // 贴地：横向铺开、纵向压扁
                Projectile.timeLeft = PiscesBalance.BrimstoneAnchorLifetime();
            }
            else
            {
                Projectile.timeLeft = 150;
                Projectile.friendly = false;
            }
        }

        public override bool? CanDamage() => IsBrimstone;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            // 注册进 owner 的锚点索引（所有客户端各自维护本地副本，随弹幕 spawn/kill 保持一致）。
            if (!registered)
            {
                registered = true;
                owner.GetModPlayer<PiscesPlayer>().RegisterAnchor(Projectile.whoAmI);
                if (IsBrimstone && Main.myPlayer == Projectile.owner)
                    EnforceBrimstoneCap(owner);
            }

            if (IsBrimstone)
                BrimstoneAI(owner);
            else
                OpticalAI();
        }

        private void BrimstoneAI(Player owner)
        {
            Projectile.velocity = Vector2.Zero;
            burnTimer++;

            float life = Projectile.timeLeft / (float)Math.Max(1, PiscesBalance.BrimstoneAnchorLifetime());
            float flick = 0.7f + 0.3f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 12f + Projectile.identity);
            Lighting.AddLight(Projectile.Center, PiscesVisuals.EmberOrange.ToVector3() * (0.5f * life * flick));

            if (Main.dedServ)
                return;

            // 贴地橙红火焰（低矮）——沿判定横向铺开，只往上冒一点点。
            float halfWidth = Projectile.width * 0.42f;
            int flames = Main.rand.Next(1, 3);
            for (int i = 0; i < flames; i++)
            {
                Vector2 basePos = Projectile.Center + new Vector2(Main.rand.NextFloat(-halfWidth, halfWidth), Projectile.height * 0.35f);
                Dust fire = Dust.NewDustPerfect(basePos, PiscesVisuals.BrimstoneDust,
                    new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-2.4f, -1f)) * (0.6f + Intensity * 0.5f),
                    0, default, Main.rand.NextFloat(0.9f, 1.5f) * (0.7f + Intensity * 0.4f) * life);
                fire.noGravity = true;
            }

            // 轻微硫黄黄绿烟（贴地慢慢散）
            if (Main.rand.NextBool(3))
            {
                Vector2 smokePos = Projectile.Center + new Vector2(Main.rand.NextFloat(-halfWidth, halfWidth), Projectile.height * 0.2f);
                Dust smoke = Dust.NewDustPerfect(smokePos, DustID.Smoke,
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-1.2f, -0.4f)), 120,
                    Color.Lerp(PiscesVisuals.SulfurGreen, Color.DarkOliveGreen, 0.4f), Main.rand.NextFloat(0.8f, 1.3f) * life);
                smoke.noGravity = true;
            }
        }

        private void OpticalAI()
        {
            Projectile.velocity = Vector2.Zero;
            float life = Projectile.timeLeft / 150f;
            Lighting.AddLight(Projectile.Center, PiscesVisuals.AuroraCyan.ToVector3() * (0.3f * life));

            if (Main.dedServ)
                return;
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2CircularEdge(14f, 14f),
                    PiscesVisuals.HolyDust, Vector2.Zero, 80, PiscesVisuals.AuroraWhite, Main.rand.NextFloat(0.6f, 1f) * life);
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsBrimstone)
                target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 40);
        }

        /// <summary>超出上限时，owner 端淘汰列表里最早的硫火锚点。</summary>
        private static void EnforceBrimstoneCap(Player owner)
        {
            PiscesPlayer mp = owner.GetModPlayer<PiscesPlayer>();
            int cap = PiscesBalance.BrimstoneAnchorCap;
            int brimstoneCount = 0;
            foreach (Projectile p in mp.EnumerateAnchors())
                if (p.ModProjectile is PiscesAnchor a && a.IsBrimstone)
                    brimstoneCount++;

            while (brimstoneCount > cap)
            {
                Projectile oldest = null;
                foreach (Projectile p in mp.EnumerateAnchors())
                {
                    if (p.ModProjectile is PiscesAnchor a && a.IsBrimstone)
                    {
                        oldest = p;
                        break; // 列表按生成先后排序，第一个硫火锚点即最早
                    }
                }
                if (oldest == null)
                    break;
                oldest.Kill();
                mp.UnregisterAnchor(oldest.whoAmI);
                brimstoneCount--;
            }
        }

        public override void OnKill(int timeLeft)
        {
            Main.player[Projectile.owner].GetModPlayer<PiscesPlayer>().UnregisterAnchor(Projectile.whoAmI);
            if (Main.dedServ)
                return;

            // 消耗 / 熄灭时的一记小 pop（硫火橙红，光学青白）。
            Color c = IsBrimstone ? PiscesVisuals.EmberOrange : PiscesVisuals.AuroraCyan;
            int dustType = IsBrimstone ? PiscesVisuals.BrimstoneDust : PiscesVisuals.HolyDust;
            int amt = IsBrimstone ? 8 : 5;
            for (int i = 0; i < amt; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, dustType,
                    Main.rand.NextVector2Circular(2.5f, 2.5f) + new Vector2(0, -1f), 0, c, Main.rand.NextFloat(0.9f, 1.4f));
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            float life = IsBrimstone
                ? Projectile.timeLeft / (float)Math.Max(1, PiscesBalance.BrimstoneAnchorLifetime())
                : Projectile.timeLeft / 150f;

            PiscesVisuals.BeginAdditive(Main.spriteBatch);
            if (IsBrimstone)
            {
                float flick = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 14f + Projectile.identity);
                float glow = (0.35f + Intensity * 0.35f) * life * flick;
                // 贴地椭圆余烬辉光（横向拉宽、纵向压扁）
                PiscesVisuals.DrawEnergyOrb(Main.spriteBatch, Projectile.Center + new Vector2(0, Projectile.height * 0.15f),
                    Projectile.width * 0.7f, PiscesVisuals.BrimLerp(0.4f), glow, new Vector2(1.15f, 0.5f));
            }
            else
            {
                float glow = 0.4f * life;
                float rot = Main.GlobalTimeWrappedHourly * 1.6f;
                // 小圆环 + 一条短朝向线（透明度低于主弹）
                PiscesVisuals.DrawRing(Main.spriteBatch, Projectile.Center, 12f + (1f - life) * 4f, rot, PiscesVisuals.AuroraCyan, glow);
                PiscesVisuals.DrawBeamSegment(Main.spriteBatch, Projectile.Center,
                    Projectile.Center + rot.ToRotationVector2() * 16f, PiscesVisuals.AuroraWhite with { A = 0 } * (glow * 0.7f), 4f);
            }
            PiscesVisuals.EndAdditive(Main.spriteBatch);
            return false;
        }
    }
}
