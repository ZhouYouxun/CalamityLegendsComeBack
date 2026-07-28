using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.Skill
{
    internal sealed class BBDrinkingFountainOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private float Timer => Projectile.localAI[0];
        private bool IsHealing => Owner.statLife < Owner.statLifeMax2;
        private Player Owner => Main.player[Projectile.owner];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 1;
        }

        public override bool? CanDamage() => IsHealing ? false : null;

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Vector2 destination = Owner.Center;
            if (!IsHealing)
            {
                NPC target = Projectile.Center.ClosestNPCAt(760f);
                if (target is not null)
                    destination = target.Center;
            }

            Vector2 desiredVelocity = Projectile.SafeDirectionTo(destination) * 13f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.12f);
            Projectile.rotation += 0.22f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.1f, 0.42f, 0.62f));

            if (IsHealing && Projectile.Hitbox.Intersects(Owner.Hitbox))
            {
                if (Main.myPlayer == Projectile.owner)
                    Owner.Heal(8);
                Projectile.Kill();
                return;
            }

            if (!Main.dedServ && (int)Timer % 7 == 0)
            {
                Color glow = Color.Lerp(new Color(105, 227, 255), Color.White, Main.rand.NextFloat(0.15f, 0.45f));
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center, -Projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.25f, 0.9f),
                    false, Main.rand.Next(10, 16), Main.rand.NextFloat(0.16f, 0.28f), glow, true, false, true));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D core = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/TinyGreyscaleCircle").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color paleBlue = new(126, 232, 255, 0);
            Vector2 bloomOrigin = bloom.Size() * 0.5f;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float completion = i / (float)Projectile.oldPos.Length;
                float opacity = 1f - completion;
                Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(core, trailPosition, null, paleBlue * opacity * 0.42f, Projectile.rotation,
                    core.Size() * 0.5f, MathHelper.Lerp(0.22f, 0.56f, opacity), SpriteEffects.None);
                Main.EntitySpriteDraw(bloom, trailPosition, null, paleBlue * opacity * 0.16f, Projectile.rotation,
                    bloomOrigin, 0.13f + opacity * 0.12f, SpriteEffects.None);
            }

            Vector2 position = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bloom, position, null, paleBlue * 0.52f, Projectile.rotation,
                bloomOrigin, 0.30f, SpriteEffects.None);
            Main.EntitySpriteDraw(core, position, null, Color.White with { A = 0 } * 0.86f, Projectile.rotation,
                core.Size() * 0.5f, 0.68f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
