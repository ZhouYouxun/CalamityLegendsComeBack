using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.D_New6
{
    public class Mythril_OrbitFlame : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_687";

        private Player Owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            if (Projectile.localAI[0] <= 26f)
            {
                float angle = Projectile.localAI[0] * 0.28f + Projectile.ai[0];
                Vector2 orbitOffset = angle.ToRotationVector2() * MathHelper.Lerp(44f, 20f, Projectile.localAI[0] / 26f);
                Projectile.Center = Owner.Center + orbitOffset;
            }
            else
            {
                NPC target = FindClosestNPC(640f);
                if (target != null)
                {
                    Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 13.5f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.12f);
                }
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.24f, 0.2f, 0.1f));

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, Main.rand.NextVector2Circular(1.4f, 1.4f), 100, default, Main.rand.NextFloat(0.85f, 1.15f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, new Color(255, 205, 125, 0), Projectile.rotation, texture.Size() * 0.5f, 0.52f, SpriteEffects.None, 0f);
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
