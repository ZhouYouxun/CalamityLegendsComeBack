using CalamityLegendsComeBack.Weapons.Malachite;
using CalamityLegendsComeBack.Weapons.Malachite.passive;
using CalamityMod;
using CalamityMod.CalPlayer;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.MC.GaleAce
{
    public sealed class GaleAce : ModItem
    {
        public override string LocalizationCategory => "Items.Accessories";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 10);
            Item.rare = ItemRarityID.Yellow;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<GaleAcePlayer>().GaleAceEquipped = true;
        }
    }

    public sealed class GaleAcePlayer : ModPlayer
    {
        private const float GrazeRadius = 110f;
        private const int GrazeStackDuration = 300;
        private const int MaxStacks = 5;
        private const float DamageBoostPerStack = 0.08f;
        private const float StealthRestorePercent = 0.20f;

        public bool GaleAceEquipped;

        private int grazeStacks;
        private int grazeDecayTimer;
        private readonly HashSet<int> grazedSlots = new();

        public override void ResetEffects()
        {
            GaleAceEquipped = false;
        }

        public override void PostUpdateEquips()
        {
            if (!GaleAceEquipped || grazeStacks <= 0)
                return;

            Player.GetDamage<RogueDamageClass>() += grazeStacks * DamageBoostPerStack;
        }

        public override void PostUpdate()
        {
            if (!GaleAceEquipped || Player.dead)
            {
                grazeStacks = 0;
                grazeDecayTimer = 0;
                grazedSlots.Clear();
                return;
            }

            if (grazeStacks > 0)
            {
                grazeDecayTimer--;
                if (grazeDecayTimer <= 0)
                {
                    grazeStacks--;
                    grazeDecayTimer = grazeStacks > 0 ? GrazeStackDuration : 0;
                }
            }

            if (Player.whoAmI == Main.myPlayer)
                CheckForGrazes();
        }

        private void CheckForGrazes()
        {
            grazedSlots.RemoveWhere(slot => slot < 0 || slot >= Main.maxProjectiles || !Main.projectile[slot].active);

            float radiusSq = GrazeRadius * GrazeRadius;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || !proj.hostile || grazedSlots.Contains(i))
                    continue;

                if (Vector2.DistanceSquared(Player.Center, proj.Center) > radiusSq)
                    continue;

                grazedSlots.Add(i);
                TriggerGraze(proj);
            }
        }

        private void TriggerGraze(Projectile proj)
        {
            grazeStacks = System.Math.Min(grazeStacks + 1, MaxStacks);
            grazeDecayTimer = GrazeStackDuration;

            CalamityPlayer calamity = Player.Calamity();
            if (calamity.rogueStealthMax > 0f)
            {
                float restoreAmount = calamity.rogueStealthMax * StealthRestorePercent;
                calamity.rogueStealth = MathHelper.Clamp(calamity.rogueStealth + restoreAmount, 0f, calamity.rogueStealthMax);
            }

            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.60f, Pitch = 0.65f, MaxInstances = 3 }, Player.Center);

            Vector2 graceDir = (Player.Center - proj.Center).SafeNormalize(Vector2.UnitY);
            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Center,
                Vector2.Zero,
                ModContent.ProjectileType<MalachiteGrazeSlashVisual>(),
                0, 0f, Player.whoAmI,
                graceDir.ToRotation());
        }
    }
}
