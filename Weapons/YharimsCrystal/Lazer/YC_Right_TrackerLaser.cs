using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal
{
    public class YC_Right_TrackerLaser : ModProjectile, ILocalizedModType
    {
        private const float Range = YC_RightHoldOut.MaxTargetRange;

        public new string LocalizationCategory => "Projectiles";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Vector2 LaserStart => Projectile.Center;
        private Vector2 LaserEnd
        {
            get => new(Projectile.localAI[0], Projectile.localAI[1]);
            set
            {
                Projectile.localAI[0] = value.X;
                Projectile.localAI[1] = value.Y;
            }
        }

        private float Fade => (float)System.Math.Pow(Utils.GetLerpValue(0f, 16f, Projectile.timeLeft), 3f);
        private int TargetNpcIndex => (int)Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 16;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * Range;

            if (TargetNpcIndex >= 0 && TargetNpcIndex < Main.maxNPCs)
            {
                NPC npc = Main.npc[TargetNpcIndex];
                if (npc.active && npc.CanBeChasedBy(Projectile))
                    end = npc.Center;
            }

            LaserEnd = end;
        }

        public override bool? CanHitNPC(NPC target) => Projectile.numHits > 0 ? false : null;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                LaserStart,
                LaserEnd,
                14f,
                ref collisionPoint);
        }

        public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles, System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles, System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (LaserEnd == Vector2.Zero)
                return false;

            Texture2D lineTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;
            Texture2D glowTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float distance = Vector2.Distance(LaserStart, LaserEnd);
            Vector2 direction = Vector2.Normalize(LaserEnd - LaserStart);
            int drawSeparation = 10;

            for (int i = drawSeparation; i < distance - drawSeparation; i += drawSeparation)
            {
                float completion = MathHelper.Lerp(0.8f, 3f, 1f - i / distance);
                Vector2 drawPosition = LaserStart - Main.screenPosition + direction * i;

                for (int layer = 0; layer < 2; layer++)
                {
                    Color color = layer == 1 ? Color.White : new Color(255, 232, 160);
                    float width = layer == 1 ? 0.24f : 0.8f;
                    Main.EntitySpriteDraw(
                        lineTex,
                        drawPosition,
                        null,
                        color with { A = 0 } * Fade,
                        direction.ToRotation() + MathHelper.PiOver2,
                        lineTex.Size() * 0.5f,
                        new Vector2(width * MathHelper.Max(Fade, 0.28f) * completion, 1.05f) * 0.01f,
                        SpriteEffects.None,
                        0f);
                }
            }

            Main.EntitySpriteDraw(
                glowTex,
                LaserStart - Main.screenPosition,
                null,
                new Color(255, 228, 150, 0),
                Projectile.rotation,
                glowTex.Size() * 0.5f,
                0.28f * Fade,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                glowTex,
                LaserStart - Main.screenPosition,
                null,
                new Color(255, 255, 255, 0) * 0.65f,
                Projectile.rotation,
                glowTex.Size() * 0.5f,
                0.14f * Fade,
                SpriteEffects.None,
                0f);

            return false;
        }
    }
}
