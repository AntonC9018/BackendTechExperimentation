using System.Linq.Expressions;

namespace efcore_transactions;

public class Box<T>
{
    public T value;
    public Box(T value) => this.value = value;
}

public static class BoxExtensions
{
    public static Expression MakeMemberAccess<T>(this Box<T> box)
    {
        return Expression.MakeMemberAccess(
            Expression.Constant(box),
            typeof(Box<T>).GetField(nameof(box.value))!);
    }
    
    public static Box<T> Box<T>(this T value)
    {
        return new Box<T>(value);
    }
}
