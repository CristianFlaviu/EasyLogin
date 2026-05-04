namespace EasyLogin.Domain.Entities;

public abstract class BaseEntity<TKey>
{
    public required TKey Id { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
