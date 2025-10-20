using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace PracticeLogger.Models
{
    public static class PracticeTypeCatalog
    {
        // Kod → Etikett (kan användas överallt)
        public static readonly IReadOnlyDictionary<byte, string> Labels = new Dictionary<byte, string>
        {
            { (byte)PracticeType.Warmup,    "Uppvärmning" },
            { (byte)PracticeType.Teknik,    "Teknik" },
            { (byte)PracticeType.Skalor,    "Skalor" },
            { (byte)PracticeType.Etyder,    "Etyder" },
            { (byte)PracticeType.Repertoar, "Repertoar" },
            { (byte)PracticeType.Ovrigt,    "Övrigt" }
        };

        // För dropdowns i formulär
        public static IEnumerable<SelectListItem> ToSelectList(byte? selected = null)
        {
            foreach (var kv in Labels)
            {
                yield return new SelectListItem
                {
                    Value = kv.Key.ToString(),
                    Text = kv.Value,
                    Selected = selected.HasValue && kv.Key == selected.Value
                };
            }
        }
    }
}
