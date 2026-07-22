using System;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick
{
    internal class BFLeftPlagueReaper : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        //public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/LeftClick/BlossomFluxBOMB";

        // 缩放方向控制
        private bool scaleExpand = true;

        // 追踪模式触发时间（每个弹幕随机）
        private int homingStartTime;

        // 是否已经进入追踪
        private bool homingActivated;

        // 追踪启动时的初始速度
        private float initialHomingSpeed = -1f;

        // 追踪累计 ticks
        private int homingTicks;

        // 飞行螺旋特效的相位
        private float spiralTime;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.alpha = 255;
            Projectile.extraUpdates = 1;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.light = 0.2f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // 每个弹幕随机不同的追踪启动时间
            // extraUpdates=1，所以这里实际上会很灵动
            homingStartTime = Main.rand.Next(25, 90);
        }

        public override void AI()
        {
            // 淡入：原本每 tick 只减 2，弹幕飞出大半程还是接近全透明，基本看不见。
            // 改成几帧内就拉满不透明度，只保留一点点出膛的渐显感。
            Projectile.alpha -= 26;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            // 呼吸缩放效果
            if (scaleExpand)
            {
                Projectile.scale += 0.05f;

                if (Projectile.scale >= 1.2f)
                    scaleExpand = false;
            }
            else
            {
                Projectile.scale -= 0.05f;

                if (Projectile.scale <= 0.8f)
                    scaleExpand = true;
            }

            // 原本的上下浮动飞行
            Projectile.ai[0] += 1f;

            if (Projectile.ai[0] >= 20f && Projectile.ai[0] < 40f)
            {
                Projectile.velocity.Y += 0.3f;
                Projectile.velocity.X *= 0.98f;
            }
            else if (Projectile.ai[0] >= 40f && Projectile.ai[0] < 60f)
            {
                Projectile.velocity.Y -= 0.3f;
                Projectile.velocity.X *= 1.02f;
            }
            else if (Projectile.ai[0] >= 60f)
            {
                Projectile.ai[0] = 0f;
            }

            // 到达随机时间后开启追踪
            if (!homingActivated && Projectile.timeLeft <= (300 - homingStartTime))
            {
                homingActivated = true;
            }

            // 追踪逻辑
            if (homingActivated)
            {
                NPC target = FindClosestNPC(900f);

                if (target != null)
                {
                    if (initialHomingSpeed <= 0f)
                    {
                        initialHomingSpeed = Projectile.velocity.Length();
                        if (initialHomingSpeed < 10f)
                            initialHomingSpeed = 10f;
                    }

                    homingTicks++;

                    // 逐渐加速到 2 倍速度
                    float speedMultiplier = MathHelper.Lerp(1f, 2f, Math.Min(1f, homingTicks / 120f));
                    float desiredSpeed = initialHomingSpeed * speedMultiplier;

                    Vector2 targetDirection = Projectile.DirectionTo(target.Center);

                    // 目标速度
                    Vector2 desiredVelocity = targetDirection * desiredSpeed;

                    // 随着速度变快，也逐渐增加转向灵敏度，防止速度过快导致绕圈追不上
                    float lerpAmount = MathHelper.Lerp(0.06f, 0.15f, Math.Min(1f, homingTicks / 120f));

                    // 平滑追踪
                    Projectile.velocity = Vector2.Lerp(
                        Projectile.velocity,
                        desiredVelocity,
                        lerpAmount);
                }
            }

            // 旋转
            Projectile.rotation += Projectile.velocity.X * 0.03f;

            // 自带照明，暗处也能一眼看到弹道
            Lighting.AddLight(Projectile.Center, new Vector3(0.36f, 0.62f, 0.18f) * Projectile.Opacity);

            // 飞行特效：绕弹体螺旋的孢子环 + 尾迹花粉带
            if (!Main.dedServ && Projectile.FinalExtraUpdate())
            {
                spiralTime += 0.42f;
                Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 side = forward.RotatedBy(MathHelper.PiOver2);

                // 双股螺旋孢子：绕着飞行轴转，速度越快螺距越长
                for (int s = -1; s <= 1; s += 2)
                {
                    float phase = spiralTime + (s > 0 ? 0f : MathHelper.Pi);
                    float radius = 7f + 2.5f * MathF.Sin(spiralTime * 0.5f);
                    Vector2 orbit = side * MathF.Sin(phase) * radius * Projectile.scale;

                    Dust spore = Dust.NewDustPerfect(
                        Projectile.Center + orbit - forward * Main.rand.NextFloat(0f, 5f),
                        DustID.GreenTorch,
                        forward * MathF.Cos(phase) * 0.6f + side * MathF.Cos(phase) * 1.4f * s,
                        90,
                        Color.Lerp(new Color(182, 220, 82), new Color(238, 255, 166), Main.rand.NextFloat()),
                        Main.rand.NextFloat(0.75f, 1.05f));
                    spore.noGravity = true;
                    spore.fadeIn = 0.4f;
                }

                // 尾迹花粉带：贴着弹道缓慢散开，拉出一条能看清的绿线
                if (Main.rand.NextBool(2))
                {
                    Dust pollen = Dust.NewDustPerfect(
                        Projectile.Center - forward * Main.rand.NextFloat(4f, 14f) + Main.rand.NextVector2Circular(2.5f, 2.5f),
                        DustID.TerraBlade,
                        -forward * Main.rand.NextFloat(0.4f, 1.3f),
                        120,
                        new Color(210, 255, 120),
                        Main.rand.NextFloat(0.5f, 0.85f));
                    pollen.noGravity = true;
                }
            }

            // PBG 掉落武器的毒牙/针筒语汇：黄绿毒液与少量橙色警戒火花，而不是彩虹色叶尘。
            if (!Main.dedServ && Projectile.FinalExtraUpdate() && Main.rand.NextBool(3))
            {
                Color plagueCore = new(130, 236, 70);
                Color plagueWarning = new(244, 153, 48);
                Dust venom = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(2f, 8f),
                    Main.rand.NextBool() ? DustID.PoisonStaff : DustID.VenomStaff,
                    -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.11f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    110,
                    Color.Lerp(plagueCore, plagueWarning, Main.rand.NextFloat(0f, 0.2f)),
                    Main.rand.NextFloat(0.62f, 0.92f));
                venom.noGravity = true;
            }
        }

        // 搜索最近敌人
        private NPC FindClosestNPC(float maxDetectDistance)
        {
            NPC closestNPC = null;
            float sqrMaxDistance = maxDetectDistance * maxDetectDistance;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active || npc.friendly || npc.dontTakeDamage)
                    continue;

                float sqrDistance = Vector2.DistanceSquared(Projectile.Center, npc.Center);

                if (sqrDistance < sqrMaxDistance)
                {
                    sqrMaxDistance = sqrDistance;
                    closestNPC = npc;
                }
            }

            return closestNPC;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color plagueCore = new(130, 236, 70);
            Color plagueWarning = new(244, 153, 48);
            float warningPulse = 0.12f + 0.1f * (0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 7f + Projectile.identity));
            // 本体提亮一档，别让它在深色背景里糊成一团暗绿
            Color body = Color.Lerp(Color.Lerp(plagueCore, plagueWarning, warningPulse), Color.White, 0.22f);
            return body * Projectile.Opacity;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(BlossomFluxSounds.LeftPlagueProjHit, target.Center);
        }

        public override void OnKill(int timeLeft)
        {
            BlossomFluxSounds.PlayLeftPlagueProjKill(Projectile.Center);

            Color plagueCore = new(130, 236, 70);
            Color plagueWarning = new(244, 153, 48);
            for (int d = 0; d < 22; d++)
            {
                Dust venom = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                    Main.rand.NextBool() ? DustID.PoisonStaff : DustID.VenomStaff,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 5.6f),
                    90,
                    Color.Lerp(plagueCore, Main.rand.NextBool(5) ? plagueWarning : Color.White, Main.rand.NextFloat(0.08f, 0.38f)),
                    Main.rand.NextFloat(0.85f, 1.3f));
                venom.noGravity = true;
            }

            int sporeAmt = Main.rand.Next(3, 7);

            if (Projectile.owner != Main.myPlayer)
                return;

            for (int s = 0; s < sporeAmt; s++)
            {
                Vector2 velocity = CalamityUtils.RandomVelocity(100f, 70f, 100f);

                int proj = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    velocity,
                    ProjectileID.SporeGas + Main.rand.Next(3),
                    (int)(Projectile.damage * 0.25),
                    0f,
                    Projectile.owner);

                if (!BFArrowCommon.InBounds(proj, Main.maxProjectiles))
                    continue;

                Main.projectile[proj].DamageType = DamageClass.Ranged;
                Main.projectile[proj].usesLocalNPCImmunity = true;
                Main.projectile[proj].usesIDStaticNPCImmunity = false;
                Main.projectile[proj].localNPCHitCooldown = 30;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Projectile.Opacity;

            Color plagueCore = new(182, 220, 82);
            Color plagueRim = new(238, 255, 166);

            // 加法层：沿速度拉长的飞行辉光 + 中心亮核，先把弹幕从背景里"点亮"
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            float stretch = MathHelper.Clamp(Projectile.velocity.Length() / 13f, 0.7f, 2.3f);
            Main.EntitySpriteDraw(
                bloomTexture,
                drawPosition,
                null,
                (plagueCore with { A = 0 }) * (0.5f * opacity),
                Projectile.velocity.ToRotation(),
                bloomTexture.Size() * 0.5f,
                new Vector2(0.3f * stretch, 0.15f) * Projectile.scale,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                bloomTexture,
                drawPosition,
                null,
                (plagueRim with { A = 0 }) * (0.4f * opacity),
                0f,
                bloomTexture.Size() * 0.5f,
                0.15f * Projectile.scale,
                SpriteEffects.None,
                0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // 包边：本体轮廓向外偏移一圈画亮色副本，主体压在上面就形成一层描边
            float rimPulse = 0.78f + 0.22f * MathF.Sin(Main.GlobalTimeWrappedHourly * 6f + Projectile.identity);
            const int rimCopies = 10;
            for (int i = 0; i < rimCopies; i++)
            {
                Vector2 rimOffset = (MathHelper.TwoPi * i / rimCopies).ToRotationVector2() * (2.2f * Projectile.scale);
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + rimOffset,
                    null,
                    plagueRim * (0.9f * opacity * rimPulse),
                    Projectile.rotation,
                    texture.Size() * 0.5f,
                    Projectile.scale,
                    SpriteEffects.None,
                    0);
            }

            CalamityUtils.DrawAfterimagesCentered(
                Projectile,
                ProjectileID.Sets.TrailingMode[Type],
                lightColor,
                1);

            BFArrowCommon.DrawCentredRotatingStar(Projectile, BlossomFluxChloroplastPresetType.Chlo_EPlague, isLeftClick: true, manageBlendState: true);

            return false;
        }
    }
}
