using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.DragoonDrizzlefish
{
    public sealed class FoodDrizzlefishFireSplit : ModProjectile, ILocalizedModType
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
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.ai[1] == 1f)
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

        public override void AI()
        {
            time++;
            if (time == 1)
                DragoonDrizzlefishFoods.ApplyFishflameStats(Projectile);

            Projectile.velocity.X *= 0.98f;
            Projectile.velocity.Y += 0.5f;

            int dustType = GetFireDust();
            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f) - Projectile.velocity * 1.5f, dustType, -Projectile.velocity);
                dust.noGravity = true;
                dust.velocity *= 0f;
                dust.scale = Main.rand.NextFloat(0.4f, 0.8f) * Projectile.scale;
            }
            DragoonDrizzlefishFoods.SpawnFoodDust(Projectile, 1, 0.7f);

            Lighting.AddLight(Projectile.Center, 0.25f, 0f, 0f);
            if (Projectile.timeLeft > 90)
                Projectile.timeLeft = 90;

            Projectile.rotation += 0.3f * Projectile.direction;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            DragoonDrizzlefishFoods.ApplyBaseDebuff(Projectile, target, 30, 60);
        }

        public override void OnKill(int timeLeft)
        {
            int dustType = GetFireDust();
            for (int i = 0; i <= 9; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, dustType, new Vector2(0f, -5f).RotatedByRandom(MathHelper.ToRadians(15f)) * Main.rand.NextFloat(0.1f, 1.9f));
                dust.noGravity = false;
                dust.scale = Main.rand.NextFloat(0.4f, 1.1f) * Projectile.scale;

                Dust dust2 = Dust.NewDustPerfect(Projectile.Center, dustType, new Vector2(0f, -3f).RotatedByRandom(MathHelper.ToRadians(25f)) * Main.rand.NextFloat(0.1f, 1.9f));
                dust2.noGravity = false;
                dust2.scale = Main.rand.NextFloat(0.4f, 1.1f) * Projectile.scale;
            }
        }

        private int GetFireDust()
        {
            if (Projectile.ai[1] == 1f)
                return Main.rand.NextBool() ? 174 : 162;

            return Main.rand.NextBool() ? 183 : 90;
        }
    }
}
