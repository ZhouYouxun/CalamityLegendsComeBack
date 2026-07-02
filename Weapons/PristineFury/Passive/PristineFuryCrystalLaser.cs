using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.Passive
{
    internal sealed class PristineFuryCrystalLaser : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        // Head texture doubles as the start node for the laser beam.
        public override string Texture => "CalamityMod/Projectiles/Boss/ProvidenceHolyRay";

        private const int LifeTime = 100;

        // ai[0] = rotation per frame (signed; positive = clockwise in screen space)
        // ai[1] = owning crystal's Projectile.whoAmI
        private ref float RotPerFrame => ref Projectile.ai[0];
        private ref float CrystalIndex => ref Projectile.ai[1];

        // localAI[0] = timer, localAI[1] = smoothed laser length
        private ref float Timer => ref Projectile.localAI[0];
        private ref float LaserLength => ref Projectile.localAI[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifeTime + 5;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            // Anchor to the owning crystal
            int crystalIdx = (int)CrystalIndex;
            bool foundCrystal = false;
            if (crystalIdx >= 0 && crystalIdx < Main.maxProjectiles)
            {
                Projectile crystal = Main.projectile[crystalIdx];
                if (crystal.active && crystal.owner == Projectile.owner && crystal.type == ModContent.ProjectileType<PristineFuryCrystal>())
                {
                    Projectile.Center = crystal.Center;
                    foundCrystal = true;
                }
            }
            if (!foundCrystal)
            {
                // Fallback: hover above player (matches crystal position)
                Projectile.Center = owner.Center + new Vector2(0f, -300f * owner.gravDir);
            }

            if (Projectile.velocity == Vector2.Zero || Projectile.velocity.HasNaNs())
                Projectile.velocity = Vector2.UnitY;

            Timer++;
            if (Timer >= LifeTime)
            {
                Projectile.Kill();
                return;
            }

            // Scale: quick ramp-up, quick fade at end (matching ProvidenceHolyRay formula)
            Projectile.scale = (float)Math.Sin(Timer * MathHelper.Pi / LifeTime) * 10f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;

            // Rotate laser direction
            float velAngle = Projectile.velocity.ToRotation() + RotPerFrame;
            Projectile.rotation = velAngle - MathHelper.PiOver2;
            Projectile.velocity = velAngle.ToRotationVector2();

            // Scan for laser length
            float[] scan = new float[3];
            Collision.LaserScan(Projectile.Center, Projectile.velocity, Projectile.width * Projectile.scale, 2400f, scan);
            float rawLen = (scan[0] + scan[1] + scan[2]) / 3f;
            LaserLength = MathHelper.Lerp(LaserLength, rawLen, 0.5f);

            // Dust at tip
            if (!Main.dedServ)
            {
                Vector2 tipPos = Projectile.Center + Projectile.velocity * (LaserLength - 14f);
                for (int j = 0; j < 2; j++)
                {
                    float randDir = Projectile.velocity.ToRotation() + (Main.rand.NextBool() ? -1f : 1f) * MathHelper.PiOver2;
                    float randSpeed = (float)Main.rand.NextDouble() * 2f + 2f;
                    Vector2 dustVel = new Vector2((float)Math.Cos(randDir) * randSpeed, (float)Math.Sin(randDir) * randSpeed);
                    Dust d = Dust.NewDustPerfect(tipPos, DustID.GoldFlame, dustVel, 0, Main.rand.NextBool(3) ? Color.White : new Color(255, 224, 72), 1.5f);
                    d.noGravity = true;
                }
            }

            // Lighting along beam
            DelegateMethods.v3_1 = new Vector3(0.9f, 0.68f, 0.16f);
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, Projectile.width * Projectile.scale, DelegateMethods.CastLight);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.scale < 0.45f)
                return false;
            float dummy = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength,
                22f * Projectile.scale, ref dummy);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            byte a = (byte)(180 * Projectile.scale);
            return new Color(255, 224, 88, a);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero || Projectile.scale <= 0.01f)
                return false;

            Vector2 drawScale = new Vector2(Projectile.scale);

            Texture2D headTex = ModContent.Request<Texture2D>(Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Texture2D midTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayMid", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Texture2D endTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayEnd", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            Color baseColor = (new Color(255, 224, 88) with { A = 0 }) * Projectile.scale;
            Vector2 drawOrigin = Projectile.Center - Main.screenPosition;

            // Head node
            Main.EntitySpriteDraw(headTex, drawOrigin, null, baseColor, Projectile.rotation, headTex.Size() * 0.5f, drawScale, SpriteEffects.None, 0);

            float remaining = LaserLength - (headTex.Height * 0.5f + endTex.Height) * Projectile.scale;
            Vector2 segPos = Projectile.Center + Projectile.velocity * headTex.Height * 0.5f * Projectile.scale;

            // Mid segments
            if (remaining > 0f)
            {
                int animRow = ((int)(Main.GameUpdateCount / 3)) % 4;
                Rectangle drawRect = new Rectangle(0, 36 * animRow, midTex.Width, 36);
                float drawn = 0f;
                while (drawn + 1f < remaining)
                {
                    if (remaining - drawn < drawRect.Height)
                        drawRect.Height = (int)(remaining - drawn);

                    Main.EntitySpriteDraw(midTex, segPos - Main.screenPosition, drawRect, baseColor, Projectile.rotation,
                        new Vector2(drawRect.Width * 0.5f, 0f), drawScale, SpriteEffects.None, 0);

                    drawn += drawRect.Height * Projectile.scale;
                    segPos += Projectile.velocity * drawRect.Height * Projectile.scale;
                    drawRect.Y += 36;
                    if (drawRect.Y + drawRect.Height > midTex.Height)
                        drawRect.Y = 0;
                }
            }

            // End cap
            Main.EntitySpriteDraw(endTex, segPos - Main.screenPosition, null, baseColor, Projectile.rotation,
                endTex.Frame().Top(), drawScale, SpriteEffects.None, 0);

            return false;
        }

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, Projectile.width * Projectile.scale, DelegateMethods.CutTiles);
        }
    }
}
