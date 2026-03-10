using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ShopApp.Models;
using ShopApp.Services;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ShopApp.Views;

public partial class OrderEditWindow : Window
{
    private Order? _order;
    private User? _currentUser;
    private bool _isNew = false;
    private ObservableCollection<OrderDetail> _orderDetails = new();

    public OrderEditWindow()
    {
        InitializeComponent();
    }

    public OrderEditWindow(Order? order, User? currentUser) : this()
    {
        _order = order;
        _currentUser = currentUser;
        
        LoadData();
    }

    private void LoadData()
    {
        using var db = new ShopContext();
        var points = db.PickupPoints.ToList();
        var allProducts = db.Products.ToList();
        var allUsers = db.Users.ToList();
        
        var pickupBox = this.FindControl<ComboBox>("PickupPointBox");
        if (pickupBox != null)
        {
            var displayPoints = points.Select(p => new { Id = p.Id, Display = $"{p.AddressCity}, {p.AddressStreet}, {p.AddressNumberHouse}", Original = p }).ToList();
            pickupBox.ItemsSource = displayPoints;
            pickupBox.DisplayMemberBinding = new Avalonia.Data.Binding("Display");
            pickupBox.SelectedValueBinding = new Avalonia.Data.Binding("Original");

            if (_order != null)
            {
                var selected = displayPoints.FirstOrDefault(i => i.Id == _order.PickupPointId);
                if (selected != null)
                {
                    pickupBox.SelectedItem = selected;
                }
            }
        }

        var userComboBox = this.FindControl<ComboBox>("UserComboBox");
        if (userComboBox != null)
        {
            userComboBox.ItemsSource = allUsers;
            userComboBox.DisplayMemberBinding = new Avalonia.Data.Binding("FullName");

            if (_order != null && _order.Id != 0)
            {
                var selectedUser = allUsers.FirstOrDefault(u => u.Id == _order.UserId);
                if (selectedUser != null)
                {
                    userComboBox.SelectedItem = selectedUser;
                }
            }
            else if (_currentUser != null)
            {
                var selectedUser = allUsers.FirstOrDefault(u => u.Id == _currentUser.Id);
                if (selectedUser != null)
                {
                    userComboBox.SelectedItem = selectedUser;
                }
            }
        }

        var productComboBox = this.FindControl<ComboBox>("ProductComboBox");
        if (productComboBox != null)
        {
            productComboBox.ItemsSource = allProducts;
            productComboBox.DisplayMemberBinding = new Avalonia.Data.Binding("Name");
        }

        var statusBox = this.FindControl<ComboBox>("StatusBox");
        var idBox = this.FindControl<TextBox>("IdBox");
        var dateOrderPicker = this.FindControl<DatePicker>("OrderDateDataPicker");
        var dateDeliveryPicker = this.FindControl<DatePicker>("DeliveryDatePicker");
        var titleBlock = this.FindControl<TextBlock>("TitleBlock");
        var detailsListBox = this.FindControl<ListBox>("OrderDetailsListBox");

            if (_order == null)
            {
                _isNew = true;
                _order = new Order();
                if (titleBlock != null) titleBlock.Text = "Новый заказ";
                if (idBox != null) idBox.Text = "Авто";
                 
                 var now = DateTimeOffset.Now;
                 var defaultDate = (now.Year == 2025 || now.Year == 2026) ? now : new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
                 
                 if (dateOrderPicker != null) dateOrderPicker.SelectedDate = defaultDate;
                 if (dateDeliveryPicker != null) dateDeliveryPicker.SelectedDate = defaultDate.AddDays(3);
                
                if (statusBox != null) statusBox.SelectedIndex = 0; 
                if (pickupBox != null && points.Any()) pickupBox.SelectedIndex = 0;
                _orderDetails = new ObservableCollection<OrderDetail>();
            }
        else
        {
            _isNew = false;
            if (titleBlock != null) titleBlock.Text = "Редактирование заказа";
            if (idBox != null) idBox.Text = _order!.Id.ToString();
            
            if (dateOrderPicker != null) 
                dateOrderPicker.SelectedDate = new DateTimeOffset(_order!.DateOrder.ToDateTime(TimeOnly.MinValue));
            
            if (dateDeliveryPicker != null)
                dateDeliveryPicker.SelectedDate = new DateTimeOffset(_order!.DateDelivery.ToDateTime(TimeOnly.MinValue));

            if (statusBox != null)
            {
                foreach (ComboBoxItem item in statusBox.Items)
                {
                    if (item.Content?.ToString() == _order!.StatusOrder)
                    {
                        statusBox.SelectedItem = item;
                        break;
                    }
                }
            }

            var details = db.OrderDetails.Include(d => d.Product).Where(d => d.OrderId == _order!.Id).ToList();
            _orderDetails = new ObservableCollection<OrderDetail>(details);
        }

        if (detailsListBox != null)
        {
            detailsListBox.ItemsSource = _orderDetails;
        }

        UpdateTotalPrice();
    }

