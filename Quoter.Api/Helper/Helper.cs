using Newtonsoft.Json;

namespace Quoter.Api.Helper
{
    public static class Helper
    {
        public static string ToJson<T>(this T obj) where T : class
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}
