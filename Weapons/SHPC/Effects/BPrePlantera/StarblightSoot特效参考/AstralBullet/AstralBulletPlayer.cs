namespace CalamityRangerExpansion.Content.Ammunition.CPreMoodLord.AstralBullet
{
    public class AstralBulletPlayer : ModPlayer
    {
        // 保存 AstralBulletPROJ 的伤害值
        public int LastAstralBulletDamage { get; private set; }

        public override void PostUpdate()
        {
            // 遍历当前世界的所有投射物
            foreach (Projectile proj in Main.projectile)
            {
                // 检查投射物是否存在且是 AstralBulletPROJ 类型
                if (proj.active && proj.type == ModContent.ProjectileType<AstralBulletPROJ>())
                {
                    // 更新伤害值
                    LastAstralBulletDamage = proj.damage;
                    return; // 找到一个后立即退出
                }
            }
            // 如果没有找到 AstralBulletPROJ，伤害值保持最后一次记录的值
        }
    }
}
