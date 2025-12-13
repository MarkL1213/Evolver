using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace EvolverCore
{
    public class SessionHoursCollection : IDictionary<string, SessionHours>
    {
        Dictionary<string, SessionHours> _sessions = new Dictionary<string, SessionHours>();

        public SessionHours this[string key] { get => ((IDictionary<string, SessionHours>)_sessions)[key]; set => ((IDictionary<string, SessionHours>)_sessions)[key] = value; }

        public ICollection<string> Keys => ((IDictionary<string, SessionHours>)_sessions).Keys;

        public ICollection<SessionHours> Values => ((IDictionary<string, SessionHours>)_sessions).Values;

        public int Count => ((ICollection<KeyValuePair<string, SessionHours>>)_sessions).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, SessionHours>>)_sessions).IsReadOnly;

        public void Add(string key, SessionHours value)
        {
            ((IDictionary<string, SessionHours>)_sessions).Add(key, value);
        }

        public void Add(KeyValuePair<string, SessionHours> item)
        {
            ((ICollection<KeyValuePair<string, SessionHours>>)_sessions).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<string, SessionHours>>)_sessions).Clear();
        }

        public bool Contains(KeyValuePair<string, SessionHours> item)
        {
            return ((ICollection<KeyValuePair<string, SessionHours>>)_sessions).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, SessionHours>)_sessions).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, SessionHours>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, SessionHours>>)_sessions).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, SessionHours>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, SessionHours>>)_sessions).GetEnumerator();
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, SessionHours>)_sessions).Remove(key);
        }

        public bool Remove(KeyValuePair<string, SessionHours> item)
        {
            return ((ICollection<KeyValuePair<string, SessionHours>>)_sessions).Remove(item);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out SessionHours value)
        {
            return ((IDictionary<string, SessionHours>)_sessions).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_sessions).GetEnumerator();
        }
    }

    public class SessionHours
    {
        public string Name { internal set; get; }
        public SessionHours()
        {
            Name = string.Empty;
        }

    }
}
