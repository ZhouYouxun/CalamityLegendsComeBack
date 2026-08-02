using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.M4A1
{
    /// <summary>
    /// 右键便携重炮的炮弹（ai[0]=是否终结弹, ai[1]=消耗前同步率阶段）。
    /// 复仇印记增益：一层略微追踪、二层爆炸范围提高、三层触发强化爆炸。
    /// </summary>
    public class M4A1Shell : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private bool IsFinisher => Projectile.ai[0] >= 1f;
        private int SyncTier => (int)Projectile.ai[1];

        private bool exploding;
        private int hitMarkLevel;

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 260;
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

            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation();

            // 印记：略微追踪最近的被标记敌人
            NPC target = FindNearestMarked(820f);
            if (target != null)
            {
                Vector2 toTarget = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
                float speed = Projectile.velocity.Length();
                Projectile.velocity = Vector2.Lerp(Projectile.velocity.SafeNormalize(Vector2.UnitX), toTarget, 0.05f) * speed;
            }

            // 火焰尾迹
            Color hot = IsFinisher ? new Color(255, 120, 60) : new Color(255, 170, 90);
            for (int i = 0; i < 2; i++)
            {
                Dust fire = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.4f, DustID.Torch,
                    -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.6f, 0.6f), 90, hot, Main.rand.NextFloat(1.2f, IsFinisher ? 2.2f : 1.7f));
                fire.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.7f, 0.35f, 0.12f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            hitMarkLevel = 0;
            StartExplosion();
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!exploding)
                hitMarkLevel = M4A1MarkGlobalNPC.Of(target).MarkLevel;

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

            // 基础范围：终结弹更大；同步率越高越大；二层印记进一步扩大；三层强化爆炸。
            float radius = IsFinisher ? 150f : 108f;
            radius *= 1f + 0.22f * SyncTier;
            if (hitMarkLevel >= 2)
                radius *= 1.4f;               // 二层：爆炸范围提高
            bool enhanced = hitMarkLevel >= 3; // 三层：强化爆炸
            if (enhanced)
            {
                radius *= 1.7f;
                Projectile.damage = (int)(Projectile.damage * 1.3f);
            }

            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = (int)radius;
            Projectile.Center = center;
            Projectile.tileCollide = false;
            Projectile.knockBack = IsFinisher ? 9f : 6f;
            if (Projectile.timeLeft > 3)
                Projectile.timeLeft = 3;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = IsFinisher ? 0.9f : 0.7f, Pitch = IsFinisher ? -0.15f : 0.05f }, center);
            if (enhanced)
                SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.7f, Pitch = -0.1f }, center);
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 center = Projectile.Center;
            if (Main.dedServ)
                return;

            bool enhanced = hitMarkLevel >= 3;
            int count = IsFinisher ? 34 : 22;
            Color hot = IsFinisher ? new Color(255, 110, 55) : new Color(255, 160, 80);
            Color mark = M4A1Visuals.MarkColor;

            for (int i = 0; i < count; i++)
            {
                Dust fire = Dust.NewDustPerfect(center, DustID.Torch, Main.rand.NextVector2Circular(7f, 7f), 80, enhanced ? mark : default, Main.rand.NextFloat(1.6f, 2.8f));
                fire.noGravity = true;
            }
            for (int i = 0; i < 12; i++)
            {
                Dust smoke = Dust.NewDustPerfect(center, DustID.Smoke, Main.rand.NextVector2Circular(4f, 4f), 120, Color.DarkGray, Main.rand.NextFloat(1.6f, 2.6f));
                smoke.noGravity = true;
            }

            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, enhanced ? mark : hot, IsFinisher ? 1.5f : 1.1f, enhanced ? 26 : 20, true));
            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, new Color(255, 235, 200), IsFinisher ? 0.9f : 0.65f, 16, true));
        }

        private NPC FindNearestMarked(float range)
        {
            NPC best = null;
            float bestDist = range * range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile) || !M4A1MarkGlobalNPC.Of(npc).HasMark)
                    continue;

                float d = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = npc;
                }
            }
            return best;
        }
    }
}
