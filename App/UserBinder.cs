using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApplication1;

public sealed class UserBinder : ModelBinderAttribute, ISwaggerIgnoreParameter
{
    public UserBinder() : base(typeof(_AuthUserBinder))
    {
    }

    private sealed class _AuthUserBinder : ModelBinderAttribute, IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var user = new User { Name = "Anton" };
            bindingContext.Result = ModelBindingResult.Success(user);
            return Task.CompletedTask;
        }
    }
}