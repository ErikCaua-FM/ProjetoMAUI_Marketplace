using System;
using System.Collections.Generic;
using System.Text;


namespace VanderShop.Helpers;

public static class ApiConfig
{
    public static string BaseUrl =>
        DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5080/api/"
            : "http://localhost:5080/api/";
}