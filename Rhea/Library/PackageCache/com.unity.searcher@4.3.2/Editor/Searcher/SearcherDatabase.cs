using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.Searcher
{
    [PublicAPI]
    public class SearcherDatabase : SearcherDatabaseBase
    {
        Dictionary<string, IReadOnlyList<ValueTuple<string, float>>> m_Index = new Dictionary<string, IReadOnlyList<ValueTuple<string, float>>>();

        class Result
        {
            public SearcherItem item;
            public float maxScore;
        }

        const bool k_IsParallel = true;

        public Func<string, SearcherItem, bool> MatchFilter { get; set; }

        public static SearcherDatabase Create(
            List<SearcherItem> items,
            string databaseDirectory,
            bool serializeToFile = true
        )
        {
            if (serializeToFile && databaseDirectory != null && !Directory.Exists(databaseDirectory))
                Directory.CreateDirectory(databaseDirectory);

            var database = new SearcherDatabase(databaseDirectory, items);

            if (serializeToFile)
                database.SerializeToFile();

            database.BuildIndex();
            return database;
        }

        public static SearcherDatabase Load(string databaseDirectory)
        {
            if (!Directory.Exists(databaseDirectory))
                throw new InvalidOperationException("databaseDirectory not found.");

            var database = new SearcherDatabase(databaseDirectory, null);
            database.LoadFromFile();
            database.BuildIndex();

            return database;
        }

        public SearcherDatabase(IReadOnlyCollection<SearcherItem> db)
            : this("", db)
        {
        }

        SearcherDatabase(string databaseDirectory, IReadOnlyCollection<SearcherItem> db)
            : base(databaseDirectory)
        {
            m_ItemList = new List<SearcherItem>();
            var nextId = 0;

            if (db != null)
                foreach (var item in db)
                    AddItemToIndex(item, ref nextId, null);
        }

        public override List<SearcherItem> Search(string query, out float localMaxScore)
        {
            // Match assumes the query is trimmed
            query = query.Trim(' ', '\t');
            localMaxScore = 0;

            if (string.IsNullOrWhiteSpace(query))
            {
                if (MatchFilter == null)
                    return m_ItemList;

                // ReSharper disable once RedundantLogicalConditionalExpressionOperand
                if (k_IsParallel && m_ItemList.Count > 100)
                    return FilterMultiThreaded(query);

                return FilterSingleThreaded(query);
            }

            var finalResults = new List<SearcherItem> { null };
            var max = new Result();
            var tokenizedQuery = new List<string>();
            foreach (var token in Tokenize(query))
            {
                tokenizedQuery.Add(token.Trim().ToLower());
            }

            // ReSharper disable once RedundantLogicalConditionalExpressionOperand
            if (k_IsParallel && m_ItemList.Count > 100)
                SearchMultithreaded(query, tokenizedQuery, max, finalResults);
            else
                SearchSingleThreaded(query, tokenizedQuery, max, finalResults);

            localMaxScore = max.maxScore;
            if (max.item != null)
                finalResults[0] = max.item;
            else
                finalResults.RemoveAt(0);

            return finalResults;
        }

        protected virtual bool Match(string query, IReadOnlyList<string> tokenizedQuery, SearcherItem item, out float score)
        {
            var filter = MatchFilter?.Invoke(query, item) ?? true;
            return Match(tokenizedQuery, item.Path, out score) && filter;
        }

        List<SearcherItem> FilterSingleThreaded(string query)
        {
            var result = new List<SearcherItem>();

            foreach (var searcherItem in m_ItemList)
            {
                if (!MatchFilter.Invoke(query, searcherItem))
                    continue;

                result.Add(searcherItem);
            }

            return result;
        }

        List<SearcherItem> FilterMultiThreaded(string query)
        {
            var result = new List<SearcherItem>();
            var count = Environment.ProcessorCount;
            var tasks = new Task[count];
            var lists = new List<SearcherItem>[count];
            var itemsPerTask = (int)Math.Ceiling(m_ItemList.Count / (float)count);

            for (var i = 0; i < count; i++)
            {
                var i1 = i;
                tasks[i] = Task.Run(() =>
                {
                    lists[i1] = new List<SearcherItem>();

                    for (var j = 0; j < itemsPerTask; j++)
                    {
                        var index = j + itemsPerTask * i1;
                        if (index >= m_ItemList.Count)
                            break;

                        var item = m_ItemList[index];
                        if (!MatchFilter.Invoke(query, item))
                            continue;

                        lists[i1].Add(item);
                    }
                });
            }

            Task.WaitAll(tasks);

            for (var i = 0; i < count; i++)
            {
                result.AddRange(lists[i]);
            }

            return result;
        }

        readonly float k_ScoreCutOff = 0.33f;

        void SearchSingleThreaded(string query, IReadOnlyList<string> tokenizedQuery, Result max, ICollection<SearcherItem> finalResults)
        {
            List<Result> results = new List<Result>();

            foreach (var item in m_ItemList)
            {
                float score = 0;
                if (query.Length == 0 || Match(query, tokenizedQuery, item, out score))
                {
                    if (score > max.maxScore)
                    {
                        max.item = item;
                        max.maxScore = score;
                    }
                    results.Add(new Result() { item = item, maxScore = score});
                }
            }

            PostprocessResults(results, finalResults, max);
        }

        void SearchMultithreaded(string query, IReadOnlyList<string> tokenizedQuery, Result max, List<SearcherItem> finalResults)
        {
            var count = Environment.ProcessorCount;
            var tasks = new Task[count];
            var localResults = new Result[count];
            var queue = new ConcurrentQueue<Result>();
            var itemsPerTask = (int)Math.Ceiling(m_ItemList.Count / (float)count);

            for (var i = 0; i < count; i++)
            {
                var i1 = i;
                localResults[i1] = new Result();
                tasks[i] = Task.Run(() =>
                {
                    var result = localResults[i1];
                    for (var j = 0; j < itemsPerTask; j++)
                    {
                        var index = j + itemsPerTask * i1;
                        if (index >= m_ItemList.Count)
                            break;
                        var item = m_ItemList[index];
                        float score = 0;
                        if (query.Length == 0 || Match(query, tokenizedQuery, item, out score))
                        {
                            if (score > result.maxScore)
                            {
                                result.maxScore = score;
                                result.item = item;
                            }

                            queue.Enqueue(new Result { item = item, maxScore = score });
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            for (var i = 0; i < count; i++)
            {
                if (localResults[i].maxScore > max.maxScore)
                {
                    max.maxScore = localResults[i].maxScore;
                    max.item = localResults[i].item;
                }
            }

            PostprocessResults(queue, finalResults, max);
        }

        void PostprocessResults(IEnumerable<Result> results, ICollection<SearcherItem> items, Result max)
        {
            foreach (var result in results)
            {
                var normalizedScore = result.maxScore / max.maxScore;
                if (result.item != null && result.item != max.item && normalizedScore > k_ScoreCutOff)
                {
                    items.Add(result.item);
                }
            }
        }

        public override void BuildIndex()
        {
            m_Index.Clear();

            foreach (var item in m_ItemList)
            {
                if (!m_Index.ContainsKey(item.Path))
                {
                    List<ValueTuple<string, float>> terms  = new List<ValueTuple<string, float>>();

                    // If the item uses synonyms to return results for similar words/phrases, add them to the search terms
                    IList<string> tokens = null;
                    if (item.Synonyms == null)
                        tokens = Tokenize(item.Name);
                    else
                        tokens = Tokenize(string.Format("{0} {1}", item.Name, string.Join(" ", item.Synonyms)));
                        
                    string tokenSuite = "";
                    foreach (var token in tokens)
                    {
                        var t = token.ToLower();
                        if (t.Length > 1)
                        {
                            terms.Add(new ValueTuple<string, float>(t, 0.8f));
                        }

                        if (tokenSuite.Length > 0)
                        {
                            tokenSuite += " " + t;
                            terms.Add(new ValueTuple<string, float>(tokenSuite, 1f));
                        }
                        else
                        {
                            tokenSuite = t;
                        }
                    }

                    // Add a term containing all the uppercase letters (CamelCase World BBox => CCWBB)
                    var initialList = Regex.Split(item.Name, @"\P{Lu}+");
                    var initials = string.Concat(initialList).Trim();
                    if (!string.IsNullOrEmpty(initials))
                        terms.Add(new ValueTuple<string, float>(initials.ToLower(), 0.5f));

                    m_Index.Add(item.Path, terms);
                }
            }
        }

        static IList<string> Tokenize(string s)
        {
            var knownTokens = new HashSet<string>();
            var tokens = new List<string>();

            // Split on word boundaries
            foreach (var t in Regex.Split(s, @"\W"))
            {
                // Split camel case words
                var tt = Regex.Split(t, @"(\p{Lu}+\P{Lu}*)");
                foreach (var ttt in tt)
                {
                    var tttt = ttt.Trim();
                    if (!string.IsNullOrEmpty(tttt) && !knownTokens.Contains(tttt))
                    {
                        knownTokens.Add(tttt);
                        tokens.Add(tttt);
                    }
                }
            }

            return tokens;
        }

        bool Match(IReadOnlyList<string> tokenizedQuery, string itemPath, out float score)
        {
            itemPath = itemPath.Trim();
            if (itemPath == "")
            {
                if (tokenizedQuery.Count == 0)
                {
                    score = 1;
                    return true;
                }
                else
                {
                    score = 0;
                    return false;
                }
            }

            IReadOnlyList<ValueTuple<string, float>> indexTerms;
            if (!m_Index.TryGetValue(itemPath, out indexTerms))
            {
                score = 0;
                return false;
            }

            float maxScore = 0.0f;
            foreach (var t in indexTerms)
            {
                float scoreForTerm = 0f;
                var querySuite = "";
                var querySuiteFactor = 1.25f;
                foreach (var q in tokenizedQuery)
                {
                    if (t.Item1.StartsWith(q))
                    {
                        scoreForTerm += t.Item2 * q.Length / t.Item1.Length;
                    }

                    if (querySuite.Length > 0)
                    {
                        querySuite += " " + q;
                        if (t.Item1.StartsWith(querySuite))
                        {
                            scoreForTerm += t.Item2 * querySuiteFactor * querySuite.Length / t.Item1.Length;
                        }
                    }
                    else
                    {
                        querySuite = q;
                    }

                    querySuiteFactor *= querySuiteFactor;
                }

                maxScore = Mathf.Max(maxScore, scoreForTerm);
            }

            score = maxScore;
            return score > 0;
        }
    }
}
