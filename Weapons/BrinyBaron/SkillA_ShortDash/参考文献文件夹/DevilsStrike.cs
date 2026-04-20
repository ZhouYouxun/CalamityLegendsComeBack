using CalamityMod;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash.参考文献文件夹
{
    public class DevilsStrike : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override LocalizedText DisplayName => CalamityUtils.GetItemName<DevilsDevastation>();
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public int time = 0;
        public float fade = 0;
        public int angleTimer = 20;
        public Color clr = Color.Lerp(Color.DeepPink, Color.Orange, 0.5f);
        public int curveDir = 1;
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
        }

        public override void AI()
        {
            float deathLerp = Utils.Remap(Projectile.timeLeft, 300, 0, 40, 5);

            if (time == 0)
            {
                if (Projectile.ai[0] == 0)
                    Projectile.ai[0] = 0.75f;
                angleTimer += Main.rand.Next(60, 80 + 1);
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
                angleTimer = Main.rand.Next(60, 80 + 1);
                Projectile.velocity = Projectile.velocity.RotatedBy((0.9f * Utils.GetLerpValue(0, 300, Projectile.timeLeft, true) * curveDir) * Main.rand.NextFloat(0.85f, 1.15f) * Projectile.ai[0]);
                curveDir *= -1;
            }
            if (time % 6 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center, Projectile.velocity * 0.1f, "CalamityMod/Particles/BloomCircle", false, 20, Main.rand.NextFloat(0.05f, 0.055f) * deathLerp * Projectile.ai[0], (Main.rand.NextBool() ? Color.MediumOrchid : clr) * (CalamityClientConfig.Instance.Photosensitivity ? 0.25f : 0.7f), new Vector2(0.8f, 1), shrinkSpeed: 0.2f));
            }
            if (angleTimer % 60 == 0 && Projectile.ai[0] > 0.7f)
            {
                if (Projectile.ai[1] > 0)
                    Projectile.ai[0] -= 0.08f;
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile crack = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, (Projectile.velocity * 0.8f).RotatedBy(Main.rand.NextBool() ? -0.6f : 0.6f), ModContent.ProjectileType<DevilsStrike>(), Projectile.damage, 0f, Projectile.owner, Projectile.ai[0] * 0.7f, 0);
                    crack.timeLeft = 250;
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => false;
    }
}
