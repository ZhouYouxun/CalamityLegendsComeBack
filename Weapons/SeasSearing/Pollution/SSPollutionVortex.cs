using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // Spawned on grade-4+ detonation; pulls enemies, inflicts heavy pollution.
    internal sealed class SSPollutionVortex : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 300;
        private bool initialized;

        private int VortexGrade => (int)Projectile.ai[1];

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/Boss/OldDukeVortex";

        public override void SetDefaults()
        {
            Projectile.width          = 120;
            Projectile.height         = 120;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.penetrate      = -1;
            Projectile.timeLeft       = Lifetime;
            Projectile.tileCollide    = false;
            Projectile.ignoreWater    = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 18;
        }

        public override void AI()
        {
            if (!initialized)
            {
                initialized = true;
                int     grade  = VortexGrade;
                float   radius = MathHelper.Clamp(70f + grade * 20f, 90f, 160f);
                Vector2 center = Projectile.Center;
                Projectile.width = Projectile.height = (int)(radius * 2f);
                Projectile.Center = center;
                Projectile.netUpdate = true;
            }

            Projectile.velocity  = Vector2.Zero;
            Projectile.rotation += 0.028f;

            float age         = 1f - Projectile.timeLeft / (float)Lifetime;
            int   grade2      = VortexGrade;
            Color vortexColor = SeasSearingPalette.GradeColor(grade2);
            Lighting.AddLight(Projectile.Center, vortexColor.ToVector3() * (0.25f + age * 0.15f));

            float pullRadius   = Projectile.width * 1.4f;
            float pullRadiusSq = pullRadius * pullRadius;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || !npc.CanBeChasedBy()) continue;
                if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > pullRadiusSq) continue;

                Vector2 toCenter = (Projectile.Center - npc.Center).SafeNormalize(Vector2.Zero);
                float strength   = 1f - Vector2.Distance(npc.Center, Projectile.Center) / pullRadius;
                float pull       = MathHelper.Lerp(0.12f, 0.32f, strength);
                if (npc.boss || npc.knockBackResist <= 0f) pull *= 0.2f;
                npc.velocity += toCenter * pull;
            }

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                float   r     = Projectile.width * 0.45f;
                float   angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos   = Projectile.Center + angle.ToRotationVector2() * r;
                Vector2 vel   = (Projectile.Center - pos).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.5f, 2.2f);
                vel = vel.RotatedBy(MathHelper.PiOver2 * Main.rand.NextFloat(-0.3f, 0.3f));
                Dust d = Dust.NewDustPerfect(pos, DustID.Vortex, vel, 120,
                    Color.Lerp(vortexColor, SeasSearingPalette.BiohazardLime, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.55f, 0.9f));
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int grade  = VortexGrade;
            int amount = Math.Clamp(grade * 4 + (int)Projectile.ai[0] / 12, 8, 22);
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, amount, 14 * 60, fromSpread: true);
            target.AddBuff(BuffID.Venom, 360);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 420);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture   = TextureAssets.Projectile[Type].Value;
            Vector2   origin    = texture.Size() * 0.5f;
            float     age       = 1f - Projectile.timeLeft / (float)Lifetime;
            float     fadeIn    = MathHelper.Clamp(Projectile.timeLeft > Lifetime - 20 ? (Lifetime - Projectile.timeLeft) / 20f : 1f, 0f, 1f);
            float     fadeOut   = MathHelper.Clamp(Projectile.timeLeft / 40f, 0f, 1f);
            float     opacity   = fadeIn * fadeOut * 0.82f;

            int   grade      = VortexGrade;
            Color gradeColor = SeasSearingPalette.GradeColor(grade);
            float drawScale  = (Projectile.width / (float)texture.Width) * (0.85f + age * 0.15f);

            Color drawColor = Color.Lerp(gradeColor, SeasSearingPalette.BiohazardLime, age * 0.45f) * opacity;
            drawColor.A = (byte)(40 * opacity);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, drawColor,           Projectile.rotation,          origin, drawScale,          SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, drawColor * 0.55f, -Projectile.rotation * 0.7f, origin, drawScale * 0.82f, SpriteEffects.None, 0);
            return false;
        }
    }
}
