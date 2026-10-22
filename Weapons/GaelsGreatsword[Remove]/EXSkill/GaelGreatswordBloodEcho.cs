using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;


namespace CalamityLegendsComeBack.Weapons.GaelsGreatsword
{
    internal sealed class GaelGreatswordBloodEcho : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 30;

        private static readonly Color SoulPurple = GaelGreatswordVisuals.CrimsonViolet;
        private static readonly Color BloodRed = GaelGreatswordVisuals.BrimstoneRed;
        private static readonly Color PaleCore = GaelGreatswordVisuals.WhiteHot;

        private float Power => MathHelper.Clamp(Projectile.ai[0], 0f, 1f);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 260;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.noEnchantmentVisuals = true;
        }

        public override void AI()
        {
            Projectile.ai[1]++;
            Projectile.rotation += 0.08f + Power * 0.04f;
            float progress = Projectile.ai[1] / Lifetime;
            Lighting.AddLight(Projectile.Center, 0.28f + Power * 0.22f, 0.03f, 0.32f + Power * 0.22f);

            if (Projectile.ai[1] == 1f)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.38f, Pitch = -0.28f }, Projectile.Center);
                SpawnOpeningDust();
            }

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                float radius = GetRadius(progress);
                Vector2 position = Projectile.Center + Main.rand.NextVector2CircularEdge(radius, radius);
                Dust dust = Dust.NewDustPerfect(position, Main.rand.NextBool(3) ? DustID.Blood : (int)CalamityDusts.Brimstone,
                    position.DirectionFrom(Projectile.Center) * Main.rand.NextFloat(0.8f, 2.4f), 100,
                    Main.rand.NextBool() ? BloodRed : SoulPurple, Main.rand.NextFloat(0.9f, 1.3f));
                dust.noGravity = true;
            }
        }

        public override bool? CanDamage()
        {
            float progress = Projectile.ai[1] / Lifetime;
            return progress >= 0.12f && progress <= 0.55f ? null : false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = GetRadius(Projectile.ai[1] / Lifetime);
            Vector2 closest = targetHitbox.ClosestPointInRect(Projectile.Center);
            return closest.Distance(Projectile.Center) <= radius ? null : false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 0.45f, MathHelper.Clamp(Projectile.numHits / 5f, 0f, 1f));
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 4.2f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(70f, 70f),
                    Main.rand.NextBool() ? DustID.Blood : (int)CalamityDusts.Brimstone, velocity, 100,
                    Main.rand.NextBool() ? BloodRed : SoulPurple, Main.rand.NextFloat(0.8f, 1.2f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            float progress = MathHelper.Clamp(Projectile.ai[1] / Lifetime, 0f, 1f);
            float opacity = Utils.GetLerpValue(1f, 5f, Projectile.ai[1], true) * Utils.GetLerpValue(Lifetime, Lifetime - 10f, Projectile.ai[1], true);
            float radius = GetRadius(progress);
            Color echoColor = Color.Lerp(SoulPurple, BloodRed, 0.48f + Power * 0.25f);
            Vector2 center = Projectile.Center - Main.screenPosition;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/HollowCircleHardEdge").Value;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, center, null, echoColor with { A = 0 } * opacity * 0.38f,
                0f, bloom.Size() * 0.5f, radius / bloom.Width * (1.15f + Power * 0.24f), SpriteEffects.None);
            Main.EntitySpriteDraw(ring, center, null, PaleCore with { A = 0 } * opacity * 0.72f,
                Projectile.rotation, ring.Size() * 0.5f, radius / ring.Width * 2f, SpriteEffects.None);
            Main.EntitySpriteDraw(ring, center, null, BloodRed with { A = 0 } * opacity * 0.44f,
                -Projectile.rotation * 0.7f, ring.Size() * 0.5f, radius / ring.Width * 1.35f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        private float GetRadius(float progress)
        {
            float eased = CalamityUtils.EaseInOutExp(MathHelper.Clamp(progress, 0f, 1f), 3f, 2f);
            return MathHelper.Lerp(28f, 132f + Power * 46f, eased);
        }

        private void SpawnOpeningDust()
        {
            if (Main.dedServ)
                return;

            int count = 16 + (int)(Power * 10f);
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.6f, 5.4f + Power * 2f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? DustID.Blood : (int)CalamityDusts.Brimstone,
                    velocity, 95, Main.rand.NextBool() ? BloodRed : SoulPurple, Main.rand.NextFloat(1f, 1.55f));
                dust.noGravity = true;
            }
        }
    }
}
