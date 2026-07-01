using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.B_HardMode
{
    /// <summary>
    /// 地狱风暴：橙红色燃烧弹，命中时生成 2 颗追踪火星子弹（50% 伤害），施加地狱烈焰。
    /// </summary>
    public class DERule_Helstorm : DEBulletRule
    {
        private static readonly Color HelOrange = new(255, 110, 20);
        private static readonly Color HelRed = new(255, 40, 10);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.Helstorm>();

        public override void SetDefaults(Projectile projectile)
        {
            projectile.light = 0.5f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(projectile.Center - forward * 4f,
                    i == 0 ? DustID.Torch : DustID.OrangeTorch,
                    -forward * 0.55f + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    120, i == 0 ? HelOrange : HelRed, Main.rand.NextFloat(0.9f, 1.2f));
                dust.noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(5))
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    projectile.Center, -forward * 0.07f,
                    HelOrange * 0.4f, 12, Main.rand.NextFloat(0.3f, 0.5f), 0.5f,
                    Main.rand.NextFloat(-0.025f, 0.025f)));
            }

            Lighting.AddLight(projectile.Center, HelOrange.R / 255f * 0.6f, HelOrange.G / 255f * 0.35f, 0f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 360);
            SpawnFireSparks(projectile);
            SpawnHelstormImpact(projectile.Center);
        }

        private static void SpawnFireSparks(Projectile source)
        {
            int sparkDmg = (int)(source.damage * 0.5f);
            float baseAngle = source.velocity.ToRotation();

            for (int i = 0; i < 2; i++)
            {
                float angle = baseAngle + MathHelper.ToRadians(i == 0 ? -25f : 25f);
                Vector2 vel = angle.ToRotationVector2() * 9f;
                Projectile.NewProjectile(
                    source.GetSource_FromAI(),
                    source.Center + vel * 0.5f, vel,
                    ModContent.ProjectileType<DEBullet_FireSpark>(),
                    sparkDmg, source.knockBack * 0.4f, source.owner);
            }
        }

        private static void SpawnHelstormImpact(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, HelOrange, 0.9f, 20));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, HelRed * 0.7f,
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, 0f, 0f, 0.07f, 18, true, 0.75f));
                for (int i = 0; i < 3; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(pos,
                        Main.rand.NextVector2Circular(2.5f, 2.5f),
                        Color.Lerp(HelOrange, HelRed, Main.rand.NextFloat()) * 0.5f,
                        16, Main.rand.NextFloat(0.4f, 0.6f), 0.5f, Main.rand.NextFloat(-0.025f, 0.025f)));
                }
            }
            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.Torch,
                    Main.rand.NextVector2Circular(8f, 8f), 110, HelOrange, 1.15f);
                dust.noGravity = true;
            }
        }

        public override string TooltipEffectEN => "Incendiary bolt; spawns 2 homing fire sparks on hit (50% damage) + Hellfire";
        public override string TooltipEffectZH => "燃烧弹，命中时生成2颗追踪火星（50%伤害），施加地狱烈焰";
    }
}
