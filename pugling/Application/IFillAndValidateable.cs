using System.ComponentModel.DataAnnotations;

namespace pugling.Application
{
    public interface IFillAndValidateable<out T, in S> : IValidatableObject
    {
        T FillAndValidate(S vocabulary);
    }
}
