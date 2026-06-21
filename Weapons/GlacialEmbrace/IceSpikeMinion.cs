using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace
{
    public class IceSpikeMinion : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Summon/GlacialEmbrace"; // 复用原版贴图

        // 斩击模式变量
        private float outerAngleOffset = 0f;
        private List<int> innerSlashHitNPCs = new List<int>();
        private List<int> outerSlashHitNPCs = new List<int>();
        private float innerPrevAngle = 0f;
        private float outerPrevAngle = 0f;

        // 突刺模式变量
        public bool embedded = false;
        public int embedNPCIndex = -1;
        public Vector2 embedOffset = Vector2.Zero;
        public int existTimer = 0;
        public bool flying = false;
        public int pierceTimer = 0;

        // 打击模式与休眠变量
        public bool hibernating = false;
        public int hibernateTimer = 0;
        public bool smashing = false;
        public float smashProgress = 0f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailingMode[Type] = 0;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 48;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 1f; // 默认占用 1 栏位
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
        }

        // 辅助方法：判断是否正在环绕玩家（非发射、非钉入、非终结技组装）
        public bool IsCirclingPlayer()
        {
            return !embedded && !flying && !smashing && !IsUltimateActive();
        }

        private bool IsUltimateActive()
        {
            Player player = Main.player[Projectile.owner];
            return player.ownedProjectileCounts[ModContent.ProjectileType<GlacialDrillProj>()] > 0;
        }

        public override bool? CanDamage()
        {
            if (hibernating) return false;
            if (embedded) return false; // 钉在怪身上时不直接造成伤害
            return null;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            // 维持生命周期
            if (!player.active || player.dead || !modPlayer.GlacialEmbraceMinion)
            {
                Projectile.Kill();
                return;
            }

            // 处理休眠冷却
            if (hibernating)
            {
                Projectile.friendly = false;
                hibernateTimer--;
                if (hibernateTimer <= 0)
                {
                    hibernating = false;
                    Projectile.friendly = true;
                    // 激活时释放粒子
                    for (int i = 0; i < 15; i++)
                    {
                        Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Ice);
                        d.velocity = Main.rand.NextVector2Circular(3f, 3f);
                        d.noGravity = true;
                    }
                }
            }

            // ===== 分支1：终结技樊寒神钻状态 =====
            if (IsUltimateActive())
            {
                HandleUltimateAssembly(player);
                return;
            }

            // 根据当前玩家所选模式执行 AI
            if (modPlayer.CurrentMode == 0) // 斩击模式
            {
                HandleSlashMode(player, modPlayer);
            }
            else if (modPlayer.CurrentMode == 1) // 突刺模式
            {
                HandlePierceMode(player, modPlayer);
            }
            else // 打击模式
            {
                HandleStrikeMode(player, modPlayer);
            }
        }

        #region ===== 斩击模式逻辑 =====
        private void HandleSlashMode(Player player, GlacialEmbracePlayer modPlayer)
        {
            Projectile.minionSlots = 0.5f; // 斩击形态只占 0.5 栏位
            Projectile.timeLeft = 2;
            Projectile.friendly = true;

            // 冰刺旋转角速度
            float speedMult = modPlayer.GlacialDivinityTimer > 0 ? 1.1f : 1.0f;
            float innerSpeed = MathHelper.ToRadians(4f * speedMult);
            float outerSpeed = MathHelper.ToRadians(2.8f * speedMult);

            // 更新内、外冰刺旋转角
            Projectile.ai[0] -= innerSpeed;
            outerAngleOffset -= outerSpeed;

            float innerAngle = Projectile.ai[0];
            float outerAngle = Projectile.ai[0] + MathHelper.Pi + outerAngleOffset;

            // 检测角度绕行是否完成一周以清空碰撞列表
            if (Math.Abs(innerAngle - innerPrevAngle) >= MathHelper.TwoPi)
            {
                innerSlashHitNPCs.Clear();
                innerPrevAngle = innerAngle;
            }
            if (Math.Abs(outerAngle - outerPrevAngle) >= MathHelper.TwoPi)
            {
                outerSlashHitNPCs.Clear();
                outerPrevAngle = outerAngle;
            }

            // 更新核心弹幕的物理坐标为内侧小冰刺坐标
            Projectile.Center = player.Center + innerAngle.ToRotationVector2() * 90f + Vector2.UnitY * player.gfxOffY;
            Projectile.rotation = innerAngle + MathHelper.PiOver2;
        }

        // 重写碰撞函数：支持大小冰晶独立检测
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            if (modPlayer.CurrentMode == 0 && IsCirclingPlayer())
            {
                // 检测小冰刺碰撞 (半径 90f)
                float innerAngle = Projectile.ai[0];
                Vector2 innerPos = player.Center + innerAngle.ToRotationVector2() * 90f;
                Rectangle innerHitbox = new Rectangle((int)innerPos.X - 16, (int)innerPos.Y - 16, 32, 32);
                if (innerHitbox.Intersects(targetHitbox)) return true;

                // 检测大冰刺碰撞 (半径 140f)
                float outerAngle = Projectile.ai[0] + MathHelper.Pi + outerAngleOffset;
                Vector2 outerPos = player.Center + outerAngle.ToRotationVector2() * 140f;
                Rectangle outerHitbox = new Rectangle((int)outerPos.X - 24, (int)outerPos.Y - 24, 48, 48);
                if (outerHitbox.Intersects(targetHitbox)) return true;

                return false;
            }

            return base.Colliding(projHitbox, targetHitbox);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            if (modPlayer.CurrentMode == 0 && IsCirclingPlayer())
            {
                // 斩击模式下限制：每个周期每个冰晶对每个敌怪只能造成一次伤害
                float innerAngle = Projectile.ai[0];
                Vector2 innerPos = player.Center + innerAngle.ToRotationVector2() * 90f;
                Rectangle innerHitbox = new Rectangle((int)innerPos.X - 16, (int)innerPos.Y - 16, 32, 32);

                float outerAngle = Projectile.ai[0] + MathHelper.Pi + outerAngleOffset;
                Vector2 outerPos = player.Center + outerAngle.ToRotationVector2() * 140f;
                Rectangle outerHitbox = new Rectangle((int)outerPos.X - 24, (int)outerPos.Y - 24, 48, 48);

                bool innerHit = innerHitbox.Intersects(target.getRect());
                bool outerHit = outerHitbox.Intersects(target.getRect());

                bool hitAllowed = false;

                if (innerHit && !innerSlashHitNPCs.Contains(target.whoAmI))
                {
                    innerSlashHitNPCs.Add(target.whoAmI);
                    hitAllowed = true;
                    // 小冰晶伤害微降
                    modifiers.SourceDamage *= 0.85f;
                }
                if (outerHit && !outerSlashHitNPCs.Contains(target.whoAmI))
                {
                    outerSlashHitNPCs.Add(target.whoAmI);
                    hitAllowed = true;
                    // 大冰晶伤害提升
                    modifiers.SourceDamage *= 1.35f;
                }

                if (!hitAllowed)
                {
                    modifiers.SourceDamage *= 0f; // 已经hit过，取消本次伤害
                }
            }
        }
        #endregion

        #region ===== 突刺模式逻辑 =====
        private void HandlePierceMode(Player player, GlacialEmbracePlayer modPlayer)
        {
            Projectile.minionSlots = 1.0f;

            if (embedded)
            {
                // 钉在敌怪身上
                NPC targetNPC = Main.npc[embedNPCIndex];
                if (!targetNPC.active || targetNPC.life <= 0)
                {
                    Projectile.Kill();
                    return;
                }

                Projectile.Center = targetNPC.Center + embedOffset.RotatedBy(targetNPC.rotation);
                Projectile.rotation = embedOffset.ToRotation() + MathHelper.PiOver2;
                Projectile.friendly = false;
                Projectile.timeLeft = 2; // 不自然死亡
            }
            else if (flying)
            {
                // 直线贯穿发射阶段
                Projectile.friendly = true;
                Projectile.penetrate = -1; // 贯穿无限敌人
                
                // 旋转朝向移动方向
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

                pierceTimer++;
                if (pierceTimer >= 25) // 飞出25帧(约400像素)后碎裂
                {
                    Projectile.Kill();
                }
            }
            else
            {
                // 正常围绕玩家旋转
                Projectile.timeLeft = 2;
                Projectile.friendly = true;
                
                float angle = Projectile.ai[0];
                Projectile.Center = player.Center + angle.ToRotationVector2() * 90f + Vector2.UnitY * player.gfxOffY;
                Projectile.rotation = angle + MathHelper.PiOver2;

                float speed = MathHelper.ToRadians(4f * (modPlayer.GlacialDivinityTimer > 0 ? 1.1f : 1.0f));
                Projectile.ai[0] -= speed;

                // 寻找前方可锁定的敌人，若存在一段时间且距离合适，自动飞出刺向敌人
                existTimer++;
                if (existTimer >= 180) // 存在 3 秒后寻找目标飞出
                {
                    NPC target = CalamityUtils.MinionHoming(Projectile.Center, 800f, player);
                    if (target != null)
                    {
                        flying = true;
                        existTimer = 0;
                        pierceTimer = 0;
                        Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 16f;
                        Projectile.netUpdate = true;
                    }
                }
            }
        }

        // 被鞭子或冰楔触发贯穿
        public void PierceThrust(Vector2 direction)
        {
            if (!embedded) return;
            embedded = false;
            flying = true;
            pierceTimer = 0;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            
            // 朝向被贯穿的方向射出
            Projectile.velocity = direction * 18f;
            Projectile.netUpdate = true;

            // 极速贯穿冰碎屑音效
            SoundEngine.PlaySound(SoundID.Item30 with { Pitch = 0.5f, Volume = 0.7f }, Projectile.Center);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            // 给敌人上冻伤 Debuff
            target.AddBuff(BuffID.Frostburn2, 300);

            if (modPlayer.CurrentMode == 1 && flying && !embedded)
            {
                // 检查已钉在该目标身上的冰刺总数
                int embeddedCount = 0;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == Type && p.owner == player.whoAmI)
                    {
                        var modProj = p.ModProjectile as IceSpikeMinion;
                        if (modProj != null && modProj.embedded && modProj.embedNPCIndex == target.whoAmI)
                        {
                            embeddedCount++;
                        }
                    }
                }

                if (embeddedCount < 6)
                {
                    // 成功钉入
                    embedded = true;
                    flying = false;
                    embedNPCIndex = target.whoAmI;
                    embedOffset = Projectile.Center - target.Center;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.netUpdate = true;

                    SoundEngine.PlaySound(SoundID.Item30 with { Pitch = -0.3f, Volume = 0.8f }, Projectile.Center);
                }
                else
                {
                    // 超过6个，直接碎裂
                    Projectile.Kill();
                }
            }
        }
        #endregion

        #region ===== 打击模式逻辑 =====
        private void HandleStrikeMode(Player player, GlacialEmbracePlayer modPlayer)
        {
            Projectile.minionSlots = 1.0f;
            Projectile.timeLeft = 2;

            if (smashing)
            {
                // 执行向心碰撞
                smashProgress += 0.12f; // 约 8 帧完成碰撞
                if (smashProgress >= 1f)
                {
                    // 发生碰撞！产生巨大冲击波并进入休眠
                    TriggerSmashCollision(player);
                }
                else
                {
                    // 朝着对向位置聚拢
                    Vector2 targetPos = Vector2.Lerp(Projectile.ai[1].ToRotationVector2(), Vector2.Zero, smashProgress);
                    Projectile.Center = player.Center + targetPos;
                }
            }
            else
            {
                // 判断是否处于打击的“对齐”状态下 (高频间隔，每 360 帧)
                // 我们可以让武器在特定时刻将 spikes 强制对齐。
                // 为了简化控制，我们检查 `GlacialEmbraceChargeHoldout` 是否正在对齐我们，
                // 或者在没有对齐指令时，保持反转环绕玩家的状态
                float angle = Projectile.ai[0];
                Projectile.Center = player.Center + angle.ToRotationVector2() * 90f + Vector2.UnitY * player.gfxOffY;
                
                // 朝内反转指向 (rotation = angle - PiOver2)
                Projectile.rotation = angle - MathHelper.PiOver2;

                if (!hibernating)
                {
                    float speed = MathHelper.ToRadians(4f * (modPlayer.GlacialDivinityTimer > 0 ? 1.1f : 1.0f));
                    Projectile.ai[0] -= speed;
                }
            }
        }

        // 由控制器触发的对齐排列
        public void AlignForStrike(Vector2 centerLine, Vector2 orthoVec, int index, int totalCount)
        {
            if (hibernating || smashing) return;

            // 均分在射线两侧
            bool isLeftRow = index % 2 == 0;
            float rowOffset = isLeftRow ? 70f : -70f;
            float lineStep = (index / 2) * 50f + 40f; // 沿射线向前排布

            Vector2 targetPos = centerLine * lineStep + orthoVec * rowOffset;
            Projectile.Center = Vector2.Lerp(Projectile.Center, targetPos, 0.25f);
            
            // 冰刺尖端对准中线
            Projectile.rotation = (centerLine * lineStep - (Projectile.Center - Main.player[Projectile.owner].Center)).ToRotation() + MathHelper.PiOver2;
            
            // 存储目标中线落点以备砸击
            Projectile.ai[1] = (targetPos - Main.player[Projectile.owner].Center).X; // 临时存储X偏移
            Projectile.ai[2] = (targetPos - Main.player[Projectile.owner].Center).Y; // 临时存储Y偏移
        }

        public void ExecuteSmash()
        {
            if (hibernating || smashing) return;
            smashing = true;
            smashProgress = 0f;
            
            // ai[1]/ai[2] 融合成对向起点位置
            Vector2 startOffset = new Vector2(Projectile.ai[1], Projectile.ai[2]);
            Projectile.ai[1] = startOffset.X;
            Projectile.ai[2] = startOffset.Y;
            
            SoundEngine.PlaySound(SoundID.Item30 with { Pitch = 0.4f, Volume = 0.5f }, Projectile.Center);
        }

        private void TriggerSmashCollision(Player player)
        {
            smashing = false;
            hibernating = true;
            hibernateTimer = 240; // 冷却休眠 4 秒
            Projectile.friendly = false;

            // 产生圆形震荡冲击波弹幕
            var source = player.GetSource_ItemUse(player.HeldItem);
            int wave = Projectile.NewProjectile(source, Projectile.Center, Vector2.Zero, ProjectileID.SolarWhipSwordExplosion, (int)(Projectile.damage * 2.2f), 6f, player.whoAmI);
            if (Main.projectile.IndexInRange(wave))
            {
                Main.projectile[wave].DamageType = DamageClass.Summon;
                Main.projectile[wave].friendly = true;
                Main.projectile[wave].hostile = false;
            }

            // 冲击波与矩阵科技粒子
            for (int i = 0; i < 35; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(7f, 7f);
                int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Electric;
                Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, dType, vel.X, vel.Y);
                d.scale = Main.rand.NextFloat(1.3f, 2.2f);
                d.noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.8f, Pitch = 0.2f }, Projectile.Center);
            
            // 冲击波屏幕震动
            player.Calamity().GeneralScreenShakePower = Math.Max(player.Calamity().GeneralScreenShakePower, 3.5f);
        }
        #endregion

        #region ===== 终结技神钻装配 =====
        private void HandleUltimateAssembly(Player player)
        {
            Projectile.minionSlots = 0f; // 终结技中不占仆从栏
            Projectile.friendly = true;

            // 寻找神钻主弹幕
            int drillType = ModContent.ProjectileType<GlacialDrillProj>();
            Projectile drill = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == drillType)
                {
                    drill = p;
                    break;
                }
            }

            if (drill == null)
            {
                Projectile.Kill();
                return;
            }

            // 终结技钻头状态下，我们由钻头主控制器来直接驱动和设置我们的坐标，这里只需要进行随从的更新
            Projectile.timeLeft = 2;
        }
        #endregion

        public override void OnKill(int timeLeft)
        {
            // 如果是在终结技结束之外毁灭，才播散碎裂粒子
            Player player = Main.player[Projectile.owner];
            if (player.active && player.ownedProjectileCounts[ModContent.ProjectileType<GlacialDrillProj>()] == 0)
            {
                for (int i = 0; i < 15; i++)
                {
                    Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Ice);
                    d.velocity = Main.rand.NextVector2Circular(2.5f, 2.5f);
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            var modPlayer = Main.player[Projectile.owner].GetModPlayer<GlacialEmbracePlayer>();

            // 如果处于终结技形态，由樊寒神钻本身集中绘制，这里直接跳过绘制以防重影
            if (IsUltimateActive()) return false;

            float time = Main.GlobalTimeWrappedHourly;
            Vector2 drawCenter = Projectile.Center - Main.screenPosition;

            // 1. 斩击模式双重冰刺绘制
            if (modPlayer.CurrentMode == 0 && IsCirclingPlayer())
            {
                // 绘制内侧小冰晶 (ai[0])
                float innerAngle = Projectile.ai[0];
                Vector2 innerPos = Main.player[Projectile.owner].Center + innerAngle.ToRotationVector2() * 90f + Vector2.UnitY * Main.player[Projectile.owner].gfxOffY - Main.screenPosition;
                Color innerCol = new Color(130, 230, 255) * 0.9f;
                Main.spriteBatch.Draw(texture, innerPos, null, innerCol, innerAngle + MathHelper.PiOver2, texture.Size() * 0.5f, 0.8f, SpriteEffects.None, 0f);

                // 绘制外侧大冰晶 (ai[0] + Pi + offset)
                float outerAngle = Projectile.ai[0] + MathHelper.Pi + outerAngleOffset;
                Vector2 outerPos = Main.player[Projectile.owner].Center + outerAngle.ToRotationVector2() * 140f + Vector2.UnitY * Main.player[Projectile.owner].gfxOffY - Main.screenPosition;
                Color outerCol = Color.Lerp(new Color(0, 180, 255), Color.White, 0.3f + 0.3f * MathF.Sin(time * 6f)) * 0.95f;
                Main.spriteBatch.Draw(texture, outerPos, null, outerCol, outerAngle + MathHelper.PiOver2, texture.Size() * 0.5f, 1.3f, SpriteEffects.None, 0f);

                return false;
            }

            // 2. 打击休眠状态绘制 (半透明虚化)
            if (hibernating)
            {
                Color sleepCol = new Color(50, 100, 150) * 0.3f;
                Main.spriteBatch.Draw(texture, drawCenter, null, sleepCol, Projectile.rotation, texture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                return false;
            }

            // 3. 正常绘制 (带有霓虹发光色)
            Color glowColor = Color.Lerp(new Color(0, 195, 255), Color.White, 0.2f + 0.2f * MathF.Sin(time * 6f));
            Main.spriteBatch.Draw(texture, drawCenter, null, glowColor * 0.95f, Projectile.rotation, texture.Size() * 0.5f, 1.0f, SpriteEffects.None, 0f);

            return false;
        }
    }
}
