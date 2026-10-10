using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace BeatFactory.EditorTools.Tests
{
    /// <summary>
    /// Menu: Tools > Beat Factory > Run Tests — runs all EditMode tests and prints a summary to the Console
    /// (also written to Temp/bf_test_results.txt). Same tests as Window > General > Test Runner.
    /// </summary>
    public static class TestRunnerTools
    {
        public static string ResultsFile => Path.Combine(Path.GetTempPath(), "bf_test_results.txt");

        [MenuItem("Tools/Beat Factory/Run Tests")]
        public static void RunAll()
        {
            if (File.Exists(ResultsFile)) File.Delete(ResultsFile);
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Collector());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, assemblyNames = new[] { "Assembly-CSharp-Editor" } }));
        }

        class Collector : ICallbacks
        {
            readonly StringBuilder lines = new StringBuilder();

            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor r)
            {
                if (r.HasChildren) return;
                lines.AppendLine(r.TestStatus + "  " + r.Name + (r.TestStatus == TestStatus.Failed ? "  → " + r.Message : ""));
            }

            public void RunFinished(ITestResultAdaptor r)
            {
                string text = "Beat Factory tests: " + r.PassCount + " passed, " + r.FailCount + " failed, " + r.SkipCount + " skipped\n" + lines;
                File.WriteAllText(ResultsFile, text);
                if (r.FailCount > 0) Debug.LogError(text); else Debug.Log(text);
            }
        }
    }
}
