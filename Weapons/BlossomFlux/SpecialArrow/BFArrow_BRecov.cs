using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    // B 战术右键箭：先输出，再折返寻找血线最低的队友完成治疗。
    internal class BFArrow_BRecov : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        private int hitCounter;
        private int storedHeal = 3;

        private ref float State => ref Projectile.ai[0];
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            BFArrowCommon.SetBaseArrowDefaults(Projectile, width: 14, height: 34, timeLeft: 210, penetrate: -1, extraUpdates: 1, tileCollide: true);
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => State == 0f ? null : false;

        public override bool? CanHitNPC(NPC target) => State == 0f ? null : false;

        public override void AI()
        {
            Timer++;
            Lighting.AddLight(Projectile.Center, new Color(110, 255, 186).ToVector3() * 0.42f);

            if (State == 0f)
            {
                BFArrowCommon.FaceForward(Projectile);
                Projectile.velocity *= 1.002f;

                if (Main.rand.NextBool(2))
                {
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center,
                        DustID.GemEmerald,
                        -Projectile.velocity * 0.12f + Main.rand.NextVector2Circular(0.8f, 0.8f),
                        100,
                        new Color(110, 255, 186),
                        Main.rand.NextFloat(0.9f, 1.2f));
                    dust.noGravity = true;
                }

                if (Timer >= 34f || Projectile.timeLeft < 120)
                    BeginReturn();

                return;
            }

            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Player healTarget = BFArrowCommon.FindLowestHealthPlayer(owner);
            Vector2 desiredVelocity = (healTarget.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 18f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.12f);
            BFArrowCommon.FaceForward(Projectile);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GreenTorch,
                    Main.rand.NextVector2Circular(0.5f, 0.5f),
                    100,
                    new Color(140, 255, 180),
                    Main.rand.NextFloat(0.95f, 1.25f));
                dust.noGravity = true;
            }

            if (Projectile.Hitbox.Intersects(healTarget.Hitbox))
            {
                int healAmount = Utils.Clamp(storedHeal + hitCounter * 2, 3, 12);
                healTarget.statLife += healAmount;
                if (healTarget.statLife > healTarget.statLifeMax2)
                    healTarget.statLife = healTarget.statLifeMax2;

                healTarget.HealEffect(healAmount, true);
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.55f, Pitch = 0.25f }, healTarget.Center);
                Projectile.Kill();
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (State != 0f)
                return;

            hitCounter++;
            storedHeal = Utils.Clamp(storedHeal + 2 + damageDone / 50, 3, 10);

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    target.Center,
                    DustID.GemEmerald,
                    Main.rand.NextVector2Circular(2.8f, 2.8f),
                    100,
                    new Color(110, 255, 186),
                    Main.rand.NextFloat(0.9f, 1.3f));
                dust.noGravity = true;
            }

            BeginReturn();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            BeginReturn();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GreenTorch,
                    Main.rand.NextVector2Circular(2.2f, 2.2f),
                    100,
                    new Color(140, 255, 190),
                    Main.rand.NextFloat(0.9f, 1.35f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            BFArrowCommon.DrawAfterimagesThenProjectile(Projectile, lightColor);
            return false;
        }

        private void BeginReturn()
        {
            if (State != 0f)
                return;

            State = 1f;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Utils.Clamp(Projectile.timeLeft, 120, 210);
            Projectile.netUpdate = true;
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.35f, Pitch = 0.25f }, Projectile.Center);
        }
    }
}
