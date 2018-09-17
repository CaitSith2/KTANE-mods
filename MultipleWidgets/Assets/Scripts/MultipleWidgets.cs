using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Random = UnityEngine.Random;

public class MultipleWidgets : MonoBehaviour
{
    #region public variables
    public GameObject[] Batteries;
    public GameObject[] BatteryHolders;
    public Transform[] AACells;
    public Transform NineVoltCell;

    public GameObject TwoFactor;
    public TextMesh TwoFactorDigits;
    public TextMesh TwoFactorTimeRemaining;
    public MeshRenderer TwoFactorDisplay;
    public GameObject Ports;

    public GameObject ParallelPort;
    public GameObject SerialPort;

    public GameObject PS2Port;
    public GameObject DVIPort;
    public GameObject StereoRCAPort;
    public GameObject RJ45Port;
    public GameObject ActivityLED;
    

    public GameObject HDMIPort;
    public GameObject USBPort;
    public GameObject ComponentVideoPort;
    public GameObject CompositeVideoPort;

    public GameObject ACPort;
    public GameObject PCMCIAPort;
    public GameObject VGAPort;

    public GameObject ACPortFiller;
    public GameObject RJ45PortFiller;
    public GameObject USBPortFiller;
    public GameObject CompositeVideoPortFiller;
    public GameObject ComponentVideoPortFiller;
    public GameObject RCAPortFiller;
    public GameObject HDMIPortFiller;
    public GameObject PS2PortFiller;

    public GameObject MultipleWidgetTopHalf;

    public GameObject Indicator;
    public TextMesh IndicatorText;
    public GameObject[] IndicatorLights;

    public Transform IndicatorTransform;
    public Transform BatteryTransform;
    public Transform TwoFactorTransform;

    public KMBombInfo Info;
    public AudioClip Notify;
	public KMRuleSeedable RuleSeed;
    #endregion

    #region private variables
    private readonly int _currentSettingsVersion = 4;
    private class ModSettings
    {
        public int SettingsVersion = 0;
        public string HowToUse0 = "Don't Touch this value. It is used by the mod internally to determine if there are new settings to be saved.";

        public bool EnableExtendedPorts = true;
        public string HowToUse1_1 = "If Enabled, The following port types will be enabled:";
        public string HowToUse1_2 = "HDMI, USB, PCMCIA, VGA, Component Video, Composite Video, 120/240V AC";

        public bool EnableExtendedBatteries = true;
        public string HowToUse2 = "If Enabled, The following battery counts in 1 holder can spawn: 0, 3, 4";

        public bool EnableIndicatorColors = true;
        public string HowToUse3 = "If Enabled, You will see Indicator light colors other than white. These are still treated as On indicators.";

        public bool EnableEncryptedIndicators = false;
        public int MaxEncryptedIndicators = 2;
        public float EncryptionProbability = 0.5f;
        public string HowToUse4_1 = "If Enabled, Some of the Indicators up to a max count per bomb will be encrypted. Refer to";
        public string HowToUse4_2 = "http://steamcommunity.com/sharedfiles/filedetails/?id=1060194010 to see how Encrypted Indicators work.";
        public string HowToUse4_3 = "This also stacks with colors if enabled as well.";
        public string HowToUse4_4 = "EncryptionProbability is a number between 0.0f to 1.0f, and determines how likely the indicators will be encrypted.";

        public bool EnableTwoFactor = true;
        public int TwoFactorDuration = 60;
        public string HowToUse7 = "If Enabled, Two factor has a possibilty to spawn.";
        public string HowToUse7_2 ="TwoFactorDuration is a number between 30 and 999 seconds (30 seconds to 16 minutes 39 seconds.)";

        public bool DebugModeForceAllPortsInCurrentSet = false;
        public string HowToUse5_1 = "This forces the port plates to have every type possible in the given set.";

        public bool DebugModeForceAllPossiblePorts = false;
        public string HowToUse5_2 = "This Forces every port from the first port plate sets to spwan. (This overrides DebugModeForceAllPortsInCurrentSet)";

        public bool DebugModeBatteries = false;
        public string HowToUse6 = "This forces the battery count to be 0, 3 or 4. Never 1 or 2.";
    }

