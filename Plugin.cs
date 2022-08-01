using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using DiscordRPC;
using BepInEx.Configuration;
using BepInEx.Logging;
using Il2CppSystem;
using UnityEngine.Playables;

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

            // Change endings to approiate number (st, th, nd)
            string DateSuffix = "";
            var currentDayFormat = _currentDay;

            string GetDaySuffix(int day)
            {
                switch (day)
                {
                    case 1:
                    case 21:
                    case 31:
                        return "st";
                    case 2:
                    case 22:
                        return "nd";
                    case 3:
                    case 23:
                        return "rd";
                    default:
                        return "th";
                }
            }

            DateSuffix = GetDaySuffix(currentDayFormat);

            if (_displayDate.Value)
            {
                Client.UpdateDetails($"{_currentSeason}, {currentDayFormat}{DateSuffix}");
            }

            // Alternate Look
            if (_optionalLook.Value)
            {
                if (_displayLocation.Value)
                {
                    Client.UpdateState($"{_currentArea}");
                }
                

                switch (_displayLevel.Value)
                {
                    case true when _displayMoney.Value:
                        Client.UpdateSmallAsset(gender, $"Level {_currentLevel} | {_currentMoney}G");
                        break;
                    case false when _displayMoney.Value:
                        Client.UpdateSmallAsset(gender, $"{_currentMoney}G");
                        break;
                    case true when _displayMoney.Value == false:
                        Client.UpdateSmallAsset(gender, $"Level {_currentLevel}");
                        break;
                    default:
                        Client.UpdateSmallAsset(gender);
                        break;
                }
                return;
            }

            // Show Location icon tooltip
            if (_displayLocation.Value)
            {
                Client.UpdateLargeAsset("icon", _currentArea);
            }

            switch (_displayLevel.Value)
            {
                // State for Level & Money
                case true when _displayMoney.Value:
                    Client.UpdateState($"Level {_currentLevel} | {_currentMoney}G");
                    break;
                case false when _displayMoney.Value:
                    Client.UpdateState($"{_currentMoney}G");
                    break;
                case true when _displayMoney.Value == false:
                    Client.UpdateState($"Level {_currentLevel}");
                    break;
            }

            switch (_displayGender.Value)
            {
                // Small Icon for Gender & Name
                case true when _displayName.Value:
                    Client.UpdateSmallAsset(gender, playername);
                    break;
                case false when _displayName.Value:
                    Client.UpdateSmallAsset("icon", playername);
                    break;
                case true when _displayName.Value == false:
                    Client.UpdateSmallAsset(gender);
                    break;
            }
        }

        // Band-aid fix for staying up after midnight
        public static void CheckTime()
        {
            TimeManager time = TimeManager.Instance;

            if (!time.Day.Equals(_currentDay))
            {
                _currentDay = time.Day;
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
                CheckTime();
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

                // 
                if (_currentLevel <= 0 || _currentMoney < 0) return;
                CheckTime();
                UpdatePresence();
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

                // HUDPlayerMoney loads faster in initial load so this makes sure the player doesn't have "level 0" for a second
                if (_currentLevel <= 0 || _currentMoney < 0) return;

                CheckTime();
                UpdatePresence();
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
