using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Semestralka.Models;
using Semestralka.Services;

namespace Semestralka.WPF;

/// <summary>
/// Main application window. Hosts login flow and the primary UI after unlocking the profile.
/// </summary>
public partial class AppWindow : Window
{
    /// <summary>
    /// Currently loaded application profile.
    /// </summary>
    private AppProfile _profile;

    /// <summary>
    /// Indicates whether the storage file is encrypted and requires unlocking.
    /// </summary>
    private readonly bool _isEncryptedFile;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppWindow"/> class.
    /// Determines if the storage is encrypted and loads profile or shows login accordingly.
    /// </summary>
    public AppWindow()
    {
        InitializeComponent();
        _isEncryptedFile = StorageService.IsEncryptedFile();

        if (_isEncryptedFile)
        {
            _profile = new AppProfile();
            LoginGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
            return;
        }

        _profile = StorageService.LoadProfile();
        InitializeViewModel();

        // sync theme combobox to profile / viewmodel
        ThemeComboBox.SelectedIndex = _profile.Theme == "Dark" ? 1 : 0;
        ApplyTheme(_profile.Theme);

        if (string.IsNullOrEmpty(_profile.PasswordHash))
        {
            UnlockApp();
        }
    }

    /// <summary>
    /// Handles the login button click. Unlocks the application either by decrypting the
    /// encrypted profile or by verifying the stored password hash.
    /// </summary>
    private void Login_Click(object sender, RoutedEventArgs e)
    {
        if (_isEncryptedFile)
        {
            var password = LoginPasswordBox.Password;
            if (!StorageService.TryUnlockEncryptedProfile(password))
            {
                LoginErrorText.Text = "Nesprávne heslo, prístup odmietnutý!";
                return;
            }

            var decryptedProfile = StorageService.LoadProfile(password: password, throwOnDecryptFailure: true);
            if (!string.IsNullOrEmpty(decryptedProfile.PasswordHash) && !SecurityHelper.VerifyPassword(password, decryptedProfile.PasswordHash))
            {
                StorageService.SetSessionPassword(null);
                LoginErrorText.Text = "Nesprávne heslo, prístup odmietnutý!";
                return;
            }

            _profile = decryptedProfile;
            InitializeViewModel();
            ThemeComboBox.SelectedIndex = _profile.Theme == "Dark" ? 1 : 0;
            ApplyTheme(_profile.Theme);
            UnlockApp();
            return;
        }

        if (SecurityHelper.VerifyPassword(LoginPasswordBox.Password, _profile.PasswordHash))
        {
            UnlockApp();
        }
        else
        {
            LoginErrorText.Text = "Nesprávne heslo, prístup odmietnutý!";
        }
    }

    /// <summary>
    /// Shows the main UI and initializes view model state after successful unlock.
    /// </summary>
    private void UnlockApp()
    {
        LoginGrid.Visibility = Visibility.Collapsed;
        MainGrid.Visibility = Visibility.Visible;

        if (DataContext is Semestralka.WPF.ViewModels.AppWindowViewModel vm)
        {
            if (!vm.Wallets.Any())
            {
                foreach (var w in _profile.Wallets) vm.Wallets.Add(w);
                vm.SelectedWallet = vm.Wallets.FirstOrDefault();
            }

            vm.RebuildPieChartData();
            RefreshUI();
        }
    }

    /// <summary>
    /// Creates and assigns the ViewModel for the window and subscribes to events used by the view.
    /// </summary>
    private void InitializeViewModel()
    {
        var vm = new Semestralka.WPF.ViewModels.AppWindowViewModel();
        DataContext = vm;
        vm.PieChartChanged += RefreshUI;
    }