    private static ModSettings _modSettings;

    private List<List<PortType>> _portGroups;
    private Dictionary<PortType, GameObject> _portLookup;
    private Dictionary<PortType, GameObject> _portFiller;
    private string[] _portGroupNames;


    private bool _indicator = false;
    private string _indicatorLabel;
    private bool _indicatorLight;
    private int _indicatorLightColor;
    private int _activityLEDTime;

    private bool _ports = false;
    private PortType _presentPorts = (PortType) 0;
    private List<string> _portList = null;

    private bool _batteries = false;
    private BatteryType _batteryType = BatteryType.NineVolt;
    private BatteryType _holderType = BatteryType.NineVolt;
    private class BatterySetInfo
    {
        public int numbatteries = 0;
        public int capacity = 0;
        public string type = "";
    }

    private bool _twofactor = false;
    private int _key = -1;
    private float _timeElapsed;

    private const float TimerLength = 60.0f;

    private static int _widgetCounter = 1;
    private int _widgetID;
    #endregion

    private void DebugLog(string log, params object[] args)
    {
        var logData = string.Format(log, args);
        Debug.LogFormat("[MultipleWidgets #{0}] {1}",_widgetID,logData);
    }

    private string GetModSettingsPath(bool directory)
    {
        string ModSettingsDirectory = Path.Combine(Application.persistentDataPath, "Modsettings");
        return directory ? ModSettingsDirectory : Path.Combine(ModSettingsDirectory, "MultipleWidgets-settings.txt");
    }

    private void WriteSettings()
    {
        DebugLog("Writing Settings File");
        try
        {
            if (!Directory.Exists(GetModSettingsPath(true)))
            {
                Directory.CreateDirectory(GetModSettingsPath(true));
            }

            _modSettings.SettingsVersion = _currentSettingsVersion;
            string settings = JsonConvert.SerializeObject(_modSettings, Formatting.Indented);
            File.WriteAllText(GetModSettingsPath(false), settings);
            DebugLog("New settings = {0}", settings);
        }
        catch (Exception ex)
        {
            DebugLog("Failed to Create settings file due to Exception:\n{0}\nStack Trace:\n{1}", ex.Message, ex.StackTrace);
        }
    }

    private void LoadSettings()
    {
        string ModSettingsDirectory = Path.Combine(Application.persistentDataPath, "Modsettings");
        string ModSettings = Path.Combine(ModSettingsDirectory, "MultipleWidgets-settings.txt");

        try
        {
            if (File.Exists(ModSettings))
            {
                string settings = File.ReadAllText(ModSettings);
                _modSettings = JsonConvert.DeserializeObject<ModSettings>(settings);

                if (_modSettings.SettingsVersion != _currentSettingsVersion)
                    WriteSettings();
            }
            else
            {
                _modSettings = new ModSettings();
                WriteSettings();
            }
        }
        catch (Exception ex)
        {
            DebugLog("Settings not loaded due to Exception:\n{0}\nStack Trace:\n{1}\nLoading default settings instead.",
                ex.Message, ex.StackTrace);
            _modSettings = new ModSettings();
            WriteSettings();
        }
    }

    void Awake()
    {
        _widgetID = _widgetCounter;
        _widgetCounter++;

        LoadSettings();

        Ports.SetActive(false);
        MultipleWidgetTopHalf.SetActive(true);
        Indicator.SetActive(false);
        IndicatorText.text = string.Empty;
        TwoFactor.SetActive(false);

        foreach (var battery in Batteries)
            battery.SetActive(false);

        foreach (var holder in BatteryHolders)
            holder.SetActive(false);

        foreach (var lightColor in IndicatorLights)
            lightColor.SetActive(false);

        string[] widgetTypes = { "Indicator", "Ports", "Batteries", "TwoFactor" };
        var widgetSet = new List<int> { 0, 1, 2, 3 };

        if (!_modSettings.EnableTwoFactor)
            widgetSet.Remove(3);

        for (var i = 0; i < 2; i++)
        {
            var widget = widgetSet[Random.Range(0, widgetSet.Count)];
            widgetSet.Remove(widget);
            DebugLog("Widget #{0} = {1}", i + 1, widgetTypes[widget]);
            switch (widget)
            {
                case 0:
                    _indicator = true;
                    SetIndicators();
                    break;
                case 1:
                    _ports = true;
                    SetPorts();
                    break;
                case 2:
                    _batteries = true;
                    SetBatteries();
                    break;
                default:
                    _twofactor = true;
                    SetTwoFactor();
                    break;
            }
        }

        if (_indicator && _batteries || (Random.value < 0.5f && !_twofactor))
        {
            IndicatorTransform.localPosition = TwoFactorTransform.localPosition;
        }
        GetComponent<KMWidget>().OnQueryRequest += GetQueryResponse;
        GetComponent<KMWidget>().OnWidgetActivate += OnActivate;
    }

