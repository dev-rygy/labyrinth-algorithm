using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif

namespace Utilities.BetterHierarchy
{
    public static partial class BetterHierarchyPreferences
    {
        public enum ScriptIconType
        {
            SmallIcon,
            BigIcon,
            UnityDefault
        }

        public enum UnityNativeScriptsDetectionType
        {
            UnityEngine,
            Unity,
            None
        }

        public const string TITLE = "Better Hierarchy";
        public static event Action OnSettingsChangedEvents = delegate { };

        public static bool IsEnabled => isEnabledBetterHierarchy.Get();
        public static bool IsShowAlwaysFirstScriptIcon => showAlwaysFirstScriptIcon.Get();
        public static bool IsOverridePrefabIcons => isOverridePrefabIconType.Get();
        public static bool IsEnableTooltipsOnHierarchyObject => isEnableTooltipsOnHierarchyObject.Get();
        public static UnityNativeScriptsDetectionType UnityScriptDetectionType => unityScriptDetectionType.Get();
        public static ScriptIconType ContainsOnlyUnityScriptsType => containsUnityScriptsOnly.Get();
        public static ScriptIconType ContainsNonUnityScriptsType => containsNonUnityScripts.Get();
        public static ScriptIconType ContainsSingleScriptType => containsSingleUserScript.Get();
        public static ScriptIconType ContainsNoScriptsType => containsNoScripts.Get();
        public static ScriptIconType OverridenPrefabIcons => isAPrefab.Get();


        private const float MIN_LABEL_WIDTH = 230;
        private const float VERTICAL_SPACE_PADDING = 15;


        private static BoolPreference isEnabledBetterHierarchy = new BoolPreference()
        {
            Label = "Enabled",
            DefaultValue = true,
            Key = nameof(BetterHierarchy) + "." + nameof(isEnabledBetterHierarchy)
        };

        private static BoolPreference showAlwaysFirstScriptIcon = new BoolPreference()
        {
            Label = "Always Show First Script Icon",
            DefaultValue = false,
            Key = nameof(BetterHierarchy) + "." + nameof(showAlwaysFirstScriptIcon)
        };

        private static EnumPreference<UnityNativeScriptsDetectionType> unityScriptDetectionType =
            new EnumPreference<UnityNativeScriptsDetectionType>()
            {
                Label = "Unity Native Script Keyword",
                DefaultValue = UnityNativeScriptsDetectionType.Unity,
                Key = nameof(BetterHierarchy) + "." + nameof(unityScriptDetectionType)
            };

        private static EnumPreference<ScriptIconType> containsUnityScriptsOnly = new EnumPreference<ScriptIconType>()
        {
            Label = "Contains Unity Scripts Only",
            DefaultValue = ScriptIconType.BigIcon,
            Key = nameof(BetterHierarchy) + "." + nameof(containsUnityScriptsOnly)
        };

        private static EnumPreference<ScriptIconType> containsNonUnityScripts = new EnumPreference<ScriptIconType>()
        {
            Label = "Contains Non-Unity Scripts",
            DefaultValue = ScriptIconType.SmallIcon,
            Key = nameof(BetterHierarchy) + "." + nameof(containsNonUnityScripts)
        };

        private static EnumPreference<ScriptIconType> containsSingleUserScript = new EnumPreference<ScriptIconType>()
        {
            Label = "Contains Single User Script Only",
            DefaultValue = ScriptIconType.SmallIcon,
            Key = nameof(BetterHierarchy) + "." + nameof(containsSingleUserScript)
        };

        private static EnumPreference<ScriptIconType> containsNoScripts = new EnumPreference<ScriptIconType>()
        {
            Label = "Contains No Scripts",
            DefaultValue = ScriptIconType.BigIcon,
            Key = nameof(BetterHierarchy) + "." + nameof(containsNoScripts)
        };

        private static BoolPreference isOverridePrefabIconType = new BoolPreference()
        {
            Label = "Override Prefab Icons",
            DefaultValue = false,
            Key = nameof(BetterHierarchy) + "." + nameof(isOverridePrefabIconType)
        };

        private static EnumPreference<ScriptIconType> isAPrefab = new EnumPreference<ScriptIconType>()
        {
            Label = "Is A Prefab",
            DefaultValue = ScriptIconType.SmallIcon,
            Key = nameof(BetterHierarchy) + "." + nameof(isAPrefab)
        };
        
