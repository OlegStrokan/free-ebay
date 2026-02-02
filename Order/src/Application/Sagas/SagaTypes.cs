public static class SagaTypes
{
    public const string OrderSaga = "OrderSaga";
    public const string ReturnSaga = "ReturnSaga";

    public static bool IsValid(string sagaType)
    {
        return sagaType is OrderSaga or ReturnSaga;
    }

    public static IReadOnlyList<string> All = new[]
    {
        OrderSaga,
        ReturnSaga
    };
}