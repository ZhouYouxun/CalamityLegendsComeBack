using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    internal static class BFArrowCommon
    {
        private const string ArrowBeamTexture = "CalamityMod/Particles/GlowBlade";

        public static void SetBaseArrowDefaults(Projectile projectile, int width = 14, int height = 34, int timeLeft = 180, int penetrate = 1, int extraUpdates = 1, bool tileCollide = true)
        {
            projectile.width = width;
            projectile.height = height;
            projectile.friendly = true;
            projectile.hostile = false;
            projectile.DamageType = DamageClass.Ranged;
            projectile.ignoreWater = true;
            projectile.tileCollide = tileCollide;
            projectile.arrow = true;
            projectile.penetrate = penetrate;
            projectile.timeLeft = timeLeft;
            projectile.extraUpdates = extraUpdates;
            ForceLocalNPCImmunity(projectile, 12);
        }

        public static void ForceLocalNPCImmunity(Projectile projectile, int cooldown = 12)
        {
            projectile.usesIDStaticNPCImmunity = false;
            projectile.idStaticNPCHitCooldown = -1;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = cooldown;
        }

        public static void FaceForward(Projectile projectile, float rotationOffset = MathHelper.PiOver2 + MathHelper.Pi)
        {
            if (projectile.velocity != Vector2.Zero)
                projectile.rotation = projectile.velocity.ToRotation() + rotationOffset;
        }

        public static void MaintainSpeed(Projectile projectile, float targetSpeed, float interpolation = 0.08f)
        {
            if (projectile.velocity == Vector2.Zero)
                return;

            float speed = MathHelper.Lerp(projectile.velocity.Length(), targetSpeed, interpolation);
            projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
        }

        public static void WeakHomeTowards(Projectile projectile, NPC target, float inertia = 24f, float targetSpeed = -1f)
        {
            if (target is null || !target.active)
                return;

            float speed = targetSpeed > 0f ? targetSpeed : System.Math.Max(projectile.velocity.Length(), 10f);
            Vector2 desiredVelocity = (target.Center - projectile.Center).SafeNormalize(projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
            projectile.velocity = (projectile.velocity * (inertia - 1f) + desiredVelocity) / inertia;
        }

        public static void DirectHomeTowards(Projectile projectile, NPC target, float responsiveness = 0.2f, float targetSpeed = -1f)
        {
            if (target is null || !target.active)
                return;

            float speed = targetSpeed > 0f ? targetSpeed : System.Math.Max(projectile.velocity.Length(), 10f);
            Vector2 aimPoint = target.Center + target.velocity * 0.18f;
            Vector2 desiredVelocity = (aimPoint - projectile.Center).SafeNormalize(projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
            projectile.velocity = Vector2.Lerp(projectile.velocity, desiredVelocity, MathHelper.Clamp(responsiveness, 0.01f, 1f));

            if (projectile.velocity.LengthSquared() < 0.01f)
                projectile.velocity = desiredVelocity;
        }

        public static bool Bounce(Projectile projectile, Vector2 oldVelocity, ref float bounceCounter, int maxBounces, float velocityRetention = 1f)
        {
            bounceCounter++;
            if (bounceCounter > maxBounces)
                return true;

            if (projectile.velocity.X != oldVelocity.X)
                projectile.velocity.X = -oldVelocity.X * velocityRetention;

            if (projectile.velocity.Y != oldVelocity.Y)
                projectile.velocity.Y = -oldVelocity.Y * velocityRetention;

            projectile.netUpdate = true;
            return false;
        }

        public static void EmitPresetTrail(Projectile projectile, BlossomFluxChloroplastPresetType preset, float intensity = 1f)
        {
            if (!Main.rand.NextBool(2))
                return;

            Color mainColor = GetPresetColor(preset);
            Color accentColor = GetPresetAccentColor(preset);
            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 spawnPosition = projectile.Center - projectile.velocity * 0.12f + normal * Main.rand.NextFloat(-4f, 4f);

            Dust dust = Dust.NewDustPerfect(
                spawnPosition,
                GetPresetDustType(preset),
                -projectile.velocity * 0.06f + Main.rand.NextVector2Circular(0.7f, 0.7f),
                100,
                Color.Lerp(mainColor, accentColor, Main.rand.NextFloat(0.12f, 0.5f)),
                Main.rand.NextFloat(0.85f, 1.2f) * intensity);
            dust.noGravity = true;

            if (!Main.rand.NextBool(4))
                return;

            Dust sparkle = Dust.NewDustPerfect(
                projectile.Center + normal * Main.rand.NextFloat(-2.5f, 2.5f),
                DustID.TerraBlade,
                direction.RotatedByRandom(0.24f) * Main.rand.NextFloat(0.45f, 1.4f),
                100,
                accentColor,
                Main.rand.NextFloat(0.7f, 1f) * intensity);
            sparkle.noGravity = true;

            if (Main.dedServ || !Main.rand.NextBool(5))
                return;

            switch (preset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                {
                    CustomSpark bladeAccent = new(
                        projectile.Center - direction * 7f + normal * Main.rand.NextFloat(-3f, 3f),
                        projectile.velocity * 0.02f,
                        ArrowBeamTexture,
                        false,
                        8,
                        0.12f * intensity,
                        Color.Lerp(mainColor, accentColor, 0.35f),
                        new Vector2(0.42f, 1.7f),
                        glowCenter: true,
                        shrinkSpeed: 1.06f,
                        glowCenterScale: 0.82f,
                        glowOpacity: 0.75f);
                    GeneralParticleHandler.SpawnParticle(bladeAccent);
                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                {
                    GlowOrbParticle recoveryOrb = new(
                        projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                        -projectile.velocity * 0.03f + Main.rand.NextVector2Circular(0.2f, 0.2f),
                        false,
                        10,
                        0.34f * intensity,
                        Color.Lerp(mainColor, Color.White, 0.35f),
                        true,
                        false,
                        true);
                    GeneralParticleHandler.SpawnParticle(recoveryOrb);
                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                {
                    CritSpark reconSpark = new(
                        projectile.Center + normal * Main.rand.NextFloat(-4f, 4f),
                        direction.RotatedByRandom(0.16f) * Main.rand.NextFloat(1.4f, 2.4f),
                        Color.White,
                        accentColor,
                        0.62f * intensity,
                        12);
                    GeneralParticleHandler.SpawnParticle(reconSpark);
                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                {
                    GlowOrbParticle emberOrb = new(
                        projectile.Center + normal * Main.rand.NextFloat(-5f, 5f),
                        -direction * Main.rand.NextFloat(0.4f, 1.1f) + Main.rand.NextVector2Circular(0.25f, 0.25f),
                        false,
                        8,
                        0.36f * intensity,
                        Color.Lerp(mainColor, accentColor, 0.4f),
                        true,
                        false,
                        true);
                    GeneralParticleHandler.SpawnParticle(emberOrb);
                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                {
                    HeavySmokeParticle plagueSmoke = new(
                        projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                        -projectile.velocity * 0.02f + Main.rand.NextVector2Circular(0.18f, 0.18f),
                        Color.Lerp(mainColor, accentColor, 0.25f),
                        12,
                        0.42f * intensity,
                        0.45f,
                        Main.rand.NextFloat(-0.04f, 0.04f),
                        false);
                    GeneralParticleHandler.SpawnParticle(plagueSmoke);
                    break;
                }
            }
        }

        public static void EmitPresetBurst(Projectile projectile, BlossomFluxChloroplastPresetType preset, int amount, float speedMin, float speedMax, float scaleMin = 0.9f, float scaleMax = 1.3f)
        {
            Color mainColor = GetPresetColor(preset);
            Color accentColor = GetPresetAccentColor(preset);
            int dustType = GetPresetDustType(preset);

            for (int i = 0; i < amount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(3f, 3f) * Main.rand.NextFloat(speedMin, speedMax);
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    dustType,
                    velocity,
                    100,
                    Color.Lerp(mainColor, accentColor, Main.rand.NextFloat(0.12f, 0.48f)),
                    Main.rand.NextFloat(scaleMin, scaleMax));
                dust.noGravity = true;
            }

            for (int i = 0; i < amount / 4; i++)
            {
                Dust sparkle = Dust.NewDustPerfect(
                    projectile.Center,
                    DustID.TerraBlade,
                    Main.rand.NextVector2CircularEdge(2.4f, 2.4f) * Main.rand.NextFloat(speedMin * 0.45f, speedMax * 0.8f),
                    100,
                    accentColor,
                    Main.rand.NextFloat(0.75f, 1.05f));
                sparkle.noGravity = true;
            }

            if (Main.dedServ)
                return;

            float intensity = MathHelper.Clamp(amount / 12f, 0.7f, 1.5f);
            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);

            switch (preset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                {
                    DirectionalPulseRing pulse = new(
                        projectile.Center,
                        projectile.velocity * 0.06f,
                        Color.Lerp(mainColor, accentColor, 0.2f),
                        new Vector2(0.76f, 2.2f),
                        direction.ToRotation(),
                        0.2f * intensity,
                        0.045f,
                        14);
                    GeneralParticleHandler.SpawnParticle(pulse);

                    for (int i = 0; i < 2; i++)
                    {
                        GenericSparkle sparkleBurst = new(
                            projectile.Center + normal * (i == 0 ? -6f : 6f),
                            Main.rand.NextVector2Circular(0.4f, 0.4f),
                            accentColor,
                            Color.White,
                            1.2f * intensity,
                            8,
                            Main.rand.NextFloat(-0.06f, 0.06f),
                            1.4f);
                        GeneralParticleHandler.SpawnParticle(sparkleBurst);
                    }

                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                {
                    DirectionalPulseRing pulse = new(
                        projectile.Center,
                        Vector2.Zero,
                        Color.Lerp(mainColor, Color.White, 0.25f),
                        Vector2.One,
                        0f,
                        0.18f * intensity,
                        0.038f,
                        16);
                    GeneralParticleHandler.SpawnParticle(pulse);

                    for (int i = 0; i < 3; i++)
                    {
                        GlowOrbParticle orb = new(
                            projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                            Main.rand.NextVector2Circular(0.9f, 0.9f),
                            false,
                            12,
                            Main.rand.NextFloat(0.28f, 0.42f) * intensity,
                            Color.Lerp(mainColor, Color.White, 0.35f),
                            true,
                            false,
                            true);
                        GeneralParticleHandler.SpawnParticle(orb);
                    }

                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                {
                    BloomLineVFX lineA = new(projectile.Center - direction * 18f, direction * 36f, 1.15f * intensity, accentColor, 12);
                    BloomLineVFX lineB = new(projectile.Center - normal * 16f, normal * 32f, 0.9f * intensity, mainColor, 10);
                    GeneralParticleHandler.SpawnParticle(lineA);
                    GeneralParticleHandler.SpawnParticle(lineB);

                    GenericSparkle scanFlash = new(
                        projectile.Center,
                        Vector2.Zero,
                        accentColor,
                        Color.White,
                        1.1f * intensity,
                        8,
                        0f,
                        1.35f);
                    GeneralParticleHandler.SpawnParticle(scanFlash);
                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                {
                    DetailedExplosion explosion = new(
                        projectile.Center,
                        Vector2.Zero,
                        Color.Lerp(mainColor, accentColor, 0.25f),
                        Vector2.One,
                        Main.rand.NextFloat(-0.25f, 0.25f),
                        0f,
                        0.22f * intensity,
                        12);
                    GeneralParticleHandler.SpawnParticle(explosion);

                    HeavySmokeParticle smoke = new(
                        projectile.Center,
                        Main.rand.NextVector2Circular(0.4f, 0.4f),
                        Color.Lerp(mainColor, Color.Black, 0.2f),
                        18,
                        0.62f * intensity,
                        0.65f,
                        Main.rand.NextFloat(-0.05f, 0.05f),
                        true);
                    GeneralParticleHandler.SpawnParticle(smoke);
                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                {
                    DirectionalPulseRing pulse = new(
                        projectile.Center,
                        Vector2.Zero,
                        Color.Lerp(mainColor, accentColor, 0.25f),
                        new Vector2(1.12f, 1.3f),
                        Main.rand.NextFloat(-0.25f, 0.25f),
                        0.17f * intensity,
                        0.036f,
                        15);
                    GeneralParticleHandler.SpawnParticle(pulse);

                    for (int i = 0; i < 2; i++)
                    {
                        HeavySmokeParticle smoke = new(
                            projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                            Main.rand.NextVector2Circular(0.45f, 0.45f) + new Vector2(0f, -0.12f),
                            Color.Lerp(mainColor, accentColor, 0.3f),
                            16,
                            0.55f * intensity,
                            0.58f,
                            Main.rand.NextFloat(-0.04f, 0.04f),
                            false);
                        GeneralParticleHandler.SpawnParticle(smoke);
                    }

                    break;
                }
            }
        }

        public static void DrawPresetArrow(Projectile projectile, Color lightColor, BlossomFluxChloroplastPresetType preset, float scale = 1f, bool drawAfterimages = true)
        {
            Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
            Texture2D glowBlade = ModContent.Request<Texture2D>(ArrowBeamTexture).Value;
            Color mainColor = projectile.GetAlpha(GetPresetColor(preset));
            Color accentColor = projectile.GetAlpha(GetPresetAccentColor(preset));
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitY);
            float pulse = 1f + 0.08f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 8f + projectile.identity * 0.37f);
            float beamRotation = forward.ToRotation() + MathHelper.PiOver2;
            Vector2 beamOrigin = new(glowBlade.Width * 0.5f, glowBlade.Height);
            Vector2 beamAnchor = drawPosition - forward * 7f;
            Vector2 outerBeamScale = new Vector2(0.88f, 1.35f) * projectile.scale * scale * 0.04f * pulse;
            Vector2 innerBeamScale = new Vector2(0.54f, 1.08f) * projectile.scale * scale * 0.04f * pulse;
            float outlineDistance = 1.6f + 0.7f * pulse;
            Color outlineColor = Color.Lerp(mainColor, accentColor, 0.4f) * 0.92f;
            Color centerGlowColor = Color.Lerp(accentColor, Color.White, 0.38f) * 0.6f;

            if (drawAfterimages)
            {
                for (int i = 0; i < projectile.oldPos.Length; i++)
                {
                    float completion = 1f - i / (float)projectile.oldPos.Length;
                    if (completion <= 0f)
                        continue;

                    Main.EntitySpriteDraw(
                        texture,
                        projectile.oldPos[i] + projectile.Size * 0.5f - Main.screenPosition,
                        null,
                        mainColor * (0.12f * completion),
                        projectile.oldRot[i],
                        texture.Size() * 0.5f,
                        projectile.scale * scale * MathHelper.Lerp(0.82f, 1f, completion),
                        SpriteEffects.None,
                        0);
                }
            }

            BeginAdditiveBatch();
            for (int i = 0; i < 16; i++)
            {
                float angle = MathHelper.TwoPi * i / 16f;
                Vector2 offset = angle.ToRotationVector2() * outlineDistance;
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + offset,
                    null,
                    outlineColor,
                    projectile.rotation,
                    texture.Size() * 0.5f,
                    projectile.scale * scale,
                    SpriteEffects.None,
                    0);
            }

            Main.EntitySpriteDraw(
                glowBlade,
                beamAnchor,
                null,
                mainColor * 0.72f,
                beamRotation,
                beamOrigin,
                outerBeamScale,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                glowBlade,
                beamAnchor + forward * 2f,
                null,
                accentColor * 0.52f,
                beamRotation,
                beamOrigin,
                innerBeamScale,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                centerGlowColor,
                projectile.rotation,
                texture.Size() * 0.5f,
                projectile.scale * scale * (1.05f + 0.03f * pulse),
                SpriteEffects.None,
                0);

            BeginAlphaBatch();

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                projectile.GetAlpha(lightColor),
                projectile.rotation,
                texture.Size() * 0.5f,
                projectile.scale * scale,
                SpriteEffects.None,
                0);

            BeginAlphaBatch();
        }

        public static void DrawProjectile(Projectile projectile, Texture2D texture, Color color, float rotation, float scale = 1f)
        {
            Main.EntitySpriteDraw(
                texture,
                projectile.Center - Main.screenPosition,
                null,
                color,
                rotation,
                texture.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0);
        }

        public static void TagBlossomFluxLeftArrow(Projectile projectile)
        {
            projectile.arrow = true;
            projectile.noDropItem = true;
            projectile.GetGlobalProjectile<BFArrow_CDetecEffect>().BlossomFluxLeftArrow = true;
        }

        public static bool InBounds(int index, int max) => index >= 0 && index < max;
        public static bool InBounds(float index, int max) => index >= 0f && index < max;

        public static bool TryPickBlossomFluxAmmo(Player player, out int projectileType, out float speed, out int damage, out float knockback, bool dontConsume = true)
        {
            Item blossomFlux = new();
            blossomFlux.SetDefaults(ModContent.ItemType<NewLegendBlossomFlux>());
            return player.PickAmmo(blossomFlux, out projectileType, out speed, out damage, out knockback, out _, dontConsume);
        }

        public static Player FindLowestHealthPlayer(Player owner, float maxDistance = 1800f)
        {
            Player bestPlayer = owner;
            float bestRatio = owner.statLifeMax2 > 0 ? owner.statLife / (float)owner.statLifeMax2 : 1f;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player candidate = Main.player[i];
                if (!candidate.active || candidate.dead)
                    continue;

                if (Vector2.Distance(owner.Center, candidate.Center) > maxDistance)
                    continue;

                float ratio = candidate.statLifeMax2 > 0 ? candidate.statLife / (float)candidate.statLifeMax2 : 1f;
                if (ratio < bestRatio)
                {
                    bestRatio = ratio;
                    bestPlayer = candidate;
                }
            }

            return bestPlayer;
        }

        public static string GetTexturePathForPreset(BlossomFluxChloroplastPresetType preset) => preset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/ABreak/BFArrow_ABreak",
            BlossomFluxChloroplastPresetType.Chlo_BRecov => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/BRecov/BFArrow_BRecov",
            BlossomFluxChloroplastPresetType.Chlo_CDetec => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/CDetec/BFArrow_CDetec",
            BlossomFluxChloroplastPresetType.Chlo_DBomb => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/DBomb/BFArrow_DBomb",
            BlossomFluxChloroplastPresetType.Chlo_EPlague => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/EPlague/BFArrow_EPlague",
            _ => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/ABreak/BFArrow_ABreak"
        };

        public static Color GetPresetColor(BlossomFluxChloroplastPresetType preset) => ChloroplastCommon.PresetColor(preset);
        public static Color GetPresetAccentColor(BlossomFluxChloroplastPresetType preset) => ChloroplastCommon.PresetAccentColor(preset);
        public static int GetPresetDustType(BlossomFluxChloroplastPresetType preset) => ChloroplastCommon.PresetDustType(preset);

        private static void BeginAdditiveBatch()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);
        }

        private static void BeginAlphaBatch()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
