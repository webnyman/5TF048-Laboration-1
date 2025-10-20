using Microsoft.AspNetCore.Mvc.Rendering;

public static class PracticeTypeCatalog
{
    private static readonly (byte Id, string Name)[] _items =
    {
        (1, "Uppvärmning"),
        (2, "Teknik"),
        (3, "Skalor"),
        (4, "Etyder"),
        (5, "Repertoar"),
        (6, "Övrigt")
    };

    // Ny version för SelectList
    public static IEnumerable<SelectListItem> ToSelectList(byte? selected = null)
        => _items.Select(i => new SelectListItem
        {
            Text = i.Name,
            Value = i.Id.ToString(),
            Selected = selected == i.Id
        });

    // Hjälpmetod för att slå upp namn
    public static string? NameOf(byte? id)
        => _items.FirstOrDefault(x => x.Id == id).Name;

    // 👇 Lägg till denna för bakåtkompatibilitet
    public static readonly Dictionary<byte, string> Labels =
        _items.ToDictionary(x => x.Id, x => x.Name);
}
