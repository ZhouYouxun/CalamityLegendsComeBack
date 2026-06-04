using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    public class LeonidCometSmall : ModProjectile, ILocalizedModType
    {
        public const int FromStealthFlag = 1;
        public const int SilverSplitFlag = 2;
        public const int SpectreCloneFlag = 4;

        public override string Texture => "CalamityMod/Items/Weapons/Rogue/LeonidProgenitor";
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";

        private readonly HashSet<string> effectFlags = new();
        private readonly Dictionary<string, float> effectStates = new();

        private LeonidMetalEffect[] activeEffects = System.Array.Empty<LeonidMetalEffect>();
        private bool initialized;

        public int PrimaryEffectID => (int)Projectile.ai[0];
        public int SecondaryEffectID => (int)Projectile.ai[1];
        public int SpawnFlags => (int)Projectile.ai[2];
        public bool FromStealthRain => (SpawnFlags & FromStealthFlag) != 0;
        public Player Owner => Main.player[Projectile.owner];
        public Vector2 InitialCenter { get; private set; }
        public Color MeteorColor { get; private set; }

        public bool IgnoreGravity { get; private set; }
        public bool EnableHoming { get; private set; }
        public float HomingStrength { get; private set; }
        public float HomingRange { get; private set; } = 720f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.alpha = 255;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            InitialCenter = Projectile.Center;
            MeteorColor = LeonidVisualUtils.GetMeteorColor(PrimaryEffectID, SecondaryEffectID);
            activeEffects = LeonidMetalEffectRegistry.ResolveEffects(PrimaryEffectID, SecondaryEffectID);
            Projectile.DamageType = Owner.HeldItem.DamageType;

            if ((SpawnFlags & SilverSplitFlag) != 0)
                SetFlag("silver_split");

            if ((SpawnFlags & SpectreCloneFlag) != 0)
                SetFlag("spectre_clone");

            for (int i = 0; i < activeEffects.Length; i++)
                activeEffects[i].OnSpawn(this, Owner);

            initialized = true;
        }

        public override void AI()
        {
            if (!initialized)
                OnSpawn(Projectile.GetSource_FromThis());

            Projectile.alpha -= 32;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            Lighting.AddLight(Projectile.Center, MeteorColor.ToVector3() * (FromStealthRain ? 0.75f : 0.5f));

            if (!IgnoreGravity)
            {
                Projectile.localAI[0]++;
                if (Projectile.localAI[0] >= 14f)
                {
                    Projectile.velocity.Y += 0.22f;
                    if (Projectile.velocity.Y > 16f)
                        Projectile.velocity.Y = 16f;
                }
            }

            if (EnableHoming)
            {
                NPC target = FindClosestNPC(HomingRange);
                if (target != null)
                {
                    Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * Projectile.velocity.Length();
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, HomingStrength);
                }
            }

            for (int i = 0; i < activeEffects.Length; i++)
                activeEffects[i].AI(this, Owner);

            if (Main.rand.NextBool(2))
            {
                Dust trailDust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    DustID.TintableDustLighted,
                    -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.12f),
                    100,
                    Color.Lerp(MeteorColor, Color.White, Main.rand.NextFloat(0.25f)),
                    Main.rand.NextFloat(0.75f, 1.25f));
                trailDust.noGravity = true;
            }

            Projectile.rotation += Projectile.velocity.X * 0.04f + 0.22f * System.Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            for (int i = 0; i < activeEffects.Length; i++)
                activeEffects[i].ModifyHitNPC(this, Owner, target, ref modifiers);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < activeEffects.Length; i++)
                activeEffects[i].OnHitNPC(this, Owner, target, hit, damageDone);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            bool shouldDie = true;
            for (int i = 0; i < activeEffects.Length; i++)
            {
                if (!activeEffects[i].OnTileCollide(this, Owner, oldVelocity))
                    shouldDie = false;
            }

            return shouldDie;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.45f, Pitch = -0.08f }, Projectile.Center);
            LeonidVisualUtils.SpawnDustBurst(Projectile.Center, MeteorColor, FromStealthRain ? 16 : 10, FromStealthRain ? 5.4f : 4.2f, FromStealthRain ? 1.3f : 1f);

            for (int i = 0; i < activeEffects.Length; i++)
                activeEffects[i].OnKill(this, Owner, timeLeft);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Rogue/LeonidProgenitorGlow").Value;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 oldDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(texture, oldDrawPosition, null, MeteorColor * completion * 0.4f, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            }

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color drawColor = MeteorColor * (1f - Projectile.alpha / 255f);
            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glow, drawPosition, null, Color.White * (1f - Projectile.alpha / 255f), Projectile.rotation, glow.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            LeonidVisualUtils.DrawBloom(Projectile.Center, MeteorColor * 0.35f, FromStealthRain ? 0.42f : 0.3f, Projectile.rotation);

            for (int i = 0; i < activeEffects.Length; i++)
                activeEffects[i].PostDraw(this, Owner, Main.spriteBatch);

            return false;
        }

        public void DisableGravity()
        {
            IgnoreGravity = true;
        }

        public void EnableSimpleHoming(float strength, float range)
        {
            EnableHoming = true;
            HomingStrength = System.Math.Max(HomingStrength, strength);
            HomingRange = System.Math.Max(HomingRange, range);
        }

        public bool HasFlag(string key) => effectFlags.Contains(key);
        public void SetFlag(string key) => effectFlags.Add(key);
        public float GetState(string key) => effectStates.TryGetValue(key, out float value) ? value : 0f;
        public void SetState(string key, float value) => effectStates[key] = value;

        public NPC FindClosestNPC(float range)
        {
            NPC target = null;
            float sqrRange = range * range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float sqrDistance = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (sqrDistance > sqrRange)
                    continue;

                sqrRange = sqrDistance;
                target = npc;
            }

            return target;
        }
    }
}
