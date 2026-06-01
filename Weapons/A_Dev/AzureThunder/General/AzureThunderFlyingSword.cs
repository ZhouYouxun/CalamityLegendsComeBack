using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityLegendsComeBack.Weapons.Visuals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    // 左键连段生成的飞剑：先蓄势悬停，再沿锁定鼠标方向高速飞出。
    internal sealed class AzureThunderFlyingSword : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/AzureThunder";

        // 本地计时器，控制蓄势、发射和绘制强度。
        private int Timer
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        // 发射后的短时间强化计时，目前主要保留给后续扩展。
        private int LaunchBoostTimer
        {
            get => (int)Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        // ai[0] 是每把飞剑的延迟帧，ai[1]/ai[2] 保存左键段开始时的鼠标坐标。
        private int Delay => Math.Max(0, (int)Projectile.ai[0]);
        private Vector2 StoredMouseWorld => new(Projectile.ai[1], Projectile.ai[2]);

        // 初始悬停点和后撤偏移只在生成首帧计算一次。
        private bool initialized;
        private Vector2 hoverAnchor;
        private Vector2 retreatOffset;

        public override void SetStaticDefaults()
        {
            // 旧位置缓存用于绘制飞剑残影。
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            // 蓄势阶段不友好，真正发射后才打开伤害。
            Projectile.width = 42;
            Projectile.height = 42;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1;
        }

        public override void AI()
        {
            Timer++;
            Player owner = Main.player[Projectile.owner];
            // 如果没有存到鼠标坐标，则默认向玩家朝向前方飞行。
            Vector2 targetPoint = StoredMouseWorld == Vector2.Zero ? owner.Center + Vector2.UnitX * owner.direction * 400f : StoredMouseWorld;

            if (!initialized)
            {
                // 首帧决定后撤方向和左右错位，避免多把飞剑完全重叠。
                initialized = true;
                hoverAnchor = Projectile.Center;
                AzureThunderSounds.PlaySwordMaterialize(Projectile.Center);
                Vector2 aimDirection = (targetPoint - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction);
                float sideOffset = ((Projectile.identity & 1) == 0 ? -1f : 1f) * (6f + Projectile.identity % 3 * 2f);
                retreatOffset = -aimDirection * 34f + aimDirection.RotatedBy(MathHelper.PiOver2) * sideOffset;
            }

            if (Timer <= Delay)
            {
                // 延迟期只做蓄势悬停，不造成伤害。
                Projectile.friendly = false;
                float hoverWave = (float)Math.Sin((Main.GameUpdateCount + Projectile.identity * 17) * 0.08f) * 5f;
                float retreatInterpolant = 1f - (float)Math.Pow(1f - Utils.GetLerpValue(0f, Math.Max(1f, Delay), Timer, true), 2.4f);
                Vector2 desiredCenter = hoverAnchor + retreatOffset * retreatInterpolant + new Vector2(0f, hoverWave);
                Projectile.Center = Vector2.Lerp(Projectile.Center, desiredCenter, 0.34f);
                Projectile.rotation = Projectile.rotation.AngleLerp((targetPoint - Projectile.Center).ToRotation() + MathHelper.PiOver4, 0.15f);
                Projectile.velocity *= 0.78f;
                SpawnChargeDust();
                return;
            }

            if (Timer == Delay + 1)
            {
                // 延迟结束后锁定发射方向；之后不再重新追踪敌人。
                Projectile.friendly = true;
                LaunchBoostTimer = 18;
                Vector2 direction = (targetPoint - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction);

                // 72f 是锁鼠标后的爆发速度，飞剑发射后不再重选目标。
                Projectile.velocity = direction * 72f;
                AzureThunderSounds.PlaySwordLaunch(Projectile.Center);
            }

            if (LaunchBoostTimer > 0)
                LaunchBoostTimer--;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Lighting.AddLight(Projectile.Center, AzureThunderColors.Yellow.ToVector3() * 0.45f);

            if (Main.rand.NextBool(2))
            {
                // 飞行时向后拉出青金色火花，突出高速感。
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity * 0.45f + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.FireworksRGB,
                    -Projectile.velocity * Main.rand.NextFloat(0.02f, 0.07f),
                    0,
                    Main.rand.NextBool() ? AzureThunderColors.Yellow : AzureThunderColors.Azure,
                    Main.rand.NextFloat(0.75f, 1.15f));
                dust.noGravity = true;
            }
        }

        private void SpawnChargeDust()
        {
            // 蓄势粒子不需要每帧生成，三分之一概率足够明显。
            if (!Main.rand.NextBool(3))
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(18f, 18f),
                DustID.FireworksRGB,
                Main.rand.NextVector2Circular(0.6f, 0.6f),
                0,
                AzureThunderColors.PaleYellow,
                Main.rand.NextFloat(0.55f, 0.85f));
            dust.noGravity = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 飞剑命中附加基础电击，并触发饰品联动。
            target.AddBuff(BuffID.Electrified, 180);
            AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);
            AzureThunderSounds.PlaySwordHit(target.Center);
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 spawnPosition = target.Center - forward * 10f * 16f;

            // 命中后补一段小平雷，从飞剑来向劈回目标中心。
            AzureThunderPlayer.SpawnFlatLightning(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                target.Center - spawnPosition,
                Math.Max(1, (int)(Projectile.damage * 0.45f)),
                Projectile.knockBack * 0.25f,
                Projectile.owner,
                0.72f,
                AzureThunderFlatLightning.WeakLightningFlag);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 主体贴图、幽灵贴图和旧位置缓存共同构成飞剑的残影层。
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D ghost = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/AzureThunderGhost").Value;
            Vector2 origin = new(texture.Width * 0.5f, texture.Height * 0.5f);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                // 旧位置越靠后透明度越低。
                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                if (oldCenter == Projectile.Size * 0.5f)
                    continue;

                float opacity = (1f - i / (float)Projectile.oldPos.Length) * 0.28f;
                Main.EntitySpriteDraw(texture, oldCenter - Main.screenPosition, null, AzureThunderColors.Azure with { A = 0 } * opacity, Projectile.rotation, origin, Projectile.scale, effects);
            }

            // 黄色描边让飞剑在高速移动和复杂背景中仍然可读。
            HoldoutOutlineHelper.DrawSolidOutline(
                texture,
                drawPosition,
                Projectile.rotation,
                origin,
                Vector2.One * Projectile.scale,
                effects,
                AzureThunderColors.Yellow,
                2.6f,
                Timer <= Delay ? 0.32f : 0.2f,
                Main.GlobalTimeWrappedHourly + Projectile.identity * 0.1f,
                14);

            for (int i = 0; i < 10; i++)
            {
                // 周围淡金幽灵影在蓄势阶段更宽，表现剑身凝聚。
                Vector2 offset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * (Timer <= Delay ? 5f : 2.5f);
                Main.EntitySpriteDraw(ghost, drawPosition + offset, null, AzureThunderColors.PaleYellow with { A = 0 } * 0.08f, Projectile.rotation, origin, Projectile.scale, effects);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, lightColor, Projectile.rotation, origin, Projectile.scale, effects);
            return false;
        }
    }
}
