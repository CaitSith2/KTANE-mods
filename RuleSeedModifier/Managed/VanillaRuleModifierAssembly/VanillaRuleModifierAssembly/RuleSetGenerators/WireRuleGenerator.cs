using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Rules;
using BombGame;

namespace VanillaRuleModifierAssembly.RuleSetGenerators
{
    public class WireRuleGenerator : AbstractRuleSetGenerator
    {
        private bool IsWireQueryValid(Rule rule)
        {
            if (rule.Queries.Count == 1)
                return true;
            var query = rule.GetQueryString();
            var lastwirecolor = QueryableWireProperty.LastWireIsColor.Text;
            var exactlyonecolor = QueryableWireProperty.IsExactlyOneColorWire.Text;
            var morethanonecolor = QueryableWireProperty.MoreThanOneColorWire.Text;
            var nocolor = QueryableWireProperty.IsExactlyZeroColorWire.Text;
            for (var i = 0; i < 4; i++)
            {
                if (query.Contains(exactlyonecolor.Replace("{color}", ((WireColor)i).ToString())) && query.Contains(nocolor.Replace("{color}", ((WireColor)i).ToString())))
                    return false;
                if (query.Contains(exactlyonecolor.Replace("{color}", ((WireColor)i).ToString())) && query.Contains(morethanonecolor.Replace("{color}", ((WireColor)i).ToString())))
                    return false;
                if (query.Contains(morethanonecolor.Replace("{color}", ((WireColor)i).ToString())) && query.Contains(nocolor.Replace("{color}", ((WireColor)i).ToString())))
                    return false;

                if (!query.Contains(lastwirecolor.Replace("{color}", ((WireColor)i).ToString()))) continue;
                if (query.Contains(nocolor.Replace("{color}", ((WireColor)i).ToString()))) return false;
                for (var j = i + 1; j < 5; j++)
                {
                    if (query.Contains(lastwirecolor.Replace("{color}", ((WireColor)j).ToString())))
                        return false;
                }
            }
            return true;
        }

