using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ashes
{
    // A locked enemy receives a short, readable brimstone brand while the relay opens with
    // homing embers. Fists and blades are intentionally reserved for ember impact callbacks.
    internal sealed class AshesofAnn_Located : ModProjectile, ILocalizedModType
    {
        private const int BrandFrames = 12;

        private static readonly Color FireOuter = new(132, 12, 24);
        private static readonly Color FireCore = new(255, 104, 42);
        private static readonly Color FireHot = new(255, 218, 132);

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private int TargetIndex => (int)Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.timeLeft = BrandFrames + 20;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(IEntitySource source)
        {
            if (!Main.dedServ)
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.26f, Pitch = -0.42f, PitchVariance = 0.08f, MaxInstances = 4 }, Projectile.Center);
        }

        public override void AI()
        {
            NPC target = GetTarget();
            if (target is null)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = target.Center;
            Timer++;
            if (!Main.dedServ)
                SpawnBrandEffects(target);

        }

        private NPC GetTarget()
        {
            return TargetIndex >= 0 && TargetIndex < Main.maxNPCs && Main.npc[TargetIndex].CanBeChasedBy(Projectile, false)
                ? Main.npc[TargetIndex]
                : null;
        }

        private void SpawnBrandEffects(NPC target)
        {
            float charge = Utils.GetLerpValue(0f, BrandFrames, Timer, true);
            float pulse = 0.85f + MathF.Sin(Timer * 0.66f) * 0.15f;
            Lighting.AddLight(target.Center, FireCore.ToVector3() * (0.35f + charge * 0.45f));

            if ((int)Timer % 2 == 0)
            {
                Vector2 offset = Main.rand.NextVector2Circular(target.width * 0.34f, target.height * 0.42f);
                Vector2 velocity = -Vector2.UnitY.RotatedByRandom(0.42f) * Main.rand.NextFloat(1.2f, 3.8f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    target.Center + offset,
                    velocity,
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.20f, 0.34f) * pulse,
                    Color.Lerp(FireOuter, FireHot, Main.rand.NextFloat(0.25f, 0.82f)),
                    true,
                    false));
            }

            if ((int)Timer % 4 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    target.Center + Main.rand.NextVector2Circular(target.width * 0.24f, target.height * 0.36f),
                    -Vector2.UnitY.RotatedByRandom(0.28f) * Main.rand.NextFloat(3f, 6.5f),
                    "CalamityMod/Particles/VerticalSmear",
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.55f, 0.95f),
                    Color.Lerp(FireCore, FireHot, Main.rand.NextFloat()),
                    new Vector2(0.16f, 0.92f),
                    true,
                    true,
                    shrinkSpeed: 0.84f,
                    glowOpacity: 0.54f));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            NPC target = GetTarget();
            if (target is null)
                return false;

            float charge = Utils.GetLerpValue(0f, BrandFrames, Timer, true);
            float pulse = 0.88f + MathF.Sin(Timer * 0.52f) * 0.12f;
            Vector2 drawPosition = target.Center - Main.screenPosition;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmear").Value;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null, FireOuter * (0.40f * charge), 0f, bloom.Size() * 0.5f, 0.55f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPosition, null, FireCore * (0.35f * charge), 0f, bloom.Size() * 0.5f, 0.33f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(ring, drawPosition, null, FireHot * (0.40f * charge), Timer * 0.11f, ring.Size() * 0.5f, 0.38f + charge * 0.24f, SpriteEffects.None);

            for (int i = 0; i < 3; i++)
            {
                float rotation = -MathHelper.PiOver2 + (i - 1) * 0.32f + MathF.Sin(Timer * 0.14f + i) * 0.08f;
                Main.EntitySpriteDraw(smear, drawPosition + new Vector2((i - 1) * 7f, -target.height * 0.25f), null,
                    Color.Lerp(FireCore, FireHot, i * 0.35f) * (0.48f * charge), rotation, smear.Size() * 0.5f,
                    new Vector2(0.16f, 0.58f + charge * 0.26f), SpriteEffects.None);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
