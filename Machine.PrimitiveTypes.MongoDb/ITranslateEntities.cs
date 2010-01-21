using MongoDB.Driver;

namespace Machine.PrimitiveTypes.MongoDb
{
  public interface ITranslateEntities<T>
  {
    MongoEntity<T> Translate(Document from);
    Document Translate(MongoEntity<T> from);
  }
}