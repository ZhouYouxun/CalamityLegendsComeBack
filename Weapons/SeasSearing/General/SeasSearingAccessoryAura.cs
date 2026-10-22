using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // A separate hit source keeps the equipped aura independent of the held gun's immunity.
    internal sealed class SeasSearingAccessoryAura : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private Player Owner => Main.player[Projectile.owner];
        private SeasSearingPlayer State => Owner.GetModPlayer<SeasSearingPlayer>();
        private float radius;
        private int visualTimer;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = SS_Balance.AccessoryAuraMinimumHitInterval;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool PreDraw(ref Color lightColor)
        {
            if (State.EquippedSeasSearing && !State.HidePressureVisuals)
                DrawPressureField();
            return false;
        }

        internal static int GetHitInterval(Player player, Item weapon)
        {
            int baseFrames = weapon.ModItem is SeasSearing
                ? SS_Balance.GetLeftClickShotIntervalFrames(SS_Balance.GetLeftClickPhase())
                : Math.Max(1, weapon.useTime);
            int interval = CombinedHooks.TotalUseTime(baseFrames, player, weapon);
            return Math.Max(SS_Balance.AccessoryAuraMinimumHitInterval, interval);
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || !State.EquippedSeasSearing)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.Center = Owner.Center;
            radius = State.PressureRadius;

            Item held = Owner.HeldItem;
            bool hasWeapon = held != null && !held.IsAir && held.damage > 0;
            Projectile.damage = hasWeapon
                ? Math.Max(1, (int)Math.Round(Owner.GetWeaponDamage(held) * SS_Balance.AccessoryAuraDamageRatio))
                : 0;
            if (hasWeapon)
            {
                Projectile.DamageType = held.DamageType;
                Projectile.CritChance = (int)Owner.GetWeaponCrit(held);
                Projectile.ArmorPenetration = (int)Owner.GetTotalArmorPenetration(held.DamageType) + held.ArmorPenetration;
                Projectile.localNPCHitCooldown = GetHitInterval(Owner, held);
            }

            if (!Main.dedServ && !State.HidePressureVisuals && ++visualTimer >= 30)
            {
                visualTimer = 0;
                for (int i = 0; i < 12; i++)
                {
                    Vector2 direction = (MathHelper.TwoPi * i / 12f + Main.GlobalTimeWrappedHourly * 0.25f).ToRotationVector2();
                    Dust dust = Dust.NewDustPerfect(Owner.Center + direction * radius,
                        DustID.GemEmerald, -direction * 0.6f, 170,
                        Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.RadioactiveCyan, i / 12f), 0.7f);
                    dust.noGravity = true;
                }
            }
        }

        public override bool? CanDamage() => Projectile.damage > 0 && State.EquippedSeasSearing;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.DistanceSquared(Projectile.Center, nearest) <= radius * radius;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            float proximity = 1f - MathHelper.Clamp(Vector2.Distance(Owner.Center, target.Center) / radius, 0f, 1f);
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(
                target, Projectile.owner, proximity > 0.72f ? 2 : 1, 7 * 60);
            State.OnHitWithSeasSearing();
        }
        private void DrawPressureField()
        {
            float power = State.PressureVisualPower;
            if (power <= 0.02f || Main.dedServ) return;

            Texture2D ring  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2   center = Owner.Center - Main.screenPosition;

            Color fieldColor = State.PressureFieldColor;

            const float fieldOpacity = 0.5f;
            Color cyan  = (fieldColor with { A = 0 }) * ((0.12f + power * 0.22f) * fieldOpacity);
            Color deep  = (SeasSearingPalette.DeepBlue with { A = 0 }) * ((0.1f + power * 0.18f) * fieldOpacity);

            Main.EntitySpriteDraw(bloom, center, null, deep * 0.55f, 0f, bloom.Size() * 0.5f,
                new Vector2(1.9f, 1.25f) * power * 0.72f, SpriteEffects.None, 0);

            for (int i = 0; i < 3; i++)
            {
                float local    = i / 3f;
                float rotation = Main.GlobalTimeWrappedHourly * (0.28f + local * 0.14f);
                float scale    = 1.65f + local * 0.74f + power * 0.35f;
                Main.EntitySpriteDraw(ring, center, null, (i == 0 ? cyan : deep) * (1f - local * 0.22f),
                    rotation, ring.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }
        }
    }
}
