using CalamityLegendsComeBack.Weapons.Vesuvius.Core;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.Passive
{
    public class VesuviusPassivePlayer : ModPlayer
    {
        public const int MaxAshSouls = 6;
        public const int AfterflameWindow = 90;
        private const int ManaPerAshSoul = 400;

        public int LeftClickCooldown;
        public int EmpoweredLeftTimer;
        public int MeteorFollowupTimer;
        public int AshSouls { get; private set; }

        private bool holdingVesuvius;
        private int ashTimer;
        private int spentMana;

        public override void ResetEffects()
        {
            holdingVesuvius = false;
        }

        public override void PostUpdate()
        {
            if (LeftClickCooldown > 0)
                LeftClickCooldown--;
            if (EmpoweredLeftTimer > 0)
                EmpoweredLeftTimer--;
            if (MeteorFollowupTimer > 0)
                MeteorFollowupTimer--;

            if (!holdingVesuvius)
            {
                ashTimer = 0;
                return;
            }

            if (Player.statManaMax2 < 100)
                Player.statManaMax2 += 100;
            Player.fireWalk = true;
            Player.lavaImmune = true;
            Player.buffImmune[BuffID.OnFire] = true;
            Player.buffImmune[BuffID.OnFire3] = true;
            Player.noFallDmg = true;
            SpawnAmbientAsh();
            MaintainAshSoulVisuals();
        }

        public override void UpdateDead()
        {
            LeftClickCooldown = 0;
            EmpoweredLeftTimer = 0;
            MeteorFollowupTimer = 0;
            AshSouls = 0;
            spentMana = 0;
        }

        public void SetHoldingVesuvius()
        {
            holdingVesuvius = true;
        }

        public override void OnConsumeMana(Item item, int manaConsumed)
        {
            if (item?.ModItem is not NewVesuvius || manaConsumed <= 0)
                return;

            spentMana += manaConsumed;
            while (spentMana >= ManaPerAshSoul)
            {
                spentMana -= ManaPerAshSoul;
                AddAshSoul();
            }
        }

        public void GrantAfterflameWindow()
        {
            EmpoweredLeftTimer = AfterflameWindow;
            MeteorFollowupTimer = AfterflameWindow;
        }

        public bool TryConsumeEmpoweredLeft()
        {
            if (EmpoweredLeftTimer <= 0)
                return false;

            EmpoweredLeftTimer = 0;
            return true;
        }

        public bool TryConsumeMeteorFollowup()
        {
            if (MeteorFollowupTimer <= 0)
                return false;

            MeteorFollowupTimer = 0;
            return true;
        }

        public bool TryConsumeAshVolley()
        {
            if (AshSouls < MaxAshSouls)
                return false;

            AshSouls = 0;
            return true;
        }

        public void AddAshSoul()
        {
            if (AshSouls >= MaxAshSouls)
                return;

            AshSouls++;
            if (Main.dedServ || Player.whoAmI != Main.myPlayer)
                return;

            Color soulColor = Color.Lerp(new Color(255, 72, 28), new Color(193, 78, 255), 0.34f);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Player.Center,
                Vector2.Zero,
                soulColor,
                "CalamityMod/Particles/SmallBloomRingLayered",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.08f,
                0.72f,
                18,
                true));
            SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.36f, Pitch = 0.18f + AshSouls * 0.035f }, Player.Center);
        }

        private void SpawnAmbientAsh()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            ashTimer++;
            if (ashTimer < 11)
                return;

            ashTimer = 0;
            int damage = 1;
            if (Player.HeldItem?.ModItem is NewVesuvius)
                damage = Math.Max(1, (int)(Player.GetWeaponDamage(Player.HeldItem) * 0.12f));

            Vector2 spawnPosition = Player.Center + new Vector2(Main.rand.NextFloat(-86f, 86f), Main.rand.NextFloat(-74f, -28f));
            Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.45f, 0.45f), Main.rand.NextFloat(0.45f, 1.05f));
            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                spawnPosition,
                velocity,
                ModContent.ProjectileType<VesuviusAshFall>(),
                damage,
                0f,
                Player.whoAmI,
                Main.rand.NextFloatDirection());
        }

        private void MaintainAshSoulVisuals()
        {
            if (Player.whoAmI != Main.myPlayer || AshSouls <= 0)
                return;

            int visualType = ModContent.ProjectileType<VesuviusAshSoulVisual>();
            for (int slot = 0; slot < AshSouls; slot++)
            {
                bool exists = false;
                foreach (Projectile projectile in Main.ActiveProjectiles)
                {
                    if (projectile.owner == Player.whoAmI && projectile.type == visualType && (int)projectile.ai[0] == slot)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, visualType, 0, 0f, Player.whoAmI, slot);
            }
        }
    }

    public class VesuviusAshFall : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.velocity.X += Projectile.ai[0] * 0.012f;
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.04f, 0.8f, 5.5f);
            Projectile.rotation += Projectile.velocity.X * 0.02f + 0.025f;
            Projectile.alpha = (int)MathHelper.Lerp(0f, 220f, Utils.GetLerpValue(42f, 0f, Projectile.timeLeft, true));

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Particle smoke = new HeavySmokeParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    Projectile.velocity * Main.rand.NextFloat(0.04f, 0.18f) + Main.rand.NextVector2Circular(0.12f, 0.12f),
                    Color.Lerp(new Color(58, 50, 44), new Color(92, 76, 60), Main.rand.NextFloat()),
                    Main.rand.Next(12, 24),
                    Main.rand.NextFloat(0.06f, 0.14f),
                    0.52f,
                    Main.rand.NextFloat(-0.04f, 0.04f),
                    false,
                    required: false);
                GeneralParticleHandler.SpawnParticle(smoke);

                if (Main.rand.NextBool(3))
                {
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                        DustID.Smoke,
                        Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.18f, 0.18f),
                        175,
                        Color.Lerp(new Color(54, 46, 38), new Color(78, 64, 50), Main.rand.NextFloat()),
                        Main.rand.NextFloat(0.14f, 0.32f));
                    dust.noGravity = true;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            VesuviusCombatSystem.ApplyVolcanicCalamity(target);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
