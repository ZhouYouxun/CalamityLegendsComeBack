using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    // 终结技：金色神手。从远处飞来，蓄力后握住玩家，赋予10秒无敌。
    // 贴图暂用透明占位符，后续替换。
    public class AegisUltimateHand : ModProjectile
    {
        // 手的贴图暂缺，用透明代替
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Player Owner => Main.player[Projectile.owner];

        // ai[0] = 状态
        private const int StateApproach = 0;   // 向玩家靠近
        private const int StateChargeUp = 1;   // 蓄力振动
        private const int StateGripping  = 2;  // 握住玩家（效果生效）

        private const int ApproachTime  = 50;  // 飞行阶段帧数
        private const int ChargeUpTime  = 25;  // 蓄力帧数

        private ref float State => ref Projectile.ai[0];
        private ref float Timer => ref Projectile.ai[1];

        private static readonly Color HandGold = new(255, 220, 80);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 80;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60 + BalanceAegisBlade.UltimateDuration + ApproachTime + ChargeUpTime;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Timer++;

            switch ((int)State)
            {
                case StateApproach:  DoApproach(); break;
                case StateChargeUp:  DoChargeUp(); break;
                case StateGripping:  DoGripping(); break;
            }
        }

        private void DoApproach()
        {
            float progress = Timer / ApproachTime;
            // 从初始位置快速飞向玩家
            Vector2 target = Owner.Center;
            float lerpFactor = CalamityUtils.EaseInOutExp(progress, 3f, 3f);
            Projectile.Center = Vector2.Lerp(Projectile.Center, target, lerpFactor * 0.15f);

            // 光晕粒子轨迹
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 vel = Main.rand.NextVector2Circular(2f, 2f);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(20f, 20f), vel, false,
                    Main.rand.Next(8, 16), 0.05f, HandGold, new Vector2(1.2f, 0.3f), true, false, 0.8f));
            }

            if (Timer >= ApproachTime)
            {
                State = StateChargeUp;
                Timer = 0;
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.25f }, Owner.Center);
            }
        }

        private void DoChargeUp()
        {
            // 震颤
            float shake = 4f * (1f - Timer / ChargeUpTime);
            Projectile.Center = Owner.Center + Main.rand.NextVector2Circular(shake, shake);

            if (!Main.dedServ && Timer % 2 == 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 orbit = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(30f, 80f);
                    Vector2 vel = (Projectile.Center - (Projectile.Center + orbit)).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(4f, 9f);
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(
                        Projectile.Center + orbit, vel, "CalamityMod/Particles/Sparkle",
                        false, Main.rand.Next(10, 18), Main.rand.NextFloat(0.7f, 1.3f),
                        HandGold, new Vector2(0.3f, 1.4f), true, true, shrinkSpeed: 0.15f));
                }
            }

            if (Timer >= ChargeUpTime)
            {
                State = StateGripping;
                Timer = 0;
                Owner.GetModPlayer<AegisBladePlayer>().ActivateUltimate();
                SoundEngine.PlaySound(SoundID.Item67 with { Volume = 1f, Pitch = 0.2f }, Owner.Center);

                if (!Main.dedServ)
                {
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                        Owner.Center, Vector2.Zero, HandGold, Vector2.One, 0f, 0.05f, 1.8f, 24));
                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(5f, 12f);
                        Dust d = Dust.NewDustPerfect(Owner.Center, DustID.GoldFlame, vel, 0, HandGold, 1.3f);
                        d.noGravity = true;
                    }
                }
            }
        }

        private void DoGripping()
        {
            // 跟随玩家
            Projectile.Center = Owner.Center;

            if (!Owner.GetModPlayer<AegisBladePlayer>().UltimateActive)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // 手的贴图待替换：用光晕占位
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            float pulse = 0.8f + 0.2f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 6f);
            float size = (int)State == StateGripping ? 2.5f : 1.2f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPos, null,
                HandGold with { A = 0 } * 0.7f * pulse,
                0f, bloom.Size() * 0.5f, size, SpriteEffects.None, 0);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
