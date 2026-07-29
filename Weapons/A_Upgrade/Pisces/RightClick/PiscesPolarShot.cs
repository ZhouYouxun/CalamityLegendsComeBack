using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.Anchor;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.Shared;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.RightClick
{
    /// <summary>
    /// 右键 I/II/III 级光弹——参照旧「北辰」的“等级提升会改变弹体性质”：
    ///   I（校准）：直线高速、无追踪；
    ///   II（聚焦）：速度更快、第 12 tick 后弱追踪，命中/撞墙裂成 2 枚折射碎片；
    ///   III（北辰锁定）：速度最快、第 10 tick 后追踪（优先被硫火灼烧的敌人），命中生成 1 个光学标记锚点并可触发联动；
    ///           途经地火锚点则折射（轻微改向最近目标并提前引爆，不增加额外弹幕）。
    /// 每一发在发射瞬间就把 ChargeTier 快照写进 ai[0]，之后绝不回读实时蓄力，避免发射后等级跳变。
    /// 轨迹保持干净：少量稳定极光光点 + 细尾，不用随机长 Spark。
    /// </summary>
    public class PiscesPolarShot : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Pisces";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // ai[0] = 发射瞬间的 ChargeTier 快照（0=I / 1=II / 2=III），ai[1] = 武器基准伤害（供联动爆破用）
        public int Tier => (int)Projectile.ai[0];
        public int BaseWeaponDamage => (int)Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private bool refracted;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = PiscesBalance.PolarShotLifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.scale = Tier switch { 2 => 1.25f, 1 => 1.05f, _ => 0.85f };
        }

        public override void AI()
        {
            Timer += 1f; // 每帧一步，追踪延迟按帧计（第 10/12 tick）
            Projectile.rotation = Projectile.velocity.ToRotation();

            Color light = PiscesVisuals.AuroraLerp(Tier / 2f);
            Lighting.AddLight(Projectile.Center, light.ToVector3() * 0.4f);

            switch (Tier)
            {
                case 1: // II：第 12 tick 后弱追踪
                    if (Timer >= PiscesBalance.TierIIHomeDelay)
                        Home(0.6f, preferBurned: false);
                    break;
                case 2: // III：第 10 tick 后追踪（优先硫火灼烧目标）；途经地火折射
                    if (Timer >= PiscesBalance.TierIIIHomeDelay)
                        Home(1f, preferBurned: true);
                    TryRefractOnGroundFire();
                    break;
            }

            EmitTrailPoints();
        }

        private void Home(float strengthScale, bool preferBurned)
        {
            NPC target = FindHomingTarget(Projectile.Center, PiscesBalance.PolarShotHomeRange, preferBurned);
            if (target == null)
                return;
            Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * Projectile.velocity.Length();
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, PiscesBalance.PolarShotHomeStrength * strengthScale);
        }

        private static NPC FindHomingTarget(Vector2 pos, float range, bool preferBurned)
        {
            NPC best = null;
            float bestScore = float.MaxValue;
            int brimstone = ModContent.BuffType<BrimstoneFlames>();
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;
                float dist = Vector2.Distance(pos, npc.Center);
                if (dist > range)
                    continue;
                float score = dist;
                if (preferBurned && npc.HasBuff(brimstone))
                    score *= 0.45f; // 明显偏向被硫火灼烧的敌人
                if (score < bestScore)
                {
                    bestScore = score;
                    best = npc;
                }
            }
            return best;
        }

        /// <summary>III 段途经硫火地火锚点 → 折射：轻微改向最近目标并提前引爆（不增加额外弹幕）。</summary>
        private void TryRefractOnGroundFire()
        {
            if (refracted || Main.myPlayer != Projectile.owner)
                return;
            PiscesPlayer mp = Main.player[Projectile.owner].GetModPlayer<PiscesPlayer>();
            foreach (Projectile p in mp.EnumerateAnchors())
            {
                if (p.ModProjectile is not PiscesAnchor a || !a.IsBrimstone)
                    continue;
                if (Vector2.Distance(p.Center, Projectile.Center) > 40f)
                    continue;

                refracted = true;
                NPC target = FindHomingTarget(Projectile.Center, PiscesBalance.PolarShotHomeRange, true);
                if (target != null)
                {
                    Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
                    Projectile.velocity = dir * Projectile.velocity.Length();
                }
                // 提前引爆：加速逼近命中（缩短寿命，不额外生成弹幕）
                if (Projectile.timeLeft > 24)
                    Projectile.timeLeft = 24;
                SpawnRefractFlash();
                Projectile.netUpdate = true;
                break;
            }
        }

        private void SpawnRefractFlash()
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, PiscesVisuals.BrimstoneDust,
                    Main.rand.NextVector2Circular(3f, 3f), 0, PiscesVisuals.BrimLerp(Main.rand.NextFloat()), Main.rand.NextFloat(0.9f, 1.4f));
                d.noGravity = true;
            }
        }

        private void EmitTrailPoints()
        {
            if (Main.dedServ || !Main.rand.NextBool(Tier == 0 ? 3 : 2))
                return;
            Color c = PiscesVisuals.AuroraLerp(Main.rand.NextFloat(0.4f, 1f));
            Dust d = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.5f, PiscesVisuals.HolyDust,
                -Projectile.velocity * 0.05f, 60, c, Main.rand.NextFloat(0.7f, 1.1f) * Projectile.scale);
            d.noGravity = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            switch (Tier)
            {
                case 2:
                    // III：优先生成 1 个光学标记锚点；若目标被硫火灼烧则触发联动串链
                    if (Projectile.owner == Main.myPlayer)
                    {
                        PiscesAnchor.Spawn(Projectile, target.Center, 1, 0.7f, 0);
                        if (target.HasBuff(ModContent.BuffType<BrimstoneFlames>()))
                            PiscesLinkSystem.TryLinkFromTierIIIShot(Projectile, target, BaseWeaponDamage);
                    }
                    break;
                case 1:
                    SplitIntoRefractionShards();
                    break;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Tier == 1)
                SplitIntoRefractionShards();
            return true;
        }

        /// <summary>II 级命中/撞墙裂成 2 枚低伤害折射碎片。</summary>
        private void SplitIntoRefractionShards()
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            int shardDamage = Math.Max(1, (int)(BaseWeaponDamage * PiscesBalance.RefractionShardDamageMult));
            Vector2 baseDir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 2; i++)
            {
                float sign = i == 0 ? -1f : 1f;
                Vector2 vel = baseDir.RotatedBy(sign * MathHelper.ToRadians(28f)) * (Projectile.velocity.Length() * 0.7f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel,
                    ModContent.ProjectileType<PiscesRefractionShard>(), shardDamage, Projectile.knockBack * 0.4f, Projectile.owner);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;
            PiscesVisuals.BeginAdditive(Main.spriteBatch);

            // 干净细尾（少量稳定光点）
            for (int i = 1; i < ProjectileID.Sets.TrailCacheLength[Type]; i++)
            {
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                if (pos == Projectile.Size * 0.5f - Main.screenPosition)
                    continue;
                float fade = 1f - i / (float)ProjectileID.Sets.TrailCacheLength[Type];
                PiscesVisuals.DrawBloom(Main.spriteBatch, pos + Main.screenPosition, 0.05f * Projectile.scale * fade,
                    PiscesVisuals.AuroraCyan, 0.5f * fade);
            }

            Vector2 center = Projectile.Center;
            float tierGlow = 0.6f + Tier * 0.2f;
            // 极光主带（青蓝）+ 白核 + 金白薄边
            PiscesVisuals.DrawEnergyOrb(Main.spriteBatch, center, 20f * Projectile.scale, PiscesVisuals.AuroraCyan, tierGlow, new Vector2(1f, 1f));

            // III 级重弹使用本地 UltimaBolt 图形作“被压缩的极光箭核”。
            // 原图纵向，因此沿当前飞行角再补 Pi/2；只给最高级，不污染 I/II 的小型光弹身份。
            if (Tier >= 2)
            {
                Texture2D ultimaBolt = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/Calamity/RangePROJ/UltimaBolt").Value;
                Main.EntitySpriteDraw(ultimaBolt, center - Main.screenPosition, null,
                    PiscesVisuals.AuroraWhite with { A = 0 }, Projectile.rotation + MathHelper.PiOver2,
                    ultimaBolt.Size() * 0.5f, Projectile.scale * 0.82f, SpriteEffects.None, 0f);
            }
            PiscesVisuals.DrawBloom(Main.spriteBatch, center, 0.09f * Projectile.scale, PiscesVisuals.AuroraWhite, 0.9f);
            PiscesVisuals.DrawBloom(Main.spriteBatch, center, 0.16f * Projectile.scale, PiscesVisuals.GoldWhite, 0.3f);

            // II+ 稳定极光环
            if (Tier >= 1)
                PiscesVisuals.DrawRing(Main.spriteBatch, center, 14f * Projectile.scale, Main.GlobalTimeWrappedHourly * 2.4f, PiscesVisuals.AuroraCyan, 0.5f);

            PiscesVisuals.EndAdditive(Main.spriteBatch);
            return false;
        }
    }

    /// <summary>II 级裂出的低伤害折射碎片——短命、直飞、青白细光。</summary>
    public class PiscesRefractionShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Pisces";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = 40;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.99f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, PiscesVisuals.AuroraCyan.ToVector3() * 0.25f);
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, PiscesVisuals.HolyDust, Vector2.Zero, 80, PiscesVisuals.AuroraWhite, 0.7f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;
            PiscesVisuals.BeginAdditive(Main.spriteBatch);
            PiscesVisuals.DrawBloom(Main.spriteBatch, Projectile.Center, 0.06f, PiscesVisuals.AuroraCyan, 0.7f);
            PiscesVisuals.DrawBloom(Main.spriteBatch, Projectile.Center, 0.035f, PiscesVisuals.AuroraWhite, 0.9f);
            PiscesVisuals.EndAdditive(Main.spriteBatch);
            return false;
        }
    }
}
