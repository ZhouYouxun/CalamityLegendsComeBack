using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.C_PostPlantera
{
    public class DERule_AstralBlaster : DEBulletRule
    {
        private static readonly Color StrandBlue = new(92, 158, 255);
        private static readonly Color StrandOrange = new(255, 128, 58);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.AstralBlaster>();

        public override int Penetrate => -1;
        public override int ExtraUpdates => 2;
        public override float DamageMultiplier => 0.84f;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.width = 18;
            projectile.height = 18;
            projectile.tileCollide = false;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 12;
            projectile.timeLeft = 150;
            projectile.light = 0.72f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.localAI[0]++;
            DEBulletUtils.OrientToVelocity(projectile);

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 2; i++)
            {
                float phase = projectile.localAI[0] * 0.38f + i * MathHelper.Pi;
                Vector2 strandPos = projectile.Center + side * (float)System.Math.Sin(phase) * 18f;
                Color color = i == 0 ? StrandBlue : StrandOrange;
                Dust dust = Dust.NewDustPerfect(strandPos, i == 0 ? DustID.Firework_Blue : DustID.OrangeTorch, -forward * 0.6f, 90, color, 1.05f);
                dust.noGravity = true;
            }

            if (!Main.rand.NextBool(3))
                DEBulletUtils.GlowTrail(projectile, Color.Lerp(StrandBlue, StrandOrange, 0.45f), 1.12f);

            Lighting.AddLight(projectile.Center, Color.Lerp(StrandBlue, StrandOrange, 0.5f).ToVector3() * 0.55f);
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= (float)System.Math.Pow(0.55f, projectile.localAI[1]);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 240);
            projectile.localAI[1]++;

            if (Main.myPlayer == projectile.owner)
                DEBulletUtils.SpawnAreaBurst(projectile.GetSource_FromAI(), target.Center, Math.Max(1, (int)(hit.Damage * 0.18f)), projectile.knockBack, projectile.owner, DEBurstStyle.Astral, 54f);
        }

        public override string TooltipEffectEN => "Fires an infinite-pierce astral DNA double helix; damage decays heavily after each hit";
        public override string TooltipEffectZH => "发射星神游龙式DNA双链，无限贯穿但每次命中后伤害严重衰减";
    }
}
