using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    internal class SHPCNecroplasmDamage : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;

            Projectile.friendly = true;
            Projectile.tileCollide = false;

            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        // ===== 飞行逻辑：纯减速 =====
        public override void AI()
        {
            timer++; // 计时器

            if (timer > 20)
            {
                NPC target = FindTarget(1400f);
                if (target != null)
                {
                    float trackingPower = Utils.GetLerpValue(20f, 120f, timer, true);
                    float lateLifeBoost = Projectile.timeLeft < 90 ? 2f : 1f;
                    float speed = MathHelper.Lerp(8f, 19f, trackingPower) * lateLifeBoost;
                    Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, MathHelper.Lerp(0.08f, 0.34f, trackingPower));
                    return;
                }
            }

            Projectile.velocity *= 0.965f;
        }

        private NPC FindTarget(float maxDistance)
        {
            NPC result = null;
            float bestDistance = maxDistance;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                result = npc;
            }

            return result;
        }

        // ===== 视觉完全复刻 =====
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D lightTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SmallGreyscaleCircle").Value;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float colorInterpolation =
                    (float)Math.Cos(
                        Projectile.timeLeft / 32f +
                        Main.GlobalTimeWrappedHourly / 20f +
                        i / (float)Projectile.oldPos.Length * MathHelper.Pi
                    ) * 0.5f + 0.5f;

                Color color = Color.Lerp(
                    new Color(255, 80, 180),
                    new Color(200, 40, 140),
                    colorInterpolation
                ) * 0.8f;

                color.A = 255;

                Vector2 drawPosition =
                    Projectile.oldPos[i]
                    + lightTexture.Size() * 0.5f
                    - Main.screenPosition
                    + new Vector2(0f, Projectile.gfxOffY)
                    + new Vector2(-28f, -28f);

                Color outerColor = color;
                Color innerColor = color * 0.5f;

                float intensity =
                    0.9f + 0.15f *
                    (float)Math.Cos(Main.GlobalTimeWrappedHourly % 60f * MathHelper.TwoPi);

                intensity *= MathHelper.Lerp(
                    0.15f,
                    1f,
                    1f - i / (float)Projectile.oldPos.Length
                );

                if (Projectile.timeLeft <= 60)
                    intensity *= Projectile.timeLeft / 60f;

                Vector2 outerScale = new Vector2(1f) * intensity;
                Vector2 innerScale = new Vector2(1f) * intensity * 0.7f;

                outerColor *= intensity;
                innerColor *= intensity;

                Main.EntitySpriteDraw(
                    lightTexture,
                    drawPosition,
                    null,
                    outerColor,
                    0f,
                    lightTexture.Size() * 0.5f,
                    outerScale * 0.6f,
                    SpriteEffects.None,
                    0
                );

                Main.EntitySpriteDraw(
                    lightTexture,
                    drawPosition,
                    null,
                    innerColor,
                    0f,
                    lightTexture.Size() * 0.5f,
                    innerScale * 0.6f,
                    SpriteEffects.None,
                    0
                );
            }

            return false;
        }

        // ===== 全部留空 =====
        public override void OnSpawn(Terraria.DataStructures.IEntitySource source) { }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) { }

        public override void OnKill(int timeLeft) { }


        private int timer;

        public override bool? CanDamage()
        {
            if (timer < 20)
                return false;
            return null;
        }
    }
}