    void Update()
    {
        TwoFactorUpdate();
        PortUpdate();
    }

    public void LogEdgework(string[] items, string edgeworkname)
    {
        var edgework = "";
        foreach (var response in items)
        {
            if (edgework == "")
                edgework = edgeworkname + " " + response;
            else
                edgework += ", " + response;
        }
        if (edgework != "")
            DebugLog(edgework);
    }

    public void OnActivate()
    {
        if (_twofactor)
            TwoFactorActivate();

        /*var idList = new List<string>();
        foreach (var response in Info.QueryWidgets("MultipleWidgetsIDQuery", null))
        {
            if (string.IsNullOrEmpty(response)) continue;
            var mwid = JsonConvert.DeserializeObject<Dictionary<string, int>>(response);
            if (mwid["MultipleWidgetsIDQuery"] > _widgetID) return;
            idList.Add(mwid["MultipleWidgetsIDQuery"].ToString());
        }

        DebugLog("For bomb with Serial# {0}, the Following widgets are present:", Info.GetSerialNumber());

        LogEdgework(idList.ToArray(),"MultipleWidget ID Numbers: ");

        LogEdgework(Info.GetOnIndicators().OrderBy(x => x).ToArray(),"Lit Indicators:");
        LogEdgework(Info.GetOffIndicators().OrderBy(x => x).ToArray(),"Unlit Indicators:");
        foreach (var color in IndicatorLights)
        {
            LogEdgework(Info.GetColoredIndicators(color.name).OrderBy(x => x).ToArray(), color.name + " Indicators:");
        }
        for (var batteries = 0; batteries <= 4; batteries++)
        {
            var holders = Info.GetBatteryHolderCount(batteries);
            if (holders == 0) continue;
            DebugLog(
                batteries == 1
                    ? "Number of Holders with {0} battery: {1}"
                    : "Number of Holders with {0} batteries: {1}", batteries, holders);
        }

        foreach (var plate in Info.GetPortPlates())
        {
            if(plate.Length == 0)
                DebugLog("Empty Port Plate");
            else
                LogEdgework(plate.OrderBy(x => x).ToArray(), "Port Plate:");
        }

        foreach (var twofactor in Info.GetTwoFactorCodes())
        {
            DebugLog("Two Factor: {0}", twofactor);
        }*/
    }

    public string GetQueryResponse(string queryKey, string queryInfo)
    {
        if (_batteries && GetBatteryQueryResponse(queryKey, queryInfo) != "")
            return GetBatteryQueryResponse(queryKey, queryInfo);
        if (_ports && GetPortQueryResponse(queryKey, queryInfo) != "")
            return GetPortQueryResponse(queryKey, queryInfo);
        if (_indicator && GetIndicatorQueryResponse(queryKey, queryInfo) != "")
            return GetIndicatorQueryResponse(queryKey, queryInfo);
        if (_twofactor && GetTwoFactorQueryResponse(queryKey, queryInfo) != "")
            return GetTwoFactorQueryResponse(queryKey, queryInfo);

        //Debugging for seeing what widgets ARE present on each bomb according to KMBombInfo.GetQueryResponse
        if (queryKey.Equals("MultipleWidgetsIDQuery"))
            return JsonConvert.SerializeObject(
                new Dictionary<string, int> {{ "MultipleWidgetsIDQuery", _widgetID}});
        return "";
    }

