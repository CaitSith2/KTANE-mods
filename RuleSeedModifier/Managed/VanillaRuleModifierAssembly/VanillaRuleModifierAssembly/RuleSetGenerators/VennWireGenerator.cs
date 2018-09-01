using System;
using System.Collections.Generic;
using Assets.Scripts.Components.VennWire;
using Assets.Scripts.Rules;

namespace VanillaRuleModifierAssembly.RuleSetGenerators
{
    public class VennWireGenerator : AbstractRuleSetGenerator
    {
        public VennWireGenerator()
        {
            CutInstructionWeights = new Dictionary<CutInstruction, float>();
            var enumerator = Enum.GetValues(typeof(CutInstruction)).GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    var obj = enumerator.Current;
                    if (obj != null) CutInstructionWeights.Add((CutInstruction) obj, 1f);
                }
            }
            finally
            {
                IDisposable disposable;
                if ((disposable = (enumerator as IDisposable)) != null)
                {
                    disposable.Dispose();
                }
            }
            PossibleInstructions = new List<CutInstruction>((CutInstruction[])Enum.GetValues(typeof(CutInstruction)));
        }

        protected CutInstruction GetWeightedRandomCutInstruction()
        {
            var num = 0f;
            foreach (var key in PossibleInstructions)
            {
                if (!CutInstructionWeights.ContainsKey(key))
                {
                    CutInstructionWeights.Add(key, 1f);
                }
                num += CutInstructionWeights[key];
            }
            var num2 = rand.NextDouble() * num;
            foreach (var cutInstruction in PossibleInstructions)
            {
                if (num2 < CutInstructionWeights[cutInstruction])
                {
                    Dictionary<CutInstruction, float> dictionary;
                    CutInstruction key2;
                    (dictionary = CutInstructionWeights)[key2 = cutInstruction] = dictionary[key2] * 0.1f;
                    return cutInstruction;
                }
                num2 -= CutInstructionWeights[cutInstruction];
            }
            return PossibleInstructions[rand.Next(0, PossibleInstructions.Count)];
        }

        protected override AbstractRuleSet CreateRules(bool useDefault)
        {
            var dictionary = new Dictionary<VennWireState, CutInstruction>
            {
                {new VennWireState(false, false, false, false), CutInstruction.Cut},
                { new VennWireState(false, false, false, true), GetWeightedRandomCutInstruction()},
                { new VennWireState(false, false, true, false), GetWeightedRandomCutInstruction()},
                { new VennWireState(false, false, true, true), GetWeightedRandomCutInstruction()},
                { new VennWireState(false, true, false, false), GetWeightedRandomCutInstruction()},
                { new VennWireState(false, true, false, true), GetWeightedRandomCutInstruction()},
                { new VennWireState(false, true, true, false), GetWeightedRandomCutInstruction()},
                { new VennWireState(false, true, true, true), GetWeightedRandomCutInstruction()},
                { new VennWireState(true, false, false, false), GetWeightedRandomCutInstruction()},
                { new VennWireState(true, false, false, true), GetWeightedRandomCutInstruction()},
                { new VennWireState(true, false, true, false), GetWeightedRandomCutInstruction()},
                { new VennWireState(true, false, true, true), GetWeightedRandomCutInstruction()},
                { new VennWireState(true, true, false, false), GetWeightedRandomCutInstruction()},
                { new VennWireState(true, true, false, true), GetWeightedRandomCutInstruction()},
                { new VennWireState(true, true, true, false), GetWeightedRandomCutInstruction()},
                { new VennWireState(true, true, true, true), GetWeightedRandomCutInstruction()}
            };
            return new VennWireRuleSet(dictionary);
        }

        public VennWireRuleSet GenerateVennWireRules(int seed)
        {
            CutInstructionWeights.Clear();
            switch (seed)
            {
                case 1:
                    return (VennWireRuleSet) GenerateRuleSet(seed);
                default:
                    var ruleSet = (VennWireRuleSet)GenerateRuleSet(seed);
                    if (ruleSet.GetStatesThatRequiresCutting().Count >= 2)
                        ruleSet.RuleDict[new VennWireState(false, false, false, false)] = GetWeightedRandomCutInstruction();
                    return ruleSet;
            }
        }

        protected Dictionary<CutInstruction, float> CutInstructionWeights;
        protected List<CutInstruction> PossibleInstructions;
    }
}