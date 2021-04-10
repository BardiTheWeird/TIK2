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
    public enum OperationType
    {
        Encode,
        Decode,
        CountEntropy,
    }

    public class OperationEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public OperationType OperationType { get; set; }
        public string VisibleName { get; set; }
        public Action<object> Execute { get; set; }
        public Func<object, bool> CanExecute { get; set; }

        public OperationEntry(OperationType operationType, string visibleName, 
            Action<object> execute, Func<object, bool> canExecute = null)
        {
            OperationType = operationType;
            VisibleName = visibleName;
            Execute = execute;
            CanExecute = canExecute ?? (x => true);
        }
    }

    class MainWindowVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        string _filepathIn;
        public string FilepathIn 
        { 
            get => _filepathIn;
            set 
            {
                if (value == _filepathIn)
                    return;
                _filepathIn = value;
                RaisePropertyChanged();
                SetFilepathOut();
            }
        }

        public string FilepathOut { get; set; }

        public string OutputText { get; set; }

        public ObservableCollection<OperationEntry> OperationsChoice { get; set; }

        OperationEntry _chosenOperation;
        public OperationEntry ChosenOperation 
        { 
            get => _chosenOperation;
            set
            {
                if (value == _chosenOperation)
                    return;
                _chosenOperation = value;
                RaisePropertyChanged();

                SetFilepathOut();
            } 
        }

        public ICommand ChooseFile { get; set; }
        public ICommand Cancel { get; set; }
        public ICommand Execute { get; set; }

        private Action _cancelAction { get; set; }

        public TIK2.Encoder Encoder { get; set; } = new TIK2.Encoder();
        public TIK2.Decoder Decoder { get; set; } = new TIK2.Decoder();
        public EntropyCounter.EntropyCounter EntropyCounter { get; set; } = new EntropyCounter.EntropyCounter();

        bool CanCreateFile(string filepath)
        {
            FileStream fs = null;
            filepath += Guid.NewGuid().ToString() + ".temp";
            try
            {
                using (fs = new FileStream(filepath, FileMode.Create)) { }
                File.Delete(filepath);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
        }

        void SetFilepathOut()
        {
            if (ChosenOperation == null || string.IsNullOrEmpty(FilepathIn))
                return;

            var directory = Path.GetDirectoryName(FilepathIn);
            var name = Path.GetFileName(FilepathIn);

            switch (ChosenOperation.OperationType)
            {
                case OperationType.CountEntropy:
                    FilepathOut = string.Empty;
                    break;
                case OperationType.Encode:
                    FilepathOut = Path.Combine(directory, name + ".encoded");
                    break;
                case OperationType.Decode:
                    if (!name.Contains('.'))
                    {
                        FilepathOut = Path.Combine(directory, name + " - decoded");
                        return;
                    }
                    
                    var nameNoExtension = Path.GetFileNameWithoutExtension(name);
                    var extension = Path.GetExtension(name);
                    if (extension != ".encoded")
                    {
                        FilepathOut = Path.Combine(directory, nameNoExtension + " - decoded" + extension);
                        return;
                    }

                    if (!name.Contains('.'))
                    {
                        FilepathOut = Path.Combine(directory, nameNoExtension + " - decoded");
                        return;
                    }

                    extension = Path.GetExtension(nameNoExtension);
                    nameNoExtension = Path.GetFileNameWithoutExtension(nameNoExtension);
                    FilepathOut = Path.Combine(directory, nameNoExtension + " - decoded" + extension);
                    break;
            }
        }

        public MainWindowVM()
        {
            //Encoder = new TIK2.Encoder();
            OperationsChoice = new ObservableCollection<OperationEntry>()
            {
                new OperationEntry(OperationType.Encode, "Encode", x =>
                {
                    var bgWorker = new BackgroundWorker();

                    var tokenSource = new CancellationTokenSource();
                    bgWorker.DoWork += (sender, e) => e.Result = Encoder.Encode(FilepathIn, FilepathOut, tokenSource.Token);
                    bgWorker.RunWorkerCompleted += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty((string)e.Result))
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
                        if (File.Exists(FilepathOut))
                            File.Delete(FilepathOut);
                    };
                }, 
                    x => File.Exists(FilepathIn) && CanCreateFile(FilepathOut)),
                new OperationEntry(OperationType.Decode, "Decode", x => 
                {
                    var bgWorker = new BackgroundWorker();

                    var tokenSource = new CancellationTokenSource();
                    bgWorker.DoWork += (sender, e) => e.Result = Decoder.Decode(FilepathIn, FilepathOut, tokenSource.Token);
                    bgWorker.RunWorkerCompleted += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty((string)e.Result))
                            OutputText += e.Result + "\n";
                        _cancelAction = null;
                    };

                    bgWorker.RunWorkerAsync();

                    _cancelAction = async () =>
                    {
                        tokenSource.Cancel();
                        _cancelAction = null;
                        Decoder.Log = "";
                        OutputText += "Decoding was canceled\n";

                        await Task.Delay(1000);
                        if (File.Exists(FilepathOut))
                            File.Delete(FilepathOut);
                    };
                }, 
                    x => File.Exists(FilepathIn) && CanCreateFile(FilepathOut)),
                new OperationEntry(OperationType.CountEntropy, "Calculate entropy", x =>
                {
                    var bgWorker = new BackgroundWorker();
                    var tokenSource = new CancellationTokenSource();

                    bgWorker.DoWork += (sender, e) =>
                        e.Result = (EntropyCounter.FromFilePath(FilepathIn, tokenSource.Token, out var msElapsed), msElapsed);
                    bgWorker.RunWorkerCompleted += (sender, e) =>
                    {
                        var res = ((double?, long))e.Result;
                        if (res.Item1 != null)
                            OutputText += $"{Path.GetFileName(FilepathIn)} entropy is {res.Item1:.00000}. " +
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
                }, 
                    x => File.Exists(FilepathIn)),
            };

            ChooseFile = new RelayCommand(x =>
            {
                var openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = false;
                if (openFileDialog.ShowDialog() == true)
                    FilepathIn = openFileDialog.FileName;
            });
            Execute = new RelayCommand(x => ChosenOperation.Execute(x), 
                x => ChosenOperation != null && ChosenOperation.CanExecute(x));
            Cancel = new RelayCommand(x => _cancelAction(), x => _cancelAction != null);
        }
    }

    public class LogValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
            string.Join("", values.Select(v => v as string)
                .Where(s => !string.IsNullOrEmpty(s)));

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
