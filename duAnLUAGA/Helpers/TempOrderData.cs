using Microsoft.AspNetCore.Http;
using System.Text.Json;
using duAnLUAGA.ViewModel;

namespace duAnLUAGA.Helpers
{
    public class TempOrderData
    {
        public int UserId { get; set; }
        public int? MaDc { get; set; }
        public string MaVoucher { get; set; }
        public decimal TongTien { get; set; }
        public decimal PhiShip { get; set; }
        public decimal GiamGia { get; set; }
        public decimal TongThanhToan { get; set; }
        public List<CartItemVM> Items { get; set; }
    }

    public static class SessionExtensions
    {
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        } 
    }
}
