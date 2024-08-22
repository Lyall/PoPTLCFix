using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace PoPTLCFix
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class TLCFix : BasePlugin
    {
        internal static new ManualLogSource Log;

        // Features
        public static ConfigEntry<bool> bSkipIntro;

        // Custom Resolution
        public static ConfigEntry<bool> bCustomRes;
        public static ConfigEntry<int> iCustomResX;
        public static ConfigEntry<int> iCustomResY;
        public static ConfigEntry<int> iWindowMode;

        // HUD
        public static ConfigEntry<bool> bSpanHUD;

        // Graphics Tweaks
        public static ConfigEntry<bool> bChromaticAberration;
        public static ConfigEntry<bool> bMotionBlur;
        public static ConfigEntry<bool> bVignette;

        // Aspect Ratio
        private const float fNativeAspect = (float)16 / 9;
        public static float fAspectRatio;
        public static float fAspectMultiplier;

        // Variables
        public static LayoutElement pillarboxLayout;
        public static ContentSizeFitter pillarboxFitter;
        public static GameObject background;
        public static FullScreenMode windowMode;

        public override void Load()
        {
            // Plugin startup logic
            Log = base.Log;
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            // Skip Intro
            bSkipIntro = Config.Bind("Intro Skip",
                                "Enabled",
                                true,
                                "Skips Ubisoft intro video and splash screens.");

            // Custom Resolution
            bCustomRes = Config.Bind("Set Custom Resolution",
                                "CustomResolution",
                                 true,
                                "Set to true to enable the custom resolution below.");

            iCustomResX = Config.Bind("Set Custom Resolution",
                                "ResolutionWidth",
                                Display.main.systemWidth, // Set default to display width so we don't leave an unsupported resolution as default.
                                "Set desired resolution width.");

            iCustomResY = Config.Bind("Set Custom Resolution",
                                "ResolutionHeight",
                                Display.main.systemHeight, // Set default to display height so we don't leave an unsupported resolution as default.
                                "Set desired resolution height.");

            iWindowMode = Config.Bind("Set Custom Resolution",
                                "WindowMode",
                                (int)2, // Default to borderless
                                new ConfigDescription("Set window mode. 1 = Windowed, 2 = Borderless.",
                                new AcceptableValueRange<int>(1, 2)));

            // HUD
            bSpanHUD = Config.Bind("Span Gameplay HUD",
                                "Enabled",
                                true,
                                "Spans gameplay HUD to the edges of the screen.");

            // Graphics tweaks
            bChromaticAberration = Config.Bind("Graphical Tweaks",
                                "DisableChromaticAberration",
                                 false,
                                "Set to true to disable chromatic aberration (color fringing).");

            bMotionBlur = Config.Bind("Graphical Tweaks",
                                "DisableMotionBlur",
                                 true,
                                "Set to true to disable motion blur.");

            bVignette = Config.Bind("Graphical Tweaks",
                                "DisableVignette",
                                 true,
                                "Set to true to disable vignetting (darkening at the edges of the screen).");
            // Apply patches
            if (bSkipIntro.Value)
            {
                Log.LogInfo($"Patches: Applying skip intro patch.");
                Harmony.CreateAndPatchAll(typeof(SkipIntroPatch));
            }
            if (bCustomRes.Value)
            {
                windowMode = iWindowMode.Value switch
                {
                    1 => FullScreenMode.Windowed, // Windowed
                    2 => FullScreenMode.FullScreenWindow, // Borderless
                    _ => FullScreenMode.FullScreenWindow,
                };

                Log.LogInfo($"Patches: Applying resolution patch.");
                Harmony.CreateAndPatchAll(typeof(ResolutionPatch));

                // Set resolution
                UnityEngine.Screen.SetResolution(iCustomResX.Value, iCustomResY.Value, windowMode, 0);
            }
            if (bChromaticAberration.Value || bMotionBlur.Value || bVignette.Value)
            {
                Harmony.CreateAndPatchAll(typeof(GraphicsPatch));
                Log.LogInfo($"Patches: Applying graphics patch.");
            }
        }

        [HarmonyPatch]
        public class SkipIntroPatch
        {
            // Skip intro video
            [HarmonyPatch(typeof(VideoPlayer), nameof(VideoPlayer.Prepare))]
            [HarmonyPostfix]
            public static void SkipIntroVideo(VideoPlayer __instance)
            {
                if (__instance.url.Contains("UBISOFTSWIRL"))
                {
                    if (Alkawa.GameSystems.Bootstrap.Instance && Alkawa.GameSystems.Bootstrap.Instance.m_volatileBootstrap)
                    {
                        Alkawa.GameSystems.Bootstrap.Instance.m_volatileBootstrap.m_isVideoFinished = true;
                        Log.LogInfo("Skipped intro video.");
                    }
                }
            }

            // Skip intro screens
            [HarmonyPatch(typeof(Alkawa.Engine.GameFlow), nameof(Alkawa.Engine.GameFlow.PushState))]
            [HarmonyPrefix]
            public static void SkipIntroScreens(ref Alkawa.Engine.EGameFlowStateType __0)
            {
                if (__0 == Alkawa.Engine.EGameFlowStateType.FirstMandatoryUIScreens)
                {
                    if (!Alkawa.Engine.GameFlowUtils.IsFTUE())
                    {
                        __0 = Alkawa.Engine.EGameFlowStateType.UbiServicesConnection;
                        Log.LogInfo($"Skipping mandatory screens.");
                    }
                    else
                    {
                        __0 = Alkawa.Engine.EGameFlowStateType.TitleScreen;
                        Log.LogInfo($"Skipping mandatory screens (FTUE).");
                    }
                }
            }
        }

        [HarmonyPatch]
        public class GraphicsPatch
        {
            // Adjust vignette
            [HarmonyPatch(typeof(UnityEngine.Rendering.Volume), nameof(UnityEngine.Rendering.Volume.OnEnable))]
            [HarmonyPostfix]
            public static void PostProcessTweaks(UnityEngine.Rendering.Volume __instance)
            {
                __instance.profile.TryGet(out UnityEngine.Rendering.Universal.AlkawaChromaticAberration ca);
                if (ca && bChromaticAberration.Value)
                {
                    Log.LogInfo($"Graphics Tweaks: Changed {__instance.gameObject.name}: Chromatic aberration from {ca.active} to False.");
                    ca.active = false;
                }

                __instance.profile.TryGet(out UnityEngine.Rendering.Universal.MotionBlur motionBlur);
                if (motionBlur && bMotionBlur.Value)
                {
                    Log.LogInfo($"Graphics Tweaks: Changed {__instance.gameObject.name}: Motion blur from {motionBlur.active} to False.");
                    motionBlur.active = false;
                }

                __instance.profile.TryGet(out UnityEngine.Rendering.Universal.AlkawaVignette vignette);
                if (vignette && bVignette.Value)
                {
                    Log.LogInfo($"Graphics Tweaks: Changed {__instance.gameObject.name}: Vignette from {vignette.active} to False.");
                    vignette.active = false;
                }
            }
        }

        [HarmonyPatch]
        public class ResolutionPatch
        {
            // Get aspect ratio after res change
            [HarmonyPatch(typeof(Screen), nameof(Screen.SetResolution), new System.Type[] { typeof(int), typeof(int), typeof(FullScreenMode), typeof(int)})]
            [HarmonyPostfix]
            public static void GetAspectRatio(Screen __instance, ref int __0, ref int __1, ref FullScreenMode __2)
            {
                // Calculate aspect ratio
                fAspectRatio = (float)__0 / __1;
                fAspectMultiplier = (float)fAspectRatio / fNativeAspect;

                Log.LogInfo($"Resolution: Setting resolution {__0}x{__1}, window mode - {__2}.");
                Log.LogInfo($"Resolution: AspectRatio: {fAspectRatio}, AspectMultiplier: {fAspectMultiplier}");

                // Set UI scale
                if (Alkawa.Gameplay.UIManager.Instance)
                {
                    if (fAspectRatio > fNativeAspect)
                    {
                        Alkawa.Gameplay.UIManager.Instance.m_canvasScaler.referenceResolution = new Vector2((float)System.Math.Round(1080 * fAspectRatio, 0), 1080f);
                        Log.LogInfo($"Resolution: Set UIManager canvasScaler refRes to {Alkawa.Gameplay.UIManager.Instance.m_canvasScaler.referenceResolution}");
                    }
                }    
            }

            // Stop resolution changes
            [HarmonyPatch(typeof(Alkawa.Engine.ScreenResolutionHandler), nameof(Alkawa.Engine.ScreenResolutionHandler.SetResolution))]
            [HarmonyPatch(typeof(Alkawa.Gameplay.BaseGameOptionsMenu), nameof(Alkawa.Gameplay.BaseGameOptionsMenu.OnResolutionChange))]
            [HarmonyPrefix]
            public static bool StopResChange()
            {
                return false;
            }

            // Fix video aspect ratio
            [HarmonyPatch(typeof(VideoPlayer), nameof(VideoPlayer.Prepare))]
            [HarmonyPostfix]
            public static void FixVideoAspect(VideoPlayer __instance)
            {
                if (fAspectRatio > fNativeAspect)
                {
                    __instance.aspectRatio = VideoAspectRatio.FitVertically;
                }
            }

            // Disable culling
            [HarmonyPatch(typeof(Alkawa.Engine.Gfx.SceneGraphicSettings), nameof(Alkawa.Engine.Gfx.SceneGraphicSettings.Apply))]
            [HarmonyPostfix]
            public static void DisableCulling()
            {
                if (fAspectRatio > fNativeAspect)
                {
                    Alkawa.Engine.Gfx.RenderManager.PreCullingFlags = 0;
                }
            }

            // Increase AI culling distance
            [HarmonyPatch(typeof(Alkawa.ObjectRegister.AlkawaCullingHelper), nameof(Alkawa.ObjectRegister.AlkawaCullingHelper.ToMultiplier))]
            [HarmonyPostfix]
            public static void DisableCulling(ref float __result)
            {
                if (fAspectRatio > fNativeAspect)
                {
                    __result *= fAspectMultiplier;
                }
            }

            // Fix UI
            [HarmonyPatch(typeof(CanvasScaler), nameof(CanvasScaler.OnEnable))]
            [HarmonyPostfix]
            public static void FixUI(CanvasScaler __instance)
            {
                if (fAspectRatio > fNativeAspect)
                {
                    // Fix scaling
                    __instance.m_ScreenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                }
            }

            // Add pillarboxing for other screens
            [HarmonyPatch(typeof(Alkawa.Gameplay.UIScreen), nameof(Alkawa.Gameplay.UIScreen.RequestStartScreen))]
            [HarmonyPostfix]
            public static void ScreenAddPillarboxing(Alkawa.Gameplay.UIScreen __instance)
            {
                // Add pillarboxing
                if (__instance.m_Canvas.gameObject.GetComponent<UnityEngine.UI.LayoutElement>() == null && __instance.m_Canvas.gameObject.GetComponent<UnityEngine.UI.ContentSizeFitter>() == null)
                {
                    __instance.m_Canvas.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                    __instance.m_Canvas.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                    Log.LogInfo($"UIScreen: Added pillarboxing components for {__instance.m_Canvas.name}.");
                }

                var menuPillarboxLayout = __instance.m_Canvas.gameObject.GetComponent<UnityEngine.UI.LayoutElement>();
                var menuPillarboxFitter = __instance.m_Canvas.gameObject.GetComponent<UnityEngine.UI.ContentSizeFitter>();

                menuPillarboxLayout.preferredWidth = 1920;
                menuPillarboxLayout.preferredHeight = 1080;

                if (fAspectRatio > fNativeAspect)
                {
                    menuPillarboxFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                }
                else if (fAspectRatio < fNativeAspect)
                {
                    menuPillarboxFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                }

                menuPillarboxLayout.enabled = true;
                menuPillarboxFitter.enabled = true;

                Log.LogInfo($"UIScreen: Enabled pillarboxing for {__instance.m_Canvas.name}.");
                
                // Add mask
                if (__instance.m_Canvas.gameObject.GetComponent<RectMask2D>() == null)
                {
                    RectMask2D mask = __instance.m_Canvas.gameObject.AddComponent<RectMask2D>();
                    mask.enabled = true;
                    Log.LogInfo($"UIScreen: Added RectMask2D for {__instance.m_Canvas.name}.");
                }
            }

            // Add pillarboxing for UIManager
            [HarmonyPatch(typeof(Alkawa.Gameplay.UIManager), nameof(Alkawa.Gameplay.UIManager.AlkawaStart))]
            [HarmonyPostfix]
            public static void ManagerAddPillarboxing(Alkawa.Gameplay.UIManager __instance)
            {
                if (__instance != null)
                {
                    // Fix UI scaling
                    __instance.m_canvasScaler.referenceResolution = new Vector2((float)System.Math.Round(1080 * fAspectRatio, 0), 1080f);
                    Log.LogInfo($"UIManager: Set UIManager canvasScaler refRes to {__instance.m_canvasScaler.referenceResolution}");

                    // Add pillarboxing
                    if (__instance.gameObject.GetComponent<UnityEngine.UI.LayoutElement>() == null && __instance.gameObject.GetComponent<UnityEngine.UI.ContentSizeFitter>() == null)
                    {
                        __instance.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                        __instance.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                        Log.LogInfo($"UIManager: Added pillarboxing components for {__instance.name}.");
                    }

                    pillarboxLayout = __instance.gameObject.GetComponent<UnityEngine.UI.LayoutElement>();
                    pillarboxFitter = __instance.gameObject.GetComponent<UnityEngine.UI.ContentSizeFitter>();

                    pillarboxLayout.preferredWidth = 1920;
                    pillarboxLayout.preferredHeight = 1080;

                    if (fAspectRatio > fNativeAspect)
                    {
                        pillarboxFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                    }
                    else if (fAspectRatio < fNativeAspect)
                    {
                        pillarboxFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                    }

                    // Enable UIManager pillarboxing
                    if (pillarboxLayout != null && pillarboxLayout != null)
                    {
                        pillarboxLayout.enabled = true;
                        pillarboxFitter.enabled = true;
                        Log.LogInfo($"UIManager: Enabled pillarboxing.");
                    }

                    // Add mask
                    if (__instance.GetComponent<RectMask2D>() == null)
                    {
                        RectMask2D mask = __instance.gameObject.AddComponent<RectMask2D>();
                        mask.enabled = true;
                        Log.LogInfo($"UIManager: Added RectMask2D for {__instance.name}.");
                    }

                    // Add black background object
                    background = new GameObject("background");
                    background.transform.SetParent(__instance.gameObject.transform);

                    // Create image and set it to black
                    Image img = background.AddComponent<Image>();
                    img.color = Color.black;

                    // Set rect size to fill screen
                    RectTransform rt = background.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(99999, 99999);

                    // Make sure it renders behind everything else
                    Canvas canvas = background.AddComponent<Canvas>();
                    canvas.overrideSorting = true;
                    canvas.sortingOrder = -1;
                }
            }

            // Enable pillarboxing for conversations
            [HarmonyPatch(typeof(Alkawa.Gameplay.ConversationUIManager), nameof(Alkawa.Gameplay.ConversationUIManager.StartConversation))]
            [HarmonyPostfix]
            public static void EnableConversationPillarboxing(ref Alkawa.Gameplay.ConversationOverridenParams __1)
            {
                if (pillarboxLayout != null && pillarboxFitter != null && background != null && __1 == null)
                {
                    pillarboxLayout.enabled = true;
                    pillarboxFitter.enabled = true;
                    background.active = false;
                    Log.LogInfo("ConversationUI: Enabled pillarboxing.");
                }
            }

            // Enable pillarboxing
            [HarmonyPatch(typeof(Alkawa.Gameplay.UIManager), nameof(Alkawa.Gameplay.UIManager.TryDisableHighContrastOnMenuOpen))]
            [HarmonyPostfix]
            public static void EnablePillarboxing()
            {
                if (pillarboxLayout != null && pillarboxFitter != null && background != null)
                {
                    if (Alkawa.Gameplay.UIManager.Instance && !Alkawa.Gameplay.UIManager.Instance.IsActiveCanvas(Alkawa.Gameplay.EUICanvasID.HUD))
                    {
                        pillarboxLayout.enabled = true;
                        pillarboxFitter.enabled = true;
                        background.active = true;
                        Log.LogInfo("UIManager: Enabled pillarboxing.");
                    }              
                }     
            }

            // Disable pillarboxing
            [HarmonyPatch(typeof(Alkawa.Gameplay.PauseChoiceController), nameof(Alkawa.Gameplay.PauseChoiceController.Close))] // Pause canvas
            [HarmonyPatch(typeof(Alkawa.Gameplay.UIManager), nameof(Alkawa.Gameplay.UIManager.RestoreActiveCanvas))]
            [HarmonyPatch(typeof(Alkawa.Gameplay.UIManager), nameof(Alkawa.Gameplay.UIManager.OnExitCutsceneStateComplete))]
            [HarmonyPatch(typeof(Alkawa.Gameplay.UIManager), nameof(Alkawa.Gameplay.UIManager.OnPlayerEnterLevel))]
            [HarmonyPatch(typeof(Alkawa.Gameplay.ConversationUIManager), nameof(Alkawa.Gameplay.ConversationUIManager.EndConversation))]
            [HarmonyPostfix]
            public static void DisablePillarboxing()
            {
                // Disable pillarboxing
                if (pillarboxLayout && pillarboxFitter && bSpanHUD.Value)
                {
                    pillarboxLayout.enabled = false;
                    pillarboxFitter.enabled = false;  
                    Log.LogInfo($"UIManager: Disabled pillarboxing.");
                }

                // Disable background
                if (background != null)
                {
                    background.active = false;
                }
            }

            // Fix character in main menu
            [HarmonyPatch(typeof(Alkawa.Gameplay.CharacterMenu), nameof(Alkawa.Gameplay.CharacterMenu.SpawnCharacterInstance))]
            [HarmonyPostfix]
            public static void CharacterMenuCamera(Alkawa.Gameplay.CharacterMenu __instance)
            {
                // "SargonCamera"
                if (__instance.m_characterCamera != null)
                {
                    __instance.m_characterCamera.aspect = fNativeAspect;
                    Log.LogInfo($"MenuChar: Fixed menu character camera aspect ratio.");
                }
            }

            // Fix map camera aspect 
            [HarmonyPatch(typeof(Alkawa.Gameplay.UIManager), nameof(Alkawa.Gameplay.UIManager.OpenWorldMap))]
            [HarmonyPostfix]
            public static void FixMapAspect(Alkawa.Gameplay.UIManager __instance)
            {
                // Fix aspect ratio of map camera
                var minimapCams = __instance.GetMenu<Alkawa.Gameplay.WorldMapMenu>().Data.m_minimapCameras;
                foreach (var cam in minimapCams)
                {
                    if (cam != null)
                    {
                        if (cam.name == "MiniMapCanvasBackgroundCamera" || cam.name == "MiniMapCanvasCamera")
                        {
                            cam.GetComponent<Camera>().aspect = fNativeAspect;
                            cam.GetComponent<Camera>().backgroundColor = Color.black;
                            Log.LogInfo($"WorldMap: Set {cam.GetComponent<Camera>().name} aspect ratio to {cam.GetComponent<Camera>().aspect}.");
                        }
                    }
                }
            }

            // Fix player camera positioning
            [HarmonyPatch(typeof(Cinemachine.LensSettings), nameof(Cinemachine.LensSettings.Aspect), MethodType.Getter)]
            [HarmonyPostfix]
            public static void FixCameraPos(Cinemachine.LensSettings __instance, ref float __result)
            {
                if (fAspectRatio > fNativeAspect)
                {
                    // Called by Alkawa.Gameplay.Cinemachine.CinemachineAlkawaBorder.ComputeScreenLimits()
                    // Changed reported aspect ratio to fix camera positioning issues
                    __result = fNativeAspect;
                }
            }

            // Change resolution for cinematic videos
            [HarmonyPatch(typeof(Alkawa.Gameplay.VideoController), nameof(Alkawa.Gameplay.VideoController.SetUIManagerTexture))]
            [HarmonyPostfix]
            public static void PreVideoFix(Alkawa.Gameplay.VideoController __instance)
            {
                if (__instance.m_videoPlayer.url != null)
                {
                    if (fAspectRatio > fNativeAspect)
                    {
                        Screen.SetResolution((int)System.Math.Round(iCustomResY.Value * fNativeAspect, 0), iCustomResY.Value, windowMode, 0);
                    }
                    else if (fAspectRatio < fNativeAspect)
                    {
                        Screen.SetResolution(iCustomResX.Value, (int)System.Math.Round(iCustomResX.Value / fNativeAspect, 0), windowMode, 0);
                    }
                    Log.LogInfo($"VideoController: Video prepared.");
                }            
            }

            // Change resolution back after cinematic videos
            [HarmonyPatch(typeof(Alkawa.Gameplay.VideoController), nameof(Alkawa.Gameplay.VideoController.StopVideo))]
            [HarmonyPatch(typeof(Alkawa.Gameplay.InteractiveElementLogic_CutsceneSequence), nameof(Alkawa.Gameplay.InteractiveElementLogic_CutsceneSequence.Skip))]
            [HarmonyPostfix]
            public static void PostVideoFix()
            {
                if (Screen.currentResolution.width != iCustomResX.Value || Screen.currentResolution.width != iCustomResY.Value)
                {
                    Screen.SetResolution(iCustomResX.Value, iCustomResY.Value, windowMode, 0);
                    Log.LogInfo($"VideoController: Video ended.");
                }    
            }
        }
    }
}