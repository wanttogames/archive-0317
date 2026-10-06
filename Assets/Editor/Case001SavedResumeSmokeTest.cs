using System;
using System.IO;
using System.Linq;
using System.Text;
using Archive0317;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Case001SavedResumeSmokeTest
{
    private const string Backup="Temp/Case001ResumeOriginalSave.txt";
    private static CaseDefinition Data=>AssetDatabase.LoadAssetAtPath<CaseDefinition>(Case001MotelBuilder.DefinitionPath);
    [MenuItem("Archive 03:17/Prepare Saved CASE 001 Resume Check")]
    public static void Prepare()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
        var key=CaseProgressStore.StorageKey(Data);
        if(File.Exists(Backup))throw new InvalidOperationException("Restore the pending original save first.");
        var json=File.ReadAllText("Temp/Case001CompletedTestSave.json");var completed=JsonUtility.FromJson<CaseProgress>(json);
        if(!completed.Has("Case001Completed"))throw new InvalidOperationException("Run the full CASE 001 smoke test first.");
        File.WriteAllText(Backup,(PlayerPrefs.HasKey(key)?"1":"0")+PlayerPrefs.GetString(key));
        // Model quitting immediately after the verdict, before the delayed file was observed.
        completed.flags.Remove("Case00Activated");PlayerPrefs.SetString(key,JsonUtility.ToJson(completed));PlayerPrefs.Save();CaseProgressStore.ClearCache();
    }
    [MenuItem("Archive 03:17/Validate Saved CASE 001 Resume")]
    public static string Validate()
    {
        if(!EditorApplication.isPlaying || SceneManager.GetActiveScene().name!="ArchiveRoom")throw new InvalidOperationException("Start a fresh ArchiveRoom Play Mode session after Prepare.");
        var report=new StringBuilder("CASE 001 separate Play Mode session save check\n");var progress=CaseProgressStore.Get(Data);var expected=JsonUtility.FromJson<CaseProgress>(File.ReadAllText("Temp/Case001CompletedTestSave.json"));
        Check(progress.Has("Case001Completed") && progress.Has("CaseCompleted"),"Completed flags survive a real Editor stop/start",report);
        Check(progress.verdict==expected.verdict && progress.Fact("Case001Verdict")==expected.verdict,"Verdict loaded from persisted JSON",report);
        Check(progress.evidenceIds.Count==expected.evidenceIds.Count && expected.evidenceIds.All(id=>progress.evidenceIds.Contains(id)),"Actually acquired evidence IDs retained",report);
        Check(progress.Has("Case00Activated") && GameObject.Find("CASE 00")!=null,"CASE 00 restored even if quitting before initial appearance",report);
        var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();player.enabled=false;var file=UnityEngine.Object.FindFirstObjectByType<CaseFile>();var controller=player.GetComponent<CharacterController>();controller.enabled=false;player.transform.position=new Vector3(.45f,.05f,-.55f);controller.enabled=true;player.ViewCamera.transform.LookAt(file.transform.position);Physics.SyncTransforms();
        Check(player.TryInteract() && player.HUD.IsCaseOpen,"Resumed completed file reachable via Raycast",report);
        Check(player.HUD.GetComponentsInChildren<Text>(true).First(t=>t.name=="Description").text.Contains(CaseReport.VerdictLabel(expected.verdict)),"Read-only report shows the saved classification",report);
        Check(!player.HUD.GetComponentsInChildren<Button>(true).First(t=>t.name=="StartField").gameObject.activeInHierarchy && !SceneTransitionManager.Begin(Data),"Completed save does not restart the motel",report);
        report.AppendLine("PASS");File.WriteAllText("Documentation/Verification/Case001ResumeTest.txt",report.ToString());Debug.Log(report.ToString());return "PASS";
    }
    [MenuItem("Archive 03:17/Restore Original CASE 001 Save")]
    public static void Restore()
    {
        if(!File.Exists(Backup))return;var text=File.ReadAllText(Backup);var key=CaseProgressStore.StorageKey(Data);if(text[0]=='1')PlayerPrefs.SetString(key,text.Substring(1));else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();CaseProgressStore.ClearCache();File.Delete(Backup);
    }
    private static void Check(bool condition,string label,StringBuilder report){if(!condition)throw new InvalidOperationException(label);report.AppendLine("PASS: "+label);}
}
