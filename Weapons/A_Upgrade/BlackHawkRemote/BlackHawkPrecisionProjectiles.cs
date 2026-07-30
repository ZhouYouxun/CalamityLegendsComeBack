using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.BlackHawkRemote
{
    internal sealed class BlackHawkMachineGunRound : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BlackHawk";
        public override string Texture => "CalamityMod/Projectiles/Summon/BlackHawkBullet";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 9;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 2;
            Projectile.timeLeft = 42;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 8;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.32f, 0.25f, 0.10f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 end = Projectile.Center + direction * 9f;
            Vector2 start = Projectile.Center - direction * 15f;
            BlackHawkVFX.DrawWorldLine(start, end, BlackHawkVFX.Additive(new Color(255, 212, 118)) * 0.88f, 2.2f);
            BlackHawkVFX.DrawBloom(Projectile.Center, new Color(255, 234, 168), 4f, 0.72f);
            return false;
        }
    }

    internal sealed class BlackHawkGuidedMissile : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BlackHawk";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int TargetIndex => (int)Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 220;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.ai[1]++;
            float speed = MathHelper.Lerp(Projectile.velocity.Length(), 17f, 0.035f);
            if (Projectile.ai[1] >= 20f && Main.npc.IndexInRange(TargetIndex))
            {
                NPC target = Main.npc[TargetIndex];
                if (target.CanBeChasedBy(Projectile, false))
                {
                    BlackHawkTargetStatusNPC status = target.GetGlobalNPC<BlackHawkTargetStatusNPC>();
                    float turnRate = status.IsIlluminated(Projectile.owner) || status.IsEMPd(Projectile.owner) ? 0.11f : 0.055f;
                    Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                    float angularDifference = MathHelper.WrapAngle(desired.ToRotation() - Projectile.velocity.ToRotation());
                    Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.Clamp(angularDifference, -turnRate, turnRate));
                }
            }

            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.44f, 0.15f, 0.08f);

            if (Projectile.numUpdates == 0)
            {
                Vector2 rear = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 9f;
                BlackHawkVFX.SpawnEnginePoint(rear, -Projectile.velocity * 0.03f, new Color(255, 88, 48));
                if (Main.GameUpdateCount % 3 == Projectile.identity % 3)
                    BlackHawkVFX.SpawnSmokePoint(rear, -Projectile.velocity * 0.04f, new Color(255, 102, 56), new Color(52, 56, 62), 0.34f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            BlackHawkVFX.SpawnCompactImpact(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), new Color(255, 91, 56));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            Color body = new Color(228, 224, 210);
            BlackHawkVFX.DrawWorldLine(Projectile.Center - direction * 8f, Projectile.Center + direction * 9f, body, 4f);
            BlackHawkVFX.DrawWorldLine(Projectile.Center - direction * 5f - side * 4f,
                Projectile.Center - direction * 1f + side * 4f, new Color(105, 111, 122), 2f);
            BlackHawkVFX.DrawBloom(Projectile.Center - direction * 9f, new Color(255, 86, 42), 7f, 0.72f);
            return false;
        }
    }

    internal sealed class BlackHawkClusterShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BlackHawk";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 7;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 9;
            Projectile.height = 9;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 92;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.992f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.numUpdates == 0 && Main.GameUpdateCount % 2 == Projectile.identity % 2)
                BlackHawkVFX.SpawnEnginePoint(Projectile.Center, -Projectile.velocity * 0.02f, new Color(255, 174, 72));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            BlackHawkVFX.DrawWorldLine(Projectile.Center - direction * 8f, Projectile.Center + direction * 4f,
                new Color(255, 192, 92), 3f);
            BlackHawkVFX.DrawBloom(Projectile.Center, new Color(255, 184, 72), 5f, 0.55f);
            return false;
        }
    }

    internal sealed class BlackHawkCryoShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BlackHawk";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 100;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.988f;
            Projectile.rotation += 0.17f * Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);
            Lighting.AddLight(Projectile.Center, 0.08f, 0.28f, 0.38f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn2, 120);
            target.GetGlobalNPC<BlackHawkTargetStatusNPC>().ApplyCryogenic(Projectile.owner, 150);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            BlackHawkVFX.DrawWorldLine(Projectile.Center - direction * 9f, Projectile.Center + direction * 7f,
                BlackHawkVFX.Additive(new Color(130, 238, 255)) * 0.86f, 2.4f);
            BlackHawkVFX.DrawWorldLine(Projectile.Center - side * 4f, Projectile.Center + side * 4f,
                BlackHawkVFX.Additive(Color.White) * 0.58f, 1.3f);
            BlackHawkVFX.DrawBloom(Projectile.Center, new Color(118, 226, 255), 5f, 0.56f);
            return false;
        }
    }

    internal sealed class BlackHawkHolyShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BlackHawk";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 9;
            Projectile.height = 9;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 2;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 96;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.994f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.38f, 0.34f, 0.12f);
            if (Projectile.numUpdates == 0 && Main.GameUpdateCount % 2 == Projectile.identity % 2)
                BlackHawkVFX.SpawnEnginePoint(Projectile.Center, -Projectile.velocity * 0.02f, new Color(255, 241, 157));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            BlackHawkVFX.DrawWorldLine(Projectile.Center - direction * 10f, Projectile.Center + direction * 6f,
                BlackHawkVFX.Additive(new Color(255, 228, 126)) * 0.92f, 2.6f);
            BlackHawkVFX.DrawBloom(Projectile.Center, Color.White, 6f, 0.62f);
            return false;
        }
    }
}
