using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace NotepadCommander.UI.Views.Components;

public partial class BreadcrumbBar : UserControl
{
    public static readonly StyledProperty<string?> FilePathProperty =
        AvaloniaProperty.Register<BreadcrumbBar, string?>(nameof(FilePath));

    public string? FilePath
    {
        get => GetValue(FilePathProperty);
        set => SetValue(FilePathProperty, value);
    }

    public BreadcrumbBar()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FilePathProperty)
            UpdateSegments();
    }

    private void UpdateSegments()
    {
        var panel = this.FindControl<StackPanel>("SegmentsPanel");
        if (panel == null) return;

        panel.Children.Clear();

        var path = FilePath;
        if (string.IsNullOrEmpty(path))
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Sans titre",
                FontSize = 11,
                Foreground = (IBrush?)this.FindResource("TextMutedBrush") ?? Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center
            });
            return;
        }

        var segments = path.Replace('/', '\\').Split('\\', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segments.Length; i++)
        {
            if (i > 0)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = ">",
                    FontSize = 10,
                    Foreground = (IBrush?)this.FindResource("TextMutedBrush") ?? Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2, 0)
                });
            }

            var isLast = i == segments.Length - 1;
            var tb = new TextBlock
            {
                Text = segments[i],
                FontSize = 11,
                FontWeight = isLast ? FontWeight.SemiBold : FontWeight.Normal,
                Foreground = isLast
                    ? ((IBrush?)this.FindResource("TextPrimaryBrush") ?? Brushes.Black)
                    : ((IBrush?)this.FindResource("TextSecondaryBrush") ?? Brushes.Gray),
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(tb);
        }
    }
}
