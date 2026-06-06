using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFMoonlord_Flame : ModProjectile, ILocalizedModType
    {
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));
        private ref float Timer => ref Projectile.localAI[0];

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 80;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Color voidGreen = new(136, 255, 162);
            Lighting.AddLight(Projectile.Center, voidGreen.ToVector3() * 0.38f);

            NPC target = FindTarget(860f);
            if (target != null && Timer > 10f)
            {
                Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * MathHelper.Clamp(Projectile.velocity.Length() + 0.12f, 18f, 28f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.08f);
            }

            if (Main.dedServ)
                return;

            if (Main.rand.NextBool(2))
                GeneralParticleHandler.SpawnParticle(new SparkParticle(Projectile.Center, -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.18f), false, 12, Main.rand.NextFloat(0.45f, 0.82f), Main.rand.NextBool(3) ? Color.Black : voidGreen));

            if (Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), ModContent.DustType<VoidDustInverted>());
                dust.velocity = -Projectile.velocity.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.08f, 0.45f);
                dust.scale = Main.rand.NextFloat(0.55f, 1.05f);
                dust.noGravity = true;
                dust.color = voidGreen;
                dust.noLightEmittence = true;
            }
        }

        private NPC FindTarget(float range)
        {
            NPC bestTarget = null;
            float bestDistance = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = npc;
                }
            }

            return bestTarget;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) =>
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 180);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D water = ModContent.Request<Texture2D>("CalamityMod/Particles/WaterFlavored").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;
            Color voidGreen = new Color(136, 255, 162) with { A = 0 };
            Color black = Color.Black;
            Vector2 center = Projectile.Center - Main.screenPosition;

            PFLeftEffectRules.BeginAdditive();
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = i / (float)Projectile.oldPos.Length;
                Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(line, trailPosition, null, voidGreen * (1f - completion) * 0.34f, Projectile.rotation, line.Size() * 0.5f, new Vector2(0.12f, 0.58f), SpriteEffects.None, 0);
            }

            for (int i = 0; i < 6; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 6f + Timer * 0.06f).ToRotationVector2() * 8f;
                Vector2 aim = (Projectile.velocity.SafeNormalize(Vector2.UnitX) * 42f - offset).SafeNormalize(Vector2.UnitY);
                Main.EntitySpriteDraw(water, center + offset, null, black * 0.78f, aim.ToRotation() - MathHelper.PiOver2, water.Size() * 0.5f, new Vector2(0.18f, 0.72f), SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(bloom, center, null, voidGreen * 0.42f, Projectile.rotation, bloom.Size() * 0.5f, 0.22f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, center, null, (Color.White with { A = 0 }) * 0.22f, Projectile.rotation, bloom.Size() * 0.5f, 0.08f, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
