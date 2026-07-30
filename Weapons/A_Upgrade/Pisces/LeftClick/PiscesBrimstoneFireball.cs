using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.Anchor;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.Shared;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.LeftClick
{
    /// <summary>
    /// 左键硫火喷吐的小火球——固定骨架取自灾厄 Dragoon Drizzlefish 的 DrizzlefishFireball：
    /// 显形延迟、8 帧拖影、红橙火粒全部保留（视觉是纯灾厄硫火，不换通用火球贴图）。
    /// 双鱼座在其上叠加：竖直持续下坠（重力）、水平缓慢衰减、可多次命中、撞墙不立刻消失，
    /// 首次命中/落地时产生一次小范围硫火爆破，并留下 1 个短寿命地火锚点。
    /// </summary>
    public class PiscesBrimstoneFireball : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Pisces";
        public override string Texture => "CalamityMod/Projectiles/Ranged/DrizzlefishFire";

        public int Time;
        protected bool hasAnchored;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 5;
            Projectile.timeLeft = PiscesBalance.SmallFireballLifetime;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.scale = PiscesBalance.FireballScaleMult;
        }

        public override void AI()
        {
            Time++;

            // 重力：水平缓慢衰减，竖直持续下坠。
            Projectile.velocity.X *= PiscesBalance.SmallFireballDrag;
            Projectile.velocity.Y += PiscesBalance.SmallFireballGravity;
            if (Projectile.velocity.Y > PiscesBalance.SmallFireballMaxFallSpeed)
                Projectile.velocity.Y = PiscesBalance.SmallFireballMaxFallSpeed;

            Lighting.AddLight(Projectile.Center, 0.25f, 0f, 0f);
            // 保持 Dragoon Drizzlefish 的默认火球表现；新增内容只在大火球分裂时介入。
            EmitFireDust(2, 5f, 0.9f, 1.5f, 8);
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        /// <summary>纯灾厄 Drizzlefish 显形延迟 + 红橙火尘（第 7 tick 显形，第 4 tick 一次爆燃）。</summary>
        protected void EmitFireDust(int perTick, float spread, float minScale, float maxScale, int burstCount)
        {
            int dustType = Main.rand.NextBool() ? 183 : 90;
            if (Time > 7)
            {
                Projectile.alpha = 0;
                for (int i = 0; i < perTick; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(spread, spread) - Projectile.velocity * 1.5f, dustType, -Projectile.velocity);
                    dust.noGravity = true;
                    dust.velocity *= 0f;
                    dust.scale = Main.rand.NextFloat(minScale, maxScale) * Projectile.scale;
                }
            }
            else
                Projectile.alpha = 255;

            if (Time == 4)
            {
                for (int i = 0; i <= burstCount; i++)
                {
                    int dt = Main.rand.NextBool() ? 183 : 90;
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, dt, Projectile.velocity);
                    dust.scale = Main.rand.NextFloat(1.1f, 1.9f) * Projectile.scale;
                    dust.velocity = Projectile.velocity.RotatedByRandom(0.8f) * Main.rand.NextFloat(0.3f, 1.3f);
                    dust.noGravity = true;
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // 撞墙不立刻消失：缓速滑落；首次落地留下地火锚点 + 小硫火爆破。
            Projectile.velocity *= 0.7f;
            if (!hasAnchored)
                LeaveGroundFire(0.5f, PiscesBalance.SmallBurstRadius);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 30);
            if (!hasAnchored)
                LeaveGroundFire(0.5f, PiscesBalance.SmallBurstRadius, Projectile.Center);
        }

        /// <summary>落地/命中处：一次小范围硫火爆破 + 留下 1 个地火锚点（每颗火球只留一次）。</summary>
        protected void LeaveGroundFire(float intensity, float burstRadius, Vector2? at = null)
        {
            if (hasAnchored)
                return;
            hasAnchored = true;
            Vector2 pos = at ?? Projectile.Center;

            SpawnBrimstoneBurst(pos, burstRadius);

            if (Projectile.owner == Main.myPlayer)
            {
                int burnDamage = Math.Max(1, (int)(Projectile.damage * PiscesBalance.BrimstoneAnchorBurnDamageRatio));
                PiscesAnchor.Spawn(Projectile, pos, 0, intensity, burnDamage);
            }
        }

        protected static void SpawnBrimstoneBurst(Vector2 pos, float radius)
        {
            if (Main.dedServ)
                return;
            int count = (int)(radius * 0.28f);
            for (int i = 0; i < count; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3.5f, 2f) + new Vector2(0, -1.2f);
                Dust d = Dust.NewDustPerfect(pos, PiscesVisuals.BrimstoneDust, vel, 0,
                    PiscesVisuals.BrimLerp(Main.rand.NextFloat()), Main.rand.NextFloat(0.9f, 1.5f));
                d.noGravity = true;
            }

        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawDrizzlefishFire(Projectile, Time, lightColor);
            return false;
        }

        /// <summary>灾厄 Drizzlefish 的绘制方式（显形前拖影用不可见贴图，显形后用 DrizzlefishFire）。</summary>
        internal static void DrawDrizzlefishFire(Projectile projectile, int time, Color lightColor)
        {
            if (time < 7)
                CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], lightColor, 1, ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj").Value);
            else
                CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], lightColor, 1, ModContent.Request<Texture2D>("CalamityMod/Projectiles/Ranged/DrizzlefishFire").Value);
        }
    }

    /// <summary>
    /// 每第 4 发的大火球——取自 DrizzlefishFire：scale 约 1.5×，起飞 4 tick 一次爆燃、7 tick 显形，
    /// 飞行 ~45 tick 后分裂 3 枚扇形小火球（分裂弹继续受重力）。命中留下更强的地火锚点。
    /// 分裂瞬间只做一次圆形焰爆，不加任何蓝色光学效果。
    /// </summary>
    public class PiscesBrimstoneFireballBig : PiscesBrimstoneFireball
    {
        private int splitTimer = PiscesBalance.BigFireballSplitTime;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 90;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.scale = PiscesBalance.BigFireballScale * PiscesBalance.FireballScaleMult;
        }

        public override void AI()
        {
            Time++;
            Projectile.scale = PiscesBalance.BigFireballScale * PiscesBalance.FireballScaleMult;

            // 大火球主要直飞，带一点点下坠感。
            Projectile.velocity.Y += 0.08f;
            Lighting.AddLight(Projectile.Center, 0.25f, 0f, 0f);
            EmitFireDust(5, 9f, 1.2f, 1.9f, 16);

            splitTimer--;
            if (splitTimer <= 0)
            {
                SplitIntoFan();
                Projectile.Kill();
                return;
            }
            Projectile.rotation += 0.5f * Projectile.direction;
        }

        private void SplitIntoFan()
        {
            // 分裂瞬间一次圆形焰爆（无蓝色光学）
            SpawnBrimstoneBurst(Projectile.Center, PiscesBalance.BigBurstRadius);

            if (Projectile.owner != Main.myPlayer)
                return;
            int numProj = PiscesBalance.BigFireballSplitCount;
            float rotation = MathHelper.ToRadians(Main.rand.Next(15, 26));
            for (int i = 0; i < numProj; i++)
            {
                float lerp = numProj == 1 ? 0f : i / (float)(numProj - 1);
                Vector2 perturbed = Projectile.velocity.RotatedBy(MathHelper.Lerp(-rotation, rotation, lerp));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, perturbed,
                    ModContent.ProjectileType<PiscesBrimstoneFireballSplit>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            }

            // 新增层只保留两枚 TotalityFire 余烬：与原版三枚扇形火球错开，
            // 用清楚的侧翼轮廓丰富爆点，不再叠加会抢走主体的高速火花。
            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 emberVelocity = Projectile.velocity.RotatedBy(MathHelper.ToRadians(20f) * i) * 0.62f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, emberVelocity,
                    ModContent.ProjectileType<PiscesTotalityEmber>(), Math.Max(1, (int)(Projectile.damage * 0.42f)),
                    Projectile.knockBack * 0.35f, Projectile.owner);
            }

        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity *= 0.9f;
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 60);
            // 命中留下更强锚点
            LeaveGroundFire(1f, PiscesBalance.BigBurstRadius, Projectile.Center);
        }
    }

    /// <summary>
    /// 大火球分裂出的扇形小火球——取自 DrizzlefishFireSplit：快速下坠（重力更强）、水平衰减，
    /// 落地留下地火锚点。视觉仍是纯灾厄硫火。
    /// </summary>
    public class PiscesBrimstoneFireballSplit : PiscesBrimstoneFireball
    {
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.scale = PiscesBalance.FireballScaleMult;
        }

        public override void AI()
        {
            Time++;
            Projectile.velocity.X *= 0.98f;
            Projectile.velocity.Y += PiscesBalance.SplitFireballGravity;
            Lighting.AddLight(Projectile.Center, 0.25f, 0f, 0f);
            EmitFireDust(2, 4f, 0.4f, 0.8f, 0);
            Projectile.rotation += 0.3f * Projectile.direction;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity *= 0.6f;
            if (!hasAnchored)
                LeaveGroundFire(0.6f, PiscesBalance.SmallBurstRadius);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 30);
            if (!hasAnchored)
                LeaveGroundFire(0.6f, PiscesBalance.SmallBurstRadius, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            // 寿命结束的上冲火尘（灾厄同款）
            if (Main.dedServ)
                return;
            for (int i = 0; i <= 9; i++)
            {
                int dt = Main.rand.NextBool() ? 183 : 90;
                Dust d = Dust.NewDustPerfect(Projectile.Center, dt, new Vector2(0, -5).RotatedByRandom(MathHelper.ToRadians(15f)) * Main.rand.NextFloat(0.1f, 1.9f));
                d.scale = Main.rand.NextFloat(0.4f, 1.1f);
            }
        }
    }

    /// <summary>
    /// TotalityFire 的三帧火焰贴图被改造成射手余烬：只从大火球分裂时出现，
    /// 目的是让左键增加一层“不同火焰形状”，而非替换 Dragoon Drizzlefish 的主火球。
    /// </summary>
    public sealed class PiscesTotalityEmber : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Pisces";
        public override string Texture => "CalamityMod/Projectiles/Rogue/TotalityFire";

        private int time;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 3;
            ProjectileID.Sets.TrailCacheLength[Type] = 3;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 75;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            time++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity.X *= 0.985f;
            Projectile.velocity.Y += 0.22f;
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            Lighting.AddLight(Projectile.Center, PiscesVisuals.EmberOrange.ToVector3() * 0.35f);
            // 余烬不是连续烟幕：隔帧只留一颗拖后的 Dust，偶尔补一颗上浮硫黄热气。
            if (!Main.dedServ && time % 2 == 0)
            {
                int dustType = time % 4 == 0 ? PiscesVisuals.BrimstoneDust : (Main.rand.NextBool() ? 183 : 90);
                Dust dust = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.35f + Main.rand.NextVector2Circular(3f, 3f),
                    dustType, -Projectile.velocity * Main.rand.NextFloat(0.1f, 0.2f), 35,
                    PiscesVisuals.BrimLerp(Main.rand.NextFloat()), Main.rand.NextFloat(0.65f, 0.95f));
                dust.noGravity = true;

                if (time % 6 == 0)
                {
                    Dust heat = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, new Vector2(0f, -0.65f), 110,
                        PiscesVisuals.SulfurGreen, 0.55f);
                    heat.noGravity = true;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) =>
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 35);

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.TwoPi * i / 8f + Main.rand.NextFloat(-0.15f, 0.15f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, i % 2 == 0 ? PiscesVisuals.BrimstoneDust : 183,
                    angle.ToRotationVector2() * Main.rand.NextFloat(1.8f, 3.2f) + new Vector2(0f, -0.7f), 30,
                    PiscesVisuals.BrimLerp(Main.rand.NextFloat()), Main.rand.NextFloat(0.75f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White,
                Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
