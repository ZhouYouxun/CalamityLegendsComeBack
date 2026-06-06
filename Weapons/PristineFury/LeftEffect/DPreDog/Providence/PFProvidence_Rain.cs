using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFProvidence_RainSpawner
    {
        internal static void Spawn(Vector2 center, Projectile source, int damage)
        {
            bool spawnHolyOrbs = !Main.dayTime || Main.rand.NextBool();
            bool spawnMoltenBlobs = !Main.dayTime || !spawnHolyOrbs;

            if (spawnHolyOrbs)
            {
                for (int i = 0; i < 22; i++)
                {
                    Vector2 spawnPosition = center + new Vector2(Main.rand.NextFloat(-430f, 430f), Main.rand.NextFloat(-620f, -470f));
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-1.8f, 1.8f), Main.rand.NextFloat(8.6f, 12.8f)) * 2f;
                    int orb = Projectile.NewProjectile(source.GetSource_FromThis(), spawnPosition, velocity, ModContent.ProjectileType<PFProvidence_HolyRainOrb>(), Math.Max(1, (int)(damage * 0.38f)), source.knockBack * 0.2f, source.owner);
                    PFLeftEffectRules.ApplyTheme(orb, (PristineFuryMark)(int)source.ai[2]);
                }
            }

            if (spawnMoltenBlobs)
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector2 spawnPosition = center + new Vector2(Main.rand.NextFloat(-360f, 360f), Main.rand.NextFloat(-700f, -520f));
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-2.2f, 2.2f), Main.rand.NextFloat(10.4f, 15.2f)) * 2f;
                    int blob = Projectile.NewProjectile(source.GetSource_FromThis(), spawnPosition, velocity, ModContent.ProjectileType<PFProvidence_MoltenRainBlob>(), Math.Max(1, (int)(damage * 0.72f)), source.knockBack * 0.45f, source.owner);
                    PFLeftEffectRules.ApplyTheme(blob, (PristineFuryMark)(int)source.ai[2]);
                }
            }
        }
    }

    internal sealed class PFProvidence_HolyRainOrb : ModProjectile, ILocalizedModType
    {
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/StarProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 210;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            NPC target = FindTarget(1100f);
            float speed = MathHelper.Clamp(Projectile.velocity.Length() * 1.012f + 0.16f, 18f, 40f);
            if (target != null)
            {
                Vector2 predictedTarget = target.Center + target.velocity * MathHelper.Clamp(Projectile.Distance(target.Center) / Math.Max(speed, 1f), 4f, 14f);
                float lockStrength = Utils.GetLerpValue(210f, 150f, Projectile.timeLeft, true);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(predictedTarget) * speed, 0.12f + lockStrength * 0.16f);
            }
            else
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * speed;

            Projectile.rotation += 0.14f;
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 0.66f);
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(Projectile.Center, -Projectile.velocity * 0.08f, false, 7, 0.68f, ThemeColor, true, false, true));
                if (Main.rand.NextBool(2))
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.1f), false, Main.rand.Next(8, 14), Main.rand.NextFloat(0.45f, 0.8f), Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.12f, 0.45f))));
            }
        }

        private NPC FindTarget(float range)
        {
            NPC closest = null;
            float bestDistance = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                closest = npc;
            }

            return closest;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) =>
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 240);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color glow = ThemeColor with { A = 0 };

            for (int i = 0; i < 4; i++)
            {
                float rotation = Projectile.rotation + MathHelper.PiOver2 * i + Main.GlobalTimeWrappedHourly * 1.6f;
                Main.EntitySpriteDraw(texture, drawPosition, null, glow * 0.34f, rotation, origin, Projectile.scale * (1.1f + i * 0.08f), SpriteEffects.None, 0f);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Color.Lerp(lightColor, ThemeColor, 0.45f), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    internal sealed class PFProvidence_MoltenRainBlob : ModProjectile, ILocalizedModType
    {
        private bool exploding;
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Boss/MoltenBlob";

        public override void SetStaticDefaults() => Main.projFrames[Type] = 3;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.scale = 1.8f;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 260;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Projectile.velocity.Y = Math.Min(24f, Projectile.velocity.Y + 0.34f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
                Projectile.frameCounter = 0;
            }

            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 0.72f);
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(Projectile.Center, -Projectile.velocity * 0.1f, Color.Lerp(ThemeColor, Color.DarkGoldenrod, 0.42f), 18, 0.72f, 0.58f, 0.02f, glowing: true));

                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    -Projectile.velocity * Main.rand.NextFloat(0.035f, 0.08f) + Main.rand.NextVector2Circular(0.35f, 0.35f),
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.32f, 0.58f),
                    Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.1f, 0.38f)),
                    true,
                    false,
                    true));

                if (Main.rand.NextBool(2))
                {
                    GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                        -Projectile.velocity * Main.rand.NextFloat(0.025f, 0.07f),
                        Color.Lerp(ThemeColor, Color.Goldenrod, Main.rand.NextFloat(0.16f, 0.4f)),
                        Color.Black,
                        Main.rand.NextFloat(0.34f, 0.68f),
                        Main.rand.Next(16, 28),
                        Main.rand.NextFloat(-0.035f, 0.035f)));
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => true;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 300);
            Explode(0.62f);
        }

        public override void OnKill(int timeLeft)
        {
            Explode(1f);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.48f, Pitch = -0.12f }, Projectile.Center);
        }

        private void Explode(float scale)
        {
            if (exploding)
                return;

            exploding = true;
            if (Projectile.owner == Main.myPlayer)
            {
                int oldWidth = Projectile.width;
                int oldHeight = Projectile.height;
                Vector2 center = Projectile.Center;
                Projectile.width = Projectile.height = (int)(126f * scale);
                Projectile.Center = center;
                Projectile.Damage();
                Projectile.width = oldWidth;
                Projectile.height = oldHeight;
                Projectile.Center = center;
            }
            exploding = false;

            if (!Main.dedServ)
                PFProvidence_NukeOfBliss.SpawnExplosionEffects(Projectile.Center, 0.48f * scale);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            Rectangle frame = new(0, frameHeight * Projectile.frame, texture.Width, frameHeight);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, ThemeColor, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
