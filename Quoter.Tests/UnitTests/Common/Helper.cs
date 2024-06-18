using Newtonsoft.Json;

namespace Quoter.Tests.UnitTests.Common
{
    public static class Helper
    {
        public static string ToJson<T>(this T obj) where T : class
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}
