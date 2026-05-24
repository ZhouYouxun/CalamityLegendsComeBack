using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    internal sealed class AzureThunderFlatLightning : ModProjectile, ILocalizedModType
    {
        public const int GainChargeFlag = 1;
        public const int StaticDischargeFlag = 2;
        public const int BigLightningFlag = 4;
        public const int CrumblingFlag = 8;

        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Flags => (int)Projectile.ai[0];
        private bool GainCharge => (Flags & GainChargeFlag) != 0;
        private bool ApplyStaticDischarge => (Flags & StaticDischargeFlag) != 0;
        private bool BigLightning => (Flags & BigLightningFlag) != 0;
        private bool ApplyCrumbling => (Flags & CrumblingFlag) != 0;
        public int time;
        public float colorValue;

        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 200;
            Projectile.extraUpdates = 10;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            colorValue = MathHelper.Lerp(colorValue, 50f, 0.025f);
            Color usedColor = Color.Lerp(Color.Cyan, Color.Orchid, Utils.GetLerpValue(0f, 50f, colorValue)) * 0.7f;

            Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[1] * 0.022f);

            float targetDist = Vector2.Distance(owner.Center, Projectile.Center);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (targetDist < 1400f)
            {
                Vector2 pos = Projectile.Center;
                if (Projectile.timeLeft % 3 == 0)
                {
                    Particle spark = new BoltParticle(pos, -Projectile.velocity * 0.05f, false, 15, 0.4f, usedColor, new Vector2(1.8f, 0.8f), true, true, false, 0.5f);
                    GeneralParticleHandler.SpawnParticle(spark);
                }

                if (Main.rand.NextBool(35))
                {
                    Particle spark = new BoltParticle(pos, Projectile.velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.3f, 1.9f), false, 23, Main.rand.NextFloat(0.2f, 0.25f), usedColor, new Vector2(1.8f, 0.8f), true, true, false, 0.3f);
                    GeneralParticleHandler.SpawnParticle(spark);
                }

                if (time % 5 == 0)
                {
                    Dust dust = Dust.NewDustPerfect(pos, DustID.FireworksRGB, new Vector2(2f, 2f).RotatedByRandom(100f) * Main.rand.NextFloat(0.5f, 1f), 0, default, Main.rand.NextFloat(0.45f, 0.6f));
                    dust.noGravity = true;
                    dust.color = usedColor;
                }
            }

            time++;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float damageMult = Utils.Remap(Projectile.numHits, 0f, 2f, 1f, 0.5f, true);
            modifiers.SourceDamage *= damageMult;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 300);
            AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);
            for (int i = 0; i < 4; i++)
            {
                Particle spark = new BoltParticle(Projectile.Center, Projectile.velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.3f, 1.9f), false, 13, Main.rand.NextFloat(0.3f, 0.35f), Main.rand.NextBool() ? Color.Orchid : Color.Cyan, new Vector2(1.8f, 0.8f), true, true, false, 0.9f);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            Player owner = Main.player[Projectile.owner];
            if (ApplyStaticDischarge)
            {
                if (owner.GetModPlayer<AzureThunderPlayer>().HarmonyActive)
                    AzureThunderPlayer.ApplyUltimateDot(target, 180);
                else
                    target.AddBuff(ModContent.BuffType<StaticDischarge>(), 180);
            }

            if (ApplyCrumbling)
                target.AddBuff(ModContent.BuffType<CalamityMod.Buffs.StatDebuffs.Crumbling>(), 180);

            if (Projectile.ai[2] > 0f)
                owner.GetModPlayer<AzureThunderPlayer>().AddUltimateEnergy((int)Projectile.ai[2]);

            if (GainCharge)
                owner.GetModPlayer<AzureThunderPlayer>().TryGainThunderChargeFromTarget(target);

            colorValue += 18f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Player owner = Main.player[Projectile.owner];
            if (time <= 1)
            {
                float collisionPoint = float.NaN;
                return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, owner.Center, 25f, ref collisionPoint);
            }

            return CalamityUtils.CircularHitboxCollision(Projectile.Center, 25f, targetHitbox);
        }

        public override bool? CanCutTiles() => false;
    }
}
