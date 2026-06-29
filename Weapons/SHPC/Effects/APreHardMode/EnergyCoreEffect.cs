using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode;
using CalamityMod.Items.Materials;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Graphics.Effects;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects
{
    public class EnergyCoreEffect : DefaultEffect
    {
        private const int ExplosionFlightTime = 10 * 60;
        private const int MaxBouncesBeforeTileExplosion = 5;

        public override int EffectID => 1;

        public override int AmmoType => ModContent.ItemType<EnergyCore>();

        public override Color ThemeColor => new Color(120, 255, 120); // 稍微偏深的Lime
        public override Color StartColor => new Color(140, 255, 140); // 稍亮一点作为起点
        public override Color EndColor => new Color(40, 180, 40);     // 深绿色收尾

        public override void SetDefaults(Projectile projectile)
        {
            projectile.penetrate = 1;
            projectile.timeLeft = ExplosionFlightTime + 120;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 20;
        }

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            SetDefaults(projectile);
            projectile.localAI[0] = 0f; // 弹射次数
            projectile.localAI[1] = 0f; // 是否已准备好在下一次碰撞中爆炸
            projectile.localAI[2] = 0f; // 飞行时间（帧数）
        }

        public override bool EnableProximityExplosion => false;

        public override void AI(Projectile projectile, Player owner)
        {
            // 累计飞行时间
            projectile.localAI[2] += 1f;

            int timeFlown = (int)projectile.localAI[2];

            // Flight timeout is one of the three explicit explosion triggers.
            if (timeFlown >= ExplosionFlightTime)
            {
                projectile.localAI[1] = 1f;
                projectile.Kill();
                return;
            }

            // ===== 模拟重力 =====

            float gravity = 0.18f;      // 重力强度（你可以调，0.1~0.3之间比较自然）
            float maxFallSpeed = 18f;   // 最大下落速度（防止无限加速）

            // 逐帧向下加速
            projectile.velocity.Y += gravity;

            // 限制最大下落速度
            if (projectile.velocity.Y > maxFallSpeed)
                projectile.velocity.Y = maxFallSpeed;

            // ===== 可选：根据速度调整朝向（更像箭）=====
            projectile.rotation = projectile.velocity.ToRotation();


            projectile.velocity *= 1.020408f;  // 抵消默认减速

            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 1.8f);

            // ===== 碰撞箱变长翻倍 =====
            // 在基类设置24x24碰撞箱之后，将宽度翻倍为48，使碰撞箱沿飞行方向更长
            Vector2 oldCenter = projectile.Center;
            projectile.width = 48;
            projectile.Center = oldCenter;
        }
        public override bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity)
        {
            int bounceCount = (int)projectile.localAI[0];

            if (bounceCount >= MaxBouncesBeforeTileExplosion)
            {
                projectile.localAI[1] = 1f;
                return true;
            }

            projectile.localAI[0] += 1f;

            // 反弹（入射=反射）
            if (projectile.velocity.X != oldVelocity.X)
                projectile.velocity.X = -oldVelocity.X;

            if (projectile.velocity.Y != oldVelocity.Y)
                projectile.velocity.Y = -oldVelocity.Y;

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item110, projectile.Center);


            return false; // 不销毁
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            projectile.localAI[1] = 1f;
            projectile.Kill();
        }

		// 死亡时炸出6个电火花（仅在碰撞触发的死亡时爆炸，自然到期不炸）
		public override void OnKill(Projectile projectile, Player owner, int timeLeft)
		{
            if (projectile.localAI[1] <= 0f)
                return;

            PlayEnergyCoreExplosionSound(projectile.Center);

			int count = 7;

			// 随机整体旋转（避免每次都一样）
			float baseRotation = Main.rand.NextFloat(MathHelper.TwoPi);

			for (int i = 0; i < count; i++)
			{
				// 基础均分角度 + 随机扰动（±15°）
				float baseAngle = MathHelper.TwoPi / count * i;
				float randomOffset = Main.rand.NextFloat(-MathHelper.ToRadians(15f), MathHelper.ToRadians(15f));
				float angle = baseRotation + baseAngle + randomOffset;

				// 速度随机（4~8）
				float speed = Main.rand.NextFloat(4f, 8f);
				Vector2 velocity = angle.ToRotationVector2() * speed;

				// 伤害随机（0.X~0.Y倍）
				float damageFactor = Main.rand.NextFloat(0.25f, 0.4f);

				Projectile.NewProjectile(
					projectile.GetSource_FromThis(),
					projectile.Center,
					velocity,
					ModContent.ProjectileType<EnergyCore_Spark>(),
					(int)(projectile.damage * damageFactor),
					projectile.knockBack,
					projectile.owner
				);
			}

            int projIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                (int)(projectile.damage * 0.8f),
                projectile.knockBack,
                projectile.owner
            );

            if (projIndex >= 0 && projIndex < Main.maxProjectiles)
            {
                Projectile proj = Main.projectile[projIndex];
                proj.width = 100;
                proj.height = 100;
                proj.Center = projectile.Center;
                proj.netUpdate = true;
            }
        }

        private static void PlayEnergyCoreExplosionSound(Vector2 position)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserChargeImpact")
            {
                Volume = 0.54f,
                Pitch = -0.08f,
                PitchVariance = 0.05f,
                MaxInstances = 4
            }, position);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/PlasmaGrenadeExplosion")
            {
                Volume = 0.42f,
                Pitch = -0.18f,
                PitchVariance = 0.04f,
                MaxInstances = 4
            }, position);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/HeliumFlashCoreImpact")
            {
                Volume = 0.38f,
                Pitch = 0.06f,
                PitchVariance = 0.04f,
                MaxInstances = 4
            }, position);
        }

        public override void PreDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch)
        {
            // ===== 特效圆缩小至外接碰撞箱正方形（以正方形对角线为直径） =====
            // 碰撞箱的外接正方形边长 = max(width, height)，对角线 = 边长 * sqrt(2)
            float hitboxSquareSide = Math.Max(projectile.width, projectile.height);
            float hitboxDiagonal = hitboxSquareSide * MathF.Sqrt(2f);
            // FrozenCrust 贴图约 256px，护盾直径 = 贴图尺寸 * scale
            Texture2D frozenCrust = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/FrozenCrust").Value;
            float texDiameter = Math.Max(frozenCrust.Width, frozenCrust.Height);
            float baseScale = hitboxDiagonal / texDiameter;
            // 加上轻微呼吸
            float breathe = 1f + 0.04f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.5f + projectile.whoAmI * 0.2f);
            float scale = baseScale * breathe;

            // ===== 护盾强度，这里固定视为满强度 =====
            float shieldStrength = 1f;

            // ===== 噪声缩放，同样不同步呼吸 =====
            float noiseScale = MathHelper.Lerp(0.4f, 0.8f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.3f) * 0.5f + 0.5f);

            // ===== 取 Rover Drive 同款 Shader =====
            Effect shieldEffect = Filters.Scene["CalamityMod:RoverDriveShield"].GetShader().Shader;
            shieldEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.24f);
            shieldEffect.Parameters["blowUpPower"].SetValue(2.5f);
            shieldEffect.Parameters["blowUpSize"].SetValue(0.5f);
            shieldEffect.Parameters["noiseScale"].SetValue(noiseScale);

            // ===== 透明度逻辑：与 Rover 一致 =====
            float baseShieldOpacity = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f);
            float finalShieldOpacity = baseShieldOpacity * (0.5f + 0.5f * shieldStrength);
            shieldEffect.Parameters["shieldOpacity"].SetValue(finalShieldOpacity);
            shieldEffect.Parameters["shieldEdgeBlendStrenght"].SetValue(4f);

            // ===== 颜色参数 =====
            Color blueTint = new Color(51, 102, 255);
            Color cyanTint = new Color(71, 202, 255);
            Color wulfGreen = new Color(194, 255, 67) * 0.8f;
            Color edgeColor = Color.Lerp(
                Color.Lerp(blueTint, cyanTint, 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.2f)),
                wulfGreen,
                0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.2f + 1.2f)
            );

            Color shieldColor = blueTint;

            shieldEffect.Parameters["shieldColor"].SetValue(shieldColor.ToVector3());
            shieldEffect.Parameters["shieldEdgeColor"].SetValue(edgeColor.ToVector3());

            // ===== 你的底图 =====
            //Texture2D tex = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/window_04").Value;
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/FrozenCrust").Value;

            // ===== 围绕弹幕中心，而不是围绕玩家 =====
            float orbitRadius = 3f; // 公转半径（自己调）
            float orbitSpeed = 1.2f;

            Vector2 orbitOffset = (Main.GlobalTimeWrappedHourly * orbitSpeed).ToRotationVector2() * orbitRadius;

            Vector2 pos = projectile.Center + orbitOffset - Main.screenPosition;

            // ===== 额外旋转底图：如果你想让底噪声缓慢旋转，这里直接加进去 =====
            float texRotation = Main.GlobalTimeWrappedHourly * 0.75f;

            // ===== 为了套 Shader，临时切一次 Batch，再切回来 =====
            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                shieldEffect,
                Main.Transform
            );

            // ===== 关键：仍然是画一张方形噪声图，但由 Shader 把它处理成护盾投影感 =====
            spriteBatch.Draw(
                tex,
                pos,
                null,
                Color.White,
                texRotation,
                tex.Size() / 2f,
                scale,
                SpriteEffects.None,
                0f
            );

            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.Transform
            );
        }

    }
}
