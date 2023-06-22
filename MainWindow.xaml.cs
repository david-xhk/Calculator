using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Calculator
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private StringBuilder expression;
        private string historyFilePath;
        private ObservableCollection<string> history;

        public MainWindow()
        {
            InitializeComponent();

            AppWindow.Resize(new SizeInt32(600, 600));
            AppWindow.Title = "Calculator";

            expression = new StringBuilder();
            historyFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "CalculatorHistory.txt");
            history = new ObservableCollection<string>();
            LoadHistoryFromFile();
        }

        private void LoadHistoryFromFile()
        {
            if (!File.Exists(historyFilePath))
            {
                return;
            }
            var historyEntries = File.ReadAllLines(historyFilePath);
            foreach (var entry in historyEntries)
            {
                history.Add(entry);
            }
        }

        private void SaveHistoryToFile()
        {
            File.WriteAllLines(historyFilePath, history);
        }
        
        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            ClearHistory();
        }

        private void ClearHistory()
        {
            history.Clear();
            File.Delete(historyFilePath);
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            CopyExpression();
        }

        private void CopyExpression()
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetText(ExpressionTextBlock.Text);
            Clipboard.SetContent(dataPackage);
        }

        private void HistoryListBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (HistoryListBox.SelectedValue == null)
            {
                return;
            }

            HistoryListBox.Focus(FocusState.Programmatic);
            HistoryListBox.ScrollIntoView(HistoryListBox.SelectedItem);

            var selectedValue = HistoryListBox.SelectedValue.ToString().TrimEnd('\r', '\n');
            NewExpression(selectedValue);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var input = button.Content.ToString();
            HandleInput(input);
        }

        private void HandleInput(string input)
        {
            ExpressionTextBlock.Focus(FocusState.Programmatic);

            if (expression.ToString().StartsWith("Error: "))
            {
                expression.Clear();
            }
            if (IsNumeric(input))
            {
                AppendToExpression(input);
            }
            else if (IsOperator(input))
            {
                AppendToExpression($" {input} ");
            }
            else if (input == "=")
            {
                EvaluateExpression();
            }
        }

        private void ClearEntry_Click(object sender, RoutedEventArgs e)
        {
            ClearEntry();
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            ClearAll();
        }

        private void ClearEntry()
        {
            if (expression.Length == 0)
            {
                return;
            }
            var lastSpaceIndex = expression.ToString().LastIndexOf(' ');

            if (expression.ToString().StartsWith("Error: ") || lastSpaceIndex == -1)
            {
                expression.Clear();
            }
            else if (lastSpaceIndex == expression.Length - 1)
            {
                expression.Remove(expression.Length - 3, 3);
            }
            else
            {
                expression.Remove(lastSpaceIndex + 1, expression.Length - lastSpaceIndex - 1);
            }
            ExpressionTextBlock.Text = expression.ToString();
        }

        private void ClearAll()
        {
            expression.Clear();
            ExpressionTextBlock.Text = expression.ToString();
        }

        private void AddToHistory(string expression)
        {
            history.Add(expression);
            HistoryListBox.ScrollIntoView(HistoryListBox.Items[history.Count - 1]);
            HistoryListBox.SelectedIndex = -1;
        }

        private void EvaluateExpression()
        {
            string expression = this.expression.ToString();
            if (expression.Length > 0)
            {
                AddToHistory(expression);
            }
            try
            {
                var dataTable = new System.Data.DataTable();
                double result = Convert.ToDouble(dataTable.Compute(expression, ""));
                if (Double.IsInfinity(result))
                {
                    throw new Exception("division by zero");
                }
                if (Double.IsNaN(result))
                {
                    throw new Exception("undefined");
                }
                NewExpression(result.ToString());
            }
            catch (Exception ex)
            {
                NewExpression($"Error: {ex.Message}");
            }
        }

        private bool IsNumeric(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            if (input.Length == 1 && input[0] == '.')
            {
                return true;
            }

            return double.TryParse(input, out _);
        }

        private bool IsOperator(string input)
        {
            return input == "+" || input == "-" || input == "*" || input == "/" || input == "%";
        }

        private void AppendToExpression(string input)
        {
            expression.Append(input);
            ExpressionTextBlock.Text = expression.ToString();
            ScrollViewer scv = ExpressionTextBlock.Parent as ScrollViewer;
            scv.UpdateLayout();
            scv.ScrollToHorizontalOffset(scv.ScrollableWidth);
        }

        private void NewExpression(string input)
        {
            expression.Clear();
            AppendToExpression(input);
        }

        private void StackPanel_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (CtrlPressed())
            {
                switch (e.Key)
                {
                    case VirtualKey.C:
                        if (expression.Length > 0)
                        {
                            CopyExpression();
                        }
                        break;
                    case VirtualKey.X:
                        if (expression.Length > 0)
                        {
                            CopyExpression();
                            ClearAll();
                        }
                        break;
                    default:
                        return;
                }
            }
            else if (ShiftPressed())
            {
                switch (e.Key)
                {
                    case (VirtualKey)187: // '=' key
                        HandleInput("+");
                        break;
                    case VirtualKey.Number5:
                        HandleInput("%");
                        break;
                    case VirtualKey.Number8:
                        HandleInput("*");
                        break;
                    default:
                        return;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case VirtualKey.Up:
                        if (history.Count > 0)
                        {
                            HistoryListBox.SelectedIndex = ((HistoryListBox.SelectedIndex <= 0 ? history.Count : HistoryListBox.SelectedIndex) - 1) % history.Count;
                        }
                        break;
                    case VirtualKey.Down:
                        if (history.Count > 0)
                        {
                            HistoryListBox.SelectedIndex = (HistoryListBox.SelectedIndex + 1) % history.Count;
                        }
                        break;
                    case VirtualKey.Enter:
                        EvaluateExpression();
                        break;
                    case VirtualKey.Back:
                        ClearEntry();
                        break;
                    case VirtualKey.Escape:
                        ClearAll();
                        break;
                    case VirtualKey.Add:
                        HandleInput("+");
                        break;
                    case VirtualKey.Multiply:
                        HandleInput("*");
                        break;
                    case (VirtualKey)187: // '=' key
                        EvaluateExpression();
                        break;
                    case (VirtualKey)189: // '-' key
                    case VirtualKey.Subtract:
                        HandleInput("-");
                        break;
                    case (VirtualKey)190: // '.' key
                        HandleInput(".");
                        break;
                    case (VirtualKey)191: // '/' key
                    case VirtualKey.Divide:
                        HandleInput("/");
                        break;

                    case VirtualKey.Number0:
                    case VirtualKey.NumberPad0:
                        HandleInput("0");
                        break;
                    case VirtualKey.Number1:
                    case VirtualKey.NumberPad1:
                        HandleInput("1");
                        break;
                    case VirtualKey.Number2:
                    case VirtualKey.NumberPad2:
                        HandleInput("2");
                        break;
                    case VirtualKey.Number3:
                    case VirtualKey.NumberPad3:
                        HandleInput("3");
                        break;
                    case VirtualKey.Number4:
                    case VirtualKey.NumberPad4:
                        HandleInput("4");
                        break;
                    case VirtualKey.Number5:
                    case VirtualKey.NumberPad5:
                        HandleInput("5");
                        break;
                    case VirtualKey.Number6:
                    case VirtualKey.NumberPad6:
                        HandleInput("6");
                        break;
                    case VirtualKey.Number7:
                    case VirtualKey.NumberPad7:
                        HandleInput("7");
                        break;
                    case VirtualKey.Number8:
                    case VirtualKey.NumberPad8:
                        HandleInput("8");
                        break;
                    case VirtualKey.Number9:
                    case VirtualKey.NumberPad9:
                        HandleInput("9");
                        break;
                    default:
                        return;
                }
            }
            e.Handled = true;
        }

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int virtualKey);

        private bool CtrlPressed()
        {
            return (GetKeyState((int)VirtualKey.Control) & 0x8000) != 0;
        }

        private bool ShiftPressed()
        {
            return (GetKeyState((int)VirtualKey.Shift) & 0x8000) != 0;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            SaveHistoryToFile();
        }
    }
}
