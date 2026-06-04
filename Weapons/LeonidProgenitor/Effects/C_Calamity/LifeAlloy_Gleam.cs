using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.C_Calamity
{
    public class LifeAlloy_Gleam : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int TargetIndex => (int)Projectile.ai[0];
        private int ColorStyle => (int)Projectile.ai[1];
        private Color GleamColor => ColorStyle switch
        {
            0 => new Color(90, 245, 255, 0),
            1 => new Color(255, 92, 215, 0),
            _ => new Color(126, 255, 118, 0)
        };

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Color color = GleamColor;
            Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.35f);

            if (Projectile.localAI[0] < 18f)
            {
                Projectile.velocity *= 0.92f;
            }
            else
            {
                if (Projectile.localAI[1] <= 0f)
                {
                    Projectile.localAI[1] = Main.rand.Next(16, 29);
                    float turn = Main.rand.NextFloat(0.32f, 0.82f) * (Main.rand.NextBool() ? 1f : -1f);
                    Projectile.velocity = Projectile.velocity.RotatedBy(turn);
                }

                Projectile.localAI[1]--;
                NPC target = GetTarget();
                if (target != null && Projectile.localAI[0] > 42f)
                {
                    Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 12.5f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.085f);
                }
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowTorch, Main.rand.NextVector2Circular(0.8f, 0.8f), 100, color, Main.rand.NextFloat(0.55f, 0.9f));
                dust.noGravity = true;
            }
        }

        public override bool? CanDamage() => Projectile.localAI[0] < 24f ? false : null;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D core = ModContent.Request<Texture2D>("CalamityMod/Particles/LargeBloom").Value;
            Color color = GleamColor;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(bloom, drawPosition, null, color * completion * 0.42f, Projectile.rotation, bloom.Size() * 0.5f, 0.18f * completion, SpriteEffects.None, 0f);
            }

            Main.EntitySpriteDraw(core, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, core.Size() * 0.5f, 0.12f, SpriteEffects.None, 0f);
            return false;
        }

        private NPC GetTarget()
        {
            if (TargetIndex >= 0 && TargetIndex < Main.maxNPCs)
            {
                NPC target = Main.npc[TargetIndex];
                if (target.active && target.CanBeChasedBy(Projectile))
                    return target;
            }

            NPC closest = null;
            float sqrRange = 640f * 640f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float sqrDistance = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (sqrDistance >= sqrRange)
                    continue;

                sqrRange = sqrDistance;
                closest = npc;
            }

            return closest;
        }
    }
}
