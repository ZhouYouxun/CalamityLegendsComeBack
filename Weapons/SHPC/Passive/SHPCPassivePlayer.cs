//using Microsoft.Xna.Framework;
//using System;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace CalamityLegendsComeBack.Weapons.SHPC.Passive
//{
//    internal sealed class SHPCPassivePlayer : ModPlayer
//    {
//        private bool holdingSHPC;
//        private float manaRegenAccumulator;
//        private int passiveVisualTimer;

//        public override void ResetEffects()
//        {
//            holdingSHPC = false;
//        }

//        public override void UpdateDead()
//        {
//            holdingSHPC = false;
//            manaRegenAccumulator = 0f;
//            passiveVisualTimer = 0;
//        }

//        public void SetHoldingSHPC()
//        {
//            holdingSHPC = true;
//        }

//        public override void PostUpdate()
//        {
//            if (!holdingSHPC || Player.HeldItem.type != ModContent.ItemType<NewLegendSHPC>())
//            {
//                manaRegenAccumulator = 0f;
//                passiveVisualTimer = 0;
//                return;
//            }

//            if (!PassiveCanTrigger())
//            {
//                passiveVisualTimer = 0;
//                return;
//            }

//            RestoreManaPerFrame();

//            if (Player.whoAmI != Main.myPlayer)
//                return;

//            if (++passiveVisualTimer >= 5)
//            {
//                passiveVisualTimer = 0;

//                // ⭐关键：二次判断（最终判定）
//                if (Player.statMana < Player.statManaMax2)
//                {
//                    SpawnGravityOrbBurst();
//                }
//            }
//        }

//        private bool PassiveCanTrigger()
//        {
//            bool stationary = Player.velocity.LengthSquared() <= 0.01f && Player.grapCount <= 0;
//            bool notAttacking = Player.itemAnimation <= 0 && Player.itemTime <= 0;
//            bool noHeldProjectile = Player.ownedProjectileCounts[ModContent.ProjectileType<RightClick.SHPCRight_HoulOut>()] <= 0;

//            return stationary &&
//                   notAttacking &&
//                   noHeldProjectile &&
//                   !Player.controlUseItem &&
//                   !Player.controlUseTile &&
//                   !Player.pulley &&
//                   !Player.dead &&
//                   Player.statMana < Player.statManaMax2; // ⭐新增：蓝没满才触发
//        }

//        private void RestoreManaPerFrame()
//        {
//            if (Player.statMana >= Player.statManaMax2)
//                return;

//            manaRegenAccumulator += Player.statManaMax2 * 0.01f;
//            int manaToRestore = (int)manaRegenAccumulator;
//            if (manaToRestore <= 0)
//                return;

//            manaRegenAccumulator -= manaToRestore;
//            int previousMana = Player.statMana;
//            Player.statMana = Utils.Clamp(Player.statMana + manaToRestore, 0, Player.statManaMax2);

//            int restored = Player.statMana - previousMana;
//            if (restored > 0 && Main.GameUpdateCount % 15 == 0)
//                Player.ManaEffect(restored);
//        }

//        private void SpawnGravityOrbBurst()
//        {
//            int orbCount = Main.rand.Next(1, 3);
//            for (int i = 0; i < orbCount; i++)
//            {
//                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
//                //float distance = Main.rand.NextFloat(360f, 360f);


//                float inner = 12f * 16f;
//                float outer = 16f * 16f;

//                // √随机，保证圆盘均匀分布
//                float t = Main.rand.NextFloat();
//                float distance = MathF.Sqrt(t * (outer * outer - inner * inner) + inner * inner);

//                Vector2 spawnOffset = angle.ToRotationVector2() * distance;
//                Vector2 spawnPosition = Player.Center + spawnOffset;

//                Vector2 inwardVelocity = (-spawnOffset).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.8f, 3.6f);
//                inwardVelocity += spawnOffset.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(-0.7f, 0.7f);

//                Projectile.NewProjectile(
//                    Player.GetSource_FromThis(),
//                    spawnPosition,
//                    inwardVelocity,
//                    ModContent.ProjectileType<SHPCPassiveOrb>(),
//                    0,
//                    0f,
//                    Player.whoAmI,
//                    Player.whoAmI,
//                    Main.rand.NextFloat(0.85f, 1.2f));
//            }
//        }
//    }
//}
