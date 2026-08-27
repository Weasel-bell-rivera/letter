using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class GroundFireThrowerTestRunner
{
    private static TestRunnerApi activeApi;
    private static Callbacks activeCallbacks;

    [MenuItem("Tools/W1/Run Ground Fire Thrower EditMode Tests")]
    public static void RunEditMode()
        => Run(TestMode.EditMode, "EditMode", "GroundFireThrowerEnemyAssetTests");

    [MenuItem("Tools/W1/Run EARTH-001 Fire Thrower PlayMode Tests")]
    public static void RunPlayMode()
        => Run(TestMode.PlayMode, "PlayMode", "GroundFireThrowerEnemyPlayModeTests");

    private static void Run(TestMode mode, string label, string fixture)
    {
        activeApi = ScriptableObject.CreateInstance<TestRunnerApi>();
        activeCallbacks = new Callbacks(label);
        activeApi.RegisterCallbacks(activeCallbacks);
        activeApi.Execute(new ExecutionSettings(new Filter
        {
            testMode = mode,
            testNames = new[] { fixture }
        }));
    }

    private sealed class Callbacks : ICallbacks
    {
        private readonly string label;

        public Callbacks(string runLabel) => label = runLabel;

        public void RunStarted(ITestAdaptor testsToRun)
            => Debug.Log($"[GroundFireThrowerTests:{label}] STARTED {testsToRun.TestCaseCount} tests.");

        public void RunFinished(ITestResultAdaptor result)
        {
            Debug.Log($"[GroundFireThrowerTests:{label}] FINISHED pass={result.PassCount} " +
                      $"fail={result.FailCount} skip={result.SkipCount} " +
                      $"inconclusive={result.InconclusiveCount}.");
            activeApi = null;
            activeCallbacks = null;
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.FailCount > 0 && !result.HasChildren)
                Debug.LogError($"[GroundFireThrowerTests:{label}] FAILED {result.FullName}: " +
                               $"{result.Message}\n{result.StackTrace}");
        }
    }
}
