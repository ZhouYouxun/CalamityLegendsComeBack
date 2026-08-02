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
    /// 左键暖机时不时甩出的火箭弹。命中/触地引爆，造成小范围战术爆破。
    /// （印记增益——追踪 / 更大范围 / 强化爆炸——在 Phase 2/3 接线。）
    /// </summary>
    public class M4A1Rocket : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int BaseBlastRadius = 120;
        private bool exploding;

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (!exploding)
            {
                if (Projectile.velocity != Vector2.Zero)
                    Projectile.rotation = Projectile.velocity.ToRotation();

                // 火焰尾迹
                for (int i = 0; i < 2; i++)
                {
                    Dust fire = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.4f, DustID.Torch,
                        -Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.6f, 0.6f), 100, default, Main.rand.NextFloat(1.1f, 1.7f));
                    fire.noGravity = true;
                }
                Dust smoke = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, -Projectile.velocity * 0.05f, 140, Color.Gray, 1f);
                smoke.noGravity = true;

                Lighting.AddLight(Projectile.Center, 0.7f, 0.35f, 0.1f);
            }
            else
            {
                Projectile.velocity *= 0.15f;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            StartExplosion();
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
            StartExplosion();
        }

        private void StartExplosion()
        {
            if (exploding)
                return;
            exploding = true;

            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = BaseBlastRadius;
            Projectile.Center = center;
            Projectile.tileCollide = false;
            Projectile.knockBack = 6f;
            if (Projectile.timeLeft > 3)
                Projectile.timeLeft = 3;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f, Pitch = 0.1f }, center);
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 center = Projectile.Center;
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.55f }, center);

            if (Main.dedServ)
                return;

            for (int i = 0; i < 24; i++)
            {
                Dust fire = Dust.NewDustPerfect(center, DustID.Torch, Main.rand.NextVector2Circular(6f, 6f), 90, default, Main.rand.NextFloat(1.4f, 2.4f));
                fire.noGravity = true;
            }
            for (int i = 0; i < 10; i++)
            {
                Dust smoke = Dust.NewDustPerfect(center, DustID.Smoke, Main.rand.NextVector2Circular(3.5f, 3.5f), 120, Color.DarkGray, Main.rand.NextFloat(1.4f, 2.2f));
                smoke.noGravity = true;
            }

            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, new Color(255, 150, 70), 1.2f, 22, true));
            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, new Color(255, 235, 180), 0.7f, 18, true));
        }
    }
}
