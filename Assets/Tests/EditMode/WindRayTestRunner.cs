using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class WindRayTestRunner
{
    private static TestRunnerApi activeApi;
    private static WindRayTestCallbacks activeCallbacks;

    [MenuItem("Tools/W1/Run Wind Ray EditMode Tests")]
    public static void RunEditMode()
        => Run(TestMode.EditMode, "EditMode", "WindRayEnemyAssetTests");

    [MenuItem("Tools/W1/Run WIND-001 PlayMode Tests")]
    public static void RunPlayMode()
        => Run(TestMode.PlayMode, "PlayMode", "Wind001PlayModeTests");

    private static void Run(TestMode mode, string label, string fixture)
    {
        activeApi = ScriptableObject.CreateInstance<TestRunnerApi>();
        activeCallbacks = new WindRayTestCallbacks(label);
        activeApi.RegisterCallbacks(activeCallbacks);
        activeApi.Execute(new ExecutionSettings(new Filter
        {
            testMode = mode,
            testNames = new[] { fixture }
        }));
    }

    private sealed class WindRayTestCallbacks : ICallbacks
    {
        private readonly string label;

        public WindRayTestCallbacks(string runLabel) => label = runLabel;

        public void RunStarted(ITestAdaptor testsToRun)
            => Debug.Log($"[WindRayTests:{label}] STARTED {testsToRun.TestCaseCount} tests.");

        public void RunFinished(ITestResultAdaptor result)
        {
            Debug.Log($"[WindRayTests:{label}] FINISHED pass={result.PassCount} fail={result.FailCount} " +
                      $"skip={result.SkipCount} inconclusive={result.InconclusiveCount}.");
            activeApi = null;
            activeCallbacks = null;
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.FailCount > 0 && !result.HasChildren)
                Debug.LogError($"[WindRayTests:{label}] FAILED {result.FullName}: {result.Message}\n{result.StackTrace}");
        }
    }
}
