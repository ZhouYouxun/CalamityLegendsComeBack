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
    // the volcano palette and given a molten meteor core.
    //
    // The orb pierces everything (penetrate -1) and PLOWS through a crowd. All the real payload
    // fires on each ENEMY HIT, and it escalates: intensity = Tier + how many enemies it has
    // already struck, so every successive hit erupts harder than the last —
    //   fireball fan  ->  pyroclastic spray  ->  thermal-core blast  ->  cataclysmic blast.
    // A higher charge tier simply starts that ladder further along, so a fully-charged orb is
    // already violent on its first hit and terrifying by its third.
    public class VesuviusArcOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Tier => (int)MathHelper.Clamp(Projectile.ai[0], 0f, 4f);
        private bool EmpoweredBySuperFlame => Projectile.ai[1] > 0f;
        private bool DirectHit { get => Projectile.localAI[1] > 0f; set => Projectile.localAI[1] = value ? 1f : 0f; }

        // How many enemies the orb has struck so far. Drives the escalating eruption ladder.
        private int hitCounter;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
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
            Projectile.penetrate = -1; // Pierces everything — the orb plows through the whole crowd.
            Projectile.timeLeft = 190;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale = Tier switch
                {
                    <= 0 => 0.85f,
                    1 => 1.15f,
                    2 => 1.4f,
                    3 => 1.7f,
                    _ => 2.05f
                };
                if (EmpoweredBySuperFlame)
                    Projectile.scale *= 1.45f;
                int size = (int)(30f * Projectile.scale);
                Projectile.Resize(size, size);
            }

            Projectile.localAI[0]++;
            // Round orb — rotation only drives the spinning ring/body in PreDraw.
            Projectile.rotation += 0.32f;

            float lightPower = 0.7f + Tier * 0.2f + hitCounter * 0.06f;
            Lighting.AddLight(Projectile.Center, lightPower, lightPower * 0.42f, lightPower * 0.08f);

            HomeInOnTarget();
            SpawnOrbTrail();
        }

        private void HomeInOnTarget()
        {
            // Tier 0-1 fly true. Higher tiers acquire the nearest target after a short arm delay
            // so the orb still leaves the muzzle heading where the player aimed, then curves back
            // into the crowd to keep plowing.
            if (Tier < 2 || Projectile.localAI[0] < 8f)
                return;

            NPC target = FindTarget(560f + Tier * 150f);
            if (target == null)
                return;

            float turn = Tier >= 4 ? 0.09f : Tier >= 3 ? 0.065f : 0.042f;
            float speed = Projectile.velocity.Length();
            if (speed < 0.01f)
                return;

            Vector2 desired = Projectile.SafeDirectionTo(target.Center + target.velocity * 6f) * speed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, turn).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
        }

        private void SpawnOrbTrail()
        {
            if (Main.dedServ)
                return;

            Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitY);

            for (int n = 0; n < (Tier >= 3 ? 2 : 1); n++)
            {
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    DustID.InfernoFork,
                    backward.RotatedByRandom(0.35f) * Main.rand.NextFloat(1f, 3f),
                    70,
                    Main.rand.NextBool(3) ? VesuviusProjectileVisuals.HotWhite : VesuviusProjectileVisuals.LavaOrange,
                    Main.rand.NextFloat(1f, 1.7f) * Projectile.scale);
                ember.noGravity = true;
            }

            if (Main.rand.NextBool(Tier >= 3 ? 3 : 5))
            {
                Particle ash = new SquareAshParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    backward * Main.rand.NextFloat(0.8f, 2.2f) + Main.rand.NextVector2Circular(0.35f, 0.35f),
                    Main.rand.Next(18, 28),
                    Main.rand.NextFloat(0.4f, 0.7f) * Projectile.scale,
                    Color.Lerp(VesuviusProjectileVisuals.AshGray, VesuviusProjectileVisuals.LavaOrange, 0.18f));
                GeneralParticleHandler.SpawnParticle(ash);
            }

            if (Main.rand.NextBool(4))
            {
                Particle fissure = new PointParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    backward.RotatedByRandom(0.24f) * Main.rand.NextFloat(1.6f, 3.8f),
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.36f, 0.6f) * Projectile.scale,
                    Main.rand.NextBool(4) ? VesuviusProjectileVisuals.HotWhite : VesuviusProjectileVisuals.LavaGold,
                    true);
                GeneralParticleHandler.SpawnParticle(fissure);
            }
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
            hitCounter++;
            DirectHit = true;

            // Intensity climbs with the charge tier AND with each enemy already struck.
            int power = Tier + hitCounter;
            target.AddBuff(BuffID.OnFire3, 120 + power * 45);

            Vector2 hitPos = target.Center;
            SpawnHitBurst(hitPos, power);

            if (Projectile.owner != Main.myPlayer)
                return;

            ReleaseEscalatingPayload(hitPos, power);
        }

        // The escalating on-hit ladder. Every hit does at least a fireball fan; the more the orb
        // has already hit (and the higher the tier), the more of the heavier set pieces it adds.
        private void ReleaseEscalatingPayload(Vector2 center, int power)
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            // Always: a radial fan of volcanic fireballs. Count/speed grow with power.
            int fan = Math.Min(3 + power, 10);
            for (int i = 0; i < fan; i++)
            {
                float angle = MathHelper.TwoPi * i / fan + Main.rand.NextFloat(-0.18f, 0.18f);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(6f, 11f + power);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    center,
                    velocity,
                    ModContent.ProjectileType<VesuviusFaultFireball>(),
                    Math.Max(1, (int)(Projectile.damage * 0.24f)),
                    Projectile.knockBack * 0.3f,
                    Projectile.owner,
                    power);
            }

            // power >= 3: pyroclastic flows spray outward along the ground.
            if (power >= 3)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        center + Vector2.UnitX * side * 10f,
                        new Vector2(side * Main.rand.NextFloat(6f, 10f), Main.rand.NextFloat(-1.5f, 1.5f)),
                        ModContent.ProjectileType<VesuviusPyroclasticFlow>(),
                        Math.Max(1, (int)(Projectile.damage * 0.3f)),
                        Projectile.knockBack * 0.3f,
                        Projectile.owner,
                        side);
                }
            }

            // Milestone eruptions — the whole point: each hit past the threshold detonates the
            // weapon's own thermal-core blast, upgrading to the cataclysmic 300px version once the
            // orb is truly wound up.
            if (power >= 7)
                SpawnThermalBlast(center, 3);
            else if (power >= 5)
                SpawnThermalBlast(center, 2);

            // Beyond that it keeps getting worse — a second, offset cataclysm at very high power.
            if (power >= 10)
                SpawnThermalBlast(center + Main.rand.NextVector2Circular(80f, 60f), 3);
        }

        private void SpawnThermalBlast(Vector2 center, int stage)
        {
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                center,
                Vector2.Zero,
                ModContent.ProjectileType<VesuviusThermalCoreBlast>(),
                Math.Max(1, (int)(Projectile.damage * (stage >= 3 ? 0.62f : 0.42f))),
                Projectile.knockBack * 1.1f,
                Projectile.owner,
                stage,
                DirectHit ? 1f : 0f,
                Projectile.velocity.ToRotation());
        }

        private void SpawnHitBurst(Vector2 center, int power)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = MathHelper.Clamp(0.4f + power * 0.05f, 0.4f, 0.95f), Pitch = -0.15f - power * 0.02f }, center);
            VesuviusProjectileVisuals.SpawnMoltenImpact(center, MathHelper.Clamp(0.5f + power * 0.16f, 0.5f, 2.6f), power >= 3);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center, Vector2.Zero, VesuviusProjectileVisuals.LavaGold, Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi), 0.16f, 1.1f + power * 0.28f, 20));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center, Vector2.Zero, VesuviusProjectileVisuals.LavaOrange, "CalamityMod/Particles/FlameExplosion",
                Vector2.One, Main.rand.NextFloat(-6f, 6f), 0.02f, MathHelper.Clamp(0.09f + power * 0.03f, 0.09f, 0.4f), 18, true));
        }

        public override void OnKill(int timeLeft)
        {
            int power = Tier + hitCounter;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f + Tier * 0.06f, Pitch = -0.25f - Tier * 0.03f }, Projectile.Center);
                VesuviusProjectileVisuals.SpawnMoltenImpact(Projectile.Center, MathHelper.Clamp(0.6f + power * 0.12f, 0.6f, 2.4f), power >= 3);
            }

            // Capstone: if it went out with any real charge behind it, leave a final eruption.
            if (Projectile.owner == Main.myPlayer && power >= 4)
                SpawnThermalBlast(Projectile.Center, power >= 7 ? 3 : 2);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D bloomRing = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D body = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMolten3").Value;
            Texture2D bodyGlow = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMoltenGlow3").Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * (9f + Tier));
            // Bolder: everything scaled up, an extra wide corona and a rotating ring layered in.
            float bodyScale = Projectile.scale * (0.9f + pulse * 0.04f);
            float bloomScale = Projectile.scale * (0.75f + pulse * 0.06f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // Molten afterimage under the orb — longer, brighter tail.
            for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                float t = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(bloom, oldCenter - Main.screenPosition, null,
                    VesuviusProjectileVisuals.AdditiveColor(VesuviusProjectileVisuals.LavaOrange) * (t * t * 0.4f),
                    0f, bloom.Size() * 0.5f, bloomScale * 0.6f * t, SpriteEffects.None);
            }

            // Wide orange corona, gold body, rotating ring, tight white hotspot — the Arc Nova
            // muzzle-orb recipe, blown up.
            Main.EntitySpriteDraw(bloom, drawPos, null, VesuviusProjectileVisuals.AdditiveColor(VesuviusProjectileVisuals.LavaOrange) * (0.5f + pulse * 0.12f),
                0f, bloom.Size() * 0.5f, bloomScale * 1.35f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPos, null, VesuviusProjectileVisuals.AdditiveColor(VesuviusProjectileVisuals.LavaGold) * 0.7f,
                0f, bloom.Size() * 0.5f, bloomScale * 0.8f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloomRing, drawPos, null, VesuviusProjectileVisuals.AdditiveColor(VesuviusProjectileVisuals.LavaGold) * (0.4f + pulse * 0.22f),
                Projectile.rotation, bloomRing.Size() * 0.5f, bloomScale * 0.85f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloomRing, drawPos, null, VesuviusProjectileVisuals.AdditiveColor(VesuviusProjectileVisuals.HotWhite) * 0.35f,
                -Projectile.rotation * 0.7f, bloomRing.Size() * 0.5f, bloomScale * 0.5f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPos, null, VesuviusProjectileVisuals.AdditiveColor(VesuviusProjectileVisuals.HotWhite) * 0.9f,
                0f, bloom.Size() * 0.5f, bloomScale * 0.42f, SpriteEffects.None);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            // Molten meteor core, spinning, with a short trailing echo and its glowmask on top.
            for (int i = Projectile.oldPos.Length - 1; i >= 2; i -= 2)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                float opacity = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length * 0.24f;
                Main.EntitySpriteDraw(body, oldCenter - Main.screenPosition, null, Color.Lerp(lightColor, Color.White, 0.3f) * opacity,
                    Projectile.rotation - i * 0.12f, body.Size() * 0.5f, bodyScale * 0.9f, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(body, drawPos, null, Color.Lerp(lightColor, Color.White, 0.55f), Projectile.rotation, body.Size() * 0.5f, bodyScale, SpriteEffects.None);
            Main.EntitySpriteDraw(bodyGlow, drawPos, null, Color.White * (0.75f + pulse * 0.2f), Projectile.rotation, bodyGlow.Size() * 0.5f, bodyScale, SpriteEffects.None);
            return false;
        }
    }
}
