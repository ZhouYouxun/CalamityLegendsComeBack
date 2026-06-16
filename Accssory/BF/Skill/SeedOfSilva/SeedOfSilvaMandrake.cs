using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.SeedOfSilva
{
    internal sealed class SeedOfSilvaMandrake : SeedOfSilvaFlowerProjectile
    {
        private int mandrakeSporeCooldown;
        private int mandrakeDartCooldown;

        protected override int FlowerSlot => 4;
        protected override BlossomFluxChloroplastPresetType FlowerPreset => BlossomFluxChloroplastPresetType.Chlo_EPlague;
        protected override string FlowerTexturePath => "CalamityLegendsComeBack/Accssory/BF/Skill/SeedOfSilva/SeedPack/Mandrake";

        protected override void UpdateBlooming(Player owner, BFAccessoryPlayer accessoryPlayer)
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy() || Vector2.DistanceSquared(npc.Center, Projectile.Center) > 230f * 230f)
                    continue;

                npc.GetGlobalNPC<BFAccessoryGlobalNPC>().ApplyMandrakeSlow(Projectile.owner, 12);
            }

            if (mandrakeSporeCooldown > 0)
                mandrakeSporeCooldown--;

            if (mandrakeDartCooldown > 0)
                mandrakeDartCooldown--;

            NPC target = FindTarget(620f);
            if (target is null || Projectile.owner != Main.myPlayer)
                return;

            if (mandrakeSporeCooldown <= 0)
            {
                mandrakeSporeCooldown = 42;
                int damage = System.Math.Max(1, (int)(owner.GetWeaponDamage(owner.HeldItem) * 0.16f));
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 4.2f);
                int spore = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, Main.rand.NextBool() ? ProjectileID.SporeTrap : ProjectileID.SporeTrap2, damage, 0.2f, Projectile.owner);
                if (Main.projectile.IndexInRange(spore))
                {
                    Main.projectile[spore].friendly = true;
                    Main.projectile[spore].hostile = false;
                    Main.projectile[spore].DamageType = DamageClass.Ranged;
                }
            }

            if (mandrakeDartCooldown <= 0)
            {
                mandrakeDartCooldown = 128;
                int damage = System.Math.Max(1, (int)(owner.GetWeaponDamage(owner.HeldItem) * 0.34f));
                for (int i = 0; i < 2; i++)
                {
                    Vector2 velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.48f) * Main.rand.NextFloat(5.5f, 8.5f);
                    int spore = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, Main.rand.NextBool() ? ProjectileID.SporeTrap : ProjectileID.SporeTrap2, damage, 0.6f, Projectile.owner);
                    if (Main.projectile.IndexInRange(spore))
                    {
                        Main.projectile[spore].friendly = true;
                        Main.projectile[spore].hostile = false;
                        Main.projectile[spore].DamageType = DamageClass.Ranged;
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (IsBlooming)
                DrawSlowAura();

            return base.PreDraw(ref lightColor);
        }

        private void DrawSlowAura()
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            float pulse = 0.92f + 0.08f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 3.1f + Projectile.identity);
            Color aura = new Color(135, 230, 72, 0) * 0.18f;
            Main.EntitySpriteDraw(
                bloom,
                center,
                null,
                aura,
                0f,
                bloom.Size() * 0.5f,
                230f / bloom.Width * 2f * pulse,
                SpriteEffects.None,
                0);
        }
    }
}
