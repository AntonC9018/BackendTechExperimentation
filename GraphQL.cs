namespace efcore_transactions;

public enum Статус
{
    Активный,
    Завершенный,
}

public class StatusType : EnumType<Статус>
{
    protected override void Configure(IEnumTypeDescriptor<Статус> descriptor)
    {
        descriptor.Name("Status");
        descriptor.Value(Статус.Активный).Name("ACTIVE");
        descriptor.Value(Статус.Завершенный).Name("COMPLETED");
    }
}

public class Thing
{
    public Статус Status { get; set; }
}

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("test")
            .Resolve(ctx => new Thing { Status = Статус.Активный });
    }
}