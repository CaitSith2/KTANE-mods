using System;
using UnityEngine;

namespace VanillaRuleModifierAssembly
{
    public class RuleSeedModifierProperties : PropertiesBehaviour
    {
		public RuleSeedModifierProperties()
        {
            AddProperty("RuleSeed", new Property(RuleSeed_Get, RuleSeed_Set));
            AddProperty("IsSeedVanilla", new Property(IsSeedVanilla_Get, null));
            AddProperty("IsSeedModded", new Property(IsSeedModded_Get, null));
            AddProperty("GetRuleManual",new Property(RuleManaul_Get, null));
		}

		public static void AddSupportedModule(string moduleType)
	    {
		    if (!VanillaRuleModifier.ModsThatSupportRuleSeedModifier.Contains(moduleType))
			    VanillaRuleModifier.ModsThatSupportRuleSeedModifier.Add(moduleType);
			foreach (RuleSeedModifierProperties properties in VanillaRuleModifier.PublicProperties)
			{
				if (!properties.ContainsKey($"RuleSeedModifier_{moduleType}"))
					properties.AddProperty($"RuleSeedModifier_{moduleType}",new Property(properties.RuleSeed_Get,null));
			    
		    }
	    }

	    private object RuleSeed_Get()
        {
            return VanillaRuleModifer._modSettings.Settings.RuleSeed;
        }

        private void RuleSeed_Set(object seed)
        {
            const string invalidArguments = "Arguments need to be either (int seed) or (object[2] {int seed, bool save}";
            if (VanillaRuleModifer.CurrentState != KMGameInfo.State.Setup && VanillaRuleModifer.CurrentState != KMGameInfo.State.PostGame)
                throw new Exception("Setting of Rule seed is only allowed duing Setup or Post game.");

            bool saveSettings = false;
            int ruleseed;
            switch (seed)
            {
	            case object[] objects when objects.Length == 2 && objects[0] is int ruleSeed && objects[1] is bool save:
	                ruleseed = ruleSeed == int.MinValue ? 0 : Mathf.Abs(ruleSeed);
		            saveSettings = save;
		            break;
	            case int ruleSeed:
	                ruleseed = ruleSeed == int.MinValue ? 0 : Mathf.Abs(ruleSeed);
                    break;
	            default:
		            throw new ArgumentException(invalidArguments);
            }
	        VanillaRuleModifer.SetRuleSeed(ruleseed, saveSettings);

        }

        private static object IsSeedVanilla_Get()
        {
            return CommonReflectedTypeInfo.IsVanillaSeed;
        }

        private static object IsSeedModded_Get()
        {
            return CommonReflectedTypeInfo.IsModdedSeed;
        }

        private object RuleManaul_Get()
        {
            return VanillaRuleModifer.GenerateManual();
        }

        internal VanillaRuleModifier VanillaRuleModifer { get; set; }
    }
}