    #region Indicators
    #region EncryptedIndicators
    string[] labels = new string[] { "CLR", "IND", "TRN", "FRK", "CAR", "FRQ", "NSA", "SIG", "MSA", "SND", "BOB" };
    Dictionary<char, int[]> answers = new Dictionary<char, int[]>();
    Dictionary<char, char[]> secondary_answers = new Dictionary<char, char[]>();
    List<char> chars;

    static int lastTime = 0;
    static int modules = 0;

    private void addSymbol(char c, int[] i, char[] secondary)
    {
        answers.Add(c, i);
        secondary_answers.Add(c, secondary);
    }
    private char getSymbol(List<char> l, int i)
    {
        return l[i];
    }

    private void initDicts(MonoRandom rng)
    {
	    if (rng.Seed == 1)
	    {
		    addSymbol('ใ', new int[] {5, 0, 4}, new char[] {'G', 'D', 'G'});
		    addSymbol('ɮ', new int[] {4, 0, 5}, new char[] {'Z', 'D', 'R'});
		    addSymbol('ʖ', new int[] {0, -1, 4}, new char[] {'C', 'S', 'O'});
		    addSymbol('ฬ', new int[] {0, 2, 5}, new char[] {'J', 'X', 'Y'});
		    addSymbol('น', new int[] {2, 1, 2}, new char[] {'V', 'B', 'L'});
		    addSymbol('Þ', new int[] {-2, 5, 5}, new char[] {'T', 'L', 'J'});
		    addSymbol('ฏ', new int[] {4, 1, 2}, new char[] {'L', 'A', 'O'});
		    addSymbol('Ѩ', new int[] {3, 5, 4}, new char[] {'G', 'A', 'S'});
		    addSymbol('Ԉ', new int[] {4, 4, 2}, new char[] {'F', 'S', 'M'});
		    addSymbol('Ԓ', new int[] {3, 2, 3}, new char[] {'P', 'O', 'F'});
		    addSymbol('ด', new int[] {-1, 3, 4}, new char[] {'K', 'Q', 'K'});
		    addSymbol('ล', new int[] {-1, -2, 4}, new char[] {'D', 'N', 'L'});
		    addSymbol('Ж', new int[] {5, 0, 5}, new char[] {'Q', 'O', 'Z'});
			//addSymbol('Ⴟ', new int[] { 5, 5, 3 }, new char[] { 'W', 'M', 'C' });
		}
		else
	    {
		    for (var index = 0; index < rng.Next(0, 25); index++)
			    rng.NextDouble();
		    var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		    foreach (var c in "ใɮʖฬนÞฏѨԈԒดลЖႿ".OrderBy(x => rng.NextDouble()).Take(13))
		    {
			    var x = rng.Next(0, 8) - 2;
			    var y = rng.Next(0, 8) - 2;
			    var z = rng.Next(2, 6);

			    var i = alphabet[rng.Next(0, 26)];
			    var j = alphabet[rng.Next(0, 26)];
			    var k = alphabet[rng.Next(0, 26)];

				addSymbol(c, new int[] {x, y, z}, new char[] {i, j, k} );
		    }

		    labels = labels.OrderBy(x => rng.NextDouble()).ToArray();
	    }

	    chars = new List<char>(answers.Keys);
    }

    private string getIndicator(int i, string secondary)
    {
        i -= 1;
        if (i < 0 || i >= 11)
        {
            return secondary;
        }
        else
        {
            return labels[i];
        }
    }

    private void setSolution()
    {
        initDicts(RuleSeed.GetRNG());
        if (lastTime != (int)Time.realtimeSinceStartup)
        {
            modules = 0;
            lastTime = (int)Time.realtimeSinceStartup;
        }
        var encrypted = _modSettings.EnableEncryptedIndicators &&
                        modules < _modSettings.MaxEncryptedIndicators &&
                        Random.value < _modSettings.EncryptionProbability;

        if(encrypted)
            modules++;

        int solutionIndex = 0;
        List<char> selections = chars;
        string secondary_label = "";
        for (int i = 0; i < 3; i++)
        {
            int index = Random.Range(0, selections.Count);
            char selection = selections[index];
            char result = getSymbol(selections, index);
            IndicatorText.text += result;
            selections.Remove(selection);
            solutionIndex += answers[selection][i];
            secondary_label += secondary_answers[result][i];
        }

        _indicatorLabel = getIndicator(solutionIndex, secondary_label);
        if (!encrypted)
            IndicatorText.text = _indicatorLabel;
    }
    #endregion

