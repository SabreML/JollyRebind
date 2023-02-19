using Menu.Remix;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;
using UnityEngine;

namespace JollyRebind
{
	public class JollyRebindConfig : OptionInterface
	{
		public static readonly Configurable<KeyCode>[] PlayerPointInputs = new Configurable<KeyCode>[4];

		public JollyRebindConfig()
		{
			for (int i = 0; i < PlayerPointInputs.Length; i++)
			{
				PlayerPointInputs[i] = config.Bind($"PlayerPointInputP{i}", KeyCode.Space);
			}
		}


		// Hook and override these methods to manually stop this interface from being opened.
		// This is needed because there aren't actually any configurable settings, and it'll be confusing to players.
		// (I can't find a better way to do this unfortunately, so this will have to do.)
		public static void SetupHooks()
		{
			On.Menu.Remix.MenuModList.ModButton.ctor += ModButtonHK;
			IL.Menu.Remix.MenuModList.ModButton.Signal += ModButton_SignalHK_IL;
		}

		private static void ModButtonHK(On.Menu.Remix.MenuModList.ModButton.orig_ctor orig, MenuModList.ModButton self, MenuModList list, int index)
		{
			orig(self, list, index);
			if (self.itf.GetType() == typeof(JollyRebindConfig))
			{
				// `type` is a readonly field, so it can't be assigned to in a hook. (afaik)
				FieldInfo typeField = typeof(MenuModList.ModButton).GetField("type", BindingFlags.Public | BindingFlags.Instance);
				typeField.SetValue(self, MenuModList.ModButton.ItfType.Blank);
			}
		}

		private static void ModButton_SignalHK_IL(ILContext il)
		{
			ILCursor cursor = new ILCursor(il);
			ILLabel label = null;

			// Move the cursor to the end of the `HasConfigurables()` check near the end of the method, and copy its label target.
			if (!cursor.TryGotoNext(MoveType.After,
				i => i.MatchCallvirt(typeof(OptionInterface).GetMethod("HasConfigurables", BindingFlags.NonPublic | BindingFlags.Instance)),
				i => i.MatchBrfalse(out label)
			))
			{
				Debug.Log("(JollyRebind) IL edit failed!");
				return;
			}

			// Load `this`.
			cursor.Emit(OpCodes.Ldarg_0);
			// Check the type of `this.itf`, using `this` as `self` in a delegate.
			cursor.EmitDelegate<Func<MenuModList.ModButton, bool>>(self => self.itf.GetType() == typeof(JollyRebindConfig));
			// If the delegate returned true, jump to the label.
			cursor.Emit(OpCodes.Brtrue_S, label);
		}
	}
}
