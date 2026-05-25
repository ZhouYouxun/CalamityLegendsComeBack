using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
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
        public float sizeMult = 1f;

        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 100;
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
            Color usedColor = Color.Lerp(Color.Cyan, Color.Orchid, Utils.GetLerpValue(0f, 50f, colorValue));

            if (time == 0)
            {
                colorValue += 30f;
                sizeMult = BigLightning ? 1.35f : 1f;
                AzureThunderSounds.PlayLightningSpawn(Projectile.Center, BigLightning);
            }

            float targetDist = Vector2.Distance(owner.Center, Projectile.Center);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (targetDist < 1400f && Projectile.timeLeft > 5)
            {
                Vector2 pos = Projectile.Center;
                if (time % 2 == 0)
                {
                    if (time < 120)
                    {
                        Particle forwardSpark = new CustomSpark(pos, Projectile.velocity * 3.6f * sizeMult, "CalamityMod/Particles/GlowSpark", false, 11, 0.15f * sizeMult, usedColor, new Vector2(2f, 0.8f), true, true, shrinkSpeed: 1f);
                        GeneralParticleHandler.SpawnParticle(forwardSpark);
                        sizeMult *= 0.985f;
                    }

                    Particle bolt = new BoltParticle(pos, -Projectile.velocity * 0.05f, false, 30, 0.6f, usedColor, new Vector2(1.8f, 0.8f), true, true, false, 0.3f);
                    GeneralParticleHandler.SpawnParticle(bolt);
                }

                if (Main.rand.NextBool(18))
                {
                    Particle sideBolt = new BoltParticle(pos, Projectile.velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.3f, 1.9f), false, 23, Main.rand.NextFloat(0.2f, 0.25f), usedColor, new Vector2(1.8f, 0.8f), true, true, false, 0.3f);
                    GeneralParticleHandler.SpawnParticle(sideBolt);
                }

                if (Main.rand.NextBool(5))
                {
                    Particle drainLine = new CustomSpark(pos, Projectile.velocity * Main.rand.NextFloat(-0.4f, 0.4f), "CalamityMod/Particles/DrainLineBloom", false, 80, Main.rand.NextFloat(1.2f, 1.3f) * sizeMult, usedColor, new Vector2(1f, 4f), true, true);
                    GeneralParticleHandler.SpawnParticle(drainLine);
                }

                if (time % 3 == 0)
                {
                    Dust dust = Dust.NewDustPerfect(pos, DustID.FireworksRGB, new Vector2(5f, 5f).RotatedByRandom(100f) * Main.rand.NextFloat(0.5f, 1f), 0, default, Main.rand.NextFloat(0.45f, 0.6f));
                    dust.noGravity = true;
                    dust.color = usedColor;
                }
            }

            time++;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float damageMult = Utils.Remap(Projectile.numHits, 0f, 3f, 1f, 0.15f, true);
            modifiers.SourceDamage *= damageMult;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 300);
            AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);
            if (Projectile.numHits == 0)
            {
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 5);
                float fxScale = BigLightning ? 3.6f : 3f;
                Vector2 pos = target.Center;
                AzureThunderSounds.PlayLightningImpact(pos, BigLightning);
                for (int i = 0; i < (int)(7 * fxScale); i++)
                {
                    Particle bolt = new BoltParticle(pos, (new Vector2(4f, 4f) * fxScale).RotatedByRandom(100f) * Main.rand.NextFloat(0.3f, 1.9f), true, 13, Main.rand.NextFloat(0.1f, 0.15f) * fxScale, Main.rand.NextBool(5) ? Color.Cyan : Color.Orchid, new Vector2(1.8f, 0.8f), true, true, false, 0.7f);
                    GeneralParticleHandler.SpawnParticle(bolt);

                    Dust dust = Dust.NewDustPerfect(pos, ModContent.DustType<LightDust>(), (new Vector2(5f, 5f) * fxScale).RotatedByRandom(100f) * Main.rand.NextFloat(0.5f, 1f), 0, default, Main.rand.NextFloat(0.4f, 0.55f) * fxScale);
                    dust.noGravity = !Main.rand.NextBool(3);
                    dust.color = Main.rand.NextBool(5) ? Color.Cyan : Color.Orchid;
                }

                Particle pulse = new CustomPulse(pos, Vector2.Zero, Color.Cyan, "CalamityMod/Particles/HighResFoggyCircleHardEdge", new Vector2(1f, 1f), 0f, 0f, 0.0815f * fxScale, 10);
                GeneralParticleHandler.SpawnParticle(pulse);
                for (int i = 0; i < 2; i++)
                {
                    Particle orb = new CustomPulse(pos, Vector2.Zero, Color.Orchid, "CalamityMod/Particles/BloomCircle", new Vector2(1f, 1f), Main.rand.NextFloat(-10f, 10f), 1.38f * fxScale, 0.5f * fxScale, 14);
                    GeneralParticleHandler.SpawnParticle(orb);
                    Particle whiteOrb = new CustomPulse(pos, Vector2.Zero, Color.White, "CalamityMod/Particles/BloomCircle", new Vector2(1f, 1f), Main.rand.NextFloat(-10f, 10f), 0.925f * fxScale, 0.2f * fxScale, 14);
                    GeneralParticleHandler.SpawnParticle(whiteOrb);
                }
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
            float size = 45f * Math.Max(sizeMult, BigLightning ? 1.15f : 0.9f) * (Projectile.numHits > 0 ? 6f : 1f);
            if (time <= 1)
            {
                float collisionPoint = float.NaN;
                return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, owner.Center, size, ref collisionPoint);
            }

            return CalamityUtils.CircularHitboxCollision(Projectile.Center, size, targetHitbox);
        }

        public override bool? CanCutTiles() => false;
    }
}
