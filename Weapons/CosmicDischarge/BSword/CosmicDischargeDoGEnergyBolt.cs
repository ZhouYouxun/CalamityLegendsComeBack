using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeDoGEnergyBolt : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.DoGSpecialColor.ToVector3() * 0.32f);
            float homingProgress = Utils.GetLerpValue(4f, 54f, Time, true);

            if (Time >= 4f)
            {
                NPC target = FindBestTarget(980f);
                if (target != null)
                {
                    float currentSpeed = Projectile.velocity.Length();
                    float baseSpeed = MathHelper.Clamp(currentSpeed, 10f, 18f);
                    float targetSpeed = MathHelper.Lerp(baseSpeed, 23f, homingProgress);
                    float turnRate = MathHelper.Lerp(0.055f, 0.24f, homingProgress);
                    Vector2 desiredVel = Projectile.SafeDirectionTo(target.Center) * targetSpeed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVel, turnRate);
                }
            }

            // 这个弹幕一次会射出 3~8 发，是全武器数量最大的弹幕 ——
            // 所以每发的拖尾必须最省：只走统一的 DoGFire 配比，别的一概不加。
            CosmicDischargeCommon.SpawnTrailWake(
                Projectile.Center,
                -Projectile.velocity.SafeNormalize(Vector2.UnitX),
                CosmicDischargeCommon.RiftLightBlue,
                0.55f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CosmicDischargeCommon.ApplyDoGDebuffs(target, 180);
            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/LanceofDestinyStrong")
            {
                Volume = 0.34f,
                Pitch = 0.28f,
                MaxInstances = 5
            }, target.Center);

            // 命中同样只给 Light 档 —— 一次齐射有 8 发，8 个 Light 叠起来才等于一次重击。
            CosmicDischargeCommon.SpawnRiftBurst(target.Center, RiftTier.Light, direction, CosmicDischargeCommon.RiftLightBlue);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = bloom.Size() * 0.5f;
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float factor = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(
                    bloom,
                    drawPos,
                    null,
                    CosmicDischargeCommon.Transparent(CosmicDischargeCommon.RiftTwilight) * 0.32f * factor,
                    0f,
                    origin,
                    0.12f * factor,
                    SpriteEffects.None
                );
            }

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                CosmicDischargeCommon.DoGWhiteColor * 0.65f,
                0f,
                origin,
                0.16f,
                SpriteEffects.None
            );

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private NPC FindBestTarget(float maxDistance)
        {
            NPC marked = null;
            NPC normal = null;
            float closestMarked = maxDistance;
            float closestNormal = maxDistance;

            int markDebuff = ModContent.BuffType<CosmicDischargeDoGMarkDebuff>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float dist = Projectile.Distance(npc.Center);
                if (npc.HasBuff(markDebuff))
                {
                    if (dist < closestMarked)
                    {
                        closestMarked = dist;
                        marked = npc;
                    }
                }
                else
                {
                    if (dist < closestNormal)
                    {
                        closestNormal = dist;
                        normal = npc;
                    }
                }
            }

            return marked ?? normal;
        }
    }
}
