using System.Collections.Generic;

static partial class ListExtensions
{
    public static List<JSONStorable> Prune(this List<JSONStorable> list)
    {
        list?.RemoveAll(storable => !storable || !storable.containingAtom);
        return list;
    }
}
