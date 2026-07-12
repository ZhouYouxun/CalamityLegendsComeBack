using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
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
    internal sealed class GaelGreatswordDarkSoul : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Melee/GaelSkull";

        private static readonly Color SoulPurple = new(85, 30, 135);
        private static readonly Color BloodRed = new(150, 8, 32);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            Main.projFrames[Type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Projectile.ai[1]++;
            Projectile.rotation += (MathF.Abs(Projectile.velocity.X) + MathF.Abs(Projectile.velocity.Y)) * 0.018f * Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);
            Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
            Lighting.AddLight(Projectile.Center, 0.22f, 0.04f, 0.32f);

            if (Projectile.ai[1] > 12f)
            {
                NPC target = GetStoredTarget();
                if (target != null)
                    HomeToward(target);
                else
                    CalamityUtils.HomeInOnNPC(Projectile, true, 900f, 15f, 26f);
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5 == 0)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];

            if (Projectile.scale > 1.2f)
            {
                Projectile.velocity *= 1.012f;
                Projectile.alpha += 2;
                if (Projectile.alpha >= 245)
                    Projectile.Kill();
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextBool(4) ? DustID.Blood : DustID.Shadowflame,
                    -Projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.3f, 1.8f), 120,
                    Main.rand.NextBool(4) ? BloodRed : SoulPurple, Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 0.55f, MathHelper.Clamp(Projectile.numHits / 3f, 0f, 1f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Main.player[Projectile.owner].GetModPlayer<GaelGreatswordPlayer>().AddDarkEmbers(3 + GaelGreatswordProgression.GetStage() / 2);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool() ? DustID.Blood : DustID.Shadowflame,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.4f, 4.2f), 100,
                    Main.rand.NextBool() ? BloodRed : SoulPurple, Main.rand.NextFloat(0.9f, 1.35f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Color drawColor = Color.Lerp(SoulPurple, BloodRed, 0.35f + MathF.Sin(Projectile.ai[1] * 0.08f) * 0.2f);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, drawPosition, frame, drawColor with { A = 0 } * completion * 0.35f,
                    Projectile.rotation, origin, Projectile.scale * (0.75f + completion * 0.35f), effects);
            }

            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, drawColor with { A = 0 } * 0.42f,
                0f, bloom.Size() * 0.5f, Projectile.scale * 0.72f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White,
                Projectile.rotation, origin, Projectile.scale, effects);
            return false;
        }

        private NPC GetStoredTarget()
        {
            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
                return null;

            NPC target = Main.npc[targetIndex];
            return target.CanBeChasedBy(Projectile) ? target : null;
        }

        private void HomeToward(NPC target)
        {
            Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center) * 16f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.075f);
        }
    }
}
