using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityLegendsComeBack.FancyIcon
{
    /// <summary>
    /// Makes the mod-list icon shine without changing icon.png itself.
    /// </summary>
    public class ModListIconShine : ModSystem
    {
        private const string OurModName = "CalamityLegendsComeBack";

        private static readonly FieldInfo UIImageTextureField = typeof(UIImage).GetField("_texture", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo UIImageNonReloadingTextureField = typeof(UIImage).GetField("_nonReloadingTexture", BindingFlags.NonPublic | BindingFlags.Instance);

        private ILHook uiModItemOnInitializeHook;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            try
            {
                Type uiModItemType = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.UIModItem", throwOnError: true);
                MethodInfo onInitializeMethod = uiModItemType.GetMethod("OnInitialize", BindingFlags.Public | BindingFlags.Instance);
                uiModItemOnInitializeHook = new ILHook(onInitializeMethod, ReplaceIconWithShineIcon);
            }
            catch (Exception e)
            {
                Mod.Logger.Warn($"{OurModName}: could not hook UIModItem to animate the mod list icon.", e);
            }
        }

        public override void Unload()
        {
            uiModItemOnInitializeHook?.Dispose();
            uiModItemOnInitializeHook = null;

            // ModSystem.Unload can run on tModLoader's background unload thread. The shine
            // toolkit disposes runtime Texture2D instances, so that work must return to the
            // main thread instead of touching FNA graphics resources here.
            if (!Main.dedServ)
                Main.QueueMainThreadAction(ShineEffectToolkit.Unload);
        }

        private static void ReplaceIconWithShineIcon(ILContext il)
        {
            ILCursor c = new(il);

            Type uiModItemType = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.UIModItem", throwOnError: true);
            FieldInfo modIconField = uiModItemType.GetField("_modIcon", BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo modNameProperty = uiModItemType.GetProperty("ModName", BindingFlags.Public | BindingFlags.Instance);

            c.GotoNext(MoveType.Before, x => x.MatchStfld(modIconField));
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Callvirt, modNameProperty.GetMethod);
            c.EmitDelegate((UIImage originalIcon, string modName) =>
            {
                if (modName != OurModName)
                    return (UIElement)originalIcon;

                return new ShineIconElement(originalIcon);
            });
        }

        private sealed class ShineIconElement : UIElement
        {
            private readonly UIImage originalIcon;
            private readonly Texture2D originalIconTexture;
            private readonly ShineEffectToolkit.State shine = new();

            public ShineIconElement(UIImage originalIcon)
            {
                this.originalIcon = originalIcon;
                originalIconTexture = GetTexture(originalIcon);

                Left = originalIcon.Left;
                Top = originalIcon.Top;
                Width = originalIcon.Width;
                Height = originalIcon.Height;

                originalIcon.Left.Set(0f, 0f);
                originalIcon.Top.Set(0f, 0f);
                originalIcon.Width.Set(0f, 1f);
                originalIcon.Height.Set(0f, 1f);
                Append(originalIcon);
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                if (IsEnabled)
                    ShineEffectToolkit.Advance(shine);
            }

            public override void Draw(SpriteBatch spriteBatch)
            {
                // Base draws us (no-op, we don't override DrawSelf) then our appended original
                // icon child - so the shine has to go here, after base.Draw, to land on top.
                base.Draw(spriteBatch);

                if (IsEnabled)
                    ShineEffectToolkit.Draw(spriteBatch, shine, originalIconTexture, originalIcon.GetDimensions().ToRectangle());
            }

            private static bool IsEnabled => CLCBClientConfig.Instance?.ShowModListIconShine ?? true;

            private static Texture2D GetTexture(UIImage image)
            {
                Asset<Texture2D> asset = UIImageTextureField?.GetValue(image) as Asset<Texture2D>;
                return asset?.Value ?? UIImageNonReloadingTextureField?.GetValue(image) as Texture2D;
            }
        }
    }
}
