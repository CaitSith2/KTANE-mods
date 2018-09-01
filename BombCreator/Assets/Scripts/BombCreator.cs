using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Assets.Scripts;
using Newtonsoft.Json;
using UnityEngine;
using Random = System.Random;

// ReSharper disable once CheckNamespace
public class BombCreator : MonoBehaviour
{
    public Transform BackingTransform;

    public TextMesh TwitchModeLabel;
    public KMSelectable TwitchModeButton;

    public TextMesh TimeText;
    public KMSelectable TimeMinusButton;
    public KMSelectable TimePlusButton;

    public TextMesh ModulesText;
    public KMSelectable ModulesMinusButton;
    public KMSelectable ModulesPlusButton;

    public TextMesh StrikesText;
    public KMSelectable StrikesMinusButton;
    public KMSelectable StrikesPlusButton;

    public TextMesh WidgetsText;
    public KMSelectable WidgetsMinimumMinusButton;
    public KMSelectable WidgetsMinimumPlusButton;
    public KMSelectable WidgetsMaximumMinusButton;
    public KMSelectable WidgetsMaximumPlusButton;

    public TextMesh ModuleDisableText;
    public KMSelectable ModuleDisableMinusButton;
    public KMSelectable ModuleDisablePlusButton;
    public KMSelectable ModuleDisableButton;

    public TextMesh FactoryModeText;
    public KMSelectable FactoryModeMinusButton;
    public KMSelectable FactoryModePlusButton;

    public TextMesh SeedText;
    public KMSelectable SeedMinusButton;
    public KMSelectable SeedDigitSelectbutton;
    public KMSelectable SeedManualButton;
    public KMSelectable SeedPlusButton;

    public TextMesh NeediesText;
    public KMSelectable NeedyMinusButton;
    public KMSelectable NeedyPlusButton;

    public TextMesh PlayModeText;
    public KMSelectable PlayModeMinusButton;
    public KMSelectable PlayModePlusButton;

    public TextMesh PacingEventsText;
    public KMSelectable PacingEventsButton;

    public TextMesh FrontFaceText;
    public KMSelectable FrontFaceButton;

    public TextMesh BombsText;
    public KMSelectable BombsMinusButton;
    public KMSelectable BombsPlusButton;

    public TextMesh DuplicateText;
    public KMSelectable DuplicateButton;

    public KMSelectable ResetButton;
    public KMSelectable StartButton;
    public KMSelectable SaveButton;
    private List<KMGameInfo.KMModuleInfo> _vanillaModules;

    public List<Transform> SettingRows;

	public Transform ArtworkBase;
    public List<Transform> Artwork;

    public KMAudio Audio;
    public KMGameInfo GameInfo;

    private int _maxModules = 11;
    private int _maxFrontFace = 5;

    private int _currentSeedDigit = 0;
    private float _seedDigitSelectHoldTime = 0;

    private const string InfiniteSign = "∞";

    private readonly ModSettings _modSettings = new ModSettings("BombCreator");
    private ModuleSettings Settings { get { return _modSettings.Settings; } }

    private static readonly Random Random = new Random();
    public delegate bool boolDelegate();

    private static Type _gameplayStateType;
    private static FieldInfo _gameplayroomPrefabOverrideField;

    private static Type _elevatorRoomType;

    static BombCreator()
    {
        _elevatorRoomType = ReflectionHelper.FindType("ElevatorRoom");
        _gameplayStateType = ReflectionHelper.FindType("GameplayState");
        if (_gameplayStateType != null)
            _gameplayroomPrefabOverrideField = _gameplayStateType.GetField("GameplayRoomPrefabOverride", BindingFlags.Public | BindingFlags.Static);
    }

    public static void DebugLog(string message, params object[] args)
    {
        var debugstring = String.Format("[BombCreator] {0}", message);
        Debug.LogFormat(debugstring, args);
    }

    private bool InfiniteMode()
    {
        int index = Settings.FactoryMode;
        if (!FactoryRoom.Installed()) return false;
        List<string> list = FactoryRoom.GetFactoryModes();
        if (list == null || index < 0 || index >= list.Count) return false;
        return list[index].Contains(InfiniteSign);
    }

    private void ResizeBacking(Transform row)
    {
        int rowIndex = SettingRows.IndexOf(row);
        if (rowIndex < 0) return;
        row.gameObject.SetActive(false);

        Vector3 size = BackingTransform.localScale;
        BackingTransform.localScale = new Vector3(size.x, size.y, size.z - 0.1f);
	    size = ArtworkBase.localScale;
	    ArtworkBase.localScale = new Vector3(size.x - 0.075f, size.y, size.z - 0.075f);

        for (var i = 0; i < SettingRows.Count; i++)
            MoveTransform(SettingRows[i], i > rowIndex);
    }

    private void MoveTransform(Transform t, bool direction)
    {
        Vector3 pos = t.localPosition;
        t.localPosition = direction 
            ? new Vector3(pos.x, pos.y, pos.z + 0.0175f) //Up
            : new Vector3(pos.x, pos.y, pos.z - 0.0175f);//Down
    }

    private IEnumerator DelayUpdateDisplay()
    {
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        UpdateDisplay();
        UpdateArtwork();
        Settings.TwitchPlaysTimeModeTime = TwitchPlays.TimeModeTimeLimit(300, true);
    }

    private IEnumerator HideMultipleBombsButtons()
    {
        var installed = MultipleBombs.Refresh();
        while (installed.MoveNext())
        {
            yield return installed.Current;
        }
        if (MultipleBombs.Installed()) yield break;
        BombsMinusButton.gameObject.SetActive(false);
        BombsPlusButton.gameObject.SetActive(false);
    }

    private IEnumerator HideVanillaSeed()
    {
        var installed = VanillaRuleModifier.Refresh();
        while (installed.MoveNext())
        {
            yield return installed.Current;
        }
        if (VanillaRuleModifier.Installed()) yield break;
        ResizeBacking(SeedManualButton.transform.parent);
    }

    private IEnumerator HideFactoryMode()
    {
        var installed = FactoryRoom.Refresh();
        while (installed.MoveNext())
        {
            yield return installed.Current;
        }
        if (FactoryRoom.Installed() && FactoryRoom.GetFactoryModes() != null && FactoryRoom.GetFactoryModes().Count > 0) yield break;
        ResizeBacking(FactoryModeMinusButton.transform.parent);
    }

	private IEnumerator HideTwitchModeButton()
	{
		var installed = TwitchPlays.Refresh();
		while (installed.MoveNext())
		{
			yield return installed.Current;
		}

		if (TwitchPlays.Installed()) yield break;
		TwitchModeButton.gameObject.SetActive(false);
	}

    private void Start()
    {
        _modSettings.ReadSettings();
        StartCoroutine(HideMultipleBombsButtons());
        StartCoroutine(HideVanillaSeed());
        StartCoroutine(HideFactoryMode());
        StartCoroutine(DelayUpdateDisplay());
        StartCoroutine(HideTwitchModeButton());

        _vanillaModules = GameInfo.GetAvailableModuleInfo().Where(x => !x.IsMod).ToList();
 
        ChangeModuleDisableIndex(0);

        TwitchModeButton.OnInteract += delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
	        if (!TwitchPlays.TimeMode() && !TwitchPlays.ZenMode())
	        {
		        TwitchPlays.SetTimeMode(true);
	        }
	        else
	        {
		        TwitchPlays.SetTimeMode(false);
		        TwitchPlays.SetZenMode(!TwitchPlays.ZenMode());
	        }

	        UpdateDisplay();
            return false;
        };

        TimeMinusButton.OnInteract += delegate { StartCoroutine(AddTimer(TwitchPlays.TimeMode() ? -60 : -30)); return false; };
        TimePlusButton.OnInteract += delegate { StartCoroutine(AddTimer(TwitchPlays.TimeMode() ? 60 : 30)); return false; };

        ModulesMinusButton.OnInteract += delegate { StartCoroutine(AddModules(-1)); return false; };
        ModulesPlusButton.OnInteract += delegate { StartCoroutine(AddModules(1)); return false; };

        WidgetsMinimumMinusButton.OnInteract += delegate {  StartCoroutine(AddWidgetsMinimum(-1)); return false; };
        WidgetsMinimumPlusButton.OnInteract += delegate {  StartCoroutine(AddWidgetsMinimum(1)); return false; };
        WidgetsMaximumMinusButton.OnInteract += delegate { StartCoroutine(AddWidgetsMaximum(-1)); return false; };
        WidgetsMaximumPlusButton.OnInteract += delegate { StartCoroutine(AddWidgetsMaximum(1)); return false; };

        StrikesMinusButton.OnInteract += delegate {  StartCoroutine(AddStrikes(-1)); return false; };
        StrikesPlusButton.OnInteract += delegate { StartCoroutine(AddStrikes(1)); return false; };

        ModuleDisableMinusButton.OnInteract += () => ChangeModuleDisableIndex(-1);
        ModuleDisablePlusButton.OnInteract += () => ChangeModuleDisableIndex(1);
        ModuleDisableButton.OnInteract += ModuleDisableButtonPressed;

