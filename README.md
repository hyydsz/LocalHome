# LocalHome

## 前言
LocalHome是集成了米家,涂鸦,DDCCI功能的电脑程序

## 功能
但是由于我是个J,目前只完成了:
* 涂鸦插座的开关
* DDCCI控制屏幕亮度
* 米家台灯控制<br>
这三个功能

## 原理
米家通过局域网MIIO协议控制
涂鸦通过涂鸦开发者平台控制
米家可以通过输入账号密码来获取设备信息

## 使用教程
0. 准备条件: 你需要有个涂鸦插座或者米家台灯
1. 打开程序点击右上角加号
2. 输入小米账号密码然后点击确认
3. 就可以控制你的台灯了 [没软用:( ]

## 注意事项
程序打开并导入设备后程序会在 C:\ProgramData\LocalHome 创建一个数据文件

## 借鉴名单
[Xiaomi-extractor](https://github.com/PiotrMachowski/Xiaomi-cloud-tokens-extractor)<br>
[miio-by-C#](https://github.com/xcray/miio-by-CSharp)
