using GodotGameTemplate.Gameplay.Actors;

namespace GodotGameTemplate.Gameplay.Input;

public interface IIntentProvider
{
    ActorIntent GetIntent();
}
