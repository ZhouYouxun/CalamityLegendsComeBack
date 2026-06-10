using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    internal class BloodstoneCore_BloodOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int timer;
        private int choiceState = 0; // 0: undecided, 1: chasing player, 2: chasing NPC
        private int chosenPlayerIndex = -1;
        private int chosenNPCIndex = -1;

        public override bool? CanDamage()
        {
            return choiceState == 2 ? null : false;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 210;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void AI()
        {
            timer++;

            // 1. 如果还未做出选择，先搜寻是否有玩家需要治疗
            if (choiceState == 0)
            {
                Player targetPlayer = null;
                float closestPlayerDist = float.MaxValue;
                float playerSearchRange = 3000f; // 范围扩大三倍

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p.active && !p.dead && p.statLife < p.statLifeMax2)
                    {
                        float dist = Vector2.Distance(Projectile.Center, p.Center);
                        if (dist < playerSearchRange && dist < closestPlayerDist)
                        {
                            closestPlayerDist = dist;
                            targetPlayer = p;
                        }
                    }
                }

                if (targetPlayer != null)
                {
                    choiceState = 1;
                    chosenPlayerIndex = targetPlayer.whoAmI;
                }
            }

            // 2. 根据做出的选择执行对应逻辑
            if (choiceState == 1) // 冲向玩家进行治疗
            {
                Player targetPlayer = Main.player[chosenPlayerIndex];
                if (targetPlayer.active && !targetPlayer.dead)
                {
                    float trackingPower = Utils.GetLerpValue(0f, 50f, timer, true);
                    Vector2 desired = (targetPlayer.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * MathHelper.Lerp(16f, 25f, trackingPower);
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, MathHelper.Lerp(0.12f, 0.34f, trackingPower));

                    if (Projectile.Distance(targetPlayer.Center) < 28f || Projectile.Hitbox.Intersects(targetPlayer.Hitbox))
                    {
                        int healAmount = Projectile.damage > 0 ? Projectile.damage : 20;
                        targetPlayer.statLife = System.Math.Min(targetPlayer.statLifeMax2, targetPlayer.statLife + healAmount);
                        targetPlayer.HealEffect(healAmount);

                        for (int j = 0; j < 10; j++)
                        {
                            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, Main.rand.NextVector2Circular(3f, 3f));
                            d.noGravity = true;
                        }

                        Projectile.Kill();
                        return;
                    }
                }
                else
                {
                    // 目标玩家失效（如断开连接或死亡），重新进入未决定状态
                    choiceState = 0;
                }
            }
            else if (choiceState == 2) // 冲向NPC进行攻击
            {
                NPC targetNPC = Main.npc[chosenNPCIndex];
                if (targetNPC.active && targetNPC.CanBeChasedBy())
                {
                    float trackingPower = Utils.GetLerpValue(60f, 110f, timer, true);
                    Vector2 desired = (targetNPC.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * MathHelper.Lerp(16f, 25f, trackingPower);
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, MathHelper.Lerp(0.12f, 0.34f, trackingPower));
                }
                else
                {
                    // 当前锁定的NPC死亡或失效，立刻寻找另一个NPC，若没有则重新进入未决定状态
                    NPC newTarget = Projectile.Center.ClosestNPCAt(1500f);
                    if (newTarget != null)
                    {
                        chosenNPCIndex = newTarget.whoAmI;
                    }
                    else
                    {
                        choiceState = 0;
                    }
                }
            }
            else // choiceState == 0 且没有发现残血玩家
            {
                if (timer < 60)
                {
                    // 一开始快速前进，然后快速减速（0.99倍减速）
                    Projectile.velocity *= 0.99f;
                }
                else if (timer < 120)
                {
                    // 减速成功后，在此处停留在原地一段时间 (乘0.85快速停下并悬停)
                    Projectile.velocity *= 0.85f;
                }
                else
                {
                    // 停留时间结束后，锁定冲向敌人
                    NPC targetNPC = Projectile.Center.ClosestNPCAt(1500f);
                    if (targetNPC != null)
                    {
                        choiceState = 2;
                        chosenNPCIndex = targetNPC.whoAmI;
                    }
                    else
                    {
                        // 依然没有敌人，继续在原地悬停
                        Projectile.velocity *= 0.85f;
                    }
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Color(220, 20, 20).ToVector3() * 0.32f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                    Main.rand.NextBool() ? DustID.Blood : DustID.RedTorch,
                    -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.22f),
                    0,
                    Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float pulse = 0.85f + (float)System.Math.Sin(timer * 0.22f) * 0.12f;

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                new Color(220, 24, 24) with { A = 0 } * 0.5f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                0.38f * pulse,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                Color.White with { A = 0 } * 0.16f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                0.16f * pulse,
                SpriteEffects.None);

            return false;
        }
    }
}