        public WireRuleSet CreateWireRules(int seed)
        {
            switch (seed)
            {
                case 1:
                    {
                        WireRuleSet wireRuleSet = new WireRuleSet();
                        wireRuleSet.RulesDictionary[3] = new List<Rule>
                    {
                        new Rule
                        {
                            Queries = new List<Query> {new Query {Args = new Dictionary<string, object>() {{"color", WireColor.red}}, Property = QueryableWireProperty.IsExactlyZeroColorWire}},
                            Solution = WireSolutions.WireIndex1
                        },
                        new Rule
                        {
                            Queries = new List<Query> {new Query {Args = new Dictionary<string, object>() {{"color", WireColor.white}}, Property = QueryableWireProperty.LastWireIsColor}},
                            Solution = WireSolutions.WireLast
                        },
                        new Rule
                        {
                            Queries = new List<Query> {new Query {Args = new Dictionary<string, object>() {{"color", WireColor.blue}}, Property = QueryableWireProperty.MoreThanOneColorWire}},
                            Solution = WireSolutions.WireColorCutLast,
                            SolutionArgs = new Dictionary<string, object> { {"color", WireColor.blue} }
                        },
                        new Rule
                        {
                            Queries = new List<Query> {new Query {Property = QueryableProperty.Otherwise}},
                            Solution = WireSolutions.WireLast
                        }
                    };

                        wireRuleSet.RulesDictionary[4] = new List<Rule>
                    {
                        new Rule
                        {
                            Queries = new List<Query>
                            {
                                new Query {Args = new Dictionary<string, object>() {{"color", WireColor.red}}, Property = QueryableWireProperty.MoreThanOneColorWire},
                                new Query {Property = QueryableProperty.IsSerialNumberOdd},
                            },
                            Solution = WireSolutions.WireColorCutLast,
                            SolutionArgs = new Dictionary<string, object> { {"color", WireColor.red} }
                        },
                        new Rule
                        {
                            Queries = new List<Query>
                            {
                                new Query {Args = new Dictionary<string, object>() {{"color", WireColor.yellow}}, Property = QueryableWireProperty.LastWireIsColor},
                                new Query {Args = new Dictionary<string, object>() {{"color", WireColor.red}}, Property = QueryableWireProperty.IsExactlyZeroColorWire},
                            },
                            Solution = WireSolutions.WireIndex0
                        },
                        new Rule
                        {
                            Queries = new List<Query>
                            {
                                new Query {Args = new Dictionary<string, object>() {{"color", WireColor.blue}}, Property = QueryableWireProperty.IsExactlyOneColorWire}
                            },
                            Solution = WireSolutions.WireIndex0
                        },
                        new Rule
                        {
                            Queries = new List<Query>
                            {
                                new Query {Args = new Dictionary<string, object>() {{"color", WireColor.yellow}}, Property = QueryableWireProperty.MoreThanOneColorWire}
                            },
                            Solution = WireSolutions.WireLast
                        },
                        new Rule
                        {
                            Queries = new List<Query>
                            {
                                new Query {Property = QueryableProperty.Otherwise}
                            },
                            Solution = WireSolutions.WireIndex1
                        }
                    };

                        wireRuleSet.RulesDictionary[5] = new List<Rule>
                    {
                        new Rule
                        {
                            Queries = new List<Query>
                            {
                                new Query {Args = new Dictionary<string, object>() {{"color", WireColor.black}}, Property = QueryableWireProperty.LastWireIsColor},
                                new Query {Property = QueryableProperty.IsSerialNumberOdd},
                            },
                            Solution = WireSolutions.WireIndex3
                        },
                        new Rule
                        {
                            Queries = new List<Query>
                            {
                                new Query {Args = new Dictionary<string, object>() {{"color", WireColor.red}}, Property = QueryableWireProperty.IsExactlyOneColorWire},
                                new Query {Args = new Dictionary<string, object>() {{"color", WireColor.yellow}}, Property = QueryableWireProperty.MoreThanOneColorWire},
                            },
                            Solution = WireSolutions.WireIndex0
                        },
                        new Rule
                        {
                            Queries = new List<Query>
                            {
                                new Query {Args = new Dictionary<string, object>() {{"color", WireColor.black}}, Property = QueryableWireProperty.IsExactlyZeroColorWire}
                            },
                            Solution = WireSolutions.WireIndex1
                        },
                        new Rule
                        {
                            Queries = new List<Query>
                            {
                                new Query {Property = QueryableProperty.Otherwise}
                            },
                            Solution = WireSolutions.WireIndex0
                        }
                    };

                        wireRuleSet.RulesDictionary[6] = new List<Rule>
                    {
                        new Rule
                        {
                            Queries = new List<Query>
                            {
                                new Query {Args = new Dictionary<string, object>() {{"color", WireColor.yellow}}, Property = QueryableWireProperty.IsExactlyZeroColorWire},
                                new Query {Property = QueryableProperty.IsSerialNumberOdd},
                            },
                            Solution = WireSolutions.WireIndex2
                        },
                        new Rule
                        {
                            Queries = new List<Query>
                            {
                                new Query {Args = new Dictionary<string, object>() {{"color", WireColor.yellow}}, Property = QueryableWireProperty.IsExactlyOneColorWire},
                                new Query {Args = new Dictionary<string, object>() {{"color", WireColor.white}}, Property = QueryableWireProperty.MoreThanOneColorWire},
                            },
                            Solution = WireSolutions.WireIndex3
                        },
                        new Rule
                        {
                            Queries = new List<Query>
                            {
                                new Query {Args = new Dictionary<string, object>() {{"color", WireColor.red}}, Property = QueryableWireProperty.IsExactlyZeroColorWire}
                            },
                            Solution = WireSolutions.WireLast
                        },
                        new Rule
                        {
                            Queries = new List<Query>
                            {
                                new Query {Property = QueryableProperty.Otherwise}
                            },
                            Solution = WireSolutions.WireIndex3
                        }
                    };

                        return wireRuleSet;
                    }
                default:
                    return (WireRuleSet)GenerateRuleSet(seed);
            }
        }

