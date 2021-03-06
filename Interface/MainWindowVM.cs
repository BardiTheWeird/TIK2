using Microsoft.Win32;
using PropertyChanged;
using siof.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using TIK2;
using TIK2.CRC;

namespace Interface
{
    public enum OperationType
    {
        Encode,
        Decode,
        CountEntropy,
        InfuseErrors,
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

    public class EncodingEntry
    {
        public EncodingType EncodingType { get; set; }
        public string VisibleName { get; set; }
        public Func<bool> CanExecute { get; set; }

        public EncodingEntry(EncodingType encodingType, string visibleName, Func<bool> canExecute)
        {
            EncodingType = encodingType;
            VisibleName = visibleName;
            CanExecute = canExecute;
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

        public ObservableCollection<EncodingEntry> EncodingChoice { get; set; }
        EncodingEntry _chosenEncoding;
        public EncodingEntry ChosenEncoding
        {
            get => _chosenEncoding;
            set
            {
                if (value == _chosenEncoding)
                    return;
                _chosenEncoding = value;
                RaisePropertyChanged();

                SetFilepathOut();
            }
        }

        public ICommand ChooseFile { get; set; }
        public ICommand Cancel { get; set; }
        public ICommand Execute { get; set; }
        public ICommand OpenOutputDirectory { get; set; }
        public ICommand OutputToInput { get; set; }

        private Action _cancelAction { get; set; }
        public bool IsExecuting { get; set; }
        private void RaiseCanExecuteChanged()
        {
            (Cancel as RelayCommand)?.UpdateCanExecuteState();
            (Execute as RelayCommand)?.UpdateCanExecuteState();
        }

        public TIK2.Encoder Encoder { get; set; } = new TIK2.Encoder();
        public TIK2.Decoder Decoder { get; set; } = new TIK2.Decoder();
        public EntropyCounter.EntropyCounter EntropyCounter { get; set; } = new EntropyCounter.EntropyCounter();
        public HammingEncoder HammingEncoder { get; set; } = new HammingEncoder();
        public HammingDecoder HammingDecoder { get; set; } = new HammingDecoder();
        public ErrorInfuser ErrorInfuser { get; set; } = new ErrorInfuser();
        public CRCEncoder CRCEncoder { get; set; } = new CRCEncoder();
        public CRCDecoder CRCDecoder { get; set; } = new CRCDecoder();

        uint _blockSize;
        public uint BlockSize 
        { 
            get => _blockSize;
            set
            {
                if (value == _blockSize)
                    return;

                _blockSize = value;
                RaisePropertyChanged();
                SetFilepathOut();
            } 
        }

        int _errorCount;
        public int ErrorCount 
        { 
            get => _errorCount;
            set
            {
                if (value == _errorCount)
                    return;
                _errorCount = value;
                RaisePropertyChanged();
                SetFilepathOut();
            } 
        }

        public uint InitialErrorOffset { get; set; }


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
                    if (ChosenEncoding != null)
                    {
                        string extension_ = ".encoded";
                        if (ChosenEncoding.EncodingType == EncodingType.Hamming)
                            extension_ = $".hamming{BlockSize}";
                        else if (ChosenEncoding.EncodingType == EncodingType.CRC)
                            extension_ = ".crc";
                        FilepathOut = Path.Combine(directory, name + extension_);
                    }
                    break;
                case OperationType.Decode:
                    if (!name.Contains('.'))
                    {
                        FilepathOut = Path.Combine(directory, name + " - decoded");
                        return;
                    }

                    var nameNoExtension = Path.GetFileNameWithoutExtension(name);
                    var extension = Path.GetExtension(name);
                    if (extension != ".encoded" && !extension.Contains(".hamming") && !extension.Contains(".crc"))
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
                case OperationType.InfuseErrors:
                    var errorsString = $" [{ErrorCount} error{(ErrorCount > 1 ? "s" : "")} per {BlockSize} bit{(BlockSize > 1 ? "s" : "")}]";
                    extension = Path.GetExtension(FilepathIn);
                    nameNoExtension = Path.GetFileNameWithoutExtension(FilepathIn);

                    if (!name.Contains('.'))
                    {
                        FilepathOut = Path.Combine(directory, nameNoExtension + errorsString);
                        return;
                    }

                    if (!nameNoExtension.Contains('.'))
                    {
                        FilepathOut = Path.Combine(directory, nameNoExtension + errorsString + extension);
                        return;
                    }

                    var nameNoExtensionNoExtension = Path.GetFileNameWithoutExtension(nameNoExtension);
                    var secondExtension = Path.GetExtension(nameNoExtension);

                    FilepathOut = Path.Combine(directory, nameNoExtensionNoExtension + errorsString + secondExtension + extension);
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

                    if (ChosenEncoding.EncodingType == EncodingType.Hamming)
                    {
                        bgWorker.DoWork += (sender, e) => e.Result = HammingEncoder.Encode(FilepathIn, FilepathOut, 
                            (int)BlockSize, tokenSource.Token);
                    }
                    else if (ChosenEncoding.EncodingType == EncodingType.CRC)
                    {
                        bgWorker.DoWork += (sender, e) => e.Result = CRCEncoder.Encode(FilepathIn, FilepathOut,
                            BlockSize, tokenSource.Token);
                    }
                    else
                    {
                        bgWorker.DoWork += (sender, e) => e.Result = Encoder.Encode(FilepathIn, FilepathOut,
                            ChosenEncoding.EncodingType, tokenSource.Token);
                    }

                    bgWorker.RunWorkerCompleted += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty((string)e.Result))
                            OutputText += e.Result + "\n";
                        _cancelAction = null;
                        IsExecuting = false;
                        RaiseCanExecuteChanged();
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
                    x => File.Exists(FilepathIn) && CanCreateFile(FilepathOut) && ChosenEncoding != null && ChosenEncoding.CanExecute()),
                
