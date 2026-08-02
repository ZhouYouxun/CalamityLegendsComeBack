using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.M4A1
{
    /// <summary>
    /// 大招齐射的追踪炮弹：极快、近乎直线，命中造成剧烈超级爆炸。
    /// ai[0] = 锁定目标 NPC 索引（-1 = 无目标走直线）。
    /// </summary>
    public class M4A1UltimateShell : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int BlastRadius = 220;
        private bool exploding;
        private int TargetIndex => (int)Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (exploding)
            {
                Projectile.velocity *= 0.2f;
                return;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            // 极快 + 轻微修正：走直线奔向锁定目标
            if (TargetIndex >= 0 && TargetIndex < Main.maxNPCs)
            {
                NPC target = Main.npc[TargetIndex];
                if (target.active && target.CanBeChasedBy(Projectile))
                {
                    Vector2 toTarget = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
                    float speed = Projectile.velocity.Length();
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity.SafeNormalize(Vector2.UnitX), toTarget, 0.08f) * speed;
                }
            }

            // 高速尾迹
            Color hot = new(255, 120, 60);
            for (int i = 0; i < 2; i++)
            {
                Dust fire = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.5f, 0.5f), 70, hot, Main.rand.NextFloat(1.4f, 2.2f));
                fire.noGravity = true;
            }
            if (Main.rand.NextBool())
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(Projectile.Center, -Projectile.velocity * 0.05f, false, 10, Main.rand.NextFloat(0.3f, 0.55f), new Color(255, 150, 90), true, true));
            }
            Lighting.AddLight(Projectile.Center, 0.9f, 0.45f, 0.15f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Explode();
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Player owner = Main.player[Projectile.owner];
                bool isBoss = target.boss || NPCID.Sets.ShouldBeCountedAsBoss[target.type];
                M4A1Player.Get(owner).GainSync(isBoss, hit.Crit);
                M4A1MarkGlobalNPC.RegisterHit(target, owner, damageDone);
            }
            Explode();
        }

        private void Explode()
        {
            if (exploding)
                return;
            exploding = true;

            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = BlastRadius;
            Projectile.Center = center;
            Projectile.knockBack = 12f;
            if (Projectile.timeLeft > 3)
                Projectile.timeLeft = 3;

            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.9f, Pitch = -0.25f }, center);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.9f }, center);
            if (Main.myPlayer == Projectile.owner)
                Main.player[Projectile.owner].Calamity().GeneralScreenShakePower = Math.Max(Main.player[Projectile.owner].Calamity().GeneralScreenShakePower, 5f);
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 center = Projectile.Center;
            if (Main.dedServ)
                return;

            Color mark = M4A1Visuals.MarkColor;
            for (int i = 0; i < 40; i++)
            {
                Dust fire = Dust.NewDustPerfect(center, DustID.Torch, Main.rand.NextVector2Circular(11f, 11f), 60, default, Main.rand.NextFloat(2f, 3.6f));
                fire.noGravity = true;
            }
            for (int i = 0; i < 16; i++)
            {
                Dust smoke = Dust.NewDustPerfect(center, DustID.Smoke, Main.rand.NextVector2Circular(6f, 6f), 110, Color.DarkGray, Main.rand.NextFloat(2f, 3.2f));
                smoke.noGravity = true;
            }

            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, mark, 2.2f, 30, true));
            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, new Color(255, 190, 130), 1.5f, 24, true));
            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, Color.White, 0.8f, 16, true));
        }
    }
}
