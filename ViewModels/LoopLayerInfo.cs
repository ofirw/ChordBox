using System.Windows.Media;

namespace ChordBox.ViewModels;

public class LoopLayerInfo
{
    public SolidColorBrush Brush { get; set; } = Brushes.Transparent;
    public bool IsStart { get; set; }
    public bool IsEnd { get; set; }
    public string Name { get; set; } = "";
    public int RepeatCount { get; set; }
    public string NameDisplay => !string.IsNullOrWhiteSpace(Name) ? Name : "";
    public string RepeatText => RepeatCount > 0 ? $"×{RepeatCount}" : "";
    public string StartVisibility => IsStart ? "Visible" : "Collapsed";
    public string EndVisibility => IsEnd ? "Visible" : "Collapsed";
}