    void SetIndicators()
    {
        IndicatorText.text = "";
        Indicator.SetActive(true);
        
        _indicatorLight = Random.value > 0.4f;
        if (_indicatorLight)
        {
            _indicatorLightColor = _modSettings.EnableIndicatorColors ? Random.Range(1, IndicatorLights.Length) : 1;
        }

        setSolution();
        var color = _modSettings.EnableIndicatorColors ? IndicatorLights[_indicatorLightColor].name : ((!_indicatorLight) ? "unlit" : "lit");
        if (!IndicatorText.text.Equals(_indicatorLabel))
        {
            //DebugLog("Indicator {0} is Encrypted as {1}", _indicatorLabel, IndicatorText.text);
            
            DebugLog("Encrypted Indicator {0} {1} acting as {0} {2}", color, IndicatorText.text, _indicatorLabel);
            IndicatorText.fontSize = 80;
        }
        else
        {
            DebugLog("Indicator {0} {1}", color, _indicatorLabel);
        }

        //Debug.LogFormat("[IndicatorWidget] Randomizing Indicator Widget: {0} {1}", (!_indicatorLight) ? "unlit" : "lit", _indicatorLabel);
        
        
        IndicatorLights[_indicatorLightColor].SetActive(true);
    }

    public string GetIndicatorQueryResponse(string queryKey, string queryInfo)
    {
        if (queryKey == KMBombInfo.QUERYKEY_GET_INDICATOR)
        {
            return JsonConvert.SerializeObject(new Dictionary<string, string>
            {
                {"label",_indicatorLabel},
                {"on",_indicatorLight.ToString()},
                {"display",IndicatorText.text}
            });
        }

        if (!_modSettings.EnableIndicatorColors) return "";
        if (queryKey == (KMBombInfo.QUERYKEY_GET_INDICATOR + "Color"))
        {
            return JsonConvert.SerializeObject(new Dictionary<string, string>
            {
                {"label",_indicatorLabel},
                {"color",IndicatorLights[_indicatorLightColor].name},
                {"display",IndicatorText.text}
            });
        }
        return "";
    }
    #endregion

    #region Ports
    void InitializePortSets()
    {
        _portGroups = new List<List<PortType>>
        {
            new List<PortType>{PortType.Serial, PortType.Parallel},
            new List<PortType> {PortType.PS2,PortType.DVI,PortType.RJ45,PortType.StereoRCA },
            new List<PortType> {PortType.HDMI,PortType.USB,PortType.ComponentVideo,PortType.AC,PortType.PCMCIA,PortType.VGA,PortType.CompositeVideo },
            new List<PortType> {PortType.DVI,PortType.StereoRCA,PortType.HDMI,PortType.ComponentVideo,PortType.VGA,PortType.CompositeVideo,PortType.AC },
            new List<PortType> {PortType.Parallel,PortType.Serial,PortType.PCMCIA,PortType.VGA,PortType.PS2,PortType.RJ45,PortType.USB,PortType.AC }
        };

        _portFiller = new Dictionary<PortType, GameObject>
        {
            {PortType.HDMI, HDMIPortFiller},
            {PortType.USB, USBPortFiller},
            {PortType.ComponentVideo, ComponentVideoPortFiller},
            {PortType.AC, ACPortFiller},
            {PortType.CompositeVideo, CompositeVideoPortFiller},
            {PortType.PS2, PS2PortFiller},
            {PortType.StereoRCA, RCAPortFiller},
            {PortType.RJ45,RJ45PortFiller }
        };

        _portLookup = new Dictionary<PortType, GameObject>
        {
            {PortType.Serial, SerialPort},
            {PortType.Parallel, ParallelPort},
            {PortType.PS2, PS2Port},
            {PortType.DVI, DVIPort},
            {PortType.RJ45, RJ45Port},
            {PortType.StereoRCA, StereoRCAPort},
            {PortType.HDMI, HDMIPort},
            {PortType.USB, USBPort},
            {PortType.ComponentVideo, ComponentVideoPort},
            {PortType.AC, ACPort},
            {PortType.PCMCIA, PCMCIAPort},
            {PortType.VGA, VGAPort},
            {PortType.CompositeVideo, CompositeVideoPort}
        };

        _portGroupNames = new[] { "Vanilla 1","Vanilla 2","New Ports","TV/Monitor Ports","Computer Ports"};

        foreach (var port in _portLookup.Keys)
            _portLookup[port].SetActive(false);

        foreach (var portFiller in _portFiller.Keys)
            _portFiller[portFiller].SetActive(true);

    }

