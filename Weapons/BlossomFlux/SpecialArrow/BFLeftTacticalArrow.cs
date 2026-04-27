using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    internal class BFLeftTacticalArrow : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private BlossomFluxChloroplastPresetType Preset => Projectile.ai[0] switch
        {
            1f => BlossomFluxChloroplastPresetType.Chlo_BRecov,
            2f => BlossomFluxChloroplastPresetType.Chlo_CDetec,
            3f => BlossomFluxChloroplastPresetType.Chlo_DBomb,
            4f => BlossomFluxChloroplastPresetType.Chlo_EPlague,
            _ => BlossomFluxChloroplastPresetType.Chlo_ABreak
        };

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.arrow = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 1;
            BFArrowCommon.ForceLocalNPCImmunity(Projectile, 10);
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.noDropItem = true;
            BFArrowCommon.TagBlossomFluxLeftArrow(Projectile);

            if (Preset == BlossomFluxChloroplastPresetType.Chlo_DBomb)
            {
                Projectile.tileCollide = false;
                Projectile.timeLeft = 105;
            }
        }

        public override void AI()
        {
            BlossomFluxChloroplastPresetType preset = Preset;
            Lighting.AddLight(Projectile.Center, BFArrowCommon.GetPresetColor(preset).ToVector3() * 0.42f);

            if (preset == BlossomFluxChloroplastPresetType.Chlo_ABreak && Projectile.velocity.LengthSquared() > 0.01f)
            {
                float speed = MathHelper.Clamp(Projectile.velocity.Length() + 0.06f, 0f, 24f);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
            }

            BFArrowCommon.FaceForward(Projectile);
            BFArrowCommon.EmitPresetTrail(Projectile, preset, preset == BlossomFluxChloroplastPresetType.Chlo_DBomb ? 0.92f : 0.78f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            BlossomFluxChloroplastPresetType preset = Preset;
            if (preset == BlossomFluxChloroplastPresetType.Chlo_EPlague)
            {
                target.AddBuff(BuffID.Poisoned, 180);
                target.AddBuff(BuffID.Venom, 120);
            }

            BFArrowCommon.EmitPresetBurst(Projectile, preset, 5, 0.6f, 1.8f, 0.65f, 0.95f);
        }

        public override void OnKill(int timeLeft)
        {
            BFArrowCommon.EmitPresetBurst(Projectile, Preset, 6, 0.7f, 2.2f, 0.7f, 1f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            BlossomFluxChloroplastPresetType preset = Preset;
            Texture2D texture = ModContent.Request<Texture2D>(BFArrowCommon.GetTexturePathForPreset(preset)).Value;
            Color mainColor = Projectile.GetAlpha(BFArrowCommon.GetPresetColor(preset));
            Color accentColor = Projectile.GetAlpha(BFArrowCommon.GetPresetAccentColor(preset));
            Vector2 drawOrigin = texture.Size() * 0.5f;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                if (completion <= 0f)
                    continue;

                Main.EntitySpriteDraw(
                    texture,
                    Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
                    null,
                    mainColor * (0.15f * completion),
                    Projectile.oldRot[i],
                    drawOrigin,
                    Projectile.scale * MathHelper.Lerp(0.82f, 1f, completion),
                    SpriteEffects.None,
                    0);
            }

            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 1.25f;
                Main.EntitySpriteDraw(
                    texture,
                    Projectile.Center - Main.screenPosition + offset,
                    null,
                    accentColor * 0.42f,
                    Projectile.rotation,
                    drawOrigin,
                    Projectile.scale,
                    SpriteEffects.None,
                    0);
            }

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                Projectile.GetAlpha(lightColor),
                Projectile.rotation,
                drawOrigin,
                Projectile.scale,
                SpriteEffects.None,
                0);

            return false;
        }
    }
}
