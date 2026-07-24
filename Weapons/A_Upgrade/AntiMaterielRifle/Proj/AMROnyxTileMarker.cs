using System;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle.Proj
{
    internal sealed class AMROnyxTileMarker : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture =>
            "CalamityLegendsComeBack/Weapons/SHPC/Effects/EAfterDog/Ascendant/AscendantSpirit_PROJ";

        public const float TriggerRadius = 85f;

        // 玛瑙配色：黑曜石本体压住整体明度，留出冷蓝紫内辉与暗金边框
        private static readonly Color OnyxSheen = new(74, 150, 214);
        private static readonly Color OnyxRim = new(44, 108, 170);
        private static readonly Color OnyxViolet = new(75, 35, 140);
        private static readonly Color OnyxDeepViolet = new(45, 18, 90);

        private int TargetIndex => (int)Projectile.ai[1] - 1;
        internal bool IsTileMarker => Projectile.ai[1] < 0.5f;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 5 * 60;
            Projectile.scale = 0.72f;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.rotation = Projectile.ai[0];

            if (!IsTileMarker)
            {
                if (!Main.npc.IndexInRange(TargetIndex) || !Main.npc[TargetIndex].active)
                {
                    Projectile.Kill();
                    return;
                }

                Projectile.Center = Main.npc[TargetIndex].Center + Projectile.velocity;
            }

            if (Main.dedServ)
                return;

            Vector2 forward = (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2();
            Vector2 normal = new(-forward.Y, forward.X);
            int age = 5 * 60 - Projectile.timeLeft;
            bool attachedToTarget = !IsTileMarker;

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                GeneralParticleHandler.SpawnParticle(new ImpactParticle(
                    Projectile.Center + forward * 15f,
                    0.12f,
                    16,
                    0.52f,
                    OnyxSheen));

                for (int i = -2; i <= 2; i++)
                {
                    Vector2 velocity = -forward.RotatedBy(MathHelper.ToRadians(9f) * i) * (1.2f + MathF.Abs(i) * 0.28f);
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + forward * 13f,
                        velocity,
                        false,
                        15 + Math.Abs(i) * 2,
                        0.28f + Math.Abs(i) * 0.035f,
                        i == 0 ? OnyxSheen : OnyxRim,
                        true,
                        false,
                        true));
                }

                GeneralParticleHandler.SpawnParticle(new GenericBloom(
                    Projectile.Center + forward * 4f,
                    Vector2.Zero,
                    OnyxDeepViolet,
                    attachedToTarget ? 0.52f : 0.35f,
                    22,
                    false,
                    false));

                int voidBurst = attachedToTarget ? 18 : 11;
                for (int i = 0; i < voidBurst; i++)
                {
                    float variance = Main.rand.NextFloat(-0.85f, 0.85f);
                    Dust voidDust = Dust.NewDustPerfect(
                        Projectile.Center + forward * 6f,
                        ModContent.DustType<VoidDustInverted>());
                    voidDust.scale = (Main.rand.NextFloat(1.5f, 2.1f) - MathF.Abs(variance)) * 0.62f;
                    voidDust.velocity = -forward.RotatedBy(variance * 1.5f) *
                        Main.rand.NextFloat(1.1f, 4.4f) * (1f - MathF.Abs(variance) * 0.5f);
                    voidDust.noGravity = true;
                    voidDust.color = Color.Lerp(OnyxViolet, OnyxRim, Main.rand.NextFloat());
                }

                if (attachedToTarget)
                {
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                        Projectile.Center + forward * 7f,
                        Vector2.Zero,
                        OnyxViolet,
                        new Vector2(0.32f, 0.94f),
                        Projectile.rotation,
                        0.04f,
                        0.82f,
                        18));

                    for (int i = 0; i < 2; i++)
                    {
                        GeneralParticleHandler.SpawnParticle(new CustomPulse(
                            Projectile.Center + forward * 5f,
                            Vector2.Zero,
                            OnyxDeepViolet,
                            "CalamityMod/Particles/SmallBloom",
                            Vector2.One,
                            Main.rand.NextFloat(-10f, 10f),
                            (0.75f + i * 0.25f),
                            0f,
                            34,
                            false));
                    }

                    for (int i = -3; i <= 3; i++)
                    {
                        float spread = i / 3f;
                        Vector2 velocity = forward.RotatedBy(MathHelper.ToRadians(22f) * spread) *
                            (2.1f + (1f - MathF.Abs(spread)) * 1.35f);
                        GeneralParticleHandler.SpawnParticle(new CritSpark(
                            Projectile.Center + forward * 12f + normal * spread * 4f,
                            velocity,
                            OnyxSheen,
                            Color.Lerp(OnyxRim, OnyxViolet, MathF.Abs(spread)),
                            0.32f,
                            16,
                            0.06f,
                            2.2f));
                    }
                }
            }

            if (age % 9 == 0)
            {
                float wave = MathF.Sin(age * 0.42f);
                Vector2 position = Projectile.Center - forward * 7f + normal * wave * 5f;
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    position,
                    -forward * 0.24f - normal * wave * 0.08f,
                    false,
                    13,
                    0.24f,
                    OnyxRim,
                    true,
                    false,
                    true));
            }

            if (age % (attachedToTarget ? 4 : 9) == 0)
            {
                Dust voidDust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    ModContent.DustType<VoidDustInverted>());
                voidDust.scale = Main.rand.NextFloat(0.42f, 0.86f) * (attachedToTarget ? 1.35f : 1f);
                voidDust.velocity = -forward * Main.rand.NextFloat(0.2f, 0.9f) +
                    normal * Main.rand.NextFloat(-0.35f, 0.35f);
                voidDust.noGravity = true;
                voidDust.color = Main.rand.NextBool(3) ? OnyxViolet : OnyxRim;
            }

            if (age % 12 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GenericBloom(
                    Projectile.Center - forward * 2f,
                    -forward * 0.12f,
                    OnyxDeepViolet,
                    attachedToTarget ? 0.28f : 0.18f,
                    16,
                    false,
                    false));
            }

            if (age % 30 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    Projectile.Center - forward * 3f,
                    -forward * 0.28f,
                    OnyxSheen,
                    OnyxRim,
                    0.28f,
                    12,
                    0.08f,
                    2.5f));
            }

            if (attachedToTarget && age % 3 == 0)
            {
                float phase = age * 0.24f;
                for (int side = -1; side <= 1; side += 2)
                {
                    float orbit = phase + side * MathHelper.PiOver2;
                    Vector2 offset = normal * MathF.Sin(orbit) * 8f + forward * MathF.Cos(orbit) * 5f;
                    Vector2 velocity = normal * MathF.Cos(orbit) * side * 0.24f - forward * MathF.Sin(orbit) * 0.14f;
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + forward * 4f + offset,
                        velocity,
                        false,
                        13,
                        0.18f,
                        side > 0 ? OnyxRim : OnyxViolet,
                        true,
                        false,
                        true));
                }
            }

            if (attachedToTarget && age % 18 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    Projectile.Center + forward * 14f,
                    forward * 0.52f,
                    OnyxSheen,
                    OnyxRim,
                    0.3f,
                    13,
                    0.05f,
                    2.8f));
            }

            if (attachedToTarget && age % 26 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center + forward * 5f,
                    Vector2.Zero,
                    OnyxDeepViolet,
                    new Vector2(0.22f, 0.64f),
                    Projectile.rotation,
                    0.035f,
                    0.58f,
                    15));
            }

            Lighting.AddLight(Projectile.Center, OnyxRim.ToVector3() * (attachedToTarget ? 0.55f : 0.34f));
        }

        internal bool IsAttachedTo(int targetIndex) => !IsTileMarker && TargetIndex == targetIndex;

        public void Detonate(int detonatorDamage)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = -0.3f }, Projectile.Center);

            if (Projectile.owner == Main.myPlayer)
            {
                int detonation = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<AMROnyxDetonation>(),
                    detonatorDamage,
                    6f,
                    Projectile.owner,
                    -1);

                if (Main.projectile.IndexInRange(detonation))
                    Main.projectile[detonation].CritChance = 0;
            }

            Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            float opacity = Math.Min(1f, Projectile.timeLeft / 24f);
            bool attachedToTarget = !IsTileMarker;
            Vector2 forward = (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2();
            Vector2 normal = new(-forward.Y, forward.X);
            float pulse = 0.5f + 0.5f * MathF.Sin((5 * 60 - Projectile.timeLeft) * 0.19f);

            // 背景发光：使用暗紫/暗蓝替掉实体黑色掩码遮罩
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                OnyxDeepViolet * (opacity * 0.45f),
                0f,
                bloom.Size() * 0.5f,
                0.22f + pulse * 0.02f,
                SpriteEffects.None,
                0f);

            // 钉附弹本体 (采用暗曜石紫与冷蓝混色渲染)
            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                Color.Lerp(OnyxDeepViolet, OnyxRim, 0.45f) * opacity,
                Projectile.rotation,
                origin,
                Projectile.scale * 1.12f,
                SpriteEffects.None,
                0f);
            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                Color.Lerp(lightColor, OnyxSheen, 0.72f) * (opacity * 0.85f),
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0f);

            // 叠加 Additive 发光
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                new Color(26, 92, 152, 0) * (opacity * 0.45f),
                Projectile.rotation,
                origin,
                Projectile.scale * 1.08f,
                SpriteEffects.None,
                0f);

            if (attachedToTarget)
            {
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition - forward * 3f,
                    null,
                    new Color(76, 32, 154, 0) * (opacity * (0.25f + pulse * 0.15f)),
                    Projectile.rotation,
                    origin,
                    Projectile.scale * (1.22f + pulse * 0.1f),
                    SpriteEffects.None,
                    0f);
                Main.EntitySpriteDraw(
                    bloom,
                    drawPosition + forward * 10f,
                    null,
                    new Color(118, 188, 238, 0) * (opacity * (0.45f + pulse * 0.2f)),
                    0f,
                    bloom.Size() * 0.5f,
                    0.12f + pulse * 0.035f,
                    SpriteEffects.None,
                    0f);
            }

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                new Color(46, 120, 186, 0) * (opacity * 0.5f),
                0f,
                bloom.Size() * 0.5f,
                0.14f,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
