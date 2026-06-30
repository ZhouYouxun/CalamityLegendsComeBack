using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // 检测由海之烧灼发射的辐射子弹（localAI[2] == 1f 标记），处理击中效果和玩家辐射积累
    public sealed class SeasSearingRadiationBulletGlobal : GlobalProjectile
    {
        // 不需要 InstancePerEntity，直接读 localAI[2]
        public override bool InstancePerEntity => false;

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.localAI[2] != 1f) return;

            Player owner = Main.player[projectile.owner];
            SeasSearingPlayer ssPlayer = owner.GetModPlayer<SeasSearingPlayer>();

            // 给玩家积累辐射
            ssPlayer.OnHitWithSeasSearing();

            // 给敌人施加少量污染
            if (Main.myPlayer == projectile.owner)
                target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, projectile.owner, 3, 8 * 60);

            // 击中时的辐射扩散特效
            if (!Main.dedServ)
                SpawnHitRadiationBurst(target.Center);
        }

        private static void SpawnHitRadiationBurst(Vector2 center)
        {
            for (int i = 0; i < 10; i++)
            {
                float ang = MathHelper.TwoPi * i / 10f + Main.rand.NextFloat(-0.15f, 0.15f);
                Vector2 vel = ang.ToRotationVector2() * Main.rand.NextFloat(1.8f, 5f);
                Dust d = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.GemEmerald, vel, 120,
                    Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.BiohazardLime, Main.rand.NextFloat(0.2f, 0.8f)),
                    Main.rand.NextFloat(0.55f, 0.95f));
                d.noGravity = true;
            }
        }
    }
}
