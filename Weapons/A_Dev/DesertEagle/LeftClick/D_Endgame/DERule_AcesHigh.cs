using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Healing;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.D_Endgame
{
    /// <summary>
    /// 极魂符枪：四牌轮转射击——
    /// ♥ 红心：穿透弹，命中吸血 1%；
    /// ♣ 梅花：命中时分裂 3 颗追踪弹（±25°/0°）；
    /// ♦ 方块：命中时蛛网爆炸；
    /// ♠ 黑桃：无限穿透+高速，幻影拖影。
    /// </summary>
    public class DERule_AcesHigh : DEBulletRule
    {
        // Per-card colors
        private static readonly Color HeartRed = new(255, 60, 80);
        private static readonly Color ClubPurple = new(160, 60, 255);
        private static readonly Color DiamondOrange = new(255, 170, 30);
        private static readonly Color SpadeBlue = new(80, 100, 255);
        private static readonly Color CardWhite = new(240, 240, 255);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.AcesHigh>();

        // ai[1] = 牌型 (0=♥,1=♣,2=♦,3=♠)
        public override float GetShotExtra(DesertEagleSlotPlayer slotPlayer)
        {
            int current = slotPlayer.CardIndex;
            slotPlayer.CardIndex = (current + 1) % 4;
            return current;
        }

        private int CardType(Projectile p) => (int)(p.ai[1] + 0.5f);

        public override int Penetrate => 2;  // 基础；♠ 覆盖为 -1

        public override void SetDefaults(Projectile projectile)
        {
            int card = CardType(projectile);
            switch (card)
            {
                case 0:  // ♥ 红心
                    projectile.penetrate = 2;
                    projectile.light = 0.5f;
                    break;
                case 1:  // ♣ 梅花
                    projectile.penetrate = 1;
                    projectile.light = 0.45f;
                    break;
                case 2:  // ♦ 方块
                    projectile.penetrate = 2;
                    projectile.light = 0.5f;
                    break;
                case 3:  // ♠ 黑桃
                    projectile.penetrate = -1;
                    projectile.light = 0.6f;
                    projectile.extraUpdates = 6;  // 更快
                    break;
            }
        }

        public override void AI(Projectile projectile, Player owner)
        {
            int card = CardType(projectile);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            switch (card)
            {
                case 0:  // ♥ 红心：红色光晕尾迹
                {
                    Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.Blood,
                        -forward * 0.4f, 110, HeartRed, 0.9f);
                    dust.noGravity = true;
                    if (!Main.dedServ && Main.rand.NextBool(4))
                        GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                            projectile.Center, -forward * 0.7f, false, 5, 0.013f, HeartRed, new Vector2(0.55f, 1.8f)));
                    Lighting.AddLight(projectile.Center, HeartRed.R / 255f * 0.5f, 0f, HeartRed.B / 255f * 0.25f);
                    break;
                }
                case 1:  // ♣ 梅花：紫色魔法尾迹
                {
                    Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.Shadowflame,
                        -forward * 0.4f, 110, ClubPurple, 0.9f);
                    dust.noGravity = true;
                    if (!Main.dedServ && Main.rand.NextBool(4))
                        GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                            projectile.Center, -forward * 0.7f, false, 5, 0.014f, ClubPurple, new Vector2(0.5f, 1.6f)));
                    Lighting.AddLight(projectile.Center, ClubPurple.R / 255f * 0.35f, 0f, ClubPurple.B / 255f * 0.5f);
                    break;
                }
                case 2:  // ♦ 方块：橙色尾迹
                {
                    Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.GoldCoin,
                        -forward * 0.4f, 110, DiamondOrange, 0.95f);
                    dust.noGravity = true;
                    if (!Main.dedServ && Main.rand.NextBool(4))
                        GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                            projectile.Center, -forward * 0.7f, false, 5, 0.013f, DiamondOrange, new Vector2(0.5f, 1.8f)));
                    Lighting.AddLight(projectile.Center, DiamondOrange.R / 255f * 0.5f, DiamondOrange.G / 255f * 0.4f, 0f);
                    break;
                }
                case 3:  // ♠ 黑桃：幽蓝尾迹+后像
                {
                    Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.TintableDustLighted,
                        -forward * 0.4f, 110, SpadeBlue, 1.0f);
                    dust.noGravity = true;
                    if (!Main.dedServ && Main.rand.NextBool(3))
                        GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                            projectile.Center, -forward * 1.0f, false, 5, 0.015f, CardWhite, new Vector2(0.4f, 2.5f)));
                    Lighting.AddLight(projectile.Center, SpadeBlue.R / 255f * 0.25f, SpadeBlue.G / 255f * 0.3f, SpadeBlue.B / 255f * 0.6f);
                    break;
                }
            }
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            int card = CardType(projectile);
            switch (card)
            {
                case 0:  // ♥ 红心：1% 吸血
                    owner.SpawnLifeStealProjectile(target, projectile,
                        ModContent.ProjectileType<TransfusionTrail>(),
                        Math.Max(1, (int)Math.Round(hit.Damage * 0.01)));
                    SpawnHeartImpact(projectile.Center);
                    break;
                case 1:  // ♣ 梅花：分裂
                    SpawnClubSplit(projectile, target.Center);
                    SpawnClubImpact(projectile.Center);
                    break;
                case 2:  // ♦ 方块：蛛网爆炸
                    SpawnDiamondExplosion(projectile.Center);
                    break;
                case 3:  // ♠ 黑桃：穿透继续
                    SpawnSpadePassthrough(projectile.Center);
                    break;
            }
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            int card = CardType(projectile);
            Color c = card == 0 ? HeartRed : card == 1 ? ClubPurple : card == 2 ? DiamondOrange : SpadeBlue;
            if (!Main.dedServ)
                GeneralParticleHandler.SpawnParticle(new StrongBloom(projectile.Center, Vector2.Zero, c, 0.6f, 14));
        }

        // ── 牌型特效 ──────────────────────────────────────────────

        private static void SpawnHeartImpact(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, HeartRed, 0.75f, 16));
                for (int i = 0; i < 8; i++)
                {
                    float angle = MathHelper.TwoPi * i / 8f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        pos + angle.ToRotationVector2() * 6f, angle.ToRotationVector2() * 7f,
                        false, 9, 0.02f, HeartRed, new Vector2(0.9f, 0.45f)));
                }
            }
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.Blood,
                    Main.rand.NextVector2Circular(6f, 6f), 100, HeartRed, 1.0f);
                dust.noGravity = true;
            }
        }

        private static void SpawnClubSplit(Projectile source, Vector2 hitPos)
        {
            int splitDmg = (int)(source.damage * 0.5f);
            float[] offsets = { -MathHelper.ToRadians(25f), 0f, MathHelper.ToRadians(25f) };
            float baseAngle = source.velocity.ToRotation();
            foreach (float offset in offsets)
            {
                Vector2 vel = (baseAngle + offset).ToRotationVector2() * 10f;
                Projectile.NewProjectile(source.GetSource_FromAI(), hitPos + vel * 0.5f, vel,
                    ModContent.ProjectileType<DEBullet_CardSplit>(),
                    splitDmg, source.knockBack * 0.4f, source.owner);
            }
        }

        private static void SpawnClubImpact(Vector2 pos)
        {
            if (!Main.dedServ)
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, ClubPurple, 0.7f, 16));
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.Shadowflame,
                    Main.rand.NextVector2Circular(6f, 6f), 100, ClubPurple, 1.0f);
                dust.noGravity = true;
            }
        }

        private static void SpawnDiamondExplosion(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, DiamondOrange, 1.0f, 22));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, DiamondOrange * 0.75f,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, 0f, 0f, 0.08f, 20, true, 0.85f));
            }
            for (int i = 0; i < 28; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.Web,
                    Main.rand.NextVector2Circular(10f, 10f), 100, DiamondOrange, 1.2f);
                dust.noGravity = true;
            }
        }

        private static void SpawnSpadePassthrough(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    pos, Vector2.Zero, false, 8, 0.022f, CardWhite, new Vector2(1.2f, 1.2f)));
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, SpadeBlue, 0.5f, 10));
            }
        }

        public override bool PreDraw(Projectile projectile, Player owner, ref Color lightColor)
        {
            int card = CardType(projectile);
            if (card == 3)
            {
                // ♠ 黑桃：绘制多个后像
                CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], SpadeBlue, 1);
                return false;
            }
            return true;
        }

        public override string TooltipEffectEN =>
            "♥ Lifesteal (1%) · ♣ Triple split · ♦ Web explosion · ♠ Infinite pierce + phantom trail — cycling between all four";
        public override string TooltipEffectZH =>
            "♥红心吸血1% · ♣梅花三重分裂 · ♦方块蛛网爆炸 · ♠黑桃无限穿透+幻影残影 — 四牌依次轮转";
    }
}
