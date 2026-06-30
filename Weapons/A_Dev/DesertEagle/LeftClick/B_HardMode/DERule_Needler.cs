using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.B_HardMode
{
    /// <summary>
    /// 叶针枪：绿色毒针，缓慢归巢追踪，命中时区域毒素爆炸，施加 Venom。
    /// </summary>
    public class DERule_Needler : DEBulletRule
    {
        private static readonly Color VenomGreen = new(50, 200, 30);
        private static readonly Color VenomDark = new(20, 120, 10);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.Needler>();

        public override int ExtraUpdates => 2;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.width = 10;
            projectile.height = 10;
            projectile.light = 0.4f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            // 归巢至最近敌人
            NPC target = FindNearest(projectile.Center, 300f);
            if (target != null && projectile.Distance(target.Center) > 28f)
            {
                float speed = projectile.velocity.Length();
                Vector2 desired = projectile.DirectionTo(target.Center) * speed;
                projectile.velocity = Vector2.Lerp(projectile.velocity, desired, 0.085f);
                if (projectile.velocity.Length() > speed)
                    projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * speed;
            }

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // 毒绿尾迹
            Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.Venom,
                -projectile.velocity * 0.4f + Main.rand.NextVector2Circular(0.4f, 0.4f),
                100, VenomGreen, 0.9f);
            dust.noGravity = true;

            if (!Main.dedServ && Main.rand.NextBool(5))
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    projectile.Center, -projectile.velocity * 0.1f,
                    false, 6, 0.013f, VenomGreen, new Vector2(0.5f, 1.8f)));
            }

            Lighting.AddLight(projectile.Center, VenomGreen.R / 255f * 0.38f, VenomGreen.G / 255f * 0.38f, 0f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 360);
            SpawnVenomExplosion(projectile.Center, projectile.damage);
        }

        public override bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity)
        {
            SpawnVenomExplosion(projectile.Center, projectile.damage);
            return true;
        }

        private static void SpawnVenomExplosion(Vector2 pos, int damage)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, VenomGreen, 0.85f, 20));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, VenomGreen * 0.7f,
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, 0f, 0f, 0.07f, 18, true, 0.78f));
                for (int i = 0; i < 16; i++)
                {
                    float angle = MathHelper.TwoPi * i / 16f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        pos + angle.ToRotationVector2() * 8f, angle.ToRotationVector2() * 9f,
                        false, 10, 0.022f, i % 2 == 0 ? VenomGreen : VenomDark, new Vector2(0.8f, 0.45f)));
                }
            }
            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.Venom,
                    Main.rand.NextVector2Circular(8f, 8f), 100, VenomGreen, 1.1f);
                dust.noGravity = true;
            }
        }

        private static NPC FindNearest(Vector2 pos, float range)
        {
            NPC nearest = null;
            float nearestDist = range;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;
                float dist = Vector2.Distance(pos, npc.Center);
                if (dist < nearestDist) { nearestDist = dist; nearest = npc; }
            }
            return nearest;
        }

        public override string TooltipEffectEN => "Homing venom needle; erupts in a toxic burst on impact + Venom";
        public override string TooltipEffectZH => "追踪毒针，命中时触发毒素爆炸，施加毒液";
    }
}
