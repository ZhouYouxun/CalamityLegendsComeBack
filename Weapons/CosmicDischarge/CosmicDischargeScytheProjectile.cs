using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeScytheProjectile : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityLegendsComeBack/Weapons/CosmicDischarge/镰刀弹幕用这个贴图";

        private ref float Mode => ref Projectile.ai[0];
        private ref float Time => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 150;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.coldDamage = true;
        }

        public override void AI()
        {
            if (Time == 0f && Mode >= 1f)
            {
                Projectile.penetrate = 4;
                Projectile.localNPCHitCooldown = 8;
            }

            Time++;
            Projectile.rotation += Projectile.direction * MathHelper.Lerp(0.18f, 0.6f, Utils.GetLerpValue(0f, 30f, Time, true));

            if (Mode == 0f)
            {
                NPC target = FindTarget(840f);
                if (target != null)
                {
                    Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center) * 16f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.09f);
                }
                else
                    Projectile.velocity *= 1.012f;
            }
            else
            {
                Projectile.velocity *= 1.03f;
                if (Projectile.velocity.Length() > 23f)
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 23f;
            }

            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.FrostGlowColor.ToVector3() * 0.32f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool() ? 67 : 187,
                    Projectile.velocity.RotatedByRandom(0.45f) * Main.rand.NextFloat(0.1f, 0.55f),
                    120,
                    CosmicDischargeCommon.FrostCoreColor,
                    Main.rand.NextFloat(0.95f, 1.35f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Nightwither>(), 180);
            target.AddBuff(ModContent.BuffType<GlacialState>(), 90);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color trailColor = Color.Lerp(CosmicDischargeCommon.FrostGlowColor, Color.White, 0.2f) * 0.45f;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float scale = Projectile.scale * MathHelper.Lerp(0.65f, 1f, i / (float)Projectile.oldPos.Length);
                Main.EntitySpriteDraw(texture, drawPosition, null, trailColor, Projectile.rotation, origin, scale, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, CosmicDischargeCommon.FrostCoreColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }

        private NPC FindTarget(float maxDistance)
        {
            NPC result = null;
            float closest = maxDistance;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance < closest)
                {
                    closest = distance;
                    result = npc;
                }
            }

            return result;
        }
    }
}
