namespace WpfApp.Models;

public enum ProductModule
{
    FrictionPlate,
    Gear
}

public class Product
{
    public string Name { get; set; } = "";
    public ProductModule Module { get; set; }
    public string PdfPath { get; set; } = "";

    public double Diameter { get; set; }
    public double ModuleValue { get; set; }
    public int TeethCount { get; set; }

    public string SpecText => Module == ProductModule.FrictionPlate
        ? $"直径 φ{Diameter:0.#} mm"
        : $"模数 {ModuleValue:0.##} · 齿数 {TeethCount}";
}