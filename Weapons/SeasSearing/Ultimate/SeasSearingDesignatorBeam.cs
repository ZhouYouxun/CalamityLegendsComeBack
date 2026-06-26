using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal sealed class SeasSearingDesignatorBeam : ModProjectile, ILocalizedModType
    {
        private const float BeamLength = 2400f;
        private const int   MaxTime    = 42;
        private bool    markerCreated;
        private Vector2 targetPoint;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width      = 8;
            Projectile.height     = 8;
            Projectile.penetrate  = -1;
            Projectile.timeLeft   = MaxTime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead) { Projectile.Kill(); return; }

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            Projectile.rotation = direction.ToRotation();
            targetPoint = FindDesignationPoint(Projectile.Center, direction, BeamLength);

            if (!markerCreated && Projectile.timeLeft <= MaxTime - 22 && Main.myPlayer == Projectile.owner)
            {
                markerCreated = true;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), targetPoint, Vector2.Zero,
                    ModContent.ProjectileType<SeasSearingNukeMarker>(),
                    System.Math.Max(1, owner.GetWeaponDamage(owner.HeldItem) * 18),
                    0f, Projectile.owner);
                SoundEngine.PlaySound(SoundID.Item124 with { Volume = 0.65f, Pitch = -0.4f }, targetPoint);
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Vector2.Lerp(Projectile.Center, targetPoint, Main.rand.NextFloat()),
                    DustID.GemDiamond,
                    -direction * Main.rand.NextFloat(0.2f, 1.1f),
                    120, SeasSearingPalette.RadioactiveCyan, Main.rand.NextFloat(0.45f, 0.8f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 start  = Projectile.Center;
            Vector2 end    = targetPoint == Vector2.Zero ? Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * BeamLength : targetPoint;
            Vector2 line   = end - start;
            float   length = line.Length();
            if (length <= 2f) return false;

            Texture2D pixel    = TextureAssets.MagicPixel.Value;
            Vector2   origin   = new(0f, 0.5f);
            float     opacity  = MathHelper.Clamp(Projectile.timeLeft / 12f, 0f, 1f);
            float     rotation = line.ToRotation();
            Color cyan  = (SeasSearingPalette.RadioactiveCyan with { A = 0 }) * opacity;
            Color white = (Color.White with { A = 0 }) * opacity;

            Main.EntitySpriteDraw(pixel, start - Main.screenPosition, new Rectangle(0, 0, 1, 1), cyan  * 0.46f, rotation, origin, new Vector2(length, 7f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(pixel, start - Main.screenPosition, new Rectangle(0, 0, 1, 1), white * 0.74f, rotation, origin, new Vector2(length, 2f), SpriteEffects.None, 0);
            return false;
        }

        private Vector2 FindDesignationPoint(Vector2 start, Vector2 direction, float length)
        {
            Vector2 end        = start + direction * length;
            NPC     bestTarget = null;
            float   bestDist   = length;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy()) continue;
                float collisionPoint = 0f;
                if (!Collision.CheckAABBvLineCollision(npc.Hitbox.TopLeft(), npc.Hitbox.Size(), start, end, 12f, ref collisionPoint)) continue;
                if (collisionPoint < bestDist) { bestDist = collisionPoint; bestTarget = npc; }
            }

            return bestTarget != null ? bestTarget.Center : end;
        }
    }
}
