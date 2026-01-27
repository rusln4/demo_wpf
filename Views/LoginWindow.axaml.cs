using Avalonia.Controls;
using Avalonia.Interactivity;
using ShopApp.Services;
using System.Linq;
using System;
using ShopApp.Models;

namespace ShopApp.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private async void Login_Click(object? sender, RoutedEventArgs e)
    {
        ErrorBlock.Text = "";
        string email = EmailBox.Text ?? "";
        string password = PasswordBox.Text ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ErrorBlock.Text = "Введите логин и пароль";
            return;
        }

        try
        {
            using var db = new ShopContext();
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.Password == password);

            if (user != null)
            {
                await ShowInfo($"Добро пожаловать, {user.FullName}!");
                new MainWindow(user).Show();
                this.Close();
            }
            else
            {
                ErrorBlock.Text = "Неверный логин или пароль";
            }
        }
        catch (Exception ex)
        {
            ErrorBlock.Text = "Ошибка БД: " + ex.Message;
        }
    }

    private void Guest_Click(object? sender, RoutedEventArgs e)
    {
        new MainWindow(null).Show();
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
