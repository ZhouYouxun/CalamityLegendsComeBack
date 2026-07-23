using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle
{
    internal sealed class AMROnyxTileMarker : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture =>
            "CalamityLegendsComeBack/Weapons/SHPC/Effects/EAfterDog/Ascendant/AscendantSpirit_PROJ";

        public const float TriggerRadius = 85f;

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

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                GeneralParticleHandler.SpawnParticle(new ImpactParticle(
                    Projectile.Center + forward * 15f,
                    0.12f,
                    16,
                    0.52f,
                    new Color(106, 220, 255)));

                for (int i = -2; i <= 2; i++)
                {
                    Vector2 velocity = -forward.RotatedBy(MathHelper.ToRadians(9f) * i) * (1.2f + MathF.Abs(i) * 0.28f);
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + forward * 13f,
                        velocity,
                        false,
                        15 + Math.Abs(i) * 2,
                        0.28f + Math.Abs(i) * 0.035f,
                        i == 0 ? Color.White : new Color(70, 185, 255),
                        true,
                        false,
                        true));
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
                    new Color(89, 202, 255),
                    true,
                    false,
                    true));
            }

            if (age % 30 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    Projectile.Center - forward * 3f,
                    -forward * 0.28f,
                    Color.White,
                    new Color(35, 151, 255),
                    0.28f,
                    12,
                    0.08f,
                    2.5f));
            }

            Lighting.AddLight(Projectile.Center, new Color(40, 150, 255).ToVector3() * 0.55f);
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

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                Color.Lerp(lightColor, new Color(150, 225, 255), 0.68f) * opacity,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                new Color(35, 165, 255, 0) * (opacity * 0.48f),
                Projectile.rotation,
                origin,
                Projectile.scale * 1.08f,
                SpriteEffects.None,
                0f);
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                new Color(75, 196, 255, 0) * (opacity * 0.72f),
                0f,
                bloom.Size() * 0.5f,
                0.13f,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
