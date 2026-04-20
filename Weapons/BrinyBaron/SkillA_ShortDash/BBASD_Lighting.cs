using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash
{
    internal class BBASD_Lighting : ModProjectile, ILocalizedModType
    {
        private int time;
        private int angleTimer = 20;
        private int curveDir = 1;
        private readonly Color boltColor = Color.Lerp(new Color(52, 186, 255), new Color(218, 248, 255), 0.5f);
        private readonly Color accentColor = new Color(128, 230, 255);

        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.extraUpdates = 80;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
            Projectile.timeLeft = 400;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            float deathLerp = Utils.Remap(Projectile.timeLeft, 300f, 0f, 40f, 5f);

            if (time == 0)
            {
                if (Projectile.ai[0] == 0f)
                    Projectile.ai[0] = 0.75f;

                angleTimer += Main.rand.Next(60, 81);
                curveDir = Main.rand.NextBool() ? 1 : -1;
            }

            time++;

            if (angleTimer > 0)
            {
                angleTimer--;
                Projectile.velocity = Projectile.velocity.RotatedByRandom(0.01f);
            }
            else
            {
                angleTimer = Main.rand.Next(60, 81);
                Projectile.velocity = Projectile.velocity.RotatedBy(
                    (0.9f * Utils.GetLerpValue(0f, 300f, Projectile.timeLeft, true) * curveDir) *
                    Main.rand.NextFloat(0.85f, 1.15f) *
                    Projectile.ai[0]);
                curveDir *= -1;
            }

            if (time % 6 == 0)
            {
                GeneralParticleHandler.SpawnParticle(
                    new CustomSpark(
                        Projectile.Center,
                        Projectile.velocity * 0.1f,
                        "CalamityMod/Particles/BloomCircle",
                        false,
                        20,
                        Main.rand.NextFloat(0.05f, 0.055f) * deathLerp * Projectile.ai[0],
                        (Main.rand.NextBool() ? accentColor : boltColor) * 0.7f,
                        new Vector2(0.8f, 1f),
                        shrinkSpeed: 0.2f));
            }

            Lighting.AddLight(Projectile.Center, 0.06f * Projectile.ai[0], 0.28f * Projectile.ai[0], 0.42f * Projectile.ai[0]);

            if (angleTimer % 60 == 0 && Projectile.ai[0] > 0.7f)
            {
                if (Projectile.ai[1] > 0f)
                    Projectile.ai[0] -= 0.08f;

                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile crack = Projectile.NewProjectileDirect(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        (Projectile.velocity * 0.8f).RotatedBy(Main.rand.NextBool() ? -0.6f : 0.6f),
                        Type,
                        Projectile.damage,
                        0f,
                        Projectile.owner,
                        Projectile.ai[0] * 0.7f,
                        0f);
                    crack.timeLeft = 250;
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => false;
    }
}