        protected override AbstractRuleSet CreateRules(bool useDefault)
        {
            WireRuleSet wireRuleSet = new WireRuleSet();
            QuerySet serialNumberQueries = QuerySet.GetSerialNumberQueries();
            QuerySet wireQueries = QuerySet.GetWireQueries();
            QuerySet portQueries = QueryablePorts.GetPortQueries();
            List<QuerySet> list = new List<QuerySet>();
            for (int i = WireSetComponent.MIN_WIRES; i <= WireSetComponent.MAX_WIRES; i++)
            {
                List<Rule> list2 = new List<Rule>();
                list.Clear();
                list.Add(serialNumberQueries);
                list.Add(wireQueries);
                if(CommonReflectedTypeInfo.IsModdedSeed)
                    list.Add(portQueries);
                this.queryPropertyWeights.Clear();
                this.solutionWeights.Clear();
                int numRules = GetNumRules();
                for (int j = 0; j < numRules; j++)
                {
                    List<WireColor> listOfWireColors = RuleUtil.GetListOfWireColors();
                    Rule rule = new Rule();
                    int numQueriesForRule = GetNumQueriesForRule();
                    List<WireColor> list3 = new List<WireColor>();
                    int num = i - 1;
                    for (int k = 0; k < numQueriesForRule; k++)
                    {
                        bool compoundQueriesAllowed = k > 0;
                        List<QueryableProperty> possibleQueryableProperties = this.CalculatePossibleQueryableProperties(list, num, compoundQueriesAllowed);
                        QueryableProperty queryableProperty = SelectQueryableProperty(possibleQueryableProperties);
                        Query query = new Query();
                        query.Property = queryableProperty;
                        if (queryableProperty is QueryableWireProperty)
                        {
                            QueryableWireProperty queryableWireProperty = (QueryableWireProperty)queryableProperty;
                            num -= queryableWireProperty.WiresInvolvedInQuery;
                            if (queryableWireProperty.UsesColor)
                            {
                                WireColor wireColor = listOfWireColors[this.rand.Next(0, listOfWireColors.Count)];
                                listOfWireColors.Remove(wireColor);
                                query.Args.Add("color", wireColor);
                                if (queryableWireProperty.ColorAvailableForSolution)
                                {
                                    list3.Add(wireColor);
                                }
                            }
                        }
                        rule.Queries.Add(query);
                    }
                    List<Solution> possibleSolutions = this.CalculatePossibleSolutions(i, rule);
                    Solution solution = SelectSolution(possibleSolutions);
                    rule.Solution = solution;
                    if (list3.Count > 0)
                    {
                        rule.SolutionArgs.Add("color", list3[this.rand.Next(0, list3.Count)]);
                    }
                    if (CommonReflectedTypeInfo.IsVanillaSeed || IsWireQueryValid(rule))
                        list2.Add(rule);
                    else
                        j--;    //Previous rule was never valid.

                }
                list2 = list2.OrderByDescending(x => x.Queries.Count).ToList();
                Rule rule2 = new Rule();
                Query query2 = new Query();
                query2.Property = QueryableProperty.Otherwise;
                rule2.Queries.Add(query2);
                List<Solution> list4 = this.CalculatePossibleSolutions(i, rule2);
                if (CommonReflectedTypeInfo.IsModdedSeed)
                    list4.Remove(list2.Last().Solution);    //Enforce no redundant rules.
                rule2.Solution = list4[this.rand.Next(0, list4.Count)];
                list2.Add(rule2);
                wireRuleSet.RulesDictionary[i] = list2;
            }
            return wireRuleSet;
        }

        private List<QueryableProperty> CalculatePossibleQueryableProperties(List<QuerySet> querySets, int wiresAvailableInQuery, bool compoundQueriesAllowed)
        {
            List<QueryableProperty> list = new List<QueryableProperty>();
            foreach (QuerySet querySet in querySets)
            {
                foreach (QueryableProperty queryableProperty in querySet.QueryableProperties)
                {
                    if (!queryableProperty.CompoundQueryOnly || compoundQueriesAllowed)
                    {
                        QueryableWireProperty queryableWireProperty = queryableProperty as QueryableWireProperty;
                        if (queryableWireProperty == null || queryableWireProperty.WiresInvolvedInQuery <= wiresAvailableInQuery)
                        {
                            list.Add(queryableProperty);
                        }
                    }
                }
            }
            List<QueryableProperty> list2 = list.ToList<QueryableProperty>();
            foreach (QueryableProperty key in list2)
            {
                if (!this.queryPropertyWeights.ContainsKey(key))
                {
                    this.queryPropertyWeights.Add(key, 1f);
                }
            }
            return list2;
        }

        protected List<Solution> CalculatePossibleSolutions(int wireCount, Rule rule)
        {
            List<Solution> list = new List<Solution>();
            list.Add(WireSolutions.WireIndex0);
            list.Add(WireSolutions.WireIndex1);
            list.Add(WireSolutions.WireLast);
            if (wireCount >= 4)
            {
                list.Add(WireSolutions.WireIndex2);
            }
            if (wireCount >= 5)
            {
                list.Add(WireSolutions.WireIndex3);
            }
            if (wireCount >= 6)
            {
                list.Add(WireSolutions.WireIndex4);
            }
            foreach (Query query in rule.Queries)
            {
                if (query.Property.AdditionalSolutions != null)
                {
                    list.AddRange(query.Property.AdditionalSolutions);
                }
            }
            foreach (Solution key in list)
            {
                if (!this.solutionWeights.ContainsKey(key))
                {
                    this.solutionWeights.Add(key, 1f);
                }
            }
            return list;
        }
    }
}