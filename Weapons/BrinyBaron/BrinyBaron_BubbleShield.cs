using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    public class BrinyBaronBubbleShieldPlayer : ModPlayer
    {
        private const int BubbleCooldownFrames = 10 * 60;
        private int bubbleCooldown;

        public bool CanSpawnBubble => bubbleCooldown <= 0;

        public override void PostUpdate()
        {
            if (bubbleCooldown > 0)
                bubbleCooldown--;
        }

        public void StartCooldown() => bubbleCooldown = BubbleCooldownFrames;
    }

    public class BrinyBaron_BubbleShield : ModProjectile
    {
        private const int MaximumInitialBlocks = 2;
        private const int ReinforcedBlockDuration = 2 * 60;

        private int blockedProjectiles;
        private bool reinforcedBlockPhase;
        private int reinforcedBlockStartTime;

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.timeLeft = 30 * 60;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.damage = 0;
            Projectile.scale = 2.75f;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center;

            if (Main.zenithWorld)
            {
                Projectile.timeLeft = 10 * 60 * 60;
                BlockHostileProjectiles(int.MaxValue);
                CreateWaterEnergyParticles();
                return;
            }

            if (!reinforcedBlockPhase)
            {
                BlockHostileProjectiles(MaximumInitialBlocks - blockedProjectiles);
                if (blockedProjectiles >= MaximumInitialBlocks)
                {
                    reinforcedBlockPhase = true;
                    reinforcedBlockStartTime = (int)Main.GameUpdateCount;
                }
                return;
            }

            BlockHostileProjectiles(int.MaxValue);
            CreateWaterEnergyParticles();
            if (Main.GameUpdateCount - reinforcedBlockStartTime >= ReinforcedBlockDuration)
                Projectile.Kill();
        }

        private void BlockHostileProjectiles(int remainingBlocks)
        {
            if (remainingBlocks <= 0)
                return;

            for (int i = 0; i < Main.maxProjectiles && remainingBlocks > 0; i++)
            {
                Projectile otherProjectile = Main.projectile[i];
                if (!otherProjectile.active || !otherProjectile.hostile || !otherProjectile.Hitbox.Intersects(Projectile.Hitbox))
                    continue;

                otherProjectile.Kill();
                blockedProjectiles++;
                remainingBlocks--;
                SpawnBlockDust();
            }
        }

        private void SpawnBlockDust()
        {
            for (int i = 0; i < 10; i++)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.WaterCandle, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 150, default, 1.9f);
        }

        private void CreateWaterEnergyParticles()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 5; i++)
            {
                Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.5f, Projectile.height * 0.5f);
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1f, 2.5f);
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(position, velocity, Color.DarkBlue, 15, 0.9f, 0.5f, 0.2f, true));
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                for (int i = 0; i < 36; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(5f, 15f);
                    GeneralParticleHandler.SpawnParticle(new LineParticle(Projectile.Center + velocity.SafeNormalize(Vector2.UnitX) * 20.5f, velocity, false, 30, 1.75f, Color.Blue));
                }
            }

            SoundEngine.PlaySound(SoundID.Item54 with { Volume = 0.75f, Pitch = -0.2f }, Projectile.Center);
            Player owner = Main.player[Projectile.owner];
            if (owner.active && !owner.dead)
            {
                owner.immune = true;
                owner.immuneNoBlink = true;
                owner.immuneTime = 60;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
