using System;

namespace Machine.PrimitiveTypes.MongoDb
{
  public interface ITranslatorLocator
  {
    ITranslateEntities<T> FindEntityTranslator<T>();
    ITranslateEntities<object> FindEntityTranslator(Type type);
  }
}