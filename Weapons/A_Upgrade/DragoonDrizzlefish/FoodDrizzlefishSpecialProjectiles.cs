using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.DragoonDrizzlefish
{
    public sealed class FoodDrizzlefishFruitNeedle : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.DragoonDrizzlefish";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 7;
            Projectile.height = 7;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 118;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] > 8f)
                DragoonDrizzlefishFoods.HomeTowardTarget(Projectile, 680f, 0.1f, 17f);

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.05f, 0.26f, 0.09f);

            if (Projectile.localAI[0] % 3f == 0f)
            {
                Color fruit = Main.rand.NextBool() ? new Color(90, 255, 125) : new Color(160, 255, 90);
                DrizzlefishVFX.Bloom(Projectile.Center, -Projectile.velocity * 0.04f, fruit, 0.11f, 0.22f, 9);
                DrizzlefishVFX.Mist(Projectile.Center - Projectile.velocity * 0.4f, -Projectile.velocity * 0.08f, fruit, new Color(40, 120, 70), 0.34f, 120f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color fruit = new(95, 255, 135, 210);
            DrizzlefishVFX.DrawSoftTrail(Projectile, DrizzlefishVFX.MiniFlower, fruit, 0.21f, 0.55f);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.MiniFlower, Projectile.Center, Color.White * 0.7f, Projectile.rotation, 0.26f);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.BloomCircle, Projectile.Center, fruit * 0.45f, 0f, 0.18f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            DrizzlefishVFX.Pulse(Projectile.Center, new Color(130, 255, 120), DrizzlefishVFX.MiniFlower, 0.05f, 0.38f, 14, 0.65f);
            DrizzlefishVFX.ImpactDust(Projectile.Center, new Color(125, 255, 120), 8, 2.6f, 0.7f);
        }
    }

    public sealed class FoodDrizzlefishFishBubble : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.DragoonDrizzlefish";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 7;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 2;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 150;
            Projectile.alpha = 70;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Projectile.localAI[1]++;
            if (Projectile.localAI[0] == 0f && Projectile.wet && !Projectile.lavaWet)
                ActivateSteamHunt();

            bool steamHunt = Projectile.localAI[0] == 1f;
            if (steamHunt)
                DragoonDrizzlefishFoods.HomeTowardTarget(Projectile, 760f, 0.13f, 15f, true);

            Projectile.rotation += 0.07f * Projectile.direction;
            Color water = new(95, 220, 255);
            Color heat = new(255, 125, 55);
            Lighting.AddLight(Projectile.Center, (steamHunt ? heat : water).ToVector3() * 0.28f);

            if (Projectile.localAI[1] % 4f == 0f)
            {
                DrizzlefishVFX.BubbleParticle(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f), -Projectile.velocity * 0.06f, Main.rand.NextFloat(0.28f, 0.48f), 24);
                if (steamHunt)
                    DrizzlefishVFX.Mist(Projectile.Center, -Projectile.velocity * 0.05f + Main.rand.NextVector2Circular(0.5f, 0.5f), heat, Color.DarkSlateGray, 0.46f, 135f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            bool steamHunt = Projectile.localAI[0] == 1f;
            Color water = new(110, 230, 255, 155);
            Color heat = new(255, 135, 70, 135);
            DrizzlefishVFX.DrawSoftTrail(Projectile, DrizzlefishVFX.Bubble, water * 0.55f, 0.34f, 0.76f);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.Bubble, Projectile.Center, Color.White * 0.78f, Projectile.rotation, steamHunt ? 0.56f : 0.48f);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.BloomCircle, Projectile.Center, (steamHunt ? heat : water) * 0.5f, 0f, steamHunt ? 0.32f : 0.24f);
            if (steamHunt)
                DrizzlefishVFX.DrawTexture(DrizzlefishVFX.CircularFire, Projectile.Center, heat * 0.55f, -Projectile.rotation, 0.27f);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, Projectile.localAI[0] == 1f ? 150 : 75);
        }

        public override void OnKill(int timeLeft)
        {
            bool steamHunt = Projectile.localAI[0] == 1f;
            Color color = steamHunt ? new Color(255, 135, 75) : new Color(105, 230, 255);
            DrizzlefishVFX.Pulse(Projectile.Center, color, steamHunt ? DrizzlefishVFX.FlameExplosion : DrizzlefishVFX.WaterFoam, 0.03f, 0.26f, 15, 0.75f);
            DrizzlefishVFX.ImpactDust(Projectile.Center, color, 18, 3.5f, 1f);
        }

        private void ActivateSteamHunt()
        {
            Projectile.localAI[0] = 1f;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = System.Math.Max(Projectile.timeLeft, 110);
            SoundEngine.PlaySound(SoundID.Item85 with { Pitch = 0.25f, Volume = 0.7f }, Projectile.Center);
            DrizzlefishVFX.Pulse(Projectile.Center, new Color(255, 145, 80), DrizzlefishVFX.BloomRing, 0.05f, 0.42f, 17, 0.8f);
            DrizzlefishVFX.Pulse(Projectile.Center, Color.White * 0.55f, DrizzlefishVFX.WaterFoam, 0.04f, 0.32f, 12, 0.6f);
        }
    }

    public sealed class FoodDrizzlefishBoozeLaser : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.DragoonDrizzlefish";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const float LaserLength = 760f;
        private const float LaserWidth = 22f;

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 9;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Lighting.AddLight(Projectile.Center, 0.32f, 0.06f, 0.44f);

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                DrizzlefishVFX.Pulse(Projectile.Center, new Color(190, 80, 255), DrizzlefishVFX.BloomRing, 0.05f, 0.55f, 10, 0.55f, new Vector2(0.65f, 1f), Projectile.velocity.ToRotation());
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 point = Projectile.Center + Projectile.velocity * Main.rand.NextFloat(60f, LaserLength - 20f);
                DrizzlefishVFX.Mist(point, Main.rand.NextVector2Circular(0.6f, 0.6f), new Color(210, 105, 255), new Color(75, 30, 110), 0.42f, 105f);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 targetPosition = new(targetHitbox.X, targetHitbox.Y);
            Vector2 targetSize = new(targetHitbox.Width, targetHitbox.Height);
            return Collision.CheckAABBvLineCollision(targetPosition, targetSize, Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, LaserWidth, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color outer = new(155, 55, 255, 150);
            Color inner = new(255, 220, 255, 230);
            DrizzlefishVFX.DrawBeam(Projectile.Center, Projectile.velocity, LaserLength, LaserWidth, outer, inner);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Confused, 45);
            DrizzlefishVFX.Pulse(target.Center, new Color(205, 100, 255), DrizzlefishVFX.BloomRing, 0.03f, 0.34f, 12, 0.6f);

            if (Projectile.owner != Main.myPlayer)
                return;

            int packed = DragoonDrizzlefishFoods.Pack(DragoonDrizzlefishFoods.GetFood(Projectile), true);
            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = (Projectile.velocity * 7f).RotatedByRandom(0.65f) + Main.rand.NextVector2Circular(1.7f, 1.7f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, velocity, ModContent.ProjectileType<FoodDrizzlefishBoozeDroplet>(),
                    System.Math.Max(1, (int)(Projectile.damage * 0.32f)), Projectile.knockBack * 0.35f, Projectile.owner, packed);
            }
        }
    }

    public sealed class FoodDrizzlefishBoozeDroplet : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.DragoonDrizzlefish";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 7;
            Projectile.height = 7;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 70;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            DragoonDrizzlefishFoods.HomeTowardTarget(Projectile, 540f, 0.075f, 12f);
            Projectile.rotation += 0.22f;
            if (Projectile.timeLeft % 4 == 0)
                DrizzlefishVFX.Bloom(Projectile.Center, -Projectile.velocity * 0.03f, new Color(220, 120, 255), 0.1f, 0.19f, 8);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrizzlefishVFX.DrawSoftTrail(Projectile, DrizzlefishVFX.BloomCircle, new Color(210, 115, 255, 165), 0.12f, 0.6f);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.BloomCircle, Projectile.Center, new Color(235, 150, 255, 210), 0f, 0.16f);
            return false;
        }
    }

    public sealed class FoodDrizzlefishSnackRocket : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.DragoonDrizzlefish";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 44;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 1.004f;
            if (Projectile.localAI[0] > 28f)
                Projectile.Kill();

            Color pink = new(255, 145, 220);
            Color sugar = Main.rand.NextBool() ? pink : new Color(255, 215, 115);
            Lighting.AddLight(Projectile.Center, sugar.ToVector3() * 0.24f);

            if (Projectile.localAI[0] % 3f == 0f)
            {
                DrizzlefishVFX.Bloom(Projectile.Center - Projectile.velocity * 0.45f, -Projectile.velocity * 0.05f, sugar, 0.14f, 0.3f, 10);
                DrizzlefishVFX.Smoke(Projectile.Center - Projectile.velocity * 0.7f, -Projectile.velocity * 0.04f + Main.rand.NextVector2Circular(0.5f, 0.5f), pink, new Color(65, 35, 55), 0.36f, 70f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            DrizzlefishVFX.DrawSoftTrail(Projectile, DrizzlefishVFX.CuteStars, new Color(255, 170, 230, 150), 0.2f, 0.6f);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.BloomCircle, Projectile.Center, new Color(255, 135, 215, 160), 0f, 0.24f);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.CuteStars, Projectile.Center + direction * 2f, Color.White * 0.8f, Projectile.rotation, 0.28f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Pitch = 0.18f, Volume = 0.65f }, Projectile.Center);
            DrizzlefishVFX.Pulse(Projectile.Center, new Color(255, 150, 225), DrizzlefishVFX.SoftRoundExplosion, 0.03f, 0.42f, 18, 0.75f);
            DrizzlefishVFX.Pulse(Projectile.Center, new Color(255, 215, 115), DrizzlefishVFX.CuteStars, 0.04f, 0.36f, 18, 0.75f);
            DrizzlefishVFX.ImpactDust(Projectile.Center, new Color(255, 145, 225), 26, 7f, 1.05f);

            if (Projectile.owner != Main.myPlayer)
                return;

            int packed = DragoonDrizzlefishFoods.Pack(DragoonDrizzlefishFoods.GetFood(Projectile), true);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<FoodDrizzlefishMealBurst>(),
                System.Math.Max(1, (int)(Projectile.damage * 0.45f)), 0f, Projectile.owner, packed, 86f);

            for (int i = 0; i < 16; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4.8f, 9.5f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<FoodDrizzlefishSnackShard>(),
                    System.Math.Max(1, (int)(Projectile.damage * 0.35f)), Projectile.knockBack * 0.35f, Projectile.owner, packed);
            }
        }
    }

    public sealed class FoodDrizzlefishSnackShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.DragoonDrizzlefish";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 56;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.985f;
            Projectile.velocity.Y += 0.035f;
            Projectile.rotation += 0.4f;
            if (Projectile.timeLeft % 5 == 0)
                DrizzlefishVFX.Bloom(Projectile.Center, Vector2.Zero, new Color(255, 185, 230), 0.08f, 0.16f, 8);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.CuteStars, Projectile.Center, new Color(255, 180, 230, 210), Projectile.rotation, 0.17f);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.BloomCircle, Projectile.Center, new Color(255, 140, 210, 90), 0f, 0.12f);
            return false;
        }
    }

    public sealed class FoodDrizzlefishGoldenSeeker : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.DragoonDrizzlefish";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 150;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            DragoonDrizzlefishFoods.HomeTowardTarget(Projectile, 820f, 0.145f, 16f, true);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.45f, 0.35f, 0.06f);

            if (Projectile.timeLeft % 3 == 0)
            {
                DrizzlefishVFX.Bloom(Projectile.Center, -Projectile.velocity * 0.035f, new Color(255, 225, 75), 0.18f, 0.34f, 10);
                if (Main.rand.NextBool(4))
                    DrizzlefishVFX.Pulse(Projectile.Center, new Color(255, 210, 70) * 0.55f, DrizzlefishVFX.BloomRing, 0.04f, 0.28f, 12, 0.48f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrizzlefishVFX.DrawSoftTrail(Projectile, DrizzlefishVFX.SoftRoundExplosion, new Color(255, 210, 75, 145), 0.14f, 0.7f);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.LargeBloom, Projectile.Center, new Color(255, 205, 70, 115), 0f, 0.22f);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.SoftRoundExplosion, Projectile.Center, new Color(255, 230, 110, 205), Projectile.rotation, 0.18f);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.BloomCircle, Projectile.Center, Color.White * 0.58f, 0f, 0.11f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Pitch = 0.45f, Volume = 0.7f }, Projectile.Center);
            DrizzlefishVFX.Pulse(Projectile.Center, new Color(255, 220, 80), DrizzlefishVFX.SoftRoundExplosion, 0.05f, 0.48f, 19, 0.9f);
            DrizzlefishVFX.Pulse(Projectile.Center, Color.White * 0.65f, DrizzlefishVFX.BloomRing, 0.04f, 0.62f, 20, 0.7f);
            DrizzlefishVFX.ImpactDust(Projectile.Center, new Color(255, 225, 75), 28, 7f, 1.25f);

            if (Projectile.owner == Main.myPlayer)
            {
                int packed = DragoonDrizzlefishFoods.Pack(DragoonDrizzlefishFoods.GetFood(Projectile), true);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<FoodDrizzlefishMealBurst>(),
                    System.Math.Max(1, (int)(Projectile.damage * 0.55f)), Projectile.knockBack, Projectile.owner, packed, 104f);
            }
        }
    }

    public sealed class FoodDrizzlefishOddSpore : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.DragoonDrizzlefish";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 135;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.velocity = Projectile.velocity.RotatedBy(System.Math.Sin(Projectile.localAI[0] * 0.12f) * 0.012f);
            if (Projectile.localAI[0] > 7f)
                DragoonDrizzlefishFoods.HomeTowardTarget(Projectile, 720f, 0.11f, 13.5f, true);

            Projectile.rotation += 0.25f;
            Color color = OddColor();
            Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.24f);

            if (Projectile.localAI[0] % 3f == 0f)
            {
                DrizzlefishVFX.Mist(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), -Projectile.velocity * 0.04f, color, new Color(55, 38, 26), 0.45f, 125f);
                DrizzlefishVFX.Bloom(Projectile.Center, Vector2.Zero, color, 0.12f, 0.24f, 9);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrizzlefishVFX.DrawSoftTrail(Projectile, DrizzlefishVFX.BloomCircle, new Color(185, 115, 65, 130), 0.17f, 0.64f);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.BloomCircle, Projectile.Center, new Color(220, 150, 85, 180), 0f, 0.23f);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.Bubble, Projectile.Center, new Color(255, 215, 95, 125), Projectile.rotation, 0.32f);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.MiniFlower, Projectile.Center, new Color(205, 65, 55, 150), -Projectile.rotation, 0.22f);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Confused, 90);
        }

        public override void OnKill(int timeLeft)
        {
            Color color = OddColor();
            DrizzlefishVFX.Pulse(Projectile.Center, color, DrizzlefishVFX.BloomRing, 0.05f, 0.4f, 18, 0.7f);
            DrizzlefishVFX.ImpactDust(Projectile.Center, color, 18, 5f, 1f);
        }

        private Color OddColor()
        {
            return Main.rand.Next(3) switch
            {
                0 => new Color(210, 50, 50),
                1 => new Color(255, 215, 75),
                _ => new Color(160, 105, 70)
            };
        }
    }

    public sealed class FoodDrizzlefishFeastComet : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.DragoonDrizzlefish";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 4;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 100;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                DragoonDrizzlefishFoods.ApplyFishflameStats(Projectile);
                DrizzlefishVFX.Pulse(Projectile.Center, new Color(255, 190, 80), DrizzlefishVFX.CircularFire, 0.04f, 0.34f, 16, 0.65f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 0.998f;
            Lighting.AddLight(Projectile.Center, 0.42f, 0.24f, 0.05f);

            if (Projectile.timeLeft % 2 == 0)
            {
                Color hot = Main.rand.NextBool() ? new Color(255, 160, 70) : new Color(255, 220, 95);
                DrizzlefishVFX.Mist(Projectile.Center - Projectile.velocity * 0.25f + Main.rand.NextVector2Circular(6f, 6f), -Projectile.velocity * 0.05f, hot, new Color(80, 45, 30), 0.56f, 140f);
                if (Main.rand.NextBool(3))
                    DrizzlefishVFX.Smoke(Projectile.Center - Projectile.velocity * 0.5f, -Projectile.velocity * 0.04f, new Color(155, 95, 55), new Color(45, 35, 30), 0.45f, 90f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrizzlefishVFX.DrawSoftTrail(Projectile, DrizzlefishVFX.FlameExplosion, new Color(255, 160, 70, 130), 0.16f, 0.7f);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.CircularFire, Projectile.Center, new Color(255, 160, 70, 160), Projectile.rotation, 0.35f * Projectile.scale);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.FlameExplosion, Projectile.Center, new Color(255, 205, 90, 190), -Projectile.rotation, 0.18f * Projectile.scale);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.BloomCircle, Projectile.Center, Color.White * 0.45f, 0f, 0.13f * Projectile.scale);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 140);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.1f, Volume = 0.6f }, Projectile.Center);
            DrizzlefishVFX.Pulse(Projectile.Center, new Color(255, 185, 80), DrizzlefishVFX.FlameExplosion, 0.04f, 0.54f, 20, 0.82f);
            DrizzlefishVFX.Pulse(Projectile.Center, new Color(255, 225, 120) * 0.7f, DrizzlefishVFX.BloomRing, 0.05f, 0.62f, 20, 0.7f);
            DrizzlefishVFX.ImpactDust(Projectile.Center, new Color(255, 205, 90), 32, 7f, 1.1f);

            if (Projectile.owner != Main.myPlayer)
                return;

            int packed = DragoonDrizzlefishFoods.Pack(DragoonDrizzlefishFoods.GetFood(Projectile), true);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<FoodDrizzlefishMealBurst>(),
                System.Math.Max(1, (int)(Projectile.damage * 0.45f)), Projectile.knockBack, Projectile.owner, packed, 92f);

            for (int i = 0; i < 6; i++)
            {
                Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.TwoPi * i / 6f) * Main.rand.NextFloat(4f, 7f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<FoodDrizzlefishFireSplit>(),
                    System.Math.Max(1, (int)(Projectile.damage * 0.5f)), Projectile.knockBack * 0.5f, Projectile.owner, packed, Main.rand.Next(2));
            }
        }
    }

    public sealed class FoodDrizzlefishMealBurst : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.DragoonDrizzlefish";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] != 0f)
                return;

            Projectile.localAI[0] = 1f;
            float radius = MathHelper.Clamp(Projectile.ai[1], 52f, 124f);
            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = (int)radius;
            Projectile.Center = center;

            Color color = DragoonDrizzlefishFoods.FoodColor(DragoonDrizzlefishFoods.GetFood(Projectile));
            DrizzlefishVFX.Pulse(center, color, BurstTexture(), 0.04f, radius / 150f, 16, 0.78f);
            DrizzlefishVFX.Pulse(center, Color.White * 0.48f, DrizzlefishVFX.BloomRing, 0.04f, radius / 130f, 18, 0.62f);
            DrizzlefishVFX.ImpactDust(center, color, 24, 6f, 1.1f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color color = DragoonDrizzlefishFoods.FoodColor(DragoonDrizzlefishFoods.GetFood(Projectile)) * 0.48f;
            float scale = Projectile.width / 130f;
            DrizzlefishVFX.DrawTexture(BurstTexture(), Projectile.Center, color, Projectile.rotation, scale);
            DrizzlefishVFX.DrawTexture(DrizzlefishVFX.BloomRing, Projectile.Center, Color.White * 0.25f, -Projectile.rotation, scale * 0.8f);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            DragoonDrizzlefishFoodType food = DragoonDrizzlefishFoods.GetFood(Projectile);
            if (food == DragoonDrizzlefishFoodType.OddMushroom || food == DragoonDrizzlefishFoodType.Alcohol)
                target.AddBuff(BuffID.Confused, 60);
            else
                target.AddBuff(BuffID.OnFire3, 80);
        }

        private string BurstTexture()
        {
            return DragoonDrizzlefishFoods.GetFood(Projectile) switch
            {
                DragoonDrizzlefishFoodType.Snack => DrizzlefishVFX.CuteStars,
                DragoonDrizzlefishFoodType.Superfood => DrizzlefishVFX.SoftRoundExplosion,
                DragoonDrizzlefishFoodType.Fish => DrizzlefishVFX.WaterFoam,
                DragoonDrizzlefishFoodType.OddMushroom => DrizzlefishVFX.BloomCircle,
                _ => DrizzlefishVFX.FlameExplosion
            };
        }
    }

    internal static class DrizzlefishVFX
    {
        internal const string BloomCircle = "CalamityMod/Particles/BloomCircle";
        internal const string BloomRing = "CalamityMod/Particles/BloomRing";
        internal const string Bubble = "CalamityMod/Particles/Bubble";
        internal const string CircularFire = "CalamityMod/Particles/CircularSmearFire1";
        internal const string CuteStars = "CalamityMod/Particles/CuteStars";
        internal const string FlameExplosion = "CalamityMod/Particles/FlameExplosion";
        internal const string LargeBloom = "CalamityMod/Particles/LargeBloom";
        internal const string MiniFlower = "CalamityMod/Particles/MiniFlower";
        internal const string SoftRoundExplosion = "CalamityMod/Particles/SoftRoundExplosion";
        internal const string WaterFoam = "CalamityMod/Particles/WaterFoam";

        private const string BeamBody = "CalamityMod/Particles/BloomLineSoftEdge";

        internal static void Bloom(Vector2 position, Vector2 velocity, Color color, float originalScale, float finalScale, int lifetime)
        {
            GeneralParticleHandler.SpawnParticle(new BloomParticle(position, velocity, color, originalScale, finalScale, lifetime));
        }

        internal static void BubbleParticle(Vector2 position, Vector2 velocity, float scale, int lifetime)
        {
            GeneralParticleHandler.SpawnParticle(new GenericBubbleParticle(position, velocity, scale, Main.rand.NextFloat(MathHelper.TwoPi), lifetime));
        }

        internal static void Mist(Vector2 position, Vector2 velocity, Color hotColor, Color fadeColor, float scale, float opacity)
        {
            GeneralParticleHandler.SpawnParticle(new MediumMistParticle(position, velocity, hotColor, fadeColor, scale, opacity, Main.rand.NextFloat(-0.08f, 0.08f)));
        }

        internal static void Smoke(Vector2 position, Vector2 velocity, Color hotColor, Color fadeColor, float scale, float opacity)
        {
            GeneralParticleHandler.SpawnParticle(new SmallSmokeParticle(position, velocity, hotColor, fadeColor, scale, opacity, Main.rand.NextFloat(-0.05f, 0.05f)));
        }

        internal static void Pulse(Vector2 position, Color color, string texture, float originalScale, float finalScale, int lifetime, float opacity, Vector2? squish = null, float rotation = 0f)
        {
            GeneralParticleHandler.SpawnParticle(new CustomPulse(position, Vector2.Zero, color, texture, squish ?? Vector2.One, rotation == 0f ? Main.rand.NextFloat(MathHelper.TwoPi) : rotation, originalScale, finalScale, lifetime, true, opacity));
        }

        internal static void ImpactDust(Vector2 center, Color color, int count, float speed, float scale)
        {
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, DustID.RainbowMk2, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(speed * 0.35f, speed), 0, color, Main.rand.NextFloat(scale * 0.75f, scale * 1.25f));
                dust.noGravity = true;
            }
        }

        internal static void DrawTexture(string path, Vector2 position, Color color, float rotation, float scale)
        {
            Texture2D texture = ModContent.Request<Texture2D>(path).Value;
            Main.EntitySpriteDraw(texture, position - Main.screenPosition, null, color, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }

        internal static void DrawSoftTrail(Projectile projectile, string texturePath, Color color, float scale, float opacity)
        {
            for (int i = 1; i < projectile.oldPos.Length; i++)
            {
                Vector2 oldCenter = projectile.oldPos[i] + projectile.Size * 0.5f;
                if (oldCenter == projectile.Size * 0.5f)
                    continue;

                float fade = (1f - i / (float)projectile.oldPos.Length) * opacity;
                DrawTexture(texturePath, oldCenter, color * fade, projectile.rotation, scale * (0.45f + fade));
            }
        }

        internal static void DrawBeam(Vector2 start, Vector2 direction, float length, float width, Color outer, Color inner)
        {
            direction = direction.SafeNormalize(Vector2.UnitX);
            Texture2D body = ModContent.Request<Texture2D>(BeamBody).Value;
            Vector2 scale = new(length / body.Width, width / body.Height);
            Main.EntitySpriteDraw(body, start - Main.screenPosition, null, outer, direction.ToRotation(), new Vector2(0f, body.Height * 0.5f), scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(body, start - Main.screenPosition, null, inner, direction.ToRotation(), new Vector2(0f, body.Height * 0.5f), new Vector2(length / body.Width, width * 0.28f / body.Height), SpriteEffects.None, 0);

            DrawTexture(BloomCircle, start, outer * 0.75f, 0f, width / 70f);
            DrawTexture(BloomCircle, start + direction * length, outer * 0.5f, 0f, width / 95f);
        }
    }
}
