using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    // Kept alive by the spinning holdout. This extends the damaging contact zone outward
    // after Duke Fishron without enlarging the blade-disc VFX itself.
    internal sealed class BrinyBaron_RightSpinOuterBubble : ModProjectile
    {
        private float OrbitPhase => Projectile.ai[0];
        private float OrbitDirection => Projectile.ai[1] < 0f ? -1f : 1f;

        public override string Texture => "CalamityMod/Particles/BloomCircle";

        public override void SetDefaults()
        {
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!Main.player.IndexInRange(Projectile.owner) || !Main.player[Projectile.owner].active)
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[Projectile.owner];
            float angle = OrbitPhase + Main.GlobalTimeWrappedHourly * 5.2f * OrbitDirection;
            float radius = BB_Balance.LeftClickCoreHitboxSize * 0.55f * BB_Balance.RightSpinPostFishronBubbleRadiusMultiplier;
            Projectile.Center = owner.Center + angle.ToRotationVector2() * radius;
            Projectile.rotation += 0.12f * OrbitDirection;

            if (!Main.dedServ && Main.GameUpdateCount % 2 == 0)
                SpawnHeavyWaterSmoke();
        }

        private void SpawnHeavyWaterSmoke()
        {
            Vector2 outward = (Projectile.Center - Main.player[Projectile.owner].Center).SafeNormalize(Vector2.UnitX);
            Dust water = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(16f, 16f), DustID.Water, outward * Main.rand.NextFloat(0.6f, 2.1f), 110, new Color(85, 190, 255), Main.rand.NextFloat(1.1f, 1.55f));
            water.noGravity = true;
            Dust smoke = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f, 20f), DustID.Smoke, outward.RotatedByRandom(0.7f) * Main.rand.NextFloat(0.4f, 1.5f), 145, Color.Lerp(new Color(75, 155, 220), Color.White, 0.45f), Main.rand.NextFloat(1.25f, 1.8f));
            smoke.noGravity = true;
            if (Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(Projectile.Center, outward * 0.45f, false, 13, 0.3f, new Color(165, 235, 255)));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 120);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, drawPosition, null, new Color(100, 215, 255, 0) * 0.42f, Projectile.rotation, texture.Size() * 0.5f, 0.55f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White * 0.28f, -Projectile.rotation * 1.4f, texture.Size() * 0.5f, 0.28f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
