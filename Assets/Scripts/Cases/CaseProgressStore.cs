using System;
using System.Collections.Generic;
using UnityEngine;

namespace Archive0317
{
    [Serializable] public sealed class CaseFact { public string key; public string value; }
    [Serializable] public sealed class CaseProgress
    {
        public List<string> flags = new List<string>();
        public List<CaseFact> facts = new List<CaseFact>();
        public bool Has(string flag) => flags.Contains(flag);
        public string Fact(string key) => facts.Find(f => f.key == key)?.value;
    }
    // Small per-case JSON records; no scene names or case-specific rules live in this store.
    public static class CaseProgressStore
    {
        private static readonly Dictionary<string, CaseProgress> cache = new Dictionary<string, CaseProgress>();
        public static string StorageKey(CaseDefinition definition) => "Archive0317.Case." + definition.Id;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearCache() => cache.Clear();
        public static CaseProgress Get(CaseDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (cache.TryGetValue(definition.Id, out var progress)) return progress;
            try { progress = JsonUtility.FromJson<CaseProgress>(PlayerPrefs.GetString(StorageKey(definition), "")); }
            catch (ArgumentException) { progress = null; }
            if (progress == null || progress.flags == null || progress.facts == null) progress = new CaseProgress();
            cache[definition.Id] = progress;
            return progress;
        }
        public static void Mark(CaseDefinition definition, string flag)
        {
            if (definition == null || string.IsNullOrEmpty(flag)) return;
            var progress = Get(definition);
            if (!progress.flags.Contains(flag)) { progress.flags.Add(flag); Save(definition); }
        }
        public static void RecordFact(CaseDefinition definition, string key, string value)
        {
            if (definition == null || string.IsNullOrEmpty(key)) return;
            var progress = Get(definition);
            var fact = progress.facts.Find(f => f.key == key);
            if (fact == null) progress.facts.Add(new CaseFact { key = key, value = value });
            else fact.value = value;
            Save(definition);
        }
        public static bool CompareRooms(CaseDefinition definition)
        {
            if (definition == null) return false;
            var progress = Get(definition);
            foreach (var requirement in definition.ComparisonRequirements ?? Array.Empty<string>())
                if (!progress.Has(requirement)) return false;
            var official = progress.Fact("OfficialRoom");
            var ledger = progress.Fact("LedgerRoom");
            if (string.IsNullOrEmpty(official) || string.IsNullOrEmpty(ledger) || official == ledger) return false;
            Mark(definition, "RoomNumberMismatchFound");
            return true;
        }
        private static void Save(CaseDefinition definition)
        { PlayerPrefs.SetString(StorageKey(definition), JsonUtility.ToJson(Get(definition))); PlayerPrefs.Save(); }
    }
}
