using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Tools.DebugTools.ProjectileTest
{
    internal class TestWeaponProj : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_466";

        // 自定义计时器
        private int timer;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 900;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
        }

        // 弹幕出生
        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {

        }

        // 主AI
        public override void AI()
        {
            timer++;
            Projectile.rotation =
                Projectile.velocity.ToRotation();
            // 当前朝向
            Vector2 forward =
                Projectile.velocity.SafeNormalize(Vector2.UnitX);


            // 获取弹幕主人
            Player owner = Main.player[Projectile.owner];

            // 寻找最近敌人
            NPC target = null;

            float maxDistance = 1200f;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active)
                    continue;

                if (npc.friendly)
                    continue;

                if (npc.dontTakeDamage)
                    continue;

                float distance =
                    Vector2.Distance(
                        Projectile.Center,
                        npc.Center);

                if (distance < maxDistance)
                {
                    maxDistance = distance;
                    target = npc;
                }
            }



            // 3.自定义贴图光线（CustomSpark）---------------------------------
            // 原灾示范：DraedonArsenal、Providence、ExoMechs
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle customSpark = new CustomSpark(
                Projectile.Center, // 生成位置
                Projectile.velocity * 0.15f, // 初始速度
                "CalamityMod/Particles/SmallBloom", // 是否受重力影响
                false, // 生命周期，单位是帧
                3, // 缩放大小
                0.9f, // 颜色
                Color.Orange * 0.85f, // 拉伸或压缩比例
                new Vector2(0.8f, 2.7f), // 开关参数
                true, // 开关参数
                true, // 旋转角度或方向
                0f, // 开关参数
                false, // 开关参数
                false, // 数值参数
                0.65f, // 数值参数
                1f, // 数值参数
                1f, // 数值参数
                false, // 开关参数
                false, // 开关参数
                0f // 旋转速度
            );
            GeneralParticleHandler.SpawnParticle(customSpark);




        }

        // 命中敌人
        public override void OnHitNPC(
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {




        }


        // 弹幕死亡
        public override void OnKill(int timeLeft)
        {

        }
    }
}
