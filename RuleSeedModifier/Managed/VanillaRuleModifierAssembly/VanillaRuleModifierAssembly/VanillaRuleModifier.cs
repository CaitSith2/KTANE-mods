using System.Collections.Generic;
using UnityEngine;
using System.IO;
using VanillaRuleModifierAssembly;
using VanillaRuleModifierAssembly.RuleSetGenerators;
using static VanillaRuleModifierAssembly.CommonReflectedTypeInfo;
using Settings = VanillaRuleModifierAssembly.ModSettings;


// ReSharper disable once CheckNamespace
public class VanillaRuleModifier : MonoBehaviour
{
    private KMGameInfo _gameInfo = null;
    public Settings _modSettings;

    // Use this for initialization
    // ReSharper disable once UnusedMember.Local
    private static bool _fixesApplied = false;
    private static void ApplyBugFixes()
    {
        if (_fixesApplied) return;
        //DebugLog("No Fixes to apply. :)");
        _fixesApplied = true;
    }

	internal static RuleSeedModifierProperties[] PublicProperties = new RuleSeedModifierProperties[2];
	internal static HashSet<string> ModsThatSupportRuleSeedModifier = new HashSet<string>
	{
		"WireSetComponentSolver","ButtonComponentSolver",
		"WireSequenceComponentSolver","WhosOnFirstComponentSolver",
		"VennWireComponentSolver","SimonComponentSolver",
		"PasswordComponentSolver","NeedyKnobComponentSolver",
		"MorseCodeComponentSolver","MemoryComponentSolver",
		"KeypadComponentSolver","InvisibleWallsComponentSolver"
	};
    private bool _started = false;
    private void Start ()
    {
        _started = true;
	    DebugLog("Service prefab Instantiated.");
	    ApplyBugFixes();
        //DestroyImmediate(GetComponent<KMService>()); //Hide from Mod Selector
        _modSettings = new Settings(GetComponent<KMModSettings>());

	    if (!Initialize())
	    {
	        DebugLog("Failed to initialize the reflection component of Vanilla Rule Modifier. Aborting load");
	        return;
	    }
	    if (!_modSettings.ReadSettings())
	    {
	        DebugLog("Failed to initialize Mod settings. Aborting load");
	        return;
	    }

        GameObject infoObject = new GameObject("VanillaRuleModifierProperties");
        infoObject.transform.parent = gameObject.transform;
        PublicProperties[0] = infoObject.AddComponent<RuleSeedModifierProperties>();
        PublicProperties[0].VanillaRuleModifer = this;

	    GameObject infoObject2 = new GameObject("RuleSeedModifierProperties");
	    infoObject2.transform.parent = gameObject.transform;
	    PublicProperties[1] = infoObject2.AddComponent<RuleSeedModifierProperties>();
	    PublicProperties[1].VanillaRuleModifer = this;

	    foreach (string mod in ModsThatSupportRuleSeedModifier)
	    {
		    RuleSeedModifierProperties.AddSupportedModule(mod);
	    }

		_gameInfo = GetComponent<KMGameInfo>();
        LoadMod();
        
	    DebugLog("Service started");
    }

    public void SetRuleSeed(int seed, bool writeSettings)
    {
        if (seed == int.MinValue) seed = 0;
        _modSettings.Settings.RuleSeed = Mathf.Abs(seed);
        if (writeSettings) _modSettings.WriteSettings();
    }

    public string GenerateManual()
    {
        if(CurrentState == KMGameInfo.State.Setup || CurrentState == KMGameInfo.State.PostGame)
            GenerateRules(_modSettings.Settings.RuleSeed);
        ManualGenerator.Instance.WriteManual(_modSettings.Settings.RuleSeed);
        return Path.Combine(Application.persistentDataPath, Path.Combine("ModifiedManuals", _modSettings.Settings.RuleSeed.ToString()));
    }

    private bool _enabled;
    // ReSharper disable once UnusedMember.Local
    private void OnDestroy()
    {
        DebugLog("Service prefab destroyed. Shutting down.");
        UnloadMod();
        _started = false;
    }

    private void OnEnable()
    {
        if (!_started || _enabled) return;
        DebugLog("Service prefab Enabled.");
        LoadMod();
    }

    private void OnDisable()
    {
        if (!_enabled) return;
        DebugLog("Service Prefab Disabled.");
        UnloadMod();
    }

    private void LoadMod()
    {
        _gameInfo.OnStateChange += OnStateChange;
        _enabled = true;
        CurrentState = KMGameInfo.State.Setup;
        _prevState = KMGameInfo.State.Setup;
    }

    private void UnloadMod()
    {
        UnloadRuleManager();
        _gameInfo.OnStateChange -= OnStateChange;
        _enabled = false;

        StopAllCoroutines();
    }

    public KMGameInfo.State CurrentState = KMGameInfo.State.Unlock;
    private KMGameInfo.State _prevState = KMGameInfo.State.Unlock;
    private void OnStateChange(KMGameInfo.State state)
    {
        DebugLog("Transitioning from {1} to {0}", state, CurrentState);
        if((_prevState == KMGameInfo.State.Setup || _prevState == KMGameInfo.State.PostGame) && CurrentState == KMGameInfo.State.Transitioning && state == KMGameInfo.State.Transitioning)
        {
            _modSettings.ReadSettings();
            var seed = _modSettings.Settings.RuleSeed;
            DebugLog("Generating Rules based on Seed {0}", seed);
            GenerateRules(seed);
            ManualGenerator.Instance.WriteManual(seed);
        }
        _prevState = CurrentState;
        CurrentState = state;
    }
}
