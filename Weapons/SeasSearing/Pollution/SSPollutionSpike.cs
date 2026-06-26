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
    // Spawned on grade-3+ detonation in radial burst; flies outward and inflicts pollution.
    internal sealed class SSPollutionSpike : ModProjectile, ILocalizedModType
    {
        private int SpikeGrade => (int)Projectile.ai[1];

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/Boss/OldDukeToothBallSpike";

        public override void SetDefaults()
        {
            Projectile.width          = 10;
            Projectile.height         = 26;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.tileCollide    = true;
            Projectile.ignoreWater    = true;
            Projectile.penetrate      = 2;
            Projectile.timeLeft       = 220;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 14;
        }

        public override void AI()
        {
            Projectile.rotation     = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity.Y   = Math.Min(Projectile.velocity.Y + 0.16f, 14f);

            int   grade      = SpikeGrade;
            Color spikeColor = SeasSearingPalette.GradeColor(grade);
            Lighting.AddLight(Projectile.Center, spikeColor.ToVector3() * 0.22f);

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center, 89,
                    -Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    120, spikeColor, Main.rand.NextFloat(0.45f, 0.8f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int grade  = SpikeGrade;
            int amount = Math.Clamp(grade * 3, 4, 14);
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, amount, 10 * 60, fromSpread: true);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 240);
        }

        public override void OnKill(int timeLeft) =>
            SeasSearingVisualUtility.SpawnGradeBurst(Projectile.Center, SpikeGrade, 8);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture    = TextureAssets.Projectile[Type].Value;
            Texture2D bloom      = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2   origin     = texture.Size() * 0.5f;
            int       grade      = SpikeGrade;
            Color     gradeColor = SeasSearingPalette.GradeColor(grade);

            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null,
                (gradeColor with { A = 0 }) * 0.5f, Projectile.rotation, bloom.Size() * 0.5f, new Vector2(0.08f, 0.22f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
                Color.Lerp(Color.White, gradeColor, 0.42f), Projectile.rotation, origin,
                Projectile.scale * (0.9f + grade * 0.04f), SpriteEffects.None, 0);
            return false;
        }
    }
}
