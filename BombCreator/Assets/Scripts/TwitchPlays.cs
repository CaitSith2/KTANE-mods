using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
    class TwitchPlays
    {
        private static GameObject _gameObject;
	    private static bool _tweaks;

        private static IDictionary<string, object> Properties
        {
            get
            {
                return _gameObject == null
                    ? null
                    : _gameObject.GetComponent<IDictionary<string, object>>();
            }
        }

        //Call this in KMGameState.Setup
        public static IEnumerator Refresh()
        {
	        _tweaks = false;
            for (var i = 0; i < 4 && _gameObject == null; i++)
            {
                _gameObject = GameObject.Find("TwitchPlays_Info");
	            if (_gameObject == null)
	            {
		            _gameObject = GameObject.Find("Tweaks_Info");
		            _tweaks = _gameObject != null;
	            }

	            yield return null;
            }
        }

        public static bool Installed()
        {
            return _gameObject != null;
        }

        public static bool TimeMode()
        {
            return Properties != null && Properties.ContainsKey("TimeMode") && ((bool) Properties["TimeMode"]);
        }

        public static void SetTimeMode(bool on)
        {
            if (Properties == null || !Properties.ContainsKey("TimeMode")) return;
            Properties["TimeMode"] = on;
        }

        public static bool ZenMode()
        {
            return Properties != null && !_tweaks && Properties.ContainsKey("ZenMode") && ((bool)Properties["ZenMode"]);
        }

        public static void SetZenMode(bool on)
        {
            if (Properties == null || _tweaks || !Properties.ContainsKey("ZenMode")) return;
            Properties["ZenMode"] = on;
        }

        public static int TimeModeTimeLimit(int time = 300, bool force = false)
        {
	        var key = _tweaks ? "TimeModeStartingTime" : "TimeModeTimeLimit";

	        if (Properties.ContainsKey(key) && (force || TimeMode()))
	        {
		        int timeint;
		        if (Properties[key] is float)
		        {
			        var timefloat = ((float) Properties[key]) * 60.0F;
			        timeint = (int) timefloat;
		        }
		        else if (Properties[key] is int)
		        {
			        timeint = ((int) Properties[key]) * 60;
		        }
		        else
		        {
			        timeint = time;
		        }

		        return timeint;
	        }

	        return time;
        }

        public static void SetTimeModeTimeLimit(int time)
        {
	        var key = _tweaks ? "TimeModeStartingTime" : "TimeModeTimeLimit";
			if (!TimeMode() || !Properties.ContainsKey(key)) return;
	        if (_tweaks)
		        Properties[key] = (float) Math.Ceiling(time / 60f);
			else
				Properties[key] = (int)Math.Ceiling(time / 60f);
        }

        public static void SetReward(int reward)
        {
            if (Properties != null && !_tweaks && Properties.ContainsKey("Reward"))
            {
                Properties["Reward"] = reward;
            }
        }

        public static void SendMessage(string s)
        {
            if (Properties != null && !_tweaks && Properties.ContainsKey("ircConnectionSendMessage"))
                Properties["ircConnectionSendMessage"] = s;
        }
    }
}
