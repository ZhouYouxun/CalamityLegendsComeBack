using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.RightGeneral
{
    internal sealed class YC_DroneShot : ModProjectile, ILocalizedModType
    {
        private static readonly Color ShotGold = new(255, 220, 88);
        private static readonly Color ShotOrange = new(255, 104, 36);

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 60;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, ShotGold.ToVector3() * 0.35f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(new BalanceYharimsCrystal().GetFireDebuffType(), 120);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                Projectile.Center,
                Projectile.velocity * 0.15f,
                false, 8, 0.06f,
                ShotGold,
                new Vector2(1.3f, 1.3f),
                true));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D streak = ModContent.Request<Texture2D>("CalamityMod/Particles/FadeStreak").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float speed = Projectile.velocity.Length();
            float fade = Utils.GetLerpValue(0f, 5f, Timer, true) * Utils.GetLerpValue(0f, 8f, Projectile.timeLeft, true);

            Color goldAdditive = ShotGold with { A = 0 };
            Color whiteAdditive = Color.White with { A = 0 };

            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float trailAlpha = (1f - (float)i / Projectile.oldPos.Length) * fade;
                float trailWidth = MathHelper.Lerp(speed * 0.18f, speed * 0.04f, (float)i / Projectile.oldPos.Length);
                Main.EntitySpriteDraw(streak, trailPos, null, goldAdditive * trailAlpha * 0.48f, Projectile.rotation, streak.Size() * 0.5f, new Vector2(trailWidth, 0.13f), SpriteEffects.None);
            }

            Main.EntitySpriteDraw(streak, drawPosition - direction * 5f, null, goldAdditive * fade, Projectile.rotation, streak.Size() * 0.5f, new Vector2(speed * 0.22f, 0.2f), SpriteEffects.None);
            Main.EntitySpriteDraw(streak, drawPosition, null, whiteAdditive * 0.55f * fade, Projectile.rotation, streak.Size() * 0.5f, new Vector2(speed * 0.1f, 0.09f), SpriteEffects.None);

            Main.EntitySpriteDraw(bloom, drawPosition + direction * 4f, null, goldAdditive * 0.72f * fade, 0f, bloom.Size() * 0.5f, 0.22f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPosition + direction * 6f, null, whiteAdditive * 0.45f * fade, 0f, bloom.Size() * 0.5f, 0.1f, SpriteEffects.None);

            return false;
        }
    }
}
