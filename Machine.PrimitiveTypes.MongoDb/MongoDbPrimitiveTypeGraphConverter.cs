using System;
using MongoDB.Driver;

namespace Machine.PrimitiveTypes.MongoDb
{
  public class MongoDbPrimitiveTypeGraphConverter : PrimitiveTypeGraphConverter<Document>
  {
    public MongoDbPrimitiveTypeGraphConverter()
      : base(() => new Document())
    {
      Override<Decimal>(value => value.ToString(), value => Decimal.Parse(value.ToString()));
      Override<Guid>(value => value.ToString(), value => new Guid(value.ToString()));
    }

    public override void AddToDictionary(Document dictionary, string key, object value)
    {
      dictionary[key] = value;
    }

    public override bool IsNull(Type type, object value)
    {
      return value is MongoDBNull || value == null;
    }
  }
}