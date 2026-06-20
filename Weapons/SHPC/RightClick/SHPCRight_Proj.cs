using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.CtrlChip;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.HeatModule;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClick
{
    internal class SHPCRight_Proj : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/LaserProj";

        public int WeaponStage;
        public int HeatLevel;
        public int BeamIndex;
        public bool IsMainBeam = true;


        private int helixTimer;
        private bool heatExplosionRolled;
        private bool penetratedSet;
        public override Color? GetAlpha(Color lightColor)
            => new Color(255, 235, 120, 0);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Projectile.DrawBeam(200f, 3f, lightColor);
            // return false;

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame();
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color drawColor = GetAlpha(lightColor) ?? lightColor;

            //Main.EntitySpriteDraw(
            //    texture,
            //    drawPosition,
            //    frame,
            //    drawColor * Projectile.Opacity,
            //    Projectile.rotation,
            //    frame.Size() * 0.5f,
            //    Projectile.scale,
            //    SpriteEffects.None,
            //    0f);

            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] trailPoints = BuildTrailPoints();
            if (trailPoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak")
            );

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    TrailWidthFunction,
                    TrailColorFunction,
                    TrailOffsetFunction,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]
                ),
                trailPoints.Length * 2
            );

            Vector2[] coreTrail = trailPoints.Take(Math.Min(9, trailPoints.Length)).ToArray();
            if (coreTrail.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak")
            );

            PrimitiveRenderer.RenderTrail(
                coreTrail,
                new PrimitiveSettings(
                    TrailCoreWidthFunction,
                    TrailCoreColorFunction,
                    TrailOffsetFunction,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]
                ),
                coreTrail.Length * 2
            );
        }

        private Vector2[] BuildTrailPoints()
        {
            Vector2[] trailPoints = Projectile.oldPos
                .Where(pos => pos != Vector2.Zero)
                .Select(pos => pos + Projectile.Size * 0.5f)
                .ToArray();

            if (trailPoints.Length == 0)
                return new Vector2[] { Projectile.Center - Projectile.velocity, Projectile.Center };

            if (trailPoints[0] != Projectile.Center)
                trailPoints = new[] { Projectile.Center }.Concat(trailPoints).ToArray();

            return trailPoints;
        }

        private Vector2 TrailOffsetFunction(float completion, Vector2 _)
        {
            return Vector2.Zero;
        }

        private float TrailWidthFunction(float completion, Vector2 _)
        {
            float maxBodyWidth = Projectile.scale * 30f;
            float curveRatio = 0.18f;

            if (completion < curveRatio)
                return MathF.Sin(completion / curveRatio * MathHelper.PiOver2) * maxBodyWidth + curveRatio;

            return Utils.Remap(completion, curveRatio, 1f, maxBodyWidth, 0f);
        }

        private Color TrailColorFunction(float completion, Vector2 _)
        {
            Color headColor = IsMainBeam ? new Color(86, 225, 255) : new Color(255, 236, 125);
            Color midColor = IsMainBeam ? new Color(24, 154, 255) : new Color(255, 214, 72);
            float opacity = Projectile.Opacity * Utils.GetLerpValue(255f, 0f, Projectile.alpha, true);
            Color bodyColor = Color.Lerp(headColor, midColor, completion * 0.65f) * opacity;
            Color tipColor = Color.Lerp(bodyColor, Color.Transparent, Utils.GetLerpValue(0.74f, 1f, completion, true));
            tipColor.A = 0;
            return Color.Lerp(bodyColor, tipColor, completion);
        }

        private float TrailCoreWidthFunction(float completion, Vector2 _)
        {
            float maxBodyWidth = Projectile.scale * 17f;
            float curveRatio = 0.18f;

            if (completion < curveRatio)
                return MathF.Sin(completion / curveRatio * MathHelper.PiOver2) * maxBodyWidth + curveRatio;

            return Utils.Remap(completion, curveRatio, 1f, maxBodyWidth, 0f);
        }

        private Color TrailCoreColorFunction(float completion, Vector2 _)
        {
            float opacity = Projectile.Opacity * Utils.GetLerpValue(255f, 0f, Projectile.alpha, true);
            Color bodyColor = IsMainBeam
                ? Color.Lerp(Color.White, new Color(150, 245, 255), completion * 0.5f) * opacity
                : Color.Lerp(Color.White, new Color(255, 250, 210), completion * 0.5f) * opacity;
            Color tipColor = Color.Lerp(bodyColor, Color.Transparent, Utils.GetLerpValue(0.78f, 1f, completion, true));
            tipColor.A = 0;
            return Color.Lerp(bodyColor, tipColor, completion);
        }



        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 500;
            Projectile.penetrate = 2;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.alpha = 255;
        }
        private float baseSpeed;
        public override void OnSpawn(IEntitySource source)
        {
            BeamIndex = (int)Projectile.ai[0];
            IsMainBeam = BeamIndex == 0;

            // ===== 根据热值调整初速度倍率 =====
            float speedMultiplier = HeatLevel switch
            {
                1 => 0.94f,
                2 => 1f,
                3 => 1.05f,
                4 => 1.1f,
                >= 5 => 1.15f,
                _ => 0.94f
            };

            Projectile.velocity *= speedMultiplier;

            baseSpeed = Projectile.velocity.Length();

        }

        public override void AI()
        {
            TryTrackTacticalReticle();
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= 25;
            }
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
            }
            Lighting.AddLight((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16, 0.5f, 0.2f, 0.5f);
            float timerIncr = 3f;
            if (Projectile.ai[1] == 0f)
            {
                Projectile.localAI[0] += timerIncr;
                if (Projectile.localAI[0] > 100f)
                {
                    Projectile.localAI[0] = 100f;
                }
            }
            else
            {
                Projectile.localAI[0] -= timerIncr;
                if (Projectile.localAI[0] <= 0f)
                {
                    Projectile.Kill();
                    return;
                }
            }
            Lighting.AddLight(Projectile.Center, new Color(255, 220, 120).ToVector3() * 0.6f);

            // 穿透设置
            if (!penetratedSet)
            {
                int heat = Math.Max(WeaponStage, HeatLevel);
                Projectile.penetrate = heat switch
                {
                    >= 5 => -1,
                    >= 2 => 3,
                    _ => 1
                };
                penetratedSet = true;
            }

            // 飞行特效：Stage3+ 才启用，且每真实帧只释放一次，避免过重
            if ((HeatLevel >= 3 || BeamIndex > 0) && Projectile.numUpdates == 0)
            {
                helixTimer++;
                SpawnHelixFlightEffects();
            }
        }

        private void TryTrackTacticalReticle()
        {
            // 只在真实帧执行，避免 extraUpdates 重复转向
            if (Projectile.numUpdates != 0)
                return;

            if (Projectile.owner != Main.myPlayer)
                return;

            Player owner = Main.player[Projectile.owner];
            CtrlChipPlayer tacticalPlayer = owner.GetModPlayer<CtrlChipPlayer>();
            if (!tacticalPlayer.CtrlChipEquipped)
                return;

            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float speed = Projectile.velocity.Length();
            float currentAngle = currentDirection.ToRotation();

            // ===== 第一优先级：追踪准星弹幕 =====
            // 直接搜索 CtrlChipNEWReticle 弹幕实体，而不仅仅使用 ReticleWorld
            int reticleType = ModContent.ProjectileType<CtrlChipNEWReticle>();
            Projectile reticleProj = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Projectile.owner && p.type == reticleType)
                {
                    reticleProj = p;
                    break;
                }
            }

            float maxReticleTurn = MathHelper.ToRadians(2f); // 准星追踪：每帧最多转2度
            float maxEnemyTurn = MathHelper.ToRadians(1f);   // 敌人追踪：每帧最多转1度

            if (reticleProj != null)
            {
                // 直接追踪准星弹幕的实际位置
                Vector2 toReticle = (reticleProj.Center - Projectile.Center).SafeNormalize(currentDirection);
                // 只追踪正前方180度内的准星
                if (Vector2.Dot(currentDirection, toReticle) > 0f)
                {
                    float desiredAngle = toReticle.ToRotation();
                    currentAngle = currentAngle.AngleTowards(desiredAngle, maxReticleTurn);
                }
            }

            // ===== 第二优先级：搜索正前方180度范围内最近的敌人 =====
            // 用更新后的方向重新计算正前方
            Vector2 updatedDirection = currentAngle.ToRotationVector2();
            NPC closestEnemy = null;
            float closestDist = float.MaxValue;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                Vector2 toEnemy = (npc.Center - Projectile.Center).SafeNormalize(updatedDirection);
                // 只搜索正前方180度（点积 > 0）
                if (Vector2.Dot(updatedDirection, toEnemy) <= 0f)
                    continue;

                float dist = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestEnemy = npc;
                }
            }

            if (closestEnemy != null)
            {
                Vector2 toTarget = (closestEnemy.Center - Projectile.Center).SafeNormalize(updatedDirection);
                float desiredAngle = toTarget.ToRotation();
                // 敌人追踪弱一些：每帧最多再转1度
                float remainingTurn = maxEnemyTurn;
                currentAngle = currentAngle.AngleTowards(desiredAngle, remainingTurn);
            }

            Projectile.velocity = currentAngle.ToRotationVector2() * speed;

            if ((int)Main.GameUpdateCount % 4 == 0)
                Projectile.netUpdate = true;
        }

        private void SpawnHelixFlightEffects()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 previousCenter = Projectile.oldPosition + Projectile.Size * 0.5f;
            if (previousCenter == Projectile.Size * 0.5f)
                previousCenter = Projectile.Center - Projectile.velocity;

            if (IsMainBeam)
            {
                //for (int i = 0; i < 2; i++)
                //{
                //    float completion = (i + 1f) / 2f;
                //    Vector2 spawnPos = Vector2.Lerp(previousCenter, Projectile.Center, completion);
                //    Dust core = Dust.NewDustPerfect(spawnPos, 267);
                //    core.velocity = forward * Main.rand.NextFloat(0.45f, 1.05f) + Main.rand.NextVector2Circular(0.18f, 0.18f);
                //    core.color = Color.Lerp(new Color(70, 210, 255), Color.White, Main.rand.NextFloat(0.35f, 0.8f));
                //    core.scale = Main.rand.NextFloat(0.42f, 0.62f);
                //    core.noGravity = true;
                //}

                return;
            }

            Color brightYellow = new Color(255, 235, 120);
            Color hotYellow = Color.Lerp(brightYellow, Color.White, 0.35f);
            float[] phases =
            {
                0f,
                MathHelper.Pi,
                MathHelper.PiOver2,
                MathHelper.PiOver2 + MathHelper.Pi
            };

            const int substeps = 4;
            for (int step = 0; step < substeps; step++)
            {
                float completion = (step + 1f) / substeps;
                Vector2 center = Vector2.Lerp(previousCenter, Projectile.Center, completion);
                float time = (helixTimer - 1f + completion) * 0.46f;

                for (int i = 0; i < phases.Length; i++)
                {
                    if (((i + step + BeamIndex) & 1) != 0)
                        continue;

                    bool thickStrand = i < 2;
                    float radius = thickStrand ? 7.5f : 4.8f;
                    float wave = (float)Math.Sin(time + phases[i] + BeamIndex * 0.7f);
                    Vector2 spawnPos = center + right * wave * radius;

                    //Dust dust = Dust.NewDustPerfect(spawnPos, 267);
                    //dust.velocity =
                    //    forward * Main.rand.NextFloat(0.35f, 0.9f) +
                    //    right * wave * Main.rand.NextFloat(0.08f, 0.22f);
                    //dust.color = Color.Lerp(brightYellow, hotYellow, Main.rand.NextFloat(0.2f, 0.75f));
                    //dust.scale = thickStrand
                    //    ? Main.rand.NextFloat(0.34f, 0.52f)
                    //    : Main.rand.NextFloat(0.22f, 0.34f);
                    //dust.noGravity = true;
                }
            }
        }

        // ===== 核心：伤害倍率 + 防御处理 =====
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            int heat = Math.Max(WeaponStage, HeatLevel);
            Player owner = Main.player[Projectile.owner];
            modifiers.SourceDamage *= GetLateHeatDamageMultiplier(heat);
            modifiers.SourceDamage *= owner.GetModPlayer<HeatModulePlayer>().GetHeatDamageMultiplier(heat);
        }

        private static float GetLateHeatDamageMultiplier(int heat)
        {
            int cappedHeat = Math.Min(5, Math.Max(0, heat));
            return MathF.Pow(1.05f, cappedHeat);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int heat = Math.Max(WeaponStage, HeatLevel);
            Vector2 impactCenter = Projectile.Center;
            TrySpawnHeatExplosion(heat, impactCenter, target.whoAmI);

            if (heat >= 2 && heat < 5)
                Projectile.damage = Math.Max(1, (int)(Projectile.damage * 0.9f));

            SpawnHitEffects(impactCenter);
        }

        private void TrySpawnHeatExplosion(int heat, Vector2 impactCenter, int targetID)
        {
            if (heat < 3)
                return;

            if (heat == 3 && heatExplosionRolled)
                return;

            if (heat == 3)
                heatExplosionRolled = true;

            float chance = IsMainBeam
                ? (heat >= 4 ? 0.2f : 0.1f)
                : (heat >= 4 ? 0.5f : 0.33f);

            if (Main.rand.NextFloat() >= chance)
                return;

            int explosionSize = IsMainBeam
                ? (heat >= 4 ? 150 : 128)
                : (heat >= 4 ? 104 : 84);
            int explosionDamage = Math.Max(1, (int)(Projectile.damage * (IsMainBeam ? 0.65f : 0.45f)));

            int explosionIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                impactCenter,
                Vector2.Zero,
                ModContent.ProjectileType<SHPCRight_Explosion>(),
                explosionDamage,
                Projectile.knockBack,
                Projectile.owner,
                targetID,
                explosionSize
            );

            if (Main.projectile.IndexInRange(explosionIndex))
                Main.projectile[explosionIndex].CritChance = Projectile.CritChance;
        }

        public override void OnKill(int timeLeft)
        {
            SpawnDeathEffects();
        }

        #region 特效[已废弃]

        //private void SpawnFlightEffects()
        //{
        //    int roll = Main.rand.Next(3);

        //    if (roll == 0)
        //    {
        //        // 电能Dust
        //        int lightDustCount = Main.rand.Next(1, 3);
        //        for (int i = 0; i < lightDustCount; i++)
        //        {
        //            Vector2 dustSpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * (1f - Projectile.Opacity) * 18f;
        //            Dust light = Dust.NewDustPerfect(dustSpawnPosition, 267);
        //            light.color = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.5f, 1f));
        //            light.velocity = Main.rand.NextVector2Circular(4f, 4f);
        //            light.scale = Main.rand.NextFloat(0.75f, 1.05f);
        //            light.noGravity = true;
        //        }
        //    }
        //    else if (roll == 1)
        //    {
        //        // 辉光�?
        //        Vector2 dir = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
        //        Vector2 position = Projectile.Center + dir * Main.rand.NextFloat(4f, 10f);
        //        Vector2 velocity = dir.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.5f, 2.2f);

        //        Color glowColor = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.25f, 0.85f));
        //        GlowOrbParticle glow = new GlowOrbParticle(
        //            position,
        //            velocity,
        //            false,
        //            18,
        //            Main.rand.NextFloat(0.6f, 0.9f),
        //            glowColor,
        //            true,
        //            true
        //        );
        //        GeneralParticleHandler.SpawnParticle(glow);
        //    }
        //    else
        //    {
        //        // 光芒粒子
        //        Vector2 velocity = Main.rand.NextVector2Circular(1.2f, 1.2f);
        //        float scale = Main.rand.NextFloat(0.2f, 0.4f);
        //        Color particleColor = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.2f, 0.7f));
        //        int lifetime = Main.rand.Next(10, 16);

        //        SquishyLightParticle particle = new(
        //            Projectile.Center,
        //            velocity,
        //            scale,
        //            particleColor,
        //            lifetime
        //        );

        //        GeneralParticleHandler.SpawnParticle(particle);
        //    }
        //}

        private void SpawnHitEffects(Vector2 impactCenter)
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -forward;
            float baseAngle = back.ToRotation();

            // 扇形Dust
            int dustCount = 13;
            float fanHalfAngle = MathHelper.ToRadians(54f);
            for (int i = 0; i < dustCount; i++)
            {
                float angle = baseAngle + Main.rand.NextFloat(-fanHalfAngle, fanHalfAngle) + Main.rand.NextFloat(-0.18f, 0.18f);
                float speed = Main.rand.NextFloat(1.8f, 7.6f);

                Dust light = Dust.NewDustPerfect(impactCenter + Main.rand.NextVector2Circular(8f, 8f), DustID.RainbowMk2);
                light.color = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.4f, 0.95f));
                light.velocity = angle.ToRotationVector2() * speed + Main.rand.NextVector2Circular(1.2f, 1.2f);
                light.scale = Main.rand.NextFloat(0.75f, 1.35f);
                light.noGravity = true;
            }

            // 扇形辉光�?
            int orbCount = 5;
            for (int i = 0; i < orbCount; i++)
            {
                float angle = baseAngle + Main.rand.NextFloat(-fanHalfAngle, fanHalfAngle);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(1.6f, 3.8f);
                Vector2 position = impactCenter + Main.rand.NextVector2Circular(7f, 7f);

                Color glowColor = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.3f, 0.85f));
                GlowOrbParticle glow = new GlowOrbParticle(
                    position,
                    velocity,
                    false,
                    18,
                    Main.rand.NextFloat(0.6f, 0.9f),
                    glowColor,
                    true,
                    true
                );
                GeneralParticleHandler.SpawnParticle(glow);
            }

            // 命中光粒�?
            int lightCount = 4;
            for (int i = 0; i < lightCount; i++)
            {
                Vector2 velocity = back.RotatedByRandom(0.95f) * Main.rand.NextFloat(0.6f, 2.8f) + Main.rand.NextVector2Circular(0.8f, 0.8f);
                float scale = Main.rand.NextFloat(0.28f, 0.48f);
                Color particleColor = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.2f, 0.8f));
                int lifetime = Main.rand.Next(12, 18);

                SquishyLightParticle particle = new(
                    impactCenter + Main.rand.NextVector2Circular(5f, 5f),
                    velocity,
                    scale,
                    particleColor,
                    lifetime
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }
        }

        private void SpawnDeathEffects()
        {
            Vector2 outwardBase = Main.rand.NextVector2Unit();

            // 扩散Dust
            int lightDustCount = 10;
            for (int i = 0; i < lightDustCount; i++)
            {
                Vector2 dustSpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(0f, 10f);
                Dust light = Dust.NewDustPerfect(dustSpawnPosition, DustID.RainbowMk2);
                light.color = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.5f, 1f));
                light.velocity = Main.rand.NextVector2Circular(6f, 6f);
                light.scale = Main.rand.NextFloat(0.9f, 1.35f);
                light.noGravity = true;
            }

            // 扩散光芒粒子
            int lightCount = 4;
            for (int i = 0; i < lightCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.2f, 4.2f);
                float scale = Main.rand.NextFloat(0.25f, 0.55f);
                Color particleColor = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.25f, 0.85f));
                int lifetime = Main.rand.Next(14, 24);

                SquishyLightParticle particle = new(
                    Projectile.Center,
                    velocity,
                    scale,
                    particleColor,
                    lifetime
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            // 扩散辉光�?
            int orbCount = 3;
            for (int i = 0; i < orbCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.4f, 3.5f);
                Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(3f, 3f);
                Color glowColor = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.2f, 0.8f));

                GlowOrbParticle glow = new GlowOrbParticle(
                    position,
                    velocity,
                    false,
                    18,
                    Main.rand.NextFloat(0.6f, 0.9f),
                    glowColor,
                    true,
                    true
                );
                GeneralParticleHandler.SpawnParticle(glow);
            }

            //// 小冲击波
            //Particle pulse = new DirectionalPulseRing(
            //    Projectile.Center,
            //    Projectile.velocity * 0.75f,
            //    Color.Gold,
            //    new Vector2(1f, 2.5f),
            //    Projectile.rotation - MathHelper.PiOver4,
            //    0.2f,
            //    0.03f,
            //    20
            //);
            //GeneralParticleHandler.SpawnParticle(pulse);
        }

        #endregion[已废弃]

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(WeaponStage);
            writer.Write(HeatLevel);
            writer.Write(BeamIndex);
            writer.Write(IsMainBeam);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            WeaponStage = reader.ReadInt32();
            HeatLevel = reader.ReadInt32();
            BeamIndex = reader.ReadInt32();
            IsMainBeam = reader.ReadBoolean();
        }


    }
}
