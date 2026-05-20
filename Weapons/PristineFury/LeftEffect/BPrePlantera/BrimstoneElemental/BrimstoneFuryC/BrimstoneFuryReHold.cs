using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using CalamityRangerExpansion.Content.BOWChange.BPrePlantera.BrimstoneFuryC;
using CalamityMod.Projectiles.BaseProjectiles;
using CalamityMod;
using CalamityRangerExpansion.Content.BOWChange;
using CalamityMod.Particles;

namespace CalamityRangerExpansion.Content.BOWChange.BPrePlantera.BrimstoneFuryC
{
    public class BrimstoneFuryReHold : BaseGunHoldoutProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectile.BPrePlantera";
        public override string Texture => "CalamityRangerExpansion/Content/BOWChange/BPrePlantera/BrimstoneFuryC/BrimstoneFuryRe";
        public override int AssociatedItemID => ModContent.ItemType<BrimstoneFuryRe>();

        private ref float ChargeFrames => ref Projectile.ai[0];
        private ref float ShotsRemaining => ref Projectile.ai[1];
        private int readyDustTicker = 0;
        private bool pickedAmmoCached = false;
        private int cachedAmmoType = ProjectileID.None;
        public override float MaxOffsetLengthFromArm => 15f;

        private bool hasTriggeredReadyFX = false;

        public override bool? CanDamage() => Main.player[Projectile.owner].GetModPlayer<OpenBowDamagePlayer>().OpenBowDamage;

        public override Vector2 GunTipPosition => Projectile.Center + Vector2.UnitX.RotatedBy(Projectile.rotation) * (Projectile.width * 0.5f + 10f);
        public override void AI()
        {
            KeepRefreshingLifetime = true;
            base.AI();
        }
        public override void HoldoutAI()
        {
            Player player = Owner;

            if (player.channel)
            {
                if (ChargeFrames < 90)
                {
                    ChargeFrames++;
                    // 蓄力期间：持续硫火吸热感（烟雾+红橙尘+深色线条低频闪）
                    BowChangeVFX.SpawnCharge(Projectile, GunTipPosition, BowChangeTheme.Brimstone, ChargeFrames / 90f, 0.75f);
                    SpawnChargeFX_EveryFrame(GunTipPosition);
                }
                else
                {
                    // ? 满蓄瞬间只触发一次爆发，不需要“满蓄持续”那一档
                    if (!hasTriggeredReadyFX)
                    {
                        BowChangeVFX.SpawnReadyBurst(Projectile, GunTipPosition, BowChangeTheme.Brimstone, 1f);
                        SpawnChargeReadyOnceFX(GunTipPosition);
                        hasTriggeredReadyFX = true;
                    }
                    BowChangeVFX.SpawnCharge(Projectile, GunTipPosition, BowChangeTheme.Brimstone, 1f, 0.55f);
                }
            }
        }

        public override void KillHoldoutLogic()
        {
            Player player = Owner;

            // 玩家已经松手
            if (!player.channel)
            {
                // 满蓄 → 进入结算阶段
                if (ShotsRemaining <= 0 && ChargeFrames >= 90)
                {
                    ShotsRemaining = 3;
                    pickedAmmoCached = false;
                }

                // 结算阶段：逐发射击
                if (ShotsRemaining > 0)
                {
                    FireNextProjectile(player);
                    ShotsRemaining--;

                    if (ShotsRemaining <= 0)
                        Projectile.Kill();

                    return;
                }

                // ? 兜底：快速点按 / 未达条件
                Projectile.Kill();
            }
        }


        private void CacheAmmo(Player player)
        {
            if (pickedAmmoCached) return;

            Item heldItem = player.HeldItem;
            if (player.HasAmmo(heldItem))
            {
                if (player.PickAmmo(heldItem, out int pickedAmmoProjectile, out float _, out int _, out float _, out int _))
                {
                    cachedAmmoType = pickedAmmoProjectile == ProjectileID.WoodenArrowFriendly
                        ? ModContent.ProjectileType<BrimstoneFuryRePROJ>()
                        : pickedAmmoProjectile;
                    pickedAmmoCached = true;
                }
            }
        }

