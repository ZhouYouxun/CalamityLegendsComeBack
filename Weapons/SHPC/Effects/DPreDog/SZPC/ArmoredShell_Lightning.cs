using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC
{
    internal class ArmoredShell_Lightning : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int MaxChainCount = 3;
        private ref float BounceUsed => ref Projectile.localAI[0];

        private int time;
        private float colorValue;
        private float sizeMult = 1f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = MaxChainCount + 1;
            Projectile.timeLeft = 200;
            Projectile.extraUpdates = 18;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            colorValue = MathHelper.Lerp(colorValue, 50f, 0.025f);
            Color usedColor = Color.Lerp(Color.Cyan, Color.Orchid, Utils.GetLerpValue(0f, 50f, colorValue));

            if (time == 0)
            {
                colorValue += 30f;
                sizeMult = Projectile.ai[1] <= 0f ? 1f : Projectile.ai[1];
            }

            float targetDist = Vector2.Distance(owner.Center, Projectile.Center);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, usedColor.ToVector3() * 0.58f);

            if (targetDist < 1400f && Projectile.timeLeft > 5)
                SpawnDragoonBoltFlightFX(usedColor);

            if (Projectile.ai[1] == 0.5f && Projectile.timeLeft == 1)
                SpawnSmallEndPulse();

            time++;
        }

        private void SpawnDragoonBoltFlightFX(Color usedColor)
        {
            Vector2 pos = Projectile.Center;

            // 这段才是闪电的核心
            if (Projectile.timeLeft % 4 == 0)
            {
                if (time < 120)
                {
                    // 这个是一个很粗的光效[我们选择只保留后者]
                    //float velMult = Projectile.ai[1] == 0.5f ? 0.2f : 3f * sizeMult;
                    //Particle spark = new CustomSpark(
                    //    pos,
                    //    Projectile.velocity * 1.2f * velMult,
                    //    "CalamityMod/Particles/GlowSpark",
                    //    false,
                    //    11,
                    //    0.15f * sizeMult,
                    //    usedColor,
                    //    new Vector2(2f, 0.8f),
                    //    true,
                    //    true,
                    //    shrinkSpeed: 1f);
                    //GeneralParticleHandler.SpawnParticle(spark);
                    //sizeMult *= 0.97f;
                }

                // 这个则是那个很细的闪电方向
                Particle bolt = new BoltParticle(
                    pos,
                    -Projectile.velocity * 0.05f,
                    false,
                    30,
                    0.6f,
                    usedColor,
                    new Vector2(1.8f, 0.8f),
                    true,
                    true,
                    false,
                    0.3f);
                GeneralParticleHandler.SpawnParticle(bolt);
            }

            if (Main.rand.NextBool(35))
            {
                Particle sideBolt = new BoltParticle(
                    pos,
                    Projectile.velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.3f, 1.9f),
                    false,
                    23,
                    Main.rand.NextFloat(0.2f, 0.25f),
                    usedColor,
                    new Vector2(1.8f, 0.8f),
                    true,
                    true,
                    false,
                    0.3f);
                GeneralParticleHandler.SpawnParticle(sideBolt);
            }

            if (Main.rand.NextBool(10))
            {
                Particle drainLine = new CustomSpark(
                    pos,
                    Projectile.velocity * Main.rand.NextFloat(-0.4f, 0.4f),
                    "CalamityMod/Particles/DrainLineBloom",
                    false,
                    80,
                    Main.rand.NextFloat(1.2f, 1.3f) * sizeMult,
                    usedColor,
                    new Vector2(1f, 4f),
                    true,
                    true);
                GeneralParticleHandler.SpawnParticle(drainLine);
            }

            if (time % 5 == 0)
            {
                Dust dust = Dust.NewDustPerfect(
                    pos,
                    DustID.FireworksRGB,
                    new Vector2(5f, 5f).RotatedByRandom(100f) * Main.rand.NextFloat(0.5f, 1f),
                    0,
                    default,
                    Main.rand.NextFloat(0.45f, 0.6f));
                dust.noGravity = true;
                dust.color = usedColor;
            }
        }

        private void SpawnSmallEndPulse()
        {
            for (int i = 0; i < 3; i++)
            {
                Particle orb = new CustomPulse(Projectile.Center, Vector2.Zero, Color.Cyan, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.1f, 1.48f, 15);
                GeneralParticleHandler.SpawnParticle(orb);
                Particle orb2 = new CustomPulse(Projectile.Center, Vector2.Zero, Color.White, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.1f, 0.925f, 15);
                GeneralParticleHandler.SpawnParticle(orb2);
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float damageMult = Utils.Remap(Projectile.numHits, 0f, 3f, 1f, 0.15f, true);
            modifiers.SourceDamage *= damageMult;
            if (IsTargetInsideBallSphere(target))
                modifiers.SourceDamage *= 1.2f;
            modifiers.Knockback *= 0f;
        }

        private static bool IsTargetInsideBallSphere(NPC target)
        {
            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (proj.type != ModContent.ProjectileType<ArmoredShell_Ball>())
                    continue;
                if (proj.Distance(target.Center) <= 20f * proj.scale)
                    return true;
            }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 300);
            SpawnDragoonBoltHitFX(target.Center, BounceUsed >= 1f ? 2.1f : 3f);

            if (BounceUsed >= MaxChainCount)
            {
                Projectile.Kill();
                return;
            }

            BounceUsed++;
            NPC nextTarget = FindBounceTarget(target);
            if (nextTarget is null)
            {
                Projectile.Kill();
                return;
            }

            float speed = MathHelper.Max(Projectile.velocity.Length(), 14.4f);
            Projectile.Center = target.Center;
            Projectile.velocity = (nextTarget.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
            Projectile.timeLeft = System.Math.Min(Projectile.timeLeft, 90);
            sizeMult = MathHelper.Max(sizeMult, 0.72f);
            colorValue += 18f;
            Projectile.netUpdate = true;
        }

        private void SpawnDragoonBoltHitFX(Vector2 pos, float fxScale)
        {
            for (int i = 0; i < (int)(7 * fxScale); i++)
            {
                Particle spark = new BoltParticle(
                    pos,
                    (new Vector2(4f, 4f) * fxScale).RotatedByRandom(100f) * Main.rand.NextFloat(0.3f, 1.9f),
                    true,
                    13,
                    Main.rand.NextFloat(0.08f, 0.12f) * fxScale,
                    Main.rand.NextBool(5) ? Color.Cyan : Color.Orchid,
                    new Vector2(1.8f, 0.8f),
                    true,
                    true,
                    false,
                    0.7f);
                GeneralParticleHandler.SpawnParticle(spark);

                Dust dust = Dust.NewDustPerfect(
                    pos,
                    ModContent.DustType<LightDust>(),
                    (new Vector2(5f, 5f) * fxScale).RotatedByRandom(100f) * Main.rand.NextFloat(0.5f, 1f),
                    0,
                    default,
                    Main.rand.NextFloat(0.32f, 0.44f) * fxScale);
                dust.noGravity = !Main.rand.NextBool(3);
                dust.color = Main.rand.NextBool(5) ? Color.Cyan : Color.Orchid;
            }

            //Particle pulse = new CustomPulse(pos, Vector2.Zero, Color.Cyan, "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, 0f, 0f, 0.05705f * fxScale, 10);
            //GeneralParticleHandler.SpawnParticle(pulse);

            for (int i = 0; i < 2; i++)
            {
                Particle orb = new CustomPulse(pos, Vector2.Zero, Color.Orchid, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.966f * fxScale, 0.35f * fxScale, 14, true, 0.5f);
                GeneralParticleHandler.SpawnParticle(orb);
                Particle orb2 = new CustomPulse(pos, Vector2.Zero, Color.White, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.6475f * fxScale, 0.14f * fxScale, 14, true, 0.5f);
                GeneralParticleHandler.SpawnParticle(orb2);
            }
        }

        private NPC FindBounceTarget(NPC previousTarget)
        {
            var candidates = new System.Collections.Generic.List<NPC>();

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.whoAmI == previousTarget.whoAmI || !npc.CanBeChasedBy(Projectile))
                    continue;
                if (Projectile.localNPCImmunity[npc.whoAmI] != 0)
                    continue;
                if (Projectile.Distance(npc.Center) >= 500f)
                    continue;
                candidates.Add(npc);
            }

            return candidates.Count > 0 ? Main.rand.Next(candidates) : null;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float size = 45f * sizeMult * (Projectile.numHits > 0 ? 1.35f : 1f);
            if (time <= 1)
            {
                float collisionPoint = float.NaN;
                return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - Projectile.velocity, Projectile.Center, size, ref collisionPoint);
            }

            return CalamityUtils.CircularHitboxCollision(Projectile.Center, size, targetHitbox);
        }

        public override bool? CanCutTiles() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2? previous = null;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 current = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                if (previous.HasValue)
                {
                    float fade = 1f - i / (float)Projectile.oldPos.Length;
                    DrawSegment(pixel, previous.Value, current, new Color(80, 220, 255) * fade, 3.2f * fade);
                    DrawSegment(pixel, previous.Value, current, Color.White * 0.55f * fade, 1.35f * fade);
                }
                previous = current;
            }

            return false;
        }

        private static void DrawSegment(Texture2D pixel, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 edge = end - start;
            if (edge.LengthSquared() <= 0.001f)
                return;

            Main.EntitySpriteDraw(
                pixel,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                edge.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(edge.Length(), width),
                SpriteEffects.None);
        }
    }
}
