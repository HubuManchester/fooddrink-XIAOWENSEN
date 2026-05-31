using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;
using Microsoft.Maui.Accessibility;

namespace FoodApp
{
    public partial class MainPage : ContentPage
    {
        private bool isShaking = false;

        public MainPage()
        {
            InitializeComponent();

            if (Accelerometer.IsSupported)
            {
                Accelerometer.ReadingChanged += OnAccelerometerReadingChanged!;
                Accelerometer.Start(SensorSpeed.UI);
            }
        }

        private void OnRecommendClicked(object? sender, EventArgs? e)
        {
            string? food = FoodEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(food))
            {
                ResultLabel.Text = "⚠️ 请输入食物名称！";
                SemanticScreenReader.Announce(ResultLabel.Text);
                return;
            }

            string recommendation = food switch
            {
                "披萨" => "🍕 推荐搭配：一杯可乐 + 一份沙拉",
                "沙拉" => "🥗 推荐搭配：柠檬水 + 全麦面包",
                "咖啡" => "☕ 推荐搭配：一块蛋糕 或 羊角面包",
                _ => $"📖 推荐：尝试用 {food} 做一道创意菜肴！"
            };

            ResultLabel.Text = recommendation;
            SemanticScreenReader.Announce(recommendation);
            ResultLabel.Focus();
        }

        private async void OnSpeakClicked(object? sender, EventArgs? e)
        {
            if (string.IsNullOrWhiteSpace(ResultLabel.Text) || ResultLabel.Text.StartsWith("⚠️"))
            {
                await DisplayAlert("提示", "请先点击「推荐食谱」按钮，获得推荐内容", "OK");
                SemanticScreenReader.Announce("请先获得推荐内容");
                return;
            }

            try
            {
                await TextToSpeech.Default.SpeakAsync(ResultLabel.Text);
            }
            catch (Exception ex)
            {
                ResultLabel.Text = "⚠️ 语音功能暂不可用（模拟器缺少TTS引擎）";
                SemanticScreenReader.Announce(ResultLabel.Text);
                System.Diagnostics.Debug.WriteLine($"TTS错误: {ex.Message}");
            }
        }

        private async void OnNearbyClicked(object? sender, EventArgs? e)
        {
            try
            {
                PermissionStatus status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("权限不足", "无法获取您的位置，请在设置中授予位置权限", "OK");
                    SemanticScreenReader.Announce("位置权限被拒绝");
                    return;
                }

                Location? location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(10)
                });

                if (location != null)
                {
                    ResultLabel.Text = $"📍 您的位置：纬度 {location.Latitude:F2}，经度 {location.Longitude:F2}\n" +
                                       "附近餐厅示例：星巴克、麦当劳、海底捞（根据实际位置有所不同）";
                    SemanticScreenReader.Announce("已获取到您的位置，并显示了附近餐厅示例");
                }
                else
                {
                    ResultLabel.Text = "无法获取位置，请确保设备定位已开启（模拟器中可手动设置坐标）";
                    SemanticScreenReader.Announce(ResultLabel.Text);
                }
            }
            catch (FeatureNotSupportedException)
            {
                ResultLabel.Text = "设备不支持地理位置功能";
                SemanticScreenReader.Announce(ResultLabel.Text);
            }
            catch (PermissionException)
            {
                ResultLabel.Text = "位置权限被拒绝，无法获取位置";
                SemanticScreenReader.Announce(ResultLabel.Text);
            }
            catch (Exception ex)
            {
                ResultLabel.Text = $"位置获取错误：{ex.Message}";
                SemanticScreenReader.Announce(ResultLabel.Text);
            }
        }

        private void OnAccelerometerReadingChanged(object? sender, AccelerometerChangedEventArgs e)
        {
            var data = e.Reading.Acceleration;
            if (!isShaking && (Math.Abs(data.X) > 1.8 || Math.Abs(data.Y) > 1.8 || Math.Abs(data.Z) > 1.8))
            {
                isShaking = true;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    string[] foods = { "披萨", "沙拉", "咖啡", "寿司", "汉堡", "意面", "牛排" };
                    Random rnd = new Random();
                    FoodEntry.Text = foods[rnd.Next(foods.Length)];
                    OnRecommendClicked(null, null);
                    SemanticScreenReader.Announce("摇一摇触发，已随机选择食物并推荐");
                });
                Task.Delay(1000).ContinueWith(_ => isShaking = false);
            }
        }

        private async void OnTakePhotoClicked(object? sender, EventArgs? e)
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("权限不足", "无法使用相机，请在设置中授予权限", "OK");
                    SemanticScreenReader.Announce("相机权限被拒绝");
                    return;
                }

                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    using var stream = await photo.OpenReadAsync();
                    var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    CapturedImage.Source = ImageSource.FromStream(() => memoryStream);
                    ResultLabel.Text = "📷 已拍照！您可以输入食物名称并点击推荐。";
                    SemanticScreenReader.Announce(ResultLabel.Text);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("拍照错误", ex.Message, "OK");
                SemanticScreenReader.Announce("拍照失败，请重试");
            }
        }

        private void OnToggleThemeClicked(object? sender, EventArgs? e)
        {
            if (App.Current != null)
            {
                App.Current.UserAppTheme = App.Current.UserAppTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
                SemanticScreenReader.Announce($"已切换为{(App.Current.UserAppTheme == AppTheme.Dark ? "暗色" : "亮色")}主题");
            }
        }

        private async void OnHelpClicked(object? sender, EventArgs? e)
        {
            string helpText =
                "📱 美食助手使用说明：\n\n" +
                "1. 在输入框中输入食物名称（如披萨、沙拉、咖啡）\n" +
                "2. 点击“推荐食谱/搭配”获得建议\n" +
                "3. 点击“语音读出推荐”可听到结果（需要TTS引擎）\n" +
                "4. 点击“查找附近餐厅”获取当前位置（模拟器中可手动设置坐标）\n" +
                "5. 点击“拍照识别食物”拍下食物，然后手动输入名称再推荐\n" +
                "6. 摇动设备（或点击模拟摇一摇）随机推荐一种食物\n" +
                "7. 点击“切换主题”适应亮光/暗光环境\n\n" +
                "🔊 屏幕阅读器用户：所有按钮均已添加描述，滑动即可聚焦。";

            await DisplayAlert("使用帮助", helpText, "知道了");
            SemanticScreenReader.Announce("帮助对话框已打开");
        }

        private void OnSimulateShakeClicked(object? sender, EventArgs? e)
        {
            string[] foods = { "披萨", "沙拉", "咖啡", "寿司", "汉堡", "意面", "牛排" };
            Random rnd = new Random();
            FoodEntry.Text = foods[rnd.Next(foods.Length)];
            OnRecommendClicked(null, null);
            SemanticScreenReader.Announce("模拟摇一摇，已随机选择食物并推荐");
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (Accelerometer.IsSupported)
            {
                Accelerometer.ReadingChanged -= OnAccelerometerReadingChanged;
                Accelerometer.Stop();
            }
        }
    }
}