using System;
using System.Linq;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Particles;
using CalamityMod.Utilities.Daybreak;
using CalamityMod.Utilities.Daybreak.Buffers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    public class HalleysStarburst : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        public override string Texture => "CalamityMod/Particles/Sparkle";
        Color drawColor = Color.Black;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.MaxUpdates = 6;
            Projectile.timeLeft = 60 * Projectile.MaxUpdates;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.frame = Main.rand.Next(0, 6);
            Projectile.scale = 0.75f;
            Projectile.rotation += Main.rand.NextFloat(0, 3);
            Projectile.stopsDealingDamageAfterPenetrateHits = true;
        }

        public override void AI()
        {
            Projectile.rotation += Projectile.direction * 0.05f;
            if (Projectile.FinalExtraUpdate())
            {
                var star = new BloomParticle(Projectile.Center, Vector2.Zero, drawColor, 0.2f, 0.25f, 2, false);
                var star2 = new CustomSpark(Projectile.Center, Vector2.UnitX.RotatedBy(Projectile.rotation) * 0.1f, Texture, false, 2, 1f, Color.White, Vector2.One);
                GeneralParticleHandler.SpawnParticle(star);
                GeneralParticleHandler.SpawnParticle(star2);
            }
                Projectile.frameCounter++;
            if (Projectile.frameCounter > 5)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 5)
                Projectile.frame = 0;
            if (Projectile.timeLeft == 1)
            {

                for (var i = 0; i < 5; i++)
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, Main.rand.NextVector2CircularEdge(10,10), false, 10, 0.02f, drawColor, new Vector2(0.5f, 1f)));
                if (Projectile.damage != 0)
                    Main.player[Projectile.owner].Calamity().HalleyAccuracyCounter -= HalleysInferno.LostAccuracyPerMiss;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (drawColor == Color.Black)
            {
                switch (Projectile.ai[0])
                {
                    case 1:
                        drawColor = Color.HotPink;
                        break;
                    case 2:
                        drawColor = Color.Yellow;
                        break;
                    case 3:
                        drawColor = Color.LimeGreen;
                        break;
                    case 4:
                        drawColor = Color.SkyBlue;
                        break;
                    case 5:
                        drawColor = Color.Lavender;
                        break;
                    case 6:
                        drawColor = Color.White;
                        break;
                }
            }

            Main.spriteBatch.End(out var ss);

            var device = Main.instance.GraphicsDevice;
            using var lease = RenderTargetPool.Shared.Rent(
                device,
                Main.screenWidth / 2,
                Main.screenHeight / 2,
                RenderTargetDescriptor.Default
            );

            using (lease.Scope(clearColor: Color.Transparent))
            {
                GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
                PrimitiveRenderer.RenderTrail(Projectile.oldPos, new(FireWidthFunction, FireColorFunction, (_, _) => Projectile.Size * 0.5f, smoothen: true, pixelate: false, shader: GameShaders.Misc["CalamityMod:ImpFlameTrail"], useUnscaledMatrices: true), Projectile.oldPos.Length + 32);

                Vector2[] fireCoreLength = Projectile.oldPos.Take(8).ToArray();
                GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
                PrimitiveRenderer.RenderTrail(fireCoreLength, new(FireCoreWidthFunction, FireCoreColorFunction, (_, _) => Projectile.Size * 0.5f, smoothen: true, pixelate: false, shader: GameShaders.Misc["CalamityMod:ImpFlameTrail"], useUnscaledMatrices: true), fireCoreLength.Length + 24);
            }

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(lease.Target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(ss);
            lightColor = drawColor;
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Voidfrost>(), 75);
            SoundEngine.PlaySound(SoundID.DD2_CrystalCartImpact, Projectile.Center);

            // Dust emission on hit
            for (int i = 0; i < 14; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? 172 : 206, Projectile.velocity);
                dust.scale = Main.rand.NextFloat(1.1f, 1.9f);
                dust.velocity = Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.2f, 2.1f);
                dust.noGravity = true;
            }
            var cplay = Main.player[Projectile.owner].Calamity();
            cplay.HalleyAccuracyCounter++;
            cplay.HalleyAccuracyCounter = MathF.Min(HalleysInferno.MaxAccuracy, cplay.HalleyAccuracyCounter);
            Main.player[Projectile.owner].Calamity().StarburstSpawnFrameCounter += cplay.HalleyAccuracyCounter / HalleysInferno.MaxAccuracy * HalleysInferno.MaxStarburstPerStar;
            Projectile.velocity *= 0.25f;
            Projectile.timeLeft = Projectile.MaxUpdates;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Nightwither>(), 450);
            SoundEngine.PlaySound(SoundID.DD2_CrystalCartImpact, Projectile.Center);

            // Dust emission on hit
            for (int i = 0; i < 14; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? 172 : 206, Projectile.velocity);
                dust.scale = Main.rand.NextFloat(1.1f, 1.9f);
                dust.velocity = Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.2f, 2.1f);
                dust.noGravity = true;
            }
        }

        public float FireWidthFunction(float completion, Vector2 pos)
        {
            float width;
            float maxBodyWidth = 38f * Projectile.scale;
            float curveRatio = 0.2f;
            var positions = Projectile.oldPos.ToList();
            positions.RemoveAll(x => x == Vector2.Zero);
            // Crop the tip of the trail into a conic shape.
            if (completion < curveRatio)
                width = MathF.Pow(completion / curveRatio, 0.5f) * maxBodyWidth;
            else
                width = Utils.Remap(completion, curveRatio, 1f, maxBodyWidth, 0f);

            // Pulse inwards and outwards over time.
            float pulseInterpolant = MathF.Cos(MathHelper.Pi * completion - Main.GlobalTimeWrappedHourly * 20f) * 0.5f + 0.5f;
            float additionalPulseWidth = MathHelper.Lerp(0f, 12f, pulseInterpolant);
            return (width + additionalPulseWidth) * positions.Count() / (float)ProjectileID.Sets.TrailCacheLength[Type];
        }

        public Color FireColorFunction(float completion, Vector2 pos)
        {
            Color mainColor = drawColor * 1.3f;
            Color endColor = Color.Lerp(mainColor, Color.Transparent, Utils.GetLerpValue(0.8f, 1f, completion, true));
            return Color.Lerp(mainColor, endColor, completion) * Projectile.Opacity;
        }

        public float FireCoreWidthFunction(float completion, Vector2 pos)
        {
            float width;
            float maxBodyWidth = Projectile.scale * 16;
            float curveRatio = 0.25f;
            var positions = Projectile.oldPos.ToList();
            positions.RemoveAll(x => x == Vector2.Zero);

            if (completion < curveRatio)
                width = MathF.Sin(completion / curveRatio * MathHelper.PiOver2) * maxBodyWidth + curveRatio;
            else
                width = Utils.Remap(completion, curveRatio, 1f, maxBodyWidth, 0f);
            return width * positions.Count() / (float)ProjectileID.Sets.TrailCacheLength[Type];
        }

        public Color FireCoreColorFunction(float completion, Vector2 pos)
        {
            Color mainColor = drawColor;
            Color tipColor = Color.Lerp(mainColor, Color.Transparent, Utils.GetLerpValue(0.8f, 1f, completion, true));
            Color fullBodyColor = Color.Lerp(mainColor, tipColor, completion);
            return Color.Lerp(fullBodyColor, Color.White, 0.175f) * Projectile.Opacity;
        }
    }
}
