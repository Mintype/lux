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

namespace lux;

public partial class MainWindow : Window
{
    private Point _lastMousePosition;
    private bool _isDragging = false;
    private double _zoom = 1.0;
    
    private double _translateX = 0;
    private double _translateY = 0;
    private TranslateTransform _imageTranslate = new TranslateTransform();
    private ScaleTransform _imageScale = new ScaleTransform();

    private WindowState _previousWindowState;
    private double _previousWidth;
    private double _previousHeight;
    private double _previousX;
    private double _previousY;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Add Scale and Translate transforms -> ImageCanvas
        var transformGroup = new TransformGroup();
        transformGroup.Children.Add(_imageScale);
        transformGroup.Children.Add(_imageTranslate);
    
        ImageCanvas.RenderTransform = transformGroup;

        PhotoImage.PointerPressed += OnPointerPressed;
        PhotoImage.PointerMoved += OnPointerMoved;
        PhotoImage.PointerReleased += OnPointerReleased;
        PhotoImage.PointerWheelChanged += OnPointerWheelChanged;
    }

    private void ExitProgram(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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
            var file = filePicker[0];
            await using var stream = await file.OpenReadAsync();
            var bitmap = new Bitmap(stream);
            PhotoImage.Source = bitmap;

            // Reset zoom and pan   
            _zoom = 1.0;
            _imageScale.ScaleX = _imageScale.ScaleY = 1;
            Canvas.SetLeft(PhotoImage, 0);
            Canvas.SetTop(PhotoImage, 0);
        }
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
    private void ZoomIn(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _zoom += 1 * 0.1;
        _zoom = Math.Clamp(_zoom, 0.2, 5);
            
        _imageScale.ScaleX = _imageScale.ScaleY = _zoom;
    }
    private void ZoomOut(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _zoom += -1 * 0.1;
        _zoom = Math.Clamp(_zoom, 0.2, 5);
            
        _imageScale.ScaleX = _imageScale.ScaleY = _zoom;
    }
    private void FullScreen(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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
}