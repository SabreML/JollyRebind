using Menu.Remix;
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
			On.OptionInterface.HasConfigurables += OptionInterface_HasConfigurables;
		}

		private static void ModButtonHK(On.Menu.Remix.MenuModList.ModButton.orig_ctor orig, MenuModList.ModButton self, MenuModList list, int index)
		{
			orig(self, list, index);
			if (self.itf.GetType() == typeof(JollyRebindConfig))
			{
				FieldInfo typeField = typeof(MenuModList.ModButton).GetField("type", BindingFlags.Public | BindingFlags.Instance);
				typeField.SetValue(self, MenuModList.ModButton.ItfType.Blank);
			}
		}

		private static bool OptionInterface_HasConfigurables(On.OptionInterface.orig_HasConfigurables orig, OptionInterface self)
		{
			if (self.GetType() == typeof(JollyRebindConfig))
			{
				return false;
			}
			return orig(self);
		}
	}
}
