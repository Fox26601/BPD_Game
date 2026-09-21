using BPD.Run;

namespace BPD.Progression
{
    /// <summary>
    /// Educational labels for the nine BPD symptom axes (never used to diagnose the player).
    /// </summary>
    public static class SymptomCatalog
    {
        public static string DisplayNameKey(BpdSymptom symptom) => symptom switch
        {
            BpdSymptom.FearOfAbandonment => "symptom.fear_abandonment.name",
            BpdSymptom.UnstableRelationships => "symptom.unstable_relationships.name",
            BpdSymptom.UnstableSelfImage => "symptom.unstable_self.name",
            BpdSymptom.Impulsivity => "symptom.impulsivity.name",
            BpdSymptom.SelfHarmSuicidality => "symptom.self_harm.name",
            BpdSymptom.AffectiveInstability => "symptom.affective.name",
            BpdSymptom.ChronicEmptiness => "symptom.emptiness.name",
            BpdSymptom.IntenseAnger => "symptom.anger.name",
            BpdSymptom.ParanoiaDissociation => "symptom.paranoia.name",
            _ => "symptom.impulsivity.name"
        };

        public static string EduKey(BpdSymptom symptom) => symptom switch
        {
            BpdSymptom.FearOfAbandonment => "symptom.fear_abandonment.edu",
            BpdSymptom.UnstableRelationships => "symptom.unstable_relationships.edu",
            BpdSymptom.UnstableSelfImage => "symptom.unstable_self.edu",
            BpdSymptom.Impulsivity => "symptom.impulsivity.edu",
            BpdSymptom.SelfHarmSuicidality => "symptom.self_harm.edu",
            BpdSymptom.AffectiveInstability => "symptom.affective.edu",
            BpdSymptom.ChronicEmptiness => "symptom.emptiness.edu",
            BpdSymptom.IntenseAnger => "symptom.anger.edu",
            BpdSymptom.ParanoiaDissociation => "symptom.paranoia.edu",
            _ => "symptom.impulsivity.edu"
        };

        public static string ModifierKey(BpdSymptom symptom) => symptom switch
        {
            BpdSymptom.FearOfAbandonment => "symptom.fear_abandonment.mod",
            BpdSymptom.UnstableRelationships => "symptom.unstable_relationships.mod",
            BpdSymptom.UnstableSelfImage => "symptom.unstable_self.mod",
            BpdSymptom.Impulsivity => "symptom.impulsivity.mod",
            BpdSymptom.SelfHarmSuicidality => "symptom.self_harm.mod",
            BpdSymptom.AffectiveInstability => "symptom.affective.mod",
            BpdSymptom.ChronicEmptiness => "symptom.emptiness.mod",
            BpdSymptom.IntenseAnger => "symptom.anger.mod",
            BpdSymptom.ParanoiaDissociation => "symptom.paranoia.mod",
            _ => "symptom.impulsivity.mod"
        };
    }
}