    void SetPorts()
    {
        Ports.SetActive(true);
        MultipleWidgetTopHalf.SetActive(false);
        InitializePortSets();
        _portList = new List<string>();

        if (!_modSettings.DebugModeForceAllPossiblePorts)
        {
            var portset = Random.Range(0, _modSettings.EnableExtendedPorts ? _portGroups.Count : 2);
            foreach (var set in _portGroups[portset])
            {
                if (!_modSettings.DebugModeForceAllPortsInCurrentSet && !(Random.value > 0.5f)) continue;
                _presentPorts |= set;
                _portLookup[set].SetActive(true);
                if (_portFiller.ContainsKey(set))
                    _portFiller[set].SetActive(false);
                _portList.Add(set.ToString());
            }

            //DebugLog("Using ports from the following port set: {0}", _portGroupNames[portset]);
            DebugLog("Ports ({0}): {1}", _portGroupNames[portset], _presentPorts.ToString());
        }
        else
        {
            DebugLog("Forcing EVERY possible port.");
            foreach (var port in _portLookup.Keys)
            {
                _presentPorts |= port;
                _portLookup[port].SetActive(true);
                _portList.Add(port.ToString());
                if (_portFiller.ContainsKey(port))
                {
                    _portFiller[port].SetActive(false);
                }
            }
            DebugLog("Ports ({0}): {1}", "Everything", _presentPorts.ToString());
        }
        //Debug.LogFormat("[PortWidget] Randomizing Port Widget: {0}", _presentPorts.ToString());
    }

    public bool IsPortPresent(PortType port)
    {
        return (_presentPorts & port) == port;
    }

    public void PortUpdate()
    {
        if (!_ports) return;

        if (IsPortPresent(PortType.RJ45))
        {
            if (_activityLEDTime <= 0)
            {
                _activityLEDTime = Random.Range(5, 30);
                ActivityLED.SetActive(!ActivityLED.activeSelf);
            }
            else
            {
                _activityLEDTime--;
            }
        }
    }

    public string GetPortQueryResponse(string queryKey, string queryInfo)
    {
        if (queryKey == KMBombInfo.QUERYKEY_GET_PORTS)
        {
            var dictionary = new Dictionary<string, List<string>>();
            dictionary.Add("presentPorts", _portList);
            return JsonConvert.SerializeObject(dictionary);
        }
        return "";
    }

    [Flags]
    public enum PortType
    {
        None = 0,

        Serial = 1,
        Parallel = 2,

        DVI = 4,
        PS2 = 8,
        RJ45 = 16,
        StereoRCA = 32,

        HDMI = 64,
        USB = 128,
        ComponentVideo = 256,

        AC = 512,
        PCMCIA = 1024,
        VGA = 2048,
        CompositeVideo = 4096,
    }
    #endregion

    #region Batteries
    void SetBatteries()
    {
        do
        {
            _batteryType = _modSettings.EnableExtendedBatteries
                ? (BatteryType) Random.Range(0, Batteries.Length)
                : (BatteryType) Random.Range(1, 3);
        } while (_modSettings.DebugModeBatteries && _modSettings.EnableExtendedBatteries 
            && (_batteryType == BatteryType.NineVolt || _batteryType == BatteryType.AAx2));
        var holder = (int)_batteryType - 1;
        if (holder < 0) holder = Random.Range(0, BatteryHolders.Length);
        _holderType = (BatteryType) (holder + 1);
        DebugLog("Putting {0} {1} into a holder that fits {2} {3}.",
            GetNumberOfBatteries(),
            _batteryType == BatteryType.NineVolt
                ? "battery"
                : "batteries",
            holder + 1,
            holder == 0
                ? "battery"
                : "batteries");
        //Debug.LogFormat("[BatteryWidget] Randomizing Battery Widget: {0}", GetNumberOfBatteries());
        Batteries[(int) _batteryType].SetActive(true);
        BatteryHolders[holder].SetActive(true);

        foreach (var cell in AACells)
        {
            cell.Rotate(new Vector3(0, 0, Random.Range(0, 360.0f)));
        }
        NineVoltCell.Rotate(new Vector3(0, 0, Random.value < 0.5f ? 0 : 180));
    }

