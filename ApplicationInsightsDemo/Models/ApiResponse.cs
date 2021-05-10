namespace ApplicationInsightsDemo.Models
{
    public class ApiResponse
    {
        public string symbol { get; set; }
        public double price_24h { get; set; }
        public double volume_24h { get; set; }
        public double last_trade_price { get; set; }
    }
}