namespace CalamityRangerExpansion.Content.Ammunition.BPrePlantera.StarblightSootBullet
{
    public class StarblightSootBulletGlobalNPC : GlobalNPC
    {
        public bool MarkedByBullet = false; // 是否被标记
        public bool MarkedByArea = false; // 是否被立场标记
        public static List<NPC> MarkedByAreaNPCs = new(); // 当前被立场笼罩的敌人列表

        public override bool InstancePerEntity => true;

        public override void ResetEffects(NPC npc)
        {
            //MarkedByBullet = false; // 每帧重置
        }

        public override void OnKill(NPC npc)
        {
            // 如果被标记且不是 Boss
            if (MarkedByBullet && !npc.boss)
            {
                // 确保力场生成在敌人的中心位置
                Vector2 spawnPosition = npc.Hitbox.Center.ToVector2();

                Projectile.NewProjectile(
                    npc.GetSource_Death(),  // 来源信息
                    spawnPosition,         // 生成位置
                    Vector2.Zero,          // 初始速度
                    ModContent.ProjectileType<StarblightSootBulletArea>(), // 力场类型
                    0,                     // 无直接伤害
                    0,
                    Main.myPlayer          // 当前玩家
                );

                // 调试输出
                //Main.NewText($"生成力场在位置: {spawnPosition}", Color.Green);
            }





            // 清理已死亡的 NPC，确保列表中只保留活跃目标
            if (MarkedByAreaNPCs.Contains(npc))
            {
                MarkedByAreaNPCs.Remove(npc);
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile proj, ref NPC.HitModifiers modifiers)
        {
            if (MarkedByArea && proj.friendly) // 如果被立场笼罩且受到友方弹幕伤害
            {
                // 确保 NPC 被加入全局立场列表
                if (!MarkedByAreaNPCs.Contains(npc))
                    MarkedByAreaNPCs.Add(npc);
            }
        }

        public override void HitEffect(NPC npc, NPC.HitInfo hit)
        {
            if (MarkedByArea) // 如果敌人被立场笼罩
            {
                foreach (var target in MarkedByAreaNPCs.ToList())
                {
                    if (target != npc && target.active) // 不对自身再次施加伤害
                    {
                        // 复制伤害信息并传播
                        var damageInfo = target.CalculateHitInfo(hit.Damage, (int)hit.Knockback, hit.Crit, 0);
                        target.StrikeNPC(damageInfo); // 使用正确的伤害传播逻辑

                        // 添加粒子效果反馈
                        for (int i = 0; i < 10; i++)
                        {
                            Dust.NewDust(target.position, target.width, target.height, DustID.MagicMirror, Scale: 1.5f);
                        }
                    }
                }
            }
        }
    }
}
