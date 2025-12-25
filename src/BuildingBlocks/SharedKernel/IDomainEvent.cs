namespace SharedKernel;

public interface IDomainEvent
{
    DateTime OccurredUtc { get; }
}