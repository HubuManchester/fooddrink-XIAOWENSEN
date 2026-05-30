using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;

namespace FoodApp
{
    public partial class MainPage : ContentPage
    {
        private bool isShaking = false;  // 防止加速度计重复触发

        public MainPage()
        {
            InitializeComponent();

            // 初始化加速度计（摇一摇）
            if (Accelerometer.IsSupported)
            {
                Accelerometer.ReadingChanged += OnAccelerometerReadingChanged;
                Accelerometer.Start(SensorSpeed.UI);
            }
        }

        // ========== 核心功能：推荐 + 验证 ==========
        private void OnRecommendClicked(object sender, EventArgs e)
        {
            string food = FoodEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(food))
            {
                ResultLabel.Text = "⚠️ 请输入食物名称！";
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
        }

        // ========== 硬件1：文本转语音 ==========
        private async void OnSpeakClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResultLabel.Text) || ResultLabel.Text.StartsWith("⚠️"))
            {
                await DisplayAlert("提示", "请先点击「推荐食谱」按钮，获得推荐内容", "OK");
                return;
            }

            try
            {
                await TextToSpeech.Default.SpeakAsync(ResultLabel.Text);
            }
            catch (Exception ex)
            {
                // 不在弹窗，只在结果标签上提示，不影响体验
                ResultLabel.Text = "⚠️ 语音功能暂不可用（模拟器缺少TTS引擎）";
                System.Diagnostics.Debug.WriteLine($"TTS错误: {ex.Message}");
            }
        }

        // ========== 硬件2：地理位置 ==========
        private async void OnNearbyClicked(object sender, EventArgs e)
        {
            try
            {
                PermissionStatus status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("权限不足", "无法获取您的位置，请在设置中授予位置权限", "OK");
                    return;
                }

                Location location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(10)
                });

                if (location != null)
                {
                    ResultLabel.Text = $"📍 您的位置：纬度 {location.Latitude:F2}，经度 {location.Longitude:F2}\n" +
                                       "附近餐厅示例：星巴克、麦当劳、海底捞（根据实际位置有所不同）";
                }
                else
                {
                    ResultLabel.Text = "无法获取位置，请确保设备定位已开启（模拟器中可手动设置坐标）";
                }
            }
            catch (FeatureNotSupportedException)
            {
                ResultLabel.Text = "设备不支持地理位置功能";
            }
            catch (PermissionException)
            {
                ResultLabel.Text = "位置权限被拒绝，无法获取位置";
            }
            catch (Exception ex)
            {
                ResultLabel.Text = $"位置获取错误：{ex.Message}";
            }
        }

        // ========== 硬件3：加速度计（摇一摇随机推荐）==========
        private void OnAccelerometerReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            var data = e.Reading.Acceleration;
            // 检测摇动：任意轴向加速度超过 1.8 且不在冷却中
            if (!isShaking && (Math.Abs(data.X) > 1.8 || Math.Abs(data.Y) > 1.8 || Math.Abs(data.Z) > 1.8))
            {
                isShaking = true;
                // 在主线程更新 UI
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    string[] foods = { "披萨", "沙拉", "咖啡", "寿司", "汉堡", "意面", "牛排" };
                    Random rnd = new Random();
                    FoodEntry.Text = foods[rnd.Next(foods.Length)];
                    OnRecommendClicked(null, null);
                });
                // 冷却 1 秒
                Task.Delay(1000).ContinueWith(_ => isShaking = false);
            }
        }

        // 硬件4：摄像头拍照（修复流关闭异常）
        private async void OnTakePhotoClicked(object sender, EventArgs e)
        {
            try
            {
                // 请求相机权限
                var status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("权限不足", "无法使用相机，请在设置中授予权限", "OK");
                    return;
                }

                // 调用系统相机
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    // 将照片读入内存流，避免流被提前释放
                    using var stream = await photo.OpenReadAsync();
                    var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0; // 重置位置
                    CapturedImage.Source = ImageSource.FromStream(() => memoryStream);
                    ResultLabel.Text = "📷 已拍照！您可以输入食物名称并点击推荐。";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("拍照错误", ex.Message, "OK");
            }
        }

        // ========== 可访问性：手动切换主题（亮色/暗色）==========
        private void OnToggleThemeClicked(object sender, EventArgs e)
        {
            App.Current.UserAppTheme = App.Current.UserAppTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        }

        // ========== 页面销毁时释放加速度计资源 ==========
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (Accelerometer.IsSupported)
            {
                Accelerometer.ReadingChanged -= OnAccelerometerReadingChanged;
                Accelerometer.Stop();
            }
        }
        private void OnSimulateShakeClicked(object sender, EventArgs e)
        {
            string[] foods = { "披萨", "沙拉", "咖啡", "寿司", "汉堡", "意面", "牛排" };
            Random rnd = new Random();
            FoodEntry.Text = foods[rnd.Next(foods.Length)];
            OnRecommendClicked(null, null);
        }
    }
}