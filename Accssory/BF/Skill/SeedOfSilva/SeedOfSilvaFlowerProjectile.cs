using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.SeedOfSilva
{
    internal abstract class SeedOfSilvaFlowerProjectile : ModProjectile
    {
        public const int FlowerCount = 5;

        private const float OrbitRadius = 222f;
        private const float OrbitSpeed = 0.018f;
        private const float OutlineRadius = 2f;
        private const float BlossomTransitionStep = 1f / 18f;

        private static readonly string[] SmallSeedTextures =
        {
            "CalamityLegendsComeBack/Accssory/BF/Skill/SeedOfSilva/SeedPack/SmallSeed1",
            "CalamityLegendsComeBack/Accssory/BF/Skill/SeedOfSilva/SeedPack/SmallSeed2",
            "CalamityLegendsComeBack/Accssory/BF/Skill/SeedOfSilva/SeedPack/SmallSeed3",
            "CalamityLegendsComeBack/Accssory/BF/Skill/SeedOfSilva/SeedPack/SmallSeed4"
        };

        public BlossomFluxChloroplastPresetType CurrentPreset => FlowerPreset;
        public int CurrentSlot => FlowerSlot;
        public bool IsBlooming { get; private set; }
        protected float VisualBloomProgress { get; private set; }
        private float orbitAngle;

        protected abstract int FlowerSlot { get; }
        protected abstract BlossomFluxChloroplastPresetType FlowerPreset { get; }
        protected abstract string FlowerTexturePath { get; }
        protected virtual Color FlowerColor => GetFlowerColor(FlowerPreset);
        protected virtual Color FlowerAccentColor => GetFlowerAccentColor(FlowerPreset);

        public override string Texture => FlowerTexturePath;

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        public override bool? CanDamage() => Projectile.damage > 0 ? null : false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || !owner.GetModPlayer<BFAccessoryPlayer>().SeedOfSilvaEquipped)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            BFAccessoryPlayer accessoryPlayer = owner.GetModPlayer<BFAccessoryPlayer>();
            IsBlooming = accessoryPlayer.HoldingBlossomFlux && accessoryPlayer.CurrentPreset == FlowerPreset;
            VisualBloomProgress = MathHelper.Clamp(VisualBloomProgress + (IsBlooming ? BlossomTransitionStep : -BlossomTransitionStep), 0f, 1f);
            Projectile.damage = GetSeedContactDamage(owner, accessoryPlayer.HoldingBlossomFlux);

            if (orbitAngle == 0f)
                orbitAngle = (Main.GameUpdateCount * OrbitSpeed) % MathHelper.TwoPi;
            orbitAngle += OrbitSpeed * GetOrbitSpeedMultiplier(owner, accessoryPlayer);
            if (orbitAngle >= MathHelper.TwoPi)
                orbitAngle -= MathHelper.TwoPi;
            float angle = orbitAngle + MathHelper.TwoPi * FlowerSlot / FlowerCount;
            float pulseRadius = OrbitRadius + (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 1.1f + FlowerSlot) * 5f;
            Projectile.Center = owner.Center + angle.ToRotationVector2() * pulseRadius + new Vector2(0f, owner.gfxOffY - 8f);
            Projectile.rotation = angle + MathHelper.PiOver2;
            Projectile.scale = MathHelper.Lerp(Projectile.scale, IsBlooming ? 1f : 0.92f, 0.16f);

            Lighting.AddLight(Projectile.Center, FlowerColor.ToVector3() * MathHelper.Lerp(0.11f, 0.34f, VisualBloomProgress));

            UpdateCommon(owner, accessoryPlayer);
            if (IsBlooming)
                UpdateBlooming(owner, accessoryPlayer);
            else
                UpdateDormant(owner, accessoryPlayer);
        }

        public virtual bool TryTriggerTorchflowerExplosion(Projectile triggeringProjectile) => false;

        protected virtual void UpdateCommon(Player owner, BFAccessoryPlayer accessoryPlayer)
        {
        }

        protected virtual void UpdateBlooming(Player owner, BFAccessoryPlayer accessoryPlayer)
        {
        }

        protected virtual void UpdateDormant(Player owner, BFAccessoryPlayer accessoryPlayer)
        {
        }

        protected virtual float GetOrbitSpeedMultiplier(Player owner, BFAccessoryPlayer accessoryPlayer) =>
            SeedOfSilvaSunflower.ShouldBoostSeedOrbit(owner, accessoryPlayer) ? 1.25f : 1f;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D seedTexture = ModContent.Request<Texture2D>(SmallSeedTextures[GetDormantSeedTextureIndex()]).Value;
            Texture2D flowerTexture = ModContent.Request<Texture2D>(FlowerTexturePath).Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            float pulse = 0.96f + 0.04f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 3.2f + Projectile.identity);
            float seedOpacity = Projectile.Opacity * (1f - VisualBloomProgress);
            float flowerOpacity = Projectile.Opacity * VisualBloomProgress;
            float seedScale = Projectile.scale * pulse * MathHelper.Lerp(1f, 0.76f, VisualBloomProgress);
            float flowerScale = Projectile.scale * pulse * MathHelper.Lerp(0.28f, 1f, VisualBloomProgress);
            float seedRotation = Projectile.rotation * 0.2f + Main.GlobalTimeWrappedHourly * 0.08f;
            float flowerRotation = Projectile.rotation + Main.GlobalTimeWrappedHourly * 0.12f;

            if (seedOpacity > 0f)
            {
                DrawDormantMagicGlow(center, seedOpacity);
                DrawOutline(seedTexture, center, seedTexture.Size() * 0.5f, seedRotation, seedScale, FlowerColor * (0.62f * seedOpacity));
                Main.EntitySpriteDraw(seedTexture, center, null, Color.Lerp(lightColor, Color.White, 0.18f) * seedOpacity, seedRotation, seedTexture.Size() * 0.5f, seedScale, SpriteEffects.None, 0);
            }

            if (flowerOpacity > 0f)
            {
                DrawFlowerUnderGlow(center, flowerTexture, flowerTexture.Size() * 0.5f, flowerRotation, flowerScale, flowerOpacity);
                DrawOutline(flowerTexture, center, flowerTexture.Size() * 0.5f, flowerRotation, flowerScale, FlowerColor * (0.88f * flowerOpacity));
                Main.EntitySpriteDraw(flowerTexture, center, null, Color.Lerp(lightColor, Color.White, 0.28f) * flowerOpacity, flowerRotation, flowerTexture.Size() * 0.5f, flowerScale, SpriteEffects.None, 0);
            }

            return false;
        }

        protected NPC FindTarget(float range)
        {
            NPC bestTarget = null;
            float bestDistance = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.Distance(npc.Center, Projectile.Center);
                if (distance >= bestDistance)
                    continue;

                bestTarget = npc;
                bestDistance = distance;
            }

            return bestTarget;
        }

        protected void EmitFlowerBurst(Color color, int amount)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < amount; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GemEmerald,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.6f, 1.8f),
                    100,
                    color,
                    Main.rand.NextFloat(0.58f, 0.9f));
                dust.noGravity = true;
            }
        }

        public static int GetFlowerType(int slot) => slot switch
        {
            1 => ModContent.ProjectileType<SeedOfSilvaDaisy>(),
            2 => ModContent.ProjectileType<SeedOfSilvaDelphinium>(),
            3 => ModContent.ProjectileType<SeedOfSilvaTorchflower>(),
            4 => ModContent.ProjectileType<SeedOfSilvaMandrake>(),
            _ => ModContent.ProjectileType<SeedOfSilvaSunflower>()
        };

        public static BlossomFluxChloroplastPresetType PresetFromSlot(int slot) => slot switch
        {
            1 => BlossomFluxChloroplastPresetType.Chlo_BRecov,
            2 => BlossomFluxChloroplastPresetType.Chlo_CDetec,
            3 => BlossomFluxChloroplastPresetType.Chlo_DBomb,
            4 => BlossomFluxChloroplastPresetType.Chlo_EPlague,
            _ => BlossomFluxChloroplastPresetType.Chlo_ABreak
        };

        private int GetDormantSeedTextureIndex()
        {
            Player owner = Main.player[Projectile.owner];
            BFAccessoryPlayer accessoryPlayer = owner.GetModPlayer<BFAccessoryPlayer>();
            if (!accessoryPlayer.HoldingBlossomFlux)
                return FlowerSlot % SmallSeedTextures.Length;

            int rank = 0;
            for (int slot = 0; slot < FlowerCount; slot++)
            {
                if (PresetFromSlot(slot) == accessoryPlayer.CurrentPreset)
                    continue;

                if (slot == FlowerSlot)
                    return Utils.Clamp(rank, 0, SmallSeedTextures.Length - 1);

                rank++;
            }

            return FlowerSlot % SmallSeedTextures.Length;
        }

        private void DrawDormantMagicGlow(Vector2 center, float opacity)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D magic = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_03").Value;
            float pulse = 0.9f + 0.1f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 2.6f + Projectile.identity);
            Color seedColor = Color.Lerp(new Color(88, 144, 84), FlowerColor, 0.34f) * opacity;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            // Glow assets are always additive so a dark or black source background cannot be rendered.
            Main.EntitySpriteDraw(bloom, center, null, seedColor * 0.28f, 0f, bloom.Size() * 0.5f, 0.085f * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(magic, center, null, Color.Lerp(seedColor, FlowerAccentColor, 0.25f) * 0.34f, -Main.GlobalTimeWrappedHourly * 0.24f, magic.Size() * 0.5f, 0.054f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        private void DrawFlowerUnderGlow(Vector2 center, Texture2D texture, Vector2 origin, float rotation, float scale, float opacity)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float pulse = 0.92f + 0.08f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 2.4f + Projectile.identity);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.EntitySpriteDraw(bloom, center, null, FlowerColor * (0.22f * opacity), 0f, bloom.Size() * 0.5f, 0.13f * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, center, null, FlowerColor * (0.26f * opacity), rotation, origin, scale * 1.08f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        private static void DrawOutline(Texture2D texture, Vector2 center, Vector2 origin, float rotation, float scale, Color color)
        {
            // Additive blend: black/dark pixels in the texture contribute nothing, only bright regions
            // show as the theme color — prevents the "black outline" artifact from dark edge pixels
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * OutlineRadius;
                Main.EntitySpriteDraw(texture, center + offset, null, color, rotation, origin, scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        private static int GetSeedContactDamage(Player owner, bool holdingBlossomFlux)
        {
            if (holdingBlossomFlux)
                return System.Math.Max(1, (int)(owner.GetWeaponDamage(owner.HeldItem) * 0.16f));

            return System.Math.Max(1, (int)owner.GetTotalDamage(DamageClass.Ranged).ApplyTo(18f));
        }

        protected static Color GetFlowerColor(BlossomFluxChloroplastPresetType preset) => preset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => new Color(255, 220, 70),
            BlossomFluxChloroplastPresetType.Chlo_BRecov => new Color(185, 255, 196),
            BlossomFluxChloroplastPresetType.Chlo_CDetec => new Color(126, 170, 255),
            BlossomFluxChloroplastPresetType.Chlo_DBomb => new Color(255, 126, 70),
            BlossomFluxChloroplastPresetType.Chlo_EPlague => new Color(178, 92, 220),
            _ => new Color(124, 255, 148)
        };

        protected static Color GetFlowerAccentColor(BlossomFluxChloroplastPresetType preset) => preset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => new Color(255, 255, 210),
            BlossomFluxChloroplastPresetType.Chlo_BRecov => new Color(240, 255, 238),
            BlossomFluxChloroplastPresetType.Chlo_CDetec => new Color(220, 244, 255),
            BlossomFluxChloroplastPresetType.Chlo_DBomb => new Color(255, 214, 132),
            BlossomFluxChloroplastPresetType.Chlo_EPlague => new Color(202, 255, 92),
            _ => new Color(220, 255, 230)
        };
    }
}
