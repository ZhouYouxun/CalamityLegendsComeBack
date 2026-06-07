using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFBrimstoneElemental_Laser : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Boss/BrimstoneRay";

        private const int Lifetime = 45;
        private const float MaxBeamScale = 1f;
        private const float MaxBeamLength = 2400f;
        private const float BeamTileCollisionWidth = 1f;
        private const float BeamHitboxCollisionWidth = 28f;
        private const int NumSamplePoints = 3;
        private const float BeamLengthChangeFactor = 0.75f;
        private Vector2 beamVector = Vector2.Zero;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.alpha = 0;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
            Projectile.timeLeft = Lifetime;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Projectile.velocity != Vector2.Zero)
            {
                beamVector = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Projectile.rotation = beamVector.ToRotation() - MathHelper.PiOver2;
                Projectile.velocity = beamVector;
                if (Projectile.localAI[0] == 0f)
                    SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.72f, Pitch = -0.18f }, Projectile.Center);
            }

            Projectile.localAI[0]++;
            Projectile.scale = (float)Math.Sin(Projectile.localAI[0] * MathHelper.Pi / Lifetime) * 10f * MaxBeamScale;
            if (Projectile.scale > MaxBeamScale)
                Projectile.scale = MaxBeamScale;

            float[] laserScanResults = new float[NumSamplePoints];
            float scanWidth = Projectile.width * Math.Max(Projectile.scale, 0.01f);
            Collision.LaserScan(Projectile.Center, beamVector, BeamTileCollisionWidth * scanWidth, MaxBeamLength, laserScanResults);
            float avg = 0f;
            for (int i = 0; i < laserScanResults.Length; i++)
                avg += laserScanResults[i];
            avg /= NumSamplePoints;
            Projectile.ai[0] = MathHelper.Lerp(Projectile.ai[0], avg, BeamLengthChangeFactor);
            Projectile.localAI[1] = Projectile.ai[0];

            ProduceBeamDust();
            DelegateMethods.v3_1 = new Vector3(0.9f, 0.3f, 0.3f);
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + beamVector * Projectile.ai[0], Projectile.width * Projectile.scale, DelegateMethods.CastLight);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (projHitbox.Intersects(targetHitbox))
                return true;

            float _ = float.NaN;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + beamVector * Projectile.ai[0], BeamHitboxCollisionWidth * Projectile.scale, ref _);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.HitDirectionOverride = (Projectile.Center.X < target.Center.X).ToDirectionInt();

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 420);

        public override void OnKill(int timeLeft)
        {
            if (beamVector == Vector2.Zero || Projectile.ai[0] <= 12f)
                return;

            if (Projectile.owner == Main.myPlayer)
            {
                const float segmentLength = 6f * 16f;
                Vector2 normal = beamVector.RotatedBy(MathHelper.PiOver2);

                for (float distance = 72f; distance < Projectile.ai[0] - 24f; distance += segmentLength)
                {
                    Vector2 segmentPosition = Projectile.Center + beamVector * distance;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 velocity = normal * side * Main.rand.NextFloat(9.5f, 12.5f);
                        int proj = Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            segmentPosition + normal * side * 18f,
                            velocity,
                            ModContent.ProjectileType<PFBrimstoneElemental_Barrage>(),
                            Math.Max(1, (int)(Projectile.damage * 0.42f)),
                            Projectile.knockBack * 0.45f,
                            Projectile.owner);

                        if (proj >= 0 && proj < Main.maxProjectiles)
                            Main.projectile[proj].timeLeft = 450;

                        PFLeftEffectRules.ApplyTheme(proj, (PristineFuryMark)(int)Projectile.ai[2]);
                    }
                }
            }

            if (Main.dedServ)
                return;

            Color color = GetBeamColor();
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center + beamVector * Projectile.ai[0], Vector2.Zero, color, Vector2.One, 0f, 0.18f, 0.88f, 18));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (beamVector == Vector2.Zero)
                return false;

            Texture2D startTexture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Texture2D midTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/BrimstoneRayMid").Value;
            Texture2D endTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/BrimstoneRayEnd").Value;
            float rayDrawLength = Projectile.ai[0];
            Color baseColor = new Color(255, 255, 255, 0) * 0.9f * Projectile.Opacity;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(startTexture, drawPosition, null, baseColor, Projectile.rotation, startTexture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            rayDrawLength -= (startTexture.Height / 2f + endTexture.Height) * Projectile.scale;

            Vector2 segmentCenter = Projectile.Center + beamVector * Projectile.scale * startTexture.Height / 2f;
            if (rayDrawLength > 0f)
            {
                float raySegment = 0f;
                Rectangle drawRectangle = new(0, 0, midTexture.Width, midTexture.Height);
                while (raySegment + 1f < rayDrawLength)
                {
                    if (rayDrawLength - raySegment < drawRectangle.Height)
                        drawRectangle.Height = (int)(rayDrawLength - raySegment);

                    Main.EntitySpriteDraw(midTexture, segmentCenter - Main.screenPosition, drawRectangle, baseColor, Projectile.rotation, new Vector2(drawRectangle.Width * 0.5f, 0f), Projectile.scale, SpriteEffects.None, 0f);
                    raySegment += drawRectangle.Height * Projectile.scale;
                    segmentCenter += beamVector * drawRectangle.Height * Projectile.scale;
                    drawRectangle.Y += midTexture.Height;
                    if (drawRectangle.Y + drawRectangle.Height > midTexture.Height)
                        drawRectangle.Y = 0;
                }
            }

            Main.EntitySpriteDraw(endTexture, segmentCenter - Main.screenPosition, null, baseColor, Projectile.rotation, endTexture.Frame(1, 1, 0, 0).Top(), Projectile.scale, SpriteEffects.None, 0f);

            return false;
        }

        private Color GetBeamColor() => new Color(255, 70, 70, 0);

        private void ProduceBeamDust()
        {
            if (Main.dedServ || beamVector == Vector2.Zero || Projectile.ai[0] <= 20f)
                return;

            Color beamColor = GetBeamColor();
            Vector2 laserEndPos = Projectile.Center + beamVector * (Projectile.ai[0] - 14.5f * Projectile.scale);
            for (int i = 0; i < 2; i++)
            {
                float dustAngle = Projectile.rotation + (Main.rand.NextBool() ? 1f : -1f) * MathHelper.PiOver2;
                Vector2 dustVel = dustAngle.ToRotationVector2() * Main.rand.NextFloat(1f, 1.8f);
                Dust dust = Dust.NewDustPerfect(laserEndPos, (int)CalamityDusts.Brimstone, dustVel, 0, default, 1.7f);
                dust.noGravity = true;
                dust.scale *= Projectile.scale;
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 pos = Projectile.Center + beamVector * Main.rand.NextFloat(Projectile.ai[0]) + beamVector.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-10f, 10f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(pos, Main.rand.NextVector2Circular(0.4f, 0.4f), false, 12, Main.rand.NextFloat(0.28f, 0.56f), Color.Lerp(PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92)), Color.White, Main.rand.NextFloat(0.08f, 0.36f)), true, true));
            }

            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 70, 70));
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = Projectile.Center + beamVector * Main.rand.NextFloat(Projectile.ai[0]);
                Vector2 normalOffset = beamVector.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-16f, 16f);
                Vector2 vel = beamVector * Main.rand.NextFloat(0.35f, 0.95f) + Main.rand.NextVector2Circular(0.18f, 0.18f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    pos + normalOffset,
                    vel,
                    false,
                    Main.rand.Next(9, 15),
                    Main.rand.NextFloat(0.34f, 0.72f),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.28f, 0.62f)),
                    true,
                    false,
                    true));

                if (Main.rand.NextBool(2))
                {
                    Dust squash = Dust.NewDustPerfect(pos + normalOffset * 0.6f, ModContent.DustType<SquashDust>(), beamVector * Main.rand.NextFloat(1.2f, 2.6f));
                    squash.noGravity = true;
                    squash.scale = Main.rand.NextFloat(1.1f, 1.5f);
                    squash.color = theme;
                    squash.fadeIn = 1.5f;
                }
            }
        }

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + beamVector * Projectile.ai[0], Projectile.width * Projectile.scale, DelegateMethods.CutTiles);
        }
    }
}
