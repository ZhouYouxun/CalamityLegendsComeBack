using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.LeftGeneral
{
    internal sealed class YC_AuricJudgementWave : ModProjectile, ILocalizedModType
    {
        private static readonly Color WaveRed = new(255, 76, 34);
        private static readonly Color WaveGold = new(255, 214, 86);

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/Melee/JudgementProj";

        private ref float Timer => ref Projectile.ai[0];
        private float hitboxSize = 34f;

        public override void SetDefaults()
        {
            Projectile.width = 336;
            Projectile.height = 274;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 130;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.scale = 0.22f + Projectile.ai[0] * 0.05f;
            YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Blade);
        }

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Projectile.velocity *= 0.986f;
            Projectile.scale += 0.0028f;
            hitboxSize += 0.9f;

            if (Projectile.timeLeft < 42)
                Projectile.Opacity = Utils.GetLerpValue(0f, 42f, Projectile.timeLeft, true);

            Lighting.AddLight(Projectile.Center, Color.Lerp(WaveRed, WaveGold, 0.45f).ToVector3() * 0.5f);
            EmitWaveDust();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(new BalanceYharimsCrystal().GetFireDebuffType(), 240);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                modifiers.SourceDamage *= 0.88f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, hitboxSize * Projectile.scale, targetHitbox);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            float fade = Projectile.Opacity * Utils.GetLerpValue(0f, 18f, Timer, true);
            Color color = Color.Lerp(WaveRed, WaveGold, 0.42f) with { A = 0 };

            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2() * 9f * i;
                Main.spriteBatch.Draw(
                    texture,
                    Projectile.Center - Main.screenPosition + offset,
                    null,
                    color * fade * (0.72f - i * 0.08f),
                    Projectile.rotation,
                    texture.Size() * 0.5f,
                    new Vector2(1f + 0.015f * i, 1.2f - 0.06f * i) * Projectile.scale,
                    SpriteEffects.None,
                    0f);
            }

            return false;
        }

        private void EmitWaveDust()
        {
            if (Main.dedServ || !Main.rand.NextBool(3))
                return;

            Vector2 edge = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(1.8f) * hitboxSize * Projectile.scale;
            Dust dust = Dust.NewDustPerfect(edge, ModContent.DustType<SquashDust>(), -Projectile.velocity * Main.rand.NextFloat(0.3f, 0.8f), 0, Main.rand.NextBool() ? WaveGold : WaveRed, Main.rand.NextFloat(0.85f, 1.15f));
            dust.noGravity = true;

            if (Main.rand.NextBool(4))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    edge,
                    -Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.4f, 1.2f),
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    Main.rand.Next(10, 15),
                    Main.rand.NextFloat(0.18f, 0.36f),
                    Main.rand.NextBool() ? WaveGold : Color.White,
                    new Vector2(0.45f, 1.1f),
                    shrinkSpeed: 0.65f));
            }
        }
    }
}
