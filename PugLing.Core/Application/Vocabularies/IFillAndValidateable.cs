using System.ComponentModel.DataAnnotations;

namespace PugLing.Core.Application.Vocabularies
{
    public interface IFillAndValidateable<out T, in S> : IValidatableObject
    {
        T FillAndValidate(S vocabulary);
    }
}