    /// <summary>
    /// Saves or removes the profile password based on user input and updates the stored profile.
    /// </summary>
    private void SavePassword_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_profile.PasswordHash))
        {
            if (!SecurityHelper.VerifyPassword(CurrentPasswordBox.Password, _profile.PasswordHash))
            {
                MessageBox.Show("Súčasné heslo je nesprávne!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        var newPassword = NewPasswordBox.Password;
        _profile.PasswordHash = SecurityHelper.HashPassword(newPassword);

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            StorageService.SetSessionPassword(null);
            StorageService.SaveProfile(_profile, password: null);
        }
        else
        {
            StorageService.SetSessionPassword(newPassword);
            StorageService.SaveProfile(_profile, password: newPassword);
        }
        MessageBox.Show("Bezpečnostné nastavenia boli úspešne aktualizované!", "Zabezpečenie", MessageBoxButton.OK, MessageBoxImage.Information);

        CurrentPasswordBox?.Clear();
        NewPasswordBox?.Clear();
    }

    /// <summary>
    /// Handles theme selection changes from the UI, persists the choice and applies it immediately.
    /// </summary>
    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        var theme = ThemeComboBox.SelectedIndex == 1 ? "Dark" : "Light";
        _profile.Theme = theme;
        StorageService.SaveProfile(_profile);
        ApplyTheme(theme);
    }

    /// <summary>
    /// Applies the requested theme by updating resource brushes used throughout the application.
    /// </summary>
    /// <param name="theme">Either "Dark" or "Light".</param>
    private void ApplyTheme(string theme)
    {
        var res = Application.Current.Resources;
        if (theme == "Dark")
        {
            res["WindowBgBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E1E1E"));
            res["TextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White) { Opacity = 0.85 };
            res["PanelBgBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D2D30"));
            res["GridBgBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#252526"));
            res["CardBgBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333337"));
            res["InputBgBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3C3C40"));
            res["InputFgBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            res["SelectionInputBgBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#BDBDBD"));
            res["SelectionInputFgBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            res["SelectionButtonFgBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
        }
        else
        {
            res["WindowBgBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            res["TextBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            res["PanelBgBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F0F8FF"));
            res["GridBgBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E8F4F8"));
            res["CardBgBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            res["InputBgBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            res["InputFgBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            res["SelectionInputBgBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            res["SelectionInputFgBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            res["SelectionButtonFgBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
        }
    }

    /// <summary>
    /// Renders the pie chart and binds the legend items from the ViewModel.
    /// </summary>
    private void RefreshUI()
    {
        if (DataContext is not Semestralka.WPF.ViewModels.AppWindowViewModel vm) return;
        PieChartCanvas.Children.Clear();
        double radius = 100;
        var center = new System.Windows.Point(radius, radius);
        foreach (var slice in vm.PieSlices)
        {
            if (slice.SweepAngle <= 0) continue;
            if (slice.SweepAngle >= 359.99)
            {
                var ellipse = new System.Windows.Shapes.Ellipse { Width = radius * 2, Height = radius * 2, Fill = slice.Fill };
                PieChartCanvas.Children.Add(ellipse);
                continue;
            }

            var path = new System.Windows.Shapes.Path { Fill = slice.Fill };
            var geometry = new System.Windows.Media.PathGeometry();
            var figure = new System.Windows.Media.PathFigure { StartPoint = center, IsClosed = true };

            double startRad = (slice.StartAngle - 90) * Math.PI / 180.0;
            double endRad = (slice.StartAngle + slice.SweepAngle - 90) * Math.PI / 180.0;

            var startPoint = new System.Windows.Point(center.X + Math.Cos(startRad) * radius, center.Y + Math.Sin(startRad) * radius);
            var endPoint = new System.Windows.Point(center.X + Math.Cos(endRad) * radius, center.Y + Math.Sin(endRad) * radius);

            figure.Segments.Add(new System.Windows.Media.LineSegment(startPoint, false));
            figure.Segments.Add(new System.Windows.Media.ArcSegment(endPoint, new System.Windows.Size(radius, radius), 0, slice.SweepAngle > 180, System.Windows.Media.SweepDirection.Clockwise, false));
            geometry.Figures.Add(figure);
            path.Data = geometry;
            PieChartCanvas.Children.Add(path);
        }

        LegendItemsControl.ItemsSource = vm.LegendItems;
    }
}