    private void UpdateTotalPrice()
    {
        double total = _orderDetails.Sum(d => d.Product.Price * d.Count);
        var totalPriceBlock = this.FindControl<TextBlock>("TotalPriceBlock");
        if (totalPriceBlock != null)
        {
            totalPriceBlock.Text = $"{total:F2} руб.";
        }
    }

    private void AddProduct_Click(object? sender, RoutedEventArgs e)
    {
        var productComboBox = this.FindControl<ComboBox>("ProductComboBox");
        if (productComboBox?.SelectedItem is Product product)
        {
            var existing = _orderDetails.FirstOrDefault(d => d.ProductId == product.Id);
            if (existing != null)
            {
                existing.Count++;
                // Trigger refresh by resetting ItemsSource or using ObservableCollection notification if bound property changes
                // For simplicity with NumericUpDown binding, we might need to manually refresh or use a ViewModel
                var detailsListBox = this.FindControl<ListBox>("OrderDetailsListBox");
                if (detailsListBox != null) detailsListBox.ItemsSource = null;
                if (detailsListBox != null) detailsListBox.ItemsSource = _orderDetails;
            }
            else
            {
                _orderDetails.Add(new OrderDetail
                {
                    Product = product,
                    ProductId = product.Id,
                    Count = 1,
                    Price = product.Price
                });
            }
            UpdateTotalPrice();
        }
    }

    private void RemoveProduct_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is OrderDetail detail)
        {
            _orderDetails.Remove(detail);
            UpdateTotalPrice();
        }
    }

    private void Quantity_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (sender is NumericUpDown numeric && numeric.DataContext is OrderDetail detail)
        {
            detail.Count = (int)(numeric.Value ?? 1);
            UpdateTotalPrice();
        }
    }

    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!_orderDetails.Any())
            {
                await ShowError("В заказе должен быть хотя бы один товар.");
                return;
            }

            var statusBox = this.FindControl<ComboBox>("StatusBox");
            var pickupBox = this.FindControl<ComboBox>("PickupPointBox");
            var dateOrderPicker = this.FindControl<DatePicker>("OrderDateDataPicker");
            var dateDeliveryPicker = this.FindControl<DatePicker>("DeliveryDatePicker");
            var userComboBox = this.FindControl<ComboBox>("UserComboBox");

            if (statusBox?.SelectedItem is ComboBoxItem statusItem &&
                pickupBox?.SelectedItem != null &&
                dateOrderPicker?.SelectedDate != null &&
                dateDeliveryPicker?.SelectedDate != null &&
                userComboBox?.SelectedItem is User selectedUser)
            {
                var orderYear = dateOrderPicker.SelectedDate.Value.Year;
                var deliveryYear = dateDeliveryPicker.SelectedDate.Value.Year;

                if (orderYear < 2025 || orderYear > 2026 || 
                    deliveryYear < 2025 || deliveryYear > 2026)
                {
                    await ShowError("Заказы можно оформлять только на 2025 или 2026 год.");
                    return;
                }

                if (dateDeliveryPicker.SelectedDate.Value < dateOrderPicker.SelectedDate.Value)
                {
                    await ShowError("Дата выдачи не может быть раньше даты заказа.");
                    return;
                }

                dynamic pickupItem = pickupBox.SelectedItem;
                using var db = new ShopContext();

                if (_isNew)
                {
                    _order = new Order();
                    _order.DateOrder = DateOnly.FromDateTime(dateOrderPicker.SelectedDate.Value.DateTime);
                    _order.DateDelivery = DateOnly.FromDateTime(dateDeliveryPicker.SelectedDate.Value.DateTime);
                    _order.StatusOrder = statusItem.Content?.ToString() ?? "Новый";
                    _order.PickupPointId = pickupItem.Id;
                    _order.Code = new Random().Next(100, 999);
                    _order.UserId = selectedUser.Id;

                    db.Orders.Add(_order);
                    db.SaveChanges(); // Save to get Order ID
                }
                else
                {
                    var existing = db.Orders.Find(_order!.Id);
                    if (existing != null)
                    {
                        existing.DateOrder = DateOnly.FromDateTime(dateOrderPicker.SelectedDate.Value.DateTime);
                        existing.DateDelivery = DateOnly.FromDateTime(dateDeliveryPicker.SelectedDate.Value.DateTime);
                        existing.StatusOrder = statusItem.Content?.ToString() ?? "Новый";
                        existing.PickupPointId = pickupItem.Id;
                        existing.UserId = selectedUser.Id;
                        _order = existing;
                    }
                }

                // Handle OrderDetails
                var existingDetails = db.OrderDetails.Where(d => d.OrderId == _order.Id).ToList();
                db.OrderDetails.RemoveRange(existingDetails);

                foreach (var detail in _orderDetails)
                {
                    db.OrderDetails.Add(new OrderDetail
                    {
                        OrderId = _order.Id,
                        ProductId = detail.ProductId,
                        Count = detail.Count,
                        Price = detail.Price
                    });
                }

                db.SaveChanges();
                await ShowInfo("Заказ успешно сохранен.");
                this.Close();
            }
        }
        catch (Exception ex)
        {
             await ShowError("Ошибка сохранения: " + ex.Message);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
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
