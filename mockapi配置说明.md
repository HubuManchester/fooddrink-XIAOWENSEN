# mockapi.io 数据源配置说明

当前项目已经支持从 mockapi.io 读取和新增食品数据。

## 1. 当前数据来源

项目原来使用的是本地 mock 数据，位置在：

```text
FoodDrinkApp/Services/FoodCatalogService.cs
```

现在已改成：

- 优先使用 mockapi.io REST API
- 如果没有配置 API 地址，或网络暂时不可用，则使用本地兜底数据，保证 App 不会崩溃

## 2. 在 mockapi.io 创建数据表

1. 打开 [mockapi.io](https://mockapi.io)
2. 新建或进入一个 Project
3. 点击添加 Resource
4. Resource 名称建议填写：

```text
foods
```

5. 添加以下字段：

| 字段名 | 类型建议 | 说明 |
|---|---|---|
| name | String | 食品或饮品名称 |
| category | String | 分类，例如 早餐、午餐、饮品 |
| description | String | 描述 |
| calories | Number | 热量 |
| protein | Number | 蛋白质 |
| carbs | Number | 碳水 |
| fat | Number | 脂肪 |
| allergyNote | String | 过敏提示 |
| tags | String | 搜索标签 |

`id` 字段由 mockapi.io 自动生成，不需要手动添加。

## 3. 示例数据

可以在 mockapi.io 中添加类似数据：

```json
{
  "name": "莓果酸奶碗",
  "category": "早餐",
  "description": "希腊酸奶搭配混合莓果、燕麦和少量蜂蜜。",
  "calories": 340,
  "protein": 24,
  "carbs": 42,
  "fat": 8,
  "allergyNote": "含乳制品和麸质。",
  "tags": "健康 早餐 酸奶 莓果"
}
```

```json
{
  "name": "鸡胸糙米餐盒",
  "category": "午餐",
  "description": "烤鸡胸、糙米、菠菜、黄瓜和柠檬调味汁。",
  "calories": 520,
  "protein": 38,
  "carbs": 58,
  "fat": 14,
  "allergyNote": "未记录常见过敏原。",
  "tags": "备餐 蛋白质 午餐"
}
```

## 4. 把 API 地址填入项目

创建 Resource 后，mockapi.io 会生成类似这样的地址：

```text
https://682xxxx.mockapi.io/api/v1/foods
```

打开：

```text
FoodDrinkApp/Services/MockApiConfig.cs
```

把 `EndpointUrl` 改成你的地址：

```csharp
public const string EndpointUrl = "https://682xxxx.mockapi.io/api/v1/foods";
```

重新运行 App 后：

- 首页列表会从 mockapi.io 获取食品数据
- 添加记录页保存时会 POST 到 mockapi.io
- 详情页会通过 API 获取单条数据

## 5. 录屏时如何说明

可以这样讲：

> 项目最开始使用本地 mock 数据。为了更符合真实移动应用的数据来源，我将数据层改成了 mockapi.io REST API。`FoodCatalogService` 使用 HttpClient 获取食品列表、添加新记录和获取详情。如果网络不可用，应用会使用本地兜底数据，避免演示时崩溃。
