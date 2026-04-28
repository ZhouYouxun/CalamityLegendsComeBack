using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.A_Pre8
{
    public class Copper_ChainLightning : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_466";

        private int RemainingJumps => (int)Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 24;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.18f, 0.28f, 0.42f));

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    DustID.Electric,
                    Main.rand.NextVector2Circular(1.6f, 1.6f),
                    100,
                    new Color(155, 220, 255),
                    Main.rand.NextFloat(0.95f, 1.35f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (RemainingJumps <= 0 || Projectile.owner != Main.myPlayer)
                return;

            NPC nextTarget = FindNextTarget(target.whoAmI, 280f);
            if (nextTarget == null)
                return;

            Vector2 velocity = (nextTarget.Center - target.Center).SafeNormalize(Vector2.UnitX) * 18f;
            int nextBolt = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                velocity,
                Type,
                (int)(Projectile.damage * 0.85f),
                Projectile.knockBack,
                Projectile.owner,
                target.whoAmI,
                RemainingJumps - 1);

            if (nextBolt >= 0 && nextBolt < Main.maxProjectiles)
                Main.projectile[nextBolt].DamageType = Projectile.DamageType;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, texture.Size() * 0.5f, 0.75f, SpriteEffects.None, 0f);
            return false;
        }

        private NPC FindNextTarget(int ignoredTarget, float range)
        {
            NPC target = null;
            float sqrRange = range * range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.whoAmI == ignoredTarget || !npc.CanBeChasedBy(Projectile))
                    continue;

                float sqrDistance = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (sqrDistance >= sqrRange)
                    continue;

                sqrRange = sqrDistance;
                target = npc;
            }

            return target;
        }
    }
}
