using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Apache.NMS.ActiveMQ.Mock;

[ExcludeFromCodeCoverage]
internal class FakePrimitiveMap : IPrimitiveMap {
    private readonly Dictionary<string, object> _dict = new();
    public object this[string key] { get => _dict[key]; set => _dict[key] = value; }
    public ICollection Keys => _dict.Keys;
    public ICollection Values => _dict.Values;
    public int Count => _dict.Count;
    public bool Contains(string key) => _dict.ContainsKey(key);
    public bool Contains(object key) => key is string s && _dict.ContainsKey(s);
    public void Remove(string key) => _dict.Remove(key);
    public void Remove(object key) { if (key is string s) _dict.Remove(s); }
    public void Clear() => _dict.Clear();
    public void SetBool(string key, bool value) => _dict[key] = value;
    public void SetByte(string key, byte value) => _dict[key] = value;
    public void SetBytes(string key, byte[] value) => _dict[key] = value;
    public void SetBytes(string key, byte[] value, int offset, int length) { var arr = new byte[length]; Array.Copy(value, offset, arr, 0, length); _dict[key] = arr; }
    public void SetChar(string key, char value) => _dict[key] = value;
    public void SetDouble(string key, double value) => _dict[key] = value;
    public void SetFloat(string key, float value) => _dict[key] = value;
    public void SetInt(string key, int value) => _dict[key] = value;
    public void SetLong(string key, long value) => _dict[key] = value;
    public void SetShort(string key, short value) => _dict[key] = value;
    public void SetString(string key, string value) => _dict[key] = value;
    public void SetList(string key, IList value) => _dict[key] = value;
    public void SetDictionary(string key, IDictionary value) => _dict[key] = value;
    public bool GetBool(string key) => (bool)_dict[key];
    public byte GetByte(string key) => (byte)_dict[key];
    public byte[] GetBytes(string key) => (byte[])_dict[key];
    public char GetChar(string key) => (char)_dict[key];
    public double GetDouble(string key) => (double)_dict[key];
    public float GetFloat(string key) => (float)_dict[key];
    public int GetInt(string key) => (int)_dict[key];
    public long GetLong(string key) => (long)_dict[key];
    public short GetShort(string key) => (short)_dict[key];
    public string GetString(string key) => (string)_dict[key];
    public IList GetList(string key) => (IList)_dict[key];
    public IDictionary GetDictionary(string key) => (IDictionary)_dict[key];
    public IEnumerator GetEnumerator() => _dict.GetEnumerator();
}