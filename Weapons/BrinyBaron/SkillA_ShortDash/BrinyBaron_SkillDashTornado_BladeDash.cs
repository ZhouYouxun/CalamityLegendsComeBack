using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash
{
    public class BrinyBaron_SkillDashTornado_BladeDash : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron";

        // =========================
        // 冲刺参数
        // =========================
        private const int DashTimeMax = 30;
        private const float DashSpeed = 28f;
        private const float BounceBackSpeed = 16f;

        // =========================
        // 内部状态
        // =========================
        private Vector2 lockedDirection = Vector2.Zero;
        private int dashTimer = 0;
        private bool initialized = false;
        private bool hasBounced = false;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = DashTimeMax + 20;

            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;

            Projectile.DamageType = DamageClass.Melee;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Player owner = Main.player[Projectile.owner];

            // 锁定冲刺方向：出生时就决定，之后不再改方向
            lockedDirection = (Main.MouseWorld - owner.Center).SafeNormalize(Vector2.UnitX);

            Projectile.Center = owner.Center;
            Projectile.velocity = lockedDirection * DashSpeed;
            Projectile.rotation = lockedDirection.ToRotation() + MathHelper.PiOver4;

            initialized = true;
            dashTimer = 0;
            hasBounced = false;

            // 冲刺起手音效
            SoundEngine.PlaySound(SoundID.Item73 with
            {
                Volume = 0.65f,
                Pitch = 0.15f
            }, Projectile.Center);

            // 起手海浪爆开
            for (int i = 0; i < 14; i++)
            {
                Vector2 burstVel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f);

                Dust water = Dust.NewDustPerfect(Projectile.Center, DustID.Water, burstVel);
                water.noGravity = true;
                water.scale = Main.rand.NextFloat(1.1f, 1.5f);

                Dust frost = Dust.NewDustPerfect(Projectile.Center, DustID.Frost, burstVel * 0.7f);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(0.9f, 1.3f);
            }
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!initialized)
            {
                lockedDirection = (Main.MouseWorld - owner.Center).SafeNormalize(Vector2.UnitX);
                Projectile.velocity = lockedDirection * DashSpeed;
                Projectile.rotation = lockedDirection.ToRotation() + MathHelper.PiOver4;
                initialized = true;
            }

            dashTimer++;

            // 小冲刺：全程锁方向，不允许中途转向
            Projectile.velocity = lockedDirection * Projectile.velocity.Length();

            // 带着玩家一起冲
            owner.velocity = Projectile.velocity;
            owner.Center = Projectile.Center;

            // 旋转一点，增强钻刺感
            Projectile.rotation += 0.55f;

            // 海蓝色光照
            Lighting.AddLight(Projectile.Center, 0.05f, 0.22f, 0.30f);

            // 冲刺拖尾
            for (int i = 0; i < 2; i++)
            {
                Vector2 spawnPos = Projectile.Center - lockedDirection * Main.rand.NextFloat(18f, 42f);
                Vector2 dustVel = -lockedDirection.RotatedByRandom(0.45f) * Main.rand.NextFloat(1.5f, 5f);

                Dust water = Dust.NewDustPerfect(spawnPos, DustID.Water, dustVel);
                water.noGravity = true;
                water.scale = Main.rand.NextFloat(1.0f, 1.35f);

                Dust frost = Dust.NewDustPerfect(spawnPos, DustID.Frost, dustVel * 0.7f);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(0.85f, 1.15f);

                if (Main.rand.NextBool(3))
                {
                    Dust gem = Dust.NewDustPerfect(spawnPos, DustID.GemSapphire, dustVel * 0.5f);
                    gem.noGravity = true;
                    gem.scale = Main.rand.NextFloat(0.9f, 1.2f);
                }
            }

            // 时间到就结束
            if (dashTimer >= DashTimeMax)
            {
                Projectile.Kill();
                return;
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            // 反弹后不再造成二次命中
            if (hasBounced)
                return false;

            return null;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (hasBounced)
                return;

            hasBounced = true;

            // 冰冻灼烧
            target.AddBuff(BuffID.Frostburn, 180);

            // 命中爆裂特效
            for (int i = 0; i < 18; i++)
            {
                Vector2 burstVel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 9f);

                Dust water = Dust.NewDustPerfect(target.Center, DustID.Water, burstVel);
                water.noGravity = true;
                water.scale = Main.rand.NextFloat(1.15f, 1.6f);

                Dust frost = Dust.NewDustPerfect(target.Center, DustID.Frost, burstVel * 0.8f);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(1.0f, 1.4f);

                if (Main.rand.NextBool(2))
                {
                    Dust gem = Dust.NewDustPerfect(target.Center, DustID.GemSapphire, burstVel * 0.55f);
                    gem.noGravity = true;
                    gem.scale = Main.rand.NextFloat(1.0f, 1.3f);
                }
            }

            // 命中音效
            SoundEngine.PlaySound(SoundID.Item71 with
            {
                Volume = 0.7f,
                Pitch = 0.2f
            }, target.Center);

            // 把玩家和弹幕往反方向弹回去
            Vector2 backVelocity = -lockedDirection * BounceBackSpeed;

            Player owner = Main.player[Projectile.owner];
            owner.velocity = backVelocity;
            Projectile.velocity = backVelocity;

            // 反弹后立刻结束这次小冲刺
            Projectile.timeLeft = 8;
            dashTimer = DashTimeMax;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // 撞墙也做一个小型碎浪
            for (int i = 0; i < 12; i++)
            {
                Vector2 burstVel = Main.rand.NextVector2Circular(4f, 4f);

                Dust water = Dust.NewDustPerfect(Projectile.Center, DustID.Water, burstVel);
                water.noGravity = true;
                water.scale = Main.rand.NextFloat(1.0f, 1.35f);

                Dust frost = Dust.NewDustPerfect(Projectile.Center, DustID.Frost, burstVel * 0.8f);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(0.9f, 1.2f);
            }

            SoundEngine.PlaySound(SoundID.Item27 with
            {
                Volume = 0.5f,
                Pitch = 0.25f
            }, Projectile.Center);

            return true;
        }

        public override void OnKill(int timeLeft)
        {
            Player owner = Main.player[Projectile.owner];

            // 结束时别把玩家继续锁在弹幕上
            if (owner.active && !owner.dead && owner.Center == Projectile.Center)
            {
                owner.velocity *= 0.9f;
            }

            // 收尾水爆
            for (int i = 0; i < 14; i++)
            {
                Vector2 burstVel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 6.5f);

                Dust water = Dust.NewDustPerfect(Projectile.Center, DustID.Water, burstVel);
                water.noGravity = true;
                water.scale = Main.rand.NextFloat(1.0f, 1.45f);

                Dust frost = Dust.NewDustPerfect(Projectile.Center, DustID.Frost, burstVel * 0.75f);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(0.9f, 1.25f);
            }

            SoundEngine.PlaySound(SoundID.Item107 with
            {
                Volume = 0.45f,
                Pitch = 0.1f
            }, Projectile.Center);
        }
    }
}