using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFPlantera_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/ExtraTextures/SmallGreyscaleCircle";

        private static readonly Color PureGreen = new(60, 255, 70);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.MaxUpdates = 2;
            Projectile.timeLeft = 90 * Projectile.MaxUpdates;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            CalamityUtils.HomeInOnNPC(Projectile, ignoreTiles: true, 640f, 15f, 20f);
            Lighting.AddLight(Projectile.Center, PureGreen.ToVector3() * 0.55f);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(8f, 8f);
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(Projectile.Center, velocity, PureGreen, Color.Black, Main.rand.NextFloat(0.4f, 0.8f), 200 - Main.rand.Next(60), 0.1f));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire3, 240);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D value = TextureAssets.Projectile[Type].Value;

            PFLeftEffectRules.BeginAdditive();
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float wave = MathF.Cos(Projectile.timeLeft / 16f + Main.GlobalTimeWrappedHourly / 20f + i / (float)Projectile.oldPos.Length * MathHelper.Pi) * 0.5f + 0.5f;
                Color color = Color.Lerp(PureGreen, Color.White, wave * 0.18f);
                Vector2 drawPosition = Projectile.oldPos[i] + value.Size() * 0.5f - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY) + new Vector2(-15f, -15f);
                float scale = (0.9f + 0.15f * MathF.Cos(Main.GlobalTimeWrappedHourly % 60f * MathHelper.TwoPi)) * MathHelper.Lerp(0.15f, 1f, 1f - i / (float)Projectile.oldPos.Length);
                Main.EntitySpriteDraw(value, drawPosition, null, color * scale * Projectile.scale, 0f, value.Size() * 0.5f, new Vector2(1.25f) * scale * 0.6f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(value, drawPosition, null, Color.Lerp(color, Color.Gold, 0.25f) * scale * Projectile.scale * 0.35f, 0f, value.Size() * 0.5f, new Vector2(1.25f) * scale * 0.42f, SpriteEffects.None, 0);
            }

            Vector2 currentPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Main.EntitySpriteDraw(value, currentPosition, null, Color.Lerp(PureGreen, Color.White, 0.22f) * Projectile.scale, 0f, value.Size() * 0.5f, 0.78f, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();

            return false;
        }
    }
}
