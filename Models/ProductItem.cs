using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.IO;
using Avalonia;

namespace ShopApp.Models;

public class ProductItem
{
    public Product Product { get; }

    public ProductItem(Product product)
    {
        Product = product;
    }

    public string ManufacturerName => Product.Manufacture?.Name ?? "Н/Д";
    public string SupplierName => Product.Supplier?.Name ?? "Н/Д";
    public string CategoryName => Product.Category?.Name ?? "Н/Д";
    public int Quantity => Product.Storages?.Sum(s => s.Count) ?? 0;
    public string Unit => Product.Storages?.FirstOrDefault()?.Unit ?? "шт.";
    
    public string PriceText => $"{Product.Price:F2}";
    public string DiscountedPriceText => Product.Discount > 0 ? $"{(Product.Price * (1 - (double)Product.Discount / 100.0)):F2}" : "";
    public bool HasDiscount => Product.Discount > 0;
    
    public IBrush BackgroundColor 
    {
        get
        {
            if (Product.Discount > 15) return Brushes.Chartreuse;
            return Brushes.White;
        }
    }

    public IBrush TextColor
    {
        get
        {
            return Brushes.Black;
        }
    }
    
    public IBrush PriceColor
    {
        get
        {
            if (HasDiscount) return Brushes.Red;
            return Brushes.Black;
        }
    }

    public IBrush QuantityBackgroundColor => Quantity == 0 ? Brushes.Blue : Brushes.Transparent;
    public IBrush QuantityTextColor => Quantity == 0 ? Brushes.White : Brushes.Black;
    public TextDecorationCollection? PriceDecorations => HasDiscount ? TextDecorations.Strikethrough : null;

    public Bitmap? ImageBitmap
    {
        get
        {
            var imgName = Product.Images.FirstOrDefault()?.Image1;
            if (string.IsNullOrEmpty(imgName)) return LoadStub();
            
            return LoadImage(imgName);
        }
    }

    private Bitmap? LoadImage(string filename)
    {
        try
        {
            string appDir = AppContext.BaseDirectory;
            string path = Path.Combine(appDir, "Images", filename);
            if (File.Exists(path))
                return new Bitmap(path);
            
            // Fallback for development
            path = Path.Combine(Directory.GetCurrentDirectory(), "Images", filename);
            if (File.Exists(path))
                return new Bitmap(path);
        }
        catch {}
        return LoadStub();
    }

    private Bitmap? LoadStub()
    {
        try
        {
            string appDir = AppContext.BaseDirectory;
            string path = Path.Combine(appDir, "Assets", "picture.png");
            if (File.Exists(path))
                return new Bitmap(path);

            // Fallback for development
            path = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "picture.png");
            if (File.Exists(path))
                return new Bitmap(path);
        }
        catch {}
        return null;
    }
}
