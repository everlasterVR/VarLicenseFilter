using System.Collections.Generic;
using System.Text.RegularExpressions;

static partial class AtomExtensions
{
    public static List<JSONStorable> FindStorablesByRegexMatch(this Atom atom, Regex regex)
    {
        return FindStorablesByRegexMatchInternal(atom, regex).ToList().Prune();
    }

    static IEnumerable<JSONStorable> FindStorablesByRegexMatchInternal(Atom atom, Regex regex)
    {
        var storableIds = atom.GetStorableIDs();
        foreach(string id in storableIds)
        {
            if(regex.IsMatch(id))
            {
                yield return atom.GetStorableByID(id);
            }
        }
    }
}
