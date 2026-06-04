using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.EStage4
{
    public class VesuviusHomingCinder : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/Magic/RancorSmallCinder";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 3;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[1] == 0f)
            {
                Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
                Projectile.localAI[1] = 1f;
            }

            Projectile.localAI[0]++;
            NPC target = FindTarget(760f);
            if (target != null && Projectile.localAI[0] > 10f)
            {
                Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center + target.velocity * 8f) * 15.5f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.075f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.78f, 0.24f, 0.06f);

            VesuviusProjectileVisuals.SpawnCinderTrail(Projectile, 1f);
        }

        private NPC FindTarget(float range)
        {
            NPC bestTarget = null;
            float bestDistance = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = npc;
                }
            }

            return bestTarget;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 210);
        }

        public override void OnKill(int timeLeft)
        {
            VesuviusProjectileVisuals.SpawnCinderImpact(Projectile.Center, 0.85f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], VesuviusProjectileVisuals.LavaOrange * 0.78f);
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            float pulse = 0.5f + 0.5f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 18f + Projectile.identity);

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                VesuviusProjectileVisuals.LavaOrange with { A = 0 } * (0.34f + pulse * 0.12f),
                Projectile.rotation,
                bloom.Size() * 0.5f,
                Projectile.scale * (0.3f + pulse * 0.06f),
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White with { A = 0 } * 0.36f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.17f,
                SpriteEffects.None);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.Lerp(Color.White, VesuviusProjectileVisuals.LavaGold, 0.25f), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale * 1.08f, SpriteEffects.None);
            return false;
        }
    }
}
