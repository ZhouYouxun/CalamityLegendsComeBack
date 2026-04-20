using System;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill
{
    internal sealed class BFUltimateField : ModProjectile, ILocalizedModType
    {
        public const int FieldRadius = 16 * 16;
        public const int FieldDiameter = FieldRadius * 2;
        public const int FieldLifetimeSeconds = 15;
        public const int FieldLifetimeFrames = FieldLifetimeSeconds * 60;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private BlossomFluxChloroplastPresetType Preset => (BlossomFluxChloroplastPresetType)(int)Projectile.ai[0];
        private Color MainColor => BFArrowCommon.GetPresetColor(Preset);
        private Color AccentColor => BFArrowCommon.GetPresetAccentColor(Preset);

        public override void SetDefaults()
        {
            Projectile.width = FieldDiameter;
            Projectile.height = FieldDiameter;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = FieldLifetimeFrames;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[1]++;

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                PlayFieldActivationSounds();

                if (Main.myPlayer == Projectile.owner && Preset == BlossomFluxChloroplastPresetType.Chlo_ABreak)
                    ApplyBreakthroughBurst(owner);
            }

            if (Projectile.timeLeft == 45)
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.4f, Pitch = -0.2f }, Projectile.Center);

            RefreshEffects();
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.45f, Pitch = -0.1f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            SpriteBatch spriteBatch = Main.spriteBatch;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_03").Value;
            Texture2D flare = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/flare_01").Value;
            Texture2D slash = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/slash_01").Value;
            Texture2D flower = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/flower_014").Value;
            Texture2D energy = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/fx_EnergyBolt7").Value;

            float fadeIn = Utils.GetLerpValue(0f, 18f, Projectile.localAI[1], true);
            float fadeOut = Utils.GetLerpValue(0f, 28f, Projectile.timeLeft, true);
            float opacity = fadeIn * fadeOut;
            float pulse = 0.96f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.2f + Projectile.whoAmI * 0.35f);
            float time = Main.GlobalTimeWrappedHourly;
            float ringScale = Projectile.width / (float)ring.Width * 1.08f;
            float bloomScale = Projectile.width / (float)bloom.Width * 1.08f;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            BeginAdditiveBatch(spriteBatch);

            DrawAdditive(ring, drawPosition, MainColor * opacity * 0.16f, time * 0.55f, ringScale * pulse);
            DrawAdditive(bloom, drawPosition, MainColor * opacity * 0.1f, 0f, bloomScale * 0.72f);

            switch (Preset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                {
                    DrawAdditive(ring, drawPosition, AccentColor * opacity * 0.13f, -time * 0.75f, ringScale * 0.78f);
                    DrawAdditive(energy, drawPosition, AccentColor * opacity * 0.16f, time * 1.3f, ringScale * 0.21f);

                    for (int i = 0; i < 6; i++)
                    {
                        float shardRotation = time * 1.05f + MathHelper.TwoPi / 6f * i;
                        Vector2 shardOffset = shardRotation.ToRotationVector2() * Projectile.width * 0.16f;
                        DrawAdditive(slash, drawPosition + shardOffset, MainColor * opacity * 0.18f, shardRotation + MathHelper.PiOver2, ringScale * 0.16f);
                    }

                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                {
                    DrawAdditive(ring, drawPosition, AccentColor * opacity * 0.12f, -time * 0.45f, ringScale * 0.84f);
                    DrawAdditive(flower, drawPosition, AccentColor * opacity * 0.18f, time * 0.7f, ringScale * 0.7f);
                    DrawAdditive(flower, drawPosition, MainColor * opacity * 0.12f, -time * 0.45f, ringScale * 0.5f);
                    DrawAdditive(bloom, drawPosition, AccentColor * opacity * 0.11f, 0f, bloomScale * 0.44f * pulse);
                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                {
                    DrawAdditive(ring, drawPosition, AccentColor * opacity * 0.12f, -time * 1.55f, ringScale * 0.88f);
                    DrawAdditive(flare, drawPosition, AccentColor * opacity * 0.14f, 0f, ringScale * 0.66f);
                    DrawAdditive(flare, drawPosition, AccentColor * opacity * 0.11f, MathHelper.PiOver2, ringScale * 0.66f);
                    DrawAdditive(energy, drawPosition, MainColor * opacity * 0.14f, -time * 1.25f, ringScale * 0.18f);
                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                {
                    DrawAdditive(ring, drawPosition, MainColor * opacity * 0.2f, time * 0.95f, ringScale * MathHelper.Lerp(0.82f, 1.04f, 0.5f + 0.5f * (float)Math.Sin(time * 6f)));
                    DrawAdditive(flare, drawPosition, AccentColor * opacity * 0.2f, 0f, ringScale * 0.78f * pulse);
                    DrawAdditive(energy, drawPosition, MainColor * opacity * 0.18f, time * 1.55f, ringScale * 0.2f);
                    DrawAdditive(bloom, drawPosition, MainColor * opacity * 0.12f, 0f, bloomScale * 0.58f);
                    break;
                }

                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                {
                    DrawAdditive(ring, drawPosition, AccentColor * opacity * 0.09f, -time * 0.9f, ringScale * 0.9f);
                    DrawAdditive(flower, drawPosition, MainColor * opacity * 0.12f, time * 0.82f, ringScale * 0.62f);
                    DrawAdditive(energy, drawPosition, AccentColor * opacity * 0.14f, -time * 1.15f, ringScale * 0.18f);

                    for (int i = 0; i < 3; i++)
                    {
                        float moteRotation = -time * 0.8f + MathHelper.TwoPi / 3f * i;
                        Vector2 moteOffset = moteRotation.ToRotationVector2() * Projectile.width * 0.14f;
                        DrawAdditive(bloom, drawPosition + moteOffset, MainColor * opacity * 0.08f, 0f, bloomScale * 0.16f);
                    }

                    break;
                }
            }

            BeginAlphaBatch(spriteBatch);

            return false;
        }

        private void RefreshEffects()
        {
            Rectangle fieldBox = Projectile.Hitbox;
            for (int npcIndex = 0; npcIndex < Main.maxNPCs; npcIndex++)
            {
                NPC npc = Main.npc[npcIndex];
                if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.lifeMax <= 5)
                    continue;

                if (!fieldBox.Intersects(npc.Hitbox))
                    continue;

                npc.GetGlobalNPC<BFUltimateFieldGlobalNPC>().RefreshFromField(Projectile.owner, Preset, Projectile.damage, npc.Center);
            }

            for (int playerIndex = 0; playerIndex < Main.maxPlayers; playerIndex++)
            {
                Player targetPlayer = Main.player[playerIndex];
                if (!targetPlayer.active || targetPlayer.dead)
                    continue;

                if (!fieldBox.Contains(targetPlayer.Center.ToPoint()))
                    continue;

                targetPlayer.GetModPlayer<BFUltimateFieldPlayer>().RefreshFromField(Preset);
            }
        }

        private void ApplyBreakthroughBurst(Player owner)
        {
            Rectangle fieldBox = Projectile.Hitbox;
            int burstDamage = Math.Max(1, Projectile.damage * 10);

            for (int npcIndex = 0; npcIndex < Main.maxNPCs; npcIndex++)
            {
                NPC npc = Main.npc[npcIndex];
                if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.lifeMax <= 5)
                    continue;

                if (!fieldBox.Intersects(npc.Hitbox))
                    continue;

                owner.ApplyDamageToNPC(npc, burstDamage, 0f, owner.direction, false);
            }
        }

        private void PlayFieldActivationSounds()
        {
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.58f, Pitch = -0.12f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.45f, Pitch = 0.16f }, Projectile.Center);
        }

        private static void DrawAdditive(Texture2D texture, Vector2 position, Color color, float rotation, float scale)
        {
            Main.EntitySpriteDraw(
                texture,
                position,
                null,
                color,
                rotation,
                texture.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0);
        }

        private static void BeginAdditiveBatch(SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);
        }

        private static void BeginAlphaBatch(SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(
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
