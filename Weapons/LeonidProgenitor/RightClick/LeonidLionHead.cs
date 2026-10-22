using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    // The former lion-head attack is now only the stationary impact stage of the charged meteor.
    public class LeonidLionHead : ModProjectile, ILocalizedModType
    {
        private const int Duration = 16;
        private bool initialized;
        private float BurstScale => 2.6f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 104;
            Projectile.friendly = true;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Duration;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        // One brief area strike; the rest of the lifespan is the original burst's visual tail.
        public override bool? CanDamage() => Projectile.timeLeft >= Duration - 2 ? null : false;

        public override void AI()
        {
            float progress = 1f - Projectile.timeLeft / (float)Duration;
            Projectile.scale = MathHelper.Lerp(1f, BurstScale, progress);
            Color gold = LeonidVisualUtils.GetReadyGold(Projectile.whoAmI * 0.3f);
            Lighting.AddLight(Projectile.Center, gold.ToVector3() * (1f - progress));

            if (initialized)
                return;
            initialized = true;
            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = (int)(40f * BurstScale);
            Projectile.Center = center;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Zombie88 with { Volume = 0.95f, Pitch = -0.3f }, center);
                SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.9f, Pitch = -0.2f }, center);
                if (Projectile.owner == Main.myPlayer)
                    global::CalamityLegendsComeBack.Systems.ModScreenShake.Max(Main.LocalPlayer, center, 6f);

                LeonidStarlight.Ellipse(center, 14, 92f, 58f, gold, LeonidStarlightShape.Shard,
                    speed: 9f, scale: 1.15f, hoverTime: 22, lifetime: 175, lanceSpeed: 21f);
                LeonidStarlight.Ellipse(center, 12, 62f, 38f, Color.Lerp(gold, LeonidVisualUtils.MoonWhite, 0.4f),
                    LeonidStarlightShape.Mote, speed: 5f, scale: 1f, hoverTime: 30, lifetime: 160,
                    lanceSpeed: 18f, angleOffset: MathHelper.Pi / 12f);
                LeonidStarlight.Spawn(center, Vector2.Zero, gold,
                    LeonidStarlightShape.Halo, 1.6f, 10, 74, 11f, linksToSiblings: false);
            }

            if (Projectile.owner != Main.myPlayer)
                return;

            // A single shockwave at impact, including tile impacts. Never multiply it by NPC count.
            if (Main.player[Projectile.owner].GetModPlayer<LeonidConstellationPlayer>().IsUnlocked(LeonidStar.RLeonis))
            {
                int shockwave = Projectile.NewProjectile(Projectile.GetSource_FromThis(), center, Vector2.Zero,
                    ModContent.ProjectileType<CalamityMod.Projectiles.Melee.EarthenTidesShockwave>(),
                    (int)(Projectile.damage * 0.75f), 0f, Projectile.owner, 1.5f);
                if (shockwave.WithinBounds(Main.maxProjectiles))
                {
                    Main.projectile[shockwave].DamageType = ModContent.GetInstance<RogueDamageClass>();
                    Main.projectile[shockwave].friendly = true;
                    Main.projectile[shockwave].hostile = false;
                }
            }

            // Thirteen orbiting nodes surround Regulus in the constellation chart.
            int count = LeonidConstellation.Nodes.Count - 1;
            int splitDamage = (int)(Projectile.damage * 0.45f);
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = (i * MathHelper.TwoPi / count).ToRotationVector2() * Main.rand.NextFloat(11.5f, 17f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), center, velocity,
                    ModContent.ProjectileType<LeonidReflectiveMeteor>(), splitDamage,
                    Projectile.knockBack * 0.4f, Projectile.owner, 0f, 0f, LeonidReflectiveMeteor.StealthFloaterFlag + Main.rand.Next(3));
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;
            Color color = LeonidVisualUtils.GetReadyGold(Projectile.whoAmI * 0.3f);
            LeonidVisualUtils.SpawnDustBurst(Projectile.Center, color, 28, 8f, 1.3f);
            LeonidVisualUtils.SpawnBloomBurst(Projectile.Center, color * 0.5f, 0.9f, 18);
            LeonidVisualUtils.SpawnCelestialPulse(Projectile.Center, Vector2.UnitY, color, 1.45f, 24);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float fade = Projectile.timeLeft / (float)Duration;
            Color gold = LeonidVisualUtils.GetReadyGold(Projectile.whoAmI * 0.3f);
            LeonidVisualUtils.BeginAdditiveSpriteBatch();
            LeonidVisualUtils.DrawBloom(Projectile.Center, gold * fade, Projectile.scale * 0.5f);
            LeonidVisualUtils.DrawSparkle(Projectile.Center, LeonidVisualUtils.MoonWhite,
                0.46f * fade, 0.54f * Projectile.scale, Main.GlobalTimeWrappedHourly);
            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();
            LeonidStarlight.DrawSunburst(Projectile.Center, gold, 0.5f * fade,
                0.07f * Projectile.scale, Main.GlobalTimeWrappedHourly * 0.9f);
            LeonidStarlight.DrawFlare(Projectile.Center, LeonidVisualUtils.MoonWhite,
                0.7f * fade, 0.2f * Projectile.scale, -Main.GlobalTimeWrappedHourly * 2f);
            return false;
        }
    }
}
