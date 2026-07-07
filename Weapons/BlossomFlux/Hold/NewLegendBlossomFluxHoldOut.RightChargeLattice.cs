using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using Microsoft.Xna.Framework;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    // 右键蓄力专属视觉：每16/20 tick 各生成一个短命的“斜圆环”粒子（具体实现见 BFRightChargeHaloParticle.cs），
    // 靠持续不断地新生成来维持观感，只在 rightChargeActive 时触发，左键攻击完全不会调用这里。
    internal sealed partial class NewLegendBlossomFluxHoldOut
    {
        private int lastChargeHaloSpawnTick = -1;

        // 供 BFRightChargeHaloParticle 跨类查询的只读入口：ChargeReady 本身是私有的，粒子那边需要每帧读取它
        // 才能知道“什么时候该从加速转圈切换成减速淡出”。
        internal bool GetHaloChargeReady() => ChargeReady;

        private void UpdateRightChargeHaloSpawning(float chargeCompletion)
        {
            if (Main.dedServ)
                return;

            // 蓄满之后不再生成新的光环，交给已经存在的那些自己减速、自己淡出。
            if (ChargeReady)
                return;

            float charge = MathHelper.Clamp(chargeCompletion, 0f, 1f);
            if (charge <= 0.001f)
                return;

            int tick = (int)Main.GameUpdateCount;
            if (tick == lastChargeHaloSpawnTick)
                return;
            lastChargeHaloSpawnTick = tick;

            Color mainColor = BFArrowCommon.GetPresetColor(CurrentPreset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(CurrentPreset);

            // 每个光环实例的作用半径 40~72 像素
            if (tick % 16 == 0)
            {
                float radius = 40f + Main.rand.Next(2) * 16f;
                float startRot = Main.rand.NextFloat(-0.6f, -0.4f);
                float zRot = Main.rand.NextFromList(0.6f, MathHelper.Pi - 0.6f) + Main.rand.NextFloat(-0.4f, 0.4f);
                float exRot = Main.rand.Next(4) * MathHelper.TwoPi * 0.6f + Main.rand.NextFloat(-0.4f, 0.4f);

                BFRightChargeHaloParticle.Spawn(Projectile, radius, startRot, zRot, exRot, Color.Lerp(mainColor, Color.White, 0.15f) * charge, charge);
            }

            if (tick % 20 == 0)
            {
                float radius = 48f + Main.rand.Next(2) * 24f;
                float startRot = Main.rand.NextFloat(-0.6f, -0.4f);
                float zRot = Main.rand.NextFromList(0.7f, MathHelper.Pi - 0.7f) + Main.rand.NextFloat(-0.3f, 0.3f);
                float exRot = Main.rand.Next(4) * MathHelper.TwoPi * 0.6f + Main.rand.NextFloat(-0.4f, 0.4f);

                BFRightChargeHaloParticle.Spawn(Projectile, radius, startRot, zRot, exRot, accentColor * (0.55f * charge), charge);
            }
        }
    }
}
