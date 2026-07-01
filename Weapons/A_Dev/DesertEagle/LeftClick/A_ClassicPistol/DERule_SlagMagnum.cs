using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles;
using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.A_ClassicPistol
{
    /// <summary>
    /// 熔渣马格南：穿透 3 目标的橙褐色弹，每次命中散射 4 枚碎片，施加护甲损伤 (ArmorCrunch)。
    /// </summary>
    public class DERule_SlagMagnum : DEBulletRule
    {
        private static readonly Color SlagOrange = new(210, 120, 30);
        private static readonly Color SlagLight = new(255, 180, 70);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.SlagMagnum>();

        public override int Penetrate => 3;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.width = 8;
            projectile.height = 8;
            projectile.light = 0.4f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // 橙色沙石尾迹
            Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.Sand,
                -projectile.velocity.SafeNormalize(Vector2.Zero) * 0.4f + Main.rand.NextVector2Circular(0.3f, 0.3f),
                120, SlagOrange, Main.rand.NextFloat(0.8f, 1.1f));
            dust.noGravity = true;

            if (!Main.dedServ && Main.rand.NextBool(6))
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    projectile.Center, -projectile.velocity * 0.05f,
                    SlagOrange * 0.5f, 12, 0.4f, 0.6f, Main.rand.NextFloat(-0.03f, 0.03f)));
            }
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<ArmorCrunch>(), 120);
            SpawnShrapnel(projectile, target.Center);
            SpawnSlagImpact(projectile.Center);
        }

        public override bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity)
        {
            SpawnSlagImpact(projectile.Center);
            return true;
        }

        private static void SpawnShrapnel(Projectile source, Vector2 hitPos)
        {
            int shrapDmg = (int)(source.damage * 0.3f);

            // 4 枚：90°、180°、270°、0°（相对于飞行方向加随机扩散）
            float baseAngle = source.velocity.ToRotation();
            for (int i = 0; i < 4; i++)
            {
                float angle = baseAngle + MathHelper.PiOver2 * i + Main.rand.NextFloat(-0.35f, 0.35f);
                Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(8f, 12f);
                Projectile.NewProjectile(
                    source.GetSource_FromAI(), hitPos, vel,
                    ModContent.ProjectileType<DEBullet_ShrapnelShard>(),
                    shrapDmg, source.knockBack * 0.4f, source.owner);
            }
        }

        private static void SpawnSlagImpact(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, SlagLight, 0.75f, 18));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, SlagOrange * 0.7f,
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, 0f, 0f, 0.06f, 16, true, 0.72f));
                for (int i = 0; i < 4; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(pos,
                        Main.rand.NextVector2Circular(2f, 2f),
                        SlagOrange * 0.6f, 16, Main.rand.NextFloat(0.45f, 0.75f),
                        0.5f, Main.rand.NextFloat(-0.02f, 0.02f)));
                }
            }
            for (int i = 0; i < 16; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.Sand,
                    Main.rand.NextVector2Circular(7f, 7f), 120, SlagOrange, 1.0f);
                dust.noGravity = true;
            }
        }

        public override string TooltipEffectEN => "Piercing slag slug (3 targets); scatters 4 shrapnel on each hit + Armor Crunch";
        public override string TooltipEffectZH => "穿透3个目标，每次命中散射4枚碎片，施加护甲损伤";
    }
}
