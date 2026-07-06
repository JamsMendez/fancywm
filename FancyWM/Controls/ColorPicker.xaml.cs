using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using FancyWM.Utilities;

namespace FancyWM.Controls
{
    /// <summary>
    /// Interaction logic for ColorPicker.xaml
    /// </summary>
    public partial class ColorPicker : UserControl
    {
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            nameof(Color),
            typeof(Color),
            typeof(ColorPicker),
            new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColorChanged));

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        private double m_hue;
        private double m_saturation;
        private double m_value;
        private byte m_alpha = 255;
        private bool m_isApplyingColor;

        public ColorPicker()
        {
            InitializeComponent();
            SizeChanged += (_, _) => UpdateThumbPositions();
            SyncFromColor(Color);
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = (ColorPicker)d;
            if (!picker.m_isApplyingColor)
            {
                picker.SyncFromColor((Color)e.NewValue);
            }
        }

        private void SyncFromColor(Color color)
        {
            var (hue, saturation, value) = color.ToHsv();
            m_hue = hue;
            m_saturation = saturation;
            m_value = value;
            m_alpha = color.A;
            UpdateVisuals();
        }

        private void ApplyColor()
        {
            var color = ColorExtensions.FromHsv(m_hue, m_saturation, m_value, m_alpha);
            m_isApplyingColor = true;
            Color = color;
            m_isApplyingColor = false;
            UpdateVisuals();
        }

        private bool IsDragging => SvArea.IsMouseCaptured || HueTrack.IsMouseCaptured || AlphaTrack.IsMouseCaptured;

        private void UpdateVisuals()
        {
            SvHueBrush.Color = ColorExtensions.FromHsv(m_hue, 1, 1);
            AlphaStopTransparent.Color = ColorExtensions.FromHsv(m_hue, m_saturation, m_value, 0);
            AlphaStopOpaque.Color = ColorExtensions.FromHsv(m_hue, m_saturation, m_value, 255);
            PreviewBrush.Color = Color;

            // Updating the TextBox's Text is comparatively expensive (undo stack, layout, TextChanged),
            // so skip it while dragging and only refresh once the drag ends.
            if (!HexTextBox.IsKeyboardFocused && !IsDragging)
            {
                HexTextBox.Text = Color.ToString();
            }

            UpdateThumbPositions();
        }

        private void UpdateThumbPositions()
        {
            if (SvArea.ActualWidth > 0 && SvArea.ActualHeight > 0)
            {
                SvThumb.Margin = new Thickness(
                    m_saturation * SvArea.ActualWidth - SvThumb.Width / 2,
                    (1 - m_value) * SvArea.ActualHeight - SvThumb.Height / 2,
                    0, 0);
            }

            if (HueTrack.ActualWidth > 0)
            {
                HueThumb.Margin = new Thickness(m_hue / 360.0 * HueTrack.ActualWidth - HueThumb.Width / 2, 0, 0, 0);
            }

            if (AlphaTrack.ActualWidth > 0)
            {
                AlphaThumb.Margin = new Thickness(m_alpha / 255.0 * AlphaTrack.ActualWidth - AlphaThumb.Width / 2, 0, 0, 0);
            }
        }

        private static double Clamp01(double value) => Math.Clamp(value, 0, 1);

        private void OnSvMouseDown(object sender, MouseButtonEventArgs e)
        {
            SvArea.CaptureMouse();
            UpdateSvFromPoint(e.GetPosition(SvArea));
        }

        private void OnSvMouseMove(object sender, MouseEventArgs e)
        {
            if (SvArea.IsMouseCaptured)
            {
                UpdateSvFromPoint(e.GetPosition(SvArea));
            }
        }

        private void OnSvMouseUp(object sender, MouseButtonEventArgs e)
        {
            SvArea.ReleaseMouseCapture();
            UpdateVisuals();
        }

        private void UpdateSvFromPoint(Point point)
        {
            if (SvArea.ActualWidth <= 0 || SvArea.ActualHeight <= 0)
            {
                return;
            }

            m_saturation = Clamp01(point.X / SvArea.ActualWidth);
            m_value = 1 - Clamp01(point.Y / SvArea.ActualHeight);
            ApplyColor();
        }

        private void OnHueMouseDown(object sender, MouseButtonEventArgs e)
        {
            HueTrack.CaptureMouse();
            UpdateHueFromPoint(e.GetPosition(HueTrack));
        }

        private void OnHueMouseMove(object sender, MouseEventArgs e)
        {
            if (HueTrack.IsMouseCaptured)
            {
                UpdateHueFromPoint(e.GetPosition(HueTrack));
            }
        }

        private void OnHueMouseUp(object sender, MouseButtonEventArgs e)
        {
            HueTrack.ReleaseMouseCapture();
            UpdateVisuals();
        }

        private void UpdateHueFromPoint(Point point)
        {
            if (HueTrack.ActualWidth <= 0)
            {
                return;
            }

            m_hue = Clamp01(point.X / HueTrack.ActualWidth) * 360;
            ApplyColor();
        }

        private void OnAlphaMouseDown(object sender, MouseButtonEventArgs e)
        {
            AlphaTrack.CaptureMouse();
            UpdateAlphaFromPoint(e.GetPosition(AlphaTrack));
        }

        private void OnAlphaMouseMove(object sender, MouseEventArgs e)
        {
            if (AlphaTrack.IsMouseCaptured)
            {
                UpdateAlphaFromPoint(e.GetPosition(AlphaTrack));
            }
        }

        private void OnAlphaMouseUp(object sender, MouseButtonEventArgs e)
        {
            AlphaTrack.ReleaseMouseCapture();
            UpdateVisuals();
        }

        private void UpdateAlphaFromPoint(Point point)
        {
            if (AlphaTrack.ActualWidth <= 0)
            {
                return;
            }

            m_alpha = (byte)Math.Round(Clamp01(point.X / AlphaTrack.ActualWidth) * 255);
            ApplyColor();
        }

        private void OnHexTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommitHexText();
                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }

        private void OnHexTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            CommitHexText();
        }

        private void CommitHexText()
        {
            try
            {
                var text = HexTextBox.Text.Trim();
                if (!text.StartsWith('#'))
                {
                    text = "#" + text;
                }

                if (System.Windows.Media.ColorConverter.ConvertFromString(text) is Color color)
                {
                    SyncFromColor(color);
                    ApplyColor();
                    return;
                }
            }
            catch (Exception)
            {
            }

            // Invalid input, revert to the current color's text.
            HexTextBox.Text = Color.ToString();
        }
    }
}
