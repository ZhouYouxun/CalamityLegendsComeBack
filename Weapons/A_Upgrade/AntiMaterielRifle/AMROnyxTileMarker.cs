using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle
{
    internal sealed class AMROnyxTileMarker : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public const float TriggerRadius = 85f;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300; // 5 秒寿命
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            // 嘉登科技玛瑙脉冲圈与微粒特效
            if (!Main.dedServ)
            {
                if (Projectile.timeLeft % 20 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                        Projectile.Center, Vector2.Zero, new Color(233, 102, 238),
                        new Vector2(1f, 1f), 0f, 0.04f, 0.9f, 25));
                }

                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(TriggerRadius * 0.8f, TriggerRadius * 0.8f),
                        DustID.PurpleTorch,
                        Vector2.Zero,
                        60,
                        new Color(233, 102, 238),
                        Main.rand.NextFloat(0.8f, 1.3f));
                    d.noGravity = true;
                }
            }
        }

        public void Detonate(int detonatorDamage)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = -0.3f }, Projectile.Center);

            if (Projectile.owner == Main.myPlayer)
            {
                int detonation = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<AMROnyxDetonation>(),
                    detonatorDamage,
                    6f,
                    Projectile.owner,
                    -1); // -1 表示物块爆炸

                if (Main.projectile.IndexInRange(detonation))
                    Main.projectile[detonation].CritChance = 0; // 不能暴击
            }

            Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 绘制嘉登科技全息提示圆圈与十字线条
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float pulse = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.08f + 0.92f;
            float opacity = Math.Min(1f, Projectile.timeLeft / 30f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Color auraColor = new Color(225, 75, 230, 0) * opacity * 0.4f;

            // 范围圆晕
            Main.EntitySpriteDraw(bloom, drawPos, null, auraColor, 0f,
                bloom.Size() * 0.5f, (TriggerRadius / (bloom.Width * 0.5f)) * pulse, SpriteEffects.None, 0f);

            // 核心过曝点
            Main.EntitySpriteDraw(bloom, drawPos, null, new Color(255, 230, 255, 0) * opacity, 0f,
                bloom.Size() * 0.5f, 0.12f, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
