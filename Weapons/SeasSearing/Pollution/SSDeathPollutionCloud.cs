using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // Spawned on death of a polluted NPC; floats in place and inflicts pollution on contact.
    internal sealed class SSDeathPollutionCloud : ModProjectile, ILocalizedModType
    {
        private const int FrameCount = 10;

        private int Stacks   => (int)Projectile.ai[0];
        private int Duration => (int)Projectile.ai[1];

        private int CloudGrade
        {
            get
            {
                int s = Stacks;
                if (s >= 150) return 5;
                if (s >= 95)  return 4;
                if (s >= 55)  return 3;
                if (s >= 28)  return 2;
                if (s >= 10)  return 1;
                return 0;
            }
        }

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/Boss/SandPoisonCloudOldDuke";

        public override void SetStaticDefaults() => Main.projFrames[Type] = FrameCount;

        public override void SetDefaults()
        {
            Projectile.width          = 70;
            Projectile.height         = 70;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.penetrate      = -1;
            Projectile.timeLeft       = 360;
            Projectile.tileCollide    = false;
            Projectile.ignoreWater    = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 30;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                int   grade  = CloudGrade;
                float radius = MathHelper.Clamp(50f + grade * 22f + Stacks * 0.5f, 65f, 180f);
                Vector2 center = Projectile.Center;
                Projectile.width  = Projectile.height = (int)(radius * 2f);
                Projectile.Center = center;
                Projectile.timeLeft = Math.Max(Duration, 60);
                Projectile.netUpdate = true;
            }

            Projectile.velocity.Y  = MathHelper.Lerp(Projectile.velocity.Y, -0.38f, 0.04f);
            Projectile.velocity.X *= 0.98f;

            if (++Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= FrameCount)
                    Projectile.frame = 0;
            }

            float age         = Projectile.timeLeft / (float)Math.Max(1, Duration);
            int   grade2      = CloudGrade;
            Color gradeColor  = grade2 >= 1 ? SeasSearingPalette.GradeColor(grade2) : SeasSearingPalette.ToxicGreen;
            Lighting.AddLight(Projectile.Center, gradeColor.ToVector3() * (0.15f + (1f - age) * 0.1f));

            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                float radius = Projectile.width * 0.4f;
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(radius, radius),
                    grade2 >= 3 ? 89 : DustID.GemEmerald,
                    (-Vector2.UnitY + Main.rand.NextVector2Circular(0.5f, 0.5f)) * Main.rand.NextFloat(0.4f, 1.2f),
                    140,
                    Color.Lerp(gradeColor, SeasSearingPalette.DeepBlue, Main.rand.NextFloat(0.2f, 0.6f)),
                    Main.rand.NextFloat(0.55f, 0.95f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int grade  = CloudGrade;
            int amount = Math.Max(1, grade * 2 + Stacks / 20);
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, amount, 12 * 60, fromSpread: true);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 240);
            if (grade >= 3)
                target.AddBuff(Terraria.ID.BuffID.Venom, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int       frameH  = texture.Height / FrameCount;
            Rectangle frame   = new(0, Projectile.frame * frameH, texture.Width, frameH);
            Vector2   origin  = frame.Size() * 0.5f;

            int   grade      = CloudGrade;
            Color gradeColor = grade >= 1 ? SeasSearingPalette.GradeColor(grade) : SeasSearingPalette.ToxicGreen;
            float opacity    = MathHelper.Clamp(Projectile.timeLeft / 60f, 0f, 1f)
                             * MathHelper.Clamp(1f - (Projectile.timeLeft - 30f) / 30f, 0f, 1f);
            opacity = MathHelper.Clamp(opacity, 0f, 0.85f);

            float scale      = Projectile.width / (float)(frameH * 2);
            Color drawColor  = Color.Lerp(Color.White, gradeColor, 0.55f) * opacity;

            for (int i = 0; i < 3; i++)
            {
                float layerScale = scale * (0.85f + i * 0.12f);
                float layerRot   = Main.GlobalTimeWrappedHourly * (0.2f + i * 0.15f) * (i % 2 == 0 ? 1f : -1f);
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame,
                    drawColor * (1f - i * 0.22f), layerRot, origin, layerScale, SpriteEffects.None, 0);
            }

            return false;
        }
    }
}
