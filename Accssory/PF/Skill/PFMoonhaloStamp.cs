using CalamityMod.Buffs.DamageOverTime;
using CalamityLegendsComeBack.Weapons.PristineFury;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.PF.Skill
{
    // 月华烙印 — 月饼配饰的范围效果弹幕。
    // 在命中位置停留 240 帧（4 秒），期间范围内的敌人会以双倍速率积累纯化进度。
    // 已达到纯化上限的敌人每 60 帧触发一次小型纯化爆燃。
    internal sealed class PFMoonhaloStamp : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 240;
        private const float Radius = 120f;
        private const int CombustionInterval = 60;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = (int)(Radius * 2);
            Projectile.height = (int)(Radius * 2);
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime + 5;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Timer++;
            if (Timer > Lifetime)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;

            int cap = GetPurificationCap();
            bool periodic = (int)Timer % CombustionInterval == 0;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || npc.friendly || npc.dontTakeDamage)
                    continue;
                if (Vector2.Distance(npc.Center, Projectile.Center) > Radius)
                    continue;

                PFPurificationGlobalNPC purif = npc.GetGlobalNPC<PFPurificationGlobalNPC>();

                // Double purification accumulation rate by registering an extra hit each frame.
                purif.RegisterPFHit(Projectile.owner);

                // Mini-combustion at cap.
                if (periodic && purif.PurificationLevel >= cap && Main.myPlayer == Projectile.owner)
                    SpawnMiniCombustion(npc);
            }

            // Visual: faint pulsing holy light ring.
            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float r = Main.rand.NextFloat(0.5f, 1f) * Radius;
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + angle.ToRotationVector2() * r,
                    DustID.AncientLight,
                    Vector2.Zero,
                    0,
                    new Color(200, 200, 255),
                    Main.rand.NextFloat(0.4f, 0.8f));
                d.noGravity = true;
            }
        }

        private void SpawnMiniCombustion(NPC target)
        {
            Color c = new Color(220, 200, 255);
            for (int k = 0; k < 8; k++)
            {
                float angle = MathHelper.TwoPi * k / 8f;
                Dust d = Dust.NewDustPerfect(
                    target.Center,
                    DustID.AncientLight,
                    angle.ToRotationVector2() * Main.rand.NextFloat(2f, 4f),
                    0, c, 1f);
                d.noGravity = true;
            }
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 120);
        }

        private static int GetPurificationCap()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead) continue;
                if (p.HeldItem?.type == ModContent.ItemType<NewLegendPristineFury>())
                    return p.GetModPlayer<PFAccessoryPlayer>().PurificationCap;
            }
            return 2;
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
