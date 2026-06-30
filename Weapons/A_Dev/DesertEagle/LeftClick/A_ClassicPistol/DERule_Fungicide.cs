using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.A_ClassicPistol
{
    /// <summary>
    /// 真菌枪：蓝绿色孢子弹，命中时爆裂成 3 颗追踪孢子（各 40% 伤害），施加 Venom。
    /// </summary>
    public class DERule_Fungicide : DEBulletRule
    {
        private static readonly Color SporeBlue = new(40, 210, 255);
        private static readonly Color SporeGlow = new(10, 180, 240);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.Fungicide>();

        public override void SetDefaults(Projectile projectile)
        {
            projectile.width = 14;
            projectile.height = 14;
            projectile.light = 0.45f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.rotation += 0.25f * projectile.direction;

            // 蓝色 BlueFairy 尾迹
            Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.BlueFairy,
                -projectile.velocity.SafeNormalize(Vector2.Zero) * 0.5f, 100, SporeBlue, 0.95f);
            dust.noGravity = true;

            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    projectile.Center, Main.rand.NextVector2Circular(1.5f, 1.5f),
                    false, 6, 0.016f, SporeGlow, new Vector2(0.7f, 0.7f)));
            }

            // 持续光照
            Lighting.AddLight(projectile.Center, SporeBlue.R / 255f * 0.5f, SporeBlue.G / 255f * 0.5f, SporeBlue.B / 255f * 0.5f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 240);
            SpawnSpores(projectile, owner, target.Center);
        }

        public override bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity)
        {
            SpawnSpores(projectile, owner, projectile.Center);
            SpawnImpactBurst(projectile.Center);
            return true;
        }

        private static void SpawnSpores(Projectile projectile, Player owner, Vector2 hitPos)
        {
            // 找最近敌人作为孢子目标
            int targetIdx = FindNearestNPCIndex(hitPos, 350f, projectile.owner);

            float baseAngle = projectile.velocity.ToRotation();
            float[] angles = { -MathHelper.ToRadians(20f), 0f, MathHelper.ToRadians(20f) };

            int sporeDmg = (int)(projectile.damage * 0.4f);
            foreach (float offset in angles)
            {
                Vector2 sporeVel = (baseAngle + offset).ToRotationVector2() * 10f;
                int sporeProj = Projectile.NewProjectile(
                    projectile.GetSource_FromAI(),
                    hitPos + sporeVel * 0.5f,
                    sporeVel,
                    ModContent.ProjectileType<DEBullet_FungiSpore>(),
                    sporeDmg,
                    projectile.knockBack * 0.5f,
                    projectile.owner,
                    targetIdx);
            }

            SpawnImpactBurst(hitPos);
        }

        private static void SpawnImpactBurst(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, SporeBlue, 0.8f, 20));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, SporeBlue * 0.75f,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, 0f, 0f, 0.06f, 18, true, 0.82f));
            }
            for (int i = 0; i < 14; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.BlueFairy,
                    Main.rand.NextVector2Circular(7f, 7f), 100, SporeBlue, 1.1f);
                dust.noGravity = true;
            }
        }

        private static int FindNearestNPCIndex(Vector2 pos, float range, int ownerIdx)
        {
            int idx = -1;
            float nearestDist = range;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;
                float dist = Vector2.Distance(pos, npc.Center);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    idx = i;
                }
            }
            return idx;
        }

        public override string TooltipEffectEN => "Fungal bolt that bursts into 3 homing spores on impact (40% damage each) + Venom";
        public override string TooltipEffectZH => "孢子弹，命中时爆裂成3颗追踪孢子（各40%伤害），并施加毒液";
    }
}
