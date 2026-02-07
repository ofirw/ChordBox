using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChordBox.Models;
using ChordBox.ViewModels;

namespace ChordBox;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        _viewModel.ClearFocusRequested += () =>
        {
            // Move keyboard focus to the window so text fields lose focus
            // but the window still receives key events
            Keyboard.Focus(this);
        };

        PreviewKeyDown += Window_PreviewKeyDown;
        PreviewTextInput += Window_PreviewTextInput;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.Dispose();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Global shortcuts (work even in TextBoxes)
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.S:
                    _viewModel.SaveCommand.Execute(null);
                    e.Handled = true;
                    return;
                case Key.Z:
                    _viewModel.UndoCommand.Execute(null);
                    e.Handled = true;
                    return;
                case Key.Y:
                    _viewModel.RedoCommand.Execute(null);
                    e.Handled = true;
                    return;
                case Key.C:
                    if (!(e.OriginalSource is TextBox))
                    {
                        _viewModel.CopyBarCommand.Execute(null);
                        e.Handled = true;
                    }
                    return;
                case Key.V:
                    if (!(e.OriginalSource is TextBox))
                    {
                        _viewModel.PasteBarCommand.Execute(null);
                        e.Handled = true;
                    }
                    return;
            }
        }

        // Don't intercept if focus is in a TextBox (lyrics, song title, etc.)
        if (e.OriginalSource is TextBox) return;

        if (_viewModel.HandleInlineKeyDown(e.Key))
            e.Handled = true;
    }

    private void Window_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Don't intercept if focus is in a TextBox
        if (e.OriginalSource is TextBox) return;

        if (_viewModel.SelectedBar != null && _viewModel.SelectedBar.IsEditing
            && !_viewModel.IsChordPickerOpen && !_viewModel.IsLoopEditorOpen)
        {
            _viewModel.HandleInlineTextInput(e.Text);
            e.Handled = true;
        }
    }

    private void PickerOverlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _viewModel.ClosePickerCommand.Execute(null);
        _viewModel.CancelLoopEditCommand.Execute(null);
        if (_viewModel.IsComposingHelpOpen)
            _viewModel.ToggleComposingHelpCommand.Execute(null);
        if (_viewModel.IsStrumPatternEditorOpen)
            _viewModel.IsStrumPatternEditorOpen = false;
    }

    private void PickerPanel_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void RecentSF2_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.ComboBox cb && cb.SelectedItem is string path)
        {
            _viewModel.LoadRecentSoundFontCommand.Execute(path);
            cb.SelectedIndex = -1; // reset so same item can be re-selected
        }
    }

    private void LyricsBox_GotFocus(object sender, RoutedEventArgs e)
    {
        // When a lyrics TextBox gets focus, deselect the bar
        _viewModel.StopInlineEditing();
        _viewModel.SelectedBar = null;
    }

    private void LyricsBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb) return;

        // Resolve bar index from either LyricsLineViewModel or BarViewModel DataContext
        int barIndex = -1;
        if (tb.DataContext is LyricsLineViewModel line)
            barIndex = line.ParentBar.Index;
        else if (tb.DataContext is BarViewModel bar)
            barIndex = bar.Index;
        if (barIndex < 0) return;

        bool shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

        if (e.Key == Key.Tab)
        {
            e.Handled = true;
            int targetIdx = shift ? barIndex - 1 : barIndex + 1;
            if (targetIdx >= 0 && targetIdx < _viewModel.Bars.Count)
                FocusLyricsBox(targetIdx);
        }
    }

    private void FocusLyricsBox(int barIndex)
    {
        var outerItemsControl = FindVisualChild<ItemsControl>(_barGrid);
        if (outerItemsControl == null) return;
        var container = outerItemsControl.ItemContainerGenerator.ContainerFromIndex(barIndex);
        if (container == null) return;
        // Find the first LyricsBox inside this bar's template
        var lyricsBox = FindVisualChild<TextBox>(container, "LyricsBox");
        lyricsBox?.Focus();
    }

    private bool _suppressStrumEventChanged;

    private void StrumEvent_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressStrumEventChanged) return;
        if (!_viewModel.IsStrumPatternEditorOpen) return;
        if (e.AddedItems.Count == 0) return; // not a real user change

        // The ComboBox has already pushed the new value into the StrumEvent
        // via two-way binding. We just need to ensure we're on a custom pattern
        // and refresh the summary. If clone happens, the current event objects
        // (which already have the new values) get cloned too.
        _viewModel.EnsureCustomStrumPattern();
        OnPropertyChanged_Summary();
    }

    private void OnPropertyChanged_Summary()
    {
        // Just refresh summary without re-syncing the collection (avoids losing ComboBox state)
        _viewModel.RefreshStrumSummary();
    }

    private void StrumEventRemove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is StrumEvent ev)
        {
            // Find index in the model's event list
            var modelEvents = _viewModel.SelectedStrumPattern?.Events;
            if (modelEvents == null) return;
            int idx = modelEvents.IndexOf(ev);
            if (idx < 0 || modelEvents.Count <= 1) return;

            _viewModel.EnsureCustomStrumPattern();
            // Re-resolve after possible clone
            _viewModel.SelectedStrumPattern.Events.RemoveAt(idx);
            _suppressStrumEventChanged = true;
            _viewModel.RefreshStrumEditor();
            _suppressStrumEventChanged = false;
        }
    }

    private void StrumSave_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.EnsureCustomStrumPattern();
        _viewModel.SaveCustomStrumPatterns();
        _viewModel.StatusText = $"Saved strum pattern: {_viewModel.CustomStrumName}";
    }

    private static T? FindVisualChild<T>(DependencyObject parent, string? name = null) where T : FrameworkElement
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T t && (name == null || t.Name == name))
                return t;
            var result = FindVisualChild<T>(child, name);
            if (result != null) return result;
        }
        return null;
    }
}