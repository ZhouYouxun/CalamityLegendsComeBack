using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.SeedOfSilva
{
    internal sealed class SeedOfSilvaDelphiniumArc : ModProjectile
    {
        private ref float TargetIndex => ref Projectile.ai[0];
        private ref float Timer => ref Projectile.localAI[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 96;
            Projectile.extraUpdates = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Timer++;
            NPC target = GetTarget();
            if (target != null)
            {
                Vector2 aimPoint = target.Center + target.velocity * 0.18f;
                Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                Vector2 desired = (aimPoint - Projectile.Center).SafeNormalize(currentDirection) * 26f;
                float curve = (float)System.Math.Sin(Timer * 0.18f + Projectile.identity * 0.3f) * 0.045f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity.RotatedBy(curve), desired, 0.34f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.42f, 0.14f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<BFAccessoryGlobalNPC>().ApplyDelphiniumStun(Projectile.owner, 60);

            if (BFArrowCommon.InBounds(Projectile.owner, Main.maxPlayers))
            {
                Player owner = Main.player[Projectile.owner];
                owner.wingTime = System.Math.Min(owner.wingTimeMax, owner.wingTime + 30f);
            }

            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.2f, Pitch = 0.58f }, target.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawTrail(new Color(64, 255, 118), new Color(220, 255, 190));
            return false;
        }

        private NPC GetTarget()
        {
            if (TargetIndex >= 0f && TargetIndex < Main.maxNPCs && Main.npc[(int)TargetIndex].CanBeChasedBy())
                return Main.npc[(int)TargetIndex];

            return null;
        }

        private void DrawTrail(Color mainColor, Color accentColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color trailColor = Color.Lerp(mainColor, accentColor, completion);
                Main.EntitySpriteDraw(bloom, drawPosition, null, trailColor * (0.18f * completion), 0f, bloom.Size() * 0.5f, 0.025f + completion * 0.024f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(spark, drawPosition, null, trailColor * (0.34f * completion), Projectile.rotation, spark.Size() * 0.5f, new Vector2(0.032f, 0.15f * completion), SpriteEffects.None, 0);
            }

            Vector2 center = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bloom, center, null, accentColor * 0.38f, 0f, bloom.Size() * 0.5f, 0.043f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(spark, center, null, Color.White * 0.62f, Projectile.rotation, spark.Size() * 0.5f, new Vector2(0.046f, 0.18f), SpriteEffects.None, 0);
        }
    }

    internal sealed class SeedOfSilvaTorchFlame : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 34;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.985f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.46f, 0.15f, 0.035f));
            if (Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.Torch, -Projectile.velocity * 0.035f + Main.rand.NextVector2Circular(0.35f, 0.35f), 100, new Color(255, 150, 70), Main.rand.NextFloat(0.82f, 1.08f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, new Color(255, 110, 42, 0) * 0.5f, 0f, bloom.Size() * 0.5f, 0.072f, SpriteEffects.None, 0);
            return false;
        }
    }

    internal sealed class SeedOfSilvaMandrakeSpore : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            HomeToTarget(0.08f, 8f);
            Projectile.velocity *= 0.992f;
            Projectile.rotation += 0.08f * Projectile.direction;
            Lighting.AddLight(Projectile.Center, new Vector3(0.14f, 0.19f, 0.04f));
            if (Main.rand.NextBool(5))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GreenTorch, Main.rand.NextVector2Circular(0.36f, 0.36f), 100, new Color(184, 235, 80), Main.rand.NextFloat(0.52f, 0.72f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<BFAccessoryGlobalNPC>().ApplyMandrakeSlow(Projectile.owner, 180);
            BFPlaguePollutionNPC pollution = target.GetGlobalNPC<BFPlaguePollutionNPC>();
            pollution.ApplyPollution(target);
            pollution.ApplyPlagueDebuffs(target, false);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, new Color(178, 255, 68, 0) * 0.36f, 0f, bloom.Size() * 0.5f, 0.052f, SpriteEffects.None, 0);
            return false;
        }

        private void HomeToTarget(float responsiveness, float speed)
        {
            NPC target = GetTarget();
            if (target is null)
                return;

            Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY)) * speed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, responsiveness);
        }

        private NPC GetTarget()
        {
            if (Projectile.ai[0] >= 0f && Projectile.ai[0] < Main.maxNPCs && Main.npc[(int)Projectile.ai[0]].CanBeChasedBy())
                return Main.npc[(int)Projectile.ai[0]];

            return null;
        }
    }

    internal sealed class SeedOfSilvaMandrakeDart : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/SeedOfSilva/SeedPack/WitheredPetal";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 110;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            NPC target = GetTarget();
            if (target != null)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY)) * 15f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.18f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Lighting.AddLight(Projectile.Center, new Vector3(0.13f, 0.04f, 0.17f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<BFAccessoryGlobalNPC>().ApplyMandrakeSlow(Projectile.owner, 180);
            target.AddBuff(BuffID.Poisoned, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 position = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, position, null, new Color(136, 70, 172, 0) * (0.22f * completion), Projectile.rotation, origin, Projectile.scale * (0.8f + completion * 0.12f), SpriteEffects.None, 0);
            }

            Vector2 center = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bloom, center, null, new Color(160, 82, 210, 0) * 0.2f, 0f, bloom.Size() * 0.5f, 0.04f, SpriteEffects.None, 0);
            DrawOutline(texture, center, origin, Projectile.rotation, Projectile.scale, new Color(35, 18, 46) * 0.78f);
            Main.EntitySpriteDraw(texture, center, null, Color.Lerp(lightColor, Color.White, 0.25f), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        private NPC GetTarget()
        {
            if (Projectile.ai[0] >= 0f && Projectile.ai[0] < Main.maxNPCs && Main.npc[(int)Projectile.ai[0]].CanBeChasedBy())
                return Main.npc[(int)Projectile.ai[0]];

            return null;
        }

        private static void DrawOutline(Texture2D texture, Vector2 center, Vector2 origin, float rotation, float scale, Color color)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 1.5f;
                Main.EntitySpriteDraw(texture, center + offset, null, color, rotation, origin, scale, SpriteEffects.None, 0);
            }
        }
    }

    internal sealed class PastLingeringShard : ModProjectile
    {
        private ref float TargetIndex => ref Projectile.ai[0];
        private ref float Timer => ref Projectile.localAI[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 110;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Timer++;
            NPC target = FindTarget();
            if (target != null)
            {
                Vector2 aimPoint = target.Center + target.velocity * 0.2f;
                Vector2 desired = (aimPoint - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY)) * 18f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.2f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.18f, 0.06f, 0.34f));
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 7; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1f, 3.2f), 100, new Color(188, 98, 255), Main.rand.NextFloat(0.72f, 1f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(spark, Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition, null, new Color(168, 92, 255, 0) * (0.46f * completion), Projectile.rotation, spark.Size() * 0.5f, new Vector2(0.045f, 0.2f * completion), SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(spark, Projectile.Center - Main.screenPosition, null, new Color(232, 248, 255, 0) * 0.74f, Projectile.rotation, spark.Size() * 0.5f, new Vector2(0.06f, 0.24f), SpriteEffects.None, 0);
            return false;
        }

        private NPC FindTarget()
        {
            if (TargetIndex >= 0f && TargetIndex < Main.maxNPCs && Main.npc[(int)TargetIndex].CanBeChasedBy())
                return Main.npc[(int)TargetIndex];

            NPC bestTarget = null;
            float bestDistance = 780f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.Distance(npc.Center, Projectile.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }
    }
}
