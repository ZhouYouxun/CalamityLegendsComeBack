using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot;
using CalamityMod;
using CalamityMod.Items.Weapons.Ranged;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle
{
    public sealed class DesertEagleHellbornGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        private int explosionCooldown;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) =>
            entity.type == ModContent.ProjectileType<DesertEagleHoldout>();

        public override void PostAI(Projectile projectile)
        {
            if (explosionCooldown > 0)
                explosionCooldown--;
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.ai[2] != 0f || explosionCooldown > 0)
                return;

            Player owner = Main.player[projectile.owner];
            if (!owner.active || owner.dead)
                return;

            if (owner.GetModPlayer<DesertEagleSlotPlayer>().SlottedGunType != ModContent.ItemType<Hellborn>())
                return;

            explosionCooldown = 26;
            owner.GetModPlayer<DesertEaglePlayer>().ActivateHellbornOverdrive(180);

            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            owner.velocity -= direction * 2.5f;
            owner.SetScreenshake(7f);

            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.95f, Pitch = -0.25f }, target.Center);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.62f, Pitch = -0.4f }, target.Center);

            if (Main.myPlayer == projectile.owner)
            {
                DEBulletUtils.SpawnAreaBurst(
                    projectile.GetSource_FromAI(),
                    target.Center,
                    Math.Max(1, (int)(damageDone * 1.25f)),
                    projectile.knockBack * 1.3f,
                    projectile.owner,
                    DEBurstStyle.Hellborn,
                    132f);
            }
        }
    }
}
