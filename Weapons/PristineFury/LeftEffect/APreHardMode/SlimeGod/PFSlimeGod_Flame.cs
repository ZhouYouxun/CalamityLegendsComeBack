using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFSlimeGod_Flame : ModProjectile, ILocalizedModType
    {
        private static readonly Color SlimeColor = new(133, 133, 224);

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/ExtraTextures/TinyGreyscaleCircle";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float BounceCount => ref Projectile.localAI[1];
        private bool Empowered => Timer >= 120f;
        private float BloomPower => Empowered ? Utils.Remap(Timer, 120f, 230f, 0.25f, 1.5f) * Utils.GetLerpValue(0f, 40f, Projectile.timeLeft, true) : 0f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 270;
            Projectile.extraUpdates = 3;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 45;
        }

        public override void AI()
        {
            Timer++;

            if (Timer == 120f)
            {
                Projectile.penetrate = 1;
                Projectile.damage = (int)(Projectile.originalDamage * 1.55f);
                Projectile.velocity *= 0.12f;
                Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                ReleaseSlimeRing(10, 3.1f);
            }
            else if (Timer > 84f)
                Projectile.velocity *= 0.972f;

            if (Empowered)
            {
                Projectile.rotation += MathHelper.ToRadians(2f);
                if (Timer > 138f)
                    HomeToCursorBiasedTarget();
            }
            else
                Projectile.rotation += Projectile.velocity.X * 0.02f;

            Lighting.AddLight(Projectile.Center, new Vector3(0.3f, 0.3f, 0.5f));
            EmitSlimeTrail();
        }

        private void HomeToCursorBiasedTarget()
        {
            NPC best = null;
            float bestScore = 620f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float score = Projectile.Distance(npc.Center) + Vector2.Distance(Main.MouseWorld, npc.Center) * 0.12f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = npc;
                }
            }

            if (best != null)
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, (best.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 7.8f, 0.045f);
        }

        private void EmitSlimeTrail()
        {
            if (Main.dedServ)
                return;

            if (Empowered)
            {
                if (Timer % 6f == 0f)
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, SlimeColor * 0.75f, Vector2.One, Projectile.rotation, 0.02f, 0.18f + BloomPower * 0.1f, 18));
                return;
            }

            if (Main.rand.NextBool(BounceCount > 0f ? 2 : 7))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextBool(3) ? 16 : 20);
                dust.scale = Main.rand.NextFloat(0.35f, 0.75f);
                dust.velocity = -Projectile.velocity * 0.7f;
                dust.noGravity = true;
            }
        }

        private void ReleaseSlimeRing(int count, float speed)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < count; i++)
            {
                float rot = MathHelper.TwoPi * i / count;
                Vector2 velocity = rot.ToRotationVector2() * speed;
                Dust dust = Dust.NewDustPerfect(Projectile.Center + velocity, Main.rand.NextBool(3) ? 59 : 20, velocity);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.2f, 1.9f);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            float bouncePower = BounceCount <= 0f ? Utils.Remap(Timer, 0f, 120f, 2.2f, 3f) : Math.Max(0.55f, 1f / BounceCount);
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X * bouncePower;
            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y * bouncePower;

            BounceCount++;
            if (BounceCount > 4f && !Empowered)
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 80);
            return false;
        }

        public override void OnKill(int timeLeft) => GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, SlimeColor, Vector2.One, 0f, 0.01f, 0.42f, 24));

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Slimed, Empowered ? 480 : 180);
            if (Empowered)
                ReleaseSlimeRing(14, 3.4f);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0 && !Empowered)
                Projectile.damage = Math.Max(1, (int)(Projectile.damage * 0.85f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Texture2D sparkle = ModContent.Request<Texture2D>("CalamityMod/Particles/Sparkle").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            PFLeftEffectRules.BeginAdditive();
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float completion = i / (float)Projectile.oldPos.Length;
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color trailColor = Color.Lerp(SlimeColor, Color.Black, completion) * (1f - completion);
                Main.EntitySpriteDraw(texture, trailPos, null, trailColor, 0f, texture.Size() * 0.5f, Projectile.scale * MathHelper.Lerp(0.16f, 1f, 1f - completion), SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, SlimeColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            if (Empowered)
            {
                Main.EntitySpriteDraw(bloom, drawPosition, null, SlimeColor * 0.5f, Projectile.rotation, bloom.Size() * 0.5f, BloomPower * Projectile.scale * 0.28f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(sparkle, drawPosition, null, SlimeColor, Projectile.rotation, sparkle.Size() * 0.5f, BloomPower * Projectile.scale, SpriteEffects.None, 0);
            }

            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
