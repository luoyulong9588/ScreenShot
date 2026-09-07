using System;
using System.Windows;
using System.Windows.Input;

namespace ScreenShot
{
    public partial class HotkeySettingWindow : Window
    {
        private bool _isRecording;
        private Key _capturedKey = Key.None;

        public HotkeyConfig Config { get; private set; }

        public HotkeySettingWindow(HotkeyConfig current)
        {
            InitializeComponent();
            Config = current;
            ChkCtrl.IsChecked = current.Ctrl;
            ChkShift.IsChecked = current.Shift;
            ChkAlt.IsChecked = current.Alt;
            TxtKey.Text = current.Key.ToString();
            _capturedKey = current.Key;
        }

        private void Record_Click(object sender, RoutedEventArgs e)
        {
            _isRecording = true;
            _capturedKey = Key.None;
            TxtHint.Text = "请按下新的快捷键...";
            TxtHint.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Colors.DodgerBlue);
            Keyboard.Focus(TxtKey);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!_isRecording) return;

            e.Handled = true;
            var key = e.Key;

            if (key == Key.System)
                key = e.SystemKey;

            if (key == Key.None ||
                key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin)
                return;

            _capturedKey = key;
            TxtKey.Text = key.ToString();
            _isRecording = false;
            TxtHint.Text = "点击「录制」可重新设置";
            TxtHint.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x80, 0x80, 0x80));
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (_capturedKey == Key.None)
            {
                MessageBox.Show("请先录制一个快捷键", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ChkCtrl.IsChecked == true && !ChkShift.IsChecked == true && !ChkAlt.IsChecked == true)
            {
                MessageBox.Show("请至少勾选一个修饰键（Ctrl/Shift/Alt）", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Config = new HotkeyConfig
            {
                Key = _capturedKey,
                Ctrl = ChkCtrl.IsChecked == true,
                Shift = ChkShift.IsChecked == true,
                Alt = ChkAlt.IsChecked == true
            };

            Config.Save();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