        private static BoolPreference isEnableTooltipsOnHierarchyObject = new BoolPreference()
        {
            Label = "Enable Hierarchy Icon Tooltips",
            DefaultValue = true,
            Key = nameof(BetterHierarchy) + "." + nameof(isEnableTooltipsOnHierarchyObject)
        };


#if UNITY_2019_1_OR_NEWER
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            return new SettingsProvider("Preferences/BetterHierarchy", SettingsScope.User)
            {
                label = TITLE,
                keywords = new HashSet<string>(new[]
                {
                    "Better", "Hierarchy"
                }),
                activateHandler = (searchContext, rootElement) =>
                {
                    var wrapper = new VisualElement()
                    {
                        style =
                        {
                            marginBottom = 2,
                            marginTop = 2,
                            marginLeft = 8,
                            marginRight = 8,
                            flexDirection = FlexDirection.Column
                        }
                    };

                    rootElement.Add(wrapper);
                    var ui = new UIElementBuilder(wrapper);

                    ui.AddHeader(TITLE);
                    
                    AddDisableFeatureToggle();
                    AddSimpleDetectorToggles();
                    AddNoUserScriptsFoundToggle();
                    AddDisableForLoopUserScriptDetectionToggle();
                    AddUserScriptForLoopDetectionOptions();
                    AddPrefabDetectionOptions();
                    AddHierarchyTooltipOption();
                    
                    ActivateUIListenersFirstTime();

                    return;

                    void AddDisableFeatureToggle()
                    {
                        var enableToggle = ui.AddToggle(isEnabledBetterHierarchy,
                            "If unchecked, then the feature is completely disabled.");
                        enableToggle.style.paddingBottom = VERTICAL_SPACE_PADDING;
                    }

                    void AddSimpleDetectorToggles()
                    {
                        ui.AddSelection(containsSingleUserScript,
                            "The icon style when there's only one script included in a GameObject (besides Transform).");
                        ui.AddSelection(containsNoScripts,
                            "The icon style when there are no scripts inside of a GameObject (besides Transform).");
                    }

                    void AddNoUserScriptsFoundToggle()
                    {
                        var unityScriptsOnlySelection = ui.AddSelection(containsUnityScriptsOnly,
                            "The icon style when there are no user-made scripts inside of a GameObject.");
                        showAlwaysFirstScriptIcon.OnValueChanged += value =>
                            unityScriptsOnlySelection.label =
                                (value ? "First Script Is Unity Related" : containsUnityScriptsOnly.Label);
                    }
                    
                    void AddDisableForLoopUserScriptDetectionToggle()
                    {
                        ui.AddToggle(showAlwaysFirstScriptIcon,
                                "If enabled, will always show the first component icon which overrides all of the other rules. \n" +
                                "- Will reduce the amount of checks for each component inside of the GameObjects. \n" +
                                $"- Also overrides a special case where the first UI component is `{nameof(CanvasRenderer)}` " +
                                "and the icon is the last unity component icon, since that's how Unity's default GameObjects are created.")
                            .style.marginTop = VERTICAL_SPACE_PADDING;
                    }

                    void AddUserScriptForLoopDetectionOptions()
                    {
                        var userScriptDetectionTooltip = ui.AddTooltip("How does user-script detection work",
                            "The script determines whether a script is user-made or Unity-made by analyzing its namespace. \n" +
                            $"Choose your preferred detection option in the '{unityScriptDetectionType.Label}' field. \n" +
                            $"For more information about the differences between each option in the '{unityScriptDetectionType.Label}' field, " +
                            $"hover over it.");
                        var userScriptsSelection = ui.AddSelection(containsNonUnityScripts,
                            "The icon style when there are any user-made scripts inside of a GameObject.");
                        var unityScriptTypeSelection = ui.AddSelection(unityScriptDetectionType,
                            "The namespace keyword determines whether a script is Unity-related or created by a user. " +
                            "If your scripts' namespaces contain this keyword, they will be considered part of Unity-native scripts. \n\n" +
                            $"Choose the '{nameof(UnityNativeScriptsDetectionType.UnityEngine)}' option for stricter detection of Unity-native namespaces, \n" +
                            $"while the '{nameof(UnityNativeScriptsDetectionType.Unity)}' option is more general " +
                            "and checks only for the 'Unity' keyword in the entire namespace. \n\n" +
                            "The second option is useful when you have additional plugins with the 'Unity' keyword " +
                            "that you want to be considered as part of non-user scripts.");
                        
                        showAlwaysFirstScriptIcon.OnValueChanged += value => userScriptsSelection.SetEnabled(!value);
                        showAlwaysFirstScriptIcon.OnValueChanged += value => userScriptDetectionTooltip.SetEnabled(!value);
                        showAlwaysFirstScriptIcon.OnValueChanged += value => unityScriptTypeSelection.SetEnabled(!value);
                    }

                    void AddPrefabDetectionOptions()
                    {
                        var isAPrefabToggle = ui.AddToggle(isOverridePrefabIconType,
                            $"If enabled, will override the general rules for prefabs " +
                            $"(select the icon type in the '{isAPrefab}'). " +
                            $"Note that this applies to all of the children of the prefab as well.");
                        isAPrefabToggle.style.paddingTop = VERTICAL_SPACE_PADDING;
                        var isAPrefabSelection = ui.AddSelection(isAPrefab,
                            "Select the Script Icon Type for Prefabs.");
                        
                        isAPrefabSelection.SetEnabled(isOverridePrefabIconType.Get());
                        isOverridePrefabIconType.OnValueChanged += value => isAPrefabSelection.SetEnabled(value);
                    }
                    
                    void AddHierarchyTooltipOption()
                    {
                        ui.AddToggle(isEnableTooltipsOnHierarchyObject,
                            "If enabled, a tooltip of the name of the component icon will show up " +
                            "when you hover on top of a GameObject in the hierarchy.")
                            .style.marginTop = VERTICAL_SPACE_PADDING;
                    }

                    void ActivateUIListenersFirstTime()
                    {
                        showAlwaysFirstScriptIcon.Set(showAlwaysFirstScriptIcon.Get());
                        isOverridePrefabIconType.Set(isOverridePrefabIconType.Get());
                    }
                }
            };
        }
#endif
    }
}