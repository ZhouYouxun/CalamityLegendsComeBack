using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode
{
    internal class StormlionMandible_ARC : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // ==================== 自定义字段 ====================
        private bool ableToHit = false;
        private bool initialized = false;
        private bool hasHit = false;
        private int arcTimer = 0;
        private float curveDirection = 1f;
        private bool hasEscapedTarget = false;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 90;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 120;
            Projectile.extraUpdates = 10;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 25;
        }

        public override bool? CanDamage() => ableToHit ? (bool?)null : false;

        public override void AI()
        {
            if ((int)Projectile.ai[0] < 0 || (int)Projectile.ai[0] >= Main.maxNPCs)
            {
                Projectile.Kill();
                return;
            }

            NPC target = Main.npc[(int)Projectile.ai[0]];
            if (!target.active || !target.CanBeChasedBy())
            {
                Projectile.Kill();
                return;
            }

            if (!initialized)
            {
                initialized = true;
                curveDirection = Main.rand.NextBool() ? 1f : -1f;
            }

            arcTimer++;

            Vector2 toTarget = target.Center - Projectile.Center;
            Vector2 forward = toTarget.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.Pi / 2f);

            // ==================== 正常连锁曲线 / 无目标时回旋返打 ====================
            if (Projectile.ai[2] == 1f)
            {
                float awayWeight = Utils.GetLerpValue(0f, 4f, arcTimer, true);
                float returnWeight = Utils.GetLerpValue(2f, 10f, arcTimer, true);

                Vector2 awayDir = (-forward).RotatedBy(curveDirection * 0.55f);
                Vector2 control =
                    awayDir * (0.04f + awayWeight * 0.025f) +
                    right * curveDirection * 0.025f +
                    forward * (0.20f + returnWeight * 0.28f);

                Projectile.velocity += control;
            }
            else
            {
                // 常规电弧：可能上弯，也可能下弯
                float homingStrength = 0.18f;
                float curveStrength = 0.065f + 0.015f * (float)Math.Sin(arcTimer * 0.22f);

                Projectile.velocity += forward * homingStrength;
                Projectile.velocity += right * curveDirection * curveStrength;
            }

            // 速度限制
            float currentSpeed = Projectile.velocity.Length();
            float minSpeed = 2f;
            float maxSpeed = 5f;

            if (currentSpeed < minSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * minSpeed;
            if (currentSpeed > maxSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * maxSpeed;

            // 轻微跟随目标位移，防止高速目标甩掉电弧
            Projectile.position += (target.position - target.oldPosition) / 16f;

            // 最后保险：寿命快结束时强制贴脸
            if (Projectile.timeLeft <= 12)
                Projectile.Center = Vector2.Lerp(Projectile.Center, target.Center, 0.5f);

            CheckNearTarget(target);
        }

        private void CheckNearTarget(NPC target)
        {
            float distanceToTarget = Vector2.Distance(Projectile.Center, target.Center);

            // 普通电弧：出生后一小段时间内不允许造成伤害，防止刚生成就贴脸命中
            if (arcTimer <= 8)
            {
                ableToHit = false;
                return;
            }

            // 回旋返打型：必须先真正飞离目标，再允许重新命中
            if (Projectile.ai[2] == 1f)
            {
                float escapeDistance = Math.Max(72f, target.Size.Length() * 0.65f);

                if (!hasEscapedTarget)
                {
                    if (distanceToTarget >= escapeDistance)
                        hasEscapedTarget = true;

                    ableToHit = false;
                    return;
                }
            }

            float hitDistance = Math.Max(28f, target.Size.Length() * 0.28f);

            bool intersects = Projectile.Hitbox.Intersects(target.Hitbox);
            bool closeEnough = distanceToTarget <= hitDistance;

            ableToHit = closeEnough || intersects;
        }

        private void SpawnNextArc(NPC currentTarget)
        {
            if (Projectile.ai[1] <= 1f)
                return;

            float maxDistance = 420f;
            int nextTargetIndex = -1;

            // ==================== 优先寻找最近的新目标 ====================
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || !npc.CanBeChasedBy())
                    continue;

                if (npc.whoAmI == currentTarget.whoAmI)
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance < maxDistance && npc.Calamity().arcZapCooldown == 0)
                {
                    maxDistance = distance;
                    nextTargetIndex = i;
                }
            }

            if (nextTargetIndex != -1)
            {
                NPC newTarget = Main.npc[nextTargetIndex];
                newTarget.Calamity().arcZapCooldown = 18;

                Vector2 toNewTarget = (newTarget.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Vector2 spawnVelocity = toNewTarget.RotatedBy(curveDirection * 0.45f) * Main.rand.NextFloat(10f, 14f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    spawnVelocity,
                    ModContent.ProjectileType<StormlionMandible_ARC>(),
                    Projectile.damage,
                    0f,
                    Projectile.owner,
                    nextTargetIndex,
                    Projectile.ai[1] - 1f,
                    0f
                );
            }
            else
            {
                // ==================== 周围没敌人：甩出去再折返继续打当前目标 ====================
                currentTarget.Calamity().arcZapCooldown = 10;

                Vector2 awayDir = (Projectile.Center - currentTarget.Center).SafeNormalize(Vector2.UnitX).RotatedBy(curveDirection * 0.85f);
                Vector2 spawnVelocity = awayDir * Main.rand.NextFloat(10f, 13f);

                Vector2 spawnPos = currentTarget.Center + awayDir * Math.Max(40f, currentTarget.Size.Length() * 0.4f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPos,
                    spawnVelocity,
                    ModContent.ProjectileType<StormlionMandible_ARC>(),
                    Projectile.damage,
                    0f,
                    Projectile.owner,
                    currentTarget.whoAmI,
                    Projectile.ai[1] - 1f,
                    1f
                );
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (hasHit)
                return;

            hasHit = true;
            ableToHit = false;

            SpawnNextArc(target);
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/ChainLightning", 4) { Volume = 0.15f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D lightTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SmallGreyscaleCircle").Value;
            Vector2 origin = lightTexture.Size() * 0.5f;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float completion = i / (float)Projectile.oldPos.Length;
                float colorInterpolation = (float)Math.Cos(Projectile.timeLeft / 13f + completion * MathHelper.Pi) * 0.5f + 0.5f;

                Color coreColor = Color.Lerp(Color.Cyan, Color.LightBlue, colorInterpolation) * 0.42f;
                coreColor.A = 0;

                Color outlineColor = Color.White * 0.18f;
                outlineColor.A = 0;

                float intensity = 0.9f + 0.15f * (float)Math.Cos(Main.GlobalTimeWrappedHourly % 60f * MathHelper.TwoPi);
                intensity *= MathHelper.Lerp(0.15f, 1f, 1f - completion);

                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);

                Vector2 outerScale = new Vector2(1f) * intensity * 0.15f;
                Vector2 innerScale = new Vector2(1f) * intensity * 0.105f;
                Vector2 outlineScale = new Vector2(1f) * intensity * 0.165f;

                Vector2 orbitOffset = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * 1.5f;

                if (Projectile.timeLeft > 12)
                {
                    // 最细包边
                    Main.EntitySpriteDraw(lightTexture, drawPosition + new Vector2(1f, 0f), null, outlineColor, 0f, origin, outlineScale, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(lightTexture, drawPosition + new Vector2(-1f, 0f), null, outlineColor, 0f, origin, outlineScale, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(lightTexture, drawPosition + new Vector2(0f, 1f), null, outlineColor, 0f, origin, outlineScale, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(lightTexture, drawPosition + new Vector2(0f, -1f), null, outlineColor, 0f, origin, outlineScale, SpriteEffects.None, 0);

                    Main.EntitySpriteDraw(lightTexture, drawPosition, null, coreColor, 0f, origin, outerScale, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(lightTexture, drawPosition, null, coreColor * 0.55f, 0f, origin, innerScale, SpriteEffects.None, 0);

                    Main.EntitySpriteDraw(lightTexture, drawPosition + orbitOffset, null, coreColor * 0.75f, 0f, origin, innerScale * 0.8f, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(lightTexture, drawPosition - orbitOffset, null, coreColor * 0.75f, 0f, origin, innerScale * 0.8f, SpriteEffects.None, 0);
                }
            }

            return false;
        }
    }
}