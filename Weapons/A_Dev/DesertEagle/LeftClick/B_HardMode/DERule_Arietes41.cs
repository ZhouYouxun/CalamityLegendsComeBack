using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.B_HardMode
{
    /// <summary>
    /// 白羊座41：电击/生命交替发射。
    /// 电击弹 → 青色光弧，命中施加 Electrified；
    /// 生命弹 → 白金光晕，命中吸血 2%。
    /// </summary>
    public class DERule_Arietes41 : DEBulletRule
    {
        // Shock
        private static readonly Color ShockCyan = new(50, 220, 255);
        private static readonly Color ShockLight = new(180, 240, 255);
        // Life
        private static readonly Color LifeWhite = new(255, 255, 255);
        private static readonly Color LifeGold = new(255, 230, 120);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.Arietes41>();

        public override float GetShotExtra(DesertEagleSlotPlayer slotPlayer)
        {
            bool current = slotPlayer.ToggleState;
            slotPlayer.ToggleState = !current;
            return current ? 1f : 0f;   // 0 = 电击, 1 = 生命
        }

        private bool IsShock(Projectile projectile) => projectile.ai[1] < 0.5f;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.light = 0.55f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            if (IsShock(projectile))
            {
                // 青色电弧尾迹
                Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.TintableDustLighted,
                    -forward * 0.5f + Main.rand.NextVector2Circular(0.5f, 0.5f), 120, ShockCyan, 0.9f);
                dust.noGravity = true;

                if (!Main.dedServ && Main.rand.NextBool(3))
                {
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(
                        projectile.Center, -forward * 1.5f + Main.rand.NextVector2Circular(1f, 1f),
                        false, 8, 0.02f, ShockCyan));
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        projectile.Center, -forward * 0.8f, false, 5, 0.012f, ShockLight, new Vector2(0.5f, 2.2f)));
                }
                Lighting.AddLight(projectile.Center, ShockCyan.R / 255f * 0.5f, ShockCyan.G / 255f * 0.5f, ShockCyan.B / 255f * 0.5f);
            }
            else
            {
                // 白金光晕尾迹
                Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.SilverCoin,
                    -forward * 0.5f + Main.rand.NextVector2Circular(0.4f, 0.4f), 120, LifeGold, 0.95f);
                dust.noGravity = true;

                if (!Main.dedServ && Main.rand.NextBool(3))
                {
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        projectile.Center, -forward * 0.8f, false, 6, 0.015f, LifeWhite, new Vector2(0.6f, 2.0f)));
                }
                Lighting.AddLight(projectile.Center, LifeGold.R / 255f * 0.5f, LifeGold.G / 255f * 0.45f, LifeGold.B / 255f * 0.25f);
            }
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsShock(projectile))
            {
                target.AddBuff(BuffID.Electrified, 180);
                SpawnShockImpact(projectile.Center);
            }
            else
            {
                // 2% 吸血
                int heal = System.Math.Max(1, (int)System.Math.Round(hit.Damage * 0.02));
                owner.statLife = System.Math.Min(owner.statLifeMax2, owner.statLife + heal);
                owner.HealEffect(heal);
                SpawnLifeImpact(projectile.Center);
            }
        }

        private static void SpawnShockImpact(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, ShockCyan, 0.8f, 16));
                for (int i = 0; i < 10; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(
                        pos, Main.rand.NextVector2Circular(7f, 7f), false, 10, 0.022f, ShockCyan));
                }
            }
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(7f, 7f), 100, ShockCyan, 1.0f);
                dust.noGravity = true;
            }
        }

        private static void SpawnLifeImpact(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, LifeWhite, 0.9f, 18));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, LifeGold * 0.6f,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, 0f, 0f, 0.06f, 16, true, 0.8f));
                for (int i = 0; i < 8; i++)
                {
                    float angle = MathHelper.TwoPi * i / 8f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        pos + angle.ToRotationVector2() * 7f, angle.ToRotationVector2() * 7f,
                        false, 10, 0.02f, i % 2 == 0 ? LifeWhite : LifeGold, new Vector2(0.8f, 0.45f)));
                }
            }
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.SilverCoin,
                    Main.rand.NextVector2Circular(6f, 6f), 100, LifeGold, 1.1f);
                dust.noGravity = true;
            }
        }

        public override string TooltipEffectEN => "Alternating shock/life shots; shock Electrifies, life heals 2% of damage dealt";
        public override string TooltipEffectZH => "电击/生命交替弹；电击弹施加电击，生命弹命中回血2%";
    }
}
