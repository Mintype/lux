using Avalonia.Controls;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;

namespace lux;

public partial class MainWindow : Window
{
    private IStorageFile? _currentImageFile;
    
    private Point _lastMousePosition;
    private bool _isDragging = false;
    private double _zoom = 1.0;
    
    private double _translateX = 0;
    private double _translateY = 0;
    private TranslateTransform _imageTranslate = new TranslateTransform();
    private ScaleTransform _imageScale = new ScaleTransform();
    
    private RotateTransform _imageRotate = new RotateTransform();
    private double _currentRotation = 0;

    private WindowState _previousWindowState;
    private double _previousWidth;
    private double _previousHeight;
    private double _previousX;
    private double _previousY;
    
    private Bitmap _bitmap;
    
    public MainWindow()
    {
        InitializeComponent();

        _bitmap = new WriteableBitmap(
            new PixelSize(256, 256),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul
        );
        
        // Add Scale and Translate transforms -> ImageCanvas
        var transformGroup = new TransformGroup();
        transformGroup.Children.Add(_imageScale);
        transformGroup.Children.Add(_imageRotate);
        transformGroup.Children.Add(_imageTranslate);
            
    
        ImageCanvas.RenderTransform = transformGroup;

        PhotoImage.PointerPressed += OnPointerPressed;
        PhotoImage.PointerMoved += OnPointerMoved;
        PhotoImage.PointerReleased += OnPointerReleased;
        PhotoImage.PointerWheelChanged += OnPointerWheelChanged;
    }
    
