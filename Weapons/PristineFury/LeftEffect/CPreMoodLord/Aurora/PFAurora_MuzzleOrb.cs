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

            int holdoutIndex = (int)Projectile.ai[0];
            if (holdoutIndex < 0 || holdoutIndex >= Main.maxProjectiles)
            {
                Projectile.Kill();
                return;
            }

            Projectile holdout = Main.projectile[holdoutIndex];
            if (!holdout.active ||
                holdout.owner != Projectile.owner ||
                holdout.ModProjectile is not NewLegendPristineFuryHoldOut pristineHoldout ||
                pristineHoldout.CurrentMark != PristineFuryMark.Aurora)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = pristineHoldout.GunTipPosition + pristineHoldout.AimDirection * 9f;
            Projectile.velocity = pristineHoldout.AimDirection;
            Projectile.rotation = pristineHoldout.AimDirection.ToRotation();
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
            Texture2D archSmear = ModContent.Request<Texture2D>("CalamityMod/Particles/ArchSmear").Value;
            Texture2D orb = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Projectile.rotation.ToRotationVector2());
            float fxRot = direction.ToRotation() + MathHelper.PiOver2;
            float sine = (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 55.5f / MathHelper.Pi);
            Vector2 tipCenter = Projectile.Center;

            PFLeftEffectRules.BeginAdditive();
            for (int i = 0; i < 6; i++)
            {
                Color tipColor = Color.Lerp(Color.Cyan, Color.Orchid, (i + 4) / 6f) with { A = 0 };
                tipColor *= 0.3f;
                Vector2 scale = new(0.25f - i * 0.04f, 1.5f + i * 0.15f);
                scale *= Main.rand.NextFloat(0.9f, 1.1f);

                Main.EntitySpriteDraw(
                    archSmear,
                    tipCenter - Main.screenPosition + direction * 10f,
                    null,
                    tipColor,
                    fxRot,
                    archSmear.Size() * 0.5f,
                    scale,
                    SpriteEffects.None,
                    0);
            }

            for (int i = 0; i < 6; i++)
            {
                Color orbColor = Color.Lerp(Color.Lerp(Color.Cyan, Color.Orchid, (i + 2) / 6f), Color.White, i / 6f) with { A = 0 };
                orbColor *= 0.5f;
                Vector2 scale = new Vector2(System.Math.Abs(sine * 0.5f) + 0.1f, 1f) * (0.05f + i * 0.01f) * Main.rand.NextFloat(0.9f, 1.1f) * 2f;

                Main.EntitySpriteDraw(
                    orb,
                    tipCenter - Main.screenPosition + direction * 22f,
                    null,
                    orbColor,
                    Main.rand.NextFloat(-5f, 5f),
                    orb.Size() * 0.5f,
                    scale,
                    SpriteEffects.None,
                    0);
            }
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
