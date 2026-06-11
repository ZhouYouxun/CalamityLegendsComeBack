using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    public class PlagueCell_Marked : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;

            Projectile.timeLeft = 35;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;

            Projectile.friendly = false;
            Projectile.hostile = false;
        }

        public override void AI()
        {
            int targetIndex = (int)Projectile.ai[0];

            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
                return;

            NPC target = Main.npc[targetIndex];

            if (!target.active)
                return;

            // 锁定目标
            Projectile.Center = target.Center;

            // ===== 出现时：音效 + 向上喷射红色粒子 =====
            if (Projectile.timeLeft == 5)
            {
                // 嘟一声
                SoundStyle fullCharge = new("CalamityMod/Sounds/Custom/PlagueSounds/PBGAttackSwitchShort");
                SoundEngine.PlaySound(fullCharge with { Volume = 0.9f }, Projectile.Center);

                Vector2 upward = -Vector2.UnitY;

                // 模仿火箭口：单方向喷射
                for (int i = 0; i < 12; i++)
                {
                    Vector2 velocity =
                        upward * Main.rand.NextFloat(4f, 10f) +
                        Main.rand.NextVector2Circular(0.6f, 0.6f);

                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center,
                        DustID.GemRuby,
                        velocity
                    );

                    dust.scale = Main.rand.NextFloat(0.45f, 0.75f);
                    dust.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D reticle = ModContent.Request<Texture2D>("CalamityMod/Particles/DestroyerReticleTelegraph").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = reticle.Size() * 0.5f;
            float progress = Utils.GetLerpValue(35f, 5f, Projectile.timeLeft, true);
            float pulse = 0.88f + 0.12f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 11f + Projectile.identity);
            Color green = new Color(74, 255, 92);
            Color paleGreen = new Color(210, 255, 218);
            float outerScale = MathHelper.Lerp(0.22f, 0.34f, progress) * pulse;
            float innerScale = MathHelper.Lerp(0.18f, 0.27f, progress) * pulse;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                reticle,
                drawPosition,
                null,
                Color.Lerp(green, paleGreen, progress * 0.35f) * 0.92f,
                Main.GlobalTimeWrappedHourly * 1.5f,
                origin,
                outerScale,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                reticle,
                drawPosition,
                null,
                paleGreen * 0.72f,
                -Main.GlobalTimeWrappedHourly * 1.1f,
                origin,
                innerScale,
                SpriteEffects.FlipHorizontally,
                0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            int targetIndex = (int)Projectile.ai[0];

            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
                return;

            NPC target = Main.npc[targetIndex];

            if (!target.active)
                return;

            // ===== 起始位置：上方 + 左右随机偏移 =====
            Vector2 spawnPos = target.Center
                               + new Vector2(Main.rand.NextFloat(-16f, 16f) * 16f, -36f * 16f);

            // ===== 精准指向目标（不是垂直）=====
            Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 30f;

            int projID = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPos,
                velocity,
                ModContent.ProjectileType<PlagueCell_Nuke>(),
                (int)(Projectile.damage * 4),
                0f,
                Projectile.owner,
                target.whoAmI
            );

            if (Main.projectile.IndexInRange(projID))
            {
                Projectile missile = Main.projectile[projID];
                missile.friendly = true;
                missile.hostile = false;
                missile.DamageType = DamageClass.Magic;
                missile.tileCollide = false;
                missile.ignoreWater = true;
                missile.usesLocalNPCImmunity = true;
                missile.localNPCHitCooldown = 10;
            }
        }




    }

    public class PlagueCell_Nuke : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/Ranged/HiveNuke";

        private bool exploded;

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 170;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity *= 1.012f;

            int targetIndex = (int)Projectile.ai[0];
            if (Main.npc.IndexInRange(targetIndex) && Main.npc[targetIndex].active && Projectile.Distance(Main.npc[targetIndex].Center) < 28f)
                Projectile.Kill();

            if (Main.rand.NextBool(2))
            {
                Color smokeColor = Color.Lerp(Color.Black, Color.Lime, 0.25f);
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center - Projectile.velocity * 2f,
                    -Projectile.velocity * Main.rand.NextFloat(0.12f, 0.38f),
                    smokeColor * 0.58f,
                    11,
                    Main.rand.NextFloat(0.36f, 0.58f),
                    0.21f,
                    Main.rand.NextFloat(-0.2f, 0.2f),
                    false));
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.18f, 0.7f, 0.08f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Plague>(), 120);
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (exploded)
                return;

            exploded = true;
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/TheHiveNuke") { Volume = 0.86f }, Projectile.Center);
            SpawnNukeExplosionEffects();

            int oldWidth = Projectile.width;
            int oldHeight = Projectile.height;
            Projectile.Resize(480, 480);
            Projectile.penetrate = -1;
            Projectile.Damage();
            Projectile.Resize(oldWidth, oldHeight);

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<PlagueCell_Fog>(),
                    System.Math.Max(1, Projectile.damage / 6),
                    0f,
                    Projectile.owner);

                int beeCount = (Main.player[Projectile.owner].strongBees ? 12 : 10) + 3;
                for (int i = 0; i < beeCount; i++)
                {
                    float delayFactor = Main.rand.NextFloat(0.7f, 1.4f);
                    float initialHomingCounter = 30f - 30f * delayFactor;
                    Vector2 velocity = (MathHelper.TwoPi * i / beeCount + Main.rand.NextFloat(-0.14f, 0.14f)).ToRotationVector2() * Main.rand.NextFloat(3.5f, 8f);
                    int bee = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        velocity,
                        ModContent.ProjectileType<BasicPlagueBee>(),
                        System.Math.Max(1, (int)(Projectile.damage * 0.16f)),
                        0f,
                        Projectile.owner,
                        initialHomingCounter,
                        120f,
                        1.5f);

                    if (Main.projectile.IndexInRange(bee))
                    {
                        Projectile plagueBee = Main.projectile[bee];
                        plagueBee.DamageType = DamageClass.Magic;
                        plagueBee.penetrate = 1;
                        plagueBee.scale *= 1.35f;
                        plagueBee.light = MathHelper.Max(plagueBee.light, 0.35f);
                    }
                }
            }

            for (int i = 0; i < 34; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 16f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? DustID.GemEmerald : DustID.SteampunkSteam, velocity, 0, Color.LimeGreen, Main.rand.NextFloat(1f, 1.8f));
                dust.noGravity = true;
                dust.alpha = Main.rand.Next(70, 190);
            }
        }

        private void SpawnNukeExplosionEffects()
        {
            Vector2 center = Projectile.Center;
            Color plagueGreen = new(74, 255, 92);
            Color deepGreen = new(12, 92, 24);
            Color toxicYellow = new(190, 255, 70);

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                plagueGreen,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.18f,
                2.4f,
                24));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Vector2.Zero,
                plagueGreen,
                Vector2.One,
                0f,
                0.18f,
                5.2f,
                28));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Vector2.Zero,
                toxicYellow,
                new Vector2(1.15f, 1.15f),
                0f,
                0.1f,
                3.6f,
                22));

            for (int i = 0; i < 46; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 velocity = direction * Main.rand.NextFloat(3.5f, 18f);

                Dust dust = Dust.NewDustPerfect(
                    center + direction * Main.rand.NextFloat(6f, 28f),
                    Main.rand.NextBool(3) ? DustID.GemEmerald : DustID.GreenTorch,
                    velocity,
                    Main.rand.Next(40, 130),
                    Main.rand.NextBool(4) ? toxicYellow : plagueGreen,
                    Main.rand.NextFloat(1.15f, 2.25f));
                dust.noGravity = true;
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 8.4f);
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    center + Main.rand.NextVector2Circular(22f, 22f),
                    velocity,
                    Color.Lerp(deepGreen, Color.Black, Main.rand.NextFloat(0.25f, 0.55f)) * 0.82f,
                    Main.rand.Next(30, 50),
                    Main.rand.NextFloat(0.72f, 1.45f),
                    0.42f,
                    Main.rand.NextFloat(-0.08f, 0.08f),
                    false));
            }

            for (int i = 0; i < 14; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(7f, 19f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    center,
                    velocity,
                    false,
                    Main.rand.Next(16, 26),
                    Main.rand.NextFloat(1.1f, 1.9f),
                    Main.rand.NextBool(3) ? toxicYellow : plagueGreen));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }

    public class PlagueCell_Fog : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/Summon/SmallAresArms/MinionPlasmaGas";

        private readonly float randomRotation1 = Main.rand.NextFloat(MathHelper.TwoPi);
        private readonly float randomRotation2 = Main.rand.NextFloat(MathHelper.TwoPi);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 480;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 78;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.scale = MathHelper.Lerp(0.62f, 1.34f, Utils.GetLerpValue(0f, 34f, Projectile.localAI[0], true));

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.42f, Projectile.height * 0.42f),
                    DustID.GemEmerald,
                    Main.rand.NextVector2Circular(0.9f, 0.9f),
                    120,
                    Color.LimeGreen,
                    Main.rand.NextFloat(1.1f, 2.2f));
                dust.noGravity = true;
            }
        }

        public override bool? CanDamage() => Projectile.timeLeft > 18 ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.width * Projectile.scale * 0.42f, targetHitbox);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Plague>(), 90);
            target.AddBuff(BuffID.Poisoned, 90);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 16f, Projectile.localAI[0], true);
            Vector2 scale = Projectile.Size / texture.Size() * Projectile.scale * 1.25f;
            Color drawColor = new Color(72, 160, 22, 0) * opacity;

            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, randomRotation1 + Projectile.localAI[0] * 0.006f, origin, scale, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor * 0.72f, randomRotation2 - Projectile.localAI[0] * 0.004f, origin, scale * 0.88f, SpriteEffects.FlipHorizontally);
            return false;
        }
    }
}
