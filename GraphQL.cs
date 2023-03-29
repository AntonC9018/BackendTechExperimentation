namespace efcore_transactions;

public class Человек
{
    public int Идно { get; set; }
    public string Имя { get; set; }
    public string Фамилия { get; set; }
}

public class HumanType : ObjectType<Человек>
{
    protected override void Configure(IObjectTypeDescriptor<Человек> descriptor)
    {
        descriptor.Name("Human");
        descriptor.Field(x => x.Идно).Name("id");
        descriptor.Field(x => x.Имя).Name("name");
        descriptor.Field(x => x.Фамилия).Name("surname");
    }
}

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("test")
            .Resolve(ctx => new[]
            {
                new Человек
                {
                    Идно = 1,
                    Имя = "Иван",
                    Фамилия = "Иванов"
                },
                new Человек
                {
                    Идно = 2,
                    Имя = "Петр",
                    Фамилия = "Петров"
                },
            });
    }
}