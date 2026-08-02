using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.Passive
{
    public sealed class VesuviusAshSoulVisual : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/DPreDog/RuinousSoul_OrbitGhost";

        private int Slot => (int)Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.scale = 0.85f;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!Main.player.IndexInRange(Projectile.owner))
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[Projectile.owner];
            VesuviusPassivePlayer state = owner.GetModPlayer<VesuviusPassivePlayer>();
            if (!owner.active || owner.dead || Slot < 0 || Slot >= state.AshSouls)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            float angle = Main.GlobalTimeWrappedHourly * 1.7f + Slot * MathHelper.TwoPi / VesuviusPassivePlayer.MaxAshSouls;
            float radius = 42f + (Slot % 2) * 7f;
            Projectile.Center = owner.MountedCenter + new Vector2(radius, 0f).RotatedBy(angle) +
                Vector2.UnitY * ((float)Math.Sin(angle * 1.7f) * 7f - 8f);
            Projectile.rotation = angle + MathHelper.PiOver2;
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.42f, 0.12f, 0.25f));
        }

        public override Color? GetAlpha(Color lightColor)
        {
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f + Slot);
            return Color.Lerp(new Color(255, 74, 34), new Color(193, 74, 255), 0.28f + pulse * 0.22f);
        }
    }

    /// <summary>
    /// 六枚灰烬魂火会围绕主火球共同前进。主火球消失后，它们保留当时的切线速度散开，
    /// 不会突然停在原地，也不会脱离主弹后立即获得不受控的强追踪。
    /// </summary>
    public sealed class VesuviusAshSoulBolt : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/DPreDog/RuinousSoul_OrbitGhost";

        private int ParentIndex => (int)Projectile.ai[0];
        private int Slot => (int)Projectile.ai[1];
        private int ParentIdentity => (int)Projectile.ai[2];
        private bool Released => Projectile.localAI[1] > 0f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 240;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 22;
            Projectile.scale = 0.8f;
        }

        public override bool ShouldUpdatePosition() => Released;

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            if (!Released && Main.projectile.IndexInRange(ParentIndex))
            {
                Projectile parent = Main.projectile[ParentIndex];
                if (parent.active && parent.owner == Projectile.owner && parent.identity == ParentIdentity &&
                    parent.type == ModContent.ProjectileType<LeftClick.VesuviusArcOrb>())
                {
                    float angle = Projectile.localAI[0] * 0.18f + Slot * MathHelper.TwoPi / VesuviusPassivePlayer.MaxAshSouls;
                    Vector2 orbit = new Vector2(26f + parent.scale * 8f, 0f).RotatedBy(angle);
                    Vector2 oldCenter = Projectile.Center;
                    Projectile.Center = parent.Center + orbit;
                    Projectile.velocity = Projectile.Center - oldCenter;
                    Projectile.rotation = Projectile.velocity.ToRotation();
                    Lighting.AddLight(Projectile.Center, new Vector3(0.45f, 0.12f, 0.22f));
                    return;
                }
            }

            if (!Released)
            {
                Projectile.localAI[1] = 1f;
                Vector2 fallback = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 13f;
                Projectile.velocity = fallback.RotatedBy((Slot - 2.5f) * 0.055f);
                Projectile.tileCollide = true;
                Projectile.netUpdate = true;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 0.997f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.38f, 0.1f, 0.2f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color soulColor = Color.Lerp(new Color(255, 76, 30), new Color(202, 86, 255), 0.34f);
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], soulColor, 1);
            return false;
        }
    }
}
