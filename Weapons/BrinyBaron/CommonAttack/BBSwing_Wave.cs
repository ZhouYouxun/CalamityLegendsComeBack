using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal class BBSwing_Wave : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles";

        private int lifeTimer;
        private float initialSpeed;

        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.light = 0.45f;
            Projectile.scale = 0.9f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 90 * Projectile.extraUpdates;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (lifeTimer < 10)
                return true;

            SpriteBatch sb = Main.spriteBatch;

            Texture2D tex = ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;

            Color[] palette = new Color[]
            {
                new Color(220, 250, 255),
                new Color(115, 215, 255),
                new Color(48, 146, 235),
                new Color(12, 54, 110),
            };

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive);

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            int trailLength = Projectile.oldPos.Length;

            // 👉 拖尾长度 = 宽度决定
            float segmentLength = Projectile.width * 0.09f;

            Vector2[] shiftedOldPos = new Vector2[trailLength];

            for (int i = 0; i < trailLength; i++)
            {
                shiftedOldPos[i] = Projectile.Center - forward * segmentLength * i;
            }

            GameShaders.Misc["CalamityMod:SideStreakTrail"].UseImage1("Images/Misc/Perlin");

            float baseWidth = Projectile.width; // ⭐ 完全绑定碰撞箱

            float WidthFunc(float t, Vector2 v)
            {
                float shape = (float)Math.Sin(t * MathHelper.Pi);
                shape = (float)Math.Pow(shape, 0.6f);
                shape = MathHelper.Lerp(0.25f, 1f, shape);
                return baseWidth * shape;
            }

            Color ColorFunc(float t, Vector2 v)
            {
                int idx = (int)(t * (palette.Length - 1));
                idx = Utils.Clamp(idx, 0, palette.Length - 1);

                Color c = palette[idx];
                c *= (1f - t) * Projectile.Opacity * 1.2f;
                c.A = 0;
                return c;
            }

            Vector2 offsetFunc(float t, Vector2 v)
            {
                // ⭐ 严格以中心为基准（不再额外前推）
                return Vector2.Zero;
            }

            PrimitiveRenderer.RenderTrail(
                shiftedOldPos,
                new PrimitiveSettings(
                    WidthFunc,
                    ColorFunc,
                    offsetFunc,
                    shader: GameShaders.Misc["CalamityMod:SideStreakTrail"]
                ),
                60
            );

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0f
            );

            return false;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            initialSpeed = Projectile.velocity.Length();
        }

        public override void AI()
        {
            lifeTimer++;

            Projectile.velocity *= 0.99f;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            float currentSpeed = Projectile.velocity.Length();
            float speedRatio = initialSpeed <= 0.001f ? 0f : MathHelper.Clamp(currentSpeed / initialSpeed, 0f, 1f);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.34f, 0.52f));

            float visualRadius = Projectile.width * 0.5f; // ⭐ 完全绑定尺寸

            float t = Main.GameUpdateCount * 0.2f;
            float sway = (float)Math.Sin(t * 2.4f) * MathHelper.Lerp(3f, 9f, 1f - speedRatio);

            Vector2 wakeAnchor = Projectile.Center - forward * MathHelper.Lerp(visualRadius * 0.08f, visualRadius * 0.22f, speedRatio);
            float edgeDistance = visualRadius * MathHelper.Lerp(0.74f, 0.9f, speedRatio);
            float fillDistance = visualRadius * 0.88f;

            if (lifeTimer % 3 == 0)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 edgePos = wakeAnchor + right * side * (edgeDistance + sway * 0.3f);
                    Vector2 edgeVelocity =
                        -forward * MathHelper.Lerp(1.6f, 4.2f, speedRatio) +
                        right * side * MathHelper.Lerp(0.5f, 1.55f, speedRatio);

                    GlowOrbParticle wakeOrb = new GlowOrbParticle(
                        edgePos,
                        edgeVelocity,
                        false,
                        Main.rand.Next(9, 14),
                        MathHelper.Lerp(0.38f, 0.72f, speedRatio),
                        side < 0 ? new Color(70, 180, 255) : new Color(185, 245, 255),
                        true,
                        false,
                        true
                    );
                    GeneralParticleHandler.SpawnParticle(wakeOrb);
                }
            }

            if (lifeTimer % 2 == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    float band = (float)Math.Sqrt(Main.rand.NextFloat());
                    float sideBias = Main.rand.NextFloatDirection();
                    Vector2 dustPos =
                        wakeAnchor
                        + right * sideBias * fillDistance * MathHelper.Lerp(0.18f, 1f, band)
                        - forward * Main.rand.NextFloat(visualRadius * 0.04f, visualRadius * 0.2f);

                    Vector2 dustVelocity =
                        -forward * Main.rand.NextFloat(1f, MathHelper.Lerp(2f, 4.2f, speedRatio))
                        + right * sideBias * Main.rand.NextFloat(0.1f, 1.15f);

                    Dust wakeDust = Dust.NewDustPerfect(
                        dustPos,
                        Main.rand.NextBool(4) ? DustID.Frost : DustID.Water,
                        dustVelocity,
                        0,
                        Color.Lerp(new Color(105, 205, 255), new Color(215, 248, 255), Main.rand.NextFloat(0.15f, 0.9f)),
                        MathHelper.Lerp(0.85f, 1.18f, speedRatio) * Main.rand.NextFloat(0.9f, 1.08f)
                    );
                    wakeDust.noGravity = true;
                }
            }

            if (speedRatio < 0.72f && lifeTimer % 4 == 0)
            {
                float driftBand = Main.rand.NextFloatDirection();
                Vector2 driftPos =
                    wakeAnchor
                    + right * driftBand * visualRadius * Main.rand.NextFloat(0.25f, 0.8f)
                    - forward * Main.rand.NextFloat(visualRadius * 0.12f, visualRadius * 0.3f)
                    + right * sway * 0.4f;

                Vector2 driftVelocity =
                    -forward * MathHelper.Lerp(0.45f, 1.35f, speedRatio)
                    + right * driftBand * Main.rand.NextFloat(0.08f, 0.45f);

                GlowOrbParticle slowOrb = new GlowOrbParticle(
                    driftPos,
                    driftVelocity,
                    false,
                    Main.rand.Next(10, 15),
                    MathHelper.Lerp(0.32f, 0.56f, 1f - speedRatio),
                    Color.Lerp(new Color(80, 170, 255), new Color(220, 250, 255), 1f - speedRatio),
                    true,
                    false,
                    true
                );
                GeneralParticleHandler.SpawnParticle(slowOrb);

                Dust slowDust = Dust.NewDustPerfect(
                    driftPos + Main.rand.NextVector2Circular(visualRadius * 0.08f, visualRadius * 0.08f),
                    DustID.Water,
                    driftVelocity.RotatedByRandom(0.24f) * Main.rand.NextFloat(0.8f, 1.1f),
                    0,
                    new Color(195, 245, 255),
                    MathHelper.Lerp(0.82f, 1.12f, 1f - speedRatio)
                );
                slowDust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 pos = Projectile.Center;

            Vector2 upwardForward = (-Vector2.UnitY).RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-10f, 10f)));

            float radius = Projectile.width * 0.4f;

            //for (int i = 0; i < 12; i++)
            //{
            //    Vector2 spawnPos = pos + Main.rand.NextVector2Circular(radius, radius);

            //    Vector2 vel = upwardForward.RotatedByRandom(0.58f) * Main.rand.NextFloat(2.8f, 7.2f);

            //    GlowSparkParticle spark = new GlowSparkParticle(
            //        spawnPos,
            //        vel,
            //        false,
            //        Main.rand.Next(8, 14),
            //        Main.rand.NextFloat(0.09f, 0.13f),
            //        Main.rand.NextBool() ? new Color(120, 220, 255) : new Color(220, 250, 255),
            //        new Vector2(Main.rand.NextFloat(2f, 2.8f), Main.rand.NextFloat(0.42f, 0.6f)),
            //        true,
            //        false,
            //        1
            //    );
            //    GeneralParticleHandler.SpawnParticle(spark);
            //}

           
        }

        public override void OnKill(int timeLeft)
        {
        }
    }
}
