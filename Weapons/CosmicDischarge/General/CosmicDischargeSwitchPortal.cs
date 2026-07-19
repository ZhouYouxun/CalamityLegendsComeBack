using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeSwitchPortal : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float TargetMode => ref Projectile.ai[0];
        private ref float Time => ref Projectile.localAI[0];
        private Player Owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.timeLeft = 18;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<NewLegendCosmicDischarge>())
            {
                Projectile.Kill();
                return;
            }

            Time++;
            Projectile.Center = Owner.Top + new Vector2(0f, -14f);
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;

            CosmicDischargeCommon.SpawnSwitchPortalAI(Owner, Projectile.Center, Time, (CosmicDischargeAttackMode)(int)TargetMode);

            if (Time == 1f)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DemonSwordKillMode") { Volume = 0.78f, Pitch = 0.15f, MaxInstances = 2 }, Owner.Center);
            }

            if (Time == 8f && Main.myPlayer == Projectile.owner)
            {
                Owner.GetModPlayer<CosmicDischargePlayer>().SetAttackMode((CosmicDischargeAttackMode)(int)TargetMode);
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftOpen") { Volume = 0.45f, Pitch = 0.35f, MaxInstances = 2 }, Owner.Center);
                Projectile.netUpdate = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D portal = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/StreamGougePortal").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
            Vector2 portalOrigin = portal.Size() * 0.5f;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            float open = Utils.GetLerpValue(0f, 6f, Time, true);
            float close = Utils.GetLerpValue(18f, 11f, Time, true);
            float scale = MathF.Sin(MathHelper.PiOver2 * MathHelper.Clamp(open * close, 0f, 1f)) * 1.15f;
            float opacity = MathHelper.Clamp(open * close, 0f, 1f);
            float rotation = Main.GlobalTimeWrappedHourly * 8f + Projectile.identity * 1.45f;
            Color modeColor = CosmicDischargeCommon.GetModeColor((CosmicDischargeAttackMode)(int)TargetMode);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null, CosmicDischargeCommon.Transparent(modeColor) * 0.25f * opacity, 0f, bloomOrigin, scale * 0.72f, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, Color.Black * 0.55f * opacity, rotation, portalOrigin, scale * 1.35f, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.RiftLightBlue) * 0.9f * opacity, rotation * 0.6f, portalOrigin, scale * 1.35f, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.RiftMagenta) * 0.9f * opacity, -rotation * 0.7f, portalOrigin, scale * 1.35f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
