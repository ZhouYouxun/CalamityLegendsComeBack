//using Microsoft.Xna.Framework;
//using Terraria;
//using Terraria.ModLoader;

//namespace CalamityLegendsComeBack.Weapons.SHPC.Text
//{
//    public class NL_SHPC_Text_Player : ModPlayer
//    {
//        // 连续静止计时
//        private int idleTimer = 0;

//        // 连续小伤计数
//        private int smallHitCount = 0;

//        // 低血触发锁
//        private bool lowLifeTriggered = false;

//        public override void ResetEffects()
//        {
//            if (Player.whoAmI == Main.myPlayer)
//                NL_SHPC_Text_Core.Update(); // 统一冷却推进
//        }

//        public override void PostUpdate()
//        {
//            if (Player.whoAmI != Main.myPlayer)
//                return;

//            // ===== 生命值 ≤25% =====
//            if (Player.statLife <= Player.statLifeMax2 * 0.25f)
//            {
//                if (!lowLifeTriggered)
//                {
//                    NL_SHPC_Text_Core.Request(Player, "CriticalCondition");
//                    lowLifeTriggered = true;
//                }
//            }
//            else
//            {
//                lowLifeTriggered = false;
//            }

//            // ===== 连续5秒不动 =====
//            if (Player.velocity.Length() <= 0.05f)
//            {
//                idleTimer++;

//                if (idleTimer >= 300)
//                {
//                    NL_SHPC_Text_Core.Request(Player, "Idle");
//                    idleTimer = 0;
//                }
//            }
//            else
//            {
//                idleTimer = 0;
//            }

//            // ===== 敌方弹幕贴脸（<16f）=====
//            for (int i = 0; i < Main.maxProjectiles; i++)
//            {
//                Projectile proj = Main.projectile[i];

//                if (!proj.active || !proj.hostile)
//                    continue;

//                if (Vector2.Distance(Player.Center, proj.Center) < 16f)
//                {
//                    NL_SHPC_Text_Core.Request(Player, "DangerClose");
//                    return;
//                }
//            }
//        }

//        public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
//        {
//            if (Player.whoAmI != Main.myPlayer)
//                return;

//            HandleDamageText(hurtInfo.Damage);
//        }

//        public override void OnHitByProjectile(Projectile proj, Player.HurtInfo hurtInfo)
//        {
//            if (Player.whoAmI != Main.myPlayer)
//                return;

//            HandleDamageText(hurtInfo.Damage);
//        }

//        private void HandleDamageText(int damage)
//        {
//            float maxLife = Player.statLifeMax2;

//            // ===== 单次 ≥50%伤害 =====
//            if (damage >= maxLife * 0.5f)
//            {
//                NL_SHPC_Text_Core.Request(Player, "HeavyDamage");
//            }

//            // ===== 连续三次 ≤5%伤害 =====
//            if (damage <= maxLife * 0.05f)
//            {
//                smallHitCount++;

//                if (smallHitCount >= 3)
//                {
//                    NL_SHPC_Text_Core.Request(Player, "ChipDamage");
//                    smallHitCount = 0;
//                }
//            }
//            else
//            {
//                smallHitCount = 0;
//            }
//        }
//    }
//}