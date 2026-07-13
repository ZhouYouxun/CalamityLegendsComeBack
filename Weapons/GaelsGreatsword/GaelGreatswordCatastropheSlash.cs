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
    internal sealed class GaelGreatswordCatastropheSlash : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Boss/SupremeCatastropheSlash";

        private static readonly Color BloodRed = new(190, 18, 38);
        private static readonly Color SoulPurple = new(95, 28, 150);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 7;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 116;
            Projectile.height = 70;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 4;
            Projectile.timeLeft = 42;
            Projectile.scale = 1.08f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.ai[1]++;
            Projectile.rotation = Projectile.ai[0] == 0f ? Projectile.velocity.ToRotation() : Projectile.ai[0];
            Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
            Projectile.velocity *= 0.985f;
            Lighting.AddLight(Projectile.Center, 0.36f, 0.02f, 0.18f);

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 4 == 0)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];

            if (Main.rand.NextBool(2))
            {
                Vector2 side = Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + side * Main.rand.NextFloat(-52f, 52f),
                    Main.rand.NextBool(3) ? DustID.Blood : DustID.Shadowflame,
                    side * Main.rand.NextFloat(-1.8f, 1.8f), 100,
                    Main.rand.NextBool() ? BloodRed : SoulPurple, Main.rand.NextFloat(1f, 1.45f));
                dust.noGravity = true;
            }

            // 灾厄原版 SupremeCatastropheSlash 的炬火拖尾：染色彩虹炬尘埃向后飘散，
            // 原版是深空蓝，这里换成至尊灾厄的血红。
            if (Main.rand.NextBool(2))
            {
                Dust torch = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(40f, 20f),
                    DustID.RainbowTorch, -Projectile.velocity * Main.rand.NextFloat(0.2f, 1.2f));
                torch.noGravity = true;
                torch.scale = Main.rand.NextFloat(0.5f, 0.7f);
                torch.color = BloodRed;
            }

            // 刃缘速度线：沿斩击前后两端拉出血色流线，读出刀刃的走向。
            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Vector2 forward = Projectile.rotation.ToRotationVector2();
                Vector2 edge = Projectile.Center + forward * (Main.rand.NextBool() ? 96f : -76f) * Projectile.scale;
                GeneralParticleHandler.SpawnParticle(new LineParticle(edge,
                    forward * Main.rand.NextFloat(2f, 5f) * (Main.rand.NextBool() ? 1f : -1f), false,
                    Main.rand.Next(9, 16), Main.rand.NextFloat(0.35f, 0.6f),
                    Main.rand.NextBool() ? BloodRed : SoulPurple));
            }
        }

        public override bool? CanDamage() => Projectile.ai[1] >= 3f && Projectile.ai[1] <= 34f ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = Projectile.rotation.ToRotationVector2();
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center - direction * 82f, Projectile.Center + direction * 104f,
                42f * Projectile.scale, ref collisionPoint) ? null : false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 0.5f, MathHelper.Clamp(Projectile.numHits / 4f, 0f, 1f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
            float opacity = Utils.GetLerpValue(42f, 30f, Projectile.timeLeft, true);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, drawPosition, frame, BloodRed with { A = 0 } * completion * 0.36f * opacity,
                    Projectile.rotation, origin, Projectile.scale * (1f + completion * 0.25f), effects);
            }
            Main.spriteBatch.ExitShaderRegion();

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.Lerp(lightColor, Color.White, 0.35f) * opacity,
                Projectile.rotation, origin, Projectile.scale, effects);
            return false;
        }
    }
}
