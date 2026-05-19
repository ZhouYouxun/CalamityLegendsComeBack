using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFAurora_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Magic/RancorFog";

        private const int Lifetime = 430;
        private const int FadeTime = 390;
        private ref float Timer => ref Projectile.localAI[0];
        private ref float LightPower => ref Projectile.localAI[1];
        private readonly Color orangeFog = new(255, 160, 120);
        private Color blueFog = new(150, 120, 255);
        private float orangeRot;
        private float blueRot;
        private float orangeScale = 1f;
        private float blueScale = 1f;
        private float damageMult = 1f;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 4;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 8;
        }

        public override void AI()
        {
            if (Timer == 0f)
            {
                blueFog.G = (byte)Main.rand.Next(130, 251);
                orangeScale = Main.rand.NextFloat(0.82f, 1f);
                blueScale = orangeScale * Main.rand.NextFloat(0.92f, 1.14f);
                orangeRot = Main.rand.NextFloat(MathHelper.TwoPi);
                blueRot = orangeRot + MathHelper.ToRadians(Main.rand.NextFloat(30f, 330f));
            }

            Timer++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            Projectile.Center += side * (float)Math.Sin(Timer * 0.047f + Projectile.ai[1] * 0.03f) * 0.62f;
            Projectile.velocity = Projectile.velocity.RotatedBy((float)Math.Sin(Timer * 0.031f) * 0.008f) * 0.998f;

            if (Timer >= FadeTime)
            {
                Projectile.scale = Utils.GetLerpValue(Lifetime, FadeTime, Timer, true);
                if (Projectile.scale <= 0.01f)
                    Projectile.Kill();
            }
            else if (Timer >= 6f)
                Projectile.scale = Utils.GetLerpValue(6f, 36f, Timer, true);
            else
                return;

            Color smokeColor = Color.Lerp(orangeFog, blueFog, 0.6f + 0.4f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f));
            Lighting.AddLight(Projectile.Center, smokeColor.ToVector3() * Projectile.scale);
            EmitAuroraSmoke(smokeColor);

            orangeRot += MathHelper.ToRadians(1f);
            blueRot -= MathHelper.ToRadians(1f);
            Projectile.Opacity = Utils.GetLerpValue(0f, 15f, Timer, true) * Utils.GetLerpValue(450f, 340f, Timer, true);

            if (Main.dedServ)
                return;

            float lightPowerBelow = Lighting.GetColor((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16 + 6).ToVector3().Length() / (float)Math.Sqrt(3D);
            LightPower = MathHelper.Lerp(LightPower, lightPowerBelow, 0.15f);
        }

        private void EmitAuroraSmoke(Color smokeColor)
        {
            if (Main.dedServ)
                return;

            float smokeRot = MathHelper.ToRadians(3f);
            Particle smoke = new HeavySmokeParticle(Projectile.Center, Projectile.velocity * 0.5f, smokeColor, 8, Projectile.scale * Main.rand.NextFloat(0.6f, 1.2f), 0.8f, smokeRot, required: true);
            GeneralParticleHandler.SpawnParticle(smoke);

            if (Main.rand.NextBool(8))
            {
                Particle glow = new HeavySmokeParticle(Projectile.Center, Projectile.velocity * 0.5f, Color.Lerp(smokeColor, Color.White, 0.25f), 6, Projectile.scale * Main.rand.NextFloat(0.4f, 0.7f), 0.6f, smokeRot, true, 0.005f, true);
                GeneralParticleHandler.SpawnParticle(glow);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 240);

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= damageMult;
            damageMult *= 0.82f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.width * Projectile.scale * 0.5f, targetHitbox);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D fog = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(0f, 0.08f, LightPower, true) * Projectile.Opacity * 0.32f;

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(fog, drawPosition, null, orangeFog * opacity, Projectile.rotation + orangeRot, fog.Size() * 0.5f, Projectile.scale * orangeScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(fog, drawPosition, null, blueFog * opacity, Projectile.rotation + blueRot, fog.Size() * 0.5f, Projectile.scale * blueScale, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
