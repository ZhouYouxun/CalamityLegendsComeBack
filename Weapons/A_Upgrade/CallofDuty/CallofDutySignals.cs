using System;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.CallofDuty
{
    internal sealed class ResponsibilityCommunicationSequence : ModProjectile
    {
        private int tokenIndex;
        private int tokenTimer;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        private ResponsibilityLanguageDefinition Definition => ResponsibilityLanguageRegistry.Get((int)Projectile.ai[0]);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 90;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            ResponsibilityLanguageDefinition definition = Definition;
            if (definition == null || !Main.player.IndexInRange(Projectile.owner))
            {
                Projectile.Kill();
                return;
            }

            if (tokenIndex >= definition.Tokens.Length)
            {
                Projectile.Kill();
                return;
            }

            if (tokenTimer-- > 0)
                return;

            if (Projectile.owner == Main.myPlayer)
            {
                float spread = definition.Id == ResponsibilityLanguage.Alarm ? 0.075f : definition.Id == ResponsibilityLanguage.Static ? 0.11f : 0.045f;
                float centeredIndex = tokenIndex - (definition.Tokens.Length - 1) * 0.5f;
                Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * definition.ProjectileSpeed;
                velocity = velocity.RotatedBy(centeredIndex * spread);
                int damage = Math.Max(1, (int)MathF.Round(Projectile.damage * definition.TotalDamageMultiplier / definition.Tokens.Length));

                int token = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    velocity,
                    ModContent.ProjectileType<ResponsibilityCommunicationToken>(),
                    damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    (float)definition.Id,
                    Projectile.ai[1],
                    Projectile.damage);

                if (Main.projectile.IndexInRange(token))
                {
                    Main.projectile[token].localAI[0] = tokenIndex;
                    Main.projectile[token].netUpdate = true;
                }
            }

            tokenIndex++;
            tokenTimer = definition.TokenInterval - 1;
        }
    }

    internal sealed class ResponsibilityCommunicationToken : ModProjectile
    {
        private bool initialized;
        private ResponsibilityLanguageDefinition Definition => ResponsibilityLanguageRegistry.Get((int)Projectile.ai[0]);
        private int SequenceId => (int)Projectile.ai[1];
        private int BaseWeaponDamage => Math.Max(1, (int)Projectile.ai[2]);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 108;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            ResponsibilityLanguageDefinition definition = Definition;
            if (definition == null)
            {
                Projectile.Kill();
                return;
            }

            if (!initialized)
            {
                Projectile.penetrate = definition.Penetration;
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, definition.Lifetime);
                initialized = true;
            }

            if (definition.HomingStrength > 0f && Projectile.timeLeft < definition.Lifetime - 3)
            {
                NPC target = CallofDutyGlobalNPC.FindPriorityTarget(Projectile.owner, Projectile.Center, 520f);
                if (target != null)
                {
                    Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center) * definition.ProjectileSpeed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, definition.HomingStrength);
                }
            }

            if (definition.Id == ResponsibilityLanguage.Static)
            {
                Projectile.velocity *= 0.996f;
                if (Main.rand.NextBool(3))
                {
                    Dust staticDust = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, -Projectile.velocity * 0.04f, 145, definition.Color, 0.55f);
                    staticDust.noGravity = true;
                }
            }
            else if (Main.rand.NextBool(4))
            {
                Dust signalDust = Dust.NewDustPerfect(Projectile.Center, DustID.GemEmerald, -Projectile.velocity * 0.03f, 120, definition.Color, 0.55f);
                signalDust.noGravity = true;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, definition.Color.ToVector3() * 0.25f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ResponsibilityLanguageDefinition definition = Definition;
            if (definition == null)
                return;

            if (definition.Id == ResponsibilityLanguage.Static)
                target.velocity *= target.boss ? 0.98f : 0.92f;

            CallofDutyGlobalNPC.RegisterSequenceHit(target, Projectile.owner, SequenceId, definition.Id, BaseWeaponDamage);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            ResponsibilityLanguageDefinition definition = Definition;
            if (definition == null)
                return false;

            string token = definition.Tokens[(int)MathHelper.Clamp(Projectile.localAI[0], 0f, definition.Tokens.Length - 1)];
            float fade = MathHelper.Clamp(Projectile.timeLeft / 12f, 0f, 1f);
            float pulse = 0.88f + MathF.Sin((float)Main.GlobalTimeWrappedHourly * 12f + Projectile.identity) * 0.08f;
            Vector2 size = FontAssets.MouseText.Value.MeasureString(token);
            Vector2 position = Projectile.Center - Main.screenPosition - size * 0.5f * pulse;
            Utils.DrawBorderString(Main.spriteBatch, token, position, definition.Color * fade, pulse);
            Utils.DrawBorderString(Main.spriteBatch, token, position, definition.AccentColor * 0.22f * fade, pulse * 1.15f);
            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.18f, Pitch = 0.35f }, Projectile.Center);
            return true;
        }
    }

    internal sealed class ResponsibilityHatMarker : ModProjectile
    {
        private int alarmCooldown;
        private int queryCooldown;
        private int staticCooldown;

        public override string Texture => "CalamityMod/Items/Armor/Wulfrum/WulfrumHat";
        public int TargetIndex => (int)Projectile.ai[0];
        public int QueryPriorityTimer => (int)Projectile.ai[1];
        public bool ConfirmArmed
        {
            get => Projectile.ai[2] > 0f;
            private set => Projectile.ai[2] = value ? 1f : 0f;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 18;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Main.npc.IndexInRange(TargetIndex) || !Main.npc[TargetIndex].active)
            {
                Projectile.Kill();
                return;
            }

            NPC target = Main.npc[TargetIndex];
            Projectile.Center = target.Top - Vector2.UnitY * MathHelper.Clamp(target.height * 0.08f, 5f, 18f);
            Projectile.rotation = target.rotation * 0.18f;
            Projectile.scale = MathHelper.Clamp(target.width / 40f, 0.72f, 1.35f);

            if (alarmCooldown > 0)
                alarmCooldown--;
            if (queryCooldown > 0)
                queryCooldown--;
            if (staticCooldown > 0)
                staticCooldown--;
            if (Projectile.ai[1] > 0f)
                Projectile.ai[1]--;

            if (ConfirmArmed && !Main.dedServ && Main.GameUpdateCount % 8 == 0)
            {
                Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(10f, 5f) - Vector2.UnitY * 7f;
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.35f, 0.35f), -Main.rand.NextFloat(0.35f, 0.9f));
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    position,
                    velocity,
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.22f, 0.34f),
                    Main.rand.NextBool(3) ? Color.White : new Color(194, 255, 67),
                    true,
                    false,
                    true));
            }

            Lighting.AddLight(Projectile.Center, new Color(76, 202, 255).ToVector3() * 0.35f);
        }

        internal void Respond(ResponsibilityLanguage language, int baseWeaponDamage)
        {
            if (Projectile.owner != Main.myPlayer || !Main.npc.IndexInRange(TargetIndex))
                return;

            NPC target = Main.npc[TargetIndex];
            switch (language)
            {
                case ResponsibilityLanguage.Alarm when alarmCooldown <= 0:
                    FireFusionArray(target, baseWeaponDamage, ConfirmArmed ? 8 : 5);
                    ConfirmArmed = false;
                    alarmCooldown = 45;
                    Projectile.netUpdate = true;
                    break;

                case ResponsibilityLanguage.Query when queryCooldown <= 0:
                    Projectile.ai[1] = 180f;
                    Main.player[Projectile.owner].GetModPlayer<CallofDutyPlayer>().SetPriorityTarget(TargetIndex, 180);
                    queryCooldown = 120;
                    Projectile.netUpdate = true;
                    SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.35f, Pitch = 0.45f }, Projectile.Center);
                    break;

                case ResponsibilityLanguage.Static when staticCooldown <= 0:
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        target.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<ResponsibilityStaticRing>(),
                        0,
                        0f,
                        Projectile.owner,
                        TargetIndex);
                    staticCooldown = 90;
                    SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.4f, Pitch = -0.35f }, Projectile.Center);
                    break;

                case ResponsibilityLanguage.Confirm:
                    ConfirmArmed = true;
                    Projectile.netUpdate = true;
                    SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 0.32f, Pitch = 0.35f }, Projectile.Center);
                    break;
            }
        }

        private void FireFusionArray(NPC target, int baseWeaponDamage, int count)
        {
            int damage = Math.Max(1, (int)MathF.Round(baseWeaponDamage * 0.12f));
            Vector2 baseDirection = Projectile.SafeDirectionTo(target.Center + new Vector2(0f, target.height * 0.35f));
            if (baseDirection == Vector2.Zero)
                baseDirection = Vector2.UnitY;

            for (int i = 0; i < count; i++)
            {
                float offset = count <= 1 ? 0f : MathHelper.Lerp(-0.55f, 0.55f, i / (float)(count - 1));
                Vector2 velocity = baseDirection.RotatedBy(offset) * 9.5f;
                int bolt = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    velocity,
                    ModContent.ProjectileType<WulfrumFusionBolt>(),
                    damage,
                    1f,
                    Projectile.owner,
                    velocity.ToRotation(),
                    TargetIndex);

                if (Main.projectile.IndexInRange(bolt))
                {
                    Main.projectile[bolt].DamageType = DamageClass.Summon;
                    Main.projectile[bolt].originalDamage = damage;
                }
            }
            SoundEngine.PlaySound(SoundID.Item91 with { Volume = 0.5f, Pitch = 0.2f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 position = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 2f;
                Main.EntitySpriteDraw(texture, position + offset, null, new Color(68, 194, 255) * 0.75f, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            }
            Main.EntitySpriteDraw(texture, position, null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }

    internal sealed class ResponsibilityHatImpact : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanHitNPC(NPC target) => target.whoAmI == (int)Projectile.ai[0];

        public override void AI()
        {
            if (Main.npc.IndexInRange((int)Projectile.ai[0]) && Main.npc[(int)Projectile.ai[0]].active)
                Projectile.Center = Main.npc[(int)Projectile.ai[0]].Center;
        }
    }

    internal sealed class ResponsibilityStaticRing : ModProjectile
    {
        private bool applied;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 240;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 24;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!applied)
            {
                applied = true;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy() || npc.Distance(Projectile.Center) > 120f)
                        continue;
                    npc.velocity *= npc.boss ? 0.94f : 0.8f;
                    npc.netUpdate = true;
                }

                if (!Main.dedServ)
                {
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                        Projectile.Center,
                        Vector2.Zero,
                        new Color(102, 232, 103),
                        Vector2.One,
                        0f,
                        0.08f,
                        1.05f,
                        24));
                    GeneralParticleHandler.SpawnParticle(new BloomParticle(
                        Projectile.Center,
                        Vector2.Zero,
                        new Color(82, 207, 255),
                        0.12f,
                        0.5f,
                        18));
                }
            }

            if (!Main.dedServ && Projectile.timeLeft % 3 == 0)
            {
                float progress = 1f - Projectile.timeLeft / 24f;
                float radius = MathHelper.Lerp(24f, 120f, progress);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 radial = Main.rand.NextVector2Unit();
                    Vector2 position = Projectile.Center + radial * radius;
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        position,
                        -radial * Main.rand.NextFloat(0.5f, 1.5f),
                        false,
                        Main.rand.Next(10, 16),
                        Main.rand.NextFloat(0.24f, 0.38f),
                        Main.rand.NextBool(3) ? new Color(194, 255, 67) : new Color(82, 207, 255),
                        true,
                        false,
                        true));
                }
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
