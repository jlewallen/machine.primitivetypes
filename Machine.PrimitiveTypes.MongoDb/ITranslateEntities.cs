using System.Collections;

namespace Machine.PrimitiveTypes.MongoDb
{
  public interface ITranslateEntities<T>
  {
    MongoEntity<T> Translate(IDictionary from);
    IDictionary Translate(MongoEntity<T> from);
  }
}