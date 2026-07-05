using CalamityLegendsComeBack.Accssory.PF;
using CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    // 大招「劫火重燃」群芒齐发阶段的单枚回响立柱：先于地面预警，再轰下一道印记色圣焰，
    // 命中后直接把目标纯化等级顶到当前上限，呼应百合系列的纯化机制。
    internal sealed class PristineFuryUltimateEchoPillar : ModProjectile, ILocalizedModType
    {
        internal const int ColumnWidth = 46;
        internal const int ColumnHeight = 320;
        private const int TelegraphFrames = 10;
        private const int ImpactFrames = 10;
        private const int FadeFrames = 16;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = ColumnWidth;
            Projectile.height = ColumnHeight;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = TelegraphFrames + ImpactFrames + FadeFrames;
        }

        public override bool? CanDamage() => Timer >= TelegraphFrames && Timer < TelegraphFrames + ImpactFrames;

        public override void AI()
        {
            Timer++;
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));
            Vector2 groundSpot = Projectile.Bottom;

            if (Timer == 1f && !Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    groundSpot, Vector2.Zero, theme * 0.9f,
                    new Vector2(1.6f, 1.6f), 0f, 0.9f, 0.08f, TelegraphFrames + 4));
            }

            if (Timer < TelegraphFrames)
            {
                SpawnTelegraphMotes(theme, groundSpot, Timer / (float)TelegraphFrames);
                Lighting.AddLight(groundSpot, theme.ToVector3() * 0.5f * (Timer / (float)TelegraphFrames));
                return;
            }

            if (Timer == TelegraphFrames)
                SpawnSlamBurst(theme, groundSpot);

            if (Timer < TelegraphFrames + ImpactFrames)
            {
                SpawnColumnFlames(theme, groundSpot);
                Lighting.AddLight(groundSpot, theme.ToVector3() * 1.3f);
            }
            else
            {
                Lighting.AddLight(groundSpot, theme.ToVector3() * 0.6f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);

            Player owner = Main.player[Projectile.owner];
            int cap = owner.GetModPlayer<PFAccessoryPlayer>().PurificationCap;
            PFPurificationGlobalNPC purification = target.GetGlobalNPC<PFPurificationGlobalNPC>();
            if (purification.PurificationLevel < cap)
                purification.PurificationLevel = cap;
        }

        private void SpawnTelegraphMotes(Color theme, Vector2 groundSpot, float progress)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 3; i++)
            {
                float angle = MathHelper.TwoPi * Main.rand.NextFloat();
                Vector2 pos = groundSpot + angle.ToRotationVector2() * MathHelper.Lerp(34f, 6f, progress);
                Dust dust = Dust.NewDustPerfect(pos, DustID.AncientLight, -Vector2.UnitY * Main.rand.NextFloat(1.5f, 3.5f), 0, theme, Main.rand.NextFloat(0.9f, 1.4f));
                dust.noGravity = true;
            }
        }

        private void SpawnSlamBurst(Color theme, Vector2 groundSpot)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.55f, Pitch = 0.25f, MaxInstances = 6 }, groundSpot);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceHolyBlastShoot") { Volume = 0.4f, Pitch = 0.3f, MaxInstances = 4 }, groundSpot);

            for (int i = 0; i < 14; i++)
            {
                float angle = MathHelper.TwoPi * i / 14f;
                Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(3f, 7f);
                GeneralParticleHandler.SpawnParticle(new CritSpark(groundSpot, vel, Color.Lerp(theme, Color.White, 0.3f), Color.White, 0.9f, 20));
            }

            for (int i = 0; i < 2; i++)
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    groundSpot, Main.rand.NextVector2Circular(1.5f, 1.5f) - Vector2.UnitY * 2f,
                    "CalamityMod/Particles/GlowSquareParticle", false, 30, Main.rand.NextFloat(0.4f, 0.65f),
                    theme, Vector2.One, true, true, MathHelper.PiOver4, spin: Main.rand.NextFloat(-0.1f, 0.1f)));
            }
        }

        private void SpawnColumnFlames(Color theme, Vector2 groundSpot)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 4; i++)
            {
                Vector2 pos = groundSpot + new Vector2(Main.rand.NextFloat(-18f, 18f), -Main.rand.NextFloat(0f, 300f));
                Dust dust = Dust.NewDustPerfect(pos, DustID.AncientLight, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(2f, 5f)), 0, theme, Main.rand.NextFloat(1f, 1.8f));
                dust.noGravity = true;
            }
        }
    }
}