    private void ExitProgram_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }

    private async void OpenImage_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var filePicker = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Image",
            AllowMultiple = false,
            FileTypeFilter = new[] {
                new FilePickerFileType("Image Files")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp" }
                }
            }
        });

        if (filePicker.Count > 0)
        {
            _currentImageFile = filePicker[0];
            await using var stream = await _currentImageFile.OpenReadAsync();
            _bitmap = new Bitmap(stream);
            PhotoImage.Source = _bitmap;

            // Reset zoom and pan   
            _zoom = 1.0;
            _imageScale.ScaleX = _imageScale.ScaleY = 1;
            Canvas.SetLeft(PhotoImage, 0);
            Canvas.SetTop(PhotoImage, 0);
        }
    }

    private async void SaveImage_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentImageFile == null || PhotoImage.Source is not Bitmap bitmap)
            return;

        await using var stream = await _currentImageFile.OpenWriteAsync();
        _bitmap.Save(stream); // Overwrites the file
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _lastMousePosition = e.GetPosition(ImageCanvas);
        _isDragging = true;
        PhotoImage.Cursor = new Cursor(StandardCursorType.Hand);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
        PhotoImage.Cursor = new Cursor(StandardCursorType.Arrow);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging)
        {
            var position = e.GetPosition(ImageCanvas);
            var dx = position.X - _lastMousePosition.X;
            var dy = position.Y - _lastMousePosition.Y;

            Canvas.SetLeft(PhotoImage, Canvas.GetLeft(PhotoImage) + dx);
            Canvas.SetTop(PhotoImage, Canvas.GetTop(PhotoImage) + dy);

            _lastMousePosition = position;
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if ((e.KeyModifiers & KeyModifiers.Control) != 0) // Zoom around.
        {
            _zoom += e.Delta.Y * 0.1;
            _zoom = Math.Clamp(_zoom, 0.2, 5);
            
            _imageScale.ScaleX = _imageScale.ScaleY = _zoom;
        }
        else if ((e.KeyModifiers & KeyModifiers.Shift) != 0) // Translate horizontally.
        {
            double offset = e.Delta.Y * 15; // Movement factor
            Console.WriteLine(offset);
        
            _translateX += offset;
            _imageTranslate.X = _translateX;  // Apply translation
        
            // _translateX = Math.Clamp(_translateX, -1000, 1000);
        }
        else // Translate vertically.
        {
            double offset = e.Delta.Y * 15; // Movement factor
        
            _translateY += offset;
            _imageTranslate.Y = _translateY;  // Apply translation
        
            // _translateY = Math.Clamp(_translateY, -1000, 1000);
        }
    }
    private void ZoomIn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _zoom += 1 * 0.1;
        _zoom = Math.Clamp(_zoom, 0.2, 5);
            
        _imageScale.ScaleX = _imageScale.ScaleY = _zoom;
    }
    private void ZoomOut_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _zoom += -1 * 0.1;
        _zoom = Math.Clamp(_zoom, 0.2, 5);
            
        _imageScale.ScaleX = _imageScale.ScaleY = _zoom;
    }
    private void FullScreen_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (WindowState != WindowState.FullScreen) // Go fullscreen
        {
            // Save the current state before going fullscreen
            _previousWindowState = WindowState;
            _previousWidth = Width;
            _previousHeight = Height;
            _previousX = Position.X;
            _previousY = Position.Y;
            
            WindowState = WindowState.FullScreen;
        }
        else // Exit fullscreen and restore previous state
        {
            WindowState = _previousWindowState;
            Width = _previousWidth;
            Height = _previousHeight;
            Position = new PixelPoint((int)_previousX, (int)_previousY);
        }
    }

    private void Rotate90Degrees_Click(object? sender, RoutedEventArgs e)
    {
        _currentRotation += 90;
        if (_currentRotation >= 360)
            _currentRotation = 0;

        _imageRotate.Angle = _currentRotation;
    }
    private void Slider_ValueChanged(object sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        Console.WriteLine("fwefewfwe");
        var slider = sender as Slider;
        Console.WriteLine("fwefewfwe");
        string tag = slider.Tag.ToString();
        double newValue = Math.Round(e.NewValue, 2);
        
        string labelName = $"{tag}Label";
        var label = this.FindControl<TextBlock>(labelName);
        
        int newValueInt = (int)newValue;
        label.Text = $"{newValueInt}";

        // switch (newValue)
        // {
        //     case -1:
        //         label.Text = "-1";
        //         break;
        //     case 0:
        //         label.Text = "0";
        //         break;
        //     case 1:
        //         label.Text = "1";
        //         break;
        //     default:
        //         label.Text = $"{newValue}";
        //         break;
        // }

        switch (tag)
        {
            case "Exposure":
                SetExposure(newValue);
                break;
            case "Brightness":
                SetBrightness(newValue);
                break;
        }
    }

    private void SetExposure(double factor)
    {
        // Assume bitmap is already loaded
        var writeableBitmap = new WriteableBitmap(_bitmap.PixelSize, _bitmap.Dpi, Avalonia.Platform.PixelFormat.Bgra8888, AlphaFormat.Premul);

        using (var fb = writeableBitmap.Lock())
        {
            _bitmap.CopyPixels(new PixelRect(_bitmap.PixelSize), fb.Address, fb.RowBytes * fb.Size.Height, fb.RowBytes);
            
            var buffer = fb.Address;
            int pixelCount = fb.RowBytes * fb.Size.Height / 4;

            unsafe
            {
                var ptr = (uint*) fb.Address;
                // Extract RGBA values
                double scale = 1 + (factor / 100.0f);

                for (int i = 0; i < pixelCount; i++)
                {
                    // Get the color of the top-left pixel
                    uint color = ptr[i];

                    uint blue = (uint)Math.Min(((color & 0xFF) * scale), 255);
                    uint green = (uint)Math.Min((((color >> 8) & 0xFF) * scale), 255);
                    uint red = (uint)Math.Min((((color >> 16) & 0xFF) * scale), 255);

                    uint alpha = (uint)((color >> 24) & 0xFF);
        
                    // Now you have the color components (blue, green, red, alpha)
                    // Console.WriteLine($"Top-left Pixel Color: R={red}, G={green}, B={blue}, A={alpha}");
                
                    // Convert to BGRA8888 format
                    uint bgra = (blue) | (green << 8) | (red << 16) | (alpha << 24);

                    // Write to pointer
                    ptr[i] = bgra;
                }
            }
        }
        PhotoImage.Source = writeableBitmap;
    }

    private void SetBrightness(double amount)
    {
        Console.WriteLine(amount);
        // Assume bitmap is already loaded
        var writeableBitmap = new WriteableBitmap(_bitmap.PixelSize, _bitmap.Dpi, Avalonia.Platform.PixelFormat.Bgra8888, AlphaFormat.Premul);

        using (var fb = writeableBitmap.Lock())
        {
            _bitmap.CopyPixels(new PixelRect(_bitmap.PixelSize), fb.Address, fb.RowBytes * fb.Size.Height, fb.RowBytes);
            
            var buffer = fb.Address;
            int pixelCount = fb.RowBytes * fb.Size.Height / 4;

            unsafe
            {
                var ptr = (uint*) fb.Address;
                
                for (int i = 0; i < pixelCount; i++)
                {
                    // Get the color of the top-left pixel
                    uint color = ptr[i];

                    uint blue = (uint)Math.Min(((color & 0xFF) + amount), 255);
                    uint green = (uint)Math.Min((((color >> 8) & 0xFF) + amount), 255);
                    uint red = (uint)Math.Min((((color >> 16) & 0xFF) + amount), 255);

                    uint alpha = (uint)((color >> 24) & 0xFF);
        
                    // Now you have the color components (blue, green, red, alpha)
                    // Console.WriteLine($"Top-left Pixel Color: R={red}, G={green}, B={blue}, A={alpha}");
                
                    // Convert to BGRA8888 format
                    uint bgra = (blue) | (green << 8) | (red << 16) | (alpha << 24);

                    // Write to pointer
                    ptr[i] = bgra;
                }
            }
        }
        PhotoImage.Source = writeableBitmap;
    }
}