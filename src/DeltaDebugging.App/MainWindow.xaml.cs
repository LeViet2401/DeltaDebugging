using DeltaDebugging.App.Helper;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DeltaDebugging.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<object> testResults = new List<object>();
        List<Change> _resolved = new List<Change>();
        private string projectPath = @$"{AppDomain.CurrentDomain.BaseDirectory}\SampleProject\SampleProject.csproj";
        private string sourceFilePath = @$"{AppDomain.CurrentDomain.BaseDirectory}\SampleProject\Program.cs";
        private string baselineCode;

        public MainWindow()
        {
            InitializeComponent();
            ResultsPanel.ItemsSource = testResults;
            PassingComponents.ItemsSource = _resolved;
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            RunButton.IsEnabled = false;
            testResults.Clear();
            _resolved.Clear();
            ResultsPanel.Items.Refresh();
            PassingComponents.Items.Refresh();
            await Task.Run(() => RunDeltaDebugging());
            RunButton.IsEnabled = true;
        }

        private void DisplayTestResult(List<Change> changes, string result)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                testResults.Add(new TestResultControl(changes, result));
                ResultsPanel.Items.Refresh();
            });
        }

        private void DisplayResolvedResult(List<Change> resolved)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var newResolved = resolved.Except(_resolved).ToList();
                _resolved.AddRange(newResolved);
                PassingComponents.Items.Refresh();
            });
        }

        private void RunDeltaDebugging()
        {
            baselineCode = File.ReadAllText(sourceFilePath);

            List<Change> changes = new List<Change>
            {
                new Change(28, "", "int a = 0;"),
                new Change(29, "", "int b = 0;"),
                new Change(30, "", "int c = 0;"),
                new Change(31, "", "a++;"),
                new Change(32, "", "b++;"),
                new Change(33, "", "c++;"),
                new Change(34, "", "a--;"),
                new Change(35, "", "b--;"),
                new Change(36, "", "c--;"),
                new Change(37, "if (arr[mid] == target)", "if (arr[mid] != target)")
            };

            var minimalSet = DeltaDebugging(changes);

            App.Current.Dispatcher.Invoke(() => LastResult.ItemsSource = minimalSet);

            File.WriteAllText(sourceFilePath, baselineCode);
        }

        public List<Change> DeltaDebugging(List<Change> changes)
        {
            return DdPlus(changes, new List<Change>(), 2);
        }

        private List<Change> DdPlus(List<Change> changes, List<Change> resolved, int n)
        {
            if (changes.Count == 1)
            {
                return Test(changes.Union(resolved).ToList()) == TestResult.Fail ? changes : new List<Change>();
            }

            var subsets = SplitIntoSubsets(changes, n);
            for (int i = 0; i < subsets.Count; i++)
            {
                var subset = subsets[i];
                var complement = changes.Except(subset).Union(resolved).ToList();

                var subsetResult = Test(subset.Union(resolved).ToList());
                DisplayTestResult(subset, subsetResult.ToString());
                var complementResult = Test(complement);
                if (subsetResult == TestResult.Fail)
                {
                    return DdPlus(subset, resolved, 2);
                }
                if (subsetResult == TestResult.Pass && complementResult == TestResult.Pass)
                {
                    var result1 = DdPlus(subset, complement, 2);
                    var result2 = DdPlus(complement.Except(subset).ToList(), subset, 2);
                    return result1.Union(result2).ToList();
                }
                if (subsetResult == TestResult.Unresolved && complementResult == TestResult.Pass)
                {
                    return DdPlus(subset, complement, 2);
                }

            }

            if (n < changes.Count)
            {

                var failingComplements = subsets.Where(s => Test(changes.Except(s).Union(resolved).ToList()) == TestResult.Fail).ToList();
                var newChanges = failingComplements.Any()
                    ? changes.Intersect(failingComplements.SelectMany(s => changes.Except(s)).Distinct()).ToList()
                    : changes;


                var passingSubsets = subsets.Where(s => Test(s.Union(resolved).ToList()) == TestResult.Pass).ToList();
                var newResolved = resolved.Union(passingSubsets.SelectMany(s => s).Distinct()).ToList();


                var newN = Math.Min(newChanges.Count, 2 * n);
                return DdPlus(newChanges, newResolved, newN);
            }

            File.WriteAllText(sourceFilePath, baselineCode);
            return changes;
        }

        static List<List<Change>> SplitIntoSubsets(List<Change> changes, int n)
        {
            var subsets = new List<List<Change>>();
            int subsetSize = (int)Math.Ceiling((double)changes.Count / n);
            for (int i = 0; i < n; i++)
            {
                var subset = changes.Skip(i * subsetSize).Take(subsetSize).ToList();
                if (subset.Count > 0) subsets.Add(subset);
            }
            return subsets;
        }


        private TestResult Test(List<Change> configuration)
        {

            string[] lines = baselineCode.Split('\n');
            foreach (var change in configuration)
            {
                lines[change.LineNumber - 1] = change.NewLine;
            }
            foreach (var change in _resolved)
            {
                lines[change.LineNumber - 1] = change.NewLine;
            }
            string modifiedCode = string.Join("\n", lines);
            File.WriteAllText(sourceFilePath, modifiedCode);

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build {projectPath} -c Debug",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                process.WaitForExit();
                if (process.ExitCode != 0) // Biên dịch thất bại
                {
                    return TestResult.Unresolved;
                }
            }

            // Chạy chương trình
            psi = new ProcessStartInfo
            {
                FileName = @$"{AppDomain.CurrentDomain.BaseDirectory}\SampleProject\bin\Debug\net8.0\SampleProject.exe",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        return TestResult.Fail;
                    }

                    if (output.Contains("Found 7 at 3"))
                    {
                        DisplayResolvedResult(configuration);
                        return TestResult.Pass;
                    }
                    return TestResult.Fail;
                }
            }
            catch
            {
                return TestResult.Unresolved;
            }
        }

    }
}