using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using ShopApp.Models;
using ShopApp.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;

namespace ShopApp.Views;

public partial class ProductWindow : Window
{
    private Product _product = null!;
    private bool _isNew;
    private string? _newImagePath;

    public ProductWindow()
    {
        InitializeComponent();
    }

    public ProductWindow(Product? product) : this()
    {
        _isNew = product == null;
        if (_isNew)
        {
            _product = new Product();
            TitleBlock.Text = "Добавление товара";
            DeleteButton.IsVisible = false;
        }
        else
        {
            _product = product!;
            TitleBlock.Text = "Редактирование товара";
            DeleteButton.IsVisible = true;
        }

        InitializeData();
    }

    private void InitializeData()
    {
        using var db = new ShopContext();
        CategoryBox.ItemsSource = db.Categories.ToList();
        ManufacturerBox.ItemsSource = db.Manufacturers.ToList();
        SupplierBox.ItemsSource = db.Suppliers.ToList();

        if (!_isNew)
        {
            NameBox.Text = _product.Name;
            DescriptionBox.Text = _product.Description;
            PriceBox.Text = _product.Price.ToString("F2");
            DiscountBox.Text = _product.Discount.ToString();
            
            CategoryBox.SelectedItem = db.Categories.FirstOrDefault(c => c.Id == _product.CategoryId);
            ManufacturerBox.SelectedItem = db.Manufacturers.FirstOrDefault(m => m.Id == _product.ManufactureId);
            SupplierBox.SelectedItem = db.Suppliers.FirstOrDefault(s => s.Id == _product.SupplierId);
            
            var storage = _product.Storages.FirstOrDefault(); 
            UnitBox.Text = storage?.Unit ?? "шт.";
            QuantityBox.Text = storage?.Count.ToString() ?? "0";

            LoadImage(_product.Images.FirstOrDefault()?.Image1);
        }
        else
        {
            UnitBox.Text = "шт.";
            PriceBox.Text = "0";
            DiscountBox.Text = "0";
            QuantityBox.Text = "0";
            LoadStub();
        }
    }

