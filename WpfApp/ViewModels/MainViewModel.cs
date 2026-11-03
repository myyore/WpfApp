using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using WpfApp.Models;
using WpfApp.Services;

namespace WpfApp.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ProductRepository _repo = new();
    private ProductModule _selectedModule = ProductModule.FrictionPlate;

    public event Action<string>? OpenDrawingRequested;

    public MainViewModel()
    {
        SearchCommand = new RelayCommand(_ => Search());
        OpenDrawingCommand = new RelayCommand(OpenDrawing);
        Search();
    }

    public bool IsFrictionPlate
    {
        get => _selectedModule == ProductModule.FrictionPlate;
        set
        {
            if (!value || _selectedModule == ProductModule.FrictionPlate) return;
            _selectedModule = ProductModule.FrictionPlate;
            OnPropertyChanged(nameof(IsFrictionPlate));
            OnPropertyChanged(nameof(IsGear));
            Search();
        }
    }

    public bool IsGear
    {
        get => _selectedModule == ProductModule.Gear;
        set
        {
            if (!value || _selectedModule == ProductModule.Gear) return;
            _selectedModule = ProductModule.Gear;
            OnPropertyChanged(nameof(IsGear));
            OnPropertyChanged(nameof(IsFrictionPlate));
            Search();
        }
    }

    private string _filterDiameter = "";
    public string FilterDiameter
    {
        get => _filterDiameter;
        set => Set(ref _filterDiameter, value);
    }

    private string _filterModule = "";
    public string FilterModule
    {
        get => _filterModule;
        set => Set(ref _filterModule, value);
    }

    private string _filterTeeth = "";
    public string FilterTeeth
    {
        get => _filterTeeth;
        set => Set(ref _filterTeeth, value);
    }

    public ObservableCollection<Product> Results { get; } = new();

    public RelayCommand SearchCommand { get; }
    public RelayCommand OpenDrawingCommand { get; }

    private void Search()
    {
        Results.Clear();
        var list = _repo.Search(
            _selectedModule,
            ParseDouble(FilterDiameter),
            ParseDouble(FilterModule),
            ParseInt(FilterTeeth));

        foreach (var p in list) Results.Add(p);
    }

    private void OpenDrawing(object? parameter)
    {
        if (parameter is not Product product) return;

        if (!File.Exists(product.PdfPath))
        {
            MessageBox.Show($"找不到图纸文件：\n{product.PdfPath}",
                "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        OpenDrawingRequested?.Invoke(product.PdfPath);
    }

    private static double? ParseDouble(string s)
        => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static int? ParseInt(string s)
        => int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
}