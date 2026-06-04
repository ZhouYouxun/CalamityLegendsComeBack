using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.F_PostLunar
{
    public class PostLunar_Shard : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_466";

        private int Style => (int)Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = Style == 1 ? 2 : 1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            NPC target = FindClosestNPC(Style == 1 ? 760f : 520f);
            if (target != null)
            {
                float homingStrength = Style == 1 ? 0.13f : 0.07f;
                Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * Projectile.velocity.Length();
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, homingStrength);
            }

            if (Style == 3)
                Projectile.velocity *= 0.992f;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, GetLightColor());

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted, Main.rand.NextVector2Circular(0.8f, 0.8f), 100, GetColor(), Main.rand.NextFloat(0.8f, 1.15f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            switch (Style)
            {
                case 0:
                    target.AddBuff(BuffID.Venom, 240);
                    break;
                case 1:
                    target.AddBuff(BuffID.MoonLeech, 60);
                    break;
                case 2:
                    target.AddBuff(BuffID.Daybreak, 120);
                    break;
                case 3:
                    target.AddBuff(BuffID.ShadowFlame, 180);
                    break;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Style == 3)
            {
                int blast = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Astral_Blast>(), Projectile.damage / 2, Projectile.knockBack, Projectile.owner);
                if (blast >= 0 && blast < Main.maxProjectiles)
                    Main.projectile[blast].DamageType = Projectile.DamageType;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, GetColor(), Projectile.rotation, texture.Size() * 0.5f, 0.6f, SpriteEffects.None, 0f);
            return false;
        }

        private Color GetColor() => Style switch
        {
            0 => new Color(148, 255, 132, 0),
            1 => new Color(124, 228, 255, 0),
            2 => new Color(255, 218, 122, 0),
            3 => new Color(176, 122, 255, 0),
            _ => Color.White
        };

        private Vector3 GetLightColor() => Style switch
        {
            0 => new Vector3(0.18f, 0.34f, 0.1f),
            1 => new Vector3(0.1f, 0.3f, 0.34f),
            2 => new Vector3(0.32f, 0.26f, 0.1f),
            3 => new Vector3(0.24f, 0.14f, 0.34f),
            _ => new Vector3(0.2f, 0.2f, 0.2f)
        };

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
