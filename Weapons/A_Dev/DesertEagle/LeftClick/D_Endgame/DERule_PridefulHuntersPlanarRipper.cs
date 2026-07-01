using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.D_Endgame
{
    public class DERule_PridefulHuntersPlanarRipper : DEBulletRule
    {
        private static readonly Color LightningBlue = new(92, 220, 255);
        private static readonly Color ShadowBlue = new(42, 75, 180);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.PridefulHuntersPlanarRipper>();

        public override int Penetrate => 2;
        public override int ExtraUpdates => 8;
        public override float SpeedMultiplier => 1.36f;
        public override float DamageMultiplier => 0.82f;

        public override float GetShotExtra(DesertEagleSlotPlayer slotPlayer)
        {
            slotPlayer.BurstCounter++;
            if (slotPlayer.BurstCounter >= 4)
            {
                slotPlayer.BurstCounter = 0;
                return 1f;
            }

            return 0f;
        }

        public override void SetDefaults(Projectile projectile)
        {
            projectile.width = 12;
            projectile.height = 12;
            projectile.light = 0.75f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            bool empowered = projectile.ai[1] == 1f;
            DEBulletUtils.OrientToVelocity(projectile);
            DEBulletUtils.TrailDust(projectile, DustID.Electric, empowered ? Color.White : LightningBlue, empowered ? 1.25f : 0.9f, 0.08f);
            DEBulletUtils.TrailDust(projectile, DustID.BlueTorch, ShadowBlue, 0.7f, 0.05f);
            DEBulletUtils.GlowTrail(projectile, empowered ? Color.White : LightningBlue, empowered ? 1.35f : 1f);
            Lighting.AddLight(projectile.Center, LightningBlue.ToVector3() * (empowered ? 0.82f : 0.55f));
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (projectile.ai[1] == 1f)
                modifiers.SourceDamage *= 1.45f;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 240);

            if (projectile.ai[1] != 1f || Main.myPlayer != projectile.owner)
                return;

            DEBulletUtils.SpawnAreaBurst(
                projectile.GetSource_FromAI(),
                target.Center,
                Math.Max(1, (int)(hit.Damage * 0.38f)),
                projectile.knockBack,
                projectile.owner,
                DEBurstStyle.Astral,
                76f);
        }

        public override string TooltipEffectEN => "Fires an extremely fast lightning-shadow round; every 4th shot is empowered and tears a small planar rift";
        public override string TooltipEffectZH => "发射极高速雷影弹；每4发强化一次，命中撕开小型雷影裂隙";
    }
}
