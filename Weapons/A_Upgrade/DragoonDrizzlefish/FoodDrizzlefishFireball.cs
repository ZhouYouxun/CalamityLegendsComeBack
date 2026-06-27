using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.DragoonDrizzlefish
{
    public sealed class FoodDrizzlefishFireball : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.DragoonDrizzlefish";
        public override string Texture => "CalamityMod/Projectiles/Ranged/DrizzlefishFire";

        private int time;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 5;
            Projectile.aiStyle = ProjAIStyleID.GroundProjectile;
            Projectile.timeLeft = 300;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
        }

        public override void AI()
        {
            time++;
            if (time == 1)
                DrizzlefishProjectileHelpers.ApplyInitialStats(Projectile);

            Projectile.velocity.X *= 0.995f;
            Projectile.velocity.Y -= 0.065f;
            DrizzlefishProjectileHelpers.ApplyMealMotion(Projectile, time);
            Lighting.AddLight(Projectile.Center, 0.25f, 0f, 0f);

            int dustType = GetFireDust();
            int burstDustType = GetFireDust();

            if (time > 7)
            {
                Projectile.alpha = 0;
                for (int i = 0; i < 2; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f) - Projectile.velocity * 1.5f, dustType, -Projectile.velocity);
                    dust.noGravity = true;
                    dust.velocity *= 0f;
                    dust.scale = Main.rand.NextFloat(0.9f, 1.5f) * Projectile.scale;
                }
                DrizzlefishProjectileHelpers.SpawnMealDust(Projectile, 1, 0.75f);
            }
            else
                Projectile.alpha = 255;

            if (time == 4)
            {
                for (int i = 0; i <= 8; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, burstDustType, Projectile.velocity);
                    dust.scale = Main.rand.NextFloat(1.1f, 1.9f) * Projectile.scale;
                    dust.velocity = Projectile.velocity.RotatedByRandom(0.8f) * Main.rand.NextFloat(0.3f, 1.3f);
                    dust.noGravity = true;
                }
            }

            DrizzlefishProjectileHelpers.MaybeSpawnSweetChild(Projectile, time);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity *= 0.98f;
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (time < 7)
                CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1, ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj").Value);
            else if (Projectile.ai[1] == 1f)
                CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1, ModContent.Request<Texture2D>("CalamityMod/Projectiles/Ranged/DrizzlefishFire2").Value);
            else
                CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1, ModContent.Request<Texture2D>("CalamityMod/Projectiles/Ranged/DrizzlefishFire").Value);

            if (Projectile.ai[1] == 1f)
            {
                Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Ranged/DrizzlefishFire2").Value;
                Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, 16, 16), Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture.Width / 2f, 10f), Projectile.scale, SpriteEffects.None, 0);
                return false;
            }

            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            DrizzlefishProjectileHelpers.ApplyBaseDebuff(Projectile, target, 20, 40);
            DrizzlefishProjectileHelpers.ApplyMealOnHit(Projectile, target, damageDone);
        }

        private int GetFireDust()
        {
            if (Projectile.ai[1] == 1f)
                return Main.rand.NextBool() ? 174 : 162;

            return Main.rand.NextBool() ? 183 : 90;
        }
    }
}
