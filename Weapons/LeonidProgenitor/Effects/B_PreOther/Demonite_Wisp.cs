using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.B_PreOther
{
    public class Demonite_Wisp : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_297";

        private bool CrimsonVariant => Projectile.ai[0] > 0.5f;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            NPC target = FindClosestNPC(520f);
            if (target != null)
            {
                Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 13f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.08f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Color wispColor = CrimsonVariant ? new Color(255, 105, 120) : new Color(146, 115, 255);
            Lighting.AddLight(Projectile.Center, wispColor.ToVector3() * 0.48f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted, Main.rand.NextVector2Circular(1.6f, 1.6f), 100, wispColor, Main.rand.NextFloat(0.85f, 1.2f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(CrimsonVariant ? BuffID.Ichor : BuffID.CursedInferno, 120);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Color drawColor = CrimsonVariant ? new Color(255, 148, 160, 0) : new Color(174, 142, 255, 0);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, drawColor, Projectile.rotation, texture.Size() * 0.5f, 0.6f, SpriteEffects.None, 0f);
            return false;
        }

        private NPC FindClosestNPC(float range)
        {
            NPC target = null;
            float sqrRange = range * range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float sqrDistance = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                if (sqrDistance >= sqrRange)
                    continue;

                sqrRange = sqrDistance;
                target = npc;
            }

            return target;
        }
    }
}
