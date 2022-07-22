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
        internal new static ManualLogSource Log;
        private static ConfigEntry<bool> _displayLevel;
        private static ConfigEntry<bool> _displayMoney;
        private static ConfigEntry<bool> _displayDate;
        private static ConfigEntry<bool> _displayLocation;
        private static ConfigEntry<bool> _displayName;
        private static ConfigEntry<bool> _optionalLook;
        private static ConfigEntry<bool> _displayGender;
        public static DiscordRpcClient Client { get; private set; }
        private static int _currentMoney;
        private static int _currentLevel = 0;
        private static int _currentDay;
        private static string _currentSeason;
        private static string _currentArea;

        public override void Load()
        {
            // Plugin startup logic
            Log = base.Log;
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            Client = new DiscordRpcClient("999087621823279114");
            Client.Initialize();

            //Check config
            _displayLevel = Config.Bind("Discord Rich Presence",
                                       "DisplayLevel",
                                       true,
                                       "Show current player level");

            _displayMoney = Config.Bind("Discord Rich Presence",
                                       "DisplayMoney",
                                       true,
                                       "Show current money");

            _displayDate = Config.Bind("Discord Rich Presence",
                                       "DisplayDate",
                                       true,
                                       "Show the in-game date");

            _displayLocation = Config.Bind("Discord Rich Presence",
                                       "DisplayLocation",
                                       true,
                                       "Show location name on large icon tooltip");

            _displayName = Config.Bind("Discord Rich Presence",
                                       "DisplayName",
                                       true,
                                       "Display player name on small icon tooltip");

            _displayGender = Config.Bind("Discord Rich Presence",
                                      "DisplayGender",
                                      true,
                                      "Display player gender on small icon");

            _optionalLook = Config.Bind("Discord Rich Presence",
                                       "AlternateLook",
                                       false,
                                       "Swaps the details(character info) and moves it to character icon(instead of name) with location being in state");

            SetPresence();

            Harmony.CreateAndPatchAll(typeof(LoadPatch));
            Harmony.CreateAndPatchAll(typeof(Calander));

            if (_displayLevel.Value)
            {
                Harmony.CreateAndPatchAll(typeof(PlayerLevel));
            }

            if (_displayMoney.Value)
            {
                Harmony.CreateAndPatchAll(typeof(PlayerMoney));
            }
        }

        private static void SetPresence()
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
            _currentLevel = level;

            string CurrentDayFormat = _currentDay switch
            {
                1 => "1st",
                2 => "2nd",
                3 => "3rd",
                _ => $"{_currentDay}th"
            };

            if (_displayDate.Value)
            {
                Client.UpdateDetails($"{_currentSeason}, {CurrentDayFormat}");
            }

            // Alternate Look
            if (_optionalLook.Value)
            {
                if (_displayLocation.Value)
                {
                    Client.UpdateState($"{_currentArea}");
                }
                

                if (_displayLevel.Value && _displayMoney.Value)
                {
                    Client.UpdateSmallAsset(gender, $"Level {_currentLevel} | {_currentMoney}G");
                }
                else if (_displayLevel.Value == false && _displayMoney.Value)
                {
                    Client.UpdateSmallAsset(gender, $"Level {_currentLevel} | {_currentMoney}G");
                }
                else if (_displayMoney.Value)
                {
                    Client.UpdateSmallAsset(gender, $"Level {_currentLevel} | {_currentMoney}G");
                }
                else
                {
                    Client.UpdateSmallAsset(gender, $"Level {_currentLevel} | {_currentMoney}G");
                }
                return;
            }

            // Show Location icon tooltip
            if (_displayLocation.Value)
            {
                Client.UpdateLargeAsset("icon", _currentArea);
            }

            // State for Level & Money
            if (_displayLevel.Value && _displayMoney.Value)
            {
                Client.UpdateState($"Level {_currentLevel} | {_currentMoney}G");
            }
            else if (_displayLevel.Value == false && _displayMoney.Value)
            {
                Client.UpdateState($"{_currentMoney}G");
            }
            else if (_displayLevel.Value && _displayMoney.Value == false)
            {
                Client.UpdateState($"Level {_currentLevel}");
            }

            // Small Icon for Gender & Name
            if (_displayGender.Value && _displayName.Value)
            {
                Client.UpdateSmallAsset(gender, playername);
            }
            else if (_displayGender.Value == false && _displayName.Value)
            {
                Client.UpdateSmallAsset("icon", playername);
            }
            else if (_displayGender.Value && _displayName.Value == false)
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
                string currentArea = __instance.GetFieldPlaceName();

                // Currently no idea why teleporting and leaving area for first time makes it "Rigbarth" but leaving anytime after makes it field.
                // This is the current "fix"
                if (currentArea == "Field")
                {
                    currentArea = "Rigbarth";
                }

                _currentArea = currentArea;
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
                int currentLevel = __instance.Level;
                _currentLevel = currentLevel;

                if (_currentLevel > 0 && _currentMoney >= 0)
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
                int currentMoney = __instance.GetNowNum();
                _currentMoney = currentMoney;

                if (_currentLevel > 0 && _currentMoney >= 0)
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
                _currentDay = __instance.Day;
                _currentSeason = __instance.Season.ToString();

                UpdatePresence();
            }
        }
    }
}
