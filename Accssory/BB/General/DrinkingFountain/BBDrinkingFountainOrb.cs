using CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB
{
    internal sealed class BBDrinkingFountainOrb : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private float BloomPower => Utils.Remap(Timer, 0f, 90f, 0.82f, 1.22f, true) * Utils.GetLerpValue(0f, 35f, Projectile.timeLeft, true);
        private static readonly Color CoreBlue = new(95, 225, 255);
        private static readonly Color DeepBlue = new(32, 104, 255);

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
            Timer++;

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
            Lighting.AddLight(Projectile.Center, new Vector3(0.06f, 0.32f, 0.52f));
            EmitBlueFlameTrail();

            if (owner.statLife < owner.statLifeMax2 && Projectile.Hitbox.Intersects(owner.Hitbox))
            {
                int heal = 8;
                owner.statLife = Utils.Clamp(owner.statLife + heal, 0, owner.statLifeMax2);
                owner.HealEffect(heal);
                Projectile.Kill();
            }

        }

        private void EmitBlueFlameTrail()
        {
            if (Main.dedServ)
                return;

            Color theme = Color.Lerp(CoreBlue, Color.White, 0.08f);
            if ((int)Timer % 7 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    -Projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.25f, 0.9f),
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.16f, 0.28f) * (0.9f + BloomPower * 0.25f),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.12f, 0.4f)),
                    true,
                    false,
                    true));
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextBool(3) ? DustID.Frost : DustID.BlueTorch,
                    -Projectile.velocity * 0.55f,
                    80,
                    Color.Lerp(DeepBlue, Color.White, Main.rand.NextFloat(0.08f, 0.28f)),
                    Main.rand.NextFloat(0.45f, 0.9f));
                dust.noGravity = true;
            }
        }

        private void ReleaseBlueRing(int count, float speed)
        {
            if (Main.dedServ)
                return;

            Color theme = Color.Lerp(CoreBlue, Color.White, 0.22f);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, theme, Vector2.One, Projectile.rotation, 0.02f, 0.3f, 18));

            for (int i = 0; i < count; i++)
            {
                float rot = MathHelper.TwoPi * i / count;
                Vector2 velocity = rot.ToRotationVector2() * speed;
                Dust dust = Dust.NewDustPerfect(Projectile.Center + velocity, Main.rand.NextBool(3) ? DustID.Frost : DustID.GemSapphire, velocity, 0, theme, Main.rand.NextFloat(0.85f, 1.35f));
                dust.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            Color theme = Color.Lerp(CoreBlue, Color.White, 0.12f);
            ReleaseBlueRing(10, 2.2f);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                theme * 0.52f,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Projectile.rotation,
                0.04f,
                0.16f,
                14,
                true));
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
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/TinyGreyscaleCircle").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = bloom.Size() * 0.5f;

            PFLeftEffectRules.BeginAdditive();

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float completion = i / (float)Projectile.oldPos.Length;
                float fade = 1f - completion;
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color trailColor = Color.Lerp(CoreBlue, Color.Transparent, completion) * fade * Projectile.Opacity;

                Main.EntitySpriteDraw(
                    texture,
                    trailPos,
                    null,
                    trailColor,
                    Projectile.rotation,
                    texture.Size() * 0.5f,
                    Projectile.scale * MathHelper.Lerp(0.28f, 0.75f, fade),
                    SpriteEffects.None);

                Main.EntitySpriteDraw(
                    bloom,
                    trailPos,
                    null,
                    Color.Lerp(DeepBlue, CoreBlue, fade) * fade * 0.22f,
                    Projectile.rotation,
                    origin,
                    0.19f * fade,
                    SpriteEffects.None);
            }

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                Color.Lerp(CoreBlue, Color.White, 0.16f) * Projectile.Opacity,
                Projectile.rotation,
                texture.Size() * 0.5f,
                Projectile.scale * 0.78f,
                SpriteEffects.None);
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                Color.Lerp(CoreBlue, Color.White, 0.18f) * 0.48f,
                Projectile.rotation,
                origin,
                BloomPower * Projectile.scale * 0.42f,
                SpriteEffects.None);

            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
