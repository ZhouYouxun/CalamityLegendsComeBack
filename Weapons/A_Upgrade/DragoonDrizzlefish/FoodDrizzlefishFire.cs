using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.DragoonDrizzlefish
{
    public sealed class FoodDrizzlefishFire : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.DragoonDrizzlefish";
        public override string Texture => "CalamityMod/Projectiles/Ranged/DrizzlefishFire";

        private int splitTimer = 45;
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
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 90;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
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

        public override void AI()
        {
            time++;
            if (time == 1)
            {
                DrizzlefishProjectileHelpers.ApplyInitialStats(Projectile);
                Projectile.scale *= 1.5f;
                splitTimer = DrizzlefishProjectileHelpers.SplitTimer(
                    DragoonDrizzlefishMeals.GetMeal(Projectile),
                    DragoonDrizzlefishMeals.IsOverfed(Projectile));
            }

            int dustType = GetFireDust();
            int burstDustType = GetFireDust();

            if (time > 7)
            {
                Projectile.alpha = 0;
                for (int i = 0; i < 5; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(9f, 9f) - Projectile.velocity * 1.5f, dustType, -Projectile.velocity);
                    dust.noGravity = true;
                    dust.velocity *= 0f;
                    dust.scale = Main.rand.NextFloat(1.2f, 1.9f) * Projectile.scale;
                }
                DrizzlefishProjectileHelpers.SpawnMealDust(Projectile, 2, 0.95f);
            }
            else
                Projectile.alpha = 255;

            if (time == 4)
            {
                for (int i = 0; i <= 16; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, burstDustType, Projectile.velocity);
                    dust.scale = Main.rand.NextFloat(1.8f, 2.3f) * Projectile.scale;
                    dust.velocity = Projectile.velocity.RotatedByRandom(1.1f) * Main.rand.NextFloat(0.6f, 1.9f);
                    dust.noGravity = true;
                }
            }

            DrizzlefishProjectileHelpers.ApplyMealMotion(Projectile, time, 0.75f);
            splitTimer--;
            if (splitTimer <= 0)
                SplitAndDie();

            Lighting.AddLight(Projectile.Center, 0.25f, 0f, 0f);
            if (Projectile.timeLeft > 90)
                Projectile.timeLeft = 90;

            Projectile.rotation += 0.5f * Projectile.direction;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            DrizzlefishProjectileHelpers.ApplyBaseDebuff(Projectile, target, 60, 120);
            DrizzlefishProjectileHelpers.ApplyMealOnHit(Projectile, target, damageDone);
        }

        private void SplitAndDie()
        {
            DragoonDrizzlefishMealType meal = DragoonDrizzlefishMeals.GetMeal(Projectile);
            bool overfed = DragoonDrizzlefishMeals.IsOverfed(Projectile);
            int splitCount = DrizzlefishProjectileHelpers.SplitCount(meal, overfed);
            float rotation = DrizzlefishProjectileHelpers.SplitRotation(meal, overfed);

            if (Projectile.owner == Main.myPlayer)
            {
                for (int i = 0; i < splitCount; i++)
                {
                    float interpolant = splitCount <= 1 ? 0.5f : i / (float)(splitCount - 1);
                    Vector2 splitVelocity = Projectile.velocity.RotatedBy(MathHelper.Lerp(-rotation, rotation, interpolant));
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        splitVelocity,
                        ModContent.ProjectileType<FoodDrizzlefishFireSplit>(),
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner,
                        Projectile.ai[0],
                        Projectile.ai[1]);
                }
            }

            Projectile.Kill();
        }

        private int GetFireDust()
        {
            if (Projectile.ai[1] == 1f)
                return Main.rand.NextBool() ? 174 : 162;

            return Main.rand.NextBool() ? 183 : 90;
        }
    }
}
