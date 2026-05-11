using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    internal sealed class PristineFuryBreath : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Particles/MediumMist";
        private int Style => (int)Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 2;
            Projectile.MaxUpdates = 3;
            Projectile.timeLeft = 54;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            float completion = 1f - Projectile.timeLeft / 54f;
            Color color = GetStyleColor(Style, completion);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);

            Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.55f);

            if (Style is 1 or 2)
            {
                float phase = Main.GlobalTimeWrappedHourly * 18f + Projectile.identity * 0.2f + (Style == 1 ? 0f : MathHelper.Pi);
                Vector2 helix = side * (float)Math.Sin(phase) * MathHelper.Lerp(4f, 18f, completion);
                SpawnBreathParticle(Projectile.Center + helix, color, direction);
            }
            else
            {
                SpawnBreathParticle(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), color, direction);
            }

            if (Style >= 5 && Main.rand.NextBool(10))
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    direction.RotatedByRandom(0.9f) * Main.rand.NextFloat(2.5f, 5.5f),
                    ModContent.ProjectileType<PristineFuryCelestialShard>(),
                    Math.Max(1, Projectile.damage / 2),
                    Projectile.knockBack * 0.5f,
                    Projectile.owner,
                    Style - 5);
            }
        }

        private static void SpawnBreathParticle(Vector2 position, Color color, Vector2 direction)
        {
            if (Main.dedServ)
                return;

            if (Main.rand.NextBool(2))
            {
                Particle smoke = new HeavySmokeParticle(position, direction * 0.8f + Main.rand.NextVector2Circular(0.4f, 0.4f), color, 14, Main.rand.NextFloat(0.45f, 0.9f), 0.42f, Main.rand.NextFloat(-0.08f, 0.08f), false);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            Dust dust = Dust.NewDustPerfect(position, DustID.Torch, direction.RotatedByRandom(0.7f) * Main.rand.NextFloat(0.4f, 2f), 100, color, Main.rand.NextFloat(0.8f, 1.25f));
            dust.noGravity = true;
        }

        private static Color GetStyleColor(int style, float completion)
        {
            return style switch
            {
                1 => Color.Lerp(new Color(200, 34, 70), new Color(255, 92, 145), completion),
                2 => Color.Lerp(new Color(112, 32, 210), new Color(238, 84, 255), completion),
                3 => Color.Lerp(new Color(255, 188, 54), new Color(255, 240, 140), completion),
                4 => Color.Lerp(new Color(255, 154, 92), new Color(118, 160, 255), 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f)),
                5 => new Color(255, 96, 42),
                6 => new Color(104, 74, 255),
                7 => new Color(68, 186, 255),
                8 => new Color(170, 222, 255),
                _ => Color.Lerp(Color.OrangeRed, Color.Goldenrod, completion)
            };
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 180);
            if (Style == 4)
                target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 180);
        }

        public override void OnKill(int timeLeft)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<PristineFuryImpactExplosion>(), Math.Max(1, Projectile.damage / 2), 0f, Projectile.owner, 45f);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    internal sealed class PristineFuryOverloadedBolt : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/ExtraTextures/TinyGreyscaleCircle";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 260;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void AI()
        {
            Projectile.rotation += 0.25f * Projectile.direction;
            Lighting.AddLight(Projectile.Center, new Vector3(0.42f, 0.14f, 0.72f));

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.2f + Main.rand.NextVector2Circular(1.2f, 1.2f), 80, new Color(190, 86, 255), 1.2f);
                dust.noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X;
            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y;

            Projectile.ai[0]++;
            SpawnBounceExplosion();
            if (Projectile.ai[0] >= 5f)
                Projectile.Kill();

            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 normal = (Projectile.Center - target.Center).SafeNormalize(-Projectile.velocity.SafeNormalize(Vector2.UnitX));
            Projectile.velocity = Vector2.Reflect(Projectile.velocity, normal);
            Projectile.ai[0]++;
            SpawnBounceExplosion();
        }

        private void SpawnBounceExplosion()
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<PristineFuryImpactExplosion>(), Math.Max(1, Projectile.damage / 2), 0f, Projectile.owner, 75f);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.45f, Pitch = 0.5f }, Projectile.Center);
        }
    }

    internal sealed class PristineFuryPressureWave : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 150;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 70;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool ShouldUpdatePosition() => Projectile.ai[0] <= 0f;

        public override void AI()
        {
            if (Projectile.ai[0] > 0f)
            {
                Projectile.ai[0]--;
                Projectile.alpha = 255;
                return;
            }

            Projectile.alpha = 0;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.scale = Projectile.ai[1] <= 0f ? 1f : Projectile.ai[1];
            Projectile.velocity *= 1.015f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.85f, 0.54f, 0.16f));

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.height) * 0.35f, DustID.GoldFlame, -Projectile.velocity * 0.1f, 80, Color.Orange, 1.3f);
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<PristineFuryImpactExplosion>(), Math.Max(1, Projectile.damage / 3), 0f, Projectile.owner, 55f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.ai[0] > 0f)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = new Color(255, 198, 72, 0) * 0.48f;
            Main.EntitySpriteDraw(bloom, drawPosition, null, color, Projectile.rotation, bloom.Size() * 0.5f, new Vector2(0.22f, 0.95f) * Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }

    internal sealed class PristineFuryBrimstoneBeam : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 56;
            Projectile.height = 56;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 44;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 7;
        }

        public override void AI()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.rotation = direction.ToRotation();
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.12f, 0.08f));

            if (Projectile.timeLeft % 4 == 0)
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    Vector2 spawn = Projectile.Center + direction.RotatedBy(MathHelper.PiOver2) * i * 20f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, direction * 11f, ModContent.ProjectileType<PristineFuryBrimstoneShard>(), Math.Max(1, Projectile.damage / 3), Projectile.knockBack, Projectile.owner);
                }
            }

            Projectile.Center += direction * 18f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition - direction * 170f;
            Main.EntitySpriteDraw(line, drawPosition, null, new Color(255, 44, 54, 0) * 0.85f, direction.ToRotation() + MathHelper.PiOver2, line.Size() * 0.5f, new Vector2(0.075f, 4.4f), SpriteEffects.None, 0);
            return false;
        }
    }

    internal sealed class PristineFuryBrimstoneShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (Projectile.timeLeft < 115)
            {
                NPC target = PristineFuryTargeting.FindTarget(Projectile.Center, 760f, Main.player[Projectile.owner]);
                if (target != null)
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 13f, 0.08f);
            }

                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * 0.15f, 80, Color.Red, 1.1f);
            dust.noGravity = true;
        }
    }

    internal sealed class PristineFuryStickySpore : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityLegendsComeBack/Weapons/PristineFury/LeftEffect/BPrePlantera/Plantera/=";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override void AI()
        {
            int targetIndex = (int)Projectile.ai[0] - 1;
            if (targetIndex >= 0 && targetIndex < Main.maxNPCs && Main.npc[targetIndex].active)
            {
                NPC target = Main.npc[targetIndex];
                Projectile.Center = target.Center + new Vector2(Projectile.localAI[0], Projectile.localAI[1]);
                Projectile.velocity = Vector2.Zero;
                Projectile.tileCollide = false;
            }
            else if (Projectile.ai[0] > 0f)
            {
                Projectile.Kill();
            }

            Projectile.rotation += 0.08f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.2f, 0.85f, 0.24f));
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            int stuckCount = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.type == Type && (int)projectile.ai[0] - 1 == target.whoAmI)
                    stuckCount++;
            }

            modifiers.SourceDamage *= 1f + stuckCount * 0.25f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.ai[0] <= 0f)
            {
                Projectile.ai[0] = target.whoAmI + 1;
                Vector2 offset = Projectile.Center - target.Center;
                Projectile.localAI[0] = offset.X;
                Projectile.localAI[1] = offset.Y;
                Projectile.timeLeft = 150;
                Projectile.netUpdate = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            Player owner = Main.player[Projectile.owner];
            if (owner.active && !owner.dead && Projectile.ai[0] > 0f)
                owner.statLife = Math.Min(owner.statLifeMax2, owner.statLife + 5);
        }
    }

    internal sealed class PristineFuryPlagueSmoke : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Particles/MediumMist";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 54;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 80;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            Projectile.rotation += 0.04f;
            if (Projectile.ai[0] > 22f)
                Projectile.velocity *= 0.94f;

            Lighting.AddLight(Projectile.Center, new Vector3(0.32f, 0.72f, 0.06f));
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(Projectile.Center + Main.rand.NextVector2Circular(15f, 15f), Projectile.velocity * 0.25f, new Color(92, 168, 48), 20, Main.rand.NextFloat(0.7f, 1.2f), 0.55f, Main.rand.NextFloat(-0.08f, 0.08f), false));
            }

            if (Projectile.ai[0] == 44f)
            {
                for (int i = 0; i < 10; i++)
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 5f), ModContent.ProjectileType<PristineFuryPlagueBee>(), Math.Max(1, Projectile.damage / 2), Projectile.knockBack, Projectile.owner);
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    internal sealed class PristineFuryPlagueBee : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityLegendsComeBack/Weapons/PristineFury/LeftEffect/CPreMoodLord/Goliath/PlaguenadeBee";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            NPC target = PristineFuryTargeting.FindTarget(Projectile.Center, 620f, Main.player[Projectile.owner]);
            if (target != null && Projectile.ai[0] > 18f)
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 9f, 0.09f);
            else
                Projectile.velocity *= 0.96f;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<PristineFuryGlobalNPC>().PlagueRelease = Math.Max(target.GetGlobalNPC<PristineFuryGlobalNPC>().PlagueRelease, 240);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<PristineFuryImpactExplosion>(), Math.Max(1, Projectile.damage / 2), 0f, Projectile.owner, 42f);
        }
    }

    internal sealed class PristineFuryCelestialShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Particles/Sparkle";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 100;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (Projectile.timeLeft < 76)
            {
                NPC target = PristineFuryTargeting.FindTarget(Projectile.Center, 640f, Main.player[Projectile.owner]);
                if (target != null)
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 12f, 0.08f);
            }

            Projectile.rotation += 0.18f;
            Lighting.AddLight(Projectile.Center, GetCelestialColor().ToVector3() * 0.55f);
        }

        private Color GetCelestialColor()
        {
            return ((int)Projectile.ai[0]) switch
            {
                0 => Color.OrangeRed,
                1 => Color.MediumPurple,
                2 => Color.DeepSkyBlue,
                _ => Color.LightSkyBlue
            };
        }
    }

    internal sealed class PristineFuryProfanedRocket : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Ranged/BlissfulBombardierSplitProjectile";

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 38;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Vector2 targetPoint = new(Projectile.ai[0], Projectile.ai[1]);
            if (targetPoint != Vector2.Zero)
            {
                Vector2 desired = Projectile.SafeDirectionTo(targetPoint) * 14f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.035f);
            }

            Projectile.velocity.Y += 0.04f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.18f, 80, Color.Gold, 1.1f);
            dust.noGravity = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<PristineFuryGroundFlame>(), Projectile.damage, 0f, Projectile.owner, 2f);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<PristineFuryImpactExplosion>(), Projectile.damage, 0f, Projectile.owner, 160f);
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }
    }

    internal sealed class PristineFuryPhantomStar : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Particles/Sparkle";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.ai[1]++;
            if (Projectile.ai[1] < 26f)
                Projectile.velocity *= 0.94f;
            else
            {
                NPC target = PristineFuryTargeting.FindTarget(Projectile.Center, 760f, Main.player[Projectile.owner]);
                if (target != null)
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 15f, 0.12f);
            }

            Projectile.rotation += 0.16f;
            Lighting.AddLight(Projectile.Center, GetColor().ToVector3() * 0.55f);
        }

        private Color GetColor()
        {
            return ((int)Projectile.ai[0]) switch
            {
                1 => Color.HotPink,
                2 => Color.Yellow,
                3 => Color.LimeGreen,
                4 => Color.SkyBlue,
                5 => Color.Lavender,
                _ => Color.White
            };
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityMod.CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], GetColor(), 1);
            return true;
        }
    }

    internal sealed class PristineFuryVoidStream : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, new Vector3(0.22f, 0.12f, 0.62f));
            if (!Main.dedServ)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.Shadowflame, -Projectile.velocity * 0.12f, 80, new Color(92, 74, 225), 1.3f);
                dust.noGravity = true;
            }
        }
    }

    internal sealed class PristineFuryVoidRift : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Particles/BloomCircle";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 80;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 46;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.08f;
            if (Projectile.timeLeft == 22)
            {
                Vector2 direction = Projectile.ai[0].ToRotationVector2().RotatedByRandom(1.7f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, direction * 13f, ModContent.ProjectileType<PristineFuryVoidStream>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center - direction * 60f, Vector2.Zero, ModContent.ProjectileType<PristineFuryImpactExplosion>(), Projectile.damage, 0f, Projectile.owner, 72f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            float opacity = Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true) * Utils.GetLerpValue(46f, 28f, Projectile.timeLeft, true);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, new Color(92, 74, 225, 0) * opacity, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 0.36f, SpriteEffects.None, 0);
            return false;
        }
    }

    internal sealed class PristineFuryDragonPellet : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (Main.rand.NextBool())
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Flare, -Projectile.velocity * 0.15f, 80, Color.OrangeRed, 1.2f);
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Spark();

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Spark();
            return true;
        }

        private void Spark()
        {
            for (int i = 0; i < 9; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Flare, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 6f), 80, Color.OrangeRed, 1.2f);
                dust.noGravity = true;
            }
        }
    }

    internal sealed class PristineFuryRightPellet : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityLegendsComeBack/Weapons/PristineFury/PristineFuryRightPellet";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 15;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 110;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 2;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity *= 0.992f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.72f, 0.22f, 0.08f));

            if (Main.rand.NextBool())
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * 0.08f, 100, Color.OrangeRed, 1.1f);
                dust.noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<PristineFuryGroundFlame>(), Projectile.damage, 0f, Projectile.owner, 1f);
            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityMod.CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.OrangeRed, 1);
            return false;
        }
    }

    internal sealed class PristineFuryGroundFlame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            float scale = Projectile.ai[0] <= 0f ? 1f : Projectile.ai[0];
            Projectile.width = (int)(80f * scale);
            Projectile.height = (int)(36f * scale);
            Lighting.AddLight(Projectile.Center, new Vector3(0.9f, 0.42f, 0.05f) * scale);

            if (!Main.dedServ)
            {
                Vector2 position = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-Projectile.width * 0.45f, Projectile.width * 0.45f), Main.rand.NextFloat(-8f, 6f));
                Dust dust = Dust.NewDustPerfect(position, DustID.Torch, new Vector2(Main.rand.NextFloat(-0.7f, 0.7f), Main.rand.NextFloat(-2.6f, -0.8f)), 100, Color.OrangeRed, Main.rand.NextFloat(1f, 1.7f) * scale);
                dust.noGravity = true;
            }
        }
    }

    internal sealed class PristineFuryImpactExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] != 0f)
                return;

            Projectile.localAI[0] = 1f;
            int radius = (int)Math.Max(30f, Projectile.ai[0]);
            Projectile.Resize(radius, radius);
            Projectile.Damage();

            if (Main.dedServ)
                return;

            for (int i = 0; i < 16; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f), 100, Color.Orange, 1.2f);
                dust.noGravity = true;
            }
        }
    }

    internal sealed class PristineFuryGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        internal int PlagueRelease;

        public override void ResetEffects(NPC npc)
        {
            if (PlagueRelease > 0)
                PlagueRelease--;
        }

        public override void OnKill(NPC npc)
        {
            if (PlagueRelease <= 0)
                return;

            Player owner = Main.LocalPlayer;
            for (int i = 0; i < 5; i++)
            {
                Projectile.NewProjectile(npc.GetSource_Death(), npc.Center, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 5f), ModContent.ProjectileType<PristineFuryPlagueBee>(), 30, 0f, owner.whoAmI);
            }
        }
    }
}
