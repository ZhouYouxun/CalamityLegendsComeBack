using CalamityLegendsComeBack.Accssory.MC;
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
                if (Projectile.owner == Main.myPlayer &&
                    owner.GetModPlayer<MalachiteAccessoryPlayer>().GaleAceEquipped &&
                    Projectile.localAI[0] % 4f == 0f)
                {
                    SpawnDamagingPetal(owner);
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
                owner.Center + direction * 110f,
                direction,
                ModContent.ProjectileType<MalachiteFinaleSlash>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner);

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile) || Vector2.Distance(owner.Center, npc.Center) > 1600f)
                    continue;

                for (int i = 0; i < 2; i++)
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        npc.Center + Main.rand.NextVector2Circular(36f, 36f),
                        Vector2.Zero,
                        ModContent.ProjectileType<MalachiteGreenExplosion>(),
                        Projectile.damage,
                        0f,
                        Projectile.owner,
                        1f);
                }
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1.15f, Pitch = -0.25f }, owner.Center);
            owner.SetImmuneTimeForAllTypes(50);
        }

        private void SpawnDamagingPetal(Player owner)
        {
            Vector2 screenSizedOffset = new(
                Main.rand.NextFloat(-Main.screenWidth * 0.55f, Main.screenWidth * 0.55f),
                -Main.screenHeight * 0.55f - Main.rand.NextFloat(30f, 160f));
            Vector2 velocity = new(owner.direction * Main.rand.NextFloat(6.5f, 10.5f), Main.rand.NextFloat(2.8f, 5.5f));

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                owner.Center + screenSizedOffset,
                velocity,
                ModContent.ProjectileType<MalachiteFinalePetal>(),
                Math.Max(1, (int)(Projectile.damage * 0.18f)),
                0.5f,
                Projectile.owner,
                owner.direction);
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
            bool galeAce = owner.active && owner.GetModPlayer<MalachiteAccessoryPlayer>().GaleAceEquipped;

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

            for (int i = 0; i < 5; i++)
            {
                float wave = MathF.Sin(Main.GlobalTimeWrappedHourly * 2.8f + i) * 18f;
                Color color = Color.Lerp(new Color(20, 95, 45, 0), new Color(145, 255, 155, 0), charge);
                Main.EntitySpriteDraw(
                    bloom,
                    center + Vector2.UnitX * wave,
                    null,
                    color * (0.12f + flash * 0.16f),
                    rotation,
                    bloomOrigin,
                    scale * (0.76f + i * 0.11f),
                    SpriteEffects.None);
            }

            float aura = MathF.Sin(MathHelper.Pi * charge);
            for (int i = 0; i < 3; i++)
            {
                float pulse = charge * 4.4f + MathF.Cos(Main.GlobalTimeWrappedHourly * 2f + i) * charge * 0.22f;
                Color ringColor = new Color(75, 255, 135, 0) * (0.18f + flash * 0.18f);
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

            Color starColor = new Color(105, 255, 150, 0) * (0.28f + flash * 0.28f);
            float starPulse = MathHelper.Lerp(0.2f, 1.25f, charge) * (1f + MathF.Sin(Main.GlobalTimeWrappedHourly * MathHelper.TwoPi) * 0.08f);
            Vector2 starScale = new(1.5f + charge * 1.3f, 2.5f + charge * 1.7f);
            Main.EntitySpriteDraw(star, playerScreen, null, starColor, MathHelper.PiOver4, starOrigin, starScale * starPulse, SpriteEffects.None);
            Main.EntitySpriteDraw(star, playerScreen, null, starColor * 0.65f, -MathHelper.PiOver4, starOrigin, starScale * starPulse * 0.68f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, playerScreen, null, new Color(150, 255, 160, 0) * flash * 0.32f, Main.GlobalTimeWrappedHourly * 0.5f, origin, 1.8f + charge * 2.1f, SpriteEffects.None);
        }

        private static void DrawPetals(Texture2D texture, Vector2 origin, float charge, bool winded, int direction)
        {
            int petalCount = 52;
            for (int i = 0; i < petalCount; i++)
            {
                float seed = i * 37.719f;
                float fall = (Main.GlobalTimeWrappedHourly * 74f + seed) % (Main.screenHeight + 160f) - 80f;
                float x = (seed * 19f + MathF.Sin(Main.GlobalTimeWrappedHourly * 1.7f + i) * 42f) % (Main.screenWidth + 120f) - 60f;
                if (winded)
                    x = (x + fall * 0.33f * direction + Main.screenWidth + 120f) % (Main.screenWidth + 120f) - 60f;

                float rotation = Main.GlobalTimeWrappedHourly * (0.7f + i % 5 * 0.11f) + i;
                float scale = 0.13f + (i % 7) * 0.012f;
                Color color = Color.Lerp(new Color(255, 188, 220, 0), new Color(170, 255, 150, 0), i % 3 / 2f);

                Main.EntitySpriteDraw(
                    texture,
                    new Vector2(x, fall),
                    null,
                    color * (0.35f + charge * 0.35f),
                    rotation,
                    origin,
                    scale,
                    SpriteEffects.None);
            }
        }
    }

    public class MalachiteFinaleSlash : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/Malachite";

        public override string LocalizationCategory => "Projectiles.Malachite";

        public override void SetDefaults()
        {
            Projectile.width = 360;
            Projectile.height = 180;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 22;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool? CanDamage() => Projectile.localAI[0] <= 8f;

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            Projectile.localAI[0]++;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            Projectile.Center = owner.Center + direction * 120f;
            Projectile.rotation = direction.ToRotation();

            if (Projectile.localAI[0] == 2f)
                Projectile.Damage();

            Lighting.AddLight(Projectile.Center, 0.18f, 0.75f, 0.22f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 10 * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float fade = Utils.GetLerpValue(22f, 5f, Projectile.localAI[0], true);
            Vector2 directionScale = new(7.2f, 1.35f);

            for (int i = 0; i < 7; i++)
            {
                float offset = (i - 3) * 18f;
                Color color = Color.Lerp(new Color(90, 255, 110, 0), Color.White, i == 3 ? 0.35f : 0.05f);
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * offset,
                    null,
                    color * fade * (0.24f + i * 0.04f),
                    Projectile.rotation + MathHelper.PiOver2,
                    origin,
                    directionScale * (1f - i * 0.035f),
                    SpriteEffects.None);
            }

            return false;
        }
    }

    public class MalachiteFinalePetal : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/Malachite";

        public override string LocalizationCategory => "Projectiles.Malachite";

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 92;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            float direction = Projectile.ai[0] == 0f ? Math.Sign(Projectile.velocity.X) : Projectile.ai[0];
            if (direction == 0f)
                direction = 1f;

            Projectile.velocity.X += direction * 0.015f;
            Projectile.velocity.Y += 0.035f;
            if (Projectile.velocity.Length() > 13f)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * 13f;

            Projectile.rotation += direction * 0.13f + Projectile.velocity.X * 0.015f;
            Projectile.alpha = (int)MathHelper.Lerp(0f, 210f, Utils.GetLerpValue(68f, 92f, Projectile.localAI[0], true));
            Lighting.AddLight(Projectile.Center, 0.08f, 0.23f, 0.08f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 5 * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color color = Color.Lerp(new Color(255, 190, 225, 0), new Color(150, 255, 145, 0), 0.55f) * Projectile.Opacity;
            Vector2 scale = new(0.18f, 0.075f);

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
    }
}
