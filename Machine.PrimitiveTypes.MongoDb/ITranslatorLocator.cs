namespace Machine.PrimitiveTypes.MongoDb
{
  public interface ITranslatorLocator
  {
    ITranslateEntities<T> FindEntityTranslator<T>();
  }
}