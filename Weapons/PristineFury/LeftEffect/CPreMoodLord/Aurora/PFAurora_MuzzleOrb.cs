using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFAurora_MuzzleOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 78;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 3;
        }

        public override void AI()
        {
            Timer++;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 2;

            int holdoutIndex = (int)Projectile.ai[0];
            if (holdoutIndex < 0 || holdoutIndex >= Main.maxProjectiles)
            {
                Projectile.Kill();
                return;
            }

            Projectile holdout = Main.projectile[holdoutIndex];
            if (!holdout.active || holdout.owner != Projectile.owner || holdout.ModProjectile is not NewLegendPristineFuryHoldOut pristineHoldout)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = pristineHoldout.GunTipPosition + pristineHoldout.AimDirection * 9f;
            Projectile.rotation = pristineHoldout.Projectile.rotation;
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(126, 210, 255));
            Lighting.AddLight(Projectile.Center, theme.ToVector3() * 1.25f);

            if (Main.dedServ || Timer % 5f != 0f)
                return;

            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                Main.rand.NextVector2Circular(0.7f, 0.7f),
                false,
                Main.rand.Next(12, 18),
                Main.rand.NextFloat(0.5f, 0.9f),
                Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.18f, 0.55f)),
                true,
                true));
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, 42f, targetHitbox);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 180);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/ArchSmear").Value;
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(126, 210, 255)) with { A = 0 };
            Color white = Color.White with { A = 0 };
            Vector2 center = Projectile.Center - Main.screenPosition;
            float pulse = 0.9f + 0.1f * (float)System.Math.Sin(Timer * 0.24f);

            PFLeftEffectRules.BeginAdditive();
            for (int i = 0; i < 5; i++)
            {
                Color drawColor = Color.Lerp(theme, white, i * 0.12f) * (0.38f - i * 0.045f);
                Main.EntitySpriteDraw(bloom, center + Main.rand.NextVector2Circular(1.5f, 1.5f), null, drawColor, Main.rand.NextFloat(-5f, 5f), bloom.Size() * 0.5f, new Vector2(0.46f + i * 0.08f, 0.23f + i * 0.035f) * pulse, SpriteEffects.None, 0);
            }

            for (int i = 0; i < 4; i++)
            {
                float rotation = Projectile.rotation + MathHelper.PiOver4 * i + Timer * 0.025f;
                Main.EntitySpriteDraw(star, center, null, Color.Lerp(theme, white, 0.45f) * 0.58f, rotation, star.Size() * 0.5f, new Vector2(0.22f, 1.65f + i * 0.15f) * pulse, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(smear, center, null, theme * 0.22f, Projectile.rotation, smear.Size() * 0.5f, new Vector2(0.22f, 0.64f) * pulse, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
