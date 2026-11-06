using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Kono.Menu.Domain;

namespace Kono.Infrastructure.Services.MenuItems;

public static class MenuHasher
{
    public static string Compute(IEnumerable<MenuItem> items)
    {
        var builder = new StringBuilder();
        foreach (var item in items.OrderBy(i => i.Id))
        {
            builder.Append(item.Id).Append('|')
                .Append(item.Name).Append('|')
                .Append(item.Price.ToString("F2", CultureInfo.InvariantCulture)).Append('|')
                .Append(item.IsDrink).Append('\n');
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
