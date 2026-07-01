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
    /// 霜炎爆破枪：冰/火交替发射，各自有独特颜色、尾迹与爆炸特效。
    /// </summary>
    public class DERule_ThermoclineBlaster : DEBulletRule
    {
        // Ice
        private static readonly Color IceBlue = new(140, 220, 255);
        private static readonly Color IceWhite = new(220, 245, 255);
        // Fire
        private static readonly Color FireOrange = new(255, 130, 20);
        private static readonly Color FireRed = new(255, 60, 10);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.ThermoclineBlaster>();

        // ai[1] = 0 → 冰弹，1 → 火弹（每发切换）
        public override float GetShotExtra(DesertEagleSlotPlayer slotPlayer)
        {
            bool current = slotPlayer.ToggleState;
            slotPlayer.ToggleState = !current;
            return current ? 1f : 0f;
        }

        private bool IsIce(Projectile projectile) => projectile.ai[1] < 0.5f;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.light = 0.5f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            if (IsIce(projectile))
            {
                // 冰弹：蓝白尾迹
                Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.Ice,
                    -forward * 0.6f + Main.rand.NextVector2Circular(0.4f, 0.4f), 120, IceBlue, 0.95f);
                dust.noGravity = true;

                if (!Main.dedServ && Main.rand.NextBool(4))
                {
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        projectile.Center, -forward * 0.9f, false, 6, 0.015f, IceWhite, new Vector2(0.55f, 2.0f)));
                }
                Lighting.AddLight(projectile.Center, IceBlue.R / 255f * 0.5f, IceBlue.G / 255f * 0.5f, IceBlue.B / 255f * 0.5f);
            }
            else
            {
                // 火弹：橙红尾迹
                Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.Torch,
                    -forward * 0.6f + Main.rand.NextVector2Circular(0.4f, 0.4f), 120, FireOrange, 1.0f);
                dust.noGravity = true;

                if (!Main.dedServ && Main.rand.NextBool(5))
                {
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                        projectile.Center, -forward * 0.06f,
                        FireOrange * 0.4f, 12, 0.35f, 0.5f, Main.rand.NextFloat(-0.025f, 0.025f)));
                }
                Lighting.AddLight(projectile.Center, FireOrange.R / 255f * 0.6f, FireOrange.G / 255f * 0.3f, 0f);
            }
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsIce(projectile))
            {
                target.AddBuff(BuffID.Frostburn2, 180);
                SpawnIceImpact(projectile.Center);
            }
            else
            {
                target.AddBuff(BuffID.OnFire3, 180);
                SpawnFireImpact(projectile.Center);
            }
        }

        public override bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity)
        {
            if (IsIce(projectile)) SpawnIceImpact(projectile.Center);
            else SpawnFireImpact(projectile.Center);
            return true;
        }

        private static void SpawnIceImpact(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, IceWhite, 0.85f, 18));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, IceBlue * 0.8f,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, 0f, 0f, 0.06f, 16, true, 0.82f));
                for (int i = 0; i < 12; i++)
                {
                    float angle = MathHelper.TwoPi * i / 12f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        pos + angle.ToRotationVector2() * 6f, angle.ToRotationVector2() * 8f,
                        false, 10, 0.022f, i % 2 == 0 ? IceBlue : IceWhite, new Vector2(0.9f, 0.45f)));
                }
            }
            for (int i = 0; i < 14; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.Ice,
                    Main.rand.NextVector2Circular(7f, 7f), 100, IceBlue, 1.1f);
                dust.noGravity = true;
            }
        }

        private static void SpawnFireImpact(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, FireOrange, 0.9f, 20));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, FireRed * 0.7f,
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, 0f, 0f, 0.07f, 18, true, 0.75f));
                for (int i = 0; i < 3; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(pos,
                        Main.rand.NextVector2Circular(2f, 2f),
                        Color.Lerp(FireOrange, FireRed, Main.rand.NextFloat()) * 0.5f,
                        16, Main.rand.NextFloat(0.4f, 0.65f), 0.5f, Main.rand.NextFloat(-0.025f, 0.025f)));
                }
            }
            for (int i = 0; i < 16; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.Torch,
                    Main.rand.NextVector2Circular(9f, 9f), 110, FireOrange, 1.2f);
                dust.noGravity = true;
            }
        }

        public override string TooltipEffectEN => "Alternating ice/fire shots; ice Frostburns, fire applies Hellfire; each with matching elemental explosions";
        public override string TooltipEffectZH => "冰/火交替发射；冰弹施加霜燃，火弹施加地狱烈焰，各有元素爆炸特效";
    }
}
