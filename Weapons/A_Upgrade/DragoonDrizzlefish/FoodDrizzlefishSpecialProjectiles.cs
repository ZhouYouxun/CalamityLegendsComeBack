using System;
using CalamityMod;
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

            if (Projectile.localAI[0] == 1f)
                DrizzlefishVFX.Pulse(Projectile.Center, new Color(105, 255, 145), DrizzlefishVFX.BloomRing, 0.05f, 0.28f, 14, 0.7f);

            if (Projectile.localAI[0] % 2f == 0f)
            {
                Color fruit = Main.rand.NextBool() ? new Color(90, 255, 125) : new Color(160, 255, 90);
                DrizzlefishVFX.SpawnFruitHelix(Projectile, fruit);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrizzlefishVFX.DrawFruitRecoveryOrb(Projectile, new Color(95, 255, 135));
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
            Projectile.penetrate = 3;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 150;
            Projectile.alpha = 70;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
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

            if (Projectile.localAI[1] % 2f == 0f)
            {
                DrizzlefishVFX.SpawnBubbleWake(Projectile, steamHunt ? heat : water, steamHunt);
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
            DrizzlefishVFX.PulseSized(Projectile.Center, color, steamHunt ? DrizzlefishVFX.FlameExplosion : DrizzlefishVFX.WaterFoam, 12f, 52f, 15, 0.75f);
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
        private const float LaserWidth = 18f;

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 10;
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

            // HalleysComet's signature is a beam built from short, heavily stretched
            // sparks. Recreate that visual language across the whole purple beam.
            DrizzlefishVFX.SpawnBoozeLaserSparks(Projectile.Center, Projectile.velocity, LaserLength);
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
            DrizzlefishVFX.DrawBoozeLaser(Projectile.Center, Projectile.velocity, LaserLength, LaserWidth);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Confused, 45);
            DrizzlefishVFX.SpawnBoozeHitRosette(target.Center, Projectile.velocity);

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

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

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
            DrizzlefishVFX.DrawSnackFlame(Projectile);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Pitch = 0.18f, Volume = 0.65f }, Projectile.Center);
            DrizzlefishVFX.Pulse(Projectile.Center, new Color(255, 150, 225), DrizzlefishVFX.BloomCircle, 0.08f, 0.46f, 18, 0.75f);
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

        private const int HomingDelay = 18;
        private const float HomingRange = 900f;
        private const float HomingInertia = 30f;
        private const float MaxSpeed = 17f;
        private ref float Timer => ref Projectile.localAI[0];

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
            Projectile.timeLeft = 180;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Timer++;
            HomeTowardTargetInertially();
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.45f, 0.35f, 0.06f);

            if ((int)Timer % 10 == 0)
            {
                DrizzlefishVFX.SpawnGoldenFlightParticles(Projectile);
                global::CalamityLegendsComeBack.CLCBLightingBoltsSystem.Spawn_DrizzlefishGoldenStarfield(Projectile.Center, Projectile.velocity, 0.55f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrizzlefishVFX.DrawGoldenSeeker(Projectile, Timer);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Pitch = 0.45f, Volume = 0.7f }, Projectile.Center);
            DrizzlefishVFX.SpawnGoldenImpactParticles(Projectile.Center, Projectile.velocity);
            global::CalamityLegendsComeBack.CLCBLightingBoltsSystem.Spawn_DrizzlefishGoldenStarfield(Projectile.Center, Projectile.velocity, 1.25f);

            if (Projectile.owner == Main.myPlayer)
            {
                int packed = DragoonDrizzlefishFoods.Pack(DragoonDrizzlefishFoods.GetFood(Projectile), true);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<FoodDrizzlefishMealBurst>(),
                    System.Math.Max(1, (int)(Projectile.damage * 0.55f)), Projectile.knockBack, Projectile.owner, packed, 104f);
            }
        }

        private void HomeTowardTargetInertially()
        {
            if (Timer <= HomingDelay)
            {
                float drift = (float)Math.Sin((Timer + Projectile.identity * 5f) * 0.08f) * 0.0045f;
                Projectile.velocity = Projectile.velocity.RotatedBy(drift) * 0.997f;
                return;
            }

            NPC target = DragoonDrizzlefishFoods.FindTarget(Projectile, HomingRange, ignoreTiles: true);
            if (target is null)
            {
                Projectile.velocity *= 0.994f;
                return;
            }

            Vector2 current = Projectile.velocity;
            if (current.LengthSquared() < 0.01f)
                current = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 5f;

            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(current.SafeNormalize(Vector2.UnitX));
            float warmup = Utils.GetLerpValue(HomingDelay, HomingDelay + 42f, Timer, true);
            float nearTarget = Utils.GetLerpValue(330f, 70f, Projectile.Distance(target.Center), true);
            float pull = MathHelper.Lerp(0.3f, 1f, MathHelper.Max(warmup, nearTarget * 0.7f));
            Vector2 desired = desiredDirection * MathHelper.Lerp(10.5f, MaxSpeed, pull);

            Projectile.velocity = (current * HomingInertia + desired) / (HomingInertia + 1f);
            float sway = (float)Math.Sin((Timer + Projectile.identity * 7f) * 0.072f) * MathHelper.Lerp(0.009f, 0.003f, pull);
            Projectile.velocity = Projectile.velocity.RotatedBy(sway);
            if (Projectile.velocity.Length() > MaxSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(desiredDirection) * MaxSpeed;
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
                Projectile.localAI[1] = Math.Max(5f, Projectile.velocity.Length());
                DragoonDrizzlefishFoods.ApplyFishflameStats(Projectile);
                DrizzlefishVFX.Pulse(Projectile.Center, new Color(105, 200, 255), DrizzlefishVFX.BloomRing, 0.05f, 0.34f, 16, 0.65f);
            }

            // Gravity curves the comet downward, while normalization preserves the initial
            // launch speed instead of turning it into a slow falling fireball.
            Projectile.velocity.Y += 0.16f;
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * Projectile.localAI[1];
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.14f, 0.34f, 0.62f);

            if (Projectile.timeLeft % 2 == 0)
            {
                Color starlight = Main.rand.NextBool() ? new Color(90, 190, 255) : new Color(175, 230, 255);
                DrizzlefishVFX.SpawnCometWake(Projectile, starlight);
                if (Main.rand.NextBool(3))
                    DrizzlefishVFX.Smoke(Projectile.Center - Projectile.velocity * 0.5f, -Projectile.velocity * 0.04f, new Color(90, 160, 235), new Color(24, 46, 92), 0.36f, 90f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrizzlefishVFX.DrawBlueComet(Projectile);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 140);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.1f, Volume = 0.6f }, Projectile.Center);
            DrizzlefishVFX.Pulse(Projectile.Center, new Color(95, 195, 255), DrizzlefishVFX.BloomCircle, 0.08f, 0.58f, 20, 0.82f);
            DrizzlefishVFX.Pulse(Projectile.Center, Color.White * 0.7f, DrizzlefishVFX.BloomRing, 0.05f, 0.62f, 20, 0.7f);
            DrizzlefishVFX.ImpactDust(Projectile.Center, new Color(110, 205, 255), 26, 7f, 1.1f);

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
            DrizzlefishVFX.PulseSized(center, color, BurstTexture(), radius * 0.18f, radius * 0.95f, 16, 0.78f);
            DrizzlefishVFX.Pulse(center, Color.White * 0.48f, DrizzlefishVFX.BloomRing, 0.04f, radius / 130f, 18, 0.62f);
            DrizzlefishVFX.ImpactDust(center, color, 24, 6f, 1.1f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color color = DragoonDrizzlefishFoods.FoodColor(DragoonDrizzlefishFoods.GetFood(Projectile)) * 0.48f;
            DrizzlefishVFX.DrawTextureSized(BurstTexture(), Projectile.Center, color, Projectile.rotation, Projectile.width * 0.95f);
            DrizzlefishVFX.DrawTextureSized(DrizzlefishVFX.BloomRing, Projectile.Center, Color.White * 0.25f, -Projectile.rotation, Projectile.width * 0.72f);
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
                DragoonDrizzlefishFoodType.Superfood => DrizzlefishVFX.BloomRing,
                DragoonDrizzlefishFoodType.Fish => DrizzlefishVFX.WaterFoam,
                DragoonDrizzlefishFoodType.OddMushroom => DrizzlefishVFX.BloomCircle,
                _ => DrizzlefishVFX.BloomCircle
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

        internal static void PulseSized(Vector2 position, Color color, string texture, float originalSize, float finalSize, int lifetime, float opacity, Vector2? squish = null, float rotation = 0f)
        {
            float textureSize = TextureSize(texture);
            Pulse(position, color, texture, originalSize / textureSize, finalSize / textureSize, lifetime, opacity, squish, rotation);
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
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(texture, position - Main.screenPosition, null, color, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        internal static void DrawTextureSized(string path, Vector2 position, Color color, float rotation, float pixelSize)
        {
            DrawTexture(path, position, color, rotation, pixelSize / TextureSize(path));
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

        internal static void DrawSoftTrailSized(Projectile projectile, string texturePath, Color color, float pixelSize, float opacity)
        {
            DrawSoftTrail(projectile, texturePath, color, pixelSize / TextureSize(texturePath), opacity);
        }

        internal static void DrawFruitRecoveryOrb(Projectile projectile, Color color)
        {
            Texture2D bloom = ModContent.Request<Texture2D>(BloomCircle).Value;
            float velocity = projectile.velocity.Length();
            Vector2 squash = new(Utils.Remap(velocity, 5f, 16f, 1f, 0.64f, true), Utils.Remap(velocity, 5f, 16f, 1f, 1.85f, true));
            float fade = (float)Math.Pow(Utils.GetLerpValue(0f, 28f, projectile.timeLeft, true), 4f);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f + projectile.identity * 0.31f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < 7; i++)
            {
                Color layer = Color.Lerp(color, Color.White, i * 0.08f) with { A = 0 };
                Vector2 scale = projectile.scale * fade * squash * (0.07f + i * 0.012f) * (2.8f + pulse * 0.25f);
                Main.EntitySpriteDraw(bloom, projectile.Center - Main.screenPosition, null, layer * (0.38f + pulse * 0.09f), projectile.rotation, bloom.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        internal static void SpawnFruitHelix(Projectile projectile, Color color)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float phase = projectile.localAI[0] * 0.42f + projectile.identity * 0.7f;
            for (int strand = 0; strand < 2; strand++)
            {
                float angle = phase + strand * MathHelper.Pi;
                Vector2 orbit = side * (float)Math.Sin(angle) * 8f + forward * (float)Math.Cos(angle) * 3f;
                Vector2 velocity = -forward * Main.rand.NextFloat(0.55f, 1.15f) + side * (float)Math.Cos(angle) * 0.85f;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    projectile.Center + orbit,
                    velocity,
                    "CalamityMod/Particles/DualTrail",
                    false,
                    13,
                    0.075f * projectile.scale,
                    Color.Lerp(color, Color.White, 0.2f),
                    new Vector2(0.72f, 1.8f),
                    true,
                    false,
                    shrinkSpeed: 0.23f));
            }

            if (((int)projectile.localAI[0] & 1) == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    projectile.Center - forward * 4f + Main.rand.NextVector2Circular(3f, 3f),
                    -forward * 0.35f + Main.rand.NextVector2Circular(0.24f, 0.24f),
                    false, 13, 0.3f, Color.Lerp(color, Color.White, 0.35f), true, false, true));
            }
        }

        internal static void SpawnBubbleWake(Projectile projectile, Color color, bool steamHunt)
        {
            if (Main.dedServ)
                return;

            Vector2 wakeVelocity = -projectile.velocity * 0.045f;
            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(6f, 6f);
                BubbleParticle(projectile.Center + offset, wakeVelocity + Main.rand.NextVector2Circular(0.25f, 0.25f), Main.rand.NextFloat(0.26f, 0.44f), Main.rand.Next(18, 28));
                GeneralParticleHandler.SpawnParticle(new WaterFlavoredParticle(
                    projectile.Center + offset,
                    wakeVelocity + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    false, Main.rand.Next(18, 27), Main.rand.NextFloat(0.38f, 0.58f), color));
            }

            GeneralParticleHandler.SpawnParticle(new WaterFoamParticle(
                projectile.Center - projectile.velocity * 0.08f,
                wakeVelocity + Main.rand.NextVector2Circular(0.2f, 0.2f),
                Main.rand.Next(16, 23), Main.rand.NextFloat(0.32f, 0.48f), Color.Lerp(color, Color.White, 0.32f)));

            Color mistFade = steamHunt ? Color.DarkSlateGray : new Color(26, 74, 110);
            Mist(projectile.Center, wakeVelocity + Main.rand.NextVector2Circular(0.45f, 0.45f), color, mistFade, steamHunt ? 0.42f : 0.3f, steamHunt ? 125f : 90f);
        }

        internal static void SpawnBoozeLaserSparks(Vector2 start, Vector2 direction, float length)
        {
            if (Main.dedServ)
                return;

            direction = direction.SafeNormalize(Vector2.UnitX);
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            const int segments = 6;
            for (int i = 1; i <= segments; i++)
            {
                float progress = i / (float)(segments + 1);
                Vector2 point = start + direction * length * progress + side * Main.rand.NextFloat(-2f, 2f);
                float scale = MathHelper.Lerp(1.8f, 2.7f, 1f - Math.Abs(progress * 2f - 1f));
                GeneralParticleHandler.SpawnParticle(new SparkParticle(point, direction * 44f, false, 7, scale, new Color(110, 35, 220)));
                GeneralParticleHandler.SpawnParticle(new SparkParticle(point, direction * 37f, false, 7, scale * 0.48f, new Color(255, 215, 255)));
            }
        }

        internal static void DrawBoozeLaser(Vector2 start, Vector2 direction, float length, float width)
        {
            Color outer = new(110, 34, 220, 155);
            Color middle = new(205, 90, 255, 205);
            DrawBeam(start, direction, length, width, outer, middle);
            DrawBeam(start, direction, length, width * 0.32f, new Color(245, 175, 255, 135), Color.White * 0.86f);
        }

        internal static void SpawnBoozeHitRosette(Vector2 center, Vector2 beamVelocity)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = beamVelocity.SafeNormalize(Vector2.UnitX);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, new Color(205, 95, 255), new Vector2(0.72f, 1.55f), forward.ToRotation(), 0.035f, 0.42f, 18));
            Pulse(center, Color.White * 0.68f, BloomRing, 0.05f, 0.36f, 15, 0.8f);

            const int arms = 8;
            for (int i = 0; i < arms; i++)
            {
                float angle = forward.ToRotation() + MathHelper.TwoPi * i / arms;
                Vector2 radial = angle.ToRotationVector2();
                Vector2 tangent = radial.RotatedBy(MathHelper.PiOver2) * (i % 2 == 0 ? 1f : -1f);
                Color color = i % 2 == 0 ? new Color(230, 110, 255) : new Color(255, 220, 255);
                GeneralParticleHandler.SpawnParticle(new LineParticle(center + radial * 5f, radial * Main.rand.NextFloat(2f, 4.2f) + tangent * 0.9f, false, Main.rand.Next(10, 16), Main.rand.NextFloat(0.14f, 0.22f), color));
                if (i % 2 == 0)
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(center + radial * 8f, radial * 1.2f, false, 16, 0.38f, color, true, false, true));
            }

            ImpactDust(center, new Color(215, 115, 255), 10, 3.6f, 0.75f);
        }

        internal static void DrawSnackFlame(Projectile projectile)
        {
            Texture2D flame = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/TinyGreyscaleCircle").Value;
            Texture2D bloom = ModContent.Request<Texture2D>(BloomCircle).Value;
            Color pink = new Color(255, 130, 220) * projectile.Opacity;
            float bloomPower = 0.9f + 0.18f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 9f + projectile.identity);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float completion = i / (float)projectile.oldPos.Length;
                Vector2 position = projectile.oldPos[i] + projectile.Size * 0.5f - Main.screenPosition;
                Color trailColor = Color.Lerp(pink, Color.Transparent, completion) * (1f - completion);
                float scale = projectile.scale * MathHelper.Lerp(0.38f, 1.05f, 1f - completion) * 0.54f;
                Main.EntitySpriteDraw(flame, position, null, trailColor, projectile.rotation, flame.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }

            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(flame, drawPosition, null, Color.Lerp(pink, Color.White, 0.16f), projectile.rotation, flame.Size() * 0.5f, projectile.scale * 0.7f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, drawPosition, null, pink * 0.5f, projectile.rotation, bloom.Size() * 0.5f, projectile.scale * bloomPower * 0.32f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, drawPosition, null, Color.White * 0.32f, -projectile.rotation * 0.5f, bloom.Size() * 0.5f, projectile.scale * bloomPower * 0.15f, SpriteEffects.None, 0);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        internal static void SpawnCometWake(Projectile projectile, Color color)
        {
            if (Main.dedServ)
                return;

            Vector2 velocity = -projectile.velocity * Main.rand.NextFloat(0.08f, 0.16f);
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(projectile.Center - projectile.velocity * 0.35f + Main.rand.NextVector2Circular(5f, 5f), velocity, false, Main.rand.Next(10, 16), Main.rand.NextFloat(0.32f, 0.52f), color, true, false, true));
            GeneralParticleHandler.SpawnParticle(new SparkParticle(projectile.Center - projectile.velocity * 0.5f, velocity * 1.7f, false, 9, 0.7f, Color.Lerp(color, Color.White, 0.32f)));
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.Frost, velocity, 80, color, Main.rand.NextFloat(0.55f, 0.85f));
                dust.noGravity = true;
            }
        }

        internal static void DrawBlueComet(Projectile projectile)
        {
            Texture2D meteor = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/CometQuasherMeteor").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;
            Texture2D bloom = ModContent.Request<Texture2D>(BloomCircle).Value;
            Color cometColor = new Color(115, 205, 255, 0);

            // Keep CometQuasherMeteor's actual meteor sprite and afterimage language.
            CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], cometColor, 1, meteor);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Vector2 center = projectile.Center - Main.screenPosition;
            float pulse = 1f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7f + projectile.identity) * 0.08f;
            Main.EntitySpriteDraw(star, center, null, new Color(115, 210, 255, 0) * 0.56f, projectile.rotation, star.Size() * 0.5f, new Vector2(0.62f, 0.2f) * projectile.scale * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(star, center, null, Color.White * 0.28f, projectile.rotation + MathHelper.PiOver2, star.Size() * 0.5f, new Vector2(0.33f, 0.09f) * projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, center, null, new Color(80, 185, 255, 0) * 0.32f, 0f, bloom.Size() * 0.5f, 0.18f * projectile.scale * pulse, SpriteEffects.None, 0);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        internal static void DrawGoldenSeeker(Projectile projectile, float timer)
        {
            Texture2D bloom = ModContent.Request<Texture2D>(BloomCircle).Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;
            Color gold = new Color(255, 205, 55, 0);
            float pulse = 1f + (float)Math.Sin(timer * 0.22f + projectile.identity) * 0.1f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = i / (float)projectile.oldPos.Length;
                Vector2 position = projectile.oldPos[i] + projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(bloom, position, null, gold * (0.06f + 0.17f * (1f - completion)), 0f, bloom.Size() * 0.5f, MathHelper.Lerp(0.05f, 0.15f, 1f - completion), SpriteEffects.None, 0);
            }

            Vector2 center = projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bloom, center, null, gold * 0.52f, 0f, bloom.Size() * 0.5f, 0.22f * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, center, null, Color.White * 0.38f, 0f, bloom.Size() * 0.5f, 0.09f * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(star, center, null, new Color(255, 232, 120, 0) * 0.76f, projectile.rotation, star.Size() * 0.5f, new Vector2(0.45f, 0.12f) * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(star, center, null, Color.White * 0.35f, projectile.rotation + MathHelper.PiOver2, star.Size() * 0.5f, new Vector2(0.25f, 0.065f), SpriteEffects.None, 0);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        internal static void SpawnGoldenFlightParticles(Projectile projectile)
        {
            if (Main.dedServ)
                return;

            Vector2 behind = -projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color gold = new Color(255, 215, 65);
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(projectile.Center + Main.rand.NextVector2Circular(4f, 4f), behind * Main.rand.NextFloat(0.45f, 1.15f), false, Main.rand.Next(11, 16), Main.rand.NextFloat(0.3f, 0.48f), gold, true, false, true));
            GeneralParticleHandler.SpawnParticle(new SparkParticle(projectile.Center - projectile.velocity * 0.2f, behind * 2.2f, false, 10, 0.72f, Color.Lerp(gold, Color.White, 0.45f)));
            Bloom(projectile.Center, behind * 0.22f, gold, 0.07f, 0.18f, 10);

            Dust dust = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(5f, 5f), DustID.GoldFlame, behind * Main.rand.NextFloat(0.4f, 1.4f), 0, gold, Main.rand.NextFloat(0.55f, 0.86f));
            dust.noGravity = true;
        }

        internal static void SpawnGoldenImpactParticles(Vector2 center, Vector2 velocity)
        {
            if (Main.dedServ)
                return;

            Color gold = new Color(255, 218, 72);
            Pulse(center, gold, BloomCircle, 0.08f, 0.56f, 20, 0.9f);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, gold, Vector2.One, velocity.ToRotation(), 0.03f, 0.48f, 20));
            for (int i = 0; i < 8; i++)
            {
                Vector2 radial = (MathHelper.TwoPi * i / 8f).ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(center + radial * 5f, radial * Main.rand.NextFloat(1.5f, 4f), false, Main.rand.Next(14, 22), Main.rand.NextFloat(0.34f, 0.58f), Color.Lerp(gold, Color.White, i % 2 * 0.45f), true, false, true));
                GeneralParticleHandler.SpawnParticle(new SparkParticle(center, radial * Main.rand.NextFloat(3f, 6f), false, 13, Main.rand.NextFloat(0.7f, 1.2f), gold));
            }
            ImpactDust(center, gold, 18, 6f, 1f);
        }

        private static float TextureSize(string path)
        {
            Texture2D texture = ModContent.Request<Texture2D>(path).Value;
            return System.Math.Max(texture.Width, texture.Height);
        }

        internal static void DrawBeam(Vector2 start, Vector2 direction, float length, float width, Color outer, Color inner)
        {
            direction = direction.SafeNormalize(Vector2.UnitX);
            Texture2D body = ModContent.Request<Texture2D>(BeamBody).Value;
            Vector2 scale = new(length / body.Width, width / body.Height);
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(body, start - Main.screenPosition, null, outer, direction.ToRotation(), new Vector2(0f, body.Height * 0.5f), scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(body, start - Main.screenPosition, null, inner, direction.ToRotation(), new Vector2(0f, body.Height * 0.5f), new Vector2(length / body.Width, width * 0.28f / body.Height), SpriteEffects.None, 0);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            DrawTexture(BloomCircle, start, outer * 0.75f, 0f, width / 70f);
            DrawTexture(BloomCircle, start + direction * length, outer * 0.5f, 0f, width / 95f);
        }
    }
}
