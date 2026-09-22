using YG;

public static class GetMetrica
{
    public static void Event(string name) => YG2.MetricaSend(name);
    public static void Event(string name, string key, string value) => YG2.MetricaSend(name, key, value);

    public static void CarBought(int segment, float price) => YG2.MetricaSend("car_bought", "segment", segment.ToString());
    public static void CarSold(int segment, float price) => YG2.MetricaSend("car_sold", "segment", segment.ToString());
    public static void HaggleResult(bool success) => YG2.MetricaSend("haggle", "result", success ? "success" : "fail");
}