        private void FireNextProjectile(Player player)
        {
            CacheAmmo(player);

            Vector2 shootDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 basePos = GunTipPosition;

            // 发射激光 + 传入 ammoProjectile
            int proj = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                basePos,
                shootDirection,
                ModContent.ProjectileType<BrimstoneFuryReBEAM>(),
                (int)(Projectile.damage * 2.0f),
                Projectile.knockBack,
                player.whoAmI,
                ai0: 0f,
                ai1: cachedAmmoType
            );

            if (proj.WithinBounds(Main.maxProjectiles))
            {
                // 可以在 BEAM 的 OnSpawn 或 AI 中读取 ai1 来生成对应弹药效果
            }

            SoundEngine.PlaySound(SoundID.Item28, player.Center);
            SpawnPerShotFX(basePos, shootDirection * 16f);
            BowChangeVFX.SpawnMuzzle(Projectile, basePos, shootDirection * 16f, BowChangeTheme.Brimstone, 1.1f);
        }





        // === 蓄力持续：硫火吸热（轻烟体积减半 + 频率↑ / 红橙尘上涌 / 深色线条偶发） ===
        // 设计：方框内持续生热的感觉；烟雾更细小但更常见；红橙尘抖动上升；少量暗线条像“硫火裂隙”
        private void SpawnChargeFX_EveryFrame(Vector2 pos)
        {
            // 方框热区（宽18，高6）稍微在枪口下方，模拟灼热涌起
            Vector2 boxCenter = pos + new Vector2(0f, 6f);
            float halfW = 9f, halfH = 3f;

            // ① 轻型烟雾（HeavySmokeParticle）——体积减半，但频率↑
            int smokeCount = Main.rand.Next(3, 5); // 每帧都来一点
            for (int i = 0; i < smokeCount; i++)
            {
                Vector2 spawn = boxCenter + new Vector2(Main.rand.NextFloat(-halfW, halfW), Main.rand.NextFloat(-halfH, halfH));
                Vector2 vel = new Vector2(Main.rand.NextFloat(-0.15f, 0.15f), Main.rand.NextFloat(-0.9f, -0.4f)); // 上飘
                                                                                                                  // 体积至少砍半：0.45~0.8（原示例0.9~1.6）
                var smoke = new HeavySmokeParticle(
                    spawn,
                    vel,
                    Color.Lerp(Color.OrangeRed, Color.DarkRed, Main.rand.NextFloat(0.25f, 0.65f)),
                    Main.rand.Next(12, 20),                      // 略短寿命，防糊屏
                    Main.rand.NextFloat(0.45f, 0.8f),            // ? 体积减半
                    0.15f,                                       // 轻烟
                    Main.rand.NextFloat(-0.9f, 0.9f),            // 微旋转
                    false
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            // ② 红橙尘上涌（Torch/Flare系 Dust）
            int dustCount = Main.rand.Next(3, 6);
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 spawn = boxCenter + Main.rand.NextVector2Circular(halfW, halfH);
                Vector2 vel = new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), Main.rand.NextFloat(-1.1f, -0.6f));
                int d = Dust.NewDust(spawn, 0, 0, DustID.Torch, vel.X, vel.Y);
                Main.dust[d].noGravity = true;
                Main.dust[d].scale = Main.rand.NextFloat(0.85f, 1.15f);
                Main.dust[d].color = Color.Lerp(Color.OrangeRed, Color.Orange, Main.rand.NextFloat(0.3f, 0.8f));
            }

            // ③ 深色线条（AltSparkParticle）——低频、极短、暗红，像裂缝里冒的硫火
            if (Main.rand.NextBool(1, 5))
            {
                Vector2 j = Main.rand.NextVector2Circular(0.6f, 0.6f);
                var line = new AltSparkParticle(
                    pos + j,
                    j * 0.1f,                 // 几乎静止的电痕/裂隙
                    false,
                    10,                       // 短寿命
                    1.0f,                     // 小尺寸
                    Color.DarkRed * 0.55f     // 深色线条
                );
                GeneralParticleHandler.SpawnParticle(line);
            }
        }






        // 达到满蓄后，每帧持续播放的特效（如环绕光效）
        private void SpawnChargeFX_Complete(Vector2 pos)
        {
            float angle = Main.GameUpdateCount * 0.1f;
            Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle * 2)) * 10f;
            Dust d = Dust.NewDustPerfect(pos + offset, DustID.LavaMoss, Vector2.Zero, 100, Color.Red, 1.4f);
            d.noGravity = true;
        }

        // === 满蓄瞬间：硫磺地狱爆发（火尘双翼扇弧 + 暗红线束放射 + 细烟团裹挟） ===
        // 结构：
        //  A 火尘双翼扇弧（DustID.Torch/Flare系）——主量体；
        //  B 暗红线束（AltSparkParticle）——尖锐感与方向性；
        //  C 轻型烟雾群（HeavySmokeParticle，小体积高频）——热浪翻腾。
        private void SpawnChargeReadyOnceFX(Vector2 pos)
        {
            // 朝向基向量
            Vector2 f = Projectile.velocity.LengthSquared() > 1e-4f
                ? Vector2.Normalize(Projectile.velocity)
                : Vector2.UnitX.RotatedBy(Projectile.rotation);
            Vector2 n = new Vector2(-f.Y, f.X);

            // === A) 火尘双翼扇弧（±28°），速度外抛，带少许法向扰动 ===
            int fireCount = 160;                             // Dust 主体量
            float arc = MathHelper.ToRadians(28f);
            for (int i = 0; i < fireCount; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float ang = side * Main.rand.NextFloat(0f, arc);
                Vector2 dir = f.RotatedBy(ang);
                Vector2 vel = dir * Main.rand.NextFloat(8f, 14f) + n * Main.rand.NextFloat(-0.6f, 0.6f);

                int d = Dust.NewDust(pos, 0, 0, DustID.Torch, vel.X, vel.Y);
                var dd = Main.dust[d];
                dd.noGravity = true;
                dd.scale = Main.rand.NextFloat(1.0f, 1.4f);
                dd.color = Color.Lerp(Color.OrangeRed, Color.Orange, Main.rand.NextFloat(0.35f, 0.85f));
            }

            // === B) 暗红线束放射（AltSparkParticle），锥束角更窄（±12°） ===
            int beamCount = 48;
            float narrow = MathHelper.ToRadians(12f);
            for (int i = 0; i < beamCount; i++)
            {
                float ang = Main.rand.NextFloat(-narrow, narrow);
                Vector2 dir = f.RotatedBy(ang);
                var beam = new AltSparkParticle(
                    pos + dir * Main.rand.NextFloat(10f, 18f), // 略偏外生成，像地狱裂隙喷出
                    dir * Main.rand.NextFloat(0.6f, 1.2f),     // 速度很低，像灼烧痕迹延展
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(1.1f, 1.5f),
                    Color.DarkRed * 0.7f
                );
                GeneralParticleHandler.SpawnParticle(beam);
            }

            // === C) 轻型烟雾群（小体积，高频）包裹在扇弧内外 ===
            int smokePuffs = 36;
            for (int i = 0; i < smokePuffs; i++)
            {
                Vector2 spawn = pos + f * Main.rand.NextFloat(6f, 24f) + n * Main.rand.NextFloat(-8f, 8f);
                Vector2 vel = new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-0.4f, 0.2f));
                var smoke = new HeavySmokeParticle(
                    spawn,
                    vel,
                    Color.Lerp(Color.Orange, Color.DarkRed, Main.rand.NextFloat(0.3f, 0.7f)),
                    Main.rand.Next(14, 22),
                    Main.rand.NextFloat(0.45f, 0.8f),          // ? 体积减半
                    0.35f,
                    Main.rand.NextFloat(-0.8f, 0.8f),
                    false
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }




        // 每次发弹时调用的特效（如发光、爆炸、弹壳抛射等）
        private void SpawnPerShotFX(Vector2 pos, Vector2 vel)
        {
            for (int i = 0; i < 4; i++)
            {
                Dust d = Dust.NewDustPerfect(pos, DustID.Flare, vel.RotatedByRandom(0.3f) * 0.3f, 150, Color.OrangeRed, 1.2f);
                d.noGravity = true;
            }
        }


    }
}
