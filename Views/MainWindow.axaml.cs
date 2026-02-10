using Avalonia.Controls;
using Avalonia.Interactivity;
using ShopApp.Models;
using ShopApp.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using System.Collections.Generic;

namespace ShopApp.Views;

public partial class MainWindow : Window
{
    private User? _currentUser;
    private List<ProductItem> _allProducts = new();
    private ProductWindow? _openedProductWindow;

    public MainWindow()
    {
        InitializeComponent();
        LoadProducts();
        LoadFilters();
    }

    public MainWindow(User? user) : this()
    {
        _currentUser = user;
        UserNameBlock.Text = user != null ? user.FullName : "Гость";
        UserNameBlock.Text += " | ";
        UserNameBlock.Text += user?.Role ?? "Гость";
        
        bool isAdmin = user?.Role == "администратр" || user?.Role == "Администратор"; 
        bool isManager = user?.Role == "менеджер" || user?.Role == "Менеджер";
        
        ToolbarPanel.IsVisible = user != null; 

        AddButton.IsVisible = isAdmin || isManager;
    }
  
    private void LoadFilters()
    {
         using var db = new ShopContext();
         var suppliers = db.Suppliers.ToList();
         suppliers.Insert(0, new Supplier { Name = "Все поставщики" });
         FilterBox.ItemsSource = suppliers;
         FilterBox.SelectedIndex = 0;
         SortBox.SelectedIndex = 0;
    }

    private void LoadProducts()
    {
        try
        {
            using var db = new ShopContext();
            var products = db.Products
                .Include(p => p.Manufacture)
                .Include(p => p.Supplier)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Storages)
                .ToList();

            _allProducts = products.Select(p => new ProductItem(p)).ToList();
            UpdateList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error: " + ex.Message);
        }
    }

    private void UpdateList()
    {
        var filtered = _allProducts.AsEnumerable();

        string searchText = SearchBox.Text?.ToLower() ?? "";
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filtered = filtered.Where(p => 
                p.Product.Name.ToLower().Contains(searchText) ||
                (p.Product.Description?.ToLower().Contains(searchText) ?? false) ||
                p.ManufacturerName.ToLower().Contains(searchText) ||
                p.CategoryName.ToLower().Contains(searchText) ||
                p.SupplierName.ToLower().Contains(searchText) ||
                p.Product.Price.ToString().Contains(searchText)
            );
        }

        if (FilterBox.SelectedItem is Supplier supplier && supplier.Name != "Все поставщики")
        {
            filtered = filtered.Where(p => p.Product.SupplierId == supplier.Id);
        }

        switch (SortBox.SelectedIndex)
        {
            case 1:
                filtered = filtered.OrderBy(p => p.Quantity);
                break;
            case 2:
                filtered = filtered.OrderByDescending(p => p.Quantity);
                break;
        }

        ProductListBox.ItemsSource = filtered.ToList();
        if(filtered.Count() == 0)
        {
          NotFoundText.IsVisible = true;
        }
        else
        {
          NotFoundText.IsVisible = false;
        }
        
    }

    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        UpdateList();
    }

    private void SortBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
         UpdateList();
    }

    private void FilterBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
         UpdateList();
    }

    private void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        bool canAdd = _currentUser?.Role == "администратр" || _currentUser?.Role == "Администратор" || 
                      _currentUser?.Role == "менеджер" || _currentUser?.Role == "Менеджер";
        if (!canAdd) return;

        if (_openedProductWindow != null) return;
        
        _openedProductWindow = new ProductWindow(null);
        _openedProductWindow.Closed += (s, ev) => 
        {
            _openedProductWindow = null;
            LoadProducts();
        };
        _openedProductWindow.Show();
    }

    private void ProductListBox_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        bool canEdit = _currentUser?.Role == "администратр" || _currentUser?.Role == "Администратор" || 
                       _currentUser?.Role == "менеджер" || _currentUser?.Role == "Менеджер";
        if (!canEdit) return;

        if (ProductListBox.SelectedItem is ProductItem item)
        {
             if (_openedProductWindow != null) return;

             using var db = new ShopContext();
             var product = db.Products
                .Include(p => p.Storages)
                .Include(p => p.Images)
                .FirstOrDefault(p => p.Id == item.Product.Id);

             if (product != null)
             {
                 _openedProductWindow = new ProductWindow(product);
                 _openedProductWindow.Closed += (s, ev) => 
                 {
                     _openedProductWindow = null;
                     LoadProducts();
                 };
                 _openedProductWindow.Show();
             }
        }
    }


    private void OrdersButton_Click(object? sender, RoutedEventArgs e)
    {
        var window = new OrdersWindow(_currentUser);
        window.ShowDialog(this);
    }

    private async void Logout_Click(object? sender, RoutedEventArgs e)
    {
        await ShowInfo("Вы успешно вышли из системы.");
        new LoginWindow().Show();
        this.Close();
    }

    private async System.Threading.Tasks.Task ShowInfo(string message)
    {
        var window = new Window();
        window.Title = "Информация";
        window.Width = 300;
        window.Height = 150;
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var panel = new StackPanel();
        panel.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        panel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        panel.Spacing = 10;

        var textBlock = new TextBlock();
        textBlock.Text = message;
        textBlock.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
        textBlock.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        panel.Children.Add(textBlock);

        var button = new Button();
        button.Content = "OK";
        button.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        button.Click += (s, e) => window.Close();
        panel.Children.Add(button);

        window.Content = panel;

        await window.ShowDialog(this);
    }
}
