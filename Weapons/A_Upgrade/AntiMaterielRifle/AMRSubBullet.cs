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
    internal sealed class AMRSubBullet : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/Ranged/AMRShot";

        private int TargetIndex => (int)Projectile.ai[0];
        private int DelayTimer => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 120;
            Projectile.scale = 0.75f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => DelayTimer <= 0 ? null : false;

        public override void AI()
        {
            // 延迟出现逻辑（支持多发连续依次现身，形成明显弹道视觉）
            if (Projectile.ai[1] > 0)
            {
                Projectile.ai[1]--;
                return;
            }

            if (Projectile.timeLeft == 119 - (int)Projectile.ai[1])
            {
                SoundEngine.PlaySound(SoundID.Item11 with { Volume = 0.35f, Pitch = 0.3f }, Projectile.Center);
            }

            NPC target = null;
            if (TargetIndex >= 0 && TargetIndex < Main.maxNPCs && Main.npc[TargetIndex].CanBeChasedBy(Projectile))
            {
                target = Main.npc[TargetIndex];
            }
            else
            {
                target = Projectile.Center.ClosestNPCAt(700f);
            }

            if (target != null && target.active)
            {
                Vector2 desiredVel = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 24f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVel, 0.18f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.CopperCoin,
                    -Projectile.velocity * 0.15f,
                    0,
                    new Color(255, 180, 80),
                    Main.rand.NextFloat(0.5f, 0.9f));
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (DelayTimer > 0)
                return false;

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Main.EntitySpriteDraw(bloom, drawPos, null, new Color(255, 160, 40, 0), 0f,
                bloom.Size() * 0.5f, 0.15f * Projectile.scale, SpriteEffects.None, 0f);

            Main.EntitySpriteDraw(texture, drawPos, null, new Color(255, 240, 180, 0),
                Projectile.rotation, origin, new Vector2(0.9f, 1.3f) * Projectile.scale, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
