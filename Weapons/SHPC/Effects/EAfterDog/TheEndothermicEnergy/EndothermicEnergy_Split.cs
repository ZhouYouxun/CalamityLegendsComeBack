using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.TheEndothermicEnergy
{
    internal class EndothermicEnergy_Split : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectile";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            // 画残影效果
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;     
        }
        public override void SetDefaults()
        {
            // 设置弹幕的基础属性
            Projectile.width = 11; // 弹幕宽度
            Projectile.height = 24; // 弹幕高度
            Projectile.friendly = true; // 对敌人有效
            Projectile.DamageType = DamageClass.Magic; // 远程伤害类型
            Projectile.penetrate = 1; // 穿透力为1，击中一个敌人就消失
            Projectile.timeLeft = 300; // 弹幕存在时间为600帧
            Projectile.usesLocalNPCImmunity = true; // 弹幕使用本地无敌帧
            Projectile.localNPCHitCooldown = 14; // 无敌帧冷却时间为14帧
            Projectile.ignoreWater = true; // 弹幕不受水影响
            Projectile.arrow = true;
            Projectile.extraUpdates = 1;
            Projectile.aiStyle = ProjAIStyleID.Arrow; // 让弹幕受到重力影响
        }


        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 300); // 原版的霜火效果
            target.AddBuff(BuffID.Chilled, 300); // 原版的寒冷效果
        }

        public override void AI()
        {
            // 调整弹幕的旋转，使其在飞行时保持水平
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.Pi;

            // Lighting - 添加天蓝色光源，光照强度为 0.49
            Lighting.AddLight(Projectile.Center, Color.LightSkyBlue.ToVector3() * 0.49f);

            // 弹幕保持直线运动并逐渐加速
            Projectile.velocity *= 1.01f;


            if (Projectile.numUpdates % 3 == 0)
            {
                Color outerSparkColor = new Color(173, 216, 230);
                float scaleBoost = MathHelper.Clamp(Projectile.ai[0] * 0.005f, 0f, 2f);
                float outerSparkScale = 1.2f + scaleBoost;
                SparkParticle spark = new SparkParticle(Projectile.Center, Projectile.velocity, false, 7, outerSparkScale, outerSparkColor);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // 在飞行路径上留下冰元素粒子特效
            if (Main.rand.NextBool(4)) // 控制粒子生成频率（1/4 概率）
            {
                Dust iceDust = Dust.NewDustPerfect(Projectile.Center, DustID.SnowflakeIce, Projectile.velocity * 0.5f, 150, Color.LightBlue, 1.2f);
                iceDust.noGravity = true; // 使粒子无重力
                iceDust.velocity *= 0.3f; // 调整粒子速度
                iceDust.fadeIn = 1.5f; // 使粒子有一个渐入效果
            }
            
        }






    }
}