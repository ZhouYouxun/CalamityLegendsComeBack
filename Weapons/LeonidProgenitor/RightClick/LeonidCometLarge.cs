using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    public class LeonidCometLarge : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/LeonidProgenitor";
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";

        public Player Owner => Main.player[Projectile.owner];
        public float Progress => Projectile.ai[2];

        private bool initialized;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = 2;
        }

        public override void AI()
        {
            if (!initialized)
            {
                Projectile.scale = 0.8f + 0.6f * Progress;
                Projectile.DamageType = Owner.HeldItem.DamageType;
                initialized = true;
            }

            Color trailColor = LeonidVisualUtils.GetCelestialColor(Progress, Projectile.whoAmI * 0.13f);
            Lighting.AddLight(Projectile.Center, trailColor.ToVector3() * 0.8f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(16f * Projectile.scale, 16f * Projectile.scale),
                    Main.rand.NextBool(3) ? DustID.Electric : DustID.TintableDustLighted,
                    -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.15f),
                    100,
                    Color.Lerp(trailColor, LeonidVisualUtils.MoonWhite, Main.rand.NextFloat(0.28f)),
                    Main.rand.NextFloat(0.9f, 1.3f));
                dust.noGravity = true;
            }

            // 大流星体积大、蓄力越满掉得越多：沿途持续剥落针状光矢。
            // extraUpdates = 2，AI 每帧跑三次，所以分母按三倍算。
            if (Main.rand.NextBool(24))
            {
                LeonidStarlight.Shed(
                    Projectile.Center + Main.rand.NextVector2Circular(14f, 14f) * Projectile.scale,
                    Projectile.velocity,
                    trailColor,
                    LeonidStarlightShape.Needle,
                    0.85f + 0.5f * Progress,
                    hoverTime: 24,
                    lifetime: 130);
            }

            Projectile.rotation += 0.16f * Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                int shockwaveIndex = Projectile.NewProjectile(
                    Projectile.GetSource_OnHit(target),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<CalamityMod.Projectiles.Melee.EarthenTidesShockwave>(),
                    (int)(Projectile.damage * 0.75f),
                    0f,
                    Projectile.owner,
                    1.5f
                );

                if (Main.projectile.IndexInRange(shockwaveIndex))
                {
                    Main.projectile[shockwaveIndex].DamageType = ModContent.GetInstance<RogueDamageClass>();
                    Main.projectile[shockwaveIndex].friendly = true;
                    Main.projectile[shockwaveIndex].hostile = false;
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.85f, Pitch = -0.2f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f, Pitch = 0.1f }, Projectile.Center);

            Color themeColor = LeonidVisualUtils.GetCelestialColor(Progress, Projectile.whoAmI * 0.17f);
            LeonidVisualUtils.SpawnDustBurst(Projectile.Center, themeColor, 78, 12f, 1.8f);
            LeonidVisualUtils.SpawnBloomBurst(Projectile.Center, themeColor * 0.45f, 0.7f, 16);
            LeonidVisualUtils.SpawnCelestialPulse(Projectile.Center, Projectile.oldVelocity, themeColor, 1.65f, 28);

            // 大流星解体：先是一圈同步张开的星辉环（读得出"阵型"），
            // 再补一层零散碎片和两枚大耀斑，蓄力越满规模越大。
            int ringCount = 8 + (int)(Progress * 6f);
            LeonidStarlight.Ring(Projectile.Center, ringCount, 30f, themeColor, LeonidStarlightShape.Shard,
                speed: 7.5f, scale: 1.1f, hoverTime: 28, lifetime: 180, lanceSpeed: 19f);

            LeonidStarlight.Burst(Projectile.Center, 10, Color.Lerp(themeColor, LeonidVisualUtils.MoonWhite, 0.35f),
                LeonidStarlightShape.Mote, speed: 9f, scale: 0.95f, hoverTime: 34, lifetime: 190, lanceSpeed: 21f, spawnRadius: 20f);

            LeonidStarlight.Burst(Projectile.Center, 2, LeonidVisualUtils.StarGold, LeonidStarlightShape.Halo,
                speed: 3.5f, scale: 1f + Progress * 0.6f, hoverTime: 16, lifetime: 100, lanceSpeed: 13f);

            // Release 7 reflective meteors
            if (Main.myPlayer == Projectile.owner)
            {
                int reflectiveType = ModContent.ProjectileType<LeonidReflectiveMeteor>();
                int splitDamage = (int)(Projectile.damage * 0.5f);

                for (int i = 0; i < 7; i++)
                {
                    // Generate initial velocity in random directions
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float speed = Main.rand.NextFloat(5.5f, 13.5f);
                    Vector2 splitVel = angle.ToRotationVector2() * speed;

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        splitVel,
                        reflectiveType,
                        splitDamage,
                        Projectile.knockBack * 0.4f,
                        Projectile.owner);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Rogue/LeonidProgenitorGlow").Value;
            Color drawColor = LeonidVisualUtils.GetCelestialColor(Progress, Projectile.whoAmI * 0.17f) * (1f - Projectile.alpha / 255f);
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            // These textures contain black source pixels; keep all emitted light additive.
            LeonidVisualUtils.BeginAdditiveSpriteBatch();
            LeonidVisualUtils.DrawGlowBlade(Projectile.Center - direction * 12f, direction, drawColor, 0.46f, 0.18f * Projectile.scale, 0.034f * Projectile.scale);
            LeonidVisualUtils.DrawCelestialHead(Projectile.Center, drawColor, 1f - Projectile.alpha / 255f, Projectile.scale * 1.28f, Projectile.rotation);
            Main.EntitySpriteDraw(glow, drawPosition, null, Color.White * (1f - Projectile.alpha / 255f), Projectile.rotation, glow.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            LeonidVisualUtils.DrawBloom(Projectile.Center, drawColor * 0.3f, Projectile.scale * 0.45f);
            // Night-sky blue outline glow
            float nsOp = (1f - Projectile.alpha / 255f) * 0.48f;
            for (int i = 0; i < 8; i++)
            {
                Vector2 off = (i * MathHelper.TwoPi / 8f).ToRotationVector2() * 2.8f * Projectile.scale;
                Main.EntitySpriteDraw(texture, drawPosition + off, null,
                    LeonidVisualUtils.NightSkyBlue * nsOp,
                    Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 oldDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(texture, oldDrawPosition, null, drawColor * completion * 0.3f, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Color.Lerp(drawColor, Color.White, 0.08f), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);

            // 星光拖尾与头部星芒。蓄力越满，星芒越大越亮。
            LeonidStarlight.DrawStarTrail(
                Projectile.oldPos,
                Projectile.Size,
                Color.Lerp(drawColor, LeonidVisualUtils.MoonWhite, 0.45f),
                Color.Lerp(drawColor, LeonidVisualUtils.DeepStratusBlue, 0.45f),
                0.55f,
                0.075f * Projectile.scale,
                Projectile.rotation,
                step: 1);

            float twinkle = 0.7f + 0.3f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.6f + Projectile.whoAmI);
            LeonidStarlight.DrawFlare(
                Projectile.Center,
                Color.Lerp(drawColor, LeonidVisualUtils.MoonWhite, 0.55f),
                0.8f * twinkle,
                (0.12f + 0.06f * Progress) * Projectile.scale,
                -Projectile.rotation * 0.3f);

            LeonidStarlight.DrawSunburst(
                Projectile.Center,
                LeonidVisualUtils.StarGold,
                0.2f + 0.14f * Progress,
                (0.035f + 0.02f * Progress) * Projectile.scale,
                Main.GlobalTimeWrappedHourly * 0.5f);

            return false;
        }
    }
}
