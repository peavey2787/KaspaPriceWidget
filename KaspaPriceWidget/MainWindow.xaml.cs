using KaspaPriceWidget;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace KaspaInfoWidget
{
    public partial class MainWindow : Window
    {
        #region Variables
        private Timer refreshTimer;
        private Timer countdownTimer;
        private bool appHasControl = true;
        private int refreshInterval = 90; // seconds
        private int countdown;
        private readonly string settingsFilePath = "settings.json";

        private const int ResizeMargin = 10;
        private bool isResizing = false;
        private bool isMoving = false;
        private bool isLocked = false;
        private ResizeDirection resizeDirection;
        [Flags]
        private enum ResizeDirection
        {
            None = 0,
            Left = 1,
            Top = 2,
            Right = 4,
            Bottom = 8,
            TopLeft = Top | Left,
            TopRight = Top | Right,
            BottomLeft = Bottom | Left,
            BottomRight = Bottom | Right
        }
        #endregion



        #region Start/Save/Load/Close
        public MainWindow()
        {
            InitializeComponent();
            this.Topmost = true;
            this.Opacity = 1.0;
            this.sliderOpacity.Value = 100;
            this.sliderOpacity.ValueChanged += SliderOpacity_ValueChanged;
            this.sliderFontSize.Value = 16;
            this.sliderFontSize.ValueChanged += SliderFontSize_ValueChanged;

            this.refreshTimer = new Timer(refreshInterval * 1000);
            this.refreshTimer.Elapsed += RefreshTimer_Elapsed;

            this.countdownTimer = new Timer(1000);
            this.countdownTimer.Elapsed += CountdownTimer_Elapsed;

            LoadSettings();
            StartTimers();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            KeepWindowOnTop();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }
        private void LoadSettings()
        {
            appHasControl = true;

            if (File.Exists(settingsFilePath))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(settingsFilePath))
                    {
                        string json = reader.ReadToEnd();
                        SettingsData settings = JsonSerializer.Deserialize<SettingsData>(json);
                        if (settings != null)
                        {
                            sliderFontSize.Value = settings.FontSize;
                            Color fontColor = (Color)ColorConverter.ConvertFromString(settings.FontColor);
                            Color backgroundColor = (Color)ColorConverter.ConvertFromString(settings.BackgroundColor);

                            ApplyFontSize(settings.FontSize);
                            ApplyFontColor(fontColor);
                            ApplyBackgroundColor(backgroundColor);
                            ApplyOpacity(settings.Opacity);
                            ApplyLock(settings.isLocked);

                            // Set window position and size
                            this.Left = settings.WindowLeft;
                            this.Top = settings.WindowTop;
                            this.Height = settings.Height;
                            this.Width = settings.Width;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading settings: {ex.Message}");
                }
            }
            appHasControl = false;
        }
        private void SaveSettings()
        {
            if (appHasControl) return;

            try
            {
                SettingsData settings = new SettingsData
                {
                    FontSize = sliderFontSize.Value,
                    FontColor = colorPicker?.SelectedColor?.ToString() ?? Colors.Black.ToString(),
                    BackgroundColor = backgroundColorPicker?.SelectedColor?.ToString() ?? Colors.White.ToString(),
                    WindowLeft = this.Left,
                    WindowTop = this.Top,
                    Opacity = this.Opacity,
                    Height = this.Height,
                    Width = this.Width,
                    isLocked = isLocked
                };

                string json = JsonSerializer.Serialize(settings);
                File.WriteAllText(settingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
        #endregion



        #region Timers
        private void StartTimers()
        {
            this.countdown = refreshInterval;
            this.refreshTimer.Start();
            this.countdownTimer.Start();
            FetchCryptoDataAsync();
        }
        private void RefreshTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            FetchCryptoDataAsync();
            this.countdown = refreshInterval;
        }
        private void CountdownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.countdown--;
                this.labelCountdown.Content = $"{this.countdown}";

                if (this.countdown <= 0)
                {
                    this.countdown = refreshInterval;
                    circularMask.Source = null; // Reset the mask
                }

                UpdateCircularMask();
            });
        }
        #endregion



        #region Helpers


        private void UpdateCircularMask()
        {
            double degrees = 360 * (this.countdown / 90.0); // Calculate the angle of the pie slice based on countdown
            double radians = degrees * Math.PI / 180; // Convert degrees to radians for trigonometric calculations

            // Create a DrawingVisual to generate the pie slice mask
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                // Calculate the endpoint of the arc
                double centerX = circularMask.Width / 2;
                double centerY = circularMask.Height / 2;
                double radiusX = circularMask.Width / 2;
                double radiusY = circularMask.Height / 2;
                double startPointX = centerX;
                double startPointY = centerY - radiusY;
                double endPointX = centerX + radiusX * Math.Sin(radians); // End point calculation
                double endPointY = centerY - radiusY * Math.Cos(radians); // End point calculation

                // Create a pie slice path
                PathFigure pathFigure = new PathFigure
                {
                    StartPoint = new Point(centerX, centerY)
                };
                pathFigure.Segments.Add(new LineSegment(new Point(startPointX, startPointY), true));
                pathFigure.Segments.Add(new ArcSegment(new Point(endPointX, endPointY),
                    new Size(radiusX, radiusY), 0, degrees > 180, SweepDirection.Clockwise, true));
                pathFigure.Segments.Add(new LineSegment(new Point(centerX, centerY), true));
                pathFigure.IsClosed = true;

                // Create a pie slice geometry
                PathGeometry pathGeometry = new PathGeometry();
                pathGeometry.Figures.Add(pathFigure);

                // Create a solid black brush
                SolidColorBrush brush = new SolidColorBrush(Colors.Black);

                // Draw the pie slice mask
                drawingContext.DrawGeometry(brush, null, pathGeometry);
            }

            // Render the drawing visual to a bitmap
            RenderTargetBitmap bitmap = new RenderTargetBitmap((int)circularMask.Width, (int)circularMask.Height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(drawingVisual);

            // Set the generated bitmap as the source for the circular mask image
            circularMask.Source = bitmap;
        }
        private async void FetchCryptoDataAsync()
        {
            string networkHashrate = await KaspaInfo.GetHashrate();
            string maxNetworkHashrate = await KaspaInfo.GetMaxHashrate();
            string price = await KaspaInfo.GetPrice();

            this.Dispatcher.Invoke(() =>
            {
                if (price == "Error")
                {
                    // Show warning icon and keep the old price
                    this.warningIcon.Visibility = Visibility.Visible;
                }
                else
                {
                    // Update the price and hide the warning icon
                    this.labelPrice.Content = $"${price}";
                    this.warningIcon.Visibility = Visibility.Collapsed;
                }

                this.labelNetworkHashrate.Content = $"{networkHashrate}";
                this.labelMaxNetworkHashrate.Content = $"{maxNetworkHashrate}";
            });
        }
        private void KeepWindowOnTop()
        {
            // Keep window on top
            Process currentProcess = Process.GetCurrentProcess();
            IntPtr mainWindowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            AlwaysOnTopToggler toggler = new AlwaysOnTopToggler(mainWindowHandle);
            toggler.SetAlwaysOnTop(true);
        }
        #endregion



        #region User Actions
        private void SliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (appHasControl) return;

            this.Opacity = sliderOpacity.Value / 100.0;
            SaveSettings();
        }
        private void SliderFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (appHasControl) return;

            double fontSize = sliderFontSize.Value;
            labelPrice.FontSize = fontSize;
            labelNetworkHashrate.FontSize = fontSize;
            labelMaxNetworkHashrate.FontSize = fontSize;
            labelCountdown.FontSize = fontSize;

            ApplyFontSize(sliderFontSize.Value);
            SaveSettings();
        }
        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (appHasControl) return;

            if (colorPicker.SelectedColor.HasValue)
            {
                Color selectedColor = colorPicker.SelectedColor.Value;
                ApplyFontColor(selectedColor);
                SaveSettings();
            }
        }
        private void BackgroundColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (appHasControl) return;

            if (backgroundColorPicker.SelectedColor.HasValue)
            {
                Color selectedColor = backgroundColorPicker.SelectedColor.Value;
                ApplyBackgroundColor(selectedColor);
                SaveSettings();
            }
        }
        private void warningIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Toggle the visibility of the tooltip
            warningToolTip.IsOpen = !warningToolTip.IsOpen;

            // Set the placement mode to mouse and open the tooltip
            warningToolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            warningToolTip.IsOpen = true;
        }

        #endregion



        #region Apply Colors/Fonts
        private void ApplyFontColor(Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            labelPrice.Foreground = brush;
            labelNetworkHashrate.Foreground = brush;
            labelMaxNetworkHashrate.Foreground = brush;
            labelCountdown.Foreground = brush;
            colorPicker.SelectedColor = color;
        }

        private void ApplyBackgroundColor(Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            mainBorder.Background = brush;
            backgroundColorPicker.SelectedColor = color;
        }

        private void ApplyFontSize(double fontSize)
        {
            labelPrice.FontSize = fontSize;
            labelNetworkHashrate.FontSize = fontSize;
            labelMaxNetworkHashrate.FontSize = fontSize;
            labelCountdown.FontSize = fontSize * 0.50;
            sliderFontSize.Value = fontSize;
        }

        private void ApplyOpacity(double opacity)
        {
            this.Opacity = opacity;
            sliderOpacity.Value = opacity * 100;
        }
        private void ApplyLock(bool isLocked)
        {
            this.isLocked = isLocked;
            lockMenuItem.Header = isLocked ? "Unlock" : "Lock"; 
        }
        #endregion



        #region Resize/Move
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isLocked) return;

            if (this.Cursor != Cursors.Arrow && resizeDirection != ResizeDirection.None)
            {
                isResizing = true;
                mainBorder.CaptureMouse();
            }
            else
            {
                isMoving = true;
                this.DragMove();                
            }
            SaveSettings();
        }
        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isLocked) return;

            if (isResizing)
            {
                isResizing = false;
                resizeDirection = ResizeDirection.None;
                this.Cursor = Cursors.Arrow;
                this.ReleaseMouseCapture();
            }
            isMoving = false;
            mainBorder.ReleaseMouseCapture();
        }
        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            if (isLocked) return;

            if (isResizing)
            {
                ResizeWindow();
                return;
            }
            else
            {
                Point mousePos = e.GetPosition(this);
                SetResizeCursor(mousePos);
            }
        }

        private void ResizeWindow()
        {
            Point mousePos = Mouse.GetPosition(this);
            Point screenMousePos = this.PointToScreen(mousePos);
            double newWidth = this.Width;
            double newHeight = this.Height;

            if (resizeDirection.HasFlag(ResizeDirection.Right))
            {
                newWidth = mousePos.X;
            }
            if (resizeDirection.HasFlag(ResizeDirection.Bottom))
            {
                newHeight = mousePos.Y;
            }
            if (resizeDirection.HasFlag(ResizeDirection.Left))
            {
                double offset = this.Left - screenMousePos.X;
                newWidth += offset;
                this.Left = screenMousePos.X;
            }
            if (resizeDirection.HasFlag(ResizeDirection.Top))
            {
                double offset = this.Top - screenMousePos.Y;
                newHeight += offset;
                this.Top = screenMousePos.Y;
            }

            this.Width = Math.Max(this.MinWidth, Math.Min(this.MaxWidth, newWidth));
            this.Height = Math.Max(this.MinHeight, Math.Min(this.MaxHeight, newHeight));
        }
        private void SetResizeCursor(Point mousePos)
        {
            resizeDirection = ResizeDirection.None;

            if (mousePos.X >= this.ActualWidth - ResizeMargin && mousePos.Y >= this.ActualHeight - ResizeMargin)
            {
                resizeDirection = ResizeDirection.BottomRight;
                this.Cursor = Cursors.SizeNWSE;
            }
            else if (mousePos.X <= ResizeMargin && mousePos.Y >= this.ActualHeight - ResizeMargin)
            {
                resizeDirection = ResizeDirection.BottomLeft;
                this.Cursor = Cursors.SizeNESW;
            }
            else if (mousePos.X >= this.ActualWidth - ResizeMargin && mousePos.Y <= ResizeMargin)
            {
                resizeDirection = ResizeDirection.TopRight;
                this.Cursor = Cursors.SizeNESW;
            }
            else if (mousePos.X <= ResizeMargin && mousePos.Y <= ResizeMargin)
            {
                resizeDirection = ResizeDirection.TopLeft;
                this.Cursor = Cursors.SizeNWSE;
            }
            else if (mousePos.X >= this.ActualWidth - ResizeMargin)
            {
                resizeDirection = ResizeDirection.Right;
                this.Cursor = Cursors.SizeWE;
            }
            else if (mousePos.X <= ResizeMargin)
            {
                resizeDirection = ResizeDirection.Left;
                this.Cursor = Cursors.SizeWE;
            }
            else if (mousePos.Y >= this.ActualHeight - ResizeMargin)
            {
                resizeDirection = ResizeDirection.Bottom;
                this.Cursor = Cursors.SizeNS;
            }
            else if (mousePos.Y <= ResizeMargin)
            {
                resizeDirection = ResizeDirection.Top;
                this.Cursor = Cursors.SizeNS;
            }
            else
            {
                this.Cursor = Cursors.Arrow;
            }
        }
        #endregion


        
        #region Context Menu
        private void MenuItem_Close_Click(object sender, RoutedEventArgs e)
        {
            // Stop the timer
            countdownTimer.Stop();

            // Wait for any pending dispatcher operations to complete
            this.Dispatcher.InvokeShutdown();

            this.Close();
        }        
        private void LockMenuItem_Click(object sender, RoutedEventArgs e)
        {
            isLocked = !isLocked; // Toggle isLocked

            // Update the menu item header based on the new isLocked value
            lockMenuItem.Header = isLocked ? "Unlock" : "Lock";

            SaveSettings();
        }
        #endregion
    }
}
