using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.Lazhar
{
    /// <summary>
    /// 拉扎尔射线专属玩家状态类 (LazharPlayer)
    /// 核心功能：
    /// - 追踪并记录玩家使用拉扎尔射线击中被雷达锁定敌怪的次数。
    /// - 连续击中 3 次锁定目标后，触发“能量过载” (Energy Overload) 增益。
    /// - 此时玩家身体周围释放环形静电粒子，且下一次左键三连发将获得双倍伤害、宽拖尾、更大粒子爆破增幅。
    /// - 开火后，清除过载状态重新积攒，创造良好的战术打击循环。
    /// </summary>
    public class LazharPlayer : ModPlayer
    {
        // 击中计数器
        public int LockedHitCount { get; set; }

        // 是否过载就绪
        public bool OverloadReady { get; set; }

        public override void ResetEffects()
        {
            // 如果玩家当前手持的不是拉扎尔射线，则清空过载层数
            if (Player.HeldItem.type != ModContent.ItemType<Lazhar>())
            {
                LockedHitCount = 0;
                OverloadReady = false;
            }
        }

        /// <summary>
        /// 监测玩家使用弹幕击中敌怪的事件
        /// </summary>
        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 必须是本玩家射出的 LazharLaser 射线弹幕
            if (proj.type == ModContent.ProjectileType<LazharLaser>() && proj.owner == Player.whoAmI)
            {
                // 且目标处于雷达锁定状态
                if (target.HasBuff<LazharTargetDebuff>())
                {
                    if (!OverloadReady)
                    {
                        LockedHitCount++;

                        // 每次击中，在被击中目标处生成少量黄色电火花，反馈充能进度
                        if (!Main.dedServ)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                Dust d = Dust.NewDustDirect(target.position, target.width, target.height, DustID.GoldCoin, 0f, 0f, 100, default, 0.9f);
                                d.velocity = Main.rand.NextVector2Circular(3f, 3f);
                                d.noGravity = true;
                            }
                        }

                        // 满 3 次击中，进入能量过载状态
                        if (LockedHitCount >= 3)
                        {
                            OverloadReady = true;

                            // 播放过载充能完毕的科幻高频音效
                            SoundEngine.PlaySound(SoundID.Item158 with { Volume = 0.8f, Pitch = 0.3f }, Player.Center);

                            // 在玩家身上生成一圈金黄色能量爆发环，提供强烈的视觉提示
                            if (!Main.dedServ)
                            {
                                for (int i = 0; i < 15; i++)
                                {
                                    Vector2 velocity = (MathHelper.TwoPi * i / 15f).ToRotationVector2() * 3.5f;
                                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                                        Player.Center,
                                        velocity,
                                        false,
                                        12,
                                        0.6f,
                                        Color.Gold,
                                        true,
                                        true
                                    ));
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void PostUpdate()
        {
            // 如果处于过载就绪状态，在玩家手臂/枪支位置不断环绕生成金色电离子，提示下一次开火强化
            if (OverloadReady && Player.HeldItem.type == ModContent.ItemType<Lazhar>())
            {
                if (Main.rand.NextBool(5))
                {
                    Vector2 offset = Main.rand.NextVector2Circular(20f, 20f);
                    Dust d = Dust.NewDustDirect(Player.MountedCenter + offset, 4, 4, DustID.GoldCoin, 0f, 0f, 100, default, 0.8f);
                    d.velocity = Player.velocity * 0.5f;
                    d.noGravity = true;
                }
            }
        }
    }
}

