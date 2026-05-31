using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFMoonlord_Flame : ModProjectile, ILocalizedModType
    {
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.LunarFlare}";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 34;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 0.62f);

            if (!Main.dedServ && Main.rand.NextBool(2))
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(Projectile.Center, -Projectile.velocity * 0.06f, false, 10, Main.rand.NextFloat(0.35f, 0.65f), ThemeColor, true, false, true));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) =>
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 180);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;
            Color theme = ThemeColor with { A = 0 };
            Vector2 center = Projectile.Center - Main.screenPosition;

            PFLeftEffectRules.BeginAdditive();
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = i / (float)Projectile.oldPos.Length;
                Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(line, trailPosition, null, theme * (1f - completion) * 0.48f, Projectile.rotation, line.Size() * 0.5f, new Vector2(0.14f, 0.68f), SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, center, null, Color.Lerp(theme, Color.White with { A = 0 }, 0.44f), Projectile.rotation, texture.Size() * 0.5f, 0.72f, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
