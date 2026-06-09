using CalamityLegendsComeBack.Accssory.MC.PeacockScroll;
using CalamityLegendsComeBack.Accssory.MC.PrecisionEmblem;
using CalamityLegendsComeBack.Accssory.MC.MalachiteFeather;
using CalamityLegendsComeBack.Accssory.MC.GaleAce;
using CalamityLegendsComeBack.Weapons.Malachite;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Malachite.EXSkill
{
    public class MalachiteFinaleController : ModProjectile, ILocalizedModType
    {
        private const int DetonateTime = 120;
        private const int TotalTime = 170;

        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/Malachite";

        public override string LocalizationCategory => "Projectiles.Malachite";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = TotalTime;
            Projectile.penetrate = -1;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center;
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            Projectile.localAI[0]++;

            owner.itemTime = Math.Max(owner.itemTime, 2);
            owner.itemAnimation = Math.Max(owner.itemAnimation, 2);
            owner.ChangeDir(Projectile.velocity.X >= 0f ? 1 : -1);

            if (Projectile.localAI[0] < DetonateTime)
            {
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.velocity.ToRotation() - MathHelper.PiOver2);
                owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, Projectile.velocity.ToRotation() - MathHelper.PiOver2);
                SpawnChargeDust(owner);
                if (Projectile.owner == Main.myPlayer && Projectile.localAI[0] % 3f == 0f)
                {
                    SpawnVisualPetal(owner);
                }

                return;
            }

            if (Projectile.localAI[0] == DetonateTime && Projectile.owner == Main.myPlayer)
                ReleaseFinale(owner);
        }

        private void ReleaseFinale(Player owner)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                owner.Center - direction * 980f + direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-80f, 80f),
                direction * 76f,
                ModContent.ProjectileType<MalachiteFinaleSlash>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner);

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile) || Vector2.Distance(owner.Center, npc.Center) > 1600f)
                    continue;

                for (int i = 0; i < 3; i++)
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        npc.Center + Main.rand.NextVector2Circular(36f, 36f),
                        Vector2.Zero,
                        ModContent.ProjectileType<MalachiteGreenExplosion>(),
                        Math.Max(1, (int)(Projectile.damage * 0.86f)),
                        0f,
                        Projectile.owner,
                        1f);
                }
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1.15f, Pitch = -0.25f }, owner.Center);
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.95f, Pitch = -0.45f }, owner.Center);
            ApplyScreenShake(owner.Center, 12f);
            owner.SetImmuneTimeForAllTypes(50);
        }

        private void SpawnVisualPetal(Player owner)
        {
            Vector2 screenSizedOffset = new(
                Main.rand.NextFloat(-Main.screenWidth * 0.55f, Main.screenWidth * 0.55f),
                -Main.screenHeight * 0.55f - Main.rand.NextFloat(30f, 160f));
            Vector2 velocity = new(Main.rand.NextFloat(-1.8f, 1.8f), Main.rand.NextFloat(1.9f, 4.2f));

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                owner.Center + screenSizedOffset,
                velocity,
                ModContent.ProjectileType<MalachiteFinalePetal>(),
                0,
                0f,
                Projectile.owner,
                Main.rand.NextFloatDirection(),
                Main.rand.Next(3));
        }

        private static void ApplyScreenShake(Vector2 center, float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1600f, 120f, Vector2.Distance(Main.LocalPlayer.Center, center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }

        private void SpawnChargeDust(Player owner)
        {
            float charge = Utils.GetLerpValue(0f, DetonateTime, Projectile.localAI[0], true);

            if (Projectile.localAI[0] > 10f && Projectile.localAI[0] < DetonateTime)
            {
                Vector2 center = owner.Center;
                Color pulseColor = Color.Lerp(new Color(25, 25, 25, 0), new Color(115, 255, 150, 0), charge);

                if (Projectile.localAI[0] % 10f == 0f)
                {
                    Particle pulse = new CustomPulse(
                        center,
                        Vector2.Zero,
                        pulseColor,
                        "CalamityMod/Particles/SoftRoundExplosion",
                        new Vector2(1.5f, 1f),
                        Main.rand.NextBool() ? 0f : MathHelper.Pi,
                        charge * 0.5f,
                        charge * 0.1f,
                        20,
                        true);
                    GeneralParticleHandler.SpawnParticle(pulse);
                }

                Vector2 sparkPosition = center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(80f, 300f) * charge * 1.6f * new Vector2(1.5f, 1f);
                Vector2 sparkVelocity = (center - sparkPosition).SafeNormalize(Vector2.Zero) * (Vector2.Distance(sparkPosition, center) / 10f);
                Particle spark = new SparkParticle(
                    sparkPosition,
                    sparkVelocity,
                    affectedByGravity: false,
                    10,
                    Main.rand.NextFloat(0.2f, 0.5f) * charge * 2f,
                    new Color(115, 255, 150));
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 spawnPosition = owner.Center + Main.rand.NextVector2Circular(180f, 130f);
                Vector2 velocity = (owner.Center - spawnPosition).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.8f, 4.2f);
                Dust dust = Dust.NewDustPerfect(spawnPosition, DustID.Terra, velocity, 80, new Color(120, 255, 135), Main.rand.NextFloat(0.75f, 1.3f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            float timer = Projectile.localAI[0];
            float charge = Utils.GetLerpValue(0f, DetonateTime, timer, true);
            float flash = timer >= DetonateTime ? Utils.GetLerpValue(TotalTime, DetonateTime, timer, true) : charge;
            Vector2 playerScreen = Projectile.Center - Main.screenPosition;
            Player owner = Main.player[Projectile.owner];
            bool galeAce = owner.active && owner.GetModPlayer<GaleAcePlayer>().GaleAceEquipped;

            DrawSpotlight(texture, origin, playerScreen, charge, flash);
            DrawPetals(texture, origin, charge, galeAce, owner.direction);
            return false;
        }

        private static void DrawSpotlight(Texture2D texture, Vector2 origin, Vector2 playerScreen, float charge, float flash)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Projectiles/StarProj").Value;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            Vector2 starOrigin = star.Size() * 0.5f;
            float height = Main.screenHeight + 220f;
            float width = MathHelper.Lerp(180f, 520f, charge);
            Vector2 start = new(playerScreen.X, -100f);
            Vector2 center = (start + playerScreen) * 0.5f;
            float rotation = (playerScreen - start).ToRotation() + MathHelper.PiOver2;
            Vector2 scale = new(width / bloom.Width, height / bloom.Height);

            DrawSoftCone(start, playerScreen, charge, flash);

            for (int i = 0; i < 3; i++)
            {
                float wave = MathF.Sin(Main.GlobalTimeWrappedHourly * 2.8f + i) * 18f;
                Color color = Color.Lerp(new Color(20, 95, 45, 0), new Color(145, 255, 155, 0), charge);
                Main.EntitySpriteDraw(
                    bloom,
                    center + Vector2.UnitX * wave,
                    null,
                    color * (0.065f + flash * 0.09f),
                    rotation,
                    bloomOrigin,
                    scale * (0.68f + i * 0.12f),
                    SpriteEffects.None);
            }

            float aura = MathF.Sin(MathHelper.Pi * charge);
            for (int i = 0; i < 3; i++)
            {
                float pulse = charge * 4.4f + MathF.Cos(Main.GlobalTimeWrappedHourly * 2f + i) * charge * 0.22f;
                Color ringColor = new Color(75, 255, 135, 0) * (0.10f + flash * 0.12f);
                Main.EntitySpriteDraw(
                    bloom,
                    playerScreen + (Main.GlobalTimeWrappedHourly * (0.8f + i * 0.17f)).ToRotationVector2() * (i * 5f + aura * 8f),
                    null,
                    ringColor,
                    0f,
                    bloomOrigin,
                    pulse + i * 0.46f,
                    SpriteEffects.None);
            }

            Color starColor = new Color(105, 255, 150, 0) * (0.18f + flash * 0.18f);
            float starPulse = MathHelper.Lerp(0.2f, 1.25f, charge) * (1f + MathF.Sin(Main.GlobalTimeWrappedHourly * MathHelper.TwoPi) * 0.08f);
            Vector2 starScale = new(1.5f + charge * 1.3f, 2.5f + charge * 1.7f);
            Main.EntitySpriteDraw(star, playerScreen, null, starColor, MathHelper.PiOver4, starOrigin, starScale * starPulse, SpriteEffects.None);
            Main.EntitySpriteDraw(star, playerScreen, null, starColor * 0.65f, -MathHelper.PiOver4, starOrigin, starScale * starPulse * 0.68f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, playerScreen, null, new Color(150, 255, 160, 0) * flash * 0.32f, Main.GlobalTimeWrappedHourly * 0.5f, origin, 1.8f + charge * 2.1f, SpriteEffects.None);
        }

        private static void DrawSoftCone(Vector2 top, Vector2 bottom, float charge, float flash)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            int layers = 5;
            float topWidth = MathHelper.Lerp(76f, 128f, charge);
            float bottomWidth = MathHelper.Lerp(260f, 520f, charge);

            for (int i = 0; i < layers; i++)
            {
                float progress = i / (float)(layers - 1);
                Vector2 position = Vector2.Lerp(top, bottom, progress);
                float softness = MathF.Sin(progress * MathHelper.Pi);
                float width = MathHelper.Lerp(topWidth, bottomWidth, progress) * (0.88f + softness * 0.12f);
                float height = MathHelper.Lerp(180f, 310f, progress);
                Color color = Color.Lerp(new Color(40, 125, 65, 0), new Color(150, 255, 165, 0), charge);
                color *= (0.035f + softness * 0.055f + flash * 0.04f);

                Main.EntitySpriteDraw(
                    bloom,
                    position,
                    null,
                    color,
                    0f,
                    bloomOrigin,
                    new Vector2(width / bloom.Width, height / bloom.Height),
                    SpriteEffects.None);
            }
        }

        private static void DrawPetals(Texture2D texture, Vector2 origin, float charge, bool winded, int direction)
        {
            Texture2D petal1 = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/Malachite/EXSkill/MalachiteSPIT").Value;
            Texture2D petal2 = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/Malachite/EXSkill/MalachiteSPIT2").Value;
            Texture2D petal3 = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/Malachite/EXSkill/MalachiteSPIT3").Value;
            int petalCount = 38;
            for (int i = 0; i < petalCount; i++)
            {
                Texture2D petalTexture = i % 3 == 0 ? petal1 : i % 3 == 1 ? petal2 : petal3;
                Vector2 petalOrigin = petalTexture.Size() * 0.5f;
                float seed = i * 37.719f;
                float fall = (Main.GlobalTimeWrappedHourly * 74f + seed) % (Main.screenHeight + 160f) - 80f;
                float x = (seed * 19f + MathF.Sin(Main.GlobalTimeWrappedHourly * 1.7f + i) * 42f) % (Main.screenWidth + 120f) - 60f;
                if (winded)
                    x = (x + fall * 0.33f * direction + Main.screenWidth + 120f) % (Main.screenWidth + 120f) - 60f;

                float rotation = Main.GlobalTimeWrappedHourly * (0.7f + i % 5 * 0.11f) + i;
                float scale = 0.13f + (i % 7) * 0.012f;
                Color color = Color.Lerp(new Color(255, 188, 220, 0), new Color(170, 255, 150, 0), i % 3 / 2f);

                Main.EntitySpriteDraw(
                    petalTexture,
                    new Vector2(x, fall),
                    null,
                    color * (0.35f + charge * 0.35f),
                    rotation,
                    petalOrigin,
                    scale,
                    SpriteEffects.None);
            }
        }
    }

    public class MalachiteFinaleSlash : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/Malachite";

        public override string LocalizationCategory => "Projectiles.Malachite";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 76;
            Projectile.height = 76;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 34;
            Projectile.extraUpdates = 2;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override void AI()
        {
            Timer++;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.rotation = direction.ToRotation();
            Projectile.velocity *= 0.996f;
            Lighting.AddLight(Projectile.Center, 0.2f, 0.8f, 0.25f);

            if (Timer == 1f)
                SoundEngine.PlaySound(SoundID.Item60 with { Volume = 0.72f, Pitch = 0.25f }, Projectile.Center);

            if (!Main.dedServ && Main.rand.NextBool())
            {
                Vector2 side = direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-32f, 32f);
                Particle line = new LineParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(16f, 54f) + side,
                    -direction * Main.rand.NextFloat(4f, 10f),
                    false,
                    Main.rand.Next(10, 17),
                    Main.rand.NextFloat(0.55f, 0.95f),
                    Main.rand.NextBool() ? new Color(120, 255, 145) : Color.White);
                GeneralParticleHandler.SpawnParticle(line);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 10 * 60);
            if (Projectile.owner != Main.myPlayer)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                direction,
                ModContent.ProjectileType<MalachiteFinaleHitSlash>(),
                Math.Max(1, (int)(Projectile.damage * 0.48f)),
                0f,
                Projectile.owner,
                direction.ToRotation(),
                Main.rand.NextFloat(-0.35f, 0.35f));

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center + Main.rand.NextVector2Circular(24f, 24f),
                Vector2.Zero,
                ModContent.ProjectileType<MalachiteGreenExplosion>(),
                Math.Max(1, (int)(Projectile.damage * 0.58f)),
                0f,
                Projectile.owner,
                1f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 start = Projectile.Center - direction * 90f;
            Vector2 end = Projectile.Center + direction * 260f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 48f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/ForwardSmear").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float fade = Utils.GetLerpValue(0f, 5f, Timer, true) * Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);
            Color green = new Color(90, 255, 125, 0);
            Color white = Color.White with { A = 0 };

            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = direction.RotatedBy(MathHelper.PiOver2) * ((i - 2) * 18f);
                Main.EntitySpriteDraw(
                    smear,
                    drawPosition + offset,
                    null,
                    Color.Lerp(green, white, i == 2 ? 0.38f : 0.08f) * fade * (0.5f - i * 0.035f),
                    direction.ToRotation() - MathHelper.PiOver2,
                    new Vector2(smear.Width * 0.5f, smear.Height),
                    new Vector2(0.052f, 1.75f + i * 0.08f),
                    SpriteEffects.None);
            }

            Main.EntitySpriteDraw(bloom, drawPosition, null, green * 0.28f * fade, 0f, bloom.Size() * 0.5f, new Vector2(1.4f, 0.22f), SpriteEffects.None);

            return false;
        }
    }

    public class MalachiteFinaleHitSlash : ModProjectile, ILocalizedModType
    {
        public override string Texture => "Terraria/Images/Extra_98";

        public override string LocalizationCategory => "Projectiles.Malachite";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 24;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Timer++;
            Projectile.velocity *= 0f;
            Projectile.rotation = Projectile.ai[0] + Projectile.ai[1];
            Projectile.Opacity = Utils.GetLerpValue(24f, 4f, Timer, true);
            if (Timer == 1f)
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.35f, Pitch = 0.45f, MaxInstances = 6 }, Projectile.Center);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = Projectile.rotation.ToRotationVector2();
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - direction * 120f, Projectile.Center + direction * 120f, 36f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Extra[ExtrasID.SharpTears].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = new Color(100, 255, 130, 0) * Projectile.Opacity;

            Main.EntitySpriteDraw(texture, drawPosition, null, color, Projectile.rotation, origin, new Vector2(4.1f, 0.55f), SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White * 0.42f * Projectile.Opacity, Projectile.rotation + MathHelper.PiOver2, origin, new Vector2(1.3f, 0.16f), SpriteEffects.None);
            return false;
        }
    }

    public class MalachiteFinalePetal : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/EXSkill/MalachiteSPIT";

        public override string LocalizationCategory => "Projectiles.Malachite";

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 140;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            float direction = Projectile.ai[0] == 0f ? Math.Sign(Projectile.velocity.X) : Projectile.ai[0];
            if (direction == 0f)
                direction = 1f;

            Projectile.velocity.X += direction * 0.01f + MathF.Sin(Projectile.localAI[0] * 0.06f + Projectile.identity) * 0.012f;
            Projectile.velocity.Y += 0.025f;
            if (Projectile.velocity.Length() > 8f)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * 8f;

            Projectile.rotation += direction * 0.08f + Projectile.velocity.X * 0.012f;
            Projectile.alpha = (int)MathHelper.Lerp(0f, 230f, Utils.GetLerpValue(100f, 140f, Projectile.localAI[0], true));
            Lighting.AddLight(Projectile.Center, 0.08f, 0.23f, 0.08f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = GetPetalTexture();
            Vector2 origin = texture.Size() * 0.5f;
            Color color = Color.Lerp(new Color(255, 206, 226, 0), new Color(170, 255, 150, 0), 0.45f) * Projectile.Opacity;
            Vector2 scale = Vector2.One * Projectile.scale * 0.72f;

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                color,
                Projectile.rotation,
                origin,
                scale,
                SpriteEffects.None);

            return false;
        }

        private Texture2D GetPetalTexture()
        {
            int variant = Utils.Clamp((int)Projectile.ai[1], 0, 2);
            string suffix = variant == 0 ? string.Empty : (variant + 1).ToString();
            return ModContent.Request<Texture2D>($"CalamityLegendsComeBack/Weapons/Malachite/EXSkill/MalachiteSPIT{suffix}").Value;
        }
    }
}
