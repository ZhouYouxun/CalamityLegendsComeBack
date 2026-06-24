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

        private const int HomingDelay = 8;
        private const float HomingInertia = 16f;
        private const float MaxSpeed = 22f;

        private static readonly Color ReconMainColor = new Color(126, 170, 255);
        private static readonly Color ReconAccentColor = new Color(220, 244, 255);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 22;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 130;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Timer++;
            HomeTowardTarget();
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.06f, 0.18f, 0.48f));
        }

        private void HomeTowardTarget()
        {
            if (Timer <= HomingDelay)
            {
                Projectile.velocity *= 0.998f;
                return;
            }

            NPC target = GetTarget();
            if (target == null)
            {
                Projectile.velocity *= 0.994f;
                return;
            }

            float warmup = Utils.GetLerpValue(HomingDelay, HomingDelay + 24f, Timer, true);
            float closePressure = Utils.GetLerpValue(380f, 65f, Projectile.Distance(target.Center), true);
            float pull = MathHelper.Lerp(0.28f, 1f, System.Math.Max(warmup, closePressure * 0.8f));

            Vector2 aimPoint = target.Center + target.velocity * 0.15f;
            Vector2 desired = (aimPoint - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY))
                * MathHelper.Lerp(9f, MaxSpeed, pull);
            Projectile.velocity = (Projectile.velocity * HomingInertia + desired) / (HomingInertia + 1f);

            if (Projectile.velocity.LengthSquared() > MaxSpeed * MaxSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * MaxSpeed;
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
            DrawReconStyle();
            return false;
        }

        private NPC GetTarget()
        {
            if (TargetIndex >= 0f && TargetIndex < Main.maxNPCs && Main.npc[(int)TargetIndex].CanBeChasedBy())
                return Main.npc[(int)TargetIndex];

            return null;
        }

        private void DrawReconStyle()
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Texture2D helix = ModContent.Request<Texture2D>("CalamityMod/Particles/WaterFlavored").Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // GlowSpark streak trail along old positions
            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float trailRot = i < Projectile.oldPos.Length - 1 && Projectile.oldPos[i + 1] != Vector2.Zero
                    ? (Projectile.oldPos[i] - Projectile.oldPos[i + 1]).ToRotation() + MathHelper.PiOver2
                    : Projectile.rotation;

                Color trailColor = Color.Lerp(ReconMainColor, ReconAccentColor, completion);
                Main.EntitySpriteDraw(spark, trailPos, null, trailColor * (0.4f * completion), trailRot, spark.Size() * 0.5f, new Vector2(0.014f, 0.065f * completion), SpriteEffects.None, 0);
                if (i % 3 == 0)
                    Main.EntitySpriteDraw(bloom, trailPos, null, ReconMainColor * (0.11f * completion), 0f, bloom.Size() * 0.5f, 0.011f + completion * 0.009f, SpriteEffects.None, 0);
            }

            // Helix (same style as BFLeafProj Chlo_CDetec)
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float time = Main.GlobalTimeWrappedHourly * 5.8f + Projectile.identity * 0.31f;
            const int helixCount = 5;
            for (int i = 0; i < helixCount; i++)
            {
                float progress = i / (float)(helixCount - 1);
                float helixAngle = time - progress * 2.1f + MathHelper.TwoPi * i / helixCount;
                Vector2 spiralOffset = right.RotatedBy(helixAngle) * MathHelper.Lerp(7f, 3f, progress);
                Vector2 backwardOffset = -forward * MathHelper.Lerp(3f, 18f, progress);
                Color helixColor = Color.Lerp(ReconMainColor, ReconAccentColor, progress * 0.45f) * MathHelper.Lerp(0.52f, 0.08f, progress) * Projectile.Opacity;
                Main.EntitySpriteDraw(helix, drawPosition + spiralOffset + backwardOffset, null, helixColor,
                    forward.ToRotation() - MathHelper.PiOver2, helix.Size() * 0.5f,
                    new Vector2(0.14f, MathHelper.Lerp(0.58f, 0.22f, progress)) * Projectile.scale, SpriteEffects.None, 0);
            }

            // Orbiting sparks around center (like BFLeafProj's spark ring)
            float pulse = 0.92f + 0.08f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 7.2f + Projectile.identity * 0.23f);
            for (int i = 0; i < 5; i++)
            {
                float angle = MathHelper.TwoPi * i / 5f;
                float side = (float)System.Math.Sin(angle + Main.GlobalTimeWrappedHourly * 2.6f);
                float along = (float)System.Math.Cos(angle) * (2.2f + 0.9f * pulse);
                Vector2 offset = right * side * (2.8f + 1.0f * pulse) + forward * along;
                Main.EntitySpriteDraw(spark, drawPosition + offset, null,
                    Color.Lerp(ReconMainColor, ReconAccentColor, 0.35f) * 0.22f,
                    Projectile.rotation + MathHelper.PiOver2, spark.Size() * 0.5f,
                    new Vector2(0.024f, (0.082f + 0.028f * pulse) * 0.5f) * Projectile.scale, SpriteEffects.None, 0);
            }

            // Center bloom + spark
            Main.EntitySpriteDraw(bloom, drawPosition, null, ReconMainColor * (0.48f * Projectile.Opacity), 0f, bloom.Size() * 0.5f, 0.21f * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(spark, drawPosition, null, ReconAccentColor * (0.74f * Projectile.Opacity), Projectile.rotation, spark.Size() * 0.5f, new Vector2(0.036f, 0.16f), SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
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
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, new Color(255, 110, 42, 0) * 0.5f, 0f, bloom.Size() * 0.5f, 0.072f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
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
            int advTier = BFArrowCommon.InBounds(Projectile.owner, Main.maxPlayers)
                ? Main.player[Projectile.owner].GetModPlayer<BFAccessoryPlayer>().PlagueAdvancedTier : 0;
            BFPlaguePollutionNPC pollution = target.GetGlobalNPC<BFPlaguePollutionNPC>();
            pollution.ApplyPollution(target);
            pollution.ApplyPlagueDebuffs(target, false, advTier);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, new Color(178, 255, 68, 0) * 0.36f, 0f, bloom.Size() * 0.5f, 0.052f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
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
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/Skill/SeedOfSilva/SeedPack/WitheredPetal";

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
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.EntitySpriteDraw(bloom, center, null, new Color(160, 82, 210, 0) * 0.2f, 0f, bloom.Size() * 0.5f, 0.04f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            DrawOutline(texture, center, origin, Projectile.rotation, Projectile.scale, new Color(178, 92, 220) * 0.72f);
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
