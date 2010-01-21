using System;
using System.Collections.Generic;

namespace Machine.PrimitiveTypes
{
  public class DictionaryPrimitiveTypeGraphConverter : PrimitiveTypeGraphConverter<IDictionary<string, object>>
  {
    public DictionaryPrimitiveTypeGraphConverter()
      : base(() => new Dictionary<string, object>())
    {
    }

    public override void AddToDictionary(IDictionary<string, object> dictionary, string key, object value)
    {
      dictionary[key] = value;
    }

    public override bool IsNull(Type type, object value)
    {
      return value == null;
    }
  }
}