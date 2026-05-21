using CalamityMod.Buffs.DamageOverTime;
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
        public override string Texture => "CalamityMod/Projectiles/LaserProj";

        private const int Lifetime = 24;
        private const float MaxBeamScale = 1.15f;
        private const float MaxBeamLength = 1100f;
        private const float BeamTileCollisionWidth = 1f;
        private const float BeamHitboxCollisionWidth = 16f;
        private const int NumSamplePoints = 3;
        private const float BeamLengthChangeFactor = 0.75f;
        private Vector2 beamVector = Vector2.Zero;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.alpha = 0;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            if (Projectile.velocity != Vector2.Zero)
            {
                beamVector = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.velocity = Vector2.Zero;
                SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.72f, Pitch = -0.18f }, Projectile.Center);
            }

            float power = Projectile.timeLeft / (float)Lifetime;
            Projectile.scale = MaxBeamScale * power;

            float[] laserScanResults = new float[NumSamplePoints];
            float scanWidth = Projectile.scale < 1f ? 1f : Projectile.scale;
            Collision.LaserScan(Projectile.Center, beamVector, BeamTileCollisionWidth * scanWidth, MaxBeamLength, laserScanResults);
            float avg = 0f;
            for (int i = 0; i < laserScanResults.Length; i++)
                avg += laserScanResults[i];
            avg /= NumSamplePoints;
            Projectile.ai[0] = MathHelper.Lerp(Projectile.ai[0], avg, BeamLengthChangeFactor);

            ProduceBeamDust();
            DelegateMethods.v3_1 = GetBeamColor().ToVector3() * power * 0.75f;
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
                            ModContent.ProjectileType<PFBrimstoneElemental_HellbornProj>(),
                            Math.Max(1, (int)(Projectile.damage * 0.42f)),
                            Projectile.knockBack * 0.45f,
                            Projectile.owner);

                        if (proj >= 0 && proj < Main.maxProjectiles)
                            Main.projectile[proj].timeLeft = 48;

                        PFLeftEffectRules.ApplyTheme(proj, (PristineFuryMark)(int)Projectile.ai[2]);
                    }
                }
            }

            if (Main.dedServ)
                return;

            Color color = GetBeamColor();
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center + beamVector * Projectile.ai[0], Vector2.Zero, color, Vector2.One, 0f, 0.18f, 0.64f, 18));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (beamVector == Vector2.Zero)
                return false;

            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            float beamLength = Projectile.ai[0] - 14.5f * Projectile.scale * Projectile.scale;
            Vector2 beamStartPos = Projectile.Center.Floor() + beamVector * Projectile.scale * 10.5f - Main.screenPosition;
            Vector2 beamEndPos = beamStartPos + beamVector * beamLength;
            Vector2 scaleVec = new(Projectile.scale);
            Utils.LaserLineFraming framing = new(DelegateMethods.RainbowLaserDraw);
            Color beamColor = GetBeamColor();

            DelegateMethods.f_1 = 1f;
            DelegateMethods.c_1 = beamColor * 0.82f * Projectile.Opacity;
            Utils.DrawLaser(Main.spriteBatch, tex, beamStartPos, beamEndPos, scaleVec, framing);

            for (int i = 0; i < 5; i++)
            {
                beamColor = Color.Lerp(beamColor, Color.White, 0.38f);
                scaleVec *= 0.84f;
                DelegateMethods.c_1 = beamColor * 0.24f * Projectile.Opacity;
                Utils.DrawLaser(Main.spriteBatch, tex, beamStartPos, beamEndPos, scaleVec, framing);
            }

            return false;
        }

        private Color GetBeamColor() => Color.Lerp(PFLeftEffectRules.GetThemeColor(Projectile, new Color(246, 55, 64)), Color.White, 0.18f) with { A = 64 };

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
                Dust dust = Dust.NewDustPerfect(laserEndPos, DustID.Torch, dustVel, 0, beamColor, 0.7f);
                dust.noGravity = true;
                dust.scale *= Projectile.scale;
            }

            if (Main.rand.NextBool(4))
            {
                Vector2 pos = Projectile.Center + beamVector * Main.rand.NextFloat(Projectile.ai[0]) + beamVector.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-8f, 8f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(pos, Main.rand.NextVector2Circular(0.4f, 0.4f), false, 12, Main.rand.NextFloat(0.45f, 0.72f), beamColor, true, true));
            }
        }

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + beamVector * Projectile.ai[0], Projectile.width * Projectile.scale, DelegateMethods.CutTiles);
        }
    }
}
