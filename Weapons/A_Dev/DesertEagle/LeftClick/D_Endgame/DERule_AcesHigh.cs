using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.D_Endgame
{
    /// <summary>
    /// 极魂符枪：每次同时射出 4 张扑克牌（1° 间隔散射）——
    /// ♥ 红心：双穿透，命中吸血 2%；
    /// ♣ 梅花：到期时分裂 3 颗追踪碎片；
    /// ♦ 方块：到期时触发 50×50 区域爆炸；
    /// ♠ 黑桃：无限穿透 + 幻影残影，每次命中触发蓝白光爆。
    /// </summary>
    public class DERule_AcesHigh : DEBulletRule
    {
        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.AcesHigh>();

        // DELeftBullet 载体不可见、无害，仅作为生成 4 张牌的触发器
        public override void SetDefaults(Projectile projectile)
        {
            projectile.alpha = 255;
            projectile.friendly = false;
            projectile.timeLeft = 1;
            projectile.light = 0f;
        }

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            float baseAngle = projectile.velocity.ToRotation();
            float speed = projectile.velocity.Length();

            int[] types =
            {
                ModContent.ProjectileType<DEBullet_CardHeart>(),
                ModContent.ProjectileType<DEBullet_CardClub>(),
                ModContent.ProjectileType<DEBullet_CardDiamond>(),
                ModContent.ProjectileType<DEBullet_CardSpade>()
            };
            float[] spreadDeg = { -1.5f, -0.5f, 0.5f, 1.5f };

            for (int i = 0; i < 4; i++)
            {
                float angle = baseAngle + MathHelper.ToRadians(spreadDeg[i]);
                Vector2 vel = angle.ToRotationVector2() * speed;
                Projectile.NewProjectile(projectile.GetSource_FromAI(),
                    projectile.Center, vel, types[i],
                    projectile.damage, projectile.knockBack, owner.whoAmI);
            }
        }

        public override string TooltipEffectEN =>
            "Fires all 4 suits at once (1° spread): ♥ 2% lifesteal · ♣ triple split shards · ♦ area explosion · ♠ infinite pierce";
        public override string TooltipEffectZH =>
            "同时射出4张扑克牌（1°散射）：♥红心2%吸血 · ♣梅花三重分裂 · ♦方块区域爆炸 · ♠黑桃无限穿透";
    }
}
