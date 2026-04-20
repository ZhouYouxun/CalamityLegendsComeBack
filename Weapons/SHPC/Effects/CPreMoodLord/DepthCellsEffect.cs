using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects
{
    public class DepthCellsEffect : DefaultEffect
    {
        private const int AbyssDustA = 104;
        private const int AbyssDustB = 29;
        private const int AbyssDustC = 191;

        private static readonly Color AbyssDark = new(8, 12, 24);
        private static readonly Color AbyssBlue = new(26, 62, 118);
        private static readonly Color AbyssGlow = new(82, 152, 220);
        private static readonly Color AbyssFoam = new(170, 235, 255);

        public override int EffectID => 17;
        public override int AmmoType => ModContent.ItemType<DepthCells>();

        // ===== 深渊暗色主题 =====
        public override Color ThemeColor => new Color(22, 42, 78);
        public override Color StartColor => new Color(84, 148, 210);
        public override Color EndColor => new Color(8, 12, 24);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0.55f;
        public override float GlowIntensityFactor => 0.42f;

        // ===== 每个弹幕各自的状态表 =====
        private readonly Dictionary<int, bool> stuckState = new();
        private readonly Dictionary<int, int> stuckTargetIndex = new();
        private readonly Dictionary<int, int> hitCountOnCurrentTarget = new();
        private readonly Dictionary<int, int> bounceCount = new();
        private readonly Dictionary<int, int> stickVisualTimer = new();
        private readonly Dictionary<int, float> orbitAngle = new();

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            int id = projectile.whoAmI;

            // 为了支持持续多段伤害和连续弹跳，这里直接给无限穿透
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 10;

            stuckState[id] = false;
            stuckTargetIndex[id] = -1;
            hitCountOnCurrentTarget[id] = 0;
            bounceCount[id] = 0;
            stickVisualTimer[id] = 0;
            orbitAngle[id] = Main.rand.NextFloat(MathHelper.TwoPi);

            projectile.rotation = projectile.velocity.ToRotation();
            SpawnSpawnAbyssEffects(projectile);
        }

        public override void AI(Projectile projectile, Player owner)
        {
            int id = projectile.whoAmI;

            EnsureStateExists(id);

            // ===== 常规飞行时：暗色深渊尾迹 =====
            if (!stuckState[id])
            {
                projectile.rotation = projectile.velocity.ToRotation();
                SpawnFlightAbyssEffects(projectile);

                // 微弱深渊幽光
                Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.22f);
                return;
            }

            // ===== 粘附状态：锁在敌人身上 =====
            int targetIndex = stuckTargetIndex[id];
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
            {
                ReleaseAndDashToNext(projectile, owner, -1);
                return;
            }

            NPC target = Main.npc[targetIndex];
            if (!target.active || !target.CanBeChasedBy(projectile))
            {
                ReleaseAndDashToNext(projectile, owner, targetIndex);
                return;
            }

            stickVisualTimer[id]++;
            orbitAngle[id] += 0.2f;

            Vector2 stickDir = (projectile.Center - target.Center).SafeNormalize(Vector2.UnitY);
            Vector2 orbitOffset = stickDir * 18f;
            
            projectile.Center = target.Center + orbitOffset;
            projectile.velocity = Vector2.Zero;
            projectile.rotation += 0.3f;

            // 粘附期间持续释放深渊特效
            SpawnStuckAbyssEffects(projectile, target);

            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.15f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            int id = projectile.whoAmI;

            EnsureStateExists(id);

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            bool firstLatch = !stuckState[id] || stuckTargetIndex[id] != target.whoAmI;
            SpawnHitAbyssEffects(projectile, target, forward, firstLatch ? 1f : 0.72f);

            // ================= Debuff =================

            target.AddBuff(ModContent.BuffType<CrushDepth>(), 320);
            target.AddBuff(ModContent.BuffType<Eutrophication>(), 320);

            // ================= 原逻辑（必须保留） =================

            if (!stuckState[id] || stuckTargetIndex[id] != target.whoAmI)
            {
                stuckState[id] = true;
                stuckTargetIndex[id] = target.whoAmI;
                hitCountOnCurrentTarget[id] = 1;
                stickVisualTimer[id] = 0;
                orbitAngle[id] = Main.rand.NextFloat(MathHelper.TwoPi);
                projectile.velocity = Vector2.Zero;
                projectile.netUpdate = true;
                return;
            }

            hitCountOnCurrentTarget[id]++;

            if (hitCountOnCurrentTarget[id] >= 4)
            {
                int currentTarget = target.whoAmI;

                if (bounceCount[id] < 3)
                {
                    bounceCount[id]++;
                    hitCountOnCurrentTarget[id] = 0;
                    ReleaseAndDashToNext(projectile, owner, currentTarget);
                }
                else
                {
                    projectile.Kill();
                }
            }
        }
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            int id = projectile.whoAmI;

            SpawnDeathAbyssEffects(projectile);

            ClearState(id);
        }

        public override void PreDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch)
        {
            int id = projectile.whoAmI;
            EnsureStateExists(id);

            Texture2D circularSmear = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmearSmokey").Value;
            Texture2D swipeSmear = ModContent.Request<Texture2D>("CalamityMod/Particles/SemiCircularSmearSwipe").Value;
            Vector2 drawPos = projectile.Center - Main.screenPosition;

            spriteBatch.SetBlendState(BlendState.Additive);

            if (!stuckState[id] && projectile.velocity.LengthSquared() > 4f)
            {
                float drawRotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
                Vector2 backOffset = -projectile.velocity * 0.24f;

                Main.EntitySpriteDraw(
                    swipeSmear,
                    drawPos + backOffset,
                    null,
                    new Color(AbyssBlue.R, AbyssBlue.G, AbyssBlue.B, 0) * 0.58f,
                    drawRotation,
                    swipeSmear.Size() * 0.5f,
                    new Vector2(0.92f, 1.15f) * projectile.scale,
                    SpriteEffects.None
                );

                Main.EntitySpriteDraw(
                    circularSmear,
                    drawPos,
                    null,
                    new Color(AbyssGlow.R, AbyssGlow.G, AbyssGlow.B, 0) * 0.36f,
                    -drawRotation * 0.45f,
                    circularSmear.Size() * 0.5f,
                    projectile.scale * 0.56f,
                    SpriteEffects.None
                );
            }
            else
            {
                float swirlRotation = orbitAngle[id];

                Main.EntitySpriteDraw(
                    circularSmear,
                    drawPos,
                    null,
                    new Color(AbyssBlue.R, AbyssBlue.G, AbyssBlue.B, 0) * 0.52f,
                    -swirlRotation * 0.65f,
                    circularSmear.Size() * 0.5f,
                    projectile.scale * 0.62f,
                    SpriteEffects.None
                );

                Main.EntitySpriteDraw(
                    swipeSmear,
                    drawPos,
                    null,
                    new Color(AbyssFoam.R, AbyssFoam.G, AbyssFoam.B, 0) * 0.24f,
                    swirlRotation + MathHelper.PiOver2,
                    swipeSmear.Size() * 0.5f,
                    new Vector2(0.6f, 0.95f) * projectile.scale,
                    SpriteEffects.None
                );
            }

            spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        public override void PostDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch)
        {
            int id = projectile.whoAmI;
            EnsureStateExists(id);

            Texture2D circularSmear = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmearSmokey").Value;
            Texture2D swipeSmear = ModContent.Request<Texture2D>("CalamityMod/Particles/SemiCircularSmearSwipe").Value;
            Vector2 drawPos = projectile.Center - Main.screenPosition;

            spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                circularSmear,
                drawPos,
                null,
                new Color(AbyssFoam.R, AbyssFoam.G, AbyssFoam.B, 0) * (stuckState[id] ? 0.18f : 0.14f),
                orbitAngle[id] * 0.35f,
                circularSmear.Size() * 0.5f,
                projectile.scale * (stuckState[id] ? 0.42f : 0.35f),
                SpriteEffects.None
            );

            if (stuckState[id])
            {
                Main.EntitySpriteDraw(
                    swipeSmear,
                    drawPos,
                    null,
                    new Color(AbyssGlow.R, AbyssGlow.G, AbyssGlow.B, 0) * 0.18f,
                    orbitAngle[id] - MathHelper.PiOver2,
                    swipeSmear.Size() * 0.5f,
                    new Vector2(0.48f, 0.72f) * projectile.scale,
                    SpriteEffects.None
                );
            }

            spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        // ========================= 工具区 =========================

        private void SpawnSpawnAbyssEffects(Projectile projectile)
        {
            DirectionalPulseRing spawnRing = new(
                projectile.Center,
                Vector2.Zero,
                Color.White * 0.24f,
                Vector2.One,
                0f,
                0.01f,
                0.08f,
                16
            );
            GeneralParticleHandler.SpawnParticle(spawnRing);

            for (int i = 0; i < 3; i++)
            {
                WaterGlobParticle glob = new(
                    projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2Circular(1.2f, 1.2f),
                    Main.rand.NextFloat(0.5f, 0.8f)
                );
                glob.Color = Color.Lerp(AbyssBlue, AbyssGlow, Main.rand.NextFloat()) * 0.3f;
                GeneralParticleHandler.SpawnParticle(glob);
            }
        }

        private void SpawnFlightAbyssEffects(Projectile projectile)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -forward;
            Vector2 drawCenter = projectile.Center - forward * Main.rand.NextFloat(2f, 8f);

            if (Main.rand.NextBool(2))
            {
                HeavySmokeParticle smoke = new(
                    drawCenter + Main.rand.NextVector2Circular(6f, 6f),
                    back * Main.rand.NextFloat(0.6f, 1.6f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    Color.Lerp(AbyssDark, AbyssBlue, Main.rand.NextFloat(0.35f, 0.9f)),
                    Main.rand.Next(18, 30),
                    Main.rand.NextFloat(0.38f, 0.6f),
                    0.55f
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (Main.rand.NextBool(3))
            {
                WaterFlavoredParticle shard = new(
                    drawCenter + Main.rand.NextVector2Circular(4f, 4f),
                    back * Main.rand.NextFloat(0.2f, 1.1f) + Main.rand.NextVector2Circular(1.3f, 1.3f),
                    false,
                    Main.rand.Next(12, 22),
                    Main.rand.NextFloat(0.7f, 1f),
                    Color.Lerp(AbyssBlue, AbyssFoam, Main.rand.NextFloat(0.15f, 0.7f))
                );
                GeneralParticleHandler.SpawnParticle(shard);
            }

            if (Main.rand.NextBool(4))
            {
                WaterGlobParticle glob = new(
                    drawCenter + Main.rand.NextVector2Circular(5f, 5f),
                    back * Main.rand.NextFloat(0.2f, 0.8f) + Main.rand.NextVector2Circular(0.3f, 0.3f),
                    Main.rand.NextFloat(0.55f, 0.85f)
                );
                glob.Color = Color.Lerp(AbyssBlue, AbyssGlow, Main.rand.NextFloat()) * 0.28f;
                GeneralParticleHandler.SpawnParticle(glob);
            }

            if (Main.rand.NextBool(4))
            {
                AltSparkParticle spark = new(
                    projectile.Center - forward * Main.rand.NextFloat(4f, 10f),
                    back * Main.rand.NextFloat(0.1f, 0.35f) + Main.rand.NextVector2Circular(0.35f, 0.35f),
                    false,
                    10,
                    Main.rand.NextFloat(0.8f, 1.15f),
                    Color.Lerp(AbyssGlow, AbyssFoam, Main.rand.NextFloat(0.15f, 0.55f)) * 0.22f
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Main.rand.NextBool(2))
            {
                int dustType = Main.rand.NextBool(4) ? AbyssDustC : (Main.rand.NextBool() ? AbyssDustA : AbyssDustB);
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center - forward * Main.rand.NextFloat(5f, 11f),
                    dustType,
                    back * Main.rand.NextFloat(0.4f, 1.4f) + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    120,
                    Color.Lerp(AbyssDark, AbyssGlow, Main.rand.NextFloat(0.35f, 0.9f)),
                    Main.rand.NextFloat(1f, 1.45f)
                );
                dust.noGravity = true;
            }
        }

        private void EnsureStateExists(int id)
        {
            if (!stuckState.ContainsKey(id))
                stuckState[id] = false;
            if (!stuckTargetIndex.ContainsKey(id))
                stuckTargetIndex[id] = -1;
            if (!hitCountOnCurrentTarget.ContainsKey(id))
                hitCountOnCurrentTarget[id] = 0;
            if (!bounceCount.ContainsKey(id))
                bounceCount[id] = 0;
            if (!stickVisualTimer.ContainsKey(id))
                stickVisualTimer[id] = 0;
            if (!orbitAngle.ContainsKey(id))
                orbitAngle[id] = 0f;
        }

        private void ClearState(int id)
        {
            stuckState.Remove(id);
            stuckTargetIndex.Remove(id);
            hitCountOnCurrentTarget.Remove(id);
            bounceCount.Remove(id);
            stickVisualTimer.Remove(id);
            orbitAngle.Remove(id);
        }

        private void ReleaseAndDashToNext(Projectile projectile, Player owner, int excludeTargetWhoAmI)
        {
            int id = projectile.whoAmI;

            NPC nextTarget = FindNextTarget(projectile, excludeTargetWhoAmI);

            // 先脱离当前粘附状态
            stuckState[id] = false;
            stuckTargetIndex[id] = -1;
            stickVisualTimer[id] = 0;

            if (nextTarget == null)
            {
                projectile.Kill();
                return;
            }

            SpawnReleaseDashEffects(projectile, nextTarget.Center);

            // 快速冲向下一个目标
            Vector2 dashDirection = (nextTarget.Center - projectile.Center).SafeNormalize(Vector2.UnitX);
            projectile.velocity = dashDirection * 22f;
            projectile.rotation = projectile.velocity.ToRotation();
            projectile.netUpdate = true;
        }

        private NPC FindNextTarget(Projectile projectile, int excludeTargetWhoAmI)
        {
            NPC result = null;
            float maxDistance = 900f;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.CanBeChasedBy(projectile))
                    continue;

                if (npc.whoAmI == excludeTargetWhoAmI)
                    continue;

                float distance = Vector2.Distance(projectile.Center, npc.Center);
                if (distance < maxDistance)
                {
                    maxDistance = distance;
                    result = npc;
                }
            }

            return result;
        }

        private void SpawnHitAbyssEffects(Projectile projectile, NPC target, Vector2 forward, float intensity)
        {
            DirectionalPulseRing ring = new(
                target.Center,
                Vector2.Zero,
                Color.White * (0.18f + 0.08f * intensity),
                Vector2.One,
                0f,
                0.018f * intensity,
                0.12f + 0.05f * intensity,
                18
            );
            GeneralParticleHandler.SpawnParticle(ring);

            BloomRing bloomRing = new(
                target.Center,
                Vector2.Zero,
                Color.Lerp(AbyssBlue, AbyssFoam, 0.35f) * (0.32f * intensity),
                0.45f * intensity,
                22
            );
            GeneralParticleHandler.SpawnParticle(bloomRing);

            StrongBloom coreBloom = new(
                target.Center,
                Vector2.Zero,
                Color.Lerp(AbyssBlue, AbyssFoam, 0.55f) * (0.25f * intensity),
                0.38f * intensity,
                20
            );
            GeneralParticleHandler.SpawnParticle(coreBloom);

            for (int i = 0; i < 4 + (int)(3 * intensity); i++)
            {
                WaterGlobParticle glob = new(
                    target.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2Circular(2.3f, 2.3f) + forward * Main.rand.NextFloat(0.2f, 1.2f),
                    Main.rand.NextFloat(0.75f, 1.15f) * intensity
                );
                glob.Color = Color.Lerp(AbyssBlue, AbyssFoam, Main.rand.NextFloat()) * 0.34f;
                GeneralParticleHandler.SpawnParticle(glob);
            }

            for (int i = 0; i < 5 + (int)(2 * intensity); i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(2.8f, 2.8f) + forward * Main.rand.NextFloat(-0.6f, 0.6f);

                HeavySmokeParticle smoke = new(
                    target.Center + Main.rand.NextVector2Circular(10f, 10f),
                    velocity,
                    Color.Lerp(AbyssDark, AbyssBlue, Main.rand.NextFloat(0.3f, 0.85f)),
                    Main.rand.Next(24, 38),
                    Main.rand.NextFloat(0.4f, 0.72f),
                    0.65f
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 8 + (int)(2 * intensity); i++)
            {
                Vector2 velocity =
                    Main.rand.NextVector2Circular(1.8f, 1.8f) +
                    forward * Main.rand.NextFloat(0.8f, 2.3f) +
                    right * Main.rand.NextFloat(-1.25f, 1.25f);

                WaterFlavoredParticle shard = new(
                    target.Center + Main.rand.NextVector2Circular(6f, 6f),
                    velocity,
                    false,
                    Main.rand.Next(12, 22),
                    Main.rand.NextFloat(0.75f, 1.05f),
                    Color.Lerp(AbyssBlue, AbyssFoam, Main.rand.NextFloat(0.25f, 0.95f))
                );
                GeneralParticleHandler.SpawnParticle(shard);
            }

            for (int i = 0; i < 3; i++)
            {
                GenericSparkle sparkle = new(
                    target.Center + Main.rand.NextVector2Circular(5f, 5f),
                    Vector2.Zero,
                    Color.Lerp(AbyssGlow, AbyssFoam, 0.4f),
                    AbyssBlue,
                    Main.rand.NextFloat(1.8f, 2.5f) * intensity,
                    16,
                    Main.rand.NextFloat(-0.04f, 0.04f),
                    2.1f
                );
                GeneralParticleHandler.SpawnParticle(sparkle);
            }

            for (int i = 0; i < 10; i++)
            {
                int dustType = Main.rand.NextBool(5) ? AbyssDustC : (Main.rand.NextBool() ? AbyssDustA : AbyssDustB);
                Dust dust = Dust.NewDustPerfect(
                    target.Center + Main.rand.NextVector2Circular(10f, 10f),
                    dustType,
                    Main.rand.NextVector2Circular(2.2f, 2.2f) + forward * Main.rand.NextFloat(0.15f, 0.7f),
                    120,
                    Color.Lerp(AbyssDark, AbyssGlow, Main.rand.NextFloat(0.35f, 0.95f)),
                    Main.rand.NextFloat(1.1f, 1.6f) * intensity
                );
                dust.noGravity = true;
            }
        }

        private void SpawnStuckAbyssEffects(Projectile projectile, NPC target)
        {
            if (Main.rand.NextBool(2))
            {
                Vector2 orbit = orbitAngle[projectile.whoAmI].ToRotationVector2();

                HeavySmokeParticle smoke = new(
                    target.Center + orbit * Main.rand.NextFloat(10f, 18f) + Main.rand.NextVector2Circular(4f, 4f),
                    orbit.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-0.5f, 0.5f),
                    Color.Lerp(AbyssDark, AbyssBlue, Main.rand.NextFloat(0.3f, 0.8f)),
                    Main.rand.Next(18, 28),
                    Main.rand.NextFloat(0.34f, 0.58f),
                    0.48f
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (Main.rand.NextBool(3))
            {
                WaterGlobParticle glob = new(
                    target.Center + orbitAngle[projectile.whoAmI].ToRotationVector2() * Main.rand.NextFloat(8f, 14f),
                    Main.rand.NextVector2Circular(0.5f, 0.5f),
                    Main.rand.NextFloat(0.55f, 0.78f)
                );
                glob.Color = Color.Lerp(AbyssBlue, AbyssFoam, Main.rand.NextFloat()) * 0.26f;
                GeneralParticleHandler.SpawnParticle(glob);
            }

            if (Main.rand.NextBool(4))
            {
                AltSparkParticle spark = new(
                    target.Center + Main.rand.NextVector2Circular(14f, 14f),
                    Main.rand.NextVector2Circular(0.4f, 0.4f),
                    false,
                    10,
                    Main.rand.NextFloat(0.7f, 0.95f),
                    Color.Lerp(AbyssGlow, AbyssFoam, Main.rand.NextFloat(0.2f, 0.55f)) * 0.22f
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        private void SpawnReleaseDashEffects(Projectile projectile, Vector2 nextTargetCenter)
        {
            Vector2 dashDirection = (nextTargetCenter - projectile.Center).SafeNormalize(Vector2.UnitX);

            DirectionalPulseRing ring = new(
                projectile.Center,
                Vector2.Zero,
                Color.White * 0.2f,
                Vector2.One,
                dashDirection.ToRotation(),
                0.012f,
                0.09f,
                16
            );
            GeneralParticleHandler.SpawnParticle(ring);

            for (int i = 0; i < 8; i++)
            {
                WaterFlavoredParticle shard = new(
                    projectile.Center,
                    Main.rand.NextVector2Circular(1.8f, 1.8f) + dashDirection * Main.rand.NextFloat(0.8f, 2.4f),
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.75f, 1f),
                    Color.Lerp(AbyssBlue, AbyssFoam, Main.rand.NextFloat(0.2f, 0.9f))
                );
                GeneralParticleHandler.SpawnParticle(shard);
            }

            for (int i = 0; i < 2; i++)
            {
                GenericSparkle sparkle = new(
                    projectile.Center,
                    Vector2.Zero,
                    Color.Lerp(AbyssGlow, AbyssFoam, 0.5f),
                    AbyssBlue,
                    Main.rand.NextFloat(1.6f, 2.2f),
                    14,
                    Main.rand.NextFloat(-0.02f, 0.02f),
                    2f
                );
                GeneralParticleHandler.SpawnParticle(sparkle);
            }
        }

        private void SpawnDeathAbyssEffects(Projectile projectile)
        {
            DirectionalPulseRing ring = new(
                projectile.Center,
                Vector2.Zero,
                Color.White * 0.24f,
                Vector2.One,
                0f,
                0.02f,
                0.16f,
                22
            );
            GeneralParticleHandler.SpawnParticle(ring);

            StrongBloom coreBloom = new(
                projectile.Center,
                Vector2.Zero,
                Color.Lerp(AbyssBlue, AbyssFoam, 0.55f) * 0.28f,
                0.48f,
                26
            );
            GeneralParticleHandler.SpawnParticle(coreBloom);

            for (int i = 0; i < 5; i++)
            {
                WaterGlobParticle glob = new(
                    projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextVector2Circular(2.2f, 2.2f),
                    Main.rand.NextFloat(0.75f, 1.05f)
                );
                glob.Color = Color.Lerp(AbyssBlue, AbyssFoam, Main.rand.NextFloat()) * 0.3f;
                GeneralParticleHandler.SpawnParticle(glob);
            }

            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(2.6f, 2.6f);

                HeavySmokeParticle smoke = new(
                    projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    velocity,
                    Color.Lerp(AbyssDark, AbyssBlue, Main.rand.NextFloat(0.25f, 0.85f)),
                    Main.rand.Next(20, 32),
                    Main.rand.NextFloat(0.42f, 0.72f),
                    0.56f
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            for (int i = 0; i < 12; i++)
            {
                int dustType = Main.rand.NextBool(5) ? AbyssDustC : (Main.rand.NextBool() ? AbyssDustA : AbyssDustB);
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    dustType,
                    Main.rand.NextVector2Circular(3.1f, 3.1f),
                    120,
                    Color.Lerp(AbyssDark, AbyssGlow, Main.rand.NextFloat(0.35f, 0.95f)),
                    Main.rand.NextFloat(1.1f, 1.6f)
                );
                dust.noGravity = true;
            }
        }
    }
}
