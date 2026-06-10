using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Malachite.RightGeneral.Stealth
{
    public sealed class MalachiteRightFeatherExplosion : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/Malachite";

        public override string LocalizationCategory => "Projectiles.Malachite";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 184;
            Projectile.height = 184;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 24;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool? CanDamage() => Timer <= 5f;

        public override void AI()
        {
            Timer++;
            if (Timer == 1f)
            {
                Projectile.Damage();
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.42f, Pitch = 0.35f, MaxInstances = 5 }, Projectile.Center);
                ApplyScreenShake(3.8f);
            }

            Lighting.AddLight(Projectile.Center, 0.14f, 0.7f, 0.22f);

            if (Main.dedServ)
                return;

            if (Timer == 1f)
            {
                GeneralParticleHandler.SpawnParticle(new PulseRing(
                    Projectile.Center,
                    Vector2.Zero,
                    new Color(90, 255, 130) * 0.58f,
                    0.05f,
                    1.25f,
                    18));
            }

            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(72f, 72f),
                    DustID.Terra,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.5f, 4.8f),
                    80,
                    Main.rand.NextBool() ? new Color(80, 255, 125) : new Color(210, 255, 120),
                    Main.rand.NextFloat(0.8f, 1.3f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Poisoned, 8 * 60);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = bloom.Size() * 0.5f;

            float progress = Timer / 24f;
            float pulse = MathF.Sin(MathHelper.Clamp(progress, 0f, 1f) * MathHelper.Pi);

            Color color = new Color(80, 255, 120, 0) * (0.55f * pulse);

            // BloomCircle 必须用 Additive 绘制，否则黑色底会被错误画出来
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                color,
                0f,
                origin,
                1.15f + pulse * 0.8f,
                SpriteEffects.None);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1000f, 120f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }
    }
}
