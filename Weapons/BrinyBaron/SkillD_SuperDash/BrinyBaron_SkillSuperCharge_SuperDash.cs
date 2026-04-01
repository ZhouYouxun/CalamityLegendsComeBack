using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    public class BrinyBaron_SkillSuperCharge_SuperDash : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron";

        // =========================
        // 参数
        // =========================
        private const int ChargeTime = 90;
        private const int DashTime = 90;
        private const float DashSpeed = 55f;

        // =========================
        // 状态
        // =========================
        private int timer = 0;
        private bool isDashing = false;
        private Vector2 lockedDirection;

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = ChargeTime + DashTime + 30;

            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;

            Projectile.DamageType = DamageClass.Melee;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            timer++;

            // =========================
            // 蓄力阶段
            // =========================
            if (!isDashing)
            {
                Projectile.Center = owner.Center;
                owner.velocity *= 0.85f;

                Vector2 dir = (Main.MouseWorld - owner.Center).SafeNormalize(Vector2.UnitY);
                Projectile.rotation = dir.ToRotation() + MathHelper.PiOver4;

                // =========================
                // 超级蓄力特效（疯狂叠）
                // =========================
                for (int i = 0; i < 6; i++)
                {
                    Vector2 rand = Main.rand.NextVector2Circular(60f, 60f);

                    Dust d = Dust.NewDustPerfect(owner.Center + rand, DustID.Water, -rand * 0.05f);
                    d.noGravity = true;
                    d.scale = Main.rand.NextFloat(1.2f, 1.8f);

                    Dust f = Dust.NewDustPerfect(owner.Center + rand, DustID.Frost, -rand * 0.04f);
                    f.noGravity = true;
                    f.scale = Main.rand.NextFloat(1.0f, 1.5f);
                }

                // ⚡ 内核发光
                if (Main.rand.NextBool(2))
                {
                    Vector2 pulse = owner.Center + Main.rand.NextVector2Circular(20f, 20f);
                    Dust gem = Dust.NewDustPerfect(pulse, DustID.GemSapphire, Vector2.Zero);
                    gem.noGravity = true;
                    gem.scale = Main.rand.NextFloat(1.8f, 2.6f);
                }

                // =========================
                // 蓄力结束 → 冲刺
                // =========================
                if (timer >= ChargeTime)
                {
                    isDashing = true;
                    lockedDirection = dir;

                    Projectile.velocity = lockedDirection * DashSpeed;

                    SoundEngine.PlaySound(SoundID.Item74 with
                    {
                        Volume = 1.2f,
                        Pitch = -0.2f
                    }, Projectile.Center);

                    // 爆发
                    for (int i = 0; i < 40; i++)
                    {
                        Vector2 v = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(6f, 20f);

                        Dust d = Dust.NewDustPerfect(owner.Center, DustID.Water, v);
                        d.noGravity = true;
                        d.scale = Main.rand.NextFloat(1.5f, 2.5f);
                    }
                }
            }
            else
            {
                // =========================
                // 冲刺阶段
                // =========================
                Projectile.friendly = true;

                Projectile.velocity = lockedDirection * Projectile.velocity.Length();

                owner.velocity = Projectile.velocity;
                owner.Center = Projectile.Center;

                Projectile.rotation += 1.1f;

                Lighting.AddLight(Projectile.Center, 0.1f, 0.35f, 0.5f);

                // =========================
                // 超级拖尾（疯狂）
                // =========================
                for (int i = 0; i < 4; i++)
                {
                    Vector2 pos = Projectile.Center - lockedDirection * Main.rand.NextFloat(20f, 120f);

                    Vector2 vel = -lockedDirection.RotatedByRandom(0.6f) * Main.rand.NextFloat(5f, 18f);

                    Dust d = Dust.NewDustPerfect(pos, DustID.Water, vel);
                    d.noGravity = true;
                    d.scale = Main.rand.NextFloat(1.5f, 2.2f);

                    Dust f = Dust.NewDustPerfect(pos, DustID.Frost, vel * 0.7f);
                    f.noGravity = true;
                    f.scale = Main.rand.NextFloat(1.2f, 1.8f);

                    if (Main.rand.NextBool())
                    {
                        Dust gem = Dust.NewDustPerfect(pos, DustID.GemSapphire, vel * 0.5f);
                        gem.noGravity = true;
                        gem.scale = Main.rand.NextFloat(1.2f, 2.0f);
                    }
                }

                // =========================
                // 爆裂脉冲（周期）
                // =========================
                if (timer % 6 == 0)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 v = Main.rand.NextVector2Circular(10f, 10f);

                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Water, v);
                        d.noGravity = true;
                        d.scale = Main.rand.NextFloat(1.5f, 2.3f);
                    }

                    SoundEngine.PlaySound(SoundID.Item88 with
                    {
                        Volume = 0.6f,
                        Pitch = Main.rand.NextFloat(-0.2f, 0.2f)
                    }, Projectile.Center);
                }

                // =========================
                // 时间结束
                // =========================
                if (timer >= ChargeTime + DashTime)
                {
                    Projectile.Kill();
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 300);

            for (int i = 0; i < 25; i++)
            {
                Vector2 v = Main.rand.NextVector2Circular(12f, 12f);

                Dust d = Dust.NewDustPerfect(target.Center, DustID.Water, v);
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(1.6f, 2.5f);

                Dust f = Dust.NewDustPerfect(target.Center, DustID.Frost, v * 0.8f);
                f.noGravity = true;
                f.scale = Main.rand.NextFloat(1.4f, 2.0f);
            }

            SoundEngine.PlaySound(SoundID.Item14 with
            {
                Volume = 1.1f,
                Pitch = -0.1f
            }, target.Center);
        }

        public override void OnKill(int timeLeft)
        {
            // 最终爆炸
            for (int i = 0; i < 80; i++)
            {
                Vector2 v = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(8f, 30f);

                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Water, v);
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(1.8f, 3.0f);

                Dust f = Dust.NewDustPerfect(Projectile.Center, DustID.Frost, v * 0.8f);
                f.noGravity = true;
                f.scale = Main.rand.NextFloat(1.5f, 2.5f);
            }

            SoundEngine.PlaySound(SoundID.Item74 with
            {
                Volume = 1.3f,
                Pitch = -0.3f
            }, Projectile.Center);
        }
    }
}