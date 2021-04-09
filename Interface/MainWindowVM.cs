using Microsoft.Win32;
using PropertyChanged;
using siof.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace Interface
{
    public class OperationEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string VisibleName { get; set; }
        public Action<object> Execute { get; set; }
        public Func<object, bool> CanExecute { get; set; }

        public OperationEntry(string visibleName, Action<object> execute, Func<object, bool> canExecute = null)
        {
            VisibleName = visibleName;
            Execute = execute;
            CanExecute = canExecute ?? (x => true);
        }
    }

    class MainWindowVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string InputText { get; set; }
        public string Filepath { get; set; }

        public string LogString => Encoder.Log;

        public string OutputText { get; set; }

        public ObservableCollection<OperationEntry> OperationsChoice { get; set; }

        public ICommand ChooseFile { get; set; }
        public ICommand Encode { get; set; }
        public ICommand Decode { get; set; }
        public ICommand CountEntropy { get; set; }
        public ICommand Cancel { get; set; }

        private Action _cancelAction { get; set; }

        public TIK2.Encoder Encoder { get; set; } = new TIK2.Encoder();
        public EntropyCounter.EntropyCounter EntropyCounter { get; set; } = new EntropyCounter.EntropyCounter();

        public MainWindowVM()
        {
            //Encoder = new TIK2.Encoder();
            OperationsChoice = new ObservableCollection<OperationEntry>()
            {
                new OperationEntry("Encode", x =>
                {
                    var outPath = Path.GetDirectoryName(Filepath) + $@"\{Path.GetFileName(Filepath)}.encoded";
                    var bgWorker = new BackgroundWorker();

                    var tokenSource = new CancellationTokenSource();
                    bgWorker.DoWork += (sender, e) => e.Result = Encoder.Encode(Filepath, outPath, tokenSource.Token);
                    bgWorker.RunWorkerCompleted += (sender, e) =>
                    {
                        if (((string)e.Result).Length > 0)
                            OutputText += e.Result + "\n";
                        _cancelAction = null;
                    };

                    bgWorker.RunWorkerAsync();

                    _cancelAction = async () =>
                    {
                        tokenSource.Cancel();
                        _cancelAction = null;
                        Encoder.Log = "";
                        OutputText += "Encoding was canceled\n";

                        await Task.Delay(1000);
                        File.Delete(outPath);
                    };

                }, x => File.Exists(Filepath)),
                new OperationEntry("Decode", x => { }, x => File.Exists(Filepath)),
                new OperationEntry("Calculate entropy", x =>
                {
                    var bgWorker = new BackgroundWorker();
                    var tokenSource = new CancellationTokenSource();

                    bgWorker.DoWork += (sender, e) =>
                        e.Result = (EntropyCounter.FromFilePath(Filepath, tokenSource.Token, out var msElapsed), msElapsed);
                    bgWorker.RunWorkerCompleted += (sender, e) =>
                    {
                        var res = ((double?, long))e.Result;
                        if (res.Item1 != null)
                            OutputText += $"{Path.GetFileName(Filepath)} entropy is {res.Item1}. " +
                            $"Time elapsed: {res.Item2/1000f:.00}s\n";
                        _cancelAction = null;
                    };

                    bgWorker.RunWorkerAsync();
                    _cancelAction = () =>
                    {
                        tokenSource.Cancel();
                        _cancelAction = null;
                        EntropyCounter.Log = "";
                        OutputText += "Counting entropy was canceled\n";
                    };
                }, x => File.Exists(Filepath)),
            };

            ChooseFile = new RelayCommand(x =>
            {
                var openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = false;
                if (openFileDialog.ShowDialog() == true)
                    Filepath = openFileDialog.FileName;
            });

            Encode = new RelayCommand(x =>
            {
                var outPath = Path.GetDirectoryName(Filepath) + $@"\{Path.GetFileName(Filepath)}.encoded";
                var bgWorker = new BackgroundWorker();

                var tokenSource = new CancellationTokenSource();
                bgWorker.DoWork += (sender, e) => e.Result = Encoder.Encode(Filepath, outPath, tokenSource.Token);
                bgWorker.RunWorkerCompleted += (sender, e) => 
                { 
                    if (((string)e.Result).Length > 0)
                        OutputText += e.Result + "\n";
                    _cancelAction = null;
                };

                bgWorker.RunWorkerAsync();

                _cancelAction = async () =>
                {
                    tokenSource.Cancel();
                    _cancelAction = null;
                    Encoder.Log = "";
                    OutputText += "Encoding was canceled\n";

                    await Task.Delay(1000);
                    File.Delete(outPath);
                };

            }, x => File.Exists(Filepath));

            Decode = new RelayCommand(x =>
            {
            }, x => File.Exists(Filepath));

            CountEntropy = new RelayCommand(x =>
            {
                var bgWorker = new BackgroundWorker();
                var tokenSource = new CancellationTokenSource();

                bgWorker.DoWork += (sender, e) => 
                    e.Result = (EntropyCounter.FromFilePath(Filepath, tokenSource.Token, out var msElapsed), msElapsed);
                bgWorker.RunWorkerCompleted += (sender, e) =>
                {
                    var res = ((double?, long))e.Result;
                    if (res.Item1 != null)
                        OutputText += $"{Path.GetFileName(Filepath)} entropy is {res.Item1}. " +
                        $"Time elapsed: {res.Item2/1000f:.00}s\n";
                    _cancelAction = null;
                };

                bgWorker.RunWorkerAsync();
                _cancelAction = () =>
                {
                    tokenSource.Cancel();
                    _cancelAction = null;
                    EntropyCounter.Log = "";
                    OutputText += "Counting entropy was canceled\n";
                };
            }, x => File.Exists(Filepath)); 

            Cancel = new RelayCommand(x => _cancelAction(), x => _cancelAction != null);
        }
    }

    public class LogValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
            values.Select(v => v as string)
                .Where(s => !string.IsNullOrEmpty(s))
                .FirstOrDefault();

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
