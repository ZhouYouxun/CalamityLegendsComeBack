using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive
{
    /// <summary>
    /// When the player is holding the right-click crystal form, this small background blade
    /// floats behind the player and periodically strikes nearby enemies.
    /// </summary>
    internal sealed class YC_BackgroundBlade : ModProjectile, ILocalizedModType
    {
        private static readonly Color BladeGold = new(255, 214, 88);
        private static readonly Color BladeOrange = new(255, 111, 34);

        private const int AttackInterval = 90;   // frames between auto-attacks
        private const float AttackRange = 600f;  // pixel radius for target search
        private const float DamageRatio = 0.3f;  // fraction of owner damage

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Items/Weapons/Melee/Earth";

        private ref float Timer => ref Projectile.localAI[0];
        private float bobPhase;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = AttackInterval;
            Projectile.hide = false;
        }

        public override bool? CanDamage() => false; // damage handled manually
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || owner.HeldItem.type != ModContent.ItemType<NewLegendYharimsCrystal>())
            {
                Projectile.Kill();
                return;
            }

            // Only alive while last weapon is Crystal (right-click active form)
            YharimsCrystalStatePlayer state = owner.GetModPlayer<YharimsCrystalStatePlayer>();
            if (state.LastWeapon != YCWeaponForm.Crystal)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Timer++;
            bobPhase += 0.04f;

            // Position: float close behind the player's back (Terraprisma style hover)
            float bobY = (float)Math.Sin(bobPhase) * 6f;
            Vector2 offset = new Vector2(-owner.direction * 34f, -14f + bobY);
            Projectile.Center = owner.MountedCenter + offset;

            // Terraprisma standing upright angle (pointing UP, nearly parallel to player vertical axis)
            float sway = (float)Math.Sin(bobPhase * 0.8f) * 0.05f;
            if (owner.direction == -1)
                Projectile.rotation = MathHelper.PiOver4 + 0.06f - sway;
            else
                Projectile.rotation = -MathHelper.PiOver4 - 0.06f + sway;

            Projectile.direction = owner.direction;
            Projectile.spriteDirection = owner.direction;

            Lighting.AddLight(Projectile.Center, BladeGold.ToVector3() * 0.35f);

            // Periodic auto-attack
            if (Projectile.owner == Main.myPlayer && (int)Timer % AttackInterval == AttackInterval - 1)
                TryAutoAttack(owner, state);
        }

        private void TryAutoAttack(Player owner, YharimsCrystalStatePlayer state)
        {
            NPC target = FindNearestTarget(owner, AttackRange);
            if (target == null)
                return;

            // Calculate damage based on owner's weapon damage
            int baseDamage = owner.HeldItem.ModItem is NewLegendYharimsCrystal yc
                ? yc.GetScaledDamage(owner, new BalanceYharimsCrystal().GetLeftClickBaseDamage())
                : owner.HeldItem.damage;
            int attackDamage = Math.Max(1, (int)(baseDamage * DamageRatio));

            // Fire a quick single-hit projectile burst
            Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction);
            int proj = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                dir * 22f,
                ModContent.ProjectileType<YC_BackgroundBladeStrike>(),
                attackDamage,
                2f,
                Projectile.owner,
                target.whoAmI);

            if (Main.projectile.IndexInRange(proj))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[proj], YCWeaponForm.Blade);
                Main.projectile[proj].CritChance = owner.GetWeaponCrit(owner.HeldItem);
            }

            if (!Main.dedServ)
            {
                // Quick flash effect
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, BladeGold, Vector2.One, Projectile.rotation, 0.04f, 0.8f, 12));
                SoundEngine.PlaySound(SoundID.Item22 with { Volume = 0.35f, Pitch = 0.2f }, Projectile.Center);
            }
        }

        private static NPC FindNearestTarget(Player owner, float range)
        {
            NPC nearest = null;
            float maxDistSq = range * range;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy())
                    continue;
                float distSq = Vector2.DistanceSquared(owner.Center, npc.Center);
                if (distSq < maxDistSq)
                {
                    maxDistSq = distSq;
                    nearest = npc;
                }
            }
            return nearest;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Melee/EarthGlow").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            Player owner = Main.player[Projectile.owner];
            SpriteEffects effects = owner.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 origin = owner.direction == -1 ? new Vector2(texture.Width, texture.Height) : new Vector2(0f, texture.Height);

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float scale = 0.55f;
            float pulse = 0.88f + 0.12f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 10f);
            float opacity = 0.75f;

            // Soft bloom behind the blade
            Main.EntitySpriteDraw(bloom, drawPos, null, BladeGold with { A = 0 } * 0.32f * opacity, 0f, bloom.Size() * 0.5f, scale * 1.8f * pulse, SpriteEffects.None);

            // Terraprisma-style prismatic afterimage trail
            for (int i = 1; i <= 3; i++)
            {
                float trailBobY = (float)Math.Sin(bobPhase - i * 0.25f) * 6f;
                Vector2 trailPos = owner.MountedCenter + new Vector2(-owner.direction * 34f, -14f + trailBobY) - Main.screenPosition;
                Color trailColor = Color.Lerp(BladeGold, BladeOrange, i / 3f) with { A = 0 } * (0.28f / i) * opacity;
                Main.EntitySpriteDraw(texture, trailPos, null, trailColor, Projectile.rotation, origin, scale, effects);
            }

            // Gold outline ring (offset passes)
            Color outlineColor = BladeGold with { A = 0 };
            for (int i = 0; i < 8; i++)
            {
                Vector2 outlineOffset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 2.4f;
                Main.EntitySpriteDraw(texture, drawPos + outlineOffset, null, outlineColor * 0.35f * opacity, Projectile.rotation, origin, scale, effects);
            }

            // Main blade body
            Color bodyColor = lightColor * opacity;
            Main.EntitySpriteDraw(texture, drawPos, null, bodyColor, Projectile.rotation, origin, scale, effects);
            Main.EntitySpriteDraw(glow, drawPos, null, BladeGold * 0.75f * opacity, Projectile.rotation, origin, scale, effects);

            return false;
        }
    }

    /// <summary>
    /// Quick-travel projectile fired by YC_BackgroundBlade to deal its auto-attack damage.
    /// </summary>
    internal sealed class YC_BackgroundBladeStrike : ModProjectile, ILocalizedModType
    {
        private static readonly Color BladeGold = new(255, 214, 88);

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 40;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, BladeGold.ToVector3() * 0.45f);
            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.GoldFlame, -Projectile.velocity * 0.4f + Main.rand.NextVector2Circular(1f, 1f), 0, default, 0.9f);
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(new BalanceYharimsCrystal().GetFireDebuffType(), 120);
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(target.Center, Vector2.Zero, BladeGold, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.04f, 0.7f, 10));
                for (int i = 0; i < 6; i++)
                {
                    Dust d = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(10f, 10f), DustID.GoldFlame, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 8f), 0, default, 1.0f);
                    d.noGravity = true;
                }
            }
        }
    }
}
