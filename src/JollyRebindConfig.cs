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
				PlayerPointInputs[i] = config.Bind($"pointInputP{i}", KeyCode.Space, new ConfigurableInfo("placeholder 1", tags: new string[]
				{
					"placeholder 2"
				}));
			}
		}


		// Manually set the `type` of the mod button for this interface to `Inconfigurable`, and stop it from being opened.
		// This is needed because there aren't actually any configurable settings, and it'll be confusing to players.
		// (I can't find a better way to do this unfortunately, so this will have to do.)
		public static void SetupHooks()
		{
			On.Menu.Remix.MenuModList.ModButton.ctor += ModButtonHK;
			On.Menu.Remix.MenuModList.ModButton.Signal += ModButton_SignalHK;
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

		private static void ModButton_SignalHK(On.Menu.Remix.MenuModList.ModButton.orig_Signal orig, MenuModList.ModButton self, Menu.Remix.MixedUI.UIfocusable _self)
		{
			if (self.itf.GetType() == typeof(JollyRebindConfig))
			{
				self._NotifyDisabled(self.Menu.Translate("This mod does not have any configurable settings."), false);
				self.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
				return;
			}
			orig(self, _self);
		}
	}
}
