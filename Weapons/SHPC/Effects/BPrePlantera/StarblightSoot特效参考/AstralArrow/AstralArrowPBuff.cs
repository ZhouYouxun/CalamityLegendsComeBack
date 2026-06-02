namespace CalamityRangerExpansion.Content.Arrows.CPreMoodLord.AstralArrow
{
    public class AstralArrowPBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // 检查是否存在与 AstralArrowSUN 和 AstralArrowMOON 相关的弹幕
            bool hasSun = false;
            bool hasMoon = false;

            // 遍历所有的弹幕
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == player.whoAmI)
                {
                    if (proj.type == ModContent.ProjectileType<AstralArrowSUN>())
                    {
                        hasSun = true;
                    }
                    else if (proj.type == ModContent.ProjectileType<AstralArrowMOON>())
                    {
                        hasMoon = true;
                    }
                }
            }

            // 如果 Buff 已经消失，并且有太阳和月亮弹幕存在，移除它们
            if (!player.HasBuff(ModContent.BuffType<AstralArrowPBuff>()))
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active && proj.owner == player.whoAmI && (proj.type == ModContent.ProjectileType<AstralArrowSUN>() || proj.type == ModContent.ProjectileType<AstralArrowMOON>()))
                    {
                        proj.Kill(); // 删除弹幕
                    }
                }
            }
        }
    }
}
