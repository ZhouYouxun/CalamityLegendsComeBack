using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.LeftClick
{
    /// <summary>
    /// 左键上挑 / 劈落命中的一次性冥蚀黑洞冲击（第三段的黑洞交给持续奇点）。
    /// 前几帧造成一次范围伤害并把敌人短促地吸向命中点；视觉走共享的分层事件视界。
    /// ai[0] = 连招段数(0/1/2)，决定半径、拉扯与震屏。
    /// </summary>
    public class UmbralNadirImpactExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public int Stage => (int)Projectile.ai[0];
        private float Radius => UmbralNadirBalance.GetImpactRadius(Stage);
        private bool IsLanceImpact => Projectile.ai[1] >= 16f;
        private float LanceDirection => Projectile.ai[1] - 20f;

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 16;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool? CanDamage() => Projectile.timeLeft >= 13 ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
            => CalamityUtils.CircularHitboxCollision(Projectile.Center, Radius, targetHitbox);

        public override void OnSpawn(IEntitySource source)
        {
            bool finale = Stage >= 2;
            float sizeMult = Radius / 150f;

            SoundEngine.PlaySound(new SoundStyle(finale ? "CalamityMod/Sounds/Item/MeldExplosion" : "CalamityMod/Sounds/Item/MeldBurn")
                with { Volume = finale ? 0.8f : 0.55f, Pitch = finale ? -0.15f : 0.15f + Stage * 0.06f }, Projectile.Center);

            UmbralNadirVisuals.EventHorizon(Projectile.Center, sizeMult, finale);
            UmbralNadirVisuals.MeldSparkBurst(Projectile.Center, 10 + Stage * 6, 5f + Stage * 2f);
            UmbralNadirVisuals.ImplosionDust(Projectile.Center, sizeMult);
            if (IsLanceImpact)
                SpawnBreakthroughFan();

            float shake = UmbralNadirBalance.GetImpactScreenShake(Stage);
            if (shake > 0f)
                UmbralNadirVisuals.ScreenShake(Projectile.Center, shake);
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            // 前几帧把敌人短促地吸向命中点（冲刺段的拉扯交给奇点，故其 strength 为 0）
            float strength = UmbralNadirBalance.GetImpactPullStrength(Stage);
            if (strength > 0f && Projectile.timeLeft >= 10)
                UmbralNadirVisuals.PullNPCs(Projectile.Center, UmbralNadirBalance.GetImpactPullRange(Stage), strength);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Voidfrost>(), 90);
            UmbralCorrosionGlobalNPC.AddStacks(target, 1);
        }

        private void SpawnBreakthroughFan()
        {
            // 保持伤害半径不变，只把贯穿命中的视觉延展成定向扇面。
            const int rays = 9;
            for (int i = 0; i < rays; i++)
            {
                float ratio = i / (float)(rays - 1);
                float angle = LanceDirection + MathHelper.Lerp(-0.88f, 0.88f, ratio);
                Vector2 direction = angle.ToRotationVector2();
                float speed = MathHelper.Lerp(5f, 12f, 1f - MathF.Abs(ratio - 0.5f) * 1.4f);

                Dust dust = Dust.NewDustPerfect(Projectile.Center + direction * 12f,
                    ModContent.DustType<CalamityMod.Dusts.VoidDustInverted>(), direction * speed,
                    0, UmbralNadirPalette.MeldGreen, MathHelper.Lerp(0.85f, 1.45f, ratio));
                dust.noGravity = true;
                dust.color = UmbralNadirPalette.MeldGreen;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!IsLanceImpact)
                return false;

            float progress = 1f - Projectile.timeLeft / 16f;
            float opacity = MathF.Sin(progress * MathHelper.Pi);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Texture2D halfSmear = ModContent.Request<Texture2D>("CalamityMod/Particles/SemiCircularSmearSwipe").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Color deepGreen = UmbralNadirPalette.MeldGreenDeep with { A = 0 };
            const int rays = 7;
            for (int i = 0; i < rays; i++)
            {
                float ratio = i / (float)(rays - 1);
                float angle = LanceDirection + MathHelper.Lerp(-0.82f, 0.82f, ratio);
                float strength = 1f - MathF.Abs(ratio - 0.5f) * 1.1f;
                Vector2 rayEnd = angle.ToRotationVector2() * (24f + progress * 118f * strength);
                Color color = i % 2 == 0 ? UmbralNadirPalette.MeldGreenBright with { A = 0 } : deepGreen;

                Main.EntitySpriteDraw(halfSmear, drawPos, null, color * (0.42f * opacity * strength), angle,
                    halfSmear.Size() * 0.5f, new Vector2(0.66f + progress * 1.15f, 0.18f + progress * 0.2f), SpriteEffects.None, 0);
                Main.EntitySpriteDraw(bloom, drawPos + rayEnd, null, color * (0.46f * opacity * strength), 0f,
                    bloom.Size() * 0.5f, 0.11f + progress * 0.2f, SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(ring, drawPos, null, UmbralNadirPalette.MeldGreenBright with { A = 0 } * (0.35f * opacity),
                LanceDirection, ring.Size() * 0.5f, new Vector2(0.45f + progress, 0.24f + progress * 0.42f), SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