        SeedMinusButton.OnInteract += delegate { StartCoroutine(AddSeed(-1)); return false; };
        SeedDigitSelectbutton.OnInteract += delegate { Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform); _seedDigitSelectHoldTime = Time.time; return false; };
        SeedManualButton.OnInteract += OpenManualDirectory;
        SeedPlusButton.OnInteract += delegate { StartCoroutine(AddSeed(1)); return false; };

        FactoryModeMinusButton.OnInteract += () => SetFactoryMode(-1);
        FactoryModePlusButton.OnInteract += () => SetFactoryMode(1);

        NeedyMinusButton.OnInteract += delegate { StartCoroutine(AddNeedyModules(-1)); return false; };
        NeedyPlusButton.OnInteract += delegate { StartCoroutine(AddNeedyModules(1)); return false; };
        PlayModeMinusButton.OnInteract += delegate { StartCoroutine(AddVanillaModules(1)); return false; };
        PlayModePlusButton.OnInteract += delegate { StartCoroutine(AddVanillaModules(-1)); return false; };


        PacingEventsButton.OnInteract += ChangePacingEvent;
        FrontFaceButton.OnInteract += ChangeFrontFace;

        BombsMinusButton.OnInteract += delegate { StartCoroutine(AddBombs(-1)); return false; };
        BombsPlusButton.OnInteract += delegate { StartCoroutine(AddBombs(1)); return false; };
        DuplicateButton.OnInteract += DuplicatesAllowed;

        StartButton.OnInteract += StartMission;
        SaveButton.OnInteract += () => SaveSettings();
        ResetButton.OnInteract += delegate { StartCoroutine(ResetSettings()); return false; };


        TwitchModeButton.OnInteractEnded += () => EndInteract(false);
        TimeMinusButton.OnInteractEnded += () => { EndInteract(); TwitchPlays.SetTimeModeTimeLimit(BombTime); };
        TimePlusButton.OnInteractEnded += () => { EndInteract(); TwitchPlays.SetTimeModeTimeLimit(BombTime); };

        ModulesMinusButton.OnInteractEnded += () => EndInteract();
        ModulesPlusButton.OnInteractEnded += () => EndInteract();

        WidgetsMinimumMinusButton.OnInteractEnded += () => EndInteract();
        WidgetsMinimumPlusButton.OnInteractEnded += () => EndInteract();
        WidgetsMaximumMinusButton.OnInteractEnded += () => EndInteract();
        WidgetsMaximumPlusButton.OnInteractEnded += () => EndInteract();

        StrikesMinusButton.OnInteractEnded += () => EndInteract();
        StrikesPlusButton.OnInteractEnded += () => EndInteract();

        ModuleDisableMinusButton.OnInteractEnded += () => EndInteract(false);
        ModuleDisablePlusButton.OnInteractEnded += () => EndInteract(false);
        ModuleDisableButton.OnInteractEnded += () => EndInteract(false);

        SeedMinusButton.OnInteractEnded += () => EndInteract();
        SeedDigitSelectbutton.OnInteractEnded += EndSeedDigitInteract;
        SeedManualButton.OnInteractEnded += () => EndInteract(false);
        SeedPlusButton.OnInteractEnded += () => EndInteract();

        FactoryModeMinusButton.OnInteractEnded += () => EndInteract(false);
        FactoryModePlusButton.OnInteractEnded += () => EndInteract(false);

        NeedyMinusButton.OnInteractEnded += () => EndInteract();
        NeedyPlusButton.OnInteractEnded += () => EndInteract();
        PlayModeMinusButton.OnInteractEnded += () => EndInteract();
        PlayModePlusButton.OnInteractEnded += () => EndInteract();

        PacingEventsButton.OnInteractEnded += () => EndInteract(false);
        FrontFaceButton.OnInteractEnded += () => EndInteract(false);

        BombsMinusButton.OnInteractEnded += () => EndInteract();
        BombsPlusButton.OnInteractEnded += () => EndInteract();
        DuplicateButton.OnInteractEnded += () => EndInteract(false);

        ResetButton.OnInteractEnded += CancelSettingsReset;
        StartButton.OnInteractEnded += () => EndInteract(false);
        SaveButton.OnInteractEnded += () => EndInteract(false);

    }

    private void Update()
    {
        if (!TwitchPlays.Installed()) return;
        if (!TwitchPlays.TimeMode() && !TwitchPlays.ZenMode())
        {
	        if (TwitchModeLabel.text == "Normal Mode") return;
	        TwitchModeLabel.text = "Normal Mode";
	        UpdateDisplay();
        }
        else if (TwitchPlays.TimeMode())
        {
	        if (TwitchModeLabel.text == "Time Mode") return;
            TwitchModeLabel.text = "Time Mode";
	        UpdateDisplay();
        }
        else
        {
	        if (TwitchModeLabel.text == "Zen Mode") return;
            TwitchModeLabel.text = "Zen Mode";
	        UpdateDisplay();
        }
    }

    private void EndSeedDigitInteract()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
        float time = Time.time - _seedDigitSelectHoldTime;
        _currentSeedDigit += time < 0.25f ? 1 : 9;
        _currentSeedDigit %= 10;
        UpdateDisplay();
    }

    private bool OpenManualDirectory()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        if (VanillaRuleModifier.Installed())
        {
            Application.OpenURL("file:///" + VanillaRuleModifier.GetRuleManualDirectory());
        }
        return false;
    }

    private bool DuplicatesAllowed()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        Settings.DuplicatesAllowed = !Settings.DuplicatesAllowed;
        UpdateDisplay();
        return false;
    }

    private bool SetFactoryMode(int offset)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        Settings.FactoryMode += offset;
        UpdateDisplay();
        return false;
    }

    private void EndInteract(bool stop=true)
    {
        _twitchPlays = false;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
        if(stop)
            StopAllCoroutines();
        UpdateDisplay();
        UpdateModuleDisableDisplay();
    }

    private void UpdateArtwork()
    {
        foreach (Transform t in Artwork)
        {
            t.gameObject.SetActive(false);
        }

	    var artwork = Artwork[0];

		while(Artwork[0] == artwork)
			Artwork = Artwork.OrderBy(x => Random.NextDouble()).ToList();

        Artwork[0].gameObject.SetActive(true);
    }

    private int GetMaxModules()
    {
        try
        {
            GameObject roomPrefab = (GameObject) _gameplayroomPrefabOverrideField.GetValue(null);
            if (roomPrefab != null)
            {
                if (roomPrefab.GetComponentInChildren(_elevatorRoomType, true) != null) return 54;
            }
        }
        catch (Exception ex)
        {
            DebugLog("Could not check for elevator bomb due to an exception: {0}\nStack Trace: {1}", ex.Message, ex.StackTrace);
        }

        _maxModules = GameInfo.GetMaximumBombModules();
        _maxFrontFace = GameInfo.GetMaximumModulesFrontFace();
        return Settings.FrontFaceOnly ? _maxFrontFace : _maxModules;
    }

    private const float StartDelay = 0.2f;
    private const float Acceleration = 0.005f;
    private const float MinDelay = 0.01f;
	private bool _twitchPlays = false;

    private int BombTime
    {
        get { return TwitchPlays.TimeMode() ? Settings.TwitchPlaysTimeModeTime : Settings.Time; }
        set
        {
            if (TwitchPlays.TimeMode()) Settings.TwitchPlaysTimeModeTime = value;
            else Settings.Time = value;
        }
    }

    private IEnumerator AddTimer(int timer, boolDelegate endWhen = null)
    {
        if (endWhen != null && endWhen.Invoke()) yield break;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        int target = 7200;
        int add = 3600;
        int startingTimer = BombTime;
        var delay = endWhen == null ? StartDelay : MinDelay;
        while (endWhen == null || !endWhen.Invoke())
        {
            BombTime += timer;
            UpdateDisplay();
            yield return new WaitForSeconds(Mathf.Max(delay, MinDelay));
            delay -= Acceleration;
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.FastestTimerBeep, transform);
            if (Mathf.Abs(BombTime - startingTimer) != target) continue;
            if (timer > 0)
                timer = add;
            else
                timer = -add;
	        switch (target)
	        {
				case 7200:	//2 Hours
					target = 864000;
					add = 86400;
					break;
				default:
					target *= 100;
					add *= 100;
					break;
			}
	        if (endWhen == null) {yield return new WaitForSeconds(0.5f);delay = StartDelay;}
        }
        TwitchPlays.SetTimeModeTimeLimit(BombTime);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
    }

    private IEnumerator AddModules(int count, boolDelegate endWhen = null)
    {
        if (endWhen != null && endWhen.Invoke()) yield break;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        var delay = endWhen == null ? StartDelay : MinDelay;
        var startingModules = Settings.Modules;
        int target = 500;
        int add = 100;
        while (endWhen == null || !endWhen.Invoke())
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.FastestTimerBeep, transform);
            Settings.Modules += count;
            UpdateDisplay();
            yield return new WaitForSeconds(Mathf.Max(delay, MinDelay));
            delay -= Acceleration;
            if (Mathf.Abs(Settings.Modules - startingModules) != target) continue;
            if (count > 0)
                count = add;
            else
                count = -add;
            target *= 100;
            add *= 100;
	        if (endWhen == null) { yield return new WaitForSeconds(0.5f); delay = StartDelay; }
		}
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
    }

    private IEnumerator AddBombs(int count, boolDelegate endWhen = null)
    {
        if (endWhen != null && endWhen.Invoke()) yield break;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        var delay = endWhen == null ? StartDelay : MinDelay;
        while (endWhen == null || !endWhen.Invoke())
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.FastestTimerBeep, transform);
            if(!InfiniteMode())
                Settings.Bombs += count;
            UpdateDisplay();
            yield return new WaitForSeconds(Mathf.Max(delay, MinDelay));
            delay -= Acceleration;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
    }

    private IEnumerator AddStrikes(int count, boolDelegate endWhen = null)
    {
        if (endWhen != null && endWhen.Invoke()) yield break;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        var delay = endWhen == null ? StartDelay : MinDelay;
        var startingStrikes = Settings.Strikes;
        int target = 500;
        int add = 100;
        while (endWhen == null || !endWhen.Invoke())
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.FastestTimerBeep, transform);
            Settings.Strikes += count;
            UpdateDisplay();
            yield return new WaitForSeconds(Mathf.Max(delay, MinDelay));
            delay -= Acceleration;
            if (Mathf.Abs(Settings.Strikes - startingStrikes) != target) continue;
            if (count > 0)
                count = add;
            else
                count = -add;
            target *= 100;
            add *= 100;
	        if (endWhen == null) { yield return new WaitForSeconds(0.5f); delay = StartDelay; }
		}
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
    }

    private IEnumerator AddWidgetsMinimum(int count, boolDelegate endWhen = null)
    {
        if (endWhen != null && endWhen.Invoke()) yield break;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        var delay = endWhen == null ? StartDelay : MinDelay;
        while (endWhen == null || !endWhen.Invoke())
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.FastestTimerBeep, transform);
            Settings.WidgetsMinimum += count;
            UpdateDisplay();
            yield return new WaitForSeconds(Mathf.Max(delay, MinDelay));
            delay -= Acceleration;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
    }

    private IEnumerator AddWidgetsMaximum(int count, boolDelegate endWhen = null)
    {
        if (endWhen != null && endWhen.Invoke()) yield break;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        var delay = endWhen == null ? StartDelay : MinDelay;
        while (endWhen == null || !endWhen.Invoke())
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.FastestTimerBeep, transform);
            Settings.WidgetsMaximum += count;
            UpdateDisplay();
            yield return new WaitForSeconds(Mathf.Max(delay, MinDelay));
            delay -= Acceleration;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
    }

    private IEnumerator AddSeed(long count, boolDelegate endWhen = null)
    {
        int[] multipliers = new[] {1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000};
        count *= multipliers[_currentSeedDigit];

        if (endWhen != null && endWhen.Invoke()) yield break;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        if (!VanillaRuleModifier.Installed())
            yield break;
        var delay = endWhen == null ? StartDelay : MinDelay;
        long currentSeed = VanillaRuleModifier.GetRuleSeed();
        long startingSeed = currentSeed;
        long target = 10;
        long add = 10;
        while (endWhen == null || !endWhen.Invoke())
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.FastestTimerBeep, transform);

            currentSeed += count;
            if (currentSeed > int.MaxValue) currentSeed = int.MaxValue;
            if (currentSeed < 0) currentSeed = 0;
            VanillaRuleModifier.SetRuleSeed((int)currentSeed);
            UpdateDisplay();

            yield return new WaitForSeconds(Mathf.Max(delay, MinDelay));
            delay -= Acceleration;
            if (Math.Abs(currentSeed - startingSeed) != target) continue;
            if (count > 0)
                count = add;
            else
                count = -add;
            target *= 10;
            add *= 10;
	        if (endWhen == null) { yield return new WaitForSeconds(0.5f); delay = StartDelay; }
		}
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
    }

    private bool _resetting = false;

    private IEnumerator ResetSettings(boolDelegate endWhen = null)
    {
        if (endWhen != null && endWhen.Invoke()) yield break;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.NormalTimerBeep, transform);
        _resetting = true;
        var prev = 4;
        for (var i = 5f; i > 0 && (endWhen == null || !endWhen.Invoke()); i-=Time.deltaTime)
        {
            if (prev > i)
            {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.NormalTimerBeep, transform);
                prev = Mathf.FloorToInt(i);
            }
            BombsText.text = string.Format("Reset in {0:0.00}", i);
            yield return null;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BombExplode, transform);
        _modSettings.Settings = new ModuleSettings();
        _resetting = false;

        UpdateDisplay();
        UpdateModuleDisableDisplay();

        BombsText.text = "Settings Reset";
        yield return new WaitForSeconds(3);

        UpdateDisplay();
        UpdateModuleDisableDisplay();
    }

    private void CancelSettingsReset()
    {
        if (!_resetting)
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
        else
            EndInteract();

        _resetting = false;
        UpdateArtwork();
    }
    
    private bool SaveSettings(bool sound = true)
    {
        if (sound)
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        _modSettings.WriteSettings();
	    var seed = VanillaRuleModifier.GetRuleSeed();
	    if (seed == int.MinValue) seed = 0;
		VanillaRuleModifier.SetRuleSeed(Mathf.Abs(seed), true);
        return false;
    }

    private void ClampSettings()
    {
        Settings.Time = Mathf.Max(30, Settings.Time);
        Settings.TwitchPlaysTimeModeTime = Mathf.Max(60, Settings.TwitchPlaysTimeModeTime);
        Settings.Modules = Mathf.Clamp(Settings.Modules, 1, GetMaxModules());
        Settings.Strikes = Mathf.Max(1, Settings.Strikes);
        Settings.Bombs = Mathf.Clamp(Settings.Bombs, 1, MultipleBombs.GetMaximumBombCount());
        Settings.WidgetsMinimum = Mathf.Clamp(Settings.WidgetsMinimum, 0, Settings.WidgetsMaximum);
        Settings.WidgetsMaximum = Mathf.Clamp(Settings.WidgetsMaximum, Settings.WidgetsMinimum, 50);
        Settings.NeedyModules = Mathf.Clamp(Settings.NeedyModules, 0, Settings.Modules - 1);
        if(FactoryRoom.Installed())
            Settings.FactoryMode = Mathf.Clamp(Settings.FactoryMode, 0, FactoryRoom.GetFactoryModes().Count - 1);
        Settings.VanillaModules = Mathf.Clamp(Settings.VanillaModules, 0, 100);
    }

    private void UpdateDisplay()
    {
        ClampSettings();

        var t = TimeSpan.FromSeconds(BombTime);
        TimeText.text = t.ToString();
        if (TimeText.text.StartsWith("00:0"))
        {
            TimeText.text = TimeText.text.Remove(0, 4);
        }
        else if (TimeText.text.StartsWith("00:"))
        {
            TimeText.text = TimeText.text.Remove(0, 3);
        }
        else if (TimeText.text.StartsWith("0"))
        {
            TimeText.text = TimeText.text.Remove(0, 1);
        }
        ModulesText.text = "" + Settings.Modules;
        WidgetsText.text = string.Format("{0} to {1}", Settings.WidgetsMinimum, Settings.WidgetsMaximum);
        BombsText.text = !MultipleBombs.Installed() ? "" : "Bombs: " + (InfiniteMode() ? InfiniteSign : Settings.Bombs.ToString());
        StrikesText.text = "" + Settings.Strikes;
        NeediesText.text = Settings.NeedyModules > 0 ? string.Format("Needies: {0}",Settings.NeedyModules) : "Needy Off";
        DuplicateText.text = Settings.DuplicatesAllowed ? "Duplicates" : "No Duplicates";
        // ReSharper disable once SwitchStatementMissingSomeCases
        switch (Settings.VanillaModules)
        {
            case 0:
                PlayModeText.text = "Mods Only";
                break;
            case 100:
                PlayModeText.text = "Vanilla Only";
                break;
            default:
                PlayModeText.text = string.Format("Vanilla {0}% - Mods {1}%", Settings.VanillaModules, 100 - Settings.VanillaModules);
                break;
        }

        PacingEventsText.text = Settings.PacingEvents ? "Pacing Events On" : "Pacing Events Off";
        FrontFaceText.text = Settings.FrontFaceOnly ? "Front Face Only" : "All Faces";

        SeedText.text = VanillaRuleModifier.GetRuleSeed() < 0 ? "-" : "";
        var seedDigits = VanillaRuleModifier.GetRuleSeed().ToString("D" + (_currentSeedDigit + 1)).Replace("-","").Select(x => x.ToString()).ToArray();
        for (int i = 0; i < seedDigits.Length; i++)
        {
            SeedText.text += i == ((seedDigits.Length - 1) - _currentSeedDigit) 
                ? "<color=\"red\">" + seedDigits[i] + "</color>" 
                : seedDigits[i];
        }

        if(FactoryRoom.Installed())
            FactoryModeText.text = FactoryRoom.GetFactoryModes()[Settings.FactoryMode];
    }

    private bool ChangeModuleDisableIndex(int diff)
    {
        if (diff != 0)
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);

        if (_vanillaModules.Count == 0)
            return false;

        Settings.ModuleDisableIndex += diff;
        if (Settings.ModuleDisableIndex < 0)
        {
            Settings.ModuleDisableIndex = _vanillaModules.Count - 1;
        }
        else if (Settings.ModuleDisableIndex >= _vanillaModules.Count)
        {
            Settings.ModuleDisableIndex = 0;
        }

        UpdateModuleDisableDisplay();
        return false;
    }

    private void UpdateModuleDisableDisplay()
    {
        if (_vanillaModules.Count <= 0) return;

        var moduleInfo = _vanillaModules[Settings.ModuleDisableIndex];
        ModuleDisableText.text = moduleInfo.DisplayName;
        ModuleDisableText.color = Settings.DisabledModuleIds.Contains(moduleInfo.ModuleId) 
            ? Color.red 
            : Color.white;
    }

    private bool ModuleDisableButtonPressed()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        if (_vanillaModules.Count <= 0) return false;

        var moduleInfo = _vanillaModules[Settings.ModuleDisableIndex];
        if(Settings.DisabledModuleIds.Contains(moduleInfo.ModuleId))
        {
            Settings.DisabledModuleIds.Remove(moduleInfo.ModuleId);
        }
        else
        {
            Settings.DisabledModuleIds.Add(moduleInfo.ModuleId);
        }

        UpdateModuleDisableDisplay();
        return false;
    }

    private IEnumerator AddNeedyModules(int count, boolDelegate endWhen = null)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        float countFloat = count;
        var delay = endWhen == null ? StartDelay : MinDelay;
        while (endWhen == null || !endWhen.Invoke())
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.FastestTimerBeep, transform);
            Settings.NeedyModules += (int)countFloat;
            if (Settings.NeedyModules >= Settings.Modules)
                Settings.Modules = Settings.NeedyModules + 1;
            UpdateDisplay();
            yield return new WaitForSeconds(Mathf.Max(delay, MinDelay));
            delay -= Acceleration;
            if (count > 0)
                countFloat += (1.0f / 30.0f);
            else
                countFloat -= (1.0f / 30.0f);
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
    }

    private IEnumerator AddVanillaModules(int count, boolDelegate endWhen = null)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        var delay = endWhen == null ? StartDelay : MinDelay;
        var initial = Settings.VanillaModules;
        while (endWhen == null || !endWhen.Invoke())
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.FastestTimerBeep, transform);
            Settings.VanillaModules += count;
            UpdateDisplay();
            yield return new WaitForSeconds(Mathf.Max(delay, MinDelay));
            delay -= Acceleration;

            if (Mathf.Abs(Settings.VanillaModules - initial) < 5) continue;
            if (count > 0) count = 5;
            else count = -5;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
    }


    private bool ChangePacingEvent()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        Settings.PacingEvents = !Settings.PacingEvents;
        UpdateDisplay();
        return false;
    }

    private bool ChangeFrontFace()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        Settings.FrontFaceOnly = !Settings.FrontFaceOnly;
        UpdateDisplay();
        return false;
    }

    private static string FormatTime(int secs)
    {
        int days = secs / 86400;
        int hours = secs / 3600;
        int mins = (secs % 3600) / 60;
        secs = secs % 60;
        return string.Format("{0}{1:D2}:{2:D2}", (days > 0 ? string.Format("{0}:{1:D2}:",days,hours) : (hours > 0 ? string.Format("{0:D2}:", hours) : "")) , mins, secs);
    }

    private KMMission CreateMission()
    {
        StringBuilder sb = new StringBuilder();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		
        if (Settings.Modules > GetMaxModules())
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, transform);
            return null;
        }

	    bool infiniteMode = (FactoryRoom.Installed() && FactoryRoom.GetFactoryModes()[Settings.FactoryMode].Contains("∞"));

		var generatorSettings = new KMGeneratorSetting
        {
            NumStrikes = Settings.Strikes,
            TimeLimit = TwitchPlays.TimeModeTimeLimit(BombTime),
            FrontFaceOnly = Settings.FrontFaceOnly,
            ComponentPools = Settings.DuplicatesAllowed ? BuildComponentPools(true) : BuildNoDuplicatesPool(true),
	        OptionalWidgetCount = Random.Next(Settings.WidgetsMinimum, Settings.WidgetsMaximum)
		};

        if (generatorSettings.ComponentPools.Count == 0)
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, transform);
            return null;
        }

		sb.Append("Bomb Creator custom Mission details\n");
	    sb.Append(string.Format("Number of Bombs = {0}\n", infiniteMode ? "∞" : Settings.Bombs.ToString()));
        sb.Append(string.Format("Number of Modules = {0}\n", Settings.Modules));
        sb.Append(string.Format("Number of Needy Modules = {0}\n", Settings.NeedyModules));
        sb.Append(string.Format("Vanilla Modules = {0}%\n", Settings.VanillaModules));
        sb.Append(string.Format("Mod Modules = {0}%\n", 100 - Settings.VanillaModules));
        sb.Append(string.Format("Number of Strikes = {0}\n", generatorSettings.NumStrikes));
        sb.Append(string.Format("Time Limit = {0}\n", FormatTime(BombTime)));
        sb.Append(string.Format("Faces = {0}\n", generatorSettings.FrontFaceOnly ? "Front Face Only" : "All Faces"));
        sb.Append(string.Format("Duplicates Allowed = {0}\n", Settings.DuplicatesAllowed ? "Yes" : "No"));
        sb.Append(string.Format("Pacing Events Enabled = {0}\n", Settings.PacingEvents ? "Yes" : "No"));
        sb.Append(string.Format("Widgets = {0}-{1}\n\n", Settings.WidgetsMinimum, Settings.WidgetsMaximum));


		int poolCount = generatorSettings.ComponentPools.Count;

        if (VanillaRuleModifier.Installed())
        {
            sb.Append(string.Format("Vanilla Rule Generator Seed = {0}\n", VanillaRuleModifier.GetRuleSeed()));
        }

		if (FactoryRoom.Installed() && Settings.FactoryMode > 0)
	    {
		    generatorSettings.ComponentPools.Add(new KMComponentPool
		    {
			    ModTypes = new List<string> { "Factory Mode" },
			    Count = Settings.FactoryMode
		    });
		    sb.Append(string.Format("Factory mode = {0}\n", FactoryRoom.GetFactoryModes()[Settings.FactoryMode]));
	    }
		
	    var generatorList = new List<KMGeneratorSetting> {generatorSettings};
        if (Settings.Bombs > 1 && !infiniteMode)
        {
			generatorSettings.ComponentPools.Add(new KMComponentPool
            {
                ModTypes = new List<string> { "Multiple Bombs" },
                Count = Settings.Bombs - 1
            });
            sb.Append(string.Format("Bombs = {0}\n", Settings.Bombs));

			//Generate additional pools for Multiple bombs
	        for (int i = 1; i < Settings.Bombs; i++)
	        {
		        var multipleBombsGeneratorSettings = new KMGeneratorSetting
		        {
			        NumStrikes = Settings.Strikes,
			        TimeLimit = TwitchPlays.TimeModeTimeLimit(BombTime),
			        FrontFaceOnly = Settings.FrontFaceOnly,
			        ComponentPools = Settings.DuplicatesAllowed ? BuildComponentPools(false) : BuildNoDuplicatesPool(false),
			        OptionalWidgetCount = Random.Next(Settings.WidgetsMinimum, Settings.WidgetsMaximum)
		        };
				generatorList.Add(multipleBombsGeneratorSettings);

		        if (multipleBombsGeneratorSettings.ComponentPools.Count == 0)
		        {
			        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, transform);
			        return null;
		        }

				generatorSettings.ComponentPools.Add(new KMComponentPool
				{
					ModTypes = new List<string> { string.Format("Multiple Bombs:{0}:{1}", i, JsonConvert.SerializeObject(multipleBombsGeneratorSettings)) },
					Count = 1
				});
			}
        }

        
        sb.Append("\n");

        if (Settings.VanillaModules > 0)
        {
            sb.Append(string.Format("Vanilla Solvable Modules Enabled = {0}\n", string.Join(", ", GameInfo.GetAvailableModuleInfo().Where(x => !x.IsMod && !x.IsNeedy && !Settings.DisabledModuleIds.Contains(x.ModuleId)).OrderBy(x => x.DisplayName).Select(x => x.DisplayName).ToArray()).Wrap(80)));
            sb.Append(string.Format("Vanilla Solvable Modules Disabled = {0}\n\n", string.Join(", ", GameInfo.GetAvailableModuleInfo().Where(x => !x.IsMod && !x.IsNeedy && Settings.DisabledModuleIds.Contains(x.ModuleId)).OrderBy(x => x.DisplayName).Select(x => x.DisplayName).ToArray()).Wrap(80)));

            if (Settings.NeedyModules > 0)
            {
                sb.Append(string.Format("Vanilla Needy Modules Enabled = {0}\n", string.Join(", ", GameInfo.GetAvailableModuleInfo().Where(x => !x.IsMod && x.IsNeedy && !Settings.DisabledModuleIds.Contains(x.ModuleId)).OrderBy(x => x.DisplayName).Select(x => x.DisplayName).ToArray()).Wrap(80)));
                sb.Append(string.Format("Vanilla Needy Modules Disabled = {0}\n\n", string.Join(", ", GameInfo.GetAvailableModuleInfo().Where(x => !x.IsMod && x.IsNeedy && Settings.DisabledModuleIds.Contains(x.ModuleId)).OrderBy(x => x.DisplayName).Select(x => x.DisplayName).ToArray()).Wrap(80)));
            }
        }
        if (Settings.VanillaModules < 100)
        {
            sb.Append(string.Format("Modded Solvable Modules Enabled = {0}\n\n", string.Join(", ", GameInfo.GetAvailableModuleInfo().Where(x => x.IsMod && !x.IsNeedy).OrderBy(x => x.DisplayName).Select(x => x.DisplayName).ToArray()).Wrap(80)));
            if (Settings.NeedyModules > 0)
            {
                sb.Append(string.Format("Modded Needy Modules Enabled = {0}\n\n", string.Join(", ", GameInfo.GetAvailableModuleInfo().Where(x => x.IsMod && x.IsNeedy).OrderBy(x => x.DisplayName).Select(x => x.DisplayName).ToArray()).Wrap(80)));
            }
        }

		List<int> rewardPointList = new List<int>();
	    List<int> maxTimeAllowedList = new List<int>();
	    
        for(var i=0; i < generatorList.Count; i++)
		{
			int vanillaSolvableSize = 0;
			var generator = generatorList[i];
			sb.Append(string.Format("Bomb #{0}:\n", i+1));
			sb.Append(string.Format("\tNumber of Widgets Chosen from range = {0}\n", generator.OptionalWidgetCount));
			sb.Append(string.Format("\tTotal Component pools Generated = {0}\n", i == 0 ? poolCount : generator.ComponentPools.Count));
			foreach (var pool in generator.ComponentPools)
	        {
	            if (pool.ModTypes.Contains("Multiple Bombs") || pool.ModTypes.Contains("Factory Mode") || pool.ComponentTypes == null) continue;
	            sb.Append(string.Format("\tNumber of Components to Select = {0}\n", pool.Count));
	            sb.Append(string.Format("\t\tVanilla Components = {0}, Modded Components = {1}\n", pool.ComponentTypes.Count, pool.ModTypes.Count));
				if(pool.ComponentTypes.Count > 0) sb.Append(string.Format("\t\tVanilla Compoents in Pool = {0}\n", string.Join(", ", pool.ComponentTypes.SelectMany(x => GameInfo.GetAvailableModuleInfo().Where(y => y.ModuleType == x).Select(y => y.DisplayName)).ToArray()).Wrap(80)));
	            if(pool.ModTypes.Count > 0) sb.Append(string.Format("\t\tModded Components in Pool = {0}\n", string.Join(", ", pool.ModTypes.SelectMany(x => GameInfo.GetAvailableModuleInfo().Where(y => y.ModuleId == x).Select(y => y.DisplayName)).ToArray()).Wrap(80)));
		        sb.Append("\n");

		        if (pool.ComponentTypes.Count > 0 && pool.ModTypes.Count == 0)
			        vanillaSolvableSize += pool.Count;
			}
			rewardPointList.Add(Convert.ToInt32((5 * Settings.Modules) - (3 * vanillaSolvableSize)));
			maxTimeAllowedList.Add((120 * Settings.Modules) - (60 * vanillaSolvableSize));
		}

	    bool staticMode = (!FactoryRoom.Installed() || Settings.FactoryMode == 0);
		int rewardPoints = staticMode ? rewardPointList.Sum() : rewardPointList.Max();
	    int maxTimeAllowed = staticMode ? maxTimeAllowedList.Sum() : maxTimeAllowedList.Max();
	    int maxStrikesAllowed = Mathf.Max(3, Settings.Modules / 12);
	    if (TwitchPlays.TimeMode())
		    maxTimeAllowed = 300;

	    double multiplier = 1;
	    multiplier += Settings.NeedyModules * (staticMode ? Settings.Bombs : 1);

	    multiplier *= 1 / ((double) Settings.Time / maxTimeAllowed);
	    multiplier *= 1 / ((double) Settings.Strikes / maxStrikesAllowed);

		TwitchPlays.SetReward((int)(rewardPoints * multiplier));
	    TwitchPlays.SendMessage(string.Format("Reward for completing bomb: {0}, Base reward = {1}, Multiplier = {2}, TimeMultipler = {3}, maxTimeAllowed = {4}, StrikesMultipler = {5}, maxStrikesAllowed = {6}", 
		    (int)(rewardPoints * multiplier), rewardPoints, multiplier,
			1 / ((double)Settings.Time / maxTimeAllowed), maxTimeAllowed,
			1 / ((double)Settings.Strikes / maxStrikesAllowed), maxStrikesAllowed) );

		var mission = ScriptableObject.CreateInstance<KMMission>();
        mission.DisplayName = "Custom Freeplay";
        mission.GeneratorSetting = generatorSettings;
        mission.PacingEventsEnabled = Settings.PacingEvents;

        SaveSettings();
        DebugLog(sb.ToString());

		return mission;
    }

    private bool StartMission()
    {
        var mission = CreateMission();

        if(mission != null)
            GetComponent<KMGameCommands>().StartMission(mission, "" + -1);
        return false;
    }


	private KMComponentPool AddComponent(Func<KMGameInfo.KMModuleInfo> module, int count)
	{
		var pool = new KMComponentPool
		{
			ComponentTypes = new List<KMComponentPool.ComponentTypeEnum>(),
			ModTypes = new List<string>(),
			Count = 1
		};

		for (var i = 0; i < count; i++)
		{
			var mod = module.Invoke();
			if (mod.IsMod)
				pool.ModTypes.Add(mod.ModuleId);
			else
				pool.ComponentTypes.Add(mod.ModuleType);
		}

		return pool;
	}

    private KMGameInfo.KMModuleInfo PopModule(ICollection<KMGameInfo.KMModuleInfo> modules, ref List<KMGameInfo.KMModuleInfo> output, string moduleType)
    {
        if(modules == null || output == null)
            throw new NullReferenceException();

        if (output.Count == 0)
        {
            output.AddRange(modules);
            output = output.OrderBy(x => Random.NextDouble()).ToList();
            if (output.Count == 0)
                throw new NullModuleException(string.Format("No Modules of type {0} to return", moduleType));
        }
        
        var module = output[0];
        output.RemoveAt(0);
        return module;
    }

	private readonly List<KMGameInfo.KMModuleInfo>[] _vanillaSolvableModules = {new List<KMGameInfo.KMModuleInfo>(), new List<KMGameInfo.KMModuleInfo>()};
	private readonly List<KMGameInfo.KMModuleInfo>[] _vanillaNeedyModules = { new List<KMGameInfo.KMModuleInfo>(), new List<KMGameInfo.KMModuleInfo>() };
	private readonly List<KMGameInfo.KMModuleInfo>[] _moddedSolvableModules = { new List<KMGameInfo.KMModuleInfo>(), new List<KMGameInfo.KMModuleInfo>() };
	private readonly List<KMGameInfo.KMModuleInfo>[] _moddedNeedyModules = { new List<KMGameInfo.KMModuleInfo>(), new List<KMGameInfo.KMModuleInfo>() };

	private KMGameInfo.KMModuleInfo PopVanillaSolvableModule() { return PopModule(_vanillaSolvableModules[0], ref _vanillaSolvableModules[1], "Vanilla Solvable"); }
	private KMGameInfo.KMModuleInfo PopVanillaNeedyModule() { return PopModule(_vanillaNeedyModules[0], ref _vanillaNeedyModules[1], "Vanilla Needy"); }
	private KMGameInfo.KMModuleInfo PopModdedSolvableModule() { return PopModule(_moddedSolvableModules[0], ref _moddedSolvableModules[1], "Modded Solvable"); }
	private KMGameInfo.KMModuleInfo PopModdedNeedyModule() { return PopModule(_moddedNeedyModules[0], ref _moddedNeedyModules[1], "Modded Needy"); }

	private List<KMComponentPool> BuildNoDuplicatesPool(bool clear)
    {
        var pools = new List<KMComponentPool>();

        var vanillaModulesChance = Settings.VanillaModules / 100.0f;
        var moddedModulesChance = 1.0f - vanillaModulesChance;

        var vanillaNeedySize = Mathf.FloorToInt(Settings.NeedyModules * vanillaModulesChance);
        var moddedNeedySize = Mathf.FloorToInt(Settings.NeedyModules * moddedModulesChance);
        var mixedNeedySize = Settings.NeedyModules - vanillaNeedySize - moddedNeedySize;

	    if (mixedNeedySize > 0)
	    {
		    DebugLog("Initial: VanillaNeedySize = {0}, ModdedNeedySize = {1}, mixedNeedySize = {2}", vanillaNeedySize, moddedNeedySize, mixedNeedySize);

		    for (var i = 0; i < mixedNeedySize; i++)
		    {
			    if (Random.NextDouble() < vanillaModulesChance)
				    vanillaNeedySize++;
			    else
				    moddedNeedySize++;
		    }
	    }
	    DebugLog("Final: VanillaNeedySize = {0}, ModdedNeedySize = {1}", vanillaNeedySize, moddedNeedySize);

        var vanillaSolvableSize = Mathf.FloorToInt((Settings.Modules - Settings.NeedyModules) * vanillaModulesChance);
        var moddedSolvableSize = Mathf.FloorToInt((Settings.Modules - Settings.NeedyModules) * moddedModulesChance);
        var mixedSolvableSize = (Settings.Modules - Settings.NeedyModules) - vanillaSolvableSize - moddedSolvableSize;

	    if (mixedSolvableSize > 0)
	    {
		    DebugLog("Initial: vanillaSolvableSize = {0}, moddedSolvableSize = {1}, mixedSolvableSize = {2}", vanillaSolvableSize, moddedSolvableSize, mixedSolvableSize);

		    for (var i = 0; i < mixedSolvableSize; i++)
		    {
			    if (Random.NextDouble() < vanillaModulesChance)
				    vanillaSolvableSize++;
			    else
				    moddedSolvableSize++;
		    }
	    }
	    DebugLog("Final: vanillaSolvableSize = {0}, moddedSolvableSize = {1}", vanillaSolvableSize, moddedSolvableSize);

	    int bombs = ((!FactoryRoom.Installed() || Settings.FactoryMode == 0) ? Settings.Bombs : 1);
		
	    if (clear)
	    {
			_vanillaSolvableModules[0].Clear();
		    _vanillaSolvableModules[1].Clear();
		    _vanillaSolvableModules[0].AddRange(GameInfo.GetAvailableModuleInfo().Where(x => !x.IsMod && !x.IsNeedy && !Settings.DisabledModuleIds.Contains(x.ModuleId)));

		    _vanillaNeedyModules[0].Clear();
		    _vanillaNeedyModules[1].Clear();
		    _vanillaNeedyModules[0].AddRange(GameInfo.GetAvailableModuleInfo().Where(x => !x.IsMod && !x.IsNeedy && !Settings.DisabledModuleIds.Contains(x.ModuleId)));

		    _moddedSolvableModules[0].Clear();
		    _moddedSolvableModules[1].Clear();
			_moddedSolvableModules[0].AddRange(GameInfo.GetAvailableModuleInfo().Where(x => x.IsMod && !x.IsNeedy));

		    _moddedNeedyModules[0].Clear();
		    _moddedNeedyModules[1].Clear();
		    _moddedNeedyModules[0].AddRange(GameInfo.GetAvailableModuleInfo().Where(x => x.IsMod && x.IsNeedy));
		}

        var maxVanillaSolvablePerPool = Mathf.Max(_vanillaSolvableModules[0].Count / (Math.Max(vanillaSolvableSize,1) * bombs), 1);
        var maxVanillaNeedyPerPool = Mathf.Max(_vanillaNeedyModules[0].Count / (Math.Max(vanillaNeedySize, 1) * bombs), 1);
        var maxModSolvablePerPool = Mathf.Max(_moddedSolvableModules[0].Count / (Math.Max(moddedSolvableSize, 1) * bombs), 1);
        var maxModNeedyPerPool = Mathf.Max(_moddedNeedyModules[0].Count / (Math.Max(moddedNeedySize, 1) * bombs), 1);

        try
        {
            for(var i = 0; i < vanillaSolvableSize; i++)
            {
	            var pool = AddComponent(PopVanillaSolvableModule, maxVanillaSolvablePerPool);
	            var pool2 = pools.FirstOrDefault(x => x.ComponentTypes.All(y => pool.ComponentTypes.Contains(y)));
	            if (pool2 != null)
		            pool2.Count++;
	            else
					pools.Add(pool);
			}

            for (var i = 0; i < moddedSolvableSize; i++)
            {
	            var pool = AddComponent(PopModdedSolvableModule, maxModSolvablePerPool);
				var pool2 = pools.FirstOrDefault(x => x.ModTypes.All(y => pool.ModTypes.Contains(y)));
	            if (pool2 != null)
		            pool2.Count++;
	            else
		            pools.Add(pool);
			}

            for (var i = 0; i < vanillaNeedySize; i++)
            {
	            var pool = AddComponent(PopVanillaNeedyModule, maxVanillaNeedyPerPool);
				var pool2 = pools.FirstOrDefault(x => x.ComponentTypes.All(y => pool.ComponentTypes.Contains(y)));
	            if (pool2 != null)
		            pool2.Count++;
	            else
		            pools.Add(pool);
			}

            for (var i = 0; i < moddedNeedySize; i++)
            {
	            var pool = AddComponent(PopModdedNeedyModule, maxModNeedyPerPool);
	            var pool2 = pools.FirstOrDefault(x => x.ModTypes.All(y => pool.ModTypes.Contains(y)));
	            if (pool2 != null)
		            pool2.Count++;
	            else
		            pools.Add(pool);
			}
        }
        catch (NullModuleException ex)
        {
            DebugLog("Failure in No Duplicates for the following reason");
            DebugLog(ex.Message);
            pools.Clear();
            return pools;
        }
        catch (Exception ex)
        {
            DebugLog("Failure in No Duplicates due to an exception.");
            DebugLog("Exception: {0}, Stack Trace: {1}", ex.Message, ex.StackTrace);
            pools.Clear();
            return pools;
        }

        return pools;
    }

    private List<KMComponentPool> BuildComponentPools(bool clear)
    {
        var pools = new List<KMComponentPool>();

        var vanillaModules = Settings.VanillaModules / 100.0f;
        var moddedModules = 1.0f - vanillaModules;

        var vanillaNeedySize = Mathf.FloorToInt(Settings.NeedyModules * vanillaModules);
        var moddedNeedySize = Mathf.FloorToInt(Settings.NeedyModules * moddedModules);
        var mixedNeedySize = Settings.NeedyModules - vanillaNeedySize - moddedNeedySize;

        for (var i = 0; i < mixedNeedySize; i++)
        {
            if (Random.NextDouble() < vanillaModules)
                vanillaNeedySize++;
            else
                moddedNeedySize++;
        }


        var vanillaSolvableSize = Mathf.FloorToInt((Settings.Modules - Settings.NeedyModules) * vanillaModules);
        var moddedSolvableSize = Mathf.FloorToInt((Settings.Modules - Settings.NeedyModules) * moddedModules);
        var mixedSolvableSize = (Settings.Modules - Settings.NeedyModules) - vanillaSolvableSize - moddedSolvableSize;
		
	    for (var i = 0; i < mixedSolvableSize; i++)
        {
            if (Random.NextDouble() < vanillaModules)
                vanillaSolvableSize++;
            else
                moddedSolvableSize++;
        }

		var moddedSolvableModules = GameInfo.GetAvailableModuleInfo().Where(x => x.IsMod && !x.IsNeedy).ToList();
        var moddedNeedyModules = GameInfo.GetAvailableModuleInfo().Where(x => x.IsMod && x.IsNeedy).ToList();

        var vanillaSolvableModules = GameInfo.GetAvailableModuleInfo().Where(x => !x.IsMod && !x.IsNeedy && !Settings.DisabledModuleIds.Contains(x.ModuleId)).ToList();
        var vanillaNeedyModules = GameInfo.GetAvailableModuleInfo().Where(x => !x.IsMod && x.IsNeedy && !Settings.DisabledModuleIds.Contains(x.ModuleId)).ToList();

        if ((vanillaNeedyModules.Count == 0 && vanillaNeedySize > 0) ||
            (vanillaSolvableModules.Count == 0 && vanillaSolvableSize > 0) ||
            (moddedNeedyModules.Count == 0 && moddedNeedySize > 0) ||
            (moddedSolvableModules.Count == 0 && moddedSolvableSize > 0))
        {
            DebugLog("Duplicates Allowed Failure");

            if (vanillaNeedyModules.Count == 0 && vanillaNeedySize > 0) DebugLog("VanillaNeedyModules.Count = {0}, VanillaNeedySize = {1}", vanillaNeedyModules.Count, vanillaNeedySize);
            if (vanillaSolvableModules.Count == 0 && vanillaSolvableSize > 0) DebugLog("vanillaSolvableModules.Count = {0}, vanillaSolvableSize = {1}", vanillaSolvableModules.Count, vanillaSolvableSize);
            if (moddedNeedyModules.Count == 0 && moddedNeedySize > 0) DebugLog("moddedNeedyModules.Count = {0}, moddedNeedySize = {1}", moddedNeedyModules.Count, moddedNeedySize);
            if (moddedSolvableModules.Count == 0 && moddedSolvableSize > 0) DebugLog("moddedSolvableModules.Count = {0}, moddedSolvableSize = {1}", moddedSolvableModules.Count, moddedSolvableSize);

            return pools;
        }

        if (vanillaNeedySize > 0)
        {
            var pool = new KMComponentPool
            {
                ComponentTypes = new List<KMComponentPool.ComponentTypeEnum>(),
                ModTypes = new List<string>()
            };
            pool.ComponentTypes.AddRange(vanillaNeedyModules.Select(x => x.ModuleType));
            pool.Count = vanillaNeedySize;
            pools.Add(pool);
        }

        if (moddedNeedySize > 0)
        {
            var pool = new KMComponentPool
            {
                ComponentTypes = new List<KMComponentPool.ComponentTypeEnum>(),
                ModTypes = new List<string>()
            };
            pool.ModTypes.AddRange(moddedNeedyModules.Select(x => x.ModuleId));
            pool.Count = moddedNeedySize;
            pools.Add(pool);
        }

        if (vanillaSolvableSize > 0)
        {
            var pool = new KMComponentPool
            {
                ComponentTypes = new List<KMComponentPool.ComponentTypeEnum>(),
                ModTypes = new List<string>()
            };
            pool.ComponentTypes.AddRange(vanillaSolvableModules.Select(x => x.ModuleType));
            pool.Count = vanillaSolvableSize;
            pools.Add(pool);
        }

        if (moddedSolvableSize > 0)
        {
            var pool = new KMComponentPool
            {
                ComponentTypes = new List<KMComponentPool.ComponentTypeEnum>(),
                ModTypes = new List<string>()
            };
            pool.ModTypes.AddRange(moddedSolvableModules.Select(x => x.ModuleId));
            pool.Count = moddedSolvableSize;
            pools.Add(pool);
        }

        return pools;
    }

	private enum ActionAllowed
	{
		Unfinished,
		Allowed,
		Denied
	}

	private IEnumerator AllowPowerUsers(Permissions permission, PowerLevel power, string errorIfNotAllowed)
	{
		ActionAllowed allowed = ActionAllowed.Unfinished;
		yield return null;
		yield return new object[]
		{
			permission.ToString(),
			new Action(() => allowed = ActionAllowed.Allowed),
			new Action(() => allowed = ActionAllowed.Denied)
		};
        yield return null;
	    DebugLog("Permission = {0}, Result = {1}", permission, allowed);

	    if (allowed == ActionAllowed.Allowed) yield break;
	    allowed = ActionAllowed.Unfinished;
	    yield return new object[]
	    {
	        EnumUtils.StringValueOf(power),
	        new Action(() => allowed = ActionAllowed.Allowed),
	        new Action(() => allowed = ActionAllowed.Denied)
	    };
	    yield return null;
	    DebugLog("Power override {0}, Result = {1}", power, allowed);

		if (allowed == ActionAllowed.Allowed) yield break;
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, transform);
		yield return "sendtochaterror " + errorIfNotAllowed;
	}

    private enum Permissions
    {
        BombCreatorEnabled,
        BombCreatorAllowedMoreThanFiveNeedyModules,
        BombCreatorAllowedToChangeVanillaSeed,
		BombCreatorAllowedToExceed199CombinedModules,
		BombCreatorAllowedToChangeTimeModeTimeLimit,
    }

    private static Dictionary<string, bool> _permissions = new Dictionary<string, bool>
    {
        { Permissions.BombCreatorEnabled.ToString(), true },
        { Permissions.BombCreatorAllowedMoreThanFiveNeedyModules.ToString(), false },
        { Permissions.BombCreatorAllowedToChangeVanillaSeed.ToString(), false },
	    { Permissions.BombCreatorAllowedToExceed199CombinedModules.ToString(), false },
	    { Permissions.BombCreatorAllowedToChangeTimeModeTimeLimit.ToString(), false },
    };

    private IEnumerator AllowBombCreator(bool front=true)
    {
        yield return _permissions;
        yield return AllowPowerUsers(Permissions.BombCreatorEnabled, PowerLevel.Mod, "Only mods or higher are allowed to use Bomb Creator");
        yield return front ? "show front" : "show back";
    }

    private string TwitchHelpMessage = "Set time with !{0} time 45:00. Set the Moulde cound with !{0} modules 23. Set strikes with !{0} strikes 3. Start the bomb with !{0} start. Go to https://github.com/CaitSith2/KTANE-mods/wiki/BombCreator for more details.";
    private bool TwitchShouldCancelCommand;

    private IEnumerator ProcessTwitchCommand(string command)
    {
	    bool infiniteMode = (FactoryRoom.Installed() && FactoryRoom.GetFactoryModes()[Settings.FactoryMode].Contains("∞"));
	    int MaximumNonModeratorBombs = 199; //10 x 10 x 2 - 1

		TwitchShouldCancelCommand = false;
        command = command.ToLowerInvariant();
        DebugLog("Received command !bombcreator {0}", command);
        string[] split = command.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
        if (command.Equals("artwork"))
        {
            yield return null;
            UpdateArtwork();
            yield return "show back";
        }
        else if (command.Equals("duplicates") || command.Equals("no duplicates"))
        {
            if (Settings.DuplicatesAllowed == command.Equals("duplicates")) yield break;
            yield return AllowBombCreator();
            yield return new KMSelectable[] {DuplicateButton};
            yield break;
        }
        else if (command.StartsWith("front face") || command.Equals("all faces"))
        {
            if (FrontFaceText.text.ToLowerInvariant().Contains(command)) yield break;
            yield return AllowBombCreator();
            yield return FrontFaceButton;
            yield return new WaitForSeconds(0.1f);
            yield return FrontFaceButton;
            yield break;
        }
        else if (command.StartsWith("veto ") && split.Length > 1)
        {
            string veto = string.Join(" ", split.Skip(1).ToArray());
            string initial = ModuleDisableText.text;
            string localveto = veto;
            int vetoCount = _vanillaModules.Count(x => x.DisplayName.ToLowerInvariant().Contains(localveto));
            if (vetoCount == 0) yield break;
            if (vetoCount > 1)
            {
                if (_vanillaModules.Count(x => x.DisplayName.ToLowerInvariant().Equals(localveto)) == 0) yield break;
                veto = _vanillaModules.First(x => x.DisplayName.ToLowerInvariant().Equals(veto)).DisplayName;
            }
            else
            {
                veto = _vanillaModules.First(x => x.DisplayName.ToLowerInvariant().Contains(veto)).DisplayName;
            }

            yield return AllowBombCreator();
            while (!ModuleDisableText.text.ToLowerInvariant().Equals(veto.ToLowerInvariant()))
            {

                ModuleDisableMinusButton.OnInteract();
                ModulesMinusButton.OnInteractEnded();
                yield return new WaitForSeconds(0.1f);
                if (ModuleDisableText.text.Equals(initial)) yield break;
            }
            ModuleDisableButton.OnInteract();
            ModuleDisableButton.OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
            

        }
        else if (command.Replace("infinite", InfiniteSign).StartsWith(InfiniteSign) || command.StartsWith("finite") || command.StartsWith("static"))
        {
            if (!FactoryRoom.Installed()) yield break;
            int factoryMode = 0;
            if (!command.StartsWith("static"))
            {
                if (command.StartsWith(InfiniteSign))
                    factoryMode = 5;
                else if (command.StartsWith("finite"))
                    factoryMode = 1;

                if (command.Contains("time"))
                    factoryMode += 1;
                if (command.Contains("strikes"))
                    factoryMode += 2;
            }
            yield return AllowBombCreator();
            while (Settings.FactoryMode < factoryMode)
            {
                FactoryModePlusButton.OnInteract();
                FactoryModePlusButton.OnInteractEnded();
                yield return new WaitForSeconds(0.1f);
            }
            while (Settings.FactoryMode > factoryMode)
            {
                FactoryModeMinusButton.OnInteract();
                FactoryModeMinusButton.OnInteractEnded();
                yield return new WaitForSeconds(0.1f);
            }
            yield break;
        }

        else if (split.Length == 1)
        {
            switch (split[0])
            {
                case "start":
                    yield return AllowBombCreator();

	                if ((Settings.Modules * (infiniteMode ? 1 : Settings.Bombs)) > MaximumNonModeratorBombs)
	                {
		                int maxBombs = MaximumNonModeratorBombs / Settings.Modules;
		                int maxModules = MaximumNonModeratorBombs / (infiniteMode ? 1 : Settings.Bombs);

		                yield return AllowPowerUsers(Permissions.BombCreatorAllowedToExceed199CombinedModules, PowerLevel.Mod,
			                string.Format(
				                "Only a moderator or higher may start the bomb in this configuration.\nMaximum bombs for {0} modules is {1} bomb{2}.\nMaximum modules for {3} bomb{4} is {5} modules.",
				                Settings.Modules, maxBombs, maxBombs == 1 ? "" : "s", 
				                Settings.Bombs, Settings.Bombs == 1 ? "" : "s", maxModules));
	                }

                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
                    var mission = CreateMission();
                    if (mission != null)
                        yield return mission;
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
                    yield return new WaitForSeconds(0.1f);
                    yield break;
                case "reset":
                    yield return AllowBombCreator();
                    yield return "elevator music";
                    yield return ResetSettings(() => TwitchShouldCancelCommand);
                    if (TwitchShouldCancelCommand && _resetting)
                    {
                        _resetting = false;
                        yield return "cancelled";
                    }
                    yield break;
                case "save":
                    yield return AllowBombCreator();
                    SaveButton.OnInteract();
                    SaveButton.OnInteractEnded();
                    yield return new WaitForSeconds(0.1f);
                    yield break;
            }
        }
        else if (split.Length == 2)
        {
            switch (split[0])
            {
                case "time":
                    string[] timeSplit = split[1].Split(new[] {":"}, StringSplitOptions.None);
                    foreach (string s in timeSplit)
                    {
                        int result;
                        if (!int.TryParse(s, out result))
                        {
                            yield return "parseerror";
                        }
                    }
                    int seconds;
                    switch (timeSplit.Length)
                    {
                        case 4:
                            seconds = (int.Parse(timeSplit[0]) * 86400) + (int.Parse(timeSplit[1]) * 3600) + (int.Parse(timeSplit[2]) * 60) + int.Parse(timeSplit[3]);
                            break;
                        case 3:
                            seconds = (int.Parse(timeSplit[0]) * 3600) + (int.Parse(timeSplit[1]) * 60) + int.Parse(timeSplit[2]);
                            break;
                        case 2:
                            seconds = (int.Parse(timeSplit[0]) * 60) + int.Parse(timeSplit[1]);
                            break;
                        case 1:
                            seconds = int.Parse(timeSplit[0]);
                            break;
                        default:
                            yield return null;
                            yield return "sendtochaterror Time command not in correct format.";
                            yield break;
                    }
                    seconds += TwitchPlays.TimeMode() ? 59 : 29;
                    seconds /= TwitchPlays.TimeMode() ? 60 : 30;
                    seconds *= TwitchPlays.TimeMode() ? 60 : 30;
                    yield return AllowBombCreator();

	                if (TwitchPlays.TimeMode())
		                yield return AllowPowerUsers(Permissions.BombCreatorAllowedToChangeTimeModeTimeLimit, PowerLevel.Admin, "Only Twitch plays Admin or higher are allowed to change the time limit in time mode.");

                    if (Mathf.Abs(BombTime - seconds) > 4*3600) yield return "elevator music";
                    while (BombTime != seconds && !TwitchShouldCancelCommand)
                    {
                        yield return AddTimer(TwitchPlays.TimeMode() ? 60 : 30, () => BombTime >= seconds || TwitchShouldCancelCommand);
                        yield return AddTimer(TwitchPlays.TimeMode() ? -60 : -30, () => BombTime <= seconds || TwitchShouldCancelCommand);
                        if (TwitchShouldCancelCommand) yield return "cancelled";
                    }
                    yield break;
                case "modules":
                    int moduleCount;
                    if (!int.TryParse(split[1], out moduleCount)) yield break;
                    if (moduleCount < 1 || moduleCount > GetMaxModules()) yield break;
                    yield return AllowBombCreator();
	                if ((moduleCount * (infiniteMode ? 1 : Settings.Bombs) > MaximumNonModeratorBombs))
	                {
		                int maxBombs = MaximumNonModeratorBombs / moduleCount;
		                int maxModules = MaximumNonModeratorBombs / (infiniteMode ? 1 : Settings.Bombs);

						yield return AllowPowerUsers(Permissions.BombCreatorAllowedToExceed199CombinedModules, PowerLevel.Mod,
				                string.Format("Only moderators or higher can exceed the max combined module count of {6}.\nMaximum bombs for {0} modules is {1} bomb{2}.\nMaximum modules for {3} bomb{4} is {5} modules.",
								moduleCount, maxBombs, maxBombs == 1 ? "" : "s",
								Settings.Bombs, Settings.Bombs == 1 ? "" : "s", maxModules, MaximumNonModeratorBombs));
					}

                    if (Mathf.Abs(Settings.Modules - moduleCount) > 200) yield return "elevator music";
                    while (Settings.Modules != moduleCount && !TwitchShouldCancelCommand)
                    {
                        yield return AddModules(1, () => Settings.Modules >= moduleCount || TwitchShouldCancelCommand);
                        yield return AddModules(-1, () => Settings.Modules <= moduleCount || TwitchShouldCancelCommand);
                        if (TwitchShouldCancelCommand) yield return "cancelled";
                    }
                    yield break;
                case "strikes":
                    int strikeCount;
                    if (!int.TryParse(split[1], out strikeCount)) yield break;
                    if (strikeCount < 1) yield break;
                    yield return AllowBombCreator();
                    if (Mathf.Abs(Settings.Strikes - strikeCount) > 100) yield return "elevator music";
                    while (Settings.Strikes != strikeCount && !TwitchShouldCancelCommand)
                    {
                        yield return AddStrikes(1, () => Settings.Strikes >= strikeCount || TwitchShouldCancelCommand);
                        yield return AddStrikes(-1, () => Settings.Strikes <= strikeCount || TwitchShouldCancelCommand);
                        if (TwitchShouldCancelCommand) yield return "cancelled";
                    }
                    yield break;
                case "needy":
                    int needyCount;
                    if (!int.TryParse(split[1], out needyCount)) yield break;
                    if (needyCount < 0 || needyCount >= GetMaxModules()) yield break;
                    yield return AllowBombCreator();
                    if (needyCount > 5)
	                {
		                yield return AllowPowerUsers(Permissions.BombCreatorAllowedMoreThanFiveNeedyModules, PowerLevel.Mod, "Only moderators or higher can set the needy count above 5");
	                }
                    if (Mathf.Abs(Settings.NeedyModules - needyCount) > 200) yield return "elevator music";
                    while (Settings.NeedyModules != needyCount && !TwitchShouldCancelCommand)
                    {
                        yield return AddNeedyModules(1, () => Settings.NeedyModules >= needyCount || TwitchShouldCancelCommand);
                        yield return AddNeedyModules(-1, () => Settings.NeedyModules <= needyCount || TwitchShouldCancelCommand);
                        if (TwitchShouldCancelCommand) yield return "cancelled";
                    }
                    yield break;
                case "mods":
                    int modCount;
                    if (!int.TryParse(split[1], out modCount)) yield break;
                    if (modCount < 0 || modCount > 100) yield break;
                    split[1] = (100 - modCount).ToString();
                    goto case "vanilla";
                case "vanilla":
                    int vanillaCount;
                    if (!int.TryParse(split[1], out vanillaCount)) yield break;
                    if (vanillaCount < 0 || vanillaCount > 100) yield break;
                    yield return AllowBombCreator();
                    while (Settings.VanillaModules != vanillaCount && !TwitchShouldCancelCommand)
                    {
                        yield return AddVanillaModules(1, () => Settings.VanillaModules >= vanillaCount || TwitchShouldCancelCommand);
                        yield return AddVanillaModules(-1, () => Settings.VanillaModules <= vanillaCount || TwitchShouldCancelCommand);
                        if (TwitchShouldCancelCommand) yield return "cancelled";
                    }
                    yield break;
                case "pacing":
                    if (split[1].Equals("on") || split[1].Equals("off"))
                    {

                        yield return null;
                        yield return null;
                        if (!PacingEventsText.text.EndsWith(split[1], StringComparison.InvariantCultureIgnoreCase))
                        {
                            PacingEventsButton.OnInteract();
                            PacingEventsButton.OnInteractEnded();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                    yield break;
                case "bombs":
                    int bombsCount;
                    if (!MultipleBombs.Installed()) yield break;
                    if (FactoryRoom.Installed() && FactoryModeText.text.Contains(InfiniteSign)) yield break;
                    if (!int.TryParse(split[1], out bombsCount)) yield break;
                    if (bombsCount < 1) yield break;
	                if (bombsCount > MultipleBombs.GetMaximumBombCount())
		                yield return string.Format("sendtochaterror The maximum bombs allowed by the current gameplay room is {0}", MultipleBombs.GetMaximumBombCount());
                    yield return AllowBombCreator();

	                if ((Settings.Modules * (infiniteMode ? 1 : bombsCount) > MaximumNonModeratorBombs))
	                {
		                int maxBombs = MaximumNonModeratorBombs / Settings.Modules;
		                int maxModules = MaximumNonModeratorBombs / (infiniteMode ? 1 : bombsCount);

		                yield return AllowPowerUsers(Permissions.BombCreatorAllowedToExceed199CombinedModules, PowerLevel.Mod,
			                string.Format(
				                "Only moderators or higher can exceed the max combined module count of {6}.\nMaximum bombs for {0} modules is {1} bomb{2}.\nMaximum modules for {3} bomb{4} is {5} modules.",
				                Settings.Modules, maxBombs, maxBombs == 1 ? "" : "s", 
				                bombsCount, bombsCount == 1 ? "" : "s", maxModules, MaximumNonModeratorBombs));
	                }
					
					while (Settings.Bombs != bombsCount && !TwitchShouldCancelCommand)
                    {
                        yield return AddBombs(1, () => Settings.Bombs >= bombsCount || TwitchShouldCancelCommand);
                        yield return AddBombs(-1, () => Settings.Bombs <= bombsCount || TwitchShouldCancelCommand);
                        if (TwitchShouldCancelCommand) yield return "cancelled";
                    }
                    yield break;
                case "seed":
                    int vanillaSeed;
                    if (!VanillaRuleModifier.Installed()) yield break;
                    if (!int.TryParse(split[1], out vanillaSeed) || vanillaSeed < 0) yield break;
                    yield return AllowBombCreator();
                    yield return AllowPowerUsers(Permissions.BombCreatorAllowedToChangeVanillaSeed, PowerLevel.Admin, "Only those with admin access or higher may set the vanilla seed.");

                    if (Mathf.Abs(VanillaRuleModifier.GetRuleSeed() - vanillaSeed) > 300) yield return "elevator music";
                    while (VanillaRuleModifier.GetRuleSeed() != vanillaSeed && !TwitchShouldCancelCommand)
                    {
                        yield return AddSeed(1, () => VanillaRuleModifier.GetRuleSeed() >= vanillaSeed || TwitchShouldCancelCommand);
                        yield return AddSeed(-1, () => VanillaRuleModifier.GetRuleSeed() <= vanillaSeed || TwitchShouldCancelCommand);
                        if (TwitchShouldCancelCommand) yield return "cancelled";
                    }
                    yield break;
                case "minwidgets":
                case "minimumwidgets":
                case "min-widgets":
                case "minimum-widgets":
                    int minWidgets;
                    if (!int.TryParse(split[1], out minWidgets)) yield break;
                    if (minWidgets < 0 || minWidgets > Settings.WidgetsMaximum) yield break;
                    yield return AllowBombCreator();
                    while (Settings.WidgetsMinimum != minWidgets && !TwitchShouldCancelCommand)
                    {
                        yield return AddWidgetsMinimum(1, () => Settings.WidgetsMinimum >= minWidgets || TwitchShouldCancelCommand);
                        yield return AddWidgetsMinimum(-1, () => Settings.WidgetsMinimum <= minWidgets || TwitchShouldCancelCommand);
                        if (TwitchShouldCancelCommand) yield return "cancelled";
                    }
                    yield break;
                case "maxwidgets":
                case "maximumwidgets":
                case "max-widgets":
                case "maximum-widgets":
                    int maxWidgets;
                    if (!int.TryParse(split[1], out maxWidgets)) yield break;
                    if (maxWidgets < Settings.WidgetsMinimum || maxWidgets > 50) yield break;
                    yield return AllowBombCreator();
                    while (Settings.WidgetsMaximum != maxWidgets && !TwitchShouldCancelCommand)
                    {
                        yield return AddWidgetsMaximum(1, () => Settings.WidgetsMaximum >= maxWidgets || TwitchShouldCancelCommand);
                        yield return AddWidgetsMaximum(-1, () => Settings.WidgetsMaximum <= maxWidgets || TwitchShouldCancelCommand);
                        if (TwitchShouldCancelCommand) yield return "cancelled";
                    }
                    yield break;
            }
        }
    }
}

public class NullModuleException : Exception
{
    public NullModuleException()
    {
    }

    public NullModuleException(string message) : base(message)
    {
    }

    public NullModuleException(string message, Exception inner) : base(message, inner)
    {
    }
}
