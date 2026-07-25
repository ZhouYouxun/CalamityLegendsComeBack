using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;


namespace CalamityLegendsComeBack.Weapons.GaelsGreatsword
{
    internal sealed class GaelGreatswordEmberUI : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color EmptyColor = GaelGreatswordVisuals.CrimsonViolet;
        private static readonly Color FilledColor = new(190, 22, 54);
        private static readonly Color ReadyColor = new(244, 202, 92);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || owner.HeldItem.type != ModContent.ItemType<NewLegendGaelsGreatsword>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Top + new Vector2(0f, -28f);
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            GaelGreatswordPlayer gaelPlayer = owner.GetModPlayer<GaelGreatswordPlayer>();
            Texture2D barBack = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barFront = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;
            float progress = gaelPlayer.DarkEmberRatio;
            bool ready = gaelPlayer.DarkEmberReady;
            float flash = Utils.GetLerpValue(0f, 18f, gaelPlayer.DarkEmberFlashTimer, true);
            float pulse = ready ? 0.86f + MathF.Sin(Main.GlobalTimeWrappedHourly * 8f) * 0.12f : 0.78f + flash * 0.18f;
            Color barColor = ready
                ? Color.Lerp(FilledColor, ReadyColor, 0.58f + MathF.Sin(Main.GlobalTimeWrappedHourly * 5f) * 0.16f)
                : Color.Lerp(EmptyColor, FilledColor, progress);

            Vector2 barPosition = owner.Top - Main.screenPosition + new Vector2(-barBack.Width * 0.5f, -34f);
            Rectangle frame = new(0, 0, (int)MathF.Ceiling(barFront.Width * progress), barFront.Height);

            Main.spriteBatch.Draw(barBack, barPosition, Color.Black * 0.58f);
            if (frame.Width > 0)
                Main.spriteBatch.Draw(barFront, barPosition, frame, barColor * pulse);

            if (gaelPlayer.GuardCooldown > 0 || gaelPlayer.GuardFlashTimer > 0)
            {
                float guardProgress = 1f - gaelPlayer.GuardCooldownRatio;
                float guardFlash = Utils.GetLerpValue(0f, 18f, gaelPlayer.GuardFlashTimer, true);
                Vector2 guardBarPosition = barPosition + new Vector2(0f, barBack.Height + 3f);
                Rectangle guardFrame = new(0, 0, (int)MathF.Ceiling(barFront.Width * guardProgress), barFront.Height);
                Color guardColor = Color.Lerp(GaelGreatswordVisuals.CrimsonViolet, new Color(236, 64, 82), 0.45f + guardFlash * 0.3f);

                Main.spriteBatch.Draw(barBack, guardBarPosition, Color.Black * 0.45f);
                if (guardFrame.Width > 0)
                    Main.spriteBatch.Draw(barFront, guardBarPosition, guardFrame, guardColor * (0.62f + guardFlash * 0.28f));
            }

            if (ready || flash > 0f)
            {
                Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
                Vector2 center = barPosition + new Vector2(barBack.Width * 0.5f, barBack.Height * 0.5f);
                Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
                Main.EntitySpriteDraw(bloom, center, null, barColor with { A = 0 } * (ready ? 0.22f : 0.12f) * Math.Max(flash, 0.5f),
                    0f, bloom.Size() * 0.5f, ready ? 0.42f : 0.25f + flash * 0.14f, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
            }

            return false;
        }
    }
}
