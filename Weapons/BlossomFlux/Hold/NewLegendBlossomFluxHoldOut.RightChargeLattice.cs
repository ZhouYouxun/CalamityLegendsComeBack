using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using Microsoft.Xna.Framework;
using Terraria;
using HaloParams = CalamityLegendsComeBack.Weapons.BlossomFlux.BFRightChargeHaloProj.HaloSpawnParams;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    // 右键蓄力专属视觉：持续生成短命的着色器环绕弹幕（实现见 BFRightChargeHaloProj.cs）。
    // 五种战术各有一套完全不同的轨迹风格——数量、生成位置、半径、寿命、转速与加/减速全部在这里按 preset 分别定义，
    // 只在 rightChargeActive 时触发，左键攻击完全不会调用这里。
    internal sealed partial class NewLegendBlossomFluxHoldOut
    {
        private int lastChargeHaloSpawnTick = -1;

        // 供 BFRightChargeHaloProj 跨类查询的只读入口：光环每帧读这两个，才知道何时从加速转圈切到减速淡出。
        internal bool GetHaloChargeReady() => ChargeReady;
        internal bool GetHaloRightChargeActive() => rightChargeActive;

        private void UpdateRightChargeHaloSpawning(float chargeCompletion)
        {
            if (Main.dedServ)
                return;

            // 蓄满后继续生成，直到右键松开；已有光环仍按自己的生命周期淡出。
            float charge = ChargeReady ? 1f : MathHelper.Clamp(chargeCompletion, 0f, 1f);
            if (charge <= 0.001f)
                return;

            int tick = (int)Main.GameUpdateCount;
            if (tick == lastChargeHaloSpawnTick)
                return;
            lastChargeHaloSpawnTick = tick;

            Color mainColor = BFArrowCommon.GetPresetColor(CurrentPreset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(CurrentPreset);

            switch (CurrentPreset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                    SpawnSlashStarHalos(tick, charge, mainColor, accentColor);
                    break;
                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                    SpawnRisingHelixHalos(tick, charge, mainColor, accentColor);
                    break;
                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                    SpawnGyroRingHalos(tick, charge, mainColor, accentColor);
                    break;
                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                    SpawnEmberBurstHalos(tick, charge, mainColor, accentColor);
                    break;
                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                    SpawnWobbleCloudHalos(tick, charge, mainColor, accentColor);
                    break;
            }
        }

        // A 破甲：少量、短命、快转的尖瓣星，像绕身的刀光。锐利半径脉冲 + 高转速，偶尔成对交叉。
        private void SpawnSlashStarHalos(int tick, float charge, Color mainColor, Color accentColor)
        {
            if (tick % 8 != 0)
                return;

            bool crossPair = tick % 24 == 0;
            int count = crossPair ? 2 : 1;
            float baseSpin = Main.rand.NextFloat(MathHelper.TwoPi);
            float lobes = Main.rand.NextBool() ? 2f : 3f;

            for (int i = 0; i < count; i++)
            {
                HaloParams p = default;
                p.Style = BFHaloStyle.SlashStar;
                p.Color = Color.Lerp(mainColor, accentColor, 0.3f) * charge;
                p.Charge = charge;
                p.AnchorForward = 14f;
                p.AnchorSide = Main.rand.NextFloat(-4f, 4f);
                p.LifeSpan = Main.rand.NextFloat(22f, 34f);
                p.FadeInFraction = 0.25f;
                p.FadeOutFraction = 0.35f;
                p.StartRotation = baseSpin + (crossPair ? i * MathHelper.Pi : 0f);
                p.MinSpeed = 0.14f;
                p.MaxSpeed = 0.42f;   // 快
                p.ChargingFollow = 0.18f;
                p.ReadyDecel = 0.08f;
                p.FadeDecay = 0.90f;  // 收得干脆
                p.Radius = Main.rand.NextFloat(34f, 52f);
                p.HalfWidth = 4.2f;
                p.TrailPoints = 9;    // 短拖尾更像刀光
                p.Lobes = lobes;
                p.Sharpness = Main.rand.NextFloat(2.4f, 3.2f);
                p.SpinPhase = Main.rand.NextFloat(MathHelper.TwoPi);
                BFRightChargeHaloProj.Spawn(Projectile, p);
            }
        }

        // B 恢复：柔和、慢转、长命的小圆，持续上浮并呼吸，像治愈光点缓缓升起。
        private void SpawnRisingHelixHalos(int tick, float charge, Color mainColor, Color accentColor)
        {
            if (tick % 12 != 0)
                return;

            int count = tick % 36 == 0 ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                HaloParams p = default;
                p.Style = BFHaloStyle.RisingHelix;
                p.Color = Color.Lerp(mainColor, Color.White, 0.3f) * (0.8f * charge);
                p.Charge = charge;
                p.AnchorForward = 10f;
                p.AnchorSide = Main.rand.NextFloat(-6f, 6f);
                p.LifeSpan = Main.rand.NextFloat(70f, 105f);
                p.FadeInFraction = 0.3f;
                p.FadeOutFraction = 0.4f;
                p.StartRotation = Main.rand.NextFloat(MathHelper.TwoPi);
                p.MinSpeed = 0.05f;
                p.MaxSpeed = 0.16f;   // 慢
                p.ChargingFollow = 0.08f;
                p.ReadyDecel = 0.04f;
                p.FadeDecay = 0.95f;  // 温柔散去
                p.Radius = Main.rand.NextFloat(24f, 40f);
                p.HalfWidth = 3.0f;
                p.TrailPoints = 12;   // 长而顺滑
                p.RiseSpeed = Main.rand.NextFloat(0.35f, 0.6f);
                p.Squash = 0.62f;
                p.SpinPhase = Main.rand.NextFloat(MathHelper.TwoPi);
                BFRightChargeHaloProj.Spawn(Projectile, p);
            }
        }

        // C 侦测：整环 5 枚共享同一倾斜平面、相位均分 → 陀螺仪 / 雷达环，匀速精密（转速不吃蓄力）。
        // 每隔一段再叠一层不同倾角的环，形成双环交叠的机械感。
        private void SpawnGyroRingHalos(int tick, float charge, Color mainColor, Color accentColor)
        {
            const int ringCount = 5;
            bool primaryRing = tick % 20 == 0;
            bool secondaryRing = tick % 33 == 0;
            if (!primaryRing && !secondaryRing)
                return;

            void SpawnRing(float tiltZ, float tiltEx, float radius, Color color, float speed)
            {
                for (int i = 0; i < ringCount; i++)
                {
                    HaloParams p = default;
                    p.Style = BFHaloStyle.GyroRing;
                    p.Color = color;
                    p.Charge = charge;
                    p.AnchorForward = 16f;
                    p.AnchorSide = 0f;
                    p.LifeSpan = Main.rand.NextFloat(48f, 62f);
                    p.FadeInFraction = 0.28f;
                    p.FadeOutFraction = 0.3f;
                    p.StartRotation = MathHelper.TwoPi * i / ringCount; // 相位均分
                    p.MinSpeed = speed;
                    p.MaxSpeed = speed; // 匀速：不随蓄力变化，机械精密
                    p.ChargingFollow = 0.1f;
                    p.ReadyDecel = 0.05f;
                    p.FadeDecay = 0.93f;
                    p.Radius = radius;
                    p.HalfWidth = 2.6f; // 纤细精密
                    p.TrailPoints = 10;
                    p.TiltZ = tiltZ;
                    p.TiltEx = tiltEx;
                    BFRightChargeHaloProj.Spawn(Projectile, p);
                }
            }

            if (primaryRing)
            {
                float tiltZ = 0.55f + Main.rand.NextFloat(-0.12f, 0.12f);
                float tiltEx = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = 44f + Main.rand.NextFloat(0f, 12f);
                SpawnRing(tiltZ, tiltEx, radius, Color.Lerp(mainColor, accentColor, 0.5f) * charge, 0.17f);
            }

            if (secondaryRing)
            {
                float tiltZ = (MathHelper.Pi - 0.55f) + Main.rand.NextFloat(-0.12f, 0.12f);
                float tiltEx = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = 56f + Main.rand.NextFloat(0f, 12f);
                SpawnRing(tiltZ, tiltEx, radius, accentColor * (0.65f * charge), 0.13f);
            }
        }

        // D 爆破：成簇的弹道余烬——近枪口高速外抛，强阻力减速 + 微重力下坠，像迸溅的火星碎屑。
        private void SpawnEmberBurstHalos(int tick, float charge, Color mainColor, Color accentColor)
        {
            if (tick % 16 != 0)
                return;

            int count = Main.rand.Next(4, 7);
            for (int i = 0; i < count; i++)
            {
                float speed = Main.rand.NextFloat(4.5f, 8f);
                Vector2 dir = Main.rand.NextVector2Unit();
                bool bright = Main.rand.NextBool(3);

                HaloParams p = default;
                p.Style = BFHaloStyle.EmberBurst;
                p.Color = (bright ? accentColor : Color.Lerp(mainColor, accentColor, 0.35f)) * charge;
                p.Charge = charge;
                p.AnchorForward = 22f; // 从枪口迸出
                p.AnchorSide = Main.rand.NextFloat(-6f, 6f);
                p.LifeSpan = Main.rand.NextFloat(34f, 60f);
                p.FadeInFraction = 0.18f;
                p.FadeOutFraction = 0.4f;
                p.FadeDecay = 0.90f;
                p.HalfWidth = 3.4f;
                p.TrailPoints = 8;
                p.EmberVelocity = dir * speed;
                p.EmberDrag = 0.90f;   // 强减速
                p.EmberGravity = 0.06f; // 微下坠 / 拖出弧线
                BFRightChargeHaloProj.Spawn(Projectile, p);
            }
        }

        // E 瘟疫：慢转、长命、粘稠的抖动云——半径缓慢外扩，垂直方向叠层正弦晃动，像扩散的孢子。
        private void SpawnWobbleCloudHalos(int tick, float charge, Color mainColor, Color accentColor)
        {
            if (tick % 10 != 0)
                return;

            int count = tick % 22 == 0 ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                HaloParams p = default;
                p.Style = BFHaloStyle.WobbleCloud;
                p.Color = Color.Lerp(mainColor, accentColor, 0.28f) * (0.85f * charge);
                p.Charge = charge;
                p.AnchorForward = 14f;
                p.AnchorSide = Main.rand.NextFloat(-8f, 8f);
                p.LifeSpan = Main.rand.NextFloat(60f, 95f);
                p.FadeInFraction = 0.3f;
                p.FadeOutFraction = 0.4f;
                p.StartRotation = Main.rand.NextFloat(MathHelper.TwoPi);
                p.MinSpeed = 0.04f;
                p.MaxSpeed = 0.12f;   // 慢而黏
                p.ChargingFollow = 0.06f;
                p.ReadyDecel = 0.035f;
                p.FadeDecay = 0.94f;
                p.Radius = Main.rand.NextFloat(30f, 46f);
                p.HalfWidth = 4.2f;   // 柔软偏宽
                p.TrailPoints = 13;
                p.ExpandSpeed = Main.rand.NextFloat(0.18f, 0.32f);
                p.WobbleAmp = Main.rand.NextFloat(6f, 11f);
                p.WobbleFreq = Main.rand.NextFloat(0.10f, 0.16f);
                p.SpinPhase = Main.rand.NextFloat(MathHelper.TwoPi);
                BFRightChargeHaloProj.Spawn(Projectile, p);
            }
        }
    }
}
