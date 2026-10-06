using System;
using System.Collections.Generic;

namespace Archive0317
{
    [Serializable] public sealed class EvidenceDefinition
    {
        public string id;
        public string title;
        public string[] requirements;
    }
    public static class EvidenceCollection
    {
        public static List<EvidenceDefinition> Collect(CaseDefinition definition)
        {
            var collected=new List<EvidenceDefinition>();if(definition==null)return collected;
            var progress=CaseProgressStore.Get(definition);
            foreach(var evidence in definition.Evidence??Array.Empty<EvidenceDefinition>())
            {
                if(evidence==null || string.IsNullOrEmpty(evidence.id))continue;
                bool found=true;foreach(var flag in evidence.requirements??Array.Empty<string>())if(!progress.Has(flag))found=false;
                if(found)CaseProgressStore.AddEvidence(definition,evidence.id);
                if(progress.evidenceIds.Contains(evidence.id))collected.Add(evidence);
            }
            return collected;
        }
    }
}
