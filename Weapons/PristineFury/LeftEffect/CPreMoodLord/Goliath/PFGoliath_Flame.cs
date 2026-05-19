using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFGoliath_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Ranged/BlightFlames";

        private ref float ScaleFactor => ref Projectile.localAI[0];
        private ref float LightPower => ref Projectile.localAI[1];
        private int time;
        private bool postTileHit;
        private bool postEnemyHit;
        private NPC attachedTarget;
        private Vector2 attachedOffset;
        private readonly Color fogColor = new(30, 255, 30);

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 400;
            Projectile.extraUpdates = 5;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 55;
        }

        public override void AI()
        {
            time++;
            Projectile.rotation += Main.rand.NextFloat(0.2f, 0.9f);

            if (time > 6 && time < 540 && Main.rand.NextBool(2 + time / 7))
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center + Main.rand.NextVector2Circular(10f + time * 0.5f, 10f + time * 0.5f), Vector2.Zero, Main.rand.NextBool(3) ? Color.LimeGreen : Color.Green, Vector2.One, 0f, Main.rand.NextFloat(0.03f, 0.09f) + time * 0.00055f, 0f, 25));

            if (time > 6 && time < 150 && !postTileHit && !postEnemyHit && Main.rand.NextBool(3 + time / 7))
            {
                Particle smoke = new MediumMistParticle(Projectile.Center + Main.rand.NextVector2Circular(5f + time * 0.2f, 5f + time * 0.2f), -Projectile.velocity * 0.05f, Main.rand.NextBool(3) ? Color.LimeGreen : Color.Lime, Color.Black, Main.rand.NextFloat(0.3f, 0.8f) + time * 0.013f, 160);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (time == 8)
                ReleaseDiamondSpores();

            ScaleFactor += 0.0061f;
            ScaleFactor = MathHelper.Clamp(ScaleFactor, 0f, Projectile.scale * 0.85f);
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 1f, 0.25f) * ScaleFactor);

            Projectile.velocity *= 0.99f;
            Projectile.Opacity = Utils.GetLerpValue(30f, 50f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 130f, Projectile.timeLeft, true);

            if (postEnemyHit && !postTileHit && attachedTarget != null && attachedTarget.active && attachedTarget.life > 0)
            {
                Projectile.Center = attachedTarget.Center + attachedOffset;
                attachedOffset *= 0.99f;
            }

            if (Main.dedServ)
                return;

            float lightPowerBelow = Lighting.GetColor((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16 + 6).ToVector3().Length() / (float)System.Math.Sqrt(3D);
            LightPower = MathHelper.Lerp(LightPower, lightPowerBelow, 0.15f);
        }

        private void ReleaseDiamondSpores()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i <= 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GemDiamond, Projectile.velocity * 0.6f);
                dust.scale = Main.rand.NextFloat(1.1f, 1.9f);
                dust.velocity = Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.3f, 2.1f);
                dust.noGravity = true;
                dust.color = Main.rand.NextBool() ? Color.Chartreuse : Color.LimeGreen;
                dust.noLight = true;
                dust.alpha = 90;
            }
        }

        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            int size = (int)Utils.Remap(time, 0f, 90f, 10f, 95f);
            hitbox.Inflate(size, size);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Plague>(), 1200);
            target.GetGlobalNPC<PristineFuryGlobalNPC>().PlagueRelease = 360;
            EmitHitVapor();

            if (!postTileHit && !postEnemyHit)
            {
                SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.6f, Pitch = -0.1f }, Projectile.Center);
                Projectile.velocity = Vector2.Zero;
                Projectile.timeLeft = 300;
                postEnemyHit = true;
                attachedTarget = target;
                attachedOffset = Projectile.Center - target.Center;
            }
        }

        private void EmitHitVapor()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i <= 3; i++)
            {
                Particle smoke = new MediumMistParticle(Projectile.Center + Main.rand.NextVector2Circular(5f + time * 0.2f, 5f + time * 0.2f), new Vector2(Main.rand.NextFloat(2f, 6f), Main.rand.NextFloat(2f, 6f)).RotatedByRandom(60f), Main.rand.NextBool(3) ? Color.LimeGreen : Color.Lime, Color.Black, Main.rand.NextFloat(1.2f, 2.3f), 140);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                Projectile.damage = System.Math.Max(1, (int)(Projectile.damage * 0.95f));
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!postEnemyHit)
            {
                Projectile.velocity = oldVelocity * 0.95f;
                Projectile.Center -= Projectile.velocity;
                if (!postTileHit)
                {
                    Projectile.timeLeft = 800;
                    postTileHit = true;
                }
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Projectile.Opacity * 0.3f * Utils.GetLerpValue(0f, 0.08f, LightPower, true);
            Color drawColor = (fogColor with { A = 0 }) * opacity;

            Main.EntitySpriteDraw(texture, drawPosition + Main.rand.NextVector2Circular(19f, 19f), null, drawColor * 0.55f, Projectile.rotation, texture.Size() * 0.5f, ScaleFactor * 1.2f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, -Projectile.rotation * 0.9f, texture.Size() * 0.5f, ScaleFactor, SpriteEffects.None, 0);
            return false;
        }
    }
}
