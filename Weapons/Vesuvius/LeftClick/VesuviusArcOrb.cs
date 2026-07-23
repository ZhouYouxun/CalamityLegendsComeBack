using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.DStage3;
using CalamityLegendsComeBack.Weapons.Vesuvius.RightClick;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick
{
    // The left-click payoff. Every charge release fires a single orange light orb — the same
    // muzzle-concentrated orb Calamity's Arc Nova Diffuser draws at its gun tip, recoloured to
    // the volcano palette. The orb's tier (ai[0], 0-4) decides how much it does on top of simply
    // flying and burning:
    //   tier 0  weak tap orb, no extras
    //   tier 1  full orange orb, ignites on hit
    //   tier 2  + gently homes onto the nearest target
    //   tier 3  + sheds volcanic fireballs along its flight, and erupts on impact
    //   tier 4  + tightest homing, faster shedding, and a cataclysmic eruption on impact
    public class VesuviusArcOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Tier => (int)MathHelper.Clamp(Projectile.ai[0], 0f, 4f);
        private bool DirectHit { get => Projectile.localAI[1] > 0f; set => Projectile.localAI[1] = value ? 1f : 0f; }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale = Tier switch
                {
                    <= 0 => 0.7f,
                    1 => 0.95f,
                    2 => 1.12f,
                    3 => 1.32f,
                    _ => 1.55f
                };
                int size = (int)(24f * Projectile.scale);
                Projectile.Resize(size, size);
                Projectile.penetrate = 1 + Tier;
            }

            Projectile.localAI[0]++;
            // Round orb — rotation only drives the spinning ring in PreDraw.
            Projectile.rotation += 0.32f;

            float lightPower = 0.55f + Tier * 0.16f;
            Lighting.AddLight(Projectile.Center, lightPower, lightPower * 0.42f, lightPower * 0.08f);

            HomeInOnTarget();
            ShedFireballs();
            SpawnOrbTrail();
        }

        private void HomeInOnTarget()
        {
            // Tier 0-1 fly true. Higher tiers acquire the nearest target after a short arm delay
            // so the orb still leaves the muzzle heading where the player aimed.
            if (Tier < 2 || Projectile.localAI[0] < 8f)
                return;

            NPC target = FindTarget(560f + Tier * 130f);
            if (target == null)
                return;

            float turn = Tier >= 4 ? 0.088f : Tier >= 3 ? 0.062f : 0.04f;
            float speed = Projectile.velocity.Length();
            if (speed < 0.01f)
                return;

            Vector2 desired = Projectile.SafeDirectionTo(target.Center + target.velocity * 6f) * speed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, turn).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
        }

        private void ShedFireballs()
        {
            if (Tier < 3 || Projectile.owner != Main.myPlayer)
                return;

            int interval = Tier >= 4 ? 9 : 14;
            if (Projectile.localAI[0] % interval != 0f)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2) * (Main.rand.NextBool() ? 1f : -1f);
            Vector2 velocity = -forward * Main.rand.NextFloat(1.5f, 3f) + side * Main.rand.NextFloat(3.5f, 6.5f);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                velocity,
                ModContent.ProjectileType<VesuviusFaultFireball>(),
                Math.Max(1, (int)(Projectile.damage * 0.26f)),
                Projectile.knockBack * 0.3f,
                Projectile.owner,
                Tier);
        }

        private void SpawnOrbTrail()
        {
            if (Main.dedServ)
                return;

            Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitY);

            if (Main.rand.NextBool(2))
            {
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.InfernoFork,
                    backward.RotatedByRandom(0.35f) * Main.rand.NextFloat(1f, 2.8f),
                    70,
                    Main.rand.NextBool(3) ? VesuviusProjectileVisuals.HotWhite : VesuviusProjectileVisuals.LavaOrange,
                    Main.rand.NextFloat(0.9f, 1.5f) * Projectile.scale);
                ember.noGravity = true;
            }

            if (Main.rand.NextBool(Tier >= 3 ? 3 : 5))
                VesuviusProjectileVisuals.SpawnMoltenBloom(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    Main.rand.NextFloat(14f, 24f + Tier * 5f),
                    0.5f);
        }

        private NPC FindTarget(float range)
        {
            NPC best = null;
            float bestDistance = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance < bestDistance && Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1))
                {
                    bestDistance = distance;
                    best = npc;
                }
            }

            return best;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180 + Tier * 60);
            DirectHit = true;
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.45f + Tier * 0.06f, Pitch = -0.2f - Tier * 0.03f }, Projectile.Center);
                VesuviusProjectileVisuals.SpawnMoltenImpact(Projectile.Center, 0.55f + Tier * 0.16f, Tier >= 3);
            }

            // Tier 3-4 detonate into the existing thermal-core eruption (stage 2 = 200px blast,
            // stage 3 = cataclysmic 300px), so higher charges pay off with a real explosion while
            // reusing the weapon's own effect rather than a new one.
            if (Tier >= 3 && Projectile.owner == Main.myPlayer)
            {
                int blastStage = Tier >= 4 ? 3 : 2;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<VesuviusThermalCoreBlast>(),
                    Math.Max(1, (int)(Projectile.damage * (Tier >= 4 ? 0.9f : 0.7f))),
                    Projectile.knockBack * 1.2f,
                    Projectile.owner,
                    blastStage,
                    DirectHit ? 1f : 0f,
                    Projectile.velocity.ToRotation());
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D bloomRing = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * (9f + Tier));
            float scale = Projectile.scale * (0.9f + pulse * 0.1f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // Molten afterimage under the orb.
            for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                float t = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(bloom, oldCenter - Main.screenPosition, null,
                    VesuviusProjectileVisuals.AdditiveColor(VesuviusProjectileVisuals.LavaOrange) * (t * t * 0.4f),
                    0f, bloom.Size() * 0.5f, scale * 0.5f * t, SpriteEffects.None);
            }

            // The Arc Nova muzzle orb, orange: wide halo, gold body, tight white hotspot, and a
            // slow rotating ring — same layered-bloom recipe, volcano colours.
            Main.EntitySpriteDraw(bloom, drawPos, null, VesuviusProjectileVisuals.AdditiveColor(VesuviusProjectileVisuals.LavaOrange) * 0.6f,
                0f, bloom.Size() * 0.5f, scale * 0.95f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPos, null, VesuviusProjectileVisuals.AdditiveColor(VesuviusProjectileVisuals.LavaGold) * 0.66f,
                0f, bloom.Size() * 0.5f, scale * 0.6f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloomRing, drawPos, null, VesuviusProjectileVisuals.AdditiveColor(VesuviusProjectileVisuals.LavaGold) * (0.32f + pulse * 0.16f),
                Projectile.rotation, bloomRing.Size() * 0.5f, scale * 0.5f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPos, null, VesuviusProjectileVisuals.AdditiveColor(VesuviusProjectileVisuals.HotWhite) * 0.9f,
                0f, bloom.Size() * 0.5f, scale * 0.32f, SpriteEffects.None);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
