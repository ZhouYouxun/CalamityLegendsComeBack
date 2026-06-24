using System;
using System.Collections.Generic;
using System.IO;
using CalamityMod;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Ranged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.A_Olds.MarkedMagnum
{
    public class MarkedMagnum : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Olds/MarkedMagnum/死神马格南";

        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 22;
            Item.damage = 95;
            Item.DamageType = DamageClass.Ranged;
            Item.crit = 10;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 4f;
            Item.value = Item.sellPrice(0, 15, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<MarkedMagnumLaser>();
            Item.shootSpeed = 16f;
            Item.UseSound = new SoundStyle("CalamityMod/Sounds/Item/NitroExpressRifleFire") { Volume = 0.6f };
        }

        public override Vector2? HoldoutOffset() => new Vector2(-5, 3);

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            player.ChangeDir(Math.Sign((player.Calamity().mouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 20f;
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = new Vector2(-5, 6);

            CalamityUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player)
        {
            player.ChangeDir(Math.Sign((player.Calamity().mouseWorld - player.Center).X));

            float animProgress = 1f - player.itemTime / (float)player.itemTimeMax;
            float rotation = (player.Center - player.Calamity().mouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            if (animProgress < 0.5f)
                rotation += -0.45f * (float)Math.Pow((0.5f - animProgress) / 0.5f, 2) * player.direction;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);

            if (animProgress > 0.5f)
            {
                float backArmRotation = rotation + 0.52f * player.direction;
                Player.CompositeArmStretchAmount stretch = GetStretchAmount((float)Math.Sin(Math.PI * (animProgress - 0.5f) / 0.5f));
                player.SetCompositeArmBack(true, stretch, backArmRotation);
            }
        }

        private static Player.CompositeArmStretchAmount GetStretchAmount(float value)
        {
            if (value <= 0.25f)
                return Player.CompositeArmStretchAmount.None;
            if (value <= 0.5f)
                return Player.CompositeArmStretchAmount.Quarter;
            if (value <= 0.8f)
                return Player.CompositeArmStretchAmount.ThreeQuarters;
            return Player.CompositeArmStretchAmount.Full;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<SlagMagnum>()
                .AddIngredient<CoreofCalamity>(3)
                .AddIngredient<RuinousSoul>(5)
                .AddIngredient<MeldBlob>(5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    public class MarkedMagnumLaser : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 12;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;

                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                float maxDistance = 1800f;
                float distance = maxDistance;

                // Raycast solid tiles
                for (float d = 0f; d < maxDistance; d += 8f)
                {
                    Vector2 checkPos = Projectile.Center + direction * d;
                    Point tilePoint = checkPos.ToTileCoordinates();
                    if (WorldGen.InWorld(tilePoint.X, tilePoint.Y))
                    {
                        Tile tile = Main.tile[tilePoint.X, tilePoint.Y];
                        if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                        {
                            distance = d;
                            break;
                        }
                    }
                }

                // Raycast NPCs
                NPC targetNPC = null;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && !npc.friendly && npc.dontTakeDamage == false && npc.chaseable)
                    {
                        float npcHitFraction = 0f;
                        if (Collision.CheckAABBvLineCollision(npc.position, npc.Size, Projectile.Center, Projectile.Center + direction * distance, 16f, ref npcHitFraction))
                        {
                            float npcDist = npcHitFraction * distance;
                            if (npcDist < distance)
                            {
                                distance = npcDist;
                                targetNPC = npc;
                            }
                        }
                    }
                }

                Projectile.localAI[1] = distance;

                if (targetNPC != null)
                {
                    // Apply Calamity's Marked for Death debuff
                    try
                    {
                        int markedForDeathID = ModContent.BuffType<CalamityMod.Buffs.StatDebuffs.MarkedforDeath>();
                        targetNPC.AddBuff(markedForDeathID, 600);
                    }
                    catch (Exception)
                    {
                        // Fallback in case of class or namespace mismatch
                    }

                    // Apply custom debuff
                    int markedMagnumDebuffID = ModContent.BuffType<MarkedMagnumDebuff>();
                    targetNPC.AddBuff(markedMagnumDebuffID, 600);

                    // Apply damage
                    int hitDirection = direction.X > 0 ? 1 : -1;
                    NPC.HitInfo hitInfo = targetNPC.CalculateHitInfo(Projectile.damage, hitDirection, false, Projectile.knockBack, Projectile.DamageType);
                    targetNPC.StrikeNPC(hitInfo);

                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, targetNPC.whoAmI, Projectile.damage, Projectile.knockBack, hitDirection);
                    }

                    // Spawn visual dust at hit point
                    Vector2 hitPos = Projectile.Center + direction * distance;
                    for (int i = 0; i < 15; i++)
                    {
                        Dust d = Dust.NewDustPerfect(hitPos, DustID.Vortex, Main.rand.NextVector2Circular(4f, 4f), 100, Color.Purple, 1.3f);
                        d.noGravity = true;
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float progress = Projectile.timeLeft / 12f;
            float fade = MathHelper.Clamp(progress, 0f, 1f);

            Vector2 start = Projectile.Center;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float length = Projectile.localAI[1];
            Vector2 end = start + direction * length;

            Color laserColor = Color.Purple;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // Layered laser lines for glowing effect
            DrawLine(pixel, start, end, laserColor * 0.4f * fade, 20f * fade);
            DrawLine(pixel, start, end, laserColor * 0.8f * fade, 8f * fade);
            DrawLine(pixel, start, end, Color.White * 0.95f * fade, 2.5f * fade);

            // Bloom circles at start and end
            try
            {
                Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
                if (bloom != null)
                {
                    Main.EntitySpriteDraw(bloom, start - Main.screenPosition, null, laserColor * 0.5f * fade, 0f, bloom.Size() * 0.5f, 0.4f * fade, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(bloom, end - Main.screenPosition, null, laserColor * 0.5f * fade, 0f, bloom.Size() * 0.5f, 0.4f * fade, SpriteEffects.None, 0);
                }
            }
            catch (Exception)
            {
                // Fallback if texture not found
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            return false;
        }

        private static void DrawLine(Texture2D pixel, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 line = end - start;
            if (line.LengthSquared() <= 0.01f)
                return;

            Main.EntitySpriteDraw(
                pixel,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                line.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(line.Length(), width),
                SpriteEffects.None,
                0f);
        }
    }

    public class MarkedMagnumDebuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Olds/MarkedMagnum/死神马格南";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
    }

    public class MarkedMagnumGlobalNPC : GlobalNPC
    {
        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (npc.HasBuff<MarkedMagnumDebuff>())
            {
                // Ensure cloning only happens on the projectile owner's client in multiplayer
                if (projectile.owner == Main.myPlayer)
                {
                    var globalProj = projectile.GetGlobalProjectile<MarkedMagnumGlobalProjectile>();
                    if (globalProj != null && !globalProj.IsMarkedMagnumClone)
                    {
                        // Check if it's a valid friendly damaging projectile, and avoid holdouts/fishing lines etc.
                        if (projectile.friendly && projectile.damage > 0 && projectile.aiStyle != ProjAIStyleID.HeldProjectile && projectile.aiStyle != ProjAIStyleID.Hook && !projectile.minion && !projectile.sentry)
                        {
                            // 25% chance to spawn clones
                            if (Main.rand.NextFloat() < 0.25f)
                            {
                                SpawnClones(projectile);
                            }
                        }
                    }
                }
            }
        }

        private void SpawnClones(Projectile proj)
        {
            Vector2 velocity = proj.velocity;
            if (velocity.LengthSquared() < 0.1f)
            {
                velocity = Main.rand.NextVector2Unit() * 8f;
            }

            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            Vector2 perp = new Vector2(-direction.Y, direction.X) * 20f;

            // Spawn left clone
            Vector2 posLeft = proj.Center + perp;
            Vector2 velLeft = velocity.RotatedBy(MathHelper.ToRadians(-15));
            int p1 = Projectile.NewProjectile(
                proj.GetSource_FromThis(),
                posLeft,
                velLeft,
                proj.type,
                proj.damage,
                proj.knockBack,
                proj.owner,
                proj.ai[0],
                proj.ai[1],
                proj.ai[2]
            );
            if (p1 >= 0 && p1 < Main.maxProjectiles)
            {
                Main.projectile[p1].GetGlobalProjectile<MarkedMagnumGlobalProjectile>().IsMarkedMagnumClone = true;
                SpawnCloneVisuals(posLeft);
            }

            // Spawn right clone
            Vector2 posRight = proj.Center - perp;
            Vector2 velRight = velocity.RotatedBy(MathHelper.ToRadians(15));
            int p2 = Projectile.NewProjectile(
                proj.GetSource_FromThis(),
                posRight,
                velRight,
                proj.type,
                proj.damage,
                proj.knockBack,
                proj.owner,
                proj.ai[0],
                proj.ai[1],
                proj.ai[2]
            );
            if (p2 >= 0 && p2 < Main.maxProjectiles)
            {
                Main.projectile[p2].GetGlobalProjectile<MarkedMagnumGlobalProjectile>().IsMarkedMagnumClone = true;
                SpawnCloneVisuals(posRight);
            }

            SoundEngine.PlaySound(SoundID.Item8, proj.Center);
        }

        private void SpawnCloneVisuals(Vector2 position)
        {
            for (int i = 0; i < 8; i++)
            {
                Dust d = Dust.NewDustPerfect(
                    position,
                    DustID.Vortex,
                    Main.rand.NextVector2Circular(3f, 3f),
                    150,
                    Color.Purple,
                    1.2f
                );
                d.noGravity = true;
            }
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (npc.HasBuff<MarkedMagnumDebuff>())
            {
                Color color = Color.Purple * 0.8f;
                Vector2 position = new Vector2(npc.Center.X, npc.position.Y - 20f) - screenPos;

                try
                {
                    Texture2D bloomRing = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
                    Texture2D bloomCircle = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

                    if (bloomRing != null && bloomCircle != null)
                    {
                        float scale = 0.25f + 0.05f * MathF.Sin(Main.GlobalTimeWrappedHourly * 6f);
                        float rotation = Main.GlobalTimeWrappedHourly * 2f;

                        // Rotating outer ring
                        spriteBatch.Draw(
                            bloomRing,
                            position,
                            null,
                            color,
                            rotation,
                            bloomRing.Size() * 0.5f,
                            scale,
                            SpriteEffects.None,
                            0f
                        );

                        // Pulsing inner core
                        spriteBatch.Draw(
                            bloomCircle,
                            position,
                            null,
                            color * 0.5f,
                            -rotation,
                            bloomCircle.Size() * 0.5f,
                            scale * 0.5f,
                            SpriteEffects.None,
                            0f
                        );
                    }
                }
                catch (Exception)
                {
                    // Fallback in case Calamity mod textures are missing
                }

                // Crosshair tick marks
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                float tickDist = 12f + 4f * MathF.Sin(Main.GlobalTimeWrappedHourly * 6f);
                float tickLen = 6f;

                // Draw ticks: top, bottom, left, right
                spriteBatch.Draw(pixel, position + new Vector2(0f, -tickDist - tickLen / 2f), new Rectangle(0, 0, 1, 1), color, 0f, new Vector2(0.5f, 0.5f), new Vector2(2f, tickLen), SpriteEffects.None, 0f);
                spriteBatch.Draw(pixel, position + new Vector2(0f, tickDist + tickLen / 2f), new Rectangle(0, 0, 1, 1), color, 0f, new Vector2(0.5f, 0.5f), new Vector2(2f, tickLen), SpriteEffects.None, 0f);
                spriteBatch.Draw(pixel, position + new Vector2(-tickDist - tickLen / 2f, 0f), new Rectangle(0, 0, 1, 1), color, 0f, new Vector2(0.5f, 0.5f), new Vector2(tickLen, 2f), SpriteEffects.None, 0f);
                spriteBatch.Draw(pixel, position + new Vector2(tickDist + tickLen / 2f, 0f), new Rectangle(0, 0, 1, 1), color, 0f, new Vector2(0.5f, 0.5f), new Vector2(tickLen, 2f), SpriteEffects.None, 0f);
            }
        }
    }

    public class MarkedMagnumGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool IsMarkedMagnumClone { get; set; } = false;

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(IsMarkedMagnumClone);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            IsMarkedMagnumClone = bitReader.ReadBit();
        }
    }
}
