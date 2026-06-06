using CalamityMod;
using CalamityLegendsComeBack.Weapons.Vesuvius.RightClick;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.DStage3
{
    public class VesuviusMagmaPillar : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/Rogue/SubductionFlameburst";

        private int frameX;
        private int frameY;

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Projectile.width = 81;
            Projectile.height = 322;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.alpha = 255;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Projectile.Center += direction * (100f + Projectile.ai[0] * 64f);
                Projectile.velocity = Vector2.Zero;
                Projectile.position.Y -= Projectile.height / 2;
                Projectile.localAI[0] = 1f;
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 7 == 6)
            {
                frameY++;
                if (frameY >= 4)
                {
                    frameX++;
                    frameY = 0;
                }

                if (frameX >= 3)
                    Projectile.Kill();
            }

            Projectile.rotation = 0f;
            Lighting.AddLight(Projectile.Center, 0.95f * VesuviusProjectileVisuals.VisualIntensity, 0.24f * VesuviusProjectileVisuals.VisualIntensity, 0.06f * VesuviusProjectileVisuals.VisualIntensity);

            if (!Main.dedServ)
            {
                Vector2 vent = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-26f, 26f), Main.rand.NextFloat(-18f, 46f));
                VesuviusProjectileVisuals.SpawnPillarTrail(Projectile, 1f + Projectile.ai[1] * 0.08f);
                VesuviusVolcanicVisuals.SpawnSubductionMix(vent, -Vector2.UnitY, true);
            }

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft % 18 == 0)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Bottom - Vector2.UnitY * 22f,
                    Vector2.Zero,
                    ModContent.ProjectileType<VesuviusMagmaResidual>(),
                    Math.Max(1, (int)(Projectile.damage * 0.28f)),
                    0f,
                    Projectile.owner,
                    0f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 240);
            target.AddBuff(BuffID.Daybreak, 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = new(frameX * Projectile.width, frameY * Projectile.height, Projectile.width, Projectile.height);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, Projectile.Size / 2f, 1f, SpriteEffects.None);
            return false;
        }
    }

    public class VesuviusMagmaResidual : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.rotation = Projectile.ai[0];
            if (!Main.dedServ)
                VesuviusProjectileVisuals.SpawnPillarResidual(Projectile.Center, Projectile.rotation, 0.82f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 120);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smoke = ModContent.Request<Texture2D>("CalamityMod/Particles/HighResFoggyCircleHardEdge").Value;
            float fade = Projectile.timeLeft / 18f;
            Main.EntitySpriteDraw(
                smoke,
                Projectile.Center - Main.screenPosition - Vector2.UnitY * 5f,
                null,
                Color.Lerp(Color.Black, VesuviusProjectileVisuals.RavagerSmoke, 0.5f) * 0.18f * fade * VesuviusProjectileVisuals.VisualIntensity,
                Projectile.rotation,
                smoke.Size() * 0.5f,
                new Vector2(0.55f, 0.24f) * (1f + (1f - fade) * 0.55f) * VesuviusProjectileVisuals.VisualScale,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                VesuviusProjectileVisuals.LavaOrange with { A = 0 } * 0.16f * fade * VesuviusProjectileVisuals.VisualIntensity,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                new Vector2(0.42f, 0.18f) * (1f + (1f - fade) * 0.4f) * VesuviusProjectileVisuals.VisualScale,
                SpriteEffects.None);
            return false;
        }
    }
}