    private void LoadImage(string? imageName)
    {
        if (string.IsNullOrEmpty(imageName))
        {
            LoadStub();
            return;
        }

        try
        {
            string appDir = AppContext.BaseDirectory;
            string path = Path.Combine(appDir, "Images", imageName);
            if (!File.Exists(path))
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), "Images", imageName);
            }

            if (File.Exists(path))
            {
                ProductImage.Source = new Bitmap(path);
            }
            else
            {
                LoadStub();
            }
        }
        catch
        {
            LoadStub();
        }
    }

    private void LoadStub()
    {
         try
         {
             string appDir = AppContext.BaseDirectory;
             string path = Path.Combine(appDir, "Assets", "picture.png");
             if (!File.Exists(path))
             {
                 path = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "picture.png");
             }

             if (File.Exists(path))
                ProductImage.Source = new Bitmap(path);
             else
                ProductImage.Source = null;
         }
         catch {}
    }

    private async void ChangeImage_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите изображение",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        if (files.Count > 0)
        {
            var file = files[0];
            string path = file.Path.LocalPath;
            
            try 
            {
                using (var stream = await file.OpenReadAsync())
                {
                    var bitmap = new Bitmap(stream);
                    
                    if (bitmap.PixelSize.Width > 300 || bitmap.PixelSize.Height > 200)
                    {
                         await ShowError("Размер изображения не должен превышать 300x200 пикселей.");
                         return;
                    }
                }

                string imagesDir = Path.Combine(AppContext.BaseDirectory, "Images");
                if (!Directory.Exists(imagesDir)) 
                {
         
                    imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "Images");
                }
                if (!Directory.Exists(imagesDir)) Directory.CreateDirectory(imagesDir);

                string ext = Path.GetExtension(path);
                string fileName = Guid.NewGuid() + ext;
                string destPath = Path.Combine(imagesDir, fileName);

                File.Copy(path, destPath, true);
                
                ProductImage.Source = new Bitmap(destPath);
                _newImagePath = destPath;
            }
            catch (Exception ex)
            {
                await ShowError("Ошибка загрузки: " + ex.Message);
            }
        }
    }

    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            await ShowError("Введите наименование");
            return;
        }
        if(NameBox.Text?.Length > 100)
        {
          await ShowError("Наименование товара превышает лимит (100 символов)");
            return;
        }
        if(DescriptionBox.Text?.Length > 255)
        {
          await ShowError("Описание товара превышает лимит (255 символов)");
            return;   
        }        
        if (!decimal.TryParse(PriceBox.Text, out decimal price) || price <= 0)
        {
            await ShowError("Цена должна быть положительным числом");
            return;
        }
        if (!int.TryParse(QuantityBox.Text, out int quantity) || quantity < 1)
        {
            await ShowError("Количество должно быть положительным целым числом");
            return;
        }
        if (!int.TryParse(DiscountBox.Text, out int discount) || discount < 0 || discount > 99)
        {
             await ShowError("Скидка должна быть целым числом от 0 до 99");
             return;
        }

        try
        {
            using var db = new ShopContext();
            Product productToSave;

            if (_isNew)
            {
                productToSave = new Product();
                db.Products.Add(productToSave);
            }
            else
            {
                productToSave = db.Products
                    .Include(p => p.Storages)
                    .Include(p => p.Images)
                    .First(p => p.Id == _product.Id);
            }

            productToSave.Name = NameBox.Text ?? "";
            productToSave.Price = (double)price;
            productToSave.Discount = discount;
            productToSave.Description = DescriptionBox.Text;
            
            if (CategoryBox.SelectedItem is Category cat) productToSave.CategoryId = cat.Id;
            if (ManufacturerBox.SelectedItem is Manufacturer man) productToSave.ManufactureId = man.Id;
            if (SupplierBox.SelectedItem is Supplier sup) productToSave.SupplierId = sup.Id;

            var storage = productToSave.Storages.FirstOrDefault();
            if (storage == null)
            {
                storage = new Storage();
                productToSave.Storages.Add(storage);
            }
            storage.Count = quantity;
            storage.Unit = UnitBox.Text ?? "шт.";

            if (_newImagePath != null)
            {
                string imagesDir = Path.Combine(AppContext.BaseDirectory, "Images");
                if (!Directory.Exists(imagesDir)) 
                {
                    // Fallback for development
                    imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "Images");
                }
                if (!Directory.Exists(imagesDir)) Directory.CreateDirectory(imagesDir);

                string fileName;
                if (Path.GetDirectoryName(_newImagePath) == imagesDir)
                {
                    fileName = Path.GetFileName(_newImagePath);
                }
                else
                {
                    string ext = Path.GetExtension(_newImagePath);
                    fileName = Guid.NewGuid() + ext;
                    string destPath = Path.Combine(imagesDir, fileName);

                    File.Copy(_newImagePath, destPath, true);
                }

                var oldImg = productToSave.Images.FirstOrDefault();
                if (oldImg != null)
                {
                    if (!string.IsNullOrEmpty(oldImg.Image1))
                    {
                        string oldPath = Path.Combine(imagesDir, oldImg.Image1);
                        if (File.Exists(oldPath))
                        {
                            try { File.Delete(oldPath); } catch {}
                        }
                    }
                }
                else
                {
                     oldImg = new ShopApp.Models.Image();
                     productToSave.Images.Add(oldImg);
                }
                oldImg.Image1 = fileName;
            }

            db.SaveChanges();
            await ShowInfo("Товар успешно сохранен.");
            this.Close();
        }
        catch (Exception ex)
        {
            await ShowError("Ошибка сохранения: " + ex.Message);
        }
    }

    private async void Delete_Click(object? sender, RoutedEventArgs e)
    {
        using var db = new ShopContext();
        bool inOrder = db.OrderDetails.Any(op => op.ProductId == _product.Id);
        
        if (inOrder)
        {
            await ShowError("Нельзя удалить товар, он есть в заказе!");
            return;
        }

        bool confirmed = await ShowConfirm("Вы точно хотите удалить этот товар?");
        
        
        if (confirmed == false)
        {
            return;
        }
        try
        {
             var prod = db.Products
                .Include(p => p.Images)
                .Include(p => p.Storages)
                .First(p => p.Id == _product.Id);
            
             
             foreach(var img in prod.Images)
             {
                 if(!string.IsNullOrEmpty(img.Image1))
                 {
                      string appDir = AppContext.BaseDirectory;
                      string path = Path.Combine(appDir, "Images", img.Image1);
                      if (!File.Exists(path))
                      {
                         
                          path = Path.Combine(Directory.GetCurrentDirectory(), "Images", img.Image1);
                      }
                      if(File.Exists(path)) File.Delete(path);
                 }
             }

             if (prod.Storages != null && prod.Storages.Any())
             {
                 db.Storages.RemoveRange(prod.Storages);
             }
             
             if (prod.Images != null && prod.Images.Any())
             {
                 db.Images.RemoveRange(prod.Images);
             }
             

             db.Products.Remove(prod);
             db.SaveChanges();
             await ShowInfo("Товар успешно удален.");
             this.Close();
        }
        catch (Exception ex)
        {
             await ShowError("Ошибка удаления: " + ex.Message);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private async Task ShowInfo(string message)
    {
        var window = new Window();
        window.Title = "Информация";
        window.Width = 300;
        window.Height = 150;
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var panel = new StackPanel();
        panel.VerticalAlignment = VerticalAlignment.Center;
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.Spacing = 10;

        var textBlock = new TextBlock();
        textBlock.Text = message;
        textBlock.TextWrapping = TextWrapping.Wrap;
        textBlock.HorizontalAlignment = HorizontalAlignment.Center;
        panel.Children.Add(textBlock);

        var button = new Button();
        button.Content = "OK";
        button.HorizontalAlignment = HorizontalAlignment.Center;
        button.Click += (s, e) => window.Close();
        panel.Children.Add(button);

        window.Content = panel;

        await window.ShowDialog(this);
    }

    private async Task ShowError(string message)
    {
        var window = new Window();
        window.Title = "Ошибка";
        window.Width = 300;
        window.Height = 150;
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var panel = new StackPanel();
        panel.VerticalAlignment = VerticalAlignment.Center;
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.Spacing = 10;

        var textBlock = new TextBlock();
        textBlock.Text = message;
        textBlock.TextWrapping = TextWrapping.Wrap;
        textBlock.HorizontalAlignment = HorizontalAlignment.Center;
        panel.Children.Add(textBlock);

        var button = new Button();
        button.Content = "OK";
        button.HorizontalAlignment = HorizontalAlignment.Center;
        button.Click += (s, e) => window.Close();
        panel.Children.Add(button);

        window.Content = panel;

        await window.ShowDialog(this);
    }

    private async Task<bool> ShowConfirm(string message)
    {
        var window = new Window();
        window.Title = "Подтверждение";
        window.Width = 300;
        window.Height = 150;
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var panel = new StackPanel();
        panel.VerticalAlignment = VerticalAlignment.Center;
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.Spacing = 10;

        var textBlock = new TextBlock();
        textBlock.Text = message;
        textBlock.TextWrapping = TextWrapping.Wrap;
        textBlock.HorizontalAlignment = HorizontalAlignment.Center;
        panel.Children.Add(textBlock);

        var buttonPanel = new StackPanel();
        buttonPanel.Orientation = Orientation.Horizontal;
        buttonPanel.Spacing = 20;
        buttonPanel.HorizontalAlignment = HorizontalAlignment.Center;

        bool result = false;

        var yesButton = new Button();
        yesButton.Content = "Да";
        yesButton.Click += (s, e) => {
            result = true;
            window.Close();
        };

        var noButton = new Button();
        noButton.Content = "Нет";
        noButton.Click += (s, e) => {
            result = false;
            window.Close();
        };

        buttonPanel.Children.Add(yesButton);
        buttonPanel.Children.Add(noButton);
        panel.Children.Add(buttonPanel);

        window.Content = panel;

        await window.ShowDialog(this);
        return result;
    }
}
