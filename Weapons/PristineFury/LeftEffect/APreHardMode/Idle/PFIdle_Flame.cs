using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFIdle_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 22;
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 2;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Color color = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 146, 62));
            Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.42f);

            if (Main.dedServ || Timer % 2f != 0f)
                return;

            Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                Projectile.Center + backward * Main.rand.NextFloat(1f, 7f),
                backward * Main.rand.NextFloat(1f, 2.4f),
                "CalamityMod/Particles/BloomLineSoftEdge",
                false,
                6,
                Main.rand.NextFloat(0.022f, 0.038f),
                Color.Lerp(color, Color.White, 0.25f),
                new Vector2(0.35f, 1.25f),
                glowCenter: true,
                shrinkSpeed: 0.9f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(ModContent.BuffType<HolyFlames>(), 120);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;
            Color color = (PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 146, 62)) with { A = 0 }) * Projectile.Opacity;
            float opacity = Utils.GetLerpValue(0f, 4f, Timer, true) * Utils.GetLerpValue(0f, 8f, Projectile.timeLeft, true);

            PFLeftEffectRules.BeginAdditive();
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Vector2 trailDirection = i == 0
                    ? Projectile.Center - (Projectile.oldPos[i] + Projectile.Size * 0.5f)
                    : Projectile.oldPos[i - 1] - Projectile.oldPos[i];
                float trailRotation = trailDirection.SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)).ToRotation() + MathHelper.PiOver2;
                Main.EntitySpriteDraw(line, drawPosition, null, color * opacity * (1f - completion), trailRotation, line.Size() * 0.5f, new Vector2(0.13f, 0.58f * (1f - completion)), SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, Color.Lerp(color, Color.White with { A = 0 }, 0.35f) * opacity * 0.58f, Projectile.rotation, bloom.Size() * 0.5f, new Vector2(0.12f, 0.075f), SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
