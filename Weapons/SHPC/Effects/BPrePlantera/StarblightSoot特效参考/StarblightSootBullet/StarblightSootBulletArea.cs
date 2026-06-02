namespace CalamityRangerExpansion.Content.Ammunition.BPrePlantera.StarblightSootBullet
{
    public class StarblightSootBulletArea : ModProjectile
    {
        public new string LocalizationCategory => "Projectile.BPrePlantera";

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 1000;
            Projectile.height = 1000;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 400; // 存在 x 秒
            Projectile.tileCollide = false; // 不与方块碰撞
        }
        public override void AI()
        {
            // 提高粒子生成频率
            for (int i = 0; i < 25; i++) // 一帧生成 x 个粒子
            {
                // 在圆周上生成粒子
                Vector2 position = Projectile.Center + Main.rand.NextVector2CircularEdge(500f, 500f); // 半径调整为 500f
                int randomDust = Utils.SelectRandom(Main.rand, new int[]
                {
            ModContent.DustType<AstralOrange>(),
            ModContent.DustType<AstralBlue>()
                });
                Dust dust = Dust.NewDustPerfect(position, randomDust, Vector2.Zero); // 无初始速度
                dust.noGravity = true; // 粒子不受重力影响
                dust.scale = 2.0f + Main.rand.NextFloat(0.3f); // 调整粒子大小
                dust.rotation = Main.rand.NextFloat(MathHelper.TwoPi); // 随机旋转
            }

            // 添加光效
            Lighting.AddLight(Projectile.Center, Color.Blue.ToVector3() * 0.8f);
        }



        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 如果目标被击中且不为 Boss
            if (!target.boss && target.TryGetGlobalNPC<StarblightSootBulletGlobalNPC>(out var modNPC))
            {
                modNPC.MarkedByArea = true; // 标记该敌人为被立场笼罩
            }
        }

    }
}
