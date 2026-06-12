using LightWeight.shared.BuildingBlocks;
using LightWeight.UserProfile.Domain.Enum;
using LightWeight.UserProfile.Domain.Events;

namespace LightWeight.UserProfile.Domain.Aggregates;

public sealed class User : AggregateRoot<Guid>
{
    private User
    (
        Guid Id,
        string name, 
        Sex sex,
        DateTime dateOfBirth,
        TrainingStage currentStage, 
        DateTime stageStartedAt
    ) : base(Id)
    {
        Name = name;
        Sex = sex;
        DateOfBirth = dateOfBirth;
        CurrentStage = currentStage;
        StageStartedAt = stageStartedAt;
    }

    public string Name{get; private set;}
    public Sex Sex {get;private set;}
    public DateTime DateOfBirth{get; private set;}

    public TrainingStage CurrentStage { get; private set; }
    public DateTime StageStartedAt { get; private set; }

    public static User Create
    (
        Guid UserId,
        string name, 
        Sex sex,
        DateTime dateOfBirth,
        TrainingStage currentStage, 
        DateTime now
    )
    {
        User user = new User
        (
            UserId,
            name,
            sex,
            dateOfBirth,
            currentStage,
            now
        );
        user.RaiseDomainEvent(new UserCompletedDomainEvent(UserId,now));
        return user;
    }

    public void Modify
    (
        string name, 
        Sex sex,
        DateTime dateOfBirth 
    )
    {
        this.Name = name;
        this.Sex = sex;
        this.DateOfBirth = dateOfBirth;

    }

    public void ChangeStage(TrainingStage stage) => CurrentStage = stage; //en un futuro esto podria lanzar un evento para comunicarse con measurments

}