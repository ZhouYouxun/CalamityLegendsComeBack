using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    // 散播箭的毒气子弹幕，主要承担滞留区域和持续伤害。
    internal class BFArrow_EPlagueGas : ModProjectile
    {
        private const int BaseLifetime = 92;
        private const int LargeSporeLifetime = 207;
        private const float LargeSporeSpeedMultiplier = 1.66f;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Variant => ref Projectile.ai[0];
        private ref float MaxVisualScale => ref Projectile.ai[1];

        internal void ConfigureFromLargeSpore()
        {
            // 92 帧基础寿命提高 125% 至 207 帧；初速同步校正为约 1.66 倍，
            // 在既有阻尼下实际飞行距离约为原来的两倍。
            Projectile.timeLeft = LargeSporeLifetime;
            Projectile.velocity *= LargeSporeSpeedMultiplier;
            Projectile.netUpdate = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
            Projectile.noDropItem = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = BaseLifetime;
            Projectile.alpha = 255;
            BFArrowCommon.ForceLocalNPCImmunity(Projectile, 12);
        }

        public override bool? CanDamage() => Projectile.localAI[0] > 4f && Projectile.timeLeft > 8 ? null : false;

        public override void AI()
        {
            Projectile.localAI[0]++;

            if (Projectile.localAI[0] == 1f)
                Projectile.scale = 0.35f;

            Projectile.rotation += 0.01f * Projectile.direction;
            Projectile.velocity *= 0.982f;
            Projectile.velocity.Y -= 0.01f;

            float targetScale = MaxVisualScale <= 0f ? 1.45f : MaxVisualScale;
            Projectile.scale = MathHelper.Lerp(Projectile.scale, targetScale, 0.08f);
            Projectile.Opacity = Utils.GetLerpValue(0f, 8f, Projectile.localAI[0], true) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);

            Lighting.AddLight(Projectile.Center, new Color(130, 205, 80).ToVector3() * 0.35f * Projectile.Opacity);

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(24f, 24f),
                    DustID.GreenTorch,
                    Main.rand.NextVector2Circular(1.1f, 1.1f),
                    100,
                    new Color(172, 228, 92),
                    Main.rand.NextFloat(0.9f, 1.3f));
                dust.noGravity = true;
            }

            if (Main.rand.NextBool(4))
            {
                Dust mist = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(30f, 30f),
                    DustID.Smoke,
                    Main.rand.NextVector2Circular(0.65f, 0.65f) + new Vector2(0f, -0.1f),
                    120,
                    new Color(92, 128, 40),
                    Main.rand.NextFloat(0.8f, 1.1f));
                mist.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 180);
            target.AddBuff(BuffID.Venom, 120);
            if (Main.rand.NextBool(8))
            {
                SoundEngine.PlaySound(BlossomFluxSounds.RightPlagueGas, target.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int textureType = (int)Variant switch
            {
                0 => ProjectileID.ToxicCloud,
                1 => ProjectileID.ToxicCloud2,
                _ => ProjectileID.ToxicCloud3
            };

            Texture2D texture = TextureAssets.Projectile[textureType].Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color drawColor = new Color(178, 255, 132, 0) * Projectile.Opacity;

            Main.EntitySpriteDraw(
                bloomTexture,
                Projectile.Center - Main.screenPosition,
                null,
                new Color(142, 220, 92, 0) * (Projectile.Opacity * 0.28f),
                0f,
                bloomTexture.Size() * 0.5f,
                Projectile.scale * 0.18f,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                drawColor,
                Projectile.rotation,
                texture.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition + new Vector2(3f, -2f),
                null,
                new Color(220, 255, 176, 0) * (Projectile.Opacity * 0.45f),
                -Projectile.rotation * 0.8f,
                texture.Size() * 0.5f,
                Projectile.scale * 0.75f,
                SpriteEffects.None,
                0);

            return false;
        }
    }
}