                new OperationEntry(OperationType.Decode, "Decode", x => 
                {
                    var bgWorker = new BackgroundWorker();

                    var tokenSource = new CancellationTokenSource();

                    if (ChosenEncoding.EncodingType == EncodingType.Hamming)
                    {
                        bgWorker.DoWork += (sender, e) => e.Result = HammingDecoder.Decode(FilepathIn, FilepathOut, 
                            (int)BlockSize, tokenSource.Token);
                    }
                    else if (ChosenEncoding.EncodingType == EncodingType.CRC)
                    {
                        bgWorker.DoWork += (sender, e) => e.Result = CRCDecoder.Decode(FilepathIn, FilepathOut,
                            BlockSize, tokenSource.Token);
                    }
                    else
                    {
                        bgWorker.DoWork += (sender, e) => e.Result = Decoder.Decode(FilepathIn, FilepathOut, tokenSource.Token);
                    }
                    bgWorker.RunWorkerCompleted += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty((string)e.Result))
                            OutputText += e.Result + "\n";
                        _cancelAction = null;
                        IsExecuting = false;
                        RaiseCanExecuteChanged();
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
                    x => File.Exists(FilepathIn) && CanCreateFile(FilepathOut) && ChosenEncoding != null && ChosenEncoding.CanExecute()),
                
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
                        IsExecuting = false;
                        RaiseCanExecuteChanged();
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
                
                new OperationEntry(OperationType.InfuseErrors, "Infuse errors", x =>
                {
                    var bgWorker = new BackgroundWorker();
                    var tokenSource = new CancellationTokenSource();

                    bgWorker.DoWork += (sender, e) => e.Result = ErrorInfuser.InfuseErrorIntoFile(FilepathIn, FilepathOut,
                        (int)BlockSize, ErrorCount, InitialErrorOffset, tokenSource.Token);

                    bgWorker.RunWorkerCompleted += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty((string)e.Result))
                            OutputText += e.Result + "\n";
                        _cancelAction = null;
                        IsExecuting = false;
                        RaiseCanExecuteChanged();
                    };

                    bgWorker.RunWorkerAsync();

                    _cancelAction = async () =>
                    {
                        tokenSource.Cancel();
                        _cancelAction = null;
                        Encoder.Log = "";
                        OutputText += "Error infusion was canceled\n";

                        await Task.Delay(1000);
                        if (File.Exists(FilepathOut))
                            File.Delete(FilepathOut);
                    };
                }, x => File.Exists(FilepathIn) && BlockSize > 0 && ErrorCount > 0),
            };

            EncodingChoice = new ObservableCollection<EncodingEntry>()
            {
                new EncodingEntry(EncodingType.ShennonFano, "Shennon-Fano", () => true),
                new EncodingEntry(EncodingType.Huffman, "Huffman", () => true),
                new EncodingEntry(EncodingType.Hamming, "Hamming", () => BlockSize > 1 && Helper.HelperMath.IsPowerOfTwo((int)BlockSize)),
                new EncodingEntry(EncodingType.CRC, "CRC", () => BlockSize > 1),
            };

            ChooseFile = new RelayCommand(x =>
            {
                var openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = false;
                if (openFileDialog.ShowDialog() == true)
                    FilepathIn = openFileDialog.FileName;
            });
            Execute = new RelayCommand(x =>
            {
                IsExecuting = true;
                ChosenOperation.Execute(x);
            },
                x => ChosenOperation != null && !IsExecuting && ChosenOperation.CanExecute(x));
            Cancel = new RelayCommand(x => _cancelAction(), x => _cancelAction != null);
            OpenOutputDirectory = new RelayCommand(x => 
            {
                var directory = Path.GetDirectoryName(FilepathOut);
                Debug.WriteLine($"Directory: {directory}");
                Process.Start(@"explorer.exe", directory);
                //Process.Start(@"cmd.exe"); 
            }, 
                x => Directory.Exists(Path.GetDirectoryName(FilepathOut)));
            OutputToInput = new RelayCommand(x =>
            {
                FilepathIn = FilepathOut;
                SetFilepathOut();
            }, x => !string.IsNullOrEmpty(FilepathOut));
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
