using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using ShopApp.Models;
using ShopApp.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ShopApp.Views;

public partial class OrdersWindow : Window
{
    private User? _currentUser;
    public bool IsAdmin { get; private set; }

    public OrdersWindow()
    {
        InitializeComponent();
    }

    public OrdersWindow(User? user) : this()
    {
        _currentUser = user;
        bool isAdmin = user?.Role == "администратр" || user?.Role == "Администратор"; 
        bool isManager = user?.Role == "менеджер" || user?.Role == "Менеджер";
        IsAdmin = isAdmin || isManager;
        
        var addButton = this.FindControl<Button>("AddOrderButton");
        if (addButton != null)
        {
            addButton.IsVisible = IsAdmin;
        }

        LoadOrders();
    }

    private void LoadOrders()
    {
        try
        {
            using var db = new ShopContext();
            var orders = db.Orders
                .Include(o => o.PickupPoint)
                .Include(o => o.User)
                .OrderByDescending(o => o.DateOrder)
                .ToList();

            var list = this.FindControl<ListBox>("OrdersList");
            if (list != null)
            {
                list.ItemsSource = orders;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    private async void AddOrder_Click(object? sender, RoutedEventArgs e)
    {
        if (!IsAdmin) return;
        
        var editWindow = new OrderEditWindow(null, _currentUser);
        await editWindow.ShowDialog(this);
        LoadOrders();
    }

    private async void OrdersList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsAdmin) return; 
        
        var list = sender as ListBox;
        if (list?.SelectedItem is Order selectedOrder)
        {
            var editWindow = new OrderEditWindow(selectedOrder, _currentUser);
            await editWindow.ShowDialog(this);
            LoadOrders();
            list.SelectedItem = null; 
        }
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        LoadOrders();
    }

    private async void DeleteOrder_Click(object? sender, RoutedEventArgs e)
    {
        if (!IsAdmin) return;
        
        if (sender is Button button && button.Tag is int orderId)
        {
             bool confirmed = await ShowConfirm("Вы точно хотите удалить этот заказ?");
             if (!confirmed) return;

             try
             {
                 using var db = new ShopContext();
                 var order = await db.Orders
                     .Include(o => o.OrderDetails)
                     .FirstOrDefaultAsync(o => o.Id == orderId);

                 if (order != null)
                 {
                     db.OrderDetails.RemoveRange(order.OrderDetails);
                     db.Orders.Remove(order);
                     await db.SaveChangesAsync();
                     await ShowInfo("Заказ успешно удален.");
                     LoadOrders();
                 }
             }
             catch (Exception ex)
             {
                 await ShowError("Ошибка при удалении заказа: " + ex.Message);
             }
        }
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

    private async System.Threading.Tasks.Task<bool> ShowConfirm(string message)
    {
        var window = new Window();
        window.Title = "Подтверждение";
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

        var buttonPanel = new StackPanel();
        buttonPanel.Orientation = Avalonia.Layout.Orientation.Horizontal;
        buttonPanel.Spacing = 20;
        buttonPanel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;

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

    private async System.Threading.Tasks.Task ShowError(string message)
    {
        var window = new Window();
        window.Title = "Ошибка";
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