    int GetNumberOfBatteries()
    {
        return (int) _batteryType;
    }

    public string GetBatteryQueryResponse(string queryKey, string queryInfo)
    {
        if (queryKey == KMBombInfo.QUERYKEY_GET_BATTERIES)
        {
            return JsonConvert.SerializeObject(new Dictionary<string, int>
            {
                {"numbatteries",GetNumberOfBatteries()}
            });
        }

        if (!_modSettings.EnableExtendedBatteries) return "";
        if (queryKey == "custombatteries")
        {
            return JsonConvert.SerializeObject(new BatterySetInfo
            {
                numbatteries = GetNumberOfBatteries(),
                capacity = (int)_holderType,
                type = (_holderType == BatteryType.NineVolt ? "NineVolt" : "AA")
            });
        }
        return "";
    }

    public enum BatteryType
    {
        Empty,
        NineVolt,
        AAx2,
        AAx3,
        AAx4
    }
    #endregion

    #region TwoFactor
    void SetTwoFactor()
    {
        if (_modSettings.TwoFactorDuration < 30)
            _modSettings.TwoFactorDuration = 30;

        if (_modSettings.TwoFactorDuration > 999)
            _modSettings.TwoFactorDuration = 999;

        GenerateKey();
        TwoFactor.SetActive(true);
    }

    private void GenerateKey()
    {
        _key = Random.Range(0, 1000000);
        //DebugLog(_key > -1 ? "Next Two-Factor key is {0}" : "First Two-Factor key is {0}", _key);
    }

    private void DisplayKey()
    {
        TwoFactorDigits.text = string.Format("{0,6}.", _key);
    }

    void UpdateKey()
    {
        GetComponent<KMAudio>().HandlePlaySoundAtTransform(Notify.name, transform);
        GenerateKey();
        DisplayKey();
    }

    public void TwoFactorActivate()
    {
        _timeElapsed = 0f;
        DisplayKey();
    }

    void UpdateTwoFactorColor(Color color)
    {
        TwoFactorDisplay.material.color = color;
    }

    private const float FadeToRedTime = 10.0f;
    void TwoFactorUpdate()
    {
        if (!_twofactor) return;
        _timeElapsed += Time.deltaTime;

        TwoFactorTimeRemaining.text = string.Format("{0,3}",(int)(_modSettings.TwoFactorDuration - _timeElapsed));

        if (_timeElapsed < (_modSettings.TwoFactorDuration - FadeToRedTime)) return;

        var colorChange = _timeElapsed - (_modSettings.TwoFactorDuration - FadeToRedTime);
        colorChange = (FadeToRedTime - colorChange) / FadeToRedTime;

        var redDiff = 127f - (108f * colorChange);
        var greenDiff = 255f * colorChange;

        if (_timeElapsed < _modSettings.TwoFactorDuration)
        {
            UpdateTwoFactorColor(new Color(redDiff / 255, greenDiff / 255, 0f / 255));
            return;
        }

        _timeElapsed = 0f;
        UpdateTwoFactorColor(new Color(19f / 255, 255f / 255, 0f / 255));
        UpdateKey();
    }

    public string GetTwoFactorQueryResponse(string queryKey, string queryInfo)
    {
        if (queryKey == KMBombInfoExtensions.WidgetQueryTwofactor && _twofactor)
        {
            return JsonConvert.SerializeObject(new Dictionary<string, int> { { KMBombInfoExtensions.WidgetTwofactorKey, _key }});
        }
        return "";
    }
    #endregion
}

static class Ext
{
    public static Color WithAlpha(this Color color, float alpha) { return new Color(color.r, color.g, color.b, alpha); }

    public static T[] NewArray<T>(params T[] array) { return array; }
}
