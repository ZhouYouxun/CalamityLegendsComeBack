namespace CalamityRangerExpansion.Content.Arrows.BPrePlantera.StarblightSootArrow
{
    public class StarblightSootArrowSpark : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectile.BPrePlantera";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.DamageType = DamageClass.Ranged;
        }

        #region 旧代码

        //public override void AI()
        //{
        //    if (Projectile.velocity.X != Projectile.velocity.X)
        //    {
        //        Projectile.velocity.X = Projectile.velocity.X * -0.1f;
        //    }
        //    if (Projectile.velocity.X != Projectile.velocity.X)
        //    {
        //        Projectile.velocity.X = Projectile.velocity.X * -0.5f;
        //    }
        //    if (Projectile.velocity.Y != Projectile.velocity.Y && Projectile.velocity.Y > 1f)
        //    {
        //        Projectile.velocity.Y = Projectile.velocity.Y * -0.5f;
        //    }
        //    Projectile.ai[0] += 1f;
        //    if (Projectile.ai[0] > 5f)
        //    {
        //        Projectile.ai[0] = 5f;
        //        if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
        //        {
        //            Projectile.velocity.X = Projectile.velocity.X * 0.97f;
        //            if ((double)Projectile.velocity.X > -0.01 && (double)Projectile.velocity.X < 0.01)
        //            {
        //                Projectile.velocity.X = 0f;
        //                Projectile.netUpdate = true;
        //            }
        //        }
        //        Projectile.velocity.Y = Projectile.velocity.Y + 0.2f;
        //    }
        //    Projectile.rotation += Projectile.velocity.X * 0.1f;
        //    int sparky = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.UnusedWhiteBluePurple, 0f, 0f, 100, default, 1f);
        //    Dust expr_8976_cp_0 = Main.dust[sparky];
        //    expr_8976_cp_0.position.X -= 2f;
        //    Dust expr_8994_cp_0 = Main.dust[sparky];
        //    expr_8994_cp_0.position.Y += 2f;
        //    Main.dust[sparky].scale += (float)Main.rand.Next(50) * 0.01f;
        //    Main.dust[sparky].noGravity = true;
        //    Dust expr_89E7_cp_0 = Main.dust[sparky];
        //    expr_89E7_cp_0.velocity.Y -= 2f;
        //    if (Main.rand.NextBool())
        //    {
        //        int sparkier = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.UnusedWhiteBluePurple, 0f, 0f, 100, default, 1f);
        //        Dust expr_8A4E_cp_0 = Main.dust[sparkier];
        //        expr_8A4E_cp_0.position.X -= 2f;
        //        Dust expr_8A6C_cp_0 = Main.dust[sparkier];
        //        expr_8A6C_cp_0.position.Y += 2f;
        //        Main.dust[sparkier].scale += 0.3f + (float)Main.rand.Next(50) * 0.01f;
        //        Main.dust[sparkier].noGravity = true;
        //        Main.dust[sparkier].velocity *= 0.1f;
        //    }
        //    if ((double)Projectile.velocity.Y < 0.25 && (double)Projectile.velocity.Y > 0.15)
        //    {
        //        Projectile.velocity.X = Projectile.velocity.X * 0.8f;
        //    }
        //    Projectile.rotation = -Projectile.velocity.X * 0.05f;
        //    if (Projectile.velocity.Y > 16f)
        //    {
        //        Projectile.velocity.Y = 16f;
        //    }
        //}

        #endregion
        public override void AI()
        {
            // 检查是否启用了特效
            if (ModContent.GetInstance<CREsConfigs>().EnableSpecialEffects)
            {
                // 生成随机的粒子特效
                int[] dustColors = { DustID.OrangeTorch, DustID.BlueTorch, DustID.WhiteTorch, DustID.CrimsonTorch };
                int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustColors[Main.rand.Next(dustColors.Length)], 0f, 0f, 100, default, 1.6f);
                Dust dust = Main.dust[dustIndex];
                dust.noGravity = true; // 粒子不受重力影响
                dust.velocity *= 1.5f; // 增强粒子的速度以模拟快速释放
                dust.scale += (float)Main.rand.Next(50) * 0.01f;
            }

            // 弹幕持续前进并逐渐减速
            Projectile.velocity *= 0.98f; // 逐渐减速，调整因子可以控制减速的速度
            Projectile.rotation += Projectile.velocity.X * 0.1f; // 保持旋转效果

            // 删除不必要的重复条件和逻辑
            if (Projectile.velocity.LengthSquared() < 0.0001f)
            {
                Projectile.velocity = Vector2.Zero; // 如果速度非常小，则停止
                Projectile.netUpdate = true;
            }
            Time++;
        }
        public ref float Time => ref Projectile.ai[1];

        public override bool? CanDamage() => Time >= 4f; // 初始的时候不会造成伤害，直到x为止


        public override bool OnTileCollide(Vector2 oldVelocity) => false;
    }
}
