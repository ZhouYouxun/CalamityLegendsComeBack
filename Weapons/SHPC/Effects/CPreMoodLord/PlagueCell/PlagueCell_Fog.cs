using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.PlagueCell
{
    public class PlagueCell_Fog : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/Summon/SmallAresArms/MinionPlasmaGas";

        private readonly float randomRotation1 = Main.rand.NextFloat(MathHelper.TwoPi);
        private readonly float randomRotation2 = Main.rand.NextFloat(MathHelper.TwoPi);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 480;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 78;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.scale = MathHelper.Lerp(0.62f, 1.34f, Utils.GetLerpValue(0f, 34f, Projectile.localAI[0], true));

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.42f, Projectile.height * 0.42f),
                    DustID.GemEmerald,
                    Main.rand.NextVector2Circular(0.9f, 0.9f),
                    120,
                    Color.LimeGreen,
                    Main.rand.NextFloat(1.1f, 2.2f));
                dust.noGravity = true;
            }
        }

        public override bool? CanDamage() => Projectile.timeLeft > 18 ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.width * Projectile.scale * 0.42f, targetHitbox);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Plague>(), 90);
            target.AddBuff(BuffID.Poisoned, 90);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 16f, Projectile.localAI[0], true);
            Vector2 scale = Projectile.Size / texture.Size() * Projectile.scale * 1.25f;
            Color drawColor = new Color(72, 160, 22, 0) * opacity;

            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, randomRotation1 + Projectile.localAI[0] * 0.006f, origin, scale, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor * 0.72f, randomRotation2 - Projectile.localAI[0] * 0.004f, origin, scale * 0.88f, SpriteEffects.FlipHorizontally);
            return false;
        }
    }
}
