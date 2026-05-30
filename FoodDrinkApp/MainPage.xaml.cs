using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace FoodApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        // 推荐功能（含验证与错误处理）
        private void OnRecommendClicked(object sender, EventArgs e)
        {
            string food = FoodEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(food))
            {
                ResultLabel.Text = "⚠️ 请输入食物名称！";
                return;
            }

            // 简单的推荐逻辑
            string recommendation = food switch
            {
                "披萨" => "🍕 推荐搭配：一杯可乐 + 一份沙拉",
                "沙拉" => "🥗 推荐搭配：柠檬水 + 全麦面包",
                "咖啡" => "☕ 推荐搭配：一块蛋糕 或 羊角面包",
                _ => $"📖 推荐：尝试用 {food} 做一道创意菜肴！"
            };

            ResultLabel.Text = recommendation;
        }

        // 硬件1：文本转语音
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
                await DisplayAlert("错误", "语音失败：" + ex.Message, "OK");
            }
        }

        // 硬件2：获取地理位置（查找附近餐厅）
        private async void OnNearbyClicked(object sender, EventArgs e)
        {
            try
            {
                // 请求位置权限
                PermissionStatus status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("权限不足", "无法获取您的位置，请在设置中授予位置权限", "OK");
                    return;
                }

                // 获取当前位置
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
    }
}