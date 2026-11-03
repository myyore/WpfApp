using System.IO;
using WpfApp.Models;

namespace WpfApp.Services;

public class ProductRepository
{
    private readonly List<Product> _all = new();

    public ProductRepository()
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "Samples");
        Directory.CreateDirectory(dir);

        foreach (var d in new double[] { 50, 60, 70, 80, 100 })
        {
            string file = Path.Combine(dir, $"friction_{d:0}.pdf");
            if (!File.Exists(file))
            {
                SamplePdfGenerator.Create(file, $"Friction Plate D{d:0}",
                    new[] { "Type: Friction Plate", $"Diameter: {d:0} mm",
                            "Material: Sintered Bronze", "Thickness: 3.2 mm" });
            }
            _all.Add(new Product
            {
                Module = ProductModule.FrictionPlate,
                Name = $"摩擦片 φ{d:0}",
                Diameter = d,
                PdfPath = file
            });
        }

        var gears = new (double m, int z)[]
        {
            (1, 20), (1, 30), (1, 40),
            (1.5, 20), (1.5, 25), (1.5, 30),
            (2, 18), (2, 24), (2, 36)
        };
        foreach (var (m, z) in gears)
        {
            string file = Path.Combine(dir, $"gear_m{m:0.##}_z{z}.pdf");
            if (!File.Exists(file))
            {
                SamplePdfGenerator.Create(file, $"Spur Gear m{m:0.##} z{z}",
                    new[] { "Type: Spur Gear", $"Module: {m:0.##}", $"Teeth: {z}",
                            $"Pitch Dia: {m * z:0.##} mm", "Pressure Angle: 20 deg" });
            }
            _all.Add(new Product
            {
                Module = ProductModule.Gear,
                Name = $"齿轮 m{m:0.##} z{z}",
                ModuleValue = m,
                TeethCount = z,
                PdfPath = file
            });
        }
    }

    public IReadOnlyList<Product> All => _all;

    public IEnumerable<Product> Search(ProductModule module, double? diameter, double? moduleValue, int? teeth)
    {
        IEnumerable<Product> q = _all.Where(p => p.Module == module);

        if (module == ProductModule.FrictionPlate && diameter.HasValue)
            q = q.Where(p => Math.Abs(p.Diameter - diameter.Value) < 0.01);

        if (module == ProductModule.Gear)
        {
            if (moduleValue.HasValue)
                q = q.Where(p => Math.Abs(p.ModuleValue - moduleValue.Value) < 0.001);
            if (teeth.HasValue)
                q = q.Where(p => p.TeethCount == teeth.Value);
        }

        return q;
    }
}