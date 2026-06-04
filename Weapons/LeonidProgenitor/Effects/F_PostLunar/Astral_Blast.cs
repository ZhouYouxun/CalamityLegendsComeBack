using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.F_PostLunar
{
    public class Astral_Blast : ModProjectile
    {
        public override string Texture => "CalamityMod/Particles/PlasmaExplosion";

        private float RotationSeed => Projectile.ai[0];
        private bool BlueCore => Projectile.ai[1] > 0.5f;

        public override void SetDefaults()
        {
            Projectile.width = 84;
            Projectile.height = 84;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 8;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.scale = MathHelper.Lerp(0.28f, 1.18f, 1f - Projectile.timeLeft / 8f);
            Lighting.AddLight(Projectile.Center, (BlueCore ? new Vector3(0.08f, 0.22f, 0.34f) : new Vector3(0.32f, 0.14f, 0.08f)) * 1.2f);

            if (Projectile.localAI[0] == 1f)
            {
                for (int i = 0; i < 18; i++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center,
                        Main.rand.NextBool() ? DustID.BlueTorch : DustID.OrangeTorch,
                        Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f),
                        100,
                        BlueCore ? new Color(96, 210, 255) : new Color(255, 142, 76),
                        Main.rand.NextFloat(0.9f, 1.45f));
                    dust.noGravity = true;
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, 52f * Projectile.scale, targetHitbox);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Color core = BlueCore ? new Color(90, 210, 255, 0) : new Color(255, 135, 76, 0);
            Color edge = BlueCore ? new Color(190, 120, 255, 0) : new Color(118, 84, 255, 0);
            float opacity = Utils.GetLerpValue(0f, 4f, Projectile.localAI[0], true) * Utils.GetLerpValue(0f, 4f, Projectile.timeLeft, true);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(texture, drawPosition, null, core * 0.62f * opacity, RotationSeed + Projectile.localAI[0] * 0.06f, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(ring, drawPosition, null, edge * 0.55f * opacity, -RotationSeed + Projectile.localAI[0] * 0.12f, ring.Size() * 0.5f, Projectile.scale * 0.48f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
