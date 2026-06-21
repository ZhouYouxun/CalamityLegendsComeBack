using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.ModLoader;
using CalamityMod;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace
{
    public class GlacialDrillProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int timer = 0;
        private bool launched = false;
        private float slotsOccupied = 10f;
        private float extraDamageMultiplier = 1.0f;

        private const int AssemblyTime = 90; // 装配所需时间 (1.5秒)
        private const int FlashTime = 20;     // 闪光提示时间 (0.33秒)
        private const int LaunchDuration = 180; // 推出后持续时间 (3秒)

        private struct DrillSpike
        {
            public int Layer;
            public float BaseAngle;
            public float CurrentRadius;
        }
        private DrillSpike[] spikes;

        public override void SetDefaults()
        {
            Projectile.width = 160;
            Projectile.height = 160;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = AssemblyTime + FlashTime + LaunchDuration;
            Projectile.DamageType = DamageClass.Summon;

            // 高频打击无敌帧设定 (每 3 帧击中一次)
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 3;
        }

        public override bool? CanDamage()
        {
            // 仅在推出后才能造成高频伤害
            return launched ? null : false;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            
            // 统计已占用的召唤槽位
            float slots = 0f;
            int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == spikeType)
                {
                    slots += p.minionSlots;
                    // 清理并回收原版围绕冰刺
                    p.Kill();
                }
            }

            slotsOccupied = slots > 0f ? slots : 1f; // 防呆置为1

            // 冰刺数量 = 槽位 * 3，上限为 30
            int spikeCount = Math.Min(30, (int)Math.Ceiling(slotsOccupied * 3));
            spikes = new DrillSpike[spikeCount];

            // 钻头三层结构组装划分
            // 第一层：最大 9 个
            // 第二层：最大 12 个
            // 第三层：最大 9 个
            for (int i = 0; i < spikeCount; i++)
            {
                int layer;
                float baseAngle;
                if (i < 9)
                {
                    layer = 0; // Layer 1 (半径 40f)
                    baseAngle = MathHelper.TwoPi * i / 9f;
                }
                else if (i < 21)
                {
                    layer = 1; // Layer 2 (半径 75f)
                    baseAngle = MathHelper.TwoPi * (i - 9) / 12f;
                }
                else
                {
                    layer = 2; // Layer 3 (半径 110f)
                    baseAngle = MathHelper.TwoPi * (i - 21) / 9f;
                }
                spikes[i] = new DrillSpike
                {
                    Layer = layer,
                    BaseAngle = baseAngle,
                    CurrentRadius = 300f // 初始从 300 像素远处开始飞入组装
                };
            }

            // 计算超上限槽位倍率 (11个栏位开始，每超出1个加 3% 伤害，爆碎加 18%)
            if (slotsOccupied > 10f)
            {
                float excess = slotsOccupied - 10f;
                extraDamageMultiplier = 1.0f + excess * 0.03f;
            }

            Projectile.netUpdate = true;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            if (!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            timer++;

            // 1. 组装与提示阶段
            if (timer < AssemblyTime + FlashTime)
            {
                // 吸附在玩家前方
                Vector2 targetPos = player.Center + (player.Calamity().mouseWorld - player.Center).SafeNormalize(Vector2.UnitX * player.direction) * 45f;
                Projectile.Center = targetPos;
                Projectile.velocity = Vector2.Zero;

                // 更新钻头内各个冰刺的半径与旋转角
                float progress = Math.Min(1f, (float)timer / AssemblyTime);
                UpdateSpikesPhysics(progress);

                // 完全成型瞬间产生十字提示
                if (timer == AssemblyTime)
                {
                    SoundEngine.PlaySound(SoundID.Item29 with { Pitch = -0.4f, Volume = 1f }, Projectile.Center);
                }

                // 提示期内，检测玩家点击左键强力推出
                if (timer >= AssemblyTime && (player.controlUseItem || Main.mouseLeft) && Main.myPlayer == Projectile.owner)
                {
                    launched = true;
                    // 钻头射向鼠标方向，初速度 12f
                    Projectile.velocity = (player.Calamity().mouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX * player.direction) * 12f;
                    
                    // 设置剩余寿命为 3 秒 (180帧)
                    Projectile.timeLeft = LaunchDuration;
                    timer = AssemblyTime + FlashTime; // 立即跳过提示阶段，进入发射期
                    
                    SoundEngine.PlaySound(SoundID.Item122 with { Pitch = -0.1f, Volume = 1f }, Projectile.Center);
                    
                    // 屏幕震动
                    player.Calamity().GeneralScreenShakePower = Math.Max(player.Calamity().GeneralScreenShakePower, 6.0f);
                    
                    // 推出爆发粒子
                    for (int i = 0; i < 25; i++)
                    {
                        int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Electric;
                        Vector2 pVel = Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.5f, 1.5f);
                        Dust d = Dust.NewDustDirect(Projectile.Center - Projectile.velocity, 0, 0, dType, pVel.X, pVel.Y);
                        d.scale = Main.rand.NextFloat(1.3f, 2.2f);
                        d.noGravity = true;
                    }
                    
                    Projectile.netUpdate = true;
                }
            }
            // 2. 发射高频轰击阶段
            else
            {
                // 无条件将旋转方向指向移动方向
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

                // 减速玩家并赋予无敌状态
                player.velocity *= 0.12f;
                player.controlLeft = player.controlRight = player.controlUp = player.controlDown = false;
                
                // 强制给予玩家无敌判定帧
                player.immune = true;
                player.immuneTime = 5;
                for (int i = 0; i < player.hurtCooldowns.Length; i++)
                {
                    player.hurtCooldowns[i] = 5;
                }

                // 更新冰刺在钻头上的高频反向狂舞旋转
                UpdateSpikesPhysics(1f);

                // 产生持续冰渣与矩阵光芒粒子 (每帧 3 个)
                for (int i = 0; i < 3; i++)
                {
                    int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Electric;
                    Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dType);
                    d.velocity = (Projectile.velocity * -0.3f).RotatedByRandom(0.6f);
                    d.scale = Main.rand.NextFloat(1.1f, 1.8f);
                    d.noGravity = true;
                }
            }
        }

        private void UpdateSpikesPhysics(float progress)
        {
            float time = Main.GlobalTimeWrappedHourly;
            
            // 设定三层组装目标半径
            float[] targetRadii = { 35f, 65f, 95f };
            // 三层旋转速度
            float[] speedMults = { 6f, -4f, 2.5f };

            for (int i = 0; i < spikes.Length; i++)
            {
                int layer = spikes[i].Layer;
                float targetRad = targetRadii[layer];
                
                // 缓动飞入组装
                spikes[i].CurrentRadius = MathHelper.Lerp(300f, targetRad, progress);

                // 自增旋转角度
                float rotationSpeed = MathHelper.ToRadians(speedMults[layer]);
                spikes[i].BaseAngle += rotationSpeed;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // 应用超召唤栏倍率伤害加成 (3% 每栏)
            modifiers.SourceDamage *= extraDamageMultiplier;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn2, 300);
        }

        public override void OnKill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];

            // 终结技结束：爆碎摧毁！
            SoundEngine.PlaySound(SoundID.Item62 with { Pitch = -0.5f, Volume = 1.2f }, Projectile.Center);

            // 增加强烈屏幕震动
            player.Calamity().GeneralScreenShakePower = Math.Max(player.Calamity().GeneralScreenShakePower, 10.0f);

            // 1. 造成大范围终结技爆碎巨额伤害 (高频伤害的6倍)
            var source = player.GetSource_ItemUse(player.HeldItem);
            int baseShatterDmg = (int)(Projectile.damage * 6.0f * extraDamageMultiplier);
            
            int shatter = Projectile.NewProjectile(source, Projectile.Center, Vector2.Zero, ProjectileID.SolarWhipSwordExplosion, baseShatterDmg, 10f, player.whoAmI);
            if (Main.projectile.IndexInRange(shatter))
            {
                Main.projectile[shatter].friendly = true;
                Main.projectile[shatter].hostile = false;
                Main.projectile[shatter].DamageType = DamageClass.Summon;
            }

            // 2. 喷射高密度爆破雪花与科技矩阵粒子
            for (int i = 0; i < 45; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(8f, 8f);
                int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Electric;
                Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, dType, vel.X, vel.Y);
                d.scale = Main.rand.NextFloat(1.5f, 2.8f);
                d.noGravity = true;
            }

            // 3. 彻底移除冰寒神性 Buff 状态并清空仆从
            player.ClearBuff(ModContent.BuffType<GlacialEmbraceBuff>());
            
            // 清理掉不占仆从栏的冰块随从
            int blockType = ModContent.ProjectileType<IceBlockMinion>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == blockType)
                {
                    p.Kill();
                }
            }

            CombatText.NewText(player.getRect(), Color.LightSkyBlue, "Shattered!", true);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D spikeTex = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Summon/GlacialEmbrace").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/star_01").Value;
            Texture2D twirl = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/twirl_02").Value;
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            float time = Main.GlobalTimeWrappedHourly;
            Vector2 drillCenter = Projectile.Center - Main.screenPosition;

            // 1. 绘制推出后的等离子旋转风暴气流
            if (launched)
            {
                Color twirlCol = new Color(0, 185, 255, 0) * 0.45f;
                Main.spriteBatch.Draw(twirl, drillCenter, null, twirlCol, time * 12f, twirl.Size() * 0.5f, 0.72f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(twirl, drillCenter, null, twirlCol * 0.6f, -time * 7f, twirl.Size() * 0.5f, 0.55f, SpriteEffects.None, 0f);
            }

            // 2. 遍历绘制钻头上装配的每一片反旋冰刺，并绘制与中心及同层之间的矩阵网格连线
            if (spikes != null)
            {
                Color lineCol = new Color(0, 195, 255, 0) * (0.35f * (launched ? 0.8f : (timer / (float)AssemblyTime)));

                for (int i = 0; i < spikes.Length; i++)
                {
                    float angle = spikes[i].BaseAngle;
                    Vector2 spikeOffset = angle.ToRotationVector2() * spikes[i].CurrentRadius;
                    Vector2 spikePos = Projectile.Center + spikeOffset;

                    // A. 绘制中心连线
                    Vector2 lineStart = Projectile.Center - Main.screenPosition;
                    Vector2 lineEnd = spikePos - Main.screenPosition;
                    Vector2 edge = lineEnd - lineStart;
                    if (edge.LengthSquared() > 0.001f)
                    {
                        Main.spriteBatch.Draw(pixel, lineStart, new Rectangle(0, 0, 1, 1), lineCol * 0.6f,
                            edge.ToRotation(), new Vector2(0f, 0.5f), new Vector2(edge.Length(), 1.2f), SpriteEffects.None, 0f);
                    }

                    // B. 绘制与同层相邻冰刺的矩阵环网格连线
                    int nextIdx = -1;
                    for (int j = i + 1; j < spikes.Length; j++)
                    {
                        if (spikes[j].Layer == spikes[i].Layer)
                        {
                            nextIdx = j;
                            break;
                        }
                    }
                    if (nextIdx != -1)
                    {
                        Vector2 nextPos = Projectile.Center + spikes[nextIdx].BaseAngle.ToRotationVector2() * spikes[nextIdx].CurrentRadius;
                        Vector2 edgeNet = (nextPos - Main.screenPosition) - lineEnd;
                        if (edgeNet.LengthSquared() > 0.001f)
                        {
                            Main.spriteBatch.Draw(pixel, lineEnd, new Rectangle(0, 0, 1, 1), lineCol * 0.4f,
                                edgeNet.ToRotation(), new Vector2(0f, 0.5f), new Vector2(edgeNet.Length(), 1.0f), SpriteEffects.None, 0f);
                        }
                    }

                    // C. 绘制冰刺贴图 (尖端指向外侧垂直切面)
                    Color spikeCol = Color.Lerp(Color.Cyan, Color.White, 0.3f);
                    Main.spriteBatch.Draw(spikeTex, lineEnd, null, spikeCol * 0.95f, angle + MathHelper.PiOver2, spikeTex.Size() * 0.5f, 0.9f, SpriteEffects.None, 0f);
                }
            }

            // 3. 绘制成型瞬间的旋转十字星爆
            if (timer >= AssemblyTime && timer < AssemblyTime + FlashTime && !launched)
            {
                float flashFrac = (timer - AssemblyTime) / (float)FlashTime;
                float rot = (float)Math.Sin(flashFrac * 12f) * 2f;
                float scale = (1f - flashFrac) * 0.5f;

                Color flashCol = Color.Lerp(Color.Cyan, Color.White, 0.5f) * (1f - flashFrac) * 0.9f;
                
                // 正十字
                Main.spriteBatch.Draw(star, drillCenter, null, flashCol, rot, star.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                // 斜十字 45 度
                Main.spriteBatch.Draw(star, drillCenter, null, flashCol * 0.7f, rot + MathHelper.PiOver4, star.Size() * 0.5f, scale * 0.8f, SpriteEffects.None, 0f);
            }

            return false;
        }
    }
}
