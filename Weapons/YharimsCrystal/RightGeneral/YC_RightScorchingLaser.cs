using System;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.RightGeneral
{
    internal sealed class YC_RightScorchingLaser : ModProjectile, ILocalizedModType
    {
        private const float MaxBeamLength = 2600f;
        private const int SampleCount = 3;
        private static readonly Color LaserRed = new(255, 70, 32);
        private static readonly Color LaserGold = new(255, 216, 92);

        private readonly BalanceYharimsCrystal balance = new();

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int HoldoutIndex => (int)Projectile.ai[0];
        private bool IsStandalone => HoldoutIndex < 0;
        private ref float Timer => ref Projectile.localAI[0];
        private ref float BeamLength => ref Projectile.localAI[1];
        private YCRightLaserVisualTier Tier => balance.GetRightLaserTier();

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Crystal);
            if (IsStandalone)
            {
                Projectile.timeLeft = 48;
                return;
            }
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/PlasmaBolt") { Volume = 0.42f, Pitch = -0.15f, MaxInstances = 4 }, Projectile.Center);
        }

        public override void AI()
        {
            if (IsStandalone)
            {
                Timer++;
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.scale = GetBeamScale();
                UpdateBeamLength();
                EmitBeamFX();
                CastBeamLight();
                // timeLeft decrements naturally — no refresh
                return;
            }

            if (!TryGetHoldout(out Projectile holdoutProjectile, out YC_RightCrystalHoldout holdout))
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[Projectile.owner];
            Timer++;
            Projectile.timeLeft = 2;
            Projectile.Center = holdout.Muzzle;
            Projectile.velocity = holdout.ForwardDirection;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.scale = GetBeamScale();

            float empower = owner.GetModPlayer<YharimsCrystalStatePlayer>().CrystalEmpowered ? 1.35f : 1f;
            Projectile.damage = Math.Max(1, (int)(holdoutProjectile.damage * GetDamageMultiplier() * empower));

            UpdateBeamLength();
            EmitBeamFX();
            CastBeamLight();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(balance.GetFireDebuffType(), 240);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
                return false;

            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + Projectile.velocity * BeamLength,
                GetCollisionWidth(),
                ref collisionPoint);
        }

        public override void CutTiles()
        {
            if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
                return;

            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * BeamLength, GetCollisionWidth() + 8f, DelegateMethods.CutTiles);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
                return false;

            return Tier switch
            {
                YCRightLaserVisualTier.Cell => DrawCellLaser(),
                YCRightLaserVisualTier.Classic => DrawClassicLaser(),
                YCRightLaserVisualTier.MoonLord => DrawMoonLordLaser(),
                YCRightLaserVisualTier.Providence => DrawProvidenceLaser(),
                YCRightLaserVisualTier.Yharon => DrawYharonLaser(),
                _ => DrawClassicLaser(),
            };
        }

        private bool TryGetHoldout(out Projectile holdoutProjectile, out YC_RightCrystalHoldout holdout)
        {
            holdoutProjectile = null;
            holdout = null;

            if (HoldoutIndex < 0 || HoldoutIndex >= Main.maxProjectiles)
                return false;

            Projectile candidate = Main.projectile[HoldoutIndex];
            if (!candidate.active ||
                candidate.owner != Projectile.owner ||
                candidate.type != ModContent.ProjectileType<YC_RightCrystalHoldout>() ||
                candidate.ModProjectile is not YC_RightCrystalHoldout holdoutMod)
            {
                return false;
            }

            holdoutProjectile = candidate;
            holdout = holdoutMod;
            return true;
        }

        private void UpdateBeamLength()
        {
            float[] samples = new float[SampleCount];
            float width = Math.Max(4f, GetCollisionWidth() * 0.35f);
            Collision.LaserScan(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), width, MaxBeamLength, samples);

            float average = 0f;
            for (int i = 0; i < samples.Length; i++)
                average += samples[i];

            average /= samples.Length;
            if (average <= 0f)
                average = MaxBeamLength;

            BeamLength = MathHelper.Lerp(BeamLength <= 0f ? average : BeamLength, average, 0.58f);
        }

        private float GetBeamScale()
        {
            return Tier switch
            {
                YCRightLaserVisualTier.Cell => 0.62f,
                YCRightLaserVisualTier.Classic => 0.86f,
                YCRightLaserVisualTier.MoonLord => 1.12f,
                YCRightLaserVisualTier.Providence => 1.18f,
                YCRightLaserVisualTier.Yharon => 1.0f,
                _ => 0.86f,
            };
        }

        private float GetCollisionWidth()
        {
            return Tier switch
            {
                YCRightLaserVisualTier.Cell => 16f,
                YCRightLaserVisualTier.Classic => 26f,
                YCRightLaserVisualTier.MoonLord => 36f,
                YCRightLaserVisualTier.Providence => 32f,
                YCRightLaserVisualTier.Yharon => 34f,
                _ => 24f,
            } * Projectile.scale;
        }

        private float GetDamageMultiplier()
        {
            return Tier switch
            {
                YCRightLaserVisualTier.Cell => 0.86f,
                YCRightLaserVisualTier.Classic => 1.0f,
                YCRightLaserVisualTier.MoonLord => 1.16f,
                YCRightLaserVisualTier.Providence => 1.28f,
                YCRightLaserVisualTier.Yharon => 1.42f,
                _ => 1f,
            };
        }

        private void CastBeamLight()
        {
            Color lightColor = Color.Lerp(LaserRed, LaserGold, 0.42f);
            DelegateMethods.v3_1 = lightColor.ToVector3() * 0.46f;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * BeamLength, GetCollisionWidth(), DelegateMethods.CastLight);
        }

        private void EmitBeamFX()
        {
            if (Main.dedServ || Main.GameUpdateCount % 4 != 0)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(BeamLength);
            Color color = Main.rand.NextBool(4) ? Color.White : Color.Lerp(LaserRed, LaserGold, Main.rand.NextFloat(0.2f, 0.85f));

            GeneralParticleHandler.SpawnParticle(new PointParticle(
                position + Main.rand.NextVector2Circular(8f, 8f),
                direction.RotatedByRandom(0.84f) * Main.rand.NextFloat(1.1f, 4.2f),
                false,
                Main.rand.Next(12, 22),
                Main.rand.NextFloat(0.72f, 1.2f),
                color,
                true));
        }

        private bool DrawCellLaser()
        {
            Texture2D outer = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;
            Texture2D inner = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineThick").Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 unit = Projectile.rotation.ToRotationVector2();
            Vector2 beamCenter = start + unit * BeamLength * 0.5f;
            float beamLengthScale = BeamLength / 1000f;
            float opacity = LaserOpacity();
            Color outerColor = LaserRed with { A = 0 };
            Color innerColor = Color.White with { A = 0 };

            Main.EntitySpriteDraw(outer, beamCenter, null, outerColor * opacity, Projectile.rotation + MathHelper.PiOver2, outer.Size() * 0.5f, new Vector2(1.5f, 55f * beamLengthScale) * Projectile.scale * 0.01f, SpriteEffects.FlipVertically);
            Main.EntitySpriteDraw(inner, beamCenter, null, innerColor * opacity, Projectile.rotation + MathHelper.PiOver2, inner.Size() * 0.5f, new Vector2(0.32f, 55f * beamLengthScale) * Projectile.scale * 0.01f, SpriteEffects.FlipVertically);
            Main.EntitySpriteDraw(glow, start + unit * 7f, null, LaserGold with { A = 0 } * opacity, Projectile.rotation, glow.Size() * 0.5f, Projectile.scale * 0.08f, SpriteEffects.None);
            return false;
        }

        private bool DrawClassicLaser()
        {
            YC_YharimBeamVisuals.DrawYharimBeam(Projectile, BeamLength, Projectile.scale * 0.48f, LaserOpacity(), Color.Lerp(LaserRed, LaserGold, 0.5f));
            return false;
        }

        private bool DrawMoonLordLaser()
        {
            DrawClassicLaser();

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 end = Projectile.Center + direction * BeamLength - Main.screenPosition;
            float opacity = LaserOpacity();
            Color color = Color.Lerp(LaserRed, Color.LimeGreen, 0.22f) with { A = 0 };

            for (int i = 0; i < 5; i++)
            {
                float rotation = Main.GlobalTimeWrappedHourly * (1.1f + i * 0.14f) + MathHelper.TwoPi * i / 5f;
                Main.EntitySpriteDraw(ring, start, null, color * 0.18f * opacity, rotation, ring.Size() * 0.5f, Projectile.scale * (0.18f + i * 0.018f), SpriteEffects.None);
                Main.EntitySpriteDraw(bloom, end, null, color * 0.22f * opacity, -rotation, bloom.Size() * 0.5f, Projectile.scale * (0.18f + i * 0.02f), SpriteEffects.None);
            }

            return false;
        }

        private bool DrawProvidenceLaser()
        {
            Texture2D startTex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/ProvidenceHolyRay").Value;
            Texture2D midTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayMid").Value;
            Texture2D endTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayEnd").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float rotation = direction.ToRotation() - MathHelper.PiOver2;
            float drawScale = Projectile.scale * 0.45f;
            float opacity = LaserOpacity();
            Color color = Color.Lerp(LaserRed, LaserGold, 0.52f) with { A = 0 };
            Vector2 start = Projectile.Center;
            Vector2 drawStart = start - Main.screenPosition;

            Main.spriteBatch.Draw(startTex, drawStart, null, color * opacity, rotation, startTex.Size() / 2f, drawScale, SpriteEffects.None, 0f);

            float currentLength = BeamLength - (startTex.Height / 2f + endTex.Height) * drawScale;
            Vector2 center = start + direction * drawScale * startTex.Height / 2f;
            if (currentLength > 0f)
            {
                float lengthDrawn = 0f;
                int frameHeight = 36;
                int frameY = frameHeight * ((int)Timer / 3 % 4);
                Rectangle sourceRect = new(0, frameY, midTex.Width, frameHeight);
                while (lengthDrawn + 1f < currentLength)
                {
                    if (currentLength - lengthDrawn < frameHeight * drawScale)
                        sourceRect.Height = (int)((currentLength - lengthDrawn) / drawScale);
                    if (sourceRect.Height <= 0)
                        break;

                    Main.spriteBatch.Draw(midTex, center - Main.screenPosition, sourceRect, color * opacity, rotation, new Vector2(sourceRect.Width / 2f, 0f), drawScale, SpriteEffects.None, 0f);
                    lengthDrawn += sourceRect.Height * drawScale;
                    center += direction * sourceRect.Height * drawScale;
                    sourceRect.Y += frameHeight;
                    if (sourceRect.Y + sourceRect.Height > midTex.Height)
                        sourceRect.Y = 0;
                }
            }

            Main.spriteBatch.Draw(endTex, center - Main.screenPosition, null, color * opacity, rotation, new Vector2(endTex.Width / 2f, 0f), drawScale, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, drawStart, null, LaserGold with { A = 0 } * 0.62f * opacity, 0f, bloom.Size() * 0.5f, Projectile.scale * 0.16f, SpriteEffects.None);
            return false;
        }

        private bool DrawYharonLaser()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2[] points =
            {
                Projectile.Center,
                Projectile.Center + direction * BeamLength * 0.25f,
                Projectile.Center + direction * BeamLength * 0.55f,
                Projectile.Center + direction * BeamLength
            };

            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityMod:Bordernado"].UseSaturation(-0.05f);
            GameShaders.Misc["CalamityMod:Bordernado"].UseOpacity(LaserOpacity());
            GameShaders.Misc["CalamityMod:Bordernado"].SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin"));
            PrimitiveRenderer.RenderTrail(points, new PrimitiveSettings(YharonWidthFunction, YharonColorFunction, shader: GameShaders.Misc["CalamityMod:Bordernado"]), 72);
            Main.spriteBatch.ExitShaderRegion();

            Texture2D vortex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/Cracks").Value;
            GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(0.75f * LaserOpacity());
            GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(LaserGold);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.White);
            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityMod:DoGPortal"].Apply();
            for (int i = 0; i < 4; i++)
            {
                float angle = MathHelper.TwoPi * i / 4f + Main.GlobalTimeWrappedHourly * MathHelper.TwoPi;
                Color drawColor = Color.White;
                drawColor.A = 0;
                Main.EntitySpriteDraw(vortex, Projectile.Center - Main.screenPosition + angle.ToRotationVector2() * 3f, null, drawColor, angle + MathHelper.PiOver2, vortex.Size() * 0.5f, Projectile.scale * 0.75f, SpriteEffects.None);
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        private float LaserOpacity()
        {
            float fadeIn = Utils.GetLerpValue(0f, 12f, Timer, true);
            if (IsStandalone)
                return fadeIn * Utils.GetLerpValue(0f, 14f, Projectile.timeLeft, true);
            return fadeIn;
        }

        private float YharonWidthFunction(float completionRatio, Vector2 vertexPosition)
        {
            float endFade = Utils.GetLerpValue(1f, 0.82f, completionRatio, true);
            return 41f * Projectile.scale * endFade;
        }

        private Color YharonColorFunction(float completionRatio, Vector2 vertexPosition)
        {
            return Color.Lerp(LaserGold, LaserRed, completionRatio * 0.45f) * LaserOpacity();
        }
    }
}
