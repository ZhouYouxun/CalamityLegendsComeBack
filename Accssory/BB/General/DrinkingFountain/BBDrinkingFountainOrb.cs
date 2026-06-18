using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB
{
    internal sealed class BBDrinkingFountainOrb : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 1;
        }

        public override bool? CanDamage()
        {
            Player owner = Main.player[Projectile.owner];
            return owner.active && owner.statLife >= owner.statLifeMax2 ? null : false;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Vector2 target = owner.Center;
            if (owner.statLife >= owner.statLifeMax2)
            {
                NPC npc = FindNearestEnemy(760f);
                if (npc != null)
                    target = npc.Center;
            }

            Vector2 desiredVelocity = (target - Projectile.Center).SafeNormalize(Vector2.Zero) * 13f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.12f);
            Projectile.rotation += 0.22f;
            Lighting.AddLight(Projectile.Center, 0.05f, 0.24f, 0.34f);

            if (owner.statLife < owner.statLifeMax2 && Projectile.Hitbox.Intersects(owner.Hitbox))
            {
                int heal = 8;
                owner.statLife = Utils.Clamp(owner.statLife + heal, 0, owner.statLifeMax2);
                owner.HealEffect(heal);
                Projectile.Kill();
            }

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    DustID.Water,
                    -Projectile.velocity * 0.08f,
                    100,
                    new Color(110, 220, 255),
                    Main.rand.NextFloat(0.65f, 1f));
                dust.noGravity = true;
            }
        }

        private NPC FindNearestEnemy(float range)
        {
            NPC best = null;
            float bestDistance = range;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.lifeMax <= 5)
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                best = npc;
            }

            return best;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = TextureAssets.Extra[98].Value;
            Vector2 origin = bloom.Size() * 0.5f;
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float fade = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(
                    bloom,
                    Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
                    null,
                    new Color(90, 210, 255, 0) * fade * 0.45f,
                    Projectile.rotation,
                    origin,
                    0.16f * fade,
                    SpriteEffects.None);
            }

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White with { A = 0 } * 0.55f,
                Projectile.rotation,
                origin,
                0.18f,
                SpriteEffects.None);
            return false;
        }
    }
}
