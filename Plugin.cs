using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using DiscordRPC;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace RF5DRP
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Rune Factory 5.exe")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        private static ConfigEntry<bool> displayLevel;
        private static ConfigEntry<bool> displayMoney;
        private static ConfigEntry<bool> displayDate;
        private static ConfigEntry<bool> displayLocation;
        private static ConfigEntry<bool> displayName;
        private static ConfigEntry<bool> optionalLook;
        private static ConfigEntry<bool> displayGender;
        public static DiscordRpcClient Client { get; private set; }
        private static int CurrentMoney;
        private static int CurrentLevel = 0;
        private static int CurrentDay;
        private static string CurrentSeason;
        private static string CurrentArea;

        public override void Load()
        {
            // Plugin startup logic
            Log = base.Log;
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            Client = new DiscordRpcClient("999087621823279114");
            Client.Initialize();

            //Check config
            displayLevel = Config.Bind("Discord Rich Presence",
                                       "DisplayLevel",
                                       true,
                                       "Show current player level");

            displayMoney = Config.Bind("Discord Rich Presence",
                                       "DisplayMoney",
                                       true,
                                       "Show current money");

            displayDate = Config.Bind("Discord Rich Presence",
                                       "DisplayDate",
                                       true,
                                       "Show the in-game date");

            displayLocation = Config.Bind("Discord Rich Presence",
                                       "DisplayLocation",
                                       true,
                                       "Show location name on large icon tooltip");

            displayName = Config.Bind("Discord Rich Presence",
                                       "DisplayName",
                                       true,
                                       "Display player name on small icon tooltip");

            displayGender = Config.Bind("Discord Rich Presence",
                                      "DisplayGender",
                                      true,
                                      "Display player gender on small icon");

            optionalLook = Config.Bind("Discord Rich Presence",
                                       "AlternateLook",
                                       false,
                                       "Swaps the details(character info) and moves it to character icon(instead of name) with location being in state");

            SetPresence();

            Harmony.CreateAndPatchAll(typeof(LoadPatch));
            Harmony.CreateAndPatchAll(typeof(Calander));

            if (displayLevel.Value)
            {
                Harmony.CreateAndPatchAll(typeof(PlayerLevel));
            }

            if (displayMoney.Value)
            {
                Harmony.CreateAndPatchAll(typeof(PlayerMoney));
            }
        }

        private void SetPresence()
        {
            // Main Menu
            Client.SetPresence(new RichPresence()
            {
                Details = "Browsing Main Menu",
                Timestamps = Timestamps.Now,
                State = null,
                Assets = new Assets()
                {
                    LargeImageKey = "icon"
                }
            });
        }

        private static void UpdatePresence()
        {
            string gender = ActorPlayer.Gender.ToString().ToLower();
            int level = ActorPlayer.Status.Level;
            string playername = TextOverwriteList.GetPlayerName();
            CurrentLevel = level;
            string CurrentDayFormat = "";

            switch (CurrentDay)
            {
                case 1:
                    CurrentDayFormat = "1st";
                    break;
                case 2:
                    CurrentDayFormat = "2nd";
                    break;
                case 3:
                    CurrentDayFormat = "3rd";
                    break;
                default:
                    CurrentDayFormat = $"{CurrentDay}th";
                    break;
            }

            if (displayDate.Value)
            {
                Client.UpdateDetails($"{CurrentSeason}, {CurrentDayFormat}");
            }

            // Alternate Look
            if (optionalLook.Value)
            {
                if (displayLocation.Value)
                {
                    Client.UpdateState($"{CurrentArea}");
                }

                if (displayLevel.Value && displayMoney.Value)
                {
                    Client.UpdateSmallAsset(gender, $"Level {CurrentLevel} | {CurrentMoney}G");
                }
                else if (displayLevel.Value == false && displayMoney.Value)
                {
                    Client.UpdateSmallAsset(gender, $"Level {CurrentLevel} | {CurrentMoney}G");
                }
                else if (displayMoney.Value)
                {
                    Client.UpdateSmallAsset(gender, $"Level {CurrentLevel} | {CurrentMoney}G");
                }
                else
                {
                    Client.UpdateSmallAsset(gender, $"Level {CurrentLevel} | {CurrentMoney}G");
                }
                return;
            }

            // Show Location icon tooltip
            if (displayLocation.Value)
            {
                Client.UpdateLargeAsset("icon", CurrentArea);
            }

            // State for Level & Money
            if (displayLevel.Value && displayMoney.Value)
            {
                Client.UpdateState($"Level {CurrentLevel} | {CurrentMoney}G");
            }
            else if (displayLevel.Value == false && displayMoney.Value)
            {
                Client.UpdateState($"{CurrentMoney}G");
            }
            else if (displayLevel.Value && displayMoney.Value == false)
            {
                Client.UpdateState($"Level {CurrentLevel}");
            }

            // Small Icon for Gender & Name
            if (displayGender.Value && displayName.Value)
            {
                Client.UpdateSmallAsset(gender, playername);
            }
            else if (displayGender.Value == false && displayName.Value)
            {
                Client.UpdateSmallAsset("icon", playername);
            }
            else if (displayGender.Value && displayName.Value == false)
            {
                Client.UpdateSmallAsset(gender);
            }
        }



        [HarmonyPatch]
        public class LoadPatch
        {
            // This one is need of fixes but ATM its not a priority
            [HarmonyPatch(typeof(TeleportAreaManager), nameof(TeleportAreaManager.EndTeleportCharacter))]
            [HarmonyPostfix]
            public static void UpdateArea(TeleportAreaManager __instance)
            {
                string _CurrentArea = __instance.GetFieldPlaceName();


                // Currently no idea why teleporting and leaving area for first time makes it "Rigbarth" but leaving anytime after makes it field.
                // This is the current "fix"
                if (_CurrentArea == "Field")
                {
                    _CurrentArea = "Rigbarth";
                }

                CurrentArea = _CurrentArea;
                UpdatePresence();
            }
        }

        [HarmonyPatch]
        public class PlayerLevel
        {
            [HarmonyPatch(typeof(PlayerStatus), nameof(PlayerStatus.LevelUp))]
            [HarmonyPostfix]
            public static void UpdateLevel(PlayerStatus __instance)
            {
                int _CurrentLevel = __instance.Level;
                CurrentLevel = _CurrentLevel;

                if (CurrentLevel > 0 && CurrentMoney >= 0)
                {
                    UpdatePresence();
                }
            }
        }

        [HarmonyPatch]
        public class PlayerMoney
        {
            [HarmonyPatch(typeof(HUDPlayerMoney), nameof(HUDPlayerMoney.RedrawText))]
            [HarmonyPostfix]
            public static void UpdateMoney(HUDPlayerMoney __instance)
            {
                int _CurrentMoney = __instance.GetNowNum();
                CurrentMoney = _CurrentMoney;

                if (CurrentLevel > 0 && CurrentMoney >= 0)
                {
                    UpdatePresence();
                }
            }
        }


        [HarmonyPatch]
        public class Calander
        {
            // Won't update if player stays up all night and day changes
            [HarmonyPatch(typeof(TimeManager), nameof(TimeManager.ToNextMorning))]
            [HarmonyPatch(typeof(TimeManager), nameof(TimeManager.AfterLoadData))]
            [HarmonyPostfix]
            public static void UpdateMoney(TimeManager __instance)
            {
                CurrentDay = __instance.Day;
                CurrentSeason = __instance.Season.ToString();

                UpdatePresence();
            }
        }
    }
}
