using CalamityMod;
using CalamityMod.Particles;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.D_Endgame
{
    /// <summary>
    /// 雷空猎影：超高速电蓝闪电弹，每 4 发中的第 4 发为强化弹（+40% 伤害 + 更强特效），命中施加 Electrified。
    /// </summary>
    public class DERule_PridefulHuntersPlanarRipper : DEBulletRule
    {
        private static readonly Color LightningBlue = new(80, 180, 255);
        private static readonly Color LightningWhite = new(200, 230, 255);
        private static readonly Color LightningCyan = new(30, 220, 255);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.PridefulHuntersPlanarRipper>();

        public override int ExtraUpdates => 10;    // 极速
        public override float SpeedMultiplier => 1.5f;

        public override float GetShotExtra(DesertEagleSlotPlayer slotPlayer)
        {
            slotPlayer.BurstCounter = (slotPlayer.BurstCounter + 1) % 4;
            return slotPlayer.BurstCounter == 0 ? 1f : 0f;   // 1 = 强化弹
        }

        private bool IsEmpowered(Projectile projectile) => projectile.ai[1] > 0.5f;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.width = 6;
            projectile.height = 6;
            projectile.light = IsEmpowered(projectile) ? 0.9f : 0.55f;
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (IsEmpowered(projectile))
                modifiers.FinalDamage += 0.4f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            bool empowered = IsEmpowered(projectile);
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            // 电弧 dust
            Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.TintableDustLighted,
                -forward * 0.4f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                110, empowered ? LightningWhite : LightningBlue, empowered ? 1.3f : 0.9f);
            dust.noGravity = true;

            if (!Main.dedServ && Main.rand.NextBool(empowered ? 2 : 4))
            {
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    projectile.Center, -forward * 2f + Main.rand.NextVector2Circular(1.5f, 1.5f),
                    false, 6, empowered ? 0.028f : 0.018f, empowered ? LightningWhite : LightningCyan));
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    projectile.Center, -forward * 0.9f, false, 4,
                    empowered ? 0.02f : 0.013f,
                    LightningBlue, new Vector2(0.5f, 2.8f)));
            }

            Lighting.AddLight(projectile.Center,
                LightningBlue.R / 255f * (empowered ? 0.7f : 0.4f),
                LightningBlue.G / 255f * (empowered ? 0.7f : 0.4f),
                LightningBlue.B / 255f * (empowered ? 0.9f : 0.6f));
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 180);
            bool empowered = IsEmpowered(projectile);
            SpawnLightningImpact(projectile.Center, empowered);
        }

        private static void SpawnLightningImpact(Vector2 pos, bool empowered)
        {
            float scale = empowered ? 1.4f : 1.0f;

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, LightningWhite, 0.9f * scale, (int)(20 * scale)));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, LightningBlue * 0.75f,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, 0f, 0f, 0.07f * scale, (int)(18 * scale), true, 0.85f));

                int sparkCount = empowered ? 16 : 10;
                for (int i = 0; i < sparkCount; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(
                        pos, Main.rand.NextVector2Circular(8f * scale, 8f * scale),
                        false, (int)(10 * scale), 0.025f * scale, i % 2 == 0 ? LightningBlue : LightningWhite));
                }

                if (empowered)
                {
                    GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, LightningCyan, 1.6f, 28));
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, LightningCyan * 0.55f,
                        "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, 0f, 0f, 0.12f, 24, true, 0.72f));
                }
            }

            for (int i = 0; i < (empowered ? 20 : 12); i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(9f * scale, 9f * scale), 110, LightningBlue, 1.1f * scale);
                dust.noGravity = true;
            }
        }

        public override string TooltipEffectEN => "Ultra-fast lightning bolt; every 4th shot is an empowered bolt (+40% damage + enhanced effects) + Electrified";
        public override string TooltipEffectZH => "极高速闪电弹；每4发有一枚强化弹（+40%伤害+增强特效），命中施加电击";
    }
}
