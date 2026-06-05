using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.PeaShooter
{
    internal sealed class PeaShooterElectricCloud : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int StageIndex => (int)Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = BalancePeaShooter.ElectricCloudLifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = BalancePeaShooter.ElectricCloudHitCooldown;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.localAI[1] = Projectile.timeLeft;
                int radius = BalancePeaShooter.GetElectricCloudRadius(StageIndex);
                Vector2 center = Projectile.Center;
                Projectile.Resize(radius * 2, radius * 2);
                Projectile.Center = center;
            }

            float age = Projectile.localAI[1] - Projectile.timeLeft;
            Projectile.Opacity = MathHelper.Clamp(age / 12f, 0f, 1f) * MathHelper.Clamp(Projectile.timeLeft / 22f, 0f, 1f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.34f, 0.42f) * Projectile.Opacity);

            if (Main.rand.NextBool(2))
                SpawnMistDust();
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = (Projectile.Center.X < target.Center.X).ToDirectionInt();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, BalancePeaShooter.GetDebuffDuration(StageIndex));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.Opacity <= 0.02f)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = bloom.Size() * 0.5f;
            Color outer = (new Color(72, 208, 255) with { A = 0 }) * (0.13f * Projectile.Opacity);
            Color inner = (Color.White with { A = 0 }) * (0.08f * Projectile.Opacity);
            float baseScale = Projectile.width / (float)bloom.Width;
            float time = Main.GlobalTimeWrappedHourly * 1.8f + Projectile.identity * 0.13f;

            for (int i = 0; i < 5; i++)
            {
                float angle = MathHelper.TwoPi * i / 5f + time;
                Vector2 offset = angle.ToRotationVector2() * Projectile.width * 0.08f * (0.7f + 0.3f * (float)Math.Sin(time + i));
                float scale = baseScale * (0.28f + i * 0.028f);
                Main.EntitySpriteDraw(bloom, Projectile.Center + offset - Main.screenPosition, null, outer, angle, origin, scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, inner, -time, origin, baseScale * 0.22f, SpriteEffects.None, 0);
            return false;
        }

        private void SpawnMistDust()
        {
            Vector2 offset = Main.rand.NextVector2Circular(Projectile.width * 0.42f, Projectile.height * 0.42f);
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + offset,
                Main.rand.NextBool(3) ? DustID.GemDiamond : DustID.Electric,
                Main.rand.NextVector2Circular(0.35f, 0.35f),
                110,
                Main.rand.NextBool(3) ? Color.White : new Color(92, 226, 255),
                Main.rand.NextFloat(0.45f, 0.82f));
            dust.noGravity = true;
            dust.noLight = true;
        }
    }
}
