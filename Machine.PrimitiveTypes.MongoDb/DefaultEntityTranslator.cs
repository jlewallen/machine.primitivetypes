using MongoDB.Driver;

namespace Machine.PrimitiveTypes.MongoDb
{
  public class DefaultEntityTranslator<T> : ITranslateEntities<T>
  {
    readonly MongoDbPrimitiveTypeGraphConverter _converter = new MongoDbPrimitiveTypeGraphConverter();

    public MongoEntity<T> Translate(Document from)
    {
      var converted = _converter.FromPrimitiveTypeGraph<T>(from);
      return new MongoEntity<T>(converted, from);
    }

    public Document Translate(MongoEntity<T> from)
    {
      var converted = (Document)_converter.ToPrimitiveTypeGraph(from.Entity);
      if (from.Attributes != null)
      {
        converted["_id"] = from.Attributes["_id"];
      }
      return converted;
    }
  